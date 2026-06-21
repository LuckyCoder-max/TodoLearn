using Microsoft.EntityFrameworkCore;
using TodoLearn.Models;

namespace TodoLearn.Services
{
    public class UserRepository
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public UserRepository(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<bool> CreateAsync(string username, string password)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            if (await db.Users.AnyAsync(u => u.Username == username))
                return false;

            var user = new User(username, password);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await FindByUsernameAsync(db, username);
        }

        public async Task<List<User>> GetAllAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Users.OrderBy(u => u.CreatedAt).ToListAsync();
        }

        public async Task<bool> UpdatePasswordAsync(string username, string newPassword)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var user = await FindByUsernameAsync(db, username);

            if (user == null)
                return false;

            user.SetPassword(newPassword);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(string username)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var user = await FindByUsernameAsync(db, username);

            if (user == null)
                return false;

            db.Users.Remove(user);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<User?> LoginAsync(string username, string password)
        {
            var user = await GetByUsernameAsync(username);
            return user?.VerifyPassword(password) == true ? user : null;
        }

        private static Task<User?> FindByUsernameAsync(AppDbContext db, string username)
        {
            return db.Users.FirstOrDefaultAsync(u => u.Username == username);
        }
    }
}
