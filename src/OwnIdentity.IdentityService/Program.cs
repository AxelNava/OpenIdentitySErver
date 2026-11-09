using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OwnIdentity.Data.Core;
using OwnIdentity.Data.Core.Entities;
using OwnIdentity.IdentityService.Data;
using OwnIdentity.IdentityService.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOwnIdentityDataStores(builder.Configuration, builder.Environment);

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 12;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddEntityFrameworkStores<ControlDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.Cookie.Name = "OwnIdentity.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<ProtocolDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
            .SetTokenEndpointUris("/connect/token")
            .SetEndSessionEndpointUris("/connect/logout");

        options.AllowAuthorizationCodeFlow();
        options.RequireProofKeyForCodeExchange();
        options.RegisterScopes(
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.Email,
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.OpenId,
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.Profile,
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.Roles,
            IdentityConfiguration.AdminScope);

        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate()
                .AddDevelopmentSigningCertificate();
        }

        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddIdentityAdministrationAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminUi", policy => policy
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddProblemDetails();
// Readiness reflects migration-complete state (S3/S4); liveness is unchanged.
builder.Services.AddHealthChecks()
    .AddCheck<MigrationReadinessCheck>("migrations");
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddScoped<DatabaseSeeder>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/api/admin/migrations/identityserver4/preflight"))
    {
        context.Response.Headers.CacheControl = "no-store";
    }

    await next();
});
app.UseCors("AdminUi");
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/ready");
app.MapControllers();
app.MapRazorPages();

if (!EF.IsDesignTime)
{
    var storeOptions = app.Services.GetRequiredService<OwnIdentityDataStoreOptions>();
    await using (var scope = app.Services.CreateAsyncScope())
    {
        if (app.Environment.IsDevelopment())
        {
            // Development convenience only: inline migrate/ensure per store (S4 keeps
            // startup replicas in non-Development from ever racing migrations).
            await DevelopmentDatabaseInitializer.InitializeAsync(app.Services, storeOptions);
        }

        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
    }
}

await app.RunAsync();

public partial class Program;
