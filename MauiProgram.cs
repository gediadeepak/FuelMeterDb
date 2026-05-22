using FuelMeter.Core.Services;
using FuelMeter.Core.Services.Interfaces;
using FuelMeter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FuelMeter
{
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

            // ── Load appsettings.json ──────────────────────────────
            // Copy the asset streams into MemoryStreams so the underlying
            // Android AssetInputStream peers can be disposed immediately.
            // Holding the original streams past CreateMauiApp() causes
            // ObjectDisposedException on Android.AssetManager+AssetInputStream.
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonStream(LoadPackagedAsset("appsettings.json")!);

#if DEBUG
            var devConfig = LoadPackagedAsset("appsettings.Development.json");
            if (devConfig is not null)
                configBuilder.AddJsonStream(devConfig);
#endif

            var config = configBuilder.Build();
            builder.Configuration.AddConfiguration(config);

            // ── Register Blazor WebView ────────────────────────────
            builder.Services.AddMauiBlazorWebView();

            // ── API base URL ───────────────────────────────────────
            var apiBase = builder.Configuration["ApiSettings:BaseUrl"]
                ?? throw new InvalidOperationException("ApiSettings:BaseUrl is missing from appsettings.json");

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[FuelMeter] ApiSettings:BaseUrl = {apiBase}");
#endif

            // ── Typed HttpClient → IApiClientService (bypass SSL for self-signed cert) ──
            builder.Services.AddSingleton<IApiClientService>(sp =>
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                var http = new HttpClient(handler) { BaseAddress = new Uri(apiBase) };
                return new ApiClientService(http);
            });

            // ── HTTP service implementations ───────────────────────
            builder.Services.AddSingleton<IAuthService,                 HttpAuthService>();
            builder.Services.AddSingleton<IMeterReadingService,         HttpMeterReadingService>();
            builder.Services.AddSingleton<ISettingsService,             HttpSettingsService>();
            builder.Services.AddSingleton<INotificationSettingsService, HttpNotificationSettingsService>();

            // ── Auth state + attach the API client ─────────────────
            builder.Services.AddSingleton<AuthStateService>(sp =>
            {
                var state = new AuthStateService();
                var api   = sp.GetRequiredService<IApiClientService>();
                state.AttachApiClient(api);
                return state;
            });

            // ── Platform notification scheduler ───────────────────
            builder.Services.AddSingleton<INotificationScheduler, NotificationSchedulerService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        // Reads a packaged MauiAsset fully into a MemoryStream and disposes the
        // platform stream right away. Returns null if the asset doesn't exist.
        private static MemoryStream? LoadPackagedAsset(string fileName)
        {
            try
            {
                using var src = FileSystem.OpenAppPackageFileAsync(fileName)
                    .GetAwaiter().GetResult();
                var ms = new MemoryStream();
                src.CopyTo(ms);
                ms.Position = 0;
                return ms;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
    }
}
