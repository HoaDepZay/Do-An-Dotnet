using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using QldtSdh.Shared;
using QldtSdh.Wpf.Services;

namespace QldtSdh.Wpf.ViewModels
{
    public partial class GlobalSearchViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedProgramme = "Tất cả";

        [ObservableProperty]
        private string _selectedStatus = "Tất cả";

        [ObservableProperty]
        private ObservableCollection<StudentDto> _students = new();

        [ObservableProperty]
        private ObservableCollection<string> _programmes = new() { "Tất cả" };

        [ObservableProperty]
        private ObservableCollection<string> _statuses = new() { "Tất cả", "Studying", "Suspended", "Graduated" };

        // Dialog Overlays states
        [ObservableProperty]
        private bool _isCreateStudentOpen;

        [ObservableProperty]
        private bool _isImportCsvOpen;

        // Manual Create Student Form fields
        [ObservableProperty]
        private string _newStudentCode = string.Empty;

        [ObservableProperty]
        private string _newFullName = string.Empty;

        [ObservableProperty]
        private DateTime _newDOB = DateTime.Today.AddYears(-25);

        [ObservableProperty]
        private string _newSelectedProgramme = "Khoa học máy tính";

        [ObservableProperty]
        private string _newSelectedStatus = "Studying";

        [ObservableProperty]
        private ObservableCollection<string> _newProgrammes = new() 
        { 
            "Khoa học máy tính", 
            "Hệ thống thông tin", 
            "Kỹ thuật phần mềm", 
            "An toàn thông tin" 
        };

        [ObservableProperty]
        private ObservableCollection<string> _newStatuses = new() 
        { 
            "Studying", 
            "Suspended", 
            "Graduated" 
        };

        // CSV Import fields
        [ObservableProperty]
        private string _csvFilePath = string.Empty;

        [ObservableProperty]
        private string _validationSummary = string.Empty;

        [ObservableProperty]
        private ObservableCollection<CreateStudentRequest> _importedStudents = new();

        public GlobalSearchViewModel(ApiService apiService, IServiceProvider serviceProvider)
        {
            _apiService = apiService;
            _serviceProvider = serviceProvider;
            
            // Start async initialization
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                // Load all students once to populate the unique programmes filter
                var allStudents = await _apiService.GetAsync<List<StudentDto>>("student");
                if (allStudents != null)
                {
                    var dbProgrammes = allStudents
                        .Select(s => s.ProgrammeName)
                        .Distinct()
                        .Where(p => !string.IsNullOrEmpty(p))
                        .OrderBy(p => p)
                        .ToList();

                    foreach (var prog in dbProgrammes)
                    {
                        Programmes.Add(prog);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi load danh mục chương trình: {ex.Message}");
            }

            await SearchAsync();
        }

        [RelayCommand]
        public async Task SearchAsync()
        {
            try
            {
                var url = $"student?search={Uri.EscapeDataString(SearchText)}&programme={Uri.EscapeDataString(SelectedProgramme)}&status={Uri.EscapeDataString(SelectedStatus)}";
                var results = await _apiService.GetAsync<List<StudentDto>>(url);
                
                Students.Clear();
                if (results != null)
                {
                    foreach (var student in results)
                    {
                        Students.Add(student);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi tìm kiếm học viên: {ex.Message}", "Lỗi kết nối API", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public async Task ClearFiltersAsync()
        {
            SearchText = string.Empty;
            SelectedProgramme = "Tất cả";
            SelectedStatus = "Tất cả";
            await SearchAsync();
        }

        [RelayCommand]
        public void ViewProfile(StudentDto student)
        {
            if (student == null) return;

            var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
            mainVm.NavigateToStudent360(student.StudentId);
        }

        [RelayCommand]
        public void CreateCase(StudentDto student)
        {
            if (student == null) return;
            
            var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
            mainVm.NavigateToCasesAndCreate(student);
        }

        [RelayCommand]
        public void OpenCreateStudentDialog()
        {
            var year = DateTime.Today.Year.ToString();
            var randPart = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            NewStudentCode = $"SDH{year}{randPart}";
            
            NewFullName = string.Empty;
            NewDOB = DateTime.Today.AddYears(-25);
            NewSelectedProgramme = "Khoa học máy tính";
            NewSelectedStatus = "Studying";
            IsCreateStudentOpen = true;
        }

        [RelayCommand]
        public void OpenImportCsvDialog()
        {
            CsvFilePath = string.Empty;
            ValidationSummary = "Vui lòng chọn một tệp CSV có dòng tiêu đề.";
            ImportedStudents.Clear();
            IsImportCsvOpen = true;
        }

        [RelayCommand]
        public void CloseDialogs()
        {
            IsCreateStudentOpen = false;
            IsImportCsvOpen = false;
        }

        [RelayCommand]
        public async Task SaveStudentAsync()
        {
            if (string.IsNullOrWhiteSpace(NewStudentCode))
            {
                System.Windows.MessageBox.Show("Mã học viên không được để trống.", "Lỗi nhập liệu", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(NewFullName))
            {
                System.Windows.MessageBox.Show("Họ và tên không được để trống.", "Lỗi nhập liệu", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                var request = new CreateStudentRequest
                {
                    StudentCode = NewStudentCode.Trim(),
                    FullName = NewFullName.Trim(),
                    DOB = NewDOB,
                    ProgrammeName = NewSelectedProgramme,
                    CurrentStatus = NewSelectedStatus
                };

                var created = await _apiService.PostAsync<CreateStudentRequest, StudentDto>("student", request);
                if (created != null)
                {
                    System.Windows.MessageBox.Show($"Thêm học viên thành công!\nMã số: {created.StudentCode}\nHọ tên: {created.FullName}", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    IsCreateStudentOpen = false;
                    await SearchAsync();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi tạo học viên: {ex.Message}", "Lỗi hệ thống", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public void SelectCsvFile()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Chọn tệp CSV học viên"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                CsvFilePath = openFileDialog.FileName;
                ParseCsvFile(CsvFilePath);
            }
        }

        private void ParseCsvFile(string filePath)
        {
            try
            {
                var lines = System.IO.File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
                if (lines.Length <= 1)
                {
                    ValidationSummary = "Tệp CSV trống hoặc chỉ có tiêu đề.";
                    return;
                }

                // Parse headers
                var headerLine = lines[0];
                var headers = headerLine.Split(',').Select(h => h.Trim().Replace("\"", "")).ToArray();
                if (headers.Length == 1 && headerLine.Contains(";"))
                {
                    headers = headerLine.Split(';').Select(h => h.Trim().Replace("\"", "")).ToArray();
                }

                int codeIdx = -1;
                int nameIdx = -1;
                int dobIdx = -1;
                int progIdx = -1;
                int statusIdx = -1;

                for (int i = 0; i < headers.Length; i++)
                {
                    var h = headers[i].ToLower();
                    if (h.Contains("mã") || h.Contains("code")) codeIdx = i;
                    else if (h.Contains("tên") || h.Contains("name") || h.Contains("họ")) nameIdx = i;
                    else if (h.Contains("ngày sinh") || h.Contains("dob") || h.Contains("birth") || h.Contains("date")) dobIdx = i;
                    else if (h.Contains("chương trình") || h.Contains("programme") || h.Contains("ngành")) progIdx = i;
                    else if (h.Contains("trạng thái") || h.Contains("status")) statusIdx = i;
                }

                if (codeIdx == -1 || nameIdx == -1)
                {
                    ValidationSummary = "Tệp CSV không đúng định dạng. Cần có cột chứa mã học viên và họ tên.";
                    return;
                }

                ImportedStudents.Clear();
                int validCount = 0;
                int invalidCount = 0;

                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(',').Select(p => p.Trim().Replace("\"", "")).ToArray();
                    if (parts.Length == 1 && line.Contains(";"))
                    {
                        parts = line.Split(';').Select(p => p.Trim().Replace("\"", "")).ToArray();
                    }

                    if (parts.Length <= Math.Max(codeIdx, nameIdx))
                    {
                        invalidCount++;
                        continue;
                    }

                    var code = parts[codeIdx];
                    var name = parts[nameIdx];
                    
                    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                    {
                        invalidCount++;
                        continue;
                    }

                    // Parse DOB
                    DateTime dob = DateTime.Today.AddYears(-25);
                    if (dobIdx != -1 && dobIdx < parts.Length)
                    {
                        var dobStr = parts[dobIdx];
                        if (DateTime.TryParse(dobStr, out var parsedDob))
                        {
                            dob = parsedDob;
                        }
                        else if (DateTime.TryParseExact(dobStr, new[] { "dd/MM/yyyy", "dd-MM-yyyy", "yyyy-MM-dd" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsedDobExact))
                        {
                            dob = parsedDobExact;
                        }
                    }

                    // Parse Programme
                    string prog = "Khoa học máy tính";
                    if (progIdx != -1 && progIdx < parts.Length)
                    {
                        prog = parts[progIdx];
                    }

                    // Parse Status
                    string status = "Studying";
                    if (statusIdx != -1 && statusIdx < parts.Length)
                    {
                        var sVal = parts[statusIdx].Trim();
                        if (sVal.Equals("Đang học", StringComparison.OrdinalIgnoreCase) || sVal.Equals("Studying", StringComparison.OrdinalIgnoreCase))
                            status = "Studying";
                        else if (sVal.Equals("Tạm dừng", StringComparison.OrdinalIgnoreCase) || sVal.Equals("Suspended", StringComparison.OrdinalIgnoreCase))
                            status = "Suspended";
                        else if (sVal.Equals("Tốt nghiệp", StringComparison.OrdinalIgnoreCase) || sVal.Equals("Graduated", StringComparison.OrdinalIgnoreCase))
                            status = "Graduated";
                        else
                            status = sVal;
                    }

                    ImportedStudents.Add(new CreateStudentRequest
                    {
                        StudentCode = code,
                        FullName = name,
                        DOB = dob,
                        ProgrammeName = prog,
                        CurrentStatus = status
                    });
                    validCount++;
                }

                ValidationSummary = $"Đã đọc {validCount} dòng hợp lệ, bỏ qua {invalidCount} dòng lỗi.";
            }
            catch (Exception ex)
            {
                ValidationSummary = $"Lỗi đọc tệp CSV: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task ConfirmImportCsvAsync()
        {
            if (!ImportedStudents.Any())
            {
                System.Windows.MessageBox.Show("Không có dữ liệu hợp lệ để nhập.", "Cảnh báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                var requests = ImportedStudents.ToList();
                var result = await _apiService.PostAsync<List<CreateStudentRequest>, BulkImportResultDto>("student/bulk", requests);
                if (result != null)
                {
                    string msg = $"Nhập thành công {result.SuccessCount}/{result.TotalCount} học viên.";
                    if (result.Errors != null && result.Errors.Any())
                    {
                        msg += $"\n\nChi tiết lỗi ({result.Errors.Count} dòng):\n" + string.Join("\n", result.Errors.Take(10));
                        if (result.Errors.Count > 10)
                        {
                            msg += $"\n... và {result.Errors.Count - 10} lỗi khác.";
                        }
                    }
                    
                    System.Windows.MessageBox.Show(msg, "Kết quả nhập hàng loạt", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    
                    IsImportCsvOpen = false;
                    await SearchAsync();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi gọi API nhập bulk: {ex.Message}", "Lỗi hệ thống", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
