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
        private TaskCategory? _category;
        public TaskCategory? Category
        {
            get => _category;
            set
            {
                if (_category == value) return;
                _category = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Category)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CategoryIndex)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CategoryName)));
            }
        }

        public int CategoryIndex
        {
            get => Category is { } category ? (int)category + 1 : 0;
            set => Category = value > 0 && value <= Enum.GetValues<TaskCategory>().Length
                ? (TaskCategory)(value - 1) : null;
        }

        public string CategoryName => TaskCategories.Names[CategoryIndex];

        public TaskItem()
        {
            Steps.CollectionChanged += (_, args) =>
            {
                if (args.OldItems is not null)
                    foreach (TaskStep step in args.OldItems) step.PropertyChanged -= OnStepChanged;
                if (args.NewItems is not null)
                    foreach (TaskStep step in args.NewItems) step.PropertyChanged += OnStepChanged;
                NotifySteps();
            };
            CreatedAt = DateTime.Now;
            DueAt = CreatedAt;
            Priority = TaskPriority.Low;
        }

        private string? _text;
        public System.Collections.ObjectModel.ObservableCollection<TaskStep> Steps { get; } = new();
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool HasSteps => Steps.Count > 0;
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string StepsProgress => string.Format(AppResources.StepsProgressFormat, Steps.Count(step => step.IsCompleted), Steps.Count)
            + (StepsExpanded ? " ▴" : " ▾");
        private bool _stepsExpanded;
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool StepsExpanded
        {
            get => _stepsExpanded;
            set
            {
                if (_stepsExpanded == value) return;
                _stepsExpanded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StepsExpanded)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StepsProgress)));
            }
        }
        private void OnStepChanged(object? sender, PropertyChangedEventArgs args) => NotifySteps();
        private void NotifySteps()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSteps)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StepsProgress)));
        }
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CompactDueAtText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DueDate)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DueTime)));
            }
        }

        public string DueAtText => string.Format(AppResources.DueAtFormat, DueAt);
        public string CompactDueAtText => DueAt.Date == DateTime.Today
            ? string.Format(AppResources.DueTodayFormat, DueAt)
            : DueAt.ToString("dd MMM yyyy · HH:mm");
        public string PriorityText => Priority switch
        {
            TaskPriority.High => AppResources.PriorityHigh,
            TaskPriority.Medium => AppResources.PriorityMedium,
            _ => AppResources.PriorityLow
        };

        public DateTime? ReminderSentForDueAt { get; set; }
        public DateTime? ReminderPendingForDueAt { get; set; }
        public DateTime? ReminderSnoozedUntil { get; set; }
        public DateTime? ReminderDismissedForDueAt { get; set; }

        internal void ResetReminderState()
        {
            ReminderSentForDueAt = null;
            ReminderPendingForDueAt = null;
            ReminderSnoozedUntil = null;
            ReminderDismissedForDueAt = null;
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PriorityText)));
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
