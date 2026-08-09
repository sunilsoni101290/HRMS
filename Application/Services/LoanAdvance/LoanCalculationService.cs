using Application.DTOs.LoanAdvance;
using Application.Interfaces.LoanAdvance;
using Domain.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.LoanAdvance
{
    /// <summary>See ILoanCalculationService for the Phase 6 vs Phase 8 scope split.</summary>
    public class LoanCalculationService : ILoanCalculationService
    {
        private readonly IUnitOfWork _uow;

        public LoanCalculationService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public List<LoanEmiSchedule> GenerateEmiSchedule(EmployeeLoan loan, DateTime firstDueDate)
        {
            var principal = loan.ApprovedAmount ?? loan.RequestedAmount;
            var tenureMonths = loan.TenureMonths;

            if (principal <= 0 || tenureMonths <= 0)
                throw new ArgumentException("Cannot generate an EMI schedule for a non-positive amount or tenure.");

            return loan.InterestMethod == InterestMethod.Flat
                ? GenerateFlatSchedule(loan, principal, tenureMonths, firstDueDate)
                : GenerateReducingBalanceSchedule(loan, principal, tenureMonths, firstDueDate);
        }

        /// <summary>
        /// Standard reducing-balance EMI:
        /// EMI = P × r × (1+r)^n / ((1+r)^n − 1), r = monthly rate.
        /// Each installment's interest is charged on the OUTSTANDING
        /// balance only (not the original principal), so the
        /// interest component shrinks and the principal component grows
        /// installment over installment - this is the system default per
        /// LoanType.InterestMethod.
        /// </summary>
        private static List<LoanEmiSchedule> GenerateReducingBalanceSchedule(
            EmployeeLoan loan, decimal principal, int tenureMonths, DateTime firstDueDate)
        {
            var monthlyRate = (loan.InterestRatePercent / 100m) / 12m;
            var schedule = new List<LoanEmiSchedule>();
            var balance = principal;

            decimal emi;

            if (monthlyRate == 0)
            {
                // Zero-interest edge case - avoid a divide-by-zero in the
                // standard formula, just split principal evenly.
                emi = Math.Round(principal / tenureMonths, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                var factor = (double)Math.Pow((double)(1 + monthlyRate), tenureMonths);
                emi = Math.Round(
                    principal * monthlyRate * (decimal)factor / ((decimal)factor - 1),
                    2, MidpointRounding.AwayFromZero);
            }

            for (var i = 1; i <= tenureMonths; i++)
            {
                var interest = Math.Round(balance * monthlyRate, 2, MidpointRounding.AwayFromZero);
                var principalComponent = i == tenureMonths
                    ? balance // last installment absorbs any rounding remainder
                    : Math.Round(emi - interest, 2, MidpointRounding.AwayFromZero);

                var closingBalance = Math.Max(0, balance - principalComponent);

                schedule.Add(new LoanEmiSchedule
                {
                    EmployeeLoanId = loan.Id,
                    InstallmentNumber = i,
                    DueDate = firstDueDate.AddMonths(i - 1),
                    OpeningBalance = balance,
                    PrincipalComponent = principalComponent,
                    InterestComponent = interest,
                    EmiAmount = principalComponent + interest,
                    ClosingBalance = closingBalance,
                    Status = InstallmentStatus.Pending,
                    CreatedBy = "SYSTEM",
                    CreatedOn = DateTime.UtcNow
                });

                balance = closingBalance;
            }

            return schedule;
        }

        /// <summary>
        /// Flat-rate: total interest = P × annualRate% × (tenureMonths/12),
        /// split evenly across every installment alongside an equal
        /// principal share - EMI is constant, but (unlike reducing
        /// balance) does not reflect a shrinking true cost of capital as
        /// the balance is paid down. Only used when LoanType/LoanPolicy
        /// explicitly opts a Loan Type into InterestMethod.Flat.
        /// </summary>
        private static List<LoanEmiSchedule> GenerateFlatSchedule(
            EmployeeLoan loan, decimal principal, int tenureMonths, DateTime firstDueDate)
        {
            var totalInterest = Math.Round(
                principal * (loan.InterestRatePercent / 100m) * (tenureMonths / 12m),
                2, MidpointRounding.AwayFromZero);

            var monthlyPrincipal = Math.Round(principal / tenureMonths, 2, MidpointRounding.AwayFromZero);
            var monthlyInterest = Math.Round(totalInterest / tenureMonths, 2, MidpointRounding.AwayFromZero);

            var schedule = new List<LoanEmiSchedule>();
            var balance = principal;

            for (var i = 1; i <= tenureMonths; i++)
            {
                var principalComponent = i == tenureMonths ? balance : monthlyPrincipal;
                var closingBalance = Math.Max(0, balance - principalComponent);

                schedule.Add(new LoanEmiSchedule
                {
                    EmployeeLoanId = loan.Id,
                    InstallmentNumber = i,
                    DueDate = firstDueDate.AddMonths(i - 1),
                    OpeningBalance = balance,
                    PrincipalComponent = principalComponent,
                    InterestComponent = monthlyInterest,
                    EmiAmount = principalComponent + monthlyInterest,
                    ClosingBalance = closingBalance,
                    Status = InstallmentStatus.Pending,
                    CreatedBy = "SYSTEM",
                    CreatedOn = DateTime.UtcNow
                });

                balance = closingBalance;
            }

            return schedule;
        }

        /// <summary>
        /// PHASE 8 - full eligibility: policy bounds, active-loan-count and
        /// service-tenure gates (unchanged from Phase 6), plus the
        /// employee's real latest Payroll.GrossSalary/NetSalary and the
        /// TRUE aggregate of every currently-Pending EMI/Advance
        /// installment (across ALL loan types and advances, not just this
        /// LoanType) to compute MaxAdditionalMonthlyDeduction against
        /// LoanPolicy.MaxDeductionPercentOfNetSalary. Also estimates the
        /// requested loan's own EMI (via the same amortization math used at
        /// disbursement) and rejects up front if it alone would breach that
        /// remaining headroom - the whole point of an eligibility check run
        /// BEFORE the employee submits.
        /// </summary>
        public async Task<LoanEligibilityDto> CheckEligibilityAsync(string employeeId, string loanTypeId, decimal requestedAmount, int tenureMonths, string tenantId)
        {
            var employee = await _uow.Repository<Employee>().Query()
                .FirstOrDefaultAsync(x => x.Id == employeeId && x.TenantId == tenantId && !x.IsDeleted);

            if (employee == null)
                return new LoanEligibilityDto { IsEligible = false, ReasonIfNotEligible = "Employee not found." };

            var policy = await _uow.Repository<LoanPolicy>().Query()
                .Where(x => x.TenantId == tenantId && x.LoanTypeId == loanTypeId && x.IsActive && !x.IsDeleted &&
                    (x.CompanyId == null || x.CompanyId == employee.CompanyId) &&
                    (x.BranchId == null || x.BranchId == employee.BranchId))
                .OrderByDescending(x => x.BranchId != null)
                .ThenByDescending(x => x.CompanyId != null)
                .FirstOrDefaultAsync();

            if (policy == null)
                return new LoanEligibilityDto { IsEligible = false, ReasonIfNotEligible = "No active Loan Policy is configured for this Loan Type / your Company / Branch." };

            var activeLoanCount = await _uow.Repository<EmployeeLoan>().Query()
                .CountAsync(x => x.EmployeeId == employeeId && x.LoanTypeId == loanTypeId &&
                    (x.Status == LoanStatus.Active || x.Status == LoanStatus.Disbursed));

            if (activeLoanCount >= policy.MaxActiveLoans)
                return new LoanEligibilityDto
                {
                    IsEligible = false,
                    ReasonIfNotEligible = $"You already have {activeLoanCount} active loan(s) of this type - the policy limit is {policy.MaxActiveLoans}.",
                    CurrentActiveLoanCount = activeLoanCount
                };

            var serviceMonths = ((DateTime.UtcNow.Year - employee.JoiningDate.Year) * 12) + DateTime.UtcNow.Month - employee.JoiningDate.Month;
            if (serviceMonths < policy.MinServiceMonthsRequired)
                return new LoanEligibilityDto
                {
                    IsEligible = false,
                    ReasonIfNotEligible = $"Minimum {policy.MinServiceMonthsRequired} months of service required - you have {serviceMonths}."
                };

            if (requestedAmount < policy.MinAmount || requestedAmount > policy.MaxAmount)
                return new LoanEligibilityDto
                {
                    IsEligible = false,
                    ReasonIfNotEligible = $"Amount must be between {policy.MinAmount:N0} and {policy.MaxAmount:N0} per policy."
                };

            if (tenureMonths < policy.MinTenureMonths || tenureMonths > policy.MaxTenureMonths)
                return new LoanEligibilityDto
                {
                    IsEligible = false,
                    ReasonIfNotEligible = $"Tenure must be between {policy.MinTenureMonths} and {policy.MaxTenureMonths} months per policy."
                };

            var outstandingTotal = await _uow.Repository<EmployeeLoan>().Query()
                .Where(x => x.EmployeeId == employeeId && (x.Status == LoanStatus.Active || x.Status == LoanStatus.Disbursed))
                .SumAsync(x => (decimal?)x.OutstandingPrincipal) ?? 0;

            // Real latest Payroll figures - not a flat assumption.
            var latestPayroll = await _uow.Repository<Payroll>().Query()
                .Where(x => x.EmployeeId == employeeId && x.TenantId == tenantId && !x.IsDeleted)
                .OrderByDescending(x => x.SalaryYear).ThenByDescending(x => x.SalaryMonth)
                .FirstOrDefaultAsync();

            var monthlyGross = latestPayroll?.GrossSalary ?? 0;
            var monthlyNet = latestPayroll?.NetSalary ?? 0;

            // TRUE aggregate of every currently-Pending obligation across
            // ALL loan types (one loan can have many Pending EMI rows for
            // future months - only the NEXT due one per loan counts toward
            // "current monthly deduction").
            var pendingLoanEmis = await _uow.Repository<LoanEmiSchedule>().Query()
                .Where(x => x.EmployeeLoan.EmployeeId == employeeId && x.Status == InstallmentStatus.Pending)
                .OrderBy(x => x.DueDate)
                .Select(x => new { x.EmployeeLoanId, x.EmiAmount })
                .ToListAsync();

            var monthlyLoanObligation = pendingLoanEmis
                .GroupBy(x => x.EmployeeLoanId)
                .Sum(g => g.First().EmiAmount); // first = earliest due, thanks to the OrderBy above

            var pendingAdvanceInstallments = await _uow.Repository<AdvanceInstallment>().Query()
                .Where(x => x.EmployeeAdvance.EmployeeId == employeeId && x.Status == InstallmentStatus.Pending)
                .OrderBy(x => x.DueDate)
                .Select(x => new { x.EmployeeAdvanceId, x.InstallmentAmount })
                .ToListAsync();

            var monthlyAdvanceObligation = pendingAdvanceInstallments
                .GroupBy(x => x.EmployeeAdvanceId)
                .Sum(g => g.First().InstallmentAmount);

            var existingMonthlyDeductions = monthlyLoanObligation + monthlyAdvanceObligation;

            var maxDeductionCap = Math.Round(monthlyNet * (policy.MaxDeductionPercentOfNetSalary / 100m), 2, MidpointRounding.AwayFromZero);
            var maxAdditionalMonthlyDeduction = Math.Max(0, maxDeductionCap - existingMonthlyDeductions);

            // Salary-multiplier ceiling, on top of (not instead of) the
            // policy's own MinAmount/MaxAmount band.
            var maxEligibleAmount = policy.MaxAmount;
            if (monthlyGross > 0)
                maxEligibleAmount = Math.Min(policy.MaxAmount, monthlyGross * policy.EligibilitySalaryMultiplier);

            var eligibility = new LoanEligibilityDto
            {
                IsEligible = true,
                MaxEligibleAmount = maxEligibleAmount,
                MaxEligibleTenureMonths = policy.MaxTenureMonths,
                CurrentActiveLoanCount = activeLoanCount,
                CurrentTotalOutstanding = outstandingTotal,
                MonthlyGrossSalary = monthlyGross,
                MonthlyNetSalary = monthlyNet,
                ExistingMonthlyDeductions = existingMonthlyDeductions,
                MaxAdditionalMonthlyDeduction = maxAdditionalMonthlyDeduction
            };

            if (latestPayroll == null)
            {
                eligibility.IsEligible = false;
                eligibility.ReasonIfNotEligible = "No processed payroll on file yet - salary-based affordability cannot be verified.";
                return eligibility;
            }

            // Estimate the EMI the requested amount/tenure would actually
            // produce (same math ILoanCalculationService uses at
            // disbursement) and check it against the remaining headroom.
            var loanType = await _uow.Repository<LoanType>().GetByIdAsync(loanTypeId);
            var effectiveRate = policy.InterestRatePercent ?? loanType?.DefaultInterestRatePercent ?? 0;
            var effectiveMethod = loanType?.InterestMethod ?? InterestMethod.Reducing;
            var estimatedEmi = EstimateMonthlyEmi(requestedAmount, tenureMonths, effectiveRate, effectiveMethod);

            if (estimatedEmi > maxAdditionalMonthlyDeduction)
            {
                eligibility.IsEligible = false;
                eligibility.ReasonIfNotEligible =
                    $"Estimated EMI of {estimatedEmi:N2} exceeds the {maxAdditionalMonthlyDeduction:N2} you have left under the policy's " +
                    $"{policy.MaxDeductionPercentOfNetSalary:N0}% of net salary cap (already committed: {existingMonthlyDeductions:N2}).";
            }

            return eligibility;
        }

        /// <summary>
        /// Estimates a constant monthly EMI for a not-yet-disbursed amount/
        /// tenure/rate combination, mirroring GenerateReducingBalanceSchedule/
        /// GenerateFlatSchedule's formulas without materializing the full
        /// row-by-row schedule - used only for the eligibility affordability
        /// check above, where a single headline EMI figure is all that's needed.
        /// </summary>
        private static decimal EstimateMonthlyEmi(decimal principal, int tenureMonths, decimal annualRatePercent, InterestMethod method)
        {
            if (principal <= 0 || tenureMonths <= 0)
                return 0;

            if (method == InterestMethod.Flat)
            {
                var totalInterest = Math.Round(principal * (annualRatePercent / 100m) * (tenureMonths / 12m), 2, MidpointRounding.AwayFromZero);
                return Math.Round((principal + totalInterest) / tenureMonths, 2, MidpointRounding.AwayFromZero);
            }

            var monthlyRate = (annualRatePercent / 100m) / 12m;
            if (monthlyRate == 0)
                return Math.Round(principal / tenureMonths, 2, MidpointRounding.AwayFromZero);

            var factor = (double)Math.Pow((double)(1 + monthlyRate), tenureMonths);
            return Math.Round(principal * monthlyRate * (decimal)factor / ((decimal)factor - 1), 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// PHASE 8 - true accrued-interest-to-date: interest is charged
        /// daily (annualRate/365) on the CURRENT OutstandingPrincipal from
        /// the later of (a) the last Recovered installment's RecoveredOn
        /// date or (b) DisbursedOn if nothing has been recovered yet,
        /// through to asOfDate - i.e. only the interest that has genuinely
        /// accrued since the last payment, not a flat one-month estimate.
        /// </summary>
        public async Task<LoanPreClosureQuoteDto> GetPreClosureQuoteAsync(string employeeLoanId, DateTime asOfDate, string tenantId)
        {
            var loan = await _uow.Repository<EmployeeLoan>().Query()
                .FirstOrDefaultAsync(x => x.Id == employeeLoanId && x.TenantId == tenantId && !x.IsDeleted);

            if (loan == null)
                throw new Application.Common.Exceptions.NotFoundException("Loan not found.");

            var policy = await _uow.Repository<LoanPolicy>().GetByIdAsync(loan.LoanPolicyId);
            var penaltyPercent = policy?.PreClosurePenaltyPercent ?? 0;

            var lastRecoveredOn = await _uow.Repository<LoanEmiSchedule>().Query()
                .Where(x => x.EmployeeLoanId == loan.Id && x.Status == InstallmentStatus.Recovered && x.RecoveredOn != null)
                .OrderByDescending(x => x.RecoveredOn)
                .Select(x => x.RecoveredOn)
                .FirstOrDefaultAsync();

            var accrualStart = lastRecoveredOn ?? loan.DisbursedOn ?? loan.CreatedOn;
            var daysSince = Math.Max(0, (asOfDate.Date - accrualStart.Date).Days);

            var dailyRate = (loan.InterestRatePercent / 100m) / 365m;
            var accruedInterest = Math.Round(loan.OutstandingPrincipal * dailyRate * daysSince, 2, MidpointRounding.AwayFromZero);

            var penalty = Math.Round(loan.OutstandingPrincipal * (penaltyPercent / 100m), 2, MidpointRounding.AwayFromZero);

            return new LoanPreClosureQuoteDto
            {
                EmployeeLoanId = loan.Id,
                AsOfDate = asOfDate,
                OutstandingPrincipal = loan.OutstandingPrincipal,
                AccruedInterest = accruedInterest,
                PreClosurePenalty = penalty,
                TotalPayable = loan.OutstandingPrincipal + accruedInterest + penalty
            };
        }
    }
}
