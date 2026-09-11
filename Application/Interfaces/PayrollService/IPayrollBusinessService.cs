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

        // Salary Processing - Select Month -> Load Attendance (Preview) ->
        // Review -> Process. Preview never writes anything; Process writes
        // exactly the employees the caller confirmed (see
        // SalaryProcessRequestDto's remarks). Recalculate re-runs
        // SalaryCalculationService against an EXISTING Payroll row, subject
        // to the Draft/Processed/Paid lock rules documented on
        // RecalculateAsync itself.
        Task<SalaryProcessingPreviewDto> PreviewAsync(int salaryYear, int salaryMonth, string? companyId, string? branchId, string tenantId);
        Task<PayrollGenerateResultDto> ProcessAsync(SalaryProcessRequestDto dto);
        Task<string> RecalculateAsync(SalaryRecalculateRequestDto dto);

        // Payslip
        Task<string> GeneratePayslipAsync(string payrollId, string userId);
        Task<PayrollDto> GetPayslipAsync(string payrollId);

        // Dashboard
        Task<PayrollDashboardDto> GetDashboardAsync(int year, int month);
    }
}
