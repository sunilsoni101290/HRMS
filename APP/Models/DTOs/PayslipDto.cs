using System;
using System.Collections.Generic;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.Payroll.PayslipDto property-for-property
    // (same pattern as PayrollDto/PayrollDtos.cs above) - this is the shape
    // ApiService deserializes "payroll/payslip-document/{id}" into on the
    // MVC side.
    public class PayslipDto
    {
        public string PayrollId { get; set; } = string.Empty;
        public string? Status { get; set; }

        public PayslipCompanyDto Company { get; set; } = new();
        public PayslipEmployeeDto Employee { get; set; } = new();
        public PayslipEmploymentDto Employment { get; set; } = new();
        public PayslipBankDetailsDto? BankDetails { get; set; }

        public int SalaryYear { get; set; }
        public int SalaryMonth { get; set; }
        public string? MonthName { get; set; }
        public DateTime SalaryDate { get; set; }
        public DateTime PayPeriodStart { get; set; }
        public DateTime PayPeriodEnd { get; set; }
        public int DaysInMonth { get; set; }

        public List<PayslipLineDto> Earnings { get; set; } = new();
        public List<PayslipLineDto> Deductions { get; set; } = new();

        public decimal GrossEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
        public string? NetSalaryInWords { get; set; }

        public PayslipAttendanceSummaryDto AttendanceSummary { get; set; } = new();
        public PayslipTaxInfoDto TaxInfo { get; set; } = new();
        public PayslipYtdSummaryDto? Ytd { get; set; }
        public List<PayslipLeaveBalanceLineDto> LeaveBalances { get; set; } = new();
        public List<string> Remarks { get; set; } = new();

        public string? VerificationToken { get; set; }
        public string? SignatoryCompanyName { get; set; }
        public string? SignatoryImageUrl { get; set; }

        public int RecalculatedCount { get; set; }

        // ---- View-only, never populated from the API response ----
        // Set by APP.Controllers.PayrollController.Payslip AFTER the DTO
        // comes back from the API - the absolute verification URL and its
        // QR code image are pure presentation concerns (Request.Scheme/
        // Url.Action, QRCoder) that belong in the MVC layer only, never in
        // the API/Application payslip-building logic. See
        // APP/Helpers/QrCodeHelper.cs.
        public string? VerificationUrl { get; set; }
        public string? VerificationQrCodeDataUri { get; set; }
    }

    public class PayslipCompanyDto
    {
        public string? Name { get; set; }
        public string? LogoUrl { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
    }

    public class PayslipEmployeeDto
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? PhotoUrl { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public string? BranchName { get; set; }
    }

    public class PayslipEmploymentDto
    {
        public string? PAN { get; set; }
        public string? UAN { get; set; }
        public string? PFNumber { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public string? EmploymentTypeName { get; set; }
    }

    public class PayslipBankDetailsDto
    {
        public string? BankName { get; set; }
        public string? AccountNumberMasked { get; set; }
        public string? IFSCCode { get; set; }
        public string? AccountTypeName { get; set; }
    }

    public class PayslipLineDto
    {
        public string ComponentName { get; set; } = string.Empty;
        public string? RateOrUnitsDisplay { get; set; }
        public decimal Amount { get; set; }
    }

    public class PayslipAttendanceSummaryDto
    {
        public decimal? TotalDays { get; set; }
        public decimal? PresentDays { get; set; }
        public decimal? AbsentDays { get; set; }
        public decimal? PaidLeaveDays { get; set; }
        public decimal? UnpaidLeaveDays { get; set; }
        public decimal? WeekOffDays { get; set; }
        public decimal? HolidayDays { get; set; }
        public decimal? PayableDays { get; set; }
    }

    public class PayslipTaxInfoDto
    {
        public string? TaxRegimeName { get; set; }
        public decimal? LopDays { get; set; }
    }

    public class PayslipYtdSummaryDto
    {
        public string? PeriodLabel { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
    }

    public class PayslipLeaveBalanceLineDto
    {
        public string LeaveTypeName { get; set; } = string.Empty;
        public decimal Entitled { get; set; }
        public decimal Used { get; set; }
        public decimal Balance { get; set; }
    }

    public class PayslipVerificationResultDto
    {
        public bool IsValid { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? CompanyName { get; set; }
        public string? MonthName { get; set; }
        public int SalaryYear { get; set; }
        public decimal NetSalary { get; set; }
        public string? Status { get; set; }
    }
}
