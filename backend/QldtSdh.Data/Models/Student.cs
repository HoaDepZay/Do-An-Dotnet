using System;
using System.Collections.Generic;

namespace QldtSdh.Data.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime DOB { get; set; }
        public string ProgrammeName { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty; // e.g., Studying, Suspended, Graduated

        // Navigation properties
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual ICollection<ThesisTopic> ThesisTopics { get; set; } = new List<ThesisTopic>();
        public virtual ICollection<Degree> Degrees { get; set; } = new List<Degree>();
        public virtual ICollection<Case> Cases { get; set; } = new List<Case>();
    }
}
