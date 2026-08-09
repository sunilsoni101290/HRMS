using Application.Common.Exceptions;
using Application.DTOs.LoanAdvance;
using Application.Interfaces.LoanAdvance;
using Domain.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.LoanAdvance
{
    /// <summary>
    /// See IPayrollLoanRecoveryService for the idempotency contract. This
    /// is the ONLY place in the module that ever transitions a
    /// LoanEmiSchedule/AdvanceInstallment out of Pending via a payroll run
    /// (manual settlement/pre-closure paths in EmployeeLoanService /
    /// EmployeeAdvanceService are the other two, mutually exclusive, exits).
    /// </summary>
    public class PayrollLoanRecoveryService : IPayrollLoanRecoveryService
    {
        private const string SystemActor = "SYSTEM-PAYROLL";

        private readonly IUnitOfWork _uow;
        private readonly ILoanAdvanceAuditLogService _auditLog;
        private readonly ILogger<PayrollLoanRecoveryService> _logger;

        public PayrollLoanRecoveryService(IUnitOfWork uow, ILoanAdvanceAuditLogService auditLog, ILogger<PayrollLoanRecoveryService> logger)
        {
            _uow = uow;
            _auditLog = auditLog;
            _logger = logger;
        }

        public async Task<PayrollRecoveryResultDto> RecoverForPayrollAsync(string payrollId, string tenantId, string actingUserId)
        {
            if (string.IsNullOrWhiteSpace(payrollId))
                throw new BadRequestException("PayrollId is required.");

            var performedBy = string.IsNullOrWhiteSpace(actingUserId) ? SystemActor : actingUserId;

            // Tracked (not AsNoTracking) - we mutate TotalDeductions/NetSalary
            // on this same instance below and rely on IUnitOfWork.SaveChangesAsync
            // to flush it alongside the loan/advance side of the ledger.
            var payroll = await _uow.Repository<Payroll>().Query(asNoTracking: false)
                .FirstOrDefaultAsync(p => p.Id == payrollId && p.TenantId == tenantId && !p.IsDeleted);

            if (payroll == null)
                throw new NotFoundException("Payroll record not found.");

            var result = new PayrollRecoveryResultDto { PayrollId = payroll.Id, EmployeeId = payroll.EmployeeId };

            // Net pay still available for loan/advance recovery this period -
            // the primary safety guard (never drive net pay negative). The
            // employee's own affordability was already validated once, up
            // front, by ILoanCalculationService.CheckEligibilityAsync's
            // MaxAdditionalMonthlyDeduction check at request time - this
            // guard only protects against a since-changed circumstance
            // (e.g. unpaid leave that month shrank NetSalary).
            var availableNetPay = payroll.NetSalary;

            // async methods can't take ref/out parameters (CS1988), so the
            // running "net pay left to deduct" balance is threaded through
            // as a return value instead of by reference.
            var (loanRecovered, afterLoanNetPay) = await RecoverLoanEmisAsync(payroll, performedBy, result, availableNetPay);
            availableNetPay = afterLoanNetPay;
            var (advanceRecovered, afterAdvanceNetPay) = await RecoverAdvanceInstallmentsAsync(payroll, performedBy, result, availableNetPay);
            availableNetPay = afterAdvanceNetPay;

            if (loanRecovered || advanceRecovered)
            {
                payroll.TotalDeductions += result.TotalRecovered;
                payroll.NetSalary = availableNetPay;
                payroll.ModifiedBy = performedBy;
                payroll.ModifiedOn = DateTime.UtcNow;

                _uow.Repository<Payroll>().Update(payroll);
                await _uow.SaveChangesAsync();
            }

            if (result.Messages.Count == 0)
                result.Messages.Add("No due loan/advance installments for this period.");

            return result;
        }

        private async Task<(bool Recovered, decimal RemainingNetPay)> RecoverLoanEmisAsync(Payroll payroll, string performedBy, PayrollRecoveryResultDto result, decimal availableNetPay)
        {
            var dueEmis = await _uow.Repository<LoanEmiSchedule>().Query(asNoTracking: false)
                .Include(x => x.EmployeeLoan)
                .Where(x => x.EmployeeLoan.EmployeeId == payroll.EmployeeId
                    && x.Status == InstallmentStatus.Pending
                    && x.DueDate <= payroll.SalaryDate
                    && (x.EmployeeLoan.Status == LoanStatus.Active || x.EmployeeLoan.Status == LoanStatus.PreClosureRequested))
                .OrderBy(x => x.DueDate)
                .ThenBy(x => x.EmployeeLoanId)
                .ToListAsync();

            var touchedLoans = new Dictionary<string, decimal>();
            var any = false;
            var localAvailable = availableNetPay;

            foreach (var emi in dueEmis)
            {
                var loan = emi.EmployeeLoan;

                if (emi.EmiAmount > localAvailable)
                {
                    emi.Status = InstallmentStatus.Skipped;
                    emi.ModifiedBy = performedBy;
                    emi.ModifiedOn = DateTime.UtcNow;
                    _uow.Repository<LoanEmiSchedule>().Update(emi);

                    result.InstallmentsSkipped++;
                    result.Messages.Add($"Loan {loan.Id} installment #{emi.InstallmentNumber} ({emi.EmiAmount:N2}) skipped - insufficient net pay this period.");
                    continue;
                }

                emi.Status = InstallmentStatus.Recovered;
                emi.RecoveredOn = DateTime.UtcNow;
                emi.PayrollId = payroll.Id;
                emi.ModifiedBy = performedBy;
                emi.ModifiedOn = DateTime.UtcNow;
                _uow.Repository<LoanEmiSchedule>().Update(emi);

                await _uow.Repository<LoanPaymentHistory>().AddAsync(new LoanPaymentHistory
                {
                    EmployeeLoanId = loan.Id,
                    LoanEmiScheduleId = emi.Id,
                    PaymentSource = PaymentSource.PayrollDeduction,
                    AmountPaid = emi.EmiAmount,
                    PrincipalPaid = emi.PrincipalComponent,
                    InterestPaid = emi.InterestComponent,
                    PaymentDate = payroll.SalaryDate,
                    PayrollId = payroll.Id,
                    Remarks = $"Auto-recovered - payroll {payroll.SalaryMonth:00}/{payroll.SalaryYear}.",
                    CreatedBy = performedBy,
                    CreatedOn = DateTime.UtcNow
                });

                loan.OutstandingPrincipal = Math.Max(0, loan.OutstandingPrincipal - emi.PrincipalComponent);
                loan.ModifiedBy = performedBy;
                loan.ModifiedOn = DateTime.UtcNow;

                if (loan.OutstandingPrincipal <= 0)
                {
                    loan.Status = LoanStatus.Closed;
                    loan.ClosedOn = DateTime.UtcNow;
                    loan.ClosureReason = LoanClosureReason.FullyRecovered;
                    result.AccountsAutoClosed++;
                    result.Messages.Add($"Loan {loan.Id} fully recovered and auto-closed.");
                }

                _uow.Repository<EmployeeLoan>().Update(loan);

                localAvailable -= emi.EmiAmount;
                result.LoanInstallmentsRecovered++;
                result.TotalLoanRecovered += emi.EmiAmount;
                touchedLoans[loan.Id] = touchedLoans.GetValueOrDefault(loan.Id) + emi.EmiAmount;
                any = true;
            }

            foreach (var (loanId, amount) in touchedLoans)
            {
                await _auditLog.LogAsync("EmployeeLoan", loanId, "PayrollRecovery", null,
                    $"{{\"payrollId\":\"{payroll.Id}\",\"amountRecovered\":{amount}}}", payroll.TenantId, performedBy);
            }

            return (any, localAvailable);
        }

        private async Task<(bool Recovered, decimal RemainingNetPay)> RecoverAdvanceInstallmentsAsync(Payroll payroll, string performedBy, PayrollRecoveryResultDto result, decimal availableNetPay)
        {
            var dueInstallments = await _uow.Repository<AdvanceInstallment>().Query(asNoTracking: false)
                .Include(x => x.EmployeeAdvance)
                .Where(x => x.EmployeeAdvance.EmployeeId == payroll.EmployeeId
                    && x.Status == InstallmentStatus.Pending
                    && x.DueDate <= payroll.SalaryDate
                    && x.EmployeeAdvance.Status == AdvanceStatus.Disbursed)
                .OrderBy(x => x.DueDate)
                .ThenBy(x => x.EmployeeAdvanceId)
                .ToListAsync();

            var touchedAdvances = new Dictionary<string, decimal>();
            var any = false;
            var localAvailable = availableNetPay;

            foreach (var installment in dueInstallments)
            {
                var advance = installment.EmployeeAdvance;

                if (installment.InstallmentAmount > localAvailable)
                {
                    installment.Status = InstallmentStatus.Skipped;
                    installment.ModifiedBy = performedBy;
                    installment.ModifiedOn = DateTime.UtcNow;
                    _uow.Repository<AdvanceInstallment>().Update(installment);

                    result.InstallmentsSkipped++;
                    result.Messages.Add($"Advance {advance.Id} installment #{installment.InstallmentNumber} ({installment.InstallmentAmount:N2}) skipped - insufficient net pay this period.");
                    continue;
                }

                installment.Status = InstallmentStatus.Recovered;
                installment.RecoveredOn = DateTime.UtcNow;
                installment.PayrollId = payroll.Id;
                installment.ModifiedBy = performedBy;
                installment.ModifiedOn = DateTime.UtcNow;
                _uow.Repository<AdvanceInstallment>().Update(installment);

                await _uow.Repository<AdvancePaymentHistory>().AddAsync(new AdvancePaymentHistory
                {
                    EmployeeAdvanceId = advance.Id,
                    AdvanceInstallmentId = installment.Id,
                    PaymentSource = PaymentSource.PayrollDeduction,
                    AmountPaid = installment.InstallmentAmount,
                    PaymentDate = payroll.SalaryDate,
                    PayrollId = payroll.Id,
                    Remarks = $"Auto-recovered - payroll {payroll.SalaryMonth:00}/{payroll.SalaryYear}.",
                    CreatedBy = performedBy,
                    CreatedOn = DateTime.UtcNow
                });

                advance.OutstandingAmount = Math.Max(0, advance.OutstandingAmount - installment.InstallmentAmount);
                advance.ModifiedBy = performedBy;
                advance.ModifiedOn = DateTime.UtcNow;

                if (advance.OutstandingAmount <= 0)
                {
                    advance.Status = AdvanceStatus.Recovered;
                    advance.ClosedOn = DateTime.UtcNow;
                    result.AccountsAutoClosed++;
                    result.Messages.Add($"Advance {advance.Id} fully recovered and auto-closed.");
                }

                _uow.Repository<EmployeeAdvance>().Update(advance);

                localAvailable -= installment.InstallmentAmount;
                result.AdvanceInstallmentsRecovered++;
                result.TotalAdvanceRecovered += installment.InstallmentAmount;
                touchedAdvances[advance.Id] = touchedAdvances.GetValueOrDefault(advance.Id) + installment.InstallmentAmount;
                any = true;
            }

            foreach (var (advanceId, amount) in touchedAdvances)
            {
                await _auditLog.LogAsync("EmployeeAdvance", advanceId, "PayrollRecovery", null,
                    $"{{\"payrollId\":\"{payroll.Id}\",\"amountRecovered\":{amount}}}", payroll.TenantId, performedBy);
            }

            return (any, localAvailable);
        }
    }
}
