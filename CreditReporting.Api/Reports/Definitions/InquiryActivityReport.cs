using CreditReporting.Api.Data;
using CreditReporting.Api.Data.Entities;
using CreditReporting.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CreditReporting.Api.Reports.Definitions;

/// <summary>
/// Credit inquiries pulled on customers, filtered by date range and hard/soft
/// type. Newest first, capped so a wide-open query stays displayable.
/// </summary>
public class InquiryActivityReport : IReportDefinition
{
    private const int MaxRows = 500;

    /// <summary>Canonical <see cref="CreditInquiry.InquiryType"/> values.</summary>
    private static readonly string[] InquiryTypes = { "Hard", "Soft" };

    private readonly AppDbContext _db;
    public InquiryActivityReport(AppDbContext db) => _db = db;

    public string Key => "inquiry-activity";
    public string DisplayName => "Inquiry Activity";
    public string Description =>
        $"Credit inquiries pulled on customers, filtered by date range and hard/soft type. Newest first, capped at {MaxRows} rows.";

    public IReadOnlyList<ReportParameterDto> Parameters { get; } = new List<ReportParameterDto>
    {
        new() { Name = "from", Label = "From date", Type = "date", Required = false },
        new() { Name = "to", Label = "To date (inclusive)", Type = "date", Required = false },
        new()
        {
            Name = "inquiryType", Label = "Inquiry type",
            Type = "choice", Required = false,
            Options = InquiryTypes.ToList()
        }
    };

    public async Task<ReportResultDto> ExecuteAsync(ReportArgs args, CancellationToken ct = default)
    {
        DateTime? from = args.GetDate("from");
        DateTime? to = args.GetDate("to");
        // The choice binding accepts any casing, so match back to the canonical value.
        string? inquiryType = InquiryTypes
            .FirstOrDefault(t => t.Equals(args.GetString("inquiryType"), StringComparison.OrdinalIgnoreCase));

        IQueryable<CreditInquiry> query = _db.CreditInquiries.AsNoTracking().Include(i => i.Customer);
        if (from is not null)
            query = query.Where(i => i.PulledDate >= from.Value.Date);
        if (to is not null)
            query = query.Where(i => i.PulledDate < to.Value.Date.AddDays(1));
        if (inquiryType is not null)
            query = query.Where(i => i.InquiryType == inquiryType);

        var inquiries = await query
            .OrderByDescending(i => i.PulledDate)
            .Take(MaxRows)
            .ToListAsync(ct);

        return new ReportResultDto
        {
            ReportType = Key,
            DisplayName = DisplayName,
            GeneratedAtUtc = DateTime.UtcNow,
            Columns = new List<ReportColumnDto>
            {
                new() { Name = "Customer", Type = "string" },
                new() { Name = "State", Type = "string" },
                new() { Name = "Inquiry Date", Type = "date" },
                new() { Name = "Type", Type = "string" },
                new() { Name = "Purpose", Type = "string" },
                new() { Name = "Requested By", Type = "string" }
            },
            Rows = inquiries.Select(i => new List<string>
            {
                $"{i.Customer.FirstName} {i.Customer.LastName}",
                i.Customer.State,
                ReportFormat.Date(i.PulledDate),
                i.InquiryType,
                i.Purpose,
                i.PulledBy
            }).ToList(),
            RowCount = inquiries.Count
        };
    }
}
