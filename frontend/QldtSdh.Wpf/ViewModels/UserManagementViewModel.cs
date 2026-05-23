using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QldtSdh.Shared;
using QldtSdh.Wpf.Services;

namespace QldtSdh.Wpf.ViewModels
{
    public partial class UserManagementViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private ObservableCollection<UserDto> _users = new();

        [ObservableProperty]
        private UserDto? _selectedUser;

        // Form Fields for Add User
        [ObservableProperty]
        private string _newUsername = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private string _newFullName = string.Empty;

        [ObservableProperty]
        private string _newEmail = string.Empty;

        [ObservableProperty]
        private int _newRoleId = 2; // Default to STAFF

        // Reset Password Field
        [ObservableProperty]
        private string _resetPasswordValue = string.Empty;

        [ObservableProperty]
        private string _feedbackMessage = string.Empty;

        [ObservableProperty]
        private bool _isSuccessFeedback;

        [ObservableProperty]
        private bool _isLoading;

        public List<KeyValuePair<int, string>> RoleOptions { get; } = new()
        {
            new KeyValuePair<int, string>(1, "Quản trị viên (ADMIN)"),
            new KeyValuePair<int, string>(2, "Cán bộ đào tạo (STAFF)")
        };

        public UserManagementViewModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        [RelayCommand]
        public async Task LoadUsersAsync()
        {
            IsLoading = true;
            FeedbackMessage = string.Empty;

            try
            {
                var list = await _apiService.GetAsync<List<UserDto>>("user");
                if (list != null)
                {
                    Users = new ObservableCollection<UserDto>(list);
                }
            }
            catch (Exception ex)
            {
                ShowFeedback($"Lỗi tải danh sách người dùng: {ex.Message}", false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task CreateUserAsync()
        {
            if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(NewFullName))
            {
                ShowFeedback("Vui lòng điền đầy đủ Tên đăng nhập, Mật khẩu và Họ tên.", false);
                return;
            }

            IsLoading = true;
            try
            {
                var request = new CreateUserRequest
                {
                    Username = NewUsername.Trim(),
                    Password = NewPassword,
                    FullName = NewFullName.Trim(),
                    Email = NewEmail?.Trim() ?? string.Empty,
                    RoleId = NewRoleId
                };

                var createdUser = await _apiService.PostAsync<CreateUserRequest, UserDto>("user", request);
                if (createdUser != null)
                {
                    Users.Add(createdUser);
                    ShowFeedback("Tạo tài khoản cán bộ thành công!", true);
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                ShowFeedback($"Tạo tài khoản thất bại: {ex.Message}", false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task ToggleStatusAsync(UserDto? user)
        {
            if (user == null) return;

            IsLoading = true;
            try
            {
                // Send put request to toggle-status
                // We pass a dummy empty object to satisfy PutAsync signature
                var success = await _apiService.PutAsync("user/" + user.UserId + "/toggle-status", new { });
                if (success)
                {
                    user.IsActive = !user.IsActive;
                    // Trigger collection refresh so UI updates
                    var index = Users.IndexOf(user);
                    if (index >= 0)
                    {
                        Users[index] = user;
                    }
                    ShowFeedback($"Đã cập nhật trạng thái tài khoản {user.Username}!", true);
                }
            }
            catch (Exception ex)
            {
                ShowFeedback($"Cập nhật trạng thái thất bại: {ex.Message}", false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task ResetPasswordAsync()
        {
            if (SelectedUser == null)
            {
                ShowFeedback("Vui lòng chọn tài khoản cần đặt lại mật khẩu.", false);
                return;
            }

            if (string.IsNullOrWhiteSpace(ResetPasswordValue))
            {
                ShowFeedback("Vui lòng nhập mật khẩu mới.", false);
                return;
            }

            IsLoading = true;
            try
            {
                var request = new ResetPasswordRequest { NewPassword = ResetPasswordValue };
                var success = await _apiService.PutAsync($"user/{SelectedUser.UserId}/reset-password", request);
                if (success)
                {
                    ShowFeedback($"Đặt lại mật khẩu cho tài khoản {SelectedUser.Username} thành công!", true);
                    ResetPasswordValue = string.Empty;
                }
            }
            catch (Exception ex)
            {
                ShowFeedback($"Đặt lại mật khẩu thất bại: {ex.Message}", false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ClearForm()
        {
            NewUsername = string.Empty;
            NewPassword = string.Empty;
            NewFullName = string.Empty;
            NewEmail = string.Empty;
            NewRoleId = 2; // Default to STAFF
        }

        private void ShowFeedback(string message, bool isSuccess)
        {
            FeedbackMessage = message;
            IsSuccessFeedback = isSuccess;
        }
    }
}
