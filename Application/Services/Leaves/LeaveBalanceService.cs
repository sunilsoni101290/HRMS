using Application.DTOs.Leaves;
using Application.Interfaces.Leaves;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Leaves
{

    public class LeaveBalanceService : ILeaveBalanceService
    {
        private readonly ApplicationDbContext _context;

        public LeaveBalanceService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        

        #region CRUD

        public async Task<List<LeaveBalanceDto>> GetAllAsync()
        {
            try
            {
            return await _context.LeaveBalances
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Select(x => new LeaveBalanceDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

                    Year = x.Year,

                    OpeningBalance = x.OpeningBalance,
                    Allocated = x.Allocated,
                    Credited = x.Credited,
                    CarryForward = x.CarryForward,
                    Used = x.Used,
                    Balance = x.Balance,

                    TenantId = x.TenantId,

                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .OrderByDescending(x => x.Year)
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<LeaveBalanceDto>();
            }
        }

        public async Task<LeaveBalanceDto?> GetByIdAsync(string id)
        {
            try
            {
            return await _context.LeaveBalances
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.Id == id)
                .Select(x => new LeaveBalanceDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

                    Year = x.Year,

                    OpeningBalance = x.OpeningBalance,
                    Allocated = x.Allocated,
                    Credited = x.Credited,
                    CarryForward = x.CarryForward,
                    Used = x.Used,
                    Balance = x.Balance,

                    TenantId = x.TenantId,

                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<LeaveBalanceDto>>
            GetByEmployeeAsync(string employeeId)
        {
            try
            {
            return await _context.LeaveBalances
                .Include(x => x.LeaveType)
                .Where(x => x.EmployeeId == employeeId)
                .Select(x => new LeaveBalanceDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

                    Year = x.Year,

                    OpeningBalance = x.OpeningBalance,
                    Allocated = x.Allocated,
                    Credited = x.Credited,
                    CarryForward = x.CarryForward,
                    Used = x.Used,
                    Balance = x.Balance
                })
                .OrderByDescending(x => x.Year)
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<LeaveBalanceDto>();
            }
        }

        public async Task<LeaveBalanceDto?>
            GetEmployeeLeaveBalanceAsync(
                string employeeId,
                string leaveTypeId,
                int year)
        {
            try
            {
            return await _context.LeaveBalances
                .Where(x =>
                    x.EmployeeId == employeeId &&
                    x.LeaveTypeId == leaveTypeId &&
                    x.Year == year)
                .Select(x => new LeaveBalanceDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,
                    LeaveTypeId = x.LeaveTypeId,

                    Year = x.Year,

                    OpeningBalance = x.OpeningBalance,
                    Allocated = x.Allocated,
                    Credited = x.Credited,
                    CarryForward = x.CarryForward,
                    Used = x.Used,
                    Balance = x.Balance
                })
                .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<LeaveBalanceDto>
            CreateAsync(LeaveBalanceDto dto)
        {
            try
            {
            bool exists = await _context.LeaveBalances
                .AnyAsync(x =>
                    x.EmployeeId == dto.EmployeeId &&
                    x.LeaveTypeId == dto.LeaveTypeId &&
                    x.Year == dto.Year);

            if (exists)
                throw new Exception(
                    "Leave balance already exists.");

            var entity = new LeaveBalance
            {
                Id=IDManager.GetNewId(new LeaveBalance()),
                EmployeeId = dto.EmployeeId,
                LeaveTypeId = dto.LeaveTypeId,

                Year = dto.Year,

                OpeningBalance = dto.OpeningBalance,
                Allocated = dto.Allocated,
                Credited = dto.Credited,
                CarryForward = dto.CarryForward,
                Used = dto.Used,

                Balance = CalculateBalance(
                    dto.OpeningBalance,
                    dto.Allocated,
                    dto.Credited,
                    dto.CarryForward,
                    dto.Used),

                TenantId = dto.TenantId,

                CreatedBy = dto.CreatedBy
            };

            _context.LeaveBalances.Add(entity);

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<LeaveBalanceDto?>
            UpdateAsync(
                string id,
                LeaveBalanceDto dto)
        {
            try
            {
            var entity = await _context.LeaveBalances
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            entity.OpeningBalance = dto.OpeningBalance;
            entity.Allocated = dto.Allocated;
            entity.Credited = dto.Credited;
            entity.CarryForward = dto.CarryForward;
            entity.Used = dto.Used;

            entity.Balance = CalculateBalance(
                dto.OpeningBalance,
                dto.Allocated,
                dto.Credited,
                dto.CarryForward,
                dto.Used);

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = dto.ModifiedBy;

            await _context.SaveChangesAsync();

            return dto;
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
            var entity = await _context.LeaveBalances
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.LeaveBalances.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Leave Operations

        // =====================================================
        // ALLOCATE LEAVE
        // =====================================================

        public async Task<bool> AllocateLeaveAsync(
            AllocateLeaveRequestDto request)
        {
            try
            {
            var leaveTypes = await _context.LeaveTypes
                .ToListAsync();

            foreach (var leaveType in leaveTypes)
            {
                bool exists = await _context.LeaveBalances
                    .AnyAsync(x =>
                        x.EmployeeId == request.EmployeeId &&
                        x.LeaveTypeId == leaveType.Id &&
                        x.Year == request.Year);

                if (exists)
                    continue;

                var balance = new LeaveBalance
                {
                    Id=IDManager.GetNewId(new LeaveBalance()),
                    EmployeeId = request.EmployeeId,
                    LeaveTypeId = leaveType.Id,

                    Year = request.Year,

                    OpeningBalance = 0,
                    Allocated = leaveType.MaxDaysPerYear,
                    Credited = 0,
                    CarryForward = 0,
                    Used = 0,

                    Balance = leaveType.MaxDaysPerYear,

                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = "System"
                };

                _context.LeaveBalances.Add(balance);

                await _context.SaveChangesAsync();

                await CreateTransactionAsync(
                    request.EmployeeId,
                    leaveType.Id,
                    request.Year,
                    LeaveTransactionType.Allocate,
                    leaveType.MaxDaysPerYear,
                    0,
                    leaveType.MaxDaysPerYear,
                    "Annual Leave Allocation");
            }

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =====================================================
        // CREDIT LEAVE
        // =====================================================

        public async Task<bool> CreditLeaveAsync(
            LeaveAdjustmentRequestDto request)
        {
            try
            {
            int year = DateTime.UtcNow.Year; // UTC-consistent with callers (e.g. LeaveAccrualService) to avoid a local-vs-UTC year mismatch right around Dec 31/Jan 1

            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == request.EmployeeId &&
                    x.LeaveTypeId == request.LeaveTypeId &&
                    x.Year == year);

            if (balance == null)
                throw new Exception("Leave balance not found.");

            decimal beforeBalance = balance.Balance;

            balance.Credited += request.Days;

            balance.Balance = CalculateBalance(
                balance.OpeningBalance,
                balance.Allocated,
                balance.Credited,
                balance.CarryForward,
                balance.Used);

            balance.ModifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await CreateTransactionAsync(
                request.EmployeeId,
                request.LeaveTypeId,
                year,
                LeaveTransactionType.Credit,
                request.Days,
                beforeBalance,
                balance.Balance,
                "Leave Credit");

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =====================================================
        // DEDUCT LEAVE
        // =====================================================

        public async Task<bool> DeductLeaveAsync(
            LeaveAdjustmentRequestDto request)
        {
            try
            {
            int year = DateTime.UtcNow.Year; // UTC-consistent with callers (e.g. LeaveAccrualService) to avoid a local-vs-UTC year mismatch right around Dec 31/Jan 1

            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == request.EmployeeId &&
                    x.LeaveTypeId == request.LeaveTypeId &&
                    x.Year == year);

            if (balance == null)
                throw new Exception("Leave balance not found.");

            if (balance.Balance < request.Days)
                throw new Exception("Insufficient leave balance.");

            decimal beforeBalance = balance.Balance;

            balance.Used += request.Days;

            balance.Balance = CalculateBalance(
                balance.OpeningBalance,
                balance.Allocated,
                balance.Credited,
                balance.CarryForward,
                balance.Used);

            balance.ModifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await CreateTransactionAsync(
                request.EmployeeId,
                request.LeaveTypeId,
                year,
                LeaveTransactionType.Deduct,
                request.Days,
                beforeBalance,
                balance.Balance,
                "Leave Approved");

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =====================================================
        // CARRY FORWARD LEAVE
        // =====================================================

        public async Task<bool> CarryForwardLeaveAsync(
            CarryForwardLeaveRequestDto request)
        {
            try
            {
            var balances = await _context.LeaveBalances
                .Include(x => x.LeaveType)
                .Where(x =>
                    x.EmployeeId == request.EmployeeId &&
                    x.Year == request.FromYear)
                .ToListAsync();

            foreach (var item in balances)
            {
                if (!item.LeaveType.AllowCarryForward)
                    continue;

                decimal carryForward = item.Balance;

                if (item.LeaveType.MaxCarryForwardDays.HasValue)
                {
                    carryForward = Math.Min(
                        carryForward,
                        item.LeaveType.MaxCarryForwardDays.Value);
                }

                bool exists = await _context.LeaveBalances
                    .AnyAsync(x =>
                        x.EmployeeId == request.EmployeeId &&
                        x.LeaveTypeId == item.LeaveTypeId &&
                        x.Year == request.ToYear);

                if (exists)
                    continue;

                var newBalance = new LeaveBalance
                {
                    EmployeeId = request.EmployeeId,
                    LeaveTypeId = item.LeaveTypeId,

                    Year = request.ToYear,

                    OpeningBalance = 0,
                    Allocated = 0,
                    Credited = 0,
                    Used = 0,

                    CarryForward = carryForward,

                    Balance = carryForward,

                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = "System"
                };

                _context.LeaveBalances.Add(newBalance);

                await _context.SaveChangesAsync();

                await CreateTransactionAsync(
                    request.EmployeeId,
                    item.LeaveTypeId,
                    request.ToYear,
                    LeaveTransactionType.CarryForward,
                    carryForward,
                    0,
                    carryForward,
                    $"Carry Forward From {request.FromYear}");
            }

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Transactions

        #region Transactions

        public async Task<List<LeaveBalanceTransactionDto>> GetTransactionsAsync(
            LeaveTransactionFilterRequestDto request)
        {
            try
            {
            return await _context.LeaveBalanceTransactions
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x =>
                    x.EmployeeId == request.EmployeeId &&
                    x.LeaveTypeId == request.LeaveTypeId &&
                    x.Year == request.Year)
                .OrderByDescending(x => x.TransactionDate)
                .Select(x => new LeaveBalanceTransactionDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

                    Year = x.Year,

                    TransactionType = x.TransactionType,

                    Quantity = x.Quantity,

                    BalanceBefore = x.BalanceBefore,
                    BalanceAfter = x.BalanceAfter,

                    Remarks = x.Remarks,

                    TransactionDate = x.TransactionDate,

                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<LeaveBalanceTransactionDto>();
            }
        }

        public async Task<List<LeaveBalanceTransactionDto>> GetEmployeeTransactionsAsync(
            EmployeeTransactionRequestDto request)
        {
            try
            {
            return await _context.LeaveBalanceTransactions
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.EmployeeId == request.EmployeeId && x.LeaveTypeId == request.LeaveTypeId)
                .OrderByDescending(x => x.TransactionDate)
                .Select(x => new LeaveBalanceTransactionDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

                    Year = x.Year,

                    TransactionType = x.TransactionType,

                    Quantity = x.Quantity,

                    BalanceBefore = x.BalanceBefore,
                    BalanceAfter = x.BalanceAfter,

                    Remarks = x.Remarks,

                    TransactionDate = x.TransactionDate,

                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<LeaveBalanceTransactionDto>();
            }
        }

        public async Task<List<LeaveBalanceTransactionDto>> GetTransactionsByDateRangeAsync(
            TransactionDateRangeRequestDto request)
        {
            try
            {
            return await _context.LeaveBalanceTransactions
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x =>
                    x.TransactionDate.Date >= request.FromDate &&
                    x.TransactionDate.Date <= request.ToDate)
                .OrderByDescending(x => x.TransactionDate)
                .Select(x => new LeaveBalanceTransactionDto
                {
                    Id = x.Id,

                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    LeaveTypeId = x.LeaveTypeId,
                    LeaveTypeName = x.LeaveType.Name,

                    Year = x.Year,

                    TransactionType = x.TransactionType,

                    Quantity = x.Quantity,

                    BalanceBefore = x.BalanceBefore,
                    BalanceAfter = x.BalanceAfter,

                    Remarks = x.Remarks,

                    TransactionDate = x.TransactionDate,

                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<LeaveBalanceTransactionDto>();
            }
        }

        #endregion

        #endregion

        #region Private Helper

        private decimal CalculateBalance(decimal opening,decimal allocated,decimal credited,decimal carryForward,decimal used)
        {
            return (opening + allocated + credited + carryForward) - used;
        }

        private async Task CreateTransactionAsync(string employeeId,string leaveTypeId,int year,LeaveTransactionType transactionType,
            decimal quantity,decimal beforeBalance,decimal afterBalance,string remarks)
        {
            var transaction = new LeaveBalanceTransaction
            {
                Id=IDManager.GetNewId(new LeaveBalanceTransaction()),
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                Year = year,

                TransactionType = transactionType,

                Quantity = quantity,

                BalanceBefore = beforeBalance,
                BalanceAfter = afterBalance,

                Remarks = remarks,

                TransactionDate = DateTime.UtcNow,

                CreatedOn = DateTime.UtcNow,
                CreatedBy = "System"
            };

            _context.LeaveBalanceTransactions.Add(transaction);

            await _context.SaveChangesAsync();
        }
        #endregion
    }
}
