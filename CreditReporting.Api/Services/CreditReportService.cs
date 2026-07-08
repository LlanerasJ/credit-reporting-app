using CreditReporting.Api.Data.Entities;
using CreditReporting.Api.Repositories;
using CreditReporting.Shared.Dtos;

namespace CreditReporting.Api.Services;

public interface ICreditReportService
{
    Task<CreditReportDto?> BuildReportAsync(int customerId, CancellationToken ct = default);
}

/// <summary>
/// Builds the full credit report for a customer: accounts, payment history,
/// inquiries, scores, and summary totals.
/// </summary>
public class CreditReportService : ICreditReportService
{
    private readonly ICustomerRepository _customers;
    public CreditReportService(ICustomerRepository customers) => _customers = customers;

    public async Task<CreditReportDto?> BuildReportAsync(int customerId, CancellationToken ct = default)
    {
        var customer = await _customers.GetWithFullHistoryAsync(customerId, ct);
        if (customer is null) return null;

        var accounts = customer.Accounts.Select(DtoMapper.ToDto).ToList();
        var openAccounts = customer.Accounts.Where(a => a.Status == AccountStatus.Open).ToList();

        var openRevolving = openAccounts.Where(a => a.PortfolioType is "R" or "C").ToList();
        decimal revolvingLimit = openRevolving.Sum(a => a.CreditLimit);
        decimal utilization = revolvingLimit > 0
            ? Math.Round(openRevolving.Sum(a => a.CurrentBalance) / revolvingLimit, 4)
            : 0m;

        return new CreditReportDto
        {
            ReportId = Guid.NewGuid(),
            GeneratedAtUtc = DateTime.UtcNow,
            Customer = DtoMapper.ToDetail(customer),
            Accounts = accounts,
            Inquiries = customer.Inquiries
                .OrderByDescending(i => i.PulledDate)
                .Select(DtoMapper.ToDto).ToList(),
            Scores = customer.Scores
                .OrderByDescending(s => s.ScoreDate)
                .Select(DtoMapper.ToDto).ToList(),
            Summary = new CreditReportSummaryDto
            {
                TotalAccounts = customer.Accounts.Count,
                OpenAccounts = openAccounts.Count,
                TotalBalance = customer.Accounts.Sum(a => a.CurrentBalance),
                TotalCreditLimit = openAccounts.Sum(a => a.CreditLimit),
                TotalPastDue = customer.Accounts.Sum(a => a.AmountPastDue),
                DelinquentAccounts = customer.Accounts.Count(a =>
                    a.Status is AccountStatus.ChargeOff or AccountStatus.Collection || a.AmountPastDue > 0),
                HardInquiriesLast12Months = customer.Inquiries.Count(i =>
                    i.InquiryType == "Hard" && i.PulledDate >= DateTime.UtcNow.AddMonths(-12)),
                Utilization = utilization
            }
        };
    }
}
