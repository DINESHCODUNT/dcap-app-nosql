using Dapper;
using DigitalCap.Core.DTO;
using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Permissions;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Security;
using DigitalCap.Infrastructure.DbContext;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using DigitalCap.Persistence.Cosmos.Repositories;
using DigitalCap.Persistence.Cosmos.Tools;
using DigitalCap.Persistence.Repositories;
using DigitalCap.WebApi.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalCap.Server.DevTools
{
    public static class ParityCompare
    {
        public static object Lists<T>(
            string check,
            IEnumerable<T> sql,
            IEnumerable<T> cosmos,
            Func<T, object>? sortKey,
            bool ordered,
            params (string Name, Func<T, object?> Get)[] props)
        {
            var sqlList = (ordered || sortKey == null ? sql : sql.OrderBy(sortKey)).ToList();
            var cosmosList = (ordered || sortKey == null ? cosmos : cosmos.OrderBy(sortKey)).ToList();

            if (sqlList.Count != cosmosList.Count)
            {
                return new
                {
                    check,
                    sqlCount = sqlList.Count,
                    cosmosCount = cosmosList.Count,
                    equal = false,
                    firstDifference = $"Count mismatch: SQL has {sqlList.Count}, Cosmos has {cosmosList.Count}"
                };
            }

            for (int i = 0; i < sqlList.Count; i++)
            {
                var s = sqlList[i];
                var c = cosmosList[i];
                foreach (var (propName, getVal) in props)
                {
                    var valS = getVal(s);
                    var valC = getVal(c);
                    if (!Equals(valS, valC))
                    {
                        return new
                        {
                            check,
                            sqlCount = sqlList.Count,
                            cosmosCount = cosmosList.Count,
                            equal = false,
                            firstDifference = $"At index {i}: SQL({propName}={valS}) != Cosmos({propName}={valC})"
                        };
                    }
                }
            }

            return new
            {
                check,
                sqlCount = sqlList.Count,
                cosmosCount = cosmosList.Count,
                equal = true,
                firstDifference = (string?)null
            };
        }

        public static object Single<T>(
            string check,
            T? sql,
            T? cosmos,
            params (string Name, Func<T, object?> Get)[] props)
        {
            int sqlCount = sql == null ? 0 : 1;
            int cosmosCount = cosmos == null ? 0 : 1;

            if (sql == null && cosmos == null)
            {
                return new
                {
                    check,
                    sqlCount = 0,
                    cosmosCount = 0,
                    equal = true,
                    firstDifference = (string?)null
                };
            }

            if (sql == null || cosmos == null)
            {
                return new
                {
                    check,
                    sqlCount,
                    cosmosCount,
                    equal = false,
                    firstDifference = sql == null ? "SQL is null, Cosmos is not null" : "SQL is not null, Cosmos is null"
                };
            }

            foreach (var (propName, getVal) in props)
            {
                var valS = getVal(sql);
                var valC = getVal(cosmos);
                if (!Equals(valS, valC))
                {
                    return new
                    {
                        check,
                        sqlCount = 1,
                        cosmosCount = 1,
                        equal = false,
                        firstDifference = $"SQL({propName}={valS}) != Cosmos({propName}={valC})"
                    };
                }
            }

            return new
            {
                check,
                sqlCount = 1,
                cosmosCount = 1,
                equal = true,
                firstDifference = (string?)null
            };
        }
    }

    public static class CosmosDevEndpoints
    {
        public static WebApplication MapCosmosDevEndpoints(this WebApplication app)
        {
            MapHealthIdentity(app);
            MapParityBatch1(app);
            MapParityBatch2(app);
            MapParityBatch3(app);
            MapParityBatch4(app);
            MapParityBatch5(app);
            MapDevCopyTable(app);
            return app;
        }

        private static void MapHealthIdentity(WebApplication app)
        {
            app.MapGet("/health/identity", async (
                string email,
                bool? testUpdate,
                IConfiguration config,
                UserManager<ApplicationUser> userManager,
                ApplicationDbContext db) =>
            {
                var provider = config["Identity:Provider"] ?? "Sql";
                var isCosmos = string.Equals(provider, "Cosmos", StringComparison.OrdinalIgnoreCase);
                try
                {
                    var user = await userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        return Results.Ok(new
                        {
                            provider,
                            userFound = false,
                            userId = (string?)null,
                            userName = (string?)null,
                            roles = Array.Empty<string>(),
                            isInFirstRole = false
                        });
                    }

                    var roles = await userManager.GetRolesAsync(user);
                    var firstRole = roles.FirstOrDefault();
                    var isInFirstRole = !string.IsNullOrEmpty(firstRole) && await userManager.IsInRoleAsync(user, firstRole);

                    string? userRoleIdCheck = null;
                    string? userRoleGeneratedId = null;
                    string? userRoleStoredId = null;
                    bool? userRoleIdMatches = null;

                    if (!isCosmos)
                    {
                        userRoleIdCheck = "not applicable (Sql)";
                    }
                    else
                    {
                        var storedLinks = await db.Set<IdentityUserRole<string>>()
                            .AsNoTracking()
                            .Where(ur => ur.UserId == user.Id)
                            .ToListAsync();

                        if (storedLinks.Count == 0)
                        {
                            userRoleIdCheck = "no link found";
                            userRoleIdMatches = false;
                        }
                        else
                        {
                            var link = storedLinks[0];
                            var probe = new IdentityUserRole<string> { UserId = link.UserId, RoleId = link.RoleId };
                            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<IdentityUserRole<string>>? entry = null;
                            string? generatedId = null;
                            string? idPropertyName = null;
                            try
                            {
                                entry = db.Add(probe);
                                var idProperty = entry.Metadata.GetProperties()
                                    .FirstOrDefault(p => CosmosPropertyExtensions.GetJsonPropertyName(p) == "id");
                                if (idProperty != null)
                                {
                                    idPropertyName = idProperty.Name;
                                    generatedId = entry.Property(idPropertyName).CurrentValue?.ToString();
                                }
                            }
                            finally
                            {
                                if (entry != null)
                                {
                                    entry.State = EntityState.Detached;
                                }
                            }

                            string? storedId = null;
                            if (!string.IsNullOrEmpty(idPropertyName))
                            {
                                storedId = await db.Set<IdentityUserRole<string>>()
                                    .AsNoTracking()
                                    .Where(ur => ur.UserId == link.UserId && ur.RoleId == link.RoleId)
                                    .Select(ur => EF.Property<string>(ur, idPropertyName))
                                    .FirstOrDefaultAsync();
                            }

                            userRoleGeneratedId = generatedId;
                            userRoleStoredId = storedId;
                            userRoleIdMatches = generatedId != null && storedId != null && generatedId == storedId;
                            userRoleIdCheck = userRoleIdMatches == true ? "matches" : "mismatch";
                        }
                    }

                    if (testUpdate == true)
                    {
                        var updateResult = await userManager.UpdateAsync(user);
                        return Results.Ok(new
                        {
                            provider,
                            userFound = true,
                            userId = user.Id,
                            userName = user.UserName,
                            roles,
                            isInFirstRole,
                            userRoleIdCheck,
                            userRoleGeneratedId,
                            userRoleStoredId,
                            userRoleIdMatches,
                            updateSucceeded = updateResult.Succeeded,
                            updateErrors = updateResult.Errors.Select(e => e.Description)
                        });
                    }

                    return Results.Ok(new
                    {
                        provider,
                        userFound = true,
                        userId = user.Id,
                        userName = user.UserName,
                        roles,
                        isInFirstRole,
                        userRoleIdCheck,
                        userRoleGeneratedId,
                        userRoleStoredId,
                        userRoleIdMatches
                    });
                }
                catch (Exception ex)
                {
                    return Results.Json(new
                    {
                        provider,
                        error = ex.Message
                    }, statusCode: StatusCodes.Status500InternalServerError);
                }
            }).AllowAnonymous();
        }

        private static void MapParityBatch1(WebApplication app)
        {
            app.MapGet("/health/parity/batch1", async (
                ClassRepository sqlClassRepo,
                CosmosClassRepository cosmosClassRepo,
                PortRepository sqlPortRepo,
                CosmosPortRepository cosmosPortRepo,
                ClientRepository sqlClientRepo,
                CosmosClientRepository cosmosClientRepo) =>
            {
                var checks = new List<object>();
                var hadException = false;

                // 1. Class.Search
                try
                {
                    var sqlList = await sqlClassRepo.Search();
                    var cosmosList = await cosmosClassRepo.Search();
                    checks.Add(ParityCompare.Lists(
                        "Class.Search",
                        sqlList,
                        cosmosList,
                        c => c.Id,
                        false,
                        ("Id", c => c.Id),
                        ("Name", c => c.Name),
                        ("Abbreviation", c => c.Abbreviation)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Class.Search", equal = false, error = ex.Message });
                }

                // Shared for Port tests
                List<Port>? allSqlPorts = null;

                // 2. Port.Search(null)
                try
                {
                    allSqlPorts = (await sqlPortRepo.Search(null!)).ToList();
                    var cosmosList = await cosmosPortRepo.Search(null!);
                    checks.Add(ParityCompare.Lists(
                        "Port.Search(null)",
                        allSqlPorts,
                        cosmosList,
                        p => p.Id,
                        false,
                        ("Id", p => p.Id),
                        ("Code", p => p.Code),
                        ("Name", p => p.Name),
                        ("CountryCode", p => p.CountryCode)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Port.Search(null)", equal = false, error = ex.Message });
                }

                // 3. Port.Search("an")
                try
                {
                    var sqlList = await sqlPortRepo.Search("an");
                    var cosmosList = await cosmosPortRepo.Search("an");
                    checks.Add(ParityCompare.Lists(
                        "Port.Search(\"an\")",
                        sqlList,
                        cosmosList,
                        p => p.Id,
                        false,
                        ("Id", p => p.Id),
                        ("Code", p => p.Code),
                        ("Name", p => p.Name),
                        ("CountryCode", p => p.CountryCode)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Port.Search(\"an\")", equal = false, error = ex.Message });
                }

                // 4. Port.Search("AN")
                try
                {
                    var sqlList = await sqlPortRepo.Search("AN");
                    var cosmosList = await cosmosPortRepo.Search("AN");
                    checks.Add(ParityCompare.Lists(
                        "Port.Search(\"AN\")",
                        sqlList,
                        cosmosList,
                        p => p.Id,
                        false,
                        ("Id", p => p.Id),
                        ("Code", p => p.Code),
                        ("Name", p => p.Name),
                        ("CountryCode", p => p.CountryCode)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Port.Search(\"AN\")", equal = false, error = ex.Message });
                }

                // 5. Port.Search("zzzznotfound")
                try
                {
                    var sqlList = await sqlPortRepo.Search("zzzznotfound");
                    var cosmosList = await cosmosPortRepo.Search("zzzznotfound");
                    checks.Add(ParityCompare.Lists(
                        "Port.Search(\"zzzznotfound\")",
                        sqlList,
                        cosmosList,
                        p => p.Id,
                        false,
                        ("Id", p => p.Id),
                        ("Code", p => p.Code),
                        ("Name", p => p.Name),
                        ("CountryCode", p => p.CountryCode)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Port.Search(\"zzzznotfound\")", equal = false, error = ex.Message });
                }

                // 6. Port.Search("")
                try
                {
                    var sqlList = await sqlPortRepo.Search("");
                    var cosmosList = await cosmosPortRepo.Search("");
                    checks.Add(ParityCompare.Lists(
                        "Port.Search(\"\")",
                        sqlList,
                        cosmosList,
                        p => p.Id,
                        false,
                        ("Id", p => p.Id),
                        ("Code", p => p.Code),
                        ("Name", p => p.Name),
                        ("CountryCode", p => p.CountryCode)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Port.Search(\"\")", equal = false, error = ex.Message });
                }

                // 7. Port.GetById(first)
                try
                {
                    var firstId = allSqlPorts != null && allSqlPorts.Count > 0 ? allSqlPorts.Min(p => p.Id) : 1;
                    var sqlPort = await sqlPortRepo.GetById(firstId);
                    var cosmosPort = await cosmosPortRepo.GetById(firstId);
                    checks.Add(ParityCompare.Single(
                        "Port.GetById(first)",
                        sqlPort,
                        cosmosPort,
                        ("Id", p => p.Id),
                        ("Code", p => p.Code),
                        ("Name", p => p.Name),
                        ("CountryCode", p => p.CountryCode)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Port.GetById(first)", equal = false, error = ex.Message });
                }

                // 8. Port.GetById(-1)
                try
                {
                    var sqlPort = await sqlPortRepo.GetById(-1);
                    var cosmosPort = await cosmosPortRepo.GetById(-1);
                    checks.Add(ParityCompare.Single(
                        "Port.GetById(-1)",
                        sqlPort,
                        cosmosPort,
                        ("Id", p => p.Id),
                        ("Code", p => p.Code),
                        ("Name", p => p.Name),
                        ("CountryCode", p => p.CountryCode)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Port.GetById(-1)", equal = false, error = ex.Message });
                }

                // 9. Client.GetAllClients
                try
                {
                    var sqlList = await sqlClientRepo.GetAllClients();
                    var cosmosList = await cosmosClientRepo.GetAllClients();
                    checks.Add(ParityCompare.Lists(
                        "Client.GetAllClients",
                        sqlList,
                        cosmosList,
                        c => c.Id,
                        false,
                        ("Id", c => c.Id),
                        ("Wcn", c => c.Wcn),
                        ("CompanyName", c => c.CompanyName),
                        ("CompanyAddress", c => c.CompanyAddress),
                        ("CompanyBillingEmail", c => c.CompanyBillingEmail),
                        ("CompanyBillingSystemUrl", c => c.CompanyBillingSystemUrl)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Client.GetAllClients", equal = false, error = ex.Message });
                }

                var allEqual = !hadException && checks.All(c =>
                {
                    var prop = c.GetType().GetProperty("equal");
                    return prop != null && (bool)prop.GetValue(c)!;
                });

                if (hadException)
                {
                    return Results.Json(new { allEqual = false, checks }, statusCode: StatusCodes.Status500InternalServerError);
                }

                return Results.Ok(new { allEqual, checks });
            }).AllowAnonymous();
        }

        private static void MapParityBatch2(WebApplication app)
        {
            app.MapGet("/health/parity/batch2", async (
                ClientProfileRepository sqlClientProfileRepo,
                CosmosClientProfileRepository cosmosClientProfileRepo,
                SecurityClientRepository sqlSecurityClientRepo,
                CosmosSecurityClientRepository cosmosSecurityClientRepo,
                string? writeTest) =>
            {
                var checks = new List<object>();
                var hadException = false;
                List<Client>? allSqlClients = null;

                // 1. ClientProfile.GetAllClientsAsync
                try
                {
                    allSqlClients = (await sqlClientProfileRepo.GetAllClientsAsync()).ToList();
                    var cosmosList = await cosmosClientProfileRepo.GetAllClientsAsync();
                    checks.Add(ParityCompare.Lists(
                        "ClientProfile.GetAllClientsAsync",
                        allSqlClients,
                        cosmosList,
                        c => c.Id,
                        false,
                        ("Id", c => c.Id),
                        ("Wcn", c => c.Wcn),
                        ("CompanyName", c => c.CompanyName),
                        ("CompanyAddress", c => c.CompanyAddress),
                        ("CompanyBillingEmail", c => c.CompanyBillingEmail),
                        ("CompanyBillingSystemUrl", c => c.CompanyBillingSystemUrl)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "ClientProfile.GetAllClientsAsync", equal = false, error = ex.Message });
                }

                // 2. ClientProfile.GetAllClients
                try
                {
                    var sqlList = await sqlClientProfileRepo.GetAllClients();
                    var cosmosList = await cosmosClientProfileRepo.GetAllClients();
                    checks.Add(ParityCompare.Lists(
                        "ClientProfile.GetAllClients",
                        sqlList,
                        cosmosList,
                        c => c.Id,
                        false,
                        ("Id", c => c.Id),
                        ("Wcn", c => c.Wcn),
                        ("CompanyName", c => c.CompanyName),
                        ("CompanyAddress", c => c.CompanyAddress),
                        ("CompanyBillingEmail", c => c.CompanyBillingEmail),
                        ("CompanyBillingSystemUrl", c => c.CompanyBillingSystemUrl)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "ClientProfile.GetAllClients", equal = false, error = ex.Message });
                }

                // 3. ClientProfile.GetClientAsync(first)
                try
                {
                    var firstId = allSqlClients != null && allSqlClients.Count > 0
                        ? allSqlClients.OrderBy(c => c.Id).First().Id
                        : Guid.Empty;
                    var sqlClient = await sqlClientProfileRepo.GetClientAsync(firstId);
                    var cosmosClient = await cosmosClientProfileRepo.GetClientAsync(firstId);
                    checks.Add(ParityCompare.Single(
                        "ClientProfile.GetClientAsync(first)",
                        sqlClient,
                        cosmosClient,
                        ("Id", c => c.Id),
                        ("Wcn", c => c.Wcn),
                        ("CompanyName", c => c.CompanyName),
                        ("CompanyAddress", c => c.CompanyAddress),
                        ("CompanyBillingEmail", c => c.CompanyBillingEmail),
                        ("CompanyBillingSystemUrl", c => c.CompanyBillingSystemUrl)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "ClientProfile.GetClientAsync(first)", equal = false, error = ex.Message });
                }

                // 4. ClientProfile.GetClientAsync(new Guid)
                try
                {
                    var randomId = Guid.NewGuid();
                    var sqlClient = await sqlClientProfileRepo.GetClientAsync(randomId);
                    var cosmosClient = await cosmosClientProfileRepo.GetClientAsync(randomId);
                    checks.Add(ParityCompare.Single(
                        "ClientProfile.GetClientAsync(new Guid)",
                        sqlClient,
                        cosmosClient,
                        ("Id", c => c.Id),
                        ("Wcn", c => c.Wcn),
                        ("CompanyName", c => c.CompanyName),
                        ("CompanyAddress", c => c.CompanyAddress),
                        ("CompanyBillingEmail", c => c.CompanyBillingEmail),
                        ("CompanyBillingSystemUrl", c => c.CompanyBillingSystemUrl)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "ClientProfile.GetClientAsync(new Guid)", equal = false, error = ex.Message });
                }

                // 5. ClientProfile.GetClientAdminsAsync(<each client id>)
                try
                {
                    var testClientIds = (allSqlClients != null ? allSqlClients.Select(c => c.Id) : Enumerable.Empty<Guid>())
                        .Append(Guid.Empty)
                        .Distinct()
                        .ToList();

                    var strCmp = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
                    var nullsFirstCmp = Comparer<string?>.Create((x, y) =>
                    {
                        if (x == null && y == null) return 0;
                        if (x == null) return -1;
                        if (y == null) return 1;
                        return strCmp.Compare(x, y);
                    });

                    string? adminDiff = null;
                    int clientsTested = 0;
                    foreach (var cid in testClientIds)
                    {
                        clientsTested++;
                        var sqlAdmins = (await sqlClientProfileRepo.GetClientAdminsAsync(cid)).ToList();
                        var cosmosAdmins = (await cosmosClientProfileRepo.GetClientAdminsAsync(cid)).ToList();

                        if (sqlAdmins.Count != cosmosAdmins.Count)
                        {
                            adminDiff = $"ClientId {cid}: count mismatch (SQL {sqlAdmins.Count}, Cosmos {cosmosAdmins.Count})";
                            break;
                        }

                        var sOrdered = sqlAdmins
                            .OrderBy(a => a.FirstName, nullsFirstCmp)
                            .ThenBy(a => a.LastName, nullsFirstCmp)
                            .ThenBy(a => a.Email, nullsFirstCmp)
                            .ThenBy(a => a.UserRole, nullsFirstCmp)
                            .ToList();

                        var cOrdered = cosmosAdmins
                            .OrderBy(a => a.FirstName, nullsFirstCmp)
                            .ThenBy(a => a.LastName, nullsFirstCmp)
                            .ThenBy(a => a.Email, nullsFirstCmp)
                            .ThenBy(a => a.UserRole, nullsFirstCmp)
                            .ToList();

                        for (int i = 0; i < sOrdered.Count; i++)
                        {
                            var s = sOrdered[i];
                            var c = cOrdered[i];
                            if (!string.Equals(s.FirstName, c.FirstName, StringComparison.OrdinalIgnoreCase) ||
                                !string.Equals(s.LastName, c.LastName, StringComparison.OrdinalIgnoreCase) ||
                                !string.Equals(s.Email, c.Email, StringComparison.OrdinalIgnoreCase) ||
                                !string.Equals(s.UserRole, c.UserRole, StringComparison.OrdinalIgnoreCase))
                            {
                                adminDiff = $"ClientId {cid} at index {i}: SQL({s.FirstName},{s.LastName},{s.Email},{s.UserRole}) != Cosmos({c.FirstName},{c.LastName},{c.Email},{c.UserRole})";
                                break;
                            }
                        }
                        if (adminDiff != null) break;
                    }
                    checks.Add(new
                    {
                        check = "ClientProfile.GetClientAdminsAsync",
                        clientsTested,
                        equal = adminDiff == null,
                        firstDifference = adminDiff
                    });
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "ClientProfile.GetClientAdminsAsync", equal = false, error = ex.Message });
                }

                // 6. SecurityClient.GetAllClientsAsync
                try
                {
                    var sqlSecList = await sqlSecurityClientRepo.GetAllClientsAsync();
                    var cosmosSecList = await cosmosSecurityClientRepo.GetAllClientsAsync();
                    checks.Add(ParityCompare.Lists(
                        "SecurityClient.GetAllClientsAsync",
                        sqlSecList,
                        cosmosSecList,
                        c => c.Id,
                        false,
                        ("Id", c => c.Id),
                        ("WCN", c => c.WCN),
                        ("CompanyName", c => c.CompanyName)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "SecurityClient.GetAllClientsAsync", equal = false, error = ex.Message });
                }

                // Writes (Cosmos only, when &writeTest=yes)
                var writeSteps = new List<object>();
                var testGuid1 = Guid.NewGuid();
                var testGuid2 = Guid.Empty;

                if (string.Equals(writeTest, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var client1 = new Client
                        {
                            Id = testGuid1,
                            CompanyName = "PARITY TEST",
                            CompanyAddress = "X",
                            Wcn = "T0000001"
                        };
                        await cosmosClientProfileRepo.AddAsync(client1);
                        var read1 = await cosmosClientProfileRepo.GetClientAsync(testGuid1);
                        bool step1Ok = read1 != null && read1.Id == testGuid1 && read1.CompanyName == "PARITY TEST" && read1.CompanyAddress == "X" && read1.Wcn == "T0000001";
                        writeSteps.Add(new { step = "1. AddAsync", ok = step1Ok, detail = step1Ok ? "Created and verified" : "Verification failed" });
                        if (!step1Ok) hadException = true;

                        client1.CompanyName = "PARITY TEST 2";
                        var savedId1 = await cosmosClientProfileRepo.SaveClientProfileAsync(client1);
                        var read2 = await cosmosClientProfileRepo.GetClientAsync(testGuid1);
                        bool step2Ok = savedId1 == testGuid1 && read2 != null && read2.CompanyName == "PARITY TEST 2" && read2.CompanyAddress == "X" && read2.Wcn == "T0000001";
                        writeSteps.Add(new { step = "2. SaveClientProfileAsync (update)", ok = step2Ok, detail = step2Ok ? "Updated and verified" : "Update verification failed" });
                        if (!step2Ok) hadException = true;

                        var client3 = new Client
                        {
                            Id = Guid.Empty,
                            CompanyName = "PARITY TEST 3",
                            CompanyAddress = "Y"
                        };
                        testGuid2 = await cosmosClientProfileRepo.SaveClientProfileAsync(client3);
                        var read3 = await cosmosClientProfileRepo.GetClientAsync(testGuid2);
                        bool step3Ok = testGuid2 != Guid.Empty && client3.Id == testGuid2 && read3 != null && read3.Id == testGuid2 && read3.CompanyName == "PARITY TEST 3" && read3.CompanyAddress == "Y";
                        writeSteps.Add(new { step = "3. SaveClientProfileAsync (insert with new Guid)", ok = step3Ok, detail = step3Ok ? $"Created with new id {testGuid2}" : "Insert verification failed" });
                        if (!step3Ok) hadException = true;

                        bool step4Ok = false;
                        string step4Detail = "";
                        try
                        {
                            await cosmosClientProfileRepo.SaveClientProfileAsync(new Client { Id = Guid.NewGuid(), CompanyName = "NOT FOUND" });
                            step4Detail = "Expected Exception was not thrown";
                        }
                        catch (Exception ex) when (ex.Message == "Update failed. No rows affected.")
                        {
                            step4Ok = true;
                            step4Detail = "Correctly threw: " + ex.Message;
                        }
                        catch (Exception ex)
                        {
                            step4Detail = "Threw unexpected exception: " + ex.Message;
                        }
                        writeSteps.Add(new { step = "4. SaveClientProfileAsync (nonexistent throws)", ok = step4Ok, detail = step4Detail });
                        if (!step4Ok) hadException = true;

                        await cosmosClientProfileRepo.DeleteClientAsync(testGuid1);
                        await cosmosClientProfileRepo.DeleteClientAsync(testGuid2);
                        var delCheck1 = await cosmosClientProfileRepo.GetClientAsync(testGuid1);
                        var delCheck2 = await cosmosClientProfileRepo.GetClientAsync(testGuid2);
                        bool delNonExistingOk = false;
                        try
                        {
                            await cosmosClientProfileRepo.DeleteClientAsync(Guid.NewGuid());
                            delNonExistingOk = true;
                        }
                        catch
                        {
                            delNonExistingOk = false;
                        }
                        bool step5Ok = delCheck1 == null && delCheck2 == null && delNonExistingOk;
                        writeSteps.Add(new { step = "5. DeleteClientAsync", ok = step5Ok, detail = step5Ok ? "Deleted both items, verified null, nonexistent delete succeeded without exception" : "Delete verification failed" });
                        if (!step5Ok) hadException = true;
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        writeSteps.Add(new { step = "WriteTest Exception", ok = false, detail = ex.Message });
                    }
                    finally
                    {
                        try { await cosmosClientProfileRepo.DeleteClientAsync(testGuid1); } catch { }
                        try { if (testGuid2 != Guid.Empty) await cosmosClientProfileRepo.DeleteClientAsync(testGuid2); } catch { }
                    }
                }

                var allEqual = !hadException && checks.All(c =>
                {
                    var prop = c.GetType().GetProperty("equal");
                    return prop != null && (bool)prop.GetValue(c)!;
                }) && (writeSteps.Count == 0 || writeSteps.All(w =>
                {
                    var prop = w.GetType().GetProperty("ok");
                    return prop != null && (bool)prop.GetValue(w)!;
                }));

                if (hadException || !allEqual)
                {
                    return Results.Json(new { allEqual = false, checks, writeSteps }, statusCode: StatusCodes.Status500InternalServerError);
                }

                return Results.Ok(new { allEqual = true, checks, writeSteps });
            }).AllowAnonymous();
        }

        private static void MapParityBatch3(WebApplication app)
        {
            app.MapGet("/health/parity/batch3", async (
                UserAccountRepository sqlUserAccountRepo,
                CosmosUserAccountRepository cosmosUserAccountRepo,
                UserPermissionRepository sqlPermRepo,
                CosmosUserPermissionRepository cosmosPermRepo,
                ClientProfileRepository sqlClientProfileRepo,
                IUnitOfWork unitOfWork,
                IConfiguration config,
                IServiceProvider sp,
                string? writeTest) =>
            {
                var checks = new List<object>();
                var hadException = false;

                var conn = unitOfWork.Connection;
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    conn.Open();
                }

                // 1. UserAccount.GetByAspNetId*
                try
                {
                    var sqlAccounts = (await conn.QueryAsync<UserAccountModel>(
                        "SELECT TOP 10 * FROM [dbo].[UserAccounts] ORDER BY Id")).ToList();

                    var deletedAccount = await conn.QueryFirstOrDefaultAsync<UserAccountModel>(
                        "SELECT TOP 1 * FROM [dbo].[UserAccounts] WHERE Deleted = 1");
                    if (deletedAccount != null && !sqlAccounts.Any(a => a.Id == deletedAccount.Id))
                    {
                        sqlAccounts.Add(deletedAccount);
                    }

                    var testUserIds = sqlAccounts
                        .Select(a => a.AspNetUserId)
                        .Where(id => !string.IsNullOrEmpty(id))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    var allTestIds = new List<string>(testUserIds);
                    allTestIds.AddRange(testUserIds.Select(id => id.ToUpperInvariant()));
                    allTestIds.Add("not-a-user");

                    foreach (var uid in allTestIds)
                    {
                        var s1 = await sqlUserAccountRepo.GetByAspNetId(uid);
                        var c1 = await cosmosUserAccountRepo.GetByAspNetId(uid);
                        checks.Add(ParityCompare.Single(
                            $"UserAccount.GetByAspNetId('{uid}')",
                            s1,
                            c1,
                            ("Id", x => x.Id),
                            ("AspNetUserId", x => x.AspNetUserId),
                            ("FirstName", x => x.FirstName),
                            ("LastName", x => x.LastName),
                            ("Company", x => x.Company)));

                        var s2 = await sqlUserAccountRepo.GetByAspNetIdAsync(uid);
                        var c2 = await cosmosUserAccountRepo.GetByAspNetIdAsync(uid);
                        checks.Add(ParityCompare.Single(
                            $"UserAccount.GetByAspNetIdAsync('{uid}')",
                            s2,
                            c2,
                            ("Id", x => x.Id),
                            ("AspNetUserId", x => x.AspNetUserId),
                            ("FirstName", x => x.FirstName),
                            ("LastName", x => x.LastName),
                            ("Company", x => x.Company)));

                        var s3 = await sqlUserAccountRepo.GetByAspNetIdIncludingDeletedAsync(uid);
                        var c3 = await cosmosUserAccountRepo.GetByAspNetIdIncludingDeletedAsync(uid);
                        checks.Add(ParityCompare.Single(
                            $"UserAccount.GetByAspNetIdIncludingDeletedAsync('{uid}')",
                            s3,
                            c3,
                            ("Id", x => x.Id),
                            ("AspNetUserId", x => x.AspNetUserId),
                            ("FirstName", x => x.FirstName),
                            ("LastName", x => x.LastName),
                            ("Company", x => x.Company),
                            ("Deleted", x => x.Deleted)));

                        var s4 = await sqlUserAccountRepo.GetByAspNetId_ActiveOrDeleted(uid);
                        var c4 = await cosmosUserAccountRepo.GetByAspNetId_ActiveOrDeleted(uid);
                        checks.Add(ParityCompare.Single(
                            $"UserAccount.GetByAspNetId_ActiveOrDeleted('{uid}')",
                            s4,
                            c4,
                            ("Id", x => x.Id),
                            ("AspNetUserId", x => x.AspNetUserId),
                            ("FirstName", x => x.FirstName),
                            ("LastName", x => x.LastName),
                            ("Company", x => x.Company),
                            ("Deleted", x => x.Deleted)));
                    }
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "UserAccount.GetByAspNetId*", equal = false, error = ex.Message });
                }

                // 2. UserAccount.GetUsersInRoleAsync
                List<Role> allRoles = new();
                try
                {
                    allRoles = await sqlPermRepo.GetAllRoles();
                    var testRoles = allRoles.Select(r => r.Name).ToList();
                    if (testRoles.Count > 0)
                    {
                        testRoles.Add(testRoles[0].ToUpperInvariant());
                    }

                    foreach (var rName in testRoles)
                    {
                        var s = await sqlUserAccountRepo.GetUsersInRoleAsync(rName);
                        var c = await cosmosUserAccountRepo.GetUsersInRoleAsync(rName);
                        checks.Add(ParityCompare.Lists(
                            $"UserAccount.GetUsersInRoleAsync('{rName}')",
                            s,
                            c,
                            x => x.Id,
                            false,
                            ("Id", x => x.Id),
                            ("AspNetUserId", x => x.AspNetUserId),
                            ("FirstName", x => x.FirstName),
                            ("LastName", x => x.LastName),
                            ("Company", x => x.Company)));
                    }
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "UserAccount.GetUsersInRoleAsync", equal = false, error = ex.Message });
                }

                // 3. UserAccount.GetUsersInRoleForClientAsync
                List<Guid> clientIds = new();
                try
                {
                    var sqlClients = await sqlClientProfileRepo.GetAllClientsAsync();
                    clientIds = sqlClients.Select(cl => cl.Id).Take(5).ToList();
                    clientIds.Add(Guid.Empty);

                    foreach (var role in allRoles.Take(3))
                    {
                        foreach (var cid in clientIds)
                        {
                            var s = await sqlUserAccountRepo.GetUsersInRoleForClientAsync(role.Name, cid);
                            var c = await cosmosUserAccountRepo.GetUsersInRoleForClientAsync(role.Name, cid);
                            checks.Add(ParityCompare.Lists(
                                $"UserAccount.GetUsersInRoleForClientAsync('{role.Name}', '{cid}')",
                                s,
                                c,
                                x => x.Id,
                                false,
                                ("Id", x => x.Id),
                                ("AspNetUserId", x => x.AspNetUserId),
                                ("FirstName", x => x.FirstName),
                                ("LastName", x => x.LastName),
                                ("Company", x => x.Company),
                                ("Phone", x => x.Phone),
                                ("TimeZone", x => x.TimeZone)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "UserAccount.GetUsersInRoleForClientAsync", equal = false, error = ex.Message });
                }

                // 4. UserAccount.GetAbsUsersAsync
                try
                {
                    var s = await sqlUserAccountRepo.GetAbsUsersAsync();
                    var c = await cosmosUserAccountRepo.GetAbsUsersAsync();
                    checks.Add(ParityCompare.Lists(
                        "UserAccount.GetAbsUsersAsync",
                        s,
                        c,
                        x => (x.Id, x.UserRole),
                        false,
                        ("Id", x => x.Id),
                        ("AspNetUserId", x => x.AspNetUserId),
                        ("FirstName", x => x.FirstName),
                        ("LastName", x => x.LastName),
                        ("Email", x => x.Email),
                        ("Phone", x => x.Phone),
                        ("TimeZone", x => x.TimeZone),
                        ("Company", x => x.Company),
                        ("UserRole", x => x.UserRole)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "UserAccount.GetAbsUsersAsync", equal = false, error = ex.Message });
                }

                // 5. UserAccount.GetAbsUsers
                try
                {
                    var s = await sqlUserAccountRepo.GetAbsUsers();
                    var c = await cosmosUserAccountRepo.GetAbsUsers();
                    checks.Add(ParityCompare.Lists(
                        "UserAccount.GetAbsUsers",
                        s,
                        c,
                        x => x.Id,
                        false,
                        ("Id", x => x.Id),
                        ("AspNetUserId", x => x.AspNetUserId),
                        ("FirstName", x => x.FirstName),
                        ("LastName", x => x.LastName),
                        ("Email", x => x.Email),
                        ("Phone", x => x.Phone),
                        ("TimeZone", x => x.TimeZone),
                        ("Company", x => x.Company)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "UserAccount.GetAbsUsers", equal = false, error = ex.Message });
                }

                // 6. UserAccount.GetClientUsersAsync
                try
                {
                    foreach (var cid in clientIds.Where(id => id != Guid.Empty).Take(3))
                    {
                        var cidStr = cid.ToString();
                        var s = await sqlUserAccountRepo.GetClientUsersAsync(cidStr);
                        var c = await cosmosUserAccountRepo.GetClientUsersAsync(cidStr);
                        checks.Add(ParityCompare.Lists(
                            $"UserAccount.GetClientUsersAsync('{cidStr}')",
                            s,
                            c,
                            x => (x.Id, x.UserRole),
                            false,
                            ("Id", x => x.Id),
                            ("AspNetUserId", x => x.AspNetUserId),
                            ("FirstName", x => x.FirstName),
                            ("LastName", x => x.LastName),
                            ("Email", x => x.Email),
                            ("Phone", x => x.Phone),
                            ("TimeZone", x => x.TimeZone),
                            ("Company", x => x.Company),
                            ("UserRole", x => x.UserRole)));
                    }
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "UserAccount.GetClientUsersAsync", equal = false, error = ex.Message });
                }

                // 7. UserAccount.GetClientAdminsAsync
                try
                {
                    foreach (var cid in clientIds)
                    {
                        var s = await sqlUserAccountRepo.GetClientAdminsAsync(cid);
                        var c = await cosmosUserAccountRepo.GetClientAdminsAsync(cid);
                        checks.Add(ParityCompare.Lists(
                            $"UserAccount.GetClientAdminsAsync('{cid}')",
                            s,
                            c,
                            null,
                            true,
                            ("Id", x => x.Id),
                            ("FirstName", x => x.FirstName),
                            ("LastName", x => x.LastName),
                            ("Email", x => x.Email),
                            ("Phone", x => x.Phone),
                            ("TimeZone", x => x.TimeZone),
                            ("ClientId", x => x.ClientId)));
                    }
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "UserAccount.GetClientAdminsAsync", equal = false, error = ex.Message });
                }

                // 8. UserAccount.GetUsersWithPermissionAsync (both throw)
                try
                {
                    bool sqlThrew = false;
                    bool cosmosThrew = false;
                    try { await sqlUserAccountRepo.GetUsersWithPermissionAsync(DigitalCap.Core.Security.Permission.ViewAllProjects); } catch { sqlThrew = true; }
                    try { await cosmosUserAccountRepo.GetUsersWithPermissionAsync(DigitalCap.Core.Security.Permission.ViewAllProjects); } catch (NotSupportedException) { cosmosThrew = true; } catch { }
                    checks.Add(new
                    {
                        check = "UserAccount.GetUsersWithPermissionAsync throws",
                        sqlCount = 0,
                        cosmosCount = 0,
                        equal = sqlThrew && cosmosThrew,
                        firstDifference = (!sqlThrew || !cosmosThrew) ? $"sqlThrew={sqlThrew}, cosmosThrew={cosmosThrew}" : null
                    });
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "UserAccount.GetUsersWithPermissionAsync throws", equal = false, error = ex.Message });
                }

                // 9. Identity (A.2)
                try
                {
                    var idProvider = config["Identity:Provider"] ?? "Sql";
                    if (!string.Equals(idProvider, "Cosmos", StringComparison.OrdinalIgnoreCase))
                    {
                        checks.Add(new
                        {
                            check = "Identity.CosmosStoreCaseCheck",
                            sqlCount = 0,
                            cosmosCount = 0,
                            equal = true,
                            firstDifference = "skipped: Identity:Provider is Sql"
                        });
                    }
                    else
                    {
                        var userMgr = sp.GetService(typeof(UserManager<ApplicationUser>)) as UserManager<ApplicationUser>;
                        var roleMgr = sp.GetService(typeof(RoleManager<IdentityRole>)) as RoleManager<IdentityRole>;

                        if (userMgr != null && roleMgr != null)
                        {
                            var firstUser = await userMgr.Users.FirstOrDefaultAsync();
                            if (firstUser != null)
                            {
                                var upperEmail = firstUser.Email?.ToUpperInvariant() ?? "";
                                var byEmail = await userMgr.FindByEmailAsync(upperEmail);
                                var byName = await userMgr.FindByNameAsync(firstUser.UserName?.ToUpperInvariant() ?? "");
                                var roleAbs = await roleMgr.FindByNameAsync("Admin (CAP HQ)");
                                bool? inRole = roleAbs != null ? await userMgr.IsInRoleAsync(firstUser, "Admin (CAP HQ)") : null;

                                bool ok = byEmail != null && byName != null;
                                checks.Add(new
                                {
                                    check = "Identity.CosmosStoreCaseCheck",
                                    sqlCount = 1,
                                    cosmosCount = ok ? 1 : 0,
                                    equal = ok,
                                    firstDifference = ok ? null : "Lookup by UPPER case email or username failed"
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Identity.CosmosStoreCaseCheck", equal = false, error = ex.Message });
                }

                // 10. UserPermission reads
                List<UserPermission> permissions = new();
                try
                {
                    permissions = await sqlPermRepo.GetAllPermissionsAsync();
                    var cosmosPerms = await cosmosPermRepo.GetAllPermissionsAsync();
                    checks.Add(ParityCompare.Lists(
                        "UserPermission.GetAllPermissionsAsync",
                        permissions,
                        cosmosPerms,
                        p => p.Id,
                        false,
                        ("Id", p => p.Id),
                        ("PermissionName", p => p.PermissionName)));

                    var sqlRoles = await sqlPermRepo.GetAllRoles();
                    var cosmosRoles = await cosmosPermRepo.GetAllRoles();
                    checks.Add(ParityCompare.Lists(
                        "UserPermission.GetAllRoles",
                        sqlRoles,
                        cosmosRoles,
                        r => r.Id,
                        false,
                        ("Id", r => r.Id),
                        ("Name", r => r.Name)));

                    var distinctRoleIds = (await conn.QueryAsync<Guid?>(
                        "SELECT DISTINCT RoleId FROM [dbo].[RolePermissionsMapping] WHERE RoleId IS NOT NULL")).ToList();

                    foreach (var rid in distinctRoleIds.Take(5))
                    {
                        if (rid.HasValue)
                        {
                            var s = await sqlPermRepo.GetRolePermission(rid.Value.ToString());
                            var c = await cosmosPermRepo.GetRolePermission(rid.Value.ToString());
                            checks.Add(ParityCompare.Lists(
                                $"UserPermission.GetRolePermission('{rid}')",
                                s,
                                c,
                                x => (x.Id, x.PermissionId, x.PermissionName),
                                false,
                                ("Id", x => x.Id),
                                ("RoleId", x => x.RoleId),
                                ("PermissionId", x => x.PermissionId),
                                ("PermissionName", x => x.PermissionName),
                                ("IsReadable", x => x.IsReadable),
                                ("IsEditable", x => x.IsEditable),
                                ("IsDeleted", x => x.IsDeleted)));
                        }
                    }

                    var distinctMappingIds = (await conn.QueryAsync<Guid>(
                        "SELECT TOP 10 Id FROM [dbo].[RolePermissionsMapping] WHERE Id IS NOT NULL")).ToList();
                    distinctMappingIds.Add(Guid.NewGuid());

                    foreach (var mid in distinctMappingIds)
                    {
                        var s = await sqlPermRepo.GetRolePermissionById(mid.ToString());
                        var c = await cosmosPermRepo.GetRolePermissionById(mid.ToString());
                        checks.Add(ParityCompare.Single(
                            $"UserPermission.GetRolePermissionById('{mid}')",
                            s,
                            c,
                            ("Id", x => x.Id),
                            ("RoleId", x => x.RoleId),
                            ("PermissionId", x => x.PermissionId),
                            ("IsReadable", x => x.IsReadable),
                            ("IsEditable", x => x.IsEditable),
                            ("IsDeleted", x => x.IsDeleted)));
                    }

                    var surveyors = (await conn.QueryAsync<string>(@"
                        SELECT DISTINCT LeadSurveyor FROM CAP.Projects WHERE LeadSurveyor IS NOT NULL
                        UNION
                        SELECT DISTINCT SecondSurveyor FROM CAP.Projects WHERE SecondSurveyor IS NOT NULL"))
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Take(5)
                        .ToList();

                    foreach (var surv in surveyors)
                    {
                        var s = await sqlPermRepo.GetCurrentUserProjects(surv);
                        var c = await cosmosPermRepo.GetCurrentUserProjects(surv);
                        checks.Add(ParityCompare.Lists(
                            $"UserPermission.GetCurrentUserProjects('{surv}')",
                            s,
                            c,
                            x => x,
                            false,
                            ("Id", x => x)));
                    }

                    // GetRolePermissionByUserName
                    var testUsername = (await conn.QueryFirstOrDefaultAsync<string>(
                        "SELECT TOP 1 UserName FROM [dbo].[AspNetUsers] WHERE UserName IS NOT NULL")) ?? "admin";

                    var userProjects = (await sqlPermRepo.GetCurrentUserProjects(testUsername)).Take(5).ToList();
                    var projectIdsToTest = new List<int?> { null };
                    projectIdsToTest.AddRange(userProjects.Cast<int?>());

                    foreach (var perm in permissions.Take(5))
                    {
                        foreach (var pid in projectIdsToTest)
                        {
                            var s = await sqlPermRepo.GetRolePermissionByUserName(testUsername, perm.PermissionName, pid);
                            var c = await cosmosPermRepo.GetRolePermissionByUserName(testUsername, perm.PermissionName, pid);
                            checks.Add(ParityCompare.Single(
                                $"UserPermission.GetRolePermissionByUserName('{testUsername}', '{perm.PermissionName}', {pid})",
                                s,
                                c,
                                ("Read", x => x.Read),
                                ("Edit", x => x.Edit),
                                ("Delete", x => x.Delete),
                                ("PermissionId", x => x.PermissionId),
                                ("PermissionName", x => x.PermissionName)));
                        }
                    }

                    // AddPermissionAsync & UpdatePermissionAsync throw
                    bool sqlAddThrew = false, cosmosAddThrew = false;
                    bool sqlUpThrew = false, cosmosUpThrew = false;
                    try { await sqlPermRepo.AddPermissionAsync(new UserPermission { Id = Guid.NewGuid() }); } catch { sqlAddThrew = true; }
                    try { await cosmosPermRepo.AddPermissionAsync(new UserPermission { Id = Guid.NewGuid() }); } catch (NotSupportedException) { cosmosAddThrew = true; } catch { }
                    try { await sqlPermRepo.UpdatePermissionAsync(new UserPermission { Id = Guid.NewGuid() }); } catch { sqlUpThrew = true; }
                    try { await cosmosPermRepo.UpdatePermissionAsync(new UserPermission { Id = Guid.NewGuid() }); } catch (NotSupportedException) { cosmosUpThrew = true; } catch { }

                    checks.Add(new
                    {
                        check = "UserPermission.AddPermissionAsync throws",
                        sqlCount = 0,
                        cosmosCount = 0,
                        equal = sqlAddThrew && cosmosAddThrew,
                        firstDifference = (!sqlAddThrew || !cosmosAddThrew) ? $"sql={sqlAddThrew}, cosmos={cosmosAddThrew}" : null
                    });
                    checks.Add(new
                    {
                        check = "UserPermission.UpdatePermissionAsync throws",
                        sqlCount = 0,
                        cosmosCount = 0,
                        equal = sqlUpThrew && cosmosUpThrew,
                        firstDifference = (!sqlUpThrew || !cosmosUpThrew) ? $"sql={sqlUpThrew}, cosmos={cosmosUpThrew}" : null
                    });
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "UserPermission reads", equal = false, error = ex.Message });
                }

                // 11. Writes: Cosmos only (when &writeTest=yes)
                var writeSteps = new List<object>();
                Guid createdAccId = Guid.Empty;
                Guid oldClientId = Guid.Empty;
                Guid newClientId = Guid.Empty;
                Guid testRoleId = Guid.Empty;
                Guid testMappingId = Guid.Empty;

                if (string.Equals(writeTest, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    var factory = sp.GetRequiredService<ICosmosContainerFactory>();
                    var userAccountsContainer = factory.GetContainer(CosmosContainers.dbo_UserAccounts);
                    var mappingContainer = factory.GetContainer(CosmosContainers.dbo_RolePermissionsMapping);

                    try
                    {
                        oldClientId = clientIds.FirstOrDefault(id => id != Guid.Empty);
                        newClientId = clientIds.FirstOrDefault(id => id != Guid.Empty && id != oldClientId);
                        if (oldClientId == Guid.Empty) oldClientId = Guid.NewGuid();
                        if (newClientId == Guid.Empty) newClientId = Guid.NewGuid();

                        // Write 1: InsertAsync
                        var newAcc = new UserAccountModel
                        {
                            Id = Guid.Empty,
                            AspNetUserId = $"parity-test-{Guid.NewGuid()}",
                            ClientId = oldClientId,
                            FirstName = "PTEST_First",
                            LastName = "PTEST_Last",
                            Company = "ABS",
                            Phone = "123456",
                            TimeZone = "UTC"
                        };
                        createdAccId = await cosmosUserAccountRepo.InsertAsync(newAcc);
                        var read1 = await cosmosUserAccountRepo.GetAsync(createdAccId);
                        bool w1Ok = createdAccId != Guid.Empty && read1 != null && read1.FirstName == "PTEST_First" && read1.CreatedAt != default;
                        writeSteps.Add(new { step = "1. UserAccount.InsertAsync", ok = w1Ok, detail = w1Ok ? $"Created Id={createdAccId}" : "Verification failed" });
                        if (!w1Ok) hadException = true;

                        // Write 2: UpdateAsync (same ClientId)
                        read1!.FirstName = "PTEST_First_Updated";
                        bool w2Ok = await cosmosUserAccountRepo.UpdateAsync(read1);
                        var read2 = await cosmosUserAccountRepo.GetAsync(createdAccId);
                        bool w2VerOk = w2Ok && read2 != null && read2.FirstName == "PTEST_First_Updated" && read2.UpdatedAt != default;
                        writeSteps.Add(new { step = "2. UserAccount.UpdateAsync (same client)", ok = w2VerOk, detail = w2VerOk ? "Updated and verified" : "Verification failed" });
                        if (!w2VerOk) hadException = true;

                        // Write 3: UpdateAsync (different ClientId)
                        read2!.ClientId = newClientId;
                        bool w3Ok = await cosmosUserAccountRepo.UpdateAsync(read2);
                        var read3 = await cosmosUserAccountRepo.GetAsync(createdAccId);
                        bool w3VerOk = w3Ok && read3 != null && read3.ClientId == newClientId;
                        writeSteps.Add(new { step = "3. UserAccount.UpdateAsync (partition move)", ok = w3VerOk, detail = w3VerOk ? "Moved to new ClientId partition" : "Partition move failed" });
                        if (!w3VerOk) hadException = true;

                        // Write 4: UpdateAsync nonexistent / empty
                        bool w4Nonexistent = !(await cosmosUserAccountRepo.UpdateAsync(new UserAccountModel { Id = Guid.NewGuid() }));
                        bool w4EmptyThrew = false;
                        try { await cosmosUserAccountRepo.UpdateAsync(new UserAccountModel { Id = Guid.Empty }); } catch (ArgumentException) { w4EmptyThrew = true; }
                        bool w4Ok = w4Nonexistent && w4EmptyThrew;
                        writeSteps.Add(new { step = "4. UserAccount.UpdateAsync (invalid ids)", ok = w4Ok, detail = w4Ok ? "Nonexistent returned false, Empty threw ArgumentException" : "Invalid ID handling failed" });
                        if (!w4Ok) hadException = true;

                        // Write 5: DeleteAsync (soft delete)
                        bool w5Del = await cosmosUserAccountRepo.DeleteAsync(createdAccId);
                        var w5Active = await cosmosUserAccountRepo.GetByAspNetIdAsync(read3!.AspNetUserId);
                        var w5Inc = await cosmosUserAccountRepo.GetByAspNetIdIncludingDeletedAsync(read3.AspNetUserId);
                        bool w5DelAgain = !(await cosmosUserAccountRepo.DeleteAsync(createdAccId));
                        bool w5Ok = w5Del && w5Active == null && w5Inc != null && w5Inc.Deleted && w5DelAgain;
                        writeSteps.Add(new { step = "5. UserAccount.DeleteAsync (soft delete)", ok = w5Ok, detail = w5Ok ? "Soft deleted, active null, including-deleted found, delete again returned false" : "Soft delete verification failed" });
                        if (!w5Ok) hadException = true;

                        // Write 6: UserPermission
                        testRoleId = Guid.NewGuid();
                        testMappingId = Guid.NewGuid();

                        var templateMapping = await conn.QueryFirstOrDefaultAsync<RolePermissionsMappingDocument>(
                            "SELECT TOP 1 * FROM [dbo].[RolePermissionsMapping] WHERE RoleId IS NULL AND PermissionId IS NOT NULL");

                        if (templateMapping != null && templateMapping.PermissionId.HasValue)
                        {
                            var newRp = new UserRolePermission
                            {
                                Id = testMappingId,
                                RoleId = testRoleId,
                                PermissionId = templateMapping.PermissionId.Value,
                                IsReadable = true,
                                IsEditable = false,
                                IsDeleted = false
                            };
                            bool addRpOk = await cosmosPermRepo.AddRolePermissionAsync(newRp);
                            var readRp = await cosmosPermRepo.GetRolePermissionById(testMappingId.ToString());
                            bool addRpVerOk = addRpOk && readRp != null && readRp.ReadExist == (templateMapping.ReadExist ?? false);

                            bool fakeAddOk = !(await cosmosPermRepo.AddRolePermissionAsync(new UserRolePermission
                            {
                                Id = Guid.NewGuid(),
                                RoleId = testRoleId,
                                PermissionId = Guid.NewGuid()
                            }));

                            readRp!.IsEditable = true;
                            bool upRpOk = await cosmosPermRepo.UpdateRolePermissionAsync(readRp);
                            bool upFakeOk = !(await cosmosPermRepo.UpdateRolePermissionAsync(new UserRolePermission
                            {
                                Id = Guid.NewGuid(),
                                RoleId = testRoleId,
                                PermissionId = templateMapping.PermissionId.Value
                            }));

                            bool delRpOk = await cosmosPermRepo.DeleteRolePermissionAsync(testRoleId);
                            bool delRpAgain = !(await cosmosPermRepo.DeleteRolePermissionAsync(testRoleId));

                            bool w6Ok = addRpVerOk && fakeAddOk && upRpOk && upFakeOk && delRpOk && delRpAgain;
                            writeSteps.Add(new { step = "6. UserPermission (Add, Update, Delete role permission)", ok = w6Ok, detail = w6Ok ? "Verified add with template, update, and delete" : "Permission write verification failed" });
                            if (!w6Ok) hadException = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        writeSteps.Add(new { step = "WriteTest Exception", ok = false, detail = ex.Message });
                    }
                    finally
                    {
                        if (createdAccId != Guid.Empty)
                        {
                            var idStr = createdAccId.ToString("D").ToLowerInvariant();
                            try { await userAccountsContainer.DeleteItemAsync<UserAccountDocument>(idStr, new PartitionKey(newClientId.ToString("D").ToLowerInvariant())); } catch { }
                            try { await userAccountsContainer.DeleteItemAsync<UserAccountDocument>(idStr, new PartitionKey(oldClientId.ToString("D").ToLowerInvariant())); } catch { }
                        }
                        if (testMappingId != Guid.Empty)
                        {
                            var mapStr = testMappingId.ToString("D").ToLowerInvariant();
                            try { await mappingContainer.DeleteItemAsync<RolePermissionsMappingDocument>(mapStr, new PartitionKey(mapStr)); } catch { }
                        }
                    }
                }

                var allEqual = !hadException && checks.All(c =>
                {
                    var prop = c.GetType().GetProperty("equal");
                    return prop != null && (bool)prop.GetValue(c)!;
                }) && (writeSteps.Count == 0 || writeSteps.All(w =>
                {
                    var prop = w.GetType().GetProperty("ok");
                    return prop != null && (bool)prop.GetValue(w)!;
                }));

                if (hadException || !allEqual)
                {
                    return Results.Json(new { allEqual = false, checks, writeSteps }, statusCode: StatusCodes.Status500InternalServerError);
                }

                return Results.Ok(new { allEqual = true, checks, writeSteps });
            }).AllowAnonymous();
        }

        private static void MapParityBatch4(WebApplication app)
        {
            app.MapGet("/health/parity/batch4", async (
                string? writeTest,
                string? taskWriteTest,
                IServiceProvider sp,
                CancellationToken ct) =>
            {
                var checks = new List<object>();
                bool hadException = false;

                var sqlActivityRepo = sp.GetRequiredService<ActivityRepository>();
                var cosmosActivityRepo = sp.GetRequiredService<CosmosActivityRepository>();

                var sqlCommentRepo = sp.GetRequiredService<CommentRepository>();
                var cosmosCommentRepo = sp.GetRequiredService<CosmosCommentRepository>();

                var sqlGradeRepo = sp.GetRequiredService<GradeRepository>();
                var cosmosGradeRepo = sp.GetRequiredService<CosmosGradeRepository>();

                var sqlFileRepo = sp.GetRequiredService<FileStorageRepository>();
                var cosmosFileRepo = sp.GetRequiredService<CosmosFileStorageRepository>();

                var sqlTaskRepo = sp.GetRequiredService<TaskRepository>();
                var cosmosTaskRepo = sp.GetRequiredService<CosmosTaskRepository>();

                var db = sp.GetRequiredService<ApplicationDbContext>();
                var conn = db.Database.GetDbConnection();

                // 1. Activity reads
                try
                {
                    // 1a. GetRecentActivitiesAsync(10)
                    var sqlAct10 = await sqlActivityRepo.GetRecentActivitiesAsync(10);
                    var cosmosAct10 = await cosmosActivityRepo.GetRecentActivitiesAsync(10);
                    checks.Add(ParityCompare.Lists(
                        "Activity.GetRecentActivitiesAsync(10)",
                        sqlAct10.OrderByDescending(x => x.CreateDttm).ThenByDescending(x => x.ID),
                        cosmosAct10.OrderByDescending(x => x.CreateDttm).ThenByDescending(x => x.ID),
                        null,
                        true,
                        ("ID", x => x.ID),
                        ("Text", x => x.Text),
                        ("CreateBy", x => x.CreateBy),
                        ("CreateDttm", x => x.CreateDttm),
                        ("ProjectId", x => x.ProjectId),
                        ("VesselName", x => x.VesselName),
                        ("PIDNumber", x => x.PIDNumber)));

                    // 1b. GetRecentActivitiesAsync(50)
                    var sqlAct50 = await sqlActivityRepo.GetRecentActivitiesAsync(50);
                    var cosmosAct50 = await cosmosActivityRepo.GetRecentActivitiesAsync(50);
                    checks.Add(ParityCompare.Lists(
                        "Activity.GetRecentActivitiesAsync(50)",
                        sqlAct50.OrderByDescending(x => x.CreateDttm).ThenByDescending(x => x.ID),
                        cosmosAct50.OrderByDescending(x => x.CreateDttm).ThenByDescending(x => x.ID),
                        null,
                        true,
                        ("ID", x => x.ID),
                        ("Text", x => x.Text),
                        ("CreateBy", x => x.CreateBy),
                        ("CreateDttm", x => x.CreateDttm),
                        ("ProjectId", x => x.ProjectId),
                        ("VesselName", x => x.VesselName),
                        ("PIDNumber", x => x.PIDNumber)));

                    // 1c. GetRecentActivitiesByProjectIdAsync(5, p) for first 20 project ids
                    var projectIdsWithActivities = (await conn.QueryAsync<int>(
                        "SELECT DISTINCT TOP 20 ProjectId FROM [CAP].[Activities] WHERE ProjectId IS NOT NULL ORDER BY ProjectId")).ToList();
                    foreach (var pid in projectIdsWithActivities)
                    {
                        var sqlActProj = await sqlActivityRepo.GetRecentActivitiesByProjectIdAsync(5, pid);
                        var cosmosActProj = await cosmosActivityRepo.GetRecentActivitiesByProjectIdAsync(5, pid);
                        checks.Add(ParityCompare.Lists(
                            $"Activity.GetRecentActivitiesByProjectIdAsync(5, {pid})",
                            sqlActProj.OrderByDescending(x => x.CreateDttm).ThenByDescending(x => x.ID),
                            cosmosActProj.OrderByDescending(x => x.CreateDttm).ThenByDescending(x => x.ID),
                            null,
                            true,
                            ("ID", x => x.ID),
                            ("Text", x => x.Text),
                            ("CreateBy", x => x.CreateBy),
                            ("CreateDttm", x => x.CreateDttm),
                            ("ProjectId", x => x.ProjectId),
                            ("VesselName", x => x.VesselName),
                            ("PIDNumber", x => x.PIDNumber)));
                    }

                    // 1d. GetRecentActivitiesAsync2(page, 10) for page 0, 1, 2
                    for (int page = 0; page <= 2; page++)
                    {
                        var sqlPage = await sqlActivityRepo.GetRecentActivitiesAsync2(page, 10);
                        var cosmosPage = await cosmosActivityRepo.GetRecentActivitiesAsync2(page, 10);
                        checks.Add(ParityCompare.Lists(
                            $"Activity.GetRecentActivitiesAsync2({page}, 10)",
                            sqlPage.OrderByDescending(x => x.CreateDttm).ThenByDescending(x => x.ID),
                            cosmosPage.OrderByDescending(x => x.CreateDttm).ThenByDescending(x => x.ID),
                            null,
                            true,
                            ("ID", x => x.ID),
                            ("Text", x => x.Text),
                            ("CreateBy", x => x.CreateBy),
                            ("CreateDttm", x => x.CreateDttm),
                            ("ProjectId", x => x.ProjectId),
                            ("VesselName", x => x.VesselName),
                            ("PIDNumber", x => x.PIDNumber)));
                    }
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Activity reads", equal = false, error = ex.Message });
                }

                // 2. Comments reads
                try
                {
                    var projectIdsWithComments = (await conn.QueryAsync<int>(
                        "SELECT DISTINCT TOP 20 ProjectId FROM [CAP].[Comments] WHERE ProjectTaskId IS NULL AND ProjectId IS NOT NULL ORDER BY ProjectId")).ToList();
                    foreach (var pid in projectIdsWithComments)
                    {
                        var sqlComments = await sqlCommentRepo.GetCommentsByProjectIdAsync(pid);
                        var cosmosComments = await cosmosCommentRepo.GetCommentsByProjectIdAsync(pid);
                        checks.Add(ParityCompare.Lists(
                            $"Comment.GetCommentsByProjectIdAsync({pid})",
                            sqlComments,
                            cosmosComments,
                            x => x.Id,
                            false,
                            ("Id", x => x.Id),
                            ("ProjectId", x => x.ProjectId),
                            ("ProjectTaskId", x => x.ProjectTaskId),
                            ("Comment", x => x.Comment),
                            ("UserId", x => x.UserId),
                            ("CreatedBy", x => x.CreatedBy),
                            ("LastModifiedDate", x => x.LastModifiedDate)));
                    }

                    var taskIdsWithComments = (await conn.QueryAsync<int>(
                        "SELECT DISTINCT TOP 20 ProjectTaskId FROM [CAP].[Comments] WHERE ProjectTaskId IS NOT NULL ORDER BY ProjectTaskId")).ToList();
                    foreach (var tid in taskIdsWithComments)
                    {
                        var sqlComments = await sqlCommentRepo.GetCommentsByProjectTaskIdAsync(tid);
                        var cosmosComments = await cosmosCommentRepo.GetCommentsByProjectTaskIdAsync(tid);
                        checks.Add(ParityCompare.Lists(
                            $"Comment.GetCommentsByProjectTaskIdAsync({tid})",
                            sqlComments,
                            cosmosComments,
                            x => x.Id,
                            false,
                            ("Id", x => x.Id),
                            ("ProjectId", x => x.ProjectId),
                            ("ProjectTaskId", x => x.ProjectTaskId),
                            ("Comment", x => x.Comment),
                            ("UserId", x => x.UserId),
                            ("CreatedBy", x => x.CreatedBy),
                            ("LastModifiedDate", x => x.LastModifiedDate)));
                    }
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Comment reads", equal = false, error = ex.Message });
                }

                // 3. Grade reads
                try
                {
                    var projectIdsWithGrades = (await conn.QueryAsync<int>(
                        "SELECT DISTINCT TOP 20 ProjectId FROM [CAP].[ProjectGrades] WHERE IsDeleted = 0 ORDER BY ProjectId")).ToList();
                    var gradeProjectsToTest = projectIdsWithGrades.Concat(new[] { 99999999 });
                    foreach (var pid in gradeProjectsToTest)
                    {
                        var sqlGrade = await sqlGradeRepo.GetProjectGrades(pid);
                        var cosmosGrade = await cosmosGradeRepo.GetProjectGrades(pid);
                        checks.Add(ParityCompare.Single(
                            $"Grade.GetProjectGrades({pid})",
                            sqlGrade,
                            cosmosGrade,
                            ("Id", x => x.Id),
                            ("ProjectId", x => x.ProjectId),
                            ("CAPCertificateNumber", x => x.CAPCertificateNumber),
                            ("CAPCertificateIssuanceDate", x => x.CAPCertificateIssuanceDate),
                            ("ClassReportNumber", x => x.ClassReportNumber),
                            ("ClassReportDate", x => x.ClassReportDate),
                            ("StructuralReportNumber", x => x.StructuralReportNumber),
                            ("StructuralReportDate", x => x.StructuralReportDate),
                            ("FinalGrade", x => x.FinalGrade),
                            ("StructuralGrade", x => x.StructuralGrade),
                            ("FatigueGrade", x => x.FatigueGrade),
                            ("RenewalGrade", x => x.RenewalGrade),
                            ("MaterialGrade", x => x.MaterialGrade),
                            ("GaugingGrade", x => x.GaugingGrade),
                            ("HullGirderStrength", x => x.HullGirderStrength),
                            ("CAP1CertificateIssuanceDate", x => x.CAP1CertificateIssuanceDate),
                            ("CAP1FinalGrade", x => x.CAP1FinalGrade)));
                    }
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Grade reads", equal = false, error = ex.Message });
                }

                // 4. File reads
                try
                {
                    var projectIdsWithFiles = (await conn.QueryAsync<int>(
                        "SELECT DISTINCT TOP 20 Projectid FROM [CAP].[Files] WHERE Projectid IS NOT NULL AND COALESCE(SurveyImage, 0) != 1 ORDER BY Projectid")).ToList();
                    foreach (var pid in projectIdsWithFiles)
                    {
                        var sqlFiles = await sqlFileRepo.GetProjectFiles(pid);
                        var cosmosFiles = await cosmosFileRepo.GetProjectFiles(pid);
                        checks.Add(ParityCompare.Lists(
                            $"File.GetProjectFiles({pid})",
                            sqlFiles,
                            cosmosFiles,
                            x => x.Id,
                            false,
                            ("Id", x => x.Id),
                            ("ProjectId", x => x.ProjectId),
                            ("TaskId", x => x.TaskId),
                            ("DataType", x => x.DataType),
                            ("Name", x => x.Name),
                            ("Extension", x => x.Extension),
                            ("MimeType", x => x.MimeType),
                            ("StorageKey", x => x.StorageKey),
                            ("UploadedBy", x => x.UploadedBy),
                            ("LastModifiedBy", x => x.LastModifiedBy),
                            ("AvailableToClient", x => x.AvailableToClient)));
                    }

                    var taskIdsWithFiles = (await conn.QueryAsync<int>(
                        "SELECT DISTINCT TOP 20 TaskId FROM [CAP].[Files] WHERE TaskId IS NOT NULL ORDER BY TaskId")).ToList();
                    foreach (var tid in taskIdsWithFiles)
                    {
                        var sqlFiles = await sqlFileRepo.GetTaskFiles(tid);
                        var cosmosFiles = await cosmosFileRepo.GetTaskFiles(tid);
                        checks.Add(ParityCompare.Lists(
                            $"File.GetTaskFiles({tid})",
                            sqlFiles,
                            cosmosFiles,
                            x => x.Id,
                            false,
                            ("Id", x => x.Id),
                            ("ProjectId", x => x.ProjectId),
                            ("TaskId", x => x.TaskId),
                            ("DataType", x => x.DataType),
                            ("Name", x => x.Name),
                            ("Extension", x => x.Extension),
                            ("MimeType", x => x.MimeType),
                            ("StorageKey", x => x.StorageKey),
                            ("UploadedBy", x => x.UploadedBy),
                            ("LastModifiedBy", x => x.LastModifiedBy),
                            ("AvailableToClient", x => x.AvailableToClient)));
                    }

                    var fileIds = (await conn.QueryAsync<int>("SELECT TOP 20 Id FROM [CAP].[Files] ORDER BY Id")).ToList();
                    var fileIdsToTest = fileIds.Concat(new[] { 99999999 });
                    foreach (var fid in fileIdsToTest)
                    {
                        var sqlFile = await sqlFileRepo.GetFile<ProjectFile>(fid);
                        var cosmosFile = await cosmosFileRepo.GetFile<ProjectFile>(fid);
                        checks.Add(ParityCompare.Single(
                            $"File.GetFile<ProjectFile>({fid})",
                            sqlFile,
                            cosmosFile,
                            ("Id", x => x.Id),
                            ("ProjectId", x => x.ProjectId),
                            ("TaskId", x => x.TaskId),
                            ("DataType", x => x.DataType),
                            ("Name", x => x.Name),
                            ("Extension", x => x.Extension),
                            ("MimeType", x => x.MimeType),
                            ("StorageKey", x => x.StorageKey),
                            ("UploadedBy", x => x.UploadedBy),
                            ("LastModifiedBy", x => x.LastModifiedBy),
                            ("AvailableToClient", x => x.AvailableToClient)));
                    }
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "File reads", equal = false, error = ex.Message });
                }

                // 5. Task reads
                try
                {
                    var projectIdsWithTasks = (await conn.QueryAsync<int>(
                        "SELECT DISTINCT TOP 20 ProjectId FROM [CAP].[ProjectTaskStatus] ORDER BY ProjectId")).ToList();
                    var categories = (await conn.QueryAsync<string>(
                        "SELECT DISTINCT Category FROM [CAP].[Tasks] WHERE Category IS NOT NULL")).ToList();

                    foreach (var pid in projectIdsWithTasks)
                    {
                        var sqlCap2Tasks = await sqlTaskRepo.GetCap2Tasks(pid);
                        var cosmosCap2Tasks = await cosmosTaskRepo.GetCap2Tasks(pid);
                        checks.Add(ParityCompare.Lists(
                            $"Task.GetCap2Tasks({pid})",
                            sqlCap2Tasks,
                            cosmosCap2Tasks,
                            x => x.ID,
                            false,
                            ("ID", x => x.ID),
                            ("ProjectId", x => x.ProjectId),
                            ("StatusId", x => x.StatusId),
                            ("TaskId", x => x.TaskId),
                            ("Category", x => x.Category),
                            ("Type", x => x.Type),
                            ("Task", x => x.Task),
                            ("IsMilestone", x => x.IsMilestone),
                            ("PercentageComplete", x => x.PercentageComplete),
                            ("Status", x => x.Status),
                            ("Value", x => x.Value),
                            ("Notes", x => x.Notes),
                            ("StatusDate", x => x.StatusDate),
                            ("LastModifiedDate", x => x.LastModifiedDate),
                            ("LastModifiedBy", x => x.LastModifiedBy),
                            ("DueDate", x => x.DueDate),
                            ("AssignedTo", x => x.AssignedTo),
                            ("AssignedDate", x => x.AssignedDate),
                            ("DaysWorked", x => x.DaysWorked),
                            ("NumberOfComments", x => x.NumberOfComments),
                            ("NumberOfAttachments", x => x.NumberOfAttachments)));

                        var sqlTasks = await sqlTaskRepo.GetTasks(pid);
                        var cosmosTasks = await cosmosTaskRepo.GetTasks(pid);
                        checks.Add(ParityCompare.Lists(
                            $"Task.GetTasks({pid})",
                            sqlTasks,
                            cosmosTasks,
                            x => x.ID,
                            false,
                            ("ID", x => x.ID),
                            ("ProjectId", x => x.ProjectId),
                            ("StatusId", x => x.StatusId),
                            ("TaskId", x => x.TaskId),
                            ("Category", x => x.Category),
                            ("Type", x => x.Type),
                            ("Task", x => x.Task),
                            ("Status", x => x.Status),
                            ("Value", x => x.Value),
                            ("Notes", x => x.Notes),
                            ("StatusDate", x => x.StatusDate),
                            ("SurveyDays", x => x.SurveyDays),
                            ("LastModifiedDate", x => x.LastModifiedDate),
                            ("LastModifiedBy", x => x.LastModifiedBy),
                            ("DaysWorked", x => x.DaysWorked)));

                        var sqlPct = await sqlTaskRepo.UpdateProjectPercentComplete(pid);
                        var cosmosPct = await cosmosTaskRepo.UpdateProjectPercentComplete(pid);
                        checks.Add(new
                        {
                            check = $"Task.UpdateProjectPercentComplete({pid})",
                            sqlValue = sqlPct,
                            cosmosValue = cosmosPct,
                            equal = sqlPct == cosmosPct,
                            firstDifference = sqlPct != cosmosPct ? $"sql={sqlPct}, cosmos={cosmosPct}" : null
                        });

                        foreach (var cat in categories)
                        {
                            var sqlIncomp = await sqlTaskRepo.GetIncompleteTasks(pid, cat);
                            var cosmosIncomp = await cosmosTaskRepo.GetIncompleteTasks(pid, cat);
                            checks.Add(ParityCompare.Lists(
                                $"Task.GetIncompleteTasks({pid}, '{cat}')",
                                sqlIncomp,
                                cosmosIncomp,
                                x => x.ID,
                                false,
                                ("ID", x => x.ID),
                                ("ProjectId", x => x.ProjectId),
                                ("StatusId", x => x.StatusId),
                                ("TaskId", x => x.TaskId),
                                ("Category", x => x.Category),
                                ("Type", x => x.Type),
                                ("Task", x => x.Task),
                                ("Status", x => x.Status)));
                        }
                    }

                    var ptsIds = (await conn.QueryAsync<int>("SELECT TOP 20 ID FROM [CAP].[ProjectTaskStatus] ORDER BY ID")).ToList();
                    var ptsIdsToTest = ptsIds.Concat(new[] { 99999999 });
                    foreach (var ptsId in ptsIdsToTest)
                    {
                        var sqlPts = await sqlTaskRepo.GetCap2Task(ptsId);
                        var cosmosPts = await cosmosTaskRepo.GetCap2Task(ptsId);
                        checks.Add(ParityCompare.Single(
                            $"Task.GetCap2Task({ptsId})",
                            sqlPts,
                            cosmosPts,
                            ("ID", x => x.ID),
                            ("ProjectId", x => x.ProjectId),
                            ("Category", x => x.Category),
                            ("Type", x => x.Type),
                            ("Task", x => x.Task),
                            ("IsMilestone", x => x.IsMilestone),
                            ("PercentageComplete", x => x.PercentageComplete),
                            ("Status", x => x.Status),
                            ("StatusId", x => x.StatusId),
                            ("Value", x => x.Value),
                            ("Notes", x => x.Notes),
                            ("StatusDate", x => x.StatusDate),
                            ("LastModifiedDate", x => x.LastModifiedDate),
                            ("LastModifiedBy", x => x.LastModifiedBy),
                            ("DueDate", x => x.DueDate),
                            ("AssignedTo", x => x.AssignedTo),
                            ("AssignedDate", x => x.AssignedDate),
                            ("SurveyDays", x => x.SurveyDays)));
                    }

                    var groupCategories = (await conn.QueryAsync<string>(
                        "SELECT DISTINCT Category FROM [CAP].[TaskStatusGroups] WHERE Category IS NOT NULL")).ToList();
                    foreach (var gcat in groupCategories)
                    {
                        var sqlGroups = await sqlTaskRepo.GetStatusGroups(gcat);
                        var cosmosGroups = await cosmosTaskRepo.GetStatusGroups(gcat);
                        checks.Add(ParityCompare.Lists(
                            $"Task.GetStatusGroups('{gcat}')",
                            sqlGroups,
                            cosmosGroups,
                            null,
                            true,
                            ("Id", x => x.Id),
                            ("Category", x => x.Category),
                            ("Name", x => x.Name),
                            ("StartedTaskId", x => x.StartedTaskId),
                            ("CompletedTaskId", x => x.CompletedTaskId)));
                    }

                    var sqlStatuses = await sqlTaskRepo.GetStatuses();
                    var cosmosStatuses = await cosmosTaskRepo.GetStatuses();
                    checks.Add(ParityCompare.Lists(
                        "Task.GetStatuses()",
                        sqlStatuses,
                        cosmosStatuses,
                        x => x.ID,
                        false,
                        ("ID", x => x.ID),
                        ("Name", x => x.Name)));

                    // 4 NotSupported methods
                    bool sqlMoveThrew = false, cosmosMoveThrew = false;
                    try { await sqlFileRepo.MoveFile(1, FileDataType.Photos, "test"); } catch { sqlMoveThrew = true; }
                    try { await cosmosFileRepo.MoveFile(1, FileDataType.Photos, "test"); } catch (NotSupportedException) { cosmosMoveThrew = true; } catch { }

                    bool sqlGetTaskThrew = false, cosmosGetTaskThrew = false;
                    try { await sqlTaskRepo.GetTask(1); } catch { sqlGetTaskThrew = true; }
                    try { await cosmosTaskRepo.GetTask(1); } catch (NotSupportedException) { cosmosGetTaskThrew = true; } catch { }

                    bool sqlCloseTechThrew = false, cosmosCloseTechThrew = false;
                    try { await sqlTaskRepo.CloseTechnicalTasks(1); } catch { sqlCloseTechThrew = true; }
                    try { await cosmosTaskRepo.CloseTechnicalTasks(1); } catch (NotSupportedException) { cosmosCloseTechThrew = true; } catch { }

                    bool sqlCloseInvThrew = false, cosmosCloseInvThrew = false;
                    try { await sqlTaskRepo.CloseInvoiceTasks(1); } catch { sqlCloseInvThrew = true; }
                    try { await cosmosTaskRepo.CloseInvoiceTasks(1); } catch (NotSupportedException) { cosmosCloseInvThrew = true; } catch { }

                    checks.Add(new
                    {
                        check = "File.MoveFile throws NotSupported",
                        equal = sqlMoveThrew && cosmosMoveThrew,
                        firstDifference = (!sqlMoveThrew || !cosmosMoveThrew) ? $"sql={sqlMoveThrew}, cosmos={cosmosMoveThrew}" : null
                    });
                    checks.Add(new
                    {
                        check = "Task.GetTask throws NotSupported",
                        equal = sqlGetTaskThrew && cosmosGetTaskThrew,
                        firstDifference = (!sqlGetTaskThrew || !cosmosGetTaskThrew) ? $"sql={sqlGetTaskThrew}, cosmos={cosmosGetTaskThrew}" : null
                    });
                    checks.Add(new
                    {
                        check = "Task.CloseTechnicalTasks throws NotSupported",
                        equal = sqlCloseTechThrew && cosmosCloseTechThrew,
                        firstDifference = (!sqlCloseTechThrew || !cosmosCloseTechThrew) ? $"sql={sqlCloseTechThrew}, cosmos={cosmosCloseTechThrew}" : null
                    });
                    checks.Add(new
                    {
                        check = "Task.CloseInvoiceTasks throws NotSupported",
                        equal = sqlCloseInvThrew && cosmosCloseInvThrew,
                        firstDifference = (!sqlCloseInvThrew || !cosmosCloseInvThrew) ? $"sql={sqlCloseInvThrew}, cosmos={cosmosCloseInvThrew}" : null
                    });
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Task reads", equal = false, error = ex.Message });
                }

                // 6. Writes (when &writeTest=yes)
                var writeSteps = new List<object>();
                if (string.Equals(writeTest, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    var factory = sp.GetRequiredService<ICosmosContainerFactory>();
                    var commentsContainer = factory.GetContainer(CosmosContainers.CAP_Comments);
                    var gradesContainer = factory.GetContainer(CosmosContainers.CAP_ProjectGrades);
                    var filesContainer = factory.GetContainer(CosmosContainers.CAP_Files);
                    var activitiesContainer = factory.GetContainer(CosmosContainers.CAP_Activities);
                    var activityWriter = sp.GetRequiredService<CosmosActivityWriter>();

                    var testUser = (await conn.QueryFirstOrDefaultAsync<string>("SELECT TOP 1 Id FROM [dbo].[AspNetUsers]")) ?? Guid.NewGuid().ToString();
                    var testUserName = (await conn.QueryFirstOrDefaultAsync<string>("SELECT UserName FROM [dbo].[AspNetUsers] WHERE Id = @Id", new { Id = testUser })) ?? "DEV_USER";

                    var createdCommentIds = new List<int>();
                    var createdFileIds = new List<int>();
                    var createdGradeIds = new List<int>();

                    try
                    {
                        // 1. Comments
                        // 1a. Create comment without task
                        var c1List = await cosmosCommentRepo.CreateCommentAsync(-999, null, testUser, "Test comment 1");
                        var c1 = c1List.FirstOrDefault(x => x.Comment == "Test comment 1");
                        bool c1Ok = c1 != null && c1.ProjectId == -999 && c1.ProjectTaskId == null;
                        if (c1 != null) createdCommentIds.Add(c1.Id);

                        bool c1DocNullDeleted = false;
                        if (c1 != null)
                        {
                            try
                            {
                                var docRes = await commentsContainer.ReadItemAsync<CommentDocument>(c1.Id.ToString(), new PartitionKey(-999.0));
                                c1DocNullDeleted = docRes.Resource.IsDeleted == null;
                            }
                            catch { }
                        }

                        var actC1 = await cosmosActivityRepo.GetRecentActivitiesByProjectIdAsync(5, -999);
                        bool c1ActOk = actC1.Any(a => a.Text == "Test comment 1" && a.CreateBy == testUserName);
                        writeSteps.Add(new { step = "1a. Comment.Create (no task)", ok = c1Ok && c1DocNullDeleted && c1ActOk, detail = $"c1Ok={c1Ok}, docNullDeleted={c1DocNullDeleted}, actOk={c1ActOk}" });

                        // 1b. Create comment with task
                        var c2List = await cosmosCommentRepo.CreateCommentAsync(-999, -888, testUser, "Test comment 2 with task");
                        var c2 = c2List.FirstOrDefault(x => x.Comment == "Test comment 2 with task");
                        bool c2Ok = c2 != null && c2.ProjectId == -999 && c2.ProjectTaskId == -888;
                        if (c2 != null) createdCommentIds.Add(c2.Id);
                        writeSteps.Add(new { step = "1b. Comment.Create (with task)", ok = c2Ok, detail = $"c2Ok={c2Ok}" });

                        // 1c. Update comment
                        DateTimeOffset? origDate = c1?.LastModifiedDate;
                        await Task.Delay(20);
                        var c1UpList = await cosmosCommentRepo.UpdateCommentAsync(c1!.Id, -999, null, "Updated test comment 1", testUser);
                        var c1Up = c1UpList.FirstOrDefault(x => x.Id == c1.Id);
                        bool c1UpDateOk = c1Up != null && c1Up.LastModifiedDate != origDate;
                        var actC1Up = await cosmosActivityRepo.GetRecentActivitiesByProjectIdAsync(5, -999);
                        bool c1UpActOk = actC1Up.Any(a => a.Text == "Updated test comment 1" && a.CreateBy == testUserName);
                        writeSteps.Add(new { step = "1c. Comment.Update", ok = c1UpDateOk && c1UpActOk, detail = $"dateChanged={c1UpDateOk}, actOk={c1UpActOk}" });

                        // 1d. Update missing comment id
                        await cosmosCommentRepo.UpdateCommentAsync(99999999, -999, null, "Missing comment update text", testUser);
                        var actC1Miss = await cosmosActivityRepo.GetRecentActivitiesByProjectIdAsync(5, -999);
                        bool c1MissActOk = actC1Miss.Any(a => a.Text == "Missing comment update text" && a.CreateBy == testUserName);
                        writeSteps.Add(new { step = "1d. Comment.Update (missing id)", ok = c1MissActOk, detail = $"actOk={c1MissActOk}" });

                        // 1e. Delete comment
                        var delResult = await cosmosCommentRepo.DeleteCommentAsync(c1.Id, -999, "Updated test comment 1", testUser);
                        bool delEmpty = delResult != null && !delResult.Any();
                        var actC1Del = await cosmosActivityRepo.GetRecentActivitiesByProjectIdAsync(5, -999);
                        bool delActOk = actC1Del.Any(a => a.Text == "Comment Deleted: Updated test comment 1" && a.CreateBy == testUserName);
                        writeSteps.Add(new { step = "1e. Comment.Delete", ok = delEmpty && delActOk, detail = $"emptyList={delEmpty}, actOk={delActOk}" });

                        // 2. Grades
                        // 2a. Create -> returns 0, item has IsDeleted false
                        int gradeCreatedRet = await cosmosGradeRepo.CreateProjectGrades(-999);
                        var gradeItem = await cosmosGradeRepo.GetProjectGrades(-999);
                        bool gCreateOk = gradeCreatedRet == 0 && gradeItem != null && gradeItem.ProjectId == -999;
                        if (gradeItem != null) createdGradeIds.Add(gradeItem.Id);
                        bool gDocIsDeletedFalse = false;
                        if (gradeItem != null)
                        {
                            try
                            {
                                var gDoc = await gradesContainer.ReadItemAsync<ProjectGradeDocument>(gradeItem.Id.ToString(), new PartitionKey(-999.0));
                                gDocIsDeletedFalse = gDoc.Resource.IsDeleted == false;
                            }
                            catch { }
                        }
                        writeSteps.Add(new { step = "2a. Grade.Create", ok = gCreateOk && gDocIsDeletedFalse, detail = $"returned0={gradeCreatedRet == 0}, found={gradeItem != null}, isDeletedFalse={gDocIsDeletedFalse}" });

                        // 2b. Update incl. ProjectId change (-999 to -998)
                        if (gradeItem != null)
                        {
                            gradeItem.ProjectId = -998;
                            gradeItem.CAPCertificateNumber = 998;
                            await cosmosGradeRepo.UpdateProjectGrades(gradeItem);
                            var oldGrade = await cosmosGradeRepo.GetProjectGrades(-999);
                            var newGrade = await cosmosGradeRepo.GetProjectGrades(-998);
                            bool movedOk = oldGrade == null && newGrade != null && newGrade.CAPCertificateNumber == 998;
                            writeSteps.Add(new { step = "2b. Grade.Update (moved partition)", ok = movedOk, detail = $"oldNull={oldGrade == null}, newFound={newGrade != null}" });
                        }

                        // 2c. Update missing id
                        bool missingGradeNoErr = false;
                        try
                        {
                            await cosmosGradeRepo.UpdateProjectGrades(new ProjectGrades { Id = 99999999, ProjectId = -998 });
                            missingGradeNoErr = true;
                        }
                        catch { }
                        writeSteps.Add(new { step = "2c. Grade.Update (missing id)", ok = missingGradeNoErr, detail = $"noError={missingGradeNoErr}" });

                        // 3. Files
                        // 3a. Save project file & task file
                        var pFile = new ProjectFile
                        {
                            ProjectId = -999,
                            Name = "test_project_file",
                            Extension = ".pdf",
                            MimeType = "application/pdf"
                        };
                        await cosmosFileRepo.SaveProjectFile(pFile, "key-50", "fileTester");
                        bool pFileSaved = pFile.Id > 0;
                        if (pFileSaved) createdFileIds.Add(pFile.Id);

                        var tFile = new ProjectFile
                        {
                            ProjectId = -999,
                            TaskId = -777,
                            Name = "test_task_file",
                            Extension = ".pdf",
                            MimeType = "application/pdf"
                        };
                        await cosmosFileRepo.SaveTaskFile(tFile, "key-task-50", -777, "fileTester");
                        bool tFileSaved = tFile.Id > 0;
                        if (tFileSaved) createdFileIds.Add(tFile.Id);
                        writeSteps.Add(new { step = "3a. File.SaveProjectFile and SaveTaskFile", ok = pFileSaved && tFileSaved, detail = $"pFileId={pFile.Id}, tFileId={tFile.Id}" });

                        // 3b. UpdateFileMetadata -> LastModifiedDate unchanged, MimeType cut to 50
                        var readPFile = await cosmosFileRepo.GetFile<ProjectFile>(pFile.Id);
                        DateTime? origFileDate = readPFile?.LastModifiedDate;
                        await Task.Delay(20);
                        await cosmosFileRepo.UpdateFileMetadata(pFile.Id, FileDataType.Photos, "updated_file_name", true, "fileModifier");
                        var readPFile2 = await cosmosFileRepo.GetFile<ProjectFile>(pFile.Id);
                        bool fileDateUnchanged = readPFile2 != null && readPFile2.LastModifiedDate == origFileDate && readPFile2.Name == "updated_file_name";
                        var actFileUp = await cosmosActivityRepo.GetRecentActivitiesByProjectIdAsync(5, -999);
                        bool actFileUpOk = actFileUp.Any(a => a.Text == "Updated file updated_file_name");
                        writeSteps.Add(new { step = "3b. File.UpdateFileMetadata", ok = fileDateUnchanged && actFileUpOk, detail = $"dateUnchanged={fileDateUnchanged}, actOk={actFileUpOk}" });

                        // 3c. StorageKey of 60 chars -> throws InvalidOperationException & writes activity
                        var file60Doc = new FileDocument
                        {
                            id = "99999998",
                            Id = 99999998,
                            Projectid = -999,
                            StorageKey = new string('S', 60),
                            Name = "file60",
                            Extension = ".pdf",
                            MimeType = "application/pdf"
                        };
                        await filesContainer.UpsertItemAsync(file60Doc, new PartitionKey("99999998"));
                        createdFileIds.Add(99999998);

                        bool threw60 = false;
                        try
                        {
                            await cosmosFileRepo.UpdateFileMetadata(99999998, FileDataType.Photos, "file60_updated", false, "testUser");
                        }
                        catch (InvalidOperationException)
                        {
                            threw60 = true;
                        }
                        catch { }
                        var act60 = await cosmosActivityRepo.GetRecentActivitiesByProjectIdAsync(5, -999);
                        bool act60Ok = act60.Any(a => a.Text == "Updated file file60_updated");
                        writeSteps.Add(new { step = "3c. File.StorageKey > 50 throws & writes activity", ok = threw60 && act60Ok, detail = $"threw={threw60}, actOk={act60Ok}" });

                        // 3d. Delete file -> activity
                        await cosmosFileRepo.DeleteFile(pFile.Id, "fileDeleter");
                        var actFileDel = await cosmosActivityRepo.GetRecentActivitiesByProjectIdAsync(5, -999);
                        bool actFileDelOk = actFileDel.Any(a => a.Text == "Deleted file updated_file_name");
                        writeSteps.Add(new { step = "3d. File.DeleteFile", ok = actFileDelOk, detail = $"actOk={actFileDelOk}" });

                        // 3e. Delete missing file -> activity with null project and null text
                        await cosmosFileRepo.DeleteFile(99999997, "missingDeleter");
                        var actNullQuery = new QueryDefinition("SELECT TOP 5 * FROM c WHERE c.CreateBy = 'missingDeleter' ORDER BY c.CreateDttm DESC");
                        var missingDelActs = new List<ActivityDocument>();
                        using (var feed = activitiesContainer.GetItemQueryIterator<ActivityDocument>(actNullQuery, requestOptions: new QueryRequestOptions { PartitionKey = PartitionKey.Null }))
                        {
                            while (feed.HasMoreResults)
                            {
                                var resp = await feed.ReadNextAsync();
                                missingDelActs.AddRange(resp);
                            }
                        }
                        bool missingDelActOk = missingDelActs.Any(a => a.ProjectId == null && a.Text == null && a.CreateBy == "missingDeleter");
                        writeSteps.Add(new { step = "3e. File.DeleteFile (missing file -> null project, null text activity)", ok = missingDelActOk, detail = $"nullActOk={missingDelActOk}" });

                        // 4. Activities
                        await activityWriter.CreateActivityAsync(-999, new string('A', 3100), new string('U', 60));
                        var actLong = await cosmosActivityRepo.GetRecentActivitiesByProjectIdAsync(5, -999);
                        var foundLongAct = actLong.FirstOrDefault(a => a.Text != null && a.Text.StartsWith("AAAA"));
                        bool longCutOk = foundLongAct != null && foundLongAct.Text!.Length == 3000 && foundLongAct.CreateBy != null && foundLongAct.CreateBy.Length == 50;
                        writeSteps.Add(new { step = "4. Activity parameter cuts (3100->3000, 60->50)", ok = longCutOk, detail = $"found={foundLongAct != null}, textLen={foundLongAct?.Text?.Length}, userLen={foundLongAct?.CreateBy?.Length}" });
                    }
                    finally
                    {
                        foreach (var cid in createdCommentIds)
                        {
                            try { await commentsContainer.DeleteItemAsync<CommentDocument>(cid.ToString(), new PartitionKey(-999.0)); } catch { }
                        }
                        foreach (var gid in createdGradeIds)
                        {
                            try { await gradesContainer.DeleteItemAsync<ProjectGradeDocument>(gid.ToString(), new PartitionKey(-999.0)); } catch { }
                            try { await gradesContainer.DeleteItemAsync<ProjectGradeDocument>(gid.ToString(), new PartitionKey(-998.0)); } catch { }
                        }
                        foreach (var fid in createdFileIds)
                        {
                            try { await filesContainer.DeleteItemAsync<FileDocument>(fid.ToString(), new PartitionKey(fid.ToString())); } catch { }
                        }
                        try
                        {
                            var acts = await cosmosActivityRepo.GetRecentActivitiesByProjectIdAsync(100, -999);
                            foreach (var a in acts)
                            {
                                try { await activitiesContainer.DeleteItemAsync<ActivityDocument>(a.ID.ToString(), new PartitionKey(-999.0)); } catch { }
                            }
                        }
                        catch { }
                        try
                        {
                            var nullFeed = activitiesContainer.GetItemQueryIterator<ActivityDocument>(
                                new QueryDefinition("SELECT * FROM c WHERE c.CreateBy = 'missingDeleter'"),
                                requestOptions: new QueryRequestOptions { PartitionKey = PartitionKey.Null });
                            while (nullFeed.HasMoreResults)
                            {
                                var r = await nullFeed.ReadNextAsync();
                                foreach (var item in r)
                                {
                                    try { await activitiesContainer.DeleteItemAsync<ActivityDocument>(item.id, PartitionKey.Null); } catch { }
                                }
                            }
                        }
                        catch { }
                    }
                }

                // 7. Task Write Test (when &taskWriteTest=yes)
                object? taskWriteResult = null;
                if (string.Equals(taskWriteTest, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var samplePtsId = await conn.QueryFirstOrDefaultAsync<int>("SELECT TOP 1 ID FROM [CAP].[ProjectTaskStatus] ORDER BY ID");
                        if (samplePtsId > 0)
                        {
                            var task = await cosmosTaskRepo.GetCap2Task(samplePtsId);
                            if (task != null)
                            {
                                bool updated = await cosmosTaskRepo.UpdateTask(task, "TaskDevTester");
                                var acts = await cosmosActivityRepo.GetRecentActivitiesByProjectIdAsync(5, task.ProjectId ?? 0);
                                var recalculator = sp.GetRequiredService<ProjectStatusRecalculator>();
                                var projectStatusesChanged = await recalculator.RecalculateAllAsync(ct);
                                taskWriteResult = new
                                {
                                    updated,
                                    ptsId = samplePtsId,
                                    projectId = task.ProjectId,
                                    projectStatusesChanged,
                                    activitiesWritten = acts.Take(2).Select(a => new { a.ID, a.Text, a.CreateBy, a.CreateDttm }).ToList()
                                };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        taskWriteResult = new { error = ex.Message };
                    }
                }

                bool allEqual = !hadException && checks.All(c =>
                {
                    var prop = c.GetType().GetProperty("equal");
                    return prop == null || (bool?)prop.GetValue(c) == true;
                });

                if (writeSteps.Any())
                {
                    bool writeAllOk = writeSteps.All(w =>
                    {
                        var prop = w.GetType().GetProperty("ok");
                        return prop == null || (bool?)prop.GetValue(w) == true;
                    });
                    if (!writeAllOk) allEqual = false;
                }

                if (!allEqual)
                {
                    return Results.Json(new { allEqual = false, checks, writeSteps, taskWriteResult }, statusCode: StatusCodes.Status500InternalServerError);
                }

                return Results.Ok(new { allEqual = true, checks, writeSteps, taskWriteResult });
            }).AllowAnonymous();
        }

        private static void MapParityBatch5(WebApplication app)
        {
            app.MapGet("/health/parity/batch5", async (
                string? writeTest,
                IServiceProvider sp,
                IConfiguration config,
                CancellationToken ct) =>
            {
                var db = sp.GetRequiredService<ApplicationDbContext>();
                var conn = db.Database.GetDbConnection();

                var sqlCapRepo = sp.GetRequiredService<CapRepository>();
                var cosmosCapRepo = sp.GetRequiredService<CosmosCapRepository>();
                var sqlVesselRepo = sp.GetRequiredService<VesselRepository>();
                var cosmosVesselRepo = sp.GetRequiredService<CosmosVesselRepository>();
                var sqlProjectRepo = sp.GetRequiredService<ProjectRepository>();
                var cosmosProjectRepo = sp.GetRequiredService<CosmosProjectRepository>();

                var checks = new List<object>();
                var writeSteps = new List<object>();
                bool hadException = false;

                // 1. CapRepository parity
                try
                {
                    // 1a. GetCapIndexAsync
                    var sqlIndex = await sqlCapRepo.GetCapIndexAsync();
                    var cosmosIndex = await cosmosCapRepo.GetCapIndexAsync();
                    checks.Add(ParityCompare.Lists("Cap.GetCapIndexAsync", sqlIndex, cosmosIndex, x => x.ID ?? 0, false,
                        ("ID", x => x.ID),
                        ("Name", x => x.Name),
                        ("IMO", x => x.IMO),
                        ("ProjectStatusName", x => x.ProjectStatusName)));

                    // 1b. GetProjectByStatusesAsync
                    var sqlStatuses = await sqlCapRepo.GetProjectByStatusesAsync();
                    var cosmosStatuses = await cosmosCapRepo.GetProjectByStatusesAsync();
                    checks.Add(ParityCompare.Lists("Cap.GetProjectByStatusesAsync", sqlStatuses, cosmosStatuses, x => x.Category, false,
                        ("Category", x => x.Category),
                        ("Value", x => x.Value),
                        ("Color", x => x.Color)));

                    // 1c. GetCertsByRegionAsync
                    var sqlCertsRegion = await sqlCapRepo.GetCertsByRegionAsync();
                    var cosmosCertsRegion = await cosmosCapRepo.GetCertsByRegionAsync();
                    checks.Add(ParityCompare.Lists("Cap.GetCertsByRegionAsync", sqlCertsRegion, cosmosCertsRegion, x => x.Category, false,
                        ("Category", x => x.Category),
                        ("Value", x => x.Value),
                        ("Color", x => x.Color)));

                    // 1d. GetProjectCountAsync
                    var sqlProjCount = await sqlCapRepo.GetProjectCountAsync();
                    var cosmosProjCount = await cosmosCapRepo.GetProjectCountAsync();
                    checks.Add(ParityCompare.Single("Cap.GetProjectCountAsync", sqlProjCount, cosmosProjCount,
                        ("Count", x => x.Count)));

                    // 1e. GetCertsCountAsync
                    var sqlCertsCount = await sqlCapRepo.GetCertsCountAsync();
                    var cosmosCertsCount = await cosmosCapRepo.GetCertsCountAsync();
                    checks.Add(ParityCompare.Single("Cap.GetCertsCountAsync", sqlCertsCount, cosmosCertsCount,
                        ("Count", x => x.Count)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "CapRepository reads", equal = false, error = ex.Message });
                }

                // 2. VesselRepository parity
                try
                {
                    var sampleProjIds = (await conn.QueryAsync<int>("SELECT DISTINCT TOP 25 ID FROM [CAP].[Projects] ORDER BY ID")).ToList();
                    sampleProjIds.Add(-99999); // missing

                    foreach (var pid in sampleProjIds)
                    {
                        var sqlSS = await sqlVesselRepo.GetSurveyStatus(pid);
                        var cosmosSS = await cosmosVesselRepo.GetSurveyStatus(pid);
                        checks.Add(ParityCompare.Lists($"Vessel.GetSurveyStatus({pid})", sqlSS, cosmosSS, null, false,
                            ("Vessel_ID", x => x.Vessel_ID),
                            ("IMONum", x => x.IMONum),
                            ("SpecialContinuousHull1", x => x.SpecialContinuousHull1),
                            ("SpecialHullNo", x => x.SpecialHullNo)));

                        var sqlMD = await sqlVesselRepo.GetVesselMainData(pid);
                        var cosmosMD = await cosmosVesselRepo.GetVesselMainData(pid);
                        checks.Add(ParityCompare.Lists($"Vessel.GetVesselMainData({pid})", sqlMD, cosmosMD, null, false,
                            ("projectId", x => x.projectId),
                            ("vesselName", x => x.vesselName),
                            ("imoNumber", x => x.imoNumber),
                            ("yearBuilt", x => x.yearBuilt)));

                        var sqlSC = await sqlVesselRepo.GetStatutoryCertificate(pid);
                        var cosmosSC = await cosmosVesselRepo.GetStatutoryCertificate(pid);
                        checks.Add(ParityCompare.Single($"Vessel.GetStatutoryCertificate({pid})", sqlSC, cosmosSC,
                            ("Exp_LLLI", x => x.StatutoryCertificatesExpirationDates?.LLLIDate),
                            ("Iss_LLLI", x => x.StatutoryCertificatesIssuedDates?.LLLIDate),
                            ("Exp_IOPP", x => x.StatutoryCertificatesExpirationDates?.IOPPDate),
                            ("Iss_IOPP", x => x.StatutoryCertificatesIssuedDates?.IOPPDate)));
                    }

                    var sampleImos = (await conn.QueryAsync<int>("SELECT DISTINCT TOP 10 IMO FROM [CAP].[Projects] WHERE IMO IS NOT NULL AND IMO > 0 ORDER BY IMO")).ToList();
                    sampleImos.Add(-99999); // missing

                    foreach (var imo in sampleImos)
                    {
                        var imoStr = imo.ToString();
                        var sqlDetails = await sqlVesselRepo.Details(imoStr);
                        var cosmosDetails = await cosmosVesselRepo.Details(imoStr);
                        checks.Add(ParityCompare.Single($"Vessel.Details({imo})", sqlDetails, cosmosDetails,
                            ("Vessel_ID", x => x.Vessel_ID),
                            ("IMONum", x => x.IMONum),
                            ("VesselName", x => x.VesselName)));
                    }
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "VesselRepository reads", equal = false, error = ex.Message });
                }

                // 3. ProjectRepository parity
                try
                {
                    var sampleProjIds = (await conn.QueryAsync<int>("SELECT DISTINCT TOP 25 ID FROM [CAP].[Projects] ORDER BY ID")).ToList();
                    sampleProjIds.Add(-99999); // missing

                    foreach (var pid in sampleProjIds)
                    {
                        var sqlP = await sqlProjectRepo.GetProject(pid);
                        var cosmosP = await cosmosProjectRepo.GetProject(pid);
                        checks.Add(ParityCompare.Single($"Project.GetProject({pid})", sqlP, cosmosP,
                            ("Id", x => x.Id),
                            ("Name", x => x.Name),
                            ("PIDNumber", x => x.PIDNumber),
                            ("IMO", x => x.IMO),
                            ("ProjectStatus", x => x.ProjectStatus),
                            ("ProjectPriority", x => x.ProjectPriority)));

                        var sqlName = await sqlProjectRepo.GetProjectName(pid);
                        var cosmosName = await cosmosProjectRepo.GetProjectName(pid);
                        checks.Add(new { check = $"Project.GetProjectName({pid})", equal = sqlName == cosmosName, sql = sqlName, cosmos = cosmosName });

                        var sqlImo = await sqlProjectRepo.GetIMONumberByProjectId(pid);
                        var cosmosImo = await cosmosProjectRepo.GetIMONumberByProjectId(pid);
                        checks.Add(new { check = $"Project.GetIMONumberByProjectId({pid})", equal = sqlImo == cosmosImo, sql = sqlImo, cosmos = cosmosImo });

                        var sqlVT = await sqlProjectRepo.GetProjectVesselType(pid);
                        var cosmosVT = await cosmosProjectRepo.GetProjectVesselType(pid);
                        checks.Add(new { check = $"Project.GetProjectVesselType({pid})", equal = sqlVT == cosmosVT, sql = sqlVT, cosmos = cosmosVT });

                        var sqlDet = await sqlProjectRepo.GetDetails(pid);
                        var cosmosDet = await cosmosProjectRepo.GetDetails(pid);
                        checks.Add(ParityCompare.Lists($"Project.GetDetails({pid})", sqlDet, cosmosDet, x => x.Name, false,
                            ("Name", x => x.Name),
                            ("Type", x => x.Type),
                            ("LastModifiedDate", x => x.LastModifiedDate)));

                        var sqlSum = await sqlProjectRepo.GetSummary(pid);
                        var cosmosSum = await cosmosProjectRepo.GetSummary(pid);
                        checks.Add(ParityCompare.Single($"Project.GetSummary({pid})", sqlSum, cosmosSum,
                            ("ID", x => x.ID),
                            ("Name", x => x.Name),
                            ("IMO", x => x.IMO),
                            ("PIDNumber", x => x.PIDNumber),
                            ("AreTechnicalTasksComplete", x => x.AreTechnicalTasksComplete),
                            ("AreInvoiceTasksComplete", x => x.AreInvoiceTasksComplete)));
                    }

                    var sampleImos = (await conn.QueryAsync<int>("SELECT DISTINCT TOP 10 IMO FROM [CAP].[Projects] WHERE IMO IS NOT NULL AND IMO > 0 ORDER BY IMO")).ToList();
                    sampleImos.Add(-99999);

                    foreach (var imo in sampleImos)
                    {
                        var sqlExists = await sqlProjectRepo.CheckProjectExistsByImoNumber(imo);
                        var cosmosExists = await cosmosProjectRepo.CheckProjectExistsByImoNumber(imo);
                        checks.Add(new { check = $"Project.CheckProjectExistsByImoNumber({imo})", equal = sqlExists == cosmosExists, sql = sqlExists, cosmos = cosmosExists });

                        var sqlList = await sqlProjectRepo.GetProjectListByIMO(imo);
                        var cosmosList = await cosmosProjectRepo.GetProjectListByIMO(imo);
                        checks.Add(ParityCompare.Lists($"Project.GetProjectListByIMO({imo})", sqlList, cosmosList, x => x.Value, false,
                            ("Value", x => x.Value),
                            ("Text", x => x.Text)));
                    }

                    // CheckProjectExistsByProjectName (case sensitivity test)
                    var sampleNames = (await conn.QueryAsync(
                        "SELECT TOP 10 Name, IMO, CAST(ShipType AS int) AS ShipType FROM [CAP].[Projects] WHERE Name IS NOT NULL AND IMO IS NOT NULL AND ShipType IS NOT NULL")).ToList();
                    foreach (dynamic row in sampleNames)
                    {
                        string name = (string)row.Name;
                        int imo = (int)row.IMO;
                        int shipType = (int)row.ShipType;
                        var sqlExistsExact = await sqlProjectRepo.CheckProjectExistsByProjectName(name, imo, shipType);
                        var cosmosExistsExact = await cosmosProjectRepo.CheckProjectExistsByProjectName(name, imo, shipType);
                        checks.Add(new { check = $"Project.CheckProjectExistsByProjectName(exact: {name})", equal = sqlExistsExact == cosmosExistsExact, sql = sqlExistsExact, cosmos = cosmosExistsExact });

                        var sqlExistsUpper = await sqlProjectRepo.CheckProjectExistsByProjectName(name.ToUpperInvariant(), imo, shipType);
                        var cosmosExistsUpper = await cosmosProjectRepo.CheckProjectExistsByProjectName(name.ToUpperInvariant(), imo, shipType);
                        checks.Add(new { check = $"Project.CheckProjectExistsByProjectName(upper: {name})", equal = sqlExistsUpper == cosmosExistsUpper, sql = sqlExistsUpper, cosmos = cosmosExistsUpper });
                    }
                    var sqlMissing = await sqlProjectRepo.CheckProjectExistsByProjectName("__NONEXISTENT__", 9999999, 1);
                    var cosmosMissing = await cosmosProjectRepo.CheckProjectExistsByProjectName("__NONEXISTENT__", 9999999, 1);
                    checks.Add(new { check = "Project.CheckProjectExistsByProjectName(missing)", equal = sqlMissing == cosmosMissing, sql = sqlMissing, cosmos = cosmosMissing });

                    // GetAllProjectsGridData
                    var sqlGrid = await sqlProjectRepo.GetAllProjectsGridData();
                    var cosmosGrid = await cosmosProjectRepo.GetAllProjectsGridData();
                    checks.Add(ParityCompare.Lists("Project.GetAllProjectsGridData", sqlGrid, cosmosGrid, x => x.Id ?? 0, false,
                        ("Id", x => x.Id),
                        ("Name", x => x.Name),
                        ("PIDNumber", x => x.PIDNumber),
                        ("IMO", x => x.IMO),
                        ("StatusName", x => x.StatusName),
                        ("IsDownload", x => x.IsDownload)));

                    // GetAllProjectsGridExport
                    var sqlGridExport = await sqlProjectRepo.GetAllProjectsGridExport();
                    var cosmosGridExport = await cosmosProjectRepo.GetAllProjectsGridExport();
                    checks.Add(ParityCompare.Lists("Project.GetAllProjectsGridExport", sqlGridExport, cosmosGridExport, x => x.ID ?? 0, false,
                        ("ID", x => x.ID),
                        ("Name", x => x.Name),
                        ("PIDNumber", x => x.PIDNumber),
                        ("CapRegion", x => x.CapRegion),
                        ("ProjectStatusName", x => x.ProjectStatusName),
                        ("FatigueGrade", x => x.FatigueGrade),
                        ("FinalGrade", x => x.FinalGrade)));

                    // GetAllProjects
                    var sqlAllProjects = await sqlProjectRepo.GetAllProjects();
                    var cosmosAllProjects = await cosmosProjectRepo.GetAllProjects();
                    checks.Add(ParityCompare.Lists("Project.GetAllProjects", sqlAllProjects, cosmosAllProjects, x => x.Id, false,
                        ("Id", x => x.Id),
                        ("Name", x => x.Name),
                        ("PIDNumber", x => x.PIDNumber),
                        ("IMO", x => x.IMO),
                        ("ProjectStatusName", x => x.ProjectStatusName)));

                    // GetProjectStats
                    var sqlStats = await sqlProjectRepo.GetProjectStats();
                    var cosmosStats = await cosmosProjectRepo.GetProjectStats();
                    checks.Add(ParityCompare.Single("Project.GetProjectStats", sqlStats, cosmosStats,
                        ("Total", x => x.Total),
                        ("Online", x => x.Online),
                        ("Offline", x => x.Offline)));

                    // GetProjectCountGroupedByClientIdAsync
                    var sqlCounts = await sqlProjectRepo.GetProjectCountGroupedByClientIdAsync(ct);
                    var cosmosCounts = await cosmosProjectRepo.GetProjectCountGroupedByClientIdAsync(ct);
                    checks.Add(ParityCompare.Lists("Project.GetProjectCountGroupedByClientIdAsync", sqlCounts, cosmosCounts, x => x.ClientId, false,
                        ("ClientId", x => x.ClientId),
                        ("ProjectCount", x => x.ProjectCount)));

                    // GetProjectsByClientIdAsync
                    var sampleClientIds = (await conn.QueryAsync<Guid>("SELECT DISTINCT TOP 5 Id FROM [CAP].[Clients]")).ToList();
                    foreach (var cid in sampleClientIds)
                    {
                        var sqlByClient = await sqlProjectRepo.GetProjectsByClientIdAsync(cid);
                        var cosmosByClient = await cosmosProjectRepo.GetProjectsByClientIdAsync(cid);
                        checks.Add(ParityCompare.Lists($"Project.GetProjectsByClientIdAsync({cid})", sqlByClient, cosmosByClient, x => x.ID, false,
                            ("ID", x => x.ID),
                            ("Name", x => x.Name),
                            ("PIDNumber", x => x.PIDNumber),
                            ("StatusName", x => x.StatusName)));
                    }
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "ProjectRepository reads", equal = false, error = ex.Message });
                }

                // 4. Write tests (&writeTest=yes, Cosmos only, cleanup in finally)
                if (string.Equals(writeTest, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    int createdProjectId = 0;
                    var containerFactory = sp.GetRequiredService<ICosmosContainerFactory>();
                    var projectsContainer = containerFactory.GetContainer(CosmosContainers.CAP_Projects);
                    var tasksContainer = containerFactory.GetContainer(CosmosContainers.CAP_ProjectTaskStatus);
                    var gradesContainer = containerFactory.GetContainer(CosmosContainers.CAP_ProjectGrades);
                    var rvmContainer = containerFactory.GetContainer(CosmosContainers.dbo_ReportVesselMainData);
                    var rscContainer = containerFactory.GetContainer(CosmosContainers.dbo_ReportStatutoryCertificates);
                    var rsdContainer = containerFactory.GetContainer(CosmosContainers.dbo_ReportSurveyDetails);
                    var actContainer = containerFactory.GetContainer(CosmosContainers.CAP_Activities);

                    try
                    {
                        // 4a. Create project
                        var newProj = new Project
                        {
                            Name = "Batch5 Dev Test Project",
                            IMO = 9991234,
                            PIDNumber = 8888,
                            CapRegion = "Americas/Europe",
                            CapType = CapType.InitialAssessment,
                            ShipType = ShipType.BulkCarrier,
                            SpecialHull = true,
                            Fatigue = true
                        };
                        createdProjectId = await cosmosProjectRepo.CreateProject(newProj, "batch5Tester");
                        var readProj = await cosmosProjectRepo.GetProject(createdProjectId);
                        writeSteps.Add(new { step = "1. CreateProject", ok = createdProjectId > 0 && readProj != null && readProj.Name == newProj.Name, detail = $"id={createdProjectId}" });

                        // 4b. Update project
                        if (readProj != null)
                        {
                            readProj.Name = "Batch5 Dev Test Project Updated";
                            await cosmosProjectRepo.Update_Project(readProj, "batch5Tester");
                            var updatedProj = await cosmosProjectRepo.GetProject(createdProjectId);
                            writeSteps.Add(new { step = "2. Update_Project", ok = updatedProj?.Name == "Batch5 Dev Test Project Updated" });
                        }

                        // 4c. Update percent complete
                        var updPctResult = await cosmosProjectRepo.UpdatePercentComplete(createdProjectId, 75, "batch5Tester");
                        var pctProj = await cosmosProjectRepo.GetProject(createdProjectId);
                        writeSteps.Add(new { step = "3. UpdatePercentComplete", ok = updPctResult == 1 && pctProj?.PercentComplete == 75 });

                        // 4d. Update project priority
                        await cosmosProjectRepo.UpdatePriority(createdProjectId, 1, "batch5Tester");
                        var prioProj = await cosmosProjectRepo.GetProject(createdProjectId);
                        writeSteps.Add(new { step = "4. UpdateProjectPriority", ok = prioProj?.ProjectPriority == 1 });

                        // 4e. Update project status (13 and 11)
                        await cosmosProjectRepo.UpdateProjectStatus(createdProjectId, "batch5Tester", 13, "batch5Tester");
                        var status13Proj = await cosmosProjectRepo.GetProject(createdProjectId);
                        await cosmosProjectRepo.UpdateProjectStatus(createdProjectId, "batch5Tester", 11, "batch5Tester");
                        var status11Proj = await cosmosProjectRepo.GetProject(createdProjectId);
                        writeSteps.Add(new { step = "5. UpdateProjectStatus", ok = status13Proj?.ProjectStatus == 13 && status11Proj?.ProjectStatus == 11 });

                        // 4f. Vessel writes (insert and update main data)
                        var surveyReport = new Report
                        {
                            ProjectId = createdProjectId,
                            ReportNo = "REP-B5-001",
                            ReportPort = "Houston",
                            ReportDate = "2026-11-18",
                            Vessel = new DigitalCap.Core.Models.Survey.Vessel
                            {
                                ID = 1,
                                ImoNumber = 9991234,
                                VesselName = "Batch5 Test Vessel",
                                VesselType = "Tanker",
                                SelectedPrefix = "2",
                                YearBuilt = 2020,
                                Length = 225.5f,
                                Breadth = 32.2f,
                                DEPTH = 18.0f,
                                Draft = 12.5f,
                                DEADWEIGHT = "50000",
                                GrossTons = 30000.0f,
                                NetTons = 15000.0f,
                                KW = "12000",
                                ShaftRPM = 105.0f
                            }
                        };
                        var vIns = await cosmosVesselRepo.UpdateReportMainDataAsync(surveyReport, createdProjectId);
                        var vMain1 = (await cosmosVesselRepo.GetVesselMainData(createdProjectId)).FirstOrDefault();

                        surveyReport.Vessel.VesselName = "Batch5 Test Vessel Modified";
                        surveyReport.Vessel.SelectedPrefix = "3";
                        var vUpd = await cosmosVesselRepo.UpdateReportMainDataAsync(surveyReport, createdProjectId);
                        var vMain2 = (await cosmosVesselRepo.GetVesselMainData(createdProjectId)).FirstOrDefault();
                        writeSteps.Add(new { step = "6. CreateReportVesselMainData (insert & update)", ok = vIns && vUpd && vMain2?.vesselName == "Batch5 Test Vessel Modified" });

                        // 4g. Delete project (soft deletes on 7 tables + activity)
                        await cosmosProjectRepo.DeleteProject(createdProjectId, "batch5Deleter", "batch5Deleter");
                        var deletedProj = await cosmosProjectRepo.GetProject(createdProjectId);
                        writeSteps.Add(new { step = "7. DeleteProject", ok = deletedProj == null });
                    }
                    finally
                    {
                        if (createdProjectId > 0)
                        {
                            var pidStr = createdProjectId.ToString();
                            var pkDbl = new PartitionKey((double)createdProjectId);
                            var pkStr = new PartitionKey(pidStr);

                            try { await projectsContainer.DeleteItemAsync<ProjectDocument>(pidStr, pkStr); } catch { }
                            try { await rvmContainer.DeleteItemAsync<ReportVesselMainDataDocument>(pidStr, pkStr); } catch { }
                            try { await rscContainer.DeleteItemAsync<ReportStatutoryCertificatesDocument>(pidStr, pkStr); } catch { }
                            try { await rsdContainer.DeleteItemAsync<ReportSurveyDetailsDocument>(pidStr, pkStr); } catch { }

                            // Clean tasks, grades, activities
                            try
                            {
                                var tasksFeed = tasksContainer.GetItemQueryIterator<ProjectTaskStatusDocument>(
                                    new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @pid").WithParameter("@pid", createdProjectId),
                                    requestOptions: new QueryRequestOptions { PartitionKey = pkDbl });
                                while (tasksFeed.HasMoreResults)
                                {
                                    var r = await tasksFeed.ReadNextAsync();
                                    foreach (var item in r)
                                    {
                                        try { await tasksContainer.DeleteItemAsync<ProjectTaskStatusDocument>(item.id, pkDbl); } catch { }
                                    }
                                }
                            }
                            catch { }

                            try
                            {
                                var gradesFeed = gradesContainer.GetItemQueryIterator<ProjectGradeDocument>(
                                    new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @pid").WithParameter("@pid", createdProjectId),
                                    requestOptions: new QueryRequestOptions { PartitionKey = pkDbl });
                                while (gradesFeed.HasMoreResults)
                                {
                                    var r = await gradesFeed.ReadNextAsync();
                                    foreach (var item in r)
                                    {
                                        try { await gradesContainer.DeleteItemAsync<ProjectGradeDocument>(item.id, pkDbl); } catch { }
                                    }
                                }
                            }
                            catch { }

                            try
                            {
                                var actFeed = actContainer.GetItemQueryIterator<ActivityDocument>(
                                    new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @pid").WithParameter("@pid", createdProjectId),
                                    requestOptions: new QueryRequestOptions { PartitionKey = pkDbl });
                                while (actFeed.HasMoreResults)
                                {
                                    var r = await actFeed.ReadNextAsync();
                                    foreach (var item in r)
                                    {
                                        try { await actContainer.DeleteItemAsync<ActivityDocument>(item.id, pkDbl); } catch { }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }

                bool allEqual = !hadException && checks.All(c =>
                {
                    var prop = c.GetType().GetProperty("equal");
                    return prop == null || (bool?)prop.GetValue(c) == true;
                });

                if (writeSteps.Any())
                {
                    bool writeAllOk = writeSteps.All(w =>
                    {
                        var prop = w.GetType().GetProperty("ok");
                        return prop == null || (bool?)prop.GetValue(w) == true;
                    });
                    if (!writeAllOk) allEqual = false;
                }

                if (!allEqual)
                {
                    return Results.Json(new { allEqual = false, checks, writeSteps }, statusCode: StatusCodes.Status500InternalServerError);
                }

                return Results.Ok(new { allEqual = true, checks, writeSteps });
            }).AllowAnonymous();
        }

        private static void MapDevCopyTable(WebApplication app)
        {
            app.MapGet("/dev/copy-table", async (
                string? schema,
                string? table,
                string? idColumn,
                string? confirm,
                SqlToCosmosCopier copier,
                CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(schema) || string.IsNullOrWhiteSpace(table))
                {
                    return Results.BadRequest(new { error = "schema and table query parameters are required" });
                }

                var allowedIdentityTables = new[] { "AspNetUsers", "AspNetRoles", "AspNetUserRoles", "UserAccounts", "AspNetUserLogins" };
                if (string.Equals(schema, "dbo", StringComparison.OrdinalIgnoreCase) &&
                    table.StartsWith("AspNet", StringComparison.OrdinalIgnoreCase) &&
                    !allowedIdentityTables.Contains(table, StringComparer.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { error = $"Copying table {schema}.{table} is not allowed." });
                }

                if (!string.Equals(confirm, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { error = "add &confirm=yes to copy" });
                }

                try
                {
                    var result = await copier.CopyTableAsync(schema, table, idColumn, ct);
                    if (result.failed == 0 && result.upserted == result.sqlRows)
                    {
                        return Results.Ok(result);
                    }

                    return Results.Json(result, statusCode: StatusCodes.Status500InternalServerError);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status500InternalServerError);
                }
            }).AllowAnonymous();

            app.MapPost("/dev/cosmos/copy-table/{table}", async (
                string table,
                string? schema,
                string? idColumn,
                string? confirm,
                SqlToCosmosCopier copier,
                CancellationToken ct) =>
            {
                var targetSchema = schema;
                var targetTable = table;
                if (table.Contains('.'))
                {
                    var parts = table.Split('.', 2);
                    targetSchema = parts[0];
                    targetTable = parts[1];
                }
                else if (string.IsNullOrWhiteSpace(targetSchema))
                {
                    targetSchema = "dbo";
                }

                var allowedIdentityTables = new[] { "AspNetUsers", "AspNetRoles", "AspNetUserRoles", "UserAccounts", "AspNetUserLogins" };
                if (string.Equals(targetSchema, "dbo", StringComparison.OrdinalIgnoreCase) &&
                    targetTable.StartsWith("AspNet", StringComparison.OrdinalIgnoreCase) &&
                    !allowedIdentityTables.Contains(targetTable, StringComparer.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { error = $"Copying table {targetSchema}.{targetTable} is not allowed." });
                }

                if (!string.Equals(confirm, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { error = "add &confirm=yes to copy" });
                }

                try
                {
                    var result = await copier.CopyTableAsync(targetSchema, targetTable, idColumn, ct);
                    if (result.failed == 0 && result.upserted == result.sqlRows)
                    {
                        return Results.Ok(result);
                    }

                    return Results.Json(result, statusCode: StatusCodes.Status500InternalServerError);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status500InternalServerError);
                }
            }).AllowAnonymous();
        }
    }
}
