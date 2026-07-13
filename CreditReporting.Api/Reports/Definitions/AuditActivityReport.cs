using CreditReporting.Api.Data;
using CreditReporting.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CreditReporting.Api.Reports.Definitions;

/// <summary>
/// Who accessed what: audit-log entries filtered by user, date range, and
/// action. Newest first, capped so a wide-open query stays displayable.
/// </summary>
public class AuditActivityReport : IReportDefinition
{
    private const int MaxRows = 500;

    private readonly AppDbContext _db;
    public AuditActivityReport(AppDbContext db) => _db = db;

    public string Key => "audit-activity";
    public string DisplayName => "Audit Activity";
    public string Description =>
        $"Audit-log entries (credit report views, report runs) filtered by user, date range, and action. Newest first, capped at {MaxRows} rows.";

    public IReadOnlyList<ReportParameterDto> Parameters { get; } = new List<ReportParameterDto>
    {
        new() { Name = "username", Label = "Username", Type = "string", Required = false },
        new() { Name = "from", Label = "From date", Type = "date", Required = false },
        new() { Name = "to", Label = "To date (inclusive)", Type = "date", Required = false },
        new()
        {
            Name = "action", Label = "Action",
            Type = "choice", Required = false,
            Options = new List<string> { "CreditReportViewed", "ReportRun" }
        }
    };

    public async Task<ReportResultDto> ExecuteAsync(ReportArgs args, CancellationToken ct = default)
    {
        string? username = args.GetString("username");
        DateTime? from = args.GetDate("from");
        DateTime? to = args.GetDate("to");
        string? action = args.GetString("action");

        var query = _db.AuditLog.AsNoTracking();
        if (!string.IsNullOrEmpty(username))
            query = query.Where(a => a.Username == username);
        if (from is not null)
            query = query.Where(a => a.TimestampUtc >= from.Value.Date);
        if (to is not null)
            query = query.Where(a => a.TimestampUtc < to.Value.Date.AddDays(1));
        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);

        var entries = await query
            .OrderByDescending(a => a.TimestampUtc)
            .Take(MaxRows)
            .ToListAsync(ct);

        return new ReportResultDto
        {
            ReportType = Key,
            DisplayName = DisplayName,
            GeneratedAtUtc = DateTime.UtcNow,
            Columns = new List<ReportColumnDto>
            {
                new() { Name = "Timestamp (UTC)", Type = "date" },
                new() { Name = "User", Type = "string" },
                new() { Name = "Action", Type = "string" },
                new() { Name = "Customer Id", Type = "number" },
                new() { Name = "Purpose", Type = "string" },
                new() { Name = "Source IP", Type = "string" }
            },
            Rows = entries.Select(e => new List<string>
            {
                ReportFormat.Timestamp(e.TimestampUtc),
                e.Username,
                e.Action,
                e.CustomerId == 0 ? "" : ReportFormat.Number(e.CustomerId),
                e.Purpose,
                e.SourceIp
            }).ToList(),
            RowCount = entries.Count
        };
    }
}
