namespace APP.Excel
{
    // Thrown by IExcelEngine.ReadRows for file-level problems (missing/
    // empty upload, wrong extension) - kept distinct from a generic parse
    // failure so a controller can show its message directly as a plain
    // validation error (TempData["GlobalError"] = ex.Message), exactly like
    // the manual checks the Employee/SalaryComponent controllers used to do
    // inline before calling into ClosedXML.
    public class ExcelFileValidationException : Exception
    {
        public ExcelFileValidationException(string message) : base(message)
        {
        }
    }
}
