using CreditReporting.Api.Data;
using CreditReporting.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditReporting.Api.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _db;
    public CustomerRepository(AppDbContext db) => _db = db;

    public async Task<List<Customer>> SearchAsync(string? name, string? ssnLast4, CancellationToken ct = default)
    {
        IQueryable<Customer> query = _db.Customers.AsNoTracking().Include(c => c.Accounts);

        if (!string.IsNullOrWhiteSpace(name))
        {
            string pattern = $"%{name.Trim()}%";
            query = query.Where(c =>
                EF.Functions.Like(c.FirstName, pattern) ||
                EF.Functions.Like(c.LastName, pattern) ||
                EF.Functions.Like(c.FirstName + " " + c.LastName, pattern));
        }

        if (!string.IsNullOrWhiteSpace(ssnLast4))
            query = query.Where(c => c.SsnLast4 == ssnLast4.Trim());

        return await query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
                          .Take(100)
                          .ToListAsync(ct);
    }

    public Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Customer?> GetWithFullHistoryAsync(int id, CancellationToken ct = default) =>
        _db.Customers.AsNoTracking()
            .Include(c => c.Accounts).ThenInclude(a => a.PaymentHistory)
            .Include(c => c.Inquiries)
            .Include(c => c.Scores)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
}
