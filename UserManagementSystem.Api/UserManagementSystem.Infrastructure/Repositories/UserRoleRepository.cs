using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Application.Interfaces.Repositories;
using UserManagementSystem.Domain.Entities;
using UserManagementSystem.Infrastructure.Persistence;

namespace UserManagementSystem.Infrastructure.Repositories
{
    public class UserRoleRepository : BaseRepository<UserRole>, IUserRoleRepository
    {
        public UserRoleRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<UserRole?> GetByUserAndRoleAsync(int userId, int roleId)
        {
            return await _dbSet
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
        }

        public async Task<IEnumerable<UserRole>> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserRole>> GetByRoleIdAsync(int roleId)
        {
            return await _dbSet
                .Include(ur => ur.User)
                .Where(ur => ur.RoleId == roleId)
                .ToListAsync();
        }

        public async Task RemoveUserFromRoleAsync(int userId, int roleId)
        {
            var userRole = await GetByUserAndRoleAsync(userId, roleId);
            if (userRole != null)
            {
                _dbSet.Remove(userRole);
            }
        }

        public async Task RemoveAllUserRolesAsync(int userId)
        {
            var userRoles = await GetByUserIdAsync(userId);
            _dbSet.RemoveRange(userRoles);
        }
    }
}