using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;

namespace APP.Excel
{
    // Concrete ClosedXML-backed implementation of IExcelEngine. Registered
    // as a scoped service in Program.cs so controllers can constructor-
    // inject it exactly like IApiService.
    public class ExcelEngine : IExcelEngine
    {
        // Matches the header styling already used by the hand-rolled
        // Employee/SalaryComponent templates and exports - dark navy fill,
        // bold white text.
        private static readonly XLColor HeaderFillColor = XLColor.FromHtml("#1B2A4A");

        public byte[] Export<T>(
            IEnumerable<T> data,
            List<ExcelColumn<T>> columns,
            string sheetName = "Data")
        {
            if (columns == null || columns.Count == 0)
                throw new ArgumentException("At least one column must be supplied.", nameof(columns));

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add(NormalizeSheetName(sheetName));

            WriteHeaderRow(sheet, columns);

            var r = 2;

            foreach (var item in data ?? Enumerable.Empty<T>())
            {
                for (var c = 0; c < columns.Count; c++)
                {
                    SetCellValue(sheet.Cell(r, c + 1), columns[c].Getter(item));
                }

                r++;
            }

            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents();

            return SaveToBytes(workbook);
        }

        public byte[] BuildTemplate<T>(
            List<ExcelColumn<T>> columns,
            string sheetName = "Data",
            List<ExcelTemplateReferenceSheet>? referenceSheets = null,
            T? sampleRow = default)
        {
            if (columns == null || columns.Count == 0)
                throw new ArgumentException("At least one column must be supplied.", nameof(columns));

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add(NormalizeSheetName(sheetName));

            WriteHeaderRow(sheet, columns);

            // Boxing lets us tell "no sample instance supplied" apart from
            // "a sample instance was supplied" regardless of whether T is a
            // reference or value type, without constraining T on the
            // interface.
            object? boxedSample = sampleRow;

            for (var c = 0; c < columns.Count; c++)
            {
                var column = columns[c];

                var value = boxedSample != null
                    ? column.Getter((T)boxedSample)
                    : column.SampleValue;

                SetCellValue(sheet.Cell(2, c + 1), value);
            }

            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents();

            if (referenceSheets != null)
            {
                foreach (var referenceSheet in referenceSheets)
                {
                    AddReferenceSheet(workbook, referenceSheet);
                }
            }

            return SaveToBytes(workbook);
        }

        public List<ExcelImportRow<T>> ReadRows<T>(
            IFormFile file,
            List<ExcelColumn<T>> columns,
            int headerRowNumber = 1)
            where T : new()
        {
            if (columns == null || columns.Count == 0)
                throw new ArgumentException("At least one column must be supplied.", nameof(columns));

            if (file == null || file.Length == 0)
                throw new ExcelFileValidationException("Please choose an Excel file to import.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (extension != ".xlsx" && extension != ".xls")
                throw new ExcelFileValidationException("Only .xlsx or .xls files are supported.");

            var rows = new List<ExcelImportRow<T>>();

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);

            // Columns are matched by POSITION, not by header text (see
            // ExcelColumn.Header) - so an uploaded file whose columns are in
            // a different order, missing, or extra (e.g. someone re-uploads
            // an Export instead of a Download-Template file, or reorders
            // columns in Excel) would otherwise be silently misread: a City
            // name could land in the "Country Code" column's Setter with no
            // structural error, only a confusing per-cell validation message
            // if any at all. Before reading data, verify the header row
            // actually matches the expected columns (order and count),
            // ignoring cosmetic decorations like "*"/"(Yes/No)" so a
            // lightly-edited header still passes. A genuine mismatch fails
            // fast with a clear, actionable message instead of importing
            // garbage into the wrong fields.
            ValidateHeaderRow(worksheet, columns, headerRowNumber);

            var dataRows = worksheet.RowsUsed().Skip(headerRowNumber).ToList();

            foreach (var row in dataRows)
            {
                var cellTexts = new string?[columns.Count];
                var rowHasAnyValue = false;

                for (var c = 0; c < columns.Count; c++)
                {
                    var text = row.Cell(c + 1).GetString().Trim();
                    cellTexts[c] = text;

                    if (!string.IsNullOrWhiteSpace(text))
                        rowHasAnyValue = true;
                }

                // Skip fully blank trailing rows - a row where every
                // mapped column is empty isn't real data.
                if (!rowHasAnyValue)
                    continue;

                var importRow = new ExcelImportRow<T>
                {
                    RowNumber = row.RowNumber(),
                    Item = new T()
                };

                for (var c = 0; c < columns.Count; c++)
                {
                    var column = columns[c];

                    try
                    {
                        column.Setter(importRow.Item, cellTexts[c]);
                    }
                    catch (Exception ex)
                    {
                        // One bad cell must not kill the whole row - it's
                        // recorded against this row's ParseErrors and the
                        // remaining columns still get a chance to parse.
                        importRow.ParseErrors.Add($"{column.Header}: {ex.Message}");
                    }
                }

                rows.Add(importRow);
            }

            return rows;
        }

        // Compares the uploaded file's header row against the expected
        // column list, ignoring purely cosmetic decoration ("*" required
        // markers, "(Yes/No)" hints, surrounding whitespace, case) so a
        // template that's been lightly re-labelled by a user still passes.
        // A genuine structural mismatch - wrong column count, reordered
        // columns, or a file that's actually an Export/unrelated sheet -
        // fails fast with a message naming exactly which position is wrong,
        // instead of silently feeding a City name into a Country Code
        // Setter (or similar) with no positional check at all.
        private static void ValidateHeaderRow<T>(IXLWorksheet worksheet, List<ExcelColumn<T>> columns, int headerRowNumber)
        {
            var headerRow = worksheet.Row(headerRowNumber);
            var actualCount = headerRow.CellsUsed().Count();

            if (actualCount == 0)
            {
                throw new ExcelFileValidationException(
                    "The uploaded file doesn't have a header row. Please use the Download Template file and fill it in without changing the column layout.");
            }

            for (var c = 0; c < columns.Count; c++)
            {
                var expectedHeader = columns[c].Header;
                var actualHeader = headerRow.Cell(c + 1).GetString();

                if (!string.Equals(NormalizeHeaderForComparison(expectedHeader), NormalizeHeaderForComparison(actualHeader), StringComparison.Ordinal))
                {
                    throw new ExcelFileValidationException(
                        $"This file doesn't match the expected import template. Column {c + 1} was expected to be " +
                        $"\"{expectedHeader}\" but found \"{(string.IsNullOrWhiteSpace(actualHeader) ? "(blank)" : actualHeader)}\". " +
                        "Please use the Download Template file - don't reorder, remove, or add columns, and don't re-upload an exported file.");
                }
            }
        }

        // Strips everything except letters/digits and lower-cases, so
        // "Country Code*", "country code", and " Country Code " all
        // normalize to the same "countrycode" key.
        private static string NormalizeHeaderForComparison(string? header) =>
            new string((header ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        private static void WriteHeaderRow<T>(IXLWorksheet sheet, List<ExcelColumn<T>> columns)
        {
            for (var i = 0; i < columns.Count; i++)
            {
                var cell = sheet.Cell(1, i + 1);
                cell.Value = columns[i].Header;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = HeaderFillColor;
            }
        }

        private static void AddReferenceSheet(XLWorkbook workbook, ExcelTemplateReferenceSheet referenceSheet)
        {
            var sheet = workbook.Worksheets.Add(NormalizeSheetName(referenceSheet.SheetName, "Reference Data"));

            for (var c = 0; c < referenceSheet.Columns.Count; c++)
            {
                var column = referenceSheet.Columns[c];

                var header = sheet.Cell(1, c + 1);
                header.Value = column.Header;
                header.Style.Font.Bold = true;

                for (var r = 0; r < column.Values.Count; r++)
                {
                    sheet.Cell(r + 2, c + 1).Value = column.Values[r];
                }
            }

            sheet.Columns().AdjustToContents();
        }

        // ClosedXML's IXLCell.Value setter only accepts a handful of
        // concrete types - this keeps every column's Getter free to return
        // whatever CLR type is natural (bool, DateTime, numeric, string,
        // null) without every module having to pre-format its own strings.
        private static void SetCellValue(IXLCell cell, object? value)
        {
            switch (value)
            {
                case null:
                    cell.Value = string.Empty;
                    break;

                case bool b:
                    cell.Value = b ? "Yes" : "No";
                    break;

                case DateTime dt:
                    cell.Value = dt;
                    cell.Style.DateFormat.Format = "yyyy-mm-dd";
                    break;

                case int i:
                    cell.Value = i;
                    break;

                case long l:
                    cell.Value = l;
                    break;

                case double d:
                    cell.Value = d;
                    break;

                case decimal m:
                    cell.Value = (double)m;
                    break;

                default:
                    cell.Value = value.ToString();
                    break;
            }
        }

        private static string NormalizeSheetName(string? sheetName, string fallback = "Data")
        {
            return string.IsNullOrWhiteSpace(sheetName) ? fallback : sheetName;
        }

        private static byte[] SaveToBytes(XLWorkbook workbook)
        {
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
