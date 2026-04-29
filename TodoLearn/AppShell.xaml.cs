namespace TodoLearn
{
    public partial class AppShell : Shell
    {
        public AppShell(IServiceProvider services)
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            Routing.RegisterRoute(nameof(AllTasksPage), typeof(AllTasksPage));
            Routing.RegisterRoute(nameof(ImportantPage), typeof(ImportantPage));
            Routing.RegisterRoute(nameof(PlannedPage), typeof(PlannedPage));
            Routing.RegisterRoute(nameof(CompletedPage), typeof(CompletedPage));

            CurrentItem = CreateFlyoutItem(services.GetRequiredService<MainPage>, "My Day", "myday");
            Items.Add(CreateFlyoutItem(services.GetRequiredService<ImportantPage>, "Important", "important"));
            Items.Add(CreateFlyoutItem(services.GetRequiredService<PlannedPage>, "Planned", "planned"));
            Items.Add(CreateFlyoutItem(services.GetRequiredService<AllTasksPage>, "All Tasks", "alltasks"));
            Items.Add(CreateFlyoutItem(services.GetRequiredService<CompletedPage>, "Completed", "completed"));
        }

        private static FlyoutItem CreateFlyoutItem<TPage>(Func<TPage> factory, string title, string route)
            where TPage : Page
        {
            var item = new FlyoutItem { Title = title };
            var content = new ShellContent
            {
                Route = route,
                Content = factory()  
            };
            item.Items.Add(content);
            return item;
        }
    }

}
