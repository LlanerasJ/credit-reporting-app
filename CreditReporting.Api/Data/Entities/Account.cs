namespace CreditReporting.Api.Data.Entities;

public enum AccountType
{
    CreditCard = 0,
    AutoLoan = 1,
    Mortgage = 2,
    PersonalLoan = 3,
    StudentLoan = 4,
    RetailCard = 5,
    LineOfCredit = 6
}

public enum AccountStatus
{
    Open = 0,
    Closed = 1,
    PaidClosed = 2,
    ChargeOff = 3,
    Collection = 4
}

public class Account
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string AccountNumber { get; set; } = "";
    public AccountType AccountType { get; set; }
    public AccountStatus Status { get; set; }
    public DateTime OpenDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal AmountPastDue { get; set; }

    /// <summary>Metro 2 portfolio type: C (line of credit), I (installment), M (mortgage), O (open), R (revolving).</summary>
    public string PortfolioType { get; set; } = "R";
    /// <summary>Metro 2 ECOA code: 1 individual, 2 joint, 3 authorized user, 5 co-maker, 7 maker...</summary>
    public string EcoaCode { get; set; } = "1";
    public string CreditorName { get; set; } = "";

    public List<PaymentRecord> PaymentHistory { get; set; } = new();
}
