namespace TodoLearn;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await TaskList.LoadTasksAsync();
        var reminderService = IPlatformApplication.Current?.Services.GetService<TodoLearn.Services.TaskReminderService>();
        if (reminderService is not null)
        {
            reminderService.SetWindowActive(true);
            await reminderService.ShowPendingInAppRemindersAsync();
        }
    }

}
