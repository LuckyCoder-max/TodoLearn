using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TodoLearn;
using ToDoListApp;
using System.IO;
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

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "tasks.db");
            builder.Services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<InactivityService>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<MainPage>();

            builder.Logging.AddDebug();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                using var db = dbFactory.CreateDbContext();
                db.Database.EnsureCreated();
                db.Database.ExecuteSqlRaw("""
                    CREATE TABLE IF NOT EXISTS "UserAccounts" (
                        "Id" INTEGER NOT NULL CONSTRAINT "PK_UserAccounts" PRIMARY KEY AUTOINCREMENT,
                        "Username" TEXT NOT NULL UNIQUE,
                        "PasswordHash" TEXT NOT NULL,
                        "PasswordSalt" TEXT NOT NULL
                    );
                    """);
            }

            return app;
        }
    }
}
