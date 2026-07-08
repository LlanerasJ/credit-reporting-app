namespace CreditReporting.Shared.Dtos;

/// <summary>
/// Aggregated credit report: customer info, accounts, payment history,
/// inquiries, and score history.
/// </summary>
public class CreditReportDto
{
    public Guid ReportId { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public CustomerDetailDto Customer { get; set; } = new();
    public List<AccountDto> Accounts { get; set; } = new();
    public List<CreditInquiryDto> Inquiries { get; set; } = new();
    public List<CreditScoreDto> Scores { get; set; } = new();
    public CreditReportSummaryDto Summary { get; set; } = new();
}

public class CreditReportSummaryDto
{
    public int TotalAccounts { get; set; }
    public int OpenAccounts { get; set; }
    public decimal TotalBalance { get; set; }
    public decimal TotalCreditLimit { get; set; }
    public decimal TotalPastDue { get; set; }
    public int DelinquentAccounts { get; set; }
    public int HardInquiriesLast12Months { get; set; }
    /// <summary>0-1 revolving utilization across open revolving accounts.</summary>
    public decimal Utilization { get; set; }
}

public class CreditScoreDto
{
    public int Score { get; set; }
    public DateTime ScoreDate { get; set; }
    public string Bureau { get; set; } = "";
    public string ModelVersion { get; set; } = "";
}

public class CreditInquiryDto
{
    public string PulledBy { get; set; } = "";
    public DateTime PulledDate { get; set; }
    /// <summary>"Hard" or "Soft".</summary>
    public string InquiryType { get; set; } = "";
    public string Purpose { get; set; } = "";
}
