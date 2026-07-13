using CreditReporting.Api.Data;
using CreditReporting.Api.Data.Entities;
using CreditReporting.Api.Reports;
using CreditReporting.Api.Reports.Definitions;
using Microsoft.EntityFrameworkCore;

namespace CreditReporting.Tests;

public class DelinquentAccountsReportTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var texan = new Customer { FirstName = "Ava", LastName = "Testman", State = "TX", SsnLast4 = "1000" };
        var ohioan = new Customer { FirstName = "Liam", LastName = "Sampleton", State = "OH", SsnLast4 = "1001" };

        texan.Accounts.Add(new Account
        {
            AccountNumber = "4001123456789", Status = AccountStatus.ChargeOff,
            AmountPastDue = 750m, CurrentBalance = 900m, CreditorName = "Acme Card Services"
        });
        texan.Accounts.Add(new Account
        {
            AccountNumber = "4001123450000", Status = AccountStatus.Open,
            AmountPastDue = 0m, CurrentBalance = 100m, CreditorName = "First Demo Bank"
        });
        ohioan.Accounts.Add(new Account
        {
            AccountNumber = "4002123456789", Status = AccountStatus.Open,
            AmountPastDue = 120m, CurrentBalance = 400m, CreditorName = "Mock Mortgage Co"
        });

        db.Customers.AddRange(texan, ohioan);
        db.SaveChanges();
        return db;
    }

    private static ReportArgs BindArgs(DelinquentAccountsReport report, Dictionary<string, string?> supplied)
    {
        var args = ReportArgs.Bind(report.Parameters, supplied, out var errors);
        Assert.Empty(errors);
        return args!;
    }

    [Fact]
    public async Task Returns_delinquent_accounts_only_with_masked_values()
    {
        using var db = NewDb();
        var report = new DelinquentAccountsReport(db);

        var result = await report.ExecuteAsync(BindArgs(report, new()));

        Assert.Equal(2, result.RowCount);
        // Ordered by past due desc: the TX charge-off first
        Assert.Equal("Ava Testman", result.Rows[0][0]);
        Assert.Equal("***-**-1000", result.Rows[0][1]);
        Assert.Equal("****6789", result.Rows[0][4]);
        Assert.Equal("750.00", result.Rows[0][7]);
        // The clean open account is excluded
        Assert.DoesNotContain(result.Rows, r => r[4] == "****0000");
    }

    [Fact]
    public async Task Filters_by_state_and_min_past_due()
    {
        using var db = NewDb();
        var report = new DelinquentAccountsReport(db);

        var byState = await report.ExecuteAsync(
            BindArgs(report, new() { ["state"] = "tx" }));
        Assert.Single(byState.Rows);
        Assert.Equal("TX", byState.Rows[0][2]);

        var byAmount = await report.ExecuteAsync(
            BindArgs(report, new() { ["minPastDue"] = "500" }));
        Assert.Single(byAmount.Rows);
        Assert.Equal("750.00", byAmount.Rows[0][7]);
    }
}
