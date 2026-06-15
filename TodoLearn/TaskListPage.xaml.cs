using TodoLearn.Models;
using TodoLearn.ViewModels;

namespace TodoLearn
{
    public partial class TaskListPage : ContentPage
    {
        private readonly TaskListViewModel _vm;

        private static readonly (string Label, string Field, string Dir)[] SortOptions =
        {
            ("Date Created",   "Created",  "Newest first"),
            ("Date Created",   "Created",  "Oldest first"),
            ("Due Date",       "Due",      "Soonest first"),
            ("Due Date",       "Due",      "Latest first"),
            ("Name",           "Name",     "A → Z"),
            ("Name",           "Name",     "Z → A"),
            ("Priority",       "Priority", "High first"),
            ("Priority",       "Priority", "Low first"),
        };

        private static readonly SortType[] SortValues =
        {
            SortType.CreatedDesc,
            SortType.CreatedAsc,
            SortType.DueAsc,
            SortType.DueDesc,
            SortType.NameAsc,
            SortType.NameDesc,
            SortType.PriorityDesc,
            SortType.PriorityAsc,
        };

        public TaskListPage(TaskListViewModel viewModel)
        {
            _vm = viewModel;
            BindingContext = _vm;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.LoadAsync();
        }

        private async void OnSortButtonClicked(object sender, EventArgs e)
        {
            string? result = await DisplayActionSheetAsync(
                "Sort by",
                "Cancel",
                null,
                "Date Created: Newest first",
                "Date Created: Oldest first",
                "Due Date: Soonest first",
                "Due Date: Latest first",
                "Name: A → Z",
                "Name: Z → A",
                "Priority: High first",
                "Priority: Low first"
            );

            if (result is null || result == "Cancel") return;

            var index = result switch
            {
                "Date Created: Newest first" => 0,
                "Date Created: Oldest first" => 1,
                "Due Date: Soonest first"    => 2,
                "Due Date: Latest first"     => 3,
                "Name: A → Z"               => 4,
                "Name: Z → A"               => 5,
                "Priority: High first"       => 6,
                "Priority: Low first"        => 7,
                _                            => -1
            };

            if (index >= 0)
                _vm.CurrentSort = SortValues[index];
        }
    }
}
