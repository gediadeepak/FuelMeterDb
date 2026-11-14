using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace FuelMeter.Api.Services;

public sealed class FirebasePushService : IDisposable
{
    private readonly FirebaseApp _app;
    private readonly FirebaseMessaging _messaging;

    public FirebasePushService(IConfiguration configuration)
    {
        var projectId = configuration["Firebase:ProjectId"];
        if (string.IsNullOrWhiteSpace(projectId))
            throw new InvalidOperationException("Firebase:ProjectId is missing.");

        _app = FirebaseApp.Create(new AppOptions
        {
            ProjectId = projectId,
            Credential = GoogleCredential.GetApplicationDefault()
        }, "FuelMeter");
        _messaging = FirebaseMessaging.GetMessaging(_app);
    }

    public Task<string> SendBudgetAlertAsync(
        string installationId, int userId, int budgetId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installationId);

        var message = new Message
        {
            Fid = installationId,
            Data = new Dictionary<string, string>
            {
                ["type"] = "budget",
                ["userId"] = userId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["budgetId"] = budgetId.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            Android = new AndroidConfig { Priority = Priority.High, TimeToLive = TimeSpan.FromHours(1) }
        };

        return _messaging.SendAsync(message, cancellationToken);
    }

    public void Dispose() => _app.Delete();
}
