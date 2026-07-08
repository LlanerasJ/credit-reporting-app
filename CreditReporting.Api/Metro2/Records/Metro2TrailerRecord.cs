namespace CreditReporting.Api.Metro2.Records;

/// <summary>File trailer record (426 chars) with control totals.</summary>
public class Metro2TrailerRecord
{
    public const int RecordLength = 426;
    public const string Identifier = "TRAILER";

    [Metro2Field(1, 4)] public string RecordDescriptorWord { get; set; } = "0426";
    [Metro2Field(5, 7)] public string RecordIdentifier { get; set; } = Identifier;
    [Metro2Field(12, 9, Metro2FieldType.Numeric)] public decimal TotalBaseRecords { get; set; }
    [Metro2Field(21, 9, Metro2FieldType.Numeric)] public decimal TotalJ1Segments { get; set; }
    [Metro2Field(30, 9, Metro2FieldType.Numeric)] public decimal TotalK1Segments { get; set; }
    [Metro2Field(39, 9, Metro2FieldType.Numeric)] public decimal TotalSocialSecurityNumbers { get; set; }
    [Metro2Field(48, 9, Metro2FieldType.Numeric)] public decimal TotalDatesOfBirth { get; set; }
    [Metro2Field(57, 9, Metro2FieldType.Numeric)] public decimal TotalTelephoneNumbers { get; set; }
    // 66-426 reserved
}
