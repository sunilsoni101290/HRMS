using Microsoft.AspNetCore.Http;

namespace APP.Excel
{
    // Generic Excel import/export engine backing every module's Import /
    // Export / DownloadImportTemplate actions. A module supplies its DTO's
    // column mapping (List&lt;ExcelColumn&lt;T&gt;&gt;) instead of hand-rolling
    // ClosedXML workbook/worksheet code - this is what
    // EmployeeController/SalaryComponentController did inline before being
    // factored out here.
    //
    // The engine never talks to any module's API (no IApiService dependency)
    // - reference-sheet data and per-row create/update calls stay the
    // calling controller's responsibility, since those legitimately vary
    // per module.
    public interface IExcelEngine
    {
        // Builds a workbook of one sheet from existing data - styled header
        // row (bold, white text, dark fill) plus one row per item via each
        // column's Getter. Returns raw .xlsx bytes ready to hand to
        // File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName).
        byte[] Export<T>(
            IEnumerable<T> data,
            List<ExcelColumn<T>> columns,
            string sheetName = "Data");

        // Builds a ready-to-fill template workbook: the same styled header
        // row, one sample row (from sampleRow's Getter values if supplied,
        // otherwise each column's own SampleValue), and optionally one or
        // more extra "reference data" worksheets listing the exact values a
        // client should type into lookup columns (values the controller
        // already fetched - the engine only renders them).
        byte[] BuildTemplate<T>(
            List<ExcelColumn<T>> columns,
            string sheetName = "Data",
            List<ExcelTemplateReferenceSheet>? referenceSheets = null,
            T? sampleRow = default);

        // Parses an uploaded workbook into typed rows, one ExcelImportRow&lt;T&gt;
        // per non-blank data row. Validates the file itself first (must be
        // present, non-empty, .xlsx/.xls) and throws
        // ExcelFileValidationException with a client-safe message if not.
        // Each column's Setter is invoked in its own try/catch, so one bad
        // cell is recorded in that row's ParseErrors instead of aborting the
        // whole import. Never calls any API - the caller still owns
        // looping over the results, calling its create/update endpoint per
        // row, and aggregating success/failure into an ExcelImportResult.
        List<ExcelImportRow<T>> ReadRows<T>(
            IFormFile file,
            List<ExcelColumn<T>> columns,
            int headerRowNumber = 1)
            where T : new();
    }
}
