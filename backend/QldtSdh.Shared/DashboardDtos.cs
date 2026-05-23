using System;
using System.Collections.Generic;

namespace QldtSdh.Shared
{
    public class KpiDto
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public double Value { get; set; }
        public string ValueType { get; set; } = "Count"; // Count, Percentage, Currency
        public string Description { get; set; } = string.Empty;
        public string IconType { get; set; } = "Default"; // Student, Graduation, Tuition, Thesis, Case
    }

    public class DashboardSnapshotDto
    {
        public int SnapshotId { get; set; }
        public string Semester { get; set; } = string.Empty;
        public string? ProgrammeName { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, double> Kpis { get; set; } = new();
    }

    public class CreateSnapshotRequest
    {
        public string Semester { get; set; } = string.Empty;
        public string? ProgrammeName { get; set; }
    }
}
