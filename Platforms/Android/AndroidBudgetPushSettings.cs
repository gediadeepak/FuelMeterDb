using Firebase.Messaging;
using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace FuelMeter.Services;

public sealed class AndroidBudgetPushSettings(
    AuthStateService auth, IApiClientService api, ILogger<AndroidBudgetPushSettings> logger) : IBudgetPushSettings
{
    private const string InstallationKey = "firebase_installation_id";
    private readonly SemaphoreSlim _sync = new(1);

    private static string PreferenceKey(int userId) => $"budget_push_enabled_{userId}";

    public bool IsEnabled => auth.CurrentUser is { } user && IsEnabledFor(user.Id);

    public static bool IsEnabledFor(int userId) => Preferences.Default.Get(PreferenceKey(userId), false);

    public async Task<bool> SetEnabledAsync(bool enabled)
    {
        if (auth.CurrentUser is not { } user)
            return false;

        if (enabled && OperatingSystem.IsAndroidVersionAtLeast(33) &&
            await Permissions.RequestAsync<Permissions.PostNotifications>() != PermissionStatus.Granted)
        {
            return false;
        }

        await _sync.WaitAsync();
        try
        {
            Preferences.Default.Set(PreferenceKey(user.Id), enabled);
            if (enabled)
            {
                FirebaseMessaging.Instance.Register();
            }
            else
            {
                await SyncAsync(Preferences.Default.Get(InstallationKey, ""), enabled: false);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not update budget push registration.");
            if (enabled)
                Preferences.Default.Set(PreferenceKey(user.Id), false);
            return false;
        }
        finally
        {
            _sync.Release();
        }
    }

    public void Refresh()
    {
        try
        {
            if (IsEnabled)
                FirebaseMessaging.Instance.Register();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not refresh Firebase registration.");
        }
    }

    public async Task OnRegisteredAsync(string installationId)
    {
        await _sync.WaitAsync();
        try
        {
            var previousId = Preferences.Default.Get(InstallationKey, "");
            if (previousId != installationId)
            {
                await SyncAsync(previousId, enabled: false);
                Preferences.Default.Set(InstallationKey, installationId);
            }
            await SyncAsync(installationId, IsEnabled);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not sync the Firebase installation.");
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task SyncAsync(string installationId, bool enabled)
    {
        if (auth.IsAuthenticated && !string.IsNullOrEmpty(installationId))
        {
            await api.PutAsync<PushRegistrationDto, bool>("api/push-installations", new()
            {
                InstallationId = installationId,
                Enabled = enabled
            });
        }
    }
}
