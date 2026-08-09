using Application.Common.Exceptions;
using Application.DTOs.LoanAdvance;
using FluentAssertions;
using LoanAdvance.Tests.TestFixtures;
using Xunit;
using static Domain.Enums.EnumExtensions;

namespace LoanAdvance.Tests.Integration
{
    /// <summary>
    /// End-to-end EmployeeAdvanceService tests - mirrors
    /// EmployeeLoanServiceTests' shape for the single-level (Reporting
    /// Manager only, no configurable matrix) approval workflow.
    /// </summary>
    public class EmployeeAdvanceServiceTests : IAsyncLifetime
    {
        private LoanAdvanceTestFixture _fixture = null!;

        public async Task InitializeAsync() => _fixture = await LoanAdvanceTestFixture.CreateAsync();
        public Task DisposeAsync()
        {
            _fixture.Dispose();
            return Task.CompletedTask;
        }

        private async Task<EmployeeAdvanceDto> SubmitAdvanceAsync(decimal amount = 6000m, int installments = 3)
        {
            var service = _fixture.CreateAdvanceService();

            return await service.SubmitAsync(new AdvanceSubmitDto
            {
                EmployeeId = _fixture.ApplicantEmployeeId,
                AdvanceTypeId = _fixture.AdvanceTypeId,
                RequestedAmount = amount,
                InstallmentCount = installments
            }, _fixture.TenantId, _fixture.ApplicantUserId);
        }

        [Fact]
        public async Task SubmitAsync_ValidRequest_ResultsInPendingApproval()
        {
            var advance = await SubmitAdvanceAsync();

            advance.Status.Should().Be((int)AdvanceStatus.PendingApproval);
            advance.CurrentApprovalLevel.Should().Be(1);
        }

        [Fact]
        public async Task SubmitAsync_AmountOverAdvanceTypeMax_ThrowsBadRequest()
        {
            // AdvanceTypeId's MaxAmount is seeded at 50000.
            var service = _fixture.CreateAdvanceService();

            Func<Task> act = () => service.SubmitAsync(new AdvanceSubmitDto
            {
                EmployeeId = _fixture.ApplicantEmployeeId,
                AdvanceTypeId = _fixture.AdvanceTypeId,
                RequestedAmount = 75000m,
                InstallmentCount = 3
            }, _fixture.TenantId, _fixture.ApplicantUserId);

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task SubmitAsync_SecondOpenRequestOfSameType_ThrowsBadRequest()
        {
            await SubmitAdvanceAsync();

            // The first request is still open (PendingApproval) - a second
            // request of the SAME AdvanceType must be rejected.
            Func<Task> act = () => SubmitAdvanceAsync();

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task ApproveAsync_ByMaker_ThrowsUnauthorized()
        {
            var advance = await SubmitAdvanceAsync();
            var service = _fixture.CreateAdvanceService();

            Func<Task> act = () => service.ApproveAsync(
                new AdvanceApprovalActionDto { EmployeeAdvanceId = advance.Id, Decision = 1 },
                _fixture.TenantId, _fixture.ApplicantUserId);

            await act.Should().ThrowAsync<UnauthorizedException>();
        }

        [Fact]
        public async Task ApproveAsync_ByReportingManager_MovesToApproved()
        {
            var advance = await SubmitAdvanceAsync();
            var service = _fixture.CreateAdvanceService();

            var approved = await service.ApproveAsync(
                new AdvanceApprovalActionDto { EmployeeAdvanceId = advance.Id, Decision = 1, ApprovedAmount = 6000m },
                _fixture.TenantId, _fixture.ManagerUserId);

            approved.Status.Should().Be((int)AdvanceStatus.Approved);
            approved.ApprovedAmount.Should().Be(6000m);
        }

        [Fact]
        public async Task ApproveAsync_ByFinanceOverride_AlsoSucceeds()
        {
            // EnsureIsApproverAsync allows either the Reporting Manager OR
            // anyone holding Approve permission on EMPLOYEE_ADVANCE - the
            // "Finance override" this module deliberately supports.
            var advance = await SubmitAdvanceAsync();
            var service = _fixture.CreateAdvanceService();

            var approved = await service.ApproveAsync(
                new AdvanceApprovalActionDto { EmployeeAdvanceId = advance.Id, Decision = 1 },
                _fixture.TenantId, _fixture.FinanceUserId);

            approved.Status.Should().Be((int)AdvanceStatus.Approved);
        }

        [Fact]
        public async Task DisburseAsync_GeneratesFlatInstallmentSchedule_SummingToDisbursedAmount()
        {
            var advance = await SubmitAdvanceAsync(amount: 10000m, installments: 3);
            var service = _fixture.CreateAdvanceService();

            await service.ApproveAsync(
                new AdvanceApprovalActionDto { EmployeeAdvanceId = advance.Id, Decision = 1, ApprovedAmount = 10000m },
                _fixture.TenantId, _fixture.ManagerUserId);

            var disbursed = await service.DisburseAsync(new AdvanceDisbursementDto
            {
                EmployeeAdvanceId = advance.Id,
                DisbursedAmount = 10000m,
                DisbursedOn = new DateTime(2026, 9, 1),
                DisbursementMode = 1,
                FirstInstallmentDueDate = new DateTime(2026, 10, 1)
            }, _fixture.TenantId, _fixture.FinanceUserId);

            disbursed.Status.Should().Be((int)AdvanceStatus.Disbursed);
            disbursed.Installments.Should().HaveCount(3);
            disbursed.Installments.Sum(i => i.InstallmentAmount).Should().Be(10000m);
            // No interest split on Advances - unlike Loan EMIs.
            disbursed.OutstandingAmount.Should().Be(10000m);
        }

        [Fact]
        public async Task SettleAsync_FullAmount_ClosesAdvanceWithZeroOutstanding()
        {
            var advance = await SubmitAdvanceAsync(amount: 9000m, installments: 3);
            var service = _fixture.CreateAdvanceService();

            await service.ApproveAsync(
                new AdvanceApprovalActionDto { EmployeeAdvanceId = advance.Id, Decision = 1, ApprovedAmount = 9000m },
                _fixture.TenantId, _fixture.ManagerUserId);

            await service.DisburseAsync(new AdvanceDisbursementDto
            {
                EmployeeAdvanceId = advance.Id,
                DisbursedAmount = 9000m,
                DisbursedOn = new DateTime(2026, 9, 1),
                DisbursementMode = 1,
                FirstInstallmentDueDate = new DateTime(2026, 10, 1)
            }, _fixture.TenantId, _fixture.FinanceUserId);

            var settled = await service.SettleAsync(new AdvanceSettlementDto
            {
                EmployeeAdvanceId = advance.Id,
                AmountPaid = 9000m,
                PaymentDate = new DateTime(2026, 9, 20)
            }, _fixture.TenantId, _fixture.FinanceUserId);

            settled.Status.Should().Be((int)AdvanceStatus.Settled);
            settled.OutstandingAmount.Should().Be(0m);
            settled.ClosedOn.Should().NotBeNull();
            settled.Installments.Should().OnlyContain(i => i.StatusName != "Pending");
        }
    }
}
