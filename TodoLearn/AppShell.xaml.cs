using TodoLearn.Models;
using TodoLearn.ViewModels;

namespace TodoLearn
{
    public partial class AppShell : Shell
    {
        public AppShell(TaskListViewModelFactory vmFactory)
        {
            InitializeComponent();

            CurrentItem = MakeFlyoutItem(vmFactory, "My Day",     "myday",     FilterType.MyDay);
            Items.Add(   MakeFlyoutItem(vmFactory, "Important",   "important", FilterType.Important));
            Items.Add(   MakeFlyoutItem(vmFactory, "Planned",     "planned",   FilterType.Planned));
            Items.Add(   MakeFlyoutItem(vmFactory, "All Tasks",   "alltasks",  FilterType.All));
            Items.Add(   MakeFlyoutItem(vmFactory, "Completed",   "completed", FilterType.Completed));
        }

        private static FlyoutItem MakeFlyoutItem(
            TaskListViewModelFactory vmFactory,
            string title, string route, FilterType filter)
        {
            var vm   = vmFactory.Create(filter);
            var page = new TaskListPage(vm) { Title = title };

            var item = new FlyoutItem { Title = title };
            item.Items.Add(new ShellContent { Route = route, Content = page });
            return item;
        }
    }
}
