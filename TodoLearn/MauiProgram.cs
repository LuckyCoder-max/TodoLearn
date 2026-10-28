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
            builder.Services.AddSingleton<AuthSessionService>();
            builder.Services.AddSingleton<IGoogleSignInService, GoogleSignInService>();
            builder.Services.AddSingleton<TaskService>();
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
                db.Database.ExecuteSqlRaw("""
                    CREATE TABLE IF NOT EXISTS "ExternalLogins" (
                        "Id" INTEGER NOT NULL CONSTRAINT "PK_ExternalLogins" PRIMARY KEY AUTOINCREMENT,
                        "UserAccountId" INTEGER NOT NULL,
                        "Provider" TEXT NOT NULL,
                        "Subject" TEXT NOT NULL,
                        CONSTRAINT "FK_ExternalLogins_UserAccounts_UserAccountId"
                            FOREIGN KEY ("UserAccountId") REFERENCES "UserAccounts" ("Id") ON DELETE CASCADE
                    );
                    """);
                db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_ExternalLogins_Provider_Subject\" ON \"ExternalLogins\" (\"Provider\", \"Subject\");");
                EnsureTaskDescriptionColumn(db);
                EnsureTaskAccountColumn(db);
            }

            return app;
        }

        private static void EnsureTaskDescriptionColumn(AppDbContext db)
        {
            var columnExists = db.Database
                .SqlQueryRaw<long>("SELECT COUNT(*) AS Value FROM pragma_table_info('Tasks') WHERE name = 'Description'")
                .Single() > 0;

            if (!columnExists)
                db.Database.ExecuteSqlRaw("ALTER TABLE \"Tasks\" ADD COLUMN \"Description\" TEXT NULL;");
        }
        private static void EnsureTaskAccountColumn(AppDbContext db)
        {
            var columnExists = db.Database
                .SqlQueryRaw<long>("SELECT COUNT(*) AS Value FROM pragma_table_info('Tasks') WHERE name = 'UserAccountId'")
                .Single() > 0;

            if (!columnExists)
                db.Database.ExecuteSqlRaw("ALTER TABLE \"Tasks\" ADD COLUMN \"UserAccountId\" INTEGER NULL REFERENCES \"UserAccounts\" (\"Id\") ON DELETE CASCADE;");

            db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS \"IX_Tasks_UserAccountId\" ON \"Tasks\" (\"UserAccountId\");");
        }
    }
}
