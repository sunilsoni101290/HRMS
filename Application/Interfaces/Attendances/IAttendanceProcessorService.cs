using Application.DTOs.Attendances;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.Attendances
{
    public interface IAttendanceProcessorService
    {
        Task<bool> ProcessAttendanceAsync();

        // Same processing as ProcessAttendanceAsync above (that method is
        // now a thin wrapper over this one - see AttendanceProcessorService),
        // returning the full reconciliation breakdown instead of a bare
        // bool: TotalRawRecords/PendingRecords/MappedRecords/
        // UnmappedRecords/AlreadyProcessed/Duplicates/AttendanceCreated/
        // AttendanceUpdated/AttendanceLogsCreated/Skipped/Failed/
        // SuccessfullyProcessed, plus a bounded per-row failure/skip detail
        // list with the exact reason for each. batchSize defaults to the
        // implementation's own default when 0/omitted.
        Task<AttendanceProcessingResultDto> ProcessAttendanceWithResultAsync(int batchSize = 0);

        Task<AttendanceProcessDto>CalculateAttendanceAsync(string employeeId,DateTime date);
    }
}
