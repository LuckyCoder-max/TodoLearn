using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TodoLearn.Models;

namespace TodoLearn
{
    public partial class MainPage : ContentPage
    {
        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();
        public ObservableCollection<TaskItem> DisplayTasks { get; } = new ObservableCollection<TaskItem>();

        private FilterType _currentFilter = FilterType.All;
        private readonly Dictionary<TaskItem, (string? Text, DateTime DueAt, TaskPriority Priority, bool IsCompleted)> _editBackups
            = new Dictionary<TaskItem, (string?, DateTime, TaskPriority, bool)>();

        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public MainPage(IDbContextFactory<AppDbContext> dbFactory)
        {
            InitializeComponent();
            BindingContext = this;
            _dbFactory = dbFactory;

            Tasks.CollectionChanged += (_, __) => RefreshDisplay();
        }

        protected override async void OnAppearing()
        {
            Tasks.Clear();
            await using var db = await _dbFactory.CreateDbContextAsync();
            var items = await db.Tasks.OrderBy(t => t.CreatedAt).ToListAsync();
            foreach (var it in items)
                Tasks.Add(it);
            RefreshDisplay();
        }

        

        private async void OnAddTaskClicked(object? sender, EventArgs e)
        {
            var text = NewTaskEntry?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;
            var task = new TaskItem { Text = text };
            await using var db = await _dbFactory.CreateDbContextAsync();
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            Tasks.Add(task);
            NewTaskEntry.Text = string.Empty;
        }

        private void OnFilterClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string s)
            {
                if (Enum.TryParse<FilterType>(s, true, out var f))
                {
                    _currentFilter = f;
                    RefreshDisplay();
                }
            }
        }

        private void RefreshDisplay()
        {
            DisplayTasks.Clear();
            IEnumerable<TaskItem> items = Tasks;
            switch (_currentFilter)
            {
                case FilterType.Important:
                    items = Tasks.Where(t => t.Priority == TaskPriority.High);
                    break;
                case FilterType.Planned:
                    items = Tasks.Where(t => t.DueAt > DateTime.Now);
                    break;
                case FilterType.All:
                    items = Tasks;
                    break;
                case FilterType.MyDay:
                default:
                    items = Tasks.Where(t => t.DueAt.Date == DateTime.Now.Date);
                    break;
            }

            foreach (var it in items)
                DisplayTasks.Add(it);
        }

        private async void OnClearCompletedClicked(object? sender, EventArgs e)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var all = await db.Tasks.ToListAsync();
            db.Tasks.RemoveRange(all);
            await db.SaveChangesAsync();
            Tasks.Clear();
        }

        private void OnTaskDoubleTapped(object? sender, EventArgs e)
        {
            if (sender is BindableObject b && b.BindingContext is TaskItem t)
            {
                if (!_editBackups.ContainsKey(t))
                {
                    _editBackups[t] = (t.Text, t.DueAt, t.Priority, t.IsCompleted);
                }
                t.IsEditing = true;
            }
        }


        

        private async void OnDeleteClicked(object? sender, EventArgs e)
        {
            if (sender is Button b && b.CommandParameter is TaskItem t)
            {
                await using var db = await _dbFactory.CreateDbContextAsync();
                db.Tasks.Remove(t);
                await db.SaveChangesAsync();
                if (Tasks.Contains(t)) Tasks.Remove(t);
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

        

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is TaskItem t)
            {
                if (_editBackups.ContainsKey(t))
                    _editBackups.Remove(t);
                await using var db = await _dbFactory.CreateDbContextAsync();
                db.Tasks.Update(t);
                await db.SaveChangesAsync();
                t.IsEditing = false;
            }
        }

        private void OnCancelClicked(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is TaskItem t)
            {
                if (_editBackups.TryGetValue(t, out var backup))
                {
                    t.Text = backup.Text;
                    t.DueAt = backup.DueAt;
                    t.Priority = backup.Priority;
                    t.IsCompleted = backup.IsCompleted;
                    _editBackups.Remove(t);
                }
                t.IsEditing = false;
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
