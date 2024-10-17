using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Botest
{
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .ToTable("users")
                .HasKey(u => u.Id);

            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=odnaglaziw.online;Port=5432;user id = postgres;Password=DsPs4N8gt3;");
            }
        }

        public async Task AddUserAsync(User user)
        {
            var userr = await GetUserByIdAsync(user.Id);
            if (userr == null)
            {

                await Users.AddAsync(user);
                await SaveChangesAsync();
            }
        }

        public async Task<User> GetUserByIdAsync(long id)
        {
            return await Users.FindAsync(id);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await Users.ToListAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            Users.Update(user);
            await SaveChangesAsync();
        }

        public async Task DeleteUserAsync(long id)
        {
            var user = await GetUserByIdAsync(id);
            if (user != null)
            {
                Users.Remove(user);
                await SaveChangesAsync();
            }
        }

        public class User
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string LastName { get; set; }
            public string State { get; set; }
            public string Group { get; set; }
        }
    }
}
