using Application.DTOs.Communication;
using Application.Interfaces.Communication;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Communication
{
    public class EventService : IEventService
    {
        private readonly ApplicationDbContext _context;

        public EventService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<EventListDto>> GetAllAsync()
        {
            try
            {
            var list = await _context.Events
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.StartDate)
                .Select(x => new EventListDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    EventType = (int)x.EventType,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    Location = x.Location,
                    OrganizedBy = x.OrganizedBy,
                    IsForAll = x.IsForAll,
                    DepartmentName = x.Department != null ? x.Department.Name : "",
                    ParticipantCount = _context.EventParticipants
                        .Count(p => p.EventId == x.Id && !p.IsDeleted)
                })
                .ToListAsync();

            foreach (var item in list)
                item.TypeText = ((EventType)item.EventType).ToString();

            return list;
            }
            catch (Exception)
            {
                return new List<EventListDto>();
            }
        }

        #endregion

        #region Get By Id

        public async Task<EventDto> GetByIdAsync(string id)
        {
            try
            {
            var entity = await _context.Events
                .Include(x => x.Department)
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return null;

            return new EventDto
            {
                Id = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                Location = entity.Location,
                EventType = (int)entity.EventType,
                TypeText = entity.EventType.ToString(),
                OrganizedBy = entity.OrganizedBy,
                IsForAll = entity.IsForAll,
                DepartmentId = entity.DepartmentId,
                DepartmentName = entity.Department != null ? entity.Department.Name : "",
                RoleId = entity.RoleId,
                RoleName = entity.Role != null ? entity.Role.Name : "",
                SendReminder = entity.SendReminder,
                ReminderBeforeMinutes = entity.ReminderBeforeMinutes,
                CompanyId = entity.CompanyId,
                BranchId = entity.BranchId,
                ParticipantCount = await _context.EventParticipants
                    .CountAsync(p => p.EventId == id && !p.IsDeleted),
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

        public async Task<string> CreateAsync(EventDto dto)
        {
            try
            {
            var entity = new Event
            {
                Id = IDManager.GetNewId(new Event()),
                Title = dto.Title,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Location = dto.Location,
                EventType = (EventType)dto.EventType,
                OrganizedBy = dto.OrganizedBy,
                IsForAll = dto.IsForAll,
                DepartmentId = dto.DepartmentId,
                RoleId = dto.RoleId,
                SendReminder = dto.SendReminder,
                ReminderBeforeMinutes = dto.ReminderBeforeMinutes,
                CompanyId = dto.CompanyId,
                BranchId = dto.BranchId,
                TenantId = dto.TenantId,
                CreatedBy = dto.CreatedBy
            };

            await _context.Events.AddAsync(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        #endregion

        #region Update

        public async Task<string> UpdateAsync(string id, EventDto dto)
        {
            try
            {
            var entity = await _context.Events
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return "Event Not Found";

            entity.Title = dto.Title;
            entity.Description = dto.Description;
            entity.StartDate = dto.StartDate;
            entity.EndDate = dto.EndDate;
            entity.StartTime = dto.StartTime;
            entity.EndTime = dto.EndTime;
            entity.Location = dto.Location;
            entity.EventType = (EventType)dto.EventType;
            entity.OrganizedBy = dto.OrganizedBy;
            entity.IsForAll = dto.IsForAll;
            entity.DepartmentId = dto.DepartmentId;
            entity.RoleId = dto.RoleId;
            entity.SendReminder = dto.SendReminder;
            entity.ReminderBeforeMinutes = dto.ReminderBeforeMinutes;
            entity.CompanyId = dto.CompanyId;
            entity.BranchId = dto.BranchId;
            entity.TenantId = dto.TenantId;
            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedOn = dto.ModifiedOn ?? DateTime.UtcNow;

            _context.Events.Update(entity);
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
            var entity = await _context.Events
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.ModifiedOn = DateTime.UtcNow;

            _context.Events.Update(entity);
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
