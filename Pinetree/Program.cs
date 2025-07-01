using Azure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pinetree.Client.Services;
using Pinetree.Components;
using Pinetree.Components.Account;
using Pinetree.Data;
using Pinetree.Middleware;
using Pinetree.Services;
using Pinetree.Shared;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddServerSideBlazor().AddHubOptions(options =>
{
    options.MaximumReceiveMessageSize = 102400;
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<PaymentService>();
// BlobStorageService with optional EncryptionService - allows profile management to work without encryption
builder.Services.AddScoped<BlobStorageService>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var dbContext = provider.GetRequiredService<ApplicationDbContext>();
    var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
    var encryptionService = provider.GetService<IEncryptionService>(); // Optional service
    return new BlobStorageService(configuration, dbContext, userManager, encryptionService);
});
builder.Services.AddSingleton<AIEmojiWithRateLimitService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<VersionService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ISensitiveDataDetectorService, SensitiveDataDetectorService>();
builder.Services.AddScoped<ISecureLoggerService, SecureLoggerService>();
builder.Services.AddScoped<FontSettingsService>();

builder.Services.AddHostedService<AuditCleanupService>();
builder.Services.AddHostedService<TrashCleanupService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthorization();
builder.Services.AddHttpClient();
builder.Services.AddControllers();

builder.Services
    .AddAuthentication() // Identity will configure the default scheme automatically
    .AddGoogleOpenIdConnect(options =>
    {
        options.ClientId = builder.Configuration.GetConnectionString("GoogleClientId");
        options.ClientSecret = builder.Configuration.GetConnectionString("GoogleClientSecret");
        options.CallbackPath = new PathString("/signin-google");
    })
    .AddFacebook(facebookOptions =>
    {
        facebookOptions.AppId = builder.Configuration.GetConnectionString("FacebookClientId") ?? "";
        facebookOptions.AppSecret = builder.Configuration.GetConnectionString("FacebookClientSecret") ?? "";
        facebookOptions.SaveTokens = true;
        facebookOptions.CallbackPath = new PathString("/signin-facebook");
        facebookOptions.Scope.Add("public_profile");
        facebookOptions.Scope.Add("email");
    });
    //.AddMicrosoftAccount(microsoftOptions =>
    //{
    //    var tenantId = builder.Configuration.GetConnectionString("MicrosoftTenantId") ?? "";
    //    microsoftOptions.ClientId = builder.Configuration.GetConnectionString("MicrosoftClientId") ?? "";
    //    microsoftOptions.ClientSecret = builder.Configuration.GetConnectionString("MicrosoftClientSecret") ?? "";
    //    microsoftOptions.CallbackPath = new PathString("/signin-microsoft");
    //    microsoftOptions.AuthorizationEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize";
    //    microsoftOptions.TokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
    //    microsoftOptions.Scope.Add("openid");
    //    microsoftOptions.Scope.Add("profile");
    //    microsoftOptions.Scope.Add("email");
    //})
    //.AddOAuth("GitHub", options =>
    // {
    //     options.ClientId = builder.Configuration.GetConnectionString("GitHubClientId") ?? "";
    //     options.ClientSecret = builder.Configuration.GetConnectionString("GitHubClientSecret") ?? "";
    //     options.CallbackPath = new PathString("/signin-github");

    //     options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
    //     options.TokenEndpoint = "https://github.com/login/oauth/access_token";
    //     options.UserInformationEndpoint = "https://api.github.com/user";

    //     options.SaveTokens = true;

    //     options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
    //     options.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
    //     options.ClaimActions.MapJsonKey("urn:github:name", "name");
    //     options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
    //     options.ClaimActions.MapJsonKey("urn:github:url", "html_url");

    //     options.Events = new OAuthEvents
    //     {
    //         OnCreatingTicket = async context =>
    //         {
    //             var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
    //             request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    //             request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);

    //             var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
    //             response.EnsureSuccessStatusCode();

    //             var user = await response.Content.ReadFromJsonAsync<JsonElement>();

    //             context.RunClaimActions(user);
    //         }
    //     };
    // });

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = !(builder.Environment.IsDevelopment() || builder.Environment.IsStaging());
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Application Cookie for enhanced persistence
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "PinetreeAppAuth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    
    // Enhanced settings for persistence across deployments
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Cookie.MaxAge = TimeSpan.FromDays(30);
    options.Cookie.IsEssential = true;
    
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    
    // Event for automatic user validation
    options.Events.OnValidatePrincipal = async context =>
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogInformation("User {UserId} not found, rejecting principal", userId);
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
            }
            else
            {
                logger.LogDebug("User {UserId} validated successfully", userId);
            }
        }
    };
});

builder.Services.AddTransient<IEmailSender<ApplicationUser>, EmailSender>();

// Configure Data Protection API for secure encryption key management
builder.Services.AddDataProtection();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.Configure<Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration>(config =>
{
    config.SetAzureTokenCredential(new DefaultAzureCredential());
});

if (!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.Configure<Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration>(config =>
    {
        config.SetAzureTokenCredential(new DefaultAzureCredential());
    });

    builder.Services.AddApplicationInsightsTelemetry(new Microsoft.ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions
    {
        ConnectionString = builder.Configuration.GetConnectionString("APPLICATIONINSIGHTS_CONNECTION_STRING")
    });
}
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var roleName in Roles.RoleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    await SeedData.Initialize(app.Services);
}
app.UseHttpsRedirection();

// Add Security Headers Middleware
app.Use(async (context, next) =>
{
    // Security headers for protection against various attacks
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
      // Content Security Policy - adjust based on environment
    var cspConnectSrc = app.Environment.IsDevelopment() 
        ? "connect-src 'self' wss: https: ws: http://localhost:* https://localhost:* blob:; "
        : "connect-src 'self' wss: https: ws: blob:; ";
    
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://www.googletagmanager.com https://connect.facebook.net; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "img-src 'self' data: https: blob:; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        cspConnectSrc +
        "frame-src 'self' https://www.youtube.com https://www.google.com https://www.facebook.com; " +
        "media-src 'self' https: blob:; " +
        "object-src 'none'; " +
        "base-uri 'self';";
    
    // HSTS header (only for HTTPS)
    if (context.Request.IsHttps)
    {
        context.Response.Headers["Strict-Transport-Security"] = 
            "max-age=31536000; includeSubDomains; preload";
    }
    
    // Additional security headers
    context.Response.Headers["Permissions-Policy"] = 
        "camera=(), microphone=(), geolocation=(), payment=()";
    
    await next();
});

app.UseMiddleware<AuditLogMiddleware>();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Pinetree.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();
app.MapControllers();
app.Run();
