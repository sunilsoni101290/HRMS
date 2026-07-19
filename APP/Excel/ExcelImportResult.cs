namespace APP.Excel
{
    // Outcome of one imported row, after the controller has attempted its
    // per-row API call (create/update). Identifier is a short free-form
    // label the controller supplies to help a client find the row again
    // (e.g. "HRA - House Rent Allowance", or an employee code) - it
    // deliberately isn't split into separate typed fields per module, since
    // what "identifies" a row varies module to module and this is display
    // text only.
    public class ExcelImportRowResult
    {
        public int RowNumber { get; set; }

        public string? Identifier { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;
    }

    // Generic, module-agnostic import summary - replaces the
    // EmployeeImportResultDto / SalaryComponentImportRowResult pair (and
    // whatever the next 11 modules would otherwise each redeclare for
    // themselves). Shared by the Import GET view (empty result), the Import
    // POST result view, and the shared result-table partial.
    public class ExcelImportResult
    {
        public int TotalRows { get; set; }

        public int SuccessCount { get; set; }

        public int FailureCount { get; set; }

        public List<ExcelImportRowResult> Rows { get; set; } = new();

        // Records one row's outcome and keeps the running totals in sync -
        // the controller calls this once per parsed row instead of hand-
        // updating three counters itself.
        public void AddRow(int rowNumber, string? identifier, bool success, string message)
        {
            Rows.Add(new ExcelImportRowResult
            {
                RowNumber = rowNumber,
                Identifier = identifier,
                Success = success,
                Message = message
            });

            TotalRows = Rows.Count;
            SuccessCount = Rows.Count(x => x.Success);
            FailureCount = TotalRows - SuccessCount;
        }
    }
}
