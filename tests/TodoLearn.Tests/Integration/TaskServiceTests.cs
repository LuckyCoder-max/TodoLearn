using Microsoft.EntityFrameworkCore;
using TodoLearn.Models;
using TodoLearn.Services;

namespace TodoLearn.Tests.Integration;

[TestFixture, Category("Integration")]
public sealed class TaskServiceTests : DatabaseTest
{
    [Test]
    public async Task CreatePersistsAllFieldsAndOwnsIndependentSteps()
    {
        var draft = new TaskStep { Text = " Plan ", IsCompleted = true };
        var task = await AddTask("Roadmap", TaskCategory.Work, [draft, new TaskStep { Text = "  " }, new TaskStep { Text = "Ship" }]);
        draft.Text = "Changed outside task";
        var stored = await Reload(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That(stored.UserAccountId, Is.EqualTo(1));
            Assert.That(stored.Text, Is.EqualTo("Roadmap"));
            Assert.That(stored.Description, Is.EqualTo("Description"));
            Assert.That(stored.Priority, Is.EqualTo(TaskPriority.High));
            Assert.That(stored.Category, Is.EqualTo(TaskCategory.Work));
            Assert.That(stored.DueAt, Is.EqualTo(Clock.Now.AddMinutes(5)));
            Assert.That(stored.Steps.Select(step => step.Text), Is.EqualTo(new[] { "Plan", "Ship" }));
            Assert.That(stored.Steps.Select(step => step.Position), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(stored.Steps.All(step => step.Id > 0 && step.TaskItemId == task.Id), Is.True);
            Assert.That(stored.Steps.First().IsCompleted, Is.True);
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public async Task EmptyDescriptionIsStoredAsNull(string? description)
    {
        var task = await Tasks.AddAsync("Task", description, TaskPriority.Low, Clock.Now);
        Assert.That((await Reload(task.Id)).Description, Is.Null);
    }

    [Test]
    public async Task RepeatedReadsUseCacheAndCreateAndDeleteKeepItCurrent()
    {
        var first = await Tasks.GetTasksAsync();
        var contexts = Database.ContextsCreated;
        Assert.That(await Tasks.GetTasksAsync(), Is.SameAs(first));
        Assert.That(Database.ContextsCreated, Is.EqualTo(contexts));
        var task = await AddTask();
        Assert.That(first, Does.Contain(task));
        await Tasks.DeleteAsync(task);
        Assert.That(first, Is.Empty);
        await using var db = Database.CreateDbContext();
        Assert.That(await db.Tasks.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task SwitchingAccountsInvalidatesCacheAndNeverReturnsOtherUsersTasks()
    {
        var first = await AddTask("First");
        Assert.That(await Tasks.GetTasksAsync(), Has.Count.EqualTo(1));
        Session.CurrentAccountId = 2;
        Assert.That(await Tasks.GetTasksAsync(), Is.Empty);
        await AddTask("Second");
        Assert.That((await Tasks.GetTasksAsync()).Single().Text, Is.EqualTo("Second"));
        Session.CurrentAccountId = 1;
        Assert.That((await Tasks.GetTasksAsync()).Single().Id, Is.EqualTo(first.Id));
    }

    [Test]
    public void SignedOutSessionCannotReadOrCreateTasks()
    {
        Session.CurrentAccountId = null;
        Assert.ThrowsAsync<InvalidOperationException>(async () => await Tasks.GetTasksAsync());
        Assert.ThrowsAsync<InvalidOperationException>(async () => await AddTask());
    }

    [Test]
    public async Task OtherAccountCannotUpdateOrDeleteExistingTask()
    {
        var task = await AddTask("Original");
        Session.CurrentAccountId = 2;
        task.Text = "Tampered";
        await Tasks.UpdateAsync(task);
        await Tasks.DeleteAsync(task);
        Assert.That((await Reload(task.Id)).Text, Is.EqualTo("Original"));
        task.UserAccountId = 2;
        await Tasks.UpdateAsync(task);
        await Tasks.DeleteAsync(task);
        Assert.That((await Reload(task.Id)).UserAccountId, Is.EqualTo(1));
    }

    [Test]
    public async Task EditPersistsFieldsAndAddsRemovesAndReordersSteps()
    {
        var task = await AddTask(steps: [new TaskStep { Text = "Plan" }, new TaskStep { Text = "Build" }]);
        var draft = await Reload(task.Id);
        draft.Text = "Edited";
        draft.Description = "New details";
        draft.Priority = TaskPriority.Medium;
        draft.Category = TaskCategory.Study;
        draft.Steps.RemoveAt(0);
        draft.Steps.First().Text = " Build and test ";
        draft.Steps.Add(new TaskStep { Text = "Ship" });
        await Tasks.UpdateAsync(draft);
        var stored = await Reload(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That((stored.Text, stored.Description, stored.Priority, stored.Category),
                Is.EqualTo(("Edited", "New details", TaskPriority.Medium, (TaskCategory?)TaskCategory.Study)));
            Assert.That(stored.Steps.Select(step => step.Text), Is.EqualTo(new[] { "Build and test", "Ship" }));
            Assert.That(stored.Steps.Select(step => step.Position), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(draft.Steps.Last().Id, Is.GreaterThan(0));
            Assert.That(task.Steps.First().Text, Is.EqualTo("Plan"));
        });
    }

    [Test]
    public async Task ForeignStepInDraftRejectsWholeEditWithoutPartialWrites()
    {
        var task = await AddTask("Original", steps: [new TaskStep { Text = "Own step" }]);
        var other = await AddTask(steps: [new TaskStep { Text = "Foreign step" }]);
        var draft = await Reload(task.Id);
        draft.Text = "Should not save";
        draft.Steps.Clear();
        draft.Steps.Add(other.Steps.Single().Copy());
        Assert.ThrowsAsync<InvalidOperationException>(() => Tasks.UpdateAsync(draft));
        var stored = await Reload(task.Id);
        Assert.That(stored.Text, Is.EqualTo("Original"));
        Assert.That(stored.Steps.Single().Text, Is.EqualTo("Own step"));
    }

    [Test]
    public async Task StepCompletionPersistsWithoutCompletingParent()
    {
        var task = await AddTask(steps: [new TaskStep { Text = "Plan" }]);
        await Tasks.SetStepCompletedAsync(task, task.Steps.Single(), true);
        var stored = await Reload(task.Id);
        Assert.That(stored.Steps.Single().IsCompleted, Is.True);
        Assert.That(stored.IsCompleted, Is.False);
        Assert.That(task.StepsProgress, Does.StartWith("1 of 1"));
    }

    [Test]
    public async Task OtherAccountCannotToggleStep()
    {
        var task = await AddTask(steps: [new TaskStep { Text = "Plan" }]);
        Session.CurrentAccountId = 2;
        Assert.ThrowsAsync<InvalidOperationException>(() => Tasks.SetStepCompletedAsync(task, task.Steps.Single(), true));
        Assert.That((await Reload(task.Id)).Steps.Single().IsCompleted, Is.False);
    }

    [Test]
    public async Task StepFromDifferentTaskCannotBeToggled()
    {
        var first = await AddTask();
        var second = await AddTask(steps: [new TaskStep { Text = "Plan" }]);
        Assert.ThrowsAsync<InvalidOperationException>(() => Tasks.SetStepCompletedAsync(first, second.Steps.Single(), true));
        Assert.That((await Reload(second.Id)).Steps.Single().IsCompleted, Is.False);
    }

    [TestCase(false, null, 0)]
    [TestCase(true, TaskCategory.Work, 1)]
    [TestCase(true, null, 1)]
    public async Task ClearCompletedRespectsAccountCategoryAndCascadesSteps(bool filter, TaskCategory? category, int completedRemaining)
    {
        var work = await AddTask(category: TaskCategory.Work, steps: [new TaskStep { Text = "Work step" }]);
        var none = await AddTask(steps: [new TaskStep { Text = "Other step" }]);
        var active = await AddTask("Active");
        work.IsCompleted = none.IsCompleted = true;
        await Tasks.UpdateAsync(work);
        await Tasks.UpdateAsync(none);
        Session.CurrentAccountId = 2;
        var other = await AddTask("Other user", TaskCategory.Work);
        other.IsCompleted = true;
        await Tasks.UpdateAsync(other);
        Session.CurrentAccountId = 1;
        await Tasks.GetTasksAsync();
        await Tasks.ClearCompletedAsync(filter, category);
        var remaining = await Tasks.GetTasksAsync();
        Assert.That(remaining.Count(task => task.IsCompleted), Is.EqualTo(completedRemaining));
        Assert.That(remaining.Any(task => task.Id == active.Id), Is.True);
        Assert.That((await Reload(other.Id)).IsCompleted, Is.True);
        await using var db = Database.CreateDbContext();
        Assert.That(await db.Set<TaskStep>().CountAsync(), Is.EqualTo(completedRemaining));
    }

    [Test]
    public async Task DeletingTaskCascadesToAllItsSteps()
    {
        var task = await AddTask(steps: [new TaskStep { Text = "Plan" }, new TaskStep { Text = "Ship" }]);
        await Tasks.DeleteAsync(task);
        await using var db = Database.CreateDbContext();
        Assert.That(await db.Set<TaskStep>().CountAsync(), Is.Zero);
    }
}
