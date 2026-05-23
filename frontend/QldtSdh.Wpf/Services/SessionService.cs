using System;

namespace QldtSdh.Wpf.Services
{
    public class SessionService
    {
        public int UserId { get; private set; }
        public string Username { get; private set; } = string.Empty;
        public string FullName { get; private set; } = string.Empty;
        public string RoleCode { get; private set; } = string.Empty;
        public string Token { get; private set; } = string.Empty;

        public bool IsLoggedIn => !string.IsNullOrEmpty(Token);
        public bool IsAdmin => RoleCode.Equals("ADMIN", StringComparison.OrdinalIgnoreCase);

        public event Action? SessionChanged;

        public void SaveSession(int userId, string username, string fullName, string roleCode, string token)
        {
            UserId = userId;
            Username = username;
            FullName = fullName;
            RoleCode = roleCode;
            Token = token;
            SessionChanged?.Invoke();
        }

        public void ClearSession()
        {
            UserId = 0;
            Username = string.Empty;
            FullName = string.Empty;
            RoleCode = string.Empty;
            Token = string.Empty;
            SessionChanged?.Invoke();
        }
    }
}
