using Application.DTOs.Payroll;

namespace Application.Interfaces.Payroll
{
    public interface IPayrollBusinessService
    {
        Task<List<PayrollListDto>> GetAllAsync(
            int? year = null,
            int? month = null,
            string? departmentId = null,
            string? designationId = null,
            string? employeeStatus = null,
            string? paymentStatus = null,
            string? search = null);
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

        // Payslip (redesigned display document) - builds a fully-formatted
        // PayslipDto from an EXISTING Payroll row (Company/Employee/Bank/PF/
        // Attendance/LeaveBalance/FinancialYear/TaxComputation lookups only -
        // never recalculates GrossSalary/TotalDeductions/NetSalary). Scoped
        // to tenantId - returns null for a payrollId belonging to a
        // different tenant, or one that doesn't exist, so the caller can
        // treat both the same way (404), never leaking which case it was.
        Task<PayslipDto?> GetPayslipDocumentAsync(string payrollId, string tenantId);

        // "Scan to Verify Payslip" - validates the HMAC token embedded in
        // the payslip's QR code (see Domain.Helper.PayslipVerificationHelper)
        // and returns a minimal, non-sensitive confirmation. No
        // authentication required to call this (anyone holding a printed
        // payslip's QR code can verify it), so the result deliberately
        // excludes bank/PAN/UAN/earnings-deductions breakdown.
        Task<PayslipVerificationResultDto?> VerifyPayslipAsync(string token);

        // "Email Payslip" toolbar action - emails the employee on file a
        // plain summary (Company/Month/Net Pay in words) plus the same
        // verification link the QR code encodes. Never attaches a PDF (no
        // PDF generation exists in this codebase - see Payslip.cshtml's
        // Download PDF button, which relies on the browser's own "Save as
        // PDF" print target instead). No-ops (returns false) exactly like
        // IEmailSender itself when SMTP isn't configured, or the employee
        // has no email on file - never throws for either case.
        Task<bool> EmailPayslipAsync(string payrollId, string tenantId, string verificationUrl);

        // Dashboard
        Task<PayrollDashboardDto> GetDashboardAsync(int year, int month);
    }
}
