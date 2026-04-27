using Application.Common.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure;
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

        public async Task<string> GetNextERPId(string module, string financialYear)
        {
            if (string.IsNullOrWhiteSpace(module))
                throw new BadRequestException("Module is required");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
               
                // 🔹 Get or Create Sequence
                var sequence = await _context.SequenceMasters
                    .FirstOrDefaultAsync(x => x.Prefix == module);

                if (sequence == null)
                {
                    sequence = new SequenceMaster
                    {
                        Id = Guid.NewGuid().ToString(),
                        Prefix = module,
                        CurrentNumber = 0,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    };

                    await _context.SequenceMasters.AddAsync(sequence);
                }

                // 🔹 Increment Sequence
                sequence.CurrentNumber += 1;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return GenerateERPId(module, sequence.CurrentNumber, financialYear);
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                throw new BadRequestException("Database error while generating ERP ID");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw; // handled by global middleware
            }
        }

        public string GenerateERPId(string prefix, int sequence, string financialYear)
        {
            return $"{prefix}/{financialYear}/{sequence:D5}";
        }

        public async Task<string> GetNextCodeSequenceAsync(ApplicationDbContext _context, string module)
        {
            if (_context == null)
                throw new ArgumentNullException(nameof(_context));

            if (string.IsNullOrWhiteSpace(module))
                throw new BadRequestException("Module is required");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 🔹 Get or Create Sequence
                var sequence = await _context.SequenceMasters
                    .FirstOrDefaultAsync(x => x.Prefix == module);

                if (sequence == null)
                {
                    sequence = new SequenceMaster
                    {
                        Id = Guid.NewGuid().ToString(),
                        Prefix = module,
                        CurrentNumber = 0,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow
                    };

                    await _context.SequenceMasters.AddAsync(sequence);
                }

                // 🔹 Increment
                sequence.CurrentNumber += 1;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return $"{module}{sequence.CurrentNumber:D5}";
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                throw new BadRequestException("Database error while generating sequence");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw; // handled by global middleware
            }

        }
    }
}
