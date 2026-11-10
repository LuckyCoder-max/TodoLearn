using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TodoLearn;
using TodoLearn.Models;
using TodoLearn.Services;

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var factory = new TestFactory(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
await using (var db = factory.CreateDbContext())
{
    await db.Database.EnsureCreatedAsync();
    db.UserAccounts.AddRange(new UserAccount { Username = "first", PasswordHash = "hash", PasswordSalt = "salt" },
        new UserAccount { Username = "second", PasswordHash = "hash", PasswordSalt = "salt" });
    await db.SaveChangesAsync();
}
var session = new AuthSessionService { CurrentAccountId = 1 };
var service = new TaskService(factory, session);
var task = await service.AddAsync("Roadmap", null, TaskPriority.Low, DateTime.Now.AddDays(1), steps:
    [new TaskStep { Text = " Plan " }, new TaskStep { Text = "Build" }]);
Check(task.Steps.Count == 2 && task.Steps.All(step => step.Id > 0), "New steps persisted with IDs");
Check(task.Steps.First().Text == "Plan", "Step text trimmed");
var loaded = (await new TaskService(factory, session).GetTasksAsync()).Single();
Check(loaded.Steps.Select(step => step.Text).SequenceEqual(new[] { "Plan", "Build" }), "Steps survive reload in order");
await service.SetStepCompletedAsync(task, task.Steps.First(), true);
Check(!task.IsCompleted && task.StepsProgress.StartsWith("1 of 2"), "Step progress independent of parent completion");
var draft = new TaskItem { Id = task.Id, UserAccountId = task.UserAccountId, Text = task.Text, CreatedAt = task.CreatedAt, DueAt = task.DueAt };
foreach (var step in task.Steps) draft.Steps.Add(step.Copy());
draft.Steps.First().Text = "Draft only";
Check(task.Steps.First().Text == "Plan", "Draft edits do not mutate original task");
draft.Steps.RemoveAt(0);
draft.Steps.First().Text = "Build and test";
draft.Steps.Add(new TaskStep { Text = "Ship" });
await service.UpdateAsync(draft);
loaded = (await new TaskService(factory, session).GetTasksAsync()).Single();
Check(loaded.Steps.Select(step => step.Text).SequenceEqual(new[] { "Build and test", "Ship" }), "Edit, remove and append saved together");
session.CurrentAccountId = 2;
Check((await new TaskService(factory, session).GetTasksAsync()).Count == 0, "Other account cannot load tasks or steps");
try
{
    await service.SetStepCompletedAsync(loaded, loaded.Steps.First(), true);
    throw new Exception("Cross-account update unexpectedly succeeded");
}
catch (InvalidOperationException) { Console.WriteLine("PASS: Cross-account step update rejected"); }
session.CurrentAccountId = 1;
loaded.IsCompleted = true;
await service.UpdateAsync(loaded);
await service.ClearCompletedAsync();
await using (var db = factory.CreateDbContext())
    Check(await db.Set<TaskStep>().CountAsync() == 0, "Bulk parent deletion cascades to steps");
Console.WriteLine("All subtask storage checks passed.");
var now = new DateTime(2026, 10, 30, 12, 0, 0);
var later = new TaskItem { Text = "Later", DueAt = now.AddHours(2) };
var next = new TaskItem { Text = "Next", DueAt = now.AddMinutes(5) };
var completed = new TaskItem { DueAt = now.AddMinutes(1), IsCompleted = true };
var overdue = new TaskItem { DueAt = now.AddMinutes(-1) };
var overview = DeadlineOverview.Create([later, completed, overdue, next], now);
Check(overview.NextTask == next && overview.OverdueCount == 1, "Nearest unfinished deadline selected; completed and overdue excluded");
overview = DeadlineOverview.Create([next], next.DueAt.AddSeconds(1));
Check(overview.NextTask is null && overview.OverdueCount == 1, "Expired deadline becomes overdue");
Check(DeadlineOverview.Create([], now).NextTask is null, "Empty overview supported");
Check(DeadlineOverview.Create([next], next.DueAt).NextTask == next, "Deadline at current time remains upcoming");

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine("PASS: " + name);
}

sealed class TestFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext() => new(options);
}

namespace TodoLearn.Services
{
    public sealed class AuthSessionService : IAccountSession
    {
        public int? CurrentAccountId { get; set; }
    }
}
