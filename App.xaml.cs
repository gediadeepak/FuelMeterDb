namespace FuelMeter
{
    public partial class App : Application
    {
        private readonly FuelMeter.Core.Services.PasswordResetLinks _resetLinks;

        public App(FuelMeter.Core.Services.PasswordResetLinks resetLinks)
        {
            _resetLinks = resetLinks;
            InitializeComponent();
        }

        protected override void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);
            _resetLinks.Receive(uri);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Use MainPage as the Window root from the start. The animated splash
            // is overlaid inside MainPage and fades out when ready. Swapping
            // Window.Page after the page has been attached caused Android to
            // crash with "No view found for id ... jumpToStart" inside the
            // AndroidX fragment manager.
            return new Window(new MainPage()) { Title = "FuelMeter" };
        }
    }
}
