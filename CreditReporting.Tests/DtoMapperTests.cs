using CreditReporting.Api.Data.Entities;
using CreditReporting.Api.Services;

namespace CreditReporting.Tests;

public class DtoMapperTests
{
    private static Account MakeAccount(params PaymentRecord[] history) => new()
    {
        Id = 1,
        AccountNumber = "4001123456789",
        AccountType = AccountType.CreditCard,
        Status = AccountStatus.Open,
        OpenDate = new DateTime(2023, 1, 1),
        CreditLimit = 5000m,
        PaymentHistory = history.ToList()
    };

    [Theory]
    [InlineData(PaymentType.Ach, "ACH")]
    [InlineData(PaymentType.AutoPay, "Auto-Pay")]
    [InlineData(PaymentType.DebitCard, "Debit Card")]
    [InlineData(PaymentType.CreditCard, "Credit Card")]
    [InlineData(PaymentType.Check, "Check")]
    [InlineData(PaymentType.Cash, "Cash")]
    [InlineData(PaymentType.Transfer, "Transfer")]
    public void SpellOut_expands_payment_type(PaymentType type, string expected) =>
        Assert.Equal(expected, DtoMapper.SpellOut(type));

    [Fact]
    public void SpellOut_leaves_unknown_payment_type_blank() =>
        Assert.Equal("", DtoMapper.SpellOut(PaymentType.Unknown));

    [Fact]
    public void ToDto_carries_the_payment_type_onto_each_history_row()
    {
        var account = MakeAccount(
            new PaymentRecord
            {
                PaymentDate = new DateTime(2024, 1, 15),
                Balance = 900m,
                AmountPaid = 100m,
                PaymentType = PaymentType.AutoPay
            },
            new PaymentRecord
            {
                PaymentDate = new DateTime(2024, 2, 15),
                Balance = 900m,
                AmountPaid = 0m,
                DaysLate = 30,
                PaymentRating = "1"
            });

        var dto = DtoMapper.ToDto(account);

        // Newest first, so the missed month leads.
        Assert.Equal("", dto.PaymentHistory[0].PaymentType);
        Assert.Equal("Auto-Pay", dto.PaymentHistory[1].PaymentType);
    }

    [Theory]
    [InlineData("C", "Line of Credit")]
    [InlineData("I", "Installment")]
    [InlineData("M", "Mortgage")]
    [InlineData("O", "Open")]
    [InlineData("R", "Revolving")]
    public void SpellOut_expands_portfolio_type(string code, string expected) =>
        Assert.Equal(expected, DtoMapper.SpellOutPortfolioType(code));

    [Theory]
    [InlineData("1", "Individual")]
    [InlineData("2", "Joint")]
    [InlineData("3", "Authorized User")]
    [InlineData("5", "Co-Maker")]
    [InlineData("7", "Maker")]
    public void SpellOut_expands_ecoa_code(string code, string expected) =>
        Assert.Equal(expected, DtoMapper.SpellOutEcoaCode(code));

    [Theory]
    [InlineData("")]
    [InlineData("Q")]
    public void SpellOut_passes_through_an_unrecognized_portfolio_type(string code) =>
        Assert.Equal(code, DtoMapper.SpellOutPortfolioType(code));

    [Theory]
    [InlineData("")]
    [InlineData("8")]
    public void SpellOut_passes_through_an_unrecognized_ecoa_code(string code) =>
        Assert.Equal(code, DtoMapper.SpellOutEcoaCode(code));

    [Fact]
    public void ToDto_spells_out_portfolio_type_and_ecoa_code()
    {
        var account = MakeAccount();
        account.PortfolioType = "I";
        account.EcoaCode = "2";

        var dto = DtoMapper.ToDto(account);

        Assert.Equal("Installment", dto.PortfolioType);
        Assert.Equal("Joint", dto.EcoaCode);
    }
}
