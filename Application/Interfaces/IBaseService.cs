using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IBaseService<TDto>
    {
        Task<string> CreateAsync(TDto dto);
        Task<bool> UpdateAsync(TDto dto);
        Task<bool> DeleteMultipleAsync(List<string> ids);
    }
}
