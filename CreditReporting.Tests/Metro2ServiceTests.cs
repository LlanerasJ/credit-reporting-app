using CreditReporting.Api.Data.Entities;
using CreditReporting.Api.Metro2;
using CreditReporting.Api.Metro2.Records;
using CreditReporting.Api.Repositories;
using CreditReporting.Shared.Dtos;

namespace CreditReporting.Tests;

public class Metro2ServiceTests
{
    private const string DefaultFurnisherId = "DEMOFURN0001";
    private const string DefaultReporterName = "Demo Data Furnisher Inc";

    /// <summary>Serves a configurable set of accounts and honours the account-id filter.</summary>
    private class StubAccountRepository : IAccountRepository
    {
        private readonly List<Account> _accounts;
        public StubAccountRepository(List<Account> accounts) => _accounts = accounts;

        public Task<List<Account>> GetByCustomerAsync(int customerId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Account?> GetWithHistoryAsync(int accountId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<List<Account>> GetForReportingWindowAsync(
            DateTime from, DateTime to, List<int>? accountIds, CancellationToken ct = default)
        {
            var result = accountIds is { Count: > 0 }
                ? _accounts.Where(a => accountIds.Contains(a.Id)).ToList()
                : _accounts.ToList();
            return Task.FromResult(result);
        }
    }

    private static Account MakeAccount(int id, string accountNumber, string first, string last, decimal balance = 1234m) => new()
    {
        Id = id,
        AccountNumber = accountNumber,
        AccountType = AccountType.CreditCard,
        Status = AccountStatus.Open,
        OpenDate = new DateTime(2020, 5, 1),
        CreditLimit = 5000m,
        CurrentBalance = balance,
        Customer = new Customer
        {
            FirstName = first,
            LastName = last,
            DateOfBirth = new DateTime(1980, 3, 15),
            SsnLast4 = "1000",
            AddressLine1 = "101 Elm St",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701",
            Phone = "5551234567"
        }
    };

    /// <summary>Builds a service over the given accounts, defaulting to one fixed account.</summary>
    private static Metro2Service NewService(params Account[] accounts)
    {
        var population = accounts.Length > 0
            ? accounts.ToList()
            : new List<Account> { MakeAccount(1, "4001123456789", "Ava", "Testman") };
        return new(new StubAccountRepository(population), new Metro2Writer(), new Metro2Parser(), new Metro2Validator());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Falls_back_to_the_default_furnisher_id(string? requested)
    {
        var (file, _) = await NewService().BuildFileAsync(
            new Metro2GenerateRequest { FurnisherIdentificationNumber = requested });

        Assert.Equal(DefaultFurnisherId, Assert.Single(file.BaseRecords).IdentificationNumber);
    }

    [Fact]
    public async Task Request_furnisher_id_overrides_the_default()
    {
        var (file, _) = await NewService().BuildFileAsync(
            new Metro2GenerateRequest { FurnisherIdentificationNumber = "ACMEFURN9999" });

        Assert.Equal("ACMEFURN9999", Assert.Single(file.BaseRecords).IdentificationNumber);
    }

    [Fact]
    public async Task Header_program_identifier_is_capped_at_ten_characters()
    {
        var (file, _) = await NewService().BuildFileAsync(
            new Metro2GenerateRequest { FurnisherIdentificationNumber = "ACMEFURNISHER99999" });

        Assert.Equal("ACMEFURNIS", file.Header.ProgramIdentifier);
    }

    [Fact]
    public async Task Oversized_furnisher_id_does_not_break_fixed_width_layout()
    {
        var service = NewService();
        var (file, _) = await service.BuildFileAsync(
            new Metro2GenerateRequest { FurnisherIdentificationNumber = new string('X', 60) });

        var lines = service.Serialize(file)
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(Metro2HeaderRecord.RecordLength, lines[0].Length);
        Assert.Equal(Metro2BaseRecord.RecordLength, lines[1].Length);
    }

    [Fact]
    public async Task Build_file_reports_only_the_requested_accounts()
    {
        var service = NewService(
            MakeAccount(1, "4001111111111", "Ava", "Testman"),
            MakeAccount(2, "4002222222222", "Ben", "Buyer"),
            MakeAccount(3, "4003333333333", "Cid", "Consumer"));

        var (file, _) = await service.BuildFileAsync(
            new Metro2GenerateRequest { AccountIds = new List<int> { 1, 3 } });

        Assert.Equal(2, file.BaseRecords.Count);
        Assert.Equal("4001111111111", file.BaseRecords[0].ConsumerAccountNumber);
        Assert.Equal("4003333333333", file.BaseRecords[1].ConsumerAccountNumber);
    }

    [Fact]
    public async Task Null_account_ids_reports_every_account_in_the_window()
    {
        var service = NewService(
            MakeAccount(1, "4001111111111", "Ava", "Testman"),
            MakeAccount(2, "4002222222222", "Ben", "Buyer"));

        var (file, _) = await service.BuildFileAsync(new Metro2GenerateRequest());

        Assert.Equal(2, file.BaseRecords.Count);
    }

    [Fact]
    public async Task Lists_reporting_accounts_with_masked_numbers_and_names()
    {
        var service = NewService(
            MakeAccount(1, "4001123456789", "Ava", "Testman", 1500m),
            MakeAccount(2, "4002987654321", "Ben", "Buyer", 250m));

        var candidates = await service.ListReportingAccountsAsync(null, null);

        Assert.Collection(candidates,
            c =>
            {
                Assert.Equal(1, c.Id);
                Assert.Equal("****6789", c.AccountNumberMasked);
                Assert.Equal("Ava Testman", c.CustomerName);
                Assert.Equal("Credit Card", c.AccountType);
                Assert.Equal(1500m, c.CurrentBalance);
            },
            c =>
            {
                Assert.Equal(2, c.Id);
                Assert.Equal("****4321", c.AccountNumberMasked);
                Assert.Equal("Ben Buyer", c.CustomerName);
            });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Falls_back_to_the_default_reporter_name(string? requested)
    {
        var (file, _) = await NewService().BuildFileAsync(
            new Metro2GenerateRequest { ReporterName = requested });

        Assert.Equal(DefaultReporterName, file.Header.ReporterName);
    }

    [Fact]
    public async Task Request_reporter_name_overrides_the_default()
    {
        var (file, _) = await NewService().BuildFileAsync(
            new Metro2GenerateRequest { ReporterName = "Acme Data Furnisher LLC" });

        Assert.Equal("Acme Data Furnisher LLC", file.Header.ReporterName);
    }

    [Fact]
    public async Task Oversized_reporter_name_does_not_break_fixed_width_layout()
    {
        var service = NewService();
        var (file, _) = await service.BuildFileAsync(
            new Metro2GenerateRequest { ReporterName = new string('X', 80) });

        var lines = service.Serialize(file)
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(Metro2HeaderRecord.RecordLength, lines[0].Length);
        Assert.Equal(Metro2BaseRecord.RecordLength, lines[1].Length);
    }
}
