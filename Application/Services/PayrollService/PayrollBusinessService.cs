using Application.DTOs.Payroll;
using Application.Interfaces.LoanAdvance;
using Application.Interfaces.Payroll;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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

        public PayrollBusinessService(
            ApplicationDbContext context,
            IPayrollLoanRecoveryService loanRecoveryService,
            ILogger<PayrollBusinessService> logger,
            ISalaryCalculationService salaryCalculationService)
        {
            _context = context;
            _loanRecoveryService = loanRecoveryService;
            _logger = logger;
            _salaryCalculationService = salaryCalculationService;
        }

        #region Get All

        public async Task<List<PayrollListDto>> GetAllAsync(int? year = null, int? month = null)
        {
            try
            {
            var query = _context.Payrolls
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (year.HasValue) query = query.Where(x => x.SalaryYear == year.Value);
            if (month.HasValue) query = query.Where(x => x.SalaryMonth == month.Value);

            var list = await query
                .OrderByDescending(x => x.SalaryYear).ThenByDescending(x => x.SalaryMonth)
                .Select(x => new PayrollListDto
                {
                    Id = x.Id,
                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee != null ? x.Employee.FirstName + " " + x.Employee.LastName : "",
                    EmployeeCode = x.Employee != null ? x.Employee.EmployeeCode : "",
                    SalaryYear = x.SalaryYear,
                    SalaryMonth = x.SalaryMonth,
                    GrossSalary = x.GrossSalary,
                    NetSalary = x.NetSalary,
                    PresentDays = x.PresentDays,
                    TotalWorkingDays = x.TotalWorkingDays,
                    Status = x.Status
                })
                .ToListAsync();

            foreach (var item in list)
                item.MonthName = MonthName(item.SalaryMonth);

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
        public async Task<SalaryProcessingPreviewDto> PreviewAsync(int salaryYear, int salaryMonth, string? companyId, string? branchId, string tenantId)
        {
            var periodEnd = new DateTime(salaryYear, salaryMonth, DateTime.DaysInMonth(salaryYear, salaryMonth));

            var structureQuery = _context.SalaryStructures
                .AsNoTracking()
                .Where(s => !s.IsDeleted && s.EffectiveFrom <= periodEnd);

            var employeeIds = await structureQuery
                .Select(s => s.EmployeeId)
                .Distinct()
                .ToListAsync();

            if (!string.IsNullOrEmpty(companyId) || !string.IsNullOrEmpty(branchId))
            {
                var filtered = await _context.Employees
                    .AsNoTracking()
                    .Where(e => employeeIds.Contains(e.Id) && !e.IsDeleted
                        && (string.IsNullOrEmpty(companyId) || e.CompanyId == companyId)
                        && (string.IsNullOrEmpty(branchId) || e.BranchId == branchId))
                    .Select(e => e.Id)
                    .ToListAsync();

                employeeIds = filtered;
            }

            var calculations = await _salaryCalculationService.CalculateBatchAsync(employeeIds, salaryYear, salaryMonth, tenantId);

            return new SalaryProcessingPreviewDto
            {
                SalaryYear = salaryYear,
                SalaryMonth = salaryMonth,
                MonthName = MonthName(salaryMonth),
                Employees = calculations.OrderBy(c => c.EmployeeName).ToList()
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

            // Monthly trend for the year
            var yearData = await _context.Payrolls
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.SalaryYear == year)
                .GroupBy(p => p.SalaryMonth)
                .Select(g => new
                {
                    Month = g.Key,
                    TotalNet = g.Sum(x => x.NetSalary),
                    Count = g.Count()
                })
                .ToListAsync();

            for (int m = 1; m <= 12; m++)
            {
                var found = yearData.FirstOrDefault(x => x.Month == m);
                dto.MonthlyTrend.Add(new PayrollTrendPointDto
                {
                    Month = m,
                    MonthName = MonthName(m),
                    TotalNet = found?.TotalNet ?? 0,
                    Count = found?.Count ?? 0
                });
            }

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
