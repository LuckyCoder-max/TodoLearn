using TodoLearn.Models;

namespace TodoLearn.Tests.Unit;

[TestFixture, Category("Unit")]
public sealed class DeadlineOverviewTests
{
    private static readonly DateTime Now = new(2026, 11, 1, 12, 0, 0);

    [Test]
    public void NearestUnfinishedFutureTaskIsSelectedAcrossCategories()
    {
        var next = new TaskItem { Text = "Next", DueAt = Now.AddMinutes(5), Category = TaskCategory.Work };
        var result = DeadlineOverview.Create([
            new TaskItem { DueAt = Now.AddDays(1), Category = TaskCategory.Home },
            new TaskItem { DueAt = Now.AddMinutes(1), IsCompleted = true },
            new TaskItem { DueAt = Now.AddMinutes(-1) }, next], Now);
        Assert.That(result.NextTask, Is.SameAs(next));
        Assert.That(result.OverdueCount, Is.EqualTo(1));
    }

    [TestCase(-1, false, true)]
    [TestCase(0, true, false)]
    [TestCase(1, true, false)]
    public void DeadlineBoundaryUsesExactTime(int seconds, bool upcoming, bool overdue)
    {
        var task = new TaskItem { DueAt = Now.AddSeconds(seconds) };
        var result = DeadlineOverview.Create([task], Now);
        Assert.That(result.NextTask is not null, Is.EqualTo(upcoming));
        Assert.That(result.OverdueCount, Is.EqualTo(overdue ? 1 : 0));
    }

    [Test]
    public void EmptyListHasNoDeadlineOrOverdueTasks() =>
        Assert.That(DeadlineOverview.Create([], Now), Is.EqualTo(new DeadlineOverview(null, 0)));

    [Test]
    public void CompletedTasksAreExcludedFromBothCounts() =>
        Assert.That(DeadlineOverview.Create([
            new TaskItem { DueAt = Now.AddDays(-1), IsCompleted = true },
            new TaskItem { DueAt = Now.AddDays(1), IsCompleted = true }], Now),
            Is.EqualTo(new DeadlineOverview(null, 0)));
}
