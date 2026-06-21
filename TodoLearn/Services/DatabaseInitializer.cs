using Microsoft.EntityFrameworkCore;

namespace TodoLearn.Services
{
    public static class DatabaseInitializer
    {
        public static void Initialize(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = dbFactory.CreateDbContext();

            db.Database.EnsureCreated();

            CreateUsersTable(db);
            AddMissingTaskColumns(db);
            RemoveOldTaskColumns(db);
        }

        private static void CreateUsersTable(AppDbContext db)
        {
            Execute(db,
                """
                CREATE TABLE IF NOT EXISTS "Users" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
                    "Username" TEXT NOT NULL,
                    "PasswordHash" TEXT NOT NULL,
                    "CreatedAt" TEXT NOT NULL
                );
                """);

            Execute(db, "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Users_Username\" ON \"Users\" (\"Username\");");
        }

        private static void AddMissingTaskColumns(AppDbContext db)
        {
            var columns = GetTaskColumns(db);

            AddColumnIfMissing(db, columns, "CreatedAt", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");
            AddColumnIfMissing(db, columns, "DueAt", "TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP");
            AddColumnIfMissing(db, columns, "IsCompleted", "INTEGER NOT NULL DEFAULT 0");
            AddColumnIfMissing(db, columns, "Priority", "INTEGER NOT NULL DEFAULT 0");
            AddColumnIfMissing(db, columns, "Description", "TEXT NULL");
        }

        private static void RemoveOldTaskColumns(AppDbContext db)
        {
            var columns = GetTaskColumns(db);
            var hasOldColumns =
                columns.Contains("DueDate") ||
                columns.Contains("DueTime") ||
                columns.Contains("PriorityIndex");

            if (!hasOldColumns)
                return;

            using var transaction = db.Database.BeginTransaction();

            Execute(db,
                """
                CREATE TABLE "Tasks_clean" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Tasks" PRIMARY KEY AUTOINCREMENT,
                    "Text" TEXT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "DueAt" TEXT NOT NULL,
                    "IsCompleted" INTEGER NOT NULL,
                    "Priority" INTEGER NOT NULL,
                    "Description" TEXT NULL
                );
                """);

            Execute(db,
                """
                INSERT INTO "Tasks_clean" ("Id", "Text", "CreatedAt", "DueAt", "IsCompleted", "Priority", "Description")
                SELECT "Id", "Text", "CreatedAt", "DueAt", "IsCompleted", "Priority", "Description"
                FROM "Tasks";
                """);

            Execute(db, "DROP TABLE \"Tasks\";");
            Execute(db, "ALTER TABLE \"Tasks_clean\" RENAME TO \"Tasks\";");

            transaction.Commit();
        }

        private static void AddColumnIfMissing(
            AppDbContext db,
            HashSet<string> columns,
            string columnName,
            string columnDefinition)
        {
            if (columns.Contains(columnName))
                return;

            Execute(db, $"ALTER TABLE \"Tasks\" ADD COLUMN \"{columnName}\" {columnDefinition};");
            columns.Add(columnName);
        }

        private static HashSet<string> GetTaskColumns(AppDbContext db)
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "PRAGMA table_info(\"Tasks\");";

            OpenConnection(command);
            using var reader = command.ExecuteReader();

            while (reader.Read())
                columns.Add(reader.GetString(1));

            return columns;
        }

        private static void Execute(AppDbContext db, string sql)
        {
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            OpenConnection(command);
            command.ExecuteNonQuery();
        }

        private static void OpenConnection(System.Data.Common.DbCommand command)
        {
            if (command.Connection?.State != System.Data.ConnectionState.Open)
                command.Connection?.Open();
        }
    }
}
