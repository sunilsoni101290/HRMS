using Application.DTOs.Leaves;
using Application.Interfaces.Leaves;
using Domain.Entities;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Application.Services.Leaves
{
    public class LeaveBalanceService : ILeaveBalanceService
    {
        private readonly ApplicationDbContext _context;

        public LeaveBalanceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<LeaveBalanceDto>> GetAllAsync()
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
                    Earned = x.Earned,
                    Used = x.Used,
                    Balance = x.Balance,

                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .ToListAsync();
        }

        public async Task<List<LeaveBalanceDto>> GetByEmployeeAsync(string employeeId)
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
                    Earned = x.Earned,
                    Used = x.Used,
                    Balance = x.Balance
                })
                .ToListAsync();
        }

        public async Task<LeaveBalanceDto?> GetByIdAsync(string id)
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
                    Earned = x.Earned,
                    Used = x.Used,
                    Balance = x.Balance
                })
                .FirstOrDefaultAsync();
        }

        public async Task<LeaveBalanceDto> CreateAsync(LeaveBalanceDto dto)
        {
            var exists = await _context.LeaveBalances
                .AnyAsync(x =>
                    x.EmployeeId == dto.EmployeeId &&
                    x.LeaveTypeId == dto.LeaveTypeId &&
                    x.Year == dto.Year);

            if (exists)
                throw new Exception("Leave Balance already exists.");

            var entity = new LeaveBalance
            {
                Id=IDManager.GetNewId(new LeaveBalance()),
                EmployeeId = dto.EmployeeId,
                LeaveTypeId = dto.LeaveTypeId,

                Year = dto.Year,

                OpeningBalance = dto.OpeningBalance,
                Earned = dto.Earned,
                Used = dto.Used,

                Balance = dto.OpeningBalance +
                          dto.Earned -
                          dto.Used,

                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow
            };

            _context.LeaveBalances.Add(entity);

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
        }

        public async Task<LeaveBalanceDto?> UpdateAsync(string id, LeaveBalanceDto dto)
        {
            var entity = await _context.LeaveBalances
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            entity.OpeningBalance = dto.OpeningBalance;
            entity.Earned = dto.Earned;
            entity.Used = dto.Used;

            entity.Balance =
                dto.OpeningBalance +
                dto.Earned -
                dto.Used;

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = dto.ModifiedBy;

            await _context.SaveChangesAsync();

            return dto;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _context.LeaveBalances
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.LeaveBalances.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<LeaveBalanceDto?> GetEmployeeLeaveBalanceAsync(
            string employeeId,
            string leaveTypeId,
            int year)
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
                    Earned = x.Earned,
                    Used = x.Used,
                    Balance = x.Balance
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> AllocateLeaveAsync(string employeeId,int year)
        {
            var leaveTypes = await _context.LeaveTypes
                .ToListAsync();

            foreach (var leaveType in leaveTypes)
            {
                bool exists = await _context.LeaveBalances
                    .AnyAsync(x =>
                        x.EmployeeId == employeeId &&
                        x.LeaveTypeId == leaveType.Id &&
                        x.Year == year);

                if (exists)
                    continue;

                var balance = new LeaveBalance
                {
                    EmployeeId = employeeId,
                    LeaveTypeId = leaveType.Id,

                    Year = year,

                    OpeningBalance = leaveType.MaxDaysPerYear,
                    Earned = 0,
                    Used = 0,
                    Balance = leaveType.MaxDaysPerYear,

                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = "System"
                };

                _context.LeaveBalances.Add(balance);
            }

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeductLeaveAsync(
        string employeeId,
        string leaveTypeId,
        decimal days)
        {
            int year = DateTime.Now.Year;

            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.LeaveTypeId == leaveTypeId &&
                    x.Year == year);

            if (balance == null)
                throw new Exception("Leave balance not found.");

            if (balance.Balance < days)
                throw new Exception("Insufficient leave balance.");

            balance.Used += days;
            balance.Balance -= days;

            balance.ModifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CreditLeaveAsync(
        string employeeId,
        string leaveTypeId,
        decimal days)
        {
            int year = DateTime.Now.Year;

            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.LeaveTypeId == leaveTypeId &&
                    x.Year == year);

            if (balance == null)
                throw new Exception("Leave balance not found.");

            balance.Earned += days;
            balance.Balance += days;

            balance.ModifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CarryForwardLeaveAsync(
        string employeeId,
        int fromYear,
        int toYear)
        {
            var balances = await _context.LeaveBalances
                .Include(x => x.LeaveType)
                .Where(x =>
                    x.EmployeeId == employeeId &&
                    x.Year == fromYear)
                .ToListAsync();

            foreach (var balance in balances)
            {
                decimal carryForward = 0;

                if (balance.LeaveType.AllowCarryForward)
                {
                    carryForward = balance.Balance;

                    if (balance.LeaveType.MaxCarryForwardDays.HasValue)
                    {
                        carryForward = Math.Min(
                            carryForward,
                            balance.LeaveType.MaxCarryForwardDays.Value);
                    }
                }

                bool exists = await _context.LeaveBalances
                    .AnyAsync(x =>
                        x.EmployeeId == employeeId &&
                        x.LeaveTypeId == balance.LeaveTypeId &&
                        x.Year == toYear);

                if (exists)
                    continue;

                _context.LeaveBalances.Add(new LeaveBalance
                {
                    EmployeeId = employeeId,
                    LeaveTypeId = balance.LeaveTypeId,

                    Year = toYear,

                    OpeningBalance = carryForward,
                    Earned = 0,
                    Used = 0,
                    Balance = carryForward,

                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = "System"
                });
            }

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
