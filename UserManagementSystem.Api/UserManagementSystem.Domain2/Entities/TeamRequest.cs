using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserManagementSystem.Domain.Enums;

namespace UserManagementSystem.Domain.Entities
{
    public class TeamRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TeamId { get; set; }
        public string Message { get; set; } = string.Empty;
        public TeamRequestStatus Status { get; set; } = TeamRequestStatus.Pending;
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public int? ProcessedByUserId { get; set; }
        public string? ProcessingNotes { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public Team Team { get; set; } = null!;
        public User? ProcessedByUser { get; set; }
    }
}
