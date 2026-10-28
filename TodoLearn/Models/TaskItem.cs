using System;
using System.ComponentModel;
using TodoLearn.Resources.Strings;

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
        public int Id { get; set; }
        public int? UserAccountId { get; set; }
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

        private string? _description;
        public string? Description
        {
            get => _description;
            set
            {
                if (_description == value) return;
                _description = value;
                CanExpandDescription = false;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasDescription)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowDescriptionToggle)));
            }
        }
        public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

        private bool _isDescriptionExpanded;
        public bool IsDescriptionExpanded
        {
            get => _isDescriptionExpanded;
            set
            {
                if (_isDescriptionExpanded == value) return;
                _isDescriptionExpanded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDescriptionExpanded)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExpandToggleText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowDescriptionToggle)));
            }
        }

        private bool _canExpandDescription;
        public bool CanExpandDescription
        {
            get => _canExpandDescription;
            set
            {
                if (_canExpandDescription == value) return;
                _canExpandDescription = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanExpandDescription)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowDescriptionToggle)));
            }
        }

        public bool ShowDescriptionToggle => CanExpandDescription || IsDescriptionExpanded;

        public string ExpandToggleText => IsDescriptionExpanded
            ? AppResources.CollapseDescription
            : AppResources.ShowFullDescription;

        public DateTime CreatedAt { get; set; }

        public string CreatedAtText => string.Format(AppResources.CreatedAtFormat, CreatedAt);

        private DateTime _dueAt;
        public DateTime DueAt
        {
            get => _dueAt;
            set
            {
                if (_dueAt == value) return;
                _dueAt = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DueAt)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DueAtText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DueDate)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DueTime)));
            }
        }

        public string DueAtText => string.Format(AppResources.DueAtFormat, DueAt);

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
