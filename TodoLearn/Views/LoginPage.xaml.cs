using System;
using Microsoft.Maui.Controls;
using TodoLearn.Resources.Strings;
using TodoLearn.Services;

namespace TodoLearn.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly InactivityService _inactivityService;
        private readonly AuthService _authService;

        public LoginPage(AuthService authService, InactivityService inactivityService)
        {
            InitializeComponent();
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _inactivityService = inactivityService ?? throw new ArgumentNullException(nameof(inactivityService));
        }

        private async void OnLoginClicked(object? sender, EventArgs e)
        {
            StatusLabel.IsVisible = false;

            SetLoading(true);
            try
            {
                var result = await _authService.SignInAsync(UsernameEntry.Text, PasswordEntry.Text);
                if (!result.IsAuthenticated)
                {
                    if (result.IsLocked)
                    {
                        var seconds = Math.Ceiling(result.LockoutRemaining!.Value.TotalSeconds);
                        ShowError(string.Format(AppResources.LoginLocked, seconds));
                    }
                    else
                    {
                        ShowError(AppResources.InvalidCredentials);
                    }

                    return;
                }

                var window = Application.Current?.Windows.FirstOrDefault();
                if (window is null)
                {
                    ShowError(AppResources.ApplicationWindowUnavailable);
                    return;
                }

                window.Page = new AppShell();
                _inactivityService.Reset();
                _inactivityService.Start();
            }
            catch
            {
                ShowError(AppResources.LoginFailed);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void OnRegisterClicked(object? sender, EventArgs e)
        {
            SetLoading(true);
            try
            {
                var error = await _authService.RegisterAsync(UsernameEntry.Text, PasswordEntry.Text);
                if (error is not null)
                {
                    ShowError(error);
                    return;
                }

                PasswordEntry.Text = string.Empty;
                StatusLabel.TextColor = Colors.Green;
                StatusLabel.Text = AppResources.AccountCreated;
                StatusLabel.IsVisible = true;
            }
            catch
            {
                ShowError(AppResources.RegistrationFailed);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void SetLoading(bool isLoading)
        {
            LoginButton.IsEnabled = !isLoading;
            RegisterButton.IsEnabled = !isLoading;
        }

        private void ShowError(string message)
        {
            StatusLabel.TextColor = Colors.Red;
            StatusLabel.Text = message;
            StatusLabel.IsVisible = true;
        }
    }
}
