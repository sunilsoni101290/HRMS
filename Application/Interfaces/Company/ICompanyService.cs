using Application.DTOs.Company;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Company
{
    public interface ICompanyService
    {
        Task<List<CompanyDto>> GetAllAsync();

        Task<CompanyDto?> GetByIdAsync(string id);

        Task<CompanyDto> CreateAsync(CompanyDto dto);

        Task<CompanyDto?> UpdateAsync(string id, CompanyDto dto);

        Task<bool> DeleteAsync(string id);
    }
}
