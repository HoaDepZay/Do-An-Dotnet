using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using QldtSdh.Shared;
using QldtSdh.Wpf.Services;

namespace QldtSdh.Wpf.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private bool _isLoading;

        public LoginViewModel(ApiService apiService, SessionService sessionService)
        {
            _apiService = apiService;
            _sessionService = sessionService;
        }

        public async Task<bool> LoginAsync(string password)
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Tên đăng nhập không được để trống.";
                HasError = true;
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage = "Mật khẩu không được để trống.";
                HasError = true;
                return false;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            HasError = false;

            try
            {
                var request = new LoginRequest { Username = Username, Password = password };
                var response = await _apiService.PostAsync<LoginRequest, LoginResponse>("auth/login", request);

                if (response != null && response.Success)
                {
                    _sessionService.SaveSession(
                        response.UserId,
                        response.Username,
                        response.FullName,
                        response.RoleCode,
                        response.Token
                    );
                    return true;
                }
                else
                {
                    ErrorMessage = response?.Message ?? "Tên đăng nhập hoặc mật khẩu không đúng.";
                    HasError = true;
                    return false;
                }
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ErrorMessage = $"Lỗi kết nối hoặc hệ thống: {msg}";
                HasError = true;
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
