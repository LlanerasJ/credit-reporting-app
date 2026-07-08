using CreditReporting.Api.Data;
using CreditReporting.Api.Data.Entities;

namespace CreditReporting.Api.Services;

public interface IAuditService
{
    Task LogAsync(string username, int customerId, string action, string purpose, string sourceIp, CancellationToken ct = default);
}

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    public AuditService(AppDbContext db) => _db = db;

    public async Task LogAsync(string username, int customerId, string action, string purpose, string sourceIp, CancellationToken ct = default)
    {
        _db.AuditLog.Add(new AuditLogEntry
        {
            TimestampUtc = DateTime.UtcNow,
            Username = username,
            CustomerId = customerId,
            Action = action,
            Purpose = purpose,
            SourceIp = sourceIp
        });
        await _db.SaveChangesAsync(ct);
    }
}
