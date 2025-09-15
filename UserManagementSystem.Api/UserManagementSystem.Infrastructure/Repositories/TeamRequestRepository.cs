using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Application.Interfaces.Repositories;
using UserManagementSystem.Domain.Entities;
using UserManagementSystem.Domain.Enums;
using UserManagementSystem.Infrastructure.Persistence;

namespace UserManagementSystem.Infrastructure.Repositories
{
    public class TeamRequestRepository : BaseRepository<TeamRequest>, ITeamRequestRepository
    {
        public TeamRequestRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<TeamRequest>> GetPendingRequestsForTeamAsync(int teamId)
        {
            return await _dbSet
                .Include(tr => tr.User)
                .Include(tr => tr.Team)
                .Where(tr => tr.TeamId == teamId && tr.Status == TeamRequestStatus.Pending)
                .OrderByDescending(tr => tr.RequestedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<TeamRequest>> GetRequestsByUserAsync(int userId)
        {
            return await _dbSet
                .Include(tr => tr.Team)
                .Include(tr => tr.ProcessedByUser)
                .Where(tr => tr.UserId == userId)
                .OrderByDescending(tr => tr.RequestedAt)
                .ToListAsync();
        }

        public async Task<TeamRequest?> GetPendingRequestByUserAndTeamAsync(int userId, int teamId)
        {
            return await _dbSet
                .Include(tr => tr.User)
                .Include(tr => tr.Team)
                .FirstOrDefaultAsync(tr => tr.UserId == userId &&
                                     tr.TeamId == teamId &&
                                     tr.Status == TeamRequestStatus.Pending);
        }
    }
}