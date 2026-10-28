using Microsoft.EntityFrameworkCore;
using TodoLearn.Models;

namespace TodoLearn.Services;

public sealed class TaskService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AuthSessionService _authSessionService;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private List<TaskItem>? _tasks;
    private int? _cachedAccountId;

    public TaskService(IDbContextFactory<AppDbContext> dbFactory, AuthSessionService authSessionService)
    {
        _dbFactory = dbFactory;
        _authSessionService = authSessionService;
    }

    public async Task<IReadOnlyList<TaskItem>> GetTasksAsync()
    {
        var accountId = GetCurrentAccountId();
        if (_tasks is not null && _cachedAccountId == accountId)
            return _tasks;

        await _loadLock.WaitAsync();
        try
        {
            if (_tasks is not null && _cachedAccountId == accountId)
                return _tasks;

            await using var db = await _dbFactory.CreateDbContextAsync();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"Tasks\" SET \"UserAccountId\" = {accountId} WHERE \"UserAccountId\" IS NULL;");
            _tasks = await db.Tasks
                .Where(task => task.UserAccountId == accountId)
                .OrderBy(task => task.CreatedAt)
                .ToListAsync();
            _cachedAccountId = accountId;
            return _tasks;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task<TaskItem> AddAsync(string text, string? description, TaskPriority priority, DateTime dueAt)
    {
        var accountId = GetCurrentAccountId();
        var task = new TaskItem
        {
            UserAccountId = accountId,
            Text = text,
            Description = string.IsNullOrWhiteSpace(description) ? null : description,
            Priority = priority,
            DueAt = dueAt
        };
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        if (_cachedAccountId == accountId)
            _tasks?.Add(task);
        return task;
    }

    public async Task UpdateAsync(TaskItem task)
    {
        var accountId = GetCurrentAccountId();
        if (task.UserAccountId != accountId)
            return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var storedTask = await db.Tasks.FirstOrDefaultAsync(item =>
            item.Id == task.Id && item.UserAccountId == accountId);
        if (storedTask is null)
            return;

        if (storedTask.DueAt != task.DueAt || (!storedTask.IsCompleted && task.IsCompleted))
            task.ResetReminderState();

        db.Entry(storedTask).CurrentValues.SetValues(task);
        await db.SaveChangesAsync();
    }

    public async Task<bool> MarkReminderPendingAsync(TaskItem task, DateTime dueAt, DateTime now)
    {
        var accountId = GetCurrentAccountId();
        if (task.UserAccountId != accountId)
            return false;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var storedTask = await db.Tasks.FirstOrDefaultAsync(item =>
            item.Id == task.Id && item.UserAccountId == accountId);
        if (storedTask is null
            || storedTask.IsCompleted
            || storedTask.DueAt != dueAt
            || storedTask.ReminderDismissedForDueAt == dueAt
            || storedTask.ReminderPendingForDueAt == dueAt)
            return false;

        var snoozeExpired = storedTask.ReminderSnoozedUntil is DateTime snoozedUntil && snoozedUntil <= now;
        var initialReminderDue = storedTask.ReminderSentForDueAt != dueAt
            && dueAt > now
            && dueAt <= now.AddMinutes(10);
        if (!snoozeExpired && !initialReminderDue)
            return false;

        if (snoozeExpired)
            storedTask.ReminderSnoozedUntil = null;
        else
            storedTask.ReminderSentForDueAt = dueAt;
        storedTask.ReminderPendingForDueAt = dueAt;
        await db.SaveChangesAsync();
        task.ReminderSentForDueAt = dueAt;
        task.ReminderPendingForDueAt = dueAt;
        task.ReminderSnoozedUntil = storedTask.ReminderSnoozedUntil;
        return true;
    }

    public async Task<bool> SnoozeReminderAsync(TaskItem task, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return false;

        var accountId = GetCurrentAccountId();
        if (task.UserAccountId != accountId)
            return false;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var storedTask = await db.Tasks.FirstOrDefaultAsync(item =>
            item.Id == task.Id && item.UserAccountId == accountId);
        if (storedTask is null || storedTask.IsCompleted || storedTask.ReminderPendingForDueAt != storedTask.DueAt)
            return false;

        storedTask.ReminderPendingForDueAt = null;
        storedTask.ReminderSnoozedUntil = DateTime.Now.Add(duration);
        await db.SaveChangesAsync();
        task.ReminderPendingForDueAt = null;
        task.ReminderSnoozedUntil = storedTask.ReminderSnoozedUntil;
        return true;
    }

    public async Task<bool> DismissReminderAsync(TaskItem task)
    {
        var accountId = GetCurrentAccountId();
        if (task.UserAccountId != accountId)
            return false;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var storedTask = await db.Tasks.FirstOrDefaultAsync(item =>
            item.Id == task.Id && item.UserAccountId == accountId);
        if (storedTask is null || storedTask.IsCompleted || storedTask.ReminderPendingForDueAt != storedTask.DueAt)
            return false;

        storedTask.ReminderPendingForDueAt = null;
        storedTask.ReminderSnoozedUntil = null;
        storedTask.ReminderDismissedForDueAt = storedTask.DueAt;
        await db.SaveChangesAsync();
        task.ReminderPendingForDueAt = null;
        task.ReminderSnoozedUntil = null;
        task.ReminderDismissedForDueAt = storedTask.DueAt;
        return true;
    }

    public async Task DeleteAsync(TaskItem task)
    {
        var accountId = GetCurrentAccountId();
        if (task.UserAccountId != accountId)
            return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var storedTask = await db.Tasks.FirstOrDefaultAsync(item =>
            item.Id == task.Id && item.UserAccountId == accountId);
        if (storedTask is null)
            return;

        db.Tasks.Remove(storedTask);
        await db.SaveChangesAsync();
        _tasks?.Remove(task);
    }

    public async Task ClearCompletedAsync()
    {
        var accountId = GetCurrentAccountId();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var tasks = db.Tasks.Where(task => task.UserAccountId == accountId && task.IsCompleted);
        await tasks.ExecuteDeleteAsync();
        _tasks?.RemoveAll(task => task.IsCompleted);
    }

    private int GetCurrentAccountId() => _authSessionService.CurrentAccountId
        ?? throw new InvalidOperationException("An authenticated account is required to access tasks.");
}
