using CreditReporting.Api.Data.Entities;

namespace CreditReporting.Api.Repositories;

public interface ICustomerRepository
{
    Task<List<Customer>> SearchAsync(string? name, string? ssnLast4, CancellationToken ct = default);
    Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Customer?> GetWithFullHistoryAsync(int id, CancellationToken ct = default);
}
