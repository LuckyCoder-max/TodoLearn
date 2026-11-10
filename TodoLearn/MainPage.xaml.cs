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
        TaskList.StartDashboardClock();
        await TaskList.LoadTasksAsync();
        var reminderService = IPlatformApplication.Current?.Services.GetService<TodoLearn.Services.TaskReminderService>();
        if (reminderService is not null)
        {
            reminderService.SetWindowActive(true);
            await reminderService.ShowPendingInAppRemindersAsync();
        }
    }

    protected override void OnDisappearing()
    {
        TaskList.StopDashboardClock();
        base.OnDisappearing();
    }

}
