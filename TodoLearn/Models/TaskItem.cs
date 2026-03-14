using System;
using System.ComponentModel;

namespace TodoLearn.Models
{
    public enum TaskPriority
    {
        Low,
        Medium,
        High
    }

    public class TaskItem : INotifyPropertyChanged
    {
        public TaskItem()
        {
            CreatedAt = DateTime.Now;
            DueAt = CreatedAt;
            Priority = TaskPriority.Low;
        }

        private string? _text;
        public string? Text
        {
            get => _text;
            set
            {
                if (_text == value) return;
                _text = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
            }
        }

        public DateTime CreatedAt { get; }

        private DateTime _dueAt;
        public DateTime DueAt
        {
            get => _dueAt;
            set
            {
                if (_dueAt == value) return;
                _dueAt = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DueAt)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DueDate)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DueTime)));
            }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing == value) return;
                _isEditing = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEditing)));
            }
        }

        public DateTime DueDate
        {
            get => DueAt.Date;
            set => DueAt = value.Date + DueAt.TimeOfDay;
        }

        public TimeSpan DueTime
        {
            get => DueAt.TimeOfDay;
            set => DueAt = DueAt.Date + value;
        }

        private bool _isCompleted;
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted == value) return;
                _isCompleted = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCompleted)));
            }
        }

        private TaskPriority _priority;
        public TaskPriority Priority
        {
            get => _priority;
            set
            {
                if (_priority == value) return;
                _priority = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Priority)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PriorityIndex)));
            }
        }

        public int PriorityIndex
        {
            get => (int)Priority;
            set => Priority = (TaskPriority)value;
        }

        private bool _isDetailsVisible;
        public bool IsDetailsVisible
        {
            get => _isDetailsVisible;
            set
            {
                if (_isDetailsVisible == value) return;
                _isDetailsVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDetailsVisible)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
