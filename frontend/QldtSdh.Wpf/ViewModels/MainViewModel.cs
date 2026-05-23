using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using QldtSdh.Shared;
using QldtSdh.Wpf.Services;

namespace QldtSdh.Wpf.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SessionService _sessionService;

        [ObservableProperty]
        private object? _currentView;

        [ObservableProperty]
        private string _activeMenu = "Search"; // e.g. Search, Dashboard, Cases, Snapshots, UserManagement

        public bool IsAdminVisible => _sessionService.IsAdmin;
        public string CurrentUserName => _sessionService.FullName;
        public string CurrentUserRole => _sessionService.RoleCode == "ADMIN" ? "Quản trị viên hệ thống" : "Cán bộ đào tạo";
        
        public string CurrentUserInitials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CurrentUserName)) return "CB";
                var parts = CurrentUserName.Trim().Split(' ');
                if (parts.Length >= 2)
                {
                    var first = parts[0];
                    var last = parts[parts.Length - 1];
                    if (first.Length > 0 && last.Length > 0)
                    {
                        return (first[0].ToString() + last[0].ToString()).ToUpper();
                    }
                }
                return CurrentUserName.Substring(0, Math.Min(2, CurrentUserName.Length)).ToUpper();
            }
        }

        public MainViewModel(IServiceProvider serviceProvider, SessionService sessionService)
        {
            _serviceProvider = serviceProvider;
            _sessionService = sessionService;
            
            // Set default view to Global Search
            NavigateToSearch();
        }

        [RelayCommand]
        public void NavigateToSearch()
        {
            var vm = _serviceProvider.GetRequiredService<GlobalSearchViewModel>();
            // Search with current filters to ensure fresh data
            _ = vm.SearchAsync();
            CurrentView = vm;
            ActiveMenu = "Search";
        }

        [RelayCommand]
        public void NavigateToDashboard()
        {
            var vm = _serviceProvider.GetRequiredService<OperationsDashboardViewModel>();
            _ = vm.LoadKpisAsync();
            CurrentView = vm;
            ActiveMenu = "Dashboard";
        }

        [RelayCommand]
        public void NavigateToCases()
        {
            var vm = _serviceProvider.GetRequiredService<CaseBoardViewModel>();
            _ = vm.LoadCasesAsync();
            CurrentView = vm;
            ActiveMenu = "Cases";
        }

        [RelayCommand]
        public void NavigateToSnapshots()
        {
            CurrentView = _serviceProvider.GetRequiredService<SnapshotHistoryViewModel>();
            ActiveMenu = "Snapshots";
        }

        [RelayCommand]
        public void NavigateToUserManagement()
        {
            var vm = _serviceProvider.GetRequiredService<UserManagementViewModel>();
            _ = vm.LoadUsersAsync();
            CurrentView = vm;
            ActiveMenu = "UserManagement";
        }

        [RelayCommand]
        public void Logout()
        {
            _sessionService.ClearSession();
            
            // Open LoginWindow
            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
            
            // Close MainWindow
            foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
            {
                if (window is MainWindow)
                {
                    window.Close();
                    break;
                }
            }
        }

        // Method to navigate directly to a student's 360 profile
        public void NavigateToStudent360(int studentId)
        {
            var student360Vm = _serviceProvider.GetRequiredService<Student360ViewModel>();
            _ = student360Vm.LoadStudentAsync(studentId);
            CurrentView = student360Vm;
            ActiveMenu = "Search";
        }

        // Method to navigate to Case Board and open Create Case Dialog with prefilled student
        public void NavigateToCasesAndCreate(StudentDto student)
        {
            var caseBoardVm = _serviceProvider.GetRequiredService<CaseBoardViewModel>();
            _ = caseBoardVm.LoadCasesAsync();
            caseBoardVm.OpenCreateCaseDialog(student);
            CurrentView = caseBoardVm;
            ActiveMenu = "Cases";
        }
    }
}
