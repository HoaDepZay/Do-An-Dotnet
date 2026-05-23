using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Extensions.DependencyInjection;
using QldtSdh.Shared;
using QldtSdh.Wpf.Services;

namespace QldtSdh.Wpf.ViewModels
{
    public partial class OperationsDashboardViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private ObservableCollection<KpiDto> _kpis = new();

        [ObservableProperty]
        private ObservableCollection<StudentDto> _drillDownStudents = new();

        [ObservableProperty]
        private ObservableCollection<CaseDto> _drillDownCases = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsShowStudentsDrillDown))]
        private bool _isShowCasesDrillDown;

        public bool IsShowStudentsDrillDown => !IsShowCasesDrillDown;

        [ObservableProperty]
        private string _selectedKpiTitle = "Chọn KPI để xem chi tiết học viên";

        // LiveCharts2 Series
        [ObservableProperty]
        private ISeries[] _statusColumnSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private ISeries[] _casePieSeries = Array.Empty<ISeries>();

        public Axis[] XAxes { get; set; } = new Axis[]
        {
            new Axis
            {
                Labels = new[] { "Đang học", "Tốt nghiệp", "Tạm dừng" },
                LabelsPaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.SlateGray)
            }
        };

        public OperationsDashboardViewModel(ApiService apiService, IServiceProvider serviceProvider)
        {
            _apiService = apiService;
            _serviceProvider = serviceProvider;
            _ = LoadKpisAsync();
        }

        public async Task LoadKpisAsync()
        {
            try
            {
                var list = await _apiService.GetAsync<List<KpiDto>>("dashboard/kpis");
                Kpis.Clear();
                if (list != null)
                {
                    foreach (var kpi in list)
                    {
                        Kpis.Add(kpi);
                    }

                    // Extract values for Chart rendering
                    double studying = list.FirstOrDefault(k => k.Key == "STUDENTS_STUDYING")?.Value ?? 0;
                    double graduated = list.FirstOrDefault(k => k.Key == "STUDENTS_GRADUATED")?.Value ?? 0;
                    double suspended = list.FirstOrDefault(k => k.Key == "STUDENTS_SUSPENDED")?.Value ?? 0;
                    
                    double activeCases = list.FirstOrDefault(k => k.Key == "CASES_ACTIVE")?.Value ?? 0;
                    double overdueCases = list.FirstOrDefault(k => k.Key == "CASES_OVERDUE")?.Value ?? 0;

                    // 1. Configure Column Chart Series
                    StatusColumnSeries = new ISeries[]
                    {
                        new ColumnSeries<double>
                        {
                            Name = "Số lượng",
                            Values = new[] { studying, graduated, suspended },
                            Stroke = null
                        }
                    };

                    // 2. Configure Pie Chart Series
                    CasePieSeries = new ISeries[]
                    {
                        new PieSeries<double> 
                        { 
                            Values = new[] { activeCases }, 
                            Name = "Đang xử lý",
                            OuterRadiusOffset = 0
                        },
                        new PieSeries<double> 
                        { 
                            Values = new[] { overdueCases }, 
                            Name = "Quá hạn",
                            OuterRadiusOffset = 0
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu Dashboard: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task DrillDownAsync(KpiDto? kpi)
        {
            if (kpi == null) return;

            bool isCaseKpi = kpi.Key == "CASES_ACTIVE" || kpi.Key == "CASES_OVERDUE";
            SelectedKpiTitle = isCaseKpi ? $"Chi tiết sự vụ: {kpi.Title}" : $"Chi tiết học viên: {kpi.Title}";

            try
            {
                if (isCaseKpi)
                {
                    var cases = await _apiService.GetAsync<List<CaseDto>>($"dashboard/kpi-details-cases/{kpi.Key}");
                    DrillDownCases.Clear();
                    DrillDownStudents.Clear();
                    IsShowCasesDrillDown = true;
                    if (cases != null)
                    {
                        foreach (var c in cases)
                        {
                            DrillDownCases.Add(c);
                        }
                    }
                }
                else
                {
                    var students = await _apiService.GetAsync<List<StudentDto>>($"dashboard/kpi-details/{kpi.Key}");
                    DrillDownStudents.Clear();
                    DrillDownCases.Clear();
                    IsShowCasesDrillDown = false;
                    if (students != null)
                    {
                        foreach (var s in students)
                        {
                            DrillDownStudents.Add(s);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải thông tin chi tiết: {ex.Message}");
            }
        }

        [RelayCommand]
        public void ViewStudentProfile(StudentDto? student)
        {
            if (student == null) return;
            var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
            mainVm.NavigateToStudent360(student.StudentId);
        }

        [RelayCommand]
        public void ProcessCase(CaseDto? caseDto)
        {
            if (caseDto == null) return;
            var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
            
            // Navigate to Case tab and load details directly for editing
            var caseBoardVm = _serviceProvider.GetRequiredService<CaseBoardViewModel>();
            _ = caseBoardVm.LoadCasesAsync();
            _ = caseBoardVm.LoadCaseDetailsAsync(caseDto);
            
            mainVm.CurrentView = caseBoardVm;
            mainVm.ActiveMenu = "Cases";
        }

        [RelayCommand]
        public async Task ExportDrillDownCsvAsync()
        {
            if (IsShowCasesDrillDown && !DrillDownCases.Any())
            {
                MessageBox.Show("Không có dữ liệu chi tiết sự vụ để xuất.");
                return;
            }
            if (!IsShowCasesDrillDown && !DrillDownStudents.Any())
            {
                MessageBox.Show("Không có dữ liệu chi tiết học viên để xuất.");
                return;
            }

            try
            {
                var savePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                if (IsShowCasesDrillDown)
                {
                    var csvLines = new List<string> { "Ma Case,Loai su vu,Tieu de,Hoc vien,Nguoi phu trach,Trang thai,Han xu ly" };
                    foreach (var c in DrillDownCases)
                    {
                        csvLines.Add($"{c.CaseCode},{c.CaseType},{c.Title},{c.StudentName},{c.Assignee ?? "Chua phan cong"},{c.Status},{c.DueDate?.ToString("dd/MM/yyyy") ?? "Khong co"}");
                    }
                    await System.IO.File.WriteAllLinesAsync(savePath, csvLines, System.Text.Encoding.UTF8);
                }
                else
                {
                    var csvLines = new List<string> { "Ma hoc vien,Ho va ten,Ngay sinh,Chuong trinh,Trang thai" };
                    foreach (var s in DrillDownStudents)
                    {
                        csvLines.Add($"{s.StudentCode},{s.FullName},{s.DOB:dd/MM/yyyy},{s.ProgrammeName},{s.CurrentStatus}");
                    }
                    await System.IO.File.WriteAllLinesAsync(savePath, csvLines, System.Text.Encoding.UTF8);
                }

                MessageBox.Show($"Xuất báo cáo chi tiết CSV thành công!\nFile lưu tại: {savePath}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất file CSV: {ex.Message}");
            }
        }
    }
}
