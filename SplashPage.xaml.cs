namespace FuelMeter
{
    public partial class SplashPage : ContentPage
    {
        private bool _navigated;

        public SplashPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = AnimateAndNavigateAsync();
        }

        private async Task AnimateAndNavigateAsync()
        {
            try
            {
                // Animate the progress bar over 1.8 seconds
                await LoadingBar.ProgressTo(1.0, 1800, Easing.CubicInOut);
            }
            catch
            {
                // ignore animation cancellation if the page is torn down early
            }

            if (_navigated) return;
            _navigated = true;

            // Stop any pending animations before the page is torn down
            this.AbortAnimation(nameof(LoadingBar.Progress));

            // Defer the page swap to the next main-thread tick so this OnAppearing
            // stack frame fully unwinds before SplashPage is disposed. This avoids
            // ObjectDisposedException on the underlying Android Context/handlers.
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Application.Current?.Windows.Count > 0)
                {
                    Application.Current.Windows[0].Page = new MainPage();
                }
            });
        }
    }
}
