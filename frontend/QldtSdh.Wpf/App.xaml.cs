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

        private bool _isDarkMode = true;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                _isDarkMode = value;
                ApplyTheme();
            }
        }

        public void ApplyTheme()
        {
            try
            {
                System.Windows.Media.Color themeBlackColor;
                System.Windows.Media.Color panelDarkColor;
                System.Windows.Media.Color borderDarkColor;
                System.Windows.Media.Color textWhiteColor;
                System.Windows.Media.Color textMutedColor;

                if (_isDarkMode)
                {
                    themeBlackColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0B0F0D");
                    panelDarkColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1A1D1A");
                    borderDarkColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D322D");
                    textWhiteColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F8FAFC");
                    textMutedColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#94A3B8");
                }
                else
                {
                    themeBlackColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F1F5F9");
                    panelDarkColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFF");
                    borderDarkColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CBD5E1");
                    textWhiteColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0F172A");
                    textMutedColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#475569");
                }

                // Re-register new brush instances to update WPF UI elements bound via DynamicResource
                Application.Current.Resources["ThemeBlackBrush"] = new System.Windows.Media.SolidColorBrush(themeBlackColor);
                Application.Current.Resources["PanelDarkBrush"] = new System.Windows.Media.SolidColorBrush(panelDarkColor);
                Application.Current.Resources["BorderDarkBrush"] = new System.Windows.Media.SolidColorBrush(borderDarkColor);
                Application.Current.Resources["TextWhiteBrush"] = new System.Windows.Media.SolidColorBrush(textWhiteColor);
                Application.Current.Resources["TextMutedBrush"] = new System.Windows.Media.SolidColorBrush(textMutedColor);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi đổi giao diện: {ex}");
                System.Diagnostics.Debug.WriteLine($"Lỗi đổi giao diện: {ex.Message}");
            }
        }

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
            
            serviceCollection.AddSingleton<SessionService>();
            serviceCollection.AddSingleton(new HttpClient { BaseAddress = new Uri(apiBaseAddress) });
            serviceCollection.AddSingleton<ApiService>();

            // Register ViewModels
            serviceCollection.AddTransient<LoginViewModel>();
            serviceCollection.AddSingleton<MainViewModel>();
            serviceCollection.AddSingleton<GlobalSearchViewModel>();
            serviceCollection.AddSingleton<Student360ViewModel>();
            serviceCollection.AddSingleton<CaseBoardViewModel>();
            serviceCollection.AddSingleton<OperationsDashboardViewModel>();
            serviceCollection.AddSingleton<SnapshotHistoryViewModel>();
            serviceCollection.AddSingleton<UserManagementViewModel>();

            // Register Views
            serviceCollection.AddTransient<LoginWindow>();
            serviceCollection.AddTransient<MainWindow>();

            ServiceProvider = serviceCollection.BuildServiceProvider();

            // 3. Start Application with LoginWindow
            var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }
    }
}
