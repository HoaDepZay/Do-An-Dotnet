using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QldtSdh.Data;
using QldtSdh.Data.Models;
using QldtSdh.Shared;

namespace QldtSdh.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN, STAFF")]
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
                        UserName = System.Net.WebUtility.UrlDecode(Request.Headers["X-User-Name"].ToString() ?? "Admin"),
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
                    UserName = System.Net.WebUtility.UrlDecode(Request.Headers["X-User-Name"].ToString() ?? "Admin"),
                    Keyword = $"Xem hồ sơ HV: {student.StudentCode}",
                    StudentId = student.StudentId,
                    SearchedAt = DateTime.Now
                });
                _context.SaveChanges();
            }
            catch { /* Ignore audit logging errors */ }

            return Ok(profile);
        }

        // POST: api/student
        [HttpPost]
        public ActionResult<StudentDto> CreateStudent([FromBody] CreateStudentRequest request)
        {
            if (request == null) return BadRequest("Dữ liệu học viên bị trống.");
            if (string.IsNullOrWhiteSpace(request.StudentCode)) return BadRequest("Mã học viên không được để trống.");
            if (string.IsNullOrWhiteSpace(request.FullName)) return BadRequest("Họ và tên không được để trống.");

            var studentCode = request.StudentCode.Trim().ToUpper();
            if (_context.Students.Any(s => s.StudentCode == studentCode))
            {
                return BadRequest($"Mã học viên '{studentCode}' đã tồn tại trong hệ thống.");
            }

            var student = new Student
            {
                StudentCode = studentCode,
                FullName = request.FullName.Trim(),
                DOB = request.DOB,
                ProgrammeName = request.ProgrammeName ?? "Khoa học máy tính",
                CurrentStatus = request.CurrentStatus ?? "Studying"
            };

            _context.Students.Add(student);
            _context.SaveChanges();

            return Ok(new StudentDto
            {
                StudentId = student.StudentId,
                StudentCode = student.StudentCode,
                FullName = student.FullName,
                DOB = student.DOB,
                ProgrammeName = student.ProgrammeName,
                CurrentStatus = student.CurrentStatus
            });
        }

        // PUT: api/student/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateStudent(int id, [FromBody] UpdateStudentRequest request)
        {
            if (request == null) return BadRequest("Dữ liệu cập nhật bị trống.");
            if (string.IsNullOrWhiteSpace(request.FullName)) return BadRequest("Họ và tên không được để trống.");

            var student = _context.Students.Find(id);
            if (student == null)
            {
                return NotFound("Không tìm thấy học viên.");
            }

            student.FullName = request.FullName.Trim();
            student.DOB = request.DOB;
            student.ProgrammeName = request.ProgrammeName ?? "Khoa học máy tính";
            student.CurrentStatus = request.CurrentStatus ?? "Studying";

            _context.SaveChanges();

            return NoContent();
        }

        // POST: api/student/bulk
        [HttpPost("bulk")]
        public ActionResult<BulkImportResultDto> CreateStudentsBulk([FromBody] List<CreateStudentRequest> requests)
        {
            if (requests == null || !requests.Any()) return BadRequest("Dữ liệu danh sách nhập bị trống.");

            int successCount = 0;
            var errors = new List<string>();

            // Get existing student codes to prevent DB roundtrips in loop
            var existingCodes = _context.Students.Select(s => s.StudentCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var req in requests)
            {
                if (string.IsNullOrWhiteSpace(req.StudentCode))
                {
                    errors.Add("Học viên bị thiếu mã.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(req.FullName))
                {
                    errors.Add($"Học viên '{req.StudentCode}' bị thiếu họ và tên.");
                    continue;
                }

                var studentCode = req.StudentCode.Trim().ToUpper();
                if (existingCodes.Contains(studentCode))
                {
                    errors.Add($"Mã học viên '{studentCode}' đã tồn tại.");
                    continue;
                }

                try
                {
                    var student = new Student
                    {
                        StudentCode = studentCode,
                        FullName = req.FullName.Trim(),
                        DOB = req.DOB,
                        ProgrammeName = string.IsNullOrWhiteSpace(req.ProgrammeName) ? "Khoa học máy tính" : req.ProgrammeName.Trim(),
                        CurrentStatus = string.IsNullOrWhiteSpace(req.CurrentStatus) ? "Studying" : req.CurrentStatus.Trim()
                    };

                    _context.Students.Add(student);
                    existingCodes.Add(studentCode); // track locally for duplicates within the file itself
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Lỗi khi thêm học viên '{studentCode}': {ex.Message}");
                }
            }

            if (successCount > 0)
            {
                _context.SaveChanges();
            }

            return Ok(new BulkImportResultDto
            {
                SuccessCount = successCount,
                TotalCount = requests.Count,
                Errors = errors
            });
        }

        // POST: api/student/5/enrollment
        [HttpPost("{id}/enrollment")]
        public ActionResult AddEnrollment(int id, [FromBody] AddEnrollmentRequest request)
        {
            var student = _context.Students.Find(id);
            if (student == null) return NotFound("Không tìm thấy học viên.");
            if (string.IsNullOrWhiteSpace(request.CourseCode)) return BadRequest("Mã môn học không được để trống.");
            if (string.IsNullOrWhiteSpace(request.CourseName)) return BadRequest("Tên môn học không được để trống.");

            var enrollment = new Enrollment
            {
                StudentId = id,
                CourseCode = request.CourseCode.Trim().ToUpper(),
                CourseName = request.CourseName.Trim(),
                Credits = request.Credits,
                EnrollStatus = request.EnrollStatus,
                EnrolledAt = DateTime.Now
            };

            _context.Enrollments.Add(enrollment);
            _context.SaveChanges(); // Save to generate EnrollmentId

            // Add grades
            var grade1 = new Grade { EnrollmentId = enrollment.EnrollmentId, ComponentName = "Chuyên cần", Score = request.PracticeScore, Weight = 0.1, GradeStatus = "Approved" };
            var grade2 = new Grade { EnrollmentId = enrollment.EnrollmentId, ComponentName = "Giữa kỳ", Score = request.MidtermScore, Weight = 0.3, GradeStatus = "Approved" };
            var grade3 = new Grade { EnrollmentId = enrollment.EnrollmentId, ComponentName = "Cuối kỳ", Score = request.FinalScore, Weight = 0.6, GradeStatus = "Approved" };

            _context.Grades.AddRange(grade1, grade2, grade3);
            _context.SaveChanges();

            return Ok(new { Message = "Thêm kết quả học tập thành công!" });
        }

        // POST: api/student/5/invoice
        [HttpPost("{id}/invoice")]
        public ActionResult AddInvoice(int id, [FromBody] AddInvoiceRequest request)
        {
            var student = _context.Students.Find(id);
            if (student == null) return NotFound("Không tìm thấy học viên.");
            if (string.IsNullOrWhiteSpace(request.Semester)) return BadRequest("Học kỳ không được để trống.");
            if (string.IsNullOrWhiteSpace(request.InvoiceNo)) return BadRequest("Số hóa đơn không được để trống.");

            var invoiceNo = request.InvoiceNo.Trim().ToUpper();
            if (_context.Invoices.Any(i => i.InvoiceNo == invoiceNo))
            {
                return BadRequest($"Số hóa đơn '{invoiceNo}' đã tồn tại.");
            }

            var invoice = new Invoice
            {
                StudentId = id,
                Semester = request.Semester.Trim(),
                InvoiceNo = invoiceNo,
                TotalAmount = request.TotalAmount,
                Status = request.Status,
                DueDate = request.DueDate
            };

            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            if (request.AmountPaid > 0)
            {
                var payment = new Payment
                {
                    InvoiceId = invoice.InvoiceId,
                    PaymentNo = "PAY-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(1000, 9999),
                    Amount = request.AmountPaid,
                    PaidAt = DateTime.Now,
                    Method = "BankTransfer"
                };
                _context.Payments.Add(payment);
                _context.SaveChanges();
            }

            return Ok(new { Message = "Tạo hóa đơn học phí thành công!" });
        }

        // POST: api/student/5/thesis
        [HttpPost("{id}/thesis")]
        public ActionResult AddThesis(int id, [FromBody] AddThesisTopicRequest request)
        {
            var student = _context.Students.Find(id);
            if (student == null) return NotFound("Không tìm thấy học viên.");
            if (string.IsNullOrWhiteSpace(request.TopicCode)) return BadRequest("Mã đề tài không được để trống.");
            if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest("Tên đề tài không được để trống.");

            var topicCode = request.TopicCode.Trim().ToUpper();
            if (_context.ThesisTopics.Any(t => t.TopicCode == topicCode))
            {
                return BadRequest($"Mã đề tài '{topicCode}' đã tồn tại.");
            }

            var topic = new ThesisTopic
            {
                StudentId = id,
                TopicCode = topicCode,
                Title = request.Title.Trim(),
                AdvisorName = request.AdvisorName.Trim(),
                Status = request.Status
            };

            _context.ThesisTopics.Add(topic);
            _context.SaveChanges();

            if (request.FinalScore.HasValue)
            {
                var result = new DefenceResult
                {
                    TopicId = topic.TopicId,
                    FinalScore = request.FinalScore.Value,
                    ResultStatus = request.FinalScore.Value >= 5.0 ? "Pass" : "Fail",
                    DefenceDate = request.DefenceDate ?? DateTime.Now
                };
                _context.DefenceResults.Add(result);
                _context.SaveChanges();
            }

            return Ok(new { Message = "Thêm đề tài nghiên cứu thành công!" });
        }

        // POST: api/student/5/degree
        [HttpPost("{id}/degree")]
        [Authorize(Roles = "ADMIN")]
        public ActionResult AddDegree(int id, [FromBody] AddDegreeRequest request)
        {
            var student = _context.Students.Find(id);
            if (student == null) return NotFound("Không tìm thấy học viên.");
            if (string.IsNullOrWhiteSpace(request.DegreeNumber)) return BadRequest("Số hiệu văn bằng không được để trống.");

            var degreeNumber = request.DegreeNumber.Trim().ToUpper();
            if (_context.Degrees.Any(d => d.DegreeNumber == degreeNumber))
            {
                return BadRequest($"Số hiệu văn bằng '{degreeNumber}' đã tồn tại.");
            }

            var degree = new Degree
            {
                StudentId = id,
                DegreeNumber = degreeNumber,
                IssueDate = request.IssueDate,
                Status = request.Status
            };

            _context.Degrees.Add(degree);
            _context.SaveChanges();

            return Ok(new { Message = "Cấp phát văn bằng thành công!" });
        }
    }
}
