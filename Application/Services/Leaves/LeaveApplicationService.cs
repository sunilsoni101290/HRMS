using Application.DTOs.Leaves;
using Application.Interfaces.Leaves;
using Application.Interfaces.Masters;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Leaves
{
    internal class LeaveApplicationService : ILeaveApplicationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWeekOffService _weekOffService;
        private readonly ITenantService _tenantService;
        private readonly ILeaveBalanceService _leaveBalanceService;
        private string tenantId = string.Empty;
        public LeaveApplicationService(ApplicationDbContext context, IWeekOffService weekOffService, ITenantService tenantService, ILeaveBalanceService leaveBalanceService)
        {
            _context = context;
            _tenantService = tenantService;
            _weekOffService = weekOffService;
            _leaveBalanceService = leaveBalanceService;
        }

        // =====================================================
        // GET ALL
        // =====================================================

        public async Task<List<LeaveApplicationDto>> GetAllAsync()
        {
            return await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new LeaveApplicationDto
                {
                    Id = x.Id,

                    CompanyId = x.CompanyId,
                    BranchId = x.BranchId,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

                    FromDate = x.FromDate,
                    ToDate = x.ToDate,
                    TotalDays = x.TotalDays,

                    IsHalfDay = x.IsHalfDay,
                    HalfDayType = x.HalfDayType,

                    Reason = x.Reason,

                    Status = x.Status,

                    ApprovedBy = x.ApprovedBy,
                    ApprovedDate = x.ApprovedDate,

                    RejectedReason = x.RejectedReason,

                    DocumentUrl = x.DocumentUrl,

                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .ToListAsync();
        }

        // =====================================================
        // GET BY ID
        // =====================================================

        public async Task<LeaveApplicationDto?> GetByIdAsync(string id)
        {
            return await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.Id == id)
                .Select(x => new LeaveApplicationDto
                {
                    Id = x.Id,

                    CompanyId = x.CompanyId,
                    BranchId = x.BranchId,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

                    FromDate = x.FromDate,
                    ToDate = x.ToDate,
                    TotalDays = x.TotalDays,

                    IsHalfDay = x.IsHalfDay,
                    HalfDayType = x.HalfDayType,

                    Reason = x.Reason,

                    Status = x.Status,

                    ApprovedBy = x.ApprovedBy,
                    ApprovedDate = x.ApprovedDate,

                    RejectedReason = x.RejectedReason,

                    DocumentUrl = x.DocumentUrl
                })
                .FirstOrDefaultAsync();
        }

        // =====================================================
        // GET BY EMPLOYEE
        // =====================================================

        public async Task<List<LeaveApplicationDto>> GetByEmployeeAsync(string employeeId)
        {
            return await _context.LeaveApplications
                .Include(x => x.LeaveType)
                .Where(x => x.EmployeeId == employeeId)
                .OrderByDescending(x => x.FromDate)
                .Select(x => new LeaveApplicationDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

                    FromDate = x.FromDate,
                    ToDate = x.ToDate,
                    TotalDays = x.TotalDays,

                    Status = x.Status,

                    Reason = x.Reason,

                    ApprovedBy = x.ApprovedBy,
                    ApprovedDate = x.ApprovedDate,

                    RejectedReason = x.RejectedReason
                })
                .ToListAsync();
        }


        public async Task<LeaveApplicationDto> ApplyLeaveAsync(
        LeaveApplicationDto dto)
        {
            var leaveBalance =
                await _leaveBalanceService
                    .GetEmployeeLeaveBalanceAsync(
                        dto.EmployeeId,
                        dto.LeaveTypeId,
                        dto.FromDate.Year);

            if (leaveBalance == null)
                throw new Exception("Leave balance not found.");

            if (leaveBalance.Balance < dto.TotalDays)
                throw new Exception("Insufficient leave balance.");

            var entity = new LeaveApplication
            {
                CompanyId = dto.CompanyId,
                BranchId = dto.BranchId,

                EmployeeId = dto.EmployeeId,
                LeaveTypeId = dto.LeaveTypeId,

                FromDate = dto.FromDate,
                ToDate = dto.ToDate,

                TotalDays = dto.TotalDays,

                IsHalfDay = dto.IsHalfDay,
                HalfDayType = dto.HalfDayType,

                Reason = dto.Reason,

                Status = ApprovalStatus.Pending,

                DocumentUrl = dto.DocumentUrl,

                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow
            };

            _context.LeaveApplications.Add(entity);

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
        }

        public async Task<bool> ApproveLeaveAsync(
        string leaveApplicationId,
        string approvedBy)
        {
            var leave =
                await _context.LeaveApplications
                    .FirstOrDefaultAsync(x => x.Id == leaveApplicationId);

            if (leave == null)
                return false;

            if (leave.Status != ApprovalStatus.Pending)
                throw new Exception("Leave already processed.");

            await _leaveBalanceService.DeductLeaveAsync(
                leave.EmployeeId,
                leave.LeaveTypeId,
                leave.TotalDays);

            leave.Status = ApprovalStatus.Approved;
            leave.ApprovedBy = approvedBy;
            leave.ApprovedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RejectLeaveAsync(
        string leaveApplicationId,
        string approvedBy,
        string rejectionReason)
        {
            var leave =
                await _context.LeaveApplications
                    .FirstOrDefaultAsync(x => x.Id == leaveApplicationId);

            if (leave == null)
                return false;

            leave.Status = ApprovalStatus.Rejected;
            leave.ApprovedBy = approvedBy;
            leave.ApprovedDate = DateTime.UtcNow;
            leave.RejectedReason = rejectionReason;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CancelLeaveAsync(
        string leaveApplicationId)
        {
            var leave =
                await _context.LeaveApplications
                    .FirstOrDefaultAsync(x => x.Id == leaveApplicationId);

            if (leave == null)
                return false;

            if (leave.Status == ApprovalStatus.Approved)
            {
                await _leaveBalanceService.CreditLeaveAsync(
                    leave.EmployeeId,
                    leave.LeaveTypeId,
                    leave.TotalDays);
            }

            leave.Status = ApprovalStatus.Cancelled;

            await _context.SaveChangesAsync();

            return true;
        }

        // =====================================================
        // DELETE
        // =====================================================
        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _context.LeaveApplications
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            if (entity.Status == ApprovalStatus.Approved)
            {
                await _leaveBalanceService.CreditLeaveAsync(
                    entity.EmployeeId,
                    entity.LeaveTypeId,
                    entity.TotalDays);
            }

            _context.LeaveApplications.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
        }

        // =====================================================
        // PENDING APPROVALS
        // =====================================================

        public async Task<List<LeaveApplicationDto>> GetPendingApprovalsAsync()
        {
            return await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.Status == ApprovalStatus.Pending)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new LeaveApplicationDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

                    FromDate = x.FromDate,
                    ToDate = x.ToDate,

                    TotalDays = x.TotalDays,

                    Reason = x.Reason,

                    Status = x.Status,

                    CreatedBy = x.CreatedBy
                })
                .ToListAsync();
        }

        // =====================================================
        // APPROVED LEAVES
        // =====================================================

        public async Task<List<LeaveApplicationDto>> GetApprovedLeavesAsync()
        {
            return await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.Status == ApprovalStatus.Approved)
                .OrderByDescending(x => x.ApprovedDate)
                .Select(x => new LeaveApplicationDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

                    FromDate = x.FromDate,
                    ToDate = x.ToDate,

                    TotalDays = x.TotalDays,

                    Status = x.Status,

                    ApprovedBy = x.ApprovedBy,
                    ApprovedDate = x.ApprovedDate
                })
                .ToListAsync();
        }

        // =====================================================
        // REJECTED LEAVES
        // =====================================================

        public async Task<List<LeaveApplicationDto>> GetRejectedLeavesAsync()
        {
            return await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.Status == ApprovalStatus.Rejected)
                .OrderByDescending(x => x.ApprovedDate)
                .Select(x => new LeaveApplicationDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

                    FromDate = x.FromDate,
                    ToDate = x.ToDate,

                    TotalDays = x.TotalDays,

                    Status = x.Status,

                    RejectedReason = x.RejectedReason,

                    ApprovedBy = x.ApprovedBy,
                    ApprovedDate = x.ApprovedDate
                })
                .ToListAsync();
        }

        // =====================================================
        // DATE RANGE
        // =====================================================

        public async Task<List<LeaveApplicationDto>> GetByDateRangeAsync(
            DateTime fromDate,
            DateTime toDate)
        {
            return await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x =>
                    x.FromDate.Date >= fromDate.Date &&
                    x.ToDate.Date <= toDate.Date)
                .OrderBy(x => x.FromDate)
                .Select(x => new LeaveApplicationDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

                    FromDate = x.FromDate,
                    ToDate = x.ToDate,

                    TotalDays = x.TotalDays,

                    Status = x.Status,

                    Reason = x.Reason
                })
                .ToListAsync();
        }

        // =====================================================
        // PENDING COUNT
        // =====================================================

        public async Task<int> GetPendingLeaveCountAsync()
        {
            return await _context.LeaveApplications
                .CountAsync(x =>
                    x.Status == ApprovalStatus.Pending);
        }

        // =====================================================
        // APPROVED COUNT
        // =====================================================

        public async Task<int> GetApprovedLeaveCountAsync()
        {
            return await _context.LeaveApplications
                .CountAsync(x =>
                    x.Status == ApprovalStatus.Approved);
        }

        // =====================================================
        // TODAY LEAVE COUNT
        // =====================================================

        public async Task<int> GetTodayLeaveCountAsync()
        {
            var today = DateTime.Today;

            return await _context.LeaveApplications
                .CountAsync(x =>
                    x.Status == ApprovalStatus.Approved &&
                    x.FromDate.Date <= today &&
                    x.ToDate.Date >= today);
        }
    }
}
