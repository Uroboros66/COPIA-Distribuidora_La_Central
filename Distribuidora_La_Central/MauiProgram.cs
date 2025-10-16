using Microsoft.Extensions.Logging;
using Distribuidora_La_Central.Shared.Services;
using Distribuidora_La_Central.Services;
using Distribuidora_La_Central.Shared.Helpers;
using Microsoft.AspNetCore.Components.WebView.Maui;

namespace Distribuidora_La_Central;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Configuración esencial de Blazor WebView
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        // Herramientas de desarrollo solo en modo DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Servicio para detectar el tipo de dispositivo
        builder.Services.AddSingleton<IFormFactor, FormFactor>();

        // Configuración de HttpClient
#if ANDROID
        builder.Services.AddScoped(sp => new HttpClient
        {
            BaseAddress = new Uri("http://10.0.2.2:5282")
        });
#else
        builder.Services.AddScoped(sp => new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5282")
        });
#endif

        // Registro de servicios personalizados
        builder.Services.AddScoped<HttpService>();
        builder.Services.AddSingleton<AppState>();

        return builder.Build();
    }
}