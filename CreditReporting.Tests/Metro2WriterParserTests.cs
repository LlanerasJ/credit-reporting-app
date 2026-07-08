using CreditReporting.Api.Metro2;
using CreditReporting.Api.Metro2.Records;

namespace CreditReporting.Tests;

public class Metro2WriterParserTests
{
    private static Metro2File SampleFile()
    {
        return new Metro2File
        {
            Header = new Metro2HeaderRecord
            {
                CycleIdentifier = "07",
                ProgramIdentifier = "DEMOFURN00",
                ActivityDate = new DateTime(2026, 6, 30),
                DateCreated = new DateTime(2026, 7, 1),
                ReporterName = "Demo Data Furnisher Inc"
            },
            BaseRecords =
            {
                new Metro2BaseRecord
                {
                    TimeStamp = "20260630000000",
                    IdentificationNumber = "DEMOFURN0001",
                    CycleIdentifier = "07",
                    ConsumerAccountNumber = "4001123456789",
                    PortfolioType = "R",
                    AccountType = "18",
                    DateOpened = new DateTime(2020, 5, 1),
                    CreditLimit = 5000,
                    HighestCredit = 4200,
                    TermsFrequency = "M",
                    AccountStatus = "11",
                    PaymentRating = "0",
                    PaymentHistoryProfile = "000000000000111000000BBB",
                    CurrentBalance = 1234,
                    AmountPastDue = 0,
                    DateAccountInformation = new DateTime(2026, 6, 30),
                    Surname = "Testman",
                    FirstName = "Ava",
                    SocialSecurityNumber = "900001234",
                    DateOfBirth = new DateTime(1980, 3, 15),
                    TelephoneNumber = "5551234567",
                    EcoaCode = "1",
                    AddressLine1 = "101 Elm St",
                    City = "Springfield",
                    State = "IL",
                    PostalCode = "62701",
                    K1 = new Metro2K1Segment { OriginalCreditorName = "Acme Card Services", CreditorClassification = "01" },
                    J1 = new Metro2J1Segment { Surname = "Testman", FirstName = "Co", SocialSecurityNumber = "900991234", EcoaCode = "2" }
                }
            }
        };
    }

    [Fact]
    public void Writer_produces_fixed_length_records()
    {
        var text = new Metro2Writer().Write(SampleFile());
        var lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(3, lines.Length);
        Assert.Equal(Metro2HeaderRecord.RecordLength, lines[0].Length);
        // base 426 + J1 100 + K1 34
        Assert.Equal(
            Metro2BaseRecord.RecordLength + Metro2J1Segment.SegmentLength + Metro2K1Segment.SegmentLength,
            lines[1].Length);
        Assert.Equal(Metro2TrailerRecord.RecordLength, lines[2].Length);
    }

    [Fact]
    public void Writer_computes_trailer_totals()
    {
        var file = SampleFile();
        new Metro2Writer().Write(file);

        Assert.Equal(1, file.Trailer.TotalBaseRecords);
        Assert.Equal(1, file.Trailer.TotalJ1Segments);
        Assert.Equal(1, file.Trailer.TotalK1Segments);
        Assert.Equal(1, file.Trailer.TotalSocialSecurityNumbers);
    }

    [Fact]
    public void Roundtrip_preserves_field_values()
    {
        var original = SampleFile();
        string text = new Metro2Writer().Write(original);

        var result = new Metro2Parser().Parse(text);

        Assert.Empty(result.StructuralErrors);
        var record = Assert.Single(result.File.BaseRecords);
        Assert.Equal("4001123456789", record.ConsumerAccountNumber);
        Assert.Equal("R", record.PortfolioType);
        Assert.Equal("18", record.AccountType);
        Assert.Equal(new DateTime(2020, 5, 1), record.DateOpened);
        Assert.Equal(5000m, record.CreditLimit);
        Assert.Equal(1234m, record.CurrentBalance);
        Assert.Equal("Testman", record.Surname);
        Assert.Equal("900001234", record.SocialSecurityNumber);
        Assert.Equal("000000000000111000000BBB", record.PaymentHistoryProfile);
        Assert.NotNull(record.K1);
        Assert.Equal("Acme Card Services", record.K1!.OriginalCreditorName.Trim());
        Assert.NotNull(record.J1);
        Assert.Equal("900991234", record.J1!.SocialSecurityNumber);
        Assert.Equal("Demo Data Furnisher Inc", result.File.Header.ReporterName);
    }

    [Fact]
    public void Parser_reports_trailer_total_mismatch()
    {
        var file = SampleFile();
        string text = new Metro2Writer().Write(file);
        // Corrupt the trailer count: replace the serialized total (…000000001) with 9
        var lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines[^1] = lines[^1].Remove(11, 9).Insert(11, "000000009");

        var result = new Metro2Parser().Parse(string.Join(Environment.NewLine, lines));

        Assert.Contains(result.StructuralErrors, e => e.Contains("control total mismatch"));
    }

    [Fact]
    public void Parser_flags_empty_and_garbage_input()
    {
        Assert.Contains("File is empty.", new Metro2Parser().Parse("").StructuralErrors);

        var garbage = new Metro2Parser().Parse("this is not a metro 2 file\n");
        Assert.NotEmpty(garbage.StructuralErrors);
    }
}
