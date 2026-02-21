using System.Collections.ObjectModel;
using TodoLearn.Models;

namespace TodoLearn
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            Tasks.Add(new TaskItem { Text = "Finish project report", Priority = TaskPriority.High, DueAt = DateTime.Now.AddHours(4) });
            Tasks.Add(new TaskItem { Text = "Buy groceries", Priority = TaskPriority.Low, DueAt = DateTime.Now.AddDays(1) });
        }

        private void OnAddTaskClicked(object? sender, EventArgs e)
        {
            var text = NewTaskEntry?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;
            Tasks.Add(new TaskItem { Text = text });
            NewTaskEntry.Text = string.Empty;
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
    }
}
