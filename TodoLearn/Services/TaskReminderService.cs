using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using TodoLearn.Models;
using TodoLearn.Resources.Strings;
using TodoLearn.Views;
#if WINDOWS
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
#endif

namespace TodoLearn.Services;

public sealed class TaskReminderService
{
    private static readonly TimeSpan ReminderLeadTime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(15);
    private readonly TaskService _taskService;
    private readonly AuthSessionService _authSessionService;
    private readonly ILogger<TaskReminderService> _logger;
    private readonly object _sync = new();
    private readonly SemaphoreSlim _presentationLock = new(1, 1);
    private CancellationTokenSource? _cancellation;
    private bool _notificationsRegistered;
    private int _windowIsActive;

    public TaskReminderService(TaskService taskService, AuthSessionService authSessionService, ILogger<TaskReminderService> logger)
    {
        _taskService = taskService;
        _authSessionService = authSessionService;
        _logger = logger;
    }

    public void Start()
    {
        lock (_sync)
        {
            if (_cancellation is not null)
                return;

#if WINDOWS
            if (!_notificationsRegistered)
            {
                try
                {
                    AppNotificationManager.Default.Register();
                    _notificationsRegistered = true;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Windows app notifications could not be registered.");
                }
            }
#endif

            _cancellation = new CancellationTokenSource();
            _ = RunAsync(_cancellation.Token);
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = null;
        }
    }

    public void SetWindowActive(bool isActive) => Volatile.Write(ref _windowIsActive, isActive ? 1 : 0);

    public async Task ShowPendingInAppRemindersAsync()
    {
        if (!await _presentationLock.WaitAsync(0))
            return;

        try
        {
            var rootPage = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (rootPage is not AppShell || _authSessionService.CurrentAccountId is null)
                return;

            var pendingTasks = (await _taskService.GetTasksAsync())
                .Where(task => !task.IsCompleted && task.ReminderPendingForDueAt == task.DueAt)
                .ToList();

            foreach (var task in pendingTasks)
            {
                if (_authSessionService.CurrentAccountId != task.UserAccountId)
                    return;

                var reminderPage = new TaskReminderPage(task, _taskService);
                await MainThread.InvokeOnMainThreadAsync(() => rootPage.Navigation.PushModalAsync(reminderPage));
                await reminderPage.TaskClosed;

                rootPage = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (rootPage is not AppShell || _authSessionService.CurrentAccountId is null)
                    return;
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "In-app task reminder could not be shown.");
        }
        finally
        {
            _presentationLock.Release();
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await CheckTasksAsync(cancellationToken);
                if (!await timer.WaitForNextTickAsync(cancellationToken))
                    return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Task deadline reminder check failed.");
            }
        }
    }

    private async Task CheckTasksAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var dueTasks = (await _taskService.GetTasksAsync())
            .Where(task => !task.IsCompleted
                && task.ReminderDismissedForDueAt != task.DueAt
                && task.ReminderPendingForDueAt != task.DueAt
                && ((task.ReminderSnoozedUntil is DateTime snoozedUntil && snoozedUntil <= now)
                    || (task.DueAt > now
                        && task.DueAt <= now + ReminderLeadTime
                        && task.ReminderSentForDueAt != task.DueAt)))
            .ToList();

        var reminders = new List<TaskItem>();
        foreach (var task in dueTasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await _taskService.MarkReminderPendingAsync(task, task.DueAt, now))
                reminders.Add(task);
        }

        if (reminders.Count > 0)
        {
#if WINDOWS
            var message = string.Join(Environment.NewLine, reminders.Select(task =>
                string.Format(CultureInfo.CurrentCulture, AppResources.TaskReminderMessage, task.Text, task.DueAt.ToString("dd MMM yyyy, HH:mm", CultureInfo.CurrentCulture))));

            try
            {
                var notification = new AppNotificationBuilder()
                    .AddText(AppResources.TaskReminderTitle)
                    .AddText(message)
                    .MuteAudio()
                    .BuildNotification();
                AppNotificationManager.Default.Show(notification);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Windows task reminder notification could not be shown.");
            }

            PlayReminderSound();
#endif
        }

        if (reminders.Count > 0 && Volatile.Read(ref _windowIsActive) != 0)
            await ShowPendingInAppRemindersAsync();
    }

#if WINDOWS
    private void PlayReminderSound()
    {
        var soundPath = Path.Combine(AppContext.BaseDirectory, "bell.wav");
        if (!File.Exists(soundPath) || !PlaySound(soundPath, IntPtr.Zero, 0x00020003))
            _logger.LogWarning("The task reminder sound could not be played from {SoundPath}.", soundPath);
    }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool PlaySound(string soundPath, IntPtr module, uint flags);
#endif
}
