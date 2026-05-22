namespace FuelMeter
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Show the animated SplashPage on all platforms before loading MainPage.
            // Android's native MauiSplashTheme covers the very first frame; once MAUI
            // is initialised the SplashPage takes over with the progress-bar animation.
            return new Window(new SplashPage()) { Title = "FuelMeter" };
        }
    }
}
