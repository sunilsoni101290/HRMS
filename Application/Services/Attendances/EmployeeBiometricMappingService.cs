using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Application.Interfaces.ErrorLog;
using Domain.Entities;
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

                var alreadyMapped = await _db.EmployeeBiometricMappings
                    .AnyAsync(x =>
                        x.BiometricEmployeeCode == dto.BiometricEmployeeCode);

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
                    BiometricEmployeeCode = dto.BiometricEmployeeCode,
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

            var codeTakenByAnother = await _db.EmployeeBiometricMappings
                .AnyAsync(x =>
                    x.Id != dto.Id &&
                    x.BiometricEmployeeCode == dto.BiometricEmployeeCode);

            if (codeTakenByAnother)
                throw new InvalidOperationException(
                    $"Biometric code '{dto.BiometricEmployeeCode}' is already mapped to another employee.");

            entity.EmployeeId = dto.EmployeeId;
            entity.BiometricEmployeeCode = dto.BiometricEmployeeCode;
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
