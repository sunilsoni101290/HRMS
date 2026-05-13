using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface ISequenceService
    {
        Task<string> GetNextERPIdAsync(string module, string tenantId);
    }
}
