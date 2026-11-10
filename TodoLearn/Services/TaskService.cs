using Microsoft.EntityFrameworkCore;
using TodoLearn.Models;

namespace TodoLearn.Services;

public sealed class TaskService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAccountSession _authSessionService;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private List<TaskItem>? _tasks;
    private int? _cachedAccountId;

    public TaskService(IDbContextFactory<AppDbContext> dbFactory, IAccountSession authSessionService, TimeProvider? timeProvider = null)
    {
        _dbFactory = dbFactory;
        _authSessionService = authSessionService;
        _timeProvider = timeProvider ?? TimeProvider.System;
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
                .Include(task => task.Steps.OrderBy(step => step.Position))
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

    public async Task<TaskItem> AddAsync(string text, string? description, TaskPriority priority, DateTime dueAt, TaskCategory? category = null, IEnumerable<TaskStep>? steps = null)
    {
        var accountId = GetCurrentAccountId();
        var task = new TaskItem
        {
            UserAccountId = accountId,
            Category = category,
            Text = text,
            Description = string.IsNullOrWhiteSpace(description) ? null : description,
            Priority = priority,
            DueAt = dueAt
        };
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Tasks.Add(task);
        foreach (var step in steps ?? [])
        {
            if (!string.IsNullOrWhiteSpace(step.Text))
                task.Steps.Add(new TaskStep { Text = step.Text.Trim(), Position = task.Steps.Count, IsCompleted = step.IsCompleted });
        }
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
        var storedTask = await db.Tasks.Include(item => item.Steps).FirstOrDefaultAsync(item =>
            item.Id == task.Id && item.UserAccountId == accountId);
        if (storedTask is null)
            return;

        if (storedTask.DueAt != task.DueAt || (!storedTask.IsCompleted && task.IsCompleted))
            task.ResetReminderState();

        db.Entry(storedTask).CurrentValues.SetValues(task);
        var desiredSteps = task.Steps.Where(step => !string.IsNullOrWhiteSpace(step.Text)).ToList();
        foreach (var removed in storedTask.Steps.Where(step => !desiredSteps.Any(desired => desired.Id == step.Id)).ToList())
            db.Remove(removed);
        var savedSteps = new List<(TaskStep Draft, TaskStep Stored)>();
        for (var index = 0; index < desiredSteps.Count; index++)
        {
            var draft = desiredSteps[index];
            var stored = draft.Id == 0 ? null : storedTask.Steps.SingleOrDefault(step => step.Id == draft.Id);
            if (draft.Id != 0 && stored is null)
                throw new InvalidOperationException("The subtask does not belong to this task.");
            if (stored is null)
            {
                stored = new TaskStep();
                storedTask.Steps.Add(stored);
            }
            stored.Text = draft.Text.Trim();
            stored.IsCompleted = draft.IsCompleted;
            stored.Position = index;
            savedSteps.Add((draft, stored));
        }
        await db.SaveChangesAsync();
        foreach (var (draft, stored) in savedSteps)
        {
            draft.Id = stored.Id;
            draft.TaskItemId = stored.TaskItemId;
            draft.Position = stored.Position;
        }
    }

    public async Task SetStepCompletedAsync(TaskItem task, TaskStep step, bool completed)
    {
        var accountId = GetCurrentAccountId();
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (!await db.Tasks.AnyAsync(item => item.Id == task.Id && item.UserAccountId == accountId))
            throw new InvalidOperationException("The task is not available to this account.");
        var stored = await db.Set<TaskStep>().SingleAsync(item => item.Id == step.Id && item.TaskItemId == task.Id);
        stored.IsCompleted = completed;
        await db.SaveChangesAsync();
        step.IsCompleted = completed;
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
        storedTask.ReminderSnoozedUntil = _timeProvider.GetLocalNow().DateTime.Add(duration);
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

    public async Task ClearCompletedAsync(bool filterByCategory = false, TaskCategory? category = null)
    {
        var accountId = GetCurrentAccountId();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var tasks = db.Tasks.Where(task => task.UserAccountId == accountId && task.IsCompleted);
        if (filterByCategory)
            tasks = tasks.Where(task => task.Category == category);
        await tasks.ExecuteDeleteAsync();
        _tasks?.RemoveAll(task => task.IsCompleted && (!filterByCategory || task.Category == category));
    }

    private int GetCurrentAccountId() => _authSessionService.CurrentAccountId
        ?? throw new InvalidOperationException("An authenticated account is required to access tasks.");
}
