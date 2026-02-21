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

        public string? Text { get; set; }

        public DateTime CreatedAt { get; }

        public DateTime DueAt { get; set; }

        private TaskPriority _priority;
        public TaskPriority Priority
        {
            get => _priority;
            set
            {
                if (_priority == value) return;
                _priority = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Priority)));
            }
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
