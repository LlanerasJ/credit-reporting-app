namespace CreditReporting.Api.Data.Entities;

/// <summary>
/// One row per credit report access: who pulled it, when, for which customer,
/// and the stated purpose.
/// </summary>
public class AuditLogEntry
{
    public long Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string Username { get; set; } = "";
    public int CustomerId { get; set; }
    public string Action { get; set; } = "";
    public string Purpose { get; set; } = "";
    public string SourceIp { get; set; } = "";
}
