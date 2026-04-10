using Microsoft.Maui.Controls;

namespace TodoLearn.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void OnLoginClicked(object? sender, EventArgs e)
        {
            Application.Current.MainPage = new AppShell();
        }
    }
}
