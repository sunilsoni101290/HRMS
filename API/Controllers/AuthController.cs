using Application.DTOs;
using Application.Interfaces;
using Application.Services.JWT_Token;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;
        private readonly ISequenceService _sequenceService;

        public AuthController(ApplicationDbContext context, JwtService jwtService,ISequenceService sequenceService)
        {
            _context = context;
            _jwtService = jwtService;
            _sequenceService = sequenceService;
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto request)
        {
            var user = _context.Users
                .FirstOrDefault(x => x.Username == request.Username);

            if (user == null)
                return Unauthorized("Invalid username");

            if (!PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized("Invalid password");

            var roles = _context.UserRoles
                .Where(x => x.UserId == user.Id)
                .Select(x => x.Role.Name)
                .ToList();

            var token = _jwtService.GenerateAccessToken(user, roles);

            var refreshToken = new RefreshToken
            {
                Id =Guid.NewGuid().ToString(),
                Token = _jwtService.GenerateRefreshToken(),
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                UserId = user.Id,
                CreatedBy = user.Id,
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken.Token,
                Expiry = DateTime.UtcNow.AddMinutes(60)
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == refreshToken && !x.IsRevoked);

            if (token == null || token.ExpiryDate < DateTime.UtcNow)
                return Unauthorized("Invalid refresh token");

            var roles = _context.UserRoles
               .Where(x => x.UserId == token.UserId)
               .Select(x => x.Role.Name)
               .ToList();

            var newAccessToken = _jwtService.GenerateAccessToken(token.User,roles);

            return Ok(new AuthResponse
            {
                Token = newAccessToken
            });
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto request)
        {
            var employee = _context.Employees
            .FirstOrDefault(e => e.Id == request.EmployeeId);

            if (employee == null)
                return BadRequest("Employee not found");

            // 🔹 1. Check user exists
            if (await _context.Users.AnyAsync(x => x.Username == request.Username))
                return BadRequest("User already exists");

            // 🔹 2. Validate Role
            var roleExists = await _context.Roles.AnyAsync(r => r.Id == request.RoleId);
            if (!roleExists)
                return BadRequest("Invalid Role");

            // 🔹 3. Create User
            var user = new User
            {
                Id = await _sequenceService.GetNextERPId("U","2026-27"), // 🔥 Important
                Username = request.Username,
                PasswordHash = PasswordHelper.HashPassword(request.Password),
                TenantId = employee.TenantId,
                EmployeeId = employee.Id,
                CreatedOn = DateTime.UtcNow,
                CreatedBy="System"
            };

            // 🔹 4. Create UserRole
            var userRole = new UserRole
            {
                Id = await _sequenceService.GetNextERPId("UR", "2026-27"),
                UserId = user.Id,
                RoleId = request.RoleId,
                CreatedOn = DateTime.UtcNow
            };

            // 🔹 5. Save in transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Users.Add(user);
                _context.UserRoles.Add(userRole);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok("User Created Successfully");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Something went wrong");
            }
        }

        [Authorize]
        [HttpGet("secure-data")]
        public IActionResult GetSecureData()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var tenantId = User.FindFirst("TenantId")?.Value;

            return Ok(new { userId, tenantId });
        }
    }
}
