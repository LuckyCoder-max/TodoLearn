using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TodoLearn.ViewModels;

namespace TodoLearn
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

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "tasks.db");
            builder.Services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Factory creates a correctly-filtered ViewModel per tab.
            // AppShell receives it via constructor injection.
            builder.Services.AddSingleton<TaskListViewModelFactory>();
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<App>();

            builder.Logging.AddDebug();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                using var db = dbFactory.CreateDbContext();
                db.Database.EnsureCreated();
            }

            return app;
        }
    }
}
