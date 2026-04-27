using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class RegisterDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        public string RoleId { get; set; }
        public string EmployeeId { get; set; }
    }
}
