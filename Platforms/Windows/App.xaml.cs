using FuelMeter.Platforms.Windows;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace FuelMeter.WinUI
{
    public partial class App : MauiWinUIApplication
    {
        public App()
        {
            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);
            WindowsNotificationHelper.RegisterAumid();
            ApplyFuelMeterTitleBar();
        }

        private static void ApplyFuelMeterTitleBar()
        {
            if (Microsoft.Maui.MauiWinUIApplication.Current?.Application?.Windows is { } windows
                && windows.Count > 0
                && windows[0].Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
            {
                var appWindow = nativeWindow.AppWindow;
                if (appWindow?.TitleBar is { } titleBar)
                {
                    titleBar.ExtendsContentIntoTitleBar = false;

                    // FuelMeter brand colors — deep navy blue title bar
                    Windows.UI.Color brandBlue     = Windows.UI.Color.FromArgb(255,  13,  71, 161); // #0D47A1
                    Windows.UI.Color brandHover    = Windows.UI.Color.FromArgb(255,  25, 118, 210); // #1976D2
                    Windows.UI.Color brandPressed  = Windows.UI.Color.FromArgb(255,  10,  47, 110); // #0A2F6E
                    Windows.UI.Color white         = Windows.UI.Color.FromArgb(255, 255, 255, 255);
                    Windows.UI.Color whiteDim      = Windows.UI.Color.FromArgb(180, 255, 255, 255);

                    titleBar.BackgroundColor                = brandBlue;
                    titleBar.ForegroundColor                = white;
                    titleBar.InactiveBackgroundColor        = brandBlue;
                    titleBar.InactiveForegroundColor        = whiteDim;
                    titleBar.ButtonBackgroundColor          = brandBlue;
                    titleBar.ButtonForegroundColor          = white;
                    titleBar.ButtonHoverBackgroundColor     = brandHover;
                    titleBar.ButtonHoverForegroundColor     = white;
                    titleBar.ButtonPressedBackgroundColor   = brandPressed;
                    titleBar.ButtonPressedForegroundColor   = white;
                    titleBar.ButtonInactiveBackgroundColor  = brandBlue;
                    titleBar.ButtonInactiveForegroundColor  = whiteDim;
                }
            }
        }
    }
}
