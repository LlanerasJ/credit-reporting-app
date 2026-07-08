namespace CreditReporting.Api.Metro2.Records;

/// <summary>
/// Base segment (426 chars), one per reported account, followed on the same
/// line by optional appended segments (J1, K1). Field positions are a documented
/// synthetic approximation of the Metro 2 base segment; see docs/METRO2-FORMAT.md.
/// </summary>
public class Metro2BaseRecord
{
    public const int RecordLength = 426;

    // --- Record control -------------------------------------------------
    [Metro2Field(1, 4)] public string RecordDescriptorWord { get; set; } = "0426";
    [Metro2Field(5, 1)] public string ProcessingIndicator { get; set; } = "1";
    [Metro2Field(6, 14)] public string TimeStamp { get; set; } = "";
    [Metro2Field(20, 1)] public string CorrectionIndicator { get; set; } = "0";
    [Metro2Field(21, 20)] public string IdentificationNumber { get; set; } = "";
    [Metro2Field(41, 2)] public string CycleIdentifier { get; set; } = "";

    // --- Account --------------------------------------------------------
    [Metro2Field(43, 30)] public string ConsumerAccountNumber { get; set; } = "";
    [Metro2Field(73, 1)] public string PortfolioType { get; set; } = "";
    [Metro2Field(74, 2)] public string AccountType { get; set; } = "";
    [Metro2Field(76, 8, Metro2FieldType.Date)] public DateTime? DateOpened { get; set; }
    [Metro2Field(84, 9, Metro2FieldType.Numeric)] public decimal CreditLimit { get; set; }
    [Metro2Field(93, 9, Metro2FieldType.Numeric)] public decimal HighestCredit { get; set; }
    [Metro2Field(102, 3)] public string TermsDuration { get; set; } = "";
    [Metro2Field(105, 1)] public string TermsFrequency { get; set; } = "M";
    [Metro2Field(106, 9, Metro2FieldType.Numeric)] public decimal ScheduledMonthlyPayment { get; set; }
    [Metro2Field(115, 9, Metro2FieldType.Numeric)] public decimal ActualPayment { get; set; }
    [Metro2Field(124, 2)] public string AccountStatus { get; set; } = "";
    [Metro2Field(126, 1)] public string PaymentRating { get; set; } = "";
    [Metro2Field(127, 24)] public string PaymentHistoryProfile { get; set; } = "";
    [Metro2Field(151, 2)] public string SpecialComment { get; set; } = "";
    [Metro2Field(153, 2)] public string ComplianceConditionCode { get; set; } = "";
    [Metro2Field(155, 9, Metro2FieldType.Numeric)] public decimal CurrentBalance { get; set; }
    [Metro2Field(164, 9, Metro2FieldType.Numeric)] public decimal AmountPastDue { get; set; }
    [Metro2Field(173, 9, Metro2FieldType.Numeric)] public decimal OriginalChargeOffAmount { get; set; }
    [Metro2Field(182, 8, Metro2FieldType.Date)] public DateTime? DateAccountInformation { get; set; }
    [Metro2Field(190, 8, Metro2FieldType.Date)] public DateTime? DateFirstDelinquency { get; set; }
    [Metro2Field(198, 8, Metro2FieldType.Date)] public DateTime? DateClosed { get; set; }
    [Metro2Field(206, 8, Metro2FieldType.Date)] public DateTime? DateLastPayment { get; set; }
    [Metro2Field(214, 1)] public string InterestTypeIndicator { get; set; } = "";
    // 215-230 reserved

    // --- Consumer -------------------------------------------------------
    [Metro2Field(231, 25)] public string Surname { get; set; } = "";
    [Metro2Field(256, 20)] public string FirstName { get; set; } = "";
    [Metro2Field(276, 20)] public string MiddleName { get; set; } = "";
    [Metro2Field(296, 1)] public string GenerationCode { get; set; } = "";
    [Metro2Field(297, 9)] public string SocialSecurityNumber { get; set; } = "";
    [Metro2Field(306, 8, Metro2FieldType.Date)] public DateTime? DateOfBirth { get; set; }
    [Metro2Field(314, 10)] public string TelephoneNumber { get; set; } = "";
    [Metro2Field(324, 1)] public string EcoaCode { get; set; } = "";
    [Metro2Field(325, 2)] public string ConsumerInformationIndicator { get; set; } = "";

    // --- Address --------------------------------------------------------
    [Metro2Field(327, 2)] public string CountryCode { get; set; } = "US";
    [Metro2Field(329, 32)] public string AddressLine1 { get; set; } = "";
    [Metro2Field(361, 32)] public string AddressLine2 { get; set; } = "";
    [Metro2Field(393, 20)] public string City { get; set; } = "";
    [Metro2Field(413, 2)] public string State { get; set; } = "";
    [Metro2Field(415, 9)] public string PostalCode { get; set; } = "";
    [Metro2Field(424, 1)] public string AddressIndicator { get; set; } = "";
    [Metro2Field(425, 1)] public string ResidenceCode { get; set; } = "";
    // 426 reserved

    /// <summary>Optional appended segments written after position 426 on the same line.</summary>
    public Metro2J1Segment? J1 { get; set; }
    public Metro2K1Segment? K1 { get; set; }
}

/// <summary>J1 appended segment (100 chars): associated consumer, same address.</summary>
public class Metro2J1Segment
{
    public const int SegmentLength = 100;
    public const string Identifier = "J1";

    [Metro2Field(1, 2)] public string SegmentIdentifier { get; set; } = Identifier;
    [Metro2Field(3, 25)] public string Surname { get; set; } = "";
    [Metro2Field(28, 20)] public string FirstName { get; set; } = "";
    [Metro2Field(48, 20)] public string MiddleName { get; set; } = "";
    [Metro2Field(68, 1)] public string GenerationCode { get; set; } = "";
    [Metro2Field(69, 9)] public string SocialSecurityNumber { get; set; } = "";
    [Metro2Field(78, 8, Metro2FieldType.Date)] public DateTime? DateOfBirth { get; set; }
    [Metro2Field(86, 10)] public string TelephoneNumber { get; set; } = "";
    [Metro2Field(96, 1)] public string EcoaCode { get; set; } = "";
    [Metro2Field(97, 2)] public string ConsumerInformationIndicator { get; set; } = "";
    // 99-100 reserved
}

/// <summary>K1 appended segment (34 chars): original creditor.</summary>
public class Metro2K1Segment
{
    public const int SegmentLength = 34;
    public const string Identifier = "K1";

    [Metro2Field(1, 2)] public string SegmentIdentifier { get; set; } = Identifier;
    [Metro2Field(3, 30)] public string OriginalCreditorName { get; set; } = "";
    [Metro2Field(33, 2)] public string CreditorClassification { get; set; } = "";
}
