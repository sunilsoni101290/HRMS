namespace APP.Models.DTOs
{
    // View model for the "Bulk Mapping" grid screen - one row per active
    // employee, left-joined with that employee's existing
    // EmployeeBiometricMapping (if any). Built entirely at the APP layer by
    // combining two already-existing API responses (Employee/employee-list +
    // EmployeeBiometricMapping) - no new API/service method was needed.
    public class BulkMappingRowDto
    {
        public string EmployeeId { get; set; } = "";
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string? Department { get; set; }
        public string? Designation { get; set; }

        // Null when the employee has no mapping yet.
        public string? MappingId { get; set; }
        public string? BiometricEmployeeCode { get; set; }
        public string? CardNumber { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class BulkMappingViewModel
    {
        public List<BulkMappingRowDto> Rows { get; set; } = new();
        public List<EsslUnmappedEmployeeDto> Unmapped { get; set; } = new();
    }

    // One row as posted back from the grid's "Save All Changes" action -
    // only rows the user actually touched are sent (tracked client-side).
    public class BulkMappingSaveRowDto
    {
        public string EmployeeId { get; set; } = "";
        public string? MappingId { get; set; }
        public string? BiometricEmployeeCode { get; set; }
        public string? CardNumber { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // One outcome line shown in the Save All / Auto-map result panel -
    // deliberately separate from APP.Excel.ExcelImportResult (that type is
    // keyed by Excel row number; these actions aren't reading a file).
    public class BulkMappingRowResult
    {
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    public class BulkMappingSaveResult
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkMappingRowResult> Results { get; set; } = new();
    }

    // Plain POCO used only as the target of IExcelEngine.ReadRows for the
    // bulk-mapping Import flow - EmployeeCode is resolved back to an
    // EmployeeId in the controller (the Excel sheet never carries internal
    // IDs), so this intentionally does NOT reuse EmployeeBiometricMappingDto.
    public class BulkMappingImportRowDto
    {
        public string EmployeeCode { get; set; } = "";
        public string BiometricEmployeeCode { get; set; } = "";
        public string? CardNumber { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
