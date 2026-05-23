using System;
using System.Collections.Generic;

namespace QldtSdh.Data.Models
{
    public class Case
    {
        public int CaseId { get; set; }
        public string CaseCode { get; set; } = string.Empty; // e.g. CASE-2026-0001
        public string CaseType { get; set; } = string.Empty; // e.g. Học tập, Học phí, Luận văn, Khác
        public int StudentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty; // e.g. Low, Medium, High, Critical
        public string Status { get; set; } = string.Empty; // e.g. Created, Assigned, Processing, Closed
        public string? Assignee { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual Student? Student { get; set; }
        public virtual ICollection<CaseStatusHistory> StatusHistories { get; set; } = new List<CaseStatusHistory>();
        public virtual ICollection<CaseNote> Notes { get; set; } = new List<CaseNote>();
    }
}
