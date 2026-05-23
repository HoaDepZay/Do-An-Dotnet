using System;

namespace QldtSdh.Data.Models
{
    public class SearchAudit
    {
        public int AuditId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Keyword { get; set; } = string.Empty;
        public int? StudentId { get; set; } // Nullable, if they searched generally without selecting a student
        public DateTime SearchedAt { get; set; }

        // Navigation property
        public virtual Student? Student { get; set; }
    }
}
