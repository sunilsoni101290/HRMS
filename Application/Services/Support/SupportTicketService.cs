using Application.DTOs.Support;
using Application.Interfaces.Support;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Support
{
    public class SupportTicketService : ISupportTicketService
    {
        private readonly ApplicationDbContext _context;

        public SupportTicketService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Assemble Helpers

        private static SupportTicketListDto AssembleListDto(SupportTicket x, int replyCount) => new()
        {
            Id = x.Id,
            EmployeeId = x.EmployeeId,
            EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,
            EmployeeCode = x.Employee?.EmployeeCode,
            Subject = x.Subject,
            Category = x.Category,
            CategoryText = x.Category.ToString(),
            Priority = x.Priority,
            PriorityText = x.Priority.ToString(),
            Status = x.Status,
            StatusText = x.Status.ToString(),
            ReplyCount = replyCount,
            CreatedOn = x.CreatedOn,
            ResolvedDate = x.ResolvedDate
        };

        // Resolves a display name + "is this a support/admin reply" flag
        // for a UserId, so the thread UI can style employee vs. support
        // messages differently. Looked up once per unique user in a batch
        // rather than per reply, to avoid N+1 queries on a busy thread.
        private async Task<Dictionary<string, (string Name, bool IsSupport)>> BuildUserDisplayMapAsync(IEnumerable<string> userIds)
        {
            var ids = userIds.Distinct().ToList();

            var users = await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

            var map = new Dictionary<string, (string Name, bool IsSupport)>();

            foreach (var u in users)
            {
                var name = u.Employee != null
                    ? $"{u.Employee.FirstName} {u.Employee.LastName}".Trim()
                    : u.Username;

                var roleName = u.UserRoles?
                    .Select(ur => ur.Role?.Name)
                    .FirstOrDefault(n => !string.IsNullOrEmpty(n)) ?? "";

                bool isSupport = roleName.Contains("admin", StringComparison.OrdinalIgnoreCase) ||
                                  roleName.Contains("manager", StringComparison.OrdinalIgnoreCase) ||
                                  roleName.Contains("hr", StringComparison.OrdinalIgnoreCase);

                map[u.Id] = (string.IsNullOrWhiteSpace(name) ? "Unknown User" : name, isSupport);
            }

            return map;
        }

        #endregion

        #region Queries

        public async Task<List<SupportTicketListDto>> GetAllAsync()
        {
            try
            {
                var tickets = await _context.SupportTickets
                    .Include(x => x.Employee)
                    .OrderByDescending(x => x.CreatedOn)
                    .ToListAsync();

                var replyCounts = await _context.SupportTicketReplies
                    .GroupBy(x => x.SupportTicketId)
                    .Select(g => new { TicketId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.TicketId, x => x.Count);

                return tickets
                    .Select(x => AssembleListDto(x, replyCounts.TryGetValue(x.Id, out var c) ? c : 0))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<SupportTicketListDto>();
            }
        }

        public async Task<List<SupportTicketListDto>> GetByEmployeeAsync(string employeeId)
        {
            try
            {
                if (string.IsNullOrEmpty(employeeId))
                    return new List<SupportTicketListDto>();

                var tickets = await _context.SupportTickets
                    .Include(x => x.Employee)
                    .Where(x => x.EmployeeId == employeeId)
                    .OrderByDescending(x => x.CreatedOn)
                    .ToListAsync();

                var ticketIds = tickets.Select(x => x.Id).ToList();

                var replyCounts = await _context.SupportTicketReplies
                    .Where(x => ticketIds.Contains(x.SupportTicketId))
                    .GroupBy(x => x.SupportTicketId)
                    .Select(g => new { TicketId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.TicketId, x => x.Count);

                return tickets
                    .Select(x => AssembleListDto(x, replyCounts.TryGetValue(x.Id, out var c) ? c : 0))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<SupportTicketListDto>();
            }
        }

        public async Task<SupportTicketDto> GetByIdAsync(string id)
        {
            try
            {
                var entity = await _context.SupportTickets
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    return null;

                var replies = await _context.SupportTicketReplies
                    .Where(x => x.SupportTicketId == id)
                    .OrderBy(x => x.CreatedOn)
                    .ToListAsync();

                var userMap = await BuildUserDisplayMapAsync(replies.Select(r => r.RepliedByUserId));

                var replyDtos = replies.Select(r =>
                {
                    var (name, isSupport) = userMap.TryGetValue(r.RepliedByUserId, out var v)
                        ? v
                        : ("Unknown User", false);

                    return new SupportTicketReplyDto
                    {
                        Id = r.Id,
                        SupportTicketId = r.SupportTicketId,
                        RepliedByUserId = r.RepliedByUserId,
                        RepliedByName = name,
                        IsSupportReply = isSupport,
                        Message = r.Message,
                        CreatedOn = r.CreatedOn
                    };
                }).ToList();

                return new SupportTicketDto
                {
                    Id = entity.Id,
                    EmployeeId = entity.EmployeeId,
                    EmployeeName = entity.Employee != null ? $"{entity.Employee.FirstName} {entity.Employee.LastName}".Trim() : null,
                    EmployeeCode = entity.Employee?.EmployeeCode,
                    Subject = entity.Subject,
                    Description = entity.Description,
                    Category = entity.Category,
                    CategoryText = entity.Category.ToString(),
                    Priority = entity.Priority,
                    PriorityText = entity.Priority.ToString(),
                    Status = entity.Status,
                    StatusText = entity.Status.ToString(),
                    AttachmentUrl = entity.AttachmentUrl,
                    ResolvedBy = entity.ResolvedBy,
                    ResolvedDate = entity.ResolvedDate,
                    CreatedOn = entity.CreatedOn,
                    CreatedBy = entity.CreatedBy,
                    Replies = replyDtos,
                    TenantId = entity.TenantId,
                    CompanyId = entity.CompanyId,
                    BranchId = entity.BranchId
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<int> GetOpenCountAsync()
        {
            try
            {
                return await _context.SupportTickets
                    .CountAsync(x => x.Status == TicketStatus.Open || x.Status == TicketStatus.InProgress);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        #endregion

        #region Commands

        public async Task<string> CreateAsync(CreateSupportTicketRequestDto request)
        {
            try
            {
                var entity = new SupportTicket
                {
                    Id = IDManager.GetNewId(new SupportTicket()),
                    TenantId = request.TenantId,
                    CompanyId = request.CompanyId,
                    BranchId = request.BranchId,
                    EmployeeId = request.EmployeeId,
                    Subject = request.Subject,
                    Description = request.Description,
                    Category = request.Category,
                    Priority = request.Priority,
                    Status = TicketStatus.Open,
                    AttachmentUrl = request.AttachmentUrl,
                    CreatedBy = request.CreatedBy
                };

                await _context.SupportTickets.AddAsync(entity);
                await _context.SaveChangesAsync();

                return entity.Id;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> AddReplyAsync(AddReplyRequestDto request)
        {
            try
            {
                var ticket = await _context.SupportTickets
                    .FirstOrDefaultAsync(x => x.Id == request.SupportTicketId);

                if (ticket == null)
                    return false;

                var reply = new SupportTicketReply
                {
                    Id = IDManager.GetNewId(new SupportTicketReply()),
                    TenantId = ticket.TenantId,
                    SupportTicketId = request.SupportTicketId,
                    RepliedByUserId = request.RepliedByUserId,
                    Message = request.Message,
                    CreatedBy = request.RepliedByUserId
                };

                await _context.SupportTicketReplies.AddAsync(reply);

                // A reply reopens a Resolved/Closed ticket back to
                // InProgress - the conversation isn't actually over just
                // because someone posted a follow-up message.
                if (ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.Closed)
                {
                    ticket.Status = TicketStatus.InProgress;
                    ticket.ModifiedOn = DateTime.UtcNow;
                    ticket.ModifiedBy = request.RepliedByUserId;
                }

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateStatusAsync(UpdateTicketStatusRequestDto request)
        {
            try
            {
                var ticket = await _context.SupportTickets
                    .FirstOrDefaultAsync(x => x.Id == request.SupportTicketId);

                if (ticket == null)
                    return false;

                ticket.Status = request.Status;
                ticket.ModifiedOn = DateTime.UtcNow;
                ticket.ModifiedBy = request.UpdatedByUserId;

                if (request.Status == TicketStatus.Resolved || request.Status == TicketStatus.Closed)
                {
                    ticket.ResolvedDate = DateTime.UtcNow;
                    ticket.ResolvedBy = request.UpdatedByUserId;
                }
                else
                {
                    ticket.ResolvedDate = null;
                    ticket.ResolvedBy = null;
                }

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
