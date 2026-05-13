using Application.DTOs.Company;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Company
{
    public interface ICompanyService
    {
        Task<List<CompanyListDto>> GetAllAsync();
        Task<CompanyDto> GetByIdAsync(string id);
        Task<string> CreateAsync(CompanyDto dto);
        Task<string> UpdateAsync(string id, CompanyDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
