using System.Collections.ObjectModel;
using System.Linq;
using TodoLearn.Models;

namespace TodoLearn
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();
        public ObservableCollection<TaskItem> DisplayTasks { get; } = new ObservableCollection<TaskItem>();
        private string _currentFilter = "All";

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

        private void OnFilterClicked(object? sender, EventArgs e) { }

        private void RefreshDisplay()
        {
            DisplayTasks.Clear();
            IEnumerable<TaskItem> items = Tasks;
            switch (_currentFilter)
            {
                case "Important":
                    items = Tasks.Where(t => t.Priority == TaskPriority.High);
                    break;
                case "Planned":
                    items = Tasks.Where(t => t.DueAt > DateTime.Now);
                    break;
                case "All":
                    items = Tasks;
                    break;
                case "MyDay":
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
                if (Tasks.Contains(t)) Tasks.Remove(t);
            }
            else if (sender is Button btn && btn.CommandParameter is TaskItem tp)
            {
                if (Tasks.Contains(tp)) Tasks.Remove(tp);
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

        

        private void OnEditClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is TaskItem t)
            {
                t.IsEditing = true;
            }
        }

        private void OnSaveClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is TaskItem t)
            {
                t.IsEditing = false;
            }
        }

        private void OnCancelClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is TaskItem t)
            {
                t.IsEditing = false;
                // пофиксить с канселом - чтобы сохраняять
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
