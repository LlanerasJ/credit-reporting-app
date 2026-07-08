using CreditReporting.Api.Data.Entities;
using CreditReporting.Shared.Dtos;

namespace CreditReporting.Api.Services;

/// <summary>Maps EF entities to DTOs. SSNs and account numbers get masked here.</summary>
public static class DtoMapper
{
    public static CustomerSummaryDto ToSummary(Customer c) => new()
    {
        Id = c.Id,
        FirstName = c.FirstName,
        LastName = c.LastName,
        DateOfBirth = c.DateOfBirth,
        SsnMasked = Masking.MaskSsn(c.SsnLast4),
        City = c.City,
        State = c.State,
        OpenAccountCount = c.Accounts.Count(a => a.Status == AccountStatus.Open)
    };

    public static CustomerDetailDto ToDetail(Customer c) => new()
    {
        Id = c.Id,
        FirstName = c.FirstName,
        LastName = c.LastName,
        DateOfBirth = c.DateOfBirth,
        SsnMasked = Masking.MaskSsn(c.SsnLast4),
        AddressLine1 = c.AddressLine1,
        City = c.City,
        State = c.State,
        PostalCode = c.PostalCode,
        Phone = c.Phone,
        Email = c.Email
    };

    public static AccountDto ToDto(Account a) => new()
    {
        Id = a.Id,
        CustomerId = a.CustomerId,
        AccountNumberMasked = Masking.MaskAccountNumber(a.AccountNumber),
        AccountType = SpellOut(a.AccountType),
        PortfolioType = a.PortfolioType,
        Status = SpellOut(a.Status),
        EcoaCode = a.EcoaCode,
        OpenDate = a.OpenDate,
        ClosedDate = a.ClosedDate,
        CreditLimit = a.CreditLimit,
        CurrentBalance = a.CurrentBalance,
        AmountPastDue = a.AmountPastDue,
        CreditorName = a.CreditorName,
        PaymentHistoryProfile = BuildPaymentProfile(a),
        PaymentHistory = a.PaymentHistory
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new PaymentHistoryDto
            {
                PaymentDate = p.PaymentDate,
                Balance = p.Balance,
                AmountPaid = p.AmountPaid,
                DaysLate = p.DaysLate,
                PaymentRating = p.PaymentRating
            }).ToList()
    };

    public static CreditInquiryDto ToDto(CreditInquiry i) => new()
    {
        PulledBy = i.PulledBy,
        PulledDate = i.PulledDate,
        InquiryType = i.InquiryType,
        Purpose = i.Purpose
    };

    public static CreditScoreDto ToDto(CreditScore s) => new()
    {
        Score = s.Score,
        ScoreDate = s.ScoreDate,
        Bureau = s.Bureau,
        ModelVersion = s.ModelVersion
    };

    /// <summary>Up to 24 months of ratings, most recent first ("B" pads months before opening).</summary>
    public static string BuildPaymentProfile(Account a)
    {
        var ratings = a.PaymentHistory
            .OrderByDescending(p => p.PaymentDate)
            .Take(24)
            .Select(p => string.IsNullOrEmpty(p.PaymentRating) ? "0" : p.PaymentRating[..1])
            .ToList();
        return string.Concat(ratings).PadRight(24, 'B');
    }

    public static string SpellOut(AccountType t) => t switch
    {
        AccountType.CreditCard => "Credit Card",
        AccountType.AutoLoan => "Auto Loan",
        AccountType.Mortgage => "Mortgage",
        AccountType.PersonalLoan => "Personal Loan",
        AccountType.StudentLoan => "Student Loan",
        AccountType.RetailCard => "Retail Card",
        AccountType.LineOfCredit => "Line of Credit",
        _ => t.ToString()
    };

    public static string SpellOut(AccountStatus s) => s switch
    {
        AccountStatus.Open => "Open",
        AccountStatus.Closed => "Closed",
        AccountStatus.PaidClosed => "Paid/Closed",
        AccountStatus.ChargeOff => "Charge-Off",
        AccountStatus.Collection => "Collection",
        _ => s.ToString()
    };
}
