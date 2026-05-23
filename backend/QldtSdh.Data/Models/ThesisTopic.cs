using System.Collections.Generic;

namespace QldtSdh.Data.Models
{
    public class ThesisTopic
    {
        public int TopicId { get; set; }
        public string TopicCode { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // e.g., Proposed, Approved, InProgress, ReadyForDefence
        public string AdvisorName { get; set; } = string.Empty;

        // Navigation properties
        public virtual Student? Student { get; set; }
        public virtual ICollection<DefenceResult> DefenceResults { get; set; } = new List<DefenceResult>();
    }
}
