using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Application.Common.Exceptions;
using Application.DTOs.DatabaseManagement;
using Application.DTOs.Employee;
using Application.Interfaces.DatabaseManagement;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services.DatabaseManagement
{
    /// <summary>
    /// Phase A implementation - see IDatabaseManagementService's remarks.
    /// Permission check mirrors ErrorLogService.EnsurePermissionAsync (a
    /// real, data-driven RolePermission/Permission check against
    /// AppFeatureConstants.DATABASE_MANAGEMENT, not a hard-coded role-name
    /// string) - in practice only System Configurator ever holds it (see
    /// DbSeeder's DATABASE_MANAGEMENT carve-out), but a tenant could grant
    /// it to another role later without any code change here.
    /// </summary>
    public class DatabaseManagementService : IDatabaseManagementService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DatabaseManagementService> _logger;

        // Short, deliberately conservative command timeout for the
        // Database Info panel's live queries - this must be fast (it's a
        // read used on ordinary page load/Refresh), and any failure
        // degrades to "Unavailable" rather than a 500 (see
        // GetDatabaseInfoAsync's remarks / the Phase A scope's explicit
        // "graceful handling" requirement).
        private const int DatabaseInfoCommandTimeoutSeconds = 5;

        public DatabaseManagementService(ApplicationDbContext db, ILogger<DatabaseManagementService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<DatabaseManagementSettingsDto> GetSettingsAsync(string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.View);

            var setting = await _db.DatabaseManagementSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId);

            if (setting == null)
            {
                // No row saved yet for this tenant - surface the entity's
                // own defaults as the initial form contents (same "legacy
                // fallback" shape as EsslAttendanceSyncService.GetConfigurationAsync).
                var defaults = new DatabaseManagementSettings();

                return new DatabaseManagementSettingsDto
                {
                    Id = null,
                    AutoBackupBeforeUpdate = defaults.AutoBackupBeforeUpdate,
                    MaxScriptFileSizeBytes = defaults.MaxScriptFileSizeBytes,
                    AllowedEnvironments = defaults.AllowedEnvironments,
                    SqlCommandTimeoutSeconds = defaults.SqlCommandTimeoutSeconds,
                    BackupDirectoryPath = defaults.BackupDirectoryPath,
                    BackupRetentionDays = defaults.BackupRetentionDays,
                    RiskyStatementValidationPolicy = defaults.RiskyStatementValidationPolicy,
                    RequireConfirmationBeforeExecution = defaults.RequireConfirmationBeforeExecution,
                    DatabaseSwapEnabled = defaults.DatabaseSwapEnabled,
                    ExecutionLogRetentionDays = defaults.ExecutionLogRetentionDays
                };
            }

            return new DatabaseManagementSettingsDto
            {
                Id = setting.Id,
                AutoBackupBeforeUpdate = setting.AutoBackupBeforeUpdate,
                MaxScriptFileSizeBytes = setting.MaxScriptFileSizeBytes,
                AllowedEnvironments = setting.AllowedEnvironments,
                SqlCommandTimeoutSeconds = setting.SqlCommandTimeoutSeconds,
                BackupDirectoryPath = setting.BackupDirectoryPath,
                BackupRetentionDays = setting.BackupRetentionDays,
                RiskyStatementValidationPolicy = setting.RiskyStatementValidationPolicy,
                RequireConfirmationBeforeExecution = setting.RequireConfirmationBeforeExecution,
                DatabaseSwapEnabled = setting.DatabaseSwapEnabled,
                ExecutionLogRetentionDays = setting.ExecutionLogRetentionDays,
                ModifiedOn = setting.ModifiedOn,
                ModifiedBy = setting.ModifiedBy
            };
        }

        public async Task<(bool Success, string Message)> SaveSettingsAsync(DatabaseManagementSettingsDto dto, string tenantId, string modifiedBy)
        {
            await EnsurePermissionAsync(modifiedBy, Actions.Edit);

            // Server-side validation beyond DataAnnotations' Range/Required
            // (never rely on the client alone) - cross-field / allowed-set
            // rules that attributes alone can't express.
            var allowedPolicies = new[] { "Strict", "Warn", "Off" };
            if (!allowedPolicies.Contains(dto.RiskyStatementValidationPolicy, StringComparer.OrdinalIgnoreCase))
                return (false, "Risky statement validation policy must be Strict, Warn, or Off.");

            if (string.IsNullOrWhiteSpace(dto.AllowedEnvironments))
                return (false, "Allowed environments is required.");

            if (string.IsNullOrWhiteSpace(dto.BackupDirectoryPath))
                return (false, "Backup directory path is required.");

            if (dto.SqlCommandTimeoutSeconds < 5 || dto.SqlCommandTimeoutSeconds > 600)
                return (false, "SQL command timeout must be between 5 and 600 seconds.");

            if (dto.BackupRetentionDays < 1 || dto.BackupRetentionDays > 365)
                return (false, "Backup retention must be between 1 and 365 days.");

            if (dto.ExecutionLogRetentionDays < 1 || dto.ExecutionLogRetentionDays > 365)
                return (false, "Execution log retention must be between 1 and 365 days.");

            if (dto.MaxScriptFileSizeBytes < 1024 || dto.MaxScriptFileSizeBytes > 1024L * 1024 * 1024)
                return (false, "Max script file size must be between 1 KB and 1 GB.");

            var setting = await _db.DatabaseManagementSettings
                .FirstOrDefaultAsync(x => x.TenantId == tenantId);

            if (setting == null)
            {
                setting = new DatabaseManagementSettings
                {
                    Id = IDManager.GetNewId(new DatabaseManagementSettings()),
                    TenantId = tenantId,
                    CreatedBy = modifiedBy,
                    CreatedOn = DateTime.UtcNow
                };
                _db.DatabaseManagementSettings.Add(setting);
            }

            setting.AutoBackupBeforeUpdate = dto.AutoBackupBeforeUpdate;
            setting.MaxScriptFileSizeBytes = dto.MaxScriptFileSizeBytes;
            setting.AllowedEnvironments = dto.AllowedEnvironments.Trim();
            setting.SqlCommandTimeoutSeconds = dto.SqlCommandTimeoutSeconds;
            setting.BackupDirectoryPath = dto.BackupDirectoryPath.Trim();
            setting.BackupRetentionDays = dto.BackupRetentionDays;
            setting.RiskyStatementValidationPolicy = dto.RiskyStatementValidationPolicy;
            setting.RequireConfirmationBeforeExecution = dto.RequireConfirmationBeforeExecution;
            setting.DatabaseSwapEnabled = dto.DatabaseSwapEnabled;
            setting.ExecutionLogRetentionDays = dto.ExecutionLogRetentionDays;
            setting.ModifiedBy = modifiedBy;
            setting.ModifiedOn = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return (true, "Database Management settings saved successfully.");
        }

        // ==================================================================
        // DATABASE INFO PANEL - real, read-only, live SQL Server data.
        // Opens its own short-lived SqlConnection off the same connection
        // string ApplicationDbContext already uses (same pattern as
        // EsslAttendanceSyncService's bulk-staging path - see
        // "_db.Database.GetConnectionString()"), with a short, fixed
        // command timeout so this can never hang the page. ANY failure
        // (unreachable server, permission denied, etc.) is caught here and
        // turned into Available=false + a clear reason - this must never
        // surface as a 500 to the Database Info panel.
        // ==================================================================

        public async Task<DatabaseInfoDto> GetDatabaseInfoAsync(string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.View);

            var result = new DatabaseInfoDto { Available = false };

            try
            {
                var connectionString = _db.Database.GetConnectionString();

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    result.UnavailableReason = "No database connection string is configured.";
                    return result;
                }

                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                await using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandTimeout = DatabaseInfoCommandTimeoutSeconds;
                    cmd.CommandText = "SELECT DB_NAME(), @@SERVERNAME, " +
                        "(SELECT SUM(CAST(size AS BIGINT)) * 8.0 / 1024 FROM sys.master_files WHERE database_id = DB_ID());";

                    await using var reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        result.DatabaseName = reader.IsDBNull(0) ? null : reader.GetString(0);
                        result.ServerName = reader.IsDBNull(1) ? null : reader.GetString(1);
                        result.DatabaseSizeMb = reader.IsDBNull(2) ? (double?)null : Math.Round(reader.GetDouble(2), 1);
                    }
                }

                result.Available = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database Management: Database Info panel query failed - degrading to Unavailable.");
                result.Available = false;
                result.UnavailableReason = "Could not read live database information right now. The database may be unreachable or busy.";
            }

            // Last Backup - MAX(CompletedAt) WHERE Status = 'Success' from
            // DatabaseBackupHistory (this repo's own EF-tracked table, not
            // the raw SqlConnection above) - correctly null/"Never" until a
            // later phase starts writing backup rows. A failure here must
            // not blank out the server/database/size values already read
            // above, so it's caught independently.
            try
            {
                result.LastBackupOn = await _db.DatabaseBackupHistories
                    .AsNoTracking()
                    .Where(x => x.TenantId == tenantId && x.Status == "Success" && x.CompletedAt != null)
                    .OrderByDescending(x => x.CompletedAt)
                    .Select(x => x.CompletedAt)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database Management: Last Backup lookup failed.");
                result.LastBackupOn = null;
            }

            return result;
        }

        public async Task<PagedResult<DatabaseOperationHistoryDto>> GetOperationHistoryAsync(
            DatabaseOperationHistoryFilterDto filter, string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.View);

            var query = _db.DatabaseOperationHistories
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .AsQueryable();

            if (filter.DateFrom.HasValue)
                query = query.Where(x => x.StartedAt >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
            {
                var to = filter.DateTo.Value.Date.AddDays(1);
                query = query.Where(x => x.StartedAt < to);
            }

            if (!string.IsNullOrWhiteSpace(filter.OperationType))
                query = query.Where(x => x.OperationType == filter.OperationType);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(x => x.Status == filter.Status);

            var total = await query.CountAsync();

            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = filter.PageSize is < 1 or > 500 ? 20 : filter.PageSize;

            var entities = await query
                .OrderByDescending(x => x.StartedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = entities.Select(x => new DatabaseOperationHistoryDto
            {
                Id = x.Id,
                OperationType = x.OperationType,
                ScriptFileName = x.ScriptFileName,
                TargetServer = x.TargetServer,
                TargetDatabase = x.TargetDatabase,
                Environment = x.Environment,
                Action = x.Action,
                Status = x.Status,
                StartedAt = x.StartedAt,
                CompletedAt = x.CompletedAt,
                ExecutedBy = x.ExecutedBy,
                Message = x.Message,
                AffectedObjectsCount = x.AffectedObjectsCount,
                ExecutionDuration = x.ExecutionDuration
            }).ToList();

            return new PagedResult<DatabaseOperationHistoryDto>
            {
                TotalRecords = total,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Data = items
            };
        }

        public async Task<DatabaseConnectionInfoDto> GetConnectionInfoAsync(string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.View);

            // Deliberately reuses the same live @@SERVERNAME/DB_NAME() read
            // as the Database Info panel rather than parsing the connection
            // string, so this never risks surfacing the raw connection
            // string (which may contain a password) - only the resolved
            // server/database names, exactly per the Phase A scope's
            // "never the password/full connection string" requirement.
            var info = await GetDatabaseInfoAsync(tenantId, actingUserId);

            return new DatabaseConnectionInfoDto
            {
                ServerName = info.ServerName,
                DatabaseName = info.DatabaseName
            };
        }

        // ==================================================================
        // PERMISSION CHECK - same shape as ErrorLogService.EnsurePermissionAsync.
        // ==================================================================

        private async Task EnsurePermissionAsync(string? actingUserId, string action)
        {
            if (string.IsNullOrEmpty(actingUserId))
                throw new UnauthorizedException("You are not authorized to access Database Management.");

            var allowed = await (
                from ur in _db.UserRoles.AsNoTracking()
                join rp in _db.RolePermissions.AsNoTracking().Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _db.Permissions.AsNoTracking().Where(x =>
                        x.FeatureId == AppFeatureConstants.DATABASE_MANAGEMENT && x.Action == action)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();

            if (!allowed)
                throw new UnauthorizedException("You are not authorized to access Database Management.");
        }
    }
}
