using Microsoft.Maui.Controls;
using TodoLearn.Services;

namespace TodoLearn
{
    public partial class AppShell : Shell
    {
        private readonly InactivityService? _inactivityService;
        private readonly AuthSessionService? _authSessionService;

        public AppShell()
        {
            InitializeComponent();

            _inactivityService = IPlatformApplication.Current?.Services.GetService<InactivityService>();
            _authSessionService = IPlatformApplication.Current?.Services.GetService<AuthSessionService>();

            CurrentUsernameLabel.Text = _authSessionService?.CurrentUsername ?? string.Empty;
            CurrentUsernameLabel.IsVisible = !string.IsNullOrWhiteSpace(CurrentUsernameLabel.Text);

            if (_inactivityService != null)
            {
                this.Navigated += (s, e) => OnUserActivity();
            }
        }

        private void OnUserActivity()
        {
            _inactivityService?.Reset();
        }

        private void OnLogoutClicked(object? sender, EventArgs e)
        {
            _inactivityService?.Stop();
            _authSessionService?.ClearRememberedAccount();

            var services = IPlatformApplication.Current?.Services;
            var loginPage = services?.GetService<TodoLearn.Views.LoginPage>();
            var window = Application.Current?.Windows.FirstOrDefault();
            if (loginPage is not null && window is not null)
                window.Page = loginPage;
        }
    }
}
