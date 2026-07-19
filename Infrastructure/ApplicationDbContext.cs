using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Infrastructure
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ITenantService _tenantService;
        public ApplicationDbContext(DbContextOptions options, ITenantService tenantService) : base(options)
        {
            _tenantService = tenantService;
        }

        #region 📊 API LOGGING
        public DbSet<ApiRequestLog> ApiRequestLogs { get; set; }
        public DbSet<ApiResponseLog> ApiResponseLogs { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }
        #endregion

        #region 🔐 AUTH MODULE
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<RoleFeature> RoleFeatures { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<LoginHistory> LoginHistories { get; set; }
        #endregion

        #region 🏢 TENANT & SUBSCRIPTION
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<TenantFeature> TenantFeatures { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }

        #endregion

        #region 🏢 ORGANIZATION
        public DbSet<Company> Companies { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Designation> Designations { get; set; }
        #endregion

        #region 🌍 LOCATION
        public DbSet<Country> Countries { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<City> Cities { get; set; }
        #endregion

        #region 👤 EMPLOYEE
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeDocument> EmployeeDocuments { get; set; }
        public DbSet<EmployeeBankDetail> EmployeeBankDetails { get; set; }
        public DbSet<EmployeePFDetail> EmployeePFDetails { get; set; }
        public DbSet<EmployeeESICDetail> EmployeeESICDetails { get; set; }
        public DbSet<EmployeeEducationDetail> EmployeeEducationDetails { get; set; }
        public DbSet<EmployeeShiftMapping> EmployeeShiftMappings { get; set; }
        #endregion

        #region ⏱️ ATTENDANCE
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<AttendanceLog> AttendanceLogs { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<BiometricDevice> BiometricDevices { get; set; }
        public DbSet<BiometricAttendanceLog> BiometricAttendanceLogs { get; set; }
        public DbSet<EmployeeBiometricMapping> EmployeeBiometricMappings { get; set; }
        public DbSet<AttendanceRegularization> AttendanceRegularizations { get; set; }
        public DbSet<AttendanceRegularizationApprovalHistory> AttendanceRegularizationApprovalHistories { get; set; }

        #endregion

        #region  📅 LEAVE
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveBalance> LeaveBalances { get; set; }
        public DbSet<LeaveBalanceTransaction> LeaveBalanceTransactions { get; set; }
        public DbSet<LeaveApplication> LeaveApplications { get; set; }
        public DbSet<LeaveApprovalHistory> LeaveApprovalHistories { get; set; }
        public DbSet<ApprovalDelegation> ApprovalDelegations { get; set; }
        #endregion

        #region 💰 PAYROLL
        public DbSet<Payroll> Payrolls { get; set; }
        public DbSet<PayrollDetail> PayrollDetails { get; set; }
        public DbSet<Payslip> Payslips { get; set; }
        public DbSet<SalaryComponent> SalaryComponents { get; set; }
        public DbSet<SalaryStructure> SalaryStructures { get; set; }
        public DbSet<SalaryDetail> SalaryDetails { get; set; }
        #endregion

        #region 💼 ASSET
        public DbSet<Asset> Assets { get; set; }
        public DbSet<AssetCategory> AssetCategories { get; set; }
        public DbSet<AssetAllocation> AssetAllocations { get; set; }
        public DbSet<AssetHistory> AssetHistories { get; set; }
        #endregion

        #region ✅ TASKS
        public DbSet<EmployeeTask> EmployeeTasks { get; set; }
        #endregion

        #region 🎯 RECRUITMENT
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<JobOpening> JobOpenings { get; set; }
        public DbSet<CandidateApplication> CandidateApplications { get; set; }
        public DbSet<InterviewSchedule> InterviewSchedules { get; set; }
        #endregion

        #region 📢 NOTIFICATION
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationGroup> NotificationGroups { get; set; }
        public DbSet<NotificationGroupAccess> NotificationGroupAccesses { get; set; }
        public DbSet<NotificationGroupUser> NotificationGroupUsers { get; set; }
        public DbSet<NotificationRecipient> NotificationRecipients { get; set; }
        #endregion

        #region ⚙️ MASTER TABLES
        public DbSet<AppFeature> AppFeatures { get; set; }
        public DbSet<FinancialYear> FinancialYears { get; set; }
        public DbSet<HolidayGroup> HolidayGroups { get; set; }
        public DbSet<HolidayGroupDetail> HolidayGroupDetails { get; set; }
        public DbSet<WeekOff> WeekOffs { get; set; }
        public DbSet<SequenceMaster> SequenceMasters { get; set; }
        #endregion

        #region 📢 ANNOUNCEMENT & EVENTS
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventParticipant> EventParticipants { get; set; }

        #endregion

        #region 🎫 HELP & SUPPORT
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<SupportTicketReply> SupportTicketReplies { get; set; }
        public DbSet<FaqItem> FaqItems { get; set; }

        #endregion

        #region 🔥 MODEL CONFIGURATION
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================================
            // 🔐 UNIQUE INDEXES
            // =====================================================
            modelBuilder.Entity<ApiResponseLog>(entity =>
            {
                entity.ToTable("ApiResponseLogs");

                entity.HasKey(e => e.Id);

                // 🔹 Identity / Codes
                entity.Property(e => e.Id)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.RequestId)
                    .HasMaxLength(50);

                entity.Property(e => e.CorrelationId)
                    .HasMaxLength(100);

                // 🔹 API Info
                entity.Property(e => e.Endpoint)
                    .HasMaxLength(500);

                entity.Property(e => e.Controller)
                    .HasMaxLength(100);

                entity.Property(e => e.Action)
                    .HasMaxLength(100);

                entity.Property(e => e.Method)
                    .HasMaxLength(10);

                // 🔹 Response
                entity.Property(e => e.ResponseBody)
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.StatusCode);

                entity.Property(e => e.IsSuccess);

                // 🔹 Performance
                entity.Property(e => e.ExecutionTimeMs);

                // 🔹 User
                entity.Property(e => e.UserId)
                    .HasMaxLength(50);

                entity.Property(e => e.UserName)
                    .HasMaxLength(150);

                // 🔹 Multi-Tenant
                entity.Property(e => e.CompanyId)
                    .HasMaxLength(50);

                entity.Property(e => e.BranchId)
                    .HasMaxLength(50);

                // 🔹 Client
                entity.Property(e => e.IPAddress)
                    .HasMaxLength(50);

                entity.Property(e => e.UserAgent)
                    .HasMaxLength(500);

                // 🔹 Time
                entity.Property(e => e.ResponseTime)
                    .IsRequired();

                // 🔹 Extra
                entity.Property(e => e.Remarks)
                    .HasMaxLength(500);

                // 🔹 Indexes (IMPORTANT)
                entity.HasIndex(e => e.Id).IsUnique();
                entity.HasIndex(e => e.RequestId);
                entity.HasIndex(e => e.CorrelationId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CompanyId);
                entity.HasIndex(e => e.ResponseTime);
            });

            modelBuilder.Entity<ErrorLog>(entity =>
            {
                entity.ToTable("ErrorLogs");

                entity.HasKey(e => e.Id);

                // 🔹 Identity
                entity.Property(e => e.Id)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.RequestId)
                    .HasMaxLength(50);

                entity.Property(e => e.CorrelationId)
                    .HasMaxLength(100);

                // 🔹 Error Info
                entity.Property(e => e.ErrorMessage)
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.ExceptionType)
                    .HasMaxLength(200);

                entity.Property(e => e.StackTrace)
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.InnerException)
                    .HasColumnType("nvarchar(max)");

                // 🔹 API Context
                entity.Property(e => e.Endpoint)
                    .HasMaxLength(500);

                entity.Property(e => e.Controller)
                    .HasMaxLength(100);

                entity.Property(e => e.Action)
                    .HasMaxLength(100);

                entity.Property(e => e.Method)
                    .HasMaxLength(10);

                // 🔹 Request Snapshot
                entity.Property(e => e.RequestBody)
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.QueryParams)
                    .HasColumnType("nvarchar(max)");

                // 🔹 User
                entity.Property(e => e.UserId)
                    .HasMaxLength(50);

                entity.Property(e => e.UserName)
                    .HasMaxLength(150);

                // 🔹 Multi-Tenant
                entity.Property(e => e.CompanyId)
                    .HasMaxLength(50);

                // 🔹 Client
                entity.Property(e => e.IPAddress)
                    .HasMaxLength(50);

                entity.Property(e => e.UserAgent)
                    .HasMaxLength(500);

                // 🔹 Severity
                entity.Property(e => e.LogLevel)
                    .HasMaxLength(20);

                // 🔹 Time
                entity.Property(e => e.ErrorTime)
                    .IsRequired();

                // 🔹 Resolution
                entity.Property(e => e.IsResolved);

                entity.Property(e => e.ResolvedBy)
                    .HasMaxLength(100);

                entity.Property(e => e.ResolvedOn);

                // 🔹 Extra
                entity.Property(e => e.Remarks)
                    .HasMaxLength(500);

                // 🔹 Indexes (CRITICAL for debugging)
                entity.HasIndex(e => e.Id).IsUnique();
                entity.HasIndex(e => e.RequestId);
                entity.HasIndex(e => e.CorrelationId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CompanyId);
                entity.HasIndex(e => e.ErrorTime);
                entity.HasIndex(e => e.LogLevel);
            });

            modelBuilder.Entity<User>()
                .HasIndex(x => x.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(x => x.Email)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .HasIndex(x => x.EmployeeCode)
                .IsUnique();

            modelBuilder.Entity<Asset>()
                .HasIndex(x => x.AssetCode)
                .IsUnique();

            modelBuilder.Entity<Attendance>()
                .HasIndex(x => new { x.EmployeeId, x.Date });

            modelBuilder.Entity<AttendanceRegularization>()
                .HasIndex(x => new { x.EmployeeId, x.Date });

            modelBuilder.Entity<Payroll>()
                .HasIndex(x => new { x.EmployeeId, x.SalaryMonth });

            // =====================================================
            // 💰 SALARY STRUCTURE
            // =====================================================
            modelBuilder.Entity<SalaryStructure>()
                .HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalaryStructure>()
                .HasMany(x => x.SalaryDetails)
                .WithOne(x => x.EmployeeSalaryStructure)
                .HasForeignKey(x => x.SalaryStructureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalaryDetail>()
                .HasOne(x => x.SalaryComponent)
                .WithMany()
                .HasForeignKey(x => x.SalaryComponentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalaryStructure>()
                .HasIndex(x => new { x.EmployeeId, x.EffectiveFrom });

            // =====================================================
            // ✅ TASKS
            // =====================================================
            modelBuilder.Entity<EmployeeTask>()
                .HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeTask>()
                .HasIndex(x => new { x.EmployeeId, x.Status });

            // =====================================================
            // 🔗 USER / ROLE / PERMISSION
            // =====================================================
            modelBuilder.Entity<UserRole>()
                .HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRole>()
                .HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RolePermission>()
                .HasOne(x => x.Role)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RolePermission>()
                .HasOne(x => x.Permission)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // 🏢 TENANT / COMPANY / BRANCH (CRITICAL 🔥)
            // =====================================================
            modelBuilder.Entity<Company>()
                .HasOne(x => x.Tenant)
                .WithMany(t => t.Companies)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Branch>()
                .HasOne(x => x.Company)
                .WithMany(c => c.Branches)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Branch>()
                .HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tenant>()
            .HasOne(t => t.Country)
            .WithMany()
            .HasForeignKey(t => t.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tenant>()
                .HasOne(t => t.State)
                .WithMany()
                .HasForeignKey(t => t.StateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tenant>()
                .HasOne(t => t.City)
                .WithMany()
                .HasForeignKey(t => t.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Company>()
            .HasOne(c => c.Country)
            .WithMany()
            .HasForeignKey(c => c.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Company>()
                .HasOne(c => c.State)
                .WithMany()
                .HasForeignKey(c => c.StateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Company>()
                .HasOne(c => c.City)
                .WithMany()
                .HasForeignKey(c => c.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // 👤 SELF REFERENCES
            // =====================================================
            modelBuilder.Entity<Department>()
                .HasOne(d => d.ParentDepartment)
                .WithMany(d => d.ChildDepartments)
                .HasForeignKey(d => d.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Designation>()
                .HasOne(d => d.ParentDesignation)
                .WithMany(d => d.ChildDesignations)
                .HasForeignKey(d => d.ParentDesignationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.ReportingManager)
                .WithMany()
                .HasForeignKey(e => e.ReportingManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // 🔁 APPROVAL DELEGATION (two distinct FKs to Employee - must be
            // configured explicitly or EF can't disambiguate them)
            // =====================================================
            modelBuilder.Entity<ApprovalDelegation>()
                .HasOne(x => x.DelegatorEmployee)
                .WithMany()
                .HasForeignKey(x => x.DelegatorEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApprovalDelegation>()
                .HasOne(x => x.DelegateEmployee)
                .WithMany()
                .HasForeignKey(x => x.DelegateEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApprovalDelegation>()
                .HasIndex(x => new { x.DelegatorEmployeeId, x.StartDate, x.EndDate });

            // =====================================================
            // 🌍 LOCATION (NO CASCADE)
            // =====================================================
            modelBuilder.Entity<State>()
                .HasOne(s => s.Country)
                .WithMany(c => c.States)
                .HasForeignKey(s => s.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<City>()
                .HasOne(c => c.State)
                .WithMany(s => s.Cities)
                .HasForeignKey(c => c.StateId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // 📢 ANNOUNCEMENT
            // =====================================================
            modelBuilder.Entity<Announcement>(entity =>
            {
                entity.Property(x => x.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.Message)
                    .HasMaxLength(1000);

                entity.HasIndex(x => new { x.PublishDate, x.ExpiryDate });
            });

            // =====================================================
            // 🎉 EVENT
            // =====================================================
            modelBuilder.Entity<Event>(entity =>
            {
                entity.Property(x => x.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.Location)
                    .HasMaxLength(200);

                entity.HasIndex(x => new { x.StartDate, x.EndDate });
            });

            modelBuilder.Entity<EventParticipant>(entity =>
            {
                entity.HasOne(x => x.Event)
                    .WithMany(x => x.Participants)
                    .HasForeignKey(x => x.EventId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Employee)
                    .WithMany()
                    .HasForeignKey(x => x.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.EventId, x.EmployeeId })
                    .IsUnique();
            });

            // =====================================================
            // 🔥 GLOBAL FIX (VERY IMPORTANT)
            // =====================================================
            foreach (var fk in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // =====================================================
            // 🗑️ SOFT DELETE FILTER
            // =====================================================
            ApplySoftDeleteFilter(modelBuilder);
        }
        #endregion

        #region 🔥 GLOBAL SOFT DELETE
        private void ApplySoftDeleteFilter(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var methodInfo = typeof(ApplicationDbContext)
                        .GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static);

                    if (methodInfo == null) continue;

                    var genericMethod = methodInfo.MakeGenericMethod(entityType.ClrType);
                    genericMethod.Invoke(null, new object[] { modelBuilder });
                }
            }
        }
        #endregion

        private static void SetSoftDeleteFilter<TEntity>(ModelBuilder builder)
        where TEntity : BaseEntity
        {
            builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
        }

        // ==============================
        // 🔥 AUDIT HANDLING
        // ==============================
        public override int SaveChanges()
        {
            ApplyAudit();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAudit();

            foreach (var entry in ChangeTracker.Entries<IEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (string.IsNullOrEmpty(entry.Entity.Id))
                    {
                        entry.Entity.Id = entry.Entity.GetNewId(); // 🔥 IDManager use
                    }
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
        private void ApplyAudit()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                    entry.Entity.CreatedOn = DateTime.UtcNow;

                if (entry.State == EntityState.Modified)
                    entry.Entity.ModifiedOn = DateTime.UtcNow;
            }
        }
    }
}
