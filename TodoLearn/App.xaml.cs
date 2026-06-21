using TodoLearn.Views;

namespace TodoLearn
{
    public partial class App : Application
    {
        private readonly InactivityService _inactivityService;
        private readonly IServiceProvider _serviceProvider;

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _inactivityService = serviceProvider.GetRequiredService<InactivityService>();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var loginPage = _serviceProvider.GetRequiredService<LoginPage>();

            _inactivityService.OnTimeout += OnUserInactiveTimeout;

            return new Window(loginPage);
        }

        private void OnUserInactiveTimeout()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var window = Windows.FirstOrDefault();
                var currentPage = window?.Page;

                if (currentPage != null)
                {
                    await currentPage.DisplayAlertAsync("Session Expired", "You have been inactive for too long. Please log in again.", "OK");
                }

                _inactivityService.Stop();

                if (window == null)
                    return;

                var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
                window.Page = loginPage;
            });
        }
    }
}
