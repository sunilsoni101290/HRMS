using Application.DTOs.Payroll;
using Application.Interfaces;
using Application.Interfaces.LoanAdvance;
using Application.Interfaces.Payroll;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.PayrollService
{
    public class PayrollBusinessService : IPayrollBusinessService
    {
        private readonly ApplicationDbContext _context;

        // Phase 8 (Loan & Advance module) - runs AFTER each payroll is
        // persisted, see the call site inside GenerateAsync below. Kept as
        // a plain Application-layer dependency (not a circular reference -
        // both interfaces live in the same Application assembly).
        private readonly IPayrollLoanRecoveryService _loanRecoveryService;
        private readonly ILogger<PayrollBusinessService> _logger;

        // Salary Processing - see SalaryCalculationService's class remarks
        // for the root-cause writeup and the algorithm this delegates the
        // actual proration math to (GenerateAsync/ProcessAsync/
        // RecalculateAsync below no longer compute a factor themselves).
        private readonly ISalaryCalculationService _salaryCalculationService;

        // Payslip verification token signing secret - reuses the existing
        // login-JWT secret (API/appsettings.json "Jwt:Key") rather than
        // introducing a second one to manage. See
        // Domain.Helper.PayslipVerificationHelper.
        private readonly IConfiguration _configuration;

        // "Email Payslip" toolbar action - see EmailPayslipAsync below.
        private readonly IEmailSender _emailSender;

        public PayrollBusinessService(
            ApplicationDbContext context,
            IPayrollLoanRecoveryService loanRecoveryService,
            ILogger<PayrollBusinessService> logger,
            ISalaryCalculationService salaryCalculationService,
            IConfiguration configuration,
            IEmailSender emailSender)
        {
            _context = context;
            _loanRecoveryService = loanRecoveryService;
            _logger = logger;
            _salaryCalculationService = salaryCalculationService;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        #region Get All

        public async Task<List<PayrollListDto>> GetAllAsync(
            int? year = null,
            int? month = null,
            string? departmentId = null,
            string? designationId = null,
            string? employeeStatus = null,
            string? paymentStatus = null,
            string? search = null)
        {
            try
            {
            var query = _context.Payrolls
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (year.HasValue) query = query.Where(x => x.SalaryYear == year.Value);
            if (month.HasValue) query = query.Where(x => x.SalaryMonth == month.Value);
            if (!string.IsNullOrEmpty(departmentId)) query = query.Where(x => x.Employee != null && x.Employee.DepartmentId == departmentId);
            if (!string.IsNullOrEmpty(designationId)) query = query.Where(x => x.Employee != null && x.Employee.DesignationId == designationId);
            if (!string.IsNullOrEmpty(paymentStatus)) query = query.Where(x => x.Status == paymentStatus);

            // "active" / "inactive" - matches Employee.IsActive (BaseEntity),
            // never a recalculated or invented status.
            if (string.Equals(employeeStatus, "active", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => x.Employee != null && x.Employee.IsActive);
            else if (string.Equals(employeeStatus, "inactive", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => x.Employee != null && !x.Employee.IsActive);

            if (!string.IsNullOrEmpty(search))
            {
                var s = search.Trim();
                query = query.Where(x =>
                    (x.Employee != null && x.Employee.EmployeeCode != null && x.Employee.EmployeeCode.Contains(s)) ||
                    (x.Employee != null && (x.Employee.FirstName + " " + x.Employee.LastName).Contains(s)));
            }

            var list = await query
                .OrderByDescending(x => x.SalaryYear).ThenByDescending(x => x.SalaryMonth)
                .Select(x => new PayrollListDto
                {
                    Id = x.Id,
                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee != null ? x.Employee.FirstName + " " + x.Employee.LastName : "",
                    EmployeeCode = x.Employee != null ? x.Employee.EmployeeCode : "",
                    DepartmentName = x.Employee != null && x.Employee.Department != null ? x.Employee.Department.Name : null,
                    DesignationName = x.Employee != null && x.Employee.Designation != null ? x.Employee.Designation.Name : null,
                    SalaryYear = x.SalaryYear,
                    SalaryMonth = x.SalaryMonth,
                    GrossSalary = x.GrossSalary,
                    Deductions = x.TotalDeductions,
                    NetSalary = x.NetSalary,
                    PresentDays = x.PresentDays,
                    TotalWorkingDays = x.TotalWorkingDays,
                    Status = x.Status
                })
                .ToListAsync();

            foreach (var item in list)
                item.MonthName = MonthName(item.SalaryMonth);

            // Basic / Allowances split - one grouped query across all fetched
            // payroll ids (never per-row), reusing the same "Code == BASIC"
            // convention already used in TaxComputationService.
            if (list.Count > 0)
            {
                var payrollIds = list.Select(x => x.Id).ToList();
                var details = await _context.PayrollDetails
                    .AsNoTracking()
                    .Where(d => payrollIds.Contains(d.PayrollId) && d.IsEarning)
                    .Select(d => new { d.PayrollId, Code = d.SalaryComponent != null ? d.SalaryComponent.Code : null, d.Amount })
                    .ToListAsync();

                var byPayroll = details.GroupBy(d => d.PayrollId).ToDictionary(g => g.Key, g => g.ToList());
                foreach (var item in list)
                {
                    if (!byPayroll.TryGetValue(item.Id, out var rows)) continue;
                    item.BasicSalary = rows.Where(r => r.Code == "BASIC").Sum(r => r.Amount);
                    item.Allowances = rows.Where(r => r.Code != "BASIC").Sum(r => r.Amount);
                }
            }

            return list;
            }
            catch (Exception)
            {
                return new List<PayrollListDto>();
            }
        }

        #endregion

        #region Get By Id

        public async Task<PayrollDto> GetByIdAsync(string id)
        {
            try
            {
            var entity = await _context.Payrolls
                .AsNoTracking()
                .Include(x => x.Employee).ThenInclude(e => e.Company).ThenInclude(c => c.City)
                .Include(x => x.Employee).ThenInclude(e => e.Company).ThenInclude(c => c.State)
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.Designation)
                .Include(x => x.PayrollDetails)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return null;

            return await MapDetailAsync(entity);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<PayrollDto> MapDetailAsync(Payroll entity)
        {
            var componentNames = await _context.SalaryComponents
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Id, c => c.Name);

            // Payslip letterhead / employee-detail fields - PF and Bank
            // records aren't navigation properties off Employee, so they're
            // looked up directly. Neither is guaranteed to exist for every
            // employee, so both are deliberately optional (null-safe below)
            // rather than treated as an error - a payslip must still render.
            var company = entity.Employee?.Company;

            var pfDetail = await _context.EmployeePFDetails
                .AsNoTracking()
                .Where(x => x.EmployeeId == entity.EmployeeId && !x.IsDeleted)
                .OrderByDescending(x => x.PFJoiningDate)
                .FirstOrDefaultAsync();

            var bankDetail = await _context.EmployeeBankDetails
                .AsNoTracking()
                .Where(x => x.EmployeeId == entity.EmployeeId && !x.IsDeleted)
                .OrderByDescending(x => x.IsPrimary)
                .FirstOrDefaultAsync();

            var dto = new PayrollDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                EmployeeName = entity.Employee != null ? entity.Employee.FirstName + " " + entity.Employee.LastName : "",
                EmployeeCode = entity.Employee != null ? entity.Employee.EmployeeCode : "",
                EmployeePhotoUrl = entity.Employee?.FilePath,

                CompanyName = company?.Name,
                CompanyAddress = BuildCompanyAddress(company),
                CompanyLogoUrl = company?.Logo,

                DepartmentName = entity.Employee?.Department?.Name,
                DesignationName = entity.Employee?.Designation?.Name,

                PAN = entity.Employee?.PANNumber,
                UAN = pfDetail?.UANNumber,
                BankAccountMasked = MaskAccountNumber(bankDetail?.AccountNumber),

                CompanyId = entity.CompanyId,
                BranchId = entity.BranchId,
                SalaryYear = entity.SalaryYear,
                SalaryMonth = entity.SalaryMonth,
                MonthName = MonthName(entity.SalaryMonth),
                SalaryDate = entity.SalaryDate,
                GrossSalary = entity.GrossSalary,
                TotalEarnings = entity.TotalEarnings,
                TotalDeductions = entity.TotalDeductions,
                NetSalary = entity.NetSalary,
                NetPayInWords = NumberToWordsHelper.ToRupeesInWords(entity.NetSalary),
                TotalWorkingDays = entity.TotalWorkingDays,
                PresentDays = entity.PresentDays,
                LeaveDays = entity.LeaveDays,
                Status = entity.Status,
                Details = (entity.PayrollDetails ?? new List<PayrollDetail>())
                    .Select(d => new PayrollLineDto
                    {
                        Id = d.Id,
                        SalaryComponentId = d.SalaryComponentId,
                        SalaryComponentName = d.SalaryComponentId != null && componentNames.ContainsKey(d.SalaryComponentId)
                            ? componentNames[d.SalaryComponentId] : "",
                        Amount = d.Amount,
                        IsEarning = d.IsEarning
                    })
                    .OrderByDescending(d => d.IsEarning)
                    .ToList()
            };

            return dto;
        }

        // "123 Business Park, Mumbai, Maharashtra" - Address + City + State,
        // matching the reference payslip's letterhead format. Any missing
        // piece is simply omitted rather than leaving a stray ", ,".
        private static string? BuildCompanyAddress(Company? company)
        {
            if (company == null)
                return null;

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(company.Address)) parts.Add(company.Address.Trim());
            if (!string.IsNullOrWhiteSpace(company.City?.Name)) parts.Add(company.City.Name.Trim());
            if (!string.IsNullOrWhiteSpace(company.State?.Name)) parts.Add(company.State.Name.Trim());

            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        // "1234567890123489" -> "XXXXXXXXXXXX3489" - only the last 4 digits
        // are ever shown on a payslip, matching the reference design.
        private static string? MaskAccountNumber(string? accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                return null;

            var trimmed = accountNumber.Trim();

            if (trimmed.Length <= 4)
                return trimmed;

            var lastFour = trimmed[^4..];
            return new string('X', trimmed.Length - 4) + lastFour;
        }

        #endregion

        #region Generate (Attendance-Prorated)

        public async Task<PayrollGenerateResultDto> GenerateAsync(PayrollGenerateDto dto)
        {
            try
            {
            var result = new PayrollGenerateResultDto();

            var periodEnd = new DateTime(dto.SalaryYear, dto.SalaryMonth,
                DateTime.DaysInMonth(dto.SalaryYear, dto.SalaryMonth));

            // Target employees (must have a structure effective by period end)
            var structureQuery = _context.SalaryStructures
                .AsNoTracking()
                .Where(s => !s.IsDeleted && s.EffectiveFrom <= periodEnd);

            if (!string.IsNullOrEmpty(dto.EmployeeId))
                structureQuery = structureQuery.Where(s => s.EmployeeId == dto.EmployeeId);

            var employeeIds = await structureQuery
                .Select(s => s.EmployeeId)
                .Distinct()
                .ToListAsync();

            if (!employeeIds.Any())
            {
                result.Messages.Add("No employees with a salary structure were found for this period.");
                return result;
            }

            // Collected so the Loan & Advance payroll-recovery hook below
            // can run against real, already-persisted Payroll.Id values -
            // see the SaveChangesAsync + recovery loop after this foreach.
            var generatedPayrolls = new List<Payroll>();

            foreach (var employeeId in employeeIds)
            {
                var employee = await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted);

                if (employee == null)
                {
                    result.Skipped++;
                    continue;
                }

                // Optional company/branch filter
                if (!string.IsNullOrEmpty(dto.CompanyId) && employee.CompanyId != dto.CompanyId)
                {
                    result.Skipped++;
                    continue;
                }
                if (!string.IsNullOrEmpty(dto.BranchId) && employee.BranchId != dto.BranchId)
                {
                    result.Skipped++;
                    continue;
                }

                // Skip duplicates
                var exists = await _context.Payrolls.AnyAsync(p =>
                    p.EmployeeId == employeeId &&
                    p.SalaryYear == dto.SalaryYear &&
                    p.SalaryMonth == dto.SalaryMonth &&
                    !p.IsDeleted);

                if (exists)
                {
                    result.Skipped++;
                    result.Messages.Add($"{employee.FirstName} {employee.LastName}: payroll already exists for this month.");
                    continue;
                }

                // Delegates the actual proration math to
                // SalaryCalculationService - see its class remarks for the
                // root-cause writeup this replaces (no more ad-hoc
                // "present attendance rows / working attendance rows"
                // ratio here).
                var calc = await _salaryCalculationService.CalculateAsync(employeeId, dto.SalaryYear, dto.SalaryMonth, dto.TenantId);

                if (calc == null || !calc.CanProcess)
                {
                    result.Skipped++;
                    result.Messages.Add($"{employee.FirstName} {employee.LastName}: {calc?.BlockReason ?? "could not be calculated."}");
                    continue;
                }

                var payroll = BuildPayrollFromCalculation(calc, employee, dto.Prorated, dto.TenantId, dto.CreatedBy);

                await _context.Payrolls.AddAsync(payroll);
                generatedPayrolls.Add(payroll);
                result.Generated++;
            }

            await _context.SaveChangesAsync();

            // Loan & Advance payroll recovery (Phase 8) - runs per employee
            // AFTER payrolls are committed, so LoanEmiSchedule/
            // AdvanceInstallment rows can reference a real Payroll.Id. Each
            // employee's recovery is its own try/catch: a failure here must
            // never undo or block payroll generation itself, and one
            // employee's failure must never stop another's - see
            // IPayrollLoanRecoveryService for the idempotent, re-runnable
            // design (safe to call again if this step needs a retry).
            foreach (var payroll in generatedPayrolls)
            {
                try
                {
                    var recovery = await _loanRecoveryService.RecoverForPayrollAsync(payroll.Id, dto.TenantId, dto.CreatedBy);

                    if (recovery.TotalRecovered > 0 || recovery.InstallmentsSkipped > 0)
                        result.Messages.AddRange(recovery.Messages);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Loan/Advance payroll recovery failed for Payroll {PayrollId} (Employee {EmployeeId}).", payroll.Id, payroll.EmployeeId);
                    result.Messages.Add($"{payroll.EmployeeId}: loan/advance recovery could not be processed automatically - it can be re-run manually.");
                }
            }

            result.Messages.Insert(0, $"Generated {result.Generated} payroll(s), skipped {result.Skipped}.");
            return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Builds an (unsaved) Payroll + PayrollDetails from a
        // SalaryCalculationResultDto. When prorated is false, earnings are
        // paid in full (MonthlySalary/each line's FullAmount) regardless of
        // PayableDays - PayableDays/PresentDays/etc are still recorded for
        // information, matching the pre-existing "Prorate earnings by
        // attendance" checkbox's behavior.
        private static Payroll BuildPayrollFromCalculation(SalaryCalculationResultDto calc, Employee employee, bool prorated, string tenantId, string createdBy)
        {
            var periodEnd = new DateTime(calc.SalaryYear, calc.SalaryMonth, DateTime.DaysInMonth(calc.SalaryYear, calc.SalaryMonth));

            var payroll = new Payroll
            {
                Id = IDManager.GetNewId(new Payroll()),
                EmployeeId = calc.EmployeeId,
                CompanyId = employee.CompanyId,
                BranchId = employee.BranchId,
                SalaryYear = calc.SalaryYear,
                SalaryMonth = calc.SalaryMonth,
                SalaryDate = periodEnd,
                TotalWorkingDays = calc.TotalDaysInPeriod,
                PresentDays = calc.PresentDays,
                LeaveDays = calc.UnpaidLeaveDays, // backward-compatible: "non-payable leave" bucket
                PaidLeaveDays = calc.PaidLeaveDays,
                UnpaidLeaveDays = calc.UnpaidLeaveDays,
                PayableDays = prorated ? calc.PayableDays : calc.TotalDaysInPeriod,
                ProrationBasisUsed = (SalaryProrationBasis)calc.ProrationBasisUsed,
                Status = "Draft",
                TenantId = tenantId,
                CreatedBy = createdBy,
                PayrollDetails = new List<PayrollDetail>()
            };

            decimal grossSalary = 0, totalEarnings = 0, totalDeductions = 0;

            foreach (var line in calc.Lines)
            {
                var isEarning = line.ComponentType == (int)SalaryComponentType.Earning;

                decimal amount;
                if (isEarning)
                {
                    grossSalary += line.FullAmount;
                    amount = prorated ? line.ProratedAmount : line.FullAmount;
                    totalEarnings += amount;
                }
                else
                {
                    amount = line.FullAmount;
                    totalDeductions += amount;
                }

                payroll.PayrollDetails.Add(new PayrollDetail
                {
                    Id = IDManager.GetNewId(new PayrollDetail()),
                    PayrollId = payroll.Id,
                    SalaryComponentId = line.SalaryComponentId,
                    Amount = amount,
                    IsEarning = isEarning,
                    TenantId = tenantId,
                    CreatedBy = createdBy
                });
            }

            payroll.GrossSalary = grossSalary;
            payroll.TotalEarnings = totalEarnings;
            payroll.TotalDeductions = totalDeductions;
            payroll.NetSalary = totalEarnings - totalDeductions;

            return payroll;
        }

        #endregion

        #region Salary Processing (Preview / Process / Recalculate)

        // Step "Load Attendance" / "Review" - read-only, writes nothing.
        // Resolves the same employee set GenerateAsync would (must have a
        // salary structure effective by period end), runs
        // SalaryCalculationService against all of them, and returns the
        // full breakdown for the Review table.
        //
        // NOTE (audit finding): an OLDER version of this method used to sit
        // here, commented out, that resolved employees by starting from
        // SalaryStructures and only filtering by Company/Branch afterwards,
        // with TenantId accepted but never applied anywhere. That dead code
        // has been removed - it was not what actually ran. The method below
        // is (and was, before this change) the one Preview() in
        // API/Controllers/PayrollController.cs actually calls; it already
        // started from Employees and joined to SalaryStructures. What this
        // change adds on top:
        //   1. EmployeesMatchingFilterCount is now returned so the Generate
        //      screen can tell "no employees at all for this company/branch"
        //      apart from "employees exist here, but none of them have a
        //      salary structure effective for this period" - see
        //      Generate.cshtml. Before this, both cases showed the exact
        //      same hardcoded client-side message regardless of which was
        //      actually true.
        //   2. Structured logging for troubleshooting future "0 employees"
        //      reports without needing a debugger.
        //
        // TenantId is DELIBERATELY NOT used as a hard filter on Employees or
        // SalaryStructures below, even though it's accepted as a parameter
        // and even though the entities do carry a TenantId column. A first
        // version of this fix added `e.TenantId == tenantId` /
        // `structure.TenantId == tenantId` here, and it broke Load
        // Attendance outright - "All Companies" returned 0 employees for
        // every month, because the session's active TenantId
        // (SessionHelper.GetActiveTenantId, empty string when the logged-in
        // user's TenantId is null) doesn't reliably equal every Employee's
        // stored TenantId in this deployment's actual data. Checking the
        // rest of this codebase confirms that's not an oversight to "fix"
        // here: EmployeeService.GetAllAsync() - the query behind the actual
        // Employee Management list - takes no tenantId parameter at all and
        // never filters by it either. Matching that established, working
        // convention (rather than introducing a stricter rule only in this
        // one method) is what keeps Load Attendance finding the same
        // employees Employee Management already shows.
        public async Task<SalaryProcessingPreviewDto> PreviewAsync(
            int salaryYear,
            int salaryMonth,
            string? companyId,
            string? branchId,
            string tenantId)
        {
            var periodEnd = new DateTime(
                salaryYear,
                salaryMonth,
                DateTime.DaysInMonth(salaryYear, salaryMonth));

            // ---------------------------------------------------------
            // 1. Employees matching Tenant/Company/Branch - independent of
            //    whether they have a salary structure yet. Kept as its own
            //    count (not just the post-join list) purely so the
            //    empty-state message below can distinguish "no employees
            //    for this org filter" from "employees exist, no applicable
            //    salary structure" - see EmployeesMatchingFilterCount above.
            // ---------------------------------------------------------

            var employeeQuery = _context.Employees
                .AsNoTracking()
                .Where(e => !e.IsDeleted);

            // No TenantId filter here - see the class remarks above this
            // method for why (matches EmployeeService.GetAllAsync's
            // established behavior; a literal TenantId match here was tried
            // and caused a regression).

            // Company filter
            if (!string.IsNullOrWhiteSpace(companyId))
            {
                employeeQuery = employeeQuery
                    .Where(e => e.CompanyId == companyId);
            }

            // Branch filter
            if (!string.IsNullOrWhiteSpace(branchId))
            {
                employeeQuery = employeeQuery
                    .Where(e => e.BranchId == branchId);
            }

            var matchingEmployeeCount = await employeeQuery.CountAsync();

            // ---------------------------------------------------------
            // 2. Of those, who has a salary structure applicable to this
            //    period. SalaryStructure (Domain/Entities/SalaryStructure.cs)
            //    has no EffectiveTo column, so "applicable" is simply
            //    EffectiveFrom <= periodEnd - there is no upper bound to
            //    add without a schema change, which is out of scope here.
            //    When an employee has more than one structure effective by
            //    then, SalaryCalculationService.CalculateBatchAsync already
            //    picks the single latest one (OrderByDescending(EffectiveFrom)
            //    .FirstOrDefault()) for the actual calculation - this join
            //    only needs to know THAT one exists, not which.
            // ---------------------------------------------------------

            var employeeIds = await (
                from employee in employeeQuery

                join structure in _context.SalaryStructures.AsNoTracking()
                    on employee.Id equals structure.EmployeeId

                where
                    !structure.IsDeleted
                    && structure.EffectiveFrom <= periodEnd
                    // No TenantId filter here either, for the same reason
                    // as the Employees query above.

                select employee.Id
            )
            .Distinct()
            .ToListAsync();

            // ---------------------------------------------------------
            // 3. Calculate payroll preview for the eligible employees.
            //    CalculateBatchAsync (SalaryCalculationService.cs) always
            //    returns exactly one result per input id - including a
            //    CanProcess:false / BlockReason row for anyone it can't
            //    actually process (missing structure lines, outside their
            //    employment window, etc). So Employees.Count here can only
            //    be 0 when employeeIds itself was empty; per-employee
            //    "can't process this one" reasons already surface as row
            //    notes on the Review table (Generate.cshtml), never as the
            //    blanket empty-state message.
            // ---------------------------------------------------------

            var calculations = new List<SalaryCalculationResultDto>();

            if (employeeIds.Count > 0)
            {
                calculations = await _salaryCalculationService
                    .CalculateBatchAsync(
                        employeeIds,
                        salaryYear,
                        salaryMonth,
                        tenantId);
            }

            _logger.LogInformation(
                "Payroll Preview: Year={SalaryYear} Month={SalaryMonth} CompanyId={CompanyId} BranchId={BranchId} TenantId={TenantId} " +
                "MatchingEmployeeCount={MatchingEmployeeCount} EligibleWithSalaryStructureCount={EligibleCount} CalculationResultCount={ResultCount}",
                salaryYear, salaryMonth, companyId, branchId, tenantId,
                matchingEmployeeCount, employeeIds.Count, calculations.Count);

            // ---------------------------------------------------------
            // 4. Return preview
            // ---------------------------------------------------------

            return new SalaryProcessingPreviewDto
            {
                SalaryYear = salaryYear,
                SalaryMonth = salaryMonth,
                MonthName = MonthName(salaryMonth),
                EmployeesMatchingFilterCount = matchingEmployeeCount,

                Employees = calculations
                    .OrderBy(c => c.EmployeeName)
                    .ToList()
            };
        }

        // Step "Process Salary" - writes exactly the employees the Review
        // screen confirmed (dto.EmployeeIds), never a blind re-filter.
        // Employees who already have a Payroll for this month are skipped
        // (same "no overwrite" guard GenerateAsync always had) - use
        // RecalculateAsync to revise an existing one instead.
        public async Task<PayrollGenerateResultDto> ProcessAsync(SalaryProcessRequestDto dto)
        {
            var result = new PayrollGenerateResultDto();

            if (dto.EmployeeIds == null || !dto.EmployeeIds.Any())
            {
                result.Messages.Add("No employees were selected to process.");
                return result;
            }

            var employeeIds = dto.EmployeeIds.Distinct().ToList();

            var employees = await _context.Employees
                .AsNoTracking()
                .Where(e => employeeIds.Contains(e.Id) && !e.IsDeleted)
                .ToDictionaryAsync(e => e.Id, e => e);

            var existingIds = (await _context.Payrolls
                .AsNoTracking()
                .Where(p => employeeIds.Contains(p.EmployeeId)
                    && p.SalaryYear == dto.SalaryYear && p.SalaryMonth == dto.SalaryMonth && !p.IsDeleted)
                .Select(p => p.EmployeeId)
                .ToListAsync())
                .ToHashSet();

            var calculations = await _salaryCalculationService.CalculateBatchAsync(employeeIds, dto.SalaryYear, dto.SalaryMonth, dto.TenantId);
            var generatedPayrolls = new List<Payroll>();

            foreach (var calc in calculations)
            {
                var employeeLabel = calc.EmployeeName ?? calc.EmployeeId;

                if (!employees.TryGetValue(calc.EmployeeId, out var employee))
                {
                    result.Skipped++;
                    result.Messages.Add($"{employeeLabel}: employee not found.");
                    continue;
                }

                if (existingIds.Contains(calc.EmployeeId))
                {
                    result.Skipped++;
                    result.Messages.Add($"{employeeLabel}: payroll already exists for this month.");
                    continue;
                }

                if (!calc.CanProcess)
                {
                    result.Skipped++;
                    result.Messages.Add($"{employeeLabel}: {calc.BlockReason}");
                    continue;
                }

                var payroll = BuildPayrollFromCalculation(calc, employee, prorated: true, dto.TenantId, dto.CreatedBy);

                await _context.Payrolls.AddAsync(payroll);

                await _context.PayrollAuditLogs.AddAsync(new PayrollAuditLog
                {
                    Id = IDManager.GetNewId(new PayrollAuditLog()),
                    PayrollId = payroll.Id,
                    Action = "Generated",
                    NewNetSalary = payroll.NetSalary,
                    NewPayableDays = payroll.PayableDays,
                    PerformedBy = dto.CreatedBy,
                    TenantId = dto.TenantId,
                    CreatedBy = dto.CreatedBy
                });

                generatedPayrolls.Add(payroll);
                result.Generated++;
            }

            await _context.SaveChangesAsync();

            // Same Loan & Advance recovery hook GenerateAsync runs - see its
            // remarks for why this must run per-payroll, after SaveChanges,
            // each in its own try/catch.
            foreach (var payroll in generatedPayrolls)
            {
                try
                {
                    var recovery = await _loanRecoveryService.RecoverForPayrollAsync(payroll.Id, dto.TenantId, dto.CreatedBy);

                    if (recovery.TotalRecovered > 0 || recovery.InstallmentsSkipped > 0)
                        result.Messages.AddRange(recovery.Messages);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Loan/Advance payroll recovery failed for Payroll {PayrollId} (Employee {EmployeeId}).", payroll.Id, payroll.EmployeeId);
                    result.Messages.Add($"{payroll.EmployeeId}: loan/advance recovery could not be processed automatically - it can be re-run manually.");
                }
            }

            result.Messages.Insert(0, $"Processed {result.Generated} payroll(s), skipped {result.Skipped}.");
            return result;
        }

        // Recalculates an EXISTING Payroll against current attendance/leave
        // data - lock rules (confirmed):
        //   Draft      -> free, no confirmation needed.
        //   Processed  -> allowed, but dto.Remarks is REQUIRED (why this
        //                 already-processed payroll is being revised - a
        //                 payslip may already have been shared).
        //   Paid       -> refused outright. A Paid payroll is never edited
        //                 in place - it stays as the historical record of
        //                 what was actually paid (data integrity / audit).
        public async Task<string> RecalculateAsync(SalaryRecalculateRequestDto dto)
        {
            try
            {
                var payroll = await _context.Payrolls
                    .Include(p => p.PayrollDetails)
                    .FirstOrDefaultAsync(p => p.Id == dto.PayrollId && !p.IsDeleted);

                if (payroll == null)
                    return "ERROR:Payroll not found.";

                if (string.Equals(payroll.Status, "Paid", StringComparison.OrdinalIgnoreCase))
                    return "ERROR:This payroll is marked Paid and cannot be recalculated. Its numbers are the historical record of what was actually paid.";

                if (string.Equals(payroll.Status, "Processed", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(dto.Remarks))
                    return "ERROR:This payroll is already Processed - please provide a reason (Remarks) to recalculate it.";

                var employee = await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == payroll.EmployeeId && !e.IsDeleted);

                if (employee == null)
                    return "ERROR:Employee not found.";

                var calc = await _salaryCalculationService.CalculateAsync(payroll.EmployeeId, payroll.SalaryYear, payroll.SalaryMonth, payroll.TenantId);

                if (calc == null || !calc.CanProcess)
                    return $"ERROR:{calc?.BlockReason ?? "Could not be recalculated."}";

                var oldNetSalary = payroll.NetSalary;
                var oldPayableDays = payroll.PayableDays;

                var rebuilt = BuildPayrollFromCalculation(calc, employee, prorated: true, payroll.TenantId, payroll.CreatedBy);

                // Replace detail lines - same "delete then re-add" pattern
                // this codebase already uses for Salary Structure updates.
                _context.PayrollDetails.RemoveRange(payroll.PayrollDetails);

                foreach (var line in rebuilt.PayrollDetails)
                {
                    line.PayrollId = payroll.Id;
                    await _context.PayrollDetails.AddAsync(line);
                }

                payroll.GrossSalary = rebuilt.GrossSalary;
                payroll.TotalEarnings = rebuilt.TotalEarnings;
                payroll.TotalDeductions = rebuilt.TotalDeductions;
                payroll.NetSalary = rebuilt.NetSalary;
                payroll.TotalWorkingDays = rebuilt.TotalWorkingDays;
                payroll.PresentDays = rebuilt.PresentDays;
                payroll.LeaveDays = rebuilt.LeaveDays;
                payroll.PaidLeaveDays = rebuilt.PaidLeaveDays;
                payroll.UnpaidLeaveDays = rebuilt.UnpaidLeaveDays;
                payroll.PayableDays = rebuilt.PayableDays;
                payroll.ProrationBasisUsed = rebuilt.ProrationBasisUsed;

                payroll.RecalculatedCount += 1;
                payroll.LastRecalculatedOn = DateTime.UtcNow;
                payroll.LastRecalculatedBy = dto.PerformedBy;
                payroll.ModifiedOn = DateTime.UtcNow;
                payroll.ModifiedBy = dto.PerformedBy;

                _context.Payrolls.Update(payroll);

                await _context.PayrollAuditLogs.AddAsync(new PayrollAuditLog
                {
                    Id = IDManager.GetNewId(new PayrollAuditLog()),
                    PayrollId = payroll.Id,
                    Action = "Recalculated",
                    OldNetSalary = oldNetSalary,
                    NewNetSalary = payroll.NetSalary,
                    OldPayableDays = oldPayableDays,
                    NewPayableDays = payroll.PayableDays,
                    PerformedBy = dto.PerformedBy,
                    Remarks = dto.Remarks,
                    TenantId = payroll.TenantId,
                    CreatedBy = dto.PerformedBy
                });

                await _context.SaveChangesAsync();

                return payroll.Id;
            }
            catch (Exception ex)
            {
                return $"ERROR:{ex.Message}";
            }
        }

        #endregion

        #region Status Workflow

        public async Task<string> ChangeStatusAsync(string id, string status, string userId)
        {
            try
            {
            var payroll = await _context.Payrolls
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (payroll == null)
                return "Payroll Not Found";

            payroll.Status = status; // Draft / Processed / Paid
            payroll.ModifiedBy = userId;
            payroll.ModifiedOn = DateTime.UtcNow;

            _context.Payrolls.Update(payroll);
            await _context.SaveChangesAsync();

            return payroll.Id;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        #endregion

        #region Delete (Soft)

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
            var payroll = await _context.Payrolls
                .Include(x => x.PayrollDetails)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (payroll == null)
                return false;

            payroll.IsDeleted = true;
            payroll.ModifiedOn = DateTime.UtcNow;

            _context.Payrolls.Update(payroll);
            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Payslip

        public async Task<string> GeneratePayslipAsync(string payrollId, string userId)
        {
            try
            {
            var payroll = await _context.Payrolls
                .FirstOrDefaultAsync(x => x.Id == payrollId && !x.IsDeleted);

            if (payroll == null)
                return "Payroll Not Found";

            var existing = await _context.Payslips
                .FirstOrDefaultAsync(x => x.PayrollId == payrollId && !x.IsDeleted);

            if (existing != null)
                return existing.Id;

            var payslip = new Payslip
            {
                Id = IDManager.GetNewId(new Payslip()),
                PayrollId = payrollId,
                GeneratedDate = DateTime.UtcNow,
                TenantId = payroll.TenantId,
                CreatedBy = userId
            };

            await _context.Payslips.AddAsync(payslip);
            await _context.SaveChangesAsync();

            return payslip.Id;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        public async Task<PayrollDto> GetPayslipAsync(string payrollId)
        {
            try
            {
            return await GetByIdAsync(payrollId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Redesigned payslip document - see PayslipDto's class remarks.
        // Tenant-scoped (defense in depth - GetByIdAsync/GetPayslipAsync
        // above intentionally aren't touched, to avoid changing behavior
        // any other caller relies on).
        public async Task<PayslipDto?> GetPayslipDocumentAsync(string payrollId, string tenantId)
        {
            try
            {
                var entity = await _context.Payrolls
                    .AsNoTracking()
                    .Include(x => x.Employee).ThenInclude(e => e.Company).ThenInclude(c => c.City)
                    .Include(x => x.Employee).ThenInclude(e => e.Company).ThenInclude(c => c.State)
                    .Include(x => x.Employee).ThenInclude(e => e.Department)
                    .Include(x => x.Employee).ThenInclude(e => e.Designation)
                    .Include(x => x.Employee).ThenInclude(e => e.Branch)
                    .Include(x => x.PayrollDetails).ThenInclude(d => d.SalaryComponent)
                    .FirstOrDefaultAsync(x => x.Id == payrollId && !x.IsDeleted);

                if (entity == null)
                    return null;

                // Tenant isolation - never let one tenant's admin reach
                // another tenant's payslip by guessing/incrementing a
                // payroll id.
                if (string.IsNullOrEmpty(tenantId) || entity.TenantId != tenantId)
                    return null;

                var employee = entity.Employee;
                var company = employee?.Company;

                var pfDetail = await _context.EmployeePFDetails
                    .AsNoTracking()
                    .Where(x => x.EmployeeId == entity.EmployeeId && !x.IsDeleted)
                    .OrderByDescending(x => x.PFJoiningDate)
                    .FirstOrDefaultAsync();

                var bankDetail = await _context.EmployeeBankDetails
                    .AsNoTracking()
                    .Where(x => x.EmployeeId == entity.EmployeeId && !x.IsDeleted)
                    .OrderByDescending(x => x.IsPrimary)
                    .FirstOrDefaultAsync();

                // Financial Year for this payslip's Company - drives both
                // the YTD panel and the Tax Regime lookup. Null when Master
                // -> Financial Year hasn't been configured for this
                // Company yet (common on a fresh tenant) - both panels
                // degrade gracefully rather than guessing Apr-Mar.
                var financialYear = await _context.FinancialYears
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted
                        && x.CompanyId == entity.CompanyId
                        && x.StartDate.Date <= entity.SalaryDate.Date
                        && x.EndDate.Date >= entity.SalaryDate.Date)
                    .FirstOrDefaultAsync();

                PayslipYtdSummaryDto? ytd = null;
                if (financialYear != null)
                {
                    var ytdTotals = await _context.Payrolls
                        .AsNoTracking()
                        .Where(x => !x.IsDeleted
                            && x.EmployeeId == entity.EmployeeId
                            && x.SalaryDate.Date >= financialYear.StartDate.Date
                            && x.SalaryDate.Date <= entity.SalaryDate.Date)
                        .GroupBy(x => 1)
                        .Select(g => new
                        {
                            Earnings = g.Sum(x => x.TotalEarnings),
                            Deductions = g.Sum(x => x.TotalDeductions),
                            Net = g.Sum(x => x.NetSalary)
                        })
                        .FirstOrDefaultAsync();

                    ytd = new PayslipYtdSummaryDto
                    {
                        PeriodLabel = $"{financialYear.StartDate:MMM yyyy} - {entity.SalaryDate:MMM yyyy}",
                        TotalEarnings = ytdTotals?.Earnings ?? 0m,
                        TotalDeductions = ytdTotals?.Deductions ?? 0m,
                        NetSalary = ytdTotals?.Net ?? 0m
                    };
                }

                string? taxRegimeName = null;
                if (financialYear != null)
                {
                    var taxComputation = await _context.EmployeeTaxComputations
                        .AsNoTracking()
                        .Where(x => !x.IsDeleted
                            && x.EmployeeId == entity.EmployeeId
                            && x.FinancialYearId == financialYear.Id)
                        .FirstOrDefaultAsync();

                    taxRegimeName = taxComputation?.Regime.ToString();
                }

                // Attendance day-type counts for this payroll's month - a
                // read-only breakdown of the SAME Attendance rows Salary
                // Processing already reads (see SalaryCalculationService),
                // never a second independent calculation of payable/paid
                // days (those stay exactly as Payroll persisted them).
                var monthStart = new DateTime(entity.SalaryYear, entity.SalaryMonth, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var attendanceCounts = await _context.Attendances
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted
                        && x.EmployeeId == entity.EmployeeId
                        && x.Date >= monthStart && x.Date <= monthEnd)
                    .GroupBy(x => x.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                decimal? CountOf(AttendanceStatus status)
                {
                    var match = attendanceCounts.FirstOrDefault(x => x.Status == status);
                    return match != null ? match.Count : (decimal?)null;
                }

                // Leave Balance - current year, this employee. Hidden by
                // the view entirely when empty.
                var leaveBalances = await _context.LeaveBalances
                    .AsNoTracking()
                    .Include(x => x.LeaveType)
                    .Where(x => !x.IsDeleted
                        && x.EmployeeId == entity.EmployeeId
                        && x.Year == entity.SalaryYear)
                    .OrderBy(x => x.LeaveType.Name)
                    .Select(x => new PayslipLeaveBalanceLineDto
                    {
                        LeaveTypeName = x.LeaveType.Name,
                        Entitled = x.OpeningBalance + x.Allocated + x.Credited + x.CarryForward,
                        Used = x.Used,
                        Balance = x.Balance
                    })
                    .ToListAsync();

                var earnings = (entity.PayrollDetails ?? new List<PayrollDetail>())
                    .Where(d => d.IsEarning)
                    .Select(d => new PayslipLineDto
                    {
                        ComponentName = d.SalaryComponent?.Name ?? "-",
                        RateOrUnitsDisplay = "-",
                        Amount = d.Amount
                    })
                    .ToList();

                var deductions = (entity.PayrollDetails ?? new List<PayrollDetail>())
                    .Where(d => !d.IsEarning)
                    .Select(d => new PayslipLineDto
                    {
                        ComponentName = d.SalaryComponent?.Name ?? "-",
                        RateOrUnitsDisplay = "-",
                        Amount = d.Amount
                    })
                    .ToList();

                var jwtSecret = _configuration["Jwt:Key"] ?? string.Empty;

                return new PayslipDto
                {
                    PayrollId = entity.Id,
                    Status = entity.Status,

                    Company = new PayslipCompanyDto
                    {
                        Name = company?.Name,
                        LogoUrl = company?.Logo,
                        Address = BuildCompanyAddress(company),
                        Phone = company?.Phone,
                        Email = company?.Email,
                        Website = company?.WebsiteUrl
                    },

                    Employee = new PayslipEmployeeDto
                    {
                        EmployeeId = entity.EmployeeId,
                        EmployeeCode = employee?.EmployeeCode,
                        EmployeeName = employee != null ? $"{employee.FirstName} {employee.LastName}".Trim() : "",
                        PhotoUrl = employee?.FilePath,
                        DepartmentName = employee?.Department?.Name,
                        DesignationName = employee?.Designation?.Name,
                        BranchName = employee?.Branch?.Name
                    },

                    Employment = new PayslipEmploymentDto
                    {
                        PAN = employee?.PANNumber,
                        UAN = pfDetail?.UANNumber,
                        PFNumber = pfDetail?.PFNumber,
                        DateOfJoining = employee?.JoiningDate,
                        EmploymentTypeName = employee?.EmploymentType.ToString()
                    },

                    BankDetails = bankDetail == null ? null : new PayslipBankDetailsDto
                    {
                        BankName = bankDetail.BankName,
                        AccountNumberMasked = MaskAccountNumber(bankDetail.AccountNumber),
                        IFSCCode = bankDetail.IFSCCode,
                        AccountTypeName = bankDetail.AccountType.ToString()
                    },

                    SalaryYear = entity.SalaryYear,
                    SalaryMonth = entity.SalaryMonth,
                    MonthName = MonthName(entity.SalaryMonth),
                    SalaryDate = entity.SalaryDate,
                    PayPeriodStart = monthStart,
                    PayPeriodEnd = monthEnd,
                    DaysInMonth = DateTime.DaysInMonth(entity.SalaryYear, entity.SalaryMonth),

                    Earnings = earnings,
                    Deductions = deductions,

                    GrossEarnings = entity.TotalEarnings,
                    TotalDeductions = entity.TotalDeductions,
                    NetSalary = entity.NetSalary,
                    NetSalaryInWords = NumberToWordsHelper.ToRupeesInWords(entity.NetSalary),

                    AttendanceSummary = new PayslipAttendanceSummaryDto
                    {
                        TotalDays = entity.TotalWorkingDays,
                        PresentDays = entity.PresentDays,
                        AbsentDays = CountOf(AttendanceStatus.Absent),
                        PaidLeaveDays = entity.PaidLeaveDays,
                        UnpaidLeaveDays = entity.UnpaidLeaveDays,
                        WeekOffDays = CountOf(AttendanceStatus.WeekOff),
                        HolidayDays = CountOf(AttendanceStatus.Holiday),
                        PayableDays = entity.PayableDays
                    },

                    TaxInfo = new PayslipTaxInfoDto
                    {
                        TaxRegimeName = taxRegimeName,
                        LopDays = entity.UnpaidLeaveDays
                    },

                    Ytd = ytd,
                    LeaveBalances = leaveBalances,

                    Remarks = new List<string>
                    {
                        "This is a computer generated payslip and does not require a signature.",
                        "Salary has been processed as per company policies.",
                        "For any discrepancies, please contact the HR department."
                    },

                    VerificationToken = string.IsNullOrEmpty(jwtSecret)
                        ? null
                        : Domain.Helper.PayslipVerificationHelper.GenerateToken(entity.Id, jwtSecret),

                    SignatoryCompanyName = company?.Name,
                    SignatoryImageUrl = null,

                    RecalculatedCount = entity.RecalculatedCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPayslipDocumentAsync failed for payrollId {PayrollId}", payrollId);
                return null;
            }
        }

        public async Task<PayslipVerificationResultDto?> VerifyPayslipAsync(string token)
        {
            try
            {
                var jwtSecret = _configuration["Jwt:Key"] ?? string.Empty;

                if (string.IsNullOrEmpty(jwtSecret) ||
                    !Domain.Helper.PayslipVerificationHelper.TryValidate(token, jwtSecret, out var payrollId))
                {
                    return new PayslipVerificationResultDto { IsValid = false };
                }

                var entity = await _context.Payrolls
                    .AsNoTracking()
                    .Include(x => x.Employee).ThenInclude(e => e.Company)
                    .FirstOrDefaultAsync(x => x.Id == payrollId && !x.IsDeleted);

                if (entity == null)
                    return new PayslipVerificationResultDto { IsValid = false };

                return new PayslipVerificationResultDto
                {
                    IsValid = true,
                    EmployeeName = entity.Employee != null
                        ? $"{entity.Employee.FirstName} {entity.Employee.LastName}".Trim()
                        : null,
                    EmployeeCode = entity.Employee?.EmployeeCode,
                    CompanyName = entity.Employee?.Company?.Name,
                    MonthName = MonthName(entity.SalaryMonth),
                    SalaryYear = entity.SalaryYear,
                    NetSalary = entity.NetSalary,
                    Status = entity.Status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VerifyPayslipAsync failed.");
                return new PayslipVerificationResultDto { IsValid = false };
            }
        }

        public async Task<bool> EmailPayslipAsync(string payrollId, string tenantId, string verificationUrl)
        {
            try
            {
                var entity = await _context.Payrolls
                    .AsNoTracking()
                    .Include(x => x.Employee).ThenInclude(e => e.Company)
                    .FirstOrDefaultAsync(x => x.Id == payrollId && !x.IsDeleted);

                if (entity == null || entity.TenantId != tenantId)
                    return false;

                var employee = entity.Employee;
                if (employee == null || string.IsNullOrWhiteSpace(employee.Email))
                    return false;

                var monthLabel = $"{MonthName(entity.SalaryMonth)} {entity.SalaryYear}";
                var companyName = employee.Company?.Name ?? "";
                var netInWords = NumberToWordsHelper.ToRupeesInWords(entity.NetSalary);

                var subject = $"Your Payslip - {monthLabel}";

                var body = $@"
                    <p>Dear {employee.FirstName},</p>
                    <p>Your payslip for <strong>{monthLabel}</strong> has been processed.</p>
                    <table style=""border-collapse:collapse;margin:12px 0;"">
                        <tr><td style=""padding:4px 12px 4px 0;color:#5b6b83;"">Net Pay</td><td style=""padding:4px 0;font-weight:bold;"">&#8377; {entity.NetSalary:N2}</td></tr>
                        <tr><td style=""padding:4px 12px 4px 0;color:#5b6b83;"">In Words</td><td style=""padding:4px 0;"">{netInWords}</td></tr>
                    </table>
                    {(string.IsNullOrEmpty(verificationUrl) ? "" : $@"<p>You can verify this payslip online at: <a href=""{verificationUrl}"">{verificationUrl}</a></p>")}
                    <p style=""color:#8b93a6;font-size:12px;"">This is a computer generated email from {companyName} HRMS and does not require a signature.</p>";

                return await _emailSender.SendAsync(employee.Email, subject, body, isBodyHtml: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailPayslipAsync failed for payrollId {PayrollId}", payrollId);
                return false;
            }
        }

        #endregion

        #region Dashboard

        public async Task<PayrollDashboardDto> GetDashboardAsync(int year, int month)
        {
            try
            {
            if (month < 1 || month > 12) month = DateTime.UtcNow.Month;

            var dto = new PayrollDashboardDto
            {
                Year = year,
                Month = month,
                MonthName = MonthName(month)
            };

            // Selected month payrolls
            var monthly = await _context.Payrolls
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.SalaryYear == year && p.SalaryMonth == month)
                .Select(p => new
                {
                    p.GrossSalary,
                    p.TotalDeductions,
                    p.NetSalary,
                    p.Status,
                    EmployeeName = p.Employee != null ? p.Employee.FirstName + " " + p.Employee.LastName : "",
                    EmployeeCode = p.Employee != null ? p.Employee.EmployeeCode : ""
                })
                .ToListAsync();

            dto.EmployeesPaid = monthly.Count;
            dto.TotalGross = monthly.Sum(x => x.GrossSalary);
            dto.TotalDeductions = monthly.Sum(x => x.TotalDeductions);
            dto.TotalNet = monthly.Sum(x => x.NetSalary);
            dto.AverageNet = monthly.Count > 0 ? Math.Round(dto.TotalNet / monthly.Count, 2) : 0;

            dto.DraftCount = monthly.Count(x => x.Status == "Draft");
            dto.ProcessedCount = monthly.Count(x => x.Status == "Processed");
            dto.PaidCount = monthly.Count(x => x.Status == "Paid");

            dto.TopEarners = monthly
                .OrderByDescending(x => x.NetSalary)
                .Take(5)
                .Select(x => new PayrollTopEarnerDto
                {
                    EmployeeName = x.EmployeeName,
                    EmployeeCode = x.EmployeeCode,
                    NetSalary = x.NetSalary
                })
                .ToList();

            // Company-wide active employee count (independent of whether a
            // payroll has been run for them this month).
            dto.TotalActiveEmployees = await _context.Employees
                .CountAsync(e => !e.IsDeleted && e.IsActive);

            // Rolling last-6-months trend ending at the selected year/month
            // (spans a year boundary correctly, e.g. Aug 2025 - Jan 2026).
            var monthKeys = new List<(int Year, int Month)>();
            {
                int ry = year, rm = month;
                for (int i = 0; i < 6; i++)
                {
                    monthKeys.Add((ry, rm));
                    rm--;
                    if (rm < 1) { rm = 12; ry--; }
                }
                monthKeys.Reverse();
            }
            var minOrdinal = monthKeys.Min(k => k.Year * 100 + k.Month);
            var maxOrdinal = monthKeys.Max(k => k.Year * 100 + k.Month);

            var trendData = await _context.Payrolls
                .AsNoTracking()
                .Where(p => !p.IsDeleted
                    && (p.SalaryYear * 100 + p.SalaryMonth) >= minOrdinal
                    && (p.SalaryYear * 100 + p.SalaryMonth) <= maxOrdinal)
                .GroupBy(p => new { p.SalaryYear, p.SalaryMonth })
                .Select(g => new
                {
                    g.Key.SalaryYear,
                    g.Key.SalaryMonth,
                    TotalGross = g.Sum(x => x.GrossSalary),
                    TotalDeductions = g.Sum(x => x.TotalDeductions),
                    TotalNet = g.Sum(x => x.NetSalary),
                    Count = g.Count()
                })
                .ToListAsync();

            foreach (var (ky, km) in monthKeys)
            {
                var found = trendData.FirstOrDefault(x => x.SalaryYear == ky && x.SalaryMonth == km);
                dto.MonthlyTrend.Add(new PayrollTrendPointDto
                {
                    Year = ky,
                    Month = km,
                    MonthName = MonthName(km),
                    TotalGross = found?.TotalGross ?? 0,
                    TotalDeductions = found?.TotalDeductions ?? 0,
                    TotalNet = found?.TotalNet ?? 0,
                    Count = found?.Count ?? 0
                });
            }

            // Salary distribution for the selected month - grouped from
            // existing PayrollDetail earning rows, never recalculated.
            var earningRows = await _context.PayrollDetails
                .AsNoTracking()
                .Where(d => d.IsEarning
                    && !d.Payroll.IsDeleted
                    && d.Payroll.SalaryYear == year
                    && d.Payroll.SalaryMonth == month)
                .Select(d => new { Name = d.SalaryComponent != null ? d.SalaryComponent.Name : "Other", d.Amount })
                .ToListAsync();

            if (earningRows.Count > 0)
            {
                var grouped = earningRows
                    .GroupBy(x => x.Name)
                    .Select(g => new { Name = g.Key, Amount = g.Sum(x => x.Amount) })
                    .OrderByDescending(x => x.Amount)
                    .ToList();

                decimal totalEarnings = grouped.Sum(x => x.Amount);
                var top = grouped.Take(5).ToList();
                var rest = grouped.Skip(5).Sum(x => x.Amount);

                foreach (var g in top)
                {
                    dto.SalaryDistribution.Add(new PayrollSalaryDistributionItemDto
                    {
                        ComponentName = g.Name ?? "Other",
                        Amount = g.Amount,
                        Percentage = totalEarnings > 0 ? Math.Round(g.Amount * 100m / totalEarnings, 1) : 0
                    });
                }
                if (rest > 0)
                {
                    dto.SalaryDistribution.Add(new PayrollSalaryDistributionItemDto
                    {
                        ComponentName = "Other Earnings",
                        Amount = rest,
                        Percentage = totalEarnings > 0 ? Math.Round(rest * 100m / totalEarnings, 1) : 0
                    });
                }
            }

            // Recent payroll payments (most recently processed first).
            dto.RecentPayments = await _context.Payrolls
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedOn)
                .Take(8)
                .Select(p => new PayrollRecentPaymentDto
                {
                    Id = p.Id,
                    EmployeeCode = p.Employee != null ? p.Employee.EmployeeCode : null,
                    EmployeeName = p.Employee != null ? p.Employee.FirstName + " " + p.Employee.LastName : null,
                    DepartmentName = p.Employee != null && p.Employee.Department != null ? p.Employee.Department.Name : null,
                    SalaryYear = p.SalaryYear,
                    SalaryMonth = p.SalaryMonth,
                    GrossSalary = p.GrossSalary,
                    Deductions = p.TotalDeductions,
                    NetSalary = p.NetSalary,
                    Status = p.Status
                })
                .ToListAsync();

            foreach (var rp in dto.RecentPayments)
                rp.MonthName = MonthName(rp.SalaryMonth);

            // Setup coverage
            dto.SalaryStructureCount = await _context.SalaryStructures.CountAsync(x => !x.IsDeleted);
            dto.SalaryComponentCount = await _context.SalaryComponents.CountAsync(x => !x.IsDeleted);

            return dto;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Helpers

        private static string MonthName(int month)
        {
            if (month < 1 || month > 12) return "";
            return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
        }

        #endregion
    }
}
