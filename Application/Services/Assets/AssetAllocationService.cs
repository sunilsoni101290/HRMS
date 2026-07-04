using Application.DTOs.Assets;
using Application.Interfaces.Assets;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Assets
{
    public class AssetAllocationService : IAssetAllocationService
    {
        private readonly ApplicationDbContext _context;

        public AssetAllocationService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<AssetAllocationListDto>> GetAllAsync()
        {
            try
            {
            var list = await Query().ToListAsync();
            ApplyStatusText(list);
            return list;
            }
            catch (Exception)
            {
                return new List<AssetAllocationListDto>();
            }
        }

        public async Task<List<AssetAllocationListDto>> GetByAssetIdAsync(string assetId)
        {
            try
            {
            var list = await Query()
                .Where(x => x.AssetId == assetId)
                .ToListAsync();
            ApplyStatusText(list);
            return list;
            }
            catch (Exception)
            {
                return new List<AssetAllocationListDto>();
            }
        }

        private IQueryable<AssetAllocationListDto> Query()
        {
            return _context.AssetAllocations
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.AllocatedOn)
                .Select(x => new AssetAllocationListDto
                {
                    Id = x.Id,
                    AssetId = x.AssetId,
                    AssetName = x.Asset != null ? x.Asset.Name : "",
                    AssetCode = x.Asset != null ? x.Asset.AssetCode : "",
                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee != null ? x.Employee.FirstName + " " + x.Employee.LastName : "",
                    AllocatedOn = x.AllocatedOn,
                    ReturnedOn = x.ReturnedOn,
                    AllocationStatus = (int)x.AllocationStatus,
                    ConditionOnIssue = x.ConditionOnIssue,
                    ConditionOnReturn = x.ConditionOnReturn,
                    Remarks = x.Remarks
                });
        }

        private static void ApplyStatusText(List<AssetAllocationListDto> list)
        {
            foreach (var item in list)
                item.AllocationStatusText = ((AllocationStatus)item.AllocationStatus).ToString();
        }

        #endregion

        #region Get By Id

        public async Task<AssetAllocationDto> GetByIdAsync(string id)
        {
            try
            {
            var entity = await _context.AssetAllocations
                .Include(x => x.Asset)
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return null;

            return new AssetAllocationDto
            {
                Id = entity.Id,
                AssetId = entity.AssetId,
                AssetName = entity.Asset != null ? entity.Asset.Name : "",
                EmployeeId = entity.EmployeeId,
                EmployeeName = entity.Employee != null ? entity.Employee.FirstName + " " + entity.Employee.LastName : "",
                AllocatedOn = entity.AllocatedOn,
                ReturnedOn = entity.ReturnedOn,
                AllocationStatus = (int)entity.AllocationStatus,
                AllocationStatusText = entity.AllocationStatus.ToString(),
                ConditionOnIssue = entity.ConditionOnIssue,
                ConditionOnReturn = entity.ConditionOnReturn,
                Remarks = entity.Remarks,
                TenantId = entity.TenantId,
                CreatedBy = entity.CreatedBy,
                ModifiedBy = entity.ModifiedBy,
                ModifiedOn = entity.ModifiedOn
            };
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Create

        public async Task<string> CreateAsync(AssetAllocationDto dto)
        {
            try
            {
                var asset = await _context.Assets
                .FirstOrDefaultAsync(x => x.Id == dto.AssetId && !x.IsDeleted);

                if (asset == null)
                    return "Asset Not Found";

                var alreadyAllocated = await _context.AssetAllocations
                    .AnyAsync(x => x.AssetId == dto.AssetId
                                && x.ReturnedOn == null
                                && !x.IsDeleted);

                if (alreadyAllocated)
                    return "Asset Is Already Allocated";

                var entity = new AssetAllocation
                {
                    Id = IDManager.GetNewId(new AssetAllocation()),
                    AssetId = dto.AssetId,
                    EmployeeId = dto.EmployeeId,
                    AllocatedOn = dto.AllocatedOn,
                    ReturnedOn = dto.ReturnedOn,
                    AllocationStatus = (AllocationStatus)dto.AllocationStatus,
                    ConditionOnIssue = dto.ConditionOnIssue,
                    ConditionOnReturn = dto.ConditionOnReturn,
                    Remarks = dto.Remarks,
                    TenantId = dto.TenantId,
                    CreatedBy = dto.CreatedBy
                };

                await _context.AssetAllocations.AddAsync(entity);

                // Keep asset status in sync
                asset.Status = "Allocated";
                asset.ModifiedOn = DateTime.UtcNow;
                asset.ModifiedBy = dto.CreatedBy;

                await AddHistoryAsync(dto.AssetId, "Allocated", entity.Id, dto.CreatedBy, dto.TenantId);
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

        public async Task<string> UpdateAsync(string id, AssetAllocationDto dto)
        {
            try
            {
            var entity = await _context.AssetAllocations
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return "Allocation Not Found";

            entity.EmployeeId = dto.EmployeeId;
            entity.AllocatedOn = dto.AllocatedOn;
            entity.ReturnedOn = dto.ReturnedOn;
            entity.AllocationStatus = (AllocationStatus)dto.AllocationStatus;
            entity.ConditionOnIssue = dto.ConditionOnIssue;
            entity.ConditionOnReturn = dto.ConditionOnReturn;
            entity.Remarks = dto.Remarks;
            entity.TenantId = dto.TenantId;
            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedOn = dto.ModifiedOn ?? DateTime.UtcNow;

            _context.AssetAllocations.Update(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        #endregion

        #region Return

        public async Task<string> ReturnAsync(string id, AssetAllocationDto dto)
        {
            try
            {
            var entity = await _context.AssetAllocations
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return "Allocation Not Found";

            entity.ReturnedOn = dto.ReturnedOn ?? DateTime.UtcNow;
            entity.ConditionOnReturn = dto.ConditionOnReturn;
            entity.AllocationStatus = AllocationStatus.Completed;
            entity.Remarks = dto.Remarks;
            entity.ModifiedBy = dto.ModifiedBy ?? dto.CreatedBy;
            entity.ModifiedOn = DateTime.UtcNow;

            _context.AssetAllocations.Update(entity);

            var asset = await _context.Assets
                .FirstOrDefaultAsync(x => x.Id == entity.AssetId && !x.IsDeleted);

            if (asset != null)
            {
                asset.Status = "Available";
                asset.ModifiedOn = DateTime.UtcNow;
                asset.ModifiedBy = dto.ModifiedBy ?? dto.CreatedBy;
            }

            await AddHistoryAsync(entity.AssetId, "Returned", entity.Id, dto.ModifiedBy ?? dto.CreatedBy, entity.TenantId);
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
            var entity = await _context.AssetAllocations
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.ModifiedOn = DateTime.UtcNow;

            _context.AssetAllocations.Update(entity);
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
