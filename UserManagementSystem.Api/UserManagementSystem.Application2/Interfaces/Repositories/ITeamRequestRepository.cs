using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserManagementSystem.Domain.Entities;

namespace UserManagementSystem.Application.Interfaces.Repositories
{
    public interface ITeamRequestRepository : IBaseRepository<TeamRequest>
    {
        Task<IEnumerable<TeamRequest>> GetPendingRequestsForTeamAsync(int teamId);
        Task<IEnumerable<TeamRequest>> GetRequestsByUserAsync(int userId);
        Task<TeamRequest?> GetPendingRequestByUserAndTeamAsync(int userId, int teamId);
    }
}
