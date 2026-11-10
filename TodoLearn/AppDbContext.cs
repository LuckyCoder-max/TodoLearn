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
        public DbSet<UserAccount> UserAccounts { get; set; } = null!;
        public DbSet<ExternalLogin> ExternalLogins { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TaskItem>().HasMany(task => task.Steps).WithOne()
                .HasForeignKey(step => step.TaskItemId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TaskStep>().ToTable("TaskSteps");
            modelBuilder.Entity<TaskStep>().Property(step => step.Text).IsRequired();
            modelBuilder.Entity<TaskStep>().HasIndex(step => new { step.TaskItemId, step.Position });

            modelBuilder.Entity<TaskItem>().Ignore(t => t.IsEditing);
            modelBuilder.Entity<TaskItem>().Ignore(t => t.CategoryIndex);
            modelBuilder.Entity<TaskItem>().Ignore(t => t.CategoryName);
            modelBuilder.Entity<TaskItem>().Ignore(t => t.IsDetailsVisible);
            modelBuilder.Entity<TaskItem>().Ignore(t => t.IsDescriptionExpanded);
            modelBuilder.Entity<TaskItem>().Ignore(t => t.CanExpandDescription);
            modelBuilder.Entity<TaskItem>()
                .HasOne<UserAccount>()
                .WithMany(user => user.Tasks)
                .HasForeignKey(task => task.UserAccountId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TaskItem>().HasIndex(task => task.UserAccountId);

            modelBuilder.Entity<UserAccount>(entity =>
            {
                entity.ToTable("UserAccounts");
                entity.HasIndex(user => user.Username).IsUnique();
                entity.Property(user => user.Username).HasMaxLength(50).IsRequired();
                entity.Property(user => user.PasswordHash).IsRequired();
                entity.Property(user => user.PasswordSalt).IsRequired();
                entity.HasMany(user => user.ExternalLogins)
                    .WithOne(login => login.UserAccount)
                    .HasForeignKey(login => login.UserAccountId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ExternalLogin>(entity =>
            {
                entity.ToTable("ExternalLogins");
                entity.Property(login => login.Provider).HasMaxLength(32).IsRequired();
                entity.Property(login => login.Subject).HasMaxLength(255).IsRequired();
                entity.HasIndex(login => new { login.Provider, login.Subject }).IsUnique();
            });
        }
    }
}
