using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using FuelMeter.Core.Services;
using FuelMeter.Core.Services.Interfaces;
using FuelMeter.Services;

namespace FuelMeter
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(null);

            var services = IPlatformApplication.Current?.Services;
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

        protected override void OnDestroy()
        {
            var services = IPlatformApplication.Current?.Services;
            if (services?.GetService<IBudgetPushSettings>() is AndroidBudgetPushSettings push)
                services.GetRequiredService<AuthStateService>().OnAuthStateChanged -= push.Refresh;

            base.OnDestroy();
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
