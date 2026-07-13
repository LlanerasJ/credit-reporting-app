using System.Globalization;

namespace CreditReporting.Api.Reports;

/// <summary>
/// Cell formatting shared by report definitions so every report emits the same
/// invariant-culture shapes the client expects (see ReportResultDto).
/// </summary>
public static class ReportFormat
{
    public static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    public static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    public static string Date(DateTime? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
    public static string Timestamp(DateTime value) => value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
}
