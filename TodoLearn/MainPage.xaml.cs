using System.Collections.ObjectModel;

namespace TodoLearn
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            Tasks.Add(new TaskItem { Text = "Finish project report" });
            Tasks.Add(new TaskItem { Text = "Buy groceries" });
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
    }

    public class TaskItem : System.ComponentModel.INotifyPropertyChanged
    {
        public TaskItem()
        {
            CreatedAt = DateTime.Now;
        }

        public string? Text { get; set; }

        public DateTime CreatedAt { get; }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }
}
