using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UserManagementSystem.Domain.Entities;
namespace UserManagementSystem.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Users
            var user = modelBuilder.Entity<User>();
            user.HasKey(x => x.Id);
            user.Property(x => x.Username).IsRequired().HasMaxLength(32);
            user.Property(x => x.Email).IsRequired().HasMaxLength(256);
            user.Property(x => x.PasswordHash).IsRequired().HasMaxLength(512);
            user.Property(x => x.CreatedAt).IsRequired();
            user.Property(x => x.IsActive).IsRequired();

            user.HasIndex(x => x.Username).IsUnique();
            user.HasIndex(x => x.Email).IsUnique();

            // Roles
            var role = modelBuilder.Entity<Role>();
            role.HasKey(x => x.Id);
            role.Property(x => x.Name).IsRequired().HasMaxLength(50);
            role.Property(x => x.Description).HasMaxLength(200);
            role.HasIndex(x => x.Name).IsUnique();

            // UserRoles (N:M)
            var userRole = modelBuilder.Entity<UserRole>();
            userRole.HasKey(x => new { x.UserId, x.RoleId });
            userRole.Property(x => x.AssignedAt).IsRequired();

            userRole.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            userRole.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // AuditLogs
            var audit = modelBuilder.Entity<AuditLog>();
            audit.HasKey(x => x.Id);
            audit.Property(x => x.Action).IsRequired().HasMaxLength(128);
            audit.Property(x => x.Metadata).HasMaxLength(2000);
            audit.Property(x => x.Timestamp).IsRequired();

            audit.HasOne(a => a.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull); // si borras el user, conservas el log
        }
    }
}