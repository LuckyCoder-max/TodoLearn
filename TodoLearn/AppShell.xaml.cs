using Microsoft.Maui.Controls;
using TodoLearn.Services;

namespace TodoLearn
{
    public partial class AppShell : Shell
    {
        private readonly InactivityService? _inactivityService;
        private readonly AuthSessionService? _authSessionService;
        private readonly TaskReminderService? _taskReminderService;

        public AppShell()
        {
            InitializeComponent();

            _inactivityService = IPlatformApplication.Current?.Services.GetService<InactivityService>();
            _authSessionService = IPlatformApplication.Current?.Services.GetService<AuthSessionService>();
            _taskReminderService = IPlatformApplication.Current?.Services.GetService<TaskReminderService>();
            _taskReminderService?.Start();
            CurrentUsernameLabel.Text = _authSessionService?.CurrentUsername ?? string.Empty;
            CurrentUsernameLabel.IsVisible = !string.IsNullOrWhiteSpace(CurrentUsernameLabel.Text);
            AddCategoryPages();

            if (_inactivityService != null)
            {
                this.Navigated += (s, e) => OnUserActivity();
            }
        }

        private void AddCategoryPages()
        {
            var names = Models.TaskCategories.Names;
            for (var index = 0; index < names.Count; index++)
            {
                Models.TaskCategory? category = index == 0 ? null : (Models.TaskCategory)(index - 1);
                var item = new FlyoutItem { Title = names[index], Route = $"category{index}" };
                item.Items.Add(new ShellContent
                {
                    Title = names[index],
                    ContentTemplate = new DataTemplate(() => new Views.CategoryPage(category))
                });
                Items.Add(item);
            }
        }

        private void OnUserActivity()
        {
            _inactivityService?.Reset();
        }

        private void OnLogoutClicked(object? sender, EventArgs e)
        {
            _inactivityService?.Stop();
            _taskReminderService?.Stop();
            _authSessionService?.ClearRememberedAccount();

            var services = IPlatformApplication.Current?.Services;
            var loginPage = services?.GetService<TodoLearn.Views.LoginPage>();
            var window = Application.Current?.Windows.FirstOrDefault();
            if (loginPage is not null && window is not null)
                window.Page = loginPage;
        }
    }
}
