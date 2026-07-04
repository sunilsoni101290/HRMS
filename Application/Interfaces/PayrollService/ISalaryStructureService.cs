using Application.DTOs.Payroll;

namespace Application.Interfaces.Payroll
{
    public interface ISalaryStructureService
    {
        Task<List<SalaryStructureListDto>> GetAllAsync();
        Task<SalaryStructureDto> GetByIdAsync(string id);
        Task<string> CreateAsync(SalaryStructureDto dto);
        Task<string> UpdateAsync(string id, SalaryStructureDto dto);
        Task<bool> DeleteAsync(string id);
    }
}
