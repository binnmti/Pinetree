using Azure.Identity;
using Google.Apis.Auth.AspNetCore3;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pinetree.Components;
using Pinetree.Components.Account;
using Pinetree.Components.Account.Services;
using Pinetree.Data;
using Pinetree.Services;
using Pinetree.Shared;
using System.Security.Claims;
using System.Text.Json;

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
builder.Services.AddScoped<BlobStorageService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthorization();
builder.Services.AddHttpClient();
builder.Services.AddControllers();

builder.Services
    .AddAuthentication(o =>
    {
        o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddGoogleOpenIdConnect(options =>
    {
        options.ClientId = builder.Configuration.GetConnectionString("GoogleClientId");
        options.ClientSecret = builder.Configuration.GetConnectionString("GoogleClientSecret");
        options.CallbackPath = new PathString("/signin-google");
    })
    .AddMicrosoftAccount(microsoftOptions =>
    {
        var tenantId = builder.Configuration.GetConnectionString("MicrosoftTenantId") ?? "";
        microsoftOptions.ClientId = builder.Configuration.GetConnectionString("MicrosoftClientId") ?? "";
        microsoftOptions.ClientSecret = builder.Configuration.GetConnectionString("MicrosoftClientSecret") ?? "";
        microsoftOptions.CallbackPath = new PathString("/signin-microsoft");
        microsoftOptions.AuthorizationEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize";
        microsoftOptions.TokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
        microsoftOptions.Scope.Clear();
        microsoftOptions.Scope.Add("openid");
        microsoftOptions.Scope.Add("profile");
        microsoftOptions.Scope.Add("email");
        microsoftOptions.Scope.Add("https://graph.microsoft.com/User.Read");
        microsoftOptions.SaveTokens = true;
        microsoftOptions.Events = new OAuthEvents
        {
            OnTicketReceived = context =>
            {
                context.ReturnUri = "/";
                return Task.CompletedTask;
            },
            OnRemoteFailure = context =>
            {
                context.Response.Redirect("/");
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    })
    .AddFacebook(facebookOptions =>
    {
        facebookOptions.AppId = builder.Configuration.GetConnectionString("FacebookClientId") ?? "";
        facebookOptions.AppSecret = builder.Configuration.GetConnectionString("FacebookClientSecret") ?? "";
        facebookOptions.SaveTokens = true;
        facebookOptions.CallbackPath = new PathString("/signin-facebook");
    })
    .AddOAuth("GitHub", options =>
     {
         options.ClientId = builder.Configuration.GetConnectionString("GitHubClientId") ?? "";
         options.ClientSecret = builder.Configuration.GetConnectionString("GitHubClientSecret") ?? "";
         options.CallbackPath = new PathString("/signin-github");

         options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
         options.TokenEndpoint = "https://github.com/login/oauth/access_token";
         options.UserInformationEndpoint = "https://api.github.com/user";

         options.SaveTokens = true;

         options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
         options.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
         options.ClaimActions.MapJsonKey("urn:github:name", "name");
         options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
         options.ClaimActions.MapJsonKey("urn:github:url", "html_url");

         options.Events = new OAuthEvents
         {
             OnCreatingTicket = async context =>
             {
                 var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                 request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                 request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);

                 var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                 response.EnsureSuccessStatusCode();

                 var user = await response.Content.ReadFromJsonAsync<JsonElement>();

                 context.RunClaimActions(user);
             }
         };
     });

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
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

builder.Services.AddTransient<IEmailSender<ApplicationUser>, EmailSender>();

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
