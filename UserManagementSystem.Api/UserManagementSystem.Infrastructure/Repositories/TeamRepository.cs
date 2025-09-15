using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Application.Interfaces.Repositories;
using UserManagementSystem.Domain.Entities;
using UserManagementSystem.Infrastructure.Persistence;

namespace UserManagementSystem.Infrastructure.Repositories
{
    public class TeamRepository : BaseRepository<Team>, ITeamRepository
    {
        public TeamRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Team?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(t => t.Name == name);
        }

        public async Task<Team?> GetWithUsersAsync(int teamId)
        {
            return await _dbSet
                .Include(t => t.Users)
                .FirstOrDefaultAsync(t => t.Id == teamId);
        }

        public async Task<Team?> GetWithManagerAsync(int teamId)
        {
            return await _dbSet
                .Include(t => t.Users)
                .ThenInclude(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(t => t.Id == teamId);
        }

        public async Task<User?> GetTeamManagerAsync(int teamId)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.TeamId == teamId &&
                                     u.UserRoles.Any(ur => ur.Role.Name == "Manager"));
        }

        public async Task<int> GetManagerCountForTeamAsync(int teamId)
        {
            return await _context.Users
                .Where(u => u.TeamId == teamId)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Manager"))
                .CountAsync();
        }
    }
}