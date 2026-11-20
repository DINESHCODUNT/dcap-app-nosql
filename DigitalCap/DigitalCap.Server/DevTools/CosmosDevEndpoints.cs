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
            MapParityBatch9(app);
            MapParityBatch10A(app);
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

        private static void MapParityBatch9(WebApplication app)
        {
            app.MapGet("/health/parity/batch9", async (
                string? writeTest,
                IConfiguration config,
                IServiceProvider sp) =>
            {
                var sqlRepo = sp.GetRequiredService<SurveyReportRepository>();
                var cosmosRepo = sp.GetRequiredService<CosmosSurveyReportRepository>();
                var containerFactory = sp.GetRequiredService<ICosmosContainerFactory>();
                var db = sp.GetRequiredService<ApplicationDbContext>();
                var conn = db.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    await conn.OpenAsync();
                }
                var checks = new List<object>();
                var writeSteps = new List<object>();
                bool allPassed = true;
                bool hadException = false;

                var cs = config.GetConnectionString("DefaultConnection");

                var ptmContainer = containerFactory.GetContainer(CosmosContainers.dbo_ProjectTemplateMapping);
                var picContainer = containerFactory.GetContainer(CosmosContainers.dbo_ProjectImageCards);
                var pgmContainer = containerFactory.GetContainer(CosmosContainers.dbo_ProjectGradingMapping);
                var pgcmContainer = containerFactory.GetContainer(CosmosContainers.dbo_ProjectGradingConditionMapping);
                var hiContainer = containerFactory.GetContainer(CosmosContainers.dbo_H_IReportTemplate);
                var ticContainer = containerFactory.GetContainer(CosmosContainers.dbo_TankImageCard);
                var gicContainer = containerFactory.GetContainer(CosmosContainers.dbo_GenericImageCard);
                var hipgContainer = containerFactory.GetContainer(CosmosContainers.dbo_HandIProjectGrading);
                var hipgcContainer = containerFactory.GetContainer(CosmosContainers.dbo_HandIProjectGradingConditions);
                var buContainer = containerFactory.GetContainer(CosmosContainers.dbo_BulkUpload);
                var clContainer = containerFactory.GetContainer(CosmosContainers.upskill_CapLog);
                var idContainer = containerFactory.GetContainer(CosmosContainers.upskill_ImageData);
                var imgContainer = containerFactory.GetContainer(CosmosContainers.upskill_Images);
                var rtContainer = containerFactory.GetContainer(CosmosContainers.dbo_ReportTemplate);
                var ccContainer = containerFactory.GetContainer(CosmosContainers.dbo_CurrentCondition);
                var rtsContainer = containerFactory.GetContainer(CosmosContainers.dbo_ReportTemplateSection);
                var icContainer = containerFactory.GetContainer(CosmosContainers.dbo_ImageCard);
                var tsmContainer = containerFactory.GetContainer(CosmosContainers.dbo_TemplateSectionMapping);
                var subContainer = containerFactory.GetContainer(CosmosContainers.dbo_SubSection);

                var methodPositiveRows = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    { "GetReportTemplateList", 0 },
                    { "GetCurrentCondition", 0 },
                    { "GetBulkUploadUnplacedImages", 0 },
                    { "GetTemplateTitle", 0 },
                    { "GetTemplateSections", 0 },
                    { "GetTemplatSections", 0 },
                    { "GetImageCard", 0 },
                    { "GetExistingReportPartsSectionIds", 0 },
                    { "GetHIReport", 0 },
                    { "GetProjectReportTemplate.2Args", 0 },
                    { "GetProjectReportTemplate.3Args", 0 },
                    { "GetProjectGrading", 0 },
                    { "GetTemplatGradings", 0 },
                    { "GetGradingCondition", 0 },
                    { "GetProjectImageCard", 0 },
                    { "GetProjectGradingConditionMapping", 0 },
                    { "GetProjectGradingById", 0 },
                    { "GetProjectImagCardById", 0 }
                };

                static bool ApproxDateEquals(DateTime? d1, DateTime? d2)
                {
                    if (!d1.HasValue && !d2.HasValue) return true;
                    if (!d1.HasValue || !d2.HasValue) return false;
                    return Math.Abs((d1.Value - d2.Value).TotalMilliseconds) < 4;
                }

                static bool ApproxNowDateEquals(DateTime? actual, DateTime expected, double maxSeconds = 10)
                {
                    if (!actual.HasValue) return false;
                    return Math.Abs((actual.Value - expected).TotalSeconds) <= maxSeconds;
                }

                static bool CompareHIReports(HandIReport? s, HandIReport? c)
                {
                    if (s == null && c == null) return true;
                    if (s == null || c == null) return false;
                    return s.Id == c.Id
                        && s.ProjectId == c.ProjectId
                        && s.SectionId == c.SectionId
                        && string.Equals(s.CompanyName?.TrimEnd(' '), c.CompanyName?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.CertificateNumber?.TrimEnd(' '), c.CertificateNumber?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && ApproxDateEquals(s.ExpiryDate, c.ExpiryDate)
                        && string.Equals(s.ThicknessMeasurementA?.TrimEnd(' '), c.ThicknessMeasurementA?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.ThicknessMeasurementB?.TrimEnd(' '), c.ThicknessMeasurementB?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.ThicknessMeasurementC?.TrimEnd(' '), c.ThicknessMeasurementC?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.AdditionalSurveyorComments?.TrimEnd(' '), c.AdditionalSurveyorComments?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.MaintenanceExecuted?.TrimEnd(' '), c.MaintenanceExecuted?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.Accommodation?.TrimEnd(' '), c.Accommodation?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.PhotoGraphs?.TrimEnd(' '), c.PhotoGraphs?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.OperationalTestLocation?.TrimEnd(' '), c.OperationalTestLocation?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && ApproxDateEquals(s.OperationalTestStart, c.OperationalTestStart)
                        && ApproxDateEquals(s.OperationalTestFinish, c.OperationalTestFinish)
                        && string.Equals(s.OptionalTestSatisFactory?.TrimEnd(' '), c.OptionalTestSatisFactory?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.AssociatedReportNumber?.TrimEnd(' '), c.AssociatedReportNumber?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.AssociatedReportDate?.TrimEnd(' '), c.AssociatedReportDate?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.Remarks?.TrimEnd(' '), c.Remarks?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.LeadSurveyorName?.TrimEnd(' '), c.LeadSurveyorName?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.LeadSurveyorNumber?.TrimEnd(' '), c.LeadSurveyorNumber?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.SurveyorName?.TrimEnd(' '), c.SurveyorName?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.SurveyorNumber?.TrimEnd(' '), c.SurveyorNumber?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && ApproxDateEquals(s.LastVisitDate, c.LastVisitDate)
                        && string.Equals(s.IndividualA?.TrimEnd(' '), c.IndividualA?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.IndividualB?.TrimEnd(' '), c.IndividualB?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.IndividualC?.TrimEnd(' '), c.IndividualC?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.IndividualD?.TrimEnd(' '), c.IndividualD?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.IndividualE?.TrimEnd(' '), c.IndividualE?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && string.Equals(s.IndividualF?.TrimEnd(' '), c.IndividualF?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                        && s.SubstantialCorrosionRemaining == c.SubstantialCorrosionRemaining
                        && s.StatutoryDeficiencies == c.StatutoryDeficiencies
                        && s.FoundSatisfactory == c.FoundSatisfactory
                        && s.UnderRemarks == c.UnderRemarks
                        && s.ScouringOrPitting == c.ScouringOrPitting
                        && s.BottomPlattingScouringOrPitting == c.BottomPlattingScouringOrPitting
                        && s.IsActive == c.IsActive
                        && s.IsDeleted == c.IsDeleted
                        && ApproxDateEquals(s.CreatedDttm, c.CreatedDttm)
                        && ApproxDateEquals(s.UpdatedDttm, c.UpdatedDttm);
                }

                // =========================================================================
                // 1. READ CHECKS
                // =========================================================================

                // 1.1 Trap 6 static helper fixed cases (calling repository's own helper per T12)
                try
                {
                    string? t6_null = null;
                    string? t6_null_res = CosmosSurveyReportRepository.TransformGradingValue(t6_null);
                    bool t6_null_ok = t6_null_res == null;

                    string t6_empty = "";
                    string? t6_empty_res = CosmosSurveyReportRepository.TransformGradingValue(t6_empty);
                    bool t6_empty_ok = t6_empty_res == "";

                    string t6_19 = "1234567890123456789";
                    string? t6_19_res = CosmosSurveyReportRepository.TransformGradingValue(t6_19);
                    bool t6_19_ok = t6_19_res == "1234567890123456789";

                    string t6_20_no_sp = "12345678901234567890";
                    string? t6_20_res = CosmosSurveyReportRepository.TransformGradingValue(t6_20_no_sp);
                    bool t6_20_ok = t6_20_res == "1234567890 123456789";

                    string t6_25_no_sp = "1234567890123456789012345";
                    string? t6_25_res = CosmosSurveyReportRepository.TransformGradingValue(t6_25_no_sp);
                    bool t6_25_ok = t6_25_res == "123456789012 3456789";

                    string t6_25_sp = "1234567890 12345678901234";
                    string? t6_25_sp_res = CosmosSurveyReportRepository.TransformGradingValue(t6_25_sp);
                    bool t6_25_sp_ok = t6_25_sp_res == "1234567890 123456789";

                    string t6_20_trail = "12345678901234567890   ";
                    string? t6_20_trail_res = CosmosSurveyReportRepository.TransformGradingValue(t6_20_trail);
                    bool t6_20_trail_ok = t6_20_trail_res == "12345678901234567890";

                    bool t6_all = t6_null_ok && t6_empty_ok && t6_19_ok && t6_20_ok && t6_25_ok && t6_25_sp_ok && t6_20_trail_ok;
                    checks.Add(new { method = "Trap6.TransformGradingValue", equal = t6_all, t6_null_ok, t6_empty_ok, t6_19_ok, t6_20_ok, t6_25_ok, t6_25_sp_ok, t6_20_trail_ok });
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { method = "Trap6.TransformGradingValue", equal = false, error = ex.Message });
                }

                // 1.2 GetReportTemplateList (T1: both empty is fail on real input)
                try
                {
                    var sqlTemplates = await sqlRepo.GetReportTemplateList();
                    var cosTemplates = await cosmosRepo.GetReportTemplateList();
                    bool listEq = sqlTemplates.Count > 0
                        && sqlTemplates.Count == cosTemplates.Count
                        && sqlTemplates.OrderBy(x => x.Id).Zip(cosTemplates.OrderBy(x => x.Id))
                            .All(p => p.First.Id == p.Second.Id && string.Equals(p.First.Name?.TrimEnd(' '), p.Second.Name?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase));
                    if (sqlTemplates.Count > 0) methodPositiveRows["GetReportTemplateList"] += sqlTemplates.Count;
                    checks.Add(new { method = "GetReportTemplateList", equal = listEq, sqlCount = sqlTemplates.Count, cosmosCount = cosTemplates.Count });
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { method = "GetReportTemplateList", equal = false, error = ex.Message });
                }

                // 1.3 GetCurrentCondition (T1)
                try
                {
                    var sqlConds = await sqlRepo.GetCurrentCondition();
                    var cosConds = await cosmosRepo.GetCurrentCondition();
                    bool condEq = sqlConds.Count > 0
                        && sqlConds.Count == cosConds.Count
                        && sqlConds.OrderBy(x => x.Id).Zip(cosConds.OrderBy(x => x.Id))
                            .All(p => p.First.Id == p.Second.Id && string.Equals(p.First.Name?.TrimEnd(' '), p.Second.Name?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase));
                    if (sqlConds.Count > 0) methodPositiveRows["GetCurrentCondition"] += sqlConds.Count;
                    checks.Add(new { method = "GetCurrentCondition", equal = condEq, sqlCount = sqlConds.Count, cosmosCount = cosConds.Count });
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { method = "GetCurrentCondition", equal = false, error = ex.Message });
                }

                // 1.4 GetBulkUploadUnplacedImages (SHA256 comparison + dates per T8, T1)
                try
                {
                    var sqlBulk = await sqlRepo.GetBulkUploadUnplacedImages();
                    var cosBulk = await cosmosRepo.GetBulkUploadUnplacedImages();
                    bool bulkEq = sqlBulk.Count > 0 && sqlBulk.Count == cosBulk.Count;
                    if (bulkEq)
                    {
                        var sqlH = sqlBulk.Select(b => new { b.Id, b.ImageId, b.ProjectId, Len = b.Base64String?.Length ?? 0, Hash = ComputeSha256(b.Base64String), b.CreateDttm, b.UpdateDttm })
                            .OrderBy(x => x.Id).ThenBy(x => x.ImageId).ToList();
                        var cosH = cosBulk.Select(b => new { b.Id, b.ImageId, b.ProjectId, Len = b.Base64String?.Length ?? 0, Hash = ComputeSha256(b.Base64String), b.CreateDttm, b.UpdateDttm })
                            .OrderBy(x => x.Id).ThenBy(x => x.ImageId).ToList();
                        bulkEq = sqlH.Zip(cosH).All(p => p.First.Id == p.Second.Id && p.First.ImageId == p.Second.ImageId && p.First.ProjectId == p.Second.ProjectId && p.First.Len == p.Second.Len && p.First.Hash == p.Second.Hash
                            && ApproxDateEquals(p.First.CreateDttm, p.Second.CreateDttm) && ApproxDateEquals(p.First.UpdateDttm, p.Second.UpdateDttm));
                    }
                    if (sqlBulk.Count > 0) methodPositiveRows["GetBulkUploadUnplacedImages"] += sqlBulk.Count;
                    checks.Add(new { method = "GetBulkUploadUnplacedImages", equal = bulkEq, sqlCount = sqlBulk.Count, cosmosCount = cosBulk.Count });
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { method = "GetBulkUploadUnplacedImages", equal = false, error = ex.Message });
                }

                // 1.5 Template queries for all template IDs in SQL
                List<int> templateIds = new List<int>();
                List<int> icTemplateIds = new List<int>();
                try
                {
                    templateIds = (await conn.QueryAsync<int>("SELECT DISTINCT TemplateId FROM [dbo].[TemplateSectionMapping] WHERE TemplateId IS NOT NULL")).ToList();
                    icTemplateIds = (await conn.QueryAsync<int>("SELECT DISTINCT TemplateId FROM [dbo].[ImageCard] WHERE TemplateId IS NOT NULL")).ToList();
                }
                catch (Exception ex)
                {
                    hadException = true;
                    allPassed = false;
                    checks.Add(new { method = "Setup.GetTemplateIds", equal = false, error = ex.Message });
                }

                // Add missing template (-99999)
                var templatesToTest = new List<int>(templateIds);
                templatesToTest.Add(-99999);

                foreach (var tid in templatesToTest)
                {
                    bool isMissingTid = tid == -99999;

                    // GetTemplateTitle
                    try
                    {
                        var sqlTitle = await sqlRepo.GetTemplateTitle(tid);
                        var cosTitle = await cosmosRepo.GetTemplateTitle(tid);
                        bool titleEq = isMissingTid
                            ? (sqlTitle == null && cosTitle == null)
                            : (sqlTitle != null && cosTitle != null && sqlTitle.Id == cosTitle.Id && string.Equals(sqlTitle.SectionName?.TrimEnd(' '), cosTitle.SectionName?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase));
                        if (sqlTitle != null) methodPositiveRows["GetTemplateTitle"]++;
                        checks.Add(new { method = "GetTemplateTitle", templateId = tid, equal = titleEq });
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { method = "GetTemplateTitle", templateId = tid, equal = false, error = ex.Message });
                    }

                    // GetTemplateSections
                    try
                    {
                        var sqlSecs = await sqlRepo.GetTemplateSections(tid);
                        var cosSecs = await cosmosRepo.GetTemplateSections(tid);
                        bool secsEq = isMissingTid
                            ? (sqlSecs.Count == 0 && cosSecs.Count == 0)
                            : (sqlSecs.Count > 0 && sqlSecs.Count == cosSecs.Count);
                        if (secsEq && !isMissingTid)
                        {
                            var sOrder = sqlSecs.OrderBy(s => s.Id).ThenBy(s => s.SectionId).ThenBy(s => s.SectionName).ToList();
                            var cOrder = cosSecs.OrderBy(s => s.Id).ThenBy(s => s.SectionId).ThenBy(s => s.SectionName).ToList();
                            secsEq = sOrder.Zip(cOrder).All(p => p.First.Id == p.Second.Id
                                && p.First.SectionId == p.Second.SectionId
                                && string.Equals(p.First.SectionName?.TrimEnd(' '), p.Second.SectionName?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                && p.First.HasSubSection == p.Second.HasSubSection
                                && string.Equals(p.First.SubSectionName?.TrimEnd(' '), p.Second.SubSectionName?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                && string.Equals(p.First.SpecialSectionName?.TrimEnd(' '), p.Second.SpecialSectionName?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase));
                        }
                        if (sqlSecs.Count > 0) methodPositiveRows["GetTemplateSections"] += sqlSecs.Count;
                        checks.Add(new { method = "GetTemplateSections", templateId = tid, equal = secsEq, sqlCount = sqlSecs.Count, cosmosCount = cosSecs.Count });
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { method = "GetTemplateSections", templateId = tid, equal = false, error = ex.Message });
                    }

                    // GetTemplatSections
                    try
                    {
                        var sqlSecs = await sqlRepo.GetTemplatSections(tid);
                        var cosSecs = await cosmosRepo.GetTemplatSections(tid);
                        bool secsEq = isMissingTid
                            ? (sqlSecs.Count == 0 && cosSecs.Count == 0)
                            : (sqlSecs.Count > 0 && sqlSecs.Count == cosSecs.Count);
                        if (secsEq && !isMissingTid)
                        {
                            var sOrder = sqlSecs.OrderBy(s => s.Id).ThenBy(s => s.SectionId).ThenBy(s => s.SectionName).ToList();
                            var cOrder = cosSecs.OrderBy(s => s.Id).ThenBy(s => s.SectionId).ThenBy(s => s.SectionName).ToList();
                            secsEq = sOrder.Zip(cOrder).All(p => p.First.Id == p.Second.Id
                                && p.First.SectionId == p.Second.SectionId
                                && string.Equals(p.First.SectionName?.TrimEnd(' '), p.Second.SectionName?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                && p.First.HasSubSection == p.Second.HasSubSection
                                && string.Equals(p.First.SubSectionName?.TrimEnd(' '), p.Second.SubSectionName?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                && string.Equals(p.First.SpecialSectionName?.TrimEnd(' '), p.Second.SpecialSectionName?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase));
                        }
                        if (sqlSecs.Count > 0) methodPositiveRows["GetTemplatSections"] += sqlSecs.Count;
                        checks.Add(new { method = "GetTemplatSections", templateId = tid, equal = secsEq, sqlCount = sqlSecs.Count, cosmosCount = cosSecs.Count });
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { method = "GetTemplatSections", templateId = tid, equal = false, error = ex.Message });
                    }
                }

                // GetImageCard on templates that have image cards, plus missing template -99999
                var icTemplatesToTest = new List<int>(icTemplateIds);
                icTemplatesToTest.Add(-99999);
                foreach (var tid in icTemplatesToTest)
                {
                    try
                    {
                        var sqlCards = await sqlRepo.GetImageCard(tid);
                        var cosCards = await cosmosRepo.GetImageCard(tid);
                        bool isMissing = tid == -99999;
                        bool cardsEq = isMissing
                            ? (sqlCards.Count == 0 && cosCards.Count == 0)
                            : (sqlCards.Count > 0 && sqlCards.Count == cosCards.Count);
                        if (cardsEq && !isMissing)
                        {
                            var sOrder = sqlCards.OrderBy(c => c.Id).ToList();
                            var cOrder = cosCards.OrderBy(c => c.Id).ToList();
                            cardsEq = sOrder.Zip(cOrder).All(p => p.First.Id == p.Second.Id
                                && p.First.SectionId == p.Second.SectionId
                                && string.Equals(p.First.FileName?.TrimEnd(' '), p.Second.FileName?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                && p.First.CurrentCondition == p.Second.CurrentCondition
                                && p.First.IsDescDropdown == p.Second.IsDescDropdown
                                && string.Equals(p.First.Description?.TrimEnd(' '), p.Second.Description?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase));
                        }
                        if (sqlCards.Count > 0) methodPositiveRows["GetImageCard"] += sqlCards.Count;
                        checks.Add(new { method = "GetImageCard", templateId = tid, equal = cardsEq, sqlCount = sqlCards.Count, cosmosCount = cosCards.Count });
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { method = "GetImageCard", templateId = tid, equal = false, error = ex.Message });
                    }
                }

                // 1.6 First 25 projects with ProjectTemplateMapping rows x their templates
                List<(int ProjectId, int TemplateId)> projectTemplates = new List<(int, int)>();
                try
                {
                    projectTemplates = (await conn.QueryAsync<(int ProjectId, int TemplateId)>(
                        "SELECT DISTINCT TOP 25 ProjectId, TemplateId FROM [dbo].[ProjectTemplateMapping] WHERE ProjectId IS NOT NULL AND TemplateId IS NOT NULL AND IsDeleted = 0 ORDER BY ProjectId, TemplateId")).ToList();
                }
                catch (Exception ex)
                {
                    hadException = true;
                    allPassed = false;
                    checks.Add(new { method = "Setup.GetProjectTemplates", equal = false, error = ex.Message });
                }

                var distinctProjects = projectTemplates.Select(pt => pt.ProjectId).Distinct().ToList();
                distinctProjects.Add(-99999); // +1 missing project

                foreach (var pid in distinctProjects)
                {
                    // GetExistingReportPartsSectionIds
                    try
                    {
                        var sqlIds = await sqlRepo.GetExistingReportPartsSectionIds(pid);
                        var cosIds = await cosmosRepo.GetExistingReportPartsSectionIds(pid);
                        bool isMissingPid = pid == -99999;
                        bool idsEq = isMissingPid
                            ? (sqlIds.Count == 0 && cosIds.Count == 0)
                            : (sqlIds.Count > 0 && sqlIds.Count == cosIds.Count && sqlIds.OrderBy(x => x).SequenceEqual(cosIds.OrderBy(x => x)));
                        if (sqlIds.Count > 0) methodPositiveRows["GetExistingReportPartsSectionIds"] += sqlIds.Count;
                        checks.Add(new { method = "GetExistingReportPartsSectionIds", projectId = pid, equal = idsEq, sqlCount = sqlIds.Count, cosmosCount = cosIds.Count });
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { method = "GetExistingReportPartsSectionIds", projectId = pid, equal = false, error = ex.Message });
                    }
                }

                // Dedicated GetHIReport check on projects with H&I reports (T8, T9, P5)
                List<int> hiProjectIds = new List<int>();
                int? inactiveOnlyPid = null;
                try
                {
                    hiProjectIds = (await conn.QueryAsync<int>("SELECT DISTINCT ProjectId FROM [dbo].[H_IReportTemplate] WHERE ProjectId IS NOT NULL AND IsActive = 1 AND IsDeleted = 0")).ToList();
                    inactiveOnlyPid = await conn.QueryFirstOrDefaultAsync<int?>(
                        "SELECT TOP 1 ProjectId FROM [dbo].[H_IReportTemplate] WHERE ProjectId IS NOT NULL AND IsActive = 0 AND IsDeleted = 0 AND ProjectId NOT IN (SELECT ProjectId FROM [dbo].[H_IReportTemplate] WHERE IsActive = 1 AND IsDeleted = 0)");
                }
                catch (Exception ex)
                {
                    hadException = true;
                    allPassed = false;
                    checks.Add(new { method = "Setup.GetHiProjectIds", equal = false, error = ex.Message });
                }
                var hiProjectsToTest = new List<int>(hiProjectIds.Take(25));
                hiProjectsToTest.Add(-99999); // missing project

                foreach (var pid in hiProjectsToTest)
                {
                    try
                    {
                        var sqlHi = await sqlRepo.GetHIReport(pid);
                        var cosHi = await cosmosRepo.GetHIReport(pid);
                        bool isMissing = pid == -99999;
                        bool hiEq = isMissing
                            ? (sqlHi == null && cosHi == null)
                            : (sqlHi != null && cosHi != null && CompareHIReports(sqlHi, cosHi));
                        if (sqlHi != null) methodPositiveRows["GetHIReport"]++;
                        checks.Add(new { method = "GetHIReport", projectId = pid, equal = hiEq });
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { method = "GetHIReport", projectId = pid, equal = false, error = ex.Message });
                    }
                }

                if (inactiveOnlyPid.HasValue)
                {
                    try
                    {
                        var sqlInact = await sqlRepo.GetHIReport(inactiveOnlyPid.Value);
                        var cosInact = await cosmosRepo.GetHIReport(inactiveOnlyPid.Value);
                        bool inactEq = sqlInact == null && cosInact == null;
                        checks.Add(new { method = "GetHIReport.InactiveOnlyProject", projectId = inactiveOnlyPid.Value, equal = inactEq, sqlNull = (sqlInact == null), cosNull = (cosInact == null) });
                        if (!inactEq) allPassed = false;
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { method = "GetHIReport.InactiveOnlyProject", projectId = inactiveOnlyPid.Value, equal = false, error = ex.Message });
                    }
                }

                // Check Trap 21 multi-row projects for GetHIReport (T9: all multi-row projects, remove Take(10), all columns)
                try
                {
                    var multiRowProjects = (await conn.QueryAsync<int>(
                        "SELECT ProjectId FROM [dbo].[H_IReportTemplate] WHERE IsActive=1 AND IsDeleted=0 GROUP BY ProjectId HAVING COUNT(1) > 1")).ToList();
                    foreach (var mPid in multiRowProjects)
                    {
                        var sqlHi = await sqlRepo.GetHIReport(mPid);
                        var cosHi = await cosmosRepo.GetHIReport(mPid);
                        bool multiHiEq = sqlHi != null && cosHi != null && CompareHIReports(sqlHi, cosHi);
                        if (sqlHi != null) methodPositiveRows["GetHIReport"]++;
                        checks.Add(new { method = "GetHIReport.MultiRowLowestIdCheck", projectId = mPid, equal = multiHiEq, sqlId = sqlHi?.Id, cosId = cosHi?.Id });
                    }
                }
                catch (Exception ex)
                {
                    hadException = true;
                    allPassed = false;
                    checks.Add(new { method = "GetHIReport.MultiRowLowestIdCheck", equal = false, error = ex.Message });
                }

                foreach (var pt in projectTemplates)
                {
                    int pid = pt.ProjectId;
                    int tid = pt.TemplateId;

                    // GetProjectReportTemplate (2 args)
                    try
                    {
                        var sqlPrt = await sqlRepo.GetProjectReportTemplate(pid, tid);
                        var cosPrt = await cosmosRepo.GetProjectReportTemplate(pid, tid);
                        bool prtEq = sqlPrt.Count > 0 && sqlPrt.Count == cosPrt.Count;
                        if (prtEq)
                        {
                            var sOrder = sqlPrt.OrderBy(x => x.Id).ToList();
                            var cOrder = cosPrt.OrderBy(x => x.Id).ToList();
                            prtEq = sOrder.Zip(cOrder).All(p => p.First.Id == p.Second.Id
                                && p.First.ProjectId == p.Second.ProjectId
                                && p.First.TemplateId == p.Second.TemplateId
                                && p.First.SectionId == p.Second.SectionId
                                && string.Equals(p.First.Notes?.TrimEnd(' '), p.Second.Notes?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                && ApproxDateEquals(p.First.CreatedDttm, p.Second.CreatedDttm)
                                && ApproxDateEquals(p.First.UpdateDttm, p.Second.UpdateDttm)
                                && p.First.IsDeleted == p.Second.IsDeleted);
                        }
                        if (sqlPrt.Count > 0) methodPositiveRows["GetProjectReportTemplate.2Args"] += sqlPrt.Count;
                        checks.Add(new { method = "GetProjectReportTemplate.2Args", projectId = pid, templateId = tid, equal = prtEq, sqlCount = sqlPrt.Count, cosmosCount = cosPrt.Count });
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { method = "GetProjectReportTemplate.2Args", projectId = pid, templateId = tid, equal = false, error = ex.Message });
                    }

                    // GetTemplatGradings (T10, P1, P2: ordered by SqlGuid SectionId then LabelName; rows with the same key compare as a set)
                    try
                    {
                        var sqlTg = await sqlRepo.GetTemplatGradings(tid, pid);
                        var cosTg = await cosmosRepo.GetTemplatGradings(tid, pid);
                        bool tgEq = sqlTg.Count > 0 && sqlTg.Count == cosTg.Count;
                        if (tgEq)
                        {
                            static List<(Guid SectionId, string? Label, HashSet<int> Ids)> GroupConsecutive(List<GradingUI> list)
                            {
                                var groups = new List<(Guid SectionId, string? Label, HashSet<int> Ids)>();
                                foreach (var item in list)
                                {
                                    var label = item.LabelName?.TrimEnd(' ');
                                    if (groups.Count > 0)
                                    {
                                        var last = groups[groups.Count - 1];
                                        if (new System.Data.SqlTypes.SqlGuid(last.SectionId).Equals(new System.Data.SqlTypes.SqlGuid(item.SectionId))
                                            && string.Equals(last.Label, label, StringComparison.OrdinalIgnoreCase))
                                        {
                                            last.Ids.Add(item.Id);
                                            continue;
                                        }
                                    }
                                    groups.Add((item.SectionId, label, new HashSet<int> { item.Id }));
                                }
                                return groups;
                            }

                            var sqlGroups = GroupConsecutive(sqlTg);
                            var cosGroups = GroupConsecutive(cosTg);
                            if (sqlGroups.Count != cosGroups.Count)
                            {
                                tgEq = false;
                            }
                            else
                            {
                                for (int gi = 0; gi < sqlGroups.Count; gi++)
                                {
                                    if (!new System.Data.SqlTypes.SqlGuid(sqlGroups[gi].SectionId).Equals(new System.Data.SqlTypes.SqlGuid(cosGroups[gi].SectionId))
                                        || !string.Equals(sqlGroups[gi].Label, cosGroups[gi].Label, StringComparison.OrdinalIgnoreCase)
                                        || !sqlGroups[gi].Ids.SetEquals(cosGroups[gi].Ids))
                                    {
                                        tgEq = false;
                                        break;
                                    }
                                }
                            }
                        }
                        if (sqlTg.Count > 0) methodPositiveRows["GetTemplatGradings"] += sqlTg.Count;
                        checks.Add(new { method = "GetTemplatGradings", projectId = pid, templateId = tid, equal = tgEq, sqlCount = sqlTg.Count, cosmosCount = cosTg.Count });
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { method = "GetTemplatGradings", projectId = pid, templateId = tid, equal = false, error = ex.Message });
                    }

                    // GetGradingCondition (P1: 0 == 0 must be equal=false)
                    try
                    {
                        var sqlGc = await sqlRepo.GetGradingCondition(tid, pid);
                        var cosGc = await cosmosRepo.GetGradingCondition(tid, pid);
                        bool gcEq = sqlGc.Count > 0 && sqlGc.Count == cosGc.Count;
                        if (gcEq)
                        {
                            var sOrder = sqlGc.OrderBy(x => x.Id).ThenBy(x => x.GradingId).ThenBy(x => x.SectionId).ToList();
                            var cOrder = cosGc.OrderBy(x => x.Id).ThenBy(x => x.GradingId).ThenBy(x => x.SectionId).ToList();
                            gcEq = sOrder.Zip(cOrder).All(p => p.First.Id == p.Second.Id
                                && p.First.GradingId == p.Second.GradingId
                                && p.First.SectionId == p.Second.SectionId
                                && string.Equals(p.First.Condition?.TrimEnd(' '), p.Second.Condition?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase));
                        }
                        if (sqlGc.Count > 0) methodPositiveRows["GetGradingCondition"] += sqlGc.Count;
                        checks.Add(new { method = "GetGradingCondition", projectId = pid, templateId = tid, equal = gcEq, sqlCount = sqlGc.Count, cosmosCount = cosGc.Count });
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        checks.Add(new { method = "GetGradingCondition", projectId = pid, templateId = tid, equal = false, error = ex.Message });
                    }

                    // Query real SectionIds for this project and template (T3: no empty catch)
                    List<Guid> sectionGuids = new List<Guid>();
                    try
                    {
                        sectionGuids = (await conn.QueryAsync<Guid>(
                            "SELECT DISTINCT SectionId FROM [dbo].[ProjectTemplateMapping] WHERE ProjectId=@pid AND TemplateId=@tid AND SectionId IS NOT NULL AND IsDeleted=0",
                            new { pid, tid })).ToList();
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { method = "Setup.GetSectionGuids", projectId = pid, templateId = tid, equal = false, error = ex.Message });
                    }

                    // T2: Test Guid.Empty for EVERY project/template (add it after Take(3), not before)
                    var sectionsToTest = sectionGuids.Take(3).ToList();
                    sectionsToTest.Add(Guid.Empty);

                    foreach (var sec in sectionsToTest)
                    {
                        bool isMissingSec = sec == Guid.Empty;

                        // GetProjectReportTemplate (3 args)
                        try
                        {
                            var sqlPrt3 = await sqlRepo.GetProjectReportTemplate(pid, tid, sec);
                            var cosPrt3 = await cosmosRepo.GetProjectReportTemplate(pid, tid, sec);
                            bool prt3Eq = isMissingSec
                                ? (sqlPrt3 == null && cosPrt3 == null)
                                : (sqlPrt3 != null && cosPrt3 != null
                                    && sqlPrt3.Id == cosPrt3.Id
                                    && sqlPrt3.ProjectId == cosPrt3.ProjectId
                                    && sqlPrt3.TemplateId == cosPrt3.TemplateId
                                    && sqlPrt3.SectionId == cosPrt3.SectionId
                                    && string.Equals(sqlPrt3.Notes?.TrimEnd(' '), cosPrt3.Notes?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                    && ApproxDateEquals(sqlPrt3.CreatedDttm, cosPrt3.CreatedDttm)
                                    && ApproxDateEquals(sqlPrt3.UpdateDttm, cosPrt3.UpdateDttm)
                                    && sqlPrt3.IsDeleted == cosPrt3.IsDeleted);
                            if (sqlPrt3 != null) methodPositiveRows["GetProjectReportTemplate.3Args"]++;
                            checks.Add(new { method = "GetProjectReportTemplate.3Args", projectId = pid, templateId = tid, sectionId = sec, equal = prt3Eq });
                        }
                        catch (Exception ex)
                        {
                            hadException = true;
                            checks.Add(new { method = "GetProjectReportTemplate.3Args", projectId = pid, templateId = tid, sectionId = sec, equal = false, error = ex.Message });
                        }

                        // GetProjectGrading
                        try
                        {
                            var sqlPg = await sqlRepo.GetProjectGrading(pid, tid, sec);
                            var cosPg = await cosmosRepo.GetProjectGrading(pid, tid, sec);
                            bool pgEq = isMissingSec
                                ? (sqlPg.Count == 0 && cosPg.Count == 0)
                                : (sqlPg.Count > 0 && sqlPg.Count == cosPg.Count);
                            if (pgEq && !isMissingSec)
                            {
                                var sOrder = sqlPg.OrderBy(x => x.Id).ToList();
                                var cOrder = cosPg.OrderBy(x => x.Id).ToList();
                                pgEq = sOrder.Zip(cOrder).All(p => p.First.Id == p.Second.Id
                                    && p.First.ProjectId == p.Second.ProjectId
                                    && p.First.TemplateId == p.Second.TemplateId
                                    && p.First.SectionId == p.Second.SectionId
                                    && p.First.GradingId == p.Second.GradingId
                                    && string.Equals(p.First.Value?.TrimEnd(' '), p.Second.Value?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                    && ApproxDateEquals(p.First.CreatedDttm, p.Second.CreatedDttm)
                                    && ApproxDateEquals(p.First.UpdatedDttm, p.Second.UpdatedDttm)
                                    && p.First.IsDeleted == p.Second.IsDeleted);
                            }
                            if (sqlPg.Count > 0) methodPositiveRows["GetProjectGrading"] += sqlPg.Count;
                            checks.Add(new { method = "GetProjectGrading", projectId = pid, templateId = tid, sectionId = sec, equal = pgEq, sqlCount = sqlPg?.Count, cosmosCount = cosPg?.Count });
                        }
                        catch (Exception ex)
                        {
                            hadException = true;
                            checks.Add(new { method = "GetProjectGrading", projectId = pid, templateId = tid, sectionId = sec, equal = false, error = ex.Message });
                        }

                        // GetProjectImageCard (T8: Src length + SHA-256)
                        try
                        {
                            var sqlPic = await sqlRepo.GetProjectImageCard(pid, tid, sec);
                            var cosPic = await cosmosRepo.GetProjectImageCard(pid, tid, sec);
                            bool picEq = isMissingSec
                                ? (sqlPic.Count == 0 && cosPic.Count == 0)
                                : (sqlPic.Count > 0 && sqlPic.Count == cosPic.Count);
                            if (picEq && !isMissingSec)
                            {
                                var sOrder = sqlPic.OrderBy(x => x.Id).ThenBy(x => x.CardId).ToList();
                                var cOrder = cosPic.OrderBy(x => x.Id).ThenBy(x => x.CardId).ToList();
                                picEq = sOrder.Zip(cOrder).All(p => p.First.Id == p.Second.Id
                                    && p.First.ProjectId == p.Second.ProjectId
                                    && p.First.TemplateId == p.Second.TemplateId
                                    && p.First.SectionId == p.Second.SectionId
                                    && p.First.CardId == p.Second.CardId
                                    && p.First.DescriptionId == p.Second.DescriptionId
                                    && string.Equals(p.First.AdditionalDescription?.TrimEnd(' '), p.Second.AdditionalDescription?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                    && p.First.CurrentCondition == p.Second.CurrentCondition
                                    && p.First.ImageId == p.Second.ImageId
                                    && (p.First.Src?.Length ?? 0) == (p.Second.Src?.Length ?? 0)
                                    && ComputeSha256(p.First.Src) == ComputeSha256(p.Second.Src)
                                    && ApproxDateEquals(p.First.CreatedDttm, p.Second.CreatedDttm)
                                    && ApproxDateEquals(p.First.UpdateDttm, p.Second.UpdateDttm)
                                    && p.First.IsDeleted == p.Second.IsDeleted);
                            }
                            if (sqlPic.Count > 0) methodPositiveRows["GetProjectImageCard"] += sqlPic.Count;
                            checks.Add(new { method = "GetProjectImageCard", projectId = pid, templateId = tid, sectionId = sec, equal = picEq, sqlCount = sqlPic?.Count, cosmosCount = cosPic?.Count });
                        }
                        catch (Exception ex)
                        {
                            hadException = true;
                            checks.Add(new { method = "GetProjectImageCard", projectId = pid, templateId = tid, sectionId = sec, equal = false, error = ex.Message });
                        }

                        // Query real GradingIds for this section from ProjectGradingMapping (T11, T3: no empty catch)
                        List<int> gradingIds = new List<int>();
                        try
                        {
                            gradingIds = (await conn.QueryAsync<int>(
                                "SELECT DISTINCT GradingId FROM [dbo].[ProjectGradingMapping] WHERE ProjectId=@pid AND TemplateId=@tid AND SectionId=@sec AND GradingId IS NOT NULL AND IsDeleted=0",
                                new { pid, tid, sec })).ToList();
                        }
                        catch (Exception ex)
                        {
                            hadException = true;
                            allPassed = false;
                            checks.Add(new { method = "Setup.GetGradingIds", projectId = pid, templateId = tid, sectionId = sec, equal = false, error = ex.Message });
                        }

                        var gradingsToTest = new List<int>(gradingIds.Take(2));
                        gradingsToTest.Add(0); // Add 0 as missing

                        foreach (var gid in gradingsToTest)
                        {
                            bool isMissingGid = gid == 0 || isMissingSec;

                            // GetProjectGradingConditionMapping
                            try
                            {
                                var sqlPgcm = await sqlRepo.GetProjectGradingConditionMapping(pid, tid, sec, gid);
                                var cosPgcm = await cosmosRepo.GetProjectGradingConditionMapping(pid, tid, sec, gid);
                                bool pgcmEq = isMissingGid
                                    ? (sqlPgcm.Count == 0 && cosPgcm.Count == 0)
                                    : (sqlPgcm.Count > 0 && sqlPgcm.Count == cosPgcm.Count);
                                if (pgcmEq && !isMissingGid)
                                {
                                    var sOrder = sqlPgcm.OrderBy(x => x.Id).ToList();
                                    var cOrder = cosPgcm.OrderBy(x => x.Id).ToList();
                                    pgcmEq = sOrder.Zip(cOrder).All(p => p.First.Id == p.Second.Id
                                        && p.First.ProjectId == p.Second.ProjectId
                                        && p.First.TemplateId == p.Second.TemplateId
                                        && p.First.SectionId == p.Second.SectionId
                                        && p.First.GradingId == p.Second.GradingId
                                        && string.Equals(p.First.GradingConditionLabel?.TrimEnd(' '), p.Second.GradingConditionLabel?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                        && p.First.GradingConditionValue == p.Second.GradingConditionValue
                                        && ApproxDateEquals(p.First.CreatedDttm, p.Second.CreatedDttm)
                                        && ApproxDateEquals(p.First.UpdateDttm, p.Second.UpdateDttm)
                                        && p.First.IsDeleted == p.Second.IsDeleted);
                                }
                                if (sqlPgcm.Count > 0) methodPositiveRows["GetProjectGradingConditionMapping"] += sqlPgcm.Count;
                                checks.Add(new { method = "GetProjectGradingConditionMapping", projectId = pid, templateId = tid, sectionId = sec, gradingId = gid, equal = pgcmEq, sqlCount = sqlPgcm?.Count, cosmosCount = cosPgcm?.Count });
                            }
                            catch (Exception ex)
                            {
                                hadException = true;
                                checks.Add(new { method = "GetProjectGradingConditionMapping", projectId = pid, templateId = tid, sectionId = sec, gradingId = gid, equal = false, error = ex.Message });
                            }

                            // GetProjectGradingById (T11)
                            try
                            {
                                var sqlGbyId = await sqlRepo.GetProjectGradingById(pid, tid, sec, gid);
                                var cosGbyId = await cosmosRepo.GetProjectGradingById(pid, tid, sec, gid);
                                bool gbyIdEq = isMissingGid
                                    ? (sqlGbyId == null && cosGbyId == null)
                                    : (sqlGbyId != null && cosGbyId != null
                                        && sqlGbyId.Id == cosGbyId.Id
                                        && sqlGbyId.ProjectId == cosGbyId.ProjectId
                                        && sqlGbyId.TemplateId == cosGbyId.TemplateId
                                        && sqlGbyId.SectionId == cosGbyId.SectionId
                                        && sqlGbyId.GradingId == cosGbyId.GradingId
                                        && string.Equals(sqlGbyId.Value?.TrimEnd(' '), cosGbyId.Value?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                        && ApproxDateEquals(sqlGbyId.CreatedDttm, cosGbyId.CreatedDttm)
                                        && ApproxDateEquals(sqlGbyId.UpdatedDttm, cosGbyId.UpdatedDttm)
                                        && sqlGbyId.IsDeleted == cosGbyId.IsDeleted);
                                if (sqlGbyId != null) methodPositiveRows["GetProjectGradingById"]++;
                                checks.Add(new { method = "GetProjectGradingById", projectId = pid, templateId = tid, sectionId = sec, gradingId = gid, equal = gbyIdEq });
                            }
                            catch (Exception ex)
                            {
                                hadException = true;
                                checks.Add(new { method = "GetProjectGradingById", projectId = pid, templateId = tid, sectionId = sec, gradingId = gid, equal = false, error = ex.Message });
                            }
                        }
                    }
                }

                // Dedicated GetProjectImagCardById check across projects (T11)
                try
                {
                    var cardRows = (await conn.QueryAsync<(int ProjectId, int TemplateId, Guid SectionId, int CardId)>(
                        "SELECT DISTINCT TOP 5 ProjectId, TemplateId, SectionId, CardId FROM [dbo].[ProjectImageCards] WHERE ProjectId IS NOT NULL AND TemplateId IS NOT NULL AND SectionId IS NOT NULL AND CardId IS NOT NULL AND IsDeleted=0")).ToList();
                    if (cardRows.Count == 0)
                    {
                        hadException = true;
                        allPassed = false;
                        checks.Add(new { method = "GetProjectImagCardById", equal = false, error = "No CardId found across projects in database" });
                    }
                    else
                    {
                        foreach (var cRow in cardRows)
                        {
                            var sqlCbyId = await sqlRepo.GetProjectImagCardById(cRow.ProjectId, cRow.TemplateId, cRow.SectionId, cRow.CardId);
                            var cosCbyId = await cosmosRepo.GetProjectImagCardById(cRow.ProjectId, cRow.TemplateId, cRow.SectionId, cRow.CardId);
                            bool cbyIdEq = sqlCbyId != null && cosCbyId != null
                                && sqlCbyId.Id == cosCbyId.Id
                                && sqlCbyId.ProjectId == cosCbyId.ProjectId
                                && sqlCbyId.TemplateId == cosCbyId.TemplateId
                                && sqlCbyId.SectionId == cosCbyId.SectionId
                                && sqlCbyId.CardId == cosCbyId.CardId
                                && sqlCbyId.DescriptionId == cosCbyId.DescriptionId
                                && sqlCbyId.ImageId == cosCbyId.ImageId
                                && string.Equals(sqlCbyId.AdditionalDescription?.TrimEnd(' '), cosCbyId.AdditionalDescription?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                && sqlCbyId.CurrentCondition == cosCbyId.CurrentCondition
                                && sqlCbyId.IsDeleted == cosCbyId.IsDeleted
                                && ApproxDateEquals(sqlCbyId.CreatedDttm, cosCbyId.CreatedDttm)
                                && ApproxDateEquals(sqlCbyId.UpdateDttm, cosCbyId.UpdateDttm);
                            if (sqlCbyId != null) methodPositiveRows["GetProjectImagCardById"]++;
                            checks.Add(new { method = "GetProjectImagCardById", projectId = cRow.ProjectId, templateId = cRow.TemplateId, sectionId = cRow.SectionId, cardId = cRow.CardId, equal = cbyIdEq });
                        }

                        // Test missing CardId (-99999)
                        var firstCard = cardRows[0];
                        var sqlMissing = await sqlRepo.GetProjectImagCardById(firstCard.ProjectId, firstCard.TemplateId, firstCard.SectionId, -99999);
                        var cosMissing = await cosmosRepo.GetProjectImagCardById(firstCard.ProjectId, firstCard.TemplateId, firstCard.SectionId, -99999);
                        bool missingEq = sqlMissing == null && cosMissing == null;
                        checks.Add(new { method = "GetProjectImagCardById", projectId = firstCard.ProjectId, cardId = -99999, equal = missingEq });
                    }
                }
                catch (Exception ex)
                {
                    hadException = true;
                    checks.Add(new { method = "GetProjectImagCardById", equal = false, error = ex.Message });
                }

                // T1: Check every read method had at least one positive row in SQL
                foreach (var kvp in methodPositiveRows)
                {
                    bool hasPositive = kvp.Value > 0;
                    checks.Add(new { method = $"PositiveRowsCheck.{kvp.Key}", equal = hasPositive, rowCount = kvp.Value });
                    if (!hasPositive) allPassed = false;
                }

                // =========================================================================
                // 2. WRITE CHECKS (Cosmos only, writeTest=yes)
                // =========================================================================
                if (string.Equals(writeTest, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    var cleanupList = new List<(Container Container, string Id, PartitionKey Pk)>();

                    try
                    {
                        int testPid = -999;
                        int testTid = -999;

                        // T4: Empty-before-run check inside try
                        var prePtm = await SafeQueryAsync<ProjectTemplateMappingDocument>(ptmContainer, "SELECT * FROM c WHERE c.ProjectId IN (-999, -998)");
                        var prePic = await SafeQueryAsync<ProjectImageCardsDocument>(picContainer, "SELECT * FROM c WHERE c.ProjectId IN (-999, -998) OR c.CardId = 7771");
                        var prePgm = await SafeQueryAsync<ProjectGradingMappingDocument>(pgmContainer, "SELECT * FROM c WHERE c.ProjectId IN (-999, -998)");
                        var prePgcm = await SafeQueryAsync<ProjectGradingConditionMappingDocument>(pgcmContainer, "SELECT * FROM c WHERE c.ProjectId IN (-999, -998)");
                        var preHi = await SafeQueryAsync<H_IReportTemplateDocument>(hiContainer, "SELECT * FROM c WHERE c.ProjectId IN (-999, -998)");
                        var preTic = await SafeQueryAsync<TankImageCardDocument>(ticContainer, "SELECT * FROM c WHERE c.ProjectId IN (-999, -998)");
                        var preGic = await SafeQueryAsync<GenericImageCardDocument>(gicContainer, "SELECT * FROM c WHERE c.ProjectId IN (-999, -998)");
                        var preHipg = await SafeQueryAsync<HandIProjectGradingDocument>(hipgContainer, "SELECT * FROM c WHERE c.ProjectID IN (-999, -998)");
                        var preHipgc = await SafeQueryAsync<HandIProjectGradingConditionsDocument>(hipgcContainer, "SELECT * FROM c WHERE c.ProjectId IN (-999, -998)");
                        var preBu = await SafeQueryAsync<BulkUploadDocument>(buContainer, "SELECT * FROM c WHERE c.imageid = -99988");
                        var preCl = await SafeQueryAsync<CapLogDocument>(clContainer, "SELECT * FROM c WHERE c.AbsProjectId = '-999'");
                        var preId = await SafeQueryAsync<ImageDataDocument>(idContainer, "SELECT * FROM c WHERE c.ImageId IN (-99901, -99921)");
                        var preImg = await SafeQueryAsync<ImagesDocument>(imgContainer, "SELECT * FROM c WHERE c.AssignmentId IN ('DupAssign', 'DelAssign', 'Assign1', 'Assign2') OR c.Id IN (-99911, -99912, -99921)");

                        bool preCheckPassed = prePtm.Count == 0 && prePic.Count == 0 && prePgm.Count == 0 && prePgcm.Count == 0
                            && preHi.Count == 0 && preTic.Count == 0 && preGic.Count == 0 && preHipg.Count == 0 && preHipgc.Count == 0
                            && preBu.Count == 0 && preCl.Count == 0 && preId.Count == 0 && preImg.Count == 0;

                        writeSteps.Add(new { step = "EmptyBeforeRunCheck.Trap4", passed = preCheckPassed,
                            ptm = prePtm.Count, pic = prePic.Count, pgm = prePgm.Count, pgcm = prePgcm.Count,
                            hi = preHi.Count, tic = preTic.Count, gic = preGic.Count, hipg = preHipg.Count, hipgc = preHipgc.Count,
                            bu = preBu.Count, cl = preCl.Count, id = preId.Count, img = preImg.Count });
                        if (!preCheckPassed)
                        {
                            allPassed = false;
                        }
                        else
                        {
                        // 2.1 CreateProjectTemplate -> false and nothing written (Trap 1, T6, P6)
                        var ptmCountBefore = (await SafeQueryAsync<ProjectTemplateMappingDocument>(ptmContainer, "SELECT * FROM c WHERE c.ProjectId = -999")).Count;
                        var ptmTest = new ProjectReportMapping
                        {
                            ProjectId = testPid,
                            TemplateId = testTid,
                            SectionId = Guid.NewGuid(),
                            CreatedDttm = DateTime.UtcNow
                        };
                        var cptRes = await cosmosRepo.CreateProjectTemplate(ptmTest);
                        var ptmCountAfter = (await SafeQueryAsync<ProjectTemplateMappingDocument>(ptmContainer, "SELECT * FROM c WHERE c.ProjectId = -999")).Count;
                        if (ptmCountAfter > ptmCountBefore)
                        {
                            var leakedPtm = await SafeQueryAsync<ProjectTemplateMappingDocument>(ptmContainer, "SELECT * FROM c WHERE c.ProjectId = -999");
                            foreach (var d in leakedPtm) cleanupList.Add((ptmContainer, d.id, new PartitionKey((double)testPid)));
                        }
                        bool cptPassed = !cptRes && ptmCountBefore == ptmCountAfter && ptmCountAfter == 0;
                        writeSteps.Add(new { step = "CreateProjectTemplate.Trap1", passed = cptPassed, returned = cptRes, ptmCountBefore, ptmCountAfter });

                        // 2.2 CreateProjectImageCard: 0/""/1900 -> NULL, ProjectId 0 -> PartitionKey.Null, MinValue -> false
                        // (a) MinValue (T6: count before = count after)
                        var picCountBeforeMin = (await SafeQueryAsync<ProjectImageCardsDocument>(picContainer, "SELECT * FROM c WHERE c.ProjectId = -999")).Count;
                        var picMinDate = new ProjectCardMapping
                        {
                            ProjectId = testPid,
                            TemplateId = testTid,
                            SectionId = Guid.NewGuid(),
                            CreatedDttm = DateTime.MinValue,
                            UpdateDttm = DateTime.UtcNow
                        };
                        var minDateRes = await cosmosRepo.CreateProjectImageCard(picMinDate);
                        var picCountAfterMin = (await SafeQueryAsync<ProjectImageCardsDocument>(picContainer, "SELECT * FROM c WHERE c.ProjectId = -999")).Count;
                        bool minDateOk = !minDateRes && picCountBeforeMin == picCountAfterMin;
                        if (picCountAfterMin > picCountBeforeMin)
                        {
                            var leaked = await SafeQueryAsync<ProjectImageCardsDocument>(picContainer, "SELECT * FROM c WHERE c.ProjectId = -999");
                            foreach (var d in leaked) cleanupList.Add((picContainer, d.id, new PartitionKey((double)testPid)));
                        }

                        // (b) 0 / "" / 1900-01-01 (T7, P7)
                        var testSecGuid = Guid.NewGuid();
                        var picNormal = new ProjectCardMapping
                        {
                            ProjectId = 0,
                            TemplateId = 0,
                            SectionId = testSecGuid,
                            CardId = 0,
                            DescriptionId = 0,
                            AdditionalDescription = "",
                            CurrentCondition = 0,
                            ImageId = 0,
                            CreatedDttm = new DateTime(1900, 1, 1),
                            UpdateDttm = new DateTime(1900, 1, 1),
                            IsDeleted = false
                        };
                        var picRes1 = await cosmosRepo.CreateProjectImageCard(picNormal);
                        var readPics = await SafeQueryAsync<ProjectImageCardsDocument>(
                            picContainer,
                            $"SELECT * FROM c WHERE c.SectionId = '{testSecGuid.ToString().ToLowerInvariant()}'");
                        var picDoc1 = readPics.FirstOrDefault();
                        if (picDoc1 != null) cleanupList.Add((picContainer, picDoc1.id, PartitionKey.Null));

                        bool picNormOk = picRes1 && picDoc1 != null
                            && picDoc1.ProjectId == null
                            && picDoc1.TemplateId == null
                            && string.Equals(picDoc1.SectionId, testSecGuid.ToString().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)
                            && picDoc1.IsDeleted == false
                            && picDoc1.CardId == null
                            && picDoc1.DescriptionId == null
                            && picDoc1.AdditionalDescription == null
                            && picDoc1.CurrentCondition == null
                            && picDoc1.ImageId == null
                            && picDoc1.CreatedDttm == null
                            && picDoc1.UpdateDttm == null;

                        // (c) ProjectId 0 -> PartitionKey.Null (T7: point read via PartitionKey.Null, verify TemplateId 0 -> NULL, SectionId, IsDeleted)
                        var testSecGuid0 = Guid.NewGuid();
                        var pic0 = new ProjectCardMapping
                        {
                            ProjectId = 0,
                            TemplateId = 0,
                            SectionId = testSecGuid0,
                            CardId = 7771,
                            CreatedDttm = DateTime.UtcNow,
                            UpdateDttm = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        var pic0Res = await cosmosRepo.CreateProjectImageCard(pic0);
                        var readPic0 = await SafeQueryAsync<ProjectImageCardsDocument>(
                            picContainer,
                            "SELECT * FROM c WHERE c.CardId = 7771");
                        var picDoc0 = readPic0.FirstOrDefault();
                        ProjectImageCardsDocument? pic0PointRead = null;
                        if (picDoc0 != null)
                        {
                            cleanupList.Add((picContainer, picDoc0.id, PartitionKey.Null));
                            pic0PointRead = await SafeReadAsync<ProjectImageCardsDocument>(picContainer, picDoc0.id, PartitionKey.Null);
                        }
                        bool pic0Ok = pic0Res && pic0PointRead != null
                            && pic0PointRead.ProjectId == null
                            && pic0PointRead.TemplateId == null
                            && string.Equals(pic0PointRead.SectionId, testSecGuid0.ToString().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)
                            && pic0PointRead.IsDeleted == false;

                        // (d) UpdateProjectImageCard -> MinValue rejected (P6), CardId/UpdateDttm (T7), CreatedDttm NULL; new ProjectId -> partition move
                        bool picUpdateOk = false;
                        if (picDoc1 != null)
                        {
                            // P6: MinValue on UpdateProjectImageCard
                            var upMinModel = new ProjectCardMapping
                            {
                                Id = picDoc1.Id,
                                ProjectId = 0,
                                TemplateId = 0,
                                SectionId = testSecGuid,
                                CardId = 99,
                                UpdateDttm = DateTime.MinValue
                            };
                            var upMinRes = await cosmosRepo.UpdateProjectImageCard(upMinModel);
                            var readBackMin = await SafeReadAsync<ProjectImageCardsDocument>(picContainer, picDoc1.id, PartitionKey.Null);
                            bool upMinOk = !upMinRes && readBackMin != null && readBackMin.CardId == null;

                            var nowUtc = DateTime.UtcNow;
                            var updateModel = new ProjectCardMapping
                            {
                                Id = picDoc1.Id,
                                ProjectId = 0,
                                TemplateId = 0,
                                SectionId = testSecGuid,
                                CardId = 55,
                                AdditionalDescription = "UpdatedDesc",
                                UpdateDttm = nowUtc,
                                IsDeleted = false
                            };
                            var upRes = await cosmosRepo.UpdateProjectImageCard(updateModel);
                            var readBackUp = await SafeReadAsync<ProjectImageCardsDocument>(picContainer, picDoc1.id, PartitionKey.Null);
                            bool upFieldOk = upRes && readBackUp != null
                                && readBackUp.CardId == 55
                                && ApproxNowDateEquals(readBackUp.UpdateDttm, nowUtc)
                                && readBackUp.AdditionalDescription == "UpdatedDesc"
                                && readBackUp.CreatedDttm == null;

                            // Partition move to -998
                            updateModel.ProjectId = -998;
                            var moveRes = await cosmosRepo.UpdateProjectImageCard(updateModel);
                            var oldRead = await SafeReadAsync<ProjectImageCardsDocument>(picContainer, picDoc1.id, PartitionKey.Null);
                            var newRead = await SafeReadAsync<ProjectImageCardsDocument>(picContainer, picDoc1.id, new PartitionKey((double)-998));
                            if (newRead != null) cleanupList.Add((picContainer, picDoc1.id, new PartitionKey((double)-998)));
                            bool moveOk = moveRes && oldRead == null && newRead != null && newRead.ProjectId == -998;

                            picUpdateOk = upMinOk && upFieldOk && moveOk;
                        }

                        bool allPicOk = minDateOk && picNormOk && pic0Ok && picUpdateOk;
                        writeSteps.Add(new { step = "CreateAndUpdateProjectImageCard.Trap2_3_4_7", passed = allPicOk, minDateOk, picNormOk, pic0Ok, picUpdateOk });

                        // 2.3 CreateProjectGrading + UpdateProjectGrading (P6: MinValue, T7: Value transformed, RequiredInReport NULL)
                        var pgmBeforeCount = (await SafeQueryAsync<ProjectGradingMappingDocument>(pgmContainer, "SELECT * FROM c WHERE c.ProjectId = -999")).Count;
                        var pgmMinModel = new ProjectGradingMapping
                        {
                            ProjectId = testPid,
                            TemplateId = testTid,
                            SectionId = Guid.NewGuid(),
                            GradingId = 902,
                            Value = "val",
                            CreatedDttm = DateTime.MinValue,
                            UpdateDttm = DateTime.UtcNow
                        };
                        var cpgMinRes = await cosmosRepo.CreateProjectGrading(pgmMinModel);
                        var pgmAfterCount = (await SafeQueryAsync<ProjectGradingMappingDocument>(pgmContainer, "SELECT * FROM c WHERE c.ProjectId = -999")).Count;
                        if (pgmAfterCount > pgmBeforeCount)
                        {
                            var leakedPgm = await SafeQueryAsync<ProjectGradingMappingDocument>(pgmContainer, "SELECT * FROM c WHERE c.ProjectId = -999");
                            foreach (var d in leakedPgm) cleanupList.Add((pgmContainer, d.id, new PartitionKey((double)testPid)));
                        }
                        bool cpgMinOk = !cpgMinRes && pgmBeforeCount == pgmAfterCount;

                        var pgmSecGuid = Guid.NewGuid();
                        var pgmModel = new ProjectGradingMapping
                        {
                            ProjectId = testPid,
                            TemplateId = testTid,
                            SectionId = pgmSecGuid,
                            GradingId = 901,
                            Value = "12345678901234567890",
                            CreatedDttm = DateTime.UtcNow,
                            UpdateDttm = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        var cpgRes = await cosmosRepo.CreateProjectGrading(pgmModel);
                        var readPgmDocs = await SafeQueryAsync<ProjectGradingMappingDocument>(
                            pgmContainer,
                            "SELECT * FROM c WHERE c.ProjectId = -999 AND c.TemplateId = -999",
                            new PartitionKey((double)testPid));
                        var pgmDoc = readPgmDocs.FirstOrDefault();
                        if (pgmDoc != null) cleanupList.Add((pgmContainer, pgmDoc.id, new PartitionKey((double)testPid)));

                        var pgmList = await cosmosRepo.GetProjectGrading(testPid, testTid, pgmSecGuid);
                        var readPgmItem = pgmList?.FirstOrDefault();
                        bool cpgOk = cpgMinOk && cpgRes && pgmDoc != null && pgmDoc.RequiredInReport == null
                            && readPgmItem != null && readPgmItem.Value == "1234567890 123456789";

                        // UpdateProjectGrading (P6: MinValue rejected, doc unchanged)
                        bool upgOk = false;
                        if (pgmDoc != null)
                        {
                            var upgMinModel = new ProjectGradingMapping
                            {
                                Id = pgmDoc.Id,
                                ProjectId = testPid,
                                TemplateId = testTid,
                                SectionId = pgmSecGuid,
                                GradingId = 901,
                                Value = "ShouldNotUpdate",
                                UpdateDttm = DateTime.MinValue
                            };
                            var upgMinRes = await cosmosRepo.UpdateProjectGrading(upgMinModel);
                            var readPgmMinDoc = await SafeReadAsync<ProjectGradingMappingDocument>(pgmContainer, pgmDoc.id, new PartitionKey((double)testPid));
                            bool upgMinOk = !upgMinRes && readPgmMinDoc != null && readPgmMinDoc.Value == "12345678901234567890";

                            var upgModel = new ProjectGradingMapping
                            {
                                Id = pgmDoc.Id,
                                ProjectId = testPid,
                                TemplateId = testTid,
                                SectionId = pgmSecGuid,
                                GradingId = 901,
                                Value = "NewVal",
                                UpdateDttm = DateTime.UtcNow,
                                IsDeleted = false
                            };
                            var upgRes = await cosmosRepo.UpdateProjectGrading(upgModel);
                            var readPgmDoc2 = await SafeReadAsync<ProjectGradingMappingDocument>(pgmContainer, pgmDoc.id, new PartitionKey((double)testPid));
                            upgOk = upgMinOk && upgRes && readPgmDoc2 != null && readPgmDoc2.Value == "NewVal" && readPgmDoc2.CreatedDttm == null && readPgmDoc2.RequiredInReport == null;
                        }

                        bool allPgmOk = cpgOk && upgOk;
                        writeSteps.Add(new { step = "CreateAndUpdateProjectGrading.Trap2_6_25", passed = allPgmOk, cpgOk, upgOk });

                        // 2.4 CreateProjectGradingConditionMapping + UpdateProjectGradingConditionMapping (P6: MinValue, T7: GradingConditionValue true/false)
                        var pgcmBeforeCount = (await SafeQueryAsync<ProjectGradingConditionMappingDocument>(pgcmContainer, "SELECT * FROM c WHERE c.ProjectId = -999")).Count;
                        var pgcmMinModel = new ProjectGradingConditionMapping
                        {
                            ProjectId = testPid,
                            TemplateId = testTid,
                            SectionId = Guid.NewGuid(),
                            GradingId = 902,
                            GradingConditionLabel = "Min",
                            GradingConditionValue = true,
                            CreatedDttm = DateTime.MinValue,
                            UpdateDttm = DateTime.UtcNow
                        };
                        var cpgcmMinRes = await cosmosRepo.CreateProjectGradingConditionMapping(pgcmMinModel);
                        var pgcmAfterCount = (await SafeQueryAsync<ProjectGradingConditionMappingDocument>(pgcmContainer, "SELECT * FROM c WHERE c.ProjectId = -999")).Count;
                        if (pgcmAfterCount > pgcmBeforeCount)
                        {
                            var leakedPgcm = await SafeQueryAsync<ProjectGradingConditionMappingDocument>(pgcmContainer, "SELECT * FROM c WHERE c.ProjectId = -999");
                            foreach (var d in leakedPgcm) cleanupList.Add((pgcmContainer, d.id, new PartitionKey((double)testPid)));
                        }
                        bool cpgcmMinOk = !cpgcmMinRes && pgcmBeforeCount == pgcmAfterCount;

                        var pgcmSecGuid = Guid.NewGuid();
                        var pgcmModel = new ProjectGradingConditionMapping
                        {
                            ProjectId = testPid,
                            TemplateId = testTid,
                            SectionId = pgcmSecGuid,
                            GradingId = 901,
                            GradingConditionLabel = "InitialLabel",
                            GradingConditionValue = true,
                            CreatedDttm = DateTime.UtcNow,
                            UpdateDttm = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        var cpgcmRes = await cosmosRepo.CreateProjectGradingConditionMapping(pgcmModel);
                        var readPgcmDocs = await SafeQueryAsync<ProjectGradingConditionMappingDocument>(
                            pgcmContainer,
                            "SELECT * FROM c WHERE c.ProjectId = -999 AND c.TemplateId = -999",
                            new PartitionKey((double)testPid));
                        var pgcmDoc = readPgcmDocs.FirstOrDefault();
                        if (pgcmDoc != null) cleanupList.Add((pgcmContainer, pgcmDoc.id, new PartitionKey((double)testPid)));

                        bool cpgcmOk = cpgcmMinOk && cpgcmRes && pgcmDoc != null && pgcmDoc.GradingConditionLabel == "InitialLabel" && pgcmDoc.GradingConditionValue == true;

                        bool upgcmOk = false;
                        if (pgcmDoc != null)
                        {
                            var upgcmMinModel = new ProjectGradingConditionMapping
                            {
                                Id = pgcmDoc.Id,
                                ProjectId = testPid,
                                TemplateId = testTid,
                                SectionId = pgcmSecGuid,
                                GradingId = 901,
                                GradingConditionLabel = "ShouldNotUpdate",
                                UpdateDttm = DateTime.MinValue
                            };
                            var upgcmMinRes = await cosmosRepo.UpdateProjectGradingConditionMapping(upgcmMinModel);
                            var readPgcmMinDoc = await SafeReadAsync<ProjectGradingConditionMappingDocument>(pgcmContainer, pgcmDoc.id, new PartitionKey((double)testPid));
                            bool upgcmMinOk = !upgcmMinRes && readPgcmMinDoc != null && readPgcmMinDoc.GradingConditionLabel == "InitialLabel";

                            var upgcmModel = new ProjectGradingConditionMapping
                            {
                                Id = pgcmDoc.Id,
                                ProjectId = testPid,
                                TemplateId = testTid,
                                SectionId = pgcmSecGuid,
                                GradingId = 901,
                                GradingConditionLabel = "UpdatedLabel",
                                GradingConditionValue = false,
                                UpdateDttm = DateTime.UtcNow,
                                IsDeleted = false
                            };
                            var upgcmRes = await cosmosRepo.UpdateProjectGradingConditionMapping(upgcmModel);
                            var readPgcmDoc2 = await SafeReadAsync<ProjectGradingConditionMappingDocument>(pgcmContainer, pgcmDoc.id, new PartitionKey((double)testPid));
                            upgcmOk = upgcmMinOk && upgcmRes && readPgcmDoc2 != null && readPgcmDoc2.GradingConditionLabel == "UpdatedLabel" && readPgcmDoc2.GradingConditionValue == false && readPgcmDoc2.CreatedDttm == null;
                        }

                        bool allPgcmOk = cpgcmOk && upgcmOk;
                        writeSteps.Add(new { step = "CreateAndUpdateProjectGradingConditionMapping.Trap2_7", passed = allPgcmOk, cpgcmOk, upgcmOk });

                        // 2.5 UpdateProjectTemplate on a directly created test document (P6: MinValue)
                        var testPtmId = Guid.NewGuid().ToString().ToLowerInvariant();
                        var testPtmDoc = new ProjectTemplateMappingDocument
                        {
                            id = testPtmId,
                            Id = testPtmId,
                            ProjectId = testPid,
                            TemplateId = testTid,
                            SectionId = Guid.NewGuid().ToString().ToLowerInvariant(),
                            Notes = "InitialNotes",
                            CreatedDttm = DateTime.UtcNow,
                            UpdatedDttm = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        await ptmContainer.CreateItemAsync(testPtmDoc, new PartitionKey((double)testPid));
                        cleanupList.Add((ptmContainer, testPtmId, new PartitionKey((double)testPid)));

                        var uptMinModel = new ProjectReportMapping
                        {
                            Id = Guid.Parse(testPtmId),
                            ProjectId = testPid,
                            TemplateId = testTid,
                            SectionId = Guid.Parse(testPtmDoc.SectionId!),
                            Notes = "ShouldNotUpdate",
                            UpdateDttm = DateTime.MinValue
                        };
                        var uptMinRes = await cosmosRepo.UpdateProjectTemplate(uptMinModel);
                        var readPtmMinBack = await SafeReadAsync<ProjectTemplateMappingDocument>(ptmContainer, testPtmId, new PartitionKey((double)testPid));
                        bool uptMinOk = !uptMinRes && readPtmMinBack != null && readPtmMinBack.Notes == "InitialNotes";

                        var uptModel = new ProjectReportMapping
                        {
                            Id = Guid.Parse(testPtmId),
                            ProjectId = testPid,
                            TemplateId = testTid,
                            SectionId = Guid.Parse(testPtmDoc.SectionId!),
                            Notes = "UpdatedNotes",
                            UpdateDttm = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        var uptRes = await cosmosRepo.UpdateProjectTemplate(uptModel);
                        var readPtmBack = await SafeReadAsync<ProjectTemplateMappingDocument>(ptmContainer, testPtmId, new PartitionKey((double)testPid));
                        bool uptOk = uptMinOk && uptRes && readPtmBack != null && readPtmBack.Notes == "UpdatedNotes" && readPtmBack.CreatedDttm == null;
                        writeSteps.Add(new { step = "UpdateProjectTemplate.Trap2", passed = uptOk, uptRes, createdDttmNull = (readPtmBack?.CreatedDttm == null) });

                        // 2.6 CreateHIReport + UpdateHIReport (P6: MinValue, P7: all columns read back, Trap 19/20, T6, T7)
                        var hiBeforeCount = (await SafeQueryAsync<H_IReportTemplateDocument>(hiContainer, "SELECT * FROM c WHERE c.ProjectId = -999")).Count;
                        var hiMinModel = new HandIReport
                        {
                            Id = Guid.NewGuid(),
                            ProjectId = testPid,
                            CompanyName = "MinCompany",
                            CreatedDttm = DateTime.MinValue,
                            UpdatedDttm = DateTime.UtcNow
                        };
                        var chiMinRes = await cosmosRepo.CreateHIReport(hiMinModel);
                        var hiAfterCount = (await SafeQueryAsync<H_IReportTemplateDocument>(hiContainer, "SELECT * FROM c WHERE c.ProjectId = -999")).Count;
                        if (hiAfterCount > hiBeforeCount)
                        {
                            var leakedHi = await SafeQueryAsync<H_IReportTemplateDocument>(hiContainer, "SELECT * FROM c WHERE c.ProjectId = -999");
                            foreach (var d in leakedHi) cleanupList.Add((hiContainer, d.id, new PartitionKey((double)testPid)));
                        }
                        bool chiMinOk = !chiMinRes && hiBeforeCount == hiAfterCount;

                        var testHiGuid = Guid.NewGuid();
                        var testHiIdStr = testHiGuid.ToString().ToLowerInvariant();
                        var hiDateNow = DateTime.UtcNow;
                        var hiModel = new HandIReport
                        {
                            Id = testHiGuid,
                            ProjectId = testPid,
                            CompanyName = "InitialCompany",
                            CertificateNumber = "CERT-001",
                            ExpiryDate = hiDateNow.Date,
                            ThicknessMeasurementA = "TMA",
                            ThicknessMeasurementB = "TMB",
                            ThicknessMeasurementC = "TMC",
                            AdditionalSurveyorComments = "Surveyor Comments",
                            MaintenanceExecuted = "Maintenance Executed",
                            Accommodation = "Accommodation",
                            PhotoGraphs = "Photos",
                            OperationalTestLocation = "Test Location",
                            OperationalTestStart = hiDateNow.Date,
                            OperationalTestFinish = hiDateNow.Date.AddDays(1),
                            OptionalTestSatisFactory = "Satisfactory",
                            AssociatedReportNumber = "ARN-1",
                            AssociatedReportDate = "2026-11-19",
                            Remarks = "Test Remarks",
                            LeadSurveyorName = "Lead Surv",
                            LeadSurveyorNumber = "LS-01",
                            SurveyorName = "Surv One",
                            SurveyorNumber = "S-01",
                            LastVisitDate = hiDateNow.Date,
                            IndividualA = "Ind A",
                            IndividualB = "Ind B",
                            IndividualC = "Ind C",
                            IndividualD = "Ind D",
                            IndividualE = "Ind E",
                            IndividualF = "Ind F",
                            SubstantialCorrosionRemaining = "SCR-1",
                            StatutoryDeficiencies = "SD-1",
                            FoundSatisfactory = "FS-1",
                            UnderRemarks = "UR-1",
                            ScouringOrPitting = "SOP-1",
                            BottomPlattingScouringOrPitting = "BPSOP-1",
                            IsActive = true,
                            IsDeleted = false,
                            CreatedDttm = hiDateNow,
                            UpdatedDttm = hiDateNow
                        };
                        var chiRes = await cosmosRepo.CreateHIReport(hiModel);
                        cleanupList.Add((hiContainer, testHiIdStr, new PartitionKey((double)testPid)));

                        // Read back fully per T7, P7 (all columns including CreatedDttm and UpdatedDttm)
                        var createdHiDoc = await SafeReadAsync<H_IReportTemplateDocument>(hiContainer, testHiIdStr, new PartitionKey((double)testPid));
                        bool chiReadBackOk = createdHiDoc != null
                            && createdHiDoc.id == testHiIdStr
                            && createdHiDoc.Id == testHiIdStr
                            && createdHiDoc.ProjectId == testPid
                            && createdHiDoc.CompanyName == "InitialCompany"
                            && createdHiDoc.CertificateNumber == "CERT-001"
                            && createdHiDoc.ExpiryDate == hiDateNow.Date
                            && createdHiDoc.ThicknessMeasurementA == "TMA"
                            && createdHiDoc.ThicknessMeasurementB == "TMB"
                            && createdHiDoc.ThicknessMeasurementC == "TMC"
                            && createdHiDoc.AdditionalSurveyorComments == "Surveyor Comments"
                            && createdHiDoc.MaintenanceExecuted == "Maintenance Executed"
                            && createdHiDoc.Accommodation == "Accommodation"
                            && createdHiDoc.PhotoGraphs == "Photos"
                            && createdHiDoc.OperationalTestLocation == "Test Location"
                            && createdHiDoc.OperationalTestStart == hiDateNow.Date
                            && createdHiDoc.OperationalTestFinish == hiDateNow.Date.AddDays(1)
                            && createdHiDoc.OptionalTestSatisFactory == "Satisfactory"
                            && createdHiDoc.AssociatedReportNumber == "ARN-1"
                            && createdHiDoc.AssociatedReportDate == "2026-11-19"
                            && createdHiDoc.Remarks == "Test Remarks"
                            && createdHiDoc.LeadSurveyorName == "Lead Surv"
                            && createdHiDoc.LeadSurveyorNumber == "LS-01"
                            && createdHiDoc.SurveyorName == "Surv One"
                            && createdHiDoc.SurveyorNumber == "S-01"
                            && createdHiDoc.LastVisitDate == hiDateNow.Date
                            && createdHiDoc.IndividualA == "Ind A"
                            && createdHiDoc.IndividualB == "Ind B"
                            && createdHiDoc.IndividualC == "Ind C"
                            && createdHiDoc.IndividualD == "Ind D"
                            && createdHiDoc.IndividualE == "Ind E"
                            && createdHiDoc.IndividualF == "Ind F"
                            && createdHiDoc.SubstantialCorrosionRemaining == "SCR-1"
                            && createdHiDoc.StatutoryDeficiencies == "SD-1"
                            && createdHiDoc.FoundSatisfactory == "FS-1"
                            && createdHiDoc.UnderRemarks == "UR-1"
                            && createdHiDoc.ScouringOrPitting == "SOP-1"
                            && createdHiDoc.BottomPlattingScouringOrPitting == "BPSOP-1"
                            && createdHiDoc.IsActive == true
                            && createdHiDoc.IsDeleted == false
                            && createdHiDoc.CreatedDttm == hiDateNow
                            && createdHiDoc.UpdatedDttm == hiDateNow
                            && createdHiDoc.OthersDescription == null;

                        // Duplicate Id in project -998 -> false (Trap 20, T6: count before = count after = 0)
                        var hi998Before = (await SafeQueryAsync<H_IReportTemplateDocument>(hiContainer, "SELECT * FROM c WHERE c.ProjectId = -998")).Count;
                        var hiDupModel = new HandIReport
                        {
                            Id = testHiGuid,
                            ProjectId = -998,
                            CompanyName = "DupCompany",
                            IsActive = true,
                            IsDeleted = false,
                            CreatedDttm = DateTime.UtcNow,
                            UpdatedDttm = DateTime.UtcNow
                        };
                        var chiDupRes = await cosmosRepo.CreateHIReport(hiDupModel);
                        var hi998After = (await SafeQueryAsync<H_IReportTemplateDocument>(hiContainer, "SELECT * FROM c WHERE c.ProjectId = -998")).Count;
                        var checkDupHi = await SafeReadAsync<H_IReportTemplateDocument>(hiContainer, testHiIdStr, new PartitionKey((double)-998));
                        if (checkDupHi != null) cleanupList.Add((hiContainer, testHiIdStr, new PartitionKey((double)-998)));
                        bool dupBlockedOk = !chiDupRes && hi998Before == hi998After && checkDupHi == null;

                        // Update with IsActive=false -> false (Trap 19, T6: doc unchanged, same _etag)
                        var hiBeforeInactiveResp = await hiContainer.ReadItemAsync<H_IReportTemplateDocument>(testHiIdStr, new PartitionKey((double)testPid));
                        var hiBeforeInactive = hiBeforeInactiveResp.Resource;
                        hiModel.IsActive = false;
                        var uhiInactiveRes = await cosmosRepo.UpdateHIReport(hiModel);
                        var hiAfterInactiveResp = await hiContainer.ReadItemAsync<H_IReportTemplateDocument>(testHiIdStr, new PartitionKey((double)testPid));
                        var hiAfterInactive = hiAfterInactiveResp.Resource;
                        bool inactiveBlockedOk = !uhiInactiveRes && hiBeforeInactive != null && hiAfterInactive != null
                            && hiBeforeInactiveResp.ETag == hiAfterInactiveResp.ETag;

                        // P6: UpdateHIReport with UpdatedDttm = DateTime.MinValue -> false, doc unchanged, same ETag
                        var hiBeforeMinResp = await hiContainer.ReadItemAsync<H_IReportTemplateDocument>(testHiIdStr, new PartitionKey((double)testPid));
                        var uhiMinModel = new HandIReport
                        {
                            Id = testHiGuid,
                            ProjectId = testPid,
                            CompanyName = "MinUpdateCompany",
                            IsActive = true,
                            UpdatedDttm = DateTime.MinValue
                        };
                        var uhiMinRes = await cosmosRepo.UpdateHIReport(uhiMinModel);
                        var hiAfterMinResp = await hiContainer.ReadItemAsync<H_IReportTemplateDocument>(testHiIdStr, new PartitionKey((double)testPid));
                        bool uhiMinOk = !uhiMinRes && hiBeforeMinResp.ETag == hiAfterMinResp.ETag;

                        // Seed OthersDescription non-null before update per T7, then update -> OthersDescription and CreatedDttm NULL
                        if (hiAfterInactive != null)
                        {
                            hiAfterInactive.OthersDescription = "seeded_not_null";
                            await hiContainer.ReplaceItemAsync(hiAfterInactive, hiAfterInactive.id, new PartitionKey((double)testPid));
                        }

                        hiModel.IsActive = true;
                        hiModel.CompanyName = "UpdatedCompany";
                        hiModel.UpdatedDttm = DateTime.UtcNow;
                        var uhiRes = await cosmosRepo.UpdateHIReport(hiModel);
                        var readHiBack = await SafeReadAsync<H_IReportTemplateDocument>(hiContainer, testHiIdStr, new PartitionKey((double)testPid));
                        bool uhiOk = uhiMinOk && uhiRes && readHiBack != null
                            && readHiBack.CompanyName == "UpdatedCompany"
                            && readHiBack.CreatedDttm == null
                            && readHiBack.OthersDescription == null;

                        bool allHiOk = chiMinOk && chiRes && chiReadBackOk && dupBlockedOk && inactiveBlockedOk && uhiOk;
                        writeSteps.Add(new { step = "CreateAndUpdateHIReport.Trap2_19_20_T6_T7_P6_P7", passed = allHiOk, chiMinOk, chiRes, chiReadBackOk, dupBlockedOk, inactiveBlockedOk, uhiOk });

                        // 2.7 DeleteProjectTemplate(-999, -997) on ALL 9 containers + second template untouched (T5, P8)
                        int delTid = -997;
                        var dateSeedCreated = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
                        var dateSeedUpdated = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

                        // Seed second template -998 for project -999 in 6 containers: PTM, PIC, PGM, PGCM (T5), TIC, GIC
                        var ptm998Id = Guid.NewGuid().ToString().ToLowerInvariant();
                        var ptm998 = new ProjectTemplateMappingDocument { id = ptm998Id, Id = ptm998Id, ProjectId = testPid, TemplateId = -998, IsDeleted = false, CreatedDttm = dateSeedCreated, UpdatedDttm = dateSeedUpdated };
                        await ptmContainer.CreateItemAsync(ptm998, new PartitionKey((double)testPid));
                        cleanupList.Add((ptmContainer, ptm998Id, new PartitionKey((double)testPid)));

                        var pic998Id = Guid.NewGuid().ToString().ToLowerInvariant();
                        var pic998 = new ProjectImageCardsDocument { id = pic998Id, Id = -9981, ProjectId = testPid, TemplateId = -998, IsDeleted = false, CreatedDttm = dateSeedCreated, UpdateDttm = dateSeedUpdated };
                        await picContainer.CreateItemAsync(pic998, new PartitionKey((double)testPid));
                        cleanupList.Add((picContainer, pic998Id, new PartitionKey((double)testPid)));

                        var pgm998Id = Guid.NewGuid().ToString().ToLowerInvariant();
                        var pgm998 = new ProjectGradingMappingDocument { id = pgm998Id, Id = -9982, ProjectId = testPid, TemplateId = -998, IsDeleted = false, CreatedDttm = dateSeedCreated, UpdatedDttm = dateSeedUpdated };
                        await pgmContainer.CreateItemAsync(pgm998, new PartitionKey((double)testPid));
                        cleanupList.Add((pgmContainer, pgm998Id, new PartitionKey((double)testPid)));

                        var pgcm998Id = Guid.NewGuid().ToString().ToLowerInvariant();
                        var pgcm998 = new ProjectGradingConditionMappingDocument { id = pgcm998Id, Id = -9989, ProjectId = testPid, TemplateId = -998, IsDeleted = false, CreatedDttm = dateSeedCreated, UpdatedDttm = dateSeedUpdated };
                        await pgcmContainer.CreateItemAsync(pgcm998, new PartitionKey((double)testPid));
                        cleanupList.Add((pgcmContainer, pgcm998Id, new PartitionKey((double)testPid)));

                        var tic998Id = Guid.NewGuid().ToString().ToLowerInvariant();
                        var tic998 = new TankImageCardDocument { id = tic998Id, Id = -9983, ProjectId = testPid, TemplateId = -998, IsActive = true, CreatedDttm = dateSeedCreated, UpdatedDttm = dateSeedUpdated };
                        await ticContainer.CreateItemAsync(tic998, new PartitionKey((double)testPid));
                        cleanupList.Add((ticContainer, tic998Id, new PartitionKey((double)testPid)));

                        var gic998Id = Guid.NewGuid().ToString().ToLowerInvariant();
                        var gic998 = new GenericImageCardDocument { id = gic998Id, Id = -9984, ProjectId = testPid, TemplateId = -998, IsActive = true, IsDeleted = false, CreatedDttm = dateSeedCreated, UpdatedDttm = dateSeedUpdated };
                        await gicContainer.CreateItemAsync(gic998, new PartitionKey((double)testPid));
                        cleanupList.Add((gicContainer, gic998Id, new PartitionKey((double)testPid)));

                        // Seed documents for template -997 for ALL 9 containers (ProjectId = -999, TemplateId = -997)
                        var ptm997Id = Guid.NewGuid().ToString().ToLowerInvariant();
                        var ptm997 = new ProjectTemplateMappingDocument { id = ptm997Id, Id = ptm997Id, ProjectId = testPid, TemplateId = delTid, IsDeleted = false, CreatedDttm = dateSeedCreated, UpdatedDttm = dateSeedUpdated };
                        await ptmContainer.CreateItemAsync(ptm997, new PartitionKey((double)testPid));
                        cleanupList.Add((ptmContainer, ptm997Id, new PartitionKey((double)testPid)));

                        var pic997Id = Guid.NewGuid().ToString().ToLowerInvariant();
                        var pic997 = new ProjectImageCardsDocument { id = pic997Id, Id = -9971, ProjectId = testPid, TemplateId = delTid, IsDeleted = false, CreatedDttm = dateSeedCreated, UpdateDttm = dateSeedUpdated };
                        await picContainer.CreateItemAsync(pic997, new PartitionKey((double)testPid));
                        cleanupList.Add((picContainer, pic997Id, new PartitionKey((double)testPid)));

                        var pgm997Id = Guid.NewGuid().ToString().ToLowerInvariant();
                        var pgm997 = new ProjectGradingMappingDocument { id = pgm997Id, Id = -9972, ProjectId = testPid, TemplateId = delTid, IsDeleted = false, CreatedDttm = dateSeedCreated, UpdatedDttm = dateSeedUpdated };
                        await pgmContainer.CreateItemAsync(pgm997, new PartitionKey((double)testPid));
                        cleanupList.Add((pgmContainer, pgm997Id, new PartitionKey((double)testPid)));

                        var pgcm997Id = Guid.NewGuid().ToString().ToLowerInvariant();
                        var pgcm997 = new ProjectGradingConditionMappingDocument { id = pgcm997Id, Id = -9978, ProjectId = testPid, TemplateId = delTid, IsDeleted = false, CreatedDttm = dateSeedCreated, UpdatedDttm = dateSeedUpdated };
                        await pgcmContainer.CreateItemAsync(pgcm997, new PartitionKey((double)testPid));
                        cleanupList.Add((pgcmContainer, pgcm997Id, new PartitionKey((double)testPid)));

                        var tic997Id = Guid.NewGuid().ToString().ToLowerInvariant();
                        var tic997 = new TankImageCardDocument { id = tic997Id, Id = -9973, ProjectId = testPid, TemplateId = delTid, IsActive = true, CreatedDttm = dateSeedCreated, UpdatedDttm = dateSeedUpdated };
                        await ticContainer.CreateItemAsync(tic997, new PartitionKey((double)testPid));
                        cleanupList.Add((ticContainer, tic997Id, new PartitionKey((double)testPid)));

                        var gic997Id = Guid.NewGuid().ToString().ToLowerInvariant();
                        var gic997 = new GenericImageCardDocument { id = gic997Id, Id = -9974, ProjectId = testPid, TemplateId = delTid, IsActive = true, IsDeleted = false, CreatedDttm = dateSeedCreated, UpdatedDttm = dateSeedUpdated };
                        await gicContainer.CreateItemAsync(gic997, new PartitionKey((double)testPid));
                        cleanupList.Add((gicContainer, gic997Id, new PartitionKey((double)testPid)));

                        var hi997Id = Guid.NewGuid().ToString().ToLowerInvariant();
                        var hi997 = new H_IReportTemplateDocument { id = hi997Id, Id = hi997Id, ProjectId = testPid, IsActive = true, IsDeleted = false, CreatedDttm = dateSeedCreated, UpdatedDttm = dateSeedUpdated };
                        await hiContainer.CreateItemAsync(hi997, new PartitionKey((double)testPid));
                        cleanupList.Add((hiContainer, hi997Id, new PartitionKey((double)testPid)));

                        var hipgId = Guid.NewGuid().ToString().ToLowerInvariant();
                        var hipg = new HandIProjectGradingDocument { id = hipgId, Id = -9995, ProjectID = testPid, IsActive = true, IsDeleted = false, CreatedOn = dateSeedCreated, ModifiedOn = dateSeedUpdated };
                        await hipgContainer.CreateItemAsync(hipg, new PartitionKey((double)testPid));
                        cleanupList.Add((hipgContainer, hipgId, new PartitionKey((double)testPid)));

                        var hipgcId = Guid.NewGuid().ToString().ToLowerInvariant();
                        var hipgc = new HandIProjectGradingConditionsDocument { id = hipgcId, Id = -9996, ProjectId = testPid, IsActive = true, IsDeleted = false, CreatedOn = dateSeedCreated, ModifiedOn = dateSeedUpdated };
                        await hipgcContainer.CreateItemAsync(hipgc, new PartitionKey((double)testPid));
                        cleanupList.Add((hipgcContainer, hipgcId, new PartitionKey((double)testPid)));

                        // Call DeleteProjectTemplate(-999, -997)
                        var dptRes = await cosmosRepo.DeleteProjectTemplate(testPid, delTid);

                        // Read back ALL 9 containers for -999/-997: verify soft-deleted / deactivated, and NO date column changed (T5, P8)
                        var rPtm = await SafeReadAsync<ProjectTemplateMappingDocument>(ptmContainer, ptm997Id, new PartitionKey((double)testPid));
                        var rPic = await SafeReadAsync<ProjectImageCardsDocument>(picContainer, pic997Id, new PartitionKey((double)testPid));
                        var rPgm = await SafeReadAsync<ProjectGradingMappingDocument>(pgmContainer, pgm997Id, new PartitionKey((double)testPid));
                        var rPgcm = await SafeReadAsync<ProjectGradingConditionMappingDocument>(pgcmContainer, pgcm997Id, new PartitionKey((double)testPid));
                        var rHi = await SafeReadAsync<H_IReportTemplateDocument>(hiContainer, hi997Id, new PartitionKey((double)testPid));
                        var rTic = await SafeReadAsync<TankImageCardDocument>(ticContainer, tic997Id, new PartitionKey((double)testPid));
                        var rGic = await SafeReadAsync<GenericImageCardDocument>(gicContainer, gic997Id, new PartitionKey((double)testPid));
                        var rHipg = await SafeReadAsync<HandIProjectGradingDocument>(hipgContainer, hipgId, new PartitionKey((double)testPid));
                        var rHipgc = await SafeReadAsync<HandIProjectGradingConditionsDocument>(hipgcContainer, hipgcId, new PartitionKey((double)testPid));

                        bool flagged999 = rPtm?.IsDeleted == true
                            && rPic?.IsDeleted == true
                            && rPgm?.IsDeleted == true
                            && rPgcm?.IsDeleted == true
                            && rHi?.IsDeleted == true
                            && rTic?.IsActive == false
                            && rGic?.IsActive == false && rGic?.IsDeleted == true
                            && rHipg?.IsActive == false && rHipg?.IsDeleted == true
                            && rHipgc?.IsActive == false && rHipgc?.IsDeleted == true;

                        bool datesUnchanged999 = rPtm?.CreatedDttm == dateSeedCreated && rPtm?.UpdatedDttm == dateSeedUpdated
                            && rPic?.CreatedDttm == dateSeedCreated && rPic?.UpdateDttm == dateSeedUpdated
                            && rPgm?.CreatedDttm == dateSeedCreated && rPgm?.UpdatedDttm == dateSeedUpdated
                            && rPgcm?.CreatedDttm == dateSeedCreated && rPgcm?.UpdatedDttm == dateSeedUpdated
                            && rHi?.CreatedDttm == dateSeedCreated && rHi?.UpdatedDttm == dateSeedUpdated
                            && rTic?.CreatedDttm == dateSeedCreated && rTic?.UpdatedDttm == dateSeedUpdated
                            && rGic?.CreatedDttm == dateSeedCreated && rGic?.UpdatedDttm == dateSeedUpdated
                            && rHipg?.CreatedOn == dateSeedCreated && rHipg?.ModifiedOn == dateSeedUpdated
                            && rHipgc?.CreatedOn == dateSeedCreated && rHipgc?.ModifiedOn == dateSeedUpdated;

                        // Second template -998 untouched across all 6 containers (T5, P8 dates check)
                        var rPtm998 = await SafeReadAsync<ProjectTemplateMappingDocument>(ptmContainer, ptm998Id, new PartitionKey((double)testPid));
                        var rPic998 = await SafeReadAsync<ProjectImageCardsDocument>(picContainer, pic998Id, new PartitionKey((double)testPid));
                        var rPgm998 = await SafeReadAsync<ProjectGradingMappingDocument>(pgmContainer, pgm998Id, new PartitionKey((double)testPid));
                        var rPgcm998 = await SafeReadAsync<ProjectGradingConditionMappingDocument>(pgcmContainer, pgcm998Id, new PartitionKey((double)testPid));
                        var rTic998 = await SafeReadAsync<TankImageCardDocument>(ticContainer, tic998Id, new PartitionKey((double)testPid));
                        var rGic998 = await SafeReadAsync<GenericImageCardDocument>(gicContainer, gic998Id, new PartitionKey((double)testPid));

                        bool untouched998 = rPtm998?.IsDeleted == false
                            && rPic998?.IsDeleted == false
                            && rPgm998?.IsDeleted == false
                            && rPgcm998?.IsDeleted == false
                            && rTic998?.IsActive == true
                            && rGic998?.IsActive == true && rGic998?.IsDeleted == false;

                        bool datesUnchanged998 = rPtm998?.CreatedDttm == dateSeedCreated && rPtm998?.UpdatedDttm == dateSeedUpdated
                            && rPic998?.CreatedDttm == dateSeedCreated && rPic998?.UpdateDttm == dateSeedUpdated
                            && rPgm998?.CreatedDttm == dateSeedCreated && rPgm998?.UpdatedDttm == dateSeedUpdated
                            && rPgcm998?.CreatedDttm == dateSeedCreated && rPgcm998?.UpdatedDttm == dateSeedUpdated
                            && rTic998?.CreatedDttm == dateSeedCreated && rTic998?.UpdatedDttm == dateSeedUpdated
                            && rGic998?.CreatedDttm == dateSeedCreated && rGic998?.UpdatedDttm == dateSeedUpdated;

                        bool dptOk = dptRes && flagged999 && datesUnchanged999 && untouched998 && datesUnchanged998;
                        writeSteps.Add(new { step = "DeleteProjectTemplate.All9Containers.Trap18_T5_P8", passed = dptOk, dptRes, flagged999, datesUnchanged999, untouched998, datesUnchanged998 });

                        // 2.8 AddEditImageNew (Trap 14)
                        int testImgId = -99901;
                        var aeiRes1 = await cosmosRepo.AddEditImageNew(testImgId, "Base64_Original");
                        var aeiDocs = await SafeQueryAsync<ImageDataDocument>(idContainer, $"SELECT * FROM c WHERE c.ImageId = {testImgId}");
                        var aeiDoc = aeiDocs.FirstOrDefault();
                        if (aeiDoc != null) cleanupList.Add((idContainer, aeiDoc.id, new PartitionKey(aeiDoc.id)));

                        var aeiRes2 = await cosmosRepo.AddEditImageNew(testImgId, "Base64_Updated");
                        var aeiDoc2 = aeiDoc != null ? await SafeReadAsync<ImageDataDocument>(idContainer, aeiDoc.id, new PartitionKey(aeiDoc.id)) : null;

                        bool aeiOk = aeiRes1 && !aeiRes2 && aeiDoc2 != null && aeiDoc2.Base64String == "Base64_Updated";
                        writeSteps.Add(new { step = "AddEditImageNew.Trap14", passed = aeiOk, aeiRes1, aeiRes2, updated = (aeiDoc2?.Base64String == "Base64_Updated") });

                        // 2.9 UploadImage + Repeated FileId test (Traps 16/17)
                        var uniqueFileId = $"parity-upload-{Guid.NewGuid()}";
                        var upId1 = await cosmosRepo.UploadImage("Assign1", uniqueFileId);
                        var upDocs = await SafeQueryAsync<ImagesDocument>(imgContainer, $"SELECT * FROM c WHERE c.Id = {upId1}");
                        var upDoc = upDocs.FirstOrDefault();
                        if (upDoc != null) cleanupList.Add((imgContainer, upDoc.id, new PartitionKey(upDoc.id)));

                        var upId2 = await cosmosRepo.UploadImage("Assign2", uniqueFileId);
                        var upDoc2 = upDoc != null ? await SafeReadAsync<ImagesDocument>(imgContainer, upDoc.id, new PartitionKey(upDoc.id)) : null;

                        bool upSingleOk = upId1 > 0 && upId2 == upId1 && upDoc2 != null && upDoc2.AssignmentId == "Assign2";

                        // Repeated FileId test: 2 docs with same FileId -> throws
                        var dupFileId = $"parity-dup-{Guid.NewGuid()}";
                        var dupDoc1 = new ImagesDocument { id = Guid.NewGuid().ToString(), Id = -99911, AssignmentId = "DupAssign", FileId = dupFileId, InsertDate = DateTime.UtcNow };
                        var dupDoc2 = new ImagesDocument { id = Guid.NewGuid().ToString(), Id = -99912, AssignmentId = "DupAssign", FileId = dupFileId, InsertDate = DateTime.UtcNow };
                        await imgContainer.CreateItemAsync(dupDoc1, new PartitionKey(dupDoc1.id));
                        await imgContainer.CreateItemAsync(dupDoc2, new PartitionKey(dupDoc2.id));
                        cleanupList.Add((imgContainer, dupDoc1.id, new PartitionKey(dupDoc1.id)));
                        cleanupList.Add((imgContainer, dupDoc2.id, new PartitionKey(dupDoc2.id)));

                        bool uploadThrew = false;
                        try { await cosmosRepo.UploadImage("DupAssign", dupFileId); } catch (InvalidOperationException) { uploadThrew = true; }

                        bool deleteThrew = false;
                        try { await cosmosRepo.DeleteImage("DupAssign", dupFileId); } catch (InvalidOperationException) { deleteThrew = true; }

                        bool allUploadOk = upSingleOk && uploadThrew && deleteThrew;
                        writeSteps.Add(new { step = "UploadImageAndRepeatedFileId.Trap16_17", passed = allUploadOk, upSingleOk, uploadThrew, deleteThrew });

                        // 2.10 DeleteImage
                        var delFileId = $"parity-del-{Guid.NewGuid()}";
                        var delImgDoc = new ImagesDocument { id = Guid.NewGuid().ToString(), Id = -99921, AssignmentId = "DelAssign", FileId = delFileId, InsertDate = DateTime.UtcNow };
                        await imgContainer.CreateItemAsync(delImgDoc, new PartitionKey(delImgDoc.id));
                        cleanupList.Add((imgContainer, delImgDoc.id, new PartitionKey(delImgDoc.id)));

                        var delDataDoc = new ImageDataDocument { id = Guid.NewGuid().ToString(), Id = -99922, ImageId = -99921, Base64String = "DelData", CreateDttm = DateTime.UtcNow };
                        await idContainer.CreateItemAsync(delDataDoc, new PartitionKey(delDataDoc.id));
                        cleanupList.Add((idContainer, delDataDoc.id, new PartitionKey(delDataDoc.id)));

                        var delRes1 = await cosmosRepo.DeleteImage("DelAssign", delFileId);
                        var checkImg = await SafeReadAsync<ImagesDocument>(imgContainer, delImgDoc.id, new PartitionKey(delImgDoc.id));
                        var checkData = await SafeReadAsync<ImageDataDocument>(idContainer, delDataDoc.id, new PartitionKey(delDataDoc.id));

                        var delRes2 = await cosmosRepo.DeleteImage("DelAssign", delFileId);

                        bool delImgOk = delRes1 && checkImg == null && checkData == null && !delRes2;
                        writeSteps.Add(new { step = "DeleteImage.Trap16", passed = delImgOk, delRes1, delRes2, checkImgNull = (checkImg == null), checkDataNull = (checkData == null) });

                        // 2.11 DeleteCard on test CapLog (returns false, doc gone; "abc" throws FormatException) (Trap 15)
                        var capLogDoc = new CapLogDocument
                        {
                            id = Guid.NewGuid().ToString(),
                            Id = -99931,
                            AbsProjectId = "-999",
                            AppId = "TestApp",
                            AssignmentId = "TestAssign",
                            SequenceId = "TestSeq",
                            CardId = 12345,
                            IntegrationId = "TestInt"
                        };
                        await clContainer.CreateItemAsync(capLogDoc, new PartitionKey(capLogDoc.id));
                        cleanupList.Add((clContainer, capLogDoc.id, new PartitionKey(capLogDoc.id)));

                        var delCardRes = await cosmosRepo.DeleteCard(-999, "12345", "TestSeq", "TestAssign", "TestApp");
                        var checkCapLog = await SafeReadAsync<CapLogDocument>(clContainer, capLogDoc.id, new PartitionKey(capLogDoc.id));

                        bool delCardThrew = false;
                        try { await cosmosRepo.DeleteCard(-999, "abc", "TestSeq", "TestAssign", "TestApp"); } catch (FormatException) { delCardThrew = true; }

                        bool delCardOk = !delCardRes && checkCapLog == null && delCardThrew;
                        writeSteps.Add(new { step = "DeleteCard.Trap15", passed = delCardOk, delCardRes, checkCapLogNull = (checkCapLog == null), delCardThrew });

                        // 2.12 DeleteBulkUploadImage (-1 and doc gone) (Trap 13)
                        var buDoc = new BulkUploadDocument
                        {
                            id = Guid.NewGuid().ToString(),
                            Id = -99941,
                            imageid = -99988
                        };
                        await buContainer.CreateItemAsync(buDoc, new PartitionKey(buDoc.id));
                        cleanupList.Add((buContainer, buDoc.id, new PartitionKey(buDoc.id)));

                        var delBuRes = await cosmosRepo.DeleteBulkUploadImage(-99988);
                        var checkBu = await SafeReadAsync<BulkUploadDocument>(buContainer, buDoc.id, new PartitionKey(buDoc.id));

                        bool delBuOk = delBuRes == -1 && checkBu == null;
                        writeSteps.Add(new { step = "DeleteBulkUploadImage.Trap13", passed = delBuOk, delBuRes, checkBuNull = (checkBu == null) });
                        } // end else (preCheckPassed)
                    }
                    catch (Exception ex)
                    {
                        hadException = true;
                        writeSteps.Add(new { step = "WriteTest.Exception", passed = false, error = ex.Message });
                    }
                    finally
                    {
                        foreach (var (cnt, id, pk) in cleanupList)
                        {
                            try
                            {
                                await cnt.DeleteItemAsync<object>(id, pk);
                            }
                            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                            }
                            catch (Exception ex)
                            {
                                hadException = true;
                                allPassed = false;
                                writeSteps.Add(new { step = "Cleanup.Error", id, passed = false, error = ex.Message });
                            }
                        }
                    }
                }

                allPassed = allPassed && !hadException
                    && checks.All(c =>
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
                    })
                    && writeSteps.All(w =>
                    {
                        var prop = w.GetType().GetProperty("passed");
                        if (prop != null)
                        {
                            var val = prop.GetValue(w);
                            if (val is bool b) return b;
                        }
                        return false;
                    });

                return Results.Ok(new
                {
                    batch = 9,
                    allPassed,
                    checksCount = checks.Count,
                    checks,
                    writeSteps
                });
            }).AllowAnonymous();
        }

        private static async Task<List<TDoc>> SafeQueryAsync<TDoc>(Container container, string queryText, PartitionKey? pk = null)
        {
            var results = new List<TDoc>();
            var q = new QueryDefinition(queryText);
            var options = pk.HasValue ? new QueryRequestOptions { PartitionKey = pk.Value } : null;
            using var feed = container.GetItemQueryIterator<TDoc>(q, requestOptions: options);
            while (feed.HasMoreResults)
            {
                var resp = await feed.ReadNextAsync();
                results.AddRange(resp);
            }
            return results;
        }

        private static async Task<TDoc?> SafeReadAsync<TDoc>(Container container, string id, PartitionKey pk)
        {
            try
            {
                var resp = await container.ReadItemAsync<TDoc>(id, pk);
                return resp.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return default;
            }
        }

        private static string ComputeSha256(string? input)
        {
            if (input == null) return string.Empty;
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        private static void MapParityBatch10A(WebApplication app)
        {
            app.MapGet("/health/parity/batch10a", async (
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

                var containerFactory = sp.GetRequiredService<ICosmosContainerFactory>();
                var sqlUserAccountRepo = sp.GetRequiredService<UserAccountRepository>();
                var cosmosUserAccountRepo = sp.GetRequiredService<CosmosUserAccountRepository>();
                var sqlClientProfileRepo = sp.GetRequiredService<ClientProfileRepository>();
                var cosmosClientProfileRepo = sp.GetRequiredService<CosmosClientProfileRepository>();
                var sqlPermRepo = sp.GetRequiredService<UserPermissionRepository>();
                var cosmosPermRepo = sp.GetRequiredService<CosmosUserPermissionRepository>();
                var sqlPortRepo = sp.GetRequiredService<PortRepository>();
                var cosmosPortRepo = sp.GetRequiredService<CosmosPortRepository>();
                var sqlTaskRepo = sp.GetRequiredService<TaskRepository>();
                var cosmosTaskRepo = sp.GetRequiredService<CosmosTaskRepository>();
                var sqlActivityRepo = sp.GetRequiredService<ActivityRepository>();
                var cosmosActivityRepo = sp.GetRequiredService<CosmosActivityRepository>();
                var sqlFileStorageRepo = sp.GetRequiredService<FileStorageRepository>();
                var cosmosFileStorageRepo = sp.GetRequiredService<CosmosFileStorageRepository>();
                var sqlCommentRepo = sp.GetRequiredService<CommentRepository>();
                var cosmosCommentRepo = sp.GetRequiredService<CosmosCommentRepository>();
                var sqlCapRepo = sp.GetRequiredService<CapRepository>();
                var cosmosCapRepo = sp.GetRequiredService<CosmosCapRepository>();
                var sqlVesselRepo = sp.GetRequiredService<VesselRepository>();
                var cosmosVesselRepo = sp.GetRequiredService<CosmosVesselRepository>();
                var sqlProjectRepo = sp.GetRequiredService<ProjectRepository>();
                var cosmosProjectRepo = sp.GetRequiredService<CosmosProjectRepository>();
                var sqlDescRepo = sp.GetRequiredService<DescriptionRepository>();
                var cosmosDescRepo = sp.GetRequiredService<CosmosDescriptionRepository>();
                var sqlExportRepo = sp.GetRequiredService<ExportRepository>();
                var cosmosExportRepo = sp.GetRequiredService<CosmosExportRepository>();
                var sqlGradingRepo = sp.GetRequiredService<GradingRepository>();
                var cosmosGradingRepo = sp.GetRequiredService<CosmosGradingRepository>();
                var sqlTankRepo = sp.GetRequiredService<TankRepository>();
                var cosmosTankRepo = sp.GetRequiredService<CosmosTankRepository>();
                var sqlReportPartRepo = sp.GetRequiredService<ReportPartRepository>();
                var cosmosReportPartRepo = sp.GetRequiredService<CosmosReportPartRepository>();

                var checks = new List<object>();
                var writeSteps = new List<object>();
                bool allPassed = true;
                bool hadException = false;
                var methodPositiveRows = new Dictionary<string, int>();

                static object? DateKey(DateTime? d)
                {
                    if (!d.HasValue) return null;
                    return d.Value.Ticks / 40000; // 40,000 ticks = 4 ms
                }

                // Reference queries from DB
                var realUser = await conn.QueryFirstOrDefaultAsync<(string Id, string Email, string UserName)>(
                    "SELECT TOP 1 Id, Email, UserName FROM [dbo].[AspNetUsers] WHERE Id IS NOT NULL AND Email IS NOT NULL AND UserName IS NOT NULL AND Active = 1");
                var realClientId = await conn.QueryFirstOrDefaultAsync<Guid>(
                    "SELECT TOP 1 Id FROM [CAP].[Clients] WHERE Id IS NOT NULL AND Id != '00000000-0000-0000-0000-000000000000'");
                var realPort = await conn.QueryFirstOrDefaultAsync<int>(
                    "SELECT TOP 1 Id FROM [core].[PortOfCall] WHERE Id IS NOT NULL");
                var realProject = await conn.QueryFirstOrDefaultAsync<(int Id, int? ClientId, int? Imo, string VesselType, string LeadSurveyor)>(
                    "SELECT TOP 1 ID as Id, ClientProfileId as ClientId, IMO as Imo, VesselType, LeadSurveyor FROM [CAP].[Projects] WHERE ID IS NOT NULL AND IMO IS NOT NULL AND IMO > 0");
                var realTask = await conn.QueryFirstOrDefaultAsync<(int Id, int? ProjectId)>(
                    "SELECT TOP 1 ID as Id, ProjectId FROM [CAP].[ProjectTaskStatus] WHERE ID IS NOT NULL AND ProjectId IS NOT NULL");
                var realVesselType = await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT TOP 1 VesselType FROM [dbo].[VesselType] WHERE VesselType IS NOT NULL") ?? "Bulk Carrier";
                var realTemplate = await conn.QueryFirstOrDefaultAsync<(int TemplateId, string TemplateName)>(
                    "SELECT TOP 1 TemplateId, TemplateName FROM [dbo].[H&I_ReportTemplate] WHERE TemplateId IS NOT NULL AND TemplateName IS NOT NULL");
                if (realTemplate.TemplateId == 0) realTemplate = (1, "Bulk Carrier");
                var realTankType = await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT TOP 1 TankType FROM [dbo].[Vessel_TankMapping] WHERE TankType IS NOT NULL") ?? "Ballast Tank";
                var realRole = await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT TOP 1 Name FROM [dbo].[AspNetRoles] WHERE Name IS NOT NULL") ?? "Admin";

                // ==========================================
                // 1. Group 1 (Batches 1-3) Read Checks
                // ==========================================
                // A1: GetByAspNetIdAsync (all columns)
                if (realUser.Id != null)
                {
                    var sqlU = await sqlUserAccountRepo.GetByAspNetIdAsync(realUser.Id);
                    var cosU = await cosmosUserAccountRepo.GetByAspNetIdAsync(realUser.Id);
                    methodPositiveRows["UserAccount.GetByAspNetIdAsync"] = sqlU != null ? 1 : 0;
                    if (sqlU == null && cosU == null)
                    {
                        checks.Add(new { check = $"A1.UserAccount.GetByAspNetIdAsync({realUser.Id})", equal = false, error = "Both null on real input" });
                        allPassed = false;
                    }
                    else
                    {
                        checks.Add(ParityCompare.Single($"A1.UserAccount.GetByAspNetIdAsync({realUser.Id})", sqlU, cosU,
                            ("Id", x => x.Id),
                            ("AspNetUserId", x => x.AspNetUserId),
                            ("ClientId", x => x.ClientId),
                            ("FirstName", x => x.FirstName),
                            ("LastName", x => x.LastName),
                            ("Company", x => x.Company),
                            ("Email", x => x.Email),
                            ("Phone", x => x.Phone),
                            ("TimeZone", x => x.TimeZone),
                            ("CreatedBy", x => x.CreatedBy),
                            ("CreatedAt", x => DateKey(x.CreatedAt)),
                            ("UpdatedBy", x => x.UpdatedBy),
                            ("UpdatedAt", x => DateKey(x.UpdatedAt)),
                            ("Deleted", x => x.Deleted)));
                    }
                }

                // A2: GetClientAdminsAsync (ClientAdminDto all columns, S5)
                if (realClientId != Guid.Empty)
                {
                    var sqlAdmins = (await sqlClientProfileRepo.GetClientAdminsAsync(realClientId)).ToList();
                    var cosAdmins = (await cosmosClientProfileRepo.GetClientAdminsAsync(realClientId)).ToList();
                    methodPositiveRows["ClientProfile.GetClientAdminsAsync"] = sqlAdmins.Count;
                    if (sqlAdmins.Count == 0 && cosAdmins.Count == 0)
                    {
                        checks.Add(new { check = $"A2.ClientProfile.GetClientAdminsAsync({realClientId})", equal = false, error = "Both empty on real input" });
                        allPassed = false;
                    }
                    else
                    {
                        checks.Add(ParityCompare.Lists($"A2.ClientProfile.GetClientAdminsAsync({realClientId})", sqlAdmins, cosAdmins, x => x.Email, false,
                            ("FirstName", x => x.FirstName),
                            ("LastName", x => x.LastName),
                            ("Email", x => x.Email),
                            ("UserRole", x => x.UserRole)));
                    }
                }

                // A3: GetAbsUsersAsync (all columns)
                {
                    var sqlAbs = (await sqlUserAccountRepo.GetAbsUsersAsync()).ToList();
                    var cosAbs = (await cosmosUserAccountRepo.GetAbsUsersAsync()).ToList();
                    methodPositiveRows["UserAccount.GetAbsUsersAsync"] = sqlAbs.Count;
                    if (sqlAbs.Count == 0 && cosAbs.Count == 0)
                    {
                        checks.Add(new { check = "A3.UserAccount.GetAbsUsersAsync", equal = false, error = "Both empty on real input" });
                        allPassed = false;
                    }
                    else
                    {
                        checks.Add(ParityCompare.Lists("A3.UserAccount.GetAbsUsersAsync", sqlAbs, cosAbs, x => x.Id, false,
                            ("Id", x => x.Id),
                            ("AspNetUserId", x => x.AspNetUserId),
                            ("FirstName", x => x.FirstName),
                            ("LastName", x => x.LastName),
                            ("Company", x => x.Company),
                            ("Email", x => x.Email),
                            ("Phone", x => x.Phone),
                            ("TimeZone", x => x.TimeZone),
                            ("UserRole", x => x.UserRole)));
                    }
                }

                // A4 & A5: GetClientUsersAsync (all columns, Company = c.CompanyName AS Company, S5)
                if (realClientId != Guid.Empty)
                {
                    var sqlCu = (await sqlUserAccountRepo.GetClientUsersAsync(realClientId.ToString())).ToList();
                    var cosCu = (await cosmosUserAccountRepo.GetClientUsersAsync(realClientId.ToString())).ToList();
                    methodPositiveRows["UserAccount.GetClientUsersAsync"] = sqlCu.Count;
                    if (sqlCu.Count == 0 && cosCu.Count == 0)
                    {
                        checks.Add(new { check = $"A4_A5.UserAccount.GetClientUsersAsync({realClientId})", equal = false, error = "Both empty on real input" });
                        allPassed = false;
                    }
                    else
                    {
                        checks.Add(ParityCompare.Lists($"A4_A5.UserAccount.GetClientUsersAsync({realClientId})", sqlCu, cosCu, x => x.Id, false,
                            ("Id", x => x.Id),
                            ("AspNetUserId", x => x.AspNetUserId),
                            ("FirstName", x => x.FirstName),
                            ("LastName", x => x.LastName),
                            ("Email", x => x.Email),
                            ("Company", x => x.Company),
                            ("Phone", x => x.Phone),
                            ("TimeZone", x => x.TimeZone),
                            ("UserRole", x => x.UserRole)));
                    }
                }

                // A7 (T-e): Null parameters on GetClientUsersAsync (call SQL too and compare)
                {
                    var sqlNullCu = (await sqlUserAccountRepo.GetClientUsersAsync(null!)).ToList();
                    var cosNullCu = (await cosmosUserAccountRepo.GetClientUsersAsync(null!)).ToList();
                    bool eqA7 = sqlNullCu.Count == 0 && cosNullCu.Count == 0;
                    checks.Add(new { check = "A7.UserAccount.GetClientUsersAsync(null)", equal = eqA7, sqlCount = sqlNullCu.Count, cosCount = cosNullCu.Count });
                    if (!eqA7) allPassed = false;
                }

                // A8: UserPermission GetAllRoles & Users in Role (trailing space S2 to BOTH sides)
                {
                    var sqlRoles = await sqlPermRepo.GetAllRoles();
                    var cosRoles = await cosmosPermRepo.GetAllRoles();
                    methodPositiveRows["UserPermission.GetAllRoles"] = sqlRoles.Count;
                    checks.Add(ParityCompare.Lists("A8.UserPermission.GetAllRoles", sqlRoles, cosRoles, x => x.Id, false,
                        ("Id", x => x.Id),
                        ("Name", x => x.Name)));

                    var sqlUir = (await sqlUserAccountRepo.GetUsersInRoleAsync(realRole + "   ")).ToList();
                    var cosUir = (await cosmosUserAccountRepo.GetUsersInRoleAsync(realRole + "   ")).ToList();
                    methodPositiveRows["UserAccount.GetUsersInRoleAsync"] = sqlUir.Count;
                    if (sqlUir.Count == 0 && cosUir.Count == 0)
                    {
                        checks.Add(new { check = $"A8.UserAccount.GetUsersInRoleAsync.TrailingSpaces({realRole})", equal = false, error = "Both empty on real input" });
                        allPassed = false;
                    }
                    else
                    {
                        checks.Add(ParityCompare.Lists($"A8.UserAccount.GetUsersInRoleAsync.TrailingSpaces({realRole})", sqlUir, cosUir, x => x.Id, false,
                            ("Id", x => x.Id),
                            ("AspNetUserId", x => x.AspNetUserId),
                            ("FirstName", x => x.FirstName),
                            ("LastName", x => x.LastName),
                            ("Email", x => x.Email)));
                    }
                }

                // A9: Port Search & GetById (all columns)
                if (realPort != 0)
                {
                    var sqlP = await sqlPortRepo.GetById(realPort);
                    var cosP = await cosmosPortRepo.GetById(realPort);
                    methodPositiveRows["Port.GetById"] = sqlP != null ? 1 : 0;
                    if (sqlP == null && cosP == null)
                    {
                        checks.Add(new { check = $"A9.Port.GetById({realPort})", equal = false, error = "Both null on real input" });
                        allPassed = false;
                    }
                    else
                    {
                        checks.Add(ParityCompare.Single($"A9.Port.GetById({realPort})", sqlP, cosP,
                            ("Id", x => x.Id),
                            ("Code", x => x.Code),
                            ("Name", x => x.Name),
                            ("CountryCode", x => x.CountryCode)));
                    }

                    var sqlSearch = (await sqlPortRepo.Search("Singapore")).ToList();
                    var cosSearch = (await cosmosPortRepo.Search("Singapore")).ToList();
                    methodPositiveRows["Port.Search"] = sqlSearch.Count;
                    checks.Add(ParityCompare.Lists("A9.Port.Search(Singapore)", sqlSearch, cosSearch, x => x.Id, false,
                        ("Id", x => x.Id),
                        ("Code", x => x.Code),
                        ("Name", x => x.Name),
                        ("CountryCode", x => x.CountryCode)));
                }

                // ==========================================
                // 2. Group 2 (Batch 4) Read Checks
                // ==========================================
                // B1 & B6: GetCap2Tasks (all columns)
                if (realProject.Id != 0)
                {
                    var sqlTasks = (await sqlTaskRepo.GetCap2Tasks(realProject.Id)).ToList();
                    var cosTasks = (await cosmosTaskRepo.GetCap2Tasks(realProject.Id)).ToList();
                    methodPositiveRows["Task.GetCap2Tasks"] = sqlTasks.Count;
                    if (sqlTasks.Count == 0 && cosTasks.Count == 0)
                    {
                        checks.Add(new { check = $"B1_B6.Task.GetCap2Tasks({realProject.Id})", equal = false, error = "Both empty on real input" });
                        allPassed = false;
                    }
                    else
                    {
                        checks.Add(ParityCompare.Lists($"B1_B6.Task.GetCap2Tasks({realProject.Id})", sqlTasks, cosTasks, x => x.ID, false,
                            ("ID", x => x.ID),
                            ("ProjectId", x => x.ProjectId),
                            ("TaskId", x => x.TaskId),
                            ("StatusId", x => x.StatusId),
                            ("Task", x => x.Task),
                            ("Status", x => x.Status),
                            ("Notes", x => x.Notes),
                            ("AssignedTo", x => x.AssignedTo),
                            ("NumberOfComments", x => x.NumberOfComments),
                            ("NumberOfAttachments", x => x.NumberOfAttachments)));
                    }
                }

                // B2: GetRecentActivitiesAsync2 (all columns, ordered)
                {
                    var sqlActs = (await sqlActivityRepo.GetRecentActivitiesAsync2(1, 10)).ToList();
                    var cosActs = (await cosmosActivityRepo.GetRecentActivitiesAsync2(1, 10)).ToList();
                    methodPositiveRows["Activity.GetRecentActivitiesAsync2"] = sqlActs.Count;
                    checks.Add(ParityCompare.Lists("B2.Activity.GetRecentActivitiesAsync2(1, 10)", sqlActs, cosActs, null, true,
                        ("ID", x => x.ID),
                        ("ProjectId", x => x.ProjectId),
                        ("PIDNumber", x => x.PIDNumber),
                        ("VesselName", x => x.VesselName),
                        ("Text", x => x.Text),
                        ("CreateBy", x => x.CreateBy),
                        ("CreateDttm", x => DateKey(x.CreateDttm))));
                }

                // B3 (T-i): GetRecentActivitiesByProjectIdAsync
                if (realProject.Id != 0)
                {
                    var sqlProjActs = (await sqlActivityRepo.GetRecentActivitiesByProjectIdAsync(10, realProject.Id)).ToList();
                    var cosProjActs = (await cosmosActivityRepo.GetRecentActivitiesByProjectIdAsync(10, realProject.Id)).ToList();
                    methodPositiveRows["Activity.GetRecentActivitiesByProjectIdAsync"] = sqlProjActs.Count;
                    checks.Add(ParityCompare.Lists($"B3.Activity.GetRecentActivitiesByProjectIdAsync({realProject.Id})", sqlProjActs, cosProjActs, null, true,
                        ("ID", x => x.ID),
                        ("ProjectId", x => x.ProjectId),
                        ("PIDNumber", x => x.PIDNumber),
                        ("VesselName", x => x.VesselName),
                        ("Text", x => x.Text),
                        ("CreateBy", x => x.CreateBy),
                        ("CreateDttm", x => DateKey(x.CreateDttm))));
                }

                // B5: GetCap2Task (all columns)
                if (realTask.Id != 0)
                {
                    var sqlSingle = await sqlTaskRepo.GetCap2Task(realTask.Id);
                    var cosSingle = await cosmosTaskRepo.GetCap2Task(realTask.Id);
                    methodPositiveRows["Task.GetCap2Task"] = sqlSingle != null ? 1 : 0;
                    if (sqlSingle == null && cosSingle == null)
                    {
                        checks.Add(new { check = $"B5.Task.GetCap2Task({realTask.Id})", equal = false, error = "Both null on real input" });
                        allPassed = false;
                    }
                    else
                    {
                        checks.Add(ParityCompare.Single($"B5.Task.GetCap2Task({realTask.Id})", sqlSingle, cosSingle,
                            ("ID", x => x.ID),
                            ("ProjectId", x => x.ProjectId),
                            ("TaskId", x => x.TaskId),
                            ("StatusId", x => x.StatusId),
                            ("Task", x => x.Task),
                            ("Status", x => x.Status),
                            ("Notes", x => x.Notes),
                            ("AssignedTo", x => x.AssignedTo),
                            ("NumberOfComments", x => x.NumberOfComments),
                            ("NumberOfAttachments", x => x.NumberOfAttachments)));
                    }
                }

                // B7: GetProjectFiles (all columns)
                if (realProject.Id != 0)
                {
                    var sqlFiles = (await sqlFileStorageRepo.GetProjectFiles(realProject.Id)).ToList();
                    var cosFiles = (await cosmosFileStorageRepo.GetProjectFiles(realProject.Id)).ToList();
                    methodPositiveRows["FileStorage.GetProjectFiles"] = sqlFiles.Count;
                    checks.Add(ParityCompare.Lists($"B7.FileStorage.GetProjectFiles({realProject.Id})", sqlFiles, cosFiles, x => x.Id, false,
                        ("Id", x => x.Id),
                        ("ProjectId", x => x.ProjectId),
                        ("TaskId", x => x.TaskId),
                        ("Name", x => x.Name),
                        ("Extension", x => x.Extension),
                        ("StorageKey", x => x.StorageKey),
                        ("MimeType", x => x.MimeType),
                        ("UploadedBy", x => x.UploadedBy),
                        ("LastModifiedDate", x => DateKey(x.LastModifiedDate))));
                }

                // B9: GetCommentsByProjectIdAsync (all columns)
                if (realProject.Id != 0)
                {
                    var sqlComms = (await sqlCommentRepo.GetCommentsByProjectIdAsync(realProject.Id)).ToList();
                    var cosComms = (await cosmosCommentRepo.GetCommentsByProjectIdAsync(realProject.Id)).ToList();
                    methodPositiveRows["Comment.GetCommentsByProjectIdAsync"] = sqlComms.Count;
                    checks.Add(ParityCompare.Lists($"B9.Comment.GetCommentsByProjectIdAsync({realProject.Id})", sqlComms, cosComms, x => x.Id, false,
                        ("Id", x => x.Id),
                        ("ProjectId", x => x.ProjectId),
                        ("ProjectTaskId", x => x.ProjectTaskId),
                        ("Comment", x => x.Comment),
                        ("CreatedBy", x => x.CreatedBy),
                        ("LastModifiedDate", x => DateKey(x.LastModifiedDate.UtcDateTime))));
                }

                // ==========================================
                // 3. Group 3 (Batch 5 - Cap & Vessel) Read Checks
                // ==========================================
                // C1 & C3: GetCapIndexAsync (all columns)
                {
                    var sqlCapIdx = (await sqlCapRepo.GetCapIndexAsync()).ToList();
                    var cosCapIdx = (await cosmosCapRepo.GetCapIndexAsync()).ToList();
                    methodPositiveRows["Cap.GetCapIndexAsync"] = sqlCapIdx.Count;
                    checks.Add(ParityCompare.Lists("C1_C3.Cap.GetCapIndexAsync", sqlCapIdx, cosCapIdx, x => x.ID, false,
                        ("ID", x => x.ID),
                        ("Name", x => x.Name),
                        ("CapRegion", x => x.CapRegion),
                        ("ProjectStatusName", x => x.ProjectStatusName),
                        ("PIDNumber", x => x.PIDNumber),
                        ("CompanyName", x => x.CompanyName),
                        ("IMO", x => x.IMO),
                        ("VesselName", x => x.VesselName),
                        ("ProjectPriority", x => x.ProjectPriority),
                        ("WCN", x => x.WCN)));

                    var sqlCertsCount = await sqlCapRepo.GetCertsCountAsync();
                    var cosCertsCount = await cosmosCapRepo.GetCertsCountAsync();
                    methodPositiveRows["Cap.GetCertsCountAsync"] = sqlCertsCount?.Count ?? 0;
                    checks.Add(ParityCompare.Single("C2.Cap.GetCertsCountAsync", sqlCertsCount, cosCertsCount,
                        ("Count", x => x.Count)));
                }

                // V1, V2, V3: Vessel MainData (all 31 columns)
                if (realProject.Id != 0)
                {
                    var sqlMd = (await sqlVesselRepo.GetVesselMainData(realProject.Id))?.ToList() ?? new List<ReportVesselMainData>();
                    var cosMd = (await cosmosVesselRepo.GetVesselMainData(realProject.Id))?.ToList() ?? new List<ReportVesselMainData>();
                    methodPositiveRows["Vessel.GetVesselMainData"] = sqlMd.Count;
                    if (sqlMd.Count == 0 && cosMd.Count == 0)
                    {
                        checks.Add(new { check = $"V1_V2_V3.Vessel.GetVesselMainData({realProject.Id})", equal = false, error = "Both empty on real input" });
                        allPassed = false;
                    }
                    else
                    {
                        checks.Add(ParityCompare.Lists($"V1_V2_V3.Vessel.GetVesselMainData({realProject.Id})", sqlMd, cosMd, null, false,
                            ("projectId", x => x.projectId),
                            ("vesselName", x => x.vesselName),
                            ("imoNumber", x => x.imoNumber),
                            ("yearBuilt", x => x.yearBuilt),
                            ("builtBy", x => x.builtBy),
                            ("length", x => x.length),
                            ("breadth", x => x.breadth),
                            ("depth", x => x.depth),
                            ("draft", x => x.draft),
                            ("grossTons", x => x.grossTons),
                            ("netTons", x => x.netTons),
                            ("deadweight", x => x.deadweight),
                            ("vesselType", x => x.vesselType),
                            ("callSign", x => x.callSign),
                            ("officalNumber", x => x.officalNumber),
                            ("homeport", x => x.homeport),
                            ("previousName", x => x.previousName),
                            ("ownerName", x => x.ownerName),
                            ("manager", x => x.manager),
                            ("hullNo", x => x.hullNo),
                            ("propulsionType", x => x.propulsionType),
                            ("kw", x => x.kw),
                            ("propulsionManufacturer", x => x.propulsionManufacturer),
                            ("gearManufacturer", x => x.gearManufacturer),
                            ("classed", x => x.classed),
                            ("classNotation", x => x.classNotation),
                            ("machinery", x => x.machinery),
                            ("other", x => x.other),
                            ("environment", x => x.environment),
                            ("selectedPrefix", x => x.selectedPrefix),
                            ("firstVisitDate", x => x.firstVisitDate),
                            ("lastVisitDate", x => x.lastVisitDate)));
                    }

                    // V11: StatutoryCertificate (all dates)
                    var sqlCert = await sqlVesselRepo.GetStatutoryCertificate(realProject.Id);
                    var cosCert = await cosmosVesselRepo.GetStatutoryCertificate(realProject.Id);
                    methodPositiveRows["Vessel.GetStatutoryCertificate"] = sqlCert != null ? 1 : 0;
                    checks.Add(ParityCompare.Single($"V11.Vessel.GetStatutoryCertificate({realProject.Id})", sqlCert, cosCert,
                        ("Exp_LLLI", x => DateKey(x?.StatutoryCertificatesExpirationDates?.LLLIDate)),
                        ("Exp_SafetyCon", x => DateKey(x?.StatutoryCertificatesExpirationDates?.SafetyConDate)),
                        ("Exp_SafetyRadio", x => DateKey(x?.StatutoryCertificatesExpirationDates?.SafetyRadioDate)),
                        ("Exp_SafetyEquip", x => DateKey(x?.StatutoryCertificatesExpirationDates?.SafetyEquipmentDate)),
                        ("Exp_IEE", x => DateKey(x?.StatutoryCertificatesExpirationDates?.IEEDate)),
                        ("Exp_IOPP", x => DateKey(x?.StatutoryCertificatesExpirationDates?.IOPPDate)),
                        ("Exp_IAP", x => DateKey(x?.StatutoryCertificatesExpirationDates?.IAPDate)),
                        ("Exp_IAFS", x => DateKey(x?.StatutoryCertificatesExpirationDates?.IAFSDate)),
                        ("Exp_ISPS", x => DateKey(x?.StatutoryCertificatesExpirationDates?.ISPSDate)),
                        ("Exp_IBWM", x => DateKey(x?.StatutoryCertificatesExpirationDates?.IBWMDate)),
                        ("Exp_ISPPSewage", x => DateKey(x?.StatutoryCertificatesExpirationDates?.ISPPSewageDate)),
                        ("Exp_ISM", x => DateKey(x?.StatutoryCertificatesExpirationDates?.ISMDate)),
                        ("Exp_MaritimeLabour", x => DateKey(x?.StatutoryCertificatesExpirationDates?.MaritimeLabourDate)),
                        ("Exp_GasCarrierFitness", x => DateKey(x?.StatutoryCertificatesExpirationDates?.GasCarrierFitness)),
                        ("Iss_LLLI", x => DateKey(x?.StatutoryCertificatesIssuedDates?.LLLIDate)),
                        ("Iss_SafetyCon", x => DateKey(x?.StatutoryCertificatesIssuedDates?.SafetyConDate)),
                        ("Iss_SafetyRadio", x => DateKey(x?.StatutoryCertificatesIssuedDates?.SafetyRadioDate)),
                        ("Iss_SafetyEquip", x => DateKey(x?.StatutoryCertificatesIssuedDates?.SafetyEquipmentDate)),
                        ("Iss_IEE", x => DateKey(x?.StatutoryCertificatesIssuedDates?.IEEDate)),
                        ("Iss_IOPP", x => DateKey(x?.StatutoryCertificatesIssuedDates?.IOPPDate)),
                        ("Iss_IAP", x => DateKey(x?.StatutoryCertificatesIssuedDates?.IAPDate)),
                        ("Iss_IAFS", x => DateKey(x?.StatutoryCertificatesIssuedDates?.IAFSDate)),
                        ("Iss_ISPS", x => DateKey(x?.StatutoryCertificatesIssuedDates?.ISPSDate)),
                        ("Iss_IBWM", x => DateKey(x?.StatutoryCertificatesIssuedDates?.IBWMDate)),
                        ("Iss_ISPPSewage", x => DateKey(x?.StatutoryCertificatesIssuedDates?.ISPPSewageDate)),
                        ("Iss_ISM", x => DateKey(x?.StatutoryCertificatesIssuedDates?.ISMDate)),
                        ("Iss_MaritimeLabour", x => DateKey(x?.StatutoryCertificatesIssuedDates?.MaritimeLabourDate)),
                        ("Iss_GasCarrierFitness", x => DateKey(x?.StatutoryCertificatesIssuedDates?.GasCarrierFitness))));
                }

                // V16: Details (char(7), S2 match, S3, all columns)
                if (realProject.Imo.HasValue && realProject.Imo.Value > 0)
                {
                    var imoStr = realProject.Imo.Value.ToString();
                    var sqlDet = await sqlVesselRepo.Details(imoStr);
                    var cosDet = await cosmosVesselRepo.Details(imoStr);
                    methodPositiveRows["Vessel.Details"] = sqlDet != null ? 1 : 0;
                    checks.Add(ParityCompare.Single($"V16.Vessel.Details({imoStr})", sqlDet, cosDet,
                        ("Vessel_ID", x => x.Vessel_ID),
                        ("IMONum", x => x.IMONum),
                        ("VesselName", x => x.VesselName),
                        ("HullNum", x => x.HullNum),
                        ("Builder", x => x.Builder),
                        ("DWT", x => x.DWT),
                        ("GT", x => x.GT),
                        ("NetTonnage", x => x.NetTonnage),
                        ("Length", x => x.Length),
                        ("Breadth", x => x.Breadth),
                        ("Depth", x => x.Depth),
                        ("CallSign", x => x.CallSign),
                        ("FlagName", x => x.FlagName),
                        ("VesselType", x => x.VesselType)));
                }

                // ==========================================
                // 4. Group 4 (Batch 5 - Project) Read Checks
                // ==========================================
                if (realProject.Id != 0)
                {
                    // P5, P14: GetProject (all columns)
                    var sqlProj = await sqlProjectRepo.GetProject(realProject.Id);
                    var cosProj = await cosmosProjectRepo.GetProject(realProject.Id);
                    methodPositiveRows["Project.GetProject"] = sqlProj != null ? 1 : 0;
                    checks.Add(ParityCompare.Single($"P5_P14.Project.GetProject({realProject.Id})", sqlProj, cosProj,
                        ("Id", x => x.Id),
                        ("Name", x => x.Name),
                        ("IMO", x => x.IMO),
                        ("PIDNumber", x => x.PIDNumber),
                        ("VesselType", x => x.VesselType),
                        ("ShipType", x => x.ShipType),
                        ("LeadSurveyor", x => x.LeadSurveyor),
                        ("SecondSurveyor", x => x.SecondSurveyor),
                        ("CapRegion", x => x.CapRegion),
                        ("ProjectStatus", x => x.ProjectStatus),
                        ("ClientProfileId", x => x.ClientProfileId),
                        ("PercentComplete", x => x.PercentComplete),
                        ("CreatedDate", x => DateKey(x.CreatedDate)),
                        ("CreatedBy", x => x.CreatedBy)));

                    // P2 (T-i): GetProjectCountGroupedByClientIdAsync (all columns)
                    var sqlProjCount = (await sqlProjectRepo.GetProjectCountGroupedByClientIdAsync()).ToList();
                    var cosProjCount = (await cosmosProjectRepo.GetProjectCountGroupedByClientIdAsync()).ToList();
                    methodPositiveRows["Project.GetProjectCountGroupedByClientIdAsync"] = sqlProjCount.Count;
                    checks.Add(ParityCompare.Lists("P2.Project.GetProjectCountGroupedByClientIdAsync", sqlProjCount, cosProjCount, x => x.ClientId, false,
                        ("ClientId", x => x.ClientId),
                        ("ClientName", x => x.ClientName),
                        ("ProjectCount", x => x.ProjectCount)));

                    // P8: Lookups and missing
                    var sqlPn = await sqlProjectRepo.GetProjectName(realProject.Id);
                    var cosPn = await cosmosProjectRepo.GetProjectName(realProject.Id);
                    var sqlImo = await sqlProjectRepo.GetIMONumberByProjectId(realProject.Id);
                    var cosImo = await cosmosProjectRepo.GetIMONumberByProjectId(realProject.Id);
                    var sqlVt = await sqlProjectRepo.GetProjectVesselType(realProject.Id);
                    var cosVt = await cosmosProjectRepo.GetProjectVesselType(realProject.Id);

                    var cosPnM = await cosmosProjectRepo.GetProjectName(-99999);
                    var cosImoM = await cosmosProjectRepo.GetIMONumberByProjectId(-99999);
                    var cosVtM = await cosmosProjectRepo.GetProjectVesselType(-99999);

                    bool eqLookups = string.Equals(sqlPn, cosPn, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(sqlImo, cosImo, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(sqlVt, cosVt, StringComparison.OrdinalIgnoreCase)
                        && cosPnM == null && cosImoM == null && cosVtM == null;
                    methodPositiveRows["Project.Lookups"] = eqLookups ? 1 : 0;
                    checks.Add(new { check = $"P8.Project.LookupsAndMissing({realProject.Id})", equal = eqLookups, cosPnM, cosImoM, cosVtM });
                    if (!eqLookups) allPassed = false;

                    // P4: GetDetails (all columns)
                    var sqlDet = (await sqlProjectRepo.GetDetails(realProject.Id)).ToList();
                    var cosDet = (await cosmosProjectRepo.GetDetails(realProject.Id)).ToList();
                    methodPositiveRows["Project.GetDetails"] = sqlDet.Count;
                    checks.Add(ParityCompare.Lists($"P4.Project.GetDetails({realProject.Id})", sqlDet, cosDet, x => x.Name, false,
                        ("Name", x => x.Name),
                        ("Type", x => x.Type),
                        ("LastModifiedDate", x => DateKey(x.LastModifiedDate))));

                    // P6, P7, P18: GetSummary (all columns)
                    var sqlSum = await sqlProjectRepo.GetSummary(realProject.Id);
                    var cosSum = await cosmosProjectRepo.GetSummary(realProject.Id);
                    methodPositiveRows["Project.GetSummary"] = sqlSum != null ? 1 : 0;
                    checks.Add(ParityCompare.Single($"P6_P7_P18.Project.GetSummary({realProject.Id})", sqlSum, cosSum,
                        ("ID", x => x.ID),
                        ("Name", x => x.Name),
                        ("ProjectId", x => x.ProjectId),
                        ("IMO", x => x.IMO),
                        ("PIDNumber", x => x.PIDNumber),
                        ("VesselName", x => x.VesselName),
                        ("ShipType", x => x.ShipType),
                        ("CapRegion", x => x.CapRegion),
                        ("YearBuilt", x => x.YearBuilt),
                        ("AgreementOwner", x => x.AgreementOwner),
                        ("AgreementNumber", x => x.AgreementNumber),
                        ("ProjectComments", x => x.ProjectComments),
                        ("AreTechnicalTasksComplete", x => x.AreTechnicalTasksComplete),
                        ("AreInvoiceTasksComplete", x => x.AreInvoiceTasksComplete),
                        ("TechnicalTasksCompletionPercentage", x => x.TechnicalTasksCompletionPercentage),
                        ("InvoiceTasksCompletionPercentage", x => x.InvoiceTasksCompletionPercentage),
                        ("ProjectPriority", x => x.ProjectPriority),
                        ("TechnicalDaysWorked", x => x.TechnicalDaysWorked),
                        ("InvoiceDaysWorked", x => x.InvoiceDaysWorked)));
                }

                // P3, P9, P10, P15, P17: Grids, Export and Stats (all columns)
                {
                    var sqlGrid = (await sqlProjectRepo.GetAllProjectsGridData()).ToList();
                    var cosGrid = (await cosmosProjectRepo.GetAllProjectsGridData()).ToList();
                    methodPositiveRows["Project.GetAllProjectsGridData"] = sqlGrid.Count;
                    checks.Add(ParityCompare.Lists("P9_P10.Project.GetAllProjectsGridData", sqlGrid, cosGrid, x => x.Id, false,
                        ("Id", x => x.Id),
                        ("PIDNumber", x => x.PIDNumber),
                        ("CompanyName", x => x.CompanyName),
                        ("CapRegion", x => x.CapRegion),
                        ("VesselName", x => x.VesselName),
                        ("IMO", x => x.IMO),
                        ("WCN", x => x.WCN),
                        ("ProjectPriority", x => x.ProjectPriority),
                        ("PercentComplete", x => x.PercentComplete)));

                    var sqlExport = await sqlProjectRepo.GetAllProjectsGridExport();
                    var cosExport = await cosmosProjectRepo.GetAllProjectsGridExport();
                    methodPositiveRows["Project.GetAllProjectsGridExport"] = sqlExport.Count;
                    checks.Add(ParityCompare.Lists("P3.Project.GetAllProjectsGridExport", sqlExport, cosExport, x => x.ID, false,
                        ("ID", x => x.ID),
                        ("Name", x => x.Name),
                        ("PIDNumber", x => x.PIDNumber),
                        ("CapRegion", x => x.CapRegion),
                        ("CapScope", x => x.CapScope),
                        ("ProjectStatus", x => x.ProjectStatus),
                        ("ProjectPriority", x => x.ProjectPriority),
                        ("SpecialHull", x => x.SpecialHull),
                        ("Fatigue", x => x.Fatigue)));

                    var sqlStats = await sqlProjectRepo.GetProjectStats();
                    var cosStats = await cosmosProjectRepo.GetProjectStats();
                    methodPositiveRows["Project.GetProjectStats"] = sqlStats != null ? 1 : 0;
                    bool eqStats = sqlStats != null && cosStats != null
                        && sqlStats.Total == cosStats.Total
                        && sqlStats.Online == cosStats.Online
                        && sqlStats.Offline == cosStats.Offline
                        && sqlStats.Regions.Count == cosStats.Regions.Count
                        && sqlStats.Regions.All(kv => cosStats.Regions.TryGetValue(kv.Key, out var v) && v == kv.Value)
                        && sqlStats.Statuses.Count == cosStats.Statuses.Count
                        && sqlStats.Statuses.All(kv => cosStats.Statuses.TryGetValue(kv.Key, out var v) && v == kv.Value);
                    checks.Add(new { check = "P10_P15_P17.Project.GetProjectStats", equal = eqStats, sqlTotal = sqlStats?.Total, cosmosTotal = cosStats?.Total });
                    if (!eqStats) allPassed = false;
                }

                // ==========================================
                // 5. Group 5 (Batch 6) Read Checks
                // ==========================================
                // D7: Description GetGradingTemplates (all columns)
                {
                    var sqlTemplates = await sqlDescRepo.GetGradingTemplates();
                    var cosTemplates = await cosmosDescRepo.GetGradingTemplates();
                    methodPositiveRows["Description.GetGradingTemplates"] = sqlTemplates.Count;
                    checks.Add(ParityCompare.Lists("D7.Description.GetGradingTemplates", sqlTemplates, cosTemplates, x => x.TemplateId, false,
                        ("TemplateId", x => x.TemplateId),
                        ("TemplateName", x => x.TemplateName)));

                    // D3 (T-e): GetAllDescription (all columns)
                    var sqlDescAll = await sqlDescRepo.GetAllDescription(1, 10);
                    var cosDescAll = await cosmosDescRepo.GetAllDescription(1, 10);
                    methodPositiveRows["Description.GetAllDescription"] = sqlDescAll?.Data?.Count ?? 0;
                    bool eqPaging = sqlDescAll != null && cosDescAll != null && sqlDescAll.Total == cosDescAll.Total;
                    checks.Add(new { check = "D3.Description.GetAllDescription.Paging", equal = eqPaging, sqlTotal = sqlDescAll?.Total, cosmosTotal = cosDescAll?.Total });
                    if (!eqPaging) allPassed = false;

                    checks.Add(ParityCompare.Lists("D3.Description.GetAllDescription.Data", sqlDescAll?.Data ?? new List<ImageDescriptions>(), cosDescAll?.Data ?? new List<ImageDescriptions>(), x => x.Id, false,
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

                    // D2, D7b (T-e): GetImageDescriptionsByProjectId
                    if (realProject.Id != 0)
                    {
                        var sqlImgDesc = await sqlDescRepo.GetImageDescriptionsByProjectId(realProject.Id);
                        var cosImgDesc = await cosmosDescRepo.GetImageDescriptionsByProjectId(realProject.Id);
                        methodPositiveRows["Description.GetImageDescriptionsByProjectId"] = sqlImgDesc.Count;
                        checks.Add(ParityCompare.Lists($"D2_D7b.Description.GetImageDescriptionsByProjectId({realProject.Id})", sqlImgDesc, cosImgDesc, x => x.Id, false,
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

                    // D4 (T-e): CheckImageDescriptionExistsOrNot (real and null)
                    var sqlNullDesc = (await sqlDescRepo.CheckImageDescriptionExistsOrNot(null!, null!, null!, null!)).ToList();
                    var cosNullDesc = (await cosmosDescRepo.CheckImageDescriptionExistsOrNot(null!, null!, null!, null!)).ToList();
                    bool eqNullDesc = sqlNullDesc.Count == 0 && cosNullDesc.Count == 0;
                    checks.Add(new { check = "D4.Description.CheckImageDescriptionExistsOrNot.NullParams", equal = eqNullDesc, sqlCount = sqlNullDesc.Count, cosCount = cosNullDesc.Count });
                    if (!eqNullDesc) allPassed = false;

                    // D1, D5 (T-e / T-i): GetDescriptionSectionNamesByTemplateNameAndVesselType
                    var sqlDescSecs = await sqlDescRepo.GetDescriptionSectionNamesByTemplateNameAndVesselType(realTemplate.TemplateName, realVesselType);
                    var cosDescSecs = await cosmosDescRepo.GetDescriptionSectionNamesByTemplateNameAndVesselType(realTemplate.TemplateName, realVesselType);
                    methodPositiveRows["Description.GetDescriptionSectionNamesByTemplateNameAndVesselType"] = sqlDescSecs.Count;
                    checks.Add(ParityCompare.Lists($"D1.Description.GetDescriptionSectionNames({realTemplate.TemplateName}, {realVesselType})", sqlDescSecs, cosDescSecs, x => x.SectionId, false,
                        ("SectionId", x => x.SectionId),
                        ("SectionName", x => x.SectionName)));

                    var sqlNullSec = await sqlDescRepo.GetDescriptionSectionNamesByTemplateNameAndVesselType(null!, realVesselType);
                    var cosNullSec = await cosmosDescRepo.GetDescriptionSectionNamesByTemplateNameAndVesselType(null!, realVesselType);
                    bool eqNullSec = sqlNullSec.Count == 0 && cosNullSec.Count == 0;
                    checks.Add(new { check = "D5.Description.GetDescriptionSectionNames.NullTemplateName", equal = eqNullSec, sqlCount = sqlNullSec.Count, cosCount = cosNullSec.Count });
                    if (!eqNullSec) allPassed = false;

                    // D6 (T-i): GetGradingSections
                    var sqlDescGradSec = await sqlDescRepo.GetGradingSections(realTemplate.TemplateId, realVesselType);
                    var cosDescGradSec = await cosmosDescRepo.GetGradingSections(realTemplate.TemplateId, realVesselType);
                    methodPositiveRows["Description.GetGradingSections"] = sqlDescGradSec.Count;
                    checks.Add(ParityCompare.Lists($"D6.Description.GetGradingSections({realTemplate.TemplateId}, {realVesselType})", sqlDescGradSec, cosDescGradSec, x => x.SectionId, false,
                        ("SectionId", x => x.SectionId),
                        ("SectionName", x => x.SectionName)));
                }

                // E1 & D8: Export Part Configuration (all columns)
                {
                    var sqlExportParts = await sqlExportRepo.GetVesselTypeExportPartConfiguration(1);
                    var cosExportParts = await cosmosExportRepo.GetVesselTypeExportPartConfiguration(1);
                    methodPositiveRows["Export.GetVesselTypeExportPartConfiguration"] = sqlExportParts.Count;
                    checks.Add(ParityCompare.Lists("E1_D8.Export.GetVesselTypeExportPartConfiguration(1)", sqlExportParts, cosExportParts, x => x.PartId, false,
                        ("PartId", x => x.PartId),
                        ("PartName", x => x.PartName)));
                }

                // ==========================================
                // 6. Group 6 (Batches 7-8) Read Checks
                // ==========================================
                // G1, T5: Grading
                if (realVesselType != null)
                {
                    var sqlGradSec = await sqlGradingRepo.GetGradingSections(1, realVesselType + "   ");
                    var cosGradSec = await cosmosGradingRepo.GetGradingSections(1, realVesselType + "   ");
                    methodPositiveRows["Grading.GetGradingSections"] = sqlGradSec.Count;
                    checks.Add(ParityCompare.Lists($"G1_T5.Grading.GetGradingSections.TrailingSpaces({realVesselType})", sqlGradSec, cosGradSec, x => x.SectionId, false,
                        ("SectionId", x => x.SectionId),
                        ("SectionName", x => x.SectionName)));

                    // T1 place 4: GetTanks_VesselByVesselType (trailing spaces sent to BOTH sides)
                    var sqlTanksVt = await sqlTankRepo.GetTanks_VesselByVesselType(realVesselType + "   ");
                    var cosTanksVt = await cosmosTankRepo.GetTanks_VesselByVesselType(realVesselType + "   ");
                    methodPositiveRows["Tank.GetTanks_VesselByVesselType"] = sqlTanksVt.Count;
                    checks.Add(ParityCompare.Lists($"T1_P4.Tank.GetTanks_VesselByVesselType.TrailingSpaces({realVesselType})", sqlTanksVt, cosTanksVt, x => x.Id, false,
                        ("Id", x => x.Id),
                        ("VesselType", x => x.VesselType),
                        ("TankTypeId", x => x.TankTypeId),
                        ("TankName", x => x.TankName),
                        ("CreatedDttm", x => DateKey(x.CreatedDttm)),
                        ("UpdateDttm", x => DateKey(x.UpdateDttm))));
                }

                if (realProject.Imo.HasValue && realProject.Imo.Value > 0)
                {
                    string imoStr = realProject.Imo.Value.ToString();
                    // T1 place 1: GetTanks_VesselByIMO (trailing spaces sent to BOTH sides)
                    var sqlTanksImo = await sqlTankRepo.GetTanks_VesselByIMO(imoStr + "   ");
                    var cosTanksImo = await cosmosTankRepo.GetTanks_VesselByIMO(imoStr + "   ");
                    methodPositiveRows["Tank.GetTanks_VesselByIMO"] = sqlTanksImo.Count;
                    checks.Add(ParityCompare.Lists($"T1_P1.Tank.GetTanks_VesselByIMO.TrailingSpaces({imoStr})", sqlTanksImo, cosTanksImo, x => x.Id, false,
                        ("Id", x => x.Id),
                        ("ImoNumber", x => x.ImoNumber),
                        ("ProjectId", x => x.ProjectId),
                        ("TankTypeId", x => x.TankTypeId),
                        ("TankName", x => x.TankName),
                        ("CreatedDttm", x => DateKey(x.CreatedDttm)),
                        ("UpdateDttm", x => DateKey(x.UpdateDttm))));

                    if (realProject.Id != 0)
                    {
                        // T1 place 2: GetTanks_VesselByProject (trailing spaces sent to BOTH sides)
                        var sqlTanksProj = await sqlTankRepo.GetTanks_VesselByProject(imoStr + "   ", realProject.Id);
                        var cosTanksProj = await cosmosTankRepo.GetTanks_VesselByProject(imoStr + "   ", realProject.Id);
                        methodPositiveRows["Tank.GetTanks_VesselByProject"] = sqlTanksProj.Count;
                        checks.Add(ParityCompare.Lists($"T1_P2.Tank.GetTanks_VesselByProject({imoStr}, {realProject.Id})", sqlTanksProj, cosTanksProj, x => x.Id, false,
                            ("Id", x => x.Id),
                            ("ImoNumber", x => x.ImoNumber),
                            ("ProjectId", x => x.ProjectId),
                            ("TankTypeId", x => x.TankTypeId),
                            ("TankName", x => x.TankName),
                            ("CreatedDttm", x => DateKey(x.CreatedDttm)),
                            ("UpdateDttm", x => DateKey(x.UpdateDttm))));

                        // T1 place 3: GetTemplateTanks (trailing spaces sent to BOTH sides)
                        var sqlTanksTmpl = await sqlTankRepo.GetTemplateTanks(realTemplate.TemplateId, imoStr + "   ", realVesselType, realProject.Id);
                        var cosTanksTmpl = await cosmosTankRepo.GetTemplateTanks(realTemplate.TemplateId, imoStr + "   ", realVesselType, realProject.Id);
                        methodPositiveRows["Tank.GetTemplateTanks"] = sqlTanksTmpl.Count;
                        checks.Add(ParityCompare.Lists($"T1_P3.Tank.GetTemplateTanks({imoStr})", sqlTanksTmpl, cosTanksTmpl, x => x.Id, false,
                            ("Id", x => x.Id),
                            ("TankName", x => x.TankName),
                            ("TankTypeId", x => x.TankTypeId),
                            ("Subheader", x => x.Subheader)));
                    }

                    // T1 place 4: GetProjectNames (trailing spaces sent to BOTH sides)
                    var sqlProjNames = await sqlTankRepo.GetProjectNames(imoStr + "   ");
                    var cosProjNames = await cosmosTankRepo.GetProjectNames(imoStr + "   ");
                    methodPositiveRows["Tank.GetProjectNames"] = sqlProjNames.Count;
                    checks.Add(ParityCompare.Lists($"T1_P4.Tank.GetProjectNames({imoStr})", sqlProjNames, cosProjNames, x => x.ProjectId, false,
                        ("Text", x => x.Text),
                        ("Value", x => x.Value),
                        ("ProjectId", x => x.ProjectId)));
                }

                // R1 & R2: ReportPart (all columns, real and null)
                {
                    var sqlSecNames = await sqlReportPartRepo.GetSectionNamesByPartNameAsync(1, "HULL");
                    var cosSecNames = await cosmosReportPartRepo.GetSectionNamesByPartNameAsync(1, "HULL");
                    methodPositiveRows["ReportPart.GetSectionNamesByPartNameAsync"] = sqlSecNames.NormalSections?.Count ?? 0;
                    checks.Add(ParityCompare.Lists("R1.ReportPart.GetSectionNamesByPartNameAsync.Normal", sqlSecNames.NormalSections ?? new List<NormalSectionNames>(), cosSecNames.NormalSections ?? new List<NormalSectionNames>(), x => x.SectionId, false,
                        ("SectionId", x => x.SectionId),
                        ("SectionName", x => x.SectionName),
                        ("PartId", x => x.PartId),
                        ("PartName", x => x.PartName),
                        ("VesselTypeId", x => x.VesselTypeId),
                        ("SubHeader", x => x.SubHeader),
                        ("TotalCards", x => x.TotalCards),
                        ("PlaceholderCount", x => x.PlaceholderCount),
                        ("FileNameCount", x => x.FileNameCount),
                        ("IsActive", x => x.IsActive)));

                    var sqlSecNamesNull = await sqlReportPartRepo.GetSectionNamesByPartNameAsync(1, null!);
                    var cosSecNamesNull = await cosmosReportPartRepo.GetSectionNamesByPartNameAsync(1, null!);
                    bool eqR1Null = (sqlSecNamesNull.NormalSections?.Count ?? 0) == 0 && (cosSecNamesNull.NormalSections?.Count ?? 0) == 0;
                    checks.Add(new { check = "R1.ReportPart.GetSectionNamesByPartNameAsync.NullPartName", equal = eqR1Null, sqlCount = sqlSecNamesNull.NormalSections?.Count ?? 0, cosCount = cosSecNamesNull.NormalSections?.Count ?? 0 });
                    if (!eqR1Null) allPassed = false;

                    var sqlExportSec = await sqlReportPartRepo.GetVesselTypeExportSectionConfiguration(1, 1);
                    var cosExportSec = await cosmosReportPartRepo.GetVesselTypeExportSectionConfiguration(1, 1);
                    methodPositiveRows["ReportPart.GetVesselTypeExportSectionConfiguration"] = sqlExportSec.Count;
                    checks.Add(ParityCompare.Lists("R2.ReportPart.GetVesselTypeExportSectionConfiguration(1, 1)", sqlExportSec, cosExportSec, x => x.SectionId, false,
                        ("SectionId", x => x.SectionId),
                        ("SectionName", x => x.SectionName)));
                }

                // ==========================================
                // 7. Known Bug Checks (T-f)
                // ==========================================
                // P1: GetAllProjects() throws
                {
                    Type? sqlP1Ex = null; string? sqlP1Msg = null;
                    Type? cosP1Ex = null; string? cosP1Msg = null;
                    try { await sqlProjectRepo.GetAllProjects(); } catch (Exception ex) { sqlP1Ex = ex.GetType(); sqlP1Msg = ex.Message; }
                    try { await cosmosProjectRepo.GetAllProjects(); } catch (Exception ex) { cosP1Ex = ex.GetType(); cosP1Msg = ex.Message; }
                    bool p1Ok = sqlP1Ex != null && cosP1Ex != null
                        && sqlP1Ex == cosP1Ex
                        && sqlP1Msg != null && cosP1Msg != null
                        && string.Equals(sqlP1Msg, cosP1Msg, StringComparison.OrdinalIgnoreCase);
                    checks.Add(new { check = "T-f.KnownBug.P1.GetAllProjects", equal = p1Ok, sqlEx = sqlP1Ex?.Name, cosEx = cosP1Ex?.Name, sqlP1Msg, cosP1Msg });
                    if (!p1Ok) allPassed = false;
                }

                // V8: UpdateReportMainDataAsync on missing project throws
                {
                    Type? sqlV8Ex = null; string? sqlV8Msg = null;
                    Type? cosV8Ex = null; string? cosV8Msg = null;
                    try { await sqlVesselRepo.UpdateReportMainDataAsync(new Report(), -99888); } catch (Exception ex) { sqlV8Ex = ex.GetType(); sqlV8Msg = ex.Message; }
                    try { await cosmosVesselRepo.UpdateReportMainDataAsync(new Report(), -99888); } catch (Exception ex) { cosV8Ex = ex.GetType(); cosV8Msg = ex.Message; }
                    bool v8Ok = sqlV8Ex != null && cosV8Ex != null
                        && sqlV8Ex == cosV8Ex
                        && sqlV8Msg != null && cosV8Msg != null
                        && string.Equals(sqlV8Msg, cosV8Msg, StringComparison.OrdinalIgnoreCase);
                    checks.Add(new { check = "T-f.KnownBug.V8.UpdateReportMainDataAsync.MissingProject", equal = v8Ok, sqlEx = sqlV8Ex?.Name, cosEx = cosV8Ex?.Name, sqlV8Msg, cosV8Msg });
                    if (!v8Ok) allPassed = false;
                }

                // V15: GetSurveyStatus returns null, UpdateSurveyAudit throws ArgumentNullException
                if (realProject.Id != 0)
                {
                    var sqlV15Res = await sqlVesselRepo.GetSurveyStatus(realProject.Id);
                    var cosV15Res = await cosmosVesselRepo.GetSurveyStatus(realProject.Id);
                    bool v15GetOk = sqlV15Res == null && cosV15Res == null;
                    checks.Add(new { check = $"T-f.KnownBug.V15.GetSurveyStatus({realProject.Id})", equal = v15GetOk, sqlNull = sqlV15Res == null, cosNull = cosV15Res == null });
                    if (!v15GetOk) allPassed = false;

                    Type? sqlV15AuditEx = null; string? sqlV15AuditMsg = null;
                    Type? cosV15AuditEx = null; string? cosV15AuditMsg = null;
                    try { await sqlVesselRepo.UpdateSurveyAudit(new SurveyStatus(), realProject.Id); } catch (Exception ex) { sqlV15AuditEx = ex.GetType(); sqlV15AuditMsg = ex.Message; }
                    try { await cosmosVesselRepo.UpdateSurveyAudit(new SurveyStatus(), realProject.Id); } catch (Exception ex) { cosV15AuditEx = ex.GetType(); cosV15AuditMsg = ex.Message; }
                    bool v15AuditOk = sqlV15AuditEx != null && cosV15AuditEx != null
                        && sqlV15AuditEx == typeof(ArgumentNullException) && cosV15AuditEx == typeof(ArgumentNullException);
                    checks.Add(new { check = $"T-f.KnownBug.V15.UpdateSurveyAudit({realProject.Id})", equal = v15AuditOk, sqlEx = sqlV15AuditEx?.Name, cosEx = cosV15AuditEx?.Name, sqlV15AuditMsg, cosV15AuditMsg });
                    if (!v15AuditOk) allPassed = false;
                }

                // R-2arg: CreateVesselMainDataAsync 2-argument returns null
                {
                    var sqlR2 = await sqlVesselRepo.CreateVesselMainDataAsync("-999", new Project { Id = -999 });
                    var cosR2 = await cosmosVesselRepo.CreateVesselMainDataAsync("-999", new Project { Id = -999 });
                    bool r2Ok = sqlR2 == null && cosR2 == null;
                    checks.Add(new { check = "T-f.KnownBug.R2arg.CreateVesselMainDataAsync", equal = r2Ok, sqlNull = sqlR2 == null, cosNull = cosR2 == null });
                    if (!r2Ok) allPassed = false;
                }

                // ==========================================
                // 8. V4 & V5 Helper Checks (T-g)
                // ==========================================
                {
                    var testDates = new[]
                    {
                        new DateTime(2026, 5, 4, 3, 15, 0),   // single-digit day (4) and hour (3)
                        new DateTime(2026, 11, 20, 0, 5, 0),  // 12 AM
                        new DateTime(2026, 11, 20, 12, 0, 0), // 12 PM
                        new DateTime(1900, 1, 1, 0, 0, 0)     // Jan  1 1900 12:00AM
                    };
                    foreach (var td in testDates)
                    {
                        var sqlVal = await conn.QueryFirstOrDefaultAsync<string>("SELECT CONVERT(nvarchar(max), @d)", new { d = td });
                        var helperVal = CosmosVesselRepository.FormatSqlStyle0(td);
                        bool eq = string.Equals(sqlVal, helperVal, StringComparison.OrdinalIgnoreCase);
                        checks.Add(new { check = $"T-g.V4.FormatSqlStyle0({td:yyyy-MM-dd HH:mm})", sql = sqlVal, cosmos = helperVal, equal = eq });
                        if (!eq) allPassed = false;
                    }

                    var testReals = new object[] { 0, 1.5, 123456.7, 1234567, 0.0001234, -12.25, 1e-5 };
                    foreach (var tr in testReals)
                    {
                        var sqlVal = await conn.QueryFirstOrDefaultAsync<string>("SELECT CONVERT(nvarchar(max), CAST(@v AS real))", new { v = tr });
                        var helperVal = CosmosVesselRepository.FormatReal(tr);
                        bool eq = string.Equals(sqlVal, helperVal, StringComparison.OrdinalIgnoreCase);
                        checks.Add(new { check = $"T-g.V5.FormatReal({tr})", sql = sqlVal, cosmos = helperVal, equal = eq });
                        if (!eq) allPassed = false;
                    }
                }

                // ==========================================
                // 9. Method Positive Rows Check (T-b)
                // ==========================================
                foreach (var kvp in methodPositiveRows)
                {
                    bool hasPositive = kvp.Value > 0;
                    checks.Add(new { check = $"T-b.PositiveRowsCheck.{kvp.Key}", equal = hasPositive, rowCount = kvp.Value });
                    if (!hasPositive) allPassed = false;
                }

                // ==========================================
                // 10. Write Tests in Partitions -999 / -998 (T-h)
                // ==========================================
                if (!string.Equals(writeTest, "true", StringComparison.OrdinalIgnoreCase))
                {
                    allPassed = allPassed && !hadException
                        && checks.All(c =>
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
                        summary = allPassed ? "ALL READ PARITY CHECKS PASSED" : "ONE OR MORE PARITY CHECKS FAILED",
                        allPassed,
                        checksCount = checks.Count,
                        checks,
                        writeStepsSkipped = "Pass ?writeTest=true to execute write tests in partition -999/-998"
                    });
                }

                var clientsContainer = containerFactory.GetContainer(CosmosContainers.CAP_Clients);
                var userContainer = containerFactory.GetContainer(CosmosContainers.dbo_UserAccounts);
                var commentsContainer = containerFactory.GetContainer(CosmosContainers.CAP_Comments);
                var fileContainer = containerFactory.GetContainer(CosmosContainers.CAP_Files);
                var projectContainer = containerFactory.GetContainer(CosmosContainers.CAP_Projects);
                var tankContainer = containerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
                var vesselContainer = containerFactory.GetContainer(CosmosContainers.dbo_ReportVesselMainData);
                var certsContainer = containerFactory.GetContainer(CosmosContainers.dbo_ReportStatutoryCertificates);
                var rolePermContainer = containerFactory.GetContainer(CosmosContainers.dbo_RolePermissionsMapping);
                var actContainer = containerFactory.GetContainer(CosmosContainers.CAP_Activities);
                var ptsContainer = containerFactory.GetContainer(CosmosContainers.CAP_ProjectTaskStatus);

                var cleanupList = new List<(Container container, string id, PartitionKey pk)>();

                try
                {
                    // Empty-before check across all 11 write containers
                    var preCp = await SafeQueryAsync<object>(clientsContainer, "SELECT * FROM c WHERE c.id = 'ffffffff-ffff-ffff-ffff-ffffffff9991'");
                    var preUser = await SafeQueryAsync<object>(userContainer, "SELECT * FROM c WHERE c.id = 'ffffffff-ffff-ffff-ffff-ffffffff9992'");
                    var preRolePerm = await SafeQueryAsync<object>(rolePermContainer, "SELECT * FROM c WHERE c.PermissionId = 'ffffffff-ffff-ffff-ffff-ffffffff9996' OR c.Id = 'ffffffff-ffff-ffff-ffff-ffffffff9998'");
                    var preComment = await SafeQueryAsync<object>(commentsContainer, "SELECT * FROM c WHERE c.ProjectId IN (-999, -998)");
                    var preFile = await SafeQueryAsync<object>(fileContainer, "SELECT * FROM c WHERE c.ProjectId IN (-999, -998)");
                    var preProj = await SafeQueryAsync<object>(projectContainer, "SELECT * FROM c WHERE c.id IN ('-999', '-998')");
                    var preTank = await SafeQueryAsync<object>(tankContainer, "SELECT * FROM c WHERE c.ProjectId IN (-999, -998)");
                    var preVessel = await SafeQueryAsync<object>(vesselContainer, "SELECT * FROM c WHERE c.id IN ('-999', '-998')");
                    var preCert = await SafeQueryAsync<object>(certsContainer, "SELECT * FROM c WHERE c.id IN ('-999', '-998')");
                    var preAct = await SafeQueryAsync<object>(actContainer, "SELECT * FROM c WHERE c.ProjectId IN (-999, -998)");
                    var prePts = await SafeQueryAsync<object>(ptsContainer, "SELECT * FROM c WHERE c.ProjectId IN (-999, -998)");

                    bool preCheckPassed = preCp.Count == 0 && preUser.Count == 0 && preRolePerm.Count == 0 && preComment.Count == 0
                        && preFile.Count == 0 && preProj.Count == 0 && preTank.Count == 0 && preVessel.Count == 0
                        && preCert.Count == 0 && preAct.Count == 0 && prePts.Count == 0;

                    if (!preCheckPassed)
                    {
                        writeSteps.Add(new { step = "T-h.PreCheck", passed = false, error = "Partitions -999 / -998 are not empty. Skipping all writes." });
                        allPassed = false;
                    }
                    else
                    {
                        // A6: AddRolePermissionAsync (writes rolepermission.Id on every row, distinct id, cleaned up)
                        var templateGuid = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff9996");
                        var roleIdGuid = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff9997");
                        var templateId = "tmpl-" + templateGuid.ToString("D").ToLowerInvariant();
                        var templateDoc = new RolePermissionsMappingDocument
                        {
                            id = templateId,
                            Id = Guid.NewGuid(),
                            PermissionId = templateGuid,
                            RoleId = null,
                            IsReadable = true,
                            IsEditable = true
                        };
                        await rolePermContainer.CreateItemAsync(templateDoc, new PartitionKey(templateId));
                        cleanupList.Add((rolePermContainer, templateId, new PartitionKey(templateId)));

                        var inputGuid = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff9998");
                        bool addRes = await cosmosPermRepo.AddRolePermissionAsync(new DigitalCap.Core.Models.Permissions.UserRolePermission
                        {
                            Id = inputGuid,
                            RoleId = roleIdGuid,
                            PermissionId = templateGuid
                        });

                        var createdRolePerms = await SafeQueryAsync<RolePermissionsMappingDocument>(
                            rolePermContainer,
                            $"SELECT * FROM c WHERE c.RoleId = '{roleIdGuid}'");

                        bool a6Ok = addRes && createdRolePerms.Count == 1 && createdRolePerms[0].Id == inputGuid;
                        foreach (var doc in createdRolePerms)
                        {
                            cleanupList.Add((rolePermContainer, doc.id, new PartitionKey(doc.id)));
                        }
                        writeSteps.Add(new { step = "A6.UserPermission.AddRolePermissionAsync", passed = a6Ok });
                        if (!a6Ok) allPassed = false;

                        // A10: ClientProfile.SaveClientProfileAsync missing throws; AddAsync rejects null; AddAsync valid writes and reads back; SaveClientProfileAsync updates
                        bool a10MissingThrows = false;
                        try
                        {
                            await cosmosClientProfileRepo.SaveClientProfileAsync(new Client { Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff9999"), CompanyName = "Missing" });
                        }
                        catch (Exception ex) when (ex.Message.Contains("Update failed. No rows affected."))
                        {
                            a10MissingThrows = true;
                        }

                        bool a10NullReject = false;
                        try
                        {
                            await cosmosClientProfileRepo.AddAsync(new Client
                            {
                                Id = Guid.NewGuid(),
                                CompanyName = null!,
                                CompanyAddress = "ValidAddress"
                            });
                        }
                        catch (ArgumentException)
                        {
                            a10NullReject = true;
                        }

                        var a10Guid = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff9991");
                        var a10Id = a10Guid.ToString("D").ToLowerInvariant();
                        await cosmosClientProfileRepo.AddAsync(new Client
                        {
                            Id = a10Guid,
                            CompanyName = "TestClientA10",
                            CompanyAddress = "123 Valid St",
                            Wcn = "WCN1"
                        });
                        cleanupList.Add((clientsContainer, a10Id, new PartitionKey(a10Id)));

                        var readA10 = await SafeReadAsync<ClientsDocument>(clientsContainer, a10Id, new PartitionKey(a10Id));
                        bool a10InsertOk = readA10 != null && readA10.CompanyName == "TestClientA10" && readA10.CompanyAddress == "123 Valid St" && readA10.Wcn == "WCN1";

                        await cosmosClientProfileRepo.SaveClientProfileAsync(new Client
                        {
                            Id = a10Guid,
                            CompanyName = "TestClientA10_Updated",
                            CompanyAddress = "456 Updated St"
                        });
                        var readA10Upd = await SafeReadAsync<ClientsDocument>(clientsContainer, a10Id, new PartitionKey(a10Id));
                        bool a10UpdOk = readA10Upd != null && readA10Upd.CompanyName == "TestClientA10_Updated" && readA10Upd.CompanyAddress == "456 Updated St";

                        bool a10AllOk = a10MissingThrows && a10NullReject && a10InsertOk && a10UpdOk;
                        writeSteps.Add(new { step = "A10.ClientProfile.ValidationAndFullReadBack", passed = a10AllOk, a10MissingThrows, a10NullReject, a10InsertOk, a10UpdOk });
                        if (!a10AllOk) allPassed = false;

                        // A11: UserAccount.InsertAsync rejects duplicate PK
                        var a11Guid = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffff9992");
                        var a11Id = a11Guid.ToString("D").ToLowerInvariant();
                        var a11Doc = new UserAccountDocument
                        {
                            id = a11Id,
                            Id = a11Guid,
                            ClientId = a11Guid,
                            FirstName = "TestA11",
                            Deleted = false
                        };
                        await userContainer.CreateItemAsync(a11Doc, new PartitionKey(a11Id));
                        cleanupList.Add((userContainer, a11Id, new PartitionKey(a11Id)));

                        bool a11DuplicateRejected = false;
                        try
                        {
                            await cosmosUserAccountRepo.InsertAsync(new UserAccountModel
                            {
                                Id = a11Guid,
                                ClientId = Guid.NewGuid(),
                                FirstName = "Duplicate"
                            });
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("Violation of PRIMARY KEY constraint"))
                        {
                            a11DuplicateRejected = true;
                        }
                        writeSteps.Add(new { step = "A11.UserAccount.InsertAsync.RejectsDuplicatePK", passed = a11DuplicateRejected });
                        if (!a11DuplicateRejected) allPassed = false;

                        // B8 Comment: CreateCommentAsync rejects empty, writes activity; UpdateCommentAsync missing no throw
                        bool b8CommentReject = false;
                        try
                        {
                            await cosmosCommentRepo.CreateCommentAsync(-999, null, "admin", "");
                        }
                        catch (ArgumentException)
                        {
                            b8CommentReject = true;
                        }

                        var b8Acts = await SafeQueryAsync<ActivityDocument>(
                            actContainer,
                            "SELECT * FROM c WHERE c.ProjectId = -999",
                            new PartitionKey(-999.0));

                        bool b8ActWritten = b8Acts.Count > 0 && b8Acts.Any(a => a.Text == null && a.CreateBy == "admin");
                        foreach (var a in b8Acts)
                        {
                            cleanupList.Add((actContainer, a.id, new PartitionKey(-999.0)));
                        }

                        bool b8UpdThrown = false;
                        try
                        {
                            await cosmosCommentRepo.UpdateCommentAsync(-99888, -99888, null, "new text", "admin");
                        }
                        catch
                        {
                            b8UpdThrown = true;
                        }

                        bool b8CommentOk = b8CommentReject && b8ActWritten && !b8UpdThrown;
                        writeSteps.Add(new { step = "B8.Comment.ThrowsActivityWrittenAndMissingNoThrow", passed = b8CommentOk, b8CommentReject, b8ActWritten, missingNoThrow = !b8UpdThrown });
                        if (!b8CommentOk) allPassed = false;

                        // B8 FileStorage: Save null throws; Update missing throws; nothing written
                        bool b8FileSaveReject = false;
                        try
                        {
                            await cosmosFileStorageRepo.SaveProjectFile(new ProjectFile { ProjectId = -999, Name = null! }, "dummyKey", "admin");
                        }
                        catch (ArgumentNullException)
                        {
                            b8FileSaveReject = true;
                        }

                        bool b8FileUpdReject = false;
                        try
                        {
                            await cosmosFileStorageRepo.UpdateFileMetadata(-99888, FileDataType.Photos, "new_name", true, "admin");
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("Could not find file"))
                        {
                            b8FileUpdReject = true;
                        }

                        var fileDocs = await SafeQueryAsync<object>(fileContainer, "SELECT * FROM c WHERE c.ProjectId = -999", new PartitionKey(-999.0));
                        bool b8FileOk = b8FileSaveReject && b8FileUpdReject && fileDocs.Count == 0;
                        writeSteps.Add(new { step = "B8.FileStorage.ValidationAndMissingThrowsNothingWritten", passed = b8FileOk, b8FileSaveReject, b8FileUpdReject, fileCount = fileDocs.Count });
                        if (!b8FileOk) allPassed = false;

                        // P12 & P13 on all 4 methods: UpdatePercentComplete, Update_Project, UpdateProjectStatus, UpdatePriority
                        var projDoc = new ProjectDocument
                        {
                            id = "-999",
                            ID = -999,
                            Name = "TestProject-999",
                            PercentComplete = 10,
                            ProjectStatus = 1,
                            ProjectPriority = 1,
                            CapRegion = "Americas",
                            IsDeleted = false
                        };
                        await projectContainer.CreateItemAsync(projDoc, new PartitionKey("-999"));
                        cleanupList.Add((projectContainer, "-999", new PartitionKey("-999")));

                        // Method 1: UpdatePercentComplete (also tests P11: currentUser null -> "SYSTEM")
                        int updPcExists = await cosmosProjectRepo.UpdatePercentComplete(-999, 85, null!);
                        int updPcMissing = await cosmosProjectRepo.UpdatePercentComplete(-99888, 85, null!);
                        var readPc = await SafeReadAsync<ProjectDocument>(projectContainer, "-999", new PartitionKey("-999"));
                        bool p12Ok = updPcExists == 1 && updPcMissing == 0 && readPc?.PercentComplete == 85;

                        // Method 2: Update_Project
                        await cosmosProjectRepo.Update_Project(new Project { Id = -999, Name = "UpdatedProject-999", CRMOpportunityNumber = "CRM999" }, "admin");
                        await cosmosProjectRepo.Update_Project(new Project { Id = -99888, Name = "Missing" }, "admin");
                        var readProj = await SafeReadAsync<ProjectDocument>(projectContainer, "-999", new PartitionKey("-999"));
                        bool p13ProjOk = readProj?.Name == "UpdatedProject-999";

                        // Method 3: UpdateProjectStatus
                        await cosmosProjectRepo.UpdateProjectStatus(-999, "uid", 2, "admin");
                        await cosmosProjectRepo.UpdateProjectStatus(-99888, "uid", 2, "admin");
                        var readStatus = await SafeReadAsync<ProjectDocument>(projectContainer, "-999", new PartitionKey("-999"));
                        bool p13StatusOk = readStatus?.ProjectStatus == 2;

                        // Method 4: UpdatePriority
                        await cosmosProjectRepo.UpdatePriority(-999, 5, "admin");
                        await cosmosProjectRepo.UpdatePriority(-999, null, "admin");
                        await cosmosProjectRepo.UpdatePriority(-99888, 5, "admin");
                        var readPrio = await SafeReadAsync<ProjectDocument>(projectContainer, "-999", new PartitionKey("-999"));
                        bool p13PrioOk = readPrio?.ProjectPriority == null;

                        bool all4MethodsOk = p12Ok && p13ProjOk && p13StatusOk && p13PrioOk;
                        writeSteps.Add(new { step = "P12_P13.Project.All4UpdateMethods.ExistsAndMissing", passed = all4MethodsOk, p12Ok, p13ProjOk, p13StatusOk, p13PrioOk });
                        if (!all4MethodsOk) allPassed = false;

                        // P16: CreateProject text length cut, dates .Date, ShipType int mapping, full read back
                        var newProj = new Project
                        {
                            Name = "TestP16",
                            CRMOpportunityNumber = "12345678901234567890EXTRAEXTRA", // > 20 chars
                            VesselType = new string('A', 500), // > 450 chars
                            ShipType = ShipType.BulkCarrier,
                            PotentialDrydockDate = new DateTime(2026, 11, 20, 14, 30, 0)
                        };
                        int newP16Id = await cosmosProjectRepo.CreateProject(newProj, "admin");
                        cleanupList.Add((projectContainer, newP16Id.ToString(), new PartitionKey(newP16Id.ToString())));
                        var readP16 = await SafeReadAsync<ProjectDocument>(projectContainer, newP16Id.ToString(), new PartitionKey(newP16Id.ToString()));
                        bool p16Ok = readP16 != null
                            && readP16.CRMOpportunityNumber?.Length == 20
                            && readP16.VesselType?.Length == 450
                            && readP16.CreatedDate?.TimeOfDay == TimeSpan.Zero
                            && readP16.PotentialDrydockDate?.TimeOfDay == TimeSpan.Zero
                            && readP16.ShipType?.ToString() == "1"; // ShipType.BulkCarrier int mapping
                        writeSteps.Add(new { step = "P16.CreateProject.TextCutAndDatesAndShipType", passed = p16Ok });
                        if (!p16Ok) allPassed = false;

                        // T4: CreateTank date < 1753 rejected without write; valid tank writes and reads back
                        var badTank = new VesselTank
                        {
                            Id = Guid.NewGuid(),
                            ProjectId = -999,
                            TankName = "BadDateTank",
                            CreatedDttm = new DateTime(1700, 1, 1),
                            UpdateDttm = new DateTime(1700, 1, 1)
                        };
                        bool createBadTankRes = await cosmosTankRepo.CreateTank(badTank);
                        var readBadTank = await SafeReadAsync<VesselTankMappingDocument>(tankContainer, badTank.Id.ToString(), new PartitionKey(-999.0));
                        bool t4BadOk = !createBadTankRes && readBadTank == null;

                        var goodTankGuid = Guid.NewGuid();
                        var goodTank = new VesselTank
                        {
                            Id = goodTankGuid,
                            ProjectId = -999,
                            TankName = "GoodTank",
                            TankTypeId = 1,
                            CreatedDttm = new DateTime(2026, 11, 20),
                            UpdateDttm = new DateTime(2026, 11, 20)
                        };
                        bool createGoodTankRes = await cosmosTankRepo.CreateTank(goodTank);
                        cleanupList.Add((tankContainer, goodTankGuid.ToString(), new PartitionKey(-999.0)));
                        var readGoodTank = await SafeReadAsync<VesselTankMappingDocument>(tankContainer, goodTankGuid.ToString(), new PartitionKey(-999.0));
                        bool t4GoodOk = createGoodTankRes && readGoodTank != null && readGoodTank.TankName == "GoodTank" && readGoodTank.TankTypeId == 1;

                        bool t4Ok = t4BadOk && t4GoodOk;
                        writeSteps.Add(new { step = "T4.Tank.CreateTank.ValidationAndWrite", passed = t4Ok, t4BadOk, t4GoodOk });
                        if (!t4Ok) allPassed = false;

                        // T3 (T-i): CreateTank rejects duplicate PK (loops all pages of COUNT query)
                        bool createDupTankRes = await cosmosTankRepo.CreateTank(goodTank);
                        bool t3Ok = !createDupTankRes;
                        writeSteps.Add(new { step = "T3.Tank.CreateTank.RejectsDuplicatePK", passed = t3Ok });
                        if (!t3Ok) allPassed = false;

                        // V6 & V10: CreateVesselMainDataAsync (3 args) writes main data, full read back
                        var v10Project = new Project
                        {
                            Id = -999,
                            VesselName = "TestVessel-999",
                            IMO = 1234567,
                            Builder = "V10Builder",
                            LengthOverall = 120.50m
                        };
                        var v10Vessel = new DigitalCap.Core.Models.VesselModel.Vessel
                        {
                            ImoNumber = "1234567",
                            SelectedPrefix = 0
                        };
                        await cosmosVesselRepo.CreateVesselMainDataAsync("-999", v10Project, v10Vessel);
                        cleanupList.Add((vesselContainer, "-999", new PartitionKey("-999")));
                        var readV10 = await SafeReadAsync<ReportVesselMainDataDocument>(vesselContainer, "-999", new PartitionKey("-999"));
                        bool v10Ok = readV10 != null
                            && readV10.vesselName == "TestVessel-999"
                            && readV10.imoNumber == 1234567
                            && readV10.SelectedPrefix == 0
                            && readV10.builtBy == "V10Builder";
                        writeSteps.Add(new { step = "V6_V10.Vessel.CreateVesselMainDataAsync.PrefixAndFullWrite", passed = v10Ok });
                        if (!v10Ok) allPassed = false;

                        // V12: UpdateStatutoryCertificate preserves isExpiryDate
                        var expectedExpiry = new DateTime(2028, 5, 20);
                        var certDoc = new ReportStatutoryCertificatesDocument
                        {
                            id = "-999",
                            projectId = -999,
                            isExpiryDate = expectedExpiry
                        };
                        await certsContainer.CreateItemAsync(certDoc, new PartitionKey("-999"));
                        cleanupList.Add((certsContainer, "-999", new PartitionKey("-999")));

                        await cosmosVesselRepo.UpdateStatutoryCertificate(-999, new Report
                        {
                            StatutoryCertificatesIssuedDates = new StatutoryCertificates
                            {
                                IEEDate = new DateTime(2025, 6, 1)
                            }
                        });
                        var readCert = await SafeReadAsync<ReportStatutoryCertificatesDocument>(certsContainer, "-999", new PartitionKey("-999"));
                        bool v12Ok = readCert != null && readCert.isExpiryDate == expectedExpiry && readCert.ieeIssueDate.HasValue;
                        writeSteps.Add(new { step = "V12.UpdateStatutoryCertificate.PreservesIsExpiryDate", passed = v12Ok });
                        if (!v12Ok) allPassed = false;

                        // V13: UpdateReportMainDataAsync on missing project throws
                        bool v13Throws = false;
                        try
                        {
                            await cosmosVesselRepo.UpdateReportMainDataAsync(new Report(), -99888);
                        }
                        catch (InvalidOperationException ex) when (ex.Message.Contains("Insert_VesselDetails has too many arguments specified"))
                        {
                            v13Throws = true;
                        }
                        writeSteps.Add(new { step = "V13.Vessel.UpdateReportMainDataAsync.MissingProjectThrows", passed = v13Throws });
                        if (!v13Throws) allPassed = false;

                        // V14: UpdateSurveyAudit date < 1753 swallowed without error
                        bool v14Res = await cosmosVesselRepo.UpdateSurveyAudit(new SurveyStatus
                        {
                            ClassSurveyDate = new DateTime(1700, 1, 1)
                        }, -999);
                        writeSteps.Add(new { step = "V14.UpdateSurveyAudit.Under1753Swallowed", passed = v14Res });
                        if (!v14Res) allPassed = false;

                        // R-2arg: CreateVesselMainDataAsync 2-argument writes nothing, returns null
                        var preCountR2 = (await SafeQueryAsync<object>(vesselContainer, "SELECT * FROM c WHERE c.id = '-998'", new PartitionKey("-998"))).Count;
                        var resR2 = await cosmosVesselRepo.CreateVesselMainDataAsync("-998", new Project { Id = -998 });
                        var postCountR2 = (await SafeQueryAsync<object>(vesselContainer, "SELECT * FROM c WHERE c.id = '-998'", new PartitionKey("-998"))).Count;
                        bool r2WritesNothing = resR2 == null && preCountR2 == postCountR2 && postCountR2 == 0;
                        writeSteps.Add(new { step = "R2arg.CreateVesselMainDataAsync.WritesNothingAndReturnsNull", passed = r2WritesNothing });
                        if (!r2WritesNothing) allPassed = false;

                        // B3 & B4: UpdateTask on CAP_ProjectTaskStatus with trimmed assignedTo (B3) and null TaskId (B4)
                        var ptsDoc = new ProjectTaskStatusDocument
                        {
                            id = "-9991",
                            ID = -9991,
                            ProjectId = -999,
                            TaskId = null, // B4: TaskId is null -> skips task lookup, sets task name null
                            AssignedTo = "John Doe   ", // B3: trimmed to "John Doe"
                            StatusId = 1
                        };
                        await ptsContainer.CreateItemAsync(ptsDoc, new PartitionKey(-999.0));
                        cleanupList.Add((ptsContainer, "-9991", new PartitionKey(-999.0)));

                        bool b3b4Res = await cosmosTaskRepo.UpdateTask(new SingleTask
                        {
                            ID = -9991,
                            ProjectId = -999,
                            TaskId = null,
                            AssignedTo = "John Doe",
                            StatusId = 2
                        }, "admin");

                        var readPts = await SafeReadAsync<ProjectTaskStatusDocument>(ptsContainer, "-9991", new PartitionKey(-999.0));
                        bool b3b4Ok = b3b4Res && readPts != null && readPts.AssignedTo == "John Doe" && readPts.StatusId == 2;
                        writeSteps.Add(new { step = "B3_B4.Task.UpdateTask.AssignedToTrimmedAndNullTaskId", passed = b3b4Ok });
                        if (!b3b4Ok) allPassed = false;

                        // E1: UpdateTankSectionRequiredInReportAsync with imo 0 matching "   "
                        var e1TankGuid = Guid.NewGuid();
                        var e1TankDoc = new VesselTankMappingDocument
                        {
                            id = e1TankGuid.ToString(),
                            Id = e1TankGuid.ToString(),
                            ProjectId = -999,
                            ImoNumber = "   ", // spaces-only -> matches docImo == 0
                            RequiredInReport = false
                        };
                        await tankContainer.CreateItemAsync(e1TankDoc, new PartitionKey(-999.0));
                        cleanupList.Add((tankContainer, e1TankGuid.ToString(), new PartitionKey(-999.0)));

                        await cosmosExportRepo.UpdateTankSectionRequiredInReportAsync(true, e1TankGuid, 0, "admin");
                        var readE1Tank = await SafeReadAsync<VesselTankMappingDocument>(tankContainer, e1TankGuid.ToString(), new PartitionKey(-999.0));
                        bool e1Ok = readE1Tank != null && readE1Tank.RequiredInReport == true;
                        writeSteps.Add(new { step = "E1.Export.UpdateTankRequiredInReport.Imo0MatchesSpaces", passed = e1Ok });
                        if (!e1Ok) allPassed = false;
                    }
                }
                catch (Exception ex)
                {
                    hadException = true;
                    writeSteps.Add(new { step = "WriteTests.Exception", passed = false, error = ex.Message });
                    allPassed = false;
                }
                finally
                {
                    foreach (var (cont, id, pk) in cleanupList)
                    {
                        try
                        {
                            await cont.DeleteItemAsync<object>(id, pk);
                        }
                        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            // ignore NotFound only
                        }
                        catch (Exception ex)
                        {
                            writeSteps.Add(new { step = "Cleanup.Error", id, passed = false, error = ex.Message });
                        }
                    }
                }

                allPassed = allPassed && !hadException
                    && checks.All(c =>
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
                    })
                    && writeSteps.All(w =>
                    {
                        var prop = w.GetType().GetProperty("passed");
                        if (prop != null)
                        {
                            var val = prop.GetValue(w);
                            if (val is bool b) return b;
                        }
                        return false;
                    });

                return Results.Ok(new
                {
                    summary = allPassed ? "ALL PARITY AND WRITE TESTS PASSED" : "ONE OR MORE TESTS FAILED",
                    allPassed,
                    hadException,
                    checksCount = checks.Count,
                    writeStepsCount = writeSteps.Count,
                    checks,
                    writeSteps
                });
            }).AllowAnonymous();
        }
    }
}
