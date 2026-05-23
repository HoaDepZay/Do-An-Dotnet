namespace QldtSdh.Data.Models
{
    public class Grade
    {
        public int GradeId { get; set; }
        public int EnrollmentId { get; set; }
        public string ComponentName { get; set; } = string.Empty; // e.g., Midterm, Practice, Final
        public double Score { get; set; }
        public double Weight { get; set; } // e.g., 0.3, 0.2, 0.5
        public string GradeStatus { get; set; } = string.Empty; // e.g., Draft, Submitted, Approved, Published

        // Navigation properties
        public virtual Enrollment? Enrollment { get; set; }
    }
}
