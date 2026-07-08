namespace CreditReporting.Api.Metro2.Records;

/// <summary>
/// File header record (426 chars). Field positions are a documented synthetic
/// approximation of the Metro 2 header; see docs/METRO2-FORMAT.md.
/// </summary>
public class Metro2HeaderRecord
{
    public const int RecordLength = 426;
    public const string Identifier = "HEADER";

    [Metro2Field(1, 4)] public string RecordDescriptorWord { get; set; } = "0426";
    [Metro2Field(5, 6)] public string RecordIdentifier { get; set; } = Identifier;
    [Metro2Field(11, 2)] public string CycleIdentifier { get; set; } = "";
    [Metro2Field(13, 10)] public string ProgramIdentifier { get; set; } = "";
    [Metro2Field(23, 8, Metro2FieldType.Date)] public DateTime? ActivityDate { get; set; }
    [Metro2Field(31, 8, Metro2FieldType.Date)] public DateTime? DateCreated { get; set; }
    [Metro2Field(39, 8, Metro2FieldType.Date)] public DateTime? ProgramDate { get; set; }
    [Metro2Field(47, 8, Metro2FieldType.Date)] public DateTime? ProgramRevisionDate { get; set; }
    [Metro2Field(55, 40)] public string ReporterName { get; set; } = "";
    [Metro2Field(95, 96)] public string ReporterAddress { get; set; } = "";
    [Metro2Field(191, 10)] public string ReporterTelephone { get; set; } = "";
    [Metro2Field(201, 40)] public string SoftwareVendorName { get; set; } = "";
    [Metro2Field(241, 5)] public string SoftwareVersion { get; set; } = "";
    // 246-426 reserved (space filler)
}
