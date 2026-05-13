using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public abstract class BaseService
    {
        protected readonly ApplicationDbContext _db;

        protected BaseService(ApplicationDbContext db)
        {
            _db = db;
        }

        protected async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
