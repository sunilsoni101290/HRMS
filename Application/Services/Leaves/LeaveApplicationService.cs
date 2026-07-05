using Application.DTOs.Leaves;
using Application.Interfaces.Leaves;
using Application.Interfaces.Masters;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Leaves
{
    public class LeaveApplicationService : ILeaveApplicationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWeekOffService _weekOffService;
        private readonly ITenantService _tenantService;
        private readonly ILeaveBalanceService _leaveBalanceService;
        private string tenantId = string.Empty;
        public LeaveApplicationService(ApplicationDbContext context, IWeekOffService weekOffService, 
            ITenantService tenantService, ILeaveBalanceService leaveBalanceService)
        {
            _context = context;
            _tenantService = tenantService;
            _weekOffService = weekOffService;
            _leaveBalanceService = leaveBalanceService;
        }

        #region CRUD

        public async Task<LeaveApplicationDto> CreateAsync(ApplyLeaveRequestDto request)
        {
            try
            {
            decimal totalDays = request.IsHalfDay
                ? 0.5m
                : (decimal)((request.ToDate.Date - request.FromDate.Date).Days + 1);

            var entity = new LeaveApplication
            {
                Id=IDManager.GetNewId(new LeaveApplication()),
                CompanyId = request.CompanyId,
                BranchId = request.BranchId,

                EmployeeId = request.EmployeeId,
                LeaveTypeId = request.LeaveTypeId,

                FromDate = request.FromDate,
                ToDate = request.ToDate,

                TotalDays = totalDays,

                IsHalfDay = request.IsHalfDay,
                HalfDayType = request.HalfDayType,

                Reason = request.Reason,

                Status = ApprovalStatus.Pending,

                DocumentUrl = request.DocumentUrl,

                CreatedOn = DateTime.UtcNow,
                CreatedBy = request.CreatedBy
            };

            _context.LeaveApplications.Add(entity);

            await _context.SaveChangesAsync();

            await CreateApprovalHistoryAsync(entity.Id,request.CreatedBy, ApprovalStatus.Pending,"Leave Applied");

            return await GetByIdAsync(entity.Id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<LeaveApplicationDto> UpdateAsync(string id,ApplyLeaveRequestDto request)
        {
            try
            {
            var entity = await _context.LeaveApplications
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new Exception("Leave application not found.");

            if (entity.Status == ApprovalStatus.Approved)
                throw new Exception("Approved leave cannot be modified.");

            decimal totalDays = request.IsHalfDay
                ? 0.5m
                : (decimal)((request.ToDate.Date - request.FromDate.Date).Days + 1);

            entity.CompanyId = request.CompanyId;
            entity.BranchId = request.BranchId;

            entity.EmployeeId = request.EmployeeId;
            entity.LeaveTypeId = request.LeaveTypeId;

            entity.FromDate = request.FromDate;
            entity.ToDate = request.ToDate;

            entity.TotalDays = totalDays;

            entity.IsHalfDay = request.IsHalfDay;
            entity.HalfDayType = request.HalfDayType;

            entity.Reason = request.Reason;

            entity.DocumentUrl = request.DocumentUrl;

            entity.ModifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetByIdAsync(entity.Id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
            var entity = await _context.LeaveApplications
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.LeaveApplications.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<LeaveApplicationDto> GetByIdAsync(string id)
        {
            try
            {
            var data = await _context.LeaveApplications
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

                    DocumentUrl = x.DocumentUrl,

                    CreatedOn = x.CreatedOn,
                    CreatedBy = x.CreatedBy,

                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .FirstOrDefaultAsync();

            if (data == null)
                throw new Exception("Leave application not found.");

            return data;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<LeaveApplicationDto>> GetAllAsync()
        {
            try
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
                    EmployeeName = x.Employee.FirstName+" "+ x.Employee.LastName,

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

                    CreatedOn = x.CreatedOn,
                    CreatedBy = x.CreatedBy,

                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<LeaveApplicationDto>();
            }
        }

        #endregion

        #region Workflow

        public async Task<bool> ApplyLeaveAsync(ApplyLeaveRequestDto request)
        {
            try
            {
            decimal totalDays = request.IsHalfDay
                ? 0.5m
                : (decimal)((request.ToDate.Date - request.FromDate.Date).Days + 1);

            // Check Leave Balance
            var leaveBalance =
                await _leaveBalanceService.GetEmployeeLeaveBalanceAsync(
                    request.EmployeeId,
                    request.LeaveTypeId,
                    request.FromDate.Year);

            if (leaveBalance == null)
                throw new Exception("Leave balance not found.");

            if (leaveBalance.Balance < totalDays)
                throw new Exception("Insufficient leave balance.");

            // Check overlapping leave
            bool overlapExists =
                await _context.LeaveApplications.AnyAsync(x =>
                    x.EmployeeId == request.EmployeeId &&
                    x.Status != ApprovalStatus.Rejected &&
                    x.Status != ApprovalStatus.Cancelled &&
                    request.FromDate <= x.ToDate &&
                    request.ToDate >= x.FromDate);

            if (overlapExists)
                throw new Exception(
                    "Leave already applied for selected dates.");

            var entity = new LeaveApplication
            {
                Id = IDManager.GetNewId(new LeaveApplication()),
                CompanyId = request.CompanyId,
                BranchId = request.BranchId,

                EmployeeId = request.EmployeeId,
                LeaveTypeId = request.LeaveTypeId,

                FromDate = request.FromDate,
                ToDate = request.ToDate,

                TotalDays = totalDays,

                IsHalfDay = request.IsHalfDay,
                HalfDayType = request.HalfDayType,

                Reason = request.Reason,

                Status = ApprovalStatus.Pending,

                DocumentUrl = request.DocumentUrl,

                CreatedOn = DateTime.UtcNow,
                CreatedBy = request.CreatedBy
            };

            _context.LeaveApplications.Add(entity);

            await _context.SaveChangesAsync();

            await CreateApprovalHistoryAsync(entity.Id, request.CreatedBy, ApprovalStatus.Pending, "Leave Applied");

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> ApproveLeaveAsync(ApproveLeaveRequestDto request)
        {
            try
            {
            var leave =
                await _context.LeaveApplications
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.LeaveApplicationId);

            if (leave == null)
                throw new Exception("Leave application not found.");

            if (leave.Status != ApprovalStatus.Pending)
                throw new Exception(
                    "Only pending leave can be approved.");

            var deductLeaveReq = new LeaveAdjustmentRequestDto()
            {
                EmployeeId = leave.EmployeeId,
                LeaveTypeId = leave.LeaveTypeId,
                Days = leave.TotalDays
            };

            await _leaveBalanceService.DeductLeaveAsync(deductLeaveReq);

            leave.Status = ApprovalStatus.Approved;

            leave.ApprovedBy = request.ApprovedBy;
            leave.ApprovedDate = DateTime.UtcNow;

            leave.ModifiedOn = DateTime.UtcNow;
            leave.ModifiedBy = request.ApprovedBy;

            await _context.SaveChangesAsync();

            await CreateApprovalHistoryAsync(request.LeaveApplicationId, request.ApprovedBy, ApprovalStatus.Approved, request.Remarks);

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> RejectLeaveAsync(RejectLeaveRequestDto request)
        {
            try
            {
            var leave =
                await _context.LeaveApplications
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.LeaveApplicationId);

            if (leave == null)
                throw new Exception("Leave application not found.");

            if (leave.Status != ApprovalStatus.Pending)
                throw new Exception(
                    "Only pending leave can be rejected.");

            leave.Status = ApprovalStatus.Rejected;

            leave.ApprovedBy = request.RejectedBy;
            leave.ApprovedDate = DateTime.UtcNow;

            leave.RejectedReason = request.RejectedReason;

            leave.ModifiedOn = DateTime.UtcNow;
            leave.ModifiedBy = request.RejectedBy;

            await _context.SaveChangesAsync();

            await CreateApprovalHistoryAsync(request.LeaveApplicationId, request.RejectedBy, ApprovalStatus.Rejected, request.RejectedReason);

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CancelLeaveAsync(CancelLeaveRequestDto request)
        {
            try
            {
            var leave =
                await _context.LeaveApplications
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.LeaveApplicationId);

            if (leave == null)
                throw new Exception("Leave application not found.");

            if (leave.Status == ApprovalStatus.Cancelled)
                throw new Exception("Leave already cancelled.");

            if (leave.Status == ApprovalStatus.Approved)
            {
                var creditLeaveReq = new LeaveAdjustmentRequestDto()
                {
                    EmployeeId= leave.EmployeeId,
                    LeaveTypeId= leave.LeaveTypeId,
                    Days= leave.TotalDays
                };

                await _leaveBalanceService.CreditLeaveAsync(creditLeaveReq);
            }

            leave.Status = ApprovalStatus.Cancelled;

            leave.ModifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await CreateApprovalHistoryAsync(request.LeaveApplicationId, request.CancelledBy, ApprovalStatus.Cancelled, "Leave Cancelled");

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Queries

        public async Task<List<LeaveApplicationDto>>GetEmployeeLeavesAsync(string employeeId)
        {
            return await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.EmployeeId == employeeId)
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

                    CreatedOn = x.CreatedOn
                })
                .ToListAsync();
        }

        public async Task<List<LeaveApplicationDto>>GetPendingLeavesAsync()
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

                    Status = x.Status,

                    Reason = x.Reason,

                    CreatedOn = x.CreatedOn
                })
                .ToListAsync();
        }

        public async Task<List<LeaveApplicationDto>>GetApprovedLeavesAsync()
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
                    ApprovedDate = x.ApprovedDate,

                    CreatedOn = x.CreatedOn
                })
                .ToListAsync();
        }

        public async Task<List<LeaveApplicationDto>>GetRejectedLeavesAsync()
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

        public async Task<List<LeaveApplicationDto>>GetCancelledLeavesAsync()
        {
            return await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.Status == ApprovalStatus.Cancelled)
                .OrderByDescending(x => x.ModifiedOn)
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

                    CreatedOn = x.CreatedOn,

                    ModifiedOn = x.ModifiedOn
                })
                .ToListAsync();
        }

        public async Task<List<LeaveApplicationDto>>GetFilteredAsync(LeaveApplicationFilterRequestDto request)
        {
            var query = _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.EmployeeId))
            {
                query = query.Where(x =>
                    x.EmployeeId == request.EmployeeId);
            }

            if (!string.IsNullOrWhiteSpace(request.LeaveTypeId))
            {
                query = query.Where(x =>
                    x.LeaveTypeId == request.LeaveTypeId);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(x =>
                    x.Status == request.Status.Value);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(x =>
                    x.FromDate.Date >= request.FromDate.Value.Date);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(x =>
                    x.ToDate.Date <= request.ToDate.Value.Date);
            }

            return await query
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new LeaveApplicationDto
                {
                    Id = x.Id,

                    CompanyId = x.CompanyId,
                    BranchId = x.BranchId,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName+" "+ x.Employee.LastName,

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

                    CreatedOn = x.CreatedOn
                })
                .ToListAsync();
        }

        #endregion

        #region Count

        public async Task<int> GetPendingLeaveCountAsync()
        {
            try
            {
            return await _context.LeaveApplications
                .CountAsync(x =>
                    x.Status == ApprovalStatus.Pending);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetApprovedLeaveCountAsync()
        {
            try
            {
            return await _context.LeaveApplications
                .CountAsync(x =>
                    x.Status == ApprovalStatus.Approved);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> GetTodayLeaveCountAsync()
        {
            try
            {
            var today = DateTime.Today;

            return await _context.LeaveApplications
                .CountAsync(x =>
                    x.Status == ApprovalStatus.Approved &&
                    x.FromDate.Date <= today &&
                    x.ToDate.Date >= today);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        #endregion

        #region Approval History
        public async Task<List<LeaveApprovalHistoryDetailDto>>GetApprovalHistoryAsync(string leaveApplicationId)
        {
            return await _context.LeaveApprovalHistories
                .Include(x => x.LeaveApplication)
                    .ThenInclude(x => x.Employee)
                .Include(x => x.LeaveApplication)
                    .ThenInclude(x => x.LeaveType)
                .Where(x => x.LeaveApplicationId == leaveApplicationId)
                .OrderBy(x => x.ActionDate)
                .Select(x => new LeaveApprovalHistoryDetailDto
                {
                    Id = x.Id,

                    LeaveApplicationId = x.LeaveApplicationId,

                    LeaveApplicationNo = x.LeaveApplication.Id,

                    EmployeeId = x.LeaveApplication.EmployeeId,

                    EmployeeName =
                        x.LeaveApplication.Employee.FirstName +
                        " " +
                        x.LeaveApplication.Employee.LastName,

                    LeaveTypeId = x.LeaveApplication.LeaveTypeId,

                    LeaveTypeName =
                        x.LeaveApplication.LeaveType.Name,

                    FromDate = x.LeaveApplication.FromDate,

                    ToDate = x.LeaveApplication.ToDate,

                    TotalDays = x.LeaveApplication.TotalDays,

                    ActionBy = x.ActionBy,

                    Action = x.Action,

                    Remarks = x.Remarks,

                    ActionDate = x.ActionDate,

                    CreatedOn = x.CreatedOn,

                    CreatedBy = x.CreatedBy
                })
                .ToListAsync();
        }

        public async Task<List<LeaveApprovalHistoryDetailDto>> GetAllApprovalHistoryAsync()
        {
            try
            {
            var histories = await _context.LeaveApprovalHistories
                .Include(x => x.LeaveApplication)
                    .ThenInclude(x => x.Employee)
                .Include(x => x.LeaveApplication)
                    .ThenInclude(x => x.LeaveType)
                .OrderByDescending(x => x.ActionDate)
                .ToListAsync();

            var userIds = histories
                .Select(x => x.ActionBy)
                .Distinct()
                .ToList();

            var users = await _context.Users
                .Include(x => x.Employee)
                .Where(x => userIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            return histories.Select(x => new LeaveApprovalHistoryDetailDto
            {
                Id = x.Id,

                LeaveApplicationId = x.LeaveApplicationId,

                LeaveApplicationNo = x.LeaveApplication.Id,

                EmployeeId = x.LeaveApplication.EmployeeId,

                EmployeeName =
                    x.LeaveApplication.Employee.FirstName + " " +
                    x.LeaveApplication.Employee.LastName,

                LeaveTypeId = x.LeaveApplication.LeaveTypeId,

                LeaveTypeName = x.LeaveApplication.LeaveType.Name,

                FromDate = x.LeaveApplication.FromDate,

                ToDate = x.LeaveApplication.ToDate,

                TotalDays = x.LeaveApplication.TotalDays,

                ActionBy = users.ContainsKey(x.ActionBy)
                    ? users[x.ActionBy].Employee.FirstName + " " +
                      users[x.ActionBy].Employee.LastName
                    : "",

                Action = x.Action,

                Remarks = x.Remarks,

                ActionDate = x.ActionDate,

                CreatedOn = x.CreatedOn,

                CreatedBy = x.CreatedBy

            }).ToList();
            }
            catch (Exception)
            {
                return new List<LeaveApprovalHistoryDetailDto>();
            }
        }

        public async Task<LeaveApprovalHistoryDetailDto>GetApprovalHistoryByIdAsync(string id)
        {
            var data =
                await _context.LeaveApprovalHistories
                .Include(x => x.LeaveApplication)
                    .ThenInclude(x => x.Employee)
                .Include(x => x.LeaveApplication)
                    .ThenInclude(x => x.LeaveType)
                .Where(x => x.Id == id)
                .Select(x => new LeaveApprovalHistoryDetailDto
                {
                    Id = x.Id,

                    LeaveApplicationId =
                        x.LeaveApplicationId,

                    LeaveApplicationNo =
                        x.LeaveApplication.Id,

                    EmployeeId =
                        x.LeaveApplication.EmployeeId,

                    EmployeeName =
                        x.LeaveApplication.Employee.FirstName +
                        " " +
                        x.LeaveApplication.Employee.LastName,

                    LeaveTypeId =
                        x.LeaveApplication.LeaveTypeId,

                    LeaveTypeName =
                        x.LeaveApplication.LeaveType.Name,

                    FromDate =
                        x.LeaveApplication.FromDate,

                    ToDate =
                        x.LeaveApplication.ToDate,

                    TotalDays =
                        x.LeaveApplication.TotalDays,

                    ActionBy =
                        x.ActionBy,

                    Action =
                        x.Action,

                    Remarks =
                        x.Remarks,

                    ActionDate =
                        x.ActionDate,

                    CreatedOn =
                        x.CreatedOn,

                    CreatedBy =
                        x.CreatedBy
                })
                .FirstOrDefaultAsync();

            if (data == null)
                throw new Exception(
                    "Approval history not found.");

            return data;
        }

        private async Task CreateApprovalHistoryAsync(string leaveApplicationId,string actionBy, ApprovalStatus action,string? remarks)
        {
            var history = new LeaveApprovalHistory
            {
                Id = IDManager.GetNewId(new LeaveApprovalHistory()),

                LeaveApplicationId = leaveApplicationId,

                ActionBy = actionBy,

                Action = action,

                Remarks = remarks,

                ActionDate = DateTime.UtcNow,

                CreatedOn = DateTime.UtcNow,
                CreatedBy = actionBy
            };

            _context.LeaveApprovalHistories.Add(history);

            await _context.SaveChangesAsync();
        }
        #endregion

    }

    /*
    public class LeaveApplicationService : ILeaveApplicationService
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
            try
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
            catch (Exception)
            {
                return new List<LeaveApplicationDto>();
            }
        }

        // =====================================================
        // GET BY ID
        // =====================================================

        public async Task<LeaveApplicationDto?> GetByIdAsync(string id)
        {
            try
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
            catch (Exception)
            {
                return null;
            }
        }

        // =====================================================
        // GET BY EMPLOYEE
        // =====================================================

        public async Task<List<LeaveApplicationDto>> GetByEmployeeAsync(string employeeId)
        {
            try
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
            catch (Exception)
            {
                return new List<LeaveApplicationDto>();
            }
        }


        public async Task<LeaveApplicationDto> ApplyLeaveAsync(
        LeaveApplicationDto dto)
        {
            try
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
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> ApproveLeaveAsync(
        string leaveApplicationId,
        string approvedBy)
        {
            try
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
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> RejectLeaveAsync(
        string leaveApplicationId,
        string approvedBy,
        string rejectionReason)
        {
            try
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
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CancelLeaveAsync(
        string leaveApplicationId)
        {
            try
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
            catch (Exception)
            {
                return false;
            }
        }

        // =====================================================
        // DELETE
        // =====================================================
        public async Task<bool> DeleteAsync(string id)
        {
            try
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
            catch (Exception)
            {
                return false;
            }
        }

        // =====================================================
        // PENDING APPROVALS
        // =====================================================

        public async Task<List<LeaveApplicationDto>> GetPendingApprovalsAsync()
        {
            try
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
            catch (Exception)
            {
                return new List<LeaveApplicationDto>();
            }
        }

        // =====================================================
        // APPROVED LEAVES
        // =====================================================

        public async Task<List<LeaveApplicationDto>> GetApprovedLeavesAsync()
        {
            try
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
            catch (Exception)
            {
                return new List<LeaveApplicationDto>();
            }
        }

        // =====================================================
        // REJECTED LEAVES
        // =====================================================

        public async Task<List<LeaveApplicationDto>> GetRejectedLeavesAsync()
        {
            try
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
            catch (Exception)
            {
                return new List<LeaveApplicationDto>();
            }
        }

        // =====================================================
        // DATE RANGE
        // =====================================================

        public async Task<List<LeaveApplicationDto>> GetByDateRangeAsync(
            DateTime fromDate,
            DateTime toDate)
        {
            try
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
            catch (Exception)
            {
                return new List<LeaveApplicationDto>();
            }
        }

        // =====================================================
        // PENDING COUNT
        // =====================================================

        public async Task<int> GetPendingLeaveCountAsync()
        {
            try
            {
            return await _context.LeaveApplications
                .CountAsync(x =>
                    x.Status == ApprovalStatus.Pending);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // =====================================================
        // APPROVED COUNT
        // =====================================================

        public async Task<int> GetApprovedLeaveCountAsync()
        {
            try
            {
            return await _context.LeaveApplications
                .CountAsync(x =>
                    x.Status == ApprovalStatus.Approved);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // =====================================================
        // TODAY LEAVE COUNT
        // =====================================================

        public async Task<int> GetTodayLeaveCountAsync()
        {
            try
            {
            var today = DateTime.Today;

            return await _context.LeaveApplications
                .CountAsync(x =>
                    x.Status == ApprovalStatus.Approved &&
                    x.FromDate.Date <= today &&
                    x.ToDate.Date >= today);
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
    */
}
