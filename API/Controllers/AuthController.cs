using Application.DTOs.Auth;
using Application.Interfaces.Auth;
using Application.Services.Auth;
using Application.Services.JWT_Token;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // 🔥 Debug
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrEmpty(dto.TenantId))
                return BadRequest("Tenant not provided");

            var result = await _authService.RegisterAsync(dto);

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto);

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Login Successful",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = ex.Message,

                    Errors = new List<string>
                    {
                        ex.Message
                    }
                });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> Refresh(string refreshToken)
        {
            var result = await _authService.RefreshTokenAsync(refreshToken);
            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(string refreshToken)
        {
            var result = await _authService.LogoutAsync(refreshToken);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("user-list")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _authService.GetAllAsync();

            return Ok(result);
        }

        // ==============================
        // GET USER BY ID
        // ==============================
        [Authorize]
        [HttpGet("get-user-details/{id}")]
        public async Task<IActionResult> GetById([FromRoute] string id)
        {
            var result = await _authService.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // ===============================================
        // GET USER DETAILS BY ID FOR UPDATE APPROACH ONLY
        // ===============================================
        [Authorize]
        [HttpGet("get-edit-details/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _authService.GetUserDetailByIdAsync(id);

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPut]
        public async Task<IActionResult> Edit(User model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var data = await _authService.UpdateUserAsync(model);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        #region CHANGE PASSWORD

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePAsswordDto model)
        {
            bool result = await _authService.ChangePasswordAsync(
                model.UserId,
                model.OldPassword,
                model.NewPassword);

            if (!result)
            {
                return NotFound();
            }

            return Ok(result);
        }

        #endregion


        // ==============================
        // GET USER BY EMP ID
        // ==============================
        [Authorize]
        [HttpGet("user-detailsby-emp/{empId}")]
        public async Task<IActionResult> GetUserByEmpId([FromRoute] string empId)
        {
            var result = await _authService.GetUserDetailsByEmpIdAsync(empId);

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
