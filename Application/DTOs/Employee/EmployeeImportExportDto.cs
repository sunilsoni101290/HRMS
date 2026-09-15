using System;
using System.Collections.Generic;

namespace Application.DTOs.Employee
{
    // ==========================================================================
    // Employee bulk Import / Export - request & result DTOs.
    //
    // Design: Excel file I/O (reading the uploaded .xlsx, building the
    // template/export/error workbooks) stays in the APP layer with
    // ClosedXML, exactly like the pre-existing Import/DownloadImportTemplate
    // actions already did - only the RAW, unresolved cell text for each row
    // crosses to the API as EmployeeImportRowInputDto. All actual validation
    // (required fields, formats, master-data lookups, duplicate checks)
    // happens here, server-side, against the live database - the single
    // source of truth shared by both the Preview (validate-only) and Commit
    // (validate-then-insert) calls, so the two can never disagree.
    // ==========================================================================

    // One row exactly as typed in the sheet - every value is the raw
    // trimmed cell text (or null if blank), completely unresolved. Dates are
    // passed as "yyyy-MM-dd" strings (APP normalizes both true Excel dates
    // and typed date text to this format before sending) so parsing only
    // ever happens in one place, here.
    public class EmployeeImportRowInputDto
    {
        public int RowNumber { get; set; }

        public string? EmployeeCode { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public string? Gender { get; set; }
        public string? MaritalStatus { get; set; }
        public string? DateOfBirth { get; set; }

        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? EmergencyContact { get; set; }

        public string? CompanyName { get; set; }
        public string? BranchName { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public string? RoleName { get; set; }
        public string? ReportingManagerCode { get; set; }
        public string? ShiftName { get; set; }

        public string? EmploymentType { get; set; }
        public string? JoiningDate { get; set; }

        public string? Address { get; set; }
        public string? Pincode { get; set; }

        public string? PANNumber { get; set; }
        public string? AadharNumber { get; set; }
    }

    // Row/Field/Value/Error - exactly the shape the Excel error report and
    // the on-screen preview table both need.
    public class ImportFieldError
    {
        public string Field { get; set; } = "";
        public string Value { get; set; } = "";
        public string Error { get; set; } = "";
    }

    public class EmployeeImportRowValidationDto
    {
        public int RowNumber { get; set; }

        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? CompanyName { get; set; }
        public string? RoleName { get; set; }
        public string? ShiftName { get; set; }
        public string? EmploymentType { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }

        public bool IsValid { get; set; }
        public List<ImportFieldError> Errors { get; set; } = new();

        // Set only by CommitImportAsync (never by ValidateImportAsync) -
        // "Imported successfully" on success, or the row's own failure
        // reason if the whole commit had to be rejected.
        public string? Message { get; set; }
    }

    // Preview (validate-only, nothing written to the database).
    public class EmployeeImportPreviewResultDto
    {
        public int TotalRows { get; set; }
        public int ValidCount { get; set; }
        public int InvalidCount { get; set; }
        public List<EmployeeImportRowValidationDto> Rows { get; set; } = new();
    }

    // Commit result. Success is only ever true when EVERY row was valid AND
    // every one of them was actually inserted inside the single database
    // transaction that backs CommitImportAsync - there is no partial/mixed
    // outcome. When Success is false, Rows carries the same row-wise errors
    // as the Preview call (nothing was written), so the caller can show
    // exactly why without a second round trip.
    public class EmployeeImportCommitResultDto
    {
        public bool Success { get; set; }
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public string Message { get; set; } = "";
        public List<EmployeeImportRowValidationDto> Rows { get; set; } = new();
    }
}
