namespace TodoLearn.Services;

public static class ThemeSettings
{
    private const string PreferenceKey = "appearance.theme";

    public static AppTheme SelectedTheme
    {
        get
        {
            var value = Preferences.Default.Get(PreferenceKey, (int)AppTheme.Unspecified);
            return Enum.IsDefined(typeof(AppTheme), value) ? (AppTheme)value : AppTheme.Unspecified;
        }
    }

    public static void SetTheme(AppTheme theme)
    {
        Preferences.Default.Set(PreferenceKey, (int)theme);
        if (Application.Current is { } application)
            Apply(application);
    }

    public static void Apply(Application application)
    {
        application.UserAppTheme = SelectedTheme;
        foreach (var window in application.Windows)
            ApplyToWindow(window);
    }

    public static void ApplyToWindow(Window window)
    {
#if WINDOWS
        if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow
            && nativeWindow.Content is Microsoft.UI.Xaml.FrameworkElement root)
        {
            root.RequestedTheme = SelectedTheme switch
            {
                AppTheme.Light => Microsoft.UI.Xaml.ElementTheme.Light,
                AppTheme.Dark => Microsoft.UI.Xaml.ElementTheme.Dark,
                _ => Microsoft.UI.Xaml.ElementTheme.Default
            };
        }
#endif
    }
}
