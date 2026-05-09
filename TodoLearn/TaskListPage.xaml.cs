using TodoLearn.ViewModels;

namespace TodoLearn
{
    /// <summary>
    /// Single page shared by all tabs.
    /// Code-behind is minimal — only wires up OnAppearing;
    /// all logic lives in TaskListViewModel.
    /// </summary>
    public partial class TaskListPage : ContentPage
    {
        private readonly TaskListViewModel _vm;

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
    }
}
