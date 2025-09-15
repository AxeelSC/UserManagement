using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UserManagementSystem.Domain.Enums;

namespace UserManagementSystem.Application.DTOs.Teams
{
    // For API responses
    public class TeamRequestDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public TeamRequestStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ProcessedBy { get; set; }
        public string? ProcessingNotes { get; set; }
    }

    // For creating team requests
    public class CreateTeamRequestDto
    {
        public int TeamId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // For processing team requests
    public class ProcessTeamRequestDto
    {
        public bool Approve { get; set; } // true = approve, false = reject
        public string? Notes { get; set; }
    }
}
