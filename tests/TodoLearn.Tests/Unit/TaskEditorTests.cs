using TodoLearn.Models;
using TodoLearn.Resources.Strings;
using TodoLearn.Services;
using TodoLearn.ViewModels;

namespace TodoLearn.Tests.Unit;

[TestFixture, Category("Unit")]
public sealed class TaskEditorTests
{
    private RecordingWriter _writer = null!;
    private TaskEditorViewModel _editor = null!;
    private TestClock _clock = null!;

    [SetUp]
    public void SetUp()
    {
        _writer = new RecordingWriter();
        _clock = new TestClock();
        _editor = new TaskEditorViewModel(_writer, _clock);
    }

    [Test]
    public void NewTaskUsesLocalTimeAndSelectedCategory()
    {
        _editor.BeginNew("Quick title", TaskCategory.Work);
        Assert.Multiple(() =>
        {
            Assert.That(_editor.IsEditing, Is.False);
            Assert.That(_editor.Title, Is.EqualTo("Quick title"));
            Assert.That(_editor.CategoryIndex, Is.EqualTo(1));
            Assert.That(_editor.PriorityIndex, Is.Zero);
            Assert.That(_editor.DueDate, Is.EqualTo(_clock.Now.Date));
            Assert.That(_editor.DueTime, Is.EqualTo(_clock.Now.TimeOfDay));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public async Task EmptyTitleShowsValidationWithoutWriting(string? title)
    {
        _editor.BeginNew(title);
        Assert.That(await _editor.SaveAsync(), Is.Null);
        Assert.That(_editor.ValidationError, Is.EqualTo(AppResources.TaskNameRequired));
        Assert.That(_writer.AddCalls, Is.Zero);
        _editor.Title = "Valid title";
        Assert.That(_editor.HasValidationError, Is.False);
    }

    [Test]
    public async Task CreationNormalizesInputAndIncludesPendingStepOnce()
    {
        _editor.BeginNew(" Plan ", TaskCategory.Study);
        _editor.Description = " Details ";
        _editor.PriorityIndex = 2;
        _editor.DueTime = new TimeSpan(23, 45, 0);
        _editor.PendingStep = " First step ";
        var saved = await _editor.SaveAsync();
        Assert.Multiple(() =>
        {
            Assert.That(saved!.Text, Is.EqualTo("Plan"));
            Assert.That(saved.Description, Is.EqualTo("Details"));
            Assert.That(saved.Priority, Is.EqualTo(TaskPriority.High));
            Assert.That(saved.Category, Is.EqualTo(TaskCategory.Study));
            Assert.That(saved.DueAt, Is.EqualTo(_clock.Now.Date.AddHours(23).AddMinutes(45)));
            Assert.That(saved.Steps.Single().Text, Is.EqualTo("First step"));
            Assert.That(_editor.PendingStep, Is.Null);
            Assert.That(_editor.IsSaving, Is.False);
        });
    }

    [TestCase(-1)]
    [TestCase(100)]
    public async Task InvalidPickerSelectionsUseSafeDefaults(int index)
    {
        _editor.BeginNew("Task");
        _editor.PriorityIndex = _editor.CategoryIndex = index;
        _editor.Description = " ";
        _editor.DueDate = null;
        _editor.DueTime = null;
        var saved = await _editor.SaveAsync();
        Assert.That(saved!.Priority, Is.EqualTo(TaskPriority.Low));
        Assert.That(saved.Category, Is.Null);
        Assert.That(saved.Description, Is.Null);
        Assert.That(saved.DueAt, Is.EqualTo(_clock.Now));
    }

    [Test]
    public void EditingAndDiscardingDraftDoNotMutateOriginal()
    {
        var original = Original();
        _editor.BeginEdit(original);
        _editor.Title = "Draft title";
        _editor.Steps[0].Text = "Draft step";
        _editor.Steps[0].IsCompleted = true;
        _editor.BeginNew();
        Assert.That(original.Text, Is.EqualTo("Original"));
        Assert.That(original.Steps[0].Text, Is.EqualTo("Plan"));
        Assert.That(original.Steps[0].IsCompleted, Is.False);
        Assert.That(_editor.Steps, Is.Empty);
        Assert.That(_editor.IsEditing, Is.False);
    }

    [Test]
    public async Task EditPreservesIdentityAndAppliesPersistedChangesToSameInstance()
    {
        var original = Original();
        _editor.BeginEdit(original);
        _editor.Title = "Updated";
        _editor.Steps[0].Text = "Build";
        _editor.PendingStep = "Ship";
        _writer.OnUpdate = draft =>
        {
            Assert.That(original.Text, Is.EqualTo("Original"));
            Assert.That(draft, Is.Not.SameAs(original));
            Assert.That(draft.Steps[0], Is.Not.SameAs(original.Steps[0]));
            draft.ReminderPendingForDueAt = null;
            draft.Steps.Last().Id = 42;
            return Task.CompletedTask;
        };
        Assert.That(await _editor.SaveAsync(), Is.SameAs(original));
        Assert.Multiple(() =>
        {
            Assert.That(original.Text, Is.EqualTo("Updated"));
            Assert.That((original.Id, original.UserAccountId, original.CreatedAt, original.IsCompleted),
                Is.EqualTo((7, (int?)1, _clock.Now.AddDays(-1), true)));
            Assert.That(original.ReminderPendingForDueAt, Is.Null);
            Assert.That(original.Steps.Last().Id, Is.EqualTo(42));
            Assert.That(_writer.AddCalls, Is.Zero);
            Assert.That(_writer.UpdateCalls, Is.EqualTo(1));
        });
        _editor.Steps[0].Text = "Unsaved later change";
        Assert.That(original.Steps[0].Text, Is.EqualTo("Build"));
    }

    [Test]
    public async Task FailedEditKeepsOriginalAndAllowsRetryWithDraftIntact()
    {
        var original = Original();
        _editor.BeginEdit(original);
        _editor.Title = "Changed";
        _editor.Steps[0].Text = "Changed step";
        _writer.OnUpdate = _ => throw new IOException("Storage unavailable");
        Assert.ThrowsAsync<IOException>(() => _editor.SaveAsync());
        Assert.That(original.Text, Is.EqualTo("Original"));
        Assert.That(original.Steps[0].Text, Is.EqualTo("Plan"));
        Assert.That(_editor.IsSaving, Is.False);
        Assert.That(_editor.Title, Is.EqualTo("Changed"));
        _writer.OnUpdate = null;
        await _editor.SaveAsync();
        Assert.That(original.Steps[0].Text, Is.EqualTo("Changed step"));
    }

    [Test]
    public async Task PendingSavePreventsDuplicateWritesAndChangingDraft()
    {
        _editor.BeginNew("Task");
        var completion = new TaskCompletionSource<TaskItem>();
        _writer.AddResult = completion.Task;
        var save = _editor.SaveAsync();
        Assert.That(_editor.CanInteract, Is.False);
        Assert.That(await _editor.SaveAsync(), Is.Null);
        Assert.Throws<InvalidOperationException>(() => _editor.BeginNew());
        Assert.Throws<InvalidOperationException>(() => _editor.BeginEdit(Original()));
        _editor.PendingStep = "Later";
        Assert.That(_editor.AddStep(), Is.False);
        Assert.That(_writer.AddCalls, Is.EqualTo(1));
        completion.SetResult(new TaskItem { Text = "Task" });
        await save;
        Assert.That(_editor.CanInteract, Is.True);
    }

    [Test]
    public async Task FailedCreationRetainsPendingStepWithoutDuplicatingItOnRetry()
    {
        _editor.BeginNew("Task");
        _editor.PendingStep = "Plan";
        _writer.AddResult = Task.FromException<TaskItem>(new IOException());
        Assert.ThrowsAsync<IOException>(() => _editor.SaveAsync());
        Assert.That(_editor.Steps, Has.Count.EqualTo(1));
        _writer.AddResult = null;
        var saved = await _editor.SaveAsync();
        Assert.That(saved!.Steps, Has.Count.EqualTo(1));
    }

    [Test]
    public void StepActionsIgnoreWhitespaceAndRemoveOnlyDraftStep()
    {
        _editor.BeginEdit(Original());
        _editor.PendingStep = " ";
        Assert.That(_editor.AddStep(), Is.False);
        _editor.RemoveStep(_editor.Steps[0]);
        Assert.That(_editor.Steps, Is.Empty);
    }

    private TaskItem Original()
    {
        var task = new TaskItem
        {
            Id = 7, UserAccountId = 1, Text = "Original", IsCompleted = true,
            CreatedAt = _clock.Now.AddDays(-1), DueAt = _clock.Now.AddDays(1),
            ReminderPendingForDueAt = _clock.Now.AddDays(1)
        };
        task.Steps.Add(new TaskStep { Id = 1, Text = "Plan" });
        return task;
    }

    private sealed class RecordingWriter : ITaskWriter
    {
        public int AddCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public Task<TaskItem>? AddResult { get; set; }
        public Func<TaskItem, Task>? OnUpdate { get; set; }

        public Task<TaskItem> AddAsync(string text, string? description, TaskPriority priority, DateTime dueAt,
            TaskCategory? category = null, IEnumerable<TaskStep>? steps = null)
        {
            AddCalls++;
            var task = new TaskItem { Text = text, Description = description, Priority = priority, DueAt = dueAt, Category = category };
            foreach (var step in steps ?? []) task.Steps.Add(step.Copy());
            return AddResult ?? Task.FromResult(task);
        }

        public Task UpdateAsync(TaskItem task)
        {
            UpdateCalls++;
            return OnUpdate?.Invoke(task) ?? Task.CompletedTask;
        }
    }
}
