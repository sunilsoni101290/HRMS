using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface ITenantBusinessService
    {
        Task<List<TenantDto>> GetAllAsync();

        Task<TenantDto> GetByIdAsync(string id);

        Task<TenantDto> CreateAsync(TenantDto dto);

        Task<TenantDto> UpdateAsync(string id, TenantDto dto);

        Task<bool> DeleteAsync(string id);

        Task<bool> ActivateAsync(string id);

        Task<bool> DeactivateAsync(string id);

        Task<TenantDto> GetByCodeAsync(string code);

        Task<TenantDto> GetByDomainAsync(string domain);
    }
}
