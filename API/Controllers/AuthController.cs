using Application.DTOs.Auth;
using Application.Interfaces.Auth;
using Application.Services.Auth;
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
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth)
        {
            _auth = auth;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // 🔥 Debug
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrEmpty(dto.TenantId))
                return BadRequest("Tenant not provided");

            var result = await _auth.RegisterAsync(dto);

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _auth.LoginAsync(dto);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> Refresh(string refreshToken)
        {
            var result = await _auth.RefreshTokenAsync(refreshToken);
            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(string refreshToken)
        {
            var result = await _auth.LogoutAsync(refreshToken);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("user-list")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _auth.GetAllAsync();

            return Ok(result);
        }

        // ==============================
        // GET USER BY ID
        // ==============================
        [Authorize]
        [HttpGet("get-user-details/{id}")]
        public async Task<IActionResult> GetById([FromRoute] string id)
        {
            var result = await _auth.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
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
