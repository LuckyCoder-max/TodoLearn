using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TodoLearn.Models;
using TodoLearn.Services;

namespace TodoLearn.Tests;

public sealed class TestClock : TimeProvider
{
    private DateTimeOffset _now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => _now;
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    public void Advance(TimeSpan duration) => _now += duration;
    public DateTime Now => GetLocalNow().DateTime;
}

public sealed class TestSession : IAccountSession
{
    public int? CurrentAccountId { get; set; } = 1;
}

public sealed class TestDatabase : IDbContextFactory<AppDbContext>, IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private DbContextOptions<AppDbContext> _options = null!;
    public int ContextsCreated { get; private set; }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();
        db.UserAccounts.AddRange(
            new UserAccount { Username = "first", PasswordHash = "hash", PasswordSalt = "salt" },
            new UserAccount { Username = "second", PasswordHash = "hash", PasswordSalt = "salt" });
        await db.SaveChangesAsync();
    }

    public AppDbContext CreateDbContext()
    {
        ContextsCreated++;
        return new AppDbContext(_options);
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}

public abstract class DatabaseTest
{
    protected TestDatabase Database = null!;
    protected TestSession Session = null!;
    protected TestClock Clock = null!;
    protected TaskService Tasks = null!;

    [SetUp]
    public async Task SetUpDatabase()
    {
        Database = new TestDatabase();
        await Database.InitializeAsync();
        Session = new TestSession();
        Clock = new TestClock();
        Tasks = new TaskService(Database, Session, Clock);
    }

    [TearDown]
    public async Task TearDownDatabase() => await Database.DisposeAsync();

    protected Task<TaskItem> AddTask(string title = "Task", TaskCategory? category = null,
        IEnumerable<TaskStep>? steps = null) =>
        Tasks.AddAsync(title, "Description", TaskPriority.High, Clock.Now.AddMinutes(5), category, steps);

    protected async Task<TaskItem> Reload(int id)
    {
        await using var db = Database.CreateDbContext();
        return await db.Tasks.AsNoTracking().Include(task => task.Steps.OrderBy(step => step.Position))
            .SingleAsync(task => task.Id == id);
    }
}
