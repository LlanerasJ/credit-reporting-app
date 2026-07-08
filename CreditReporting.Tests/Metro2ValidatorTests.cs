using CreditReporting.Api.Metro2;
using CreditReporting.Api.Metro2.Records;

namespace CreditReporting.Tests;

public class Metro2ValidatorTests
{
    private static Metro2File ValidFile()
    {
        return new Metro2File
        {
            Header = new Metro2HeaderRecord
            {
                ReporterName = "Demo Data Furnisher Inc",
                ActivityDate = new DateTime(2026, 6, 30)
            },
            BaseRecords =
            {
                new Metro2BaseRecord
                {
                    ConsumerAccountNumber = "4001123456789",
                    PortfolioType = "R",
                    AccountType = "18",
                    DateOpened = new DateTime(2020, 5, 1),
                    AccountStatus = "11",
                    PaymentRating = "0",
                    PaymentHistoryProfile = new string('0', 24),
                    Surname = "Testman",
                    FirstName = "Ava",
                    SocialSecurityNumber = "900001234",
                    DateOfBirth = new DateTime(1980, 3, 15),
                    EcoaCode = "1"
                }
            }
        };
    }

    [Fact]
    public void Valid_file_has_no_errors()
    {
        var issues = new Metro2Validator().Validate(ValidFile());
        Assert.DoesNotContain(issues, i => i.Severity == "Error");
    }

    [Theory]
    [InlineData("ConsumerAccountNumber", "")]
    [InlineData("Surname", "")]
    [InlineData("FirstName", "")]
    public void Missing_required_field_is_an_error(string field, string value)
    {
        var file = ValidFile();
        var record = file.BaseRecords[0];
        typeof(Metro2BaseRecord).GetProperty(field)!.SetValue(record, value);

        var issues = new Metro2Validator().Validate(file);
        Assert.Contains(issues, i => i.Severity == "Error" && i.FieldName == field);
    }

    [Fact]
    public void Invalid_codes_are_errors()
    {
        var file = ValidFile();
        var record = file.BaseRecords[0];
        record.PortfolioType = "Q";
        record.AccountType = "99";
        record.AccountStatus = "42";
        record.PaymentRating = "9";
        record.EcoaCode = "8";

        var issues = new Metro2Validator().Validate(file);
        var errorFields = issues.Where(i => i.Severity == "Error").Select(i => i.FieldName).ToList();

        Assert.Contains("PortfolioType", errorFields);
        Assert.Contains("AccountType", errorFields);
        Assert.Contains("AccountStatus", errorFields);
        Assert.Contains("PaymentRating", errorFields);
        Assert.Contains("EcoaCode", errorFields);
    }

    [Fact]
    public void Date_closed_before_date_opened_is_an_error()
    {
        var file = ValidFile();
        file.BaseRecords[0].DateClosed = new DateTime(2019, 1, 1);

        var issues = new Metro2Validator().Validate(file);
        Assert.Contains(issues, i => i.Severity == "Error" && i.FieldName == "DateClosed");
    }

    [Fact]
    public void Missing_ssn_is_a_warning_not_an_error()
    {
        var file = ValidFile();
        file.BaseRecords[0].SocialSecurityNumber = "";

        var issues = new Metro2Validator().Validate(file);
        var ssnIssue = Assert.Single(issues, i => i.FieldName == "SocialSecurityNumber");
        Assert.Equal("Warning", ssnIssue.Severity);
    }

    [Fact]
    public void Chargeoff_without_amount_is_a_warning()
    {
        var file = ValidFile();
        file.BaseRecords[0].AccountStatus = "97";
        file.BaseRecords[0].OriginalChargeOffAmount = 0;

        var issues = new Metro2Validator().Validate(file);
        Assert.Contains(issues, i => i.Severity == "Warning" && i.FieldName == "OriginalChargeOffAmount");
    }
}
