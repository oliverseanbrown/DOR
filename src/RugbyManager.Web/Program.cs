using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RugbyManager.Web;
using RugbyManager.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<PlaybackSettingsService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<SupabaseAuthService>();
builder.Services.AddScoped<CloudSaveService>();

await builder.Build().RunAsync();
