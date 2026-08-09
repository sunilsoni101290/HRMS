using Application.Interfaces.LoanAdvance;
using Application.Services.LoanAdvance;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Interfaces;
using Moq;
using Xunit;
using static Domain.Enums.EnumExtensions;

namespace LoanAdvance.Tests.Unit
{
    /// <summary>
    /// Pure EMI-math tests for LoanCalculationService.GenerateEmiSchedule -
    /// no database involved (IUnitOfWork is only touched by
    /// CheckEligibilityAsync/GetPreClosureQuoteAsync, not this method), so a
    /// loose Moq.Of&lt;IUnitOfWork&gt;() stand-in is enough.
    /// </summary>
    public class LoanCalculationServiceTests
    {
        private readonly ILoanCalculationService _sut = new LoanCalculationService(Mock.Of<IUnitOfWork>());

        [Fact]
        public void ReducingBalance_TotalPrincipalAcrossInstallments_EqualsRequestedPrincipal()
        {
            var loan = new EmployeeLoan
            {
                Id = "L1",
                RequestedAmount = 120000m,
                ApprovedAmount = 120000m,
                TenureMonths = 12,
                InterestRatePercent = 12m,
                InterestMethod = InterestMethod.Reducing
            };

            var schedule = _sut.GenerateEmiSchedule(loan, new DateTime(2026, 9, 1));

            schedule.Should().HaveCount(12);
            schedule.Sum(e => e.PrincipalComponent).Should().BeApproximately(120000m, 0.02m);
            schedule.Last().ClosingBalance.Should().Be(0m);
            schedule[0].DueDate.Should().Be(new DateTime(2026, 9, 1));
            schedule[11].DueDate.Should().Be(new DateTime(2027, 8, 1));
        }

        [Fact]
        public void ReducingBalance_InterestComponent_ShrinksAsBalancePaysDown()
        {
            var loan = new EmployeeLoan
            {
                Id = "L2",
                RequestedAmount = 100000m,
                ApprovedAmount = 100000m,
                TenureMonths = 6,
                InterestRatePercent = 18m,
                InterestMethod = InterestMethod.Reducing
            };

            var schedule = _sut.GenerateEmiSchedule(loan, DateTime.UtcNow.Date);

            // Reducing-balance: interest is charged on the shrinking
            // outstanding balance, so it must never increase installment
            // over installment.
            for (var i = 1; i < schedule.Count; i++)
                schedule[i].InterestComponent.Should().BeLessThanOrEqualTo(schedule[i - 1].InterestComponent);
        }

        [Fact]
        public void ReducingBalance_ZeroInterestRate_SplitsEvenlyWithoutDivideByZero()
        {
            var loan = new EmployeeLoan
            {
                Id = "L3",
                RequestedAmount = 60000m,
                ApprovedAmount = 60000m,
                TenureMonths = 6,
                InterestRatePercent = 0m,
                InterestMethod = InterestMethod.Reducing
            };

            // Should not throw (would be a divide-by-zero in the naive EMI
            // formula) - if it does, this test fails with the exception.
            var schedule = _sut.GenerateEmiSchedule(loan, DateTime.UtcNow.Date);

            schedule.Should().OnlyContain(e => e.InterestComponent == 0m);
            schedule.Sum(e => e.PrincipalComponent).Should().Be(60000m);
        }

        [Fact]
        public void FlatMethod_TotalPrincipalAcrossInstallments_EqualsRequestedPrincipal()
        {
            var loan = new EmployeeLoan
            {
                Id = "L4",
                RequestedAmount = 60000m,
                ApprovedAmount = 60000m,
                TenureMonths = 12,
                InterestRatePercent = 10m,
                InterestMethod = InterestMethod.Flat
            };

            var schedule = _sut.GenerateEmiSchedule(loan, DateTime.UtcNow.Date);

            schedule.Should().HaveCount(12);
            schedule.Sum(e => e.PrincipalComponent).Should().Be(60000m);
            // Flat method: every installment's interest component is identical.
            schedule.Select(e => e.InterestComponent).Distinct().Should().HaveCount(1);
        }

        [Fact]
        public void GenerateEmiSchedule_NonPositiveAmount_Throws()
        {
            var loan = new EmployeeLoan { Id = "L5", RequestedAmount = 0m, ApprovedAmount = 0m, TenureMonths = 12, InterestRatePercent = 10m, InterestMethod = InterestMethod.Reducing };

            Action act = () => _sut.GenerateEmiSchedule(loan, DateTime.UtcNow.Date);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GenerateEmiSchedule_NonPositiveTenure_Throws()
        {
            var loan = new EmployeeLoan { Id = "L6", RequestedAmount = 10000m, ApprovedAmount = 10000m, TenureMonths = 0, InterestRatePercent = 10m, InterestMethod = InterestMethod.Reducing };

            Action act = () => _sut.GenerateEmiSchedule(loan, DateTime.UtcNow.Date);

            act.Should().Throw<ArgumentException>();
        }
    }
}
