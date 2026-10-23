using TodoLearn.Models;

namespace TodoLearn;

public partial class AllTasksPage : ContentPage
{
	public AllTasksPage()
	{
		InitializeComponent();
		TaskList.SetFilter(FilterType.All);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await TaskList.LoadTasksAsync();
	}
}
