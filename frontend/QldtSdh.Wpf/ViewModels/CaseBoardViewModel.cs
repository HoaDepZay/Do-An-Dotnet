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

        [ObservableProperty]
        private string? _selectedCaseAssignee;

        // Creating Case variables
        [ObservableProperty]
        private ObservableCollection<StudentDto> _studentsList = new();

        [ObservableProperty]
        private ObservableCollection<UserDto> _staffList = new();

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

        [ObservableProperty]
        private bool _isDayCasesListOpen;

        [ObservableProperty]
        private CalendarDayViewModel? _selectedCalendarDay;

        partial void OnSelectedCalendarDayChanged(CalendarDayViewModel? value)
        {
            OnPropertyChanged(nameof(HasSelectedDayCases));
            OnPropertyChanged(nameof(NoSelectedDayCases));
        }

        public bool HasSelectedDayCases => SelectedCalendarDay?.ActiveCases?.Count > 0;
        public bool NoSelectedDayCases => !HasSelectedDayCases;

        // View Mode & Filters
        [ObservableProperty]
        private string _currentViewMode = "List"; // List, Kanban, Calendar

        [ObservableProperty]
        private string _filterStudentName = string.Empty;

        [ObservableProperty]
        private string _filterCaseType = "Tất cả";

        [ObservableProperty]
        private string _filterPriority = "Tất cả";

        [ObservableProperty]
        private string _filterAssignee = string.Empty;

        [ObservableProperty]
        private string _filterStatus = "Tất cả";

        [ObservableProperty]
        private DateTime? _filterDueDate;

        // View Mode Collections
        [ObservableProperty]
        private ObservableCollection<CaseDto> _filteredCases = new();

        [ObservableProperty]
        private ObservableCollection<CaseDto> _kanbanCreated = new();

        [ObservableProperty]
        private ObservableCollection<CaseDto> _kanbanAssigned = new();

        [ObservableProperty]
        private ObservableCollection<CaseDto> _kanbanProcessing = new();

        [ObservableProperty]
        private ObservableCollection<CaseDto> _kanbanClosed = new();

        [ObservableProperty]
        private ObservableCollection<CalendarDayViewModel> _calendarDays = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedMonthLabel))]
        private DateTime _selectedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        public string SelectedMonthLabel => $"Tháng {SelectedMonth:MM/yyyy}";

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
                ApplyFilters();
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
            IsDayCasesListOpen = false;

            try
            {
                var details = await _apiService.GetAsync<CaseDetailResponse>($"case/{caseDto.CaseId}");
                SelectedCaseDetail = details;
                IsCaseDetailOpen = true;

                // Load active staff list
                await LoadStaffListAsync();
                SelectedCaseAssignee = details?.Case?.Assignee ?? caseDto.Assignee;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết Case: {ex.Message}");
            }
        }

        public async Task LoadStaffListAsync()
        {
            try
            {
                var list = await _apiService.GetAsync<List<UserDto>>("user");
                StaffList.Clear();
                if (list != null)
                {
                    foreach (var u in list)
                    {
                        if (u.IsActive)
                        {
                            StaffList.Add(u);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách cán bộ: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task OpenCreateCaseDialogAsync(StudentDto? student = null)
        {
            SelectedStudent = student;
            NewCaseTitle = student != null ? $"Yêu cầu hỗ trợ về học tập cho HV {student.FullName}" : string.Empty;
            NewCaseAssignee = "System";
            
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

                await LoadStaffListAsync();
                if (StaffList.Count > 0)
                {
                    NewCaseAssignee = StaffList[0].FullName;
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
            IsDayCasesListOpen = false;
        }

        [RelayCommand]
        public void ShowDayCases(CalendarDayViewModel day)
        {
            SelectedCalendarDay = day;
            IsDayCasesListOpen = true;
        }

        // Filters and Calendar Navigation Logic
        partial void OnFilterStudentNameChanged(string value) => ApplyFilters();
        partial void OnFilterCaseTypeChanged(string value) => ApplyFilters();
        partial void OnFilterPriorityChanged(string value) => ApplyFilters();
        partial void OnFilterAssigneeChanged(string value) => ApplyFilters();
        partial void OnFilterStatusChanged(string value) => ApplyFilters();
        partial void OnFilterDueDateChanged(DateTime? value) => ApplyFilters();
        partial void OnSelectedMonthChanged(DateTime value) => GenerateCalendar();

        public void ApplyFilters()
        {
            if (Cases == null) return;

            var result = new List<CaseDto>();
            foreach (var c in Cases)
            {
                if (!string.IsNullOrWhiteSpace(FilterStudentName) && 
                    (c.StudentName == null || !c.StudentName.Contains(FilterStudentName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (!string.IsNullOrWhiteSpace(FilterCaseType) && FilterCaseType != "Tất cả" && 
                    !c.CaseType.Equals(FilterCaseType, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrWhiteSpace(FilterPriority) && FilterPriority != "Tất cả" && 
                    !c.Priority.Equals(FilterPriority, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrWhiteSpace(FilterAssignee) && 
                    (c.Assignee == null || !c.Assignee.Contains(FilterAssignee, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (!string.IsNullOrWhiteSpace(FilterStatus) && FilterStatus != "Tất cả" && 
                    !c.Status.Equals(FilterStatus, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (FilterDueDate.HasValue && 
                    (!c.DueDate.HasValue || c.DueDate.Value.Date != FilterDueDate.Value.Date))
                    continue;

                result.Add(c);
            }

            FilteredCases.Clear();
            foreach (var c in result)
            {
                FilteredCases.Add(c);
            }

            UpdateKanbanGroups();
            GenerateCalendar();
        }

        [RelayCommand]
        public void ClearFilters()
        {
            FilterStudentName = string.Empty;
            FilterCaseType = "Tất cả";
            FilterPriority = "Tất cả";
            FilterAssignee = string.Empty;
            FilterStatus = "Tất cả";
            FilterDueDate = null;

            ApplyFilters();
        }

        private void UpdateKanbanGroups()
        {
            KanbanCreated.Clear();
            KanbanAssigned.Clear();
            KanbanProcessing.Clear();
            KanbanClosed.Clear();

            foreach (var c in FilteredCases)
            {
                if (c.Status.Equals("Created", StringComparison.OrdinalIgnoreCase))
                    KanbanCreated.Add(c);
                else if (c.Status.Equals("Assigned", StringComparison.OrdinalIgnoreCase))
                    KanbanAssigned.Add(c);
                else if (c.Status.Equals("Processing", StringComparison.OrdinalIgnoreCase))
                    KanbanProcessing.Add(c);
                else if (c.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase))
                    KanbanClosed.Add(c);
                else
                    KanbanCreated.Add(c);
            }
        }

        private void GenerateCalendar()
        {
            CalendarDays.Clear();

            var firstDay = new DateTime(SelectedMonth.Year, SelectedMonth.Month, 1);
            int dayOfWeek = (int)firstDay.DayOfWeek;
            int offset = dayOfWeek == 0 ? 6 : dayOfWeek - 1;

            var startDate = firstDay.AddDays(-offset);

            for (int i = 0; i < 42; i++)
            {
                var currentDate = startDate.AddDays(i);
                var dayVm = new CalendarDayViewModel
                {
                    DayNumber = currentDate.Day,
                    Date = currentDate,
                    IsCurrentMonth = currentDate.Month == SelectedMonth.Month,
                    IsToday = currentDate.Date == DateTime.Today
                };

                foreach (var c in FilteredCases)
                {
                    var start = c.CreatedAt.Date;
                    var end = c.DueDate.HasValue ? c.DueDate.Value.Date : start;
                    if (currentDate >= start && currentDate <= end)
                    {
                        dayVm.ActiveCases.Add(c);
                    }
                }

                CalendarDays.Add(dayVm);
            }
        }

        [RelayCommand]
        public void PreviousMonth()
        {
            SelectedMonth = SelectedMonth.AddMonths(-1);
        }

        [RelayCommand]
        public void NextMonth()
        {
            SelectedMonth = SelectedMonth.AddMonths(1);
        }
    }
}
