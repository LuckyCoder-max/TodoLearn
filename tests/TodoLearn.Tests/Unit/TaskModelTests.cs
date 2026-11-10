using TodoLearn.Models;
using TodoLearn.Resources.Strings;

namespace TodoLearn.Tests.Unit;

[TestFixture, Category("Unit")]
public sealed class TaskModelTests
{
    [Test]
    public void ChangingDatePreservesTime()
    {
        var task = new TaskItem { DueAt = new DateTime(2026, 11, 1, 23, 45, 0) };
        task.DueDate = new DateTime(2026, 12, 5, 8, 0, 0);
        Assert.That(task.DueAt, Is.EqualTo(new DateTime(2026, 12, 5, 23, 45, 0)));
    }

    [Test]
    public void ChangingTimePreservesDateAndSupports24Hours()
    {
        var task = new TaskItem { DueAt = new DateTime(2026, 11, 1, 9, 0, 0) };
        task.DueTime = new TimeSpan(23, 59, 0);
        Assert.That(task.DueAt, Is.EqualTo(new DateTime(2026, 11, 1, 23, 59, 0)));
    }

    [TestCase(-1, null)]
    [TestCase(0, null)]
    [TestCase(1, TaskCategory.Work)]
    [TestCase(5, TaskCategory.Personal)]
    [TestCase(6, null)]
    public void CategoryPickerMapsValidAndInvalidIndices(int index, TaskCategory? category)
    {
        var task = new TaskItem { CategoryIndex = index };
        Assert.Multiple(() =>
        {
            Assert.That(task.Category, Is.EqualTo(category));
            Assert.That(task.CategoryName, Is.Not.Empty);
        });
    }

    [TestCase(null, false)]
    [TestCase(" ", false)]
    [TestCase("Description", true)]
    public void DescriptionVisibilityRequiresContent(string? description, bool visible) =>
        Assert.That(new TaskItem { Description = description }.HasDescription, Is.EqualTo(visible));

    [Test]
    public void DescriptionToggleTracksOverflowAndExpandedState()
    {
        var task = new TaskItem { Description = "Long text" };
        Assert.That(task.ShowDescriptionToggle, Is.False);
        task.CanExpandDescription = true;
        Assert.That(task.ShowDescriptionToggle, Is.True);
        task.IsDescriptionExpanded = true;
        task.CanExpandDescription = false;
        Assert.That(task.ExpandToggleText, Is.EqualTo(AppResources.CollapseDescription));
        Assert.That(task.ShowDescriptionToggle, Is.True);
        task.IsDescriptionExpanded = false;
        Assert.That(task.ShowDescriptionToggle, Is.False);
    }

    [Test]
    public void DescriptionChangeRequiresOverflowRemeasurement()
    {
        var task = new TaskItem { Description = "Old", CanExpandDescription = true };
        task.Description = "New";
        Assert.That(task.CanExpandDescription, Is.False);
    }

    [Test]
    public void StepCopyPreservesDataWithoutSharingEdits()
    {
        var original = new TaskStep { Id = 3, TaskItemId = 7, Position = 2, Text = "Plan", IsCompleted = true };
        var copy = original.Copy();
        Assert.Multiple(() =>
        {
            Assert.That(copy, Is.Not.SameAs(original));
            Assert.That((copy.Id, copy.TaskItemId, copy.Position, copy.Text, copy.IsCompleted),
                Is.EqualTo((3, 7, 2, "Plan", true)));
        });
        copy.Text = "Changed";
        copy.IsCompleted = false;
        Assert.That(original.Text, Is.EqualTo("Plan"));
        Assert.That(original.IsCompleted, Is.True);
    }

    [Test]
    public void StepCompletionUpdatesProgressWithoutCompletingParent()
    {
        var task = new TaskItem();
        var step = new TaskStep { Text = "Plan" };
        task.Steps.Add(step);
        task.Steps.Add(new TaskStep { Text = "Ship" });
        var changes = new List<string?>();
        task.PropertyChanged += (_, args) => changes.Add(args.PropertyName);
        step.IsCompleted = true;
        Assert.Multiple(() =>
        {
            Assert.That(task.StepsProgress, Does.StartWith("1 of 2"));
            Assert.That(changes, Does.Contain(nameof(TaskItem.StepsProgress)));
            Assert.That(task.IsCompleted, Is.False);
        });
    }

    [Test]
    public void RemovingStepStopsProgressNotificationsFromThatStep()
    {
        var task = new TaskItem();
        var step = new TaskStep();
        task.Steps.Add(step);
        task.Steps.Remove(step);
        var changes = new List<string?>();
        task.PropertyChanged += (_, args) => changes.Add(args.PropertyName);
        step.IsCompleted = true;
        Assert.That(changes, Is.Empty);
        Assert.That(task.HasSteps, Is.False);
    }

    [Test]
    public void CategoryChangeNotifiesAllDependentBindings()
    {
        var task = new TaskItem();
        var changes = new List<string?>();
        task.PropertyChanged += (_, args) => changes.Add(args.PropertyName);
        task.Category = TaskCategory.Work;
        Assert.That(changes, Is.EquivalentTo(new[] { "Category", "CategoryIndex", "CategoryName" }));
        changes.Clear();
        task.Category = TaskCategory.Work;
        Assert.That(changes, Is.Empty);
    }
}
