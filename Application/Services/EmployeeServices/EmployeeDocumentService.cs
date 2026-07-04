using Application.DTOs.Employee;
using Application.Interfaces.EmployeeInterface;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.EmployeeServices
{
    public class EmployeeDocumentService : IEmployeeDocumentService
    {
        private readonly ApplicationDbContext _context;

        public EmployeeDocumentService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<EmployeeDocumentDto>> GetAllAsync()
        {
            try
            {
                var documents = await _context.EmployeeDocuments
                    .AsNoTracking()
                    .Include(x => x.Employee)
                    .Where(x => !x.IsDeleted)
                    .OrderByDescending(x => x.CreatedOn)
                    .ToListAsync();

                return documents.Select(MapToDto).ToList();
            }
            catch (Exception)
            {
                return new List<EmployeeDocumentDto>();
            }
        }

        #endregion

        #region Get By Id

        public async Task<EmployeeDocumentDto> GetByIdAsync(string id)
        {
            try
            {
                var document = await _context.EmployeeDocuments
                    .AsNoTracking()
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                return document == null ? null : MapToDto(document);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Get By Employee

        public async Task<List<EmployeeDocumentDto>> GetByEmployeeAsync(string employeeId)
        {
            try
            {
                var documents = await _context.EmployeeDocuments
                    .AsNoTracking()
                    .Include(x => x.Employee)
                    .Where(x => x.EmployeeId == employeeId && !x.IsDeleted)
                    .OrderByDescending(x => x.CreatedOn)
                    .ToListAsync();

                return documents.Select(MapToDto).ToList();
            }
            catch (Exception)
            {
                return new List<EmployeeDocumentDto>();
            }
        }

        #endregion

        #region Get By Document Type

        public async Task<List<EmployeeDocumentDto>> GetByDocumentTypeAsync(EmployeeDocumentType documentType)
        {
            try
            {
                var documents = await _context.EmployeeDocuments
                    .AsNoTracking()
                    .Include(x => x.Employee)
                    .Where(x => x.DocumentType == documentType && !x.IsDeleted)
                    .OrderByDescending(x => x.CreatedOn)
                    .ToListAsync();

                return documents.Select(MapToDto).ToList();
            }
            catch (Exception)
            {
                return new List<EmployeeDocumentDto>();
            }
        }

        #endregion

        #region Create

        public async Task<EmployeeDocumentDto> CreateAsync(EmployeeDocumentDto dto)
        {
            try
            {
                var entity = new EmployeeDocument
                {
                    Id = IDManager.GetNewId(new EmployeeDocument()),
                    EmployeeId = dto.EmployeeId,
                    DocumentType = dto.DocumentType,
                    DocumentNumber = dto.DocumentNumber,
                    DocumentName = dto.DocumentName,

                    FileName = dto.FileName,
                    FilePath = dto.FilePath ?? "",
                    FileExtension = dto.FileExtension,
                    FileSize = dto.FileSize,

                    IssueDate = dto.IssueDate,
                    ExpiryDate = dto.ExpiryDate,
                    IssuedBy = dto.IssuedBy,

                    IsVerified = false,
                    Remarks = dto.Remarks,

                    TenantId = dto.TenantId,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = dto.CreatedBy
                };

                await _context.EmployeeDocuments.AddAsync(entity);
                await _context.SaveChangesAsync();

                return await GetByIdAsync(entity.Id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Update

        public async Task<EmployeeDocumentDto> UpdateAsync(string id, EmployeeDocumentDto dto)
        {
            try
            {
                var entity = await _context.EmployeeDocuments
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (entity == null)
                    return null;

                entity.DocumentType = dto.DocumentType;
                entity.DocumentNumber = dto.DocumentNumber;
                entity.DocumentName = dto.DocumentName;

                // Only replace the file when a new one was uploaded
                if (!string.IsNullOrWhiteSpace(dto.FilePath))
                {
                    entity.FileName = dto.FileName;
                    entity.FilePath = dto.FilePath;
                    entity.FileExtension = dto.FileExtension;
                    entity.FileSize = dto.FileSize;
                }

                entity.IssueDate = dto.IssueDate;
                entity.ExpiryDate = dto.ExpiryDate;
                entity.IssuedBy = dto.IssuedBy;
                entity.Remarks = dto.Remarks;

                entity.TenantId = dto.TenantId;
                entity.ModifiedOn = DateTime.UtcNow;
                entity.ModifiedBy = dto.ModifiedBy;

                _context.EmployeeDocuments.Update(entity);
                await _context.SaveChangesAsync();

                return await GetByIdAsync(entity.Id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Delete (Soft)

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                var entity = await _context.EmployeeDocuments
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (entity == null)
                    return false;

                entity.IsDeleted = true;
                entity.ModifiedOn = DateTime.UtcNow;

                _context.EmployeeDocuments.Update(entity);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Verify / UnVerify

        public async Task<bool> VerifyDocumentAsync(string id, string verifiedBy, string? remarks)
        {
            try
            {
                var entity = await _context.EmployeeDocuments
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (entity == null)
                    return false;

                entity.IsVerified = true;
                entity.VerifiedOn = DateTime.UtcNow;
                entity.VerifiedBy = verifiedBy;

                if (!string.IsNullOrWhiteSpace(remarks))
                    entity.Remarks = remarks;

                entity.ModifiedOn = DateTime.UtcNow;
                entity.ModifiedBy = verifiedBy;

                _context.EmployeeDocuments.Update(entity);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UnVerifyDocumentAsync(string id)
        {
            try
            {
                var entity = await _context.EmployeeDocuments
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (entity == null)
                    return false;

                entity.IsVerified = false;
                entity.VerifiedOn = null;
                entity.VerifiedBy = null;
                entity.ModifiedOn = DateTime.UtcNow;

                _context.EmployeeDocuments.Update(entity);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Expiry

        public async Task<List<EmployeeDocumentDto>> GetExpiredDocumentsAsync()
        {
            try
            {
                var today = DateTime.Today;

                var documents = await _context.EmployeeDocuments
                    .AsNoTracking()
                    .Include(x => x.Employee)
                    .Where(x => !x.IsDeleted
                             && x.ExpiryDate.HasValue
                             && x.ExpiryDate.Value.Date < today)
                    .OrderBy(x => x.ExpiryDate)
                    .ToListAsync();

                return documents.Select(MapToDto).ToList();
            }
            catch (Exception)
            {
                return new List<EmployeeDocumentDto>();
            }
        }

        public async Task<List<EmployeeDocumentDto>> GetExpiringDocumentsAsync(int days)
        {
            try
            {
                var today = DateTime.Today;
                var endDate = today.AddDays(days);

                var documents = await _context.EmployeeDocuments
                    .AsNoTracking()
                    .Include(x => x.Employee)
                    .Where(x => !x.IsDeleted
                             && x.ExpiryDate.HasValue
                             && x.ExpiryDate.Value.Date >= today
                             && x.ExpiryDate.Value.Date <= endDate)
                    .OrderBy(x => x.ExpiryDate)
                    .ToListAsync();

                return documents.Select(MapToDto).ToList();
            }
            catch (Exception)
            {
                return new List<EmployeeDocumentDto>();
            }
        }

        #endregion

        #region Exists

        public async Task<bool> DocumentExistsAsync(string employeeId, EmployeeDocumentType documentType)
        {
            try
            {
                return await _context.EmployeeDocuments
                    .AnyAsync(x => x.EmployeeId == employeeId
                                && x.DocumentType == documentType
                                && !x.IsDeleted);
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Private Mapper

        private static EmployeeDocumentDto MapToDto(EmployeeDocument entity)
        {
            return new EmployeeDocumentDto
            {
                Id = entity.Id,

                EmployeeId = entity.EmployeeId,
                EmployeeName = entity.Employee == null
                    ? ""
                    : entity.Employee.FirstName + " " + entity.Employee.LastName,

                DocumentType = entity.DocumentType,
                DocumentTypeText = entity.DocumentType.ToString(),

                DocumentNumber = entity.DocumentNumber,
                DocumentName = entity.DocumentName,

                FileName = entity.FileName,
                FilePath = entity.FilePath,
                FileExtension = entity.FileExtension,
                FileSize = entity.FileSize,

                IssueDate = entity.IssueDate,
                ExpiryDate = entity.ExpiryDate,
                IssuedBy = entity.IssuedBy,

                IsVerified = entity.IsVerified,
                VerifiedOn = entity.VerifiedOn,
                VerifiedBy = entity.VerifiedBy,

                Remarks = entity.Remarks,

                TenantId = entity.TenantId,
                CreatedOn = entity.CreatedOn,
                CreatedBy = entity.CreatedBy,
                ModifiedOn = entity.ModifiedOn,
                ModifiedBy = entity.ModifiedBy
            };
        }

        #endregion
    }
}
