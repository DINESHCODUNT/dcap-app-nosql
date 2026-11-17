using Azure.Identity;
using DigitalCap.Infrastructure.DbContext;
using DigitalCap.Infrastructure.Identity;
using DigitalCap.Persistence.Cosmos.Configuration;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using DigitalCap.Persistence.Extensions;
using DigitalCap.WebApi.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
// Load secrets from Azure Key Vault (works locally via VS/CLI login; in Azure via Managed Identity)
var keyVaultUri = builder.Configuration["KeyVaultUri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}
var configuration = builder.Configuration;
// Add services to the container.

builder.Services.AddControllers(options =>
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

// Configure large request body limits for sync uploads (160 MB, matching reference)
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 167773180;
});
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 167773180;
});
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueLengthLimit = 167773180;
    options.MultipartBodyLengthLimit = 167773180;
    options.MultipartHeadersLengthLimit = 167773180;
});
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<OpenIdConnectOptions>(options =>
{
    options.TokenValidationParameters.NameClaimType = "preferred_username";
    // OR
    options.TokenValidationParameters.NameClaimType = ClaimTypes.Upn;
});

var identityProvider = builder.Configuration["Identity:Provider"] ?? "Sql";
var useCosmosIdentity = string.Equals(identityProvider, "Cosmos", StringComparison.OrdinalIgnoreCase);

if (useCosmosIdentity)
{
    var cosmosSection = builder.Configuration.GetSection("CosmosDb");
    var cosmosOptions = cosmosSection.Get<CosmosDbOptions>()
        ?? throw new InvalidOperationException("Missing configuration section 'CosmosDb'.");

    if (string.IsNullOrWhiteSpace(cosmosOptions.Endpoint))
    {
        throw new InvalidOperationException("Cosmos DB configuration is missing 'CosmosDb:Endpoint'.");
    }

    if (!cosmosOptions.UseManagedIdentity && string.IsNullOrWhiteSpace(cosmosOptions.Key))
    {
        throw new InvalidOperationException("Cosmos DB configuration is missing 'CosmosDb:Key'.");
    }

    if (string.IsNullOrWhiteSpace(cosmosOptions.DatabaseName))
    {
        throw new InvalidOperationException("Cosmos DB configuration is missing 'CosmosDb:DatabaseName'.");
    }

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        void ConfigureCosmos(Microsoft.EntityFrameworkCore.Infrastructure.CosmosDbContextOptionsBuilder cosmosBuilder)
        {
            if (cosmosOptions.UseEmulator)
            {
                cosmosBuilder.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);
                cosmosBuilder.LimitToEndpoint();

                var uri = new Uri(cosmosOptions.Endpoint);
                if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
                {
                    cosmosBuilder.HttpClientFactory(() => new HttpClient(new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    }));
                }
            }
            else
            {
                cosmosBuilder.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Direct);
            }
        }

        if (cosmosOptions.UseManagedIdentity)
        {
            options.UseCosmos(
                cosmosOptions.Endpoint,
                new DefaultAzureCredential(),
                cosmosOptions.DatabaseName,
                ConfigureCosmos);
        }
        else
        {
            options.UseCosmos(
                cosmosOptions.Endpoint,
                cosmosOptions.Key,
                cosmosOptions.DatabaseName,
                ConfigureCosmos);
        }
    });
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
}


var adAudience = configuration["AzureAd:Audience"] ?? "d8a3a596-6fcc-4047-ac76-d990b30f1bb0";
var adAuthority = configuration["AzureAd:Authority"] ?? "https://login.microsoftonline.com/d810b06c-d004-4d52-b0aa-4f3581ee7020/v2.0";
var tenantId = "d810b06c-d004-4d52-b0aa-4f3581ee7020";

var validIssuers = new[]
{
    $"https://sts.windows.net/{tenantId}/",
    $"https://login.microsoftonline.com/{tenantId}/v2.0"
};

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Authority drives automatic OIDC discovery (signing keys, issuer).
    // Do NOT also set options.Audience — use ValidAudiences only to avoid
    // double-validation where the audience string added by options.Audience
    // conflicts with the list in ValidAudiences.
    options.Authority = adAuthority;
    options.RequireHttpsMetadata = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuers = validIssuers,

        ValidateAudience = true,
        // Accept client-id GUID, api://client-id URI, or legacy 00000002 resource
        ValidAudiences = new[]
        {
            adAudience,
            $"api://{adAudience}",
            "00000002-0000-0000-c000-000000000000"
        },

        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5),

        // v2 tokens use preferred_username; v1 tokens use unique_name
        NameClaimType = "preferred_username",
        RoleClaimType = "roles",

        ValidateIssuerSigningKey = true,
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var msg = context.Exception.Message;
            Console.WriteLine($"[JWT] Auth failed ({context.Exception.GetType().Name}): {msg}");
            context.Response.Headers["X-Auth-Error"] = context.Exception.GetType().Name;
            context.Response.Headers["X-Auth-Error-Detail"] = msg.Length > 200 ? msg[..200] : msg;
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"[JWT] Challenge: error={context.Error}, desc={context.ErrorDescription}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            // Resolve username from claims in priority order (v2 → v1 → fallbacks)
            var principal = context.Principal;
            var username = principal?.FindFirst("preferred_username")?.Value
                ?? principal?.FindFirst("unique_name")?.Value
                ?? principal?.FindFirst("upn")?.Value
                ?? principal?.FindFirst("email")?.Value
                ?? principal?.Identity?.Name;

            Console.WriteLine($"[JWT] Token valid. User={username}, Iss={principal?.FindFirst("iss")?.Value}, Aud={principal?.FindFirst("aud")?.Value}");
            context.HttpContext.Items["AuthProvider"] = "AzureAd";
            context.HttpContext.Items["AuthUser"] = username;
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            // Allow token from query string for SignalR / streaming (optional)
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});
if (useCosmosIdentity)
{
    builder.Services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 3;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredUniqueChars = 0;
            options.Password.RequireUppercase = false;
        })
        .AddRoles<IdentityRole>()
        .AddSignInManager()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddUserStore<CosmosUserStore>()
        .AddDefaultTokenProviders();
}
else
{
    builder.Services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 3;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredUniqueChars = 0;
            options.Password.RequireUppercase = false;
        })
        .AddRoles<IdentityRole>()
        .AddSignInManager()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();
}

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/health/cosmos", async (
    IServiceProvider serviceProvider,
    IOptions<CosmosDbOptions> cosmosOptions,
    IWebHostEnvironment env) =>
{
    var options = cosmosOptions.Value;
    double totalRequestCharge = 0;
    try
    {
        var cosmosClient = serviceProvider.GetRequiredService<CosmosClient>();
        var db = cosmosClient.GetDatabase(options.DatabaseName);
        var dbResponse = await db.ReadAsync();
        totalRequestCharge += dbResponse.RequestCharge;

        var existingContainers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var iterator = db.GetContainerQueryIterator<ContainerProperties>();
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync();
            totalRequestCharge += page.RequestCharge;
            foreach (var container in page)
            {
                existingContainers.Add(container.Id);
            }
        }

        var missingContainers = CosmosContainers.PartitionKeys.Keys
            .Where(name => !existingContainers.Contains(name))
            .ToList();

        bool sysCountersFound = existingContainers.Contains(CosmosContainers.sys_Counters);

        if (missingContainers.Count > 0)
        {
            if (env.IsDevelopment())
            {
                return Results.Json(new
                {
                    status = "Unhealthy",
                    reason = $"Missing {missingContainers.Count} containers in database '{options.DatabaseName}'.",
                    database = options.DatabaseName,
                    containerCount = existingContainers.Count,
                    missingContainers,
                    sysCountersFound,
                    requestCharge = totalRequestCharge
                }, statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            return Results.Json(new { status = "Unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Ok(new
        {
            status = "Healthy",
            database = options.DatabaseName,
            containerCount = existingContainers.Count,
            missingContainers,
            sysCountersFound,
            requestCharge = totalRequestCharge
        });
    }
    catch (Exception ex)
    {
        if (env.IsDevelopment())
        {
            return Results.Json(new
            {
                status = "Unhealthy",
                error = ex.Message,
                database = options.DatabaseName,
                requestCharge = totalRequestCharge
            }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Json(new { status = "Unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

if (app.Environment.IsDevelopment())
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
                    updateErrors = updateResult.Succeeded ? null : updateResult.Errors.Select(e => e.Description).ToArray()
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

app.MapFallbackToFile("/index.html");

app.Run();
