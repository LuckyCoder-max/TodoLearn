using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using TodoLearn.Models;

namespace TodoLearn;

public partial class PlannedPage : ContentPage
{
    public ObservableCollection<TaskItem> DisplayTasks { get; } = new();

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly Dictionary<TaskItem, (string? Text, DateTime DueAt, TaskPriority Priority, bool IsCompleted)> _editBackups = new();

    public PlannedPage(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DisplayTasks.Clear();
        var now = DateTime.Now;
        await using var db = await _dbFactory.CreateDbContextAsync();
        var items = await db.Tasks
            .Where(t => t.DueAt > now)
            .OrderBy(t => t.DueAt)
            .ToListAsync();
        foreach (var it in items)
            DisplayTasks.Add(it);
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
            if (t.DueAt <= DateTime.Now)
                DisplayTasks.Remove(t);
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
            if (int.TryParse(s, out var idx))
                t.Priority = (TaskPriority)idx;
    }
}
