using Android.App;
using Android.Content.PM;
using Android.Content;
using Android.OS;
using Android.Views;
using FuelMeter.Core.Services;
using FuelMeter.Core.Services.Interfaces;
using FuelMeter.Services;

namespace FuelMeter
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter([Android.Content.Intent.ActionView], Categories = [Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable],
        DataScheme = "https", DataHost = PasswordResetLinks.Host, DataPath = PasswordResetLinks.ResetPath, AutoVerify = true)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            // Capture before MAUI creates the Window so Blazor can start on the reset route.
            var resetQueued = HandleResetLink(Intent);
            base.OnCreate(null);
            if (!resetQueued) HandleResetLink(Intent);

            var services = IPlatformApplication.Current?.Services;
            if (services?.GetService<ISubscriptionPurchases>() is FuelMeter.Platforms.Android.GooglePlayPurchases billing)
                services.GetRequiredService<AuthStateService>().OnAuthStateChanged += billing.Refresh;
            if (services?.GetService<IBudgetPushSettings>() is AndroidBudgetPushSettings push)
            {
                var auth = services.GetRequiredService<AuthStateService>();
                auth.OnAuthStateChanged += push.Refresh;
                push.Refresh();
            }

            if (Window is not null)
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
                {
                    // Hide status bar — immersive full-screen
                    Window.InsetsController?.Hide(Android.Views.WindowInsets.Type.StatusBars());
                }
                else
                {
#pragma warning disable CA1422
                    Window.DecorView.SystemUiVisibility =
                        (StatusBarVisibility)(
                            (int)SystemUiFlags.Fullscreen |
                            (int)SystemUiFlags.ImmersiveSticky |
                            (int)SystemUiFlags.LayoutFullscreen);
#pragma warning restore CA1422
                }

                // Keep navigation bar branded
                Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#0D47A1"));
            }
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            if (intent is not null) Intent = intent;
            HandleResetLink(intent);
        }

        private static bool HandleResetLink(Intent? intent)
        {
            if (intent?.Action == Android.Content.Intent.ActionView && Uri.TryCreate(intent.DataString, UriKind.Absolute, out var uri))
                return IPlatformApplication.Current?.Services.GetService<PasswordResetLinks>()?.Receive(uri) == true;
            return false;
        }

        protected override void OnDestroy()
        {
            var services = IPlatformApplication.Current?.Services;
            if (services?.GetService<ISubscriptionPurchases>() is FuelMeter.Platforms.Android.GooglePlayPurchases billing)
                services.GetRequiredService<AuthStateService>().OnAuthStateChanged -= billing.Refresh;
            if (services?.GetService<IBudgetPushSettings>() is AndroidBudgetPushSettings push)
                services.GetRequiredService<AuthStateService>().OnAuthStateChanged -= push.Refresh;

            base.OnDestroy();
        }

        protected override void OnResume()
        {
            base.OnResume();
            var services = IPlatformApplication.Current?.Services;
            if (services?.GetService<ISubscriptionPurchases>() is FuelMeter.Platforms.Android.GooglePlayPurchases billing)
                billing.Refresh();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            // Intentionally suppress saving instance state.
            // BlazorWebView fragment cannot be restored from a saved Bundle —
            // calling base here causes "No view found for id jumpToStart" on resume.
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            // Intentionally suppress restoring instance state.
            // Prevents Android from re-attaching the BlazorWebView fragment
            // to a view that hasn't been inflated yet.
        }
    }
}
