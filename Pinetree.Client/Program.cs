using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Pinetree.Client.Services;
using Pinetree.Shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddScoped<FontSettingsService>();
builder.Services.AddScoped<LocalizationService>();

builder.Services.AddSingleton(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
