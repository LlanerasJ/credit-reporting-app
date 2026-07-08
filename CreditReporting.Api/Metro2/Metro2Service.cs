using CreditReporting.Api.Data.Entities;
using CreditReporting.Api.Metro2.Records;
using CreditReporting.Api.Repositories;
using CreditReporting.Api.Services;
using CreditReporting.Shared.Dtos;

namespace CreditReporting.Api.Metro2;

public interface IMetro2Service
{
    Task<(Metro2File File, List<Metro2ValidationIssueDto> Issues)> BuildFileAsync(Metro2GenerateRequest request, CancellationToken ct = default);
    string Serialize(Metro2File file);
    Metro2ParseResponseDto ParseAndValidate(string content);
}

/// <summary>
/// Orchestrates Metro 2 work: maps Account/Customer/PaymentHistory entities into
/// Metro 2 records, validates them, serializes files, and parses uploads.
/// </summary>
public class Metro2Service : IMetro2Service
{
    private const string FurnisherId = "DEMOFURN0001";
    private const string ReporterName = "Demo Data Furnisher Inc";

    private readonly IAccountRepository _accounts;
    private readonly IMetro2Writer _writer;
    private readonly IMetro2Parser _parser;
    private readonly IMetro2Validator _validator;

    public Metro2Service(IAccountRepository accounts, IMetro2Writer writer, IMetro2Parser parser, IMetro2Validator validator)
    {
        _accounts = accounts;
        _writer = writer;
        _parser = parser;
        _validator = validator;
    }

    public async Task<(Metro2File, List<Metro2ValidationIssueDto>)> BuildFileAsync(
        Metro2GenerateRequest request, CancellationToken ct = default)
    {
        DateTime to = request.ToDate ?? DateTime.Today;
        DateTime from = request.FromDate ?? to.AddMonths(-1);

        var accounts = await _accounts.GetForReportingWindowAsync(from, to, request.AccountIds, ct);

        var file = new Metro2File
        {
            Header = new Metro2HeaderRecord
            {
                CycleIdentifier = to.ToString("MM"),
                ProgramIdentifier = FurnisherId[..Math.Min(10, FurnisherId.Length)],
                ActivityDate = to,
                DateCreated = DateTime.Today,
                ProgramDate = new DateTime(2026, 1, 1),
                ProgramRevisionDate = new DateTime(2026, 6, 1),
                ReporterName = ReporterName,
                ReporterAddress = "100 Demo Plaza, Springfield, IL 62701",
                ReporterTelephone = "5550100200",
                SoftwareVendorName = "CreditReporting Portfolio App",
                SoftwareVersion = "1.0"
            },
            BaseRecords = accounts.Select(a => MapAccount(a, to)).ToList()
        };

        return (file, _validator.Validate(file));
    }

    public string Serialize(Metro2File file) => _writer.Write(file);

    public Metro2ParseResponseDto ParseAndValidate(string content)
    {
        var parsed = _parser.Parse(content);
        var issues = _validator.Validate(parsed.File);

        issues.InsertRange(0, parsed.StructuralErrors.Select(e => new Metro2ValidationIssueDto
        {
            RecordNumber = 0,
            FieldName = "(structure)",
            Message = e,
            Severity = "Error"
        }));

        var response = new Metro2ParseResponseDto
        {
            Header = new Metro2HeaderDto
            {
                CycleIdentifier = parsed.File.Header.CycleIdentifier,
                ProgramIdentifier = parsed.File.Header.ProgramIdentifier,
                ActivityDate = parsed.File.Header.ActivityDate,
                CreatedDate = parsed.File.Header.DateCreated,
                ReporterName = parsed.File.Header.ReporterName
            },
            Trailer = new Metro2TrailerDto
            {
                TotalBaseRecords = (int)parsed.File.Trailer.TotalBaseRecords,
                TotalJ1Segments = (int)parsed.File.Trailer.TotalJ1Segments,
                TotalK1Segments = (int)parsed.File.Trailer.TotalK1Segments
            },
            Issues = issues
        };

        for (int i = 0; i < parsed.File.BaseRecords.Count; i++)
        {
            var r = parsed.File.BaseRecords[i];
            response.Records.Add(new Metro2ParsedRecordDto
            {
                RecordNumber = i + 1,
                ConsumerAccountNumber = Masking.MaskAccountNumber(r.ConsumerAccountNumber.Trim()),
                PortfolioType = r.PortfolioType,
                AccountType = r.AccountType,
                DateOpened = r.DateOpened,
                CreditLimit = r.CreditLimit,
                CurrentBalance = r.CurrentBalance,
                AmountPastDue = r.AmountPastDue,
                AccountStatus = r.AccountStatus,
                PaymentRating = r.PaymentRating,
                PaymentHistoryProfile = r.PaymentHistoryProfile,
                Surname = r.Surname,
                FirstName = r.FirstName,
                SsnMasked = r.SocialSecurityNumber.Length >= 4
                    ? Masking.MaskSsn(r.SocialSecurityNumber[^4..])
                    : "***-**-????",
                DateOfBirth = r.DateOfBirth,
                EcoaCode = r.EcoaCode,
                OriginalCreditor = r.K1?.OriginalCreditorName.Trim(),
                AssociatedConsumer = r.J1?.Surname.Trim()
            });
        }

        return response;
    }

    /// <summary>Maps one Account (+customer +history) to a Metro 2 base record with appended segments.</summary>
    private static Metro2BaseRecord MapAccount(Account account, DateTime activityDate)
    {
        var customer = account.Customer;
        var history = account.PaymentHistory.OrderByDescending(p => p.PaymentDate).ToList();
        var latest = history.FirstOrDefault();
        var firstDelinquency = history
            .Where(p => p.DaysLate > 0)
            .OrderBy(p => p.PaymentDate)
            .FirstOrDefault();

        var record = new Metro2BaseRecord
        {
            TimeStamp = activityDate.ToString("yyyyMMdd") + "000000",
            IdentificationNumber = FurnisherId,
            CycleIdentifier = activityDate.ToString("MM"),
            ConsumerAccountNumber = account.AccountNumber,
            PortfolioType = account.PortfolioType,
            AccountType = MapAccountType(account.AccountType),
            DateOpened = account.OpenDate,
            CreditLimit = account.CreditLimit,
            HighestCredit = history.Count > 0 ? history.Max(p => p.Balance) : account.CurrentBalance,
            TermsDuration = account.PortfolioType == "I" || account.PortfolioType == "M" ? "360" : "REV",
            TermsFrequency = "M",
            ScheduledMonthlyPayment = latest?.AmountPaid ?? 0,
            ActualPayment = latest?.AmountPaid ?? 0,
            AccountStatus = MapStatus(account, latest),
            PaymentRating = latest?.PaymentRating ?? "0",
            PaymentHistoryProfile = BuildProfile(history),
            CurrentBalance = account.CurrentBalance,
            AmountPastDue = account.AmountPastDue,
            OriginalChargeOffAmount = account.Status == AccountStatus.ChargeOff ? account.CurrentBalance : 0,
            DateAccountInformation = activityDate,
            DateFirstDelinquency = firstDelinquency?.PaymentDate,
            DateClosed = account.ClosedDate,
            DateLastPayment = history.FirstOrDefault(p => p.AmountPaid > 0)?.PaymentDate,
            Surname = customer.LastName,
            FirstName = customer.FirstName,
            // Synthetic 900-range SSN reconstructed for the demo file; not a real SSN.
            SocialSecurityNumber = $"90000{customer.SsnLast4}",
            DateOfBirth = customer.DateOfBirth,
            TelephoneNumber = customer.Phone,
            EcoaCode = account.EcoaCode,
            CountryCode = "US",
            AddressLine1 = customer.AddressLine1,
            City = customer.City,
            State = customer.State,
            PostalCode = customer.PostalCode
        };

        // Collections/charge-offs carry the original creditor in an appended K1 segment.
        if (account.Status is AccountStatus.Collection or AccountStatus.ChargeOff)
        {
            record.K1 = new Metro2K1Segment
            {
                OriginalCreditorName = account.CreditorName,
                CreditorClassification = "01"
            };
        }

        // Joint accounts (ECOA 2) get a J1 associated-consumer segment.
        if (account.EcoaCode == "2")
        {
            record.J1 = new Metro2J1Segment
            {
                Surname = customer.LastName,
                FirstName = "Codemo",
                SocialSecurityNumber = $"90099{customer.SsnLast4}",
                DateOfBirth = customer.DateOfBirth.AddYears(-2),
                TelephoneNumber = customer.Phone,
                EcoaCode = "2"
            };
        }

        return record;
    }

    private static string MapAccountType(AccountType type) => type switch
    {
        AccountType.AutoLoan => "00",
        AccountType.PersonalLoan => "01",
        AccountType.RetailCard => "07",
        AccountType.StudentLoan => "12",
        AccountType.LineOfCredit => "15",
        AccountType.CreditCard => "18",
        AccountType.Mortgage => "26",
        _ => "01"
    };

    private static string MapStatus(Account account, PaymentRecord? latest) => account.Status switch
    {
        AccountStatus.ChargeOff => "97",
        AccountStatus.Collection => "93",
        AccountStatus.PaidClosed or AccountStatus.Closed => "13",
        _ => (latest?.DaysLate ?? 0) switch
        {
            0 => "11",
            < 60 => "71",
            < 90 => "78",
            < 120 => "80",
            < 150 => "82",
            < 180 => "83",
            _ => "84"
        }
    };

    private static string BuildProfile(List<PaymentRecord> historyNewestFirst)
    {
        var chars = historyNewestFirst
            .Take(24)
            .Select(p => string.IsNullOrEmpty(p.PaymentRating) ? "0" : p.PaymentRating[..1]);
        return string.Concat(chars).PadRight(24, 'B');
    }
}
