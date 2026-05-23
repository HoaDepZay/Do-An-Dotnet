using System;

namespace QldtSdh.Data.Models
{
    public class DefenceResult
    {
        public int ResultId { get; set; }
        public int TopicId { get; set; }
        public double FinalScore { get; set; }
        public string ResultStatus { get; set; } = string.Empty; // Pass / Fail
        public DateTime DefenceDate { get; set; }

        // Navigation properties
        public virtual ThesisTopic? ThesisTopic { get; set; }
    }
}
