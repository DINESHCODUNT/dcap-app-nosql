using Dapper;
using DigitalCap.Core.DTO;
using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.ExportConfig;
using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.Models.ImageDescription;
using DigitalCap.Core.Models.Permissions;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.Models.View.Admin;
using DigitalCap.Core.Models.VesselModel;
using DigitalCap.Core.Security;
using ShipType = DigitalCap.Core.Enumerations.ShipType;
using Grading = DigitalCap.Core.Models.Grading.Grading;
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
            MapParityBatch6(app);
            MapParityBatch7(app);
            MapParityBatch8(app);
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
                bool allPassed = true;

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

        private static void MapParityBatch6(WebApplication app)
        {
            app.MapGet("/health/parity/batch6", async (
                string? writeTest,
                IServiceProvider sp,
                IConfiguration config,
                CancellationToken ct) =>
            {
                var sqlTemplateRepo = sp.GetRequiredService<TemplateRepository>();
                var cosmosTemplateRepo = sp.GetRequiredService<CosmosTemplateRepository>();
                var sqlReportRepo = sp.GetRequiredService<ProjectReportRepository>();
                var cosmosReportRepo = sp.GetRequiredService<CosmosProjectReportRepository>();
                var sqlExportRepo = sp.GetRequiredService<ExportRepository>();
                var cosmosExportRepo = sp.GetRequiredService<CosmosExportRepository>();
                var sqlDescRepo = sp.GetRequiredService<DescriptionRepository>();
                var cosmosDescRepo = sp.GetRequiredService<CosmosDescriptionRepository>();

                var checks = new List<object>();
                var writeSteps = new List<object>();
                bool hadException = false;
                bool allPassed = true;

                // 1. TemplateRepository parity
                try
                {
                    var sqlBytes = await sqlTemplateRepo.PopulateTemplate(1);
                    var cosmosBytes = await cosmosTemplateRepo.PopulateTemplate(1);
                    checks.Add(new
                    {
                        check = "Template.PopulateTemplate",
                        sqlLen = sqlBytes?.Length ?? 0,
                        cosmosLen = cosmosBytes?.Length ?? 0,
                        match = (sqlBytes?.Length ?? 0) == (cosmosBytes?.Length ?? 0)
                    });
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Template.PopulateTemplate", error = ex.Message });
                }

                // 2. ProjectReportRepository parity across 25 projects + 1 missing
                var testProjectIds = Enumerable.Range(1, 25).Concat(new[] { 999999 }).ToList();
                foreach (var pid in testProjectIds)
                {
                    try
                    {
                        var sqlParts = await sqlReportRepo.GetProjectTemplatebyProjectId(pid);
                        var cosmosParts = await cosmosReportRepo.GetProjectTemplatebyProjectId(pid);
                        checks.Add(ParityCompare.Lists($"Report.GetProjectTemplatebyProjectId({pid})", sqlParts, cosmosParts, x => x.PartId, false,
                            ("PartId", x => x.PartId),
                            ("PartName", x => x.PartName),
                            ("SequenceNo", x => x.SequenceNo)));

                        var sqlCard = await sqlReportRepo.GetGenericImagCardByName(pid, 1, Guid.Empty, 1);
                        var cosmosCard = await cosmosReportRepo.GetGenericImagCardByName(pid, 1, Guid.Empty, 1);
                        checks.Add(ParityCompare.Single($"Report.GetGenericImagCardByName({pid})", sqlCard, cosmosCard,
                            ("Id", x => x?.Id),
                            ("ProjectId", x => x?.ProjectId),
                            ("TemplateId", x => x?.TemplateId),
                            ("CardNumber", x => x?.CardNumber),
                            ("CardName", x => x?.CardName),
                            ("AdditionalDescription", x => x?.AdditionalDescription),
                            ("CurrentCondition", x => x?.CurrentCondition),
                            ("ImageId", x => x?.ImageId),
                            ("IsActive", x => x?.IsActive)));
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = $"Report.ProjectId_{pid}", error = ex.Message });
                    }
                }

                // 3. ExportRepository parity
                // All vessel types x parts
                var testVesselTypeIds = new[] { 1, 2, 3, 4, 5, 999999 };
                var testPartIds = new[] { 1, 2, 3, 4 };
                foreach (var vtid in testVesselTypeIds)
                {
                    try
                    {
                        var sqlVtParts = await sqlExportRepo.GetVesselTypeExportPartConfiguration(vtid);
                        var cosmosVtParts = await cosmosExportRepo.GetVesselTypeExportPartConfiguration(vtid);
                        checks.Add(ParityCompare.Lists($"Export.GetVesselTypeExportPartConfiguration({vtid})", sqlVtParts, cosmosVtParts, x => x.PartId, false,
                            ("PartId", x => x.PartId),
                            ("PartName", x => x.PartName),
                            ("RequiredInReport", x => x.RequiredInReport)));

                        foreach (var partId in testPartIds)
                        {
                            var sqlVtSec = await sqlExportRepo.GetVesselTypeExportSectionConfiguration(partId, vtid);
                            var cosmosVtSec = await cosmosExportRepo.GetVesselTypeExportSectionConfiguration(partId, vtid);
                            checks.Add(ParityCompare.Lists($"Export.GetVesselTypeExportSectionConfiguration({partId},{vtid})", sqlVtSec, cosmosVtSec, x => x.SectionId, false,
                                ("SectionId", x => x.SectionId),
                                ("SectionName", x => x.SectionName),
                                ("RequiredInReport", x => x.RequiredInReport),
                                ("TankTypeId", x => x.TankTypeId),
                                ("TankType", x => x.TankType),
                                ("IsSubSection", x => x.IsSubSection),
                                ("ParentId", x => x.ParentId)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = $"Export.VesselType_{vtid}", error = ex.Message });
                    }
                }

                // Projects export configuration
                foreach (var pid in testProjectIds.Take(5).Concat(new[] { 999999 }))
                {
                    try
                    {
                        var sqlProjParts = await sqlExportRepo.GetProjectExportPartConfiguration(pid);
                        var cosmosProjParts = await cosmosExportRepo.GetProjectExportPartConfiguration(pid);
                        checks.Add(ParityCompare.Lists($"Export.GetProjectExportPartConfiguration({pid})", sqlProjParts, cosmosProjParts, x => x.PartId, false,
                            ("PartId", x => x.PartId),
                            ("PartName", x => x.PartName),
                            ("RequiredInReport", x => x.RequiredInReport)));

                        foreach (var partId in testPartIds.Take(2))
                        {
                            var sqlProjSec = await sqlExportRepo.GetProjectExportSectionConfiguration(partId, pid);
                            var cosmosProjSec = await cosmosExportRepo.GetProjectExportSectionConfiguration(partId, pid);
                            checks.Add(ParityCompare.Lists($"Export.GetProjectExportSectionConfiguration({partId},{pid})", sqlProjSec, cosmosProjSec, x => x.SectionId, false,
                                ("SectionId", x => x.SectionId),
                                ("SectionName", x => x.SectionName),
                                ("RequiredInReport", x => x.RequiredInReport),
                                ("TankTypeId", x => x.TankTypeId),
                                ("TankType", x => x.TankType),
                                ("IsSubSection", x => x.IsSubSection),
                                ("ParentId", x => x.ParentId)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = $"Export.Project_{pid}", error = ex.Message });
                    }
                }

                // 4. DescriptionRepository parity
                var testVesselNames = new[] { (string?)null, "Oil Tanker", "Chemical Carrier", "Bulk Carrier", "Container", "NONEXISTENT_VESSEL" };

                // 4a. GetGradingTemplates
                try
                {
                    var sqlGrad = await sqlDescRepo.GetGradingTemplates();
                    var cosmosGrad = await cosmosDescRepo.GetGradingTemplates();
                    checks.Add(ParityCompare.Lists("Desc.GetGradingTemplates(null)", sqlGrad, cosmosGrad, x => x.TemplateId, false,
                        ("TemplateId", x => x.TemplateId),
                        ("TemplateName", x => x.TemplateName)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Desc.GetGradingTemplates", error = ex.Message });
                }

                // 4b. GetGradingSections(0, null) & (t, vt) for every template x vessel type
                var testTemplates = new[] { 0, 1, 2, 3, 4, 8, 9999 };
                foreach (var vt in testVesselNames)
                {
                    foreach (var tId in testTemplates)
                    {
                        try
                        {
                            var sqlSecs = await sqlDescRepo.GetGradingSections(tId, vt!);
                            var cosmosSecs = await cosmosDescRepo.GetGradingSections(tId, vt!);
                            checks.Add(ParityCompare.Lists($"Desc.GetGradingSections({tId},{vt ?? "null"})", sqlSecs, cosmosSecs, x => (x.SectionId, x.TanktypeId, x.TemplateId, x.SectionName), false,
                                ("SectionId", x => x.SectionId),
                                ("TanktypeId", x => x.TanktypeId),
                                ("TemplateId", x => x.TemplateId),
                                ("SectionName", x => x.SectionName)));
                        }
                        catch (Exception ex)
                        {
                            hadException = true;
                            checks.Add(new { check = $"Desc.GetGradingSections({tId},{vt ?? "null"})", error = ex.Message });
                        }
                    }
                }

                // 4c. GetAllDescription pages 1-3 + 0 + Total
                foreach (var (pNum, pSize) in new[] { (1, 10), (2, 10), (3, 10), (1, 0) })
                {
                    try
                    {
                        var sqlAll = await sqlDescRepo.GetAllDescription(pNum, pSize);
                        var cosmosAll = await cosmosDescRepo.GetAllDescription(pNum, pSize);
                        checks.Add(new
                        {
                            check = $"Desc.GetAllDescription({pNum},{pSize}).Total",
                            sqlTotal = sqlAll?.Total ?? 0,
                            cosmosTotal = cosmosAll?.Total ?? 0,
                            match = (sqlAll?.Total ?? 0) == (cosmosAll?.Total ?? 0)
                        });
                        checks.Add(ParityCompare.Lists($"Desc.GetAllDescription({pNum},{pSize}).Data", sqlAll?.Data ?? new List<ImageDescriptions>(), cosmosAll?.Data ?? new List<ImageDescriptions>(), x => x.Id, false,
                            ("Id", x => x.Id),
                            ("Description", x => x.Description),
                            ("CategoryId", x => x.CategoryId),
                            ("ProjectId", x => x.ProjectId),
                            ("VesselType", x => x.VesselType),
                            ("TemplateId", x => x.TemplateId),
                            ("TemplateName", x => x.TemplateName),
                            ("IsActive", x => x.IsActive),
                            ("IsDeleted", x => x.IsDeleted),
                            ("SectionId", x => x.SectionId),
                            ("SectionName", x => x.SectionName),
                            ("TankTypeId", x => x.TankTypeId)));
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = $"Desc.GetAllDescription({pNum},{pSize})", error = ex.Message });
                    }
                }

                // 4d. CheckImageDescriptionExistsOrNot for 10 real template descriptions + uppercase + missing
                var checkTestCases = new[]
                {
                    ("Oil Tanker", "Slop Tank", "Part F", "Test"),
                    ("Oil Tanker", "SLOP TANK", "PART F", "TEST"),
                    ("Oil Tanker", "Slop Tank", "Part F", "Nonexistent Description 12345"),
                    ("Chemical Carrier", "Cargo Tank 1P", "Part A", "Corrosion observed"),
                    ("Bulk Carrier", "Hold 1", "Part A", "Damage"),
                    ("Bulk Carrier", "HOLD 1", "PART A", "DAMAGE"),
                    ("Container", "Bay 1", "Part B", "Wear and tear"),
                    ("NonexistentVessel", "NoSection", "NoPart", "None"),
                    ("Oil Tanker", "NoSection", "Part F", "Test"),
                    ("Oil Tanker", "Slop Tank", "NoPart", "Test")
                };

                foreach (var tc in checkTestCases)
                {
                    try
                    {
                        var sqlC = await sqlDescRepo.CheckImageDescriptionExistsOrNot(tc.Item1, tc.Item2, tc.Item3, tc.Item4);
                        var cosmosC = await cosmosDescRepo.CheckImageDescriptionExistsOrNot(tc.Item1, tc.Item2, tc.Item3, tc.Item4);
                        checks.Add(ParityCompare.Lists($"Desc.CheckImageDescriptionExistsOrNot({tc.Item1},{tc.Item2})", sqlC, cosmosC, x => x.Id, false,
                            ("Id", x => x.Id),
                            ("VesselType", x => x.VesselType),
                            ("SectionId", x => x.SectionId),
                            ("CategoryId", x => x.CategoryId),
                            ("Description", x => x.Description),
                            ("TemplateId", x => x.TemplateId)));
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = $"Desc.CheckImageDescriptionExistsOrNot({tc.Item1},{tc.Item2})", error = ex.Message });
                    }
                }

                // 4e. GetImageDescriptionsByProjectId
                foreach (var pid in testProjectIds.Take(5).Concat(new[] { 999999 }))
                {
                    try
                    {
                        var sqlImg = await sqlDescRepo.GetImageDescriptionsByProjectId(pid);
                        var cosmosImg = await cosmosDescRepo.GetImageDescriptionsByProjectId(pid);
                        checks.Add(ParityCompare.Lists($"Desc.GetImageDescriptionsByProjectId({pid})", sqlImg, cosmosImg, x => x.Id, false,
                            ("Id", x => x.Id),
                            ("Description", x => x.Description),
                            ("CategoryId", x => x.CategoryId),
                            ("ProjectId", x => x.ProjectId),
                            ("IsActive", x => x.IsActive),
                            ("IsDeleted", x => x.IsDeleted),
                            ("SectionId", x => x.SectionId),
                            ("SectionName", x => x.SectionName),
                            ("TankTypeId", x => x.TankTypeId)));
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = $"Desc.GetImageDescriptionsByProjectId({pid})", error = ex.Message });
                    }
                }

                // 4f. GetImageDescriptionById
                foreach (var descId in new[] { 1, 2, 3, 5, 10, 999999 })
                {
                    try
                    {
                        var sqlD = await sqlDescRepo.GetImageDescriptionById(descId);
                        var cosmosD = await cosmosDescRepo.GetImageDescriptionById(descId);
                        checks.Add(ParityCompare.Single($"Desc.GetImageDescriptionById({descId})", sqlD, cosmosD,
                            ("Id", x => x?.Id),
                            ("Description", x => x?.Description),
                            ("CategoryId", x => x?.CategoryId),
                            ("ProjectId", x => x?.ProjectId),
                            ("IsActive", x => x?.IsActive),
                            ("IsDeleted", x => x?.IsDeleted),
                            ("VesselType", x => x?.VesselType)));
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = $"Desc.GetImageDescriptionById({descId})", error = ex.Message });
                    }
                }

                // Write tests: Cosmos only, writeTest=yes, project -999, vessel type PARITY-TEST
                if (string.Equals(writeTest, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    var factory = sp.GetRequiredService<ICosmosContainerFactory>();
                    var cardContainer = factory.GetContainer(CosmosContainers.dbo_GenericImageCard);
                    var descContainer = factory.GetContainer(CosmosContainers.dbo_ImageDescriptions);
                    var vsdContainer = factory.GetContainer(CosmosContainers.Config_VesselTypeSectionDescriptionMapping);
                    var vtpContainer = factory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
                    var vnsContainer = factory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
                    var rpContainer = factory.GetContainer(CosmosContainers.Config_ReportPart);
                    var psmContainer = factory.GetContainer(CosmosContainers.Config_ProjectSectionMapping);
                    var projContainer = factory.GetContainer(CosmosContainers.CAP_Projects);
                    var vtmContainer = factory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);

                    var testSecGuid = Guid.NewGuid();
                    var testSecStr = testSecGuid.ToString();
                    var testTankSecGuid = Guid.NewGuid();
                    var testTankSecStr = testTankSecGuid.ToString();

                    bool vtpOk = false;
                    bool rpOk = false;
                    bool vnsOk = false;
                    bool psmUnchanged = false;
                    bool psmOk = false;
                    bool vtmPidMatchOk = false;
                    bool vtmPidNoMatchOk = false;
                    bool vtm0Ok = false;
                    bool createDescRes1 = false;
                    bool vsdCreated = false;
                    bool noSecondMapping = false;
                    bool updateDescRes = false;
                    bool oldGone = false;
                    bool newExists = false;
                    bool datesApproxNow = false;
                    bool createCardRes = false;
                    bool guidEmptyKept = false;
                    bool cardMoved = false;
                    ImageDescriptionDocument? createdRealDoc = null;
                    var trackedVsdIds = new List<string>();

                    try
                    {
                        // 1. Create test rows first
                        var vtpDoc = new VesselTypePartMappingDocument
                        {
                            id = "test-vtp--999",
                            VesselTypePartMappingId = 9991,
                            VesselTypeId = -999,
                            PartName = "TEST-PART",
                            RequiredInReport = false,
                            IsActive = true,
                            IsDeleted = false
                        };
                        await vtpContainer.CreateItemAsync(vtpDoc, new PartitionKey(-999.0));

                        var vnsDoc = new VesselTypeNormalSectionMappingDocument
                        {
                            id = "test-vns--999",
                            VesselTypeId = -999,
                            VesselTypePartMappingId = 9991,
                            SectionMappingId = testSecStr,
                            SectionName = "TEST-NORMAL-SECTION",
                            SubHeader = "KEEP",
                            TotalCards = 7,
                            RequiredInReport = false,
                            IsActive = true,
                            IsDeleted = false
                        };
                        await vnsContainer.CreateItemAsync(vnsDoc, new PartitionKey(-999.0));

                        var rpDoc = new ReportPartDocument
                        {
                            id = "test-rp--999",
                            PartId = 9991,
                            ProjectId = -999,
                            PartName = "TEST-REPORT-PART",
                            RequiredInReport = false,
                            IsActive = true,
                            IsDeleted = false
                        };
                        await rpContainer.CreateItemAsync(rpDoc, new PartitionKey(-999.0));

                        var psmDoc = new ProjectSectionMappingDocument
                        {
                            id = "test-psm--999",
                            ProjectId = -999,
                            SectionMappingId = testSecStr,
                            SubHeader = "KEEP",
                            TotalCards = 7,
                            RequiredInReport = false,
                            IsDeleted = false
                        };
                        await psmContainer.CreateItemAsync(psmDoc, new PartitionKey(-999.0));

                        var projDoc = new ProjectDocument
                        {
                            id = "-999",
                            ID = -999,
                            IdProp = -999,
                            IMO = 9999999
                        };
                        await projContainer.CreateItemAsync(projDoc, new PartitionKey("-999"));

                        var vtmMatch = new VesselTankMappingDocument
                        {
                            id = "test-vtm-match",
                            Id = testTankSecStr,
                            ProjectId = -999,
                            ImoNumber = "9999999",
                            RequiredInReport = false,
                            IsDeleted = false
                        };
                        await vtmContainer.CreateItemAsync(vtmMatch, new PartitionKey(-999.0));

                        var vtmNoMatch = new VesselTankMappingDocument
                        {
                            id = "test-vtm-nomatch",
                            Id = testTankSecStr,
                            ProjectId = -999,
                            ImoNumber = "1234567",
                            RequiredInReport = false,
                            IsDeleted = false
                        };
                        await vtmContainer.CreateItemAsync(vtmNoMatch, new PartitionKey(-999.0));

                        writeSteps.Add(new { step = "TestRowsCreated", success = true });

                        // 2. Run every Update*RequiredInReport branch and READ BACK
                        // Part: VesselTypeId -999
                        await cosmosExportRepo.UpdatePartRequiredInReportAsync(true, 9991, 0, -999, "parity_tester");
                        var vtpRead = await vtpContainer.ReadItemAsync<VesselTypePartMappingDocument>("test-vtp--999", new PartitionKey(-999.0));
                        vtpOk = vtpRead.Resource.RequiredInReport == true && vtpRead.Resource.ModifiedBy == "parity_tester";

                        // Part: VesselTypeId 0, ProjectId -999
                        await cosmosExportRepo.UpdatePartRequiredInReportAsync(true, 9991, -999, 0, "parity_tester");
                        var rpRead = await rpContainer.ReadItemAsync<ReportPartDocument>("test-rp--999", new PartitionKey(-999.0));
                        rpOk = rpRead.Resource.RequiredInReport == true && rpRead.Resource.ModifiedBy == "parity_tester";
                        writeSteps.Add(new { step = "UpdatePartRequiredInReportBranches", vtpOk, rpOk });

                        // Section: VesselTypeId -999: UpdateSectionRequiredInReportAsync(true, testSecGuid, 0, -999, "parity_tester") then read vns
                        await cosmosExportRepo.UpdateSectionRequiredInReportAsync(true, testSecGuid, 0, -999, "parity_tester");
                        var vnsRead = await vnsContainer.ReadItemAsync<VesselTypeNormalSectionMappingDocument>("test-vns--999", new PartitionKey(-999.0));
                        vnsOk = vnsRead.Resource.RequiredInReport == true &&
                                vnsRead.Resource.ModifiedBy == "parity_tester" &&
                                vnsRead.Resource.SubHeader == "KEEP" &&
                                vnsRead.Resource.TotalCards == 7;

                        // Check the OTHER row (psm) is still unchanged (RequiredInReport == false)
                        var psmReadBefore = await psmContainer.ReadItemAsync<ProjectSectionMappingDocument>("test-psm--999", new PartitionKey(-999.0));
                        psmUnchanged = psmReadBefore.Resource.RequiredInReport == false;

                        // Section: ProjectId -999: UpdateSectionRequiredInReportAsync(true, testSecGuid, -999, 0, "parity_tester") then read psm
                        await cosmosExportRepo.UpdateSectionRequiredInReportAsync(true, testSecGuid, -999, 0, "parity_tester");
                        var psmRead = await psmContainer.ReadItemAsync<ProjectSectionMappingDocument>("test-psm--999", new PartitionKey(-999.0));
                        psmOk = psmRead.Resource.RequiredInReport == true &&
                                psmRead.Resource.ModifiedBy == "parity_tester" &&
                                psmRead.Resource.SubHeader == "KEEP" &&
                                psmRead.Resource.TotalCards == 7;
                        writeSteps.Add(new { step = "UpdateSectionRequiredInReportBranches", vnsOk, psmUnchanged, psmOk });

                        // TankSection: ProjectId -999 (IMO match vs no match)
                        await cosmosExportRepo.UpdateTankSectionRequiredInReportAsync(true, testTankSecGuid, -999, "parity_tester");
                        var vtmMatchRead = await vtmContainer.ReadItemAsync<VesselTankMappingDocument>("test-vtm-match", new PartitionKey(-999.0));
                        var vtmNoMatchRead = await vtmContainer.ReadItemAsync<VesselTankMappingDocument>("test-vtm-nomatch", new PartitionKey(-999.0));
                        vtmPidMatchOk = vtmMatchRead.Resource.RequiredInReport == true && vtmMatchRead.Resource.UpdatedBy == "parity_tester";
                        vtmPidNoMatchOk = vtmNoMatchRead.Resource.RequiredInReport == false; // unchanged

                        // TankSection: ProjectId 0 (updates by SectionId only)
                        await cosmosExportRepo.UpdateTankSectionRequiredInReportAsync(true, testTankSecGuid, 0, "parity_tester_0");
                        var vtmNoMatchAfter0 = await vtmContainer.ReadItemAsync<VesselTankMappingDocument>("test-vtm-nomatch", new PartitionKey(-999.0));
                        vtm0Ok = vtmNoMatchAfter0.Resource.RequiredInReport == true && vtmNoMatchAfter0.Resource.UpdatedBy == "parity_tester_0";
                        writeSteps.Add(new { step = "UpdateTankSectionRequiredInReportBranches", vtmPidMatchOk, vtmPidNoMatchOk, vtm0Ok });

                        // 3. CreateImageDescription with test normal section (Category max+1, call twice -> same Category, no second mapping)
                        var testDesc1 = new ImageDescriptions
                        {
                            Description = "Batch6 Parity Created Desc 1",
                            ProjectId = 0,
                            VesselType = "PARITY-TEST",
                            SectionId = testSecGuid,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedDttm = DateTime.UtcNow,
                            UpdatedDttm = DateTime.UtcNow
                        };
                        createDescRes1 = await cosmosDescRepo.CreateImageDescription(testDesc1);

                        // Check mapping row created in vsdContainer
                        var vsdQ = new QueryDefinition("SELECT * FROM c WHERE c.SectionId = @secId").WithParameter("@secId", testSecStr);
                        var vsdDocs = new List<VesselTypeSectionDescriptionMappingDocument>();
                        using (var feed = vsdContainer.GetItemQueryIterator<VesselTypeSectionDescriptionMappingDocument>(vsdQ))
                        {
                            while (feed.HasMoreResults)
                            {
                                var resp = await feed.ReadNextAsync();
                                vsdDocs.AddRange(resp);
                            }
                        }
                        vsdCreated = vsdDocs.Count == 1 && vsdDocs.All(d => d.id == $"{d.SectionId}|{d.CategoryId}");
                        trackedVsdIds.AddRange(vsdDocs.Select(d => d.id));
                        var firstCatId = vsdDocs.Count > 0 ? vsdDocs[0].CategoryId : null;

                        // Call CreateImageDescription second time with same section
                        var testDesc2 = new ImageDescriptions
                        {
                            Description = "Batch6 Parity Created Desc 2",
                            ProjectId = 0,
                            VesselType = "PARITY-TEST",
                            SectionId = testSecGuid,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedDttm = DateTime.UtcNow,
                            UpdatedDttm = DateTime.UtcNow
                        };
                        var createDescRes2 = await cosmosDescRepo.CreateImageDescription(testDesc2);

                        var vsdDocs2 = new List<VesselTypeSectionDescriptionMappingDocument>();
                        using (var feed = vsdContainer.GetItemQueryIterator<VesselTypeSectionDescriptionMappingDocument>(vsdQ))
                        {
                            while (feed.HasMoreResults)
                            {
                                var resp = await feed.ReadNextAsync();
                                vsdDocs2.AddRange(resp);
                            }
                        }
                        trackedVsdIds.AddRange(vsdDocs2.Select(d => d.id).Except(trackedVsdIds));
                        noSecondMapping = vsdDocs2.Count == 1 && vsdDocs2[0].CategoryId == firstCatId && vsdDocs2[0].id == $"{vsdDocs2[0].SectionId}|{vsdDocs2[0].CategoryId}";
                        writeSteps.Add(new { step = "CreateImageDescription.NewCategoryAndNoDuplicateMapping", createDescRes1, vsdCreated, noSecondMapping });

                        // 4. UpdateImageDescription on description just created (real Id), change ProjectId 0 -> -998
                        // Find the real created item
                        var descQ = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = 0 AND c.Description = 'Batch6 Parity Created Desc 1'");
                        var descDocs = new List<ImageDescriptionDocument>();
                        using (var feed = descContainer.GetItemQueryIterator<ImageDescriptionDocument>(descQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(0.0) }))
                        {
                            while (feed.HasMoreResults)
                            {
                                var resp = await feed.ReadNextAsync();
                                descDocs.AddRange(resp);
                            }
                        }
                        createdRealDoc = descDocs.FirstOrDefault();
                        int realDescId = createdRealDoc?.Id ?? 0;

                        if (createdRealDoc == null)
                        {
                            writeSteps.Add(new { step = "UpdateImageDescription.FindCreatedDoc", success = false, error = "createdRealDoc is null" });
                        }
                        else
                        {
                            var updateDescModel = new ImageDescriptions
                            {
                                Id = realDescId,
                                Description = "Batch6 Parity Created Desc 1 Moved",
                                ProjectId = -998,
                                CategoryId = (int)(firstCatId ?? 0),
                                VesselType = "PARITY-TEST",
                                IsActive = true,
                                IsDeleted = false
                            };
                            updateDescRes = await cosmosDescRepo.UpdateImageDescription(updateDescModel);

                            // Check old item at pk 0 is gone
                            var oldDescQ = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id").WithParameter("@id", realDescId);
                            var oldDescDocs = new List<ImageDescriptionDocument>();
                            using (var feed = descContainer.GetItemQueryIterator<ImageDescriptionDocument>(oldDescQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(0.0) }))
                            {
                                while (feed.HasMoreResults)
                                {
                                    var resp = await feed.ReadNextAsync();
                                    oldDescDocs.AddRange(resp);
                                }
                            }
                            oldGone = oldDescDocs.Count == 0;

                            // Check new item at pk -998 exists with CreatedDttm/UpdatedDttm approx now
                            var newDescDocs = new List<ImageDescriptionDocument>();
                            using (var feed = descContainer.GetItemQueryIterator<ImageDescriptionDocument>(oldDescQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(-998.0) }))
                            {
                                while (feed.HasMoreResults)
                                {
                                    var resp = await feed.ReadNextAsync();
                                    newDescDocs.AddRange(resp);
                                }
                            }
                            var movedDoc = newDescDocs.FirstOrDefault();
                            newExists = movedDoc != null;
                            datesApproxNow = movedDoc?.CreatedDttm.HasValue == true &&
                                                  movedDoc?.UpdatedDttm.HasValue == true &&
                                                  Math.Abs((movedDoc.CreatedDttm.Value - DateTime.Now).TotalMinutes) < 2 &&
                                                  Math.Abs((movedDoc.UpdatedDttm.Value - DateTime.Now).TotalMinutes) < 2;

                            writeSteps.Add(new { step = "UpdateImageDescription.PartitionMoveAndDates", updateDescRes, oldGone, newExists, datesApproxNow });
                        }

                        // 5. Card create -> read -> update with ProjectId change (-999 -> -998, check old partition empty and new partition found)
                        var testCard = new GenericImageCard
                        {
                            ProjectId = -999,
                            TemplateId = 1,
                            SectionId = Guid.Empty,
                            CardNumber = 999,
                            CardName = "Batch6 Parity Test Card",
                            DescriptionId = 1,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedDttm = DateTime.UtcNow,
                            UpdatedDttm = DateTime.UtcNow
                        };
                        createCardRes = await cosmosReportRepo.CreateGenericImageCard(testCard);
                        var readCard = await cosmosReportRepo.GetGenericImagCardByName(-999, 1, Guid.Empty, 999);
                        guidEmptyKept = readCard != null && readCard.SectionId == Guid.Empty;

                        if (readCard != null)
                        {
                            readCard.ProjectId = -998;
                            readCard.CardName = "Batch6 Parity Test Card Moved";
                            var updateCardRes = await cosmosReportRepo.UpdateGenericImageCard(readCard);

                            var oldCard = await cosmosReportRepo.GetGenericImagCardByName(-999, 1, Guid.Empty, 999);
                            var newCard = await cosmosReportRepo.GetGenericImagCardByName(-998, 1, Guid.Empty, 999);
                            cardMoved = updateCardRes && oldCard == null && newCard != null;
                        }
                        else
                        {
                            writeSteps.Add(new { step = "Card.ReadFailed", success = false, error = "readCard is null" });
                        }
                        writeSteps.Add(new { step = "Card.CreateReadMove", createCardRes, guidEmptyKept, cardMoved });

                        // Check all write test booleans
                        bool writePassed = vtpOk && rpOk && vnsOk && psmUnchanged && psmOk &&
                                           vtmPidMatchOk && vtmPidNoMatchOk && vtm0Ok &&
                                           createDescRes1 && vsdCreated && noSecondMapping &&
                                           (createdRealDoc != null) && updateDescRes && oldGone && newExists && datesApproxNow &&
                                           createCardRes && guidEmptyKept && cardMoved;
                        if (!writePassed)
                        {
                            hadException = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        writeSteps.Add(new { step = "WriteTestException", error = ex.Message });
                    }
                    finally
                    {
                        // Delete EVERY test row. Do NOT swallow cleanup errors: add each failure to writeSteps and set allPassed = false.
                        async Task TryCleanup(string stepName, Func<Task> action)
                        {
                            try
                            {
                                await action();
                            }
                            catch (Exception ex)
                            {
                                hadException = true;
                                writeSteps.Add(new { step = $"CleanupFailed.{stepName}", error = ex.Message });
                            }
                        }

                        await TryCleanup("VesselTypePartMapping", () => vtpContainer.DeleteItemAsync<VesselTypePartMappingDocument>("test-vtp--999", new PartitionKey(-999.0)));
                        await TryCleanup("VesselTypeNormalSectionMapping", () => vnsContainer.DeleteItemAsync<VesselTypeNormalSectionMappingDocument>("test-vns--999", new PartitionKey(-999.0)));
                        await TryCleanup("ReportPart", () => rpContainer.DeleteItemAsync<ReportPartDocument>("test-rp--999", new PartitionKey(-999.0)));
                        await TryCleanup("ProjectSectionMapping", () => psmContainer.DeleteItemAsync<ProjectSectionMappingDocument>("test-psm--999", new PartitionKey(-999.0)));
                        await TryCleanup("CAP_Projects", () => projContainer.DeleteItemAsync<ProjectDocument>("-999", new PartitionKey("-999")));
                        await TryCleanup("VesselTankMapping.Match", () => vtmContainer.DeleteItemAsync<VesselTankMappingDocument>("test-vtm-match", new PartitionKey(-999.0)));
                        await TryCleanup("VesselTankMapping.NoMatch", () => vtmContainer.DeleteItemAsync<VesselTankMappingDocument>("test-vtm-nomatch", new PartitionKey(-999.0)));
                        foreach (var vid in trackedVsdIds)
                        {
                            await TryCleanup("VesselTypeSectionDescriptionMapping", () => vsdContainer.DeleteItemAsync<VesselTypeSectionDescriptionMappingDocument>(vid, new PartitionKey(vid)));
                        }

                        // Clean up image descriptions
                        foreach (var p in new[] { -999, -998, 0 })
                        {
                            await TryCleanup($"ImageDescriptions.p{p}", async () =>
                            {
                                var q = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @pid AND c.VesselType = 'PARITY-TEST'").WithParameter("@pid", p);
                                using var feed = descContainer.GetItemQueryIterator<ImageDescriptionDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)p) });
                                while (feed.HasMoreResults)
                                {
                                    var resp = await feed.ReadNextAsync();
                                    foreach (var item in resp)
                                    {
                                        await descContainer.DeleteItemAsync<ImageDescriptionDocument>(item.id, new PartitionKey((double)p));
                                    }
                                }
                            });
                        }

                        // Clean up generic image cards
                        foreach (var p in new[] { -999, -998 })
                        {
                            await TryCleanup($"GenericImageCard.p{p}", async () =>
                            {
                                var q = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @pid").WithParameter("@pid", p);
                                using var feed = cardContainer.GetItemQueryIterator<GenericImageCardDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)p) });
                                while (feed.HasMoreResults)
                                {
                                    var resp = await feed.ReadNextAsync();
                                    foreach (var item in resp)
                                    {
                                        await cardContainer.DeleteItemAsync<GenericImageCardDocument>(item.id, new PartitionKey((double)p));
                                    }
                                }
                            });
                        }
                    }
                }

                allPassed = allPassed && !hadException && checks.All(c =>
                {
                    var prop = c.GetType().GetProperty("equal") ?? c.GetType().GetProperty("match") ?? c.GetType().GetProperty("allMatch");
                    if (prop != null)
                    {
                        var val = prop.GetValue(c);
                        if (val is bool b) return b;
                    }
                    var errProp = c.GetType().GetProperty("error");
                    if (errProp != null) return false;
                    return true;
                });

                return Results.Ok(new
                {
                    batch = 6,
                    allPassed,
                    checksCount = checks.Count,
                    checks,
                    writeSteps
                });
            }).AllowAnonymous();
        }

        private static void MapParityBatch7(WebApplication app)
        {
            app.MapGet("/health/parity/batch7", async (
                string? writeTest,
                IConfiguration config,
                IServiceProvider sp) =>
            {
                var db = sp.GetRequiredService<ApplicationDbContext>();
                var conn = db.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    await conn.OpenAsync();
                }

                var sqlGradingRepo = sp.GetRequiredService<GradingRepository>();
                var cosmosGradingRepo = sp.GetRequiredService<CosmosGradingRepository>();
                var sqlTankRepo = sp.GetRequiredService<TankRepository>();
                var cosmosTankRepo = sp.GetRequiredService<CosmosTankRepository>();

                var checks = new List<object>();
                var writeSteps = new List<object>();
                bool hadException = false;
                bool allPassed = true;

                // Load reference data from DB
                var vesselTypes = (await conn.QueryAsync<string>(
                    "SELECT DISTINCT VesselType FROM [dbo].[VesselType] WHERE VesselType IS NOT NULL ORDER BY VesselType")).ToList();
                var templates = (await conn.QueryAsync<(int Id, string Name)>(
                    "SELECT Id, TemplateName FROM [dbo].[ReportTemplate] WHERE TemplateName IS NOT NULL ORDER BY Id")).ToList();
                var projectIds = (await conn.QueryAsync<int>(
                    "SELECT DISTINCT TOP 25 ID FROM [CAP].[Projects] WHERE ID IS NOT NULL ORDER BY ID")).ToList();
                var imos = (await conn.QueryAsync<int>(
                    "SELECT DISTINCT TOP 25 IMO FROM [CAP].[Projects] WHERE IMO IS NOT NULL AND IMO > 0 ORDER BY IMO")).ToList();

                // C15: LeadSurveyor (up to 2) and SecondSurveyor (1 that only matches SecondSurveyor)
                var leadSurveyors = (await conn.QueryAsync<string>(
                    "SELECT DISTINCT TOP 2 LeadSurveyor FROM [CAP].[Projects] WHERE LeadSurveyor IS NOT NULL AND LeadSurveyor != '' ORDER BY LeadSurveyor")).ToList();
                var secondOnlySurveyors = (await conn.QueryAsync<string>(
                    @"SELECT DISTINCT TOP 1 SecondSurveyor FROM [CAP].[Projects] 
                      WHERE SecondSurveyor IS NOT NULL AND SecondSurveyor != '' 
                        AND SecondSurveyor NOT IN (SELECT LeadSurveyor FROM [CAP].[Projects] WHERE LeadSurveyor IS NOT NULL AND LeadSurveyor != '') 
                      ORDER BY SecondSurveyor")).ToList();

                var surveyorNamesToTest = new List<string?> { null };
                surveyorNamesToTest.AddRange(leadSurveyors);
                surveyorNamesToTest.AddRange(secondOnlySurveyors);
                if (surveyorNamesToTest.Count < 3)
                {
                    checks.Add(new { check = "Tank.GetTanks_Vessel real surveyor count check", info = $"Found {surveyorNamesToTest.Count - 1} real surveyor names in DB" });
                }

                // C8: First 25 projects with tanks + 1 missing
                var projectsWithTanks = (await conn.QueryAsync<(int ProjectId, string ImoNumber)>(
                    @"SELECT DISTINCT TOP 25 vtm.ProjectId, vtm.ImoNumber 
                      FROM [dbo].[Vessel_TankMapping] vtm 
                      WHERE vtm.ProjectId IS NOT NULL AND vtm.ImoNumber IS NOT NULL AND vtm.ImoNumber != ''")).ToList();

                // C9: Real tanks (top 10)
                var realTanks = (await conn.QueryAsync<(Guid Id, int? ProjectId)>(
                    "SELECT DISTINCT TOP 10 Id, ProjectId FROM [dbo].[Vessel_TankMapping] WHERE Id IS NOT NULL")).ToList();

                // C11: Tank types by vessel type
                var vtTankTypes = (await conn.QueryAsync<(string VesselType, int TankTypeId)>(
                    "SELECT DISTINCT VesselType, TankTypeId FROM [dbo].[Vessel_TankMapping] WHERE VesselType IS NOT NULL AND TankTypeId IS NOT NULL")).ToList();
                var vtTankTypeMap = vtTankTypes.GroupBy(x => x.VesselType, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.TankTypeId).Distinct().ToList(), StringComparer.OrdinalIgnoreCase);

                // C12: Real TankImageCards (top 10)
                var realCards = (await conn.QueryAsync<(int ProjectId, int TemplateId, Guid SectionId, int CardNumber)>(
                    "SELECT DISTINCT TOP 10 ProjectId, TemplateId, SectionId, CardNumber FROM [dbo].[TankImageCard] WHERE ProjectId IS NOT NULL AND SectionId IS NOT NULL AND CardNumber IS NOT NULL")).ToList();

                // C13: Real Vessel_GradingMapping IDs (top 50) + 1 missing
                var vgmIds = (await conn.QueryAsync<int>(
                    "SELECT DISTINCT TOP 50 Id FROM [dbo].[Vessel_GradingMapping] WHERE Id IS NOT NULL ORDER BY Id")).ToList();
                vgmIds.Add(99999999);

                // C14: Seeds for VT and IMO branches of GetTemplateTanks
                var vtBranchSeed = await conn.QueryFirstOrDefaultAsync<(int TemplateId, string VesselType)>(
                    "SELECT TOP 1 TemplateId, VesselType FROM [dbo].[Config_VesselTypeTankSectionMapping] WHERE TemplateId IS NOT NULL AND VesselType IS NOT NULL");
                var imoBranchSeed = await conn.QueryFirstOrDefaultAsync<(string ImoNumber, int ProjectId, int TemplateId)>(
                    @"SELECT TOP 1 vtm.ImoNumber, vtm.ProjectId, ISNULL(tt.TemplateId, 1) as TemplateId 
                      FROM [dbo].[Vessel_TankMapping] vtm 
                      INNER JOIN [dbo].[TankType] tt ON tt.Id = vtm.TankTypeId 
                      WHERE vtm.ImoNumber IS NOT NULL AND vtm.ImoNumber != '' AND vtm.ProjectId IS NOT NULL AND vtm.ProjectId > 0 AND vtm.IsDeleted = 0");

                // 1. Grading reads
                // 1a. GetAllGradingAsync (C16)
                try
                {
                    var sqlAllGrading = await sqlGradingRepo.GetAllGradingAsync();
                    var cosmosAllGrading = await cosmosGradingRepo.GetAllGradingAsync();
                    checks.Add(ParityCompare.Lists("Grading.GetAllGradingAsync",
                        sqlAllGrading, cosmosAllGrading,
                        null, true,
                        ("GradingId", x => x.GradingId),
                        ("GradingName", x => x.GradingName),
                        ("TanktypeId", x => x.TanktypeId),
                        ("TemplateId", x => x.TemplateId),
                        ("TemplateName", x => x.TemplateName),
                        ("SectionId", x => x.SectionId),
                        ("SectionName", x => x.SectionName),
                        ("VesselType", x => x.VesselType),
                        ("ProjectId", x => x.ProjectId),
                        ("IsActive", x => x.IsActive),
                        ("IsDeleted", x => x.IsDeleted),
                        ("RequiredInReport", x => x.RequiredInReport)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Grading.GetAllGradingAsync", error = ex.Message });
                }

                // 1b. GetGradingTemplates
                try
                {
                    var sqlTemplates = await sqlGradingRepo.GetGradingTemplates();
                    var cosmosTemplates = await cosmosGradingRepo.GetGradingTemplates();
                    checks.Add(ParityCompare.Lists("Grading.GetGradingTemplates",
                        sqlTemplates, cosmosTemplates,
                        x => x.TemplateId, false,
                        ("TemplateId", x => x.TemplateId),
                        ("TemplateName", x => x.TemplateName)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Grading.GetGradingTemplates", error = ex.Message });
                }

                // 1c. GetGradingTemplatesByVesselType (every vessel type)
                foreach (var vt in vesselTypes)
                {
                    try
                    {
                        var sqlVt = await sqlGradingRepo.GetGradingTemplatesByVesselType(vt);
                        var cosmosVt = await cosmosGradingRepo.GetGradingTemplatesByVesselType(vt);
                        checks.Add(ParityCompare.Lists($"Grading.GetGradingTemplatesByVesselType({vt})",
                            sqlVt, cosmosVt,
                            x => x.TemplateId, false,
                            ("TemplateId", x => x.TemplateId),
                            ("TemplateName", x => x.TemplateName)));
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = $"Grading.GetGradingTemplatesByVesselType({vt})", error = ex.Message });
                    }
                }

                // 1d. GetGradingSectionNamesByTemplateNameAndVesselType (C10, C17: every template & every vessel type, ordered)
                foreach (var tpl in templates)
                {
                    foreach (var vt in vesselTypes)
                    {
                        try
                        {
                            var sqlSec = await sqlGradingRepo.GetGradingSectionNamesByTemplateNameAndVesselType(tpl.Name, vt);
                            var cosmosSec = await cosmosGradingRepo.GetGradingSectionNamesByTemplateNameAndVesselType(tpl.Name, vt);
                            checks.Add(ParityCompare.Lists($"Grading.GetGradingSectionNames({tpl.Name}, {vt})",
                                sqlSec, cosmosSec,
                                null, true,
                                ("SectionId", x => x.SectionId),
                                ("SectionName", x => x.SectionName),
                                ("TanktypeId", x => x.TanktypeId),
                                ("TemplateId", x => x.TemplateId)));
                        }
                        catch (Exception ex)
                        {
                            hadException = true;
                            checks.Add(new { check = $"Grading.GetGradingSectionNames({tpl.Name}, {vt})", error = ex.Message });
                        }
                    }
                }

                // 1e. GetGradingSections (C10, C17: every template & every vessel type, ordered)
                foreach (var tpl in templates)
                {
                    foreach (var vt in vesselTypes)
                    {
                        try
                        {
                            var sqlSec = await sqlGradingRepo.GetGradingSections(tpl.Id, vt);
                            var cosmosSec = await cosmosGradingRepo.GetGradingSections(tpl.Id, vt);
                            checks.Add(ParityCompare.Lists($"Grading.GetGradingSections({tpl.Id}, {vt})",
                                sqlSec, cosmosSec,
                                null, true, // SQL orders by [Order]
                                ("SectionId", x => x.SectionId),
                                ("SectionName", x => x.SectionName),
                                ("TanktypeId", x => x.TanktypeId),
                                ("TemplateId", x => x.TemplateId)));
                        }
                        catch (Exception ex)
                        {
                            hadException = true;
                            checks.Add(new { check = $"Grading.GetGradingSections({tpl.Id}, {vt})", error = ex.Message });
                        }
                    }
                }

                // 1f. GetAllGrading_Pagination (C18: NotSupportedException check)
                try
                {
                    await cosmosGradingRepo.GetAllGrading_Pagination(1, 10);
                    checks.Add(new { check = "Grading.GetAllGrading_Pagination (NotSupportedException)", equal = false, error = "Expected NotSupportedException but no exception was thrown" });
                    allPassed = false;
                }
                catch (NotSupportedException)
                {
                    checks.Add(new { check = "Grading.GetAllGrading_Pagination (NotSupportedException)", equal = true });
                }
                catch (Exception ex)
                {
                    checks.Add(new { check = "Grading.GetAllGrading_Pagination (NotSupportedException)", equal = false, error = $"Expected NotSupportedException but got {ex.GetType().Name}: {ex.Message}" });
                    allPassed = false;
                }

                // 2. Tank reads
                // 2a. GetShipType (C17: unordered compare with Id key)
                try
                {
                    var sqlShip = await sqlTankRepo.GetShipType();
                    var cosmosShip = await cosmosTankRepo.GetShipType();
                    checks.Add(ParityCompare.Lists("Tank.GetShipType",
                        sqlShip, cosmosShip,
                        x => x.Id, false,
                        ("Id", x => x.Id),
                        ("VesselType", x => x.VesselType)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Tank.GetShipType", error = ex.Message });
                }

                // 2b. GetVesselType (C17: unordered compare with VesselType key)
                try
                {
                    var sqlVt = await sqlTankRepo.GetVesselType();
                    var cosmosVt = await cosmosTankRepo.GetVesselType();
                    checks.Add(ParityCompare.Lists("Tank.GetVesselType",
                        sqlVt, cosmosVt,
                        x => x.VesselType, false,
                        ("VesselType", x => x.VesselType)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Tank.GetVesselType", error = ex.Message });
                }

                // 2c. GetVesselIMONo (C16: IMO and VesselName)
                try
                {
                    var sqlImo = await sqlTankRepo.GetVesselIMONo();
                    var cosmosImo = await cosmosTankRepo.GetVesselIMONo();
                    checks.Add(ParityCompare.Lists("Tank.GetVesselIMONo",
                        sqlImo, cosmosImo,
                        null, true,
                        ("IMO", x => x.IMO),
                        ("VesselName", x => x.VesselName)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Tank.GetVesselIMONo", error = ex.Message });
                }

                // 2d. GetVesselTypeList
                try
                {
                    var sqlVtl = await sqlTankRepo.GetVesselTypeList();
                    var cosmosVtl = await cosmosTankRepo.GetVesselTypeList();
                    checks.Add(ParityCompare.Lists("Tank.GetVesselTypeList",
                        sqlVtl, cosmosVtl,
                        null, true,
                        ("ID", x => x.ID),
                        ("IMO", x => x.IMO),
                        ("Name", x => x.Name),
                        ("VesselType", x => x.VesselType),
                        ("CombinedDescription", x => x.CombinedDescription)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Tank.GetVesselTypeList", error = ex.Message });
                }

                // 2e. GetTanks_Vessel (C15, C16, C17: null + real LeadSurveyor and SecondSurveyor, ProjectName, ordered if name != null)
                foreach (var sName in surveyorNamesToTest)
                {
                    try
                    {
                        var sqlTv = await sqlTankRepo.GetTanks_Vessel(sName);
                        var cosmosTv = await cosmosTankRepo.GetTanks_Vessel(sName);
                        bool isOrdered = sName != null;
                        checks.Add(ParityCompare.Lists($"Tank.GetTanks_Vessel({sName ?? "null"})",
                            sqlTv, cosmosTv,
                            isOrdered ? null : x => (object)x.TankId, isOrdered,
                            ("TankId", x => x.TankId),
                            ("TankName", x => x.TankName),
                            ("VesselType", x => x.VesselType),
                            ("VesselName", x => x.VesselName),
                            ("IMONumber", x => x.IMONumber),
                            ("TankType", x => x.TankType),
                            ("TankTypeId", x => x.TankTypeId),
                            ("Status", x => x.Status),
                            ("IsActive", x => x.IsActive),
                            ("IsDeleted", x => x.IsDeleted),
                            ("IsDownload", x => x.IsDownload),
                            ("Subheader", x => x.Subheader),
                            ("ProjectId", x => x.ProjectId),
                            ("ProjectName", x => x.ProjectName)));
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = $"Tank.GetTanks_Vessel({sName ?? "null"})", error = ex.Message });
                    }
                }

                // 2f. GetTanks_VesselByIMO (C16: first 25 + 1 missing, full columns)
                var imosToTest = new List<int>(imos);
                imosToTest.Add(99999999);
                foreach (var imo in imosToTest)
                {
                    try
                    {
                        var sqlImo = await sqlTankRepo.GetTanks_VesselByIMO(imo.ToString());
                        var cosmosImo = await cosmosTankRepo.GetTanks_VesselByIMO(imo.ToString());
                        checks.Add(ParityCompare.Lists($"Tank.GetTanks_VesselByIMO({imo})",
                            sqlImo, cosmosImo,
                            x => x.Id, false,
                            ("Id", x => x.Id),
                            ("ImoNumber", x => x.ImoNumber),
                            ("VesselType", x => x.VesselType),
                            ("VesselName", x => x.VesselName),
                            ("TankTypeId", x => x.TankTypeId),
                            ("Subheader", x => x.Subheader),
                            ("TankName", x => x.TankName),
                            ("ProjectId", x => x.ProjectId),
                            ("IsDeleted", x => x.IsDeleted),
                            ("IsActive", x => x.IsActive),
                            ("RequiredInReport", x => x.RequiredInReport),
                            ("CreatedDttm", x => x.CreatedDttm),
                            ("UpdateDttm", x => x.UpdateDttm),
                            ("TemplateId", x => x.TemplateId)));
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = $"Tank.GetTanks_VesselByIMO({imo})", error = ex.Message });
                    }
                }

                // 2g. GetTanks_VesselByProject (C8, C16: real project IMOs + 1 missing, full columns)
                var testProjectsWithTanks = new List<(int ProjectId, string ImoNumber)>(projectsWithTanks);
                testProjectsWithTanks.Add((-999, "99999999"));
                foreach (var p in testProjectsWithTanks)
                {
                    try
                    {
                        var sqlProj = await sqlTankRepo.GetTanks_VesselByProject(p.ImoNumber, p.ProjectId);
                        var cosmosProj = await cosmosTankRepo.GetTanks_VesselByProject(p.ImoNumber, p.ProjectId);
                        checks.Add(ParityCompare.Lists($"Tank.GetTanks_VesselByProject({p.ImoNumber}, {p.ProjectId})",
                            sqlProj, cosmosProj,
                            null, true, // SQL orders by TankTypeId, TankName, CreatedDttm
                            ("Id", x => x.Id),
                            ("ImoNumber", x => x.ImoNumber),
                            ("VesselType", x => x.VesselType),
                            ("VesselName", x => x.VesselName),
                            ("TankTypeId", x => x.TankTypeId),
                            ("Subheader", x => x.Subheader),
                            ("TankName", x => x.TankName),
                            ("ProjectId", x => x.ProjectId),
                            ("IsDeleted", x => x.IsDeleted),
                            ("IsActive", x => x.IsActive),
                            ("RequiredInReport", x => x.RequiredInReport),
                            ("CreatedDttm", x => x.CreatedDttm),
                            ("UpdateDttm", x => x.UpdateDttm),
                            ("TemplateId", x => x.TemplateId)));
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = $"Tank.GetTanks_VesselByProject({p.ImoNumber}, {p.ProjectId})", error = ex.Message });
                    }
                }

                // 2h. GetTanks_VesselByVesselType (C16: every vessel type, full columns)
                foreach (var vt in vesselTypes)
                {
                    try
                    {
                        var sqlVt = await sqlTankRepo.GetTanks_VesselByVesselType(vt);
                        var cosmosVt = await cosmosTankRepo.GetTanks_VesselByVesselType(vt);
                        checks.Add(ParityCompare.Lists($"Tank.GetTanks_VesselByVesselType({vt})",
                            sqlVt, cosmosVt,
                            x => x.Id, false,
                            ("Id", x => x.Id),
                            ("ImoNumber", x => x.ImoNumber),
                            ("VesselType", x => x.VesselType),
                            ("VesselName", x => x.VesselName),
                            ("TankTypeId", x => x.TankTypeId),
                            ("Subheader", x => x.Subheader),
                            ("TankName", x => x.TankName),
                            ("ProjectId", x => x.ProjectId),
                            ("IsDeleted", x => x.IsDeleted),
                            ("IsActive", x => x.IsActive),
                            ("RequiredInReport", x => x.RequiredInReport),
                            ("CreatedDttm", x => x.CreatedDttm),
                            ("UpdateDttm", x => x.UpdateDttm),
                            ("TemplateId", x => x.TemplateId)));
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = $"Tank.GetTanks_VesselByVesselType({vt})", error = ex.Message });
                    }
                }

                // 2i. GetVessel_GradingByVesselType (C16: every vessel type, full columns)
                foreach (var vt in vesselTypes)
                {
                    try
                    {
                        var sqlVg = await sqlTankRepo.GetVessel_GradingByVesselType(vt);
                        var cosmosVg = await cosmosTankRepo.GetVessel_GradingByVesselType(vt);
                        checks.Add(ParityCompare.Lists($"Tank.GetVessel_GradingByVesselType({vt})",
                            sqlVg, cosmosVg,
                            x => x.Id, false,
                            ("Id", x => x.Id),
                            ("VesselType", x => x.VesselType),
                            ("VesselName", x => x.VesselName),
                            ("TankTypeId", x => x.TankTypeId),
                            ("GradingName", x => x.GradingName),
                            ("GradingGroupId", x => x.GradingGroupId),
                            ("GradingConditionGroupId", x => x.GradingConditionGroupId),
                            ("IsActive", x => x.IsActive),
                            ("ProjectId", x => x.ProjectId),
                            ("RequiredInReport", x => x.RequiredInReport),
                            ("CreatedDttm", x => x.CreatedDttm),
                            ("UpdateDttm", x => x.UpdateDttm),
                            ("TemplateId", x => x.TemplateId)));
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = $"Tank.GetVessel_GradingByVesselType({vt})", error = ex.Message });
                    }
                }

                // 2j. GetTankTypes (C10: both overloads, no Take limits, project 0 and first 25 projects)
                try
                {
                    var sqlTt = await sqlTankRepo.GetTankTypes();
                    var cosmosTt = await cosmosTankRepo.GetTankTypes();
                    checks.Add(ParityCompare.Lists("Tank.GetTankTypes()",
                        sqlTt, cosmosTt,
                        x => x.Id, false,
                        ("Id", x => x.Id),
                        ("TankType", x => x.TankType),
                        ("TankName", x => x.TankName),
                        ("TemplateId", x => x.TemplateId)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Tank.GetTankTypes()", error = ex.Message });
                }

                var pidsForTankTypes = new List<int> { 0 };
                pidsForTankTypes.AddRange(projectIds);
                foreach (var pid in pidsForTankTypes)
                {
                    foreach (var vt in vesselTypes)
                    {
                        try
                        {
                            var sqlTtOverload = await sqlTankRepo.GetTankTypes(pid, vt);
                            var cosmosTtOverload = await cosmosTankRepo.GetTankTypes(pid, vt);
                            checks.Add(ParityCompare.Lists($"Tank.GetTankTypes({pid}, {vt})",
                                sqlTtOverload, cosmosTtOverload,
                                x => x.Id, false,
                                ("Id", x => x.Id),
                                ("TankType", x => x.TankType),
                                ("TankName", x => x.TankName),
                                ("TemplateId", x => x.TemplateId)));
                        }
                        catch (Exception ex)
                        {
                            hadException = true;
                            checks.Add(new { check = $"Tank.GetTankTypes({pid}, {vt})", error = ex.Message });
                        }
                    }
                }

                // 2k. GetTemplateTanks (C14, C16: VT branch, IMO branch, no-branch; assert counts > 0)
                if (!string.IsNullOrEmpty(vtBranchSeed.VesselType))
                {
                    try
                    {
                        var sqlTtV = await sqlTankRepo.GetTemplateTanks(vtBranchSeed.TemplateId, null, vtBranchSeed.VesselType, 0);
                        var cosmosTtV = await cosmosTankRepo.GetTemplateTanks(vtBranchSeed.TemplateId, null, vtBranchSeed.VesselType, 0);
                        bool sqlCountOk = sqlTtV.Count > 0;
                        if (!sqlCountOk) allPassed = false;
                        checks.Add(new { check = "Tank.GetTemplateTanks(VT branch) count > 0", equal = sqlCountOk, sqlCount = sqlTtV.Count, cosmosCount = cosmosTtV.Count });
                        checks.Add(ParityCompare.Lists("Tank.GetTemplateTanks(VT branch)",
                            sqlTtV, cosmosTtV,
                            null, true,
                            ("OrderNumber", x => x.OrderNumber),
                            ("Id", x => x.Id),
                            ("TankName", x => x.TankName),
                            ("TankTypeId", x => x.TankTypeId),
                            ("Subheader", x => x.Subheader),
                            ("CurrentconditionPlaceholders", x => x.CurrentconditionPlaceholders)));
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = "Tank.GetTemplateTanks(VT branch)", error = ex.Message });
                    }
                }

                if (!string.IsNullOrEmpty(imoBranchSeed.ImoNumber))
                {
                    try
                    {
                        var sqlTtI = await sqlTankRepo.GetTemplateTanks(imoBranchSeed.TemplateId, imoBranchSeed.ImoNumber, "NON_EXISTING_VESSEL_TYPE", imoBranchSeed.ProjectId);
                        var cosmosTtI = await cosmosTankRepo.GetTemplateTanks(imoBranchSeed.TemplateId, imoBranchSeed.ImoNumber, "NON_EXISTING_VESSEL_TYPE", imoBranchSeed.ProjectId);
                        bool sqlCountOk = sqlTtI.Count > 0;
                        if (!sqlCountOk) allPassed = false;
                        checks.Add(new { check = "Tank.GetTemplateTanks(IMO branch) count > 0", equal = sqlCountOk, sqlCount = sqlTtI.Count, cosmosCount = cosmosTtI.Count });
                        checks.Add(ParityCompare.Lists("Tank.GetTemplateTanks(IMO branch)",
                            sqlTtI, cosmosTtI,
                            null, true,
                            ("OrderNumber", x => x.OrderNumber),
                            ("Id", x => x.Id),
                            ("TankName", x => x.TankName),
                            ("TankTypeId", x => x.TankTypeId),
                            ("Subheader", x => x.Subheader),
                            ("CurrentconditionPlaceholders", x => x.CurrentconditionPlaceholders)));
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = "Tank.GetTemplateTanks(IMO branch)", error = ex.Message });
                    }
                }

                try
                {
                    var sqlTtN = await sqlTankRepo.GetTemplateTanks(1, null, null, 0);
                    var cosmosTtN = await cosmosTankRepo.GetTemplateTanks(1, null, null, 0);
                    checks.Add(ParityCompare.Lists("Tank.GetTemplateTanks(No branch)",
                        sqlTtN, cosmosTtN,
                        null, true,
                        ("OrderNumber", x => x.OrderNumber)));
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { check = "Tank.GetTemplateTanks(No branch)", error = ex.Message });
                }

                // 2l. GetTemplateTankGrading (C11: every vessel type x (0 and all tank types) x (0 and first 25 projects))
                var pidsForGrading = new List<int> { 0 };
                pidsForGrading.AddRange(projectIds);
                foreach (var vt in vesselTypes)
                {
                    var ttIds = new HashSet<int> { 0 };
                    if (vtTankTypeMap.TryGetValue(vt, out var list))
                    {
                        foreach (var id in list) ttIds.Add(id);
                    }
                    foreach (var ttId in ttIds)
                    {
                        foreach (var pid in pidsForGrading)
                        {
                            try
                            {
                                var sqlTtg = await sqlTankRepo.GetTemplateTankGrading(ttId, pid, vt);
                                var cosmosTtg = await cosmosTankRepo.GetTemplateTankGrading(ttId, pid, vt);
                                checks.Add(ParityCompare.Lists($"Tank.GetTemplateTankGrading({ttId}, {pid}, {vt})",
                                    sqlTtg, cosmosTtg,
                                    null, true, // SQL orders by TankTypeId, Name
                                    ("Id", x => x.Id),
                                    ("Name", x => x.Name),
                                    ("Value", x => x.Value),
                                    ("GradingConditionGroupId", x => x.GradingConditionGroupId),
                                    ("RequiredInReport", x => x.RequiredInReport),
                                    ("TankTypeId", x => x.TankTypeId),
                                    ("SectionId", x => x.SectionId)));
                            }
                            catch (Exception ex)
                            {
                                hadException = true;
                                checks.Add(new { check = $"Tank.GetTemplateTankGrading({ttId}, {pid}, {vt})", error = ex.Message });
                            }
                        }
                    }
                }

                // 2m. GetTemplateTankGradingCondition (C13: real vgmIds + 99999999)
                foreach (var gId in vgmIds)
                {
                    try
                    {
                        var sqlCnd = await sqlTankRepo.GetTemplateTankGradingCondition(gId);
                        var cosmosCnd = await cosmosTankRepo.GetTemplateTankGradingCondition(gId);
                        checks.Add(ParityCompare.Lists($"Tank.GetTemplateTankGradingCondition({gId})",
                            sqlCnd, cosmosCnd,
                            x => x.Id, false,
                            ("Id", x => x.Id),
                            ("Name", x => x.Name),
                            ("Value", x => x.Value)));
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = $"Tank.GetTemplateTankGradingCondition({gId})", error = ex.Message });
                    }
                }

                // 2n & 2o. GetProjectTankImageCard and GetProjectTankImagCardByName (C12, C16: real cards + Guid.Empty + missing)
                var cardsToTest = new List<(int ProjectId, int TemplateId, Guid SectionId, int CardNumber)>(realCards);
                if (projectIds.Count > 0)
                {
                    cardsToTest.Add((projectIds[0], 1, Guid.Empty, 1));
                }
                cardsToTest.Add((-999, 9999, Guid.NewGuid(), 9999));

                foreach (var c in cardsToTest)
                {
                    try
                    {
                        var sqlCard = await sqlTankRepo.GetProjectTankImageCard(c.ProjectId, c.TemplateId, c.SectionId);
                        var cosmosCard = await cosmosTankRepo.GetProjectTankImageCard(c.ProjectId, c.TemplateId, c.SectionId);
                        checks.Add(ParityCompare.Lists($"Tank.GetProjectTankImageCard({c.ProjectId}, {c.TemplateId}, {c.SectionId})",
                            sqlCard, cosmosCard,
                            x => x.Id, false,
                            ("Id", x => x.Id),
                            ("ProjectId", x => x.ProjectId),
                            ("TemplateId", x => x.TemplateId),
                            ("SectionId", x => x.SectionId),
                            ("CardNumber", x => x.CardNumber),
                            ("CardName", x => x.CardName),
                            ("DescriptionId", x => x.DescriptionId),
                            ("AdditionalDescription", x => x.AdditionalDescription),
                            ("CurrentCondition", x => x.CurrentCondition),
                            ("ImageId", x => x.ImageId),
                            ("IsActive", x => x.IsActive),
                            ("CreatedDttm", x => x.CreatedDttm),
                            ("UpdatedDttm", x => x.UpdatedDttm),
                            ("src", x => x.src)));
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = $"Tank.GetProjectTankImageCard({c.ProjectId}, {c.TemplateId}, {c.SectionId})", error = ex.Message });
                    }

                    try
                    {
                        var sqlCardName = await sqlTankRepo.GetProjectTankImagCardByName(c.ProjectId, c.TemplateId, c.SectionId, c.CardNumber);
                        var cosmosCardName = await cosmosTankRepo.GetProjectTankImagCardByName(c.ProjectId, c.TemplateId, c.SectionId, c.CardNumber);
                        checks.Add(ParityCompare.Single($"Tank.GetProjectTankImagCardByName({c.ProjectId}, {c.TemplateId}, {c.SectionId}, {c.CardNumber})",
                            sqlCardName, cosmosCardName,
                            ("Id", x => x?.Id),
                            ("ProjectId", x => x?.ProjectId),
                            ("TemplateId", x => x?.TemplateId),
                            ("SectionId", x => x?.SectionId),
                            ("CardNumber", x => x?.CardNumber),
                            ("CardName", x => x?.CardName),
                            ("DescriptionId", x => x?.DescriptionId),
                            ("AdditionalDescription", x => x?.AdditionalDescription),
                            ("CurrentCondition", x => x?.CurrentCondition),
                            ("ImageId", x => x?.ImageId),
                            ("IsActive", x => x?.IsActive),
                            ("CreatedDttm", x => x?.CreatedDttm),
                            ("UpdatedDttm", x => x?.UpdatedDttm),
                            ("src", x => x?.src)));
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = $"Tank.GetProjectTankImagCardByName({c.ProjectId}, {c.TemplateId}, {c.SectionId}, {c.CardNumber})", error = ex.Message });
                    }
                }

                // 2p. GetTanks_VesselById (C9, C16: real tanks + missing with null, 0, and real project)
                var tanksToTest = new List<(Guid Id, int? ProjectId)>(realTanks);
                tanksToTest.Add((Guid.NewGuid(), (int?)0));

                foreach (var t in tanksToTest)
                {
                    var pids = new List<int?> { null, 0 };
                    if (t.ProjectId.HasValue && t.ProjectId.Value != 0)
                    {
                        pids.Add(t.ProjectId.Value);
                    }
                    foreach (var pid in pids)
                    {
                        try
                        {
                            var sqlTvId = await sqlTankRepo.GetTanks_VesselById(t.Id, pid);
                            var cosmosTvId = await cosmosTankRepo.GetTanks_VesselById(t.Id, pid);
                            checks.Add(ParityCompare.Single($"Tank.GetTanks_VesselById({t.Id}, {pid?.ToString() ?? "null"})",
                                sqlTvId, cosmosTvId,
                                ("Id", x => x?.Id),
                                ("VesselType", x => x?.VesselType),
                                ("ImoNumber", x => x?.ImoNumber),
                                ("TankTypeId", x => x?.TankTypeId),
                                ("TankName", x => x?.TankName),
                                ("IsActive", x => x?.IsActive),
                                ("CreatedDttm", x => x?.CreatedDttm),
                                ("UpdateDttm", x => x?.UpdateDttm),
                                ("IsDeleted", x => x?.IsDeleted),
                                ("Subheader", x => x?.Subheader),
                                ("RequiredInReport", x => x?.RequiredInReport),
                                ("ProjectId", x => x?.ProjectId)));
                        }
                        catch (Exception ex)
                        {
                            hadException = true;
                            checks.Add(new { check = $"Tank.GetTanks_VesselById({t.Id}, {pid?.ToString() ?? "null"})", error = ex.Message });
                        }
                    }
                }

                // 2q. GetProjectNames (real IMO, null, "abc")
                foreach (var imoStr in new[] { imos.FirstOrDefault().ToString(), null, "abc" })
                {
                    try
                    {
                        var sqlPn = await sqlTankRepo.GetProjectNames(imoStr);
                        var cosmosPn = await cosmosTankRepo.GetProjectNames(imoStr);
                        bool bothNull = sqlPn == null && cosmosPn == null;
                        if (sqlPn == null || cosmosPn == null)
                        {
                            checks.Add(new { check = $"Tank.GetProjectNames({imoStr ?? "null"}) null check", equal = bothNull, sqlNull = sqlPn == null, cosmosNull = cosmosPn == null });
                            if (!bothNull) allPassed = false;
                        }
                        if (sqlPn != null && cosmosPn != null)
                        {
                            checks.Add(ParityCompare.Lists($"Tank.GetProjectNames({imoStr ?? "null"})",
                                sqlPn, cosmosPn,
                                x => x.Value, false,
                                ("Value", x => x.Value),
                                ("Text", x => x.Text),
                                ("ProjectId", x => x.ProjectId)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { check = $"Tank.GetProjectNames({imoStr ?? "null"})", error = ex.Message });
                    }
                }

                // 2r. GetShipTypes (C18: NotImplementedException check)
                try
                {
                    await cosmosTankRepo.GetShipTypes();
                    checks.Add(new { check = "Tank.GetShipTypes (NotImplementedException)", equal = false, error = "Expected NotImplementedException but no exception was thrown" });
                    allPassed = false;
                }
                catch (NotImplementedException)
                {
                    checks.Add(new { check = "Tank.GetShipTypes (NotImplementedException)", equal = true });
                }
                catch (Exception ex)
                {
                    checks.Add(new { check = "Tank.GetShipTypes (NotImplementedException)", equal = false, error = $"Expected NotImplementedException but got {ex.GetType().Name}: {ex.Message}" });
                    allPassed = false;
                }

                // Write tests: Cosmos only, writeTest=yes, project -999, vessel type PARITY-TEST
                if (string.Equals(writeTest, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    var factory = sp.GetRequiredService<ICosmosContainerFactory>();
                    var gradingContainer = factory.GetContainer(CosmosContainers.dbo_Grading);
                    var vgmContainer = factory.GetContainer(CosmosContainers.dbo_Vessel_GradingMapping);
                    var vtmContainer = factory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
                    var cardContainer = factory.GetContainer(CosmosContainers.dbo_TankImageCard);
                    var vtsgmContainer = factory.GetContainer(CosmosContainers.Config_VesselTypeSectionGradingMapping);
                    var projectsContainer = factory.GetContainer(CosmosContainers.CAP_Projects);

                    // C25: Exact ID tracking collections - NO partition sweeps!
                    var trackedGrading = new List<(string id, double partitionKey)>();
                    var trackedVgm = new List<(string id, double partitionKey)>();
                    var trackedVtm = new List<(string id, double partitionKey)>();
                    var trackedCard = new List<(string id, double partitionKey)>();
                    var trackedVtsgm = new List<string>();
                    var trackedProjects = new List<(string id, string partitionKey)>();

                    var cleanupErrors = new List<string>();
                    async Task TryCleanup(string name, Func<Task> action)
                    {
                        try { await action(); }
                        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { }
                        catch (Exception ex) { cleanupErrors.Add($"{name}: {ex.Message}"); }
                    }

                    async Task<TDoc?> SafeReadAsync<TDoc>(Container container, string id, PartitionKey pk) where TDoc : class
                    {
                        try
                        {
                            var r = await container.ReadItemAsync<TDoc>(id, pk);
                            return r.Resource;
                        }
                        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            return null;
                        }
                    }

                    var testSecGuid = Guid.NewGuid();
                    var templateGradingId = 999901;

                    try
                    {
                        // 1. CreateProjectSectionGrading
                        // C4: CreateItemAsync only; on 409 fail step and skip template steps
                        var templateDoc = new GradingDocument
                        {
                            id = templateGradingId.ToString(),
                            Id = templateGradingId,
                            LabelName = "Parity Test Section",
                            GradingGroupId = 9991,
                            GradingConditionGroupId = 1,
                            IsActive = true,
                            IsDeleted = false,
                            VesselType = "PARITY-TEST",
                            ProjectId = 0,
                            RequiredInReport = true
                        };
                        bool templateCreated = false;
                        try
                        {
                            await gradingContainer.CreateItemAsync(templateDoc, new PartitionKey((double)0));
                            templateCreated = true;
                            trackedGrading.Add((templateDoc.id, 0));
                        }
                        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                        {
                            allPassed = false;
                            writeSteps.Add(new { step = "Grading.CreateItemAsync(template)", passed = false, error = "409 Conflict: id 999901 already exists in partition 0" });
                        }

                        // C19: verify copyCount == exact template row count and read back
                        if (templateCreated)
                        {
                            var copyCount = await cosmosGradingRepo.CreateProjectSectionGrading(-999, "PARITY-TEST");
                            bool copyOk = copyCount == 1;
                            writeSteps.Add(new { step = "Grading.CreateProjectSectionGrading", passed = copyOk, copyCount });
                            if (!copyOk) allPassed = false;

                            var qCopies = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = -999 AND c.VesselType = 'PARITY-TEST'");
                            using var feedCopies = gradingContainer.GetItemQueryIterator<GradingDocument>(qCopies, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)-999) });
                            int readBackCount = 0;
                            while (feedCopies.HasMoreResults)
                            {
                                var resp = await feedCopies.ReadNextAsync();
                                foreach (var doc in resp)
                                {
                                    readBackCount++;
                                    trackedGrading.Add((doc.id, -999));
                                    bool propsOk = doc.LabelName == templateDoc.LabelName 
                                        && doc.GradingGroupId == templateDoc.GradingGroupId 
                                        && doc.GradingConditionGroupId == templateDoc.GradingConditionGroupId 
                                        && doc.IsActive == true 
                                        && doc.IsDeleted != true 
                                        && doc.ProjectId == -999 
                                        && doc.RequiredInReport == true;
                                    writeSteps.Add(new { step = $"Grading.CreateProjectSectionGrading readback ({doc.id})", passed = propsOk });
                                    if (!propsOk) allPassed = false;
                                }
                            }
                            bool readCountOk = readBackCount == 1;
                            writeSteps.Add(new { step = "Grading.CreateProjectSectionGrading readBackCount == 1", passed = readCountOk, readBackCount });
                            if (!readCountOk) allPassed = false;
                        }
                        else
                        {
                            writeSteps.Add(new { step = "Grading.CreateProjectSectionGrading", passed = false, skipped = true });
                        }

                        // 2. Create_Vessel_Grading
                        // 2a. Valid (both repos) - C5 read back every column of valid case
                        var validVg = new VesselTankGrading
                        {
                            ProjectId = -999,
                            VesselType = "PARITY-TEST",
                            TankTypeId = 1,
                            GradingName = "Test VG",
                            GradingGroupId = "1",
                            GradingConditionGroupId = "1",
                            IsActive = true,
                            CreatedDttm = DateTime.UtcNow,
                            UpdateDttm = DateTime.UtcNow,
                            RequiredInReport = true
                        };
                        var createVgRes = await cosmosGradingRepo.CreateVessel_Grading(validVg);
                        writeSteps.Add(new { step = "Grading.CreateVessel_Grading(valid)", passed = createVgRes });
                        if (!createVgRes) allPassed = false;

                        var vgQ = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = -999 AND c.GradingName = 'Test VG'");
                        using var vgFeed = vgmContainer.GetItemQueryIterator<VesselGradingMappingDocument>(vgQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)-999) });
                        VesselGradingMappingDocument? vgDoc = null;
                        if (vgFeed.HasMoreResults)
                        {
                            var r = await vgFeed.ReadNextAsync();
                            vgDoc = r.FirstOrDefault();
                        }
                        bool vgDocOk = vgDoc != null 
                            && vgDoc.GradingName == "Test VG" 
                            && vgDoc.GradingGroupId == 1 
                            && vgDoc.GradingConditionGroupId == 1 
                            && vgDoc.TankTypeId == 1 
                            && vgDoc.VesselType == "PARITY-TEST" 
                            && vgDoc.IsActive == true 
                            && vgDoc.ProjectId == -999 
                            && vgDoc.RequiredInReport == true;
                        if (vgDoc != null) trackedVgm.Add((vgDoc.id, -999));
                        writeSteps.Add(new { step = "Grading.CreateVessel_Grading readback", passed = vgDocOk, docFound = vgDoc != null });
                        if (!vgDocOk) allPassed = false;

                        var validVgTank = new VesselTankGrading
                        {
                            ProjectId = -999,
                            VesselType = "PARITY-TEST",
                            TankTypeId = 2,
                            GradingName = "Test VG Tank",
                            GradingGroupId = "2",
                            GradingConditionGroupId = "1",
                            IsActive = true,
                            CreatedDttm = DateTime.UtcNow,
                            UpdateDttm = DateTime.UtcNow,
                            RequiredInReport = true
                        };
                        var tankCreateVgRes = await cosmosTankRepo.CreateVessel_Grading(validVgTank);
                        writeSteps.Add(new { step = "Tank.CreateVessel_Grading(valid)", passed = tankCreateVgRes });
                        if (!tankCreateVgRes) allPassed = false;

                        var vgTankQ = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = -999 AND c.GradingName = 'Test VG Tank'");
                        using var vgTankFeed = vgmContainer.GetItemQueryIterator<VesselGradingMappingDocument>(vgTankQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)-999) });
                        VesselGradingMappingDocument? vgTankDoc = null;
                        if (vgTankFeed.HasMoreResults)
                        {
                            var r = await vgTankFeed.ReadNextAsync();
                            vgTankDoc = r.FirstOrDefault();
                        }
                        bool vgTankDocOk = vgTankDoc != null 
                            && vgTankDoc.GradingName == "Test VG Tank" 
                            && vgTankDoc.GradingGroupId == 2 
                            && vgTankDoc.GradingConditionGroupId == 1 
                            && vgTankDoc.TankTypeId == 2 
                            && vgTankDoc.VesselType == "PARITY-TEST" 
                            && vgTankDoc.IsActive == true 
                            && vgTankDoc.ProjectId == -999 
                            && vgTankDoc.RequiredInReport == true;
                        if (vgTankDoc != null) trackedVgm.Add((vgTankDoc.id, -999));
                        writeSteps.Add(new { step = "Tank.CreateVessel_Grading readback", passed = vgTankDocOk, docFound = vgTankDoc != null });
                        if (!vgTankDocOk) allPassed = false;

                        // 2b. Invalid: non-number string for GradingGroupId -> false (with valid dates)
                        var invalidVgNum = new VesselTankGrading
                        {
                            ProjectId = -999,
                            VesselType = "PARITY-TEST",
                            GradingGroupId = "abc",
                            GradingConditionGroupId = "1",
                            CreatedDttm = DateTime.UtcNow,
                            UpdateDttm = DateTime.UtcNow
                        };
                        var createVgInvalidNum = await cosmosGradingRepo.CreateVessel_Grading(invalidVgNum);
                        bool invalidNumOk = !createVgInvalidNum;
                        writeSteps.Add(new { step = "Grading.CreateVessel_Grading(non-numeric string)", passed = invalidNumOk });
                        if (!invalidNumOk) allPassed = false;

                        var tankInvalidNum = await cosmosTankRepo.CreateVessel_Grading(invalidVgNum);
                        bool tankInvalidNumOk = !tankInvalidNum;
                        writeSteps.Add(new { step = "Tank.CreateVessel_Grading(non-numeric string)", passed = tankInvalidNumOk });
                        if (!tankInvalidNumOk) allPassed = false;

                        // 2c. Invalid: date < 1753 -> false
                        var invalidVgDate = new VesselTankGrading
                        {
                            ProjectId = -999,
                            VesselType = "PARITY-TEST",
                            GradingGroupId = "1",
                            GradingConditionGroupId = "1",
                            CreatedDttm = new DateTime(1600, 1, 1),
                            UpdateDttm = DateTime.UtcNow
                        };
                        var createVgInvalidDate = await cosmosGradingRepo.CreateVessel_Grading(invalidVgDate);
                        bool invalidDateOk = !createVgInvalidDate;
                        writeSteps.Add(new { step = "Grading.CreateVessel_Grading(date < 1753)", passed = invalidDateOk });
                        if (!invalidDateOk) allPassed = false;

                        var tankInvalidDate = await cosmosTankRepo.CreateVessel_Grading(invalidVgDate);
                        bool tankInvalidDateOk = !tankInvalidDate;
                        writeSteps.Add(new { step = "Tank.CreateVessel_Grading(date < 1753)", passed = tankInvalidDateOk });
                        if (!tankInvalidDateOk) allPassed = false;

                        // 2d. Invalid: default(DateTime) -> false
                        var defaultVgDate = new VesselTankGrading
                        {
                            ProjectId = -999,
                            VesselType = "PARITY-TEST",
                            GradingGroupId = "1",
                            GradingConditionGroupId = "1",
                            CreatedDttm = default,
                            UpdateDttm = DateTime.UtcNow
                        };
                        var createVgDefaultDate = await cosmosGradingRepo.CreateVessel_Grading(defaultVgDate);
                        bool defaultDateOk = !createVgDefaultDate;
                        writeSteps.Add(new { step = "Grading.CreateVessel_Grading(default date)", passed = defaultDateOk });
                        if (!defaultDateOk) allPassed = false;

                        var tankDefaultDate = await cosmosTankRepo.CreateVessel_Grading(defaultVgDate);
                        bool tankDefaultDateOk = !tankDefaultDate;
                        writeSteps.Add(new { step = "Tank.CreateVessel_Grading(default date)", passed = tankDefaultDateOk });
                        if (!tankDefaultDateOk) allPassed = false;

                        // 3. CreateTankGradingAsync (C20: check GradingGroupId, TankTypeId, VesselType, IsActive, RequiredInReport, ProjectId)
                        var tankGrading = new Grading
                        {
                            ProjectId = -999,
                            VesselType = "PARITY-TEST",
                            GradingName = "  Spaced Tank Grading  ",
                            TanktypeId = 1,
                            IsActive = true,
                            RequiredInReport = true
                        };
                        var createTgRes = await cosmosGradingRepo.CreateTankGradingAsync(tankGrading);
                        writeSteps.Add(new { step = "Grading.CreateTankGradingAsync", passed = createTgRes });
                        if (!createTgRes) allPassed = false;

                        var tgQ = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = -999 AND c.GradingName = 'Spaced Tank Grading'");
                        using var tgFeed = vgmContainer.GetItemQueryIterator<VesselGradingMappingDocument>(tgQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)-999) });
                        VesselGradingMappingDocument? tgDoc = null;
                        if (tgFeed.HasMoreResults)
                        {
                            var r = await tgFeed.ReadNextAsync();
                            tgDoc = r.FirstOrDefault();
                        }
                        bool tgDocOk = tgDoc != null 
                            && tgDoc.GradingName == "Spaced Tank Grading" 
                            && tgDoc.GradingGroupId != null 
                            && tgDoc.TankTypeId == 1 
                            && tgDoc.VesselType == "PARITY-TEST" 
                            && tgDoc.IsActive == true 
                            && tgDoc.RequiredInReport == true 
                            && tgDoc.ProjectId == -999 
                            && tgDoc.GradingConditionGroupId == 1 
                            && tgDoc.CreatedDttm == null 
                            && tgDoc.UpdateDttm == null 
                            && tgDoc.IsDeleted == false;
                        if (tgDoc != null) trackedVgm.Add((tgDoc.id, -999));
                        writeSteps.Add(new { step = "Grading.CreateTankGradingAsync readback", passed = tgDocOk, docFound = tgDoc != null });
                        if (!tgDocOk) allPassed = false;

                        // 4. UpdateTankGradingAsync (C21: ensure non-null UpdateDttm first, test partition move, test missing Id returns false)
                        if (tgDoc != null)
                        {
                            tgDoc.UpdateDttm = DateTime.UtcNow;
                            await vgmContainer.ReplaceItemAsync(tgDoc, tgDoc.id, new PartitionKey((double)-999));

                            var updateTg = new Grading
                            {
                                GradingId = tgDoc.Id,
                                ProjectId = -998,
                                GradingName = "  Updated Spaced  ",
                                TanktypeId = 1
                            };
                            var updateTgRes = await cosmosGradingRepo.UpdateTankGradingAsync(updateTg);
                            var oldTgQ = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id").WithParameter("@id", tgDoc.Id);
                            using var oldTgFeed = vgmContainer.GetItemQueryIterator<VesselGradingMappingDocument>(oldTgQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)-999) });
                            bool oldTgGone = true;
                            if (oldTgFeed.HasMoreResults)
                            {
                                var r = await oldTgFeed.ReadNextAsync();
                                oldTgGone = !r.Any();
                            }
                            using var newTgFeed = vgmContainer.GetItemQueryIterator<VesselGradingMappingDocument>(oldTgQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)-998) });
                            VesselGradingMappingDocument? newTgDoc = null;
                            if (newTgFeed.HasMoreResults)
                            {
                                var r = await newTgFeed.ReadNextAsync();
                                newTgDoc = r.FirstOrDefault();
                            }
                            if (newTgDoc != null) trackedVgm.Add((newTgDoc.id, -998));
                            bool newTgOk = updateTgRes && oldTgGone && newTgDoc != null && newTgDoc.GradingName == "  Updated Spaced  " && newTgDoc.UpdateDttm == null && newTgDoc.CreatedDttm == null;
                            writeSteps.Add(new { step = "Grading.UpdateTankGradingAsync partition move & UpdateDttm NULL", passed = newTgOk, oldTgGone, newExists = newTgDoc != null, updateDttmNull = newTgDoc?.UpdateDttm == null });
                            if (!newTgOk) allPassed = false;
                        }

                        // C21: Test updating missing Id returns false
                        var missingTg = new Grading
                        {
                            GradingId = 99999998,
                            ProjectId = -999,
                            GradingName = "Missing Grading",
                            TanktypeId = 1
                        };
                        var missingTgRes = await cosmosGradingRepo.UpdateTankGradingAsync(missingTg);
                        bool missingTgOk = !missingTgRes;
                        writeSteps.Add(new { step = "Grading.UpdateTankGradingAsync(missing Id -> false)", passed = missingTgOk });
                        if (!missingTgOk) allPassed = false;

                        // 5. CreateSectionGradingAsync (C22: call 1 creates mapping, call 2 reuses mapping)
                        var secGrading1 = new Grading
                        {
                            ProjectId = -999,
                            VesselType = "PARITY-TEST",
                            SectionId = testSecGuid,
                            GradingName = "Last Visit Date for Under Water Body Survey  "
                        };
                        var createSg1Res = await cosmosGradingRepo.CreateSectionGradingAsync(secGrading1);
                        var vtsgmQ = new QueryDefinition("SELECT * FROM c WHERE c.SectionId = @secId").WithParameter("@secId", testSecGuid.ToString());
                        using var vtsgmFeed = vtsgmContainer.GetItemQueryIterator<VesselTypeSectionGradingMappingDocument>(vtsgmQ);
                        var vtsgmDocs = new List<VesselTypeSectionGradingMappingDocument>();
                        while (vtsgmFeed.HasMoreResults)
                        {
                            var r = await vtsgmFeed.ReadNextAsync();
                            vtsgmDocs.AddRange(r);
                        }
                        var vtsgmDoc = vtsgmDocs.FirstOrDefault();
                        foreach (var d in vtsgmDocs) trackedVtsgm.Add(d.id);

                        var sg1Q = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = -999 AND c.LabelName = 'Last Visit Date for Under Water Body Survey'");
                        using var sg1Feed = gradingContainer.GetItemQueryIterator<GradingDocument>(sg1Q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)-999) });
                        GradingDocument? sg1Doc = null;
                        if (sg1Feed.HasMoreResults)
                        {
                            var r = await sg1Feed.ReadNextAsync();
                            sg1Doc = r.FirstOrDefault();
                        }
                        if (sg1Doc != null) trackedGrading.Add((sg1Doc.id, -999));

                        bool vtsgmCreated = vtsgmDocs.Count == 1
                            && vtsgmDoc != null 
                            && vtsgmDoc.id == $"{testSecGuid}|{vtsgmDoc.CategoryId}"
                            && vtsgmDoc.CreatedBy == "System" 
                            && vtsgmDoc.IsActive == true 
                            && vtsgmDoc.IsDeleted == false 
                            && vtsgmDoc.CreatedOn.HasValue && vtsgmDoc.CreatedOn.Value.Date == DateTime.UtcNow.Date;

                        bool sg1Ok = createSg1Res 
                            && vtsgmCreated 
                            && sg1Doc != null 
                            && sg1Doc.GradingConditionGroupId == null 
                            && sg1Doc.LabelName == "Last Visit Date for Under Water Body Survey" 
                            && sg1Doc.GradingGroupId == vtsgmDoc!.CategoryId;
                        writeSteps.Add(new { step = "Grading.CreateSectionGradingAsync(call 1 new mapping)", passed = sg1Ok, vtsgmCreated, docFound = sg1Doc != null });
                        if (!sg1Ok) allPassed = false;

                        // Call 2: same SectionId -> reuses mapping
                        var secGrading2 = new Grading
                        {
                            ProjectId = -999,
                            VesselType = "PARITY-TEST",
                            SectionId = testSecGuid,
                            GradingName = "Regular Section Grading  "
                        };
                        var createSg2Res = await cosmosGradingRepo.CreateSectionGradingAsync(secGrading2);

                        using var vtsgmFeed2 = vtsgmContainer.GetItemQueryIterator<VesselTypeSectionGradingMappingDocument>(vtsgmQ);
                        var vtsgmDocs2 = new List<VesselTypeSectionGradingMappingDocument>();
                        while (vtsgmFeed2.HasMoreResults)
                        {
                            var r = await vtsgmFeed2.ReadNextAsync();
                            vtsgmDocs2.AddRange(r);
                        }
                        var vtsgmDoc2 = vtsgmDocs2.FirstOrDefault();
                        foreach (var d in vtsgmDocs2) trackedVtsgm.Add(d.id);

                        bool mappingUnchanged = vtsgmDocs2.Count == 1
                            && vtsgmDoc2 != null 
                            && vtsgmDoc != null
                            && vtsgmDoc2.id == $"{testSecGuid}|{vtsgmDoc2.CategoryId}"
                            && vtsgmDoc2.CategoryId == vtsgmDoc.CategoryId 
                            && vtsgmDoc2.CreatedOn == vtsgmDoc.CreatedOn;

                        var sg2Q = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = -999 AND c.LabelName = 'Regular Section Grading'");
                        using var sg2Feed = gradingContainer.GetItemQueryIterator<GradingDocument>(sg2Q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)-999) });
                        GradingDocument? sg2Doc = null;
                        if (sg2Feed.HasMoreResults)
                        {
                            var r = await sg2Feed.ReadNextAsync();
                            sg2Doc = r.FirstOrDefault();
                        }
                        if (sg2Doc != null) trackedGrading.Add((sg2Doc.id, -999));

                        bool sg2Ok = createSg2Res 
                            && mappingUnchanged 
                            && sg2Doc != null 
                            && sg2Doc.GradingConditionGroupId == 1 
                            && sg2Doc.LabelName == "Regular Section Grading" 
                            && sg2Doc.GradingGroupId == vtsgmDoc.CategoryId;
                        writeSteps.Add(new { step = "Grading.CreateSectionGradingAsync(call 2 reuse mapping)", passed = sg2Ok, mappingUnchanged, docFound = sg2Doc != null });
                        if (!sg2Ok) allPassed = false;

                        // 6. UpdateSectionGradingAsync (including partition move to -998)
                        if (sg2Doc != null)
                        {
                            var updateSg = new Grading
                            {
                                GradingId = sg2Doc.Id,
                                ProjectId = -998,
                                GradingName = "  Updated Section Label  "
                            };
                            var updateSgRes = await cosmosGradingRepo.UpdateSectionGradingAsync(updateSg);
                            var oldSgQ = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id").WithParameter("@id", sg2Doc.Id);
                            using var oldSgFeed = gradingContainer.GetItemQueryIterator<GradingDocument>(oldSgQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)-999) });
                            bool oldSgGone = true;
                            if (oldSgFeed.HasMoreResults)
                            {
                                var r = await oldSgFeed.ReadNextAsync();
                                oldSgGone = !r.Any();
                            }
                            using var newSgFeed = gradingContainer.GetItemQueryIterator<GradingDocument>(oldSgQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)-998) });
                            GradingDocument? newSgDoc = null;
                            if (newSgFeed.HasMoreResults)
                            {
                                var r = await newSgFeed.ReadNextAsync();
                                newSgDoc = r.FirstOrDefault();
                            }
                            if (newSgDoc != null) trackedGrading.Add((newSgDoc.id, -998));
                            bool updateSgOk = updateSgRes && oldSgGone && newSgDoc != null && newSgDoc.LabelName == "  Updated Section Label  " && newSgDoc.GradingConditionGroupId == 1;
                            writeSteps.Add(new { step = "Grading.UpdateSectionGradingAsync partition move", passed = updateSgOk, oldSgGone, newExists = newSgDoc != null });
                            if (!updateSgOk) allPassed = false;
                        }

                        // 7. DeleteGradingAsync (C24: test wrong TankId returns false, nothing changed)
                        if (sg1Doc != null)
                        {
                            var delWrong1 = await cosmosGradingRepo.DeleteGradingAsync(sg1Doc.Id, 999);
                            var readSg1Unchanged = await SafeReadAsync<GradingDocument>(gradingContainer, sg1Doc.id, new PartitionKey((double)-999));
                            bool wrong1Ok = !delWrong1 && readSg1Unchanged != null && readSg1Unchanged.IsDeleted == false;
                            writeSteps.Add(new { step = "Grading.DeleteGradingAsync(wrong TankId != 0 -> false)", passed = wrong1Ok });
                            if (!wrong1Ok) allPassed = false;

                            var delRes0 = await cosmosGradingRepo.DeleteGradingAsync(sg1Doc.Id, 0);
                            var delDoc0 = await SafeReadAsync<GradingDocument>(gradingContainer, sg1Doc.id, new PartitionKey((double)-999));
                            bool del0Ok = delRes0 && delDoc0 != null && delDoc0.IsDeleted == true;
                            writeSteps.Add(new { step = "Grading.DeleteGradingAsync(tankId=0)", passed = del0Ok });
                            if (!del0Ok) allPassed = false;
                        }

                        if (tgDoc != null)
                        {
                            var delWrong2 = await cosmosGradingRepo.DeleteGradingAsync(tgDoc.Id, 0);
                            var readTgUnchanged = await SafeReadAsync<VesselGradingMappingDocument>(vgmContainer, tgDoc.id, new PartitionKey((double)-998));
                            bool wrong2Ok = !delWrong2 && readTgUnchanged != null && readTgUnchanged.IsDeleted == false;
                            writeSteps.Add(new { step = "Grading.DeleteGradingAsync(wrong TankId == 0 -> false)", passed = wrong2Ok });
                            if (!wrong2Ok) allPassed = false;

                            var delResT = await cosmosGradingRepo.DeleteGradingAsync(tgDoc.Id, 1);
                            var delDocT = await SafeReadAsync<VesselGradingMappingDocument>(vgmContainer, tgDoc.id, new PartitionKey((double)-998));
                            bool delTOk = delResT && delDocT != null && delDocT.IsDeleted == true;
                            writeSteps.Add(new { step = "Grading.DeleteGradingAsync(tankId!=0)", passed = delTOk });
                            if (!delTOk) allPassed = false;
                        }

                        // 8. Create_Vessel_TankMapping (C23: check every column, CreatedDttm/UpdateDttm close to now)
                        var tankGuid = Guid.NewGuid();
                        var vtDoc1 = new VesselTank
                        {
                            Id = tankGuid,
                            ProjectId = -999,
                            TankName = "Test Vessel Tank",
                            Subheader = "Sub 1",
                            ImoNumber = "9999999",
                            TankTypeId = 1,
                            VesselType = "PARITY-TEST",
                            IsActive = true,
                            RequiredInReport = true
                        };
                        var createVtRes1 = await cosmosTankRepo.CreateTank(vtDoc1);
                        var readVt1 = await SafeReadAsync<VesselTankMappingDocument>(vtmContainer, tankGuid.ToString(), new PartitionKey((double)-999));
                        if (readVt1 != null) trackedVtm.Add((readVt1.id, -999));

                        bool datesCloseToNow = readVt1 != null 
                            && readVt1.CreatedDttm.HasValue && Math.Abs((readVt1.CreatedDttm.Value - DateTime.UtcNow).TotalMinutes) < 2
                            && readVt1.UpdateDttm.HasValue && Math.Abs((readVt1.UpdateDttm.Value - DateTime.UtcNow).TotalMinutes) < 2;

                        bool createVt1Ok = createVtRes1 && readVt1 != null 
                            && readVt1.Id == tankGuid.ToString()
                            && readVt1.ProjectId == -999
                            && readVt1.TankName == "Test Vessel Tank"
                            && readVt1.Subheader == "Sub 1"
                            && readVt1.ImoNumber == "9999999"
                            && readVt1.TankTypeId == 1
                            && readVt1.VesselType == "PARITY-TEST"
                            && readVt1.IsActive == true
                            && readVt1.IsDeleted == false
                            && readVt1.RequiredInReport == true
                            && datesCloseToNow;
                        writeSteps.Add(new { step = "Tank.CreateTank(valid - all columns checked & dates near now)", passed = createVt1Ok, datesCloseToNow });
                        if (!createVt1Ok) allPassed = false;

                        // 8b. Duplicate Guid across partition -998 -> blocked; nothing appears in partition -998; -999 unchanged
                        var vtDocDup = new VesselTank
                        {
                            Id = tankGuid,
                            ProjectId = -998,
                            TankName = "Duplicate Tank",
                            ImoNumber = "9999999"
                        };
                        var createVtDupRes = await cosmosTankRepo.CreateTank(vtDocDup);
                        var read998 = await SafeReadAsync<VesselTankMappingDocument>(vtmContainer, tankGuid.ToString(), new PartitionKey((double)-998));
                        var read999Unchanged = await SafeReadAsync<VesselTankMappingDocument>(vtmContainer, tankGuid.ToString(), new PartitionKey((double)-999));
                        bool dupBlockedOk = !createVtDupRes && read998 == null && read999Unchanged != null && read999Unchanged.TankName == "Test Vessel Tank";
                        writeSteps.Add(new { step = "Tank.CreateTank(duplicate Guid cross-partition blocked & partition -998 empty)", passed = dupBlockedOk });
                        if (!dupBlockedOk) allPassed = false;

                        // 8c. C23: Run CreateTanks_VesselByProject("9999999", -999, -998, "9999998") on test data
                        var testProjDoc = new
                        {
                            id = "-999",
                            ID = -999,
                            VesselType = "PARITY-TEST",
                            IMO = 9999999,
                            Name = "Parity Test Project"
                        };
                        try
                        {
                            await projectsContainer.CreateItemAsync(testProjDoc, new PartitionKey("-999"));
                            trackedProjects.Add(("-999", "-999"));
                        }
                        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                        {
                        }

                        var copiedTanks = await cosmosTankRepo.CreateTanks_VesselByProject("9999999", -999, -998, "9999998");
                        bool copiedListEmpty = copiedTanks != null && copiedTanks.Count == 0;

                        var qCopiedVtm = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = -998 AND c.ImoNumber = '9999998'");
                        using var feedCopiedVtm = vtmContainer.GetItemQueryIterator<VesselTankMappingDocument>(qCopiedVtm, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)-998) });
                        int copiedVtmCount = 0;
                        while (feedCopiedVtm.HasMoreResults)
                        {
                            var resp = await feedCopiedVtm.ReadNextAsync();
                            foreach (var doc in resp)
                            {
                                copiedVtmCount++;
                                trackedVtm.Add((doc.id, -998));
                                bool docCopyOk = doc.VesselType == "PARITY-TEST" && doc.TankName == "Test Vessel Tank" && doc.ProjectId == -998;
                                writeSteps.Add(new { step = $"Tank.CreateTanks_VesselByProject readback ({doc.id})", passed = docCopyOk });
                                if (!docCopyOk) allPassed = false;
                            }
                        }
                        bool createByProjectOk = copiedListEmpty && copiedVtmCount >= 1;
                        writeSteps.Add(new { step = "Tank.CreateTanks_VesselByProject(empty list & rows created in -998)", passed = createByProjectOk, copiedListEmpty, copiedVtmCount });
                        if (!createByProjectOk) allPassed = false;

                        // 8d. C23: Run PopulateTemplate (both overloads) on test data
                        var templateTankGuid = Guid.NewGuid();
                        var templateTankDoc = new VesselTankMappingDocument
                        {
                            id = templateTankGuid.ToString(),
                            Id = templateTankGuid.ToString(),
                            ProjectId = 0,
                            VesselType = "PARITY-TEST",
                            TankName = "Template Tank Parity",
                            ImoNumber = "",
                            TankTypeId = 1,
                            IsActive = true,
                            IsDeleted = false
                        };
                        await vtmContainer.CreateItemAsync(templateTankDoc, new PartitionKey((double)0));
                        trackedVtm.Add((templateTankDoc.id, 0));

                        var pop1Res = await cosmosTankRepo.PopulateTemplate("9999998", "PARITY-TEST", "9999999");
                        var qPop1 = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = -998 AND c.TankName = 'Template Tank Parity'");
                        using var feedPop1 = vtmContainer.GetItemQueryIterator<VesselTankMappingDocument>(qPop1, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)-998) });
                        int pop1Count = 0;
                        while (feedPop1.HasMoreResults)
                        {
                            var resp = await feedPop1.ReadNextAsync();
                            foreach (var doc in resp)
                            {
                                pop1Count++;
                                trackedVtm.Add((doc.id, -998));
                            }
                        }
                        bool pop1Ok = pop1Res && pop1Count >= 1;
                        writeSteps.Add(new { step = "Tank.PopulateTemplate(3 params)", passed = pop1Ok, pop1Count });
                        if (!pop1Ok) allPassed = false;

                        var pop2Res = await cosmosTankRepo.PopulateTemplate("9999998", "PARITY-TEST", "9999999", -998, -999);
                        writeSteps.Add(new { step = "Tank.PopulateTemplate(4 params)", passed = pop2Res });
                        if (!pop2Res) allPassed = false;

                        var qPop2 = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = -998 AND c.TankName = 'Template Tank Parity'");
                        using var feedPop2 = vtmContainer.GetItemQueryIterator<VesselTankMappingDocument>(qPop2, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)-998) });
                        while (feedPop2.HasMoreResults)
                        {
                            var resp = await feedPop2.ReadNextAsync();
                            foreach (var doc in resp)
                            {
                                if (!trackedVtm.Any(t => t.id == doc.id && t.partitionKey == -998))
                                {
                                    trackedVtm.Add((doc.id, -998));
                                }
                            }
                        }

                        // 9. Update_Vessel_TankMapping (C24: check VesselType, TankTypeId, Subheader, UpdateDttm, IsActive, RequiredInReport, no doc in -998)
                        var updateVt = new VesselTank
                        {
                            Id = tankGuid,
                            ProjectId = -998,
                            TankName = "Updated Tank Name",
                            Subheader = "Sub 2",
                            ImoNumber = "9999999",
                            TankTypeId = 2,
                            VesselType = "PARITY-TEST-UPDATED",
                            IsActive = true,
                            RequiredInReport = true,
                            UpdateDttm = DateTime.UtcNow
                        };
                        var updateVtRes = await cosmosTankRepo.UpdateTank(updateVt);
                        var readVtUpdated = await SafeReadAsync<VesselTankMappingDocument>(vtmContainer, tankGuid.ToString(), new PartitionKey((double)-999));
                        var readVt998 = await SafeReadAsync<VesselTankMappingDocument>(vtmContainer, tankGuid.ToString(), new PartitionKey((double)-998));
                        bool updateDttmNearNow = readVtUpdated?.UpdateDttm != null && Math.Abs((readVtUpdated.UpdateDttm.Value - DateTime.UtcNow).TotalMinutes) < 2;

                        bool updateVtOk = updateVtRes && readVtUpdated != null
                            && readVtUpdated.ProjectId == -999 // ProjectId NOT in SET, unchanged!
                            && readVtUpdated.CreatedDttm == null // Overwritten with NULL (SQL bug kept)!
                            && readVtUpdated.TankName == "Updated Tank Name"
                            && readVtUpdated.VesselType == "PARITY-TEST-UPDATED"
                            && readVtUpdated.TankTypeId == 2
                            && readVtUpdated.Subheader == "Sub 2"
                            && updateDttmNearNow
                            && readVtUpdated.IsActive == true
                            && readVtUpdated.RequiredInReport == true
                            && readVt998 == null;
                        writeSteps.Add(new { step = "Tank.UpdateTank(all properties, CreatedDttm NULL, no doc in -998)", passed = updateVtOk });
                        if (!updateVtOk) allPassed = false;

                        // 10. CreateProjectTankImageCard
                        var card = new TankImageCard
                        {
                            ProjectId = -999,
                            SectionId = Guid.Empty,
                            CardName = "Parity Card 1",
                            CardNumber = 1,
                            DescriptionId = 1,
                            ImageId = 1
                        };
                        var createCardRes = await cosmosTankRepo.CreateProjectTankImageCard(card);
                        var cardQ = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = -999 AND c.CardName = 'Parity Card 1'");
                        using var cardFeed = cardContainer.GetItemQueryIterator<TankImageCardDocument>(cardQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)-999) });
                        TankImageCardDocument? cardDoc = null;
                        if (cardFeed.HasMoreResults)
                        {
                            var r = await cardFeed.ReadNextAsync();
                            cardDoc = r.FirstOrDefault();
                        }
                        if (cardDoc != null) trackedCard.Add((cardDoc.id, -999));
                        bool cardCreateOk = createCardRes && cardDoc != null && cardDoc.CreatedDttm == null && cardDoc.UpdatedDttm == null && cardDoc.SectionId == Guid.Empty.ToString();
                        writeSteps.Add(new { step = "Tank.CreateProjectTankImageCard", passed = cardCreateOk, docFound = cardDoc != null });
                        if (!cardCreateOk) allPassed = false;

                        // 11. UpdateProjectTankImageCard (C24: whole doc unchanged, compare _etag before and after)
                        if (cardDoc != null)
                        {
                            var beforeCardResponse = await cardContainer.ReadItemAsync<TankImageCardDocument>(cardDoc.id, new PartitionKey((double)-999));
                            var beforeEtag = beforeCardResponse.ETag;

                            var updateCard = new TankImageCard
                            {
                                Id = cardDoc.Id,
                                ProjectId = -999,
                                CardName = "Modified Card Name",
                                SectionId = Guid.Empty
                            };
                            var updateCardRes = await cosmosTankRepo.UpdateProjectTankImageCard(updateCard);
                            var afterCardResponse = await cardContainer.ReadItemAsync<TankImageCardDocument>(cardDoc.id, new PartitionKey((double)-999));
                            var afterEtag = afterCardResponse.ETag;

                            bool updateCardBugOk = !updateCardRes && beforeEtag == afterEtag && afterCardResponse.Resource.CardName == "Parity Card 1";
                            writeSteps.Add(new { step = "Tank.UpdateProjectTankImageCard(returns false, etag unchanged, doc unchanged)", passed = updateCardBugOk });
                            if (!updateCardBugOk) allPassed = false;
                        }

                        // 12. UpdateStatus (true, empty list, unknown Guid)
                        var updateStatusRes = await cosmosTankRepo.UpdateStatus(new List<Guid> { tankGuid }, true);
                        var readVtStatus = await SafeReadAsync<VesselTankMappingDocument>(vtmContainer, tankGuid.ToString(), new PartitionKey((double)-999));
                        bool statusOk = updateStatusRes && readVtStatus != null && readVtStatus.IsActive == true;
                        writeSteps.Add(new { step = "Tank.UpdateStatus(true)", passed = statusOk });
                        if (!statusOk) allPassed = false;

                        var emptyStatusRes = await cosmosTankRepo.UpdateStatus(new List<Guid>(), true);
                        bool emptyStatusOk = !emptyStatusRes;
                        writeSteps.Add(new { step = "Tank.UpdateStatus(empty list -> false)", passed = emptyStatusOk });
                        if (!emptyStatusOk) allPassed = false;

                        var unknownStatusRes = await cosmosTankRepo.UpdateStatus(new List<Guid> { Guid.NewGuid() }, true);
                        bool unknownStatusOk = !unknownStatusRes;
                        writeSteps.Add(new { step = "Tank.UpdateStatus(unknown Guid -> false)", passed = unknownStatusOk });
                        if (!unknownStatusOk) allPassed = false;
                    }
                    catch (Exception ex)
                    {
                        allPassed = false;
                        writeSteps.Add(new { step = "WriteException", passed = false, error = ex.ToString() });
                    }
                    finally
                    {
                        // C25: Cleanup EXACT tracked IDs only - NO partition sweeping!
                        foreach (var item in trackedGrading)
                        {
                            await TryCleanup($"Grading({item.id},{item.partitionKey})", async () =>
                            {
                                try
                                {
                                    await gradingContainer.DeleteItemAsync<GradingDocument>(item.id, new PartitionKey(item.partitionKey));
                                }
                                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { }
                            });
                        }

                        foreach (var item in trackedVgm)
                        {
                            await TryCleanup($"VesselGradingMapping({item.id},{item.partitionKey})", async () =>
                            {
                                try
                                {
                                    await vgmContainer.DeleteItemAsync<VesselGradingMappingDocument>(item.id, new PartitionKey(item.partitionKey));
                                }
                                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { }
                            });
                        }

                        foreach (var item in trackedVtm)
                        {
                            await TryCleanup($"VesselTankMapping({item.id},{item.partitionKey})", async () =>
                            {
                                try
                                {
                                    await vtmContainer.DeleteItemAsync<VesselTankMappingDocument>(item.id, new PartitionKey(item.partitionKey));
                                }
                                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { }
                            });
                        }

                        foreach (var item in trackedCard)
                        {
                            await TryCleanup($"TankImageCard({item.id},{item.partitionKey})", async () =>
                            {
                                try
                                {
                                    await cardContainer.DeleteItemAsync<TankImageCardDocument>(item.id, new PartitionKey(item.partitionKey));
                                }
                                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { }
                            });
                        }

                        foreach (var id in trackedVtsgm)
                        {
                            await TryCleanup($"VesselTypeSectionGradingMapping({id})", async () =>
                            {
                                try
                                {
                                    await vtsgmContainer.DeleteItemAsync<VesselTypeSectionGradingMappingDocument>(id, new PartitionKey(id));
                                }
                                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { }
                            });
                        }

                        foreach (var item in trackedProjects)
                        {
                            await TryCleanup($"CAP_Projects({item.id},{item.partitionKey})", async () =>
                            {
                                try
                                {
                                    await projectsContainer.DeleteItemAsync<object>(item.id, new PartitionKey(item.partitionKey));
                                }
                                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { }
                            });
                        }
                    }

                    if (cleanupErrors.Count > 0)
                    {
                        allPassed = false;
                        writeSteps.Add(new { step = "CleanupErrors", passed = false, errors = cleanupErrors });
                    }
                }

                allPassed = allPassed && !hadException && checks.All(c =>
                {
                    var prop = c.GetType().GetProperty("equal") ?? c.GetType().GetProperty("match") ?? c.GetType().GetProperty("allMatch");
                    if (prop != null)
                    {
                        var val = prop.GetValue(c);
                        if (val is bool b) return b;
                    }
                    var errProp = c.GetType().GetProperty("error");
                    if (errProp != null) return false;
                    return true;
                });

                return Results.Ok(new
                {
                    batch = 7,
                    allPassed,
                    checksCount = checks.Count,
                    checks,
                    writeSteps
                });
            }).AllowAnonymous();
        }

        private static void MapParityBatch8(WebApplication app)
        {
            app.MapGet("/health/parity/batch8", async (
                string? writeTest,
                IConfiguration config,
                IServiceProvider sp) =>
            {
                var db = sp.GetRequiredService<ApplicationDbContext>();
                var conn = db.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    await conn.OpenAsync();
                }

                var sqlReportPartRepo = sp.GetRequiredService<ReportPartRepository>();
                var cosmosReportPartRepo = sp.GetRequiredService<CosmosReportPartRepository>();
                var factory = sp.GetRequiredService<ICosmosContainerFactory>();

                var checks = new List<object>();
                var writeSteps = new List<object>();
                bool hadException = false;
                bool allPassed = true;

                // Load reference data from DB inside try so error fails run
                var vesselTypes = new List<int>();
                var projectIds = new List<int>();
                var partNames = new List<(int VesselTypeId, string PartName)>();
                var partIds = new List<(int VesselTypeId, int PartId)>();
                var projectParts = new List<(int ProjectId, int PartId)>();
                var sections = new List<Guid>();
                var gradingSectionPairs = new List<(int ProjectId, Guid SectionId)>();
                var cards = new List<(int ProjectId, int TemplateId, Guid SectionId)>();
                var tankSections = new List<(int ProjectId, int PartId, int ImoNumber)>();
                var tankTypePairs = new List<(int ProjectId, int TankTypeId)>();
                var exportSections = new List<(int PartId, int VesselTypeId)>();
                var subSectionPairs = new List<(int ProjectId, Guid SectionId)>();
                var showTrueInputs = new List<(int ProjectId, int PartId)>();

                var sqlNonEmptyCount = new Dictionary<string, int>();
                void RecordSqlNonEmpty(string key, bool hasItems)
                {
                    if (hasItems) sqlNonEmptyCount[key] = sqlNonEmptyCount.GetValueOrDefault(key, 0) + 1;
                }

                try
                {
                    vesselTypes = (await conn.QueryAsync<int>(
                        "SELECT DISTINCT Id FROM [dbo].[VesselType] WHERE Id IS NOT NULL ORDER BY Id")).ToList();
                    projectIds = (await conn.QueryAsync<int>(
                        "SELECT DISTINCT TOP 25 ID FROM [CAP].[Projects] WHERE ID IS NOT NULL AND IsDeleted = 0 ORDER BY ID")).ToList();
                    partNames = (await conn.QueryAsync<(int VesselTypeId, string PartName)>(
                        "SELECT DISTINCT VesselTypeId, PartName FROM [Config].[VesselTypePartMapping] WHERE VesselTypeId IS NOT NULL AND PartName IS NOT NULL AND IsDeleted = 0")).ToList();
                    partIds = (await conn.QueryAsync<(int VesselTypeId, int PartId)>(
                        "SELECT DISTINCT VesselTypeId, VesselTypePartMappingId as PartId FROM [Config].[VesselTypePartMapping] WHERE VesselTypeId IS NOT NULL AND IsDeleted = 0")).ToList();
                    projectParts = (await conn.QueryAsync<(int ProjectId, int PartId)>(
                        "SELECT DISTINCT ProjectId, PartId FROM [Config].[ProjectSectionMapping] WHERE ProjectId IS NOT NULL AND PartId IS NOT NULL AND IsDeleted = 0")).ToList();
                    sections = (await conn.QueryAsync<Guid>(
                        "SELECT DISTINCT SectionMappingId FROM [Config].[ProjectSectionMapping] WHERE SectionMappingId IS NOT NULL AND IsDeleted = 0")).ToList();
                    gradingSectionPairs = (await conn.QueryAsync<(int ProjectId, Guid SectionId)>(
                        "SELECT DISTINCT ProjectId, SectionMappingId FROM [Config].[ProjectSectionMapping] WHERE ProjectId IS NOT NULL AND SectionMappingId IS NOT NULL AND IsDeleted = 0")).ToList();
                    cards = (await conn.QueryAsync<(int ProjectId, int TemplateId, Guid SectionId)>(
                        "SELECT DISTINCT ProjectId, TemplateId, SectionId FROM [dbo].[GenericImageCard] WHERE ProjectId IS NOT NULL AND TemplateId IS NOT NULL AND SectionId IS NOT NULL AND IsDeleted = 0")).ToList();
                    tankSections = (await conn.QueryAsync<(int ProjectId, int PartId, int ImoNumber)>(
                        @"SELECT DISTINCT p.ID as ProjectId, psm.PartId, ISNULL(p.IMO, 0) as ImoNumber 
                          FROM [Config].[ProjectTankSectionMapping] psm 
                          INNER JOIN [CAP].[Projects] p ON p.ID = psm.ProjectId 
                          WHERE psm.ProjectId IS NOT NULL AND psm.IsDeleted = 0")).ToList();
                    tankTypePairs = (await conn.QueryAsync<(int ProjectId, int TankTypeId)>(
                        "SELECT DISTINCT ProjectId, TankTypeId FROM [Config].[ProjectTankSectionMapping] WHERE ProjectId IS NOT NULL AND TankTypeId IS NOT NULL AND IsDeleted = 0")).ToList();
                    exportSections = (await conn.QueryAsync<(int PartId, int VesselTypeId)>(
                        "SELECT DISTINCT VesselTypePartMappingId as PartId, VesselTypeId FROM [Config].[VesselTypePartMapping] WHERE VesselTypeId IS NOT NULL AND IsDeleted = 0")).ToList();
                    subSectionPairs = (await conn.QueryAsync<(int ProjectId, Guid SectionId)>(
                        @"SELECT DISTINCT psm.ProjectId, pss.Id as SectionId 
                          FROM [Config].[ProjectSubSection] pss 
                          INNER JOIN [Config].[ProjectSectionMapping] psm ON psm.SectionMappingId = pss.ProjectSectionId 
                          WHERE psm.ProjectId IS NOT NULL AND pss.Id IS NOT NULL AND pss.IsDeleted = 0")).ToList();
                    showTrueInputs = (await conn.QueryAsync<(int ProjectId, int PartId)>(
                        "SELECT TOP 5 ProjectId, PartId FROM [Config].[ProjectSectionMapping] WHERE RequiredInReport = 0 AND ProjectId IS NOT NULL AND PartId IS NOT NULL AND IsDeleted = 0")).ToList();
                }
                catch (Exception ex)
                {
                    hadException = true;
                    allPassed = false;
                    checks.Add(new { check = "SetupReferenceData", error = ex.Message });
                }

                // 1. GetVesselTypes
                try
                {
                    var sqlVt = await sqlReportPartRepo.GetVesselTypes();
                    var cosmosVt = await cosmosReportPartRepo.GetVesselTypes();
                    RecordSqlNonEmpty("GetVesselTypes", sqlVt?.vesselTypes != null && sqlVt.vesselTypes.Count > 0);
                    if (sqlVt?.vesselTypes == null || cosmosVt?.vesselTypes == null)
                    {
                        bool bothNull = sqlVt?.vesselTypes == null && cosmosVt?.vesselTypes == null;
                        checks.Add(new { check = "ReportPart.GetVesselTypes", sqlNull = sqlVt?.vesselTypes == null, cosmosNull = cosmosVt?.vesselTypes == null, equal = false });
                        allPassed = false;
                    }
                    else
                    {
                        checks.Add(ParityCompare.Lists("ReportPart.GetVesselTypes",
                            sqlVt.vesselTypes, cosmosVt.vesselTypes,
                            x => x.VesselTypeId, false,
                            ("VesselTypeId", x => x.VesselTypeId),
                            ("VesselType", x => x.VesselType)));
                    }
                }
                catch (Exception ex)
                {
                    hadException = true;
                    allPassed = false;
                    checks.Add(new { check = "ReportPart.GetVesselTypes", error = ex.Message });
                }

                // 2. GetReportPartsByVesselTypeAsync
                var testVesselTypes = new List<int>(vesselTypes);
                testVesselTypes.Add(-99999);
                foreach (var vt in testVesselTypes)
                {
                    bool isMissingId = vt == -99999;
                    try
                    {
                        var sqlParts = await sqlReportPartRepo.GetReportPartsByVesselTypeAsync(vt);
                        var cosmosParts = await cosmosReportPartRepo.GetReportPartsByVesselTypeAsync(vt);
                        RecordSqlNonEmpty("GetReportPartsByVesselTypeAsync", sqlParts?.reportTemplates != null && sqlParts.reportTemplates.Count > 0);
                        if (sqlParts?.reportTemplates == null || cosmosParts?.reportTemplates == null)
                        {
                            bool bothNull = sqlParts?.reportTemplates == null && cosmosParts?.reportTemplates == null;
                            checks.Add(new { check = $"ReportPart.GetReportPartsByVesselTypeAsync({vt})", sqlNull = sqlParts?.reportTemplates == null, cosmosNull = cosmosParts?.reportTemplates == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetReportPartsByVesselTypeAsync({vt})",
                                sqlParts.reportTemplates, cosmosParts.reportTemplates,
                                null, true,
                                ("PartId", x => x.PartId),
                                ("PartName", x => x.PartName),
                                ("SequenceNo", x => x.SequenceNo),
                                ("VesselTypeId", x => x.VesselTypeId),
                                ("IsActive", x => x.IsActive),
                                ("IsEditable", x => x.IsEditable)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetReportPartsByVesselTypeAsync({vt})", error = ex.Message });
                    }
                }

                // 3. GetSectionNamesByPartNameAsync
                var testPartNames = new List<(int VesselTypeId, string PartName)>(partNames);
                testPartNames.Add((-99999, "NoSuchPart"));
                foreach (var (vt, pn) in testPartNames)
                {
                    bool isMissingId = vt == -99999 || pn == "NoSuchPart";
                    try
                    {
                        var sqlSec = await sqlReportPartRepo.GetSectionNamesByPartNameAsync(vt, pn);
                        var cosmosSec = await cosmosReportPartRepo.GetSectionNamesByPartNameAsync(vt, pn);
                        RecordSqlNonEmpty("GetSectionNamesByPartNameAsync.Normal", sqlSec?.NormalSections != null && sqlSec.NormalSections.Count > 0);
                        RecordSqlNonEmpty("GetSectionNamesByPartNameAsync.Tanks", sqlSec?.TanksSections != null && sqlSec.TanksSections.Count > 0);
                        if (sqlSec?.NormalSections == null || cosmosSec?.NormalSections == null)
                        {
                            bool bothNull = sqlSec?.NormalSections == null && cosmosSec?.NormalSections == null;
                            checks.Add(new { check = $"ReportPart.GetSectionNamesByPartNameAsync.Normal({vt}, {pn})", sqlNull = sqlSec?.NormalSections == null, cosmosNull = cosmosSec?.NormalSections == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetSectionNamesByPartNameAsync.Normal({vt}, {pn})",
                                sqlSec.NormalSections, cosmosSec.NormalSections,
                                x => x.SectionId, false,
                                ("SectionId", x => x.SectionId),
                                ("SectionName", x => x.SectionName),
                                ("SubHeader", x => x.SubHeader),
                                ("TotalCards", x => x.TotalCards),
                                ("PlaceholderCount", x => x.PlaceholderCount),
                                ("FileNameCount", x => x.FileNameCount),
                                ("IsActive", x => x.IsActive),
                                ("PartName", x => x.PartName),
                                ("PartId", x => x.PartId),
                                ("VesselTypeId", x => x.VesselTypeId)));
                        }

                        if (sqlSec?.TanksSections == null || cosmosSec?.TanksSections == null)
                        {
                            bool bothNull = sqlSec?.TanksSections == null && cosmosSec?.TanksSections == null;
                            checks.Add(new { check = $"ReportPart.GetSectionNamesByPartNameAsync.Tanks({vt}, {pn})", sqlNull = sqlSec?.TanksSections == null, cosmosNull = cosmosSec?.TanksSections == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetSectionNamesByPartNameAsync.Tanks({vt}, {pn})",
                                sqlSec.TanksSections, cosmosSec.TanksSections,
                                x => x.TankTypeId, false,
                                ("TankTypeId", x => x.TankTypeId),
                                ("TankType", x => x.TankType),
                                ("TotalCards", x => x.TotalCards),
                                ("PlaceholderCount", x => x.PlaceholderCount),
                                ("FileNameCount", x => x.FileNameCount),
                                ("IsActive", x => x.IsActive),
                                ("IsMapped", x => x.IsMapped),
                                ("PartName", x => x.PartName),
                                ("PartId", x => x.PartId),
                                ("VesselTypeId", x => x.VesselTypeId)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetSectionNamesByPartNameAsync({vt}, {pn})", error = ex.Message });
                    }
                }

                // 4. GetSectionNamesByPartId
                var testPartIds = new List<(int VesselTypeId, int PartId)>(partIds);
                testPartIds.Add((-99999, -99999));
                foreach (var (vt, pid) in testPartIds)
                {
                    bool isMissingId = vt == -99999 || pid == -99999;
                    try
                    {
                        var sqlSec = await sqlReportPartRepo.GetSectionNamesByPartId(vt, pid);
                        var cosmosSec = await cosmosReportPartRepo.GetSectionNamesByPartId(vt, pid);
                        RecordSqlNonEmpty("GetSectionNamesByPartId.Normal", sqlSec?.NormalSections != null && sqlSec.NormalSections.Count > 0);
                        RecordSqlNonEmpty("GetSectionNamesByPartId.Sub", sqlSec?.SubSections != null && sqlSec.SubSections.Count > 0);
                        RecordSqlNonEmpty("GetSectionNamesByPartId.Tanks", sqlSec?.TanksSections != null && sqlSec.TanksSections.Count > 0);
                        if (sqlSec?.NormalSections == null || cosmosSec?.NormalSections == null)
                        {
                            bool bothNull = sqlSec?.NormalSections == null && cosmosSec?.NormalSections == null;
                            checks.Add(new { check = $"ReportPart.GetSectionNamesByPartId.Normal({vt}, {pid})", sqlNull = sqlSec?.NormalSections == null, cosmosNull = cosmosSec?.NormalSections == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetSectionNamesByPartId.Normal({vt}, {pid})",
                                sqlSec.NormalSections, cosmosSec.NormalSections,
                                null, true,
                                ("SectionId", x => x.SectionId),
                                ("SectionName", x => x.SectionName),
                                ("SubHeader", x => x.SubHeader),
                                ("TotalCards", x => x.TotalCards),
                                ("PlaceholderCount", x => x.PlaceholderCount),
                                ("FileNameCount", x => x.FileNameCount),
                                ("IsActive", x => x.IsActive),
                                ("PartName", x => x.PartName),
                                ("PartId", x => x.PartId),
                                ("VesselTypeId", x => x.VesselTypeId)));
                        }

                        if (sqlSec?.SubSections == null || cosmosSec?.SubSections == null)
                        {
                            bool bothNull = sqlSec?.SubSections == null && cosmosSec?.SubSections == null;
                            checks.Add(new { check = $"ReportPart.GetSectionNamesByPartId.Sub({vt}, {pid})", sqlNull = sqlSec?.SubSections == null, cosmosNull = cosmosSec?.SubSections == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetSectionNamesByPartId.Sub({vt}, {pid})",
                                sqlSec.SubSections, cosmosSec.SubSections,
                                null, true,
                                ("SubSectionId", x => x.SubSectionId),
                                ("SectionId", x => x.SectionId),
                                ("SectionName", x => x.SectionName),
                                ("TotalCards", x => x.TotalCards),
                                ("PlaceholderCount", x => x.PlaceholderCount),
                                ("FileNameCount", x => x.FileNameCount),
                                ("IsActive", x => x.IsActive),
                                ("PartName", x => x.PartName),
                                ("PartId", x => x.PartId),
                                ("VesselTypeId", x => x.VesselTypeId)));
                        }

                        if (sqlSec?.TanksSections == null || cosmosSec?.TanksSections == null)
                        {
                            bool bothNull = sqlSec?.TanksSections == null && cosmosSec?.TanksSections == null;
                            checks.Add(new { check = $"ReportPart.GetSectionNamesByPartId.Tanks({vt}, {pid})", sqlNull = sqlSec?.TanksSections == null, cosmosNull = cosmosSec?.TanksSections == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetSectionNamesByPartId.Tanks({vt}, {pid})",
                                sqlSec.TanksSections, cosmosSec.TanksSections,
                                x => x.TankTypeId, false,
                                ("TankTypeId", x => x.TankTypeId),
                                ("TankType", x => x.TankType),
                                ("TotalCards", x => x.TotalCards),
                                ("PlaceholderCount", x => x.PlaceholderCount),
                                ("FileNameCount", x => x.FileNameCount),
                                ("IsActive", x => x.IsActive),
                                ("IsMapped", x => x.IsMapped),
                                ("PartName", x => x.PartName),
                                ("PartId", x => x.PartId),
                                ("VesselTypeId", x => x.VesselTypeId)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetSectionNamesByPartId({vt}, {pid})", error = ex.Message });
                    }
                }

                // 5. GetGradingCondition
                try
                {
                    var sqlGc = await sqlReportPartRepo.GetGradingCondition();
                    var cosmosGc = await cosmosReportPartRepo.GetGradingCondition();
                    RecordSqlNonEmpty("GetGradingCondition", sqlGc != null && sqlGc.Count > 0);
                    if (sqlGc == null || cosmosGc == null)
                    {
                        checks.Add(new { check = "ReportPart.GetGradingCondition", sqlNull = sqlGc == null, cosmosNull = cosmosGc == null, equal = false });
                        allPassed = false;
                    }
                    else
                    {
                        checks.Add(ParityCompare.Lists("ReportPart.GetGradingCondition",
                            sqlGc, cosmosGc,
                            x => x.Id, false,
                            ("Id", x => x.Id),
                            ("Name", x => x.Name)));
                    }
                }
                catch (Exception ex)
                {
                    hadException = true;
                    allPassed = false;
                    checks.Add(new { check = "ReportPart.GetGradingCondition", error = ex.Message });
                }

                // 6. GetNormalSectionByProjectId
                var testProjectParts = new List<(int ProjectId, int PartId)>(projectParts);
                testProjectParts.Add((-99999, -99999));
                foreach (var (projId, partId) in testProjectParts)
                {
                    bool isMissingId = projId == -99999;
                    try
                    {
                        var sqlNs = await sqlReportPartRepo.GetNormalSectionByProjectId(projId, partId);
                        var cosmosNs = await cosmosReportPartRepo.GetNormalSectionByProjectId(projId, partId);
                        RecordSqlNonEmpty("GetNormalSectionByProjectId", sqlNs != null && sqlNs.Count > 0);
                        if (sqlNs == null || cosmosNs == null)
                        {
                            bool bothNull = sqlNs == null && cosmosNs == null;
                            checks.Add(new { check = $"ReportPart.GetNormalSectionByProjectId({projId}, {partId})", sqlNull = sqlNs == null, cosmosNull = cosmosNs == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetNormalSectionByProjectId({projId}, {partId})",
                                sqlNs, cosmosNs,
                                null, true,
                                ("SectionMappingId", x => x.SectionMappingId),
                                ("SectionName", x => x.SectionName),
                                ("SubHeader", x => x.SubHeader),
                                ("Order", x => x.Order),
                                ("HasSubsection", x => x.HasSubsection),
                                ("Totalcards", x => x.Totalcards),
                                ("PlaceholderCount", x => x.PlaceholderCount),
                                ("FileNameCount", x => x.FileNameCount)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetNormalSectionByProjectId({projId}, {partId})", error = ex.Message });
                    }
                }

                // 7. GetProjectSubsectionBySectionId
                var testSections = new List<Guid>(sections);
                testSections.Add(Guid.NewGuid());
                foreach (var secId in testSections)
                {
                    bool isMissingId = !sections.Contains(secId);
                    try
                    {
                        var sqlSub = await sqlReportPartRepo.GetProjectSubsectionBySectionId(secId);
                        var cosmosSub = await cosmosReportPartRepo.GetProjectSubsectionBySectionId(secId);
                        RecordSqlNonEmpty("GetProjectSubsectionBySectionId", sqlSub != null && sqlSub.Count > 0);
                        if (sqlSub == null || cosmosSub == null)
                        {
                            bool bothNull = sqlSub == null && cosmosSub == null;
                            checks.Add(new { check = $"ReportPart.GetProjectSubsectionBySectionId({secId})", sqlNull = sqlSub == null, cosmosNull = cosmosSub == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetProjectSubsectionBySectionId({secId})",
                                sqlSub, cosmosSub,
                                null, true,
                                ("SectionMappingId", x => x.SectionMappingId),
                                ("Id", x => x.Id),
                                ("SectionName", x => x.SectionName),
                                ("Order", x => x.Order),
                                ("Totalcards", x => x.Totalcards),
                                ("PlaceholderCount", x => x.PlaceholderCount),
                                ("FileNameCount", x => x.FileNameCount),
                                ("IsSubSection", x => x.IsSubSection)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetProjectSubsectionBySectionId({secId})", error = ex.Message });
                    }
                }

                // 8. GetGradingConditionByProjectId
                var testProjectIds = new List<int>(projectIds);
                testProjectIds.Add(-99999);
                foreach (var projId in testProjectIds)
                {
                    bool isMissingId = projId == -99999;
                    try
                    {
                        var sqlGc = await sqlReportPartRepo.GetGradingConditionByProjectId(projId);
                        var cosmosGc = await cosmosReportPartRepo.GetGradingConditionByProjectId(projId);
                        RecordSqlNonEmpty("GetGradingConditionByProjectId", sqlGc != null && sqlGc.Count > 0);
                        if (sqlGc == null || cosmosGc == null)
                        {
                            bool bothNull = sqlGc == null && cosmosGc == null;
                            checks.Add(new { check = $"ReportPart.GetGradingConditionByProjectId({projId})", sqlNull = sqlGc == null, cosmosNull = cosmosGc == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetGradingConditionByProjectId({projId})",
                                sqlGc, cosmosGc,
                                x => x.Id, false,
                                ("Id", x => x.Id),
                                ("Name", x => x.Name),
                                ("Value", x => x.Value),
                                ("SectionId", x => x.SectionId),
                                ("GradingId", x => x.GradingId)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetGradingConditionByProjectId({projId})", error = ex.Message });
                    }
                }

                // 9. GetGradingBySectionId
                var testGradingSectionPairs = new List<(int ProjectId, Guid SectionId)>(gradingSectionPairs);
                testGradingSectionPairs.Add((-99999, Guid.NewGuid()));
                foreach (var (projId, secId) in testGradingSectionPairs)
                {
                    bool isMissingId = projId == -99999;
                    try
                    {
                        var sqlG = await sqlReportPartRepo.GetGradingBySectionId(projId, secId);
                        var cosmosG = await cosmosReportPartRepo.GetGradingBySectionId(projId, secId);
                        RecordSqlNonEmpty("GetGradingBySectionId", sqlG != null && sqlG.Count > 0);
                        if (sqlG == null || cosmosG == null)
                        {
                            bool bothNull = sqlG == null && cosmosG == null;
                            checks.Add(new { check = $"ReportPart.GetGradingBySectionId({projId}, {secId})", sqlNull = sqlG == null, cosmosNull = cosmosG == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetGradingBySectionId({projId}, {secId})",
                                sqlG, cosmosG,
                                null, true,
                                ("Id", x => x.Id),
                                ("Name", x => x.Name),
                                ("GradingConditionGroupId", x => x.GradingConditionGroupId),
                                ("RequiredInReport", x => x.RequiredInReport)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetGradingBySectionId({projId}, {secId})", error = ex.Message });
                    }
                }

                // 10. GetImageDescriptionsBySectionId
                foreach (var (projId, secId) in testGradingSectionPairs)
                {
                    bool isMissingId = projId == -99999;
                    try
                    {
                        var sqlDesc = await sqlReportPartRepo.GetImageDescriptionsBySectionId(projId, secId);
                        var cosmosDesc = await cosmosReportPartRepo.GetImageDescriptionsBySectionId(projId, secId);
                        RecordSqlNonEmpty("GetImageDescriptionsBySectionId", sqlDesc != null && sqlDesc.Count > 0);
                        if (sqlDesc == null || cosmosDesc == null)
                        {
                            bool bothNull = sqlDesc == null && cosmosDesc == null;
                            checks.Add(new { check = $"ReportPart.GetImageDescriptionsBySectionId({projId}, {secId})", sqlNull = sqlDesc == null, cosmosNull = cosmosDesc == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetImageDescriptionsBySectionId({projId}, {secId})",
                                sqlDesc, cosmosDesc,
                                x => x.Id, false,
                                ("Id", x => x.Id),
                                ("Description", x => x.Description)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetImageDescriptionsBySectionId({projId}, {secId})", error = ex.Message });
                    }
                }

                // 11. GetImageDescriptionsBySubSectionId (T9: real subsection ids)
                var testSubSectionPairs = new List<(int ProjectId, Guid SectionId)>(subSectionPairs);
                testSubSectionPairs.Add((-99999, Guid.NewGuid()));
                foreach (var (projId, secId) in testSubSectionPairs)
                {
                    bool isMissingId = projId == -99999;
                    try
                    {
                        var sqlDesc = await sqlReportPartRepo.GetImageDescriptionsBySubSectionId(projId, secId);
                        var cosmosDesc = await cosmosReportPartRepo.GetImageDescriptionsBySubSectionId(projId, secId);
                        RecordSqlNonEmpty("GetImageDescriptionsBySubSectionId", sqlDesc != null && sqlDesc.Count > 0);
                        if (sqlDesc == null || cosmosDesc == null)
                        {
                            bool bothNull = sqlDesc == null && cosmosDesc == null;
                            checks.Add(new { check = $"ReportPart.GetImageDescriptionsBySubSectionId({projId}, {secId})", sqlNull = sqlDesc == null, cosmosNull = cosmosDesc == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetImageDescriptionsBySubSectionId({projId}, {secId})",
                                sqlDesc, cosmosDesc,
                                x => x.Id, false,
                                ("Id", x => x.Id),
                                ("Description", x => x.Description)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetImageDescriptionsBySubSectionId({projId}, {secId})", error = ex.Message });
                    }
                }

                // 12. GetGenericImageCard
                var testCards = new List<(int ProjectId, int TemplateId, Guid SectionId)>(cards);
                testCards.Add((-99999, -99999, Guid.NewGuid()));
                foreach (var (projId, tmplId, secId) in testCards)
                {
                    bool isMissingId = projId == -99999;
                    try
                    {
                        var sqlCards = await sqlReportPartRepo.GetGenericImageCard(projId, tmplId, secId);
                        var cosmosCards = await cosmosReportPartRepo.GetGenericImageCard(projId, tmplId, secId);
                        RecordSqlNonEmpty("GetGenericImageCard", sqlCards != null && sqlCards.Count > 0);
                        if (sqlCards == null || cosmosCards == null)
                        {
                            bool bothNull = sqlCards == null && cosmosCards == null;
                            checks.Add(new { check = $"ReportPart.GetGenericImageCard({projId}, {tmplId}, {secId})", sqlNull = sqlCards == null, cosmosNull = cosmosCards == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetGenericImageCard({projId}, {tmplId}, {secId})",
                                sqlCards, cosmosCards,
                                x => x.Id, false,
                                ("Id", x => x.Id),
                                ("CardName", x => x.CardName),
                                ("CardNumber", x => x.CardNumber),
                                ("DescriptionId", x => x.DescriptionId),
                                ("AdditionalDescription", x => x.AdditionalDescription),
                                ("CurrentCondition", x => x.CurrentCondition),
                                ("IsActive", x => x.IsActive),
                                ("ImageId", x => x.ImageId),
                                ("src", x => x.src)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetGenericImageCard({projId}, {tmplId}, {secId})", error = ex.Message });
                    }
                }

                // 13. GetTankSectionByProjectId
                var testTankSections = new List<(int ProjectId, int PartId, int ImoNumber)>(tankSections);
                testTankSections.Add((-99999, -99999, 0));
                foreach (var (projId, partId, imo) in testTankSections)
                {
                    bool isMissingId = projId == -99999;
                    try
                    {
                        var sqlT = await sqlReportPartRepo.GetTankSectionByProjectId(projId, partId, imo);
                        var cosmosT = await cosmosReportPartRepo.GetTankSectionByProjectId(projId, partId, imo);
                        RecordSqlNonEmpty("GetTankSectionByProjectId", sqlT != null && sqlT.Count > 0);
                        if (sqlT == null || cosmosT == null)
                        {
                            bool bothNull = sqlT == null && cosmosT == null;
                            checks.Add(new { check = $"ReportPart.GetTankSectionByProjectId({projId}, {partId}, {imo})", sqlNull = sqlT == null, cosmosNull = cosmosT == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetTankSectionByProjectId({projId}, {partId}, {imo})",
                                sqlT, cosmosT,
                                null, true,
                                ("TankTypeId", x => x.TankTypeId),
                                ("TankName", x => x.TankName),
                                ("Id", x => x.Id),
                                ("OrderNumber", x => x.OrderNumber),
                                ("SectionMappingId", x => x.SectionMappingId),
                                ("Subheader", x => x.Subheader),
                                ("Totalcards", x => x.Totalcards),
                                ("PlaceholderCount", x => x.PlaceholderCount),
                                ("FileNameCount", x => x.FileNameCount)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetTankSectionByProjectId({projId}, {partId}, {imo})", error = ex.Message });
                    }
                }

                // 14. GetH_IGrading(false) and (true)
                try
                {
                    var sqlHI0 = await sqlReportPartRepo.GetH_IGrading(false);
                    var cosmosHI0 = await cosmosReportPartRepo.GetH_IGrading(false);
                    RecordSqlNonEmpty("GetH_IGrading.false", sqlHI0 != null && sqlHI0.Count > 0);
                    if (sqlHI0 == null || cosmosHI0 == null)
                    {
                        checks.Add(new { check = "ReportPart.GetH_IGrading(false)", sqlNull = sqlHI0 == null, cosmosNull = cosmosHI0 == null, equal = false });
                        allPassed = false;
                    }
                    else
                    {
                        checks.Add(ParityCompare.Lists("ReportPart.GetH_IGrading(false)",
                            sqlHI0, cosmosHI0,
                            x => x.Id, false,
                            ("Id", x => x.Id),
                            ("Name", x => x.Name),
                            ("Value", x => x.Value)));
                    }

                    var sqlHI1 = await sqlReportPartRepo.GetH_IGrading(true);
                    var cosmosHI1 = await cosmosReportPartRepo.GetH_IGrading(true);
                    RecordSqlNonEmpty("GetH_IGrading.true", sqlHI1 != null && sqlHI1.Count > 0);
                    if (sqlHI1 == null || cosmosHI1 == null)
                    {
                        checks.Add(new { check = "ReportPart.GetH_IGrading(true)", sqlNull = sqlHI1 == null, cosmosNull = cosmosHI1 == null, equal = false });
                        allPassed = false;
                    }
                    else
                    {
                        checks.Add(ParityCompare.Lists("ReportPart.GetH_IGrading(true)",
                            sqlHI1, cosmosHI1,
                            x => x.Id, false,
                            ("Id", x => x.Id),
                            ("Name", x => x.Name),
                            ("Value", x => x.Value)));
                    }
                }
                catch (Exception ex)
                {
                    hadException = true;
                    allPassed = false;
                    checks.Add(new { check = "ReportPart.GetH_IGrading", error = ex.Message });
                }

                // 15. GetImageDescriptionsByTankTypeId
                var testTankTypePairs = new List<(int ProjectId, int TankTypeId)>(tankTypePairs);
                testTankTypePairs.Add((-99999, -99999));
                foreach (var (projId, ttId) in testTankTypePairs)
                {
                    bool isMissingId = projId == -99999;
                    try
                    {
                        var sqlDesc = await sqlReportPartRepo.GetImageDescriptionsByTankTypeId(projId, ttId);
                        var cosmosDesc = await cosmosReportPartRepo.GetImageDescriptionsByTankTypeId(projId, ttId);
                        RecordSqlNonEmpty("GetImageDescriptionsByTankTypeId", sqlDesc != null && sqlDesc.Count > 0);
                        if (sqlDesc == null || cosmosDesc == null)
                        {
                            bool bothNull = sqlDesc == null && cosmosDesc == null;
                            checks.Add(new { check = $"ReportPart.GetImageDescriptionsByTankTypeId({projId}, {ttId})", sqlNull = sqlDesc == null, cosmosNull = cosmosDesc == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetImageDescriptionsByTankTypeId({projId}, {ttId})",
                                sqlDesc, cosmosDesc,
                                null, true,
                                ("Id", x => x.Id),
                                ("Description", x => x.Description),
                                ("value", x => x.value),
                                ("TankTypeId", x => x.TankTypeId)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetImageDescriptionsByTankTypeId({projId}, {ttId})", error = ex.Message });
                    }
                }

                // 16. GetHandIGradingConditionByProjectId
                foreach (var projId in testProjectIds)
                {
                    bool isMissingId = projId == -99999;
                    try
                    {
                        var sqlHi = await sqlReportPartRepo.GetHandIGradingConditionByProjectId(projId);
                        var cosmosHi = await cosmosReportPartRepo.GetHandIGradingConditionByProjectId(projId);
                        RecordSqlNonEmpty("GetHandIGradingConditionByProjectId", sqlHi != null && sqlHi.Count > 0);
                        if (sqlHi == null || cosmosHi == null)
                        {
                            bool bothNull = sqlHi == null && cosmosHi == null;
                            checks.Add(new { check = $"ReportPart.GetHandIGradingConditionByProjectId({projId})", sqlNull = sqlHi == null, cosmosNull = cosmosHi == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetHandIGradingConditionByProjectId({projId})",
                                sqlHi, cosmosHi,
                                x => x.Id, false,
                                ("Id", x => x.Id),
                                ("ProjectId", x => x.ProjectId),
                                ("TemplateId", x => x.TemplateId),
                                ("SectionId", x => x.SectionId),
                                ("GradingId", x => x.GradingId),
                                ("GradingConditionLabel", x => x.GradingConditionLabel),
                                ("GradingConditionValue", x => x.GradingConditionValue)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetHandIGradingConditionByProjectId({projId})", error = ex.Message });
                    }
                }

                // 17. GetH_IProjectGradingByProjectId
                foreach (var projId in testProjectIds)
                {
                    bool isMissingId = projId == -99999;
                    try
                    {
                        var sqlPg = await sqlReportPartRepo.GetH_IProjectGradingByProjectId(projId);
                        var cosmosPg = await cosmosReportPartRepo.GetH_IProjectGradingByProjectId(projId);
                        RecordSqlNonEmpty("GetH_IProjectGradingByProjectId", sqlPg != null && sqlPg.Count > 0);
                        if (sqlPg == null || cosmosPg == null)
                        {
                            bool bothNull = sqlPg == null && cosmosPg == null;
                            checks.Add(new { check = $"ReportPart.GetH_IProjectGradingByProjectId({projId})", sqlNull = sqlPg == null, cosmosNull = cosmosPg == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetH_IProjectGradingByProjectId({projId})",
                                sqlPg, cosmosPg,
                                x => x.Id, false,
                                ("Id", x => x.Id),
                                ("ProjectId", x => x.ProjectId),
                                ("TemplateId", x => x.TemplateId),
                                ("SectionId", x => x.SectionId),
                                ("GradingId", x => x.GradingId),
                                ("Value", x => x.Value)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetH_IProjectGradingByProjectId({projId})", error = ex.Message });
                    }
                }

                // 18. GetProjectTemplateTitle (partId, projId) (T9)
                var testTitleParts = new List<(int PartId, int ProjectId)>();
                if (projectIds.Count == 0)
                {
                    allPassed = false;
                    checks.Add(new { check = "ReportPart.GetProjectTemplateTitle", error = "projectIds is empty" });
                }
                else
                {
                    if (projectParts.Count > 0)
                    {
                        foreach (var pp in projectParts.Take(5))
                        {
                            testTitleParts.Add((pp.PartId, pp.ProjectId));
                        }
                    }
                    testTitleParts.Add((-99999, projectIds[0]));
                }
                foreach (var (partId, projId) in testTitleParts)
                {
                    bool isMissingId = partId == -99999;
                    try
                    {
                        var sqlT = await sqlReportPartRepo.GetProjectTemplateTitle(partId, projId);
                        var cosmosT = await cosmosReportPartRepo.GetProjectTemplateTitle(partId, projId);
                        RecordSqlNonEmpty("GetProjectTemplateTitle", sqlT != null && !string.IsNullOrEmpty(sqlT.PartName));
                        bool bothNull = sqlT == null && cosmosT == null;
                        if (bothNull && !isMissingId)
                        {
                            allPassed = false;
                            checks.Add(new { check = $"ReportPart.GetProjectTemplateTitle({partId}, {projId})", sqlNull = true, cosmosNull = true, equal = false });
                        }
                        else
                        {
                            checks.Add(ParityCompare.Single($"ReportPart.GetProjectTemplateTitle({partId}, {projId})",
                                sqlT, cosmosT,
                                ("PartId", x => x?.PartId),
                                ("PartName", x => x?.PartName),
                                ("SequenceNo", x => x?.SequenceNo)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetProjectTemplateTitle({partId}, {projId})", error = ex.Message });
                    }
                }

                // 19. GetVesselTypeExportPartConfiguration
                foreach (var vt in testVesselTypes)
                {
                    bool isMissingId = vt == -99999;
                    try
                    {
                        var sqlEp = await sqlReportPartRepo.GetVesselTypeExportPartConfiguration(vt);
                        var cosmosEp = await cosmosReportPartRepo.GetVesselTypeExportPartConfiguration(vt);
                        RecordSqlNonEmpty("GetVesselTypeExportPartConfiguration", sqlEp != null && sqlEp.Count > 0);
                        if (sqlEp == null || cosmosEp == null)
                        {
                            bool bothNull = sqlEp == null && cosmosEp == null;
                            checks.Add(new { check = $"ReportPart.GetVesselTypeExportPartConfiguration({vt})", sqlNull = sqlEp == null, cosmosNull = cosmosEp == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetVesselTypeExportPartConfiguration({vt})",
                                sqlEp, cosmosEp,
                                null, true,
                                ("PartId", x => x.PartId),
                                ("PartName", x => x.PartName),
                                ("RequiredInReport", x => x.RequiredInReport)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetVesselTypeExportPartConfiguration({vt})", error = ex.Message });
                    }
                }

                // 20. GetVesselTypeExportSectionConfiguration
                var testExportSections = new List<(int PartId, int VesselTypeId)>(exportSections);
                testExportSections.Add((-99999, -99999));
                foreach (var (partId, vt) in testExportSections)
                {
                    bool isMissingId = vt == -99999;
                    try
                    {
                        var sqlEs = await sqlReportPartRepo.GetVesselTypeExportSectionConfiguration(partId, vt);
                        var cosmosEs = await cosmosReportPartRepo.GetVesselTypeExportSectionConfiguration(partId, vt);
                        RecordSqlNonEmpty("GetVesselTypeExportSectionConfiguration", sqlEs != null && sqlEs.Count > 0);
                        if (sqlEs == null || cosmosEs == null)
                        {
                            bool bothNull = sqlEs == null && cosmosEs == null;
                            checks.Add(new { check = $"ReportPart.GetVesselTypeExportSectionConfiguration({partId}, {vt})", sqlNull = sqlEs == null, cosmosNull = cosmosEs == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetVesselTypeExportSectionConfiguration({partId}, {vt})",
                                sqlEs, cosmosEs,
                                null, true,
                                ("SectionId", x => x.SectionId),
                                ("SectionName", x => x.SectionName),
                                ("TankTypeId", x => x.TankTypeId),
                                ("TankType", x => x.TankType),
                                ("IsSubSection", x => x.IsSubSection),
                                ("ParentId", x => x.ParentId),
                                ("RequiredInReport", x => x.RequiredInReport)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetVesselTypeExportSectionConfiguration({partId}, {vt})", error = ex.Message });
                    }
                }

                // 21. GetProjectExportPartConfiguration
                foreach (var projId in testProjectIds)
                {
                    bool isMissingId = projId == -99999;
                    try
                    {
                        var sqlPep = await sqlReportPartRepo.GetProjectExportPartConfiguration(projId);
                        var cosmosPep = await cosmosReportPartRepo.GetProjectExportPartConfiguration(projId);
                        RecordSqlNonEmpty("GetProjectExportPartConfiguration", sqlPep != null && sqlPep.Count > 0);
                        if (sqlPep == null || cosmosPep == null)
                        {
                            bool bothNull = sqlPep == null && cosmosPep == null;
                            checks.Add(new { check = $"ReportPart.GetProjectExportPartConfiguration({projId})", sqlNull = sqlPep == null, cosmosNull = cosmosPep == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetProjectExportPartConfiguration({projId})",
                                sqlPep, cosmosPep,
                                null, true,
                                ("PartId", x => x.PartId),
                                ("PartName", x => x.PartName),
                                ("RequiredInReport", x => x.RequiredInReport)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetProjectExportPartConfiguration({projId})", error = ex.Message });
                    }
                }

                // 22. GetProjectExportSectionConfiguration
                foreach (var (projId, partId) in testProjectParts)
                {
                    bool isMissingId = projId == -99999;
                    try
                    {
                        var sqlPes = await sqlReportPartRepo.GetProjectExportSectionConfiguration(partId, projId);
                        var cosmosPes = await cosmosReportPartRepo.GetProjectExportSectionConfiguration(partId, projId);
                        RecordSqlNonEmpty("GetProjectExportSectionConfiguration", sqlPes != null && sqlPes.Count > 0);
                        if (sqlPes == null || cosmosPes == null)
                        {
                            bool bothNull = sqlPes == null && cosmosPes == null;
                            checks.Add(new { check = $"ReportPart.GetProjectExportSectionConfiguration({partId}, {projId})", sqlNull = sqlPes == null, cosmosNull = cosmosPes == null, equal = bothNull && isMissingId });
                            if (!bothNull || !isMissingId) allPassed = false;
                        }
                        else
                        {
                            checks.Add(ParityCompare.Lists($"ReportPart.GetProjectExportSectionConfiguration({partId}, {projId})",
                                sqlPes, cosmosPes,
                                null, true,
                                ("SectionId", x => x.SectionId),
                                ("SectionName", x => x.SectionName),
                                ("TankTypeId", x => x.TankTypeId),
                                ("TankType", x => x.TankType),
                                ("IsSubSection", x => x.IsSubSection),
                                ("ParentId", x => x.ParentId),
                                ("RequiredInReport", x => x.RequiredInReport)));
                        }
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.GetProjectExportSectionConfiguration({partId}, {projId})", error = ex.Message });
                    }
                }

                // 23. CheckToShowSectionExportConfig (Y4: fail unless SQL returns true for at least one input)
                var testShowConfigs = new List<(int ProjectId, int PartId, int ImoNumber)>(tankSections);
                foreach (var sti in showTrueInputs)
                {
                    testShowConfigs.Add((sti.ProjectId, sti.PartId, 0));
                }
                if (projectParts.Count > 0)
                {
                    testShowConfigs.Add((projectParts[0].ProjectId, projectParts[0].PartId, 0));
                }
                testShowConfigs.Add((-99999, -99999, 0));
                testShowConfigs.Add((-99999, -99999, -99999));
                bool anySqlShowTrue = false;
                foreach (var (projId, partId, imo) in testShowConfigs)
                {
                    try
                    {
                        var sqlShow = await sqlReportPartRepo.CheckToShowSectionExportConfig(projId, imo, partId);
                        var cosmosShow = await cosmosReportPartRepo.CheckToShowSectionExportConfig(projId, imo, partId);
                        if (sqlShow) anySqlShowTrue = true;
                        checks.Add(new { check = $"ReportPart.CheckToShowSectionExportConfig({projId}, {imo}, {partId})", sql = sqlShow, cosmos = cosmosShow, equal = sqlShow == cosmosShow });
                        if (sqlShow != cosmosShow) allPassed = false;
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { check = $"ReportPart.CheckToShowSectionExportConfig({projId}, {imo}, {partId})", error = ex.Message });
                    }
                }
                if (!anySqlShowTrue)
                {
                    allPassed = false;
                    checks.Add(new { check = "ReportPart.CheckToShowSectionExportConfig.AtLeastOneTrue", passed = false, error = "SQL never returned true for CheckToShowSectionExportConfig" });
                }

                // T1: Check that every expected method had at least 1 non-empty SQL result across the run
                var expectedMethods = new[]
                {
                    "GetVesselTypes", "GetReportPartsByVesselTypeAsync", "GetSectionNamesByPartNameAsync.Normal",
                    "GetSectionNamesByPartNameAsync.Tanks", "GetSectionNamesByPartId.Normal", "GetSectionNamesByPartId.Sub",
                    "GetSectionNamesByPartId.Tanks", "GetGradingCondition", "GetNormalSectionByProjectId",
                    "GetProjectSubsectionBySectionId", "GetGradingConditionByProjectId", "GetGradingBySectionId",
                    "GetImageDescriptionsBySectionId", "GetImageDescriptionsBySubSectionId", "GetGenericImageCard",
                    "GetTankSectionByProjectId", "GetH_IGrading.false", "GetH_IGrading.true", "GetImageDescriptionsByTankTypeId",
                    "GetHandIGradingConditionByProjectId", "GetH_IProjectGradingByProjectId", "GetProjectTemplateTitle",
                    "GetVesselTypeExportPartConfiguration", "GetVesselTypeExportSectionConfiguration",
                    "GetProjectExportPartConfiguration", "GetProjectExportSectionConfiguration"
                };
                foreach (var em in expectedMethods)
                {
                    int c = sqlNonEmptyCount.GetValueOrDefault(em, 0);
                    if (c == 0)
                    {
                        allPassed = false;
                        checks.Add(new { check = $"SqlHasNonEmptyResults.{em}", passed = false, error = $"Method {em} had 0 non-empty SQL results across the run" });
                    }
                }

                // Write tests (&writeTest=yes)
                if (string.Equals(writeTest, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    var rpContainer = factory.GetContainer(CosmosContainers.Config_ReportPart);
                    var psmContainer = factory.GetContainer(CosmosContainers.Config_ProjectSectionMapping);
                    var ptsContainer = factory.GetContainer(CosmosContainers.Config_ProjectTankSectionMapping);
                    var psubContainer = factory.GetContainer(CosmosContainers.Config_ProjectSubSection);
                    var psdContainer = factory.GetContainer(CosmosContainers.Config_ProjectSectionDescriptionMapping);
                    var psdSubContainer = factory.GetContainer(CosmosContainers.Config_ProjectSubSectionDescriptionMapping);
                    var psgContainer = factory.GetContainer(CosmosContainers.Config_ProjectSectionGradingMapping);

                    var vtpContainer = factory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
                    var vnsContainer = factory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
                    var vtsContainer = factory.GetContainer(CosmosContainers.Config_VesselTypeTankSectionMapping);
                    var vsubContainer = factory.GetContainer(CosmosContainers.Config_VesselTypeSubSection);
                    var vsdContainer = factory.GetContainer(CosmosContainers.Config_VesselTypeSectionDescriptionMapping);
                    var vsubDescContainer = factory.GetContainer(CosmosContainers.Config_VesselTypeSubSectionDescriptionMapping);
                    var vsgContainer = factory.GetContainer(CosmosContainers.Config_VesselTypeSectionGradingMapping);

                    var trackedRp = new List<(string id, double partitionKey)>();
                    var trackedPsm = new List<(string id, double partitionKey)>();
                    var trackedPts = new List<(string id, double partitionKey)>();
                    var trackedPsub = new List<string>();
                    var trackedPsd = new List<string>();
                    var trackedPsdSub = new List<string>();
                    var trackedPsg = new List<string>();

                    var trackedVtp = new List<(string id, double partitionKey)>();
                    var trackedVns = new List<(string id, double partitionKey)>();
                    var trackedVts = new List<(string id, double partitionKey)>();
                    var trackedVsub = new List<string>();
                    var trackedVsubDesc = new List<string>();
                    var trackedVsd = new List<string>();
                    var trackedVsg = new List<string>();

                    var cleanupErrors = new List<string>();
                    async Task TryCleanup(string name, Func<Task> action)
                    {
                        try { await action(); }
                        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { }
                        catch (Exception ex) { cleanupErrors.Add($"{name}: {ex.Message}"); }
                    }

                    async Task<TDoc?> SafeReadAsync<TDoc>(Container container, string id, PartitionKey pk) where TDoc : class
                    {
                        try
                        {
                            var r = await container.ReadItemAsync<TDoc>(id, pk);
                            return r.Resource;
                        }
                        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            return null;
                        }
                    }

                    async Task<int> GetCountAsync(Container c, string query, PartitionKey pk)
                    {
                        var it = c.GetItemQueryIterator<int>(new QueryDefinition(query), requestOptions: new QueryRequestOptions { PartitionKey = pk });
                        int cnt = 0;
                        while (it.HasMoreResults)
                        {
                            var r = await it.ReadNextAsync();
                            cnt += r.FirstOrDefault();
                        }
                        return cnt;
                    }

                    try
                    {
                        // E8 & T5: Assert -999 and -998 partitions are empty before running (INSIDE try)
                        int preRp = await GetCountAsync(rpContainer, "SELECT VALUE COUNT(1) FROM c WHERE c.ProjectId = -999", new PartitionKey(-999.0));
                        int prePsm = await GetCountAsync(psmContainer, "SELECT VALUE COUNT(1) FROM c WHERE c.ProjectId = -999", new PartitionKey(-999.0));
                        int prePts = await GetCountAsync(ptsContainer, "SELECT VALUE COUNT(1) FROM c WHERE c.ProjectId = -999", new PartitionKey(-999.0));
                        int preVtp = await GetCountAsync(vtpContainer, "SELECT VALUE COUNT(1) FROM c WHERE c.VesselTypeId = -999", new PartitionKey(-999.0));
                        int preVns = await GetCountAsync(vnsContainer, "SELECT VALUE COUNT(1) FROM c WHERE c.VesselTypeId = -999", new PartitionKey(-999.0));
                        int preVts = await GetCountAsync(vtsContainer, "SELECT VALUE COUNT(1) FROM c WHERE c.VesselTypeId = -999", new PartitionKey(-999.0));

                        int preRbRp = await GetCountAsync(rpContainer, "SELECT VALUE COUNT(1) FROM c WHERE c.ProjectId = -998", new PartitionKey(-998.0));
                        int preRbPsm = await GetCountAsync(psmContainer, "SELECT VALUE COUNT(1) FROM c WHERE c.ProjectId = -998", new PartitionKey(-998.0));
                        int preRbPts = await GetCountAsync(ptsContainer, "SELECT VALUE COUNT(1) FROM c WHERE c.ProjectId = -998", new PartitionKey(-998.0));
                        int preRbVtp = await GetCountAsync(vtpContainer, "SELECT VALUE COUNT(1) FROM c WHERE c.VesselTypeId = -998", new PartitionKey(-998.0));
                        int preRbVns = await GetCountAsync(vnsContainer, "SELECT VALUE COUNT(1) FROM c WHERE c.VesselTypeId = -998", new PartitionKey(-998.0));
                        int preRbVts = await GetCountAsync(vtsContainer, "SELECT VALUE COUNT(1) FROM c WHERE c.VesselTypeId = -998", new PartitionKey(-998.0));

                        if (preRp > 0 || prePsm > 0 || prePts > 0 || preVtp > 0 || preVns > 0 || preVts > 0 ||
                            preRbRp > 0 || preRbPsm > 0 || preRbPts > 0 || preRbVtp > 0 || preRbVns > 0 || preRbVts > 0)
                        {
                            allPassed = false;
                            writeSteps.Add(new { step = "AssertPartitionsEmptyBeforeRun", passed = false, error = $"Partitions for -999 or -998 are not empty: rp={preRp}, psm={prePsm}, pts={prePts}, vtp={preVtp}, vns={preVns}, vts={preVts}, rbRp={preRbRp}, rbPsm={preRbPsm}, rbPts={preRbPts}, rbVtp={preRbVtp}, rbVns={preRbVns}, rbVts={preRbVts}" });
                            return Results.Ok(new { batch = 8, allPassed, checksCount = checks.Count, checks, writeSteps });
                        }
                        writeSteps.Add(new { step = "AssertPartitionsEmptyBeforeRun", passed = true });

                        // E6 & E7: Build -999 config in-memory and write to Cosmos
                        int testVtId = -999;
                        int testProjectId = -999;

                        // Parts (2 parts)
                        var partDoc1 = new VesselTypePartMappingDocument
                        {
                            id = "-9991",
                            VesselTypePartMappingId = -9991,
                            VesselTypeId = testVtId,
                            PartName = "Batch8 Test Part 1",
                            SequenceNo = 1,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "SetupTester",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "SetupTester",
                            ModifiedOn = DateTime.UtcNow.Date,
                            RequiredInReport = true
                        };
                        var partDoc2 = new VesselTypePartMappingDocument
                        {
                            id = "-9992",
                            VesselTypePartMappingId = -9992,
                            VesselTypeId = testVtId,
                            PartName = "Batch8 Test Part 2",
                            SequenceNo = 2,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "SetupTester",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "SetupTester",
                            ModifiedOn = DateTime.UtcNow.Date,
                            RequiredInReport = true
                        };
                        await vtpContainer.CreateItemAsync(partDoc1, new PartitionKey((double)testVtId));
                        trackedVtp.Add((partDoc1.id, testVtId));
                        await vtpContainer.CreateItemAsync(partDoc2, new PartitionKey((double)testVtId));
                        trackedVtp.Add((partDoc2.id, testVtId));

                        // Normal Sections (2 sections)
                        var testSecGuid1 = Guid.NewGuid();
                        var testSecGuid2 = Guid.NewGuid();
                        var secDoc1 = new VesselTypeNormalSectionMappingDocument
                        {
                            id = testSecGuid1.ToString(),
                            SectionMappingId = testSecGuid1.ToString(),
                            VesselTypePartMappingId = -9991,
                            VesselTypeId = testVtId,
                            SectionName = "Batch8 Test Section 1",
                            SubHeader = "SubHeader 1",
                            Order = 1,
                            HasSubSection = true, // T2: HasSubSection = true so CreateProjectSubParts runs
                            TotalCards = 4,
                            PlaceholderCount = 2,
                            FileNameCount = 2,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "SetupTester",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "SetupTester",
                            ModifiedOn = DateTime.UtcNow.Date,
                            RequiredInReport = true
                        };
                        var secDoc2 = new VesselTypeNormalSectionMappingDocument
                        {
                            id = testSecGuid2.ToString(),
                            SectionMappingId = testSecGuid2.ToString(),
                            VesselTypePartMappingId = -9991,
                            VesselTypeId = testVtId,
                            SectionName = "Batch8 Test Section 2",
                            SubHeader = "SubHeader 2",
                            Order = 2,
                            TotalCards = 4,
                            PlaceholderCount = 2,
                            FileNameCount = 2,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "SetupTester",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "SetupTester",
                            ModifiedOn = DateTime.UtcNow.Date,
                            RequiredInReport = true
                        };
                        await vnsContainer.CreateItemAsync(secDoc1, new PartitionKey((double)testVtId));
                        trackedVns.Add((secDoc1.id, testVtId));
                        await vnsContainer.CreateItemAsync(secDoc2, new PartitionKey((double)testVtId));
                        trackedVns.Add((secDoc2.id, testVtId));

                        // Subsection under Section 1
                        var testSubSecGuid = Guid.NewGuid();
                        var vsubDoc = new VesselTypeSubSectionDocument
                        {
                            id = testSubSecGuid.ToString(),
                            Id = testSubSecGuid.ToString(),
                            VesselTypeSectionId = testSecGuid1.ToString(),
                            SectionName = "Batch8 Test Subsection 1",
                            OrderNumber = 1,
                            Totalcards = 2,
                            PlaceholderCount = 1,
                            FileNameCount = 1,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "SetupTester",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "SetupTester",
                            ModifiedOn = DateTime.UtcNow.Date
                        };
                        await vsubContainer.CreateItemAsync(vsubDoc, new PartitionKey(vsubDoc.id));
                        trackedVsub.Add(vsubDoc.id);

                        // Subsection description mapping for Subsection 1 (T2)
                        var vsubDescId = $"{testSubSecGuid}|301";
                        var vsubDescDoc = new VesselTypeSubSectionDescriptionMappingDocument
                        {
                            id = vsubDescId,
                            SubSectionId = testSubSecGuid.ToString(),
                            CategoryId = 301,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "SetupTester",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "SetupTester",
                            ModifiedOn = DateTime.UtcNow.Date
                        };
                        await vsubDescContainer.CreateItemAsync(vsubDescDoc, new PartitionKey(vsubDescDoc.id));
                        trackedVsubDesc.Add(vsubDescDoc.id);

                        // Tank Section
                        var testTankSecGuid = Guid.NewGuid();
                        var tankDoc = new VesselTypeTankSectionMappingDocument
                        {
                            id = testTankSecGuid.ToString(),
                            VesselTypeTankSectionMappingId = testTankSecGuid.ToString(),
                            VesselTypePartMappingId = -9992,
                            VesselTypeId = testVtId,
                            TankTypeId = 701,
                            TotalCards = 2,
                            PlaceholderCount = 1,
                            FileNameCount = 1,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "SetupTester",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "SetupTester",
                            ModifiedOn = DateTime.UtcNow.Date
                        };
                        await vtsContainer.CreateItemAsync(tankDoc, new PartitionKey((double)testVtId));
                        trackedVts.Add((tankDoc.id, testVtId));

                        // Description mapping for Section 1: TWO categories!
                        var vsdId1 = $"{testSecGuid1}|101";
                        var vsdId2 = $"{testSecGuid1}|102";
                        var vsdDoc1 = new VesselTypeSectionDescriptionMappingDocument
                        {
                            id = vsdId1,
                            SectionId = testSecGuid1.ToString(),
                            CategoryId = 101,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "SetupTester",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "SetupTester",
                            ModifiedOn = DateTime.UtcNow.Date
                        };
                        var vsdDoc2 = new VesselTypeSectionDescriptionMappingDocument
                        {
                            id = vsdId2,
                            SectionId = testSecGuid1.ToString(),
                            CategoryId = 102,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "SetupTester",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "SetupTester",
                            ModifiedOn = DateTime.UtcNow.Date
                        };
                        await vsdContainer.CreateItemAsync(vsdDoc1, new PartitionKey(vsdDoc1.id));
                        trackedVsd.Add(vsdDoc1.id);
                        await vsdContainer.CreateItemAsync(vsdDoc2, new PartitionKey(vsdDoc2.id));
                        trackedVsd.Add(vsdDoc2.id);

                        // Section grading mapping for Section 1
                        var vsgId = $"{testSecGuid1}|201";
                        var vsgDoc = new VesselTypeSectionGradingMappingDocument
                        {
                            id = vsgId,
                            SectionId = testSecGuid1.ToString(),
                            CategoryId = 201,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "SetupTester",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "SetupTester",
                            ModifiedOn = DateTime.UtcNow.Date
                        };
                        await vsgContainer.CreateItemAsync(vsgDoc, new PartitionKey(vsgDoc.id));
                        trackedVsg.Add(vsgDoc.id);

                        // 1. CreateProjectReportTemplate with imoExists = true
                        var createTmplRes = await cosmosReportPartRepo.CreateProjectReportTemplate(testVtId, "ParityUserBatch8", testProjectId, true, 9999999);
                        writeSteps.Add(new { step = "CreateProjectReportTemplate(valid)", passed = createTmplRes });
                        if (!createTmplRes) allPassed = false;

                        // Read-backs and assertions for CreateProjectReportTemplate (T3: assert every column)
                        // a) ReportPart: 2 items
                        var createdRp = new List<ReportPartDocument>();
                        using (var it = rpContainer.GetItemQueryIterator<ReportPartDocument>(new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @pid").WithParameter("@pid", testProjectId), requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)testProjectId) }))
                        {
                            while (it.HasMoreResults)
                            {
                                var r = await it.ReadNextAsync();
                                createdRp.AddRange(r);
                                foreach (var d in r) trackedRp.Add((d.id, testProjectId));
                            }
                        }
                        bool rpOk = createdRp.Count == 2 && createdRp.All(d =>
                            d.ProjectId == testProjectId &&
                            d.IsDeleted == false &&
                            d.IsActive == true &&
                            d.CreatedBy == "ParityUserBatch8" &&
                            d.CreatedOn.HasValue &&
                            d.ModifiedBy == "ParityUserBatch8" &&
                            d.ModifiedOn.HasValue &&
                            d.RequiredInReport == true)
                            && createdRp.Any(d => d.PartName == "Batch8 Test Part 1" && d.SequenceNo == 1 && d.PartId == -9991 && d.id == $"{testProjectId}|-9991")
                            && createdRp.Any(d => d.PartName == "Batch8 Test Part 2" && d.SequenceNo == 2 && d.PartId == -9992 && d.id == $"{testProjectId}|-9992");

                        // b) ProjectSectionMapping: 2 items (T3: assert every column)
                        var createdPsm = new List<ProjectSectionMappingDocument>();
                        using (var it = psmContainer.GetItemQueryIterator<ProjectSectionMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @pid").WithParameter("@pid", testProjectId), requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)testProjectId) }))
                        {
                            while (it.HasMoreResults)
                            {
                                var r = await it.ReadNextAsync();
                                createdPsm.AddRange(r);
                                foreach (var d in r) trackedPsm.Add((d.id, testProjectId));
                            }
                        }
                        bool psmOk = createdPsm.Count == 2 && createdPsm.All(d =>
                            d.ProjectId == testProjectId &&
                            d.IsDeleted == false &&
                            d.IsActive == true &&
                            d.CreatedBy == "ParityUserBatch8" &&
                            d.CreatedOn.HasValue &&
                            d.ModifiedBy == "ParityUserBatch8" &&
                            d.ModifiedOn.HasValue &&
                            d.RequiredInReport == true &&
                            d.TotalCards == 4 && d.PlaceholderCount == 2 && d.FileNameCount == 2 &&
                            d.id == d.SectionMappingId && !string.IsNullOrEmpty(d.id) &&
                            d.PartId == -9991)
                            && createdPsm.Any(d => d.SectionName == "Batch8 Test Section 1" && d.SubHeader == "SubHeader 1" && d.Order == 1 && d.HasSubSection == true)
                            && createdPsm.Any(d => d.SectionName == "Batch8 Test Section 2" && d.SubHeader == "SubHeader 2" && d.Order == 2 && d.HasSubSection == false);

                        // c) ProjectTankSectionMapping: 1 item (because imoExists = true) (T3: assert every column)
                        var createdPts = new List<ProjectTankSectionMappingDocument>();
                        using (var it = ptsContainer.GetItemQueryIterator<ProjectTankSectionMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @pid").WithParameter("@pid", testProjectId), requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)testProjectId) }))
                        {
                            while (it.HasMoreResults)
                            {
                                var r = await it.ReadNextAsync();
                                createdPts.AddRange(r);
                                foreach (var d in r) trackedPts.Add((d.id, testProjectId));
                            }
                        }
                        bool ptsOk = createdPts.Count == 1 && createdPts.All(d =>
                            d.ProjectId == testProjectId &&
                            d.TankTypeId == 701 &&
                            d.IsDeleted == false &&
                            d.IsActive == true &&
                            d.CreatedBy == "ParityUserBatch8" &&
                            d.CreatedOn.HasValue &&
                            d.ModifiedBy == "ParityUserBatch8" &&
                            d.ModifiedOn.HasValue &&
                            d.TotalCards == 2 && d.PlaceholderCount == 1 && d.FileNameCount == 1 &&
                            d.id == d.TankSectionMappingId && !string.IsNullOrEmpty(d.id) &&
                            d.PartId == -9992);

                        // d) ProjectSubSection: created for Section 1 with 'NVijyakumar@eagle.org' (T3: assert every column)
                        var psm1 = createdPsm.FirstOrDefault(s => s.SectionName == "Batch8 Test Section 1");
                        var createdPsub = new List<ProjectSubSectionDocument>();
                        if (psm1 != null)
                        {
                            using (var it = psubContainer.GetItemQueryIterator<ProjectSubSectionDocument>(new QueryDefinition("SELECT * FROM c WHERE c.ProjectSectionId = @secId").WithParameter("@secId", psm1.SectionMappingId)))
                            {
                                while (it.HasMoreResults)
                                {
                                    var r = await it.ReadNextAsync();
                                    createdPsub.AddRange(r);
                                    foreach (var d in r) trackedPsub.Add(d.id);
                                }
                            }
                        }
                        bool psubOk = createdPsub.Count == 1 && createdPsub.All(d =>
                            d.id == d.Id && !string.IsNullOrEmpty(d.id) &&
                            d.ProjectSectionId == psm1!.SectionMappingId &&
                            d.SectionName == "Batch8 Test Subsection 1" &&
                            d.OrderNumber == 1 &&
                            d.Totalcards == 2 && d.PlaceholderCount == 1 && d.FileNameCount == 1 &&
                            d.IsActive == true &&
                            d.IsDeleted == false &&
                            d.CreatedBy == "NVijyakumar@eagle.org" &&
                            d.CreatedOn.HasValue &&
                            d.ModifiedBy == "NVijyakumar@eagle.org" &&
                            d.ModifiedOn.HasValue);

                        // e) ProjectSectionDescriptionMapping: BOTH ...|101 and ...|102 exist on section, AND seeded on subsection (T2 & T4)
                        var createdPsd = new List<ProjectSectionDescriptionMappingDocument>();
                        if (psm1 != null)
                        {
                            using (var it = psdContainer.GetItemQueryIterator<ProjectSectionDescriptionMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.SectionId = @secId").WithParameter("@secId", psm1.SectionMappingId)))
                            {
                                while (it.HasMoreResults)
                                {
                                    var r = await it.ReadNextAsync();
                                    createdPsd.AddRange(r);
                                    foreach (var d in r) trackedPsd.Add(d.id);
                                }
                            }
                        }
                        // Also track ProjectSectionDescriptionMapping by subsection id (T4)
                        var createdPsdForSub = new List<ProjectSectionDescriptionMappingDocument>();
                        if (createdPsub.Count > 0)
                        {
                            using (var it = psdContainer.GetItemQueryIterator<ProjectSectionDescriptionMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.SectionId = @subId").WithParameter("@subId", createdPsub[0].Id)))
                            {
                                while (it.HasMoreResults)
                                {
                                    var r = await it.ReadNextAsync();
                                    createdPsdForSub.AddRange(r);
                                    foreach (var d in r) trackedPsd.Add(d.id);
                                }
                            }
                        }
                        bool psdOk = createdPsd.Count == 2 
                            && createdPsd.Any(d => d.CategoryId == 101 && d.id == $"{psm1!.SectionMappingId}|101" && d.SectionId == psm1.SectionMappingId && d.CreatedBy == "ParityUserBatch8" && d.ModifiedBy == "ParityUserBatch8" && d.CreatedOn.HasValue && d.ModifiedOn.HasValue && d.IsActive == true && d.IsDeleted == false)
                            && createdPsd.Any(d => d.CategoryId == 102 && d.id == $"{psm1!.SectionMappingId}|102" && d.SectionId == psm1.SectionMappingId && d.CreatedBy == "ParityUserBatch8" && d.ModifiedBy == "ParityUserBatch8" && d.CreatedOn.HasValue && d.ModifiedOn.HasValue && d.IsActive == true && d.IsDeleted == false)
                            && createdPsdForSub.Count == 2
                            && createdPsdForSub.Any(d => d.CategoryId == 101 && d.id == $"{createdPsub[0].Id}|101" && d.SectionId == createdPsub[0].Id && d.CreatedBy == "ParityUserBatch8" && d.ModifiedBy == "ParityUserBatch8" && d.CreatedOn.HasValue && d.ModifiedOn.HasValue && d.IsActive == true && d.IsDeleted == false)
                            && createdPsdForSub.Any(d => d.CategoryId == 102 && d.id == $"{createdPsub[0].Id}|102" && d.SectionId == createdPsub[0].Id && d.CreatedBy == "ParityUserBatch8" && d.ModifiedBy == "ParityUserBatch8" && d.CreatedOn.HasValue && d.ModifiedOn.HasValue && d.IsActive == true && d.IsDeleted == false);

                        // f) ProjectSubSectionDescriptionMapping: ...|301 exists! (T2 & T4: fill trackedPsdSub)
                        var createdPsdSub = new List<ProjectSubSectionDescriptionMappingDocument>();
                        if (createdPsub.Count > 0)
                        {
                            using (var it = psdSubContainer.GetItemQueryIterator<ProjectSubSectionDescriptionMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.SubSectionId = @subId").WithParameter("@subId", createdPsub[0].Id)))
                            {
                                while (it.HasMoreResults)
                                {
                                    var r = await it.ReadNextAsync();
                                    createdPsdSub.AddRange(r);
                                    foreach (var d in r) trackedPsdSub.Add(d.id);
                                }
                            }
                        }
                        bool psdSubOk = createdPsdSub.Count == 1 
                            && createdPsdSub[0].id == $"{createdPsub[0].Id}|301"
                            && createdPsdSub[0].CategoryId == 301
                            && createdPsdSub[0].SubSectionId == createdPsub[0].Id
                            && createdPsdSub[0].CreatedBy == "ParityUserBatch8"
                            && createdPsdSub[0].ModifiedBy == "ParityUserBatch8"
                            && createdPsdSub[0].CreatedOn.HasValue
                            && createdPsdSub[0].ModifiedOn.HasValue
                            && createdPsdSub[0].IsActive == true
                            && createdPsdSub[0].IsDeleted == false;

                        // g) ProjectSectionGradingMapping: ...|201 exists!
                        var createdPsg = new List<ProjectSectionGradingMappingDocument>();
                        if (psm1 != null)
                        {
                            using (var it = psgContainer.GetItemQueryIterator<ProjectSectionGradingMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.SectionId = @secId").WithParameter("@secId", psm1.SectionMappingId)))
                            {
                                while (it.HasMoreResults)
                                {
                                    var r = await it.ReadNextAsync();
                                    createdPsg.AddRange(r);
                                    foreach (var d in r) trackedPsg.Add(d.id);
                                }
                            }
                        }
                        bool psgOk = createdPsg.Count == 1 
                            && createdPsg[0].id == $"{psm1!.SectionMappingId}|201"
                            && createdPsg[0].CategoryId == 201
                            && createdPsg[0].SectionId == psm1.SectionMappingId
                            && createdPsg[0].CreatedBy == "ParityUserBatch8"
                            && createdPsg[0].ModifiedBy == "ParityUserBatch8"
                            && createdPsg[0].CreatedOn.HasValue
                            && createdPsg[0].ModifiedOn.HasValue
                            && createdPsg[0].IsActive == true
                            && createdPsg[0].IsDeleted == false;

                        bool allCreateTmplReadbacksOk = rpOk && psmOk && ptsOk && psubOk && psdOk && psdSubOk && psgOk;
                        writeSteps.Add(new { step = "CreateProjectReportTemplate.Readbacks", passed = allCreateTmplReadbacksOk, rpOk, psmOk, ptsOk, psubOk, psdOk, psdSubOk, psgOk });
                        if (!allCreateTmplReadbacksOk) allPassed = false;

                        // 2. Rollback test for CreateProjectReportTemplate
                        int rollbackVtId = -998;
                        int rollbackProjId = -998;
                        var rbPart = new VesselTypePartMappingDocument
                        {
                            id = "-9981",
                            VesselTypePartMappingId = -9981,
                            VesselTypeId = rollbackVtId,
                            PartName = "Rollback Part",
                            SequenceNo = 1,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "Tester",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "Tester",
                            ModifiedOn = DateTime.UtcNow.Date,
                            RequiredInReport = true
                        };
                        await vtpContainer.CreateItemAsync(rbPart, new PartitionKey((double)rollbackVtId));
                        trackedVtp.Add((rbPart.id, rollbackVtId));

                        var rbSecGuid1 = Guid.NewGuid();
                        var rbSecGuid2 = Guid.NewGuid();
                        var rbSec1 = new VesselTypeNormalSectionMappingDocument
                        {
                            id = rbSecGuid1.ToString(),
                            SectionMappingId = rbSecGuid1.ToString(),
                            VesselTypePartMappingId = -9981,
                            VesselTypeId = rollbackVtId,
                            SectionName = "Same Section Name",
                            SubHeader = "Sub",
                            Order = 1,
                            HasSubSection = true, // Y1(a): HasSubSection = true so subsection steps run
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "Tester",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "Tester",
                            ModifiedOn = DateTime.UtcNow.Date,
                            RequiredInReport = true
                        };
                        var rbSec2 = new VesselTypeNormalSectionMappingDocument
                        {
                            id = rbSecGuid2.ToString(),
                            SectionMappingId = rbSecGuid2.ToString(),
                            VesselTypePartMappingId = -9981,
                            VesselTypeId = rollbackVtId,
                            SectionName = "Same Section Name",
                            SubHeader = "Sub",
                            Order = 2,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "Tester",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "Tester",
                            ModifiedOn = DateTime.UtcNow.Date,
                            RequiredInReport = true
                        };
                        await vnsContainer.CreateItemAsync(rbSec1, new PartitionKey((double)rollbackVtId));
                        trackedVns.Add((rbSec1.id, rollbackVtId));
                        await vnsContainer.CreateItemAsync(rbSec2, new PartitionKey((double)rollbackVtId));
                        trackedVns.Add((rbSec2.id, rollbackVtId));

                        // Y1(a): Subsection under rbSec1
                        var rbSubSecGuid = Guid.NewGuid();
                        var rbSubSecDoc = new VesselTypeSubSectionDocument
                        {
                            id = rbSubSecGuid.ToString(),
                            Id = rbSubSecGuid.ToString(),
                            VesselTypeSectionId = rbSecGuid1.ToString(),
                            SectionName = "Rollback Test Subsection",
                            OrderNumber = 1,
                            Totalcards = 2,
                            PlaceholderCount = 1,
                            FileNameCount = 1,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "Tester",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "Tester",
                            ModifiedOn = DateTime.UtcNow.Date
                        };
                        await vsubContainer.CreateItemAsync(rbSubSecDoc, new PartitionKey(rbSubSecDoc.id));
                        trackedVsub.Add(rbSubSecDoc.id);

                        // Y1(a): Subsection description mapping
                        var rbVsubDescDoc = new VesselTypeSubSectionDescriptionMappingDocument
                        {
                            id = $"{rbSubSecGuid}|301",
                            SubSectionId = rbSubSecGuid.ToString(),
                            CategoryId = 301,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "Tester",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "Tester",
                            ModifiedOn = DateTime.UtcNow.Date
                        };
                        await vsubDescContainer.CreateItemAsync(rbVsubDescDoc, new PartitionKey(rbVsubDescDoc.id));
                        trackedVsubDesc.Add(rbVsubDescDoc.id);

                        // Y1(a): Section description mapping
                        var rbVsdDoc = new VesselTypeSectionDescriptionMappingDocument
                        {
                            id = $"{rbSecGuid1}|101",
                            SectionId = rbSecGuid1.ToString(),
                            CategoryId = 101,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "Tester",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "Tester",
                            ModifiedOn = DateTime.UtcNow.Date
                        };
                        await vsdContainer.CreateItemAsync(rbVsdDoc, new PartitionKey(rbVsdDoc.id));
                        trackedVsd.Add(rbVsdDoc.id);

                        // Y1(a): Section grading mappings (forces duplicate mapId at step 8 - the LAST step!)
                        var rbVsg1 = new VesselTypeSectionGradingMappingDocument
                        {
                            id = $"{rbSecGuid1}|301",
                            SectionId = rbSecGuid1.ToString(),
                            CategoryId = 301,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "Tester",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "Tester",
                            ModifiedOn = DateTime.UtcNow.Date
                        };
                        var rbVsg2 = new VesselTypeSectionGradingMappingDocument
                        {
                            id = $"{rbSecGuid2}|301",
                            SectionId = rbSecGuid2.ToString(),
                            CategoryId = 301,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "Tester",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "Tester",
                            ModifiedOn = DateTime.UtcNow.Date
                        };
                        await vsgContainer.CreateItemAsync(rbVsg1, new PartitionKey(rbVsg1.id));
                        trackedVsg.Add(rbVsg1.id);
                        await vsgContainer.CreateItemAsync(rbVsg2, new PartitionKey(rbVsg2.id));
                        trackedVsg.Add(rbVsg2.id);

                        var rollbackRes = await cosmosReportPartRepo.CreateProjectReportTemplate(rollbackVtId, "RbUser", rollbackProjId, false, 0);
                        bool rollbackTriggered = !rollbackRes;

                        // Y1(b) & (c): Count by -998 project/section/subsection ids, track any doc found for cleanup
                        var rbRpDocs = new List<ReportPartDocument>();
                        using (var it = rpContainer.GetItemQueryIterator<ReportPartDocument>(new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = -998"), requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(-998.0) }))
                        {
                            while (it.HasMoreResults)
                            {
                                var r = await it.ReadNextAsync();
                                rbRpDocs.AddRange(r);
                                foreach (var d in r) trackedRp.Add((d.id, -998));
                            }
                        }
                        int rbRpCount = rbRpDocs.Count;

                        var rbPsmDocs = new List<ProjectSectionMappingDocument>();
                        using (var it = psmContainer.GetItemQueryIterator<ProjectSectionMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = -998"), requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(-998.0) }))
                        {
                            while (it.HasMoreResults)
                            {
                                var r = await it.ReadNextAsync();
                                rbPsmDocs.AddRange(r);
                                foreach (var d in r) trackedPsm.Add((d.id, -998));
                            }
                        }
                        int rbPsmCount = rbPsmDocs.Count;

                        var rbPtsDocs = new List<ProjectTankSectionMappingDocument>();
                        using (var it = ptsContainer.GetItemQueryIterator<ProjectTankSectionMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = -998"), requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(-998.0) }))
                        {
                            while (it.HasMoreResults)
                            {
                                var r = await it.ReadNextAsync();
                                rbPtsDocs.AddRange(r);
                                foreach (var d in r) trackedPts.Add((d.id, -998));
                            }
                        }
                        int rbPtsCount = rbPtsDocs.Count;

                        var setupSectionIds = new List<string> { rbSecGuid1.ToString(), rbSecGuid2.ToString() };
                        setupSectionIds.AddRange(rbPsmDocs.Select(s => s.SectionMappingId).Where(id => !string.IsNullOrEmpty(id))!);

                        var setupSubSecIds = new List<string> { rbSubSecGuid.ToString() };

                        var rbPsubDocs = new List<ProjectSubSectionDocument>();
                        using (var it = psubContainer.GetItemQueryIterator<ProjectSubSectionDocument>(new QueryDefinition(
                            "SELECT * FROM c WHERE ARRAY_CONTAINS(@secIds, c.ProjectSectionId) OR c.SectionName = 'Rollback Test Subsection'")
                            .WithParameter("@secIds", setupSectionIds)))
                        {
                            while (it.HasMoreResults)
                            {
                                var r = await it.ReadNextAsync();
                                rbPsubDocs.AddRange(r);
                                foreach (var d in r) trackedPsub.Add(d.id);
                            }
                        }
                        int rbPsubCount = rbPsubDocs.Count;
                        setupSubSecIds.AddRange(rbPsubDocs.Select(s => s.Id).Where(id => !string.IsNullOrEmpty(id))!);

                        var allSecAndSubIds = setupSectionIds.Concat(setupSubSecIds).Distinct().ToList();

                        var rbPsdDocs = new List<ProjectSectionDescriptionMappingDocument>();
                        using (var it = psdContainer.GetItemQueryIterator<ProjectSectionDescriptionMappingDocument>(new QueryDefinition(
                            "SELECT * FROM c WHERE ARRAY_CONTAINS(@allIds, c.SectionId)")
                            .WithParameter("@allIds", allSecAndSubIds)))
                        {
                            while (it.HasMoreResults)
                            {
                                var r = await it.ReadNextAsync();
                                rbPsdDocs.AddRange(r);
                                foreach (var d in r) trackedPsd.Add(d.id);
                            }
                        }
                        int rbPsdCount = rbPsdDocs.Count;

                        var rbPsdSubDocs = new List<ProjectSubSectionDescriptionMappingDocument>();
                        using (var it = psdSubContainer.GetItemQueryIterator<ProjectSubSectionDescriptionMappingDocument>(new QueryDefinition(
                            "SELECT * FROM c WHERE ARRAY_CONTAINS(@subIds, c.SubSectionId)")
                            .WithParameter("@subIds", setupSubSecIds)))
                        {
                            while (it.HasMoreResults)
                            {
                                var r = await it.ReadNextAsync();
                                rbPsdSubDocs.AddRange(r);
                                foreach (var d in r) trackedPsdSub.Add(d.id);
                            }
                        }
                        int rbPsdSubCount = rbPsdSubDocs.Count;

                        var rbPsgDocs = new List<ProjectSectionGradingMappingDocument>();
                        using (var it = psgContainer.GetItemQueryIterator<ProjectSectionGradingMappingDocument>(new QueryDefinition(
                            "SELECT * FROM c WHERE ARRAY_CONTAINS(@secIds, c.SectionId)")
                            .WithParameter("@secIds", setupSectionIds)))
                        {
                            while (it.HasMoreResults)
                            {
                                var r = await it.ReadNextAsync();
                                rbPsgDocs.AddRange(r);
                                foreach (var d in r) trackedPsg.Add(d.id);
                            }
                        }
                        int rbPsgCount = rbPsgDocs.Count;

                        bool rollbackZeroDocs = rbRpCount == 0 && rbPsmCount == 0 && rbPtsCount == 0 && rbPsubCount == 0 && rbPsdCount == 0 && rbPsdSubCount == 0 && rbPsgCount == 0;

                        bool rollbackPassed = rollbackTriggered && rollbackZeroDocs;
                        writeSteps.Add(new { step = "CreateProjectReportTemplate.RollbackOnConflict", passed = rollbackPassed, rollbackTriggered, rollbackZeroDocs, rbRpCount, rbPsmCount, rbPtsCount, rbPsubCount, rbPsdCount, rbPsdSubCount, rbPsgCount });
                        if (!rollbackPassed) allPassed = false;

                        // 3. CreateReportPart (Helper 14: Sp_Create_VesseltypeReportConfig)
                        // a) New part, and part update
                        var testNewPartName = "Batch8 New Part A";
                        var reportConfig1 = new VesselTypeReportConfigList
                        {
                            reportParts = new List<VesselTypePartMapping>
                            {
                                new VesselTypePartMapping
                                {
                                    vesselTypePartMappingId = 0,
                                    VesselTypeId = testVtId,
                                    PartName = testNewPartName,
                                    SequenceNo = 10,
                                    IsActive = true,
                                    CreatedBy = "PartCreator"
                                }
                            }
                        };
                        var createPartRes1 = await cosmosReportPartRepo.CreateReportPart(reportConfig1, testVtId, "Modifier");
                        var partsForVt = new List<VesselTypePartMappingDocument>();
                        using (var it = vtpContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vtid AND c.PartName = @pname").WithParameter("@vtid", testVtId).WithParameter("@pname", testNewPartName), requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)testVtId) }))
                        {
                            while (it.HasMoreResults)
                            {
                                var r = await it.ReadNextAsync();
                                partsForVt.AddRange(r);
                                foreach (var d in r) trackedVtp.Add((d.id, testVtId));
                            }
                        }
                        var newPartDoc = partsForVt.FirstOrDefault();
                        bool newPartCreated = createPartRes1 && newPartDoc != null
                            && newPartDoc.id != null && newPartDoc.VesselTypePartMappingId != 0
                            && newPartDoc.VesselTypeId == testVtId
                            && newPartDoc.PartName == testNewPartName
                            && newPartDoc.SequenceNo == 10
                            && newPartDoc.IsActive == true
                            && newPartDoc.IsDeleted == false
                            && newPartDoc.CreatedBy == "Modifier"
                            && newPartDoc.CreatedOn.HasValue
                            && newPartDoc.ModifiedBy == "Modifier"
                            && newPartDoc.ModifiedOn.HasValue
                            && newPartDoc.RequiredInReport == true;

                        bool partUpdated = false;
                        if (newPartDoc != null)
                        {
                            var updatePartConfig = new VesselTypeReportConfigList
                            {
                                reportParts = new List<VesselTypePartMapping>
                                {
                                    new VesselTypePartMapping
                                    {
                                        vesselTypePartMappingId = newPartDoc.VesselTypePartMappingId,
                                        VesselTypeId = testVtId,
                                        PartName = testNewPartName,
                                        SequenceNo = 20,
                                        IsActive = true,
                                        CreatedBy = "PartUpdater"
                                    }
                                }
                            };
                            var updatePartRes = await cosmosReportPartRepo.CreateReportPart(updatePartConfig, testVtId, "Modifier2");
                            var updatedPartDoc = await SafeReadAsync<VesselTypePartMappingDocument>(vtpContainer, newPartDoc.id, new PartitionKey((double)testVtId));
                            partUpdated = updatePartRes && updatedPartDoc != null
                                && updatedPartDoc.id == newPartDoc.id
                                && updatedPartDoc.VesselTypePartMappingId == newPartDoc.VesselTypePartMappingId
                                && updatedPartDoc.VesselTypeId == testVtId
                                && updatedPartDoc.PartName == testNewPartName
                                && updatedPartDoc.SequenceNo == 20
                                && updatedPartDoc.IsActive == true
                                && updatedPartDoc.IsDeleted == false
                                && updatedPartDoc.CreatedBy == "Modifier"
                                && updatedPartDoc.CreatedOn.HasValue
                                && updatedPartDoc.ModifiedBy == "PartUpdater"
                                && updatedPartDoc.ModifiedOn.HasValue
                                && updatedPartDoc.RequiredInReport == true;
                        }
                        bool partMergeOk = newPartCreated && partUpdated;
                        writeSteps.Add(new { step = "CreateReportPart.PartNewAndUpdate", passed = partMergeOk, newPartCreated, partUpdated });
                        if (!partMergeOk) allPassed = false;

                        // b) Two new sections in one part -> same [Order]
                        var reportConfigTwoSecs = new VesselTypeReportConfigList
                        {
                            normalSectionMappings = new List<VesselTypeSectionMapping>
                            {
                                new VesselTypeSectionMapping
                                {
                                    VesselTypeNormalSectionMappingId = Guid.Empty,
                                    VesselTypePartMappingId = 0,
                                    VesselTypeId = testVtId,
                                    PartName = testNewPartName,
                                    SectionName = "Order Test Sec 1",
                                    SubHeader = "H1",
                                    TotalCards = 2,
                                    IsActive = true,
                                    CreatedBy = "Tester"
                                },
                                new VesselTypeSectionMapping
                                {
                                    VesselTypeNormalSectionMappingId = Guid.Empty,
                                    VesselTypePartMappingId = 0,
                                    VesselTypeId = testVtId,
                                    PartName = testNewPartName,
                                    SectionName = "Order Test Sec 2",
                                    SubHeader = "H2",
                                    TotalCards = 2,
                                    IsActive = true,
                                    CreatedBy = "Tester"
                                }
                            }
                        };
                        var createTwoSecsRes = await cosmosReportPartRepo.CreateReportPart(reportConfigTwoSecs, testVtId, "Modifier");
                        var createdTwoSecs = new List<VesselTypeNormalSectionMappingDocument>();
                        using (var it = vnsContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vtid AND (c.SectionName = 'Order Test Sec 1' OR c.SectionName = 'Order Test Sec 2')").WithParameter("@vtid", testVtId), requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)testVtId) }))
                        {
                            while (it.HasMoreResults)
                            {
                                var r = await it.ReadNextAsync();
                                createdTwoSecs.AddRange(r);
                                foreach (var d in r) trackedVns.Add((d.id, testVtId));
                            }
                        }
                        bool twoSecsSameOrder = createTwoSecsRes && createdTwoSecs.Count == 2
                            && createdTwoSecs.All(d => d.id != null && d.SectionMappingId == d.id && Guid.TryParse(d.id, out _) && d.VesselTypeId == testVtId && d.VesselTypePartMappingId == newPartDoc.VesselTypePartMappingId && d.TotalCards == 2 && d.IsActive == true && d.IsDeleted == false && d.CreatedBy == "Modifier" && d.CreatedOn.HasValue && d.ModifiedBy == "Modifier" && d.ModifiedOn.HasValue && d.RequiredInReport == false)
                            && createdTwoSecs.Any(d => d.SectionName == "Order Test Sec 1" && d.SubHeader == "H1" && d.Order == 1)
                            && createdTwoSecs.Any(d => d.SectionName == "Order Test Sec 2" && d.SubHeader == "H2" && d.Order == 1);
                        writeSteps.Add(new { step = "CreateReportPart.TwoNewSectionsSameOrder", passed = twoSecsSameOrder, count = createdTwoSecs.Count, orders = createdTwoSecs.Select(s => s.Order).ToList() });
                        if (!twoSecsSameOrder) allPassed = false;

                        // c) Section update
                        bool secUpdated = false;
                        if (createdTwoSecs.Count > 0)
                        {
                            var targetSec = createdTwoSecs[0];
                            var updateSecConfig = new VesselTypeReportConfigList
                            {
                                normalSectionMappings = new List<VesselTypeSectionMapping>
                                {
                                    new VesselTypeSectionMapping
                                    {
                                        VesselTypeNormalSectionMappingId = Guid.Parse(targetSec.SectionMappingId!),
                                        VesselTypePartMappingId = targetSec.VesselTypePartMappingId,
                                        VesselTypeId = testVtId,
                                        PartName = testNewPartName,
                                        SectionName = "Updated Sec Name",
                                        SubHeader = "Updated Header",
                                        TotalCards = 10,
                                        IsActive = true,
                                        CreatedBy = "SecUpdater"
                                    }
                                }
                            };
                            var updateSecRes = await cosmosReportPartRepo.CreateReportPart(updateSecConfig, testVtId, "Modifier");
                            var updatedSecDoc = await SafeReadAsync<VesselTypeNormalSectionMappingDocument>(vnsContainer, targetSec.id, new PartitionKey((double)testVtId));
                            secUpdated = updateSecRes && updatedSecDoc != null
                                && updatedSecDoc.id == targetSec.id
                                && updatedSecDoc.SectionMappingId == targetSec.SectionMappingId
                                && updatedSecDoc.VesselTypeId == testVtId
                                && updatedSecDoc.VesselTypePartMappingId == targetSec.VesselTypePartMappingId
                                && updatedSecDoc.SectionName == "Updated Sec Name"
                                && updatedSecDoc.SubHeader == "Updated Header"
                                && updatedSecDoc.TotalCards == 10
                                && updatedSecDoc.Order == targetSec.Order
                                && updatedSecDoc.IsActive == true
                                && updatedSecDoc.IsDeleted == false
                                && updatedSecDoc.CreatedBy == targetSec.CreatedBy
                                && updatedSecDoc.CreatedOn.HasValue
                                && updatedSecDoc.ModifiedBy == "Modifier"
                                && updatedSecDoc.ModifiedOn.HasValue
                                && updatedSecDoc.RequiredInReport == targetSec.RequiredInReport;
                        }
                        writeSteps.Add(new { step = "CreateReportPart.SectionUpdate", passed = secUpdated });
                        if (!secUpdated) allPassed = false;

                        // d) New tank section, and tank section update (with PartName set)
                        var reportConfigTank = new VesselTypeReportConfigList
                        {
                            tankSectionMappings = new List<VesselTypeTankSectionMapping>
                            {
                                new VesselTypeTankSectionMapping
                                {
                                    VessselTypeTankSectionMappingId = Guid.Empty,
                                    VesselTypePartMappingId = 0,
                                    VesselTypeId = testVtId,
                                    PartName = testNewPartName,
                                    TankTypeId = 801,
                                    TotalCards = 4,
                                    IsActive = true,
                                    CreatedBy = "TankCreator"
                                }
                            }
                        };
                        var createTankRes = await cosmosReportPartRepo.CreateReportPart(reportConfigTank, testVtId, "Modifier");
                        var createdTanks = new List<VesselTypeTankSectionMappingDocument>();
                        using (var it = vtsContainer.GetItemQueryIterator<VesselTypeTankSectionMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vtid AND c.TankTypeId = 801").WithParameter("@vtid", testVtId), requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)testVtId) }))
                        {
                            while (it.HasMoreResults)
                            {
                                var r = await it.ReadNextAsync();
                                createdTanks.AddRange(r);
                                foreach (var d in r) trackedVts.Add((d.id, testVtId));
                            }
                        }
                        var newTankDoc = createdTanks.FirstOrDefault();
                        bool tankCreated = createTankRes && newTankDoc != null
                            && newTankDoc.id != null && newTankDoc.VesselTypeTankSectionMappingId == newTankDoc.id && Guid.TryParse(newTankDoc.id, out _)
                            && newTankDoc.VesselTypeId == testVtId
                            && newTankDoc.VesselTypePartMappingId == newPartDoc.VesselTypePartMappingId
                            && newTankDoc.TankTypeId == 801
                            && newTankDoc.TotalCards == 4
                            && newTankDoc.IsActive == true
                            && newTankDoc.IsDeleted == false
                            && newTankDoc.CreatedBy == "Modifier"
                            && newTankDoc.CreatedOn.HasValue
                            && newTankDoc.ModifiedBy == "Modifier"
                            && newTankDoc.ModifiedOn.HasValue;

                        bool tankUpdated = false;
                        if (newTankDoc != null)
                        {
                            var updateTankConfig = new VesselTypeReportConfigList
                            {
                                tankSectionMappings = new List<VesselTypeTankSectionMapping>
                                {
                                    new VesselTypeTankSectionMapping
                                    {
                                        VessselTypeTankSectionMappingId = Guid.Parse(newTankDoc.VesselTypeTankSectionMappingId!),
                                        VesselTypePartMappingId = newTankDoc.VesselTypePartMappingId,
                                        VesselTypeId = testVtId,
                                        PartName = testNewPartName,
                                        TankTypeId = 801,
                                        TotalCards = 8,
                                        IsActive = true,
                                        CreatedBy = "TankUpdater"
                                    }
                                }
                            };
                            var updateTankRes = await cosmosReportPartRepo.CreateReportPart(updateTankConfig, testVtId, "Modifier");
                            var updatedTankDoc = await SafeReadAsync<VesselTypeTankSectionMappingDocument>(vtsContainer, newTankDoc.id, new PartitionKey((double)testVtId));
                            tankUpdated = updateTankRes && updatedTankDoc != null
                                && updatedTankDoc.id == newTankDoc.id
                                && updatedTankDoc.VesselTypeTankSectionMappingId == newTankDoc.VesselTypeTankSectionMappingId
                                && updatedTankDoc.VesselTypeId == testVtId
                                && updatedTankDoc.VesselTypePartMappingId == newTankDoc.VesselTypePartMappingId
                                && updatedTankDoc.TankTypeId == 801
                                && updatedTankDoc.TotalCards == 8
                                && updatedTankDoc.IsActive == true
                                && updatedTankDoc.IsDeleted == false
                                && updatedTankDoc.CreatedBy == newTankDoc.CreatedBy
                                && updatedTankDoc.CreatedOn.HasValue
                                && updatedTankDoc.ModifiedBy == "Modifier"
                                && updatedTankDoc.ModifiedOn.HasValue;
                        }
                        bool tankMergeOk = tankCreated && tankUpdated;
                        writeSteps.Add(new { step = "CreateReportPart.TankSectionNewAndUpdate", passed = tankMergeOk, tankCreated, tankUpdated });
                        if (!tankMergeOk) allPassed = false;

                        // e) Subsection CreatedBy null and 'x@y' (Trap 10 & E3)
                        var testSubSecGuidTrap = Guid.NewGuid();
                        var subTrapDoc = new VesselTypeSubSectionDocument
                        {
                            id = testSubSecGuidTrap.ToString(),
                            Id = testSubSecGuidTrap.ToString(),
                            VesselTypeSectionId = testSecGuid1.ToString(),
                            SectionName = "Trap 10 Subsection",
                            OrderNumber = 1,
                            Totalcards = 2,
                            PlaceholderCount = 1,
                            FileNameCount = 1,
                            IsActive = true,
                            IsDeleted = false,
                            CreatedBy = "InitialAuthor",
                            CreatedOn = DateTime.UtcNow.Date,
                            ModifiedBy = "InitialAuthor",
                            ModifiedOn = DateTime.UtcNow.Date
                        };
                        var createdSubTrapResp = await vsubContainer.CreateItemAsync(subTrapDoc, new PartitionKey(subTrapDoc.id));
                        trackedVsub.Add(subTrapDoc.id);

                        // Call 1: CreatedBy = null -> expect true, ModifiedBy == "0"
                        var validSubConfig = new VesselTypeReportConfigList
                        {
                            normalSubSectionMappings = new List<VesselTypeSubSectionMapping>
                            {
                                new VesselTypeSubSectionMapping
                                {
                                    SubSectionId = testSubSecGuidTrap,
                                    SectionId = testSecGuid1,
                                    SectionName = "Trap 10 Updated",
                                    TotalCards = 5,
                                    IsActive = true,
                                    IsDeleted = false,
                                    CreatedBy = null
                                }
                            }
                        };
                        var subRes1 = await cosmosReportPartRepo.CreateReportPart(validSubConfig, testVtId, "Modifier");
                        var readSub1 = await SafeReadAsync<VesselTypeSubSectionDocument>(vsubContainer, subTrapDoc.id, new PartitionKey(subTrapDoc.id));
                        var etagAfterCall1 = readSub1 != null ? (await vsubContainer.ReadItemAsync<VesselTypeSubSectionDocument>(subTrapDoc.id, new PartitionKey(subTrapDoc.id))).ETag : null;
                        bool trap10ValidOk = subRes1 && readSub1 != null
                            && readSub1.id == subTrapDoc.id && readSub1.Id == subTrapDoc.Id
                            && readSub1.VesselTypeSectionId == testSecGuid1.ToString()
                            && readSub1.SectionName == "Trap 10 Updated"
                            && readSub1.Totalcards == 5
                            && readSub1.OrderNumber == subTrapDoc.OrderNumber
                            && readSub1.IsActive == true
                            && readSub1.IsDeleted == false
                            && readSub1.CreatedBy == "InitialAuthor"
                            && readSub1.CreatedOn.HasValue
                            && readSub1.ModifiedBy == "0"
                            && readSub1.ModifiedOn.HasValue;

                        // Call 2: CreatedBy = "x@y" -> expect false, document unchanged (etag equal)
                        var invalidSubConfig = new VesselTypeReportConfigList
                        {
                            normalSubSectionMappings = new List<VesselTypeSubSectionMapping>
                            {
                                new VesselTypeSubSectionMapping
                                {
                                    SubSectionId = testSubSecGuidTrap,
                                    SectionId = testSecGuid1,
                                    SectionName = "Should Not Apply",
                                    TotalCards = 99,
                                    IsActive = true,
                                    IsDeleted = false,
                                    CreatedBy = "x@y"
                                }
                            }
                        };
                        var subRes2 = await cosmosReportPartRepo.CreateReportPart(invalidSubConfig, testVtId, "Modifier");
                        var readSub2Resp = await vsubContainer.ReadItemAsync<VesselTypeSubSectionDocument>(subTrapDoc.id, new PartitionKey(subTrapDoc.id));
                        bool trap10InvalidOk = !subRes2 && readSub2Resp.ETag == etagAfterCall1 && readSub2Resp.Resource.Totalcards == 5;

                        bool trap10TotalOk = trap10ValidOk && trap10InvalidOk;
                        writeSteps.Add(new { step = "CreateReportPart.Trap10SubsectionBitConversion", passed = trap10TotalOk, trap10ValidOk, trap10InvalidOk, subRes1, subRes2, modifiedBy = readSub1?.ModifiedBy });
                        if (!trap10TotalOk) allPassed = false;

                        // f) Too-long PartName (> 100 on part or > 50 on section) -> false and nothing written (T6: assert count unchanged)
                        int countBeforeTooLong = await GetCountAsync(vtpContainer, "SELECT VALUE COUNT(1) FROM c WHERE c.VesselTypeId = -999", new PartitionKey((double)testVtId));
                        var tooLongConfig = new VesselTypeReportConfigList
                        {
                            reportParts = new List<VesselTypePartMapping>
                            {
                                new VesselTypePartMapping
                                {
                                    vesselTypePartMappingId = 0,
                                    VesselTypeId = testVtId,
                                    PartName = new string('A', 105),
                                    SequenceNo = 99,
                                    IsActive = true
                                }
                            }
                        };
                        var tooLongRes = await cosmosReportPartRepo.CreateReportPart(tooLongConfig, testVtId, "Modifier");
                        int countAfterTooLong = await GetCountAsync(vtpContainer, "SELECT VALUE COUNT(1) FROM c WHERE c.VesselTypeId = -999", new PartitionKey((double)testVtId));
                        bool tooLongOk = !tooLongRes && countAfterTooLong == countBeforeTooLong;
                        writeSteps.Add(new { step = "CreateReportPart.TooLongPartNameRejection", passed = tooLongOk, countBefore = countBeforeTooLong, countAfter = countAfterTooLong });
                        if (!tooLongOk) allPassed = false;

                        // g) Two source rows for one target -> false (T6: assert ETag unchanged)
                        if (newPartDoc != null)
                        {
                            var etagBeforeTwoSrc = (await vtpContainer.ReadItemAsync<VesselTypePartMappingDocument>(newPartDoc.id, new PartitionKey((double)testVtId))).ETag;
                            var twoSourceConfig = new VesselTypeReportConfigList
                            {
                                reportParts = new List<VesselTypePartMapping>
                                {
                                    new VesselTypePartMapping
                                    {
                                        vesselTypePartMappingId = newPartDoc.VesselTypePartMappingId,
                                        VesselTypeId = testVtId,
                                        PartName = "Name A",
                                        SequenceNo = 1
                                    },
                                    new VesselTypePartMapping
                                    {
                                        vesselTypePartMappingId = newPartDoc.VesselTypePartMappingId,
                                        VesselTypeId = testVtId,
                                        PartName = "Name B",
                                        SequenceNo = 2
                                    }
                                }
                            };
                            var twoSourceRes = await cosmosReportPartRepo.CreateReportPart(twoSourceConfig, testVtId, "Modifier");
                            var etagAfterTwoSrc = (await vtpContainer.ReadItemAsync<VesselTypePartMappingDocument>(newPartDoc.id, new PartitionKey((double)testVtId))).ETag;
                            bool twoSourceOk = !twoSourceRes && etagAfterTwoSrc == etagBeforeTwoSrc;
                            writeSteps.Add(new { step = "CreateReportPart.TwoSourceOneTargetRejection", passed = twoSourceOk });
                            if (!twoSourceOk) allPassed = false;
                        }

                        // h) ModifiedBy source longer than 50 -> false (T6: assert ETag unchanged)
                        if (newPartDoc != null)
                        {
                            var etagBeforeLongMod = (await vtpContainer.ReadItemAsync<VesselTypePartMappingDocument>(newPartDoc.id, new PartitionKey((double)testVtId))).ETag;
                            var longModifiedByConfig = new VesselTypeReportConfigList
                            {
                                reportParts = new List<VesselTypePartMapping>
                                {
                                    new VesselTypePartMapping
                                    {
                                        vesselTypePartMappingId = newPartDoc.VesselTypePartMappingId,
                                        VesselTypeId = testVtId,
                                        PartName = testNewPartName,
                                        CreatedBy = new string('M', 55)
                                    }
                                }
                            };
                            var longModRes = await cosmosReportPartRepo.CreateReportPart(longModifiedByConfig, testVtId, "Modifier");
                            var etagAfterLongMod = (await vtpContainer.ReadItemAsync<VesselTypePartMappingDocument>(newPartDoc.id, new PartitionKey((double)testVtId))).ETag;
                            bool longModOk = !longModRes && etagAfterLongMod == etagBeforeLongMod;
                            writeSteps.Add(new { step = "CreateReportPart.LongModifiedByRejection", passed = longModOk });
                            if (!longModOk) allPassed = false;
                        }

                        // i) R8a: Two sources with no existing target insert both rows (T6)
                        var r8aConfig = new VesselTypeReportConfigList
                        {
                            reportParts = new List<VesselTypePartMapping>
                            {
                                new VesselTypePartMapping
                                {
                                    vesselTypePartMappingId = 0,
                                    VesselTypeId = testVtId,
                                    PartName = "R8a Test Part 1",
                                    SequenceNo = 31,
                                    IsActive = true,
                                    CreatedBy = "R8aTester"
                                },
                                new VesselTypePartMapping
                                {
                                    vesselTypePartMappingId = 0,
                                    VesselTypeId = testVtId,
                                    PartName = "R8a Test Part 2",
                                    SequenceNo = 32,
                                    IsActive = true,
                                    CreatedBy = "R8aTester"
                                }
                            }
                        };
                        var r8aRes = await cosmosReportPartRepo.CreateReportPart(r8aConfig, testVtId, "Modifier");
                        var r8aDocs = new List<VesselTypePartMappingDocument>();
                        using (var it = vtpContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vtid AND (c.PartName = 'R8a Test Part 1' OR c.PartName = 'R8a Test Part 2')").WithParameter("@vtid", testVtId), requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)testVtId) }))
                        {
                            while (it.HasMoreResults)
                            {
                                var r = await it.ReadNextAsync();
                                r8aDocs.AddRange(r);
                                foreach (var d in r) trackedVtp.Add((d.id, testVtId));
                            }
                        }
                        bool r8aOk = r8aRes && r8aDocs.Count == 2;
                        writeSteps.Add(new { step = "CreateReportPart.R8aTwoSourcesInsert", passed = r8aOk, count = r8aDocs.Count });
                        if (!r8aOk) allPassed = false;

                        // j) R8b: CreatedBy 60 chars on insert succeeds (<= 100 limit) (T6)
                        var r8bUser60 = new string('C', 60);
                        var r8bConfig = new VesselTypeReportConfigList
                        {
                            reportParts = new List<VesselTypePartMapping>
                            {
                                new VesselTypePartMapping
                                {
                                    vesselTypePartMappingId = 0,
                                    VesselTypeId = testVtId,
                                    PartName = "R8b 60Char Part",
                                    SequenceNo = 40,
                                    IsActive = true,
                                    CreatedBy = r8bUser60
                                }
                            }
                        };
                        var r8bRes = await cosmosReportPartRepo.CreateReportPart(r8bConfig, testVtId, "Modifier");
                        var r8bDocs = new List<VesselTypePartMappingDocument>();
                        using (var it = vtpContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vtid AND c.PartName = 'R8b 60Char Part'").WithParameter("@vtid", testVtId), requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)testVtId) }))
                        {
                            while (it.HasMoreResults)
                            {
                                var r = await it.ReadNextAsync();
                                r8bDocs.AddRange(r);
                                foreach (var d in r) trackedVtp.Add((d.id, testVtId));
                            }
                        }
                        bool r8bOk = r8bRes && r8bDocs.Count == 1 && r8bDocs[0].CreatedBy == r8bUser60;
                        writeSteps.Add(new { step = "CreateReportPart.R8bCreatedBy60CharsInsert", passed = r8bOk });
                        if (!r8bOk) allPassed = false;

                        // k) R8d: error in MERGE 1 still runs MERGE 2, method returns false (T6)
                        var r8dConfig = new VesselTypeReportConfigList
                        {
                            reportParts = new List<VesselTypePartMapping>
                            {
                                new VesselTypePartMapping
                                {
                                    vesselTypePartMappingId = 0,
                                    VesselTypeId = testVtId,
                                    PartName = new string('Z', 110), // MERGE 1 fails
                                    SequenceNo = 99,
                                    IsActive = true
                                }
                            },
                            normalSectionMappings = new List<VesselTypeSectionMapping>
                            {
                                new VesselTypeSectionMapping
                                {
                                    VesselTypeNormalSectionMappingId = Guid.Empty,
                                    VesselTypePartMappingId = 0,
                                    VesselTypeId = testVtId,
                                    PartName = testNewPartName,
                                    SectionName = "R8d Valid Section",
                                    SubHeader = "R8dSub",
                                    TotalCards = 3,
                                    IsActive = true,
                                    CreatedBy = "R8dTester"
                                }
                            }
                        };
                        var r8dRes = await cosmosReportPartRepo.CreateReportPart(r8dConfig, testVtId, "Modifier");
                        var r8dSections = new List<VesselTypeNormalSectionMappingDocument>();
                        using (var it = vnsContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vtid AND c.SectionName = 'R8d Valid Section'").WithParameter("@vtid", testVtId), requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)testVtId) }))
                        {
                            while (it.HasMoreResults)
                            {
                                var r = await it.ReadNextAsync();
                                r8dSections.AddRange(r);
                                foreach (var d in r) trackedVns.Add((d.id, testVtId));
                            }
                        }
                        bool r8dOk = !r8dRes && r8dSections.Count == 1;
                        writeSteps.Add(new { step = "CreateReportPart.R8dMerge1FailMerge2Runs", passed = r8dOk, returnsFalse = !r8dRes, sectionCreated = r8dSections.Count == 1 });
                        if (!r8dOk) allPassed = false;
                    }
                    catch (Exception ex)
                    {
                        allPassed = false;
                        writeSteps.Add(new { step = "WriteException", passed = false, error = ex.ToString() });
                    }
                    finally
                    {
                        // Clean up EXACT tracked items only
                        foreach (var item in trackedRp)
                            await TryCleanup($"ReportPart({item.id})", () => rpContainer.DeleteItemAsync<ReportPartDocument>(item.id, new PartitionKey(item.partitionKey)));
                        foreach (var item in trackedPsm)
                            await TryCleanup($"ProjectSectionMapping({item.id})", () => psmContainer.DeleteItemAsync<ProjectSectionMappingDocument>(item.id, new PartitionKey(item.partitionKey)));
                        foreach (var item in trackedPts)
                            await TryCleanup($"ProjectTankSection({item.id})", () => ptsContainer.DeleteItemAsync<ProjectTankSectionMappingDocument>(item.id, new PartitionKey(item.partitionKey)));
                        foreach (var id in trackedPsub)
                            await TryCleanup($"ProjectSubSection({id})", () => psubContainer.DeleteItemAsync<ProjectSubSectionDocument>(id, new PartitionKey(id)));
                        foreach (var id in trackedPsd)
                            await TryCleanup($"ProjectSectionDescription({id})", () => psdContainer.DeleteItemAsync<ProjectSectionDescriptionMappingDocument>(id, new PartitionKey(id)));
                        foreach (var id in trackedPsdSub)
                            await TryCleanup($"ProjectSubSectionDescription({id})", () => psdSubContainer.DeleteItemAsync<ProjectSubSectionDescriptionMappingDocument>(id, new PartitionKey(id)));
                        foreach (var id in trackedPsg)
                            await TryCleanup($"ProjectSectionGrading({id})", () => psgContainer.DeleteItemAsync<ProjectSectionGradingMappingDocument>(id, new PartitionKey(id)));

                        foreach (var item in trackedVtp)
                            await TryCleanup($"VesselTypePart({item.id})", () => vtpContainer.DeleteItemAsync<VesselTypePartMappingDocument>(item.id, new PartitionKey(item.partitionKey)));
                        foreach (var item in trackedVns)
                            await TryCleanup($"VesselTypeNormalSection({item.id})", () => vnsContainer.DeleteItemAsync<VesselTypeNormalSectionMappingDocument>(item.id, new PartitionKey(item.partitionKey)));
                        foreach (var item in trackedVts)
                            await TryCleanup($"VesselTypeTankSection({item.id})", () => vtsContainer.DeleteItemAsync<VesselTypeTankSectionMappingDocument>(item.id, new PartitionKey(item.partitionKey)));
                        foreach (var id in trackedVsub)
                            await TryCleanup($"VesselTypeSubSection({id})", () => vsubContainer.DeleteItemAsync<VesselTypeSubSectionDocument>(id, new PartitionKey(id)));
                        foreach (var id in trackedVsubDesc)
                            await TryCleanup($"VesselTypeSubSectionDescription({id})", () => vsubDescContainer.DeleteItemAsync<VesselTypeSubSectionDescriptionMappingDocument>(id, new PartitionKey(id)));
                        foreach (var id in trackedVsd)
                            await TryCleanup($"VesselTypeSectionDescription({id})", () => vsdContainer.DeleteItemAsync<VesselTypeSectionDescriptionMappingDocument>(id, new PartitionKey(id)));
                        foreach (var id in trackedVsg)
                            await TryCleanup($"VesselTypeSectionGrading({id})", () => vsgContainer.DeleteItemAsync<VesselTypeSectionGradingMappingDocument>(id, new PartitionKey(id)));
                    }

                    if (cleanupErrors.Count > 0)
                    {
                        allPassed = false;
                        writeSteps.Add(new { step = "CleanupErrors", passed = false, errors = cleanupErrors });
                    }
                }

                allPassed = allPassed && !hadException && checks.All(c =>
                {
                    var prop = c.GetType().GetProperty("equal") ?? c.GetType().GetProperty("match") ?? c.GetType().GetProperty("allMatch");
                    if (prop != null)
                    {
                        var val = prop.GetValue(c);
                        if (val is bool b) return b;
                    }
                    var errProp = c.GetType().GetProperty("error");
                    if (errProp != null) return false;
                    return true;
                });

                return Results.Ok(new
                {
                    batch = 8,
                    allPassed,
                    checksCount = checks.Count,
                    checks,
                    writeSteps
                });
            }).AllowAnonymous();
        }
    }
}
