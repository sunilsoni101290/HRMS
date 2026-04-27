using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface ISequenceService
    {
        Task<string> GetNextERPId(string module, string financialYear);
        Task<string> GetNextCodeSequenceAsync(ApplicationDbContext _context, string module);
    }
}
