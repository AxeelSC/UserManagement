using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserManagementSystem.Domain.Enums;
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
        public DbSet<Team> Teams => Set<Team>();                    
        public DbSet<TeamRequest> TeamRequests => Set<TeamRequest>();
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

            //User-team relationship
            user.HasOne(u => u.Team)
              .WithMany(t => t.Users)
              .HasForeignKey(u => u.TeamId)
              .OnDelete(DeleteBehavior.SetNull);

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
                .OnDelete(DeleteBehavior.SetNull); // If user is deleted, you still have the log

            //Teams configuration
            var team = modelBuilder.Entity<Team>();
            team.HasKey(x => x.Id);
            team.Property(x => x.Name).IsRequired().HasMaxLength(100);
            team.Property(x => x.Description).HasMaxLength(500);
            team.Property(x => x.CreatedAt).IsRequired();
            team.HasIndex(x => x.Name).IsUnique();

            //TeamRequests configuration
            var teamRequest = modelBuilder.Entity<TeamRequest>();
            teamRequest.HasKey(x => x.Id);
            teamRequest.Property(x => x.Message).HasMaxLength(500);
            teamRequest.Property(x => x.Status).IsRequired();
            teamRequest.Property(x => x.RequestedAt).IsRequired();
            teamRequest.Property(x => x.ProcessingNotes).HasMaxLength(500);

            teamRequest.HasOne(tr => tr.User)
                .WithMany(u => u.TeamRequests)
                .HasForeignKey(tr => tr.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            teamRequest.HasOne(tr => tr.Team)
                .WithMany(t => t.TeamRequests)
                .HasForeignKey(tr => tr.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            teamRequest.HasOne(tr => tr.ProcessedByUser)
                .WithMany(u => u.ProcessedTeamRequests)
                .HasForeignKey(tr => tr.ProcessedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", Description = "Full system access and user management" },
                new Role { Id = 2, Name = "Manager", Description = "Manage users and view reports" },
                new Role { Id = 3, Name = "User", Description = "Standard user access" },
                new Role { Id = 4, Name = "Viewer", Description = "Read-only access" }
            );

            modelBuilder.Entity<Team>().HasData(
                new Team { Id = 1, Name = "Finance", Description = "Finance Department", CreatedAt = new DateTime(2025, 1, 15) },
                new Team { Id = 2, Name = "HR", Description = "Human Resources Department", CreatedAt = new DateTime(2025, 1, 15) },
                new Team { Id = 3, Name = "IT", Description = "Information Technology Department", CreatedAt = new DateTime(2025, 1, 15) },
                new Team { Id = 4, Name = "Marketing", Description = "Marketing Department", CreatedAt = new DateTime(2025, 1, 15) }
            );
        }
    }
}