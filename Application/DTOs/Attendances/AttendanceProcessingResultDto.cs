namespace Application.DTOs.Attendances
{
    /// <summary>
    /// Full reconciliation result of one AttendanceProcessorService.
    /// ProcessAttendanceWithResultAsync run - every counter here is a
    /// mutually-exclusive bucket over TotalRawRecords found in this run,
    /// so:
    ///
    ///   TotalRawRecords == AlreadyProcessed + SuccessfullyProcessed +
    ///                       UnmappedRecords + Duplicates + Skipped + Failed
    ///
    /// (AlreadyProcessed = rows healed from a prior partially-completed
    /// run, not replayed again; SuccessfullyProcessed = rows newly applied
    /// to Attendance/AttendanceLogs this run and marked IsProcessed=true).
    /// See AttendanceProcessorService.ProcessAttendanceWithResultAsync's
    /// remarks for exactly what puts a row in each bucket.
    /// </summary>
    public class AttendanceProcessingResultDto
    {
        public bool Success { get; set; }

        /// <summary>Failure message when Success is false because the run itself threw (not a per-row failure - those are counted in Failed below and never stop the run).</summary>
        public string? ErrorMessage { get; set; }

        public int TotalRawRecords { get; set; }

        /// <summary>Rows already sitting IsProcessed=false at the START of this run, before any work - what "Pending" looked like going in.</summary>
        public int PendingRecords { get; set; }

        /// <summary>Rows whose raw EmployeeCode resolved to an active EmployeeBiometricMapping (regardless of what happened to them afterward).</summary>
        public int MappedRecords { get; set; }

        /// <summary>Rows with no active EmployeeBiometricMapping for their raw EmployeeCode - left IsProcessed=false, retried on every future run until a mapping is created.</summary>
        public int UnmappedRecords { get; set; }

        /// <summary>Rows healed from a prior run that had already been applied to AttendanceLogs but failed to save IsProcessed=true (idempotency fast-path/fallback) - not replayed, just tagged and marked processed.</summary>
        public int AlreadyProcessed { get; set; }

        /// <summary>New Attendance rows created this run (first punch of a new Employee+Date).</summary>
        public int AttendanceCreated { get; set; }

        /// <summary>Existing Attendance rows updated this run (second+ punch of an Employee+Date already open).</summary>
        public int AttendanceUpdated { get; set; }

        /// <summary>New AttendanceLog rows created this run.</summary>
        public int AttendanceLogsCreated { get; set; }

        /// <summary>Rows recognized as an exact repeat of an already-applied punch (idempotency match) - counted separately from AlreadyProcessed for clarity in the summary line, though both come from the same healing path.</summary>
        public int Duplicates { get; set; }

        /// <summary>Rows AttendanceService rejected as business-rule out-of-sequence (e.g. IN after IN with no OUT in between, OUT with no open IN, Break* out of order) - left IsProcessed=false for retry/investigation. See PerRowFailures for the exact reason per row.</summary>
        public int Skipped { get; set; }

        /// <summary>Rows that threw an unexpected exception while processing (DB error, missing post-apply AttendanceLog, etc.) - left IsProcessed=false, logged to ErrorLog.</summary>
        public int Failed { get; set; }

        /// <summary>Rows newly applied to Attendance/AttendanceLogs and marked IsProcessed=true this run.</summary>
        public int SuccessfullyProcessed { get; set; }

        /// <summary>Per-row detail for every row that did NOT become a fresh AttendanceLog this run (Unmapped/Skipped/Failed) - EmployeeCode, RawId, PunchTime, PunchType, AttendanceDate (when resolvable) and the exact reason/exception text. Bounded (see AttendanceProcessorService.MaxPerRowFailureDetails) so a huge backlog can't blow up memory/log size - TotalRawRecords/UnmappedRecords/Skipped/Failed above are always the complete counts even when this list is truncated.</summary>
        public List<AttendanceProcessingRowDetailDto> PerRowFailures { get; set; } = new();

        public bool PerRowFailuresTruncated { get; set; }
    }

    public class AttendanceProcessingRowDetailDto
    {
        public string RawId { get; set; } = "";
        public string EmployeeCode { get; set; } = "";
        public DateTime PunchTime { get; set; }
        public string PunchType { get; set; } = "";
        public DateTime? AttendanceDate { get; set; }
        public string Reason { get; set; } = "";
    }
}
