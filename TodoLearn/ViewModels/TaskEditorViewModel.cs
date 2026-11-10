using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TodoLearn.Models;
using TodoLearn.Resources.Strings;
using TodoLearn.Services;

namespace TodoLearn.ViewModels;

public sealed class TaskEditorViewModel : INotifyPropertyChanged
{
    private readonly ITaskWriter _writer;
    private readonly TimeProvider _clock;
    private TaskItem? _original;
    private string? _title;
    private string? _description;
    private string? _pendingStep;
    private string? _validationError;
    private int _priorityIndex;
    private int _categoryIndex;
    private DateTime? _dueDate;
    private TimeSpan? _dueTime;
    private bool _isSaving;

    public TaskEditorViewModel(ITaskWriter writer, TimeProvider? clock = null)
    {
        _writer = writer;
        _clock = clock ?? TimeProvider.System;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<TaskStep> Steps { get; } = new();
    public IReadOnlyList<string> PriorityOptions { get; } = [AppResources.PriorityLow, AppResources.PriorityMedium, AppResources.PriorityHigh];
    public IReadOnlyList<string> CategoryOptions => TaskCategories.Names;
    public bool IsEditing => _original is not null;
    public string Heading => IsEditing ? AppResources.EditTaskAction : AppResources.NewTaskSectionTitle;
    public string SaveText => IsEditing ? AppResources.SaveButton : AppResources.AddButton;
    public bool CanInteract => !IsSaving;
    public bool HasValidationError => ValidationError is not null;

    public string? Title
    {
        get => _title;
        set
        {
            if (Set(ref _title, value) && !string.IsNullOrWhiteSpace(value))
                ValidationError = null;
        }
    }
    public string? Description { get => _description; set => Set(ref _description, value); }
    public string? PendingStep { get => _pendingStep; set => Set(ref _pendingStep, value); }
    public int PriorityIndex { get => _priorityIndex; set => Set(ref _priorityIndex, value); }
    public int CategoryIndex { get => _categoryIndex; set => Set(ref _categoryIndex, value); }
    public DateTime? DueDate { get => _dueDate; set => Set(ref _dueDate, value); }
    public TimeSpan? DueTime { get => _dueTime; set => Set(ref _dueTime, value); }
    public string? ValidationError
    {
        get => _validationError;
        private set
        {
            if (Set(ref _validationError, value))
                Notify(nameof(HasValidationError));
        }
    }
    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (Set(ref _isSaving, value))
                Notify(nameof(CanInteract));
        }
    }

    public void BeginNew(string? title = null, TaskCategory? category = null)
    {
        EnsureNotSaving();
        _original = null;
        var now = _clock.GetLocalNow().DateTime;
        Initialize(title, null, TaskPriority.Low, now, category, []);
    }

    public void BeginEdit(TaskItem task)
    {
        EnsureNotSaving();
        _original = task;
        Initialize(task.Text, task.Description, task.Priority, task.DueAt, task.Category, task.Steps);
    }

    private void Initialize(string? title, string? description, TaskPriority priority, DateTime dueAt,
        TaskCategory? category, IEnumerable<TaskStep> steps)
    {
        Title = title;
        Description = description;
        PriorityIndex = (int)priority;
        CategoryIndex = category is { } value ? (int)value + 1 : 0;
        DueDate = dueAt.Date;
        DueTime = dueAt.TimeOfDay;
        PendingStep = null;
        ValidationError = null;
        Steps.Clear();
        foreach (var step in steps)
            Steps.Add(step.Copy());
        Notify(nameof(IsEditing));
        Notify(nameof(Heading));
        Notify(nameof(SaveText));
    }

    public bool AddStep()
    {
        if (IsSaving || string.IsNullOrWhiteSpace(PendingStep))
            return false;
        Steps.Add(new TaskStep { Text = PendingStep.Trim() });
        PendingStep = null;
        return true;
    }

    public void RemoveStep(TaskStep step)
    {
        if (!IsSaving)
            Steps.Remove(step);
    }

    public async Task<TaskItem?> SaveAsync()
    {
        if (IsSaving)
            return null;
        var title = Title?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            ValidationError = AppResources.TaskNameRequired;
            return null;
        }

        ValidationError = null;
        AddStep();
        var now = _clock.GetLocalNow().DateTime;
        var dueAt = (DueDate?.Date ?? now.Date) + (DueTime ?? now.TimeOfDay);
        var priority = Enum.IsDefined(typeof(TaskPriority), PriorityIndex) ? (TaskPriority)PriorityIndex : TaskPriority.Low;
        var category = CategoryIndex > 0 && CategoryIndex <= Enum.GetValues<TaskCategory>().Length
            ? (TaskCategory?)(CategoryIndex - 1) : null;
        var description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
        IsSaving = true;
        try
        {
            if (_original is null)
                return await _writer.AddAsync(title, description, priority, dueAt, category, Steps);

            var draft = CreateDraft(_original, title, description, priority, dueAt, category);
            await _writer.UpdateAsync(draft);
            ApplySavedDraft(_original, draft);
            return _original;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private TaskItem CreateDraft(TaskItem original, string title, string? description, TaskPriority priority,
        DateTime dueAt, TaskCategory? category)
    {
        var draft = new TaskItem
        {
            Id = original.Id, UserAccountId = original.UserAccountId, CreatedAt = original.CreatedAt,
            IsCompleted = original.IsCompleted, Text = title, Description = description,
            Priority = priority, DueAt = dueAt, Category = category,
            ReminderSentForDueAt = original.ReminderSentForDueAt,
            ReminderPendingForDueAt = original.ReminderPendingForDueAt,
            ReminderSnoozedUntil = original.ReminderSnoozedUntil,
            ReminderDismissedForDueAt = original.ReminderDismissedForDueAt
        };
        foreach (var step in Steps)
            draft.Steps.Add(step.Copy());
        return draft;
    }

    private static void ApplySavedDraft(TaskItem original, TaskItem draft)
    {
        original.Text = draft.Text;
        original.Description = draft.Description;
        original.Priority = draft.Priority;
        original.DueAt = draft.DueAt;
        original.Category = draft.Category;
        original.ReminderSentForDueAt = draft.ReminderSentForDueAt;
        original.ReminderPendingForDueAt = draft.ReminderPendingForDueAt;
        original.ReminderSnoozedUntil = draft.ReminderSnoozedUntil;
        original.ReminderDismissedForDueAt = draft.ReminderDismissedForDueAt;
        foreach (var step in original.Steps.ToList())
            original.Steps.Remove(step);
        foreach (var step in draft.Steps.Where(step => !string.IsNullOrWhiteSpace(step.Text)))
            original.Steps.Add(step.Copy());
    }

    private void EnsureNotSaving()
    {
        if (IsSaving)
            throw new InvalidOperationException("Wait for the current task to finish saving.");
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? property = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        Notify(property);
        return true;
    }

    private void Notify([CallerMemberName] string? property = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
}
