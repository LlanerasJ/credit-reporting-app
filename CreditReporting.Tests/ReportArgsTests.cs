using CreditReporting.Api.Reports;
using CreditReporting.Shared.Dtos;

namespace CreditReporting.Tests;

public class ReportArgsTests
{
    private static readonly List<ReportParameterDto> Parameters = new()
    {
        new() { Name = "state", Type = "string", Required = false },
        new() { Name = "minPastDue", Type = "decimal", Required = false, DefaultValue = "0" },
        new() { Name = "from", Type = "date", Required = true },
        new() { Name = "bucket", Type = "choice", Required = false, Options = new List<string> { "30", "60", "90" } }
    };

    [Fact]
    public void Bind_applies_defaults_and_parses_values()
    {
        var args = ReportArgs.Bind(Parameters,
            new Dictionary<string, string?> { ["from"] = "2026-01-01", ["state"] = " TX " },
            out var errors);

        Assert.Empty(errors);
        Assert.NotNull(args);
        Assert.Equal("TX", args!.GetString("state"));
        Assert.Equal(0m, args.GetDecimal("minPastDue")); // from DefaultValue
        Assert.Equal(new DateTime(2026, 1, 1), args.GetDate("from"));
        Assert.Null(args.GetString("bucket"));
    }

    [Fact]
    public void Bind_reports_missing_required_parameter()
    {
        var args = ReportArgs.Bind(Parameters, new Dictionary<string, string?>(), out var errors);

        Assert.Null(args);
        Assert.Contains(errors, e => e.Contains("'from'") && e.Contains("required"));
    }

    [Fact]
    public void Bind_rejects_unparseable_and_unknown_values()
    {
        var args = ReportArgs.Bind(Parameters,
            new Dictionary<string, string?>
            {
                ["from"] = "not-a-date",
                ["minPastDue"] = "abc",
                ["bucket"] = "45",
                ["nope"] = "1"
            },
            out var errors);

        Assert.Null(args);
        Assert.Equal(4, errors.Count);
        Assert.Contains(errors, e => e.Contains("'from'"));
        Assert.Contains(errors, e => e.Contains("'minPastDue'"));
        Assert.Contains(errors, e => e.Contains("'bucket'"));
        Assert.Contains(errors, e => e.Contains("'nope'"));
    }

    [Fact]
    public void Bind_treats_blank_as_missing_and_ignores_optional()
    {
        var args = ReportArgs.Bind(Parameters,
            new Dictionary<string, string?> { ["from"] = "2026-01-01", ["state"] = "  " },
            out var errors);

        Assert.Empty(errors);
        Assert.Null(args!.GetString("state"));
    }
}
