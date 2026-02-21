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

            Tasks.CollectionChanged += Tasks_CollectionChanged;

            Tasks.Add(new TaskItem { Text = "Finish project report" });
            Tasks.Add(new TaskItem { Text = "Buy groceries" });
        }

        private void OnAddTaskClicked(object? sender, EventArgs e)
        {
            var text = NewTaskEntry?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;
            Tasks.Insert(0, new TaskItem { Text = text });
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

        private void Tasks_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (TaskItem item in e.NewItems)
                {
                    item.PropertyChanged += Task_PropertyChanged;
                }
            }
        }

        private void Task_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TaskItem.IsCompleted)
                && sender is TaskItem task)
            {
                if (Tasks.Contains(task))
                {
                    Tasks.Remove(task);

                    if (task.IsCompleted)
                        Tasks.Add(task); // вниз
                    else
                        Tasks.Insert(0, task); // вверх
                }
            }
        }
    }

    public class TaskItem : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isCompleted;

        public TaskItem()
        {
            CreatedAt = DateTime.Now;
        }

        public string? Text { get; set; }

        public DateTime CreatedAt { get; }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    OnPropertyChanged(nameof(IsCompleted));
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this,
                new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}
