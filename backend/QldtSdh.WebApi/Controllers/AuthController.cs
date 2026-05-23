using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QldtSdh.Data;
using QldtSdh.Data.Models;
using QldtSdh.Shared;

namespace QldtSdh.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly QldtSdhDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(QldtSdhDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("login")]
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new LoginResponse { Success = false, Message = "Tên đăng nhập và mật khẩu không được trống." });
            }

            var hash = HashPassword(request.Password);
            var user = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Username.ToLower() == request.Username.ToLower());

            if (user == null || user.PasswordHash != hash)
            {
                return Unauthorized(new LoginResponse { Success = false, Message = "Tên đăng nhập hoặc mật khẩu không chính xác." });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new LoginResponse { Success = false, Message = "Tài khoản của bạn đã bị khóa." });
            }

            // Generate JWT Token
            var jwtSettings = _config.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "Antigravity_DeepMind_Super_Secret_Key_For_Jwt_Auth_2026!";
            var issuer = jwtSettings["Issuer"] ?? "QldtSdh.WebApi";
            var audience = jwtSettings["Audience"] ?? "QldtSdh.Wpf";
            var expiryMin = int.TryParse(jwtSettings["ExpiryInMinutes"], out var min) ? min : 480;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role?.RoleCode ?? "STAFF"),
                new Claim("FullName", user.FullName)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMin),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new LoginResponse
            {
                Success = true,
                Message = "Đăng nhập thành công.",
                Token = tokenString,
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                RoleCode = user.Role?.RoleCode ?? "STAFF"
            });
        }

        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
