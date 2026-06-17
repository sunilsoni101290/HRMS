using Application.DTOs.Employee;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.EmployeeInterface
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardAsync(string tenantId);
    }
}
