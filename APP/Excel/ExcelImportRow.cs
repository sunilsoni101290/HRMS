namespace APP.Excel
{
    // One parsed row out of ReadRows(). Item is always populated (a new T()
    // with whatever columns succeeded applied to it) - ParseErrors lists any
    // column Setters that threw, so the controller can decide row-by-row
    // whether to still attempt the API call or report it as a straight
    // parse failure without ever hitting the API.
    public class ExcelImportRow<T>
    {
        // The actual Excel row number (1-based, matching what the user sees
        // in the spreadsheet) - used so failure messages can say "Row 7:
        // ..." instead of an opaque zero-based index.
        public int RowNumber { get; set; }

        public T Item { get; set; } = default!;

        public List<string> ParseErrors { get; set; } = new();

        public bool HasErrors => ParseErrors.Count > 0;
    }
}
