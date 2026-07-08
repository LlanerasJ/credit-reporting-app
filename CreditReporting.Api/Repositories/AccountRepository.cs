using CreditReporting.Api.Data;
using CreditReporting.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditReporting.Api.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _db;
    public AccountRepository(AppDbContext db) => _db = db;

    public Task<List<Account>> GetByCustomerAsync(int customerId, CancellationToken ct = default) =>
        _db.Accounts.AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .Include(a => a.PaymentHistory.OrderByDescending(p => p.PaymentDate))
            .OrderBy(a => a.OpenDate)
            .ToListAsync(ct);

    public Task<Account?> GetWithHistoryAsync(int accountId, CancellationToken ct = default) =>
        _db.Accounts.AsNoTracking()
            .Include(a => a.PaymentHistory.OrderByDescending(p => p.PaymentDate))
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == accountId, ct);

    public Task<List<Account>> GetForReportingWindowAsync(
        DateTime from, DateTime to, List<int>? accountIds, CancellationToken ct = default)
    {
        var query = _db.Accounts.AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.PaymentHistory.OrderByDescending(p => p.PaymentDate))
            .Where(a => a.PaymentHistory.Any(p => p.PaymentDate >= from && p.PaymentDate <= to));

        if (accountIds is { Count: > 0 })
            query = query.Where(a => accountIds.Contains(a.Id));

        return query.OrderBy(a => a.CustomerId).ThenBy(a => a.Id).ToListAsync(ct);
    }
}
