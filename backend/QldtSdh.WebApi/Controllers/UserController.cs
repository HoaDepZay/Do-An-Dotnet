using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QldtSdh.Data;
using QldtSdh.Data.Models;
using QldtSdh.Shared;

namespace QldtSdh.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN, STAFF")]
    public class UserController : ControllerBase
    {
        private readonly QldtSdhDbContext _context;

        public UserController(QldtSdhDbContext context)
        {
            _context = context;
        }

        // GET: api/user
        [HttpGet]
        public ActionResult<IEnumerable<UserDto>> GetUsers()
        {
            var users = _context.Users
                .Include(u => u.Role)
                .OrderBy(u => u.Username)
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    RoleId = u.RoleId,
                    RoleCode = u.Role != null ? u.Role.RoleCode : string.Empty,
                    RoleName = u.Role != null ? u.Role.RoleName : string.Empty,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToList();

            return Ok(users);
        }

        // POST: api/user
        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public ActionResult<UserDto> CreateUser([FromBody] CreateUserRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Thông tin người dùng không hợp lệ.");
            }

            var usernameExists = _context.Users.Any(u => u.Username.ToLower() == request.Username.ToLower());
            if (usernameExists)
            {
                return BadRequest("Tên đăng nhập đã tồn tại trong hệ thống.");
            }

            var role = _context.Roles.Find(request.RoleId);
            if (role == null)
            {
                return BadRequest("Vai trò (Role) không tồn tại.");
            }

            var user = new User
            {
                Username = request.Username.Trim(),
                PasswordHash = HashPassword(request.Password),
                FullName = request.FullName.Trim(),
                Email = request.Email?.Trim() ?? string.Empty,
                RoleId = request.RoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleCode = role.RoleCode,
                RoleName = role.RoleName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
        }

        // PUT: api/user/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN")]
        public IActionResult UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            if (request == null)
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            var role = _context.Roles.Find(request.RoleId);
            if (role == null)
            {
                return BadRequest("Vai trò (Role) không tồn tại.");
            }

            user.FullName = request.FullName.Trim();
            user.Email = request.Email?.Trim() ?? string.Empty;
            user.RoleId = request.RoleId;
            user.IsActive = request.IsActive;

            _context.SaveChanges();

            return NoContent();
        }

        // PUT: api/user/{id}/toggle-status
        [HttpPut("{id}/toggle-status")]
        [Authorize(Roles = "ADMIN")]
        public IActionResult ToggleStatus(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            user.IsActive = !user.IsActive;
            _context.SaveChanges();

            return Ok(new { IsActive = user.IsActive });
        }

        // PUT: api/user/{id}/reset-password
        [HttpPut("{id}/reset-password")]
        [Authorize(Roles = "ADMIN")]
        public IActionResult ResetPassword(int id, [FromBody] ResetPasswordRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest("Mật khẩu mới không được để trống.");
            }

            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            user.PasswordHash = HashPassword(request.NewPassword);
            _context.SaveChanges();

            return Ok("Đặt lại mật khẩu thành công.");
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
