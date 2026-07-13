namespace CreditReporting.Api.Data.Entities;

/// <summary>
/// A saved configuration of a catalog report: which report type to run and the
/// parameter values to run it with. Private to its owner unless IsShared.
/// </summary>
public class SavedReport
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";

    /// <summary>Catalog key of the report type, e.g. "delinquent-accounts".</summary>
    public string ReportType { get; set; } = "";
    /// <summary>Parameter values as a JSON object of name → string value.</summary>
    public string ParametersJson { get; set; } = "{}";

    public string OwnerUsername { get; set; } = "";
    /// <summary>Shared reports are visible to every user; only admins may set this.</summary>
    public bool IsShared { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime ModifiedUtc { get; set; }
}
