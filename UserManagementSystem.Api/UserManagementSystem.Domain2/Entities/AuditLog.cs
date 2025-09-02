using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagementSystem.Domain.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public string Action { get; set; } = default!;
        public string? Metadata { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
