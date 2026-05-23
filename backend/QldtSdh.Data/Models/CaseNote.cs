using System;

namespace QldtSdh.Data.Models
{
    public class CaseNote
    {
        public int NoteId { get; set; }
        public int CaseId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public virtual Case? Case { get; set; }
    }
}
