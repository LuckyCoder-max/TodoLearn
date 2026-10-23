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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TaskItem>().Ignore(t => t.IsEditing);
            modelBuilder.Entity<TaskItem>().Ignore(t => t.IsDetailsVisible);
            modelBuilder.Entity<TaskItem>().Ignore(t => t.IsDescriptionExpanded);

            modelBuilder.Entity<UserAccount>(entity =>
            {
                entity.ToTable("UserAccounts");
                entity.HasIndex(user => user.Username).IsUnique();
                entity.Property(user => user.Username).HasMaxLength(50).IsRequired();
                entity.Property(user => user.PasswordHash).IsRequired();
                entity.Property(user => user.PasswordSalt).IsRequired();
            });
        }
    }
}
