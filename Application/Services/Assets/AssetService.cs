using Application.DTOs.Assets;
using Application.Interfaces.Assets;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Assets
{
    public class AssetService : IAssetService
    {
        private readonly ApplicationDbContext _context;

        public AssetService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<AssetListDto>> GetAllAsync()
        {
            try
            {
            return await _context.Assets
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.CreatedOn)
                .Select(x => new AssetListDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    AssetCode = x.AssetCode,
                    SerialNumber = x.SerialNumber,

                    AssetCategoryId = x.AssetCategoryId,
                    AssetCategoryName = x.AssetCategory != null ? x.AssetCategory.Name : "",

                    PurchaseDate = x.PurchaseDate,
                    PurchaseCost = x.PurchaseCost,
                    WarrantyExpiryDate = x.WarrantyExpiryDate,

                    Status = x.Status,

                    CompanyId = x.CompanyId,
                    CompanyName = _context.Companies
                        .Where(c => c.Id == x.CompanyId)
                        .Select(c => c.Name).FirstOrDefault(),
                    BranchId = x.BranchId,
                    BranchName = _context.Branches
                        .Where(b => b.Id == x.BranchId)
                        .Select(b => b.Name).FirstOrDefault(),

                    CurrentEmployeeName = _context.AssetAllocations
                        .Where(a => a.AssetId == x.Id
                                 && a.ReturnedOn == null
                                 && !a.IsDeleted)
                        .OrderByDescending(a => a.AllocatedOn)
                        .Select(a => a.Employee.FirstName + " " + a.Employee.LastName)
                        .FirstOrDefault()
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<AssetListDto>();
            }
        }

        #endregion

        #region Get By Id

        public async Task<AssetDto> GetByIdAsync(string id)
        {
            try
            {
            var entity = await _context.Assets
                .Include(x => x.AssetCategory)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return null;

            var dto = new AssetDto
            {
                Id = entity.Id,
                Name = entity.Name,
                AssetCode = entity.AssetCode,
                SerialNumber = entity.SerialNumber,

                AssetCategoryId = entity.AssetCategoryId,
                AssetCategoryName = entity.AssetCategory != null ? entity.AssetCategory.Name : "",

                PurchaseDate = entity.PurchaseDate,
                PurchaseCost = entity.PurchaseCost,
                VendorName = entity.VendorName,
                WarrantyExpiryDate = entity.WarrantyExpiryDate,
                Status = entity.Status,

                TenantId = entity.TenantId,
                CompanyId = entity.CompanyId,
                CompanyName = await _context.Companies
                    .Where(c => c.Id == entity.CompanyId)
                    .Select(c => c.Name).FirstOrDefaultAsync(),
                BranchId = entity.BranchId,
                BranchName = await _context.Branches
                    .Where(b => b.Id == entity.BranchId)
                    .Select(b => b.Name).FirstOrDefaultAsync(),

                Description = entity.Description,
                CreatedBy = entity.CreatedBy,
                ModifiedBy = entity.ModifiedBy,
                ModifiedOn = entity.ModifiedOn
            };

            dto.Histories = await _context.AssetHistories
                .AsNoTracking()
                .Where(h => h.AssetId == id && !h.IsDeleted)
                .OrderByDescending(h => h.ActionDate)
                .Select(h => new AssetHistoryDto
                {
                    Id = h.Id,
                    AssetId = h.AssetId,
                    Action = h.Action,
                    ReferenceId = h.ReferenceId,
                    ActionDate = h.ActionDate,
                    PerformedBy = h.PerformedBy
                })
                .ToListAsync();

            dto.Allocations = await _context.AssetAllocations
                .AsNoTracking()
                .Where(a => a.AssetId == id && !a.IsDeleted)
                .OrderByDescending(a => a.AllocatedOn)
                .Select(a => new AssetAllocationListDto
                {
                    Id = a.Id,
                    AssetId = a.AssetId,
                    EmployeeId = a.EmployeeId,
                    EmployeeName = a.Employee != null ? a.Employee.FirstName + " " + a.Employee.LastName : "",
                    AllocatedOn = a.AllocatedOn,
                    ReturnedOn = a.ReturnedOn,
                    AllocationStatus = (int)a.AllocationStatus,
                    ConditionOnIssue = a.ConditionOnIssue,
                    ConditionOnReturn = a.ConditionOnReturn,
                    Remarks = a.Remarks
                })
                .ToListAsync();

            foreach (var a in dto.Allocations)
                a.AllocationStatusText = ((AllocationStatus)a.AllocationStatus).ToString();

            return dto;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Create

        public async Task<string> CreateAsync(AssetDto dto)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(dto.AssetCode))
                {
                    var isDuplicate = await _context.Assets
                        .AnyAsync(x => x.AssetCode == dto.AssetCode && !x.IsDeleted);

                    if (isDuplicate)
                        return "Asset Code Already Exists";
                }

                var entity = new Asset
                {
                    Id = IDManager.GetNewId(new Asset()),
                    Name = dto.Name,
                    AssetCode = dto.AssetCode,
                    SerialNumber = dto.SerialNumber,
                    AssetCategoryId = dto.AssetCategoryId,
                    PurchaseDate = dto.PurchaseDate,
                    PurchaseCost = dto.PurchaseCost,
                    VendorName = dto.VendorName,
                    WarrantyExpiryDate = dto.WarrantyExpiryDate,
                    Status = string.IsNullOrEmpty(dto.Status) ? "Available" : dto.Status,
                    TenantId = dto.TenantId,
                    CompanyId = dto.CompanyId,
                    BranchId = dto.BranchId,
                    Description = dto.Description,
                    CreatedBy = dto.CreatedBy
                };

                await _context.Assets.AddAsync(entity);
                await AddHistoryAsync(entity.Id, "Created", entity.Id, dto.CreatedBy, dto.TenantId);
                await _context.SaveChangesAsync();

                return entity.Id;
            }
            catch(Exception ex)
            {
                return ex.ToString();
            }
        }

        #endregion

        #region Update

        public async Task<string> UpdateAsync(string id, AssetDto dto)
        {
            try
            {
            var entity = await _context.Assets
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return "Asset Not Found";

            entity.Name = dto.Name;
            entity.AssetCode = dto.AssetCode;
            entity.SerialNumber = dto.SerialNumber;
            entity.AssetCategoryId = dto.AssetCategoryId;
            entity.PurchaseDate = dto.PurchaseDate;
            entity.PurchaseCost = dto.PurchaseCost;
            entity.VendorName = dto.VendorName;
            entity.WarrantyExpiryDate = dto.WarrantyExpiryDate;
            if (!string.IsNullOrEmpty(dto.Status))
                entity.Status = dto.Status;
            entity.TenantId = dto.TenantId;
            entity.CompanyId = dto.CompanyId;
            entity.BranchId = dto.BranchId;
            entity.Description = dto.Description;
            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedOn = dto.ModifiedOn ?? DateTime.UtcNow;

            _context.Assets.Update(entity);
            await AddHistoryAsync(entity.Id, "Updated", null, dto.ModifiedBy ?? dto.CreatedBy, dto.TenantId);
            await _context.SaveChangesAsync();

            return entity.Id;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        #endregion

        #region Delete (Soft)

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
            var entity = await _context.Assets
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.ModifiedOn = DateTime.UtcNow;

            _context.Assets.Update(entity);
            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Helpers

        private async Task AddHistoryAsync(string assetId, string action, string referenceId, string performedBy, string tenantId)
        {
            var history = new AssetHistory
            {
                Id = IDManager.GetNewId(new AssetHistory()),
                AssetId = assetId,
                Action = action,
                ReferenceId = referenceId,
                ActionDate = DateTime.UtcNow,
                PerformedBy = performedBy,
                TenantId = tenantId,
                CreatedBy = performedBy
            };

            await _context.AssetHistories.AddAsync(history);
        }

        #endregion
    }
}
