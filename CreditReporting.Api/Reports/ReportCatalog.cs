namespace CreditReporting.Api.Reports;

public interface IReportCatalog
{
    IReadOnlyList<IReportDefinition> All { get; }
    IReportDefinition? Find(string key);
}

/// <summary>The set of report types users can run; built from DI registrations.</summary>
public class ReportCatalog : IReportCatalog
{
    private readonly List<IReportDefinition> _definitions;

    public ReportCatalog(IEnumerable<IReportDefinition> definitions) =>
        _definitions = definitions.OrderBy(d => d.DisplayName).ToList();

    public IReadOnlyList<IReportDefinition> All => _definitions;

    public IReportDefinition? Find(string key) =>
        _definitions.FirstOrDefault(d => d.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
}
