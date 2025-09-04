using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserManagementSystem.Domain.Entities;

namespace UserManagementSystem.Application.Interfaces.Repositories
{
    public interface IUserRoleRepository : IBaseRepository<UserRole>
    {
        Task<UserRole?> GetByUserAndRoleAsync(int userId, int roleId);
        Task<IEnumerable<UserRole>> GetByUserIdAsync(int userId);
        Task<IEnumerable<UserRole>> GetByRoleIdAsync(int roleId);
        Task RemoveUserFromRoleAsync(int userId, int roleId);
        Task RemoveAllUserRolesAsync(int userId);
    }
}
