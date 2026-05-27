using Microsoft.AspNetCore.Components.WebView.Maui;

namespace FuelMeter
{
    public partial class MainPage : ContentPage
    {
        private bool _splashAnimated;

        public MainPage()
        {
            InitializeComponent();

            // Register the MAUI-local Routes component as the Blazor root.
            // Using typeof() here (not XAML) ensures the Razor assembly is
            // loaded before BlazorWebView tries to resolve the component type.
            blazorWebView.RootComponents.Add(new RootComponent
            {
                Selector      = "#app",
                ComponentType = typeof(FuelMeter.Components.Components.Routes)
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (_splashAnimated) return;
            _splashAnimated = true;

            _ = AnimateSplashAsync();
        }

        private async Task AnimateSplashAsync()
        {
            try
            {
                await LoadingBar.ProgressTo(1.0, 1800, Easing.CubicInOut);
            }
            catch
            {
                // animation may be aborted if the page is disposed early
            }

            // Fade out the splash overlay rather than swapping the Window.Page.
            try
            {
                await SplashOverlay.FadeTo(0, 250, Easing.CubicIn);
            }
            catch
            {
                // ignore fade cancellation
            }

            SplashOverlay.IsVisible = false;
        }
    }
}
