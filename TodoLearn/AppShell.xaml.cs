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
            BuildNavigation();

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

        private readonly List<(Button Button, ShellItem Item)> _navigationButtons = new();

        private void BuildNavigation()
        {
            var symbols = new[] { "☀", "☆", "◷", "☷" };
            for (var index = 0; index < Items.Count; index++)
            {
                if (index == 4)
                {
                    var heading = new Label { Text = TodoLearn.Resources.Strings.AppResources.Categories, FontSize = 11, Margin = new Thickness(12, 24, 0, 8) };
                    heading.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#637581"), Color.FromArgb("#A9BBC4"));
                    NavigationItems.Children.Add(heading);
                }
                var item = Items[index];
                var button = new Button
                {
                    Text = $"{(index < 4 ? symbols[index] : "·")}   {item.Title}",
                    FontSize = 14, HeightRequest = 42, CornerRadius = 8,
                    Padding = new Thickness(12, 0), HorizontalOptions = LayoutOptions.Fill,
                    BackgroundColor = Colors.Transparent
                };
                button.SetAppThemeColor(Button.TextColorProperty, Color.FromArgb("#344650"), Color.FromArgb("#DFE8EC"));
#if WINDOWS
                button.HandlerChanged += (_, _) =>
                {
                    if (button.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Button nativeButton)
                    {
                        nativeButton.HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left;
                        nativeButton.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                    }
                };
#endif
                button.Clicked += (_, _) => CurrentItem = item;
                NavigationItems.Children.Add(button);
                _navigationButtons.Add((button, item));
            }
            Navigated += (_, _) => UpdateNavigationSelection();
            UpdateNavigationSelection();
        }

        private void UpdateNavigationSelection()
        {
            foreach (var (button, item) in _navigationButtons)
            {
                if (CurrentItem == item)
                    button.SetAppThemeColor(Button.BackgroundColorProperty, Color.FromArgb("#D6E9EC"), Color.FromArgb("#30454D"));
                else
                    button.BackgroundColor = Colors.Transparent;
                button.FontAttributes = CurrentItem == item ? FontAttributes.Bold : FontAttributes.None;
            }
        }

        private void OnUserActivity()
        {
            _inactivityService?.Reset();
        }

        private async void OnSettingsClicked(object? sender, EventArgs e)
        {
            if (!SettingsButton.IsEnabled) return;
            SettingsButton.IsEnabled = false;
            try { await Navigation.PushModalAsync(new Views.SettingsPage()); }
            finally { SettingsButton.IsEnabled = true; }
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
