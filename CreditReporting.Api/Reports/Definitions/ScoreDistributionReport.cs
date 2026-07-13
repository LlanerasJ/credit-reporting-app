using CreditReporting.Api.Data;
using CreditReporting.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CreditReporting.Api.Reports.Definitions;

/// <summary>
/// How many customers fall into each credit-score band, using each customer's
/// most recent score.
/// </summary>
public class ScoreDistributionReport : IReportDefinition
{
    private static readonly (string Band, int Min, int Max)[] Bands =
    {
        ("Poor", 300, 579),
        ("Fair", 580, 669),
        ("Good", 670, 739),
        ("Very Good", 740, 799),
        ("Excellent", 800, 850)
    };

    private readonly AppDbContext _db;
    public ScoreDistributionReport(AppDbContext db) => _db = db;

    public string Key => "score-distribution";
    public string DisplayName => "Score Distribution";
    public string Description =>
        "Customer counts per credit-score band (Poor through Excellent), based on each customer's latest score.";

    public IReadOnlyList<ReportParameterDto> Parameters { get; } = new List<ReportParameterDto>
    {
        new()
        {
            Name = "state", Label = "Customer state (2 letters)",
            Type = "string", Required = false
        }
    };

    public async Task<ReportResultDto> ExecuteAsync(ReportArgs args, CancellationToken ct = default)
    {
        string? state = args.GetString("state")?.ToUpperInvariant();

        var query = _db.Customers.AsNoTracking();
        if (!string.IsNullOrEmpty(state))
            query = query.Where(c => c.State == state);

        // Latest score per customer; customers with no score history are skipped.
        var latestScores = await query
            .Where(c => c.Scores.Any())
            .Select(c => c.Scores.OrderByDescending(s => s.ScoreDate).First().Score)
            .ToListAsync(ct);

        var rows = Bands.Select(band =>
        {
            var inBand = latestScores.Where(s => s >= band.Min && s <= band.Max).ToList();
            return new List<string>
            {
                band.Band,
                $"{band.Min}-{band.Max}",
                ReportFormat.Number(inBand.Count),
                inBand.Count == 0 ? "" : ReportFormat.Number((int)Math.Round(inBand.Average()))
            };
        }).ToList();

        return new ReportResultDto
        {
            ReportType = Key,
            DisplayName = DisplayName,
            GeneratedAtUtc = DateTime.UtcNow,
            Columns = new List<ReportColumnDto>
            {
                new() { Name = "Band", Type = "string" },
                new() { Name = "Score Range", Type = "string" },
                new() { Name = "Customers", Type = "number" },
                new() { Name = "Avg Score", Type = "number" }
            },
            Rows = rows,
            RowCount = rows.Count
        };
    }
}
