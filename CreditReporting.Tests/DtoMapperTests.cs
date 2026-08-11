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

    [Fact]
    public void ToDto_reports_the_most_recent_payment_date_and_amount()
    {
        var account = MakeAccount(
            new PaymentRecord { PaymentDate = new DateTime(2024, 1, 15), AmountPaid = 100m },
            new PaymentRecord { PaymentDate = new DateTime(2024, 2, 15), AmountPaid = 150m });

        var dto = DtoMapper.ToDto(account);

        Assert.Equal(new DateTime(2024, 2, 15), dto.LastPaymentDate);
        Assert.Equal(150m, dto.LastPaymentAmount);
    }

    [Fact]
    public void ToDto_skips_back_to_the_last_real_payment_when_the_newest_month_was_missed()
    {
        var account = MakeAccount(
            new PaymentRecord { PaymentDate = new DateTime(2024, 1, 15), AmountPaid = 100m },
            new PaymentRecord { PaymentDate = new DateTime(2024, 2, 15), AmountPaid = 0m, DaysLate = 30, PaymentRating = "1" });

        var dto = DtoMapper.ToDto(account);

        Assert.Equal(new DateTime(2024, 1, 15), dto.LastPaymentDate);
        Assert.Equal(100m, dto.LastPaymentAmount);
    }

    [Fact]
    public void ToDto_leaves_last_payment_date_null_when_the_account_has_never_been_paid()
    {
        var account = MakeAccount(
            new PaymentRecord { PaymentDate = new DateTime(2024, 1, 15), AmountPaid = 0m, DaysLate = 30, PaymentRating = "1" },
            new PaymentRecord { PaymentDate = new DateTime(2024, 2, 15), AmountPaid = 0m, DaysLate = 60, PaymentRating = "2" });

        var dto = DtoMapper.ToDto(account);

        Assert.Null(dto.LastPaymentDate);
        Assert.Equal(0m, dto.LastPaymentAmount);
    }
}
