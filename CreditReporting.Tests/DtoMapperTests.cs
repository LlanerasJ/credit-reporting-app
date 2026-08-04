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
}
