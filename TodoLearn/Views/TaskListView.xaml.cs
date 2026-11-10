using System.Collections.ObjectModel;
using TodoLearn.Models;
using TodoLearn.Resources.Strings;
using TodoLearn.Services;

namespace TodoLearn.Views;

public partial class TaskListView : ContentView
{
    private readonly TaskService _taskService;
    private TaskItem? _editingTask;
    public string ComposerTitle => _editingTask is null ? AppResources.NewTaskSectionTitle : AppResources.EditTaskAction;
    public string ComposerSaveText => _editingTask is null ? AppResources.AddButton : AppResources.SaveButton;
    private bool _isComposerExpanded;
    public bool IsComposerExpanded
    {
        get => _isComposerExpanded;
        private set
        {
            _isComposerExpanded = value;
            OnPropertyChanged();
        }
    }

    public string TodayText => DateTime.Today.ToString("dddd, d MMMM", System.Globalization.CultureInfo.CurrentCulture);

    private void OnComposerToggleClicked(object? sender, EventArgs e)
    {
        if (!CreateTaskButton.IsEnabled)
            return;
        if (IsComposerExpanded)
        {
            CloseComposer();
            return;
        }
        _editingTask = null;
        NewTaskEntry.Text = QuickTaskEntry.Text;
        NewTaskDescriptionEditor.Text = string.Empty;
        NewTaskPriorityPicker.SelectedIndex = 0;
        NewTaskDatePicker.Date = DateTime.Today;
        NewTaskTimePicker.Time = DateTime.Now.TimeOfDay;
        NewTaskCategoryPicker.SelectedIndex = _categoryView && _selectedCategory is { } category ? (int)category + 1 : 0;
        OpenComposer();
    }

    private void OpenComposer()
    {
        ValidationLabel.IsVisible = false;
        OnPropertyChanged(nameof(ComposerTitle));
        OnPropertyChanged(nameof(ComposerSaveText));
        IsComposerExpanded = true;
        QuickComposer.IsEnabled = false;
        TaskCollection.IsEnabled = false;
        Dispatcher.Dispatch(() => NewTaskEntry.Focus());
    }

    private void CloseComposer()
    {
        IsComposerExpanded = false;
        _editingTask = null;
        QuickComposer.IsEnabled = true;
        TaskCollection.IsEnabled = true;
        QuickTaskEntry.Focus();
    }

#if WINDOWS
    private Microsoft.UI.Xaml.UIElement? _keyboardHost;
    private readonly List<Microsoft.UI.Xaml.Input.KeyboardAccelerator> _shortcuts = new();

    private void AttachShortcuts(object? sender, EventArgs e)
    {
        if (Handler?.PlatformView is not Microsoft.UI.Xaml.UIElement host || _keyboardHost == host)
            return;
        DetachShortcuts(sender, e);
        _keyboardHost = host;
        AddShortcut(Windows.System.VirtualKey.Escape, Windows.System.VirtualKeyModifiers.None, false);
        AddShortcut(Windows.System.VirtualKey.Enter, Windows.System.VirtualKeyModifiers.Control, true);
    }

    private void AddShortcut(Windows.System.VirtualKey key, Windows.System.VirtualKeyModifiers modifiers, bool save)
    {
        var shortcut = new Microsoft.UI.Xaml.Input.KeyboardAccelerator { Key = key, Modifiers = modifiers };
        shortcut.Invoked += (_, args) =>
        {
            if (!IsComposerExpanded)
                return;
            args.Handled = true;
            if (!CreateTaskButton.IsEnabled)
                return;
            if (save)
                OnAddTaskClicked(this, EventArgs.Empty);
            else
                CloseComposer();
        };
        _keyboardHost!.KeyboardAccelerators.Add(shortcut);
        _shortcuts.Add(shortcut);
    }

    private void DetachShortcuts(object? sender, EventArgs e)
    {
        if (_keyboardHost is not null)
            foreach (var shortcut in _shortcuts)
                _keyboardHost.KeyboardAccelerators.Remove(shortcut);
        _shortcuts.Clear();
        _keyboardHost = null;
    }
#endif

    public ObservableCollection<TaskItem> Tasks { get; } = new();
    public ObservableCollection<TaskItem> DisplayTasks { get; private set; } = new();
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
        SizeChanged += (_, _) => ListLayout.WidthRequest = Math.Max(0, Math.Min(960, Width - 48));
#if WINDOWS
        Loaded += AttachShortcuts;
        Unloaded += DetachShortcuts;
#endif
        TitleLabel.Text = GetTitle(Filter);
        NewTaskDatePicker.Date = DateTime.Today;
        NewTaskTimePicker.Time = DateTime.Now.TimeOfDay;
    }

    public async Task LoadTasksAsync()
    {
        var tasks = await _taskService.GetTasksAsync();
        if (!Tasks.SequenceEqual(tasks))
        {
            Tasks.Clear();
            foreach (var task in tasks)
                Tasks.Add(task);
        }

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
        if (!CreateTaskButton.IsEnabled)
            return;
        var text = NewTaskEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            ValidationLabel.Text = AppResources.TaskNameRequired;
            ValidationLabel.IsVisible = true;
            NewTaskEntry.Focus();
            return;
        }
        ValidationLabel.IsVisible = false;

        var description = NewTaskDescriptionEditor.Text?.Trim();
        var priority = Enum.IsDefined(typeof(TaskPriority), NewTaskPriorityPicker.SelectedIndex)
            ? (TaskPriority)NewTaskPriorityPicker.SelectedIndex
            : TaskPriority.Low;
        var dueAt = (NewTaskDatePicker.Date?.Date ?? DateTime.Today)
            + (NewTaskTimePicker.Time ?? DateTime.Now.TimeOfDay);

        var categoryIndex = NewTaskCategoryPicker.SelectedIndex;
        TaskCategory? category = categoryIndex > 0 && categoryIndex < CategoryOptions.Count
            ? (TaskCategory)(categoryIndex - 1) : null;
        TaskItem task;
        CreateTaskButton.IsEnabled = false;
        try
        {
            if (_editingTask is null)
            {
                task = await _taskService.AddAsync(text, description, priority, dueAt, category);
                Tasks.Add(task);
            }
            else
            {
                var original = _editingTask;
                var draft = new TaskItem
                {
                    Id = original.Id, UserAccountId = original.UserAccountId,
                    CreatedAt = original.CreatedAt, IsCompleted = original.IsCompleted,
                    Text = text, Description = description, Priority = priority, DueAt = dueAt, Category = category,
                    ReminderSentForDueAt = original.ReminderSentForDueAt,
                    ReminderPendingForDueAt = original.ReminderPendingForDueAt,
                    ReminderSnoozedUntil = original.ReminderSnoozedUntil,
                    ReminderDismissedForDueAt = original.ReminderDismissedForDueAt
                };
                await _taskService.UpdateAsync(draft);
                original.Text = draft.Text;
                original.Description = draft.Description;
                original.Priority = draft.Priority;
                original.DueAt = draft.DueAt;
                original.Category = draft.Category;
                original.ReminderSentForDueAt = draft.ReminderSentForDueAt;
                original.ReminderPendingForDueAt = draft.ReminderPendingForDueAt;
                original.ReminderSnoozedUntil = draft.ReminderSnoozedUntil;
                original.ReminderDismissedForDueAt = draft.ReminderDismissedForDueAt;
                task = original;
            }
        }
        catch
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is not null)
                await page.DisplayAlertAsync(AppResources.TaskSaveFailedTitle, AppResources.TaskSaveFailedMessage, AppResources.OkButton);
            return;
        }
        finally
        {
            CreateTaskButton.IsEnabled = true;
        }
        QuickTaskEntry.Text = string.Empty;
        NewTaskEntry.Text = string.Empty;
        NewTaskDescriptionEditor.Text = string.Empty;
        NewTaskPriorityPicker.SelectedIndex = 0;
        NewTaskDatePicker.Date = DateTime.Today;
        NewTaskTimePicker.Time = DateTime.Now.TimeOfDay;
        RefreshDisplay();
        CloseComposer();
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
            UpdateClearCompletedButton();
        }
        catch
        {
            task.IsCompleted = previousValue;
            UpdateClearCompletedButton();

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
        if (IsComposerExpanded || sender is not BindableObject { BindingContext: TaskItem task })
            return;
        _editingTask = task;
        NewTaskEntry.Text = task.Text;
        NewTaskDescriptionEditor.Text = task.Description;
        NewTaskPriorityPicker.SelectedIndex = (int)task.Priority;
        NewTaskDatePicker.Date = task.DueAt.Date;
        NewTaskTimePicker.Time = task.DueAt.TimeOfDay;
        NewTaskCategoryPicker.SelectedIndex = task.CategoryIndex;
        OpenComposer();
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

    private void RefreshDisplay()
    {
        var orderedTasks = TodoLearn.Helpers.TaskQuery.Select(Tasks, Filter, DateTime.Now, _categoryView, _selectedCategory);
        if (DisplayTasks.SequenceEqual(orderedTasks))
        {
            UpdateClearCompletedButton();
            return;
        }
        if (DisplayTasks.Count == 0)
        {
            DisplayTasks = new ObservableCollection<TaskItem>(orderedTasks);
            OnPropertyChanged(nameof(DisplayTasks));
            UpdateClearCompletedButton();
            return;
        }
        var visibleTasks = orderedTasks.ToHashSet();
        for (var index = DisplayTasks.Count - 1; index >= 0; index--)
        {
            if (!visibleTasks.Contains(DisplayTasks[index]))
                DisplayTasks.RemoveAt(index);
        }

        for (var index = 0; index < orderedTasks.Count; index++)
        {
            var task = orderedTasks[index];
            if (index < DisplayTasks.Count && ReferenceEquals(DisplayTasks[index], task))
                continue;
            var currentIndex = DisplayTasks.IndexOf(task);
            if (currentIndex < 0)
                DisplayTasks.Insert(index, task);
            else if (currentIndex != index)
                DisplayTasks.Move(currentIndex, index);
        }

        UpdateClearCompletedButton();
    }

    private static string GetTitle(FilterType filter) => filter switch
    {
        FilterType.Important => AppResources.ImportantPageTitle,
        FilterType.Planned => AppResources.PlannedPageTitle,
        FilterType.All => AppResources.AllTasksPageTitle,
        _ => AppResources.MyDay
    };


    private void OnTaskPointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Border row)
            row.SetAppThemeColor(BackgroundColorProperty, (Color)Application.Current!.Resources["HoverLight"], (Color)Application.Current.Resources["HoverDark"]);
    }

    private void OnTaskPointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is Border row)
            row.BackgroundColor = Colors.Transparent;
    }

    private async void OnTaskMenuClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: TaskItem task } button)
            return;
#if WINDOWS
        if (button.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement anchor)
        {
            var menu = new Microsoft.UI.Xaml.Controls.MenuFlyout();
            var edit = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = AppResources.EditTaskAction };
            edit.Click += (_, _) => OnTaskDoubleTapped(button, EventArgs.Empty);
            var delete = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = AppResources.DeleteButton };
            delete.Click += (_, _) => OnDeleteClicked(new Button { CommandParameter = task }, EventArgs.Empty);
            menu.Items.Add(edit);
            menu.Items.Add(delete);
            menu.ShowAt(anchor);
            return;
        }
#endif
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null)
            return;
        var action = await page.DisplayActionSheetAsync(AppResources.TaskActions, AppResources.CancelButton, null, AppResources.EditTaskAction, AppResources.DeleteButton);
        if (action == AppResources.EditTaskAction)
            OnTaskDoubleTapped(button, EventArgs.Empty);
        else if (action == AppResources.DeleteButton)
            OnDeleteClicked(new Button { CommandParameter = task }, EventArgs.Empty);
    }

    private void UpdateClearCompletedButton()
    {
        ClearCompletedButton.IsEnabled = Tasks.Any(task => task.IsCompleted && (!_categoryView || task.Category == _selectedCategory));
    }
}
