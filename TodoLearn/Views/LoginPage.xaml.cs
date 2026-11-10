using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using System;
using TodoLearn.Resources.Strings;
using TodoLearn.Services;

namespace TodoLearn.Views
{
    public partial class LoginPage : ContentPage
    {
        private async void OnSettingsClicked(object? sender, EventArgs e)
        {
            if (sender is not Button button || !button.IsEnabled) return;
            button.IsEnabled = false;
            try { await Navigation.PushModalAsync(new SettingsPage()); }
            finally { button.IsEnabled = true; }
        }

        private readonly InactivityService _inactivityService;
        private readonly AuthService _authService;
        private readonly AuthSessionService _authSessionService;
        private readonly IGoogleSignInService _googleSignInService;
        private bool _restoreAttempted;

        public LoginPage(
            AuthService authService,
            InactivityService inactivityService,
            AuthSessionService authSessionService,
            IGoogleSignInService googleSignInService)
        {
            InitializeComponent();
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _inactivityService = inactivityService ?? throw new ArgumentNullException(nameof(inactivityService));
            _authSessionService = authSessionService ?? throw new ArgumentNullException(nameof(authSessionService));
            _googleSignInService = googleSignInService ?? throw new ArgumentNullException(nameof(googleSignInService));
            var isWindows = DeviceInfo.Platform == DevicePlatform.WinUI;
            GoogleSignInSeparator.IsVisible = isWindows;
            GoogleSignInButton.IsVisible = isWindows;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_restoreAttempted)
                return;

            _restoreAttempted = true;
            var username = await _authSessionService.GetRememberedUsernameAsync();
            if (username is null)
                return;

            var window = Application.Current?.Windows.FirstOrDefault();
            if (window is null)
                return;

            window.Page = new AppShell();
            _inactivityService.Reset();
            _inactivityService.Start();
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

                await OpenAuthenticatedAppAsync(UsernameEntry.Text!.Trim());
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

        private async void OnGoogleSignInClicked(object? sender, EventArgs e)
        {
            StatusLabel.IsVisible = false;
            SetLoading(true);

            try
            {
                var googleIdentity = await _googleSignInService.SignInAsync();
                var username = await _authService.GetOrCreateExternalAccountAsync(
                    GoogleSignInService.ProviderName,
                    googleIdentity.Subject,
                    googleIdentity.Email);
                await OpenAuthenticatedAppAsync(username);
            }
            catch (GoogleSignInCancelledException)
            {
            }
            catch (GoogleClientConfigurationException)
            {
                ShowError(AppResources.GoogleClientSecretRequired);
            }
            catch (TimeoutException)
            {
                ShowError(AppResources.GoogleAuthTimeout);
            }
            catch (Exception exception)
            {
                var details = exception.GetBaseException().Message;
                ShowError(string.IsNullOrWhiteSpace(details)
                    ? AppResources.GoogleAuthFailed
                    : $"{AppResources.GoogleAuthFailed}{Environment.NewLine}{details}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async Task OpenAuthenticatedAppAsync(string username)
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window is null)
            {
                ShowError(AppResources.ApplicationWindowUnavailable);
                return;
            }

            await _authSessionService.RememberAccountAsync(username);
            window.Page = new AppShell();
            _inactivityService.Reset();
            _inactivityService.Start();
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
            GoogleSignInButton.IsEnabled = !isLoading;
        }

        private void ShowError(string message)
        {
            StatusLabel.TextColor = Colors.Red;
            StatusLabel.Text = message;
            StatusLabel.IsVisible = true;
        }
    }
}
