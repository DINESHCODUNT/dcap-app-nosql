using Azure.Identity;
using DigitalCap.Infrastructure.DbContext;
using DigitalCap.Persistence.Extensions;
using DigitalCap.WebApi.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));


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

app.MapFallbackToFile("/index.html");

app.Run();
