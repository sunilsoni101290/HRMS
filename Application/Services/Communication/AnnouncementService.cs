using Application.DTOs.Communication;
using Application.Interfaces.Communication;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Communication
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly ApplicationDbContext _context;

        public AnnouncementService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<AnnouncementListDto>> GetAllAsync()
        {
            try
            {
            var list = await _context.Announcements
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.PublishDate)
                .Select(x => new AnnouncementListDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    Message = x.Message,
                    AnnouncementType = (int)x.AnnouncementType,
                    Priority = (int)x.Priority,
                    PublishDate = x.PublishDate,
                    ExpiryDate = x.ExpiryDate,
                    IsForAll = x.IsForAll,
                    IsActive = x.IsActive,
                    DepartmentName = x.Department != null ? x.Department.Name : ""
                })
                .ToListAsync();

            foreach (var item in list)
            {
                item.TypeText = ((AnnouncementType)item.AnnouncementType).ToString();
                item.PriorityText = ((AnnouncementPriority)item.Priority).ToString();
                if (item.Message != null && item.Message.Length > 120)
                    item.Message = item.Message.Substring(0, 120) + "...";
            }

            return list;
            }
            catch (Exception)
            {
                return new List<AnnouncementListDto>();
            }
        }

        #endregion

        #region Get By Id

        public async Task<AnnouncementDto> GetByIdAsync(string id)
        {
            try
            {
            var entity = await _context.Announcements
                .Include(x => x.Department)
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return null;

            return new AnnouncementDto
            {
                Id = entity.Id,
                Title = entity.Title,
                Message = entity.Message,
                AnnouncementType = (int)entity.AnnouncementType,
                TypeText = entity.AnnouncementType.ToString(),
                Priority = (int)entity.Priority,
                PriorityText = entity.Priority.ToString(),
                PublishDate = entity.PublishDate,
                ExpiryDate = entity.ExpiryDate,
                IsForAll = entity.IsForAll,
                DepartmentId = entity.DepartmentId,
                DepartmentName = entity.Department != null ? entity.Department.Name : "",
                RoleId = entity.RoleId,
                RoleName = entity.Role != null ? entity.Role.Name : "",
                AttachmentUrl = entity.AttachmentUrl,
                CompanyId = entity.CompanyId,
                BranchId = entity.BranchId,
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

        public async Task<string> CreateAsync(AnnouncementDto dto)
        {
            try
            {
                var entity = new Announcement
                {
                    Id = IDManager.GetNewId(new Announcement()),
                    Title = dto.Title,
                    Message = dto.Message,
                    AnnouncementType = (AnnouncementType)dto.AnnouncementType,
                    Priority = (AnnouncementPriority)dto.Priority,
                    PublishDate = dto.PublishDate,
                    ExpiryDate = dto.ExpiryDate,
                    IsForAll = dto.IsForAll,
                    DepartmentId = dto.DepartmentId,
                    RoleId = dto.RoleId,
                    AttachmentUrl = dto.AttachmentUrl,
                    CompanyId = dto.CompanyId,
                    BranchId = dto.BranchId,
                    TenantId = dto.TenantId,
                    CreatedBy = dto.CreatedBy
                };

                await _context.Announcements.AddAsync(entity);
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

        public async Task<string> UpdateAsync(string id, AnnouncementDto dto)
        {
            try
            {
            var entity = await _context.Announcements
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return "Announcement Not Found";

            entity.Title = dto.Title;
            entity.Message = dto.Message;
            entity.AnnouncementType = (AnnouncementType)dto.AnnouncementType;
            entity.Priority = (AnnouncementPriority)dto.Priority;
            entity.PublishDate = dto.PublishDate;
            entity.ExpiryDate = dto.ExpiryDate;
            entity.IsForAll = dto.IsForAll;
            entity.DepartmentId = dto.DepartmentId;
            entity.RoleId = dto.RoleId;
            if (!string.IsNullOrEmpty(dto.AttachmentUrl))
                entity.AttachmentUrl = dto.AttachmentUrl;
            entity.CompanyId = dto.CompanyId;
            entity.BranchId = dto.BranchId;
            entity.TenantId = dto.TenantId;
            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedOn = dto.ModifiedOn ?? DateTime.UtcNow;

            _context.Announcements.Update(entity);
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
            var entity = await _context.Announcements
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.ModifiedOn = DateTime.UtcNow;

            _context.Announcements.Update(entity);
            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion
    }
}
