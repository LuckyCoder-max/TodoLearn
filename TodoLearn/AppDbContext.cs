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
        public DbSet<User> Users { get; set; } = null!;  

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var task = modelBuilder.Entity<TaskItem>();
            task.Ignore(t => t.IsEditing);
            task.Ignore(t => t.DueDate);
            task.Ignore(t => t.DueTime);
            task.Ignore(t => t.PriorityIndex);
            task.Ignore(t => t.HasDescription);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
        }
    }
}
