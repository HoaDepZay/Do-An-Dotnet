using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using QldtSdh.Shared;

namespace QldtSdh.Wpf.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private object? _currentView;

        [ObservableProperty]
        private string _activeMenu = "Search"; // e.g. Search, Dashboard, Cases, Snapshots

        public MainViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
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
