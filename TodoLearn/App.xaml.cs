using TodoLearn.Views;

namespace TodoLearn
{
    public partial class App : Application
    {
        private readonly InactivityService? _inactivityService;

        public App()
        {
            InitializeComponent();
            _inactivityService = IPlatformApplication.Current?.Services.GetService<InactivityService>();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var loginPage = new LoginPage();

            if (_inactivityService != null)
            {
                _inactivityService.OnTimeout += OnUserInactiveTimeout;
                _inactivityService.Start();
            }

            return new Window(loginPage);
        }

        private void OnUserInactiveTimeout()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Application.Current!.MainPage!.DisplayAlert("Session Expired", "You have been inactive for too long. Please log in again.", "OK");
                _inactivityService?.Stop();
                Application.Current.MainPage = new LoginPage();
            });
        }
    }
}