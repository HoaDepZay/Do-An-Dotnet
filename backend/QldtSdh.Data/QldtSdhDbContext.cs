using Microsoft.EntityFrameworkCore;
using QldtSdh.Data.Models;

namespace QldtSdh.Data
{
    public class QldtSdhDbContext : DbContext
    {
        public QldtSdhDbContext()
        {
        }

        public QldtSdhDbContext(DbContextOptions<QldtSdhDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Student> Students { get; set; } = null!;
        public virtual DbSet<Enrollment> Enrollments { get; set; } = null!;
        public virtual DbSet<Grade> Grades { get; set; } = null!;
        public virtual DbSet<Invoice> Invoices { get; set; } = null!;
        public virtual DbSet<Payment> Payments { get; set; } = null!;
        public virtual DbSet<ThesisTopic> ThesisTopics { get; set; } = null!;
        public virtual DbSet<DefenceResult> DefenceResults { get; set; } = null!;
        public virtual DbSet<Degree> Degrees { get; set; } = null!;
        public virtual DbSet<Case> Cases { get; set; } = null!;
        public virtual DbSet<CaseStatusHistory> CaseStatusHistories { get; set; } = null!;
        public virtual DbSet<CaseNote> CaseNotes { get; set; } = null!;
        public virtual DbSet<DashboardSnapshot> DashboardSnapshots { get; set; } = null!;
        public virtual DbSet<SearchAudit> SearchAudits { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Fallback connection string for local development / migrations
                // Azure SQL Server will be configured at runtime or through this string
                optionsBuilder.UseSqlServer("Server=tcp:quan-tri-nha-su.database.windows.net,1433;Initial Catalog=HE-THONG-QUAN-TRI-306;User ID=CloudSAe02b7603;Password=31052006Hoa*;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Student relationships
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.StudentId);
                entity.Property(e => e.StudentCode).IsRequired().HasMaxLength(20);
                entity.HasIndex(e => e.StudentCode).IsUnique();
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ProgrammeName).HasMaxLength(100);
                entity.Property(e => e.CurrentStatus).HasMaxLength(30);
            });

            // Enrollment
            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.HasKey(e => e.EnrollmentId);
                entity.Property(e => e.CourseCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CourseName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EnrollStatus).HasMaxLength(30);

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.Enrollments)
                    .HasForeignKey(d => d.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Grade
            modelBuilder.Entity<Grade>(entity =>
            {
                entity.HasKey(e => e.GradeId);
                entity.Property(e => e.ComponentName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.GradeStatus).HasMaxLength(30);

                entity.HasOne(d => d.Enrollment)
                    .WithMany(p => p.Grades)
                    .HasForeignKey(d => d.EnrollmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Invoice
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.InvoiceId);
                entity.Property(e => e.Semester).IsRequired().HasMaxLength(30);
                entity.Property(e => e.InvoiceNo).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.InvoiceNo).IsUnique();
                entity.Property(e => e.Status).HasMaxLength(30);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.Invoices)
                    .HasForeignKey(d => d.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Payment
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.PaymentId);
                entity.Property(e => e.PaymentNo).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Method).HasMaxLength(30);

                entity.HasOne(d => d.Invoice)
                    .WithMany(p => p.Payments)
                    .HasForeignKey(d => d.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ThesisTopic
            modelBuilder.Entity<ThesisTopic>(entity =>
            {
                entity.HasKey(e => e.TopicId);
                entity.Property(e => e.TopicCode).IsRequired().HasMaxLength(20);
                entity.HasIndex(e => e.TopicCode).IsUnique();
                entity.Property(e => e.Title).IsRequired().HasMaxLength(250);
                entity.Property(e => e.Status).HasMaxLength(30);
                entity.Property(e => e.AdvisorName).HasMaxLength(100);

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.ThesisTopics)
                    .HasForeignKey(d => d.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // DefenceResult
            modelBuilder.Entity<DefenceResult>(entity =>
            {
                entity.HasKey(e => e.ResultId);
                entity.Property(e => e.ResultStatus).HasMaxLength(30);

                entity.HasOne(d => d.ThesisTopic)
                    .WithMany(p => p.DefenceResults)
                    .HasForeignKey(d => d.TopicId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Degree
            modelBuilder.Entity<Degree>(entity =>
            {
                entity.HasKey(e => e.DegreeId);
                entity.Property(e => e.DegreeNumber).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.DegreeNumber).IsUnique();
                entity.Property(e => e.Status).HasMaxLength(30);

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.Degrees)
                    .HasForeignKey(d => d.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Case
            modelBuilder.Entity<Case>(entity =>
            {
                entity.HasKey(e => e.CaseId);
                entity.Property(e => e.CaseCode).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.CaseCode).IsUnique();
                entity.Property(e => e.CaseType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Priority).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Assignee).HasMaxLength(100);

                entity.HasOne(d => d.Student)
                    .WithMany(p => p.Cases)
                    .HasForeignKey(d => d.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // CaseStatusHistory
            modelBuilder.Entity<CaseStatusHistory>(entity =>
            {
                entity.HasKey(e => e.HistoryId);
                entity.Property(e => e.OldStatus).HasMaxLength(30);
                entity.Property(e => e.NewStatus).HasMaxLength(30);
                entity.Property(e => e.ChangedBy).HasMaxLength(100);

                entity.HasOne(d => d.Case)
                    .WithMany(p => p.StatusHistories)
                    .HasForeignKey(d => d.CaseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // CaseNote
            modelBuilder.Entity<CaseNote>(entity =>
            {
                entity.HasKey(e => e.NoteId);
                entity.Property(e => e.Content).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.CreatedBy).HasMaxLength(100);

                entity.HasOne(d => d.Case)
                    .WithMany(p => p.Notes)
                    .HasForeignKey(d => d.CaseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // DashboardSnapshot
            modelBuilder.Entity<DashboardSnapshot>(entity =>
            {
                entity.HasKey(e => e.SnapshotId);
                entity.Property(e => e.Semester).IsRequired().HasMaxLength(30);
                entity.Property(e => e.ProgrammeName).HasMaxLength(100);
                entity.Property(e => e.Status).HasMaxLength(30);
                entity.Property(e => e.DataJson).IsRequired();
            });

            // SearchAudit
            modelBuilder.Entity<SearchAudit>(entity =>
            {
                entity.HasKey(e => e.AuditId);
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Keyword).IsRequired().HasMaxLength(200);

                entity.HasOne(d => d.Student)
                    .WithMany()
                    .HasForeignKey(d => d.StudentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
