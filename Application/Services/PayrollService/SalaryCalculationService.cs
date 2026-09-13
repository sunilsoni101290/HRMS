using Application.DTOs.Payroll;
using Application.Interfaces.Attendances;
using Application.Interfaces.Payroll;
using Domain.Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.PayrollService
{
    // =====================================================================
    // SALARY PROCESSING - attendance-based proration (dedicated calculator)
    // =====================================================================
    //
    // ROOT CAUSE this service fixes (see the "Salary Structure Management"
    // conversation's root-cause writeup for the full inspection):
    //
    //  1. The old formula in PayrollBusinessService.GenerateAsync computed
    //     `factor = presentDays / workingDays` where BOTH sides came from
    //     `COUNT(Attendance rows that month)` - NOT a fixed Calendar/Working
    //     Days denominator. Attendance rows are sparse (only written when a
    //     punch/WFH/OnDuty/manual entry happens - there is no nightly job
    //     that backfills Absent/WeekOff/Holiday rows for every calendar
    //     day), so "workingDays" silently varied employee-to-employee and
    //     month-to-month instead of being ₹31,000/31 or /26 as required.
    //
    //  2. Approved Leave (paid or unpaid) is NEVER synced into Attendance -
    //     LeaveApplicationService has no such code at all (an older,
    //     entirely-commented-out LeaveService class once did, and is dead).
    //     So a Paid Leave day produced NO Attendance row -> it was excluded
    //     from BOTH the numerator and denominator, silently double-hiding
    //     itself rather than being credited as a payable day. An Unpaid
    //     Leave day was hidden the exact same way, which is the real bug
    //     behind "salary isn't reducing correctly for unpaid leave": with a
    //     ratio-based factor, hiding a day from BOTH sides barely moves the
    //     ratio even though it should have reduced pay.
    //
    //  3. Attendance has no unique (EmployeeId, Date) constraint (plain
    //     index only), so duplicate rows for the same date were counted
    //     twice with no de-duplication.
    //
    // THE FIX (per confirmed decisions):
    //  - Payable Days = Present Days (deduplicated Attendance, ONE row per
    //    date) + Paid Leave Days, where Paid/Unpaid Leave is read DIRECTLY
    //    from approved LeaveApplication + LeaveType.IsPaid - bypassing
    //    Attendance for leave entirely, since that sync path is dead code
    //    and resurrecting it was explicitly out of scope for this fix.
    //  - Total Days In Period (the denominator) = a FIXED, config-driven
    //    number per AttendancePolicy.SalaryProrationBasis: either the
    //    month's actual Calendar Days (28-31), or a company-wide fixed
    //    Working Days figure (AttendancePolicy.FixedWorkingDaysPerMonth,
    //    default 26) - never a count of however many Attendance rows exist.
    //  - Each Earning salary-structure line is prorated as
    //    FullAmount * PayableDays / TotalDaysInPeriod, rounded once
    //    (2 decimals, AwayFromZero) - no compounding/intermediate rounding.
    //    Deductions stay flat/unprorated, exactly like the pre-existing
    //    behavior (this codebase has no PF/ESI/PT/TDS slab-calculation
    //    engine to plug in - only whatever flat Deduction-type Salary
    //    Component lines the Salary Structure carries).
    public class SalaryCalculationService : ISalaryCalculationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAttendancePolicyService _attendancePolicyService;

        // Statuses that count as "present" for a day (same set
        // PayrollBusinessService used before this fix - unchanged).
        private static readonly AttendanceStatus[] PresentStatuses =
        {
            AttendanceStatus.Present, AttendanceStatus.Late, AttendanceStatus.EarlyExit,
            AttendanceStatus.WorkFromHome, AttendanceStatus.OnDuty,
            AttendanceStatus.Overtime, AttendanceStatus.CompOff
        };

        public SalaryCalculationService(ApplicationDbContext context, IAttendancePolicyService attendancePolicyService)
        {
            _context = context;
            _attendancePolicyService = attendancePolicyService;
        }

        public async Task<SalaryCalculationResultDto> CalculateAsync(string employeeId, int salaryYear, int salaryMonth, string tenantId)
        {
            var results = await CalculateBatchAsync(new List<string> { employeeId }, salaryYear, salaryMonth, tenantId);
            return results.FirstOrDefault();
        }

        public async Task<List<SalaryCalculationResultDto>> CalculateBatchAsync(List<string> employeeIds, int salaryYear, int salaryMonth, string tenantId)
        {
            var results = new List<SalaryCalculationResultDto>();

            if (employeeIds == null || !employeeIds.Any())
                return results;

            var monthStart = new DateTime(salaryYear, salaryMonth, 1);
            var monthEnd = new DateTime(salaryYear, salaryMonth, DateTime.DaysInMonth(salaryYear, salaryMonth));

            var employees = await _context.Employees
                .AsNoTracking()
                .Where(e => employeeIds.Contains(e.Id) && !e.IsDeleted)
                .ToListAsync();

            // Existing Payroll rows for this month, for the "already
            // processed" flag the Review screen needs.
            var existingPayrolls = await _context.Payrolls
                .AsNoTracking()
                .Where(p => employeeIds.Contains(p.EmployeeId)
                    && p.SalaryYear == salaryYear && p.SalaryMonth == salaryMonth && !p.IsDeleted)
                .ToDictionaryAsync(p => p.EmployeeId, p => p);

            // One AttendancePolicy lookup per distinct Company (not per
            // employee) - cheap cache for this batch call.
            var policyCache = new Dictionary<string, (SalaryProrationBasis Basis, decimal FixedWorkingDays)>();

            foreach (var employeeId in employeeIds)
            {
                var employee = employees.FirstOrDefault(e => e.Id == employeeId);
                var result = new SalaryCalculationResultDto
                {
                    EmployeeId = employeeId,
                    SalaryYear = salaryYear,
                    SalaryMonth = salaryMonth
                };

                if (employee == null)
                {
                    result.CanProcess = false;
                    result.BlockReason = "Employee not found.";
                    results.Add(result);
                    continue;
                }

                result.EmployeeName = $"{employee.FirstName} {employee.LastName}".Trim();
                result.EmployeeCode = employee.EmployeeCode;

                if (existingPayrolls.TryGetValue(employeeId, out var existing))
                {
                    result.AlreadyProcessed = true;
                    result.ExistingPayrollId = existing.Id;
                    result.ExistingPayrollStatus = existing.Status;
                }

                // ---- Salary Structure (latest effective by period end) ----
                var structure = await _context.SalaryStructures
                    .AsNoTracking()
                    .Include(s => s.SalaryDetails)
                        .ThenInclude(d => d.SalaryComponent)
                    .Where(s => s.EmployeeId == employeeId && !s.IsDeleted && s.EffectiveFrom <= monthEnd)
                    .OrderByDescending(s => s.EffectiveFrom)
                    .FirstOrDefaultAsync();

                if (structure == null || structure.SalaryDetails == null || !structure.SalaryDetails.Any())
                {
                    result.CanProcess = false;
                    result.BlockReason = "No salary structure (assignment) found for this employee, effective by this period.";
                    results.Add(result);
                    continue;
                }

                // ---- Employment window within the month ----
                var joiningDate = ((DateTime)employee.JoiningDate).Date;
                var effectiveStart = joiningDate > monthStart ? joiningDate : monthStart;
                var effectiveEnd = monthEnd;

                if (employee.RelievingDate.HasValue && employee.RelievingDate.Value.Date < effectiveEnd)
                    effectiveEnd = employee.RelievingDate.Value.Date;

                if (effectiveStart > monthEnd || effectiveEnd < monthStart || effectiveStart > effectiveEnd)
                {
                    result.CanProcess = false;
                    result.BlockReason = "Employee was not employed during any part of this period (Joining/Relieving Date).";
                    results.Add(result);
                    continue;
                }

                if (joiningDate > monthStart)
                    result.Warnings.Add($"Employee joined on {joiningDate:dd MMM yyyy} - days before joining are excluded from Payable Days.");

                if (employee.RelievingDate.HasValue && employee.RelievingDate.Value.Date <= monthEnd)
                    result.Warnings.Add($"Employee was relieved on {employee.RelievingDate.Value:dd MMM yyyy} - days after relieving are excluded from Payable Days.");

                // ---- Proration basis (per Company, cached) ----
                var companyKey = employee.CompanyId ?? "__none__";
                if (!policyCache.TryGetValue(companyKey, out var basisInfo))
                {
                    var policy = await _attendancePolicyService.GetActiveForTenantAsync(tenantId, employee.CompanyId);
                    var basis = policy != null ? (SalaryProrationBasis)policy.SalaryProrationBasis : SalaryProrationBasis.WorkingDays;
                    var fixedDays = policy != null && policy.FixedWorkingDaysPerMonth > 0 ? policy.FixedWorkingDaysPerMonth : 26m;
                    basisInfo = (basis, fixedDays);
                    policyCache[companyKey] = basisInfo;
                }

                result.ProrationBasisUsed = (int)basisInfo.Basis;
                result.ProrationBasisUsedName = basisInfo.Basis.ToString();
                result.TotalDaysInPeriod = basisInfo.Basis == SalaryProrationBasis.CalendarDays
                    ? DateTime.DaysInMonth(salaryYear, salaryMonth)
                    : basisInfo.FixedWorkingDays;

                // ---- Attendance: one row per date, most-recently-updated
                //      wins (de-duplication - see class remarks #3) ----
                var rawAttendance = await _context.Attendances
                    .AsNoTracking()
                    .Where(a => a.EmployeeId == employeeId
                        && a.Date >= effectiveStart && a.Date <= effectiveEnd
                        && !a.IsDeleted)
                    .Select(a => new { a.Date, a.Status, a.ModifiedOn, a.CreatedOn })
                    .ToListAsync();

                var dedupedAttendance = rawAttendance
                    .GroupBy(a => a.Date.Date)
                    .Select(g => g.OrderByDescending(a => a.ModifiedOn ?? a.CreatedOn).First())
                    .ToList();

                if (rawAttendance.Count > dedupedAttendance.Count)
                {
                    result.Warnings.Add(
                        $"{rawAttendance.Count - dedupedAttendance.Count} duplicate attendance record(s) were found for {dedupedAttendance.Count} date(s) - the most recently updated row per date was used.");
                }

                // ---- Approved Leave (Paid/Unpaid), read directly from
                //      LeaveApplication - NOT from Attendance (see class
                //      remarks #2 for why) ----
                var leaveApplications = await _context.LeaveApplications
                    .AsNoTracking()
                    .Include(l => l.LeaveType)
                    .Where(l => l.EmployeeId == employeeId
                        && l.Status == ApprovalStatus.Approved
                        && !l.IsDeleted
                        && l.FromDate.Date <= effectiveEnd && l.ToDate.Date >= effectiveStart)
                    .ToListAsync();

                // date -> (isPaid, dayValue) - built once so Attendance and
                // Leave never both count the same date (Leave always wins
                // for a date it covers).
                var leaveByDate = new Dictionary<DateTime, (bool IsPaid, decimal DayValue)>();

                foreach (var leave in leaveApplications)
                {
                    var isPaid = leave.LeaveType == null || leave.LeaveType.IsPaid;
                    var overlapStart = leave.FromDate.Date < effectiveStart ? effectiveStart : leave.FromDate.Date;
                    var overlapEnd = leave.ToDate.Date > effectiveEnd ? effectiveEnd : leave.ToDate.Date;

                    if (overlapStart > overlapEnd)
                        continue;

                    var isSingleDayHalf = leave.IsHalfDay && leave.FromDate.Date == leave.ToDate.Date;

                    for (var d = overlapStart; d <= overlapEnd; d = d.AddDays(1))
                    {
                        var dayValue = isSingleDayHalf ? 0.5m : 1m;
                        // A date already covered by another leave application
                        // (shouldn't normally happen - overlapping leave
                        // requests should be prevented by the Leave module)
                        // keeps whichever was recorded first rather than
                        // double-counting.
                        if (!leaveByDate.ContainsKey(d))
                            leaveByDate[d] = (isPaid, dayValue);
                    }
                }

                decimal presentDays = 0;
                foreach (var a in dedupedAttendance)
                {
                    var date = a.Date.Date;
                    if (leaveByDate.ContainsKey(date))
                        continue; // Leave takes precedence for this date.

                    if (a.Status == AttendanceStatus.HalfDay)
                        presentDays += 0.5m;
                    else if (PresentStatuses.Contains(a.Status))
                        presentDays += 1m;
                }

                var paidLeaveDays = leaveByDate.Values.Where(v => v.IsPaid).Sum(v => v.DayValue);
                var unpaidLeaveDays = leaveByDate.Values.Where(v => !v.IsPaid).Sum(v => v.DayValue);

                var employedDaysInMonth = (decimal)(effectiveEnd - effectiveStart).Days + 1;

                var payableDays = presentDays + paidLeaveDays;
                if (payableDays > employedDaysInMonth)
                    payableDays = employedDaysInMonth;
                if (payableDays > result.TotalDaysInPeriod)
                    payableDays = result.TotalDaysInPeriod;
                if (payableDays < 0)
                    payableDays = 0;

                result.PresentDays = presentDays;
                result.PaidLeaveDays = paidLeaveDays;
                result.UnpaidLeaveDays = unpaidLeaveDays;
                result.PayableDays = payableDays;

                // ---- Prorate the Salary Structure lines ----
                decimal monthlySalary = 0, grossSalary = 0, totalDeductions = 0;

                foreach (var line in structure.SalaryDetails)
                {
                    var isEarning = line.SalaryComponent != null
                        && line.SalaryComponent.ComponentType == SalaryComponentType.Earning;

                    var lineDto = new SalaryCalculationLineDto
                    {
                        SalaryComponentId = line.SalaryComponentId,
                        SalaryComponentName = line.SalaryComponent?.Name ?? "",
                        ComponentType = isEarning ? (int)SalaryComponentType.Earning : (int)SalaryComponentType.Deduction,
                        FullAmount = line.Amount
                    };

                    if (isEarning)
                    {
                        monthlySalary += line.Amount;

                        lineDto.ProratedAmount = result.TotalDaysInPeriod > 0
                            ? Math.Round(line.Amount * payableDays / result.TotalDaysInPeriod, 2, MidpointRounding.AwayFromZero)
                            : 0;

                        grossSalary += lineDto.ProratedAmount;
                    }
                    else
                    {
                        // Deductions are NOT prorated - flat, same as the
                        // pre-existing behavior (no statutory PF/ESI/PT/TDS
                        // calculation engine exists in this codebase to
                        // recompute them from Payable Days).
                        lineDto.ProratedAmount = line.Amount;
                        totalDeductions += line.Amount;
                    }

                    result.Lines.Add(lineDto);
                }

                result.MonthlySalary = monthlySalary;
                result.GrossSalary = grossSalary;
                result.TotalDeductions = totalDeductions;
                result.NetSalary = grossSalary - totalDeductions;

                results.Add(result);
            }

            return results;
        }
    }
}
