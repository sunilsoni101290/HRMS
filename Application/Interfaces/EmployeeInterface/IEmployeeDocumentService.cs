using Application.DTOs.Employee;
using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.Interfaces.EmployeeInterface
{
    public interface IEmployeeDocumentService
    {
        Task<List<EmployeeDocumentDto>> GetAllAsync();

        Task<EmployeeDocumentDto> GetByIdAsync(string id);

        Task<List<EmployeeDocumentDto>> GetByEmployeeAsync(string employeeId);

        Task<List<EmployeeDocumentDto>> GetByDocumentTypeAsync(EmployeeDocumentType documentType);

        Task<EmployeeDocumentDto> CreateAsync(EmployeeDocumentDto dto);

        Task<EmployeeDocumentDto> UpdateAsync(string id, EmployeeDocumentDto dto);

        Task<bool> DeleteAsync(string id);

        Task<bool> VerifyDocumentAsync(
            string id,
            string verifiedBy,
            string? remarks);

        Task<bool> UnVerifyDocumentAsync(string id);

        Task<List<EmployeeDocumentDto>> GetExpiredDocumentsAsync();

        Task<List<EmployeeDocumentDto>> GetExpiringDocumentsAsync(int days);

        Task<bool> DocumentExistsAsync(
            string employeeId,
            EmployeeDocumentType documentType);
    }
}
