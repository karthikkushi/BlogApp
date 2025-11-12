using BlogAppFrontend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using MudBlazor.Services;
using System.Text.Json;

namespace BlogAppFrontend;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        // Configure HttpClient with retry policy
        builder.Services.AddScoped(sp => new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7248/api/"),
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Configure JSON serialization options globally
        builder.Services.AddScoped(sp => new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        builder.Services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
            config.SnackbarConfiguration.PreventDuplicates = true;
            config.SnackbarConfiguration.NewestOnTop = true;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 3000;
            config.SnackbarConfiguration.HideTransitionDuration = 500;
            config.SnackbarConfiguration.ShowTransitionDuration = 500;
        });

        builder.Services.AddScoped<BlogService>();
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<SupabaseImageUploader>();
        builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
        builder.Services.AddAuthorizationCore();
        builder.Services.AddScoped<IAuthorizationPolicyProvider, MinimalAuthorizationPolicyProvider>();
        builder.Services.AddScoped<UnsplashService>();

        var host = builder.Build();
        var jsRuntime = host.Services.GetRequiredService<IJSRuntime>();
        
        // Configure error handling for WebSocket connections
        try
        {
            await jsRuntime.InvokeVoidAsync("console.log", "Application started successfully");
        }
        catch (Exception ex)
        {
            // Log the error but continue with application startup
            Console.WriteLine($"WebSocket connection error: {ex.Message}");
        }

        await host.RunAsync();
    }
}