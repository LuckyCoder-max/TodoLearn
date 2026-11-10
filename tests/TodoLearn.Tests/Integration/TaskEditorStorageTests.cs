using TodoLearn.Models;
using TodoLearn.ViewModels;

namespace TodoLearn.Tests.Integration;

[TestFixture, Category("Integration")]
public sealed class TaskEditorStorageTests : DatabaseTest
{
    [Test]
    public async Task EditorCreatesAndEditsTaskThroughRealStorageWithoutReplacingCachedReference()
    {
        var editor = new TaskEditorViewModel(Tasks, Clock);
        await Tasks.GetTasksAsync();
        editor.BeginNew("Roadmap", TaskCategory.Work);
        editor.PendingStep = "Plan";
        var task = (await editor.SaveAsync())!;
        Assert.That((await Tasks.GetTasksAsync()).Single(), Is.SameAs(task));
        editor.BeginEdit(task);
        editor.Title = "Updated roadmap";
        editor.Steps[0].Text = "Plan and build";
        editor.PendingStep = "Ship";
        await editor.SaveAsync();
        var stored = await Reload(task.Id);
        Assert.That(stored.Text, Is.EqualTo("Updated roadmap"));
        Assert.That(stored.Category, Is.EqualTo(TaskCategory.Work));
        Assert.That(stored.Steps.Select(step => step.Text), Is.EqualTo(new[] { "Plan and build", "Ship" }));
        Assert.That(task.Steps.All(step => step.Id > 0), Is.True);
        Assert.That((await Tasks.GetTasksAsync()).Single(), Is.SameAs(task));
        Assert.That(task.Text, Is.EqualTo(stored.Text));
    }

    [Test]
    public async Task ChangingDeadlineThroughEditorResetsPersistedReminderState()
    {
        var task = await AddTask();
        await Tasks.MarkReminderPendingAsync(task, task.DueAt, Clock.Now);
        var editor = new TaskEditorViewModel(Tasks, Clock);
        editor.BeginEdit(task);
        editor.DueDate = Clock.Now.AddDays(1);
        await editor.SaveAsync();
        var stored = await Reload(task.Id);
        Assert.That(stored.ReminderPendingForDueAt, Is.Null);
        Assert.That(task.ReminderPendingForDueAt, Is.Null);
        Assert.That(stored.DueAt.Date, Is.EqualTo(Clock.Now.AddDays(1).Date));
    }

    [Test]
    public async Task BulkStepEditsPreserveOrderAndIdentityAfterRemovalAndAppend()
    {
        var task = await AddTask(steps: Enumerable.Range(0, 100).Select(index => new TaskStep { Text = $"Step {index}" }));
        var draft = await Reload(task.Id);
        var retained = draft.Steps.Skip(50).Reverse().ToList();
        draft.Steps.Clear();
        foreach (var step in retained)
            draft.Steps.Add(step);
        draft.Steps.Add(new TaskStep { Text = "Ship" });
        await Tasks.UpdateAsync(draft);
        var stored = await Reload(task.Id);
        Assert.That(stored.Steps.Select(step => step.Text), Is.EqualTo(retained.Select(step => step.Text).Append("Ship")));
        Assert.That(stored.Steps.Take(50).Select(step => step.Id), Is.EqualTo(retained.Select(step => step.Id)));
        Assert.That(stored.Steps.Select(step => step.Position), Is.EqualTo(Enumerable.Range(0, 51)));
    }
}
