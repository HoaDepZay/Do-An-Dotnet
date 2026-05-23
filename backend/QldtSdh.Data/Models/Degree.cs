using System;

namespace QldtSdh.Data.Models
{
    public class Degree
    {
        public int DegreeId { get; set; }
        public int StudentId { get; set; }
        public string DegreeNumber { get; set; } = string.Empty; // e.g. VB2026-0001
        public DateTime IssueDate { get; set; }
        public string Status { get; set; } = string.Empty; // e.g. Approved, Issued, Closed

        // Navigation properties
        public virtual Student? Student { get; set; }
    }
}
