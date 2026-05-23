using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QldtSdh.Data;
using QldtSdh.Data.Models;
using QldtSdh.Shared;

namespace QldtSdh.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly QldtSdhDbContext _context;

        public DashboardController(QldtSdhDbContext context)
        {
            _context = context;
        }

        // GET: api/dashboard/kpis
        [HttpGet("kpis")]
        public ActionResult<IEnumerable<KpiDto>> GetKpis()
        {
            var kpis = CalculateKpis();
            return Ok(kpis);
        }

        // GET: api/dashboard/kpi-details/TOTAL_STUDENTS
        [HttpGet("kpi-details/{kpiKey}")]
        public ActionResult<IEnumerable<StudentDto>> GetKpiDetails(string kpiKey)
        {
            IQueryable<Student> query = _context.Students;

            switch (kpiKey.ToUpper())
            {
                case "TOTAL_STUDENTS":
                    // Return all
                    break;
                case "STUDENTS_STUDYING":
                    query = query.Where(s => s.CurrentStatus == "Studying");
                    break;
                case "STUDENTS_GRADUATED":
                    query = query.Where(s => s.CurrentStatus == "Graduated");
                    break;
                case "STUDENTS_SUSPENDED":
                    query = query.Where(s => s.CurrentStatus == "Suspended");
                    break;
                case "CASES_ACTIVE":
                    // Students who have active cases
                    var activeStudentIds = _context.Cases
                        .Where(c => c.Status != "Closed")
                        .Select(c => c.StudentId)
                        .Distinct()
                        .ToList();
                    query = query.Where(s => activeStudentIds.Contains(s.StudentId));
                    break;
                case "CASES_OVERDUE":
                    // Students who have overdue cases
                    var overdueStudentIds = _context.Cases
                        .Where(c => c.Status != "Closed" && c.DueDate.HasValue && c.DueDate < DateTime.Now)
                        .Select(c => c.StudentId)
                        .Distinct()
                        .ToList();
                    query = query.Where(s => overdueStudentIds.Contains(s.StudentId));
                    break;
                case "TUITION_UNPAID":
                    // Students with unpaid/partially paid invoices
                    var debtorStudentIds = _context.Invoices
                        .Where(i => i.Status != "Paid" && i.Status != "Draft")
                        .Select(i => i.StudentId)
                        .Distinct()
                        .ToList();
                    query = query.Where(s => debtorStudentIds.Contains(s.StudentId));
                    break;
                case "THESIS_ACTIVE":
                    // Students with InProgress or Approved thesis topics
                    var thesisStudentIds = _context.ThesisTopics
                        .Where(t => t.Status == "InProgress" || t.Status == "Approved")
                        .Select(t => t.StudentId)
                        .Distinct()
                        .ToList();
                    query = query.Where(s => thesisStudentIds.Contains(s.StudentId));
                    break;
                default:
                    // For GPA or Defence Score average, just return students with grades
                    break;
            }

            var students = query
                .OrderBy(s => s.FullName)
                .Select(s => new StudentDto
                {
                    StudentId = s.StudentId,
                    StudentCode = s.StudentCode,
                    FullName = s.FullName,
                    DOB = s.DOB,
                    ProgrammeName = s.ProgrammeName,
                    CurrentStatus = s.CurrentStatus
                })
                .ToList();

            return Ok(students);
        }

        // GET: api/dashboard/kpi-details-cases/CASES_OVERDUE
        [HttpGet("kpi-details-cases/{kpiKey}")]
        public ActionResult<IEnumerable<CaseDto>> GetKpiDetailsCases(string kpiKey)
        {
            IQueryable<Case> query = _context.Cases.Include(c => c.Student);

            switch (kpiKey.ToUpper())
            {
                case "CASES_ACTIVE":
                    query = query.Where(c => c.Status != "Closed");
                    break;
                case "CASES_OVERDUE":
                    query = query.Where(c => c.Status != "Closed" && c.DueDate.HasValue && c.DueDate < DateTime.Now);
                    break;
                default:
                    return BadRequest("Không hỗ trợ xem danh sách Case cho KPI này.");
            }

            var cases = query
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

        // POST: api/dashboard/snapshots
        [HttpPost("snapshots")]
        public ActionResult<DashboardSnapshotDto> CreateSnapshot([FromBody] CreateSnapshotRequest request)
        {
            if (request == null) return BadRequest("Dữ liệu yêu cầu trống.");

            var currentKpis = CalculateKpis();
            var kpiDictionary = currentKpis.ToDictionary(k => k.Key, k => k.Value);

            var json = JsonSerializer.Serialize(kpiDictionary);

            var snapshot = new DashboardSnapshot
            {
                Semester = request.Semester,
                ProgrammeName = request.ProgrammeName ?? "Tất cả",
                GeneratedAt = DateTime.Now,
                DataJson = json,
                Status = "Draft" // Initial state for Snapshot: Draft -> Generated -> Exported -> Archived
            };

            _context.DashboardSnapshots.Add(snapshot);
            _context.SaveChanges();

            // Auto-transition to Generated
            snapshot.Status = "Generated";
            _context.SaveChanges();

            return Ok(new DashboardSnapshotDto
            {
                SnapshotId = snapshot.SnapshotId,
                Semester = snapshot.Semester,
                ProgrammeName = snapshot.ProgrammeName,
                GeneratedAt = snapshot.GeneratedAt,
                Status = snapshot.Status,
                Kpis = kpiDictionary
            });
        }

        // GET: api/dashboard/snapshots
        [HttpGet("snapshots")]
        public ActionResult<IEnumerable<DashboardSnapshotDto>> GetSnapshots()
        {
            var list = _context.DashboardSnapshots
                .OrderByDescending(s => s.GeneratedAt)
                .ToList()
                .Select(s => {
                    Dictionary<string, double> kpis;
                    try
                    {
                        kpis = JsonSerializer.Deserialize<Dictionary<string, double>>(s.DataJson) ?? new();
                    }
                    catch
                    {
                        kpis = new();
                    }
                    return new DashboardSnapshotDto
                    {
                        SnapshotId = s.SnapshotId,
                        Semester = s.Semester,
                        ProgrammeName = s.ProgrammeName,
                        GeneratedAt = s.GeneratedAt,
                        Status = s.Status,
                        Kpis = kpis
                    };
                }).ToList();

            return Ok(list);
        }

        private List<KpiDto> CalculateKpis()
        {
            var list = new List<KpiDto>();

            // 1. Total Students
            var totalStudents = _context.Students.Count();
            list.Add(new KpiDto
            {
                Key = "TOTAL_STUDENTS",
                Title = "Tổng số học viên",
                Value = totalStudents,
                ValueType = "Count",
                Description = "Tổng số học viên đăng ký trong hệ thống đào tạo.",
                IconType = "Student"
            });

            // 2. Studying
            var studying = _context.Students.Count(s => s.CurrentStatus == "Studying");
            list.Add(new KpiDto
            {
                Key = "STUDENTS_STUDYING",
                Title = "Học viên đang học",
                Value = studying,
                ValueType = "Count",
                Description = "Học viên có trạng thái học vụ đang học tích cực.",
                IconType = "Student"
            });

            // 3. Graduated
            var graduated = _context.Students.Count(s => s.CurrentStatus == "Graduated");
            list.Add(new KpiDto
            {
                Key = "STUDENTS_GRADUATED",
                Title = "Học viên tốt nghiệp",
                Value = graduated,
                ValueType = "Count",
                Description = "Học viên đã bảo vệ luận án thành công và nhận bằng.",
                IconType = "Graduation"
            });

            // 4. Suspended
            var suspended = _context.Students.Count(s => s.CurrentStatus == "Suspended");
            list.Add(new KpiDto
            {
                Key = "STUDENTS_SUSPENDED",
                Title = "Học viên tạm dừng",
                Value = suspended,
                ValueType = "Count",
                Description = "Học viên đang tạm ngưng học tập hoặc bảo lưu.",
                IconType = "Student"
            });

            // 5. Active Cases
            var activeCases = _context.Cases.Count(c => c.Status != "Closed");
            list.Add(new KpiDto
            {
                Key = "CASES_ACTIVE",
                Title = "Case đang xử lý",
                Value = activeCases,
                ValueType = "Count",
                Description = "Các yêu cầu hỗ trợ sự vụ chưa được đóng lại.",
                IconType = "Case"
            });

            // 6. Overdue Cases
            var overdueCases = _context.Cases.Count(c => c.Status != "Closed" && c.DueDate.HasValue && c.DueDate < DateTime.Now);
            list.Add(new KpiDto
            {
                Key = "CASES_OVERDUE",
                Title = "Case quá hạn",
                Value = overdueCases,
                ValueType = "Count",
                Description = "Các case chưa hoàn thành đã quá hạn giải quyết.",
                IconType = "Case"
            });

            // 7. Average GPA of completed enrollments
            // Course average score: Sum(Score * Weight)
            // GPA = Sum(Course average score * Course Credits) / Sum(Course Credits)
            // For simplicity in SQL, we can calculate GPA in memory
            var completedGrades = _context.Grades
                .Include(g => g.Enrollment)
                .Where(g => g.Enrollment != null && g.Enrollment.EnrollStatus == "Completed")
                .ToList();

            double avgGpa = 0;
            if (completedGrades.Any())
            {
                var grouped = completedGrades.GroupBy(g => g.EnrollmentId);
                double totalScoreWeight = 0;
                int totalCredits = 0;

                foreach (var group in grouped)
                {
                    var firstEnroll = group.First().Enrollment!;
                    var courseScore = group.Sum(g => g.Score * g.Weight);
                    totalScoreWeight += courseScore * firstEnroll.Credits;
                    totalCredits += firstEnroll.Credits;
                }

                if (totalCredits > 0)
                {
                    avgGpa = totalScoreWeight / totalCredits;
                }
            }

            list.Add(new KpiDto
            {
                Key = "GRADUATED_GPA_AVG",
                Title = "Điểm GPA trung bình",
                Value = Math.Round(avgGpa, 2),
                ValueType = "Count", // or decimal
                Description = "Điểm GPA trung bình tích lũy của các học viên.",
                IconType = "Graduation"
            });

            // 8. Total unpaid tuition debt
            // Unpaid tuition = sum of invoices total - sum of payments
            var invoicesTotal = _context.Invoices.Where(i => i.Status != "Draft").Sum(i => i.TotalAmount);
            var paymentsTotal = _context.Payments.Include(p => p.Invoice).Where(p => p.Invoice != null && p.Invoice.Status != "Draft").Sum(p => p.Amount);
            var unpaidTuition = invoicesTotal - paymentsTotal;

            list.Add(new KpiDto
            {
                Key = "TUITION_UNPAID",
                Title = "Tổng nợ học phí",
                Value = (double)unpaidTuition,
                ValueType = "Currency",
                Description = "Tổng nợ công học phí của học viên chưa thanh toán.",
                IconType = "Tuition"
            });

            // 9. Active Thesis Topics (InProgress, Approved)
            var activeThesis = _context.ThesisTopics.Count(t => t.Status == "InProgress" || t.Status == "Approved");
            list.Add(new KpiDto
            {
                Key = "THESIS_ACTIVE",
                Title = "Đề tài đang thực hiện",
                Value = activeThesis,
                ValueType = "Count",
                Description = "Số đề tài luận văn/luận án thạc sĩ, tiến sĩ đang nghiên cứu.",
                IconType = "Thesis"
            });

            // 10. Avg defense score
            var avgDefenceScore = _context.DefenceResults.Any() ? _context.DefenceResults.Average(d => d.FinalScore) : 0;
            list.Add(new KpiDto
            {
                Key = "DEFENCE_SCORE_AVG",
                Title = "Điểm bảo vệ trung bình",
                Value = Math.Round(avgDefenceScore, 2),
                ValueType = "Count",
                Description = "Điểm số trung bình trong các phiên bảo vệ luận văn.",
                IconType = "Thesis"
            });

            return list;
        }
    }
}
