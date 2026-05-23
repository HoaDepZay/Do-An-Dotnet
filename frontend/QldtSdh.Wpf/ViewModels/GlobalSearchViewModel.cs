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
    }
}
