using Microsoft.Maui.Controls;
using TodoLearn;

namespace TodoLearn.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly InactivityService _inactivityService;

        public LoginPage()
        {
            InitializeComponent();
            _inactivityService = IPlatformApplication.Current?.Services.GetService<InactivityService>() 
                ?? new InactivityService();
        }

        private void OnLoginClicked(object? sender, EventArgs e)
        {
            var password = PasswordEntry.Text;
            var (isValid, errorMessage) = _inactivityService.ValidatePassword(password, isPasswordField: true);

            if (!isValid)
            {
                StatusLabel.Text = errorMessage;
                StatusLabel.IsVisible = true;
                return;
            }

            StatusLabel.IsVisible = false;

            _inactivityService.Reset();
            _inactivityService.Start();

            Application.Current.MainPage = new AppShell();
        }
    }
}
