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
    public class StudentController : ControllerBase
    {
        private readonly QldtSdhDbContext _context;

        public StudentController(QldtSdhDbContext context)
        {
            _context = context;
        }

        // GET: api/student
        [HttpGet]
        public ActionResult<IEnumerable<StudentDto>> GetStudents([FromQuery] string? search, [FromQuery] string? programme, [FromQuery] string? status)
        {
            IQueryable<Student> query = _context.Students;

            // Apply search text
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x => x.StudentCode.ToLower().Contains(s) || 
                                         x.FullName.ToLower().Contains(s));
            }

            // Apply programme filter
            if (!string.IsNullOrWhiteSpace(programme) && programme != "Tất cả")
            {
                query = query.Where(x => x.ProgrammeName == programme);
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả")
            {
                query = query.Where(x => x.CurrentStatus == status);
            }

            var students = query
                .OrderBy(x => x.FullName)
                .Select(x => new StudentDto
                {
                    StudentId = x.StudentId,
                    StudentCode = x.StudentCode,
                    FullName = x.FullName,
                    DOB = x.DOB,
                    ProgrammeName = x.ProgrammeName,
                    CurrentStatus = x.CurrentStatus
                })
                .ToList();

            // Log search audit if there's a search term
            if (!string.IsNullOrWhiteSpace(search))
            {
                try
                {
                    _context.SearchAudits.Add(new SearchAudit
                    {
                        UserName = Request.Headers["X-User-Name"].ToString() ?? "Admin",
                        Keyword = search,
                        SearchedAt = DateTime.Now
                    });
                    _context.SaveChanges();
                }
                catch { /* Ignore audit logging errors */ }
            }

            return Ok(students);
        }

        // GET: api/student/5/profile360
        [HttpGet("{id}/profile360")]
        public ActionResult<StudentProfile360Dto> GetStudentProfile360(int id)
        {
            var student = _context.Students
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Grades)
                .Include(s => s.Invoices)
                    .ThenInclude(i => i.Payments)
                .Include(s => s.ThesisTopics)
                    .ThenInclude(t => t.DefenceResults)
                .Include(s => s.Degrees)
                .Include(s => s.Cases)
                .FirstOrDefault(s => s.StudentId == id);

            if (student == null)
            {
                return NotFound(new { Message = $"Không tìm thấy học viên với ID = {id}" });
            }

            // 1. Calculate GPA & Completed Credits
            double totalScoreWeight = 0;
            int gpaCredits = 0;
            int completedCredits = 0;

            var enrollmentsList = new List<EnrollmentDto>();
            foreach (var enroll in student.Enrollments)
            {
                double finalScore = 0;
                var gradesList = enroll.Grades.ToList();
                if (gradesList.Any())
                {
                    finalScore = gradesList.Sum(g => g.Score * g.Weight);
                }

                enrollmentsList.Add(new EnrollmentDto
                {
                    EnrollmentId = enroll.EnrollmentId,
                    CourseCode = enroll.CourseCode,
                    CourseName = enroll.CourseName,
                    Credits = enroll.Credits,
                    EnrollStatus = enroll.EnrollStatus,
                    EnrolledAt = enroll.EnrolledAt,
                    MidtermScore = gradesList.FirstOrDefault(g => g.ComponentName.Contains("Giữa kỳ"))?.Score ?? 0,
                    PracticeScore = gradesList.FirstOrDefault(g => g.ComponentName.Contains("Chuyên cần"))?.Score ?? 0,
                    FinalScore = gradesList.FirstOrDefault(g => g.ComponentName.Contains("Cuối kỳ"))?.Score ?? 0,
                    AverageScore = Math.Round(finalScore, 2)
                });

                if (enroll.EnrollStatus == "Completed")
                {
                    completedCredits += enroll.Credits;
                    gpaCredits += enroll.Credits;
                    totalScoreWeight += finalScore * enroll.Credits;
                }
                else if (enroll.EnrollStatus == "Failed")
                {
                    gpaCredits += enroll.Credits;
                    totalScoreWeight += finalScore * enroll.Credits;
                }
            }

            var gpa = gpaCredits > 0 ? Math.Round(totalScoreWeight / gpaCredits, 2) : 0;

            // 2. Calculate Invoices & Total Debt
            var invoicesList = new List<InvoiceDto>();
            decimal totalDebt = 0;
            foreach (var inv in student.Invoices)
            {
                var totalPaid = inv.Payments.Sum(p => p.Amount);
                var remaining = inv.TotalAmount - totalPaid;

                if (inv.Status != "Draft")
                {
                    totalDebt += remaining;
                }

                invoicesList.Add(new InvoiceDto
                {
                    InvoiceId = inv.InvoiceId,
                    InvoiceNo = inv.InvoiceNo,
                    Semester = inv.Semester,
                    TotalAmount = inv.TotalAmount,
                    PaidAmount = totalPaid,
                    RemainingAmount = remaining,
                    Status = inv.Status,
                    DueDate = inv.DueDate,
                    PaymentsList = string.Join("; ", inv.Payments.Select(p => $"{p.Amount:N0}đ ({p.PaidAt:dd/MM/yyyy})"))
                });
            }

            // 3. Map Thesis Topics
            var thesisList = student.ThesisTopics.Select(t =>
            {
                var defResult = t.DefenceResults.FirstOrDefault();
                return new ThesisTopicDto
                {
                    TopicId = t.TopicId,
                    TopicCode = t.TopicCode,
                    Title = t.Title,
                    Status = t.Status,
                    AdvisorName = t.AdvisorName,
                    FinalScore = defResult?.FinalScore,
                    DefenceResultStatus = defResult?.ResultStatus,
                    DefenceDate = defResult?.DefenceDate
                };
            }).ToList();

            // 4. Map Degrees
            var degreesList = student.Degrees.Select(d => new DegreeDto
            {
                DegreeId = d.DegreeId,
                DegreeNumber = d.DegreeNumber,
                IssueDate = d.IssueDate,
                Status = d.Status
            }).ToList();

            // 5. Map Cases
            var casesList = student.Cases.Select(c => new CaseDto
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
            }).ToList();

            var profile = new StudentProfile360Dto
            {
                Student = new StudentDto
                {
                    StudentId = student.StudentId,
                    StudentCode = student.StudentCode,
                    FullName = student.FullName,
                    DOB = student.DOB,
                    ProgrammeName = student.ProgrammeName,
                    CurrentStatus = student.CurrentStatus
                },
                GPA = gpa,
                TotalCredits = completedCredits,
                TotalDebt = totalDebt,
                Enrollments = enrollmentsList,
                Invoices = invoicesList,
                ThesisTopics = thesisList,
                Degrees = degreesList,
                Cases = casesList
            };

            // Write Search Audit log for viewing profile
            try
            {
                _context.SearchAudits.Add(new SearchAudit
                {
                    UserName = Request.Headers["X-User-Name"].ToString() ?? "Admin",
                    Keyword = $"Xem hồ sơ HV: {student.StudentCode}",
                    StudentId = student.StudentId,
                    SearchedAt = DateTime.Now
                });
                _context.SaveChanges();
            }
            catch { /* Ignore audit logging errors */ }

            return Ok(profile);
        }
    }
}
