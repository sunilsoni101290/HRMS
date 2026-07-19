namespace APP.Excel
{
    // One named lookup column inside a template's "Reference Data" sheet -
    // e.g. Header "Department" with the tenant's live department names, or
    // Header "Gender" with a fixed enum's names. A single reference sheet
    // can carry several of these side by side in adjacent columns (e.g. the
    // Employee template needs Company, Department, Role, Gender, Marital
    // Status, Employment Type and Designation all in one "Reference Data"
    // sheet today).
    public class ExcelReferenceColumn
    {
        public string Header { get; set; } = string.Empty;

        public List<string> Values { get; set; } = new();

        public ExcelReferenceColumn()
        {
        }

        public ExcelReferenceColumn(string header, IEnumerable<string> values)
        {
            Header = header;
            Values = values?.ToList() ?? new List<string>();
        }
    }

    // Describes one extra worksheet to append to a template workbook,
    // listing the exact values a client can type into the main sheet's
    // lookup columns (Company/Department/Designation names, fixed enum
    // choices, etc). The engine only renders whatever values the calling
    // controller already fetched (e.g. via IApiService) and passed in here -
    // it never calls out to any API itself, keeping it decoupled from any
    // specific module.
    public class ExcelTemplateReferenceSheet
    {
        public string SheetName { get; set; } = "Reference Data";

        public List<ExcelReferenceColumn> Columns { get; set; } = new();

        public ExcelTemplateReferenceSheet()
        {
        }

        public ExcelTemplateReferenceSheet(string sheetName, List<ExcelReferenceColumn> columns)
        {
            SheetName = sheetName;
            Columns = columns ?? new List<ExcelReferenceColumn>();
        }
    }
}
