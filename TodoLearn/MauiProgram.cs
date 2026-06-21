using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TodoLearn;
using TodoLearn.Services;
using TodoLearn.Views;

namespace ToDoListApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            RegisterServices(builder.Services);
            builder.Logging.AddDebug();

            var app = builder.Build();
            DatabaseInitializer.Initialize(app.Services);

            return app;
        }

        private static void RegisterServices(IServiceCollection services)
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "tasks.db");

            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            services.AddSingleton<UserRepository>();
            services.AddSingleton<InactivityService>();

            services.AddTransient<LoginPage>();
            services.AddTransient<MainPage>();
            services.AddTransient<ImportantPage>();
            services.AddTransient<PlannedPage>();
            services.AddTransient<AllTasksPage>();
            services.AddTransient<AppShell>();
        }
    }
}
