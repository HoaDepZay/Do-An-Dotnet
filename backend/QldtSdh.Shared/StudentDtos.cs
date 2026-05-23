using System;
using System.Collections.Generic;

namespace QldtSdh.Shared
{
    public class StudentDto
    {
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime DOB { get; set; }
        public string ProgrammeName { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
    }

    public class StudentProfile360Dto
    {
        public StudentDto Student { get; set; } = null!;
        public double GPA { get; set; }
        public int TotalCredits { get; set; }
        public decimal TotalDebt { get; set; }
        
        public List<EnrollmentDto> Enrollments { get; set; } = new();
        public List<InvoiceDto> Invoices { get; set; } = new();
        public List<ThesisTopicDto> ThesisTopics { get; set; } = new();
        public List<DegreeDto> Degrees { get; set; } = new();
        public List<CaseDto> Cases { get; set; } = new();
    }

    public class EnrollmentDto
    {
        public int EnrollmentId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int Credits { get; set; }
        public string EnrollStatus { get; set; } = string.Empty;
        public DateTime EnrolledAt { get; set; }
        
        public double MidtermScore { get; set; }
        public double PracticeScore { get; set; }
        public double FinalScore { get; set; }
        public double AverageScore { get; set; }
    }

    public class InvoiceDto
    {
        public int InvoiceId { get; set; }
        public string Semester { get; set; } = string.Empty;
        public string InvoiceNo { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public string PaymentsList { get; set; } = string.Empty;
    }

    public class ThesisTopicDto
    {
        public int TopicId { get; set; }
        public string TopicCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AdvisorName { get; set; } = string.Empty;
        public double? FinalScore { get; set; }
        public string? DefenceResultStatus { get; set; }
        public DateTime? DefenceDate { get; set; }
    }

    public class DegreeDto
    {
        public int DegreeId { get; set; }
        public string DegreeNumber { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
