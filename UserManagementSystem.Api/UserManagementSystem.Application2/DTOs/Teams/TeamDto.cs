using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagementSystem.Application.DTOs.Teams
{
    // For API responses
    public class TeamDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public UserSummaryDto? Manager { get; set; }
        public List<UserSummaryDto> Members { get; set; } = new();
        public int MemberCount { get; set; }
    }

    // For creating teams
    public class CreateTeamDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // For updating teams
    public class UpdateTeamDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // Simple team info
    public class TeamSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ManagerName { get; set; }
        public int MemberCount { get; set; }
    }
}
