using Application.DTOs.Payroll;

namespace Application.Interfaces.Payroll
{
    // Dedicated, standalone salary-proration calculator - deliberately
    // separate from PayrollBusinessService (which owns persistence/
    // orchestration/status workflow/loan recovery). Nothing in here writes
    // to the database - callers (PayrollBusinessService.PreviewAsync /
    // ProcessAsync / RecalculateAsync) decide what to do with the result.
    // See SalaryCalculationService for the full algorithm and the "Salary
    // Processing" root-cause writeup in its class remarks.
    public interface ISalaryCalculationService
    {
        Task<SalaryCalculationResultDto> CalculateAsync(string employeeId, int salaryYear, int salaryMonth, string tenantId);

        Task<List<SalaryCalculationResultDto>> CalculateBatchAsync(List<string> employeeIds, int salaryYear, int salaryMonth, string tenantId);
    }
}
