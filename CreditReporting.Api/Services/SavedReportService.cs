using System.Text.Json;
using CreditReporting.Api.Data;
using CreditReporting.Api.Data.Entities;
using CreditReporting.Api.Reports;
using CreditReporting.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CreditReporting.Api.Services;

public enum SavedReportStatus { Ok, Invalid, NotFound, Forbidden }

/// <summary>Outcome of a saved-report operation; Error is display-ready when not Ok.</summary>
public record SavedReportResult(SavedReportStatus Status, SavedReportDto? Report = null, string? Error = null)
{
    public static SavedReportResult Ok(SavedReportDto? report = null) => new(SavedReportStatus.Ok, report);
    public static SavedReportResult Invalid(string error) => new(SavedReportStatus.Invalid, Error: error);
    public static SavedReportResult NotFound(int id) => new(SavedReportStatus.NotFound, Error: $"Saved report {id} not found.");
    public static SavedReportResult Forbidden(string error) => new(SavedReportStatus.Forbidden, Error: error);
}

public interface ISavedReportService
{
    /// <summary>The caller's own reports plus everything shared, shared-first then by name.</summary>
    Task<List<SavedReportDto>> GetVisibleAsync(string username, CancellationToken ct = default);
    Task<SavedReportResult> CreateAsync(string username, bool isAdmin, SaveReportRequest request, CancellationToken ct = default);
    Task<SavedReportResult> UpdateAsync(int id, string username, bool isAdmin, SaveReportRequest request, CancellationToken ct = default);
    Task<SavedReportResult> DeleteAsync(int id, string username, bool isAdmin, CancellationToken ct = default);
}

/// <summary>
/// CRUD for saved reports. Rules: everyone saves private reports; only admins
/// may mark a report shared; only the owner (or an admin) may edit or delete.
/// Saves are validated against the catalog so a saved report always runs.
/// </summary>
public class SavedReportService : ISavedReportService
{
    private readonly AppDbContext _db;
    private readonly IReportCatalog _catalog;

    public SavedReportService(AppDbContext db, IReportCatalog catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    public async Task<List<SavedReportDto>> GetVisibleAsync(string username, CancellationToken ct = default)
    {
        var reports = await _db.SavedReports.AsNoTracking()
            .Where(r => r.OwnerUsername == username || r.IsShared)
            .OrderByDescending(r => r.IsShared).ThenBy(r => r.Name)
            .ToListAsync(ct);
        return reports.Select(ToDto).ToList();
    }

    public async Task<SavedReportResult> CreateAsync(
        string username, bool isAdmin, SaveReportRequest request, CancellationToken ct = default)
    {
        string? error = Validate(request, isAdmin);
        if (error is not null) return SavedReportResult.Invalid(error);

        var now = DateTime.UtcNow;
        var report = new SavedReport
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            ReportType = request.ReportType,
            ParametersJson = JsonSerializer.Serialize(request.Parameters),
            OwnerUsername = username,
            IsShared = request.IsShared,
            CreatedUtc = now,
            ModifiedUtc = now
        };
        _db.SavedReports.Add(report);
        await _db.SaveChangesAsync(ct);
        return SavedReportResult.Ok(ToDto(report));
    }

    public async Task<SavedReportResult> UpdateAsync(
        int id, string username, bool isAdmin, SaveReportRequest request, CancellationToken ct = default)
    {
        var report = await _db.SavedReports.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (report is null) return SavedReportResult.NotFound(id);
        if (report.OwnerUsername != username && !isAdmin)
            return SavedReportResult.Forbidden("Only the owner or an admin can modify this report.");

        string? error = Validate(request, isAdmin || report.IsShared == request.IsShared);
        if (error is not null) return SavedReportResult.Invalid(error);

        report.Name = request.Name.Trim();
        report.Description = request.Description.Trim();
        report.ReportType = request.ReportType;
        report.ParametersJson = JsonSerializer.Serialize(request.Parameters);
        report.IsShared = request.IsShared;
        report.ModifiedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return SavedReportResult.Ok(ToDto(report));
    }

    public async Task<SavedReportResult> DeleteAsync(
        int id, string username, bool isAdmin, CancellationToken ct = default)
    {
        var report = await _db.SavedReports.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (report is null) return SavedReportResult.NotFound(id);
        if (report.OwnerUsername != username && !isAdmin)
            return SavedReportResult.Forbidden("Only the owner or an admin can delete this report.");

        _db.SavedReports.Remove(report);
        await _db.SaveChangesAsync(ct);
        return SavedReportResult.Ok();
    }

    /// <summary>Null when valid. canShare covers "admin" and "sharing flag unchanged".</summary>
    private string? Validate(SaveReportRequest request, bool canShare)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return "A report name is required.";
        if (request.IsShared && !canShare)
            return "Only admins can share a report with other users.";

        var definition = _catalog.Find(request.ReportType);
        if (definition is null)
            return $"Unknown report type '{request.ReportType}'.";

        ReportArgs.Bind(definition.Parameters, request.Parameters, out var errors);
        return errors.Count == 0 ? null : string.Join(" ", errors);
    }

    private static SavedReportDto ToDto(SavedReport report) => new()
    {
        Id = report.Id,
        Name = report.Name,
        Description = report.Description,
        ReportType = report.ReportType,
        Parameters = ParseParameters(report.ParametersJson),
        OwnerUsername = report.OwnerUsername,
        IsShared = report.IsShared,
        CreatedUtc = report.CreatedUtc,
        ModifiedUtc = report.ModifiedUtc
    };

    private static Dictionary<string, string?> ParseParameters(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(json) ?? new();
        }
        catch (JsonException)
        {
            return new();
        }
    }
}
