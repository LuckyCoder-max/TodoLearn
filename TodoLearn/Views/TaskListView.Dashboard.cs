using TodoLearn.Models;
using TodoLearn.Resources.Strings;

namespace TodoLearn.Views;

public partial class TaskListView
{
    private IDispatcherTimer? _clockTimer;
    private TaskItem? _nextDeadline;
    private DateTime _clockNow = DateTime.Now;
    private DateTime _summaryUpdatedAt;
    private int _overdueCount;

    public bool IsDashboardVisible => !_categoryView && Filter == FilterType.MyDay;
    public string CurrentTimeText => _clockNow.ToString("HH:mm:ss");
    public string NextDeadlineTitle => _nextDeadline?.Text ?? AppResources.NoUpcomingDeadlines;
    public string CompactNextTaskTitle => _nextDeadline is null ? AppResources.NoUpcomingDeadlines
        : string.Format(AppResources.NextTaskCompactFormat, _nextDeadline.Text);
    public bool HasNextDeadline => _nextDeadline is not null;
    public bool IsDeadlineSoon => _nextDeadline is not null && _nextDeadline.DueAt - _clockNow <= TimeSpan.FromMinutes(10);
    public bool HasOverdueTasks => _overdueCount > 0;
    public string OverdueSummary => string.Format(AppResources.OverdueTasksFormat, _overdueCount);
    public string NextDeadlineTime => _nextDeadline is null ? string.Empty
        : string.Format(AppResources.DueAtFormat, _nextDeadline.DueAt);
    public string NextDeadlineRemaining
    {
        get
        {
            if (_nextDeadline is null) return string.Empty;
            var remaining = _nextDeadline.DueAt - _clockNow;
            if (remaining.TotalMinutes < 1) return AppResources.DueNow;
            if (remaining.TotalHours < 1)
                return string.Format(AppResources.DueInMinutes, (int)Math.Ceiling(remaining.TotalMinutes));
            if (remaining.TotalDays < 1)
                return string.Format(AppResources.DueInHoursMinutes, (int)remaining.TotalHours, remaining.Minutes);
            return string.Format(AppResources.DueInDaysHours, (int)remaining.TotalDays, remaining.Hours);
        }
    }

    public void StartDashboardClock()
    {
        if (!IsDashboardVisible) return;
        UpdateDashboard();
        _clockTimer ??= Dispatcher.CreateTimer();
        _clockTimer.Interval = TimeSpan.FromSeconds(1);
        _clockTimer.Tick -= OnClockTick;
        _clockTimer.Tick += OnClockTick;
        _clockTimer.Start();
    }

    public void StopDashboardClock() => _clockTimer?.Stop();

    private void OnClockTick(object? sender, EventArgs e)
    {
        var previousDate = _clockNow.Date;
        _clockNow = DateTime.Now;
        OnPropertyChanged(nameof(CurrentTimeText));
        if (previousDate != _clockNow.Date)
        {
            OnPropertyChanged(nameof(TodayText));
            RefreshDisplay();
        }
        else if (_clockNow - _summaryUpdatedAt >= TimeSpan.FromMinutes(1)
            || _clockNow < _summaryUpdatedAt
            || (_nextDeadline is not null && (_nextDeadline.IsCompleted || _nextDeadline.DueAt < _clockNow)))
            UpdateDashboard();
        OnPropertyChanged(nameof(NextDeadlineRemaining));
        OnPropertyChanged(nameof(IsDeadlineSoon));
    }

    private void UpdateDashboard()
    {
        if (!IsDashboardVisible) return;
        _clockNow = DateTime.Now;
        _summaryUpdatedAt = _clockNow;
        var overview = DeadlineOverview.Create(Tasks, _clockNow);
        _nextDeadline = overview.NextTask;
        _overdueCount = overview.OverdueCount;
        foreach (var property in new[] { nameof(CurrentTimeText), nameof(NextDeadlineTitle), nameof(CompactNextTaskTitle), nameof(HasNextDeadline),
            nameof(NextDeadlineTime), nameof(NextDeadlineRemaining), nameof(IsDeadlineSoon), nameof(HasOverdueTasks), nameof(OverdueSummary) })
            OnPropertyChanged(property);
    }
}
