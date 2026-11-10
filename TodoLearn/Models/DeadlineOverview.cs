namespace TodoLearn.Models;

public sealed record DeadlineOverview(TaskItem? NextTask, int OverdueCount)
{
    public static DeadlineOverview Create(IEnumerable<TaskItem> tasks, DateTime now)
    {
        TaskItem? next = null;
        var overdue = 0;
        foreach (var task in tasks)
        {
            if (task.IsCompleted) continue;
            if (task.DueAt < now) overdue++;
            else if (next is null || task.DueAt < next.DueAt) next = task;
        }
        return new DeadlineOverview(next, overdue);
    }
}
