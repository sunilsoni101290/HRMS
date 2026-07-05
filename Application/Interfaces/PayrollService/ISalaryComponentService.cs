using Application.DTOs.Payroll;

namespace Application.Interfaces.Payroll
{
    public interface ISalaryComponentService
    {
        Task<List<SalaryComponentListDto>> GetAllAsync();
        Task<SalaryComponentDto> GetByIdAsync(string id);
        Task<string> CreateAsync(SalaryComponentDto dto);
        Task<string> UpdateAsync(string id, SalaryComponentDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
