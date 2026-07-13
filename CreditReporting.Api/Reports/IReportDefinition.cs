using CreditReporting.Shared.Dtos;

namespace CreditReporting.Api.Reports;

/// <summary>
/// One report type in the catalog. Implementations declare their parameters
/// and run a reviewed, masked query; the client renders parameter inputs from
/// the descriptors, so adding a report is one server-side class.
/// </summary>
public interface IReportDefinition
{
    /// <summary>Stable catalog key, e.g. "delinquent-accounts".</summary>
    string Key { get; }
    string DisplayName { get; }
    string Description { get; }
    IReadOnlyList<ReportParameterDto> Parameters { get; }

    /// <summary>Runs the report. Args have already been validated against <see cref="Parameters"/>.</summary>
    Task<ReportResultDto> ExecuteAsync(ReportArgs args, CancellationToken ct = default);
}
