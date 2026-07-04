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

        public string? Status { get; set; }

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
        public string? Status { get; set; }
    }

    public class PayrollGenerateResultDto
    {
        public int Generated { get; set; }
        public int Skipped { get; set; }
        public List<string> Messages { get; set; } = new();
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
