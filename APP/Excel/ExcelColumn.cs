namespace APP.Excel
{
    // Describes one Excel column for a given model/DTO type T. A single
    // column definition owns everything needed to both write it (Export /
    // BuildTemplate, via Getter) and read it back (Import, via Setter) - so
    // a module only has to declare its columns once instead of hand-rolling
    // ClosedXML header/cell code and matching "Cell(int col)" parsing helpers
    // per controller (the pattern duplicated today in EmployeeController and
    // SalaryComponentController).
    public class ExcelColumn<T>
    {
        // Column header text written into row 1 of the sheet (Export,
        // BuildTemplate) - e.g. "Component Name*". Import still matches
        // columns positionally (by the order columns are declared), not by
        // header text, so the header can freely include hints like "*" or
        // "(Yes/No)" without affecting parsing - but ExcelEngine.ReadRows
        // now validates the uploaded file's header row against this list
        // (ignoring that same cosmetic decoration) before reading any data,
        // so a structurally wrong file (reordered/missing columns, or an
        // Export re-uploaded as an Import) fails fast with a clear message
        // instead of silently reading values into the wrong fields.
        public string Header { get; set; } = string.Empty;

        // Pulls the display value out of an instance of T for Export/
        // template-sample rendering. May return null - the engine renders
        // that as a blank cell.
        public Func<T, object?> Getter { get; set; } = _ => null;

        // Given the raw trimmed cell text for this column on an import row,
        // apply it to the (blank) T instance being built. Each column owns
        // its own parsing/validation logic (e.g. a Yes/No column parses
        // "Yes"/"No" into a bool, a date column parses into DateTime?) -
        // throw with a clear message on invalid input; the engine catches
        // per-column so one bad cell never kills the whole row.
        public Action<T, string?> Setter { get; set; } = (_, _) => { };

        // Purely informational today - used for template styling/validation
        // hints (e.g. marking the header with "*"). Not enforced by the
        // engine itself; a column's own Setter is what actually decides
        // whether a blank value is an error.
        public bool IsRequired { get; set; }

        // Optional fallback value shown in a template's sample row when
        // BuildTemplate isn't given a full sample T instance to pull
        // Getter values from.
        public object? SampleValue { get; set; }

        public ExcelColumn()
        {
        }

        public ExcelColumn(
            string header,
            Func<T, object?> getter,
            Action<T, string?> setter,
            bool isRequired = false,
            object? sampleValue = null)
        {
            Header = header;
            Getter = getter;
            Setter = setter;
            IsRequired = isRequired;
            SampleValue = sampleValue;
        }
    }
}
