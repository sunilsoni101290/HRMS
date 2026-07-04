using Application.DTOs.Payroll;

namespace Application.Interfaces.Payroll
{
    public interface IPayrollBusinessService
    {
        Task<List<PayrollListDto>> GetAllAsync(int? year = null, int? month = null);
        Task<PayrollDto> GetByIdAsync(string id);
        Task<PayrollGenerateResultDto> GenerateAsync(PayrollGenerateDto dto);
        Task<string> ChangeStatusAsync(string id, string status, string userId);
        Task<bool> DeleteAsync(string id);

        // Payslip
        Task<string> GeneratePayslipAsync(string payrollId, string userId);
        Task<PayrollDto> GetPayslipAsync(string payrollId);

        // Dashboard
        Task<PayrollDashboardDto> GetDashboardAsync(int year, int month);
    }
}
