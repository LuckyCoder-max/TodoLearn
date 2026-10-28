using TodoLearn.Models;

namespace TodoLearn.Views;

public sealed class CategoryPage : ContentPage
{
    private readonly TaskListView _taskList;

    public CategoryPage(TaskCategory? category)
    {
        Title = TaskCategories.Names[category is { } value ? (int)value + 1 : 0];
        _taskList = new TaskListView();
        _taskList.SetCategory(category);
        Content = _taskList;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _taskList.LoadTasksAsync();
    }
}
