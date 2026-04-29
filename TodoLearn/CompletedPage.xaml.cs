using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using TodoLearn.Models;

namespace TodoLearn;

public partial class CompletedPage : ContentPage
{
    public ObservableCollection<TaskItem> DisplayTasks { get; } = new();

    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CompletedPage(IDbContextFactory<AppDbContext> dbFactory)
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
        var items = await db.Tasks
            .Where(t => t.IsCompleted)
            .OrderByDescending(t => t.DueAt)
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

    private async void OnClearCompletedClicked(object? sender, EventArgs e)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var completed = await db.Tasks.Where(t => t.IsCompleted).ToListAsync();
        db.Tasks.RemoveRange(completed);
        await db.SaveChangesAsync();
        DisplayTasks.Clear();
    }
}
