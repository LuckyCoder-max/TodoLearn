using System.Collections.ObjectModel;
using System.Linq;
using TodoLearn.Models;

namespace TodoLearn
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();
        public ObservableCollection<TaskItem> DisplayTasks { get; } = new ObservableCollection<TaskItem>();

        private FilterType _currentFilter = FilterType.All;
        private readonly Dictionary<TaskItem, (string? Text, DateTime DueAt, TaskPriority Priority, bool IsCompleted)> _editBackups
            = new Dictionary<TaskItem, (string?, DateTime, TaskPriority, bool)>();

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            Tasks.Add(new TaskItem { Text = "Finish project report", Priority = TaskPriority.High, DueAt = DateTime.Now.AddHours(4) });
            Tasks.Add(new TaskItem { Text = "Buy groceries", Priority = TaskPriority.Low, DueAt = DateTime.Now.AddDays(1) });

            Tasks.CollectionChanged += (_, __) => RefreshDisplay();
            RefreshDisplay();
        }

        

        private void OnAddTaskClicked(object? sender, EventArgs e)
        {
            var text = NewTaskEntry?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;
            Tasks.Add(new TaskItem { Text = text });
            NewTaskEntry.Text = string.Empty;
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

        private void OnClearCompletedClicked(object? sender, EventArgs e)
        {
            Tasks.Clear();
        }

        private void OnTaskDoubleTapped(object? sender, EventArgs e)
        {
            if (sender is BindableObject b && b.BindingContext is TaskItem t)
            {
                if (!_editBackups.ContainsKey(t))
                {
                    _editBackups[t] = (t.Text, t.DueAt, t.Priority, t.IsCompleted);
                }
                t.IsEditing = true;
            }
        }


        

        private void OnDeleteClicked(object? sender, EventArgs e)
        {
            if (sender is Button b && b.CommandParameter is TaskItem t)
            {
                if (Tasks.Contains(t)) Tasks.Remove(t);
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

        

        private void OnSaveClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is TaskItem t)
            {
                if (_editBackups.ContainsKey(t))
                    _editBackups.Remove(t);
                t.IsEditing = false;
            }
        }

        private void OnCancelClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is TaskItem t)
            {
                if (_editBackups.TryGetValue(t, out var backup))
                {
                    t.Text = backup.Text;
                    t.DueAt = backup.DueAt;
                    t.Priority = backup.Priority;
                    t.IsCompleted = backup.IsCompleted;
                    _editBackups.Remove(t);
                }
                t.IsEditing = false;
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
