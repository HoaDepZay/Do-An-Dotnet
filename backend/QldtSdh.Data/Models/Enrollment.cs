using System;
using System.Collections.Generic;

namespace QldtSdh.Data.Models
{
    public class Enrollment
    {
        public int EnrollmentId { get; set; }
        public int StudentId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int Credits { get; set; }
        public string EnrollStatus { get; set; } = string.Empty; // e.g., Enrolled, Completed, Failed
        public DateTime EnrolledAt { get; set; }

        // Navigation properties
        public virtual Student? Student { get; set; }
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
    }
}
