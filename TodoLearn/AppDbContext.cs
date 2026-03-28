using Microsoft.EntityFrameworkCore;
using TodoLearn.Models;

namespace TodoLearn
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaskItem> Tasks { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TaskItem>().Ignore(t => t.IsEditing);
            modelBuilder.Entity<TaskItem>().Ignore(t => t.IsDetailsVisible);
        }
    }
}
