using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Application.Interfaces.ErrorLog;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Attendances
{
    /// <summary>
    /// Links an Employee to the code/card their biometric device reports in
    /// punch records (BiometricAttendanceLog.EmployeeCode). Without a mapping
    /// row, AttendanceProcessorService silently skips a device's punches
    /// (it can't tell which employee they belong to), so this has to exist
    /// before punches will ever turn into Attendance records - including
    /// during testing with the Mock agent driver.
    /// </summary>
    public class EmployeeBiometricMappingService : IEmployeeBiometricMappingService
    {
        private readonly ApplicationDbContext _db;
        private readonly IErrorLogService _errorLogService;
        private string tenantId = string.Empty;
        public EmployeeBiometricMappingService(ApplicationDbContext db, IErrorLogService errorLogService)
        {
            _db = db;
            _errorLogService = errorLogService;
        }

        public async Task<List<EmployeeBiometricMappingDto>> GetAllAsync()
        {
            return await _db.EmployeeBiometricMappings
                .Include(x => x.Employee)
                .Select(x => new EmployeeBiometricMappingDto
                {
                    Id = x.Id,
                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,
                    BiometricEmployeeCode = x.BiometricEmployeeCode,
                    CardNumber = x.CardNumber,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        }

        public async Task<EmployeeBiometricMappingDto?> GetByIdAsync(string id)
        {
            return await _db.EmployeeBiometricMappings
                .Include(x => x.Employee)
                .Where(x => x.Id == id)
                .Select(x => new EmployeeBiometricMappingDto
                {
                    Id = x.Id,
                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,
                    BiometricEmployeeCode = x.BiometricEmployeeCode,
                    CardNumber = x.CardNumber,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync();
        }

        public async Task<EmployeeBiometricMappingDto> CreateAsync(EmployeeBiometricMappingDto dto)
        {
            try
            {
                var employee = await _db.Employees
                    .FirstOrDefaultAsync(x => x.Id == dto.EmployeeId);

                if (employee == null)
                    throw new InvalidOperationException("Employee not found.");

                // ROOT-CAUSE FIX: the uniqueness check (and the mapping
                // value itself, below) is now normalized the same way as
                // every lookup site (BiometricEmployeeCodeNormalizer) - a
                // plain "==" comparison here previously let two mappings
                // like "260123" and "260123 " both be saved as if they were
                // different codes, which is exactly backwards from the real
                // bug (device-vs-mapping mismatch), but still worth closing
                // so admin data entry can't reintroduce whitespace/casing
                // drift going forward. Small table - safe to compare
                // in-memory rather than trying to get SQL Server's default
                // collation to do this translation-safely.
                var normalizedNewCode = BiometricEmployeeCodeNormalizer.Normalize(dto.BiometricEmployeeCode);

                var existingCodes = await _db.EmployeeBiometricMappings
                    .Select(x => x.BiometricEmployeeCode)
                    .ToListAsync();

                var alreadyMapped = existingCodes.Any(c =>
                    BiometricEmployeeCodeNormalizer.Normalize(c) == normalizedNewCode);

                if (alreadyMapped)
                {
                    throw new InvalidOperationException(
                        $"Biometric code '{dto.BiometricEmployeeCode}' is already mapped to another employee.");
                }

                tenantId = employee.TenantId;

                var entity = new EmployeeBiometricMapping
                {
                    Id = IDManager.GetNewId(new EmployeeBiometricMapping()),
                    EmployeeId = dto.EmployeeId,
                    // Stored trimmed (whitespace noise removed) but with the
                    // admin's original casing preserved for readability -
                    // every actual match against a device's raw UserId goes
                    // through BiometricEmployeeCodeNormalizer at lookup
                    // time, so casing here is cosmetic only, never load-bearing.
                    BiometricEmployeeCode = dto.BiometricEmployeeCode?.Trim() ?? "",
                    CardNumber = dto.CardNumber,
                    IsActive = dto.IsActive,
                    TenantId = employee.TenantId,
                    CreatedBy=dto.CreatedBy,
                    CreatedOn = DateTime.UtcNow
                };

                _db.EmployeeBiometricMappings.Add(entity);

                await _db.SaveChangesAsync();

                dto.Id = entity.Id;

                return dto;
            }
            catch (Exception ex)
            {
                // Log exception
                await _errorLogService.LogAsync(
                    ex,
                    module: "HRMS",
                    feature: "Employee Biometric Mapping",
                    controller: "EmployeeBiometricMapping",
                    action: "CreateAsync",
                    userId:dto.CreatedBy,
                    tenantId: tenantId
                );
                throw;
            }
        }

        public async Task<bool> UpdateAsync(EmployeeBiometricMappingDto dto)
        {
            var entity = await _db.EmployeeBiometricMappings
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (entity == null)
                return false;

            var employee = await _db.Employees
                .FirstOrDefaultAsync(x => x.Id == dto.EmployeeId);

            if (employee == null)
                throw new InvalidOperationException("Employee not found.");

            var normalizedUpdatedCode = BiometricEmployeeCodeNormalizer.Normalize(dto.BiometricEmployeeCode);

            var otherCodes = await _db.EmployeeBiometricMappings
                .Where(x => x.Id != dto.Id)
                .Select(x => x.BiometricEmployeeCode)
                .ToListAsync();

            var codeTakenByAnother = otherCodes.Any(c =>
                BiometricEmployeeCodeNormalizer.Normalize(c) == normalizedUpdatedCode);

            if (codeTakenByAnother)
                throw new InvalidOperationException(
                    $"Biometric code '{dto.BiometricEmployeeCode}' is already mapped to another employee.");

            entity.EmployeeId = dto.EmployeeId;
            entity.BiometricEmployeeCode = dto.BiometricEmployeeCode?.Trim() ?? "";
            entity.CardNumber = dto.CardNumber;
            entity.IsActive = dto.IsActive;
            entity.ModifiedOn = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _db.EmployeeBiometricMappings
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _db.EmployeeBiometricMappings.Remove(entity);

            await _db.SaveChangesAsync();

            return true;
        }
    }
}
