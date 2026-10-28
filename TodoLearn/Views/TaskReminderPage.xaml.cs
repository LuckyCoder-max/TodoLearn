using TodoLearn.Models;
using TodoLearn.Resources.Strings;
using TodoLearn.Services;

namespace TodoLearn.Views;

public partial class TaskReminderPage : ContentPage
{
    private static readonly TimeSpan[] SnoozeDurations =
    [
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(1)
    ];

    private readonly TaskItem _task;
    private readonly TaskService _taskService;
    private readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _isSaving;

    public Task TaskClosed => _closed.Task;

    public TaskReminderPage(TaskItem task, TaskService taskService)
    {
        InitializeComponent();
        _task = task;
        _taskService = taskService;
        TaskNameLabel.Text = task.Text;
        TaskDescriptionLabel.Text = task.Description;
        TaskDescriptionLabel.IsVisible = !string.IsNullOrWhiteSpace(task.Description);
        DueAtLabel.Text = string.Format(AppResources.DueAtFormat, task.DueAt);
        SnoozePicker.ItemsSource = new[]
        {
            AppResources.TaskReminderSnooze5Minutes,
            AppResources.TaskReminderSnooze15Minutes,
            AppResources.TaskReminderSnooze30Minutes,
            AppResources.TaskReminderSnooze1Hour
        };
        SnoozePicker.SelectedIndex = 1;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _closed.TrySetResult();
    }

    private async void OnSnoozeClicked(object? sender, EventArgs e)
    {
        if (SnoozePicker.SelectedIndex < 0 || SnoozePicker.SelectedIndex >= SnoozeDurations.Length)
            return;

        await SaveAndCloseAsync(() => _taskService.SnoozeReminderAsync(_task, SnoozeDurations[SnoozePicker.SelectedIndex]));
    }

    private async void OnDismissClicked(object? sender, EventArgs e)
    {
        await SaveAndCloseAsync(() => _taskService.DismissReminderAsync(_task));
    }

    private async Task SaveAndCloseAsync(Func<Task<bool>> saveAction)
    {
        if (_isSaving)
            return;

        _isSaving = true;
        SetActionsEnabled(false);
        try
        {
            if (!await saveAction())
            {
                await DisplayAlertAsync(
                    AppResources.TaskReminderSaveFailedTitle,
                    AppResources.TaskReminderSaveFailedMessage,
                    AppResources.OkButton);
                return;
            }

            await Navigation.PopModalAsync();
        }
        catch
        {
            await DisplayAlertAsync(
                AppResources.TaskReminderSaveFailedTitle,
                AppResources.TaskReminderSaveFailedMessage,
                AppResources.OkButton);
        }
        finally
        {
            _isSaving = false;
            SetActionsEnabled(true);
        }
    }

    private void SetActionsEnabled(bool isEnabled)
    {
        SnoozePicker.IsEnabled = isEnabled;
        SnoozeButton.IsEnabled = isEnabled;
        DismissButton.IsEnabled = isEnabled;
    }
}
