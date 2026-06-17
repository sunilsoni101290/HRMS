using Application.DTOs.Masters;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Masters
{
    public interface IFinancialYearService
    {
        Task<List<FinancialYearDto>> GetAllAsync();

        Task<FinancialYearDto?> GetByIdAsync(string id);

        Task<FinancialYearDto> CreateAsync(FinancialYearDto dto);

        Task<FinancialYearDto?> UpdateAsync(string id, FinancialYearDto dto);

        Task<bool> DeleteAsync(string id);

        Task<FinancialYearDto?> GetCurrentFinancialYearAsync();
    }
}
