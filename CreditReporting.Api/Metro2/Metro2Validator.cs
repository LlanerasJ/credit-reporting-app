using CreditReporting.Api.Metro2.Records;
using CreditReporting.Shared.Dtos;

namespace CreditReporting.Api.Metro2;

public interface IMetro2Validator
{
    List<Metro2ValidationIssueDto> Validate(Metro2File file);
}

/// <summary>
/// Field-level validation for Metro 2-style records: required fields, valid code
/// ranges, date sanity. Runs before generation and after parsing.
/// </summary>
public class Metro2Validator : IMetro2Validator
{
    private static readonly HashSet<string> PortfolioTypes = new() { "C", "I", "M", "O", "R" };
    private static readonly HashSet<string> AccountTypes = new()
        { "00", "01", "07", "12", "15", "18", "26" };
    private static readonly HashSet<string> AccountStatuses = new()
        { "11", "13", "61", "62", "63", "64", "65", "71", "78", "80", "82", "83", "84", "93", "97" };
    private static readonly HashSet<char> PaymentRatings = new()
        { '0', '1', '2', '3', '4', '5', '6', 'G', 'L' };
    private static readonly HashSet<char> HistoryChars = new()
        { '0', '1', '2', '3', '4', '5', '6', 'B', 'D', 'E', 'G', 'H', 'J', 'K', 'L' };
    private static readonly HashSet<string> EcoaCodes = new()
        { "1", "2", "3", "5", "7", "T", "W", "X", "Z" };

    public List<Metro2ValidationIssueDto> Validate(Metro2File file)
    {
        var issues = new List<Metro2ValidationIssueDto>();

        if (string.IsNullOrWhiteSpace(file.Header.ReporterName))
            AddError(issues, 0, "", nameof(file.Header.ReporterName), "Header reporter name is required.");
        if (file.Header.ActivityDate is null)
            AddError(issues, 0, "", nameof(file.Header.ActivityDate), "Header activity date is required.");

        for (int i = 0; i < file.BaseRecords.Count; i++)
            ValidateBaseRecord(file.BaseRecords[i], i + 1, issues);

        return issues;
    }

    private void ValidateBaseRecord(Metro2BaseRecord r, int recordNumber, List<Metro2ValidationIssueDto> issues)
    {
        string acct = MaskAccount(r.ConsumerAccountNumber);

        void Error(string field, string message) => AddError(issues, recordNumber, acct, field, message);
        void Warn(string field, string message) => AddWarning(issues, recordNumber, acct, field, message);

        // Required fields
        if (string.IsNullOrWhiteSpace(r.ConsumerAccountNumber))
            Error(nameof(r.ConsumerAccountNumber), "Consumer account number is required.");
        if (string.IsNullOrWhiteSpace(r.Surname))
            Error(nameof(r.Surname), "Consumer surname is required.");
        if (string.IsNullOrWhiteSpace(r.FirstName))
            Error(nameof(r.FirstName), "Consumer first name is required.");
        if (r.DateOpened is null)
            Error(nameof(r.DateOpened), "Date opened is required.");

        // Code ranges
        if (!PortfolioTypes.Contains(r.PortfolioType))
            Error(nameof(r.PortfolioType), $"Portfolio type '{r.PortfolioType}' is not one of C/I/M/O/R.");
        if (!AccountTypes.Contains(r.AccountType))
            Error(nameof(r.AccountType), $"Account type '{r.AccountType}' is not a recognized code.");
        if (!AccountStatuses.Contains(r.AccountStatus))
            Error(nameof(r.AccountStatus), $"Account status '{r.AccountStatus}' is not a recognized code.");
        if (r.PaymentRating.Length != 1 || !PaymentRatings.Contains(r.PaymentRating[0]))
            Error(nameof(r.PaymentRating), $"Payment rating '{r.PaymentRating}' must be 0-6, G, or L.");
        if (!EcoaCodes.Contains(r.EcoaCode))
            Error(nameof(r.EcoaCode), $"ECOA code '{r.EcoaCode}' is not a recognized code.");

        // Payment history profile
        if (r.PaymentHistoryProfile.Length != 24)
            Error(nameof(r.PaymentHistoryProfile),
                $"Payment history profile must be exactly 24 characters (got {r.PaymentHistoryProfile.Length}).");
        else if (r.PaymentHistoryProfile.Any(c => !HistoryChars.Contains(c)))
            Error(nameof(r.PaymentHistoryProfile),
                "Payment history profile contains characters outside 0-6/B/D/E/G/H/J/K/L.");

        // Date sanity
        if (r.DateOpened is { } opened && opened > DateTime.Today)
            Error(nameof(r.DateOpened), "Date opened cannot be in the future.");
        if (r.DateClosed is { } closed && r.DateOpened is { } opened2 && closed < opened2)
            Error(nameof(r.DateClosed), "Date closed precedes date opened.");
        if (r.DateOfBirth is { } dob && dob > DateTime.Today.AddYears(-18))
            Warn(nameof(r.DateOfBirth), "Consumer appears to be under 18.");

        // Identifier warnings
        if (r.SocialSecurityNumber.Length != 9 || !r.SocialSecurityNumber.All(char.IsDigit))
            Warn(nameof(r.SocialSecurityNumber), "SSN is missing or not 9 digits.");
        if (r.DateOfBirth is null)
            Warn(nameof(r.DateOfBirth), "Date of birth is missing.");

        // Status/amount consistency
        if (r.AccountStatus == "97" && r.OriginalChargeOffAmount <= 0)
            Warn(nameof(r.OriginalChargeOffAmount),
                "Charge-off status (97) reported without an original charge-off amount.");
        if (r.AmountPastDue > 0 && r.AccountStatus == "11")
            Warn(nameof(r.AmountPastDue),
                "Amount past due reported on a current (11) account.");
        if (r.CurrentBalance > 0 && r.AccountStatus == "13")
            Warn(nameof(r.CurrentBalance),
                "Paid/closed status (13) reported with a non-zero balance.");
    }

    private static void AddError(List<Metro2ValidationIssueDto> issues, int recordNumber, string acct, string field, string message) =>
        issues.Add(new Metro2ValidationIssueDto
        {
            RecordNumber = recordNumber,
            AccountNumberMasked = acct,
            FieldName = field,
            Message = message,
            Severity = "Error"
        });

    private static void AddWarning(List<Metro2ValidationIssueDto> issues, int recordNumber, string acct, string field, string message) =>
        issues.Add(new Metro2ValidationIssueDto
        {
            RecordNumber = recordNumber,
            AccountNumberMasked = acct,
            FieldName = field,
            Message = message,
            Severity = "Warning"
        });

    private static string MaskAccount(string accountNumber)
    {
        string trimmed = accountNumber.Trim();
        return trimmed.Length <= 4 ? new string('*', trimmed.Length) : $"****{trimmed[^4..]}";
    }
}
