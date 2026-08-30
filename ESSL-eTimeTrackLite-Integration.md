# eSSL eTimeTrackLite1 Direct-SQL Attendance Integration

## Architecture

```
eSSL Biometric Device
      |
eTimeTrackLite1 Software
      |
SQL Server Database (etimetracklite1.dbo.DeviceLogs)  <- READ-ONLY, SELECT only
      |
EsslAttendanceDataSource (Application/Services/Attendances/EsslAttendanceDataSource.cs)
      |                    reads via a SEPARATE EF Core DbContext (EsslDbContext)
EsslAttendanceSyncService (Application/Services/Attendances/EsslAttendanceSyncService.cs)
      |                    resolves employee via EmployeeBiometricMapping,
      |                    writes into the EXISTING BiometricAttendanceLog table
BiometricAttendanceLog (existing staging table - unchanged)
      |
AttendanceProcessorService.ProcessAttendanceAsync() (existing, unmodified)
      |                    replays each punch through AttendanceService
      |                    (shift resolution, night-shift date rollover,
      |                    grace/late/early, overtime, Present/HalfDay/Absent)
Attendance / AttendanceLog (existing tables)
      |
Shift / Leave / Overtime / Comp Off / Payroll (existing, untouched)
```

No eSSL Web API, `webapiservice.asmx`, or device HTTP call is used anywhere in this
integration - the only network path to eSSL data is a SQL Server connection to
`etimetracklite1`, and only `SELECT` statements are ever issued against it.

### Why no new attendance engine

The codebase already had `BiometricAttendanceLog` (a raw-punch staging table with a
`DeviceTransactionId` idempotency key) and `AttendanceProcessorService` (replays staged
punches through the existing `AttendanceService.PunchInAsync/PunchOutAsync/BreakInAsync/
BreakOutAsync` - the same engine the manual/web punch clock uses). `EsslAttendanceSyncService`
only has one job: get eSSL's raw punches into that existing staging table correctly and
idempotently. It contains no shift, late/early, overtime, or night-shift logic of its own.

## Database changes (HRMS/ERP database only - `etimetracklite1` is never modified)

Run `add EsslAttendanceSyncState table and BiometricSyncLog columns.sql` (repo root).
It is idempotent (`IF NOT EXISTS` guarded).

- **New table** `EsslAttendanceSyncStates` - one row per tenant; the sync cursor/lock
  (`LastProcessedDeviceLogId`, `LastProcessedLogDate`, `LastSyncStartedAt/CompletedAt`,
  `LastSyncStatus`, `LastError`, `RecordsRead/Imported/Skipped/Failed`, `IsSyncRunning`).
- **Extended** `BiometricSyncLogs` (existing table) with `FromDate`, `ToDate`,
  `TriggeredBy` - this integration's sync *history* reuses that existing table via
  `SyncType = "EsslDbPull"` rather than a new history table.
- **No changes** to `BiometricAttendanceLogs` - its existing unique indexes already
  provide full idempotency for eSSL-sourced rows (see below).

If you have the .NET SDK available, `dotnet ef migrations add AddEsslAttendanceSync
--project Infrastructure --startup-project API` should produce an equivalent migration
from the entity/`OnModelCreating` changes already made in code; the SQL script above is
the ready-to-run equivalent for environments without the SDK on hand.

## Employee mapping

Reuses the existing `EmployeeBiometricMapping.BiometricEmployeeCode` table/field - no new
mapping table was created. `BiometricEmployeeCode` is matched against eTimeTrackLite1's
`Employees.EmployeeCodeInDevice` value, which is exactly what `DeviceLogs.UserId` contains
(both confirmed from the supplied schema files, not assumed).

Unmapped punches are **never discarded**: they are still inserted into
`BiometricAttendanceLog` with the raw device code as `EmployeeCode`, so creating the
mapping later automatically and retroactively lets them flow into attendance (they simply
stay `IsProcessed = false` until then). The **Unmapped Employees** admin tab lists these
by querying eSSL-sourced rows with no active mapping, cross-referencing eTimeTrackLite1's
own `Employees` table (read-only) for a display name.

## Idempotency

- `BiometricAttendanceLog.DeviceTransactionId = "ESSL-{eTimeTrackLite1 DeviceLogId}"`.
  `DeviceLogId` is an `IDENTITY` column - unique across the *entire* `DeviceLogs` table
  regardless of that table's own composite primary key, making it a safe, simple,
  globally-unique key on its own.
- `BiometricAttendanceLog.DeviceId = "ESSL-{eTimeTrackLite1 DeviceId}"` - keeps multiple
  physical devices distinguishable for auditing, and participates in the existing unique
  index `(TenantId, DeviceId, EmployeeCode, PunchTime)`.
- Existing unique indexes (unchanged) enforce both at the database level:
  `IX_BiometricAttendanceLogs_Device_TransactionId` and
  `IX_BiometricAttendanceLogs_Tenant_Device_Employee_PunchTime`.
- The sync service also pre-checks which `DeviceTransactionId`s already exist before
  attempting an insert (avoids the exception path in the common case), and catches
  `DbUpdateException` as a final backstop for a race between two overlapping runs.
- Running the same window twice imports 0 new records the second time - covered by
  `EsslAttendanceSyncServiceTests.Sync_IsIdempotent_SecondRunOverSameWindowImportsNothingNew`.

## Sync window / late-arriving records

Automatic (incremental) runs use `LastProcessedLogDate` **minus a configurable overlap**
(`EsslDatabase:OverlapMinutes`, default 30) as the window start - never a bare
`WHERE DeviceLogId > LastProcessedId`. Re-scanning the overlap on every run is safe because
duplicates are caught by the unique indexes above, not by the window being exact - this is
what safely picks up late-arriving or corrected rows. Manual/historical syncs use the
admin's explicit From/To dates instead.

## Locking

`EsslAttendanceSyncState.IsSyncRunning` is a per-tenant flag set for the duration of a run
and cleared in a `finally` block, so the background service and a manual "Sync Now"/
historical import can never process the same batch concurrently. A lock held longer than
30 minutes is treated as abandoned (e.g. the process crashed mid-run) and taken over rather
than blocking the feature forever.

## Punch direction

`DeviceLogs.Direction` (falling back to `AttDirection`) is normalized case-insensitively
against known tokens (`IN`/`I`/`CHECK-IN`/`CHECKIN`, `OUT`/`O`/`CHECK-OUT`/`CHECKOUT`). If
neither field yields a recognizable token, the sync falls back to alternating In/Out per
(employee, calendar day) within the run - see `EsslAttendanceSyncService.ResolvePunchType`
and its unit tests in `Tests/EsslIntegration.Tests/Unit/PunchDirectionResolutionTests.cs`.

## Configuration (`API/appsettings.json`)

```json
"EsslDatabase": {
  "ConnectionString": "",
  "DatabaseName": "etimetracklite1",
  "Enabled": false,
  "SyncIntervalMinutes": 5,
  "BatchSize": 500,
  "OverlapMinutes": 30
}
```

- Leave `Enabled: false` until the connection string and SQL login are ready - the
  `EsslDbContext`, background service, and admin screens all stay registered and harmless
  either way (every real query checks `Enabled` first).
- `ConnectionString` should use a SQL login granted **`db_datareader`/SELECT only** on
  `etimetracklite1` - never `db_owner`. This must be configured on the SQL Server side;
  the application code never issues `INSERT`/`UPDATE`/`DELETE`/`TRUNCATE`/`ALTER`/`DROP`
  against that database (there is no method anywhere in `IEsslAttendanceDataSource` that
  could).
- Never commit a real password into `appsettings.json` - use `appsettings.Development.json`
  (git-ignored) locally, or an environment variable / Azure Key Vault / User Secrets in
  production, following whatever secret-storage convention this deployment already uses
  for `ConnectionStrings:ERPConnection`.

## How to test the connection

Admin UI: **Attendance Management -> eSSL SQL Integration -> Test Connection** button.
Runs a cheap `SELECT TOP 1 ... ORDER BY DeviceLogId DESC` against `DeviceLogs` and reports
the latest `DeviceLogId`/`LogDate` found, or a safe error message (never a raw SQL
exception) if unreachable.

## How manual sync / historical import works

Same screen's **Sync Now** button (no dates - incremental) and **Historical Import**
section (From/To dates) both call `POST /api/EsslAttendance/sync`, which is the *exact
same* `EsslAttendanceSyncService.SyncAsync` method the background service calls
automatically - there is no separate/duplicated sync logic for manual vs. automatic.

## How automatic sync works

`API/BackgroundServices/EsslAttendanceSyncBackgroundService.cs` - same skeleton as the
existing `CompOffDetectionService`/`LeaveAccrualService` (singleton `BackgroundService`,
scoped dependencies resolved per cycle). Checks `EsslDatabase:Enabled` and
`EsslDatabase:SyncIntervalMinutes` from configuration on every cycle (no restart needed to
change the interval or toggle it off), and runs `SyncAsync` once per active tenant,
sequentially, isolating one tenant's failure from the others.

## Error handling

- One bad punch record is caught, logged (via the existing `IErrorLogService` - shows up
  in the **Error Log** screen tagged `Biometric Device Integration` / `eSSL SQL Sync`),
  and the batch continues - it never rolls back the records already saved in that batch.
- A logging failure itself can never crash a sync run (mirrors the existing
  `BiometricSyncService.FinishSyncLogAsync` pattern).
- Every run - success, partial failure, or total failure - writes one row to
  `BiometricSyncLogs` (`SyncType = "EsslDbPull"`), visible on the **Sync Logs** tab.

## Troubleshooting sync failures

1. **Attendance Management -> eSSL SQL Integration -> Settings tab** - check
   `LastSyncStatus`/`LastError` and whether `IsSyncRunning` is stuck `true` (a crash mid-run
   clears itself automatically after 30 minutes; the next attempt takes the lock over).
2. **Sync Logs tab** - find the specific failed run, its window (`FromDate`/`ToDate`), and
   `ErrorMessage`.
3. **Unmapped Employees tab** - if `RecordsImported` looks lower than expected, check
   whether the missing punches actually belong to employees with no
   `EmployeeBiometricMapping` yet (they are still imported, just flagged, not lost).
4. **Error Log screen** (System Configurator) - per-record failures (e.g. a malformed row)
   are logged there with the source `DeviceLogId`/`UserId`/`DeviceId`/`LogDate`.
5. **Test Connection** button - isolates "can the app reach `etimetracklite1` at all" from
   "is the sync logic failing on the data itself".

## IIS deployment considerations

- The eTimeTrackLite1 SQL Server must be network-reachable from the IIS server hosting the
  API (same LAN/VPN as wherever `etimetracklite1` actually lives - typically the same
  machine/network as the eSSL software installation, which may be a different site/branch
  than the HRMS server).
- `EsslDatabase:ConnectionString`'s SQL login should be a dedicated, `SELECT`-only login -
  do not reuse `sa` or the HRMS database's own login.
- The background service runs inside the same IIS application pool/process as the rest of
  the API - no separate Windows Service or scheduled task is required. An IIS app pool
  recycle simply restarts the timer loop (2-minute startup delay, then the configured
  interval); `EsslAttendanceSyncState` ensures no punches are lost across a recycle,
  restart, or crash (the incremental window's overlap plus the DB unique indexes make
  re-scanning safe either way).
- If `etimetracklite1` is temporarily unreachable, `TestConnectionAsync`/`GetDeviceLogsAsync`
  fail gracefully (caught, logged, reported in `BiometricSyncLogs`/`EsslAttendanceSyncState`)
  and the next scheduled cycle simply retries - no manual intervention needed for a
  transient outage.

## Files changed

See the chat summary for the full list of files added/modified, database changes,
configuration changes, and test coverage.
