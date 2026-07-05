using Application.DTOs.Tasks;

namespace Application.Interfaces.Tasks
{
    public interface IEmployeeTaskService
    {
        Task<List<EmployeeTaskListDto>> GetAllAsync();
        Task<EmployeeTaskDto> GetByIdAsync(string id);
        Task<string> CreateAsync(EmployeeTaskDto dto);
        Task<string> UpdateAsync(string id, EmployeeTaskDto dto);
        Task<string> ChangeStatusAsync(string id, string status, string userId);
        Task<bool> DeleteAsync(string id);
    }
}
