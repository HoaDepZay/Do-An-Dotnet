using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QldtSdh.Data;
using QldtSdh.Data.Models;
using QldtSdh.Shared;

namespace QldtSdh.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CaseController : ControllerBase
    {
        private readonly QldtSdhDbContext _context;

        public CaseController(QldtSdhDbContext context)
        {
            _context = context;
        }

        // GET: api/case
        [HttpGet]
        public ActionResult<IEnumerable<CaseDto>> GetCases()
        {
            var cases = _context.Cases
                .Include(c => c.Student)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CaseDto
                {
                    CaseId = c.CaseId,
                    CaseCode = c.CaseCode,
                    CaseType = c.CaseType,
                    StudentId = c.StudentId,
                    StudentName = c.Student != null ? c.Student.FullName : "N/A",
                    StudentCode = c.Student != null ? c.Student.StudentCode : "N/A",
                    Title = c.Title,
                    Priority = c.Priority,
                    Status = c.Status,
                    Assignee = c.Assignee,
                    DueDate = c.DueDate,
                    CreatedAt = c.CreatedAt
                })
                .ToList();

            return Ok(cases);
        }

        // GET: api/case/5
        [HttpGet("{id}")]
        public ActionResult<CaseDetailResponse> GetCaseDetails(int id)
        {
            var c = _context.Cases
                .Include(x => x.Student)
                .Include(x => x.Notes)
                .Include(x => x.StatusHistories)
                .FirstOrDefault(x => x.CaseId == id);

            if (c == null)
            {
                return NotFound(new { Message = $"Không tìm thấy Case với ID = {id}" });
            }

            var response = new CaseDetailResponse
            {
                Case = new CaseDto
                {
                    CaseId = c.CaseId,
                    CaseCode = c.CaseCode,
                    CaseType = c.CaseType,
                    StudentId = c.StudentId,
                    StudentName = c.Student != null ? c.Student.FullName : "N/A",
                    StudentCode = c.Student != null ? c.Student.StudentCode : "N/A",
                    Title = c.Title,
                    Priority = c.Priority,
                    Status = c.Status,
                    Assignee = c.Assignee,
                    DueDate = c.DueDate,
                    CreatedAt = c.CreatedAt
                },
                Notes = c.Notes
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new CaseNoteDto
                    {
                        NoteId = n.NoteId,
                        CaseId = n.CaseId,
                        Content = n.Content,
                        CreatedAt = n.CreatedAt,
                        CreatedBy = n.CreatedBy
                    }).ToList(),
                StatusHistories = c.StatusHistories
                    .OrderBy(h => h.ChangedAt)
                    .Select(h => new CaseStatusHistoryDto
                    {
                        HistoryId = h.HistoryId,
                        CaseId = h.CaseId,
                        OldStatus = h.OldStatus,
                        NewStatus = h.NewStatus,
                        ChangedAt = h.ChangedAt,
                        ChangedBy = h.ChangedBy
                    }).ToList()
            };

            return Ok(response);
        }

        // POST: api/case
        [HttpPost]
        public ActionResult<CaseDto> CreateCase([FromBody] CreateCaseRequest request)
        {
            if (request == null) return BadRequest("Dữ liệu yêu cầu không hợp lệ.");

            var student = _context.Students.Find(request.StudentId);
            if (student == null) return BadRequest("Không tìm thấy học viên tương ứng.");

            // Generate unique Case Code
            var count = _context.Cases.Count(x => x.CreatedAt.Year == DateTime.Now.Year);
            var caseCode = $"CASE-{DateTime.Now.Year}-{(count + 1):D4}";

            var c = new Case
            {
                CaseCode = caseCode,
                CaseType = request.CaseType,
                StudentId = request.StudentId,
                Title = request.Title,
                Priority = request.Priority,
                Status = "Created", // initial state
                Assignee = request.Assignee,
                DueDate = request.DueDate,
                CreatedAt = DateTime.Now
            };

            _context.Cases.Add(c);
            _context.SaveChanges();

            // Record initial history log
            _context.CaseStatusHistories.Add(new CaseStatusHistory
            {
                CaseId = c.CaseId,
                OldStatus = "",
                NewStatus = "Created",
                ChangedAt = DateTime.Now,
                ChangedBy = "Hệ thống"
            });
            _context.SaveChanges();

            var dto = new CaseDto
            {
                CaseId = c.CaseId,
                CaseCode = c.CaseCode,
                CaseType = c.CaseType,
                StudentId = c.StudentId,
                StudentName = student.FullName,
                StudentCode = student.StudentCode,
                Title = c.Title,
                Priority = c.Priority,
                Status = c.Status,
                Assignee = c.Assignee,
                DueDate = c.DueDate,
                CreatedAt = c.CreatedAt
            };

            return CreatedAtAction(nameof(GetCaseDetails), new { id = c.CaseId }, dto);
        }

        // PUT: api/case/5/assign
        [HttpPut("{id}/assign")]
        public IActionResult AssignCase(int id, [FromBody] string assignee)
        {
            var c = _context.Cases.Find(id);
            if (c == null) return NotFound("Không tìm thấy Case.");

            var oldStatus = c.Status;
            c.Assignee = assignee;

            // Automatically transition status from Created to Assigned if an assignee is provided
            if (c.Status == "Created" && !string.IsNullOrEmpty(assignee))
            {
                c.Status = "Assigned";

                _context.CaseStatusHistories.Add(new CaseStatusHistory
                {
                    CaseId = c.CaseId,
                    OldStatus = oldStatus,
                    NewStatus = "Assigned",
                    ChangedAt = DateTime.Now,
                    ChangedBy = "Admin"
                });
            }

            _context.SaveChanges();
            return Ok(new { Message = "Đã gán người xử lý thành công.", Case = c });
        }

        // PUT: api/case/5/status
        [HttpPut("{id}/status")]
        public IActionResult UpdateStatus(int id, [FromBody] UpdateCaseStatusRequest request)
        {
            if (request == null) return BadRequest("Dữ liệu yêu cầu trống.");

            var c = _context.Cases
                .Include(x => x.Notes)
                .FirstOrDefault(x => x.CaseId == id);

            if (c == null) return NotFound("Không tìm thấy Case.");

            var oldStatus = c.Status;
            var newStatus = request.NewStatus;

            // RULE 1: Only the assigned user (or Admin) can transition to Processing or Closed
            if (newStatus == "Processing" || newStatus == "Closed")
            {
                if (c.Assignee != request.User && request.User != "Admin")
                {
                    return BadRequest($"Chỉ cán bộ được phân công ({c.Assignee}) mới được phép chuyển Case sang trạng thái {newStatus}.");
                }
            }

            // RULE 2: Cannot close Case without at least one note containing "kết luận" or "hoàn thành"
            if (newStatus == "Closed")
            {
                var hasClosingNote = c.Notes.Any(n => 
                    n.Content.ToLower().Contains("kết luận") || 
                    n.Content.ToLower().Contains("hoàn thành") || 
                    n.Content.ToLower().Contains("conclude") || 
                    n.Content.ToLower().Contains("resolve"));

                if (!hasClosingNote)
                {
                    return BadRequest("Không thể đóng Case. Yêu cầu phải có ít nhất 1 ghi chú (CaseNote) chứa nội dung 'kết luận' hoặc 'hoàn thành' trước khi đóng.");
                }
            }

            // Update status and log history
            c.Status = newStatus;
            _context.CaseStatusHistories.Add(new CaseStatusHistory
            {
                CaseId = c.CaseId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedAt = DateTime.Now,
                ChangedBy = request.User
            });

            _context.SaveChanges();
            return Ok(new { Message = $"Đã chuyển trạng thái sang {newStatus} thành công.", CurrentStatus = newStatus });
        }

        // POST: api/case/5/notes
        [HttpPost("{id}/notes")]
        public ActionResult<CaseNoteDto> AddNote(int id, [FromBody] CreateCaseNoteRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Content))
                return BadRequest("Nội dung ghi chú không được trống.");

            var c = _context.Cases.Find(id);
            if (c == null) return NotFound("Không tìm thấy Case.");

            var note = new CaseNote
            {
                CaseId = id,
                Content = request.Content,
                CreatedAt = DateTime.Now,
                CreatedBy = request.User
            };

            _context.CaseNotes.Add(note);
            _context.SaveChanges();

            return Ok(new CaseNoteDto
            {
                NoteId = note.NoteId,
                CaseId = note.CaseId,
                Content = note.Content,
                CreatedAt = note.CreatedAt,
                CreatedBy = note.CreatedBy
            });
        }
    }
}
