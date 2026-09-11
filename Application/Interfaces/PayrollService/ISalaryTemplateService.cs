using Application.DTOs.Payroll;

namespace Application.Interfaces.Payroll
{
    public interface ISalaryTemplateService
    {
        Task<List<SalaryTemplateListDto>> GetAllAsync();
        Task<SalaryTemplateDto> GetByIdAsync(string id);
        Task<string> CreateAsync(SalaryTemplateDto dto);
        Task<string> UpdateAsync(string id, SalaryTemplateDto dto);
        Task<string> DeleteAsync(string id);
        Task<SalaryTemplateAssignResultDto> AssignAsync(SalaryTemplateAssignDto dto);
        Task<bool> ToggleActiveAsync(string id);
    }
}
