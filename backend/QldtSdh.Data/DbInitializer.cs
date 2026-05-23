using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QldtSdh.Data.Models;

namespace QldtSdh.Data
{
    public static class DbInitializer
    {
        public static void Initialize(QldtSdhDbContext context)
        {
            // Ensure database is created
            context.Database.EnsureCreated();

            // Ensure roles and users tables are created in existing DB
            CreateTablesIfNotExist(context);

            // Seed Roles
            if (!context.Roles.Any())
            {
                var adminRole = new Role { RoleCode = "ADMIN", RoleName = "Quản trị viên" };
                var staffRole = new Role { RoleCode = "STAFF", RoleName = "Cán bộ đào tạo" };
                context.Roles.AddRange(adminRole, staffRole);
                context.SaveChanges();
            }

            // Seed Users
            if (!context.Users.Any())
            {
                var adminRole = context.Roles.First(r => r.RoleCode == "ADMIN");
                var staffRole = context.Roles.First(r => r.RoleCode == "STAFF");

                var usersToSeed = new List<User>
                {
                    new User
                    {
                        Username = "admin",
                        PasswordHash = HashPassword("admin123"),
                        FullName = "Quản trị viên",
                        Email = "admin@qldtsdh.edu.vn",
                        RoleId = adminRole.RoleId,
                        IsActive = true
                    },
                    new User
                    {
                        Username = "canboA",
                        PasswordHash = HashPassword("canbo123"),
                        FullName = "Cán bộ A",
                        Email = "canboa@qldtsdh.edu.vn",
                        RoleId = staffRole.RoleId,
                        IsActive = true
                    },
                    new User
                    {
                        Username = "canboB",
                        PasswordHash = HashPassword("canbo123"),
                        FullName = "Cán bộ B",
                        Email = "canbob@qldtsdh.edu.vn",
                        RoleId = staffRole.RoleId,
                        IsActive = true
                    }
                };
                context.Users.AddRange(usersToSeed);
                context.SaveChanges();
            }

            // Look for any students.
            if (context.Students.Any())
            {
                return;   // DB has been seeded
            }

            // --- SEED STUDENTS ---
            var firstNames = new[] { "Nguyễn Văn", "Trần Thị", "Lê Văn", "Phạm Thị", "Hoàng", "Phan Văn", "Vũ Thị", "Đặng", "Bùi Văn", "Đỗ Thị" };
            var lastNames = new[] { "An", "Bình", "Cường", "Dũng", "Em", "Giang", "Hương", "Hải", "Khánh", "Linh", "Minh", "Nam", "Oanh", "Phong", "Quang", "Sơn", "Trang", "Tuấn", "Vy", "Yến" };
            var programmes = new[] { "Khoa học máy tính", "Hệ thống thông tin", "Kỹ thuật phần mềm", "An toàn thông tin" };
            var studentStatuses = new[] { "Studying", "Studying", "Studying", "Suspended", "Graduated" }; // higher weight for Studying

            var rand = new Random(13); // Seed for reproducibility (Nhóm 13)
            var students = new List<Student>();

            for (int i = 1; i <= 30; i++) // Seed 30 students to satisfy "tối thiểu 20 học viên"
            {
                var fName = firstNames[rand.Next(firstNames.Length)];
                var lName = lastNames[rand.Next(lastNames.Length)];
                var student = new Student
                {
                    StudentCode = $"SDH{2020 + rand.Next(5)}{i:D4}",
                    FullName = $"{fName} {lName}",
                    DOB = new DateTime(1990 + rand.Next(12), rand.Next(12) + 1, rand.Next(28) + 1),
                    ProgrammeName = programmes[rand.Next(programmes.Length)],
                    CurrentStatus = studentStatuses[i % studentStatuses.Length] // deterministic distribution
                };
                students.Add(student);
            }

            context.Students.AddRange(students);
            context.SaveChanges();

            // --- SEED ENROLLMENTS & GRADES ---
            var courses = new[]
            {
                new { Code = "CH101", Name = "Phương pháp nghiên cứu khoa học", Credits = 3 },
                new { Code = "CH102", Name = "Cơ sở dữ liệu nâng cao", Credits = 4 },
                new { Code = "CH103", Name = "Kiến trúc phần mềm nâng cao", Credits = 3 },
                new { Code = "CH104", Name = "Trí tuệ nhân tạo", Credits = 4 },
                new { Code = "CH105", Name = "An toàn thông tin nâng cao", Credits = 3 }
            };

            var enrollments = new List<Enrollment>();
            foreach (var student in students)
            {
                // Each student has 2 to 4 enrollments
                int numCourses = rand.Next(3) + 2;
                var selectedCourses = courses.OrderBy(x => rand.Next()).Take(numCourses).ToList();

                foreach (var course in selectedCourses)
                {
                    var isCompleted = student.CurrentStatus == "Graduated" || (student.CurrentStatus == "Studying" && rand.Next(10) > 1);
                    var enroll = new Enrollment
                    {
                        StudentId = student.StudentId,
                        CourseCode = course.Code,
                        CourseName = course.Name,
                        Credits = course.Credits,
                        EnrollStatus = isCompleted ? "Completed" : (student.CurrentStatus == "Suspended" ? "Enrolled" : (rand.Next(2) == 0 ? "Enrolled" : "Failed")),
                        EnrolledAt = DateTime.Now.AddMonths(-rand.Next(12) - 6)
                    };
                    enrollments.Add(enroll);
                }
            }

            context.Enrollments.AddRange(enrollments);
            context.SaveChanges();

            // Seed Grades for each Enrollment
            var grades = new List<Grade>();
            foreach (var enroll in enrollments)
            {
                var componentWeights = new[]
                {
                    new { Name = "Chuyên cần", Weight = 0.1 },
                    new { Name = "Kiểm tra giữa kỳ", Weight = 0.3 },
                    new { Name = "Thi cuối kỳ", Weight = 0.6 }
                };

                bool hasGrades = enroll.EnrollStatus == "Completed" || enroll.EnrollStatus == "Failed";
                
                foreach (var comp in componentWeights)
                {
                    double score = 0;
                    if (hasGrades)
                    {
                        // Completed usually has passing scores, Failed might have low scores
                        score = enroll.EnrollStatus == "Completed" 
                            ? rand.Next(50, 51) / 10.0 + 5.0 // 5.0 to 10.0
                            : rand.Next(0, 50) / 10.0;       // 0.0 to 5.0
                    }
                    else
                    {
                        score = rand.Next(0, 101) / 10.0; // Draft grade or random
                    }

                    grades.Add(new Grade
                    {
                        EnrollmentId = enroll.EnrollmentId,
                        ComponentName = comp.Name,
                        Score = Math.Round(score, 1),
                        Weight = comp.Weight,
                        GradeStatus = hasGrades ? "Published" : "Draft"
                    });
                }
            }

            context.Grades.AddRange(grades);
            context.SaveChanges();

            // --- SEED INVOICES & PAYMENTS ---
            var invoices = new List<Invoice>();
            var semesters = new[] { "HK1_2024_2025", "HK2_2024_2025", "HK1_2025_2026" };

            foreach (var student in students)
            {
                // Each student has 1 or 2 invoices
                int numInvoices = rand.Next(2) + 1;
                for (int j = 0; j < numInvoices; j++)
                {
                    var sem = semesters[j];
                    var invoiceNo = $"INV-{DateTime.Now.Year}-{student.StudentId:D3}{j}";
                    var totalAmount = (decimal)(rand.Next(3) + 2) * 3500000m; // 7M, 10.5M, 14M

                    // Determine invoice status based on student status
                    string status = "Issued";
                    if (student.CurrentStatus == "Graduated") status = "Paid";
                    else if (student.CurrentStatus == "Suspended") status = "PartiallyPaid";
                    else status = (new[] { "Paid", "Paid", "PartiallyPaid", "Issued", "Draft" })[rand.Next(5)];

                    var invoice = new Invoice
                    {
                        StudentId = student.StudentId,
                        Semester = sem,
                        InvoiceNo = invoiceNo,
                        TotalAmount = totalAmount,
                        Status = status,
                        DueDate = DateTime.Now.AddDays(rand.Next(30) - 15) // some overdue, some not
                    };
                    invoices.Add(invoice);
                }
            }

            context.Invoices.AddRange(invoices);
            context.SaveChanges();

            // Seed Payments for Invoices
            var payments = new List<Payment>();
            foreach (var inv in invoices)
            {
                if (inv.Status == "Paid")
                {
                    payments.Add(new Payment
                    {
                        InvoiceId = inv.InvoiceId,
                        PaymentNo = $"PAY-{DateTime.Now.Year}-{inv.InvoiceId:D4}1",
                        Amount = inv.TotalAmount,
                        PaidAt = inv.DueDate.AddDays(-rand.Next(10)),
                        Method = rand.Next(2) == 0 ? "BankTransfer" : "Cash"
                    });
                }
                else if (inv.Status == "PartiallyPaid")
                {
                    var partialAmount = Math.Round(inv.TotalAmount / 2, 0);
                    payments.Add(new Payment
                    {
                        InvoiceId = inv.InvoiceId,
                        PaymentNo = $"PAY-{DateTime.Now.Year}-{inv.InvoiceId:D4}1",
                        Amount = partialAmount,
                        PaidAt = inv.DueDate.AddDays(-rand.Next(10)),
                        Method = "BankTransfer"
                    });
                }
                else if (inv.Status == "Issued" && rand.Next(3) == 0) // some minor refund or failed payment log
                {
                    // Simulated refund payment
                    payments.Add(new Payment
                    {
                        InvoiceId = inv.InvoiceId,
                        PaymentNo = $"PAY-{DateTime.Now.Year}-{inv.InvoiceId:D4}REF",
                        Amount = -500000m,
                        PaidAt = DateTime.Now.AddDays(-1),
                        Method = "Refund"
                    });
                }
            }

            context.Payments.AddRange(payments);
            context.SaveChanges();

            // --- SEED THESIS TOPICS & DEFENCE RESULTS ---
            var thesisTitles = new[]
            {
                "Nghiên cứu kiến trúc Microservices cho hệ thống thương mại điện tử lớn",
                "Ứng dụng Deep Learning trong nhận diện khuôn mặt và chấm công tự động",
                "Xây dựng hệ thống tối ưu hóa định tuyến giao hàng chặng cuối dùng thuật toán Kiến",
                "Phát hiện xâm nhập mạng sử dụng thuật toán Học máy và phân tích luồng dữ liệu",
                "Giải pháp bảo mật blockchain cho quản lý chuỗi cung ứng nông sản xuất khẩu",
                "Tích hợp công nghệ thực tế tăng cường (AR) trong giáo dục kỹ thuật sau đại học",
                "Xử lý ngôn ngữ tự nhiên ứng dụng trong tóm tắt văn bản pháp luật tiếng Việt",
                "Thiết kế hệ thống ERP mini hỗ trợ quản lý dự án cho doanh nghiệp vừa và nhỏ",
                "Phân tích dữ liệu lớn hành vi người tiêu dùng trên mạng xã hội bằng Spark",
                "Nghiên cứu các cuộc tấn công DDoS vào hệ thống đám mây và giải pháp phòng chống"
            };

            var advisors = new[] { "PGS.TS. Nguyễn Văn A", "TS. Trần Thị B", "PGS.TS. Lê Hoàng C", "TS. Phạm Minh D", "TS. Vũ Hữu E" };
            var thesisStatuses = new[] { "Proposed", "Approved", "InProgress", "ReadyForDefence" };

            var topics = new List<ThesisTopic>();
            int topicIndex = 1;
            foreach (var student in students)
            {
                // 60% of students have thesis topics
                if (rand.Next(10) < 6 || student.CurrentStatus == "Graduated")
                {
                    var status = student.CurrentStatus == "Graduated" ? "ReadyForDefence" : thesisStatuses[rand.Next(thesisStatuses.Length)];
                    var topic = new ThesisTopic
                    {
                        StudentId = student.StudentId,
                        TopicCode = $"DT{DateTime.Now.Year}{topicIndex:D3}",
                        Title = thesisTitles[rand.Next(thesisTitles.Length)] + $" - Mã số {topicIndex}",
                        Status = status,
                        AdvisorName = advisors[rand.Next(advisors.Length)]
                    };
                    topics.Add(topic);
                    topicIndex++;
                }
            }

            context.ThesisTopics.AddRange(topics);
            context.SaveChanges();

            // Seed Defence Results
            var defenceResults = new List<DefenceResult>();
            foreach (var topic in topics)
            {
                // Only ReadyForDefence or some InProgress can have defence results (all Graduated must have it)
                var student = students.First(s => s.StudentId == topic.StudentId);
                if (student.CurrentStatus == "Graduated" || (topic.Status == "ReadyForDefence" && rand.Next(2) == 0))
                {
                    var score = rand.Next(70, 96) / 10.0; // 7.0 to 9.5
                    defenceResults.Add(new DefenceResult
                    {
                        TopicId = topic.TopicId,
                        FinalScore = score,
                        ResultStatus = score >= 7.0 ? "Pass" : "Fail",
                        DefenceDate = DateTime.Now.AddMonths(-rand.Next(6) - 1)
                    });
                }
            }

            context.DefenceResults.AddRange(defenceResults);
            context.SaveChanges();

            // --- SEED DEGREES ---
            var degrees = new List<Degree>();
            int degreeIndex = 1;
            foreach (var student in students)
            {
                if (student.CurrentStatus == "Graduated")
                {
                    degrees.Add(new Degree
                    {
                        StudentId = student.StudentId,
                        DegreeNumber = $"VB{DateTime.Now.Year}-{degreeIndex:D4}",
                        IssueDate = DateTime.Now.AddMonths(-rand.Next(3)),
                        Status = "Issued"
                    });
                    degreeIndex++;
                }
            }

            context.Degrees.AddRange(degrees);
            context.SaveChanges();

            // --- SEED CASES ---
            // Let's seed a few cases for Case Management demo
            var caseTypes = new[] { "Học tập", "Học phí", "Luận văn", "Khác" };
            var priorities = new[] { "Low", "Medium", "High", "Critical" };
            var caseStatuses = new[] { "Created", "Assigned", "Processing", "Closed" };
            var users = new[] { "Admin", "Cán bộ A", "Cán bộ B" };

            var cases = new List<Case>();
            for (int k = 1; k <= 8; k++)
            {
                var student = students[rand.Next(students.Count)];
                var caseStatus = caseStatuses[k % caseStatuses.Length];
                var casePriority = priorities[rand.Next(priorities.Length)];
                var assignee = caseStatus == "Created" ? null : users[rand.Next(users.Length)];

                // Create Case
                var item = new Case
                {
                    CaseCode = $"CASE-{DateTime.Now.Year}-{k:D4}",
                    CaseType = caseTypes[rand.Next(caseTypes.Length)],
                    StudentId = student.StudentId,
                    Title = $"Yêu cầu hỗ trợ về {caseTypes[rand.Next(caseTypes.Length)].ToLower()} cho HV {student.FullName}",
                    Priority = casePriority,
                    Status = caseStatus,
                    Assignee = assignee,
                    DueDate = DateTime.Now.AddDays(rand.Next(10) - 3), // some overdue
                    CreatedAt = DateTime.Now.AddDays(-rand.Next(15) - 5)
                };
                cases.Add(item);
            }

            context.Cases.AddRange(cases);
            context.SaveChanges();

            // Seed Case History & Notes
            foreach (var c in cases)
            {
                // Notes
                int numNotes = rand.Next(3);
                for (int n = 0; n < numNotes; n++)
                {
                    context.CaseNotes.Add(new CaseNote
                    {
                        CaseId = c.CaseId,
                        Content = $"Ghi chú xử lý lần {n + 1} cho yêu cầu này. Nội dung ghi chú mô tả công việc đã xử lý.",
                        CreatedAt = c.CreatedAt.AddHours(n + 1),
                        CreatedBy = c.Assignee ?? "Hệ thống"
                    });
                }

                // If status is Closed, must have a concluding note
                if (c.Status == "Closed")
                {
                    context.CaseNotes.Add(new CaseNote
                    {
                        CaseId = c.CaseId,
                        Content = "Kết luận xử lý: Đã hoàn thành yêu cầu hỗ trợ và liên hệ học viên.",
                        CreatedAt = c.CreatedAt.AddDays(2),
                        CreatedBy = c.Assignee ?? "Admin"
                    });
                }

                // History
                context.CaseStatusHistories.Add(new CaseStatusHistory
                {
                    CaseId = c.CaseId,
                    OldStatus = "",
                    NewStatus = "Created",
                    ChangedAt = c.CreatedAt,
                    ChangedBy = "Hệ thống"
                });

                if (c.Status != "Created" && c.Status != "")
                {
                    context.CaseStatusHistories.Add(new CaseStatusHistory
                    {
                        CaseId = c.CaseId,
                        OldStatus = "Created",
                        NewStatus = "Assigned",
                        ChangedAt = c.CreatedAt.AddHours(1),
                        ChangedBy = "Admin"
                    });
                }

                if (c.Status == "Processing" || c.Status == "Closed")
                {
                    context.CaseStatusHistories.Add(new CaseStatusHistory
                    {
                        CaseId = c.CaseId,
                        OldStatus = "Assigned",
                        NewStatus = "Processing",
                        ChangedAt = c.CreatedAt.AddHours(4),
                        ChangedBy = c.Assignee ?? "Cán bộ"
                    });
                }

                if (c.Status == "Closed")
                {
                    context.CaseStatusHistories.Add(new CaseStatusHistory
                    {
                        CaseId = c.CaseId,
                        OldStatus = "Processing",
                        NewStatus = "Closed",
                        ChangedAt = c.CreatedAt.AddDays(2),
                        ChangedBy = c.Assignee ?? "Cán bộ"
                    });
                }
            }

            context.SaveChanges();
        }

        private static void CreateTablesIfNotExist(QldtSdhDbContext context)
        {
            // Create Roles table if not exists
            context.Database.ExecuteSqlRaw(@"
                IF OBJECT_ID('Roles', 'U') IS NULL
                BEGIN
                    CREATE TABLE Roles (
                        RoleId INT IDENTITY(1,1) PRIMARY KEY,
                        RoleCode NVARCHAR(50) NOT NULL,
                        RoleName NVARCHAR(100) NOT NULL
                    );
                    CREATE UNIQUE INDEX IX_Roles_RoleCode ON Roles(RoleCode);
                END
            ");

            // Create Users table if not exists
            context.Database.ExecuteSqlRaw(@"
                IF OBJECT_ID('Users', 'U') IS NULL
                BEGIN
                    CREATE TABLE Users (
                        UserId INT IDENTITY(1,1) PRIMARY KEY,
                        Username NVARCHAR(50) NOT NULL,
                        PasswordHash NVARCHAR(256) NOT NULL,
                        FullName NVARCHAR(100) NOT NULL,
                        Email NVARCHAR(100) NULL,
                        RoleId INT NOT NULL,
                        IsActive BIT NOT NULL DEFAULT 1,
                        CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
                        CONSTRAINT FK_Users_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES Roles(RoleId) ON DELETE NO ACTION
                    );
                    CREATE UNIQUE INDEX IX_Users_Username ON Users(Username);
                END
            ");
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                var builder = new System.Text.StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
