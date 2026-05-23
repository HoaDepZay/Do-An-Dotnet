using System.Windows;
using System.Windows.Input;
using QldtSdh.Wpf.ViewModels;

namespace QldtSdh.Wpf
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            // Focus username box on load
            Loaded += (s, e) => TxtUsername.Focus();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var password = TxtPassword.Password;
            bool success = await _viewModel.LoginAsync(password);
            if (success)
            {
                var mainWindow = App.ServiceProvider.GetService(typeof(MainWindow)) as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.Show();
                    Close();
                }
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnLogin_Click(sender, e);
            }
        }
    }
}
