using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserManagementSystem.Application.DTOs;

namespace UserManagementSystem.Application.Services
{
    public interface IAuditLogService
    {
        Task<ApiResponse<List<AuditLogDto>>> GetAuditLogsByUserIdAsync(int userId);
        Task<ApiResponse<List<AuditLogDto>>> GetAuditLogsByActionAsync(string action);
        Task<ApiResponse<List<AuditLogDto>>> GetRecentAuditLogsAsync(int count = 100);
        Task LogActionAsync(int? userId, string action, string? metadata = null);
    }
}
