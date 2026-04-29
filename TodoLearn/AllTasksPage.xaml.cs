using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using TodoLearn.Models;

namespace TodoLearn;

public partial class AllTasksPage : ContentPage
{
    public ObservableCollection<TaskItem> DisplayTasks { get; } = new();

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly Dictionary<TaskItem, (string? Text, DateTime DueAt, TaskPriority Priority, bool IsCompleted)> _editBackups = new();

    public AllTasksPage(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DisplayTasks.Clear();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var items = await db.Tasks.OrderBy(t => t.CreatedAt).ToListAsync();
        foreach (var it in items)
            DisplayTasks.Add(it);
    }

    private async void OnAddTaskClicked(object? sender, EventArgs e)
    {
        var text = NewTaskEntry?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;
        var task = new TaskItem { Text = text };
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        DisplayTasks.Add(task);
        NewTaskEntry.Text = string.Empty;
    }

    private async void OnTaskCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (sender is CheckBox cb && cb.BindingContext is TaskItem t)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            db.Tasks.Update(t);
            await db.SaveChangesAsync();
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is Button b && b.CommandParameter is TaskItem t)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            db.Tasks.Remove(t);
            await db.SaveChangesAsync();
            DisplayTasks.Remove(t);
        }
    }

    private async void OnClearClicked(object? sender, EventArgs e)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var completed = await db.Tasks.Where(t => t.IsCompleted).ToListAsync();
        db.Tasks.RemoveRange(completed);
        await db.SaveChangesAsync();

        foreach (var t in completed.ToList())
            DisplayTasks.Remove(t);
    }

    private void OnTaskDoubleTapped(object? sender, EventArgs e)
    {
        if (sender is BindableObject b && b.BindingContext is TaskItem t)
        {
            if (!_editBackups.ContainsKey(t))
                _editBackups[t] = (t.Text, t.DueAt, t.Priority, t.IsCompleted);
            t.IsEditing = true;
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is TaskItem t)
        {
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

    private void OnPriorityTapped(object? sender, EventArgs e)
    {
        if (sender is BindableObject b && b.BindingContext is TaskItem t)
            t.IsDetailsVisible = !t.IsDetailsVisible;
    }

    private void OnPrioritySelectClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string s && btn.BindingContext is TaskItem t)
            if (int.TryParse(s, out var idx))
                t.Priority = (TaskPriority)idx;
    }
}
