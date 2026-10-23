using System.Collections.ObjectModel;
using System.Linq;
using TodoLearn.Models;
using TodoLearn.Resources.Strings;
using TodoLearn.Services;

namespace TodoLearn
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();
        public ObservableCollection<TaskItem> DisplayTasks { get; } = new ObservableCollection<TaskItem>();

        private FilterType _currentFilter = FilterType.All;
        private readonly Dictionary<TaskItem, (string? Text, string? Description, DateTime DueAt, TaskPriority Priority, bool IsCompleted)> _editBackups
            = new Dictionary<TaskItem, (string?, string?, DateTime, TaskPriority, bool)>();

        private readonly TaskService _taskService;

        public MainPage()
            : this(IPlatformApplication.Current?.Services.GetService<TaskService>()
                ?? throw new InvalidOperationException(AppResources.TaskServiceRequired))
        {
        }

        public MainPage(TaskService taskService)
        {
            InitializeComponent();
            BindingContext = this;
            _taskService = taskService;

            Tasks.CollectionChanged += (_, __) => RefreshDisplay();
        }

        protected override async void OnAppearing()
        {
            Tasks.Clear();
            var items = await _taskService.GetTasksAsync();
            foreach (var it in items)
                Tasks.Add(it);
            RefreshDisplay();
        }



        private async void OnAddTaskClicked(object? sender, EventArgs e)
        {
            var newTaskEntry = NewTaskEntry;
            if (newTaskEntry is null) return;

            var text = newTaskEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;
            var task = await _taskService.AddAsync(text);
            Tasks.Add(task);
            newTaskEntry.Text = string.Empty;
            SimpleLogger.Log(string.Format(AppResources.TaskAddedLog, task.Text, task.Id));
        }

        private void OnFilterClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string s)
            {
                if (Enum.TryParse<FilterType>(s, true, out var f))
                {
                    _currentFilter = f;
                    RefreshDisplay();
                }
            }
        }

        private void RefreshDisplay()
        {
            DisplayTasks.Clear();
            IEnumerable<TaskItem> items = Tasks;
            switch (_currentFilter)
            {
                case FilterType.Important:
                    items = Tasks.Where(t => t.Priority == TaskPriority.High);
                    break;
                case FilterType.Planned:
                    items = Tasks.Where(t => t.DueAt > DateTime.Now);
                    break;
                case FilterType.All:
                    items = Tasks;
                    break;
                case FilterType.MyDay:
                default:
                    items = Tasks.Where(t => t.DueAt.Date == DateTime.Now.Date);
                    break;
            }

            foreach (var it in items)
                DisplayTasks.Add(it);
        }

        private async void OnClearCompletedClicked(object? sender, EventArgs e)
        {
            await _taskService.ClearAsync();
            Tasks.Clear();
        }

        private void OnTaskDoubleTapped(object? sender, EventArgs e)
        {
            if (sender is BindableObject b && b.BindingContext is TaskItem t)
            {
                if (!_editBackups.ContainsKey(t))
                {
                    _editBackups[t] = (t.Text, t.Description, t.DueAt, t.Priority, t.IsCompleted);
                }
                t.IsEditing = true;
            }
        }




        private async void OnDeleteClicked(object? sender, EventArgs e)
        {
            if (sender is Button b && b.CommandParameter is TaskItem t)
            {
                await _taskService.DeleteAsync(t);
                if (Tasks.Contains(t)) Tasks.Remove(t);
                SimpleLogger.Log(string.Format(AppResources.TaskDeletedLog, t.Text, t.Id));
            }
        }

        private void OnPriorityPointerEntered(object? sender, EventArgs e)
        {
            if (sender is BindableObject b && b.BindingContext is TaskItem t)
            {
                t.IsDetailsVisible = true;
            }
        }

        private void OnPriorityPointerExited(object? sender, EventArgs e)
        {
            if (sender is BindableObject b && b.BindingContext is TaskItem t)
            {
                t.IsDetailsVisible = false;
            }
        }

        private async void OnPriorityTapped(object? sender, EventArgs e)
        {
            if (sender is BindableObject b && b.BindingContext is TaskItem t)
            {
                t.IsDetailsVisible = !t.IsDetailsVisible;
            }
        }



        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is TaskItem t)
            {
                if (_editBackups.ContainsKey(t))
                    _editBackups.Remove(t);
                await _taskService.UpdateAsync(t);
                t.IsEditing = false;
                SimpleLogger.Log(string.Format(AppResources.TaskEditedLog, t.Text, t.Id));
            }
        }

        private void OnCancelClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is TaskItem t)
            {
                if (_editBackups.TryGetValue(t, out var backup))
                {
                    t.Text = backup.Text;
                    t.Description = backup.Description;
                    t.DueAt = backup.DueAt;
                    t.Priority = backup.Priority;
                    t.IsCompleted = backup.IsCompleted;
                    _editBackups.Remove(t);
                }
                t.IsEditing = false;
            }
        }

        private void OnDescriptionToggleTapped(object? sender, EventArgs e)
        {
            if (sender is BindableObject b && b.BindingContext is TaskItem t)
            {
                t.IsDescriptionExpanded = !t.IsDescriptionExpanded;
            }
        }

        private void OnPrioritySelectClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string s && btn.BindingContext is TaskItem t)
            {
                if (int.TryParse(s, out var idx))
                {
                    t.Priority = (TaskPriority)idx;
                }
            }
            else if (sender is Button b && b.CommandParameter is int i && b.BindingContext is TaskItem ti)
            {
                ti.Priority = (TaskPriority)i;
            }
        }
    }
}
