using TodoLearn.Views;

namespace TodoLearn
{
    using TodoLearn.Resources.Strings;

    public partial class App : Application
    {
        private readonly InactivityService _inactivityService;
        private readonly IServiceProvider _services;
        private readonly Views.LoginPage _loginPage;

        public App(Views.LoginPage loginPage, InactivityService inactivityService, IServiceProvider services)
        {
            InitializeComponent();
            _loginPage = loginPage;
            _inactivityService = inactivityService;
            _services = services;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            _inactivityService.OnTimeout -= OnUserInactiveTimeout;
            _inactivityService.OnTimeout += OnUserInactiveTimeout;
            _inactivityService.Start();
            return new Window(_loginPage);
        }

        private void OnUserInactiveTimeout()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var window = Application.Current?.Windows.FirstOrDefault();
                if (window?.Page is null)
                    return;

                await window.Page.DisplayAlertAsync(
                    AppResources.SessionExpiredTitle,
                    AppResources.SessionExpiredMessage,
                    AppResources.OkButton);
                _inactivityService.Stop();
                window.Page = _services.GetService(typeof(LoginPage)) as LoginPage
                    ?? throw new InvalidOperationException(AppResources.LoginPageRequired);
            });
        }
    }
}
