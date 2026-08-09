using Application.Common.Exceptions;
using Application.DTOs.LoanAdvance;
using Application.Interfaces.LoanAdvance;
using Domain.Entities;
using Domain.Helper;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.LoanAdvance
{
    /// <summary>See ILoanReportService for the scope/architecture summary (Phase 14).</summary>
    public class LoanReportService : ILoanReportService
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmployeeLoanService _loanService;
        private readonly IEmployeeAdvanceService _advanceService;

        public LoanReportService(IUnitOfWork uow, IEmployeeLoanService loanService, IEmployeeAdvanceService advanceService)
        {
            _uow = uow;
            _loanService = loanService;
            _advanceService = advanceService;
        }

        public async Task<LoanAdvanceDashboardDto> GetDashboardAsync(string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, AppFeatureConstants.LOAN_ADVANCE_DASHBOARD, Actions.View);

            var loans = _uow.Repository<EmployeeLoan>().Query().Where(x => x.TenantId == tenantId && !x.IsDeleted);
            var advances = _uow.Repository<EmployeeAdvance>().Query().Where(x => x.TenantId == tenantId && !x.IsDeleted);

            var totalLoanDisbursed = await loans
                .Where(x => x.DisbursedAmount != null)
                .SumAsync(x => (decimal?)x.DisbursedAmount) ?? 0;

            var totalLoanOutstanding = await loans
                .Where(x => x.Status == LoanStatus.Active || x.Status == LoanStatus.Disbursed || x.Status == LoanStatus.PreClosureRequested)
                .SumAsync(x => (decimal?)x.OutstandingPrincipal) ?? 0;

            var totalAdvanceDisbursed = await advances
                .Where(x => x.DisbursedAmount != null)
                .SumAsync(x => (decimal?)x.DisbursedAmount) ?? 0;

            // AdvanceStatus has no separate "Active" state (unlike LoanStatus) -
            // Disbursed IS the live/outstanding state until Recovered/Settled.
            var totalAdvanceOutstanding = await advances
                .Where(x => x.Status == AdvanceStatus.Disbursed)
                .SumAsync(x => (decimal?)x.OutstandingAmount) ?? 0;

            var pendingLoanApprovals = await loans.CountAsync(x => x.Status == LoanStatus.PendingApproval);
            var pendingAdvanceApprovals = await advances.CountAsync(x => x.Status == AdvanceStatus.PendingApproval);

            var today = DateTime.UtcNow.Date;
            var overdueLoanInstallments = await _uow.Repository<LoanEmiSchedule>().Query()
                .CountAsync(x => x.EmployeeLoan.TenantId == tenantId && x.Status == InstallmentStatus.Pending && x.DueDate < today);

            var overdueAdvanceInstallments = await _uow.Repository<AdvanceInstallment>().Query()
                .CountAsync(x => x.EmployeeAdvance.TenantId == tenantId && x.Status == InstallmentStatus.Pending && x.DueDate < today);

            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var thisMonthLoanRecovered = await _uow.Repository<LoanPaymentHistory>().Query()
                .Where(x => x.EmployeeLoan.TenantId == tenantId && x.PaymentDate >= monthStart)
                .SumAsync(x => (decimal?)x.AmountPaid) ?? 0;

            var thisMonthAdvanceRecovered = await _uow.Repository<AdvancePaymentHistory>().Query()
                .Where(x => x.EmployeeAdvance.TenantId == tenantId && x.PaymentDate >= monthStart)
                .SumAsync(x => (decimal?)x.AmountPaid) ?? 0;

            var thisMonthLoanDisbursed = await loans
                .Where(x => x.DisbursedOn != null && x.DisbursedOn >= monthStart)
                .SumAsync(x => (decimal?)x.DisbursedAmount) ?? 0;

            var thisMonthAdvanceDisbursed = await advances
                .Where(x => x.DisbursedOn != null && x.DisbursedOn >= monthStart)
                .SumAsync(x => (decimal?)x.DisbursedAmount) ?? 0;

            // Reuse the existing per-level/self-service approval resolution
            // in EmployeeLoanService/EmployeeAdvanceService rather than
            // re-implementing the N-level matrix lookup here.
            var pendingOnMeLoans = await _loanService.GetPendingOnMeAsync(tenantId, actingUserId);
            var pendingOnMeAdvances = await _advanceService.GetPendingOnMeAsync(tenantId, actingUserId);

            return new LoanAdvanceDashboardDto
            {
                TotalLoanDisbursed = totalLoanDisbursed,
                TotalLoanOutstanding = totalLoanOutstanding,
                TotalAdvanceDisbursed = totalAdvanceDisbursed,
                TotalAdvanceOutstanding = totalAdvanceOutstanding,
                PendingApprovalCount = pendingLoanApprovals + pendingAdvanceApprovals,
                OverdueInstallmentCount = overdueLoanInstallments + overdueAdvanceInstallments,
                ThisMonthRecoveredAmount = thisMonthLoanRecovered + thisMonthAdvanceRecovered,
                ThisMonthDisbursedAmount = thisMonthLoanDisbursed + thisMonthAdvanceDisbursed,
                PendingOnMeCount = pendingOnMeLoans.Count + pendingOnMeAdvances.Count
            };
        }

        public async Task<List<OutstandingBalanceReportRowDto>> GetOutstandingBalanceReportAsync(string tenantId, string actingUserId, string? departmentId = null)
        {
            await EnsurePermissionAsync(actingUserId, AppFeatureConstants.LOAN_ADVANCE_REPORT, Actions.View);

            var employeesQuery = _uow.Repository<Employee>().Query()
                .Where(x => x.TenantId == tenantId && !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(departmentId))
                employeesQuery = employeesQuery.Where(x => x.DepartmentId == departmentId);

            var employees = await employeesQuery
                .Select(x => new { x.Id, x.EmployeeCode, x.FirstName, x.LastName, x.DepartmentId })
                .ToListAsync();

            if (employees.Count == 0)
                return new List<OutstandingBalanceReportRowDto>();

            var employeeIds = employees.Select(e => e.Id).ToList();

            var departmentNames = await _uow.Repository<Department>().Query()
                .Where(d => employees.Select(e => e.DepartmentId).Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Name);

            var activeLoans = await _uow.Repository<EmployeeLoan>().Query()
                .Where(x => employeeIds.Contains(x.EmployeeId) &&
                    (x.Status == LoanStatus.Active || x.Status == LoanStatus.Disbursed || x.Status == LoanStatus.PreClosureRequested))
                .Select(x => new { x.Id, x.EmployeeId, x.OutstandingPrincipal })
                .ToListAsync();

            var activeAdvances = await _uow.Repository<EmployeeAdvance>().Query()
                .Where(x => employeeIds.Contains(x.EmployeeId) && x.Status == AdvanceStatus.Disbursed)
                .Select(x => new { x.Id, x.EmployeeId, x.OutstandingAmount })
                .ToListAsync();

            var loanIds = activeLoans.Select(l => l.Id).ToList();
            var advanceIds = activeAdvances.Select(a => a.Id).ToList();

            var pendingLoanEmis = loanIds.Count == 0
                ? new List<LoanEmiSchedule>()
                : await _uow.Repository<LoanEmiSchedule>().Query()
                    .Where(x => loanIds.Contains(x.EmployeeLoanId) && x.Status == InstallmentStatus.Pending)
                    .ToListAsync();

            var pendingAdvanceInstallments = advanceIds.Count == 0
                ? new List<AdvanceInstallment>()
                : await _uow.Repository<AdvanceInstallment>().Query()
                    .Where(x => advanceIds.Contains(x.EmployeeAdvanceId) && x.Status == InstallmentStatus.Pending)
                    .ToListAsync();

            var today = DateTime.UtcNow.Date;
            var rows = new List<OutstandingBalanceReportRowDto>();

            foreach (var emp in employees)
            {
                var empLoans = activeLoans.Where(l => l.EmployeeId == emp.Id).ToList();
                var empAdvances = activeAdvances.Where(a => a.EmployeeId == emp.Id).ToList();

                if (empLoans.Count == 0 && empAdvances.Count == 0)
                    continue; // only surface employees with an actual outstanding position

                var empLoanIds = empLoans.Select(l => l.Id).ToHashSet();
                var empAdvanceIds = empAdvances.Select(a => a.Id).ToHashSet();

                var empPendingEmis = pendingLoanEmis.Where(x => empLoanIds.Contains(x.EmployeeLoanId)).ToList();
                var empPendingInstallments = pendingAdvanceInstallments.Where(x => empAdvanceIds.Contains(x.EmployeeAdvanceId)).ToList();

                DateTime? nextDueDate = null;
                decimal? nextDueAmount = null;

                var nextLoanDue = empPendingEmis.OrderBy(x => x.DueDate).FirstOrDefault();
                var nextAdvanceDue = empPendingInstallments.OrderBy(x => x.DueDate).FirstOrDefault();

                if (nextLoanDue != null && (nextAdvanceDue == null || nextLoanDue.DueDate <= nextAdvanceDue.DueDate))
                {
                    nextDueDate = nextLoanDue.DueDate;
                    nextDueAmount = nextLoanDue.EmiAmount;
                }
                else if (nextAdvanceDue != null)
                {
                    nextDueDate = nextAdvanceDue.DueDate;
                    nextDueAmount = nextAdvanceDue.InstallmentAmount;
                }

                var maxDaysPastDue = 0;
                var allDueDates = empPendingEmis.Select(x => x.DueDate).Concat(empPendingInstallments.Select(x => x.DueDate));
                foreach (var due in allDueDates)
                {
                    if (due < today)
                        maxDaysPastDue = Math.Max(maxDaysPastDue, (today - due.Date).Days);
                }

                rows.Add(new OutstandingBalanceReportRowDto
                {
                    EmployeeId = emp.Id,
                    EmployeeName = $"{emp.FirstName} {emp.LastName}".Trim(),
                    EmployeeCode = emp.EmployeeCode,
                    DepartmentName = emp.DepartmentId != null ? departmentNames.GetValueOrDefault(emp.DepartmentId) : null,
                    ActiveLoanCount = empLoans.Count,
                    TotalLoanOutstanding = empLoans.Sum(l => l.OutstandingPrincipal),
                    ActiveAdvanceCount = empAdvances.Count,
                    TotalAdvanceOutstanding = empAdvances.Sum(a => a.OutstandingAmount),
                    NextDueDate = nextDueDate,
                    NextDueAmount = nextDueAmount,
                    MaxDaysPastDue = maxDaysPastDue
                });
            }

            return rows.OrderByDescending(x => x.TotalLoanOutstanding + x.TotalAdvanceOutstanding).ToList();
        }

        public async Task<List<LoanAdvancePaymentReportRowDto>> GetPaymentHistoryReportAsync(string tenantId, string actingUserId, DateTime fromDate, DateTime toDate)
        {
            await EnsurePermissionAsync(actingUserId, AppFeatureConstants.LOAN_ADVANCE_REPORT, Actions.View);

            var toDateInclusive = toDate.Date.AddDays(1).AddTicks(-1);

            var loanPayments = await _uow.Repository<LoanPaymentHistory>().Query()
                .Include(x => x.EmployeeLoan).ThenInclude(l => l.Employee)
                .Include(x => x.EmployeeLoan).ThenInclude(l => l.LoanType)
                .Where(x => x.EmployeeLoan.TenantId == tenantId && x.PaymentDate >= fromDate.Date && x.PaymentDate <= toDateInclusive)
                .ToListAsync();

            var advancePayments = await _uow.Repository<AdvancePaymentHistory>().Query()
                .Include(x => x.EmployeeAdvance).ThenInclude(a => a.Employee)
                .Include(x => x.EmployeeAdvance).ThenInclude(a => a.AdvanceType)
                .Where(x => x.EmployeeAdvance.TenantId == tenantId && x.PaymentDate >= fromDate.Date && x.PaymentDate <= toDateInclusive)
                .ToListAsync();

            var rows = new List<LoanAdvancePaymentReportRowDto>();

            rows.AddRange(loanPayments.Select(x => new LoanAdvancePaymentReportRowDto
            {
                EntityType = "Loan",
                EntityId = x.EmployeeLoanId,
                EmployeeId = x.EmployeeLoan.EmployeeId,
                EmployeeName = $"{x.EmployeeLoan.Employee.FirstName} {x.EmployeeLoan.Employee.LastName}".Trim(),
                EmployeeCode = x.EmployeeLoan.Employee.EmployeeCode,
                TypeName = x.EmployeeLoan.LoanType?.Name,
                PaymentSource = (int)x.PaymentSource,
                PaymentSourceName = x.PaymentSource.ToString(),
                AmountPaid = x.AmountPaid,
                PaymentDate = x.PaymentDate,
                PayrollId = x.PayrollId,
                ReceiptReference = x.ReceiptReference
            }));

            rows.AddRange(advancePayments.Select(x => new LoanAdvancePaymentReportRowDto
            {
                EntityType = "Advance",
                EntityId = x.EmployeeAdvanceId,
                EmployeeId = x.EmployeeAdvance.EmployeeId,
                EmployeeName = $"{x.EmployeeAdvance.Employee.FirstName} {x.EmployeeAdvance.Employee.LastName}".Trim(),
                EmployeeCode = x.EmployeeAdvance.Employee.EmployeeCode,
                TypeName = x.EmployeeAdvance.AdvanceType?.Name,
                PaymentSource = (int)x.PaymentSource,
                PaymentSourceName = x.PaymentSource.ToString(),
                AmountPaid = x.AmountPaid,
                PaymentDate = x.PaymentDate,
                PayrollId = x.PayrollId,
                ReceiptReference = x.ReceiptReference
            }));

            return rows.OrderByDescending(x => x.PaymentDate).ToList();
        }

        private async Task EnsurePermissionAsync(string? actingUserId, string featureId, string action)
        {
            if (string.IsNullOrEmpty(actingUserId))
                throw new UnauthorizedException("You are not authorized to view Loan & Advance reports.");

            var allowed = await (
                from ur in _uow.Repository<UserRole>().Query()
                join rp in _uow.Repository<RolePermission>().Query().Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _uow.Repository<Permission>().Query().Where(x => x.FeatureId == featureId && x.Action == action)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();

            if (!allowed)
                throw new UnauthorizedException("You are not authorized to view Loan & Advance reports.");
        }
    }
}
