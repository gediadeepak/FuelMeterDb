using Microsoft.AspNetCore.Components.WebView.Maui;

namespace FuelMeter
{
    public partial class MainPage : ContentPage
    {
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
    }
}
