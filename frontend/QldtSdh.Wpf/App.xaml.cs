using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QldtSdh.Wpf.Services;
using QldtSdh.Wpf.ViewModels;

namespace QldtSdh.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Build Configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // 2. Build Dependency Injection Container
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(configuration);

            // Configure HttpClient and ApiService
            var apiBaseAddress = configuration["ApiSettings:BaseAddress"] 
                                 ?? "http://localhost:5000/api/";
            
            serviceCollection.AddSingleton(new HttpClient { BaseAddress = new Uri(apiBaseAddress) });
            serviceCollection.AddSingleton<ApiService>();

            // Register ViewModels
            serviceCollection.AddSingleton<MainViewModel>();
            serviceCollection.AddSingleton<GlobalSearchViewModel>();
            serviceCollection.AddSingleton<Student360ViewModel>();
            serviceCollection.AddSingleton<CaseBoardViewModel>();
            serviceCollection.AddSingleton<OperationsDashboardViewModel>();
            serviceCollection.AddSingleton<SnapshotHistoryViewModel>();

            // Register Views
            serviceCollection.AddTransient<MainWindow>();

            ServiceProvider = serviceCollection.BuildServiceProvider();

            // 3. Start Application
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}
