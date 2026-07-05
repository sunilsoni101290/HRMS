using Application.Common.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class SequenceService : ISequenceService
    {
        private readonly ApplicationDbContext _context;

        public SequenceService(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔥 MAIN METHOD (Use this everywhere)
        public async Task<string> GetNextERPIdAsync(string module, string tenantId)
        {
            try
            {
            if (string.IsNullOrWhiteSpace(module))
                throw new BadRequestException("Module is required");

            if (string.IsNullOrWhiteSpace(tenantId))
                throw new BadRequestException("TenantId is required");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var fy = await GetCurrentFinancialYear(tenantId);

                    if (fy == null)
                        throw new Exception("Financial Year not found");

                    var sequence = await _context.SequenceMasters
                        .FirstOrDefaultAsync(x => x.Prefix == module && x.FinancialYearId == fy.Id);

                    if (sequence == null)
                    {
                        sequence = new SequenceMaster
                        {
                            Id = IDManager.GetNewId(new SequenceMaster()),
                            Prefix = module,
                            FinancialYearId = fy.Id,
                            CurrentNumber = 0,
                            CreatedBy = "System"
                        };

                        await _context.SequenceMasters.AddAsync(sequence);
                    }

                    // 🔥 increment
                    sequence.CurrentNumber += 1;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return GenerateERPId(module, sequence.CurrentNumber, fy.Code);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        // 🔹 FORMAT GENERATOR
        private string GenerateERPId(string prefix, int sequence, string financialYear)
        {
            // Example: EMP-FY25-26-00001
            return $"{prefix}-{financialYear}-{sequence:D5}";
        }

        // 🔹 GET CURRENT FY
        private async Task<FinancialYear?> GetCurrentFinancialYear(string tenantId)
        {
            return await _context.FinancialYears
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsCurrent);
        }
    }
}
