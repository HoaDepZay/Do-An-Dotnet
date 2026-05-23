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

        public Student360ViewModel(ApiService apiService, IServiceProvider serviceProvider)
        {
            _apiService = apiService;
            _serviceProvider = serviceProvider;
        }

        public async void LoadStudent(int studentId)
        {
            await LoadStudentAsync(studentId);
        }

        public async Task LoadStudentAsync(int studentId)
        {
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
    }
}
