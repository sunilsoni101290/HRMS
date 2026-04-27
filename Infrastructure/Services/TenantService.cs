using Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Services
{
    public class TenantService : ITenantService
    {
        private string _tenantId;

        public string GetTenantId()
        {
            return _tenantId;
        }

        public void SetTenantId(string tenantId)
        {
            _tenantId = tenantId;
        }
    }
}
