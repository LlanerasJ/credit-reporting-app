using System.Globalization;
using CreditReporting.Shared.Dtos;

namespace CreditReporting.Api.Reports;

/// <summary>
/// Validated parameter values for one report run. <see cref="Bind"/> checks the
/// supplied values against the report's parameter descriptors (required,
/// parseable, allowed choice, no unknown names) so definitions can read typed
/// values without re-validating.
/// </summary>
public class ReportArgs
{
    private readonly Dictionary<string, string> _values;

    private ReportArgs(Dictionary<string, string> values) => _values = values;

    /// <summary>
    /// Merges supplied values with declared defaults and validates them.
    /// Returns null when there are errors; the errors are display-ready.
    /// </summary>
    public static ReportArgs? Bind(
        IReadOnlyList<ReportParameterDto> parameters,
        IReadOnlyDictionary<string, string?>? supplied,
        out List<string> errors)
    {
        errors = new List<string>();
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var known = parameters.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var name in (supplied ?? new Dictionary<string, string?>()).Keys)
            if (!known.Contains(name))
                errors.Add($"Unknown parameter '{name}'.");

        foreach (var p in parameters)
        {
            string? raw = null;
            if (supplied is not null && supplied.TryGetValue(p.Name, out var v))
                raw = v;
            raw = string.IsNullOrWhiteSpace(raw) ? p.DefaultValue : raw.Trim();

            if (string.IsNullOrWhiteSpace(raw))
            {
                if (p.Required) errors.Add($"Parameter '{p.Name}' is required.");
                continue;
            }

            bool valid = p.Type switch
            {
                "int" => int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                "decimal" => decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
                "date" => DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
                "choice" => p.Options?.Contains(raw, StringComparer.OrdinalIgnoreCase) == true,
                _ => true // "string"
            };

            if (!valid)
            {
                errors.Add(p.Type == "choice"
                    ? $"Parameter '{p.Name}' must be one of: {string.Join(", ", p.Options ?? new List<string>())}."
                    : $"Parameter '{p.Name}' must be a valid {p.Type}.");
                continue;
            }

            values[p.Name] = raw;
        }

        return errors.Count == 0 ? new ReportArgs(values) : null;
    }

    public string? GetString(string name) =>
        _values.TryGetValue(name, out var v) ? v : null;

    public int? GetInt(string name) =>
        _values.TryGetValue(name, out var v)
            ? int.Parse(v, NumberStyles.Integer, CultureInfo.InvariantCulture) : null;

    public decimal? GetDecimal(string name) =>
        _values.TryGetValue(name, out var v)
            ? decimal.Parse(v, NumberStyles.Number, CultureInfo.InvariantCulture) : null;

    public DateTime? GetDate(string name) =>
        _values.TryGetValue(name, out var v)
            ? DateTime.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.None) : null;

    /// <summary>Non-empty values, for audit logging (e.g. "state=TX, minPastDue=500").</summary>
    public string Describe() =>
        _values.Count == 0 ? "(no parameters)" : string.Join(", ", _values.Select(kv => $"{kv.Key}={kv.Value}"));
}
