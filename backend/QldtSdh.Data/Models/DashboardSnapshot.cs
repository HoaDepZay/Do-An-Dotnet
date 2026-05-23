using System;

namespace QldtSdh.Data.Models
{
    public class DashboardSnapshot
    {
        public int SnapshotId { get; set; }
        public string Semester { get; set; } = string.Empty; // e.g. HK1_2025_2026
        public string? ProgrammeName { get; set; } // Nullable, if captured for all programmes
        public DateTime GeneratedAt { get; set; }
        public string DataJson { get; set; } = string.Empty; // Store KPI values as JSON string
        public string Status { get; set; } = string.Empty; // e.g. Draft, Generated, Exported, Archived
    }
}
