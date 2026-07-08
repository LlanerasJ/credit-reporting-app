using CreditReporting.Api.Data.Entities;

namespace CreditReporting.Api.Repositories;

public interface IAccountRepository
{
    Task<List<Account>> GetByCustomerAsync(int customerId, CancellationToken ct = default);
    Task<Account?> GetWithHistoryAsync(int accountId, CancellationToken ct = default);
    /// <summary>Accounts with activity (payment records) inside the window; used for Metro 2 generation.</summary>
    Task<List<Account>> GetForReportingWindowAsync(DateTime from, DateTime to, List<int>? accountIds, CancellationToken ct = default);
}
