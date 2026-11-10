using System.Collections.ObjectModel;
using TodoLearn.Models;
using TodoLearn.Resources.Strings;
using TodoLearn.Services;

namespace TodoLearn.Views;

public partial class TaskListView : ContentView
{
    private readonly TaskService _taskService;
    private TaskComposerView? _composer;
    private TaskComposerView Composer
    {
        get
        {
            if (_composer is not null) return _composer;
            _composer = new TaskComposerView();
            _composer.SaveRequested += OnAddTaskClicked;
            _composer.CancelRequested += OnComposerToggleClicked;
            ComposerHost.Content = _composer;
            return _composer;
        }
    }
    private Entry NewTaskEntry => Composer.NewTaskEntry;
    private Editor NewTaskDescriptionEditor => Composer.NewTaskDescriptionEditor;
    private Picker NewTaskPriorityPicker => Composer.NewTaskPriorityPicker;
    private DatePicker NewTaskDatePicker => Composer.NewTaskDatePicker;
    private TimePicker NewTaskTimePicker => Composer.NewTaskTimePicker;
    private Picker NewTaskCategoryPicker => Composer.NewTaskCategoryPicker;
    private Label ValidationLabel => Composer.ValidationLabel;
    private Button CreateTaskButton => Composer.CreateTaskButton;
    private TaskItem? _editingTask;
    public string ComposerTitle => _editingTask is null ? AppResources.NewTaskSectionTitle : AppResources.EditTaskAction;
    public string ComposerSaveText => _editingTask is null ? AppResources.AddButton : AppResources.SaveButton;
    private bool _isComposerClosing;
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

    private async void OnComposerToggleClicked(object? sender, EventArgs e)
    {
        if (!CreateTaskButton.IsEnabled || _isComposerClosing)
            return;
        if (IsComposerExpanded)
        {
            await CloseComposerAsync();
            return;
        }
        _editingTask = null;
        NewTaskEntry.Text = QuickTaskEntry.Text;
        Composer.SetSteps([]);
        NewTaskDescriptionEditor.Text = string.Empty;
        NewTaskPriorityPicker.SelectedIndex = 0;
        NewTaskDatePicker.Date = DateTime.Today;
        NewTaskTimePicker.Time = DateTime.Now.TimeOfDay;
        NewTaskCategoryPicker.SelectedIndex = _categoryView && _selectedCategory is { } category ? (int)category + 1 : 0;
        await OpenComposerAsync();
    }

    private async Task OpenComposerAsync()
    {
        ValidationLabel.IsVisible = false;
        OnPropertyChanged(nameof(ComposerTitle));
        OnPropertyChanged(nameof(ComposerSaveText));
        Composer.PrepareToShow();
        IsComposerExpanded = true;
        QuickComposer.IsEnabled = false;
        TaskCollection.IsEnabled = false;
        await Composer.ShowAsync();
    }

    private async Task CloseComposerAsync()
    {
        if (!IsComposerExpanded || _isComposerClosing)
            return;
        _isComposerClosing = true;
        Composer.IsEnabled = false;
        try
        {
            await Composer.HideAsync();
            IsComposerExpanded = false;
            QuickComposer.IsEnabled = true;
            TaskCollection.IsEnabled = true;
            QuickTaskEntry.Focus();
        }
        finally
        {
            Composer.IsEnabled = true;
            _isComposerClosing = false;
        }
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
        shortcut.Invoked += async (_, args) =>
        {
            if (!IsComposerExpanded)
                return;
            args.Handled = true;
            if (!CreateTaskButton.IsEnabled)
                return;
            if (save)
                OnAddTaskClicked(this, EventArgs.Empty);
            else
                await CloseComposerAsync();
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
        OnPropertyChanged(nameof(IsDashboardVisible));
        _selectedCategory = category;
        TitleLabel.Text = CategoryOptions[category is { } value ? (int)value + 1 : 0];
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
        Unloaded += (_, _) => StopDashboardClock();
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
        OnPropertyChanged(nameof(IsDashboardVisible));
        TitleLabel.Text = GetTitle(filter);
        RefreshDisplay();
    }

    private async void OnAddTaskClicked(object? sender, EventArgs e)
    {
        if (!CreateTaskButton.IsEnabled || _isComposerClosing)
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
        Composer.IncludePendingStep();
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
                task = await _taskService.AddAsync(text, description, priority, dueAt, category, Composer.DraftSteps);
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
                foreach (var step in Composer.DraftSteps) draft.Steps.Add(step.Copy());
                await _taskService.UpdateAsync(draft);
                foreach (var step in original.Steps.ToList()) original.Steps.Remove(step);
                foreach (var step in draft.Steps.Where(step => !string.IsNullOrWhiteSpace(step.Text))) original.Steps.Add(step.Copy());
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
        RefreshDisplay();
        await CloseComposerAsync();
        QuickTaskEntry.Text = string.Empty;
        NewTaskEntry.Text = string.Empty;
        NewTaskDescriptionEditor.Text = string.Empty;
        NewTaskPriorityPicker.SelectedIndex = 0;
        NewTaskDatePicker.Date = DateTime.Today;
        NewTaskTimePicker.Time = DateTime.Now.TimeOfDay;

    }

    private async void OnTaskCompletionChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (sender is not CheckBox { BindingContext: TaskItem task } checkbox)
            return;

        if (task.IsCompleted == e.Value)
            return;

        var previousValue = task.IsCompleted;
        checkbox.IsEnabled = false;
        task.IsCompleted = e.Value;
        var summary = (checkbox.Parent as Grid)?.Children.OfType<VerticalStackLayout>().FirstOrDefault();
        try
        {
            await Task.WhenAll(_taskService.UpdateAsync(task), Helpers.UiMotion.SetCompletedAsync(checkbox, summary, e.Value));
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
        finally
        {
            checkbox.Scale = 1;
            checkbox.IsEnabled = true;
            if (summary?.BindingContext is TaskItem bound)
                summary.Opacity = bound.IsCompleted ? 0.55 : 1;
        }
    }

    private bool _isDeleting;

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_isDeleting || sender is not Button { CommandParameter: TaskItem task } || Window?.Page is not { } page)
            return;

        _isDeleting = true;
        try
        {
            var confirmed = await page.DisplayAlertAsync(AppResources.DeleteTaskTitle,
                string.Format(AppResources.DeleteTaskConfirmation, task.Text),
                AppResources.DeleteButton, AppResources.CancelButton);
            if (!confirmed)
                return;

            await _taskService.DeleteAsync(task);
            Tasks.Remove(task);
            RefreshDisplay();
            SimpleLogger.Log(string.Format(AppResources.TaskDeletedLog, task.Text, task.Id));
        }
        catch
        {
            await page.DisplayAlertAsync(AppResources.DeleteFailedTitle, AppResources.DeleteFailedMessage, AppResources.OkButton);
        }
        finally
        {
            _isDeleting = false;
        }
    }

    private async void OnClearCompletedClicked(object? sender, EventArgs e)
    {
        if (_isDeleting || Window?.Page is not { } page)
            return;
        var count = Tasks.Count(task => task.IsCompleted && (!_categoryView || task.Category == _selectedCategory));
        if (count == 0)
            return;

        _isDeleting = true;
        try
        {
            var confirmed = await page.DisplayAlertAsync(AppResources.ClearCompletedButton,
                string.Format(AppResources.ClearCompletedConfirmation, count),
                AppResources.DeleteButton, AppResources.CancelButton);
            if (!confirmed)
                return;

            await _taskService.ClearCompletedAsync(_categoryView, _selectedCategory);
            foreach (var task in Tasks.Where(task => task.IsCompleted && (!_categoryView || task.Category == _selectedCategory)).ToList())
                Tasks.Remove(task);
            RefreshDisplay();
        }
        catch
        {
            await page.DisplayAlertAsync(AppResources.DeleteFailedTitle, AppResources.DeleteFailedMessage, AppResources.OkButton);
        }
        finally
        {
            _isDeleting = false;
        }
    }

    private async void OnTaskDoubleTapped(object? sender, EventArgs e)
    {
        if (IsComposerExpanded || sender is not BindableObject { BindingContext: TaskItem task })
            return;
        _editingTask = task;
        Composer.SetSteps(task.Steps);
        NewTaskEntry.Text = task.Text;
        NewTaskDescriptionEditor.Text = task.Description;
        NewTaskPriorityPicker.SelectedIndex = (int)task.Priority;
        NewTaskDatePicker.Date = task.DueAt.Date;
        NewTaskTimePicker.Time = task.DueAt.TimeOfDay;
        NewTaskCategoryPicker.SelectedIndex = task.CategoryIndex;
        await OpenComposerAsync();
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
        UpdateDashboard();
        ClearCompletedButton.IsEnabled = Tasks.Any(task => task.IsCompleted && (!_categoryView || task.Category == _selectedCategory));
    }

    private void OnStepsToggleClicked(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: TaskItem task }) task.StepsExpanded = !task.StepsExpanded;
    }

    private async void OnStepCompletionChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (sender is not CheckBox { BindingContext: TaskStep step } checkbox || step.IsCompleted == e.Value) return;
        var task = Tasks.FirstOrDefault(item => item.Id == step.TaskItemId);
        if (task is null) return;
        checkbox.IsEnabled = false;
        try { await _taskService.SetStepCompletedAsync(task, step, e.Value); }
        catch
        {
            checkbox.IsChecked = step.IsCompleted;
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is not null) await page.DisplayAlertAsync(AppResources.TaskSaveFailedTitle, AppResources.TaskSaveFailedMessage, AppResources.OkButton);
        }
        finally { checkbox.IsEnabled = true; }
    }

    private void OnTaskSummaryBindingContextChanged(object? sender, EventArgs e)
    {
        if (sender is VisualElement { BindingContext: TaskItem task } summary)
        {
            summary.CancelAnimations();
            summary.Opacity = task.IsCompleted ? 0.55 : 1;
        }
    }
}
