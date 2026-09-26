using System;
using System.Collections.Generic;

namespace Application.DTOs.Payroll
{
    // ==============================
    // Payslip (display document)
    // ==============================
    //
    // This is deliberately a SEPARATE, read-only DTO from PayrollDto - the
    // payslip is a formatted presentation of an already-generated Payroll
    // record (see PayrollBusinessService.GetPayslipDocumentAsync) plus a
    // handful of read-only lookups (Attendance day-type counts, Leave
    // Balance, Financial-Year-to-date totals, Tax Regime). Nothing here
    // recalculates payroll figures - GrossEarnings/TotalDeductions/
    // NetSalary are copied straight from the Payroll row, which remains
    // the single source of truth.
    public class PayslipDto
    {
        public string PayrollId { get; set; } = string.Empty;
        public string? Status { get; set; }

        public PayslipCompanyDto Company { get; set; } = new();
        public PayslipEmployeeDto Employee { get; set; } = new();
        public PayslipEmploymentDto Employment { get; set; } = new();

        // Null when the employee has no bank record on file - the view
        // hides the Bank Details panel entirely rather than showing blanks.
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

        // Copied verbatim from Payroll.TotalEarnings / TotalDeductions /
        // NetSalary - see class remarks above.
        public decimal GrossEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
        public string? NetSalaryInWords { get; set; }

        public PayslipAttendanceSummaryDto AttendanceSummary { get; set; } = new();
        public PayslipTaxInfoDto TaxInfo { get; set; } = new();

        // Null when the employee's company has no Financial Year configured
        // (Master -> Financial Year) - the view hides the YTD panel rather
        // than fabricating a period.
        public PayslipYtdSummaryDto? Ytd { get; set; }

        // Empty when the employee has no Leave Balance rows for the payslip
        // year - the view hides the Leave Balance panel entirely.
        public List<PayslipLeaveBalanceLineDto> LeaveBalances { get; set; } = new();

        public List<string> Remarks { get; set; } = new();

        // HMAC-signed, storage-free token for the "Scan to Verify" QR code
        // - see Domain.Helper.PayslipVerificationHelper. Never a raw
        // PayrollId/database id by itself.
        public string? VerificationToken { get; set; }

        public string? SignatoryCompanyName { get; set; }

        // Always null today - Company has no configured signature image.
        // Kept so the view (and a future Company "Signature" upload) don't
        // need restructuring later.
        public string? SignatoryImageUrl { get; set; }

        public int RecalculatedCount { get; set; }
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

        // Always "-" today - PayrollDetail persists only the final
        // (already-prorated) Amount, not the configured rate/percentage/
        // units it was derived from, so a genuine Rate/Units figure can't
        // be reconstructed for an already-generated payroll without
        // re-running Salary Processing (which the payslip must never do -
        // see class remarks on PayslipDto). Kept as its own column so a
        // future PayrollDetail.RateDisplay/CalculationType field can
        // populate it with no view changes.
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
        // Null when the employee has no EmployeeTaxComputation row for the
        // payslip's Financial Year (Taxation module not run for them yet).
        public string? TaxRegimeName { get; set; }

        // Loss-of-Pay days - mapped from Payroll.UnpaidLeaveDays (the same
        // "not paid" day bucket Salary Processing already computes), not a
        // separately tracked figure.
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

    // Result of scanning/opening a payslip's verification QR - deliberately
    // minimal (no bank/PAN/UAN/full breakdown) since this endpoint is
    // reachable by anyone holding the token, with no login required.
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
