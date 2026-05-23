using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using QldtSdh.Shared;
using QldtSdh.Wpf.Services;

namespace QldtSdh.Wpf.ViewModels
{
    public partial class CaseBoardViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private ObservableCollection<CaseDto> _cases = new();

        [ObservableProperty]
        private CaseDto? _selectedCase;

        [ObservableProperty]
        private CaseDetailResponse? _selectedCaseDetail;

        // Creating Case variables
        [ObservableProperty]
        private ObservableCollection<StudentDto> _studentsList = new();

        [ObservableProperty]
        private StudentDto? _selectedStudent;

        [ObservableProperty]
        private string _newCaseType = "Học tập";

        [ObservableProperty]
        private string _newCaseTitle = string.Empty;

        [ObservableProperty]
        private string _newCasePriority = "Medium";

        [ObservableProperty]
        private string? _newCaseAssignee = "Cán bộ A";

        [ObservableProperty]
        private DateTime? _newCaseDueDate = DateTime.Now.AddDays(7);

        // Update Case variables
        [ObservableProperty]
        private string _newNoteContent = string.Empty;

        [ObservableProperty]
        private string _currentUser = "Cán bộ A"; // Simulated current logged in user

        // Dialog Visibility states
        [ObservableProperty]
        private bool _isCreateCaseOpen;

        [ObservableProperty]
        private bool _isCaseDetailOpen;

        public CaseBoardViewModel(ApiService apiService, IServiceProvider serviceProvider)
        {
            _apiService = apiService;
            _serviceProvider = serviceProvider;
            
            _ = LoadCasesAsync();
        }

        public async Task LoadCasesAsync()
        {
            try
            {
                var list = await _apiService.GetAsync<List<CaseDto>>("case");
                Cases.Clear();
                if (list != null)
                {
                    foreach (var c in list)
                    {
                        Cases.Add(c);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách Case: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task LoadCaseDetailsAsync(CaseDto? caseDto)
        {
            if (caseDto == null) return;
            SelectedCase = caseDto;

            try
            {
                var details = await _apiService.GetAsync<CaseDetailResponse>($"case/{caseDto.CaseId}");
                SelectedCaseDetail = details;
                IsCaseDetailOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết Case: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task OpenCreateCaseDialogAsync(StudentDto? student = null)
        {
            SelectedStudent = student;
            NewCaseTitle = student != null ? $"Yêu cầu hỗ trợ về học tập cho HV {student.FullName}" : string.Empty;
            
            // Load students list for dropdown if not provided
            try
            {
                var list = await _apiService.GetAsync<List<StudentDto>>("student");
                StudentsList.Clear();
                if (list != null)
                {
                    foreach (var s in list)
                    {
                        StudentsList.Add(s);
                    }
                }
            }
            catch { }

            IsCreateCaseOpen = true;
        }

        // Direct navigation helper from student profile
        public void OpenCreateCaseDialog(StudentDto student)
        {
            _ = OpenCreateCaseDialogAsync(student);
        }

        [RelayCommand]
        public async Task CreateCaseAsync()
        {
            if (SelectedStudent == null)
            {
                MessageBox.Show("Vui lòng chọn học viên.");
                return;
            }

            if (string.IsNullOrWhiteSpace(NewCaseTitle))
            {
                MessageBox.Show("Vui lòng nhập tiêu đề yêu cầu.");
                return;
            }

            var request = new CreateCaseRequest
            {
                StudentId = SelectedStudent.StudentId,
                CaseType = NewCaseType,
                Title = NewCaseTitle,
                Priority = NewCasePriority,
                Assignee = NewCaseAssignee,
                DueDate = NewCaseDueDate
            };

            try
            {
                var created = await _apiService.PostAsync<CreateCaseRequest, CaseDto>("case", request);
                if (created != null)
                {
                    IsCreateCaseOpen = false;
                    MessageBox.Show($"Đã tạo thành công Case hỗ trợ: {created.CaseCode}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadCasesAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo Case: {ex.Message}", "Lỗi API", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public async Task AddNoteAsync()
        {
            if (SelectedCase == null || string.IsNullOrWhiteSpace(NewNoteContent)) return;

            var request = new CreateCaseNoteRequest
            {
                Content = NewNoteContent,
                User = CurrentUser
            };

            try
            {
                var note = await _apiService.PostAsync<CreateCaseNoteRequest, CaseNoteDto>($"case/{SelectedCase.CaseId}/notes", request);
                if (note != null)
                {
                    NewNoteContent = string.Empty;
                    // Reload details
                    await LoadCaseDetailsAsync(SelectedCase);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi thêm ghi chú: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task TransitionStatusAsync(string newStatus)
        {
            if (SelectedCase == null) return;

            var request = new UpdateCaseStatusRequest
            {
                NewStatus = newStatus,
                User = CurrentUser
            };

            try
            {
                var result = await _apiService.PutAsync($"case/{SelectedCase.CaseId}/status", request);
                if (result)
                {
                    MessageBox.Show($"Đã chuyển trạng thái sang {newStatus} thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    // Reload details & list
                    await LoadCaseDetailsAsync(SelectedCase);
                    await LoadCasesAsync();
                }
            }
            catch (Exception ex)
            {
                // Display the specific API validation error (e.g. blocking closing without concluding note)
                MessageBox.Show(ex.Message, "Ràng buộc nghiệp vụ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        public async Task AssignHandlerAsync(string assigneeName)
        {
            if (SelectedCase == null) return;

            try
            {
                var result = await _apiService.PutAsync($"case/{SelectedCase.CaseId}/assign", assigneeName);
                if (result)
                {
                    MessageBox.Show($"Đã gán cán bộ xử lý thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadCaseDetailsAsync(SelectedCase);
                    await LoadCasesAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi gán cán bộ: {ex.Message}");
            }
        }

        [RelayCommand]
        public void CloseDialogs()
        {
            IsCreateCaseOpen = false;
            IsCaseDetailOpen = false;
        }
    }
}
