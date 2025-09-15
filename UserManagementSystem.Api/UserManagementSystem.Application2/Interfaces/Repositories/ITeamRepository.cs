using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserManagementSystem.Domain.Entities;

namespace UserManagementSystem.Application.Interfaces.Repositories
{
    public interface ITeamRepository : IBaseRepository<Team>
    {
        Task<Team?> GetByNameAsync(string name);
        Task<Team?> GetWithUsersAsync(int teamId);
        Task<Team?> GetWithManagerAsync(int teamId);
        Task<User?> GetTeamManagerAsync(int teamId);
        Task<int> GetManagerCountForTeamAsync(int teamId);
    }
}
