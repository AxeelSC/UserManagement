using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Application.Interfaces.Repositories;
using UserManagementSystem.Domain.Entities;
using UserManagementSystem.Infrastructure.Persistence;

namespace UserManagementSystem.Infrastructure.Repositories
{
    public class RoleRepository : BaseRepository<Role>, IRoleRepository
    {
        public RoleRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Role?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(r => r.Name == name);
        }

        public async Task<IEnumerable<Role>> GetRolesByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(r => r.UserRoles)
                .Where(r => r.UserRoles.Any(ur => ur.UserId == userId))
                .ToListAsync();
        }

        public async Task<bool> IsNameUniqueAsync(string name, int? excludeRoleId = null)
        {
            var query = _dbSet.Where(r => r.Name == name);

            if (excludeRoleId.HasValue)
                query = query.Where(r => r.Id != excludeRoleId.Value);

            return !await query.AnyAsync();
        }
    }
}