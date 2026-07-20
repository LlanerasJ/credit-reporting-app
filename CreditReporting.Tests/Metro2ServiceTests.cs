using CreditReporting.Api.Data.Entities;
using CreditReporting.Api.Metro2;
using CreditReporting.Api.Metro2.Records;
using CreditReporting.Api.Repositories;
using CreditReporting.Shared.Dtos;

namespace CreditReporting.Tests;

public class Metro2ServiceTests
{
    private const string DefaultFurnisherId = "DEMOFURN0001";

    /// <summary>Serves one fixed account so tests can assert on the generated records.</summary>
    private class StubAccountRepository : IAccountRepository
    {
        public Task<List<Account>> GetByCustomerAsync(int customerId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Account?> GetWithHistoryAsync(int accountId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<List<Account>> GetForReportingWindowAsync(
            DateTime from, DateTime to, List<int>? accountIds, CancellationToken ct = default)
        {
            var account = new Account
            {
                AccountNumber = "4001123456789",
                AccountType = AccountType.CreditCard,
                Status = AccountStatus.Open,
                OpenDate = new DateTime(2020, 5, 1),
                CreditLimit = 5000m,
                CurrentBalance = 1234m,
                Customer = new Customer
                {
                    FirstName = "Ava",
                    LastName = "Testman",
                    DateOfBirth = new DateTime(1980, 3, 15),
                    SsnLast4 = "1000",
                    AddressLine1 = "101 Elm St",
                    City = "Springfield",
                    State = "IL",
                    PostalCode = "62701",
                    Phone = "5551234567"
                }
            };
            return Task.FromResult(new List<Account> { account });
        }
    }

    private static Metro2Service NewService() =>
        new(new StubAccountRepository(), new Metro2Writer(), new Metro2Parser(), new Metro2Validator());

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
}
