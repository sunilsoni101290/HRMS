using Application.Common.Exceptions;
using Application.DTOs.LoanAdvance;
using FluentAssertions;
using LoanAdvance.Tests.TestFixtures;
using Xunit;
using static Domain.Enums.EnumExtensions;

namespace LoanAdvance.Tests.Integration
{
    /// <summary>
    /// End-to-end EmployeeLoanService tests against a real UnitOfWork/
    /// Repository&lt;T&gt; over EF Core's InMemory provider - see
    /// LoanAdvanceTestFixture for what's seeded (a single-level
    /// ReportingManager LoanPolicy, a Finance user with Approve permission,
    /// a processed Payroll row). Each test gets a fresh fixture/database via
    /// IAsyncLifetime, so tests never interfere with each other.
    /// </summary>
    public class EmployeeLoanServiceTests : IAsyncLifetime
    {
        private LoanAdvanceTestFixture _fixture = null!;

        public async Task InitializeAsync() => _fixture = await LoanAdvanceTestFixture.CreateAsync();
        public Task DisposeAsync()
        {
            _fixture.Dispose();
            return Task.CompletedTask;
        }

        private async Task<EmployeeLoanDto> SubmitLoanAsync(decimal amount = 50000m, int tenureMonths = 12)
        {
            var service = _fixture.CreateLoanService();

            return await service.SubmitAsync(new LoanSubmitDto
            {
                EmployeeId = _fixture.ApplicantEmployeeId,
                LoanTypeId = _fixture.LoanTypeId,
                RequestedAmount = amount,
                TenureMonths = tenureMonths
            }, _fixture.TenantId, _fixture.ApplicantUserId);
        }

        [Fact]
        public async Task SubmitAsync_ValidRequest_ResultsInPendingApproval_AtLevel1()
        {
            var loan = await SubmitLoanAsync();

            loan.Status.Should().Be((int)LoanStatus.PendingApproval);
            loan.CurrentApprovalLevel.Should().Be(1);
            loan.MakerId.Should().Be(_fixture.ApplicantUserId);
        }

        [Fact]
        public async Task ApproveAsync_ByMaker_ThrowsUnauthorized()
        {
            var loan = await SubmitLoanAsync();
            var service = _fixture.CreateLoanService();

            // The Maker-Checker invariant: actingUserId != MakerId, checked
            // BEFORE any permission/approver-identity check, no override.
            Func<Task> act = () => service.ApproveAsync(
                new LoanApprovalActionDto { EmployeeLoanId = loan.Id, Decision = 1 },
                _fixture.TenantId, _fixture.ApplicantUserId);

            await act.Should().ThrowAsync<UnauthorizedException>();
        }

        [Fact]
        public async Task ApproveAsync_ByNonApprover_ThrowsUnauthorized()
        {
            var loan = await SubmitLoanAsync();
            var service = _fixture.CreateLoanService();

            // FinanceUserId isn't the resolved Level-1 approver (only the
            // Reporting Manager is) - Approve permission alone doesn't make
            // someone an approver at a specific level.
            Func<Task> act = () => service.ApproveAsync(
                new LoanApprovalActionDto { EmployeeLoanId = loan.Id, Decision = 1 },
                _fixture.TenantId, _fixture.FinanceUserId);

            await act.Should().ThrowAsync<UnauthorizedException>();
        }

        [Fact]
        public async Task ApproveAsync_ByResolvedManager_MovesToApproved_SingleLevelPolicy()
        {
            var loan = await SubmitLoanAsync();
            var service = _fixture.CreateLoanService();

            var approved = await service.ApproveAsync(
                new LoanApprovalActionDto { EmployeeLoanId = loan.Id, Decision = 1, ApprovedAmount = 50000m },
                _fixture.TenantId, _fixture.ManagerUserId);

            approved.Status.Should().Be((int)LoanStatus.Approved);
            approved.ApprovedAmount.Should().Be(50000m);
            approved.ApprovalHistory.Should().ContainSingle(h => h.CheckerId == _fixture.ManagerUserId && h.DecisionName == "Approved");
        }

        [Fact]
        public async Task RejectAsync_WithoutRemarks_ThrowsBadRequest()
        {
            var loan = await SubmitLoanAsync();
            var service = _fixture.CreateLoanService();

            Func<Task> act = () => service.RejectAsync(
                new LoanApprovalActionDto { EmployeeLoanId = loan.Id, Decision = 2, Remarks = null! },
                _fixture.TenantId, _fixture.ManagerUserId);

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task RejectAsync_WithRemarks_ResultsInRejectedStatus()
        {
            var loan = await SubmitLoanAsync();
            var service = _fixture.CreateLoanService();

            var rejected = await service.RejectAsync(
                new LoanApprovalActionDto { EmployeeLoanId = loan.Id, Decision = 2, Remarks = "Not eligible per policy exception." },
                _fixture.TenantId, _fixture.ManagerUserId);

            rejected.Status.Should().Be((int)LoanStatus.Rejected);
        }

        [Fact]
        public async Task DisburseAsync_ByFinance_GeneratesEmiScheduleAndActivatesLoan()
        {
            var loan = await SubmitLoanAsync(amount: 60000m, tenureMonths: 6);
            var service = _fixture.CreateLoanService();

            await service.ApproveAsync(
                new LoanApprovalActionDto { EmployeeLoanId = loan.Id, Decision = 1, ApprovedAmount = 60000m },
                _fixture.TenantId, _fixture.ManagerUserId);

            var disbursed = await service.DisburseAsync(new LoanDisbursementDto
            {
                EmployeeLoanId = loan.Id,
                DisbursedAmount = 60000m,
                DisbursedOn = new DateTime(2026, 9, 1),
                DisbursementMode = 1,
                FirstEmiDueDate = new DateTime(2026, 10, 1)
            }, _fixture.TenantId, _fixture.FinanceUserId);

            disbursed.Status.Should().Be((int)LoanStatus.Active);
            disbursed.OutstandingPrincipal.Should().Be(60000m);
            disbursed.EmiSchedule.Should().HaveCount(6);
            disbursed.EmiSchedule.Sum(e => e.PrincipalComponent).Should().BeApproximately(60000m, 0.02m);
        }

        [Fact]
        public async Task DisburseAsync_ByNonFinanceUser_ThrowsUnauthorized()
        {
            var loan = await SubmitLoanAsync();
            var service = _fixture.CreateLoanService();

            await service.ApproveAsync(
                new LoanApprovalActionDto { EmployeeLoanId = loan.Id, Decision = 1, ApprovedAmount = 50000m },
                _fixture.TenantId, _fixture.ManagerUserId);

            // ManagerUserId can approve at Level 1, but holds no Approve
            // permission on EMPLOYEE_LOAN - Disburse is Finance-only.
            Func<Task> act = () => service.DisburseAsync(new LoanDisbursementDto
            {
                EmployeeLoanId = loan.Id,
                DisbursedAmount = 50000m,
                DisbursedOn = DateTime.UtcNow,
                DisbursementMode = 1,
                FirstEmiDueDate = DateTime.UtcNow.AddMonths(1)
            }, _fixture.TenantId, _fixture.ManagerUserId);

            await act.Should().ThrowAsync<UnauthorizedException>();
        }

        [Fact]
        public async Task FullLifecycle_SubmitApproveDisburseSettle_ClosesLoanWithZeroOutstanding()
        {
            var service = _fixture.CreateLoanService();

            var loan = await SubmitLoanAsync(amount: 24000m, tenureMonths: 12);

            await service.ApproveAsync(
                new LoanApprovalActionDto { EmployeeLoanId = loan.Id, Decision = 1, ApprovedAmount = 24000m },
                _fixture.TenantId, _fixture.ManagerUserId);

            await service.DisburseAsync(new LoanDisbursementDto
            {
                EmployeeLoanId = loan.Id,
                DisbursedAmount = 24000m,
                DisbursedOn = new DateTime(2026, 9, 1),
                DisbursementMode = 1,
                FirstEmiDueDate = new DateTime(2026, 10, 1)
            }, _fixture.TenantId, _fixture.FinanceUserId);

            var afterPreClosureRequest = await service.RequestPreClosureAsync(loan.Id, _fixture.TenantId, _fixture.ApplicantUserId);
            afterPreClosureRequest.Status.Should().Be((int)LoanStatus.PreClosureRequested);

            var settled = await service.SettleAsync(new LoanSettlementDto
            {
                EmployeeLoanId = loan.Id,
                AmountPaid = 24500m, // principal + a bit of accrued interest/penalty
                PaymentDate = new DateTime(2026, 9, 15),
                ClosureReason = 2 // Pre-Closed
            }, _fixture.TenantId, _fixture.FinanceUserId);

            settled.Status.Should().Be((int)LoanStatus.Closed);
            settled.OutstandingPrincipal.Should().Be(0m);
            settled.ClosedOn.Should().NotBeNull();
            // Every remaining Pending EMI must be cancelled - no future
            // recoveries should be attempted against a closed loan.
            settled.EmiSchedule.Should().OnlyContain(e => e.StatusName != "Pending");
        }
    }
}
