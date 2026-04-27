using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Interfaces
{
    public interface ITenantService
    {
        string GetTenantId();
        void SetTenantId(string tenantId);
    }
}
