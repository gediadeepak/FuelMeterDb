using FuelMeter.Components;
using FuelMeter.Components.Components;
using FuelMeter.Core.Services;
using FuelMeter.Core.Services.Interfaces;
using FuelMeter.Web.Components;
using FuelMeter.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Blazor Server ──────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── API base URL ───────────────────────────────────────────────
var apiBase = builder.Configuration["ApiSettings:BaseUrl"]
    ?? throw new InvalidOperationException("ApiSettings:BaseUrl is missing.");

// ── Named HttpClient — bypass SSL for self-signed/invalid cert on deployed API ─
builder.Services.AddHttpClient("FuelMeterApi", client =>
{
    client.BaseAddress = new Uri(apiBase);
    client.DefaultRequestHeaders.Add("User-Agent", "FuelMeterWeb/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// ── ApiClientService is Scoped: same instance as AuthStateService per circuit ─
builder.Services.AddScoped<IApiClientService, ApiClientService>();

// ── HTTP service implementations ──────────────────────────────────────────────
builder.Services.AddScoped<IAuthService,                 HttpAuthService>();
builder.Services.AddScoped<IMeterReadingService,         HttpMeterReadingService>();
builder.Services.AddScoped<ISettingsService,             HttpSettingsService>();
builder.Services.AddScoped<INotificationSettingsService, HttpNotificationSettingsService>();
builder.Services.AddScoped<IBudgetService,               HttpBudgetService>();
builder.Services.AddScoped<IBillEstimationService,       HttpBillEstimationService>();
builder.Services.AddScoped<IExportImportService,         HttpExportImportService>();

// ── Auth state (per Blazor Server circuit) ────────────────────────────────────
builder.Services.AddScoped<AuthStateService>();

// ── Web has no OS alarm API — use a no-op scheduler ────────────
builder.Services.AddScoped<INotificationScheduler, NullNotificationScheduler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
   .AddAdditionalAssemblies(typeof(Routes).Assembly)
   .AddInteractiveServerRenderMode();

app.Run();
