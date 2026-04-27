using Application.Common.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.Services
{
    public class FinancialYearService : IFinancialYearService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public FinancialYearService(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public async Task<FinancialYear> GetActiveFinancialYearAsync()
        {
            var tenantId = _tenantService.GetTenantId();

            var financialYear = await _context.FinancialYears
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Status == FinancialYearStatus.Open &&
                    x.TenantId == tenantId &&
                    !x.IsDeleted);

            if (financialYear == null)
                throw new NotFoundException("Active Financial Year not found");

            return financialYear;
        }
    }
}
