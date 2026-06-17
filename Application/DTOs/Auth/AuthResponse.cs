using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Auth
{
    public class AuthResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public int ExpiresIn { get; set; }

        public string FullName { get; set; }

        public string RoleName { get; set; }

        public string Designation { get; set; }

        public string Email { get; set; }

        public string UserId { get; set; }

        public string TenantId { get; set; }

        public string Username { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public T Data { get; set; }

        public List<string> Errors { get; set; }
    }
}
