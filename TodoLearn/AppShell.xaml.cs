using Microsoft.Maui.Controls;

namespace TodoLearn
{
    public partial class AppShell : Shell
    {
        private readonly InactivityService? _inactivityService;

        public AppShell()
        {
            InitializeComponent();

            _inactivityService = IPlatformApplication.Current?.Services.GetService<InactivityService>();

            if (_inactivityService != null)
            {
                this.Navigated += (s, e) => OnUserActivity();
            }
        }

        private void OnUserActivity()
        {
            _inactivityService?.Reset();
        }
    }
}
