using TodoLearn.Models;

namespace TodoLearn;

public partial class PlannedPage : ContentPage
{
	public PlannedPage()
	{
		InitializeComponent();
		TaskList.SetFilter(FilterType.Planned);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await TaskList.LoadTasksAsync();
	}
}
