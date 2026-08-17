using System.Security.Cryptography.X509Certificates;
using cvAts.Class;
using Microsoft.EntityFrameworkCore;

namespace cvAts
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; }

        public DbSet<Mycv> Mycvs { get; set; }
        public DbSet<Token> Tokens { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Mycv>()
                .Property(c => c.Data)
                .HasColumnType("jsonb");

            modelBuilder.Entity<Mycv>()
                .HasOne(c => c.User)
                .WithMany(u => u.Mycvs)
                .HasForeignKey(c => c.UserId);

            modelBuilder.Entity<User>()
                 .HasOne(u => u.Token)
                 .WithOne(t => t.User)
                 .HasForeignKey<Token>(t => t.UserId);

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
