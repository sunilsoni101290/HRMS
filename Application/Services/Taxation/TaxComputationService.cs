using Application.DTOs.Taxation;
using Application.Interfaces.Taxation;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Taxation
{
    // The core Income Tax / TDS calculation engine for Indian salaried
    // employees - see Domain/Entities/EmployeeTaxComputation.cs /
    // ITaxComputationService. Slab RATES are DB-driven (TaxSlab, Admin-
    // configurable per Financial Year since the Union Budget revises them
    // almost every year); the HANDFUL of numbers the Income Tax Act fixes
    // in the section text itself (Section 80C's Rs. 1,50,000 cap, Section
    // 87A's rebate thresholds, the 4% cess rate, the Rs. 50,000/75,000
    // standard deduction) are kept as named constants below, following
    // this codebase's own convention of inline business-rule constants
    // (see ProbationConfirmationService.DueForReviewLookaheadDays) -
    // update them centrally here whenever a Budget changes these figures.
    public class TaxComputationService : ITaxComputationService
    {
        private readonly ApplicationDbContext _context;

        public TaxComputationService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ---- FY2025-26 fixed figures (Income Tax Act, as amended) ----
        private const decimal StandardDeductionOldRegime = 50000m;
        private const decimal StandardDeductionNewRegime = 75000m;

        private const decimal Section80CCap = 150000m;
        private const decimal Section80CCD1BCap = 50000m;
        // Non-senior-citizen self+family cap; senior-citizen/parent higher
        // caps (Rs. 50,000) are not separately modeled in this phase.
        private const decimal Section80DCap = 25000m;
        private const decimal Section24BCap = 200000m;

        private const decimal Rebate87AOldRegimeIncomeLimit = 500000m;
        private const decimal Rebate87AOldRegimeMaxAmount = 12500m;

        private const decimal Rebate87ANewRegimeIncomeLimit = 700000m;
        private const decimal Rebate87ANewRegimeMaxAmount = 25000m;

        private const decimal CessRate = 0.04m;

        #region Compute

        public async Task<EmployeeTaxComputationDto> ComputeAsync(string employeeId, string financialYearId, string tenantId, string actingUserId)
        {
            if (!await HasPermissionAsync(actingUserId))
                throw new UnauthorizedAccessException("You are not authorized to compute Tax records.");

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == employeeId && x.TenantId == tenantId && !x.IsDeleted);

            if (employee == null)
                throw new Exception("Employee not found.");

            var financialYear = await _context.FinancialYears
                .FirstOrDefaultAsync(x => x.Id == financialYearId && x.TenantId == tenantId && !x.IsDeleted);

            if (financialYear == null)
                throw new Exception("Financial Year not found.");

            // Verified declaration only - a Draft/Submitted/Rejected
            // declaration must never influence an actual TDS figure. No
            // declaration on file defaults to New Regime with zero
            // deductions, matching the Income Tax Act's own "New Regime is
            // the default unless the employee opts out" rule.
            var declaration = await _context.TaxDeclarations
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.FinancialYearId == financialYearId &&
                    x.TenantId == tenantId &&
                    !x.IsDeleted &&
                    x.Status == TaxDeclarationStatus.Verified);

            var regime = declaration?.Regime ?? TaxRegime.New;

            var (annualGross, annualBasic, annualHra) = await ProjectAnnualSalaryAsync(employeeId, financialYear);

            decimal standardDeduction = regime == TaxRegime.Old ? StandardDeductionOldRegime : StandardDeductionNewRegime;

            decimal hraExemption = 0;
            decimal chapterVia = 0;

            if (regime == TaxRegime.Old && declaration != null)
            {
                hraExemption = ComputeHraExemption(declaration, annualBasic, annualHra);

                var capped80C = Math.Min(declaration.Section80C, Section80CCap);
                var capped80CCD1B = Math.Min(declaration.Section80CCD1B, Section80CCD1BCap);
                var capped80D = Math.Min(declaration.Section80D, Section80DCap);
                var capped24B = Math.Min(declaration.Section24B, Section24BCap);

                chapterVia = capped80C + capped80CCD1B + capped80D + capped24B + Math.Max(0, declaration.OtherDeductions);
            }

            var taxableIncome = Math.Max(0, annualGross - standardDeduction - hraExemption - chapterVia);

            var slabs = await _context.TaxSlabs
                .Where(x => x.FinancialYearId == financialYearId && x.Regime == regime && x.TenantId == tenantId && !x.IsDeleted)
                .OrderBy(x => x.MinIncome)
                .ToListAsync();

            if (!slabs.Any())
                throw new Exception($"No Tax Slabs are configured for {financialYear.Name} ({regime} Regime). Please configure Tax Slabs first.");

            var taxBeforeCess = ComputeSlabTax(taxableIncome, slabs);

            var rebate = ApplyRebate87A(taxableIncome, taxBeforeCess, regime);

            var cess = Math.Round(Math.Max(0, taxBeforeCess - rebate) * CessRate, 2);

            var annualTax = Math.Max(0, taxBeforeCess - rebate + cess);

            var tdsDeducted = await GetTdsDeductedTillDateAsync(employeeId, financialYear, tenantId);

            var remainingMonths = CountRemainingPayrollMonths(financialYear);

            var monthlyTds = remainingMonths > 0
                ? Math.Round(Math.Max(0, annualTax - tdsDeducted) / remainingMonths, 2)
                : 0;

            var entity = await _context.EmployeeTaxComputations
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.FinancialYearId == financialYearId &&
                    x.TenantId == tenantId &&
                    !x.IsDeleted);

            if (entity == null)
            {
                entity = new EmployeeTaxComputation
                {
                    Id = IDManager.GetNewId(new EmployeeTaxComputation()),
                    EmployeeId = employeeId,
                    FinancialYearId = financialYearId,
                    TenantId = tenantId,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = actingUserId
                };

                _context.EmployeeTaxComputations.Add(entity);
            }
            else
            {
                entity.ModifiedOn = DateTime.UtcNow;
                entity.ModifiedBy = actingUserId;
            }

            entity.TaxDeclarationId = declaration?.Id;
            entity.Regime = regime;
            entity.AnnualGrossSalary = annualGross;
            entity.StandardDeduction = standardDeduction;
            entity.HraExemption = hraExemption;
            entity.TotalChapterVIADeductions = chapterVia;
            entity.TaxableIncome = taxableIncome;
            entity.TaxBeforeCess = taxBeforeCess;
            entity.Rebate87A = rebate;
            entity.HealthEducationCess = cess;
            entity.AnnualTaxLiability = annualTax;
            entity.TdsDeductedTillDate = tdsDeducted;
            entity.MonthlyTdsForRemainingMonths = monthlyTds;
            entity.ComputedOn = DateTime.UtcNow;
            entity.ComputedBy = actingUserId;

            await _context.SaveChangesAsync();

            return await GetAsync(employeeId, financialYearId, tenantId, actingUserId)
                ?? throw new Exception("Tax computation failed to persist.");
        }

        public async Task<EmployeeTaxComputationDto?> GetAsync(string employeeId, string financialYearId, string tenantId, string actingUserId)
        {
            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);
            bool isOwn = !string.IsNullOrEmpty(actingEmployeeId) && actingEmployeeId == employeeId;

            if (!isOwn && !await HasPermissionAsync(actingUserId))
                throw new UnauthorizedAccessException("You are not authorized to view this Tax computation.");

            var entity = await _context.EmployeeTaxComputations
                .Include(x => x.Employee)
                .Include(x => x.FinancialYear)
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.FinancialYearId == financialYearId &&
                    x.TenantId == tenantId &&
                    !x.IsDeleted);

            return entity == null ? null : await AssembleDtoAsync(entity);
        }

        public async Task<List<EmployeeTaxComputationDto>> GetAllAsync(string tenantId, string financialYearId, string? departmentId, string? search, string actingUserId)
        {
            if (!await HasPermissionAsync(actingUserId))
                throw new UnauthorizedAccessException("You are not authorized to view Tax computations.");

            var query = _context.EmployeeTaxComputations
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.FinancialYear)
                .Where(x => x.TenantId == tenantId && x.FinancialYearId == financialYearId && !x.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(departmentId))
                query = query.Where(x => x.Employee.DepartmentId == departmentId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    (x.Employee.FirstName + " " + x.Employee.LastName).Contains(term) ||
                    x.Employee.EmployeeCode.Contains(term));
            }

            var entities = await query.OrderBy(x => x.Employee.FirstName).ToListAsync();

            var list = new List<EmployeeTaxComputationDto>();
            foreach (var x in entities)
                list.Add(await AssembleDtoAsync(x));

            return list;
        }

        // Payroll integration point - see ITaxComputationService for the
        // contract (returns 0, never throws, if nothing has been computed
        // yet, so Payroll never blocks on Taxation not being set up).
        public async Task<decimal> GetMonthlyTdsAsync(string employeeId, DateTime payPeriod, string tenantId)
        {
            var financialYear = await _context.FinancialYears
                .Where(x => x.TenantId == tenantId && !x.IsDeleted &&
                            x.StartDate.Date <= payPeriod.Date && x.EndDate.Date >= payPeriod.Date)
                .FirstOrDefaultAsync();

            if (financialYear == null)
                return 0;

            var computation = await _context.EmployeeTaxComputations
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.FinancialYearId == financialYear.Id &&
                    x.TenantId == tenantId &&
                    !x.IsDeleted);

            return computation?.MonthlyTdsForRemainingMonths ?? 0;
        }

        #endregion

        #region Calculation Helpers

        // Projects full-year gross salary from the latest SalaryStructure
        // effective on/before the Financial Year's end date - a simple x12
        // of the current monthly structure. LIMITATION: does not prorate
        // for employees who joined mid-year or whose structure changes
        // mid-year (a future enhancement could sum actual/projected
        // monthly Payroll rows instead once enough months have run).
        private async Task<(decimal AnnualGross, decimal AnnualBasic, decimal AnnualHra)> ProjectAnnualSalaryAsync(string employeeId, FinancialYear financialYear)
        {
            var structure = await _context.SalaryStructures
                .AsNoTracking()
                .Include(s => s.SalaryDetails).ThenInclude(d => d.SalaryComponent)
                .Where(s => s.EmployeeId == employeeId && !s.IsDeleted && s.EffectiveFrom <= financialYear.EndDate)
                .OrderByDescending(s => s.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (structure?.SalaryDetails == null || !structure.SalaryDetails.Any())
                return (0, 0, 0);

            decimal monthlyGross = 0, monthlyBasic = 0, monthlyHra = 0;

            foreach (var line in structure.SalaryDetails)
            {
                if (line.SalaryComponent == null || line.SalaryComponent.ComponentType != SalaryComponentType.Earning)
                    continue;

                monthlyGross += line.Amount;

                var code = line.SalaryComponent.Code?.Trim().ToUpperInvariant();
                if (code == "BASIC") monthlyBasic += line.Amount;
                else if (code == "HRA") monthlyHra += line.Amount;
            }

            return (monthlyGross * 12, monthlyBasic * 12, monthlyHra * 12);
        }

        // Standard 3-way minimum HRA exemption formula (Old Regime only):
        // least of (a) HRA actually received, (b) rent paid minus 10% of
        // Basic, (c) 50%/40% of Basic (metro/non-metro) - floored at 0.
        private static decimal ComputeHraExemption(TaxDeclaration declaration, decimal annualBasic, decimal annualHraReceived)
        {
            if (annualHraReceived <= 0 || declaration.AnnualRentPaid <= 0)
                return 0;

            var rentMinusTenPercentBasic = Math.Max(0, declaration.AnnualRentPaid - 0.10m * annualBasic);
            var percentOfBasic = (declaration.IsMetroCity ? 0.50m : 0.40m) * annualBasic;

            return Math.Max(0, Math.Min(annualHraReceived, Math.Min(rentMinusTenPercentBasic, percentOfBasic)));
        }

        // Progressive slab walk - each slab's rate applies only to the
        // portion of taxableIncome that falls within that slab's
        // [MinIncome, MaxIncome) band.
        private static decimal ComputeSlabTax(decimal taxableIncome, List<TaxSlab> slabs)
        {
            decimal tax = 0;

            foreach (var slab in slabs)
            {
                if (taxableIncome <= slab.MinIncome)
                    continue;

                var upperBound = slab.MaxIncome ?? decimal.MaxValue;
                var amountInSlab = Math.Min(taxableIncome, upperBound) - slab.MinIncome;

                if (amountInSlab > 0)
                    tax += amountInSlab * (slab.RatePercent / 100m);
            }

            return Math.Round(tax, 2);
        }

        private static decimal ApplyRebate87A(decimal taxableIncome, decimal taxBeforeCess, TaxRegime regime)
        {
            var (limit, maxRebate) = regime == TaxRegime.Old
                ? (Rebate87AOldRegimeIncomeLimit, Rebate87AOldRegimeMaxAmount)
                : (Rebate87ANewRegimeIncomeLimit, Rebate87ANewRegimeMaxAmount);

            if (taxableIncome > limit)
                return 0;

            return Math.Min(taxBeforeCess, maxRebate);
        }

        // Sums PayrollDetail amounts already withheld this Financial Year
        // against a SalaryComponent coded "TDS" - returns 0 (not an error)
        // if no such component/rows exist yet, since most tenants won't
        // have TDS wired into Payroll on day one of adopting this module.
        private async Task<decimal> GetTdsDeductedTillDateAsync(string employeeId, FinancialYear financialYear, string tenantId)
        {
            return await (
                from pd in _context.PayrollDetails
                join p in _context.Payrolls on pd.PayrollId equals p.Id
                join sc in _context.SalaryComponents on pd.SalaryComponentId equals sc.Id
                where p.EmployeeId == employeeId
                      && p.TenantId == tenantId
                      && !p.IsDeleted
                      && !pd.IsDeleted
                      && sc.Code == "TDS"
                      && p.SalaryDate >= financialYear.StartDate
                      && p.SalaryDate <= financialYear.EndDate
                select pd.Amount
            ).SumAsync();
        }

        // Number of calendar months, from the current month through the
        // Financial Year's last month (inclusive), remaining for Payroll
        // to spread the balance TDS over - at least 1, so
        // MonthlyTdsForRemainingMonths never divides by zero even when
        // computed after the FY has technically ended.
        private static int CountRemainingPayrollMonths(FinancialYear financialYear)
        {
            var today = DateTime.UtcNow.Date;
            var fyEndMonth = new DateTime(financialYear.EndDate.Year, financialYear.EndDate.Month, 1);

            var cursor = today > financialYear.StartDate
                ? new DateTime(today.Year, today.Month, 1)
                : new DateTime(financialYear.StartDate.Year, financialYear.StartDate.Month, 1);

            if (cursor > fyEndMonth)
                return 1;

            return ((fyEndMonth.Year - cursor.Year) * 12) + (fyEndMonth.Month - cursor.Month) + 1;
        }

        #endregion

        #region Authorization / Assembly Helpers

        private async Task<string?> GetActingEmployeeIdAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            return user?.EmployeeId;
        }

        // Reuses TAX_DECLARATION's Approve permission as the "may
        // compute/view all Tax Computations" gate - see
        // ITaxComputationService's header comment for why there is no
        // separate TAX_COMPUTATION service-level permission check.
        private async Task<bool> HasPermissionAsync(string? actingUserId)
        {
            if (string.IsNullOrEmpty(actingUserId))
                return false;

            return await (
                from ur in _context.UserRoles
                join rp in _context.RolePermissions.Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _context.Permissions.Where(x =>
                        x.FeatureId == AppFeatureConstants.TAX_DECLARATION && x.Action == Actions.Approve)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();
        }

        private async Task<EmployeeTaxComputationDto> AssembleDtoAsync(EmployeeTaxComputation x)
        {
            string? computedByName = null;

            if (!string.IsNullOrEmpty(x.ComputedBy))
            {
                var user = await _context.Users.Include(u => u.Employee)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == x.ComputedBy);

                computedByName = user?.Employee != null
                    ? $"{user.Employee.FirstName} {user.Employee.LastName}".Trim()
                    : user?.Username;
            }

            return new EmployeeTaxComputationDto
            {
                Id = x.Id,

                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,
                EmployeeCode = x.Employee?.EmployeeCode,
                PAN = x.Employee?.PANNumber,

                FinancialYearId = x.FinancialYearId,
                FinancialYearName = x.FinancialYear?.Name,

                TaxDeclarationId = x.TaxDeclarationId,

                Regime = (int)x.Regime,
                RegimeName = x.Regime.ToString(),

                AnnualGrossSalary = x.AnnualGrossSalary,
                StandardDeduction = x.StandardDeduction,
                HraExemption = x.HraExemption,
                TotalChapterVIADeductions = x.TotalChapterVIADeductions,
                TaxableIncome = x.TaxableIncome,

                TaxBeforeCess = x.TaxBeforeCess,
                Rebate87A = x.Rebate87A,
                HealthEducationCess = x.HealthEducationCess,
                AnnualTaxLiability = x.AnnualTaxLiability,

                TdsDeductedTillDate = x.TdsDeductedTillDate,
                MonthlyTdsForRemainingMonths = x.MonthlyTdsForRemainingMonths,

                ComputedOn = x.ComputedOn,
                ComputedBy = x.ComputedBy,
                ComputedByName = computedByName
            };
        }

        #endregion
    }
}
