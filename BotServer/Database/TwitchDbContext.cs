

using BotServer.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BotServer.Database
{
    public class TwitchDbContext : DbContext
    {
        public DbSet<Users> Users { get; set; }

        public DbSet<UserTokens> Tokens { get; set; }

        public DbSet<JoinedChannels> JoinedChannels { get; set; }

        public TwitchDbContext(DbContextOptions options) : base(options)
        {
        }

        protected TwitchDbContext()
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Users>().HasOne(u => u.Puntos).WithOne(p => p.User).HasForeignKey<Puntos>(p => p.UsersId);

            modelBuilder.Entity<Users>().HasOne(u => u.Tokens).WithOne(t => t.User).HasForeignKey<UserTokens>(t => t.UsersId);

            modelBuilder.Entity<JoinedChannels>().HasIndex(c => c.ChannelName).IsUnique();
        }
    }
}
