using CreditReporting.Api.Data;
using CreditReporting.Api.Data.Entities;
using CreditReporting.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CreditReporting.Api.Reports.Definitions;

/// <summary>Account counts and balance totals grouped by product type.</summary>
public class PortfolioSummaryByTypeReport : IReportDefinition
{
    private readonly AppDbContext _db;
    public PortfolioSummaryByTypeReport(AppDbContext db) => _db = db;

    public string Key => "portfolio-summary";
    public string DisplayName => "Portfolio Summary by Type";
    public string Description =>
        "Account counts, balances, credit limits, and past-due totals grouped by account type.";

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

        var query = _db.Accounts.AsNoTracking();
        if (!string.IsNullOrEmpty(state))
            query = query.Where(a => a.Customer.State == state);

        var groups = await query
            .GroupBy(a => a.AccountType)
            .Select(g => new
            {
                Type = g.Key,
                Accounts = g.Count(),
                Open = g.Count(a => a.Status == AccountStatus.Open),
                Balance = g.Sum(a => a.CurrentBalance),
                Limit = g.Sum(a => a.CreditLimit),
                PastDue = g.Sum(a => a.AmountPastDue)
            })
            .OrderBy(g => g.Type)
            .ToListAsync(ct);

        return new ReportResultDto
        {
            ReportType = Key,
            DisplayName = DisplayName,
            GeneratedAtUtc = DateTime.UtcNow,
            Columns = new List<ReportColumnDto>
            {
                new() { Name = "Account Type", Type = "string" },
                new() { Name = "Accounts", Type = "number" },
                new() { Name = "Open", Type = "number" },
                new() { Name = "Total Balance", Type = "number" },
                new() { Name = "Total Limit", Type = "number" },
                new() { Name = "Past Due", Type = "number" }
            },
            Rows = groups.Select(g => new List<string>
            {
                g.Type.ToString(),
                ReportFormat.Number(g.Accounts),
                ReportFormat.Number(g.Open),
                ReportFormat.Money(g.Balance),
                ReportFormat.Money(g.Limit),
                ReportFormat.Money(g.PastDue)
            }).ToList(),
            RowCount = groups.Count
        };
    }
}
