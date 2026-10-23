using TodoLearn.Models;

namespace TodoLearn;

public partial class ImportantPage : ContentPage
{
	public ImportantPage()
	{
		InitializeComponent();
		TaskList.SetFilter(FilterType.Important);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await TaskList.LoadTasksAsync();
	}
}
