using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EsslIntegration
{
    /// <summary>
    /// A dedicated, SEPARATE EF Core DbContext for the eTimeTrackLite1
    /// database - deliberately not folded into ApplicationDbContext
    /// (requirement #11: "Do not force eTimeTrackLite entities into the
    /// primary HRMS EF Core DbContext"). Two independent connection strings,
    /// two independent DbContexts, exactly like two independent databases.
    ///
    /// READ-ONLY BY CONVENTION AT THE CODE LEVEL: nothing in this codebase
    /// ever calls SaveChangesAsync on this context (see
    /// EsslAttendanceDataSource - every query is .AsNoTracking()). The real,
    /// enforced guarantee must come from the SQL login itself having
    /// db_datareader/SELECT-only grants on etimetracklite1 (requirement
    /// #25) - that is a DBA/deployment step, not something C# can guarantee
    /// on its own, and is called out explicitly in the deployment docs.
    ///
    /// NOT part of the EF Core migrations pipeline - `dotnet ef migrations`
    /// must always be run with `--context ApplicationDbContext`. This
    /// context's OnModelCreating exists only to describe the ALREADY-EXISTING
    /// eTimeTrackLite1 tables well enough for LINQ queries to compile; it must
    /// never generate or apply a migration against a device vendor's database
    /// (requirement: "Do NOT modify the eTimeTrackLite1 database schema").
    /// </summary>
    public class EsslDbContext : DbContext
    {
        public EsslDbContext(DbContextOptions<EsslDbContext> options)
            : base(options)
        {
        }

        public DbSet<EsslDeviceLogRaw> DeviceLogs { get; set; }

        public DbSet<EsslEmployeeRaw> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Exact schema per the supplied esslDevicelogs.sql - column
            // names/types/PK verified against that script, not assumed.
            modelBuilder.Entity<EsslDeviceLogRaw>(entity =>
            {
                entity.ToTable("DeviceLogs");
                entity.HasKey(e => e.DeviceLogId);
                entity.Property(e => e.DeviceLogId).HasColumnName("DeviceLogId").ValueGeneratedOnAdd();
                entity.Property(e => e.DeviceId).HasColumnName("DeviceId");
                entity.Property(e => e.UserId).HasColumnName("UserId").HasMaxLength(50);
                entity.Property(e => e.LogDate).HasColumnName("LogDate");
                entity.Property(e => e.DownloadDate).HasColumnName("DownloadDate");
                entity.Property(e => e.Direction).HasColumnName("Direction");
                entity.Property(e => e.AttDirection).HasColumnName("AttDirection");
                entity.Property(e => e.WorkCode).HasColumnName("WorkCode");

                // C1-C7 are intentionally NOT mapped here (Ignore'd below) -
                // this DbContext/DbSet is only ever used for the trivial
                // single-table "Test Connection" sample query
                // (EsslAttendanceDataSource.TestRawConnectionAsync), which
                // never selects them, and their real underlying SQL type is
                // unconfirmed (see EsslDeviceLogRaw's remarks) - mapping them
                // here with a possibly-wrong type could break that query for
                // no benefit. The actual sync path (GetDeviceLogsAsync) reads
                // C1-C7 via raw ADO.NET with an explicit CONVERT instead,
                // which sidesteps the type-guessing problem entirely.
                entity.Ignore(e => e.C1);
                entity.Ignore(e => e.C2);
                entity.Ignore(e => e.C3);
                entity.Ignore(e => e.C4);
                entity.Ignore(e => e.C5);
                entity.Ignore(e => e.C6);
                entity.Ignore(e => e.C7);

                // SourceTable is never a real eTimeTrackLite1 column - it is
                // populated in memory only by the raw-ADO.NET read path in
                // EsslAttendanceDataSource.GetDeviceLogsAsync, never by EF
                // Core, which would otherwise try (and fail) to find a
                // matching column for it.
                entity.Ignore(e => e.SourceTable);

                // Helps the (LogDate range) + (DeviceLogId keyset) query
                // pattern EsslAttendanceDataSource uses - if the real table
                // already has an equivalent index this is a harmless no-op
                // for LINQ purposes (EF never creates it; this DbContext is
                // excluded from migrations, see class remarks above). Purely
                // documents the query shape this integration depends on.
                entity.HasIndex(e => e.LogDate);
            });

            // Exact schema per the supplied esslEmployeeAdd.sql - PRIMARY
            // KEY really is EmployeeCode on the real table, not EmployeeId.
            modelBuilder.Entity<EsslEmployeeRaw>(entity =>
            {
                entity.ToTable("Employees");
                entity.HasKey(e => e.EmployeeCode);
                entity.Property(e => e.EmployeeCode).HasColumnName("EmployeeCode").HasMaxLength(50);
                entity.Property(e => e.EmployeeName).HasColumnName("EmployeeName").HasMaxLength(50);
                entity.Property(e => e.EmployeeCodeInDevice).HasColumnName("EmployeeCodeInDevice").HasMaxLength(50);
                entity.Property(e => e.Status).HasColumnName("Status");
                entity.Property(e => e.Designation).HasColumnName("Designation");
            });
        }
    }
}
