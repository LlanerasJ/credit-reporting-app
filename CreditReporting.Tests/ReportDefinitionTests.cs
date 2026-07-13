using CreditReporting.Api.Data;
using CreditReporting.Api.Data.Entities;
using CreditReporting.Api.Reports;
using CreditReporting.Api.Reports.Definitions;
using Microsoft.EntityFrameworkCore;

namespace CreditReporting.Tests;

/// <summary>Execution tests for the Phase 4 report definitions.</summary>
public class ReportDefinitionTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ReportArgs Bind(IReportDefinition report, Dictionary<string, string?> supplied)
    {
        var args = ReportArgs.Bind(report.Parameters, supplied, out var errors);
        Assert.Empty(errors);
        return args!;
    }

    [Fact]
    public async Task PortfolioSummary_groups_by_type_and_filters_by_state()
    {
        using var db = NewDb();
        var texan = new Customer { State = "TX", SsnLast4 = "1000" };
        texan.Accounts.Add(new Account
        {
            AccountType = AccountType.CreditCard, Status = AccountStatus.Open,
            CurrentBalance = 100m, CreditLimit = 1000m
        });
        texan.Accounts.Add(new Account
        {
            AccountType = AccountType.CreditCard, Status = AccountStatus.Closed,
            CurrentBalance = 0m, CreditLimit = 500m
        });
        var ohioan = new Customer { State = "OH", SsnLast4 = "1001" };
        ohioan.Accounts.Add(new Account
        {
            AccountType = AccountType.Mortgage, Status = AccountStatus.Open,
            CurrentBalance = 90000m, CreditLimit = 120000m, AmountPastDue = 250m
        });
        db.Customers.AddRange(texan, ohioan);
        db.SaveChanges();

        var report = new PortfolioSummaryByTypeReport(db);

        var all = await report.ExecuteAsync(Bind(report, new()));
        Assert.Equal(2, all.RowCount);
        var creditCards = all.Rows.Single(r => r[0] == "CreditCard");
        Assert.Equal("2", creditCards[1]);      // accounts
        Assert.Equal("1", creditCards[2]);      // open
        Assert.Equal("100.00", creditCards[3]); // balance
        Assert.Equal("1500.00", creditCards[4]); // limit

        var txOnly = await report.ExecuteAsync(Bind(report, new() { ["state"] = "tx" }));
        Assert.Single(txOnly.Rows);
        Assert.Equal("CreditCard", txOnly.Rows[0][0]);
    }

    [Fact]
    public async Task AuditActivity_filters_by_user_action_and_date_range()
    {
        using var db = NewDb();
        db.AuditLog.AddRange(
            new AuditLogEntry { TimestampUtc = new DateTime(2026, 7, 1, 10, 0, 0), Username = "analyst", Action = "CreditReportViewed", CustomerId = 3, Purpose = "Review" },
            new AuditLogEntry { TimestampUtc = new DateTime(2026, 7, 2, 11, 0, 0), Username = "analyst", Action = "ReportRun", CustomerId = 0, Purpose = "delinquent-accounts: (no parameters)" },
            new AuditLogEntry { TimestampUtc = new DateTime(2026, 7, 3, 12, 0, 0), Username = "admin", Action = "ReportRun", CustomerId = 0, Purpose = "portfolio-summary: (no parameters)" });
        db.SaveChanges();

        var report = new AuditActivityReport(db);

        var all = await report.ExecuteAsync(Bind(report, new()));
        Assert.Equal(3, all.RowCount);
        Assert.Equal("2026-07-03 12:00:00", all.Rows[0][0]); // newest first
        Assert.Equal("", all.Rows[0][3]);                    // CustomerId 0 shown blank

        var analystRuns = await report.ExecuteAsync(Bind(report,
            new() { ["username"] = "analyst", ["action"] = "ReportRun" }));
        Assert.Single(analystRuns.Rows);
        Assert.Equal("delinquent-accounts: (no parameters)", analystRuns.Rows[0][4]);

        // "to" is inclusive: entries on July 2 itself are kept
        var range = await report.ExecuteAsync(Bind(report,
            new() { ["from"] = "2026-07-02", ["to"] = "2026-07-02" }));
        Assert.Single(range.Rows);
        Assert.Equal("analyst", range.Rows[0][1]);
    }

    [Fact]
    public async Task ScoreDistribution_uses_latest_score_per_customer()
    {
        using var db = NewDb();
        var improving = new Customer { State = "TX", SsnLast4 = "1000" };
        improving.Scores.Add(new CreditScore { Score = 560, ScoreDate = new DateTime(2025, 1, 1) });
        improving.Scores.Add(new CreditScore { Score = 700, ScoreDate = new DateTime(2026, 6, 1) }); // latest: Good
        var excellent = new Customer { State = "OH", SsnLast4 = "1001" };
        excellent.Scores.Add(new CreditScore { Score = 810, ScoreDate = new DateTime(2026, 6, 1) });
        var noHistory = new Customer { State = "TX", SsnLast4 = "1002" };
        db.Customers.AddRange(improving, excellent, noHistory);
        db.SaveChanges();

        var report = new ScoreDistributionReport(db);
        var result = await report.ExecuteAsync(Bind(report, new()));

        Assert.Equal(5, result.RowCount); // always all five bands
        var byBand = result.Rows.ToDictionary(r => r[0]);
        Assert.Equal("1", byBand["Good"][2]);      // the improving customer counts once, at 700
        Assert.Equal("700", byBand["Good"][3]);
        Assert.Equal("1", byBand["Excellent"][2]);
        Assert.Equal("0", byBand["Poor"][2]);      // old 560 score no longer counted
        Assert.Equal("", byBand["Poor"][3]);       // empty average for empty band
    }
}
