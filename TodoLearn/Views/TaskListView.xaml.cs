using System.Collections.ObjectModel;
using TodoLearn.Models;
using TodoLearn.Resources.Strings;
using TodoLearn.Services;

namespace TodoLearn.Views;

public partial class TaskListView : ContentView
{
    private readonly TaskService _taskService;
    private readonly Dictionary<TaskItem, TaskBackup> _editBackups = new();

    public ObservableCollection<TaskItem> Tasks { get; } = new();
    public ObservableCollection<TaskItem> DisplayTasks { get; } = new();
    public IReadOnlyList<string> PriorityOptions { get; } = new[]
    {
        AppResources.PriorityLow,
        AppResources.PriorityMedium,
        AppResources.PriorityHigh
    };

    public FilterType Filter { get; set; } = FilterType.MyDay;

    public IReadOnlyList<string> CategoryOptions => TaskCategories.Names;
    private bool _categoryView;
    private TaskCategory? _selectedCategory;

    public void SetCategory(TaskCategory? category)
    {
        _categoryView = true;
        _selectedCategory = category;
        TitleLabel.Text = CategoryOptions[category is { } value ? (int)value + 1 : 0];
        NewTaskCategoryPicker.SelectedIndex = category is { } selected ? (int)selected + 1 : 0;
        RefreshDisplay();
    }

    public TaskListView()
    {
        InitializeComponent();
        _taskService = IPlatformApplication.Current?.Services.GetService<TaskService>()
            ?? throw new InvalidOperationException(AppResources.TaskServiceRequired);
        BindingContext = this;
        TitleLabel.Text = GetTitle(Filter);
        NewTaskDatePicker.Date = DateTime.Today;
        NewTaskTimePicker.Time = DateTime.Now.TimeOfDay;
    }

    public async Task LoadTasksAsync()
    {
        Tasks.Clear();
        foreach (var task in await _taskService.GetTasksAsync())
            Tasks.Add(task);

        RefreshDisplay();
    }

    public void SetFilter(FilterType filter)
    {
        Filter = filter;
        TitleLabel.Text = GetTitle(filter);
        RefreshDisplay();
    }

    private async void OnAddTaskClicked(object? sender, EventArgs e)
    {
        var text = NewTaskEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        var description = NewTaskDescriptionEditor.Text?.Trim();
        var priority = Enum.IsDefined(typeof(TaskPriority), NewTaskPriorityPicker.SelectedIndex)
            ? (TaskPriority)NewTaskPriorityPicker.SelectedIndex
            : TaskPriority.Low;
        var dueAt = (NewTaskDatePicker.Date?.Date ?? DateTime.Today)
            + (NewTaskTimePicker.Time ?? DateTime.Now.TimeOfDay);

        TaskCategory? category = NewTaskCategoryPicker.SelectedIndex > 0 ? (TaskCategory)(NewTaskCategoryPicker.SelectedIndex - 1) : null;
        var task = await _taskService.AddAsync(text, description, priority, dueAt, category);
        Tasks.Add(task);
        NewTaskEntry.Text = string.Empty;
        NewTaskDescriptionEditor.Text = string.Empty;
        NewTaskPriorityPicker.SelectedIndex = 0;
        NewTaskDatePicker.Date = DateTime.Today;
        NewTaskTimePicker.Time = DateTime.Now.TimeOfDay;
        RefreshDisplay();
        SimpleLogger.Log(string.Format(AppResources.TaskAddedLog, task.Text, task.Id));
    }

    private async void OnTaskCompletionChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (sender is not CheckBox { BindingContext: TaskItem task })
            return;

        if (task.IsCompleted == e.Value)
            return;

        var previousValue = task.IsCompleted;
        task.IsCompleted = e.Value;
        try
        {
            await _taskService.UpdateAsync(task);
            RefreshDisplay();
        }
        catch
        {
            task.IsCompleted = previousValue;
            RefreshDisplay();

            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is not null)
            {
                await page.DisplayAlertAsync(
                    AppResources.ClearCompletedFailedTitle,
                    AppResources.ClearCompletedFailedMessage,
                    AppResources.OkButton);
            }
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: TaskItem task })
            return;

        await _taskService.DeleteAsync(task);
        Tasks.Remove(task);
        RefreshDisplay();
        SimpleLogger.Log(string.Format(AppResources.TaskDeletedLog, task.Text, task.Id));
    }

    private async void OnClearCompletedClicked(object? sender, EventArgs e)
    {
        try
        {
            await _taskService.ClearCompletedAsync(_categoryView, _selectedCategory);
            foreach (var task in Tasks.Where(task => task.IsCompleted && (!_categoryView || task.Category == _selectedCategory)).ToList())
                Tasks.Remove(task);
            RefreshDisplay();
        }
        catch
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is not null)
            {
                await page.DisplayAlertAsync(
                    AppResources.TaskSaveFailedTitle,
                    AppResources.TaskSaveFailedMessage,
                    AppResources.OkButton);
            }
        }
    }

    private void OnTaskDoubleTapped(object? sender, EventArgs e)
    {
        if (sender is not BindableObject { BindingContext: TaskItem task })
            return;

        if (!_editBackups.ContainsKey(task))
        {
            _editBackups[task] = new TaskBackup(
                task.Text,
                task.Description,
                task.DueAt,
                task.Priority,
                task.IsCompleted,
                task.Category);
        }

        task.IsEditing = true;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: TaskItem task })
            return;

        await _taskService.UpdateAsync(task);
        _editBackups.Remove(task);
        task.IsEditing = false;
        SimpleLogger.Log(string.Format(AppResources.TaskEditedLog, task.Text, task.Id));
        RefreshDisplay();
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: TaskItem task })
            return;

        if (_editBackups.Remove(task, out var backup))
        {
            task.Text = backup.Text;
            task.Description = backup.Description;
            task.DueAt = backup.DueAt;
            task.Priority = backup.Priority;
            task.IsCompleted = backup.IsCompleted;
            task.Category = backup.Category;
        }

        task.IsEditing = false;
    }

    private void OnDescriptionToggleTapped(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: TaskItem task } && task.ShowDescriptionToggle)
            task.IsDescriptionExpanded = !task.IsDescriptionExpanded;
    }

    private void OnDescriptionLabelSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not Label { BindingContext: TaskItem task } label)
            return;

        if (task.IsDescriptionExpanded)
            return;

        if (!task.HasDescription || label.Width <= 0)
        {
            task.CanExpandDescription = false;
            return;
        }

        var naturalSize = label.Measure(double.PositiveInfinity, double.PositiveInfinity);
        task.CanExpandDescription = naturalSize.Width > label.Width + 1;
    }

    private void OnDescriptionLabelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is Label label && e.PropertyName == Label.TextProperty.PropertyName)
            label.Dispatcher.Dispatch(() => OnDescriptionLabelSizeChanged(label, EventArgs.Empty));
    }

    private void OnPrioritySelectClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: TaskItem task, CommandParameter: string value })
            return;

        if (int.TryParse(value, out var index) && Enum.IsDefined(typeof(TaskPriority), index))
            task.Priority = (TaskPriority)index;
    }

    private void RefreshDisplay()
    {
        IEnumerable<TaskItem> filteredTasks = Filter switch
        {
            FilterType.Important => Tasks.Where(task => task.Priority == TaskPriority.High),
            FilterType.Planned => Tasks.Where(task => task.DueAt > DateTime.Now),
            FilterType.All => Tasks,
            _ => Tasks.Where(task => task.DueAt.Date == DateTime.Now.Date)
        };

        if (_categoryView)
            filteredTasks = Tasks.Where(task => task.Category == _selectedCategory);

        DisplayTasks.Clear();
        foreach (var task in filteredTasks.OrderBy(task => task.IsCompleted).ThenBy(task => task.DueAt))
            DisplayTasks.Add(task);

        ClearCompletedButton.IsEnabled = Tasks.Any(task => task.IsCompleted && (!_categoryView || task.Category == _selectedCategory));
    }

    private static string GetTitle(FilterType filter) => filter switch
    {
        FilterType.Important => AppResources.ImportantPageTitle,
        FilterType.Planned => AppResources.PlannedPageTitle,
        FilterType.All => AppResources.AllTasksPageTitle,
        _ => AppResources.MyDay
    };

    private sealed record TaskBackup(
        string? Text,
        string? Description,
        DateTime DueAt,
        TaskPriority Priority,
        bool IsCompleted,
        TaskCategory? Category);
}
