using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using QldtSdh.Shared;
using QldtSdh.Wpf.Services;

namespace QldtSdh.Wpf.ViewModels
{
    public partial class Student360ViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly IServiceProvider _serviceProvider;
        private readonly SessionService _sessionService;

        [ObservableProperty]
        private StudentDto? _student;

        [ObservableProperty]
        private double _gpa;

        [ObservableProperty]
        private int _totalCredits;

        [ObservableProperty]
        private decimal _totalDebt;

        [ObservableProperty]
        private ObservableCollection<EnrollmentDto> _enrollmentsWithGrades = new();

        [ObservableProperty]
        private ObservableCollection<InvoiceDto> _invoicesWithPayments = new();

        [ObservableProperty]
        private ObservableCollection<ThesisTopicDto> _thesisTopics = new();

        [ObservableProperty]
        private ObservableCollection<DegreeDto> _degrees = new();

        [ObservableProperty]
        private ObservableCollection<CaseDto> _studentCases = new();

        // Dialog Open States
        [ObservableProperty]
        private bool _isAddEnrollmentOpen;

        [ObservableProperty]
        private bool _isAddInvoiceOpen;

        [ObservableProperty]
        private bool _isAddThesisOpen;

        [ObservableProperty]
        private bool _isAddDegreeOpen;

        [ObservableProperty]
        private bool _isEditStudentOpen;

        // Form Fields: Enrollment
        [ObservableProperty]
        private string _enrollCourseCode = string.Empty;

        [ObservableProperty]
        private string _enrollCourseName = string.Empty;

        [ObservableProperty]
        private int _enrollCredits = 3;

        [ObservableProperty]
        private string _enrollSelectedStatus = "Completed";

        [ObservableProperty]
        private double _enrollPracticeScore = 10.0;

        [ObservableProperty]
        private double _enrollMidtermScore = 8.0;

        [ObservableProperty]
        private double _enrollFinalScore = 8.0;

        public ObservableCollection<string> EnrollStatuses { get; } = new() { "Enrolled", "Completed", "Failed" };

        // Form Fields: Invoice
        [ObservableProperty]
        private string _invoiceSemester = "HK1_2025_2026";

        [ObservableProperty]
        private string _invoiceNo = string.Empty;

        [ObservableProperty]
        private decimal _invoiceTotalAmount = 15000000;

        [ObservableProperty]
        private string _invoiceSelectedStatus = "Issued";

        [ObservableProperty]
        private DateTime _invoiceDueDate = DateTime.Today.AddDays(30);

        [ObservableProperty]
        private decimal _invoiceAmountPaid;

        public ObservableCollection<string> InvoiceStatuses { get; } = new() { "Draft", "Issued", "PartiallyPaid", "Paid" };

        // Form Fields: Thesis Topic
        [ObservableProperty]
        private string _thesisTopicCode = string.Empty;

        [ObservableProperty]
        private string _thesisTitle = string.Empty;

        [ObservableProperty]
        private string _thesisAdvisorName = string.Empty;

        [ObservableProperty]
        private string _thesisSelectedStatus = "Approved";

        [ObservableProperty]
        private double? _thesisFinalScore;

        [ObservableProperty]
        private DateTime? _thesisDefenceDate;

        public ObservableCollection<string> ThesisStatuses { get; } = new() { "Proposed", "Approved", "InProgress", "ReadyForDefence" };

        // Form Fields: Degree
        [ObservableProperty]
        private string _degreeNumber = string.Empty;

        [ObservableProperty]
        private DateTime _degreeIssueDate = DateTime.Today;

        [ObservableProperty]
        private string _degreeSelectedStatus = "Approved";

        public ObservableCollection<string> DegreeStatuses { get; } = new() { "Approved", "Issued" };

        // Form Fields: Edit Student
        [ObservableProperty]
        private string _editFullName = string.Empty;

        [ObservableProperty]
        private DateTime _editDOB = DateTime.Today;

        [ObservableProperty]
        private string _editSelectedProgramme = string.Empty;

        [ObservableProperty]
        private string _editSelectedStatus = string.Empty;

        public ObservableCollection<string> EditProgrammes { get; } = new() 
        { 
            "Khoa học máy tính", 
            "Hệ thống thông tin", 
            "Kỹ thuật phần mềm", 
            "An toàn thông tin" 
        };

        public ObservableCollection<string> EditStatuses { get; } = new() 
        { 
            "Studying", 
            "Suspended", 
            "Graduated" 
        };

        [ObservableProperty]
        private bool _isAdmin;

        public Student360ViewModel(ApiService apiService, IServiceProvider serviceProvider, SessionService sessionService)
        {
            _apiService = apiService;
            _serviceProvider = serviceProvider;
            _sessionService = sessionService;
            IsAdmin = _sessionService.IsAdmin;
        }

        public async void LoadStudent(int studentId)
        {
            await LoadStudentAsync(studentId);
        }

        public async Task LoadStudentAsync(int studentId)
        {
            IsAdmin = _sessionService.IsAdmin;
            try
            {
                var profile = await _apiService.GetAsync<StudentProfile360Dto>($"student/{studentId}/profile360");
                if (profile != null)
                {
                    Student = profile.Student;
                    Gpa = profile.GPA;
                    TotalCredits = profile.TotalCredits;
                    TotalDebt = profile.TotalDebt;

                    EnrollmentsWithGrades.Clear();
                    foreach (var enroll in profile.Enrollments)
                    {
                        EnrollmentsWithGrades.Add(enroll);
                    }

                    InvoicesWithPayments.Clear();
                    foreach (var inv in profile.Invoices)
                    {
                        InvoicesWithPayments.Add(inv);
                    }

                    ThesisTopics.Clear();
                    foreach (var topic in profile.ThesisTopics)
                    {
                        ThesisTopics.Add(topic);
                    }

                    Degrees.Clear();
                    foreach (var deg in profile.Degrees)
                    {
                        Degrees.Add(deg);
                    }

                    StudentCases.Clear();
                    foreach (var c in profile.Cases)
                    {
                        StudentCases.Add(c);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi tải hồ sơ học viên 360°: {ex.Message}", "Lỗi tải dữ liệu", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public void BackToSearch()
        {
            var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
            mainVm.NavigateToSearch();
        }

        [RelayCommand]
        public void CreateNewCase()
        {
            if (Student == null) return;
            var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
            mainVm.NavigateToCasesAndCreate(Student);
        }

        [RelayCommand]
        public void OpenAddEnrollment()
        {
            if (Student == null) return;
            EnrollCourseCode = string.Empty;
            EnrollCourseName = string.Empty;
            EnrollCredits = 3;
            EnrollSelectedStatus = "Completed";
            EnrollPracticeScore = 10.0;
            EnrollMidtermScore = 8.0;
            EnrollFinalScore = 8.0;
            IsAddEnrollmentOpen = true;
        }

        [RelayCommand]
        public async Task SaveEnrollmentAsync()
        {
            if (Student == null) return;
            if (string.IsNullOrWhiteSpace(EnrollCourseCode))
            {
                System.Windows.MessageBox.Show("Mã môn học không được để trống.", "Lỗi nhập liệu", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(EnrollCourseName))
            {
                System.Windows.MessageBox.Show("Tên môn học không được để trống.", "Lỗi nhập liệu", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                var req = new AddEnrollmentRequest
                {
                    CourseCode = EnrollCourseCode.Trim(),
                    CourseName = EnrollCourseName.Trim(),
                    Credits = EnrollCredits,
                    EnrollStatus = EnrollSelectedStatus,
                    PracticeScore = EnrollPracticeScore,
                    MidtermScore = EnrollMidtermScore,
                    FinalScore = EnrollFinalScore
                };

                await _apiService.PostAsync<AddEnrollmentRequest, object>($"student/{Student.StudentId}/enrollment", req);
                System.Windows.MessageBox.Show("Thêm kết quả học tập thành công!", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                IsAddEnrollmentOpen = false;
                await LoadStudentAsync(Student.StudentId);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi thêm kết quả học tập: {ex.Message}", "Lỗi hệ thống", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public void OpenAddInvoice()
        {
            if (Student == null) return;
            InvoiceSemester = "HK1_2025_2026";
            InvoiceNo = "INV-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(100, 999);
            InvoiceTotalAmount = 15000000;
            InvoiceSelectedStatus = "Issued";
            InvoiceDueDate = DateTime.Today.AddDays(30);
            InvoiceAmountPaid = 0;
            IsAddInvoiceOpen = true;
        }

        [RelayCommand]
        public async Task SaveInvoiceAsync()
        {
            if (Student == null) return;
            if (string.IsNullOrWhiteSpace(InvoiceSemester))
            {
                System.Windows.MessageBox.Show("Học kỳ không được để trống.", "Lỗi nhập liệu", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(InvoiceNo))
            {
                System.Windows.MessageBox.Show("Số hóa đơn không được để trống.", "Lỗi nhập liệu", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                var req = new AddInvoiceRequest
                {
                    Semester = InvoiceSemester.Trim(),
                    InvoiceNo = InvoiceNo.Trim(),
                    TotalAmount = InvoiceTotalAmount,
                    Status = InvoiceSelectedStatus,
                    DueDate = InvoiceDueDate,
                    AmountPaid = InvoiceAmountPaid
                };

                await _apiService.PostAsync<AddInvoiceRequest, object>($"student/{Student.StudentId}/invoice", req);
                System.Windows.MessageBox.Show("Tạo hóa đơn thành công!", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                IsAddInvoiceOpen = false;
                await LoadStudentAsync(Student.StudentId);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi tạo hóa đơn: {ex.Message}", "Lỗi hệ thống", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public void OpenAddThesis()
        {
            if (Student == null) return;
            ThesisTopicCode = "DT-" + DateTime.Now.ToString("yyyy") + "-" + new Random().Next(100, 999);
            ThesisTitle = string.Empty;
            ThesisAdvisorName = string.Empty;
            ThesisSelectedStatus = "Approved";
            ThesisFinalScore = null;
            ThesisDefenceDate = null;
            IsAddThesisOpen = true;
        }

        [RelayCommand]
        public async Task SaveThesisAsync()
        {
            if (Student == null) return;
            if (string.IsNullOrWhiteSpace(ThesisTopicCode))
            {
                System.Windows.MessageBox.Show("Mã đề tài không được để trống.", "Lỗi nhập liệu", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(ThesisTitle))
            {
                System.Windows.MessageBox.Show("Tên đề tài không được để trống.", "Lỗi nhập liệu", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                var req = new AddThesisTopicRequest
                {
                    TopicCode = ThesisTopicCode.Trim(),
                    Title = ThesisTitle.Trim(),
                    AdvisorName = ThesisAdvisorName.Trim(),
                    Status = ThesisSelectedStatus,
                    FinalScore = ThesisFinalScore,
                    DefenceDate = ThesisFinalScore.HasValue ? (ThesisDefenceDate ?? DateTime.Today) : null
                };

                await _apiService.PostAsync<AddThesisTopicRequest, object>($"student/{Student.StudentId}/thesis", req);
                System.Windows.MessageBox.Show("Thêm đề tài nghiên cứu thành công!", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                IsAddThesisOpen = false;
                await LoadStudentAsync(Student.StudentId);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi thêm đề tài: {ex.Message}", "Lỗi hệ thống", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public void OpenAddDegree()
        {
            if (Student == null) return;
            DegreeNumber = "VB" + DateTime.Now.ToString("yyyy") + "-" + new Random().Next(1000, 9999);
            DegreeIssueDate = DateTime.Today;
            DegreeSelectedStatus = "Approved";
            IsAddDegreeOpen = true;
        }

        [RelayCommand]
        public async Task SaveDegreeAsync()
        {
            if (Student == null) return;
            if (string.IsNullOrWhiteSpace(DegreeNumber))
            {
                System.Windows.MessageBox.Show("Số hiệu văn bằng không được để trống.", "Lỗi nhập liệu", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                var req = new AddDegreeRequest
                {
                    DegreeNumber = DegreeNumber.Trim(),
                    IssueDate = DegreeIssueDate,
                    Status = DegreeSelectedStatus
                };

                await _apiService.PostAsync<AddDegreeRequest, object>($"student/{Student.StudentId}/degree", req);
                System.Windows.MessageBox.Show("Cấp phát văn bằng thành công!", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                IsAddDegreeOpen = false;
                await LoadStudentAsync(Student.StudentId);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi cấp phát văn bằng: {ex.Message}", "Lỗi hệ thống", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public void CloseDialogs()
        {
            IsAddEnrollmentOpen = false;
            IsAddInvoiceOpen = false;
            IsAddThesisOpen = false;
            IsAddDegreeOpen = false;
            IsEditStudentOpen = false;
        }

        [RelayCommand]
        public void OpenEditStudent()
        {
            if (Student == null) return;
            EditFullName = Student.FullName;
            EditDOB = Student.DOB;
            EditSelectedProgramme = Student.ProgrammeName;
            EditSelectedStatus = Student.CurrentStatus;
            IsEditStudentOpen = true;
        }

        [RelayCommand]
        public async Task SaveEditStudentAsync()
        {
            if (Student == null) return;
            if (string.IsNullOrWhiteSpace(EditFullName))
            {
                System.Windows.MessageBox.Show("Họ và tên không được để trống.", "Lỗi nhập liệu", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                var req = new UpdateStudentRequest
                {
                    FullName = EditFullName.Trim(),
                    DOB = EditDOB,
                    ProgrammeName = EditSelectedProgramme,
                    CurrentStatus = EditSelectedStatus
                };

                await _apiService.PutAsync($"student/{Student.StudentId}", req);
                System.Windows.MessageBox.Show("Cập nhật thông tin học viên thành công!", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                IsEditStudentOpen = false;
                await LoadStudentAsync(Student.StudentId);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi cập nhật học viên: {ex.Message}", "Lỗi hệ thống", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
