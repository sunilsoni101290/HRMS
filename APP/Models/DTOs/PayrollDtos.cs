using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // ==============================
    // Salary Component
    // ==============================

    public class SalaryComponentDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please enter Component Name.")]
        [MaxLength(100)]
        [Display(Name = "Component Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Please enter Component Code.")]
        [MaxLength(50)]
        [Display(Name = "Code")]
        public string Code { get; set; }

        [Display(Name = "Component Type")]
        public int ComponentType { get; set; } = 1;
        public string? ComponentTypeText { get; set; }

        [Display(Name = "Taxable")]
        public bool IsTaxable { get; set; }

        [Display(Name = "PF Applicable")]
        public bool IsPFApplicable { get; set; }

        [Display(Name = "ESIC Applicable")]
        public bool IsESICApplicable { get; set; }

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class SalaryComponentListDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public int ComponentType { get; set; }
        public string? ComponentTypeText { get; set; }
        public bool IsTaxable { get; set; }
        public bool IsPFApplicable { get; set; }
        public bool IsESICApplicable { get; set; }
    }

    // SalaryComponentImportRowResult / SalaryComponentImportResultDto have
    // been replaced by the generic APP.Excel.ExcelImportResult /
    // ExcelImportRowResult, now used by SalaryComponentController's Import
    // action - see APP/Excel/ExcelImportResult.cs.

    // ==============================
    // Salary Structure
    // ==============================

    public class SalaryStructureDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please select an Employee.")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }

        [Required(ErrorMessage = "Please select the Effective Date.")]
        [Display(Name = "Effective From")]
        [DataType(DataType.Date)]
        public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

        // Set only when this assignment was created by applying a
        // Salary Template (single/bulk Assign) - null for one-off
        // assignments (including legacy Excel import).
        public string? SourceTemplateId { get; set; }
        public string? SourceTemplateName { get; set; }

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }

        public List<SalaryStructureLineDto> Details { get; set; } = new();

        public decimal TotalEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
    }

    public class SalaryStructureLineDto
    {
        public string? Id { get; set; }
        public string SalaryComponentId { get; set; }
        public string? SalaryComponentName { get; set; }
        public int ComponentType { get; set; }
        public decimal Amount { get; set; }
    }

    public class SalaryStructureListDto
    {
        public string Id { get; set; }
        public string? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
        public int ComponentCount { get; set; }

        public string? SourceTemplateId { get; set; }
        public string? SourceTemplateName { get; set; }
    }

    // ==============================
    // Salary Template (reusable master)
    // ==============================

    public class SalaryTemplateDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please enter a Structure Name.")]
        [MaxLength(150)]
        [Display(Name = "Structure Name")]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please select the Effective Date.")]
        [Display(Name = "Effective From")]
        [DataType(DataType.Date)]
        public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

        [Display(Name = "Status")]
        public bool IsActive { get; set; } = true;

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }

        public List<SalaryTemplateLineDto> Details { get; set; } = new();

        public decimal TotalEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal GrossSalary { get; set; }

        public int EmployeeCount { get; set; }
    }

    public class SalaryTemplateLineDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please select a Salary Component.")]
        public string SalaryComponentId { get; set; }
        public string? SalaryComponentName { get; set; }
        public int ComponentType { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Amount cannot be negative.")]
        public decimal Amount { get; set; }

        public int CalculationType { get; set; } = 1;
    }

    public class SalaryTemplateListDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public decimal GrossSalary { get; set; }
        public int ComponentCount { get; set; }
        public int EmployeeCount { get; set; }
        public bool IsActive { get; set; }
    }

    public class SalaryTemplateAssignDto
    {
        [Required(ErrorMessage = "Please select a Salary Structure (template).")]
        public string SalaryTemplateId { get; set; }

        [Required(ErrorMessage = "Please select at least one employee.")]
        [MinLength(1, ErrorMessage = "Please select at least one employee.")]
        public List<string> EmployeeIds { get; set; } = new();

        [Required(ErrorMessage = "Please select the Effective Date.")]
        [Display(Name = "Effective From")]
        [DataType(DataType.Date)]
        public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
    }

    public class SalaryTemplateAssignResultLineDto
    {
        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class SalaryTemplateAssignResultDto
    {
        public int TotalSelected { get; set; }
        public int AppliedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<SalaryTemplateAssignResultLineDto> Results { get; set; } = new();
    }

    // ==============================
    // Payroll
    // ==============================

    public class PayrollGenerateDto
    {
        [Display(Name = "Year")]
        public int SalaryYear { get; set; } = DateTime.UtcNow.Year;

        [Display(Name = "Month")]
        public int SalaryMonth { get; set; } = DateTime.UtcNow.Month;

        [Display(Name = "Employee")]
        public string? EmployeeId { get; set; }

        public string? CompanyId { get; set; }
        public string? BranchId { get; set; }

        [Display(Name = "Prorate by Attendance")]
        public bool Prorated { get; set; } = true;

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
    }

    public class PayrollDto
    {
        public string? Id { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }

        // Payslip letterhead / employee-detail fields - see Application-side
        // PayrollDto for how these get populated; kept in sync here.
        public string? CompanyName { get; set; }
        public string? CompanyAddress { get; set; }
        public string? CompanyLogoUrl { get; set; }

        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }

        public string? PAN { get; set; }
        public string? UAN { get; set; }

        public string? BankAccountMasked { get; set; }

        public string? NetPayInWords { get; set; }

        public string? CompanyId { get; set; }
        public string? BranchId { get; set; }

        public int SalaryYear { get; set; }
        public int SalaryMonth { get; set; }
        public string? MonthName { get; set; }
        public DateTime SalaryDate { get; set; }

        public decimal GrossSalary { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }

        public decimal? TotalWorkingDays { get; set; }
        public decimal? PresentDays { get; set; }
        public decimal? LeaveDays { get; set; }
        public decimal? PaidLeaveDays { get; set; }
        public decimal? UnpaidLeaveDays { get; set; }
        public decimal? PayableDays { get; set; }
        public int? ProrationBasisUsed { get; set; }
        public string? ProrationBasisUsedName { get; set; }

        public string? Status { get; set; }

        public int RecalculatedCount { get; set; }
        public DateTime? LastRecalculatedOn { get; set; }
        public string? LastRecalculatedBy { get; set; }

        public List<PayrollLineDto> Details { get; set; } = new();
    }

    public class PayrollLineDto
    {
        public string Id { get; set; }
        public string? SalaryComponentId { get; set; }
        public string? SalaryComponentName { get; set; }
        public decimal Amount { get; set; }
        public bool IsEarning { get; set; }
    }

    public class PayrollListDto
    {
        public string Id { get; set; }
        public string? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public int SalaryYear { get; set; }
        public int SalaryMonth { get; set; }
        public string? MonthName { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal NetSalary { get; set; }
        public decimal? PresentDays { get; set; }
        public decimal? TotalWorkingDays { get; set; }
        public decimal? PayableDays { get; set; }
        public string? Status { get; set; }
        public int RecalculatedCount { get; set; }
    }

    public class PayrollGenerateResultDto
    {
        public int Generated { get; set; }
        public int Skipped { get; set; }
        public List<string> Messages { get; set; } = new();
    }

    // ==============================
    // Salary Processing (attendance-based proration) - see
    // SalaryCalculationService. PayableDays = Present + Paid Leave, capped
    // to TotalDaysInPeriod and to the employee's employed window in the
    // month. NetSalary-per-earning-line = FullAmount * PayableDays /
    // TotalDaysInPeriod (Deductions are NOT prorated - flat, unchanged from
    // the Salary Structure, matching this codebase's existing behavior;
    // there is no PF/ESI/PT/TDS calculation engine here today).
    // ==============================

    public class SalaryCalculationLineDto
    {
        public string SalaryComponentId { get; set; }
        public string? SalaryComponentName { get; set; }
        public int ComponentType { get; set; } // 1 Earning / 2 Deduction
        public decimal FullAmount { get; set; }
        public decimal ProratedAmount { get; set; }
    }

    public class SalaryCalculationResultDto
    {
        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }

        public int SalaryYear { get; set; }
        public int SalaryMonth { get; set; }

        // The basis actually used for THIS calculation (from the
        // employee's Company's active AttendancePolicy at calculation
        // time).
        public int ProrationBasisUsed { get; set; }
        public string? ProrationBasisUsedName { get; set; }
        public decimal TotalDaysInPeriod { get; set; }

        public decimal PresentDays { get; set; }
        public decimal PaidLeaveDays { get; set; }
        public decimal UnpaidLeaveDays { get; set; }
        public decimal PayableDays { get; set; }

        public decimal MonthlySalary { get; set; }     // full, unprorated gross earnings
        public decimal GrossSalary { get; set; }        // prorated earnings total
        public decimal TotalDeductions { get; set; }     // flat, unprorated
        public decimal NetSalary { get; set; }

        public List<SalaryCalculationLineDto> Lines { get; set; } = new();

        // Non-fatal notices for the Review screen - e.g. "2 duplicate
        // attendance rows on 2026-01-05 were de-duplicated", "Employee
        // joined mid-month - days before Joining Date are excluded".
        public List<string> Warnings { get; set; } = new();

        // Fatal - this employee cannot be processed (no salary structure,
        // etc.) - CanProcess is false and NetSalary/Lines are meaningless.
        public bool CanProcess { get; set; } = true;
        public string? BlockReason { get; set; }

        public bool AlreadyProcessed { get; set; }
        public string? ExistingPayrollId { get; set; }
        public string? ExistingPayrollStatus { get; set; }
    }

    public class SalaryProcessingPreviewDto
    {
        public int SalaryYear { get; set; }
        public int SalaryMonth { get; set; }
        public string? MonthName { get; set; }
        public List<SalaryCalculationResultDto> Employees { get; set; } = new();
    }

    // Bulk "Process Salary" - Review screen posts back exactly the
    // employees the user confirmed (never a bare month/company filter
    // re-evaluated blind at process time - what HR reviewed is what gets
    // processed).
    public class SalaryProcessRequestDto
    {
        [Required]
        public int SalaryYear { get; set; }

        [Required]
        public int SalaryMonth { get; set; }

        [Required(ErrorMessage = "Please select at least one employee to process.")]
        [MinLength(1, ErrorMessage = "Please select at least one employee to process.")]
        public List<string> EmployeeIds { get; set; } = new();

        public string? TenantId { get; set; }
        public string CreatedBy { get; set; }
    }

    public class SalaryRecalculateRequestDto
    {
        [Required]
        public string PayrollId { get; set; }

        // Required (server-enforced) when the target Payroll's Status is
        // "Processed" - see PayrollBusinessService.RecalculateAsync's
        // Draft/Processed/Paid rules.
        public string? Remarks { get; set; }

        public string PerformedBy { get; set; }
    }

    // ==============================
    // Payroll Dashboard
    // ==============================

    public class PayrollDashboardDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string? MonthName { get; set; }

        public decimal TotalGross { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalNet { get; set; }
        public decimal AverageNet { get; set; }
        public int EmployeesPaid { get; set; }

        public int DraftCount { get; set; }
        public int ProcessedCount { get; set; }
        public int PaidCount { get; set; }

        public int SalaryStructureCount { get; set; }
        public int SalaryComponentCount { get; set; }

        public List<PayrollTrendPointDto> MonthlyTrend { get; set; } = new();
        public List<PayrollTopEarnerDto> TopEarners { get; set; } = new();
    }

    public class PayrollTrendPointDto
    {
        public int Month { get; set; }
        public string? MonthName { get; set; }
        public decimal TotalNet { get; set; }
        public int Count { get; set; }
    }

    public class PayrollTopEarnerDto
    {
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public decimal NetSalary { get; set; }
    }
}
