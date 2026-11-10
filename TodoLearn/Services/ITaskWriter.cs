using TodoLearn.Models;

namespace TodoLearn.Services;

public interface ITaskWriter
{
    Task<TaskItem> AddAsync(string text, string? description, TaskPriority priority, DateTime dueAt,
        TaskCategory? category = null, IEnumerable<TaskStep>? steps = null);
    Task UpdateAsync(TaskItem task);
}
