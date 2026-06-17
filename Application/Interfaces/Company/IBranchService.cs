using Application.DTOs.Company;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Company
{
    public interface IBranchService
    {
        Task<List<BranchDto>> GetAllAsync();

        Task<List<BranchDto>> GetByCompanyAsync(string companyId);

        Task<BranchDto?> GetByIdAsync(string id);

        Task<BranchDto> CreateAsync(BranchDto dto);

        Task<BranchDto?> UpdateAsync(string id, BranchDto dto);

        Task<bool> DeleteAsync(string id);
    }
}
