using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;


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
            // удалить после тестирования с БД
            Tasks.Add(new TaskItem { Text = "Finish project report" });
            Tasks.Add(new TaskItem { Text = "Buy groceries" });
            Tasks.Add(new TaskItem { Text = "Finish project report", CreatedAt = DateTime.Now.AddYears(-2) });
            Tasks.Add(new TaskItem { Text = "Buy groceries", CreatedAt = DateTime.Now.AddDays(-1) });
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

    public partial class TaskItem : ObservableObject
    {
        [ObservableProperty]
        private string? text;

        [ObservableProperty]
        private bool isCompleted;

        //public DateTime CreatedAt { get; } = DateTime.Now;
        // удалить после тестирования 
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        //TODO протестировать коректную работу свойства при хранении в базе данных

        public string DisplayDate
        {
            get
            {
                var now = DateTime.Now;

                if (CreatedAt.Date == now.Date)
                    return CreatedAt.ToString("HH:mm");

                if (CreatedAt.Date == now.Date.AddDays(-1))
                    return "Yesterday";

                if (CreatedAt.Year == now.Year)
                    return CreatedAt.ToString("dd MMM");

                return CreatedAt.ToString("dd MMM yyyy");
            }
        }
    }
}
