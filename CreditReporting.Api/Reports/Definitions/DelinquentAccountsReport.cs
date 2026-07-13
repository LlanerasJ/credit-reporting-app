using CreditReporting.Api.Data;
using CreditReporting.Api.Data.Entities;
using CreditReporting.Api.Services;
using CreditReporting.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CreditReporting.Api.Reports.Definitions;

/// <summary>
/// Accounts that are charged off, in collection, or carrying a past-due
/// balance, optionally filtered by customer state and a minimum past-due amount.
/// </summary>
public class DelinquentAccountsReport : IReportDefinition
{
    private readonly AppDbContext _db;
    public DelinquentAccountsReport(AppDbContext db) => _db = db;

    public string Key => "delinquent-accounts";
    public string DisplayName => "Delinquent Accounts";
    public string Description =>
        "Accounts charged off, in collection, or past due, with the customer, creditor, and amounts.";

    public IReadOnlyList<ReportParameterDto> Parameters { get; } = new List<ReportParameterDto>
    {
        new()
        {
            Name = "state", Label = "Customer state (2 letters)",
            Type = "string", Required = false
        },
        new()
        {
            Name = "minPastDue", Label = "Minimum amount past due",
            Type = "decimal", Required = false, DefaultValue = "0"
        }
    };

    public async Task<ReportResultDto> ExecuteAsync(ReportArgs args, CancellationToken ct = default)
    {
        string? state = args.GetString("state")?.ToUpperInvariant();
        decimal minPastDue = args.GetDecimal("minPastDue") ?? 0m;

        var query = _db.Accounts.AsNoTracking()
            .Include(a => a.Customer)
            .Where(a => a.Status == AccountStatus.ChargeOff
                        || a.Status == AccountStatus.Collection
                        || a.AmountPastDue > 0)
            .Where(a => a.AmountPastDue >= minPastDue);

        if (!string.IsNullOrEmpty(state))
            query = query.Where(a => a.Customer.State == state);

        var accounts = await query
            .OrderByDescending(a => a.AmountPastDue)
            .ToListAsync(ct);

        return new ReportResultDto
        {
            ReportType = Key,
            DisplayName = DisplayName,
            GeneratedAtUtc = DateTime.UtcNow,
            Columns = new List<ReportColumnDto>
            {
                new() { Name = "Customer", Type = "string" },
                new() { Name = "SSN", Type = "string" },
                new() { Name = "State", Type = "string" },
                new() { Name = "Creditor", Type = "string" },
                new() { Name = "Account #", Type = "string" },
                new() { Name = "Type", Type = "string" },
                new() { Name = "Status", Type = "string" },
                new() { Name = "Past Due", Type = "number" },
                new() { Name = "Balance", Type = "number" },
                new() { Name = "Opened", Type = "date" }
            },
            Rows = accounts.Select(a => new List<string>
            {
                $"{a.Customer.FirstName} {a.Customer.LastName}",
                Masking.MaskSsn(a.Customer.SsnLast4),
                a.Customer.State,
                a.CreditorName,
                Masking.MaskAccountNumber(a.AccountNumber),
                a.AccountType.ToString(),
                a.Status.ToString(),
                ReportFormat.Money(a.AmountPastDue),
                ReportFormat.Money(a.CurrentBalance),
                ReportFormat.Date(a.OpenDate)
            }).ToList(),
            RowCount = accounts.Count
        };
    }
}
