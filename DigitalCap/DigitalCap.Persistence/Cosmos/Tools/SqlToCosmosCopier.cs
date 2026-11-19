using Dapper;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Tools
{
    public class CopyResult
    {
        public string table { get; set; } = string.Empty;
        public string container { get; set; } = string.Empty;
        public string idSource { get; set; } = "PK";
        public int sqlRows { get; set; }
        public int upserted { get; set; }
        public int failed { get; set; }
        public List<string> errors { get; set; } = new();
        public double requestChargeTotal { get; set; }
        public double seconds { get; set; }
        public int? counterSeededTo { get; set; }
    }

    public class SqlToCosmosCopier
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICosmosContainerFactory _containerFactory;
        private readonly ILogger<SqlToCosmosCopier> _logger;

        public SqlToCosmosCopier(
            IUnitOfWork unitOfWork,
            ICosmosContainerFactory containerFactory,
            ILogger<SqlToCosmosCopier> logger)
        {
            _unitOfWork = unitOfWork;
            _containerFactory = containerFactory;
            _logger = logger;
        }

        public async Task<CopyResult> CopyTableAsync(string schema, string table, string? idColumn = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(schema))
            {
                throw new ArgumentException("Schema must be specified.", nameof(schema));
            }
            if (string.IsNullOrWhiteSpace(table))
            {
                throw new ArgumentException("Table must be specified.", nameof(table));
            }

            // 1. Validate container exists in CosmosContainers
            var expectedContainer = $"{schema}_{table}".Replace(" ", "_");
            var matchedKey = CosmosContainers.PartitionKeys.Keys
                .FirstOrDefault(k => string.Equals(k, expectedContainer, StringComparison.OrdinalIgnoreCase));

            if (matchedKey == null)
            {
                throw new ArgumentException($"Container '{expectedContainer}' does not exist in CosmosContainers.");
            }
            var containerName = matchedKey;

            var connection = _unitOfWork.Connection;
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            // 1b. Validate table exists in INFORMATION_SCHEMA.TABLES (BASE TABLE)
            var tableInfo = await connection.QueryFirstOrDefaultAsync<(string Schema, string Table)>(new CommandDefinition(
                "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table AND TABLE_TYPE = 'BASE TABLE'",
                new { schema, table },
                cancellationToken: ct
            ));

            if (tableInfo.Schema == null)
            {
                throw new ArgumentException($"Table [{schema}].[{table}] was not found in INFORMATION_SCHEMA.TABLES as a BASE TABLE.");
            }

            // 2. Primary key columns (ordered by ORDINAL_POSITION)
            var pkSql = @"
                SELECT kcu.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                  ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                 AND tc.TABLE_SCHEMA = kcu.TABLE_SCHEMA
                 AND tc.TABLE_NAME = kcu.TABLE_NAME
                WHERE tc.TABLE_SCHEMA = @schema
                  AND tc.TABLE_NAME = @table
                  AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                ORDER BY kcu.ORDINAL_POSITION";

            var pkColumns = (await connection.QueryAsync<string>(new CommandDefinition(
                pkSql,
                new { schema = tableInfo.Schema, table = tableInfo.Table },
                cancellationToken: ct
            ))).ToList();

            var idSource = "PK";
            if (!string.IsNullOrWhiteSpace(idColumn))
            {
                var requestedCols = idColumn.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                if (requestedCols.Count == 0)
                {
                    throw new ArgumentException("idColumn parameter was empty or contained only separators.");
                }

                foreach (var col in requestedCols)
                {
                    var hasNamedCol = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                        @"SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS 
                          WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table AND COLUMN_NAME = @col",
                        new { schema = tableInfo.Schema, table = tableInfo.Table, col },
                        cancellationToken: ct
                    )) > 0;

                    if (!hasNamedCol)
                    {
                        throw new ArgumentException($"Column '{col}' was not found in table [{tableInfo.Schema}].[{tableInfo.Table}].");
                    }
                }

                var nullConditions = string.Join(" OR ", requestedCols.Select(c => $"[{c.Replace("]", "]]")}] IS NULL"));
                var colListSql = string.Join(", ", requestedCols.Select(c => $"[{c.Replace("]", "]]")}]"));

                var idStats = await connection.QueryFirstOrDefaultAsync<(int TotalRows, int NullIds, int DuplicateIds)>(new CommandDefinition(
                    $@"SELECT 
                         COUNT(1) AS TotalRows,
                         COUNT(CASE WHEN {nullConditions} THEN 1 END) AS NullIds,
                         COUNT(1) - (SELECT COUNT(1) FROM (SELECT DISTINCT {colListSql} FROM [{tableInfo.Schema.Replace("]", "]]")}].[{tableInfo.Table.Replace("]", "]]")}]) AS _d) AS DuplicateIds
                       FROM [{tableInfo.Schema.Replace("]", "]]")}].[{tableInfo.Table.Replace("]", "]]")}]",
                    cancellationToken: ct
                ));

                if (idStats.NullIds == 0 && idStats.DuplicateIds == 0)
                {
                    pkColumns = requestedCols;
                    idSource = $"{string.Join(",", requestedCols)} column(s) (no PK)";
                }
                else
                {
                    throw new InvalidOperationException($"Table [{tableInfo.Schema}].[{tableInfo.Table}] column(s) '{string.Join(",", requestedCols)}' has {idStats.NullIds} nulls and {idStats.DuplicateIds} duplicates across {idStats.TotalRows} rows.");
                }
            }
            else if (pkColumns.Count == 0)
            {
                var hasIdCol = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                    @"SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS 
                      WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table AND COLUMN_NAME = 'Id'",
                    new { schema = tableInfo.Schema, table = tableInfo.Table },
                    cancellationToken: ct
                )) > 0;

                if (hasIdCol)
                {
                    var idStats = await connection.QueryFirstOrDefaultAsync<(int TotalRows, int NullIds, int DuplicateIds)>(new CommandDefinition(
                        $@"SELECT 
                             COUNT(1) AS TotalRows,
                             COUNT(CASE WHEN [Id] IS NULL THEN 1 END) AS NullIds,
                             COUNT(1) - COUNT(DISTINCT [Id]) AS DuplicateIds
                           FROM [{tableInfo.Schema.Replace("]", "]]")}].[{tableInfo.Table.Replace("]", "]]")}]",
                        cancellationToken: ct
                    ));

                    if (idStats.NullIds == 0 && idStats.DuplicateIds == 0)
                    {
                        pkColumns = new List<string> { "Id" };
                        idSource = "Id column (no PK)";
                    }
                    else
                    {
                        throw new InvalidOperationException($"Table [{tableInfo.Schema}].[{tableInfo.Table}] has no primary key, and 'Id' column has {idStats.NullIds} nulls and {idStats.DuplicateIds} duplicates across {idStats.TotalRows} rows.");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Table [{tableInfo.Schema}].[{tableInfo.Table}] has no primary key and no 'Id' column defined.");
                }
            }

            // 5. Partition key path from CosmosContainers
            var pkPath = CosmosContainers.PartitionKeys[containerName];
            var partitionKeyPropName = pkPath.StartsWith('/') ? pkPath.Substring(1) : pkPath;

            var safeSchema = tableInfo.Schema.Replace("]", "]]");
            var safeTable = tableInfo.Table.Replace("]", "]]");
            var selectSql = $"SELECT * FROM [{safeSchema}].[{safeTable}]";

            // 3. Read rows (stream with buffered: false)
            var rows = await connection.QueryAsync(new CommandDefinition(
                selectSql,
                flags: CommandFlags.None,
                cancellationToken: ct
            ));

            var container = _containerFactory.GetContainer(containerName);

            int sqlRows = 0;
            int upserted = 0;
            int failed = 0;
            var errors = new List<string>();
            double requestChargeTotal = 0;
            var statsLock = new object();

            // 6. Up to 20 in parallel
            using var semaphore = new SemaphoreSlim(20, 20);
            var tasks = new List<Task>();
            var sw = Stopwatch.StartNew();

            foreach (var row in rows)
            {
                ct.ThrowIfCancellationRequested();
                sqlRows++;

                var dict = (IDictionary<string, object>)row;

                // 4. Case-sensitive check for column named "id"
                if (dict.Keys.Any(k => string.Equals(k, "id", StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException($"Table [{tableInfo.Schema}].[{tableInfo.Table}] has a column named lowercase 'id', which conflicts with Cosmos id.");
                }

                // 4. "id" = primary-key value as string (GUID lowercase); composite key -> joined with "|"
                var idString = string.Join("|", pkColumns.Select(c => FormatPkValue(dict.TryGetValue(c, out var v) ? v : null)));

                // 4. Build JSON object with one property per SQL column, same name and case
                var jObject = new JObject();
                foreach (var kvp in dict)
                {
                    jObject[kvp.Key] = ConvertSqlValueToJToken(kvp.Value);
                }
                jObject["id"] = idString;

                // 5. Partition key value with its JSON type
                var pkToken = jObject[partitionKeyPropName];
                PartitionKey partitionKey;
                if (pkToken == null || pkToken.Type == JTokenType.Null || pkToken.Type == JTokenType.Undefined)
                {
                    partitionKey = PartitionKey.Null;
                }
                else
                {
                    switch (pkToken.Type)
                    {
                        case JTokenType.String:
                            partitionKey = new PartitionKey((string)pkToken!);
                            break;
                        case JTokenType.Integer:
                            partitionKey = new PartitionKey((double)(long)pkToken);
                            break;
                        case JTokenType.Float:
                            partitionKey = new PartitionKey((double)pkToken);
                            break;
                        case JTokenType.Boolean:
                            partitionKey = new PartitionKey((bool)pkToken);
                            break;
                        default:
                            partitionKey = new PartitionKey(pkToken.ToString());
                            break;
                    }
                }

                await semaphore.WaitAsync(ct);

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var response = await container.UpsertItemAsync(jObject, partitionKey, cancellationToken: ct);
                        lock (statsLock)
                        {
                            upserted++;
                            requestChargeTotal += response.RequestCharge;
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (statsLock)
                        {
                            failed++;
                            if (errors.Count < 20)
                            {
                                errors.Add(ex.Message);
                            }
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, ct));

                if (tasks.Count >= 500)
                {
                    tasks.RemoveAll(t => t.IsCompleted);
                }
            }

            await Task.WhenAll(tasks);
            sw.Stop();

            // 7. Seed sys_Counters if table has an int IDENTITY column
            int? counterSeededTo = null;
            string? identityCol = null;
            try
            {
                var identityColSql = @"
                    SELECT c.name
                    FROM sys.identity_columns ic
                    JOIN sys.tables t ON ic.object_id = t.object_id
                    JOIN sys.schemas s ON t.schema_id = s.schema_id
                    JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                    WHERE s.name = @schema AND t.name = @table";

                identityCol = await connection.ExecuteScalarAsync<string>(new CommandDefinition(
                    identityColSql,
                    new { schema = tableInfo.Schema, table = tableInfo.Table },
                    cancellationToken: ct));

                if (!string.IsNullOrEmpty(identityCol))
                {
                    var maxValSql = $"SELECT MAX([{identityCol.Replace("]", "]]")}]) FROM [{safeSchema}].[{safeTable}]";
                    var maxValObj = await connection.ExecuteScalarAsync<object>(new CommandDefinition(
                        maxValSql,
                        cancellationToken: ct));

                    int maxVal = 0;
                    if (maxValObj != null && maxValObj != DBNull.Value && int.TryParse(maxValObj.ToString(), out var parsedVal))
                    {
                        maxVal = parsedVal;
                    }

                    var countersContainer = _containerFactory.GetContainer(CosmosContainers.sys_Counters);
                    var pk = new PartitionKey(containerName);

                    for (int attempt = 1; attempt <= 5; attempt++)
                    {
                        try
                        {
                            ItemResponse<JObject> response;
                            try
                            {
                                response = await countersContainer.ReadItemAsync<JObject>(containerName, pk, cancellationToken: ct);
                            }
                            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                var newDoc = new JObject
                                {
                                    ["id"] = containerName,
                                    ["Current"] = maxVal
                                };
                                await countersContainer.CreateItemAsync(newDoc, pk, cancellationToken: ct);
                                counterSeededTo = maxVal;
                                break;
                            }

                            var doc = response.Resource;
                            var curVal = doc["Current"]?.Value<int>() ?? 0;
                            if (maxVal > curVal)
                            {
                                doc["Current"] = maxVal;
                                var options = new ItemRequestOptions { IfMatchEtag = response.ETag };
                                await countersContainer.ReplaceItemAsync(doc, containerName, pk, options, cancellationToken: ct);
                                counterSeededTo = maxVal;
                            }
                            else
                            {
                                counterSeededTo = curVal;
                            }
                            break;
                        }
                        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed || ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                        {
                            if (attempt == 5) throw;
                            await Task.Delay(50 * attempt, ct);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed sys_Counters for {ContainerName}.", containerName);
                errors.Add($"Failed to seed sys_Counters for {containerName}: {ex.Message}");
                counterSeededTo = null;
                failed++;
            }

            if (!string.IsNullOrEmpty(identityCol) && !counterSeededTo.HasValue)
            {
                if (!errors.Any(e => e.Contains("sys_Counters") || e.Contains("IDENTITY")))
                {
                    errors.Add($"Table [{tableInfo.Schema}].[{tableInfo.Table}] has IDENTITY column '{identityCol}', but counter could not be confirmed.");
                }
                failed++;
            }

            return new CopyResult
            {
                table = $"{tableInfo.Schema}.{tableInfo.Table}",
                container = containerName,
                idSource = idSource,
                sqlRows = sqlRows,
                upserted = upserted,
                failed = failed,
                errors = errors,
                requestChargeTotal = Math.Round(requestChargeTotal, 2),
                seconds = Math.Round(sw.Elapsed.TotalSeconds, 2),
                counterSeededTo = counterSeededTo
            };
        }

        private static string FormatPkValue(object? val)
        {
            if (val == null || val is DBNull)
            {
                return string.Empty;
            }

            return val switch
            {
                Guid g => g.ToString("D").ToLowerInvariant(),
                DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
                DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
                _ => val.ToString() ?? string.Empty
            };
        }

        private static JToken ConvertSqlValueToJToken(object? val)
        {
            if (val == null || val is DBNull)
            {
                return JValue.CreateNull();
            }

            return val switch
            {
                bool b => new JValue(b),
                Guid g => new JValue(g.ToString("D").ToLowerInvariant()),
                DateTime dt => new JValue(dt.ToString("o", CultureInfo.InvariantCulture)),
                DateTimeOffset dto => new JValue(dto.ToString("o", CultureInfo.InvariantCulture)),
                byte[] bytes => new JValue(Convert.ToBase64String(bytes)),
                string s => new JValue(s),
                byte num => new JValue(num),
                sbyte num => new JValue(num),
                short num => new JValue(num),
                ushort num => new JValue(num),
                int num => new JValue(num),
                uint num => new JValue(num),
                long num => new JValue(num),
                ulong num => new JValue(num),
                float num => new JValue(num),
                double num => new JValue(num),
                decimal num => new JValue(num),
                TimeSpan ts => new JValue(ts.ToString("c")),
                _ => JToken.FromObject(val)
            };
        }
    }
}
