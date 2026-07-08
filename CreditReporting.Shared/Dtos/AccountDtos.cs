namespace CreditReporting.Shared.Dtos;

public class AccountDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    /// <summary>Masked account number, e.g. "****5678".</summary>
    public string AccountNumberMasked { get; set; } = "";
    public string AccountType { get; set; } = "";
    public string PortfolioType { get; set; } = "";
    public string Status { get; set; } = "";
    public string EcoaCode { get; set; } = "";
    public DateTime OpenDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal AmountPastDue { get; set; }
    public string CreditorName { get; set; } = "";
    public List<PaymentHistoryDto> PaymentHistory { get; set; } = new();

    /// <summary>Most-recent-first payment rating string, e.g. "000012100...".</summary>
    public string PaymentHistoryProfile { get; set; } = "";
}

public class PaymentHistoryDto
{
    public DateTime PaymentDate { get; set; }
    public decimal Balance { get; set; }
    public decimal AmountPaid { get; set; }
    public int DaysLate { get; set; }
    /// <summary>Metro 2-style rating: 0 = current, 1 = 30-59, 2 = 60-89, ...</summary>
    public string PaymentRating { get; set; } = "0";
}
