using TodoLearn.Services;

namespace TodoLearn.Views;

public partial class SettingsPage : ContentPage
{
    private bool _initializing = true;
    private bool _closing;

    public SettingsPage()
    {
        InitializeComponent();
        var selected = ThemeSettings.SelectedTheme;
        LightThemeRadio.IsChecked = selected == AppTheme.Light;
        DarkThemeRadio.IsChecked = selected == AppTheme.Dark;
        SystemThemeRadio.IsChecked = selected == AppTheme.Unspecified;
        _initializing = false;
    }

    private void OnThemeChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (_initializing || !e.Value || sender is not RadioButton radio)
            return;
        var theme = (radio.Value as string) switch
        {
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
        ThemeSettings.SetTheme(theme);
    }

    private async void OnDoneClicked(object? sender, EventArgs e)
    {
        if (_closing) return;
        _closing = true;
        try { await Navigation.PopModalAsync(); }
        finally { _closing = false; }
    }
}
