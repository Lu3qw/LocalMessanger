using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalMessangerServer.EF
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Block> Blocks { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<UserLog> UserLogs { get; set; }
        public DbSet<ServerLog> ServerLogs { get; set; }

        public AppDbContext() { }

        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Block>()
                    .HasOne(b => b.Blocker)
                    .WithMany(u => u.BlocksInitiated)
                    .HasForeignKey(b => b.BlockerId)
                    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Block>()
                .HasOne(b => b.Blocked)
                .WithMany(u => u.BlocksReceived)
                .HasForeignKey(b => b.BlockedId)
                .OnDelete(DeleteBehavior.Restrict);
            
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseLazyLoadingProxies()
                .UseSqlServer(@"Server=localhost;Database=LocalMessanger;Trusted_Connection=True; TrustServerCertificate=True;");
        }
    }
}
