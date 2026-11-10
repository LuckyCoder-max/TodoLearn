using TodoLearn.Models;

namespace TodoLearn.Tests.Integration;

[TestFixture, Category("Integration")]
public sealed class ReminderTests : DatabaseTest
{
    [TestCase(-1, false)]
    [TestCase(0, false)]
    [TestCase(1, true)]
    [TestCase(10, true)]
    [TestCase(11, false)]
    public async Task InitialReminderUsesTenMinuteWindow(int minutes, bool expected)
    {
        var task = await AddTask();
        task.DueAt = Clock.Now.AddMinutes(minutes);
        await Tasks.UpdateAsync(task);
        Assert.That(await Tasks.MarkReminderPendingAsync(task, task.DueAt, Clock.Now), Is.EqualTo(expected));
        var stored = await Reload(task.Id);
        Assert.That(stored.ReminderPendingForDueAt, Is.EqualTo(expected ? task.DueAt : (DateTime?)null));
    }

    [Test]
    public async Task PendingReminderIsNotDeliveredTwiceAfterReload()
    {
        var task = await AddTask();
        Assert.That(await Tasks.MarkReminderPendingAsync(task, task.DueAt, Clock.Now), Is.True);
        Assert.That(await Tasks.MarkReminderPendingAsync(await Reload(task.Id), task.DueAt, Clock.Now), Is.False);
    }

    [Test]
    public async Task DismissPersistsWithoutCompletingTaskAndPreventsRepeat()
    {
        var task = await AddTask();
        await Tasks.MarkReminderPendingAsync(task, task.DueAt, Clock.Now);
        Assert.That(await Tasks.DismissReminderAsync(task), Is.True);
        var stored = await Reload(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That(stored.IsCompleted, Is.False);
            Assert.That(stored.ReminderDismissedForDueAt, Is.EqualTo(task.DueAt));
            Assert.That(stored.ReminderPendingForDueAt, Is.Null);
            Assert.That(stored.ReminderSnoozedUntil, Is.Null);
        });
        Assert.That(await Tasks.MarkReminderPendingAsync(stored, task.DueAt, Clock.Now), Is.False);
    }

    [TestCase(5)]
    [TestCase(15)]
    [TestCase(30)]
    [TestCase(60)]
    public async Task SnoozedReminderReturnsAtSelectedTimeEvenAfterDeadline(int minutes)
    {
        var task = await AddTask();
        await Tasks.MarkReminderPendingAsync(task, task.DueAt, Clock.Now);
        Assert.That(await Tasks.SnoozeReminderAsync(task, TimeSpan.FromMinutes(minutes)), Is.True);
        var stored = await Reload(task.Id);
        Assert.That(stored.ReminderPendingForDueAt, Is.Null);
        Assert.That(stored.ReminderSnoozedUntil, Is.EqualTo(Clock.Now.AddMinutes(minutes)));
        Clock.Advance(TimeSpan.FromMinutes(minutes).Subtract(TimeSpan.FromSeconds(1)));
        Assert.That(await Tasks.MarkReminderPendingAsync(stored, task.DueAt, Clock.Now), Is.False);
        Clock.Advance(TimeSpan.FromSeconds(1));
        Assert.That(await Tasks.MarkReminderPendingAsync(stored, task.DueAt, Clock.Now), Is.True);
        Assert.That((await Reload(task.Id)).ReminderSnoozedUntil, Is.Null);
    }

    [TestCase(0)]
    [TestCase(-5)]
    public async Task NonpositiveSnoozeIsRejectedWithoutChangingPendingReminder(int minutes)
    {
        var task = await AddTask();
        await Tasks.MarkReminderPendingAsync(task, task.DueAt, Clock.Now);
        Assert.That(await Tasks.SnoozeReminderAsync(task, TimeSpan.FromMinutes(minutes)), Is.False);
        Assert.That((await Reload(task.Id)).ReminderPendingForDueAt, Is.EqualTo(task.DueAt));
    }

    [Test]
    public async Task SnoozeAndDismissRequirePendingReminder()
    {
        var task = await AddTask();
        Assert.That(await Tasks.SnoozeReminderAsync(task, TimeSpan.FromMinutes(5)), Is.False);
        Assert.That(await Tasks.DismissReminderAsync(task), Is.False);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task CompletionOrDeadlineChangeResetsReminderState(bool complete)
    {
        var task = await AddTask();
        await Tasks.MarkReminderPendingAsync(task, task.DueAt, Clock.Now);
        await Tasks.SnoozeReminderAsync(task, TimeSpan.FromMinutes(5));
        if (complete) task.IsCompleted = true;
        else task.DueAt = Clock.Now.AddMinutes(8);
        await Tasks.UpdateAsync(task);
        var stored = await Reload(task.Id);
        Assert.Multiple(() =>
        {
            Assert.That(stored.ReminderSentForDueAt, Is.Null);
            Assert.That(stored.ReminderPendingForDueAt, Is.Null);
            Assert.That(stored.ReminderSnoozedUntil, Is.Null);
            Assert.That(stored.ReminderDismissedForDueAt, Is.Null);
        });
        Assert.That(await Tasks.MarkReminderPendingAsync(task, task.DueAt, Clock.Now), Is.EqualTo(!complete));
    }

    [Test]
    public async Task StaleDeadlineCannotTriggerReminder()
    {
        var task = await AddTask();
        Assert.That(await Tasks.MarkReminderPendingAsync(task, task.DueAt.AddMinutes(-1), Clock.Now), Is.False);
    }

    [Test]
    public async Task OtherAccountCannotTriggerSnoozeOrDismissReminder()
    {
        var task = await AddTask();
        await Tasks.MarkReminderPendingAsync(task, task.DueAt, Clock.Now);
        Session.CurrentAccountId = 2;
        Assert.That(await Tasks.MarkReminderPendingAsync(task, task.DueAt, Clock.Now), Is.False);
        Assert.That(await Tasks.SnoozeReminderAsync(task, TimeSpan.FromMinutes(5)), Is.False);
        Assert.That(await Tasks.DismissReminderAsync(task), Is.False);
        Assert.That((await Reload(task.Id)).ReminderPendingForDueAt, Is.EqualTo(task.DueAt));
    }
}
