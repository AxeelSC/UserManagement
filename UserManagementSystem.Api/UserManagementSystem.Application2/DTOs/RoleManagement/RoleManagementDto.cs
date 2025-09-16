namespace UserManagementSystem.Application.DTOs.RoleManagement
{
    public class PromoteToManagerDto
    {
        public int UserId { get; set; }
        public int TeamId { get; set; }
    }

    public class ChangeRoleDto
    {
        public string NewRole { get; set; } = string.Empty;
    }

    public class RoleChangeRequestDto
    {
        public int UserId { get; set; }
        public string CurrentRole { get; set; } = string.Empty;
        public string NewRole { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class RolePermissionsDto
    {
        public string Role { get; set; } = string.Empty;
        public List<string> CanChangeTo { get; set; } = new();
        public List<string> CanBeChangedBy { get; set; } = new();
    }
}