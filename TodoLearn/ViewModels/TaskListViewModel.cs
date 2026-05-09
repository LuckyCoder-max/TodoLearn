using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TodoLearn.Models;

namespace TodoLearn.ViewModels
{
    public class TaskListViewModel : BaseViewModel
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly FilterType _filter;

        private readonly ObservableCollection<TaskItem> _allTasks = new();

        public ObservableCollection<TaskItem> DisplayTasks { get; } = new();

        private string _newTaskText = string.Empty;
        public string NewTaskText
        {
            get => _newTaskText;
            set => SetProperty(ref _newTaskText, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public bool CanAddTasks => _filter is FilterType.All or FilterType.MyDay;

        public ICommand AddTaskCommand        { get; }
        public ICommand DeleteTaskCommand     { get; }
        public ICommand ClearCompletedCommand { get; }
        public ICommand ToggleEditCommand     { get; }
        public ICommand SaveTaskCommand       { get; }
        public ICommand CancelEditCommand     { get; }
        public ICommand ToggleCompleteCommand { get; }
        public ICommand SetPriorityCommand    { get; }

        private readonly Dictionary<TaskItem, (string? Text, DateTime DueAt, TaskPriority Priority, bool IsCompleted)>
            _editBackups = new();

        public TaskListViewModel(IDbContextFactory<AppDbContext> dbFactory, FilterType filter)
        {
            _dbFactory = dbFactory;
            _filter = filter;

            AddTaskCommand        = new RelayCommand(_ => AddTaskAsync(),    _ => CanAddTasks);
            DeleteTaskCommand     = new RelayCommand(p => DeleteTaskAsync(p as TaskItem));
            ClearCompletedCommand = new RelayCommand(_ => ClearCompletedAsync());
            ToggleEditCommand     = new RelayCommand(p => ToggleEdit(p as TaskItem));
            SaveTaskCommand       = new RelayCommand(p => SaveTaskAsync(p as TaskItem));
            CancelEditCommand     = new RelayCommand(p => CancelEdit(p as TaskItem));
            ToggleCompleteCommand = new RelayCommand(p => ToggleCompleteAsync(p as TaskItem));
            SetPriorityCommand    = new RelayCommand(p => SetPriority(p));
        }

        public async Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                _allTasks.Clear();
                DisplayTasks.Clear();

                await using var db = await _dbFactory.CreateDbContextAsync();
                var query = db.Tasks.AsQueryable();

                query = _filter switch
                {
                    FilterType.Important => query.Where(t => t.Priority == TaskPriority.High),
                    FilterType.Planned   => query.Where(t => t.DueAt > DateTime.Now),
                    FilterType.Completed => query.Where(t => t.IsCompleted),
                    _                    => query
                };

                query = _filter switch
                {
                    FilterType.Planned   => query.OrderBy(t => t.DueAt),
                    FilterType.Completed => query.OrderByDescending(t => t.DueAt),
                    _                    => query.OrderBy(t => t.CreatedAt)
                };

                var items = await query.ToListAsync();

                if (_filter == FilterType.MyDay)
                    items = items.Where(t => t.DueAt.Date == DateTime.Today).ToList();

                foreach (var it in items)
                {
                    _allTasks.Add(it);
                    DisplayTasks.Add(it);
                }
            }
            finally { IsBusy = false; }
        }

        private async Task AddTaskAsync()
        {
            var text = NewTaskText.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            var task = new TaskItem { Text = text };
            await using var db = await _dbFactory.CreateDbContextAsync();
            db.Tasks.Add(task);
            await db.SaveChangesAsync();

            _allTasks.Add(task);
            if (MatchesFilter(task))
                DisplayTasks.Add(task);

            NewTaskText = string.Empty;
        }

        private async Task DeleteTaskAsync(TaskItem? task)
        {
            if (task is null) return;
            await using var db = await _dbFactory.CreateDbContextAsync();
            db.Tasks.Remove(task);
            await db.SaveChangesAsync();
            _allTasks.Remove(task);
            DisplayTasks.Remove(task);
        }

        private async Task ClearCompletedAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var completed = await db.Tasks.Where(t => t.IsCompleted).ToListAsync();
            db.Tasks.RemoveRange(completed);
            await db.SaveChangesAsync();
            foreach (var t in completed)
            {
                _allTasks.Remove(t);
                DisplayTasks.Remove(t);
            }
        }

        private void ToggleEdit(TaskItem? task)
        {
            if (task is null) return;
            if (!_editBackups.ContainsKey(task))
                _editBackups[task] = (task.Text, task.DueAt, task.Priority, task.IsCompleted);
            task.IsEditing = !task.IsEditing;
        }

        private async Task SaveTaskAsync(TaskItem? task)
        {
            if (task is null) return;
            _editBackups.Remove(task);
            await using var db = await _dbFactory.CreateDbContextAsync();
            db.Tasks.Update(task);
            await db.SaveChangesAsync();
            task.IsEditing = false;

            if (!MatchesFilter(task))
                DisplayTasks.Remove(task);
        }

        private void CancelEdit(TaskItem? task)
        {
            if (task is null) return;
            if (_editBackups.TryGetValue(task, out var backup))
            {
                task.Text = backup.Text;
                task.DueAt = backup.DueAt;
                task.Priority = backup.Priority;
                task.IsCompleted = backup.IsCompleted;
                _editBackups.Remove(task);
            }
            task.IsEditing = false;
        }

        private async Task ToggleCompleteAsync(TaskItem? task)
        {
            if (task is null) return;
            await using var db = await _dbFactory.CreateDbContextAsync();
            db.Tasks.Update(task);
            await db.SaveChangesAsync();

            if (_filter == FilterType.Completed && !task.IsCompleted)
                DisplayTasks.Remove(task);
        }

        private void SetPriority(object? parameter)
        {
            if (parameter is not string s) return;
            var parts = s.Split(':');
            if (parts.Length != 2) return;
            if (!int.TryParse(parts[0], out var id) || !int.TryParse(parts[1], out var idx)) return;
            var task = _allTasks.FirstOrDefault(t => t.Id == id);
            if (task is not null)
                task.Priority = (TaskPriority)idx;
        }

        private bool MatchesFilter(TaskItem task) => _filter switch
        {
            FilterType.MyDay     => task.DueAt.Date == DateTime.Today,
            FilterType.Important => task.Priority == TaskPriority.High,
            FilterType.Planned   => task.DueAt > DateTime.Now,
            FilterType.Completed => task.IsCompleted,
            FilterType.All       => true,
            _                    => true
        };
    }
}
