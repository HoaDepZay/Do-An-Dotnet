using System;

namespace QldtSdh.Data.Models
{
    public class CaseStatusHistory
    {
        public int HistoryId { get; set; }
        public int CaseId { get; set; }
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; } = string.Empty;

        // Navigation properties
        public virtual Case? Case { get; set; }
    }
}
