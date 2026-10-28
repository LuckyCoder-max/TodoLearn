using TodoLearn.Views;

namespace TodoLearn
{
    using TodoLearn.Resources.Strings;
    using TodoLearn.Services;

    public partial class App : Application
    {
        private readonly InactivityService _inactivityService;
        private readonly AuthSessionService _authSessionService;
        private readonly TaskReminderService _taskReminderService;
        private readonly IServiceProvider _services;
        private readonly Views.LoginPage _loginPage;

        public App(Views.LoginPage loginPage, InactivityService inactivityService, AuthSessionService authSessionService, TaskReminderService taskReminderService, IServiceProvider services)
        {
            InitializeComponent();
            _loginPage = loginPage;
            _inactivityService = inactivityService;
            _authSessionService = authSessionService;
            _taskReminderService = taskReminderService;
            _services = services;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            _inactivityService.OnTimeout -= OnUserInactiveTimeout;
            _inactivityService.OnTimeout += OnUserInactiveTimeout;
            _inactivityService.Start();
            var window = new Window(_loginPage);
            window.Activated += OnWindowActivated;
            window.Deactivated += OnWindowDeactivated;
            return window;
        }

        private async void OnWindowActivated(object? sender, EventArgs e)
        {
            _taskReminderService.SetWindowActive(true);
            if (_authSessionService.CurrentAccountId is not null)
                await _taskReminderService.ShowPendingInAppRemindersAsync();
        }

        private void OnWindowDeactivated(object? sender, EventArgs e)
        {
            _taskReminderService.SetWindowActive(false);
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
                _taskReminderService.Stop();
                _authSessionService.ClearRememberedAccount();
                window.Page = _services.GetService(typeof(LoginPage)) as LoginPage
                    ?? throw new InvalidOperationException(AppResources.LoginPageRequired);
            });
        }
    }
}
