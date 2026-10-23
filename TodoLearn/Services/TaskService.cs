using Microsoft.EntityFrameworkCore;
using TodoLearn.Models;

namespace TodoLearn.Services;

public sealed class TaskService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private List<TaskItem>? _tasks;

    public TaskService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<TaskItem>> GetTasksAsync()
    {
        if (_tasks is not null)
            return _tasks;

        await _loadLock.WaitAsync();
        try
        {
            if (_tasks is not null)
                return _tasks;

            await using var db = await _dbFactory.CreateDbContextAsync();
            _tasks = await db.Tasks.OrderBy(task => task.CreatedAt).ToListAsync();
            return _tasks;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task<TaskItem> AddAsync(string text)
    {
        var task = new TaskItem { Text = text };
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        _tasks?.Add(task);
        return task;
    }

    public async Task UpdateAsync(TaskItem task)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Tasks.Update(task);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(TaskItem task)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
        _tasks?.Remove(task);
    }

    public async Task ClearAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Tasks.ExecuteDeleteAsync();
        _tasks?.Clear();
    }
}
