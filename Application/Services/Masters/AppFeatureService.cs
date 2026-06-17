using Application.DTOs;
using Domain.Entities;
using Infrastructure;
using System;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Application.Interfaces.Masters;

namespace Application.Services.Masters
{
    public class AppFeatureService : IAppFeatureService
    {
        private readonly ApplicationDbContext _context;

        public AppFeatureService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======================================================
        // GET ALL
        // ======================================================

        public async Task<List<AppFeatureDto>> GetAllAsync()
        {
            var data = await _context.AppFeatures
                .Include(x => x.ParentFeature)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            return data.Select(x => new AppFeatureDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                Module = x.Module,
                Description = x.Description,

                ParentFeatureId = x.ParentFeatureId,
                ParentFeatureName = x.ParentFeature != null
                                        ? x.ParentFeature.Name
                                        : null,

                DisplayOrder = x.DisplayOrder,

                ControllerName = x.ControllerName,
                ActionName = x.ActionName,
                AreaName = x.AreaName,
                Url = x.Url,

                IsVisible = x.IsVisible,
                IsMenu = x.IsMenu,
                IsActive = x.IsActive,

                Icon = x.Icon,
                BadgeText = x.BadgeText,

                CanView = x.CanView,
                CanAdd = x.CanAdd,
                CanEdit = x.CanEdit,
                CanDelete = x.CanDelete,
                CanApprove = x.CanApprove,
                CanExport = x.CanExport,
                CanPrint = x.CanPrint,

                AppFeatureType = x.AppFeatureType,
                IsHRMSFeature = x.IsHRMSFeature

            }).ToList();
        }

        // ======================================================
        // GET BY ID
        // ======================================================

        public async Task<AppFeatureDto?> GetByIdAsync(string id)
        {
            var x = await _context.AppFeatures
                .Include(a => a.ParentFeature)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (x == null)
                return null;

            return new AppFeatureDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                Module = x.Module,
                Description = x.Description,

                ParentFeatureId = x.ParentFeatureId,
                ParentFeatureName = x.ParentFeature?.Name,

                DisplayOrder = x.DisplayOrder,

                ControllerName = x.ControllerName,
                ActionName = x.ActionName,
                AreaName = x.AreaName,
                Url = x.Url,

                IsVisible = x.IsVisible,
                IsMenu = x.IsMenu,
                IsActive = x.IsActive,

                Icon = x.Icon,
                BadgeText = x.BadgeText,

                CanView = x.CanView,
                CanAdd = x.CanAdd,
                CanEdit = x.CanEdit,
                CanDelete = x.CanDelete,
                CanApprove = x.CanApprove,
                CanExport = x.CanExport,
                CanPrint = x.CanPrint,

                AppFeatureType = x.AppFeatureType,
                IsHRMSFeature = x.IsHRMSFeature,

                CreatedBy = x.CreatedBy,
                ModifiedBy = x.ModifiedBy,
                ModifiedOn = x.ModifiedOn,
            };
        }

        // ======================================================
        // CREATE
        // ======================================================

        public async Task<AppFeatureDto> CreateAsync(AppFeatureDto dto)
        {
            var entity = new AppFeature
            {
                Id=IDManager.GetNewId(new AppFeature()),
                Name = dto.Name,
                Code = dto.Code,
                Module = dto.Module,
                Description = dto.Description,

                ParentFeatureId = dto.ParentFeatureId,
                DisplayOrder = dto.DisplayOrder,

                ControllerName = dto.ControllerName,
                ActionName = dto.ActionName,
                AreaName = dto.AreaName,

                IsVisible = dto.IsVisible,
                IsMenu = dto.IsMenu,
                IsActive = dto.IsActive,

                Icon = dto.Icon,
                BadgeText = dto.BadgeText,

                CanView = dto.CanView,
                CanAdd = dto.CanAdd,
                CanEdit = dto.CanEdit,
                CanDelete = dto.CanDelete,
                CanApprove = dto.CanApprove,
                CanExport = dto.CanExport,
                CanPrint = dto.CanPrint,

                AppFeatureType = dto.AppFeatureType,
                IsHRMSFeature = dto.IsHRMSFeature,

                CreatedBy = dto.CreatedBy,
            };

            await _context.AppFeatures.AddAsync(entity);
            await _context.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
        }

        // ======================================================
        // UPDATE
        // ======================================================

        public async Task<AppFeatureDto?> UpdateAsync(AppFeatureDto dto)
        {
            var entity = await _context.AppFeatures
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (entity == null)
                return null;

            entity.Name = dto.Name;
            entity.Code = dto.Code;
            entity.Module = dto.Module;
            entity.Description = dto.Description;

            entity.ParentFeatureId = dto.ParentFeatureId;
            entity.DisplayOrder = dto.DisplayOrder;

            entity.ControllerName = dto.ControllerName;
            entity.ActionName = dto.ActionName;
            entity.AreaName = dto.AreaName;

            entity.IsVisible = dto.IsVisible;
            entity.IsMenu = dto.IsMenu;
            entity.IsActive = dto.IsActive;

            entity.Icon = dto.Icon;
            entity.BadgeText = dto.BadgeText;

            entity.CanView = dto.CanView;
            entity.CanAdd = dto.CanAdd;
            entity.CanEdit = dto.CanEdit;
            entity.CanDelete = dto.CanDelete;
            entity.CanApprove = dto.CanApprove;
            entity.CanExport = dto.CanExport;
            entity.CanPrint = dto.CanPrint;

            entity.AppFeatureType = dto.AppFeatureType;
            entity.IsHRMSFeature = dto.IsHRMSFeature;

            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedOn = dto.ModifiedOn;

            _context.AppFeatures.Update(entity);

            await _context.SaveChangesAsync();

            return dto;
        }

        // ======================================================
        // DELETE
        // ======================================================

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _context.AppFeatures
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.AppFeatures.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
        }

        // =========================================================
        // LOAD MENU FROM DATABASE
        // =========================================================

        public async Task<List<AppFeatureDto>> GetMenuAsync()
        {
            return await _context.AppFeatures
                .AsNoTracking()
                .Where(x => x.IsActive && x.IsMenu)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new AppFeatureDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    ParentFeatureId = x.ParentFeatureId,
                    Icon = x.Icon,

                    ControllerName = x.ControllerName,
                    ActionName = x.ActionName,
                    AreaName = x.AreaName,

                    Url = !string.IsNullOrEmpty(x.AreaName)
                            ? "/" + x.AreaName + "/" + x.ControllerName + "/" + x.ActionName
                            : "/" + x.ControllerName + "/" + x.ActionName,

                    DisplayOrder = x.DisplayOrder
                })
                .ToListAsync();
        }
    }
}
