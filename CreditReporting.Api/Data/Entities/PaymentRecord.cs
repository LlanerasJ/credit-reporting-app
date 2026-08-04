namespace CreditReporting.Api.Data.Entities;

/// <summary>How a monthly payment was made.</summary>
public enum PaymentType
{
    /// <summary>No payment that month, or the method wasn't captured (e.g. parsed Metro 2 records).</summary>
    Unknown = 0,
    Ach = 1,
    AutoPay = 2,
    DebitCard = 3,
    CreditCard = 4,
    Check = 5,
    Cash = 6,
    Transfer = 7
}

/// <summary>One monthly balance/payment observation for an account.</summary>
public class PaymentRecord
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public DateTime PaymentDate { get; set; }
    public decimal Balance { get; set; }
    public decimal AmountPaid { get; set; }
    public int DaysLate { get; set; }

    /// <summary>How the payment was made. <see cref="PaymentType.Unknown"/> for months with no payment.</summary>
    public PaymentType PaymentType { get; set; } = PaymentType.Unknown;

    /// <summary>Metro 2-style payment rating: 0 current, 1 = 30-59 late, 2 = 60-89, 3 = 90-119, 4 = 120-149, 5 = 150-179, 6 = 180+.</summary>
    public string PaymentRating { get; set; } = "0";
}
