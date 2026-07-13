namespace CreditReporting.Shared.Dtos;

/// <summary>One report type from the server-side catalog.</summary>
public class ReportDefinitionDto
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public List<ReportParameterDto> Parameters { get; set; } = new();
}

/// <summary>
/// Describes one parameter of a catalog report so the client can render an
/// input for it without knowing the report type in advance.
/// </summary>
public class ReportParameterDto
{
    public string Name { get; set; } = "";
    /// <summary>Human-readable label, e.g. "Minimum past due".</summary>
    public string Label { get; set; } = "";
    /// <summary>"string" | "int" | "decimal" | "date" | "choice".</summary>
    public string Type { get; set; } = "string";
    public bool Required { get; set; }
    /// <summary>Used when the parameter is omitted; null = no default.</summary>
    public string? DefaultValue { get; set; }
    /// <summary>Allowed values when Type is "choice".</summary>
    public List<string>? Options { get; set; }
}

/// <summary>Request to execute a catalog report with the given parameter values.</summary>
public class RunReportRequest
{
    /// <summary>Catalog key, e.g. "delinquent-accounts".</summary>
    public string ReportType { get; set; } = "";
    /// <summary>Parameter values keyed by parameter name; all values as entered.</summary>
    public Dictionary<string, string?> Parameters { get; set; } = new();
}

public class ReportColumnDto
{
    public string Name { get; set; } = "";
    /// <summary>"string" | "number" | "date" — a display hint (alignment, sorting).</summary>
    public string Type { get; set; } = "string";
}

/// <summary>
/// A report run result. Cell values are pre-formatted strings (invariant
/// culture, dates as yyyy-MM-dd, empty string for null) so the client can bind
/// and export them without type juggling.
/// </summary>
public class ReportResultDto
{
    public string ReportType { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public DateTime GeneratedAtUtc { get; set; }
    public List<ReportColumnDto> Columns { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public int RowCount { get; set; }
}
