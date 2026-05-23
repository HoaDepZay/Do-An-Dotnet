using System;

namespace QldtSdh.Shared
{
    public class CaseDto
    {
        public int CaseId { get; set; }
        public string CaseCode { get; set; } = string.Empty;
        public string CaseType { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Assignee { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsOverdue => DueDate.HasValue && DueDate < DateTime.Now && Status != "Closed";
    }

    public class CaseStatusHistoryDto
    {
        public int HistoryId { get; set; }
        public int CaseId { get; set; }
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
    }

    public class CaseNoteDto
    {
        public int NoteId { get; set; }
        public int CaseId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class CreateCaseRequest
    {
        public int StudentId { get; set; }
        public string CaseType { get; set; } = string.Empty; // Học tập, Học phí, Luận văn, Khác
        public string Title { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty; // Low, Medium, High, Critical
        public string? Assignee { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class UpdateCaseStatusRequest
    {
        public string NewStatus { get; set; } = string.Empty; // Created, Assigned, Processing, Closed
        public string User { get; set; } = string.Empty;
    }

    public class CreateCaseNoteRequest
    {
        public string Content { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
    }

    public class CaseDetailResponse
    {
        public CaseDto Case { get; set; } = null!;
        public List<CaseNoteDto> Notes { get; set; } = new();
        public List<CaseStatusHistoryDto> StatusHistories { get; set; } = new();
    }
}
