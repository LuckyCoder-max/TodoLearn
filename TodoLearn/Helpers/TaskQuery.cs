using TodoLearn.Models;

namespace TodoLearn.Helpers;

public static class TaskQuery
{
    public static IReadOnlyList<TaskItem> Select(IEnumerable<TaskItem> tasks, FilterType filter,
        DateTime now, bool filterByCategory = false, TaskCategory? category = null)
    {
        var filtered = filterByCategory
            ? tasks.Where(task => task.Category == category)
            : filter switch
            {
                FilterType.Important => tasks.Where(task => task.Priority == TaskPriority.High),
                FilterType.Planned => tasks.Where(task => task.DueAt > now),
                FilterType.All => tasks,
                _ => tasks.Where(task => task.DueAt.Date == now.Date)
            };
        return filtered.OrderBy(task => task.IsCompleted).ThenBy(task => task.DueAt).ToList();
    }
}
