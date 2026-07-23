using Application.DTOs.Employee;
using Application.Interfaces.EmployeeInterface;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.EmployeeServices
{
    public class EmployeeBankDetailService : IEmployeeBankDetailService
    {
        private readonly ApplicationDbContext _context;

        // Standard Indian IFSC format: 4 letters (bank code) + '0' +
        // 6 alphanumeric characters (branch code) = 11 characters total.
        private static readonly Regex IfscRegex =
            new Regex(@"^[A-Z]{4}0[A-Z0-9]{6}$", RegexOptions.Compiled);

        public EmployeeBankDetailService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<EmployeeBankDetailDto>> GetAllAsync(string tenantId, string? search)
        {
            var query = _context.EmployeeBankDetails
                .AsNoTracking()
                .Include(x => x.Employee)
                .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(x =>
                    (x.Employee.FirstName + " " + x.Employee.LastName).ToLower().Contains(term) ||
                    (x.Employee.EmployeeCode ?? "").ToLower().Contains(term) ||
                    x.BankName.ToLower().Contains(term) ||
                    x.AccountNumber.ToLower().Contains(term) ||
                    x.IFSCCode.ToLower().Contains(term));
            }

            var entities = await query
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            return entities.Select(MapToDto).ToList();
        }

        #endregion

        #region Get By Id

        public async Task<EmployeeBankDetailDto> GetByIdAsync(string id, string tenantId)
        {
            var entity = await _context.EmployeeBankDetails
                .AsNoTracking()
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            return entity == null ? null : MapToDto(entity);
        }

        #endregion

        #region Get By Employee

        // Same ordering the payroll payslip lookup relies on
        // (PayrollBusinessService.GenerateAsync) - the primary account
        // always sorts first.
        public async Task<List<EmployeeBankDetailDto>> GetByEmployeeIdAsync(string employeeId, string tenantId)
        {
            var entities = await _context.EmployeeBankDetails
                .AsNoTracking()
                .Include(x => x.Employee)
                .Where(x => x.EmployeeId == employeeId && x.TenantId == tenantId && !x.IsDeleted)
                .OrderByDescending(x => x.IsPrimary)
                .ThenByDescending(x => x.CreatedOn)
                .ToListAsync();

            return entities.Select(MapToDto).ToList();
        }

        #endregion

        #region Create

        public async Task<EmployeeBankDetailDto> CreateAsync(EmployeeBankDetailDto dto, string tenantId, string actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            await ValidateAsync(dto, tenantId);

            var createdBy = string.IsNullOrWhiteSpace(actingUserId) ? "System" : actingUserId;

            var entity = new EmployeeBankDetail
            {
                Id = IDManager.GetNewId(new EmployeeBankDetail()),

                EmployeeId = dto.EmployeeId,
                BankName = dto.BankName,
                BranchName = dto.BranchName,
                AccountNumber = dto.AccountNumber,
                AccountHolderName = dto.AccountHolderName,
                IFSCCode = dto.IFSCCode.Trim().ToUpper(),
                MICRCode = dto.MICRCode,
                AccountType = dto.AccountType,
                CancelledChequeFilePath = dto.CancelledChequeFilePath,
                IsPrimary = dto.IsPrimary,
                Remarks = dto.Remarks,

                TenantId = tenantId,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            // Enforce "only one primary account per employee" - PayrollBusinessService
            // trusts IsPrimary to pick the account for the payslip, so if this new
            // record is (or is the first and therefore defaults to) primary, every
            // other bank account of this employee must be un-primaried first.
            bool employeeHasAnyAccount = await _context.EmployeeBankDetails
                .AnyAsync(x => x.EmployeeId == dto.EmployeeId && x.TenantId == tenantId && !x.IsDeleted);

            if (!employeeHasAnyAccount)
                entity.IsPrimary = true;

            if (entity.IsPrimary)
                await UnsetOtherPrimariesAsync(dto.EmployeeId, tenantId, null);

            await _context.EmployeeBankDetails.AddAsync(entity);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(entity.Id, tenantId);
        }

        #endregion

        #region Update

        public async Task<EmployeeBankDetailDto> UpdateAsync(string id, EmployeeBankDetailDto dto, string tenantId, string actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var entity = await _context.EmployeeBankDetails
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                throw new Exception("Bank detail record not found.");

            await ValidateAsync(dto, tenantId, id);

            entity.BankName = dto.BankName;
            entity.BranchName = dto.BranchName;
            entity.AccountNumber = dto.AccountNumber;
            entity.AccountHolderName = dto.AccountHolderName;
            entity.IFSCCode = dto.IFSCCode.Trim().ToUpper();
            entity.MICRCode = dto.MICRCode;
            entity.AccountType = dto.AccountType;
            entity.CancelledChequeFilePath = dto.CancelledChequeFilePath;
            entity.Remarks = dto.Remarks;
            entity.IsPrimary = dto.IsPrimary;

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = string.IsNullOrWhiteSpace(actingUserId) ? "System" : actingUserId;

            if (entity.IsPrimary)
            {
                await UnsetOtherPrimariesAsync(entity.EmployeeId, tenantId, entity.Id);
            }
            else
            {
                // Guard: PayrollBusinessService requires exactly one primary
                // account whenever an employee has at least one active bank
                // account. If this update would unset the ONLY primary
                // (no other active account is currently primary), refuse
                // the change rather than silently leaving the employee with
                // zero primary accounts.
                bool anyOtherPrimary = await _context.EmployeeBankDetails
                    .AnyAsync(x => x.EmployeeId == entity.EmployeeId
                                && x.TenantId == tenantId
                                && x.Id != entity.Id
                                && !x.IsDeleted
                                && x.IsPrimary);

                if (!anyOtherPrimary)
                    entity.IsPrimary = true;
            }

            _context.EmployeeBankDetails.Update(entity);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(entity.Id, tenantId);
        }

        #endregion

        #region Delete (Soft)

        public async Task<bool> DeleteAsync(string id, string tenantId)
        {
            var entity = await _context.EmployeeBankDetails
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                return false;

            bool wasPrimary = entity.IsPrimary;

            entity.IsDeleted = true;
            entity.ModifiedOn = DateTime.UtcNow;

            // If the deleted account was the primary one, promote the most
            // recently created remaining account so payroll (which trusts
            // "the IsPrimary one wins") always has exactly one to pick when
            // at least one account still exists for this employee. Queried
            // and saved together with the soft-delete in a single
            // SaveChangesAsync call below so this is atomic - note entity
            // isn't actually IsDeleted=true in the DB yet at query time
            // (only in this tracked in-memory instance), so it must be
            // excluded explicitly by Id rather than relying on !x.IsDeleted.
            if (wasPrimary)
            {
                var replacement = await _context.EmployeeBankDetails
                    .Where(x => x.EmployeeId == entity.EmployeeId
                             && x.TenantId == tenantId
                             && x.Id != entity.Id
                             && !x.IsDeleted)
                    .OrderByDescending(x => x.CreatedOn)
                    .FirstOrDefaultAsync();

                if (replacement != null)
                {
                    replacement.IsPrimary = true;
                    replacement.ModifiedOn = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Set Primary

        public async Task<bool> SetPrimaryAsync(string id, string tenantId, string actingUserId)
        {
            var entity = await _context.EmployeeBankDetails
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                return false;

            await UnsetOtherPrimariesAsync(entity.EmployeeId, tenantId, entity.Id);

            entity.IsPrimary = true;
            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = string.IsNullOrWhiteSpace(actingUserId) ? "System" : actingUserId;

            _context.EmployeeBankDetails.Update(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Private Helpers

        private async Task ValidateAsync(EmployeeBankDetailDto dto, string tenantId, string? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(dto.EmployeeId))
                throw new Exception("Employee is required.");

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == dto.EmployeeId && x.TenantId == tenantId && !x.IsDeleted);

            if (employee == null)
                throw new Exception("Employee not found.");

            if (string.IsNullOrWhiteSpace(dto.BankName))
                throw new Exception("Bank Name is required.");

            if (string.IsNullOrWhiteSpace(dto.AccountNumber))
                throw new Exception("Account Number is required.");

            if (string.IsNullOrWhiteSpace(dto.AccountHolderName))
                throw new Exception("Account Holder Name is required.");

            if (string.IsNullOrWhiteSpace(dto.IFSCCode))
                throw new Exception("IFSC Code is required.");

            if (!IfscRegex.IsMatch(dto.IFSCCode.Trim().ToUpper()))
                throw new Exception("Invalid IFSC Code format. Expected format: 4 letters, '0', then 6 alphanumeric characters (e.g. SBIN0001234).");

            // Guard against the same account number being registered twice
            // for the same employee (excluding the record being updated).
            bool duplicate = await _context.EmployeeBankDetails
                .AnyAsync(x => x.EmployeeId == dto.EmployeeId
                            && x.TenantId == tenantId
                            && !x.IsDeleted
                            && x.AccountNumber == dto.AccountNumber
                            && x.Id != excludeId);

            if (duplicate)
                throw new Exception("This account number is already registered for this employee.");
        }

        // Unsets IsPrimary on every other active bank account of the given
        // employee - keeps the "at most one primary per employee" invariant
        // that PayrollBusinessService relies on when picking the account to
        // print on a payslip.
        private async Task UnsetOtherPrimariesAsync(string employeeId, string tenantId, string? excludeId)
        {
            var others = await _context.EmployeeBankDetails
                .Where(x => x.EmployeeId == employeeId
                         && x.TenantId == tenantId
                         && !x.IsDeleted
                         && x.IsPrimary
                         && x.Id != excludeId)
                .ToListAsync();

            foreach (var other in others)
            {
                other.IsPrimary = false;
                other.ModifiedOn = DateTime.UtcNow;
            }

            if (others.Count > 0)
                _context.EmployeeBankDetails.UpdateRange(others);
        }

        private static EmployeeBankDetailDto MapToDto(EmployeeBankDetail entity)
        {
            return new EmployeeBankDetailDto
            {
                Id = entity.Id,

                EmployeeId = entity.EmployeeId,
                EmployeeName = entity.Employee == null
                    ? ""
                    : entity.Employee.FirstName + " " + entity.Employee.LastName,
                EmployeeCode = entity.Employee?.EmployeeCode,

                BankName = entity.BankName,
                BranchName = entity.BranchName,
                AccountNumber = entity.AccountNumber,
                AccountHolderName = entity.AccountHolderName,
                IFSCCode = entity.IFSCCode,
                MICRCode = entity.MICRCode,
                AccountType = entity.AccountType,
                AccountTypeName = entity.AccountType.ToString(),
                CancelledChequeFilePath = entity.CancelledChequeFilePath,
                IsPrimary = entity.IsPrimary,

                Remarks = entity.Remarks,

                TenantId = entity.TenantId,
                CreatedOn = entity.CreatedOn,
                CreatedBy = entity.CreatedBy,
                ModifiedOn = entity.ModifiedOn,
                ModifiedBy = entity.ModifiedBy
            };
        }

        #endregion
    }
}
