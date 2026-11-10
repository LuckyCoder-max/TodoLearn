using TodoLearn.Helpers;
using TodoLearn.Models;

namespace TodoLearn.Tests.Unit;

[TestFixture, Category("Unit")]
public sealed class TaskQueryTests
{
    private static readonly DateTime Now = new(2026, 11, 1, 12, 0, 0);

    [TestCase(-1, 23, false)]
    [TestCase(0, 0, true)]
    [TestCase(0, 23, true)]
    [TestCase(1, 0, false)]
    public void MyDayIncludesEntireLocalDay(int day, int hour, bool included)
    {
        var task = new TaskItem { DueAt = Now.Date.AddDays(day).AddHours(hour) };
        Assert.That(TaskQuery.Select([task], FilterType.MyDay, Now).Count, Is.EqualTo(included ? 1 : 0));
    }

    [TestCase(TaskPriority.Low, false)]
    [TestCase(TaskPriority.Medium, false)]
    [TestCase(TaskPriority.High, true)]
    public void ImportantIncludesOnlyHighPriority(TaskPriority priority, bool included)
    {
        var task = new TaskItem { Priority = priority };
        Assert.That(TaskQuery.Select([task], FilterType.Important, Now).Count, Is.EqualTo(included ? 1 : 0));
    }

    [TestCase(-1, false)]
    [TestCase(0, false)]
    [TestCase(1, true)]
    public void PlannedIncludesOnlyFutureDeadlines(int seconds, bool included)
    {
        var task = new TaskItem { DueAt = Now.AddSeconds(seconds) };
        Assert.That(TaskQuery.Select([task], FilterType.Planned, Now).Count, Is.EqualTo(included ? 1 : 0));
    }

    [Test]
    public void AllTasksIncludesPastFutureAndCompletedTasks()
    {
        TaskItem[] tasks = [new() { DueAt = Now.AddDays(-1) }, new() { DueAt = Now.AddDays(1), IsCompleted = true }];
        Assert.That(TaskQuery.Select(tasks, FilterType.All, Now), Is.EquivalentTo(tasks));
    }

    [TestCase(null)]
    [TestCase(TaskCategory.Work)]
    [TestCase(TaskCategory.Home)]
    [TestCase(TaskCategory.Shopping)]
    [TestCase(TaskCategory.Study)]
    [TestCase(TaskCategory.Personal)]
    public void CategoryViewSelectsExactCategoryIncludingUncategorised(TaskCategory? category)
    {
        var target = new TaskItem { Category = category, DueAt = Now.AddDays(-2) };
        var others = Enum.GetValues<TaskCategory>().Select(value => new TaskItem { Category = value, DueAt = Now });
        var result = TaskQuery.Select(others.Append(new TaskItem()).Append(target), FilterType.Planned, Now, true, category);
        Assert.That(result, Does.Contain(target));
        Assert.That(result.All(task => task.Category == category), Is.True);
    }

    [Test]
    public void SortingPlacesActiveFirstThenOrdersByDeadlineWithoutMutatingInput()
    {
        var completed = new TaskItem { IsCompleted = true, DueAt = Now.AddDays(-1) };
        var later = new TaskItem { DueAt = Now.AddHours(1) };
        var earlier = new TaskItem { DueAt = Now };
        TaskItem[] input = [completed, later, earlier];
        Assert.That(TaskQuery.Select(input, FilterType.All, Now), Is.EqualTo(new[] { earlier, later, completed }));
        Assert.That(input, Is.EqualTo(new[] { completed, later, earlier }));
    }

    [TestCase(FilterType.MyDay)]
    [TestCase(FilterType.Important)]
    [TestCase(FilterType.Planned)]
    [TestCase(FilterType.All)]
    public void EmptyListWorksForEveryFilter(FilterType filter) =>
        Assert.That(TaskQuery.Select([], filter, Now), Is.Empty);
}
