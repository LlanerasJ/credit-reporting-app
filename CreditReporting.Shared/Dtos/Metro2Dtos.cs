namespace CreditReporting.Shared.Dtos;

/// <summary>Request for Metro 2 generation/preview: accounts active in the window.</summary>
public class Metro2GenerateRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    /// <summary>Optional explicit account population; null = all accounts in range.</summary>
    public List<int>? AccountIds { get; set; }
}

/// <summary>Returned by the preview endpoint before the file is actually generated.</summary>
public class Metro2PreviewDto
{
    public int RecordCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public List<Metro2ValidationIssueDto> Issues { get; set; } = new();
}

public class Metro2ValidationIssueDto
{
    /// <summary>1-based base-record index within the file ("0" = header/trailer).</summary>
    public int RecordNumber { get; set; }
    public string AccountNumberMasked { get; set; } = "";
    public string FieldName { get; set; } = "";
    public string Message { get; set; } = "";
    /// <summary>"Error" or "Warning".</summary>
    public string Severity { get; set; } = "";
}

public class Metro2ParseResponseDto
{
    public Metro2HeaderDto? Header { get; set; }
    public Metro2TrailerDto? Trailer { get; set; }
    public List<Metro2ParsedRecordDto> Records { get; set; } = new();
    public List<Metro2ValidationIssueDto> Issues { get; set; } = new();
}

public class Metro2HeaderDto
{
    public string CycleIdentifier { get; set; } = "";
    public string ProgramIdentifier { get; set; } = "";
    public DateTime? ActivityDate { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string ReporterName { get; set; } = "";
}

public class Metro2TrailerDto
{
    public int TotalBaseRecords { get; set; }
    public int TotalJ1Segments { get; set; }
    public int TotalK1Segments { get; set; }
}

/// <summary>Flattened view of one parsed base record (plus appended segments).</summary>
public class Metro2ParsedRecordDto
{
    public int RecordNumber { get; set; }
    public string ConsumerAccountNumber { get; set; } = "";
    public string PortfolioType { get; set; } = "";
    public string AccountType { get; set; } = "";
    public DateTime? DateOpened { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal AmountPastDue { get; set; }
    public string AccountStatus { get; set; } = "";
    public string PaymentRating { get; set; } = "";
    public string PaymentHistoryProfile { get; set; } = "";
    public string Surname { get; set; } = "";
    public string FirstName { get; set; } = "";
    /// <summary>Masked; raw SSN from the file is never returned by the API.</summary>
    public string SsnMasked { get; set; } = "";
    public DateTime? DateOfBirth { get; set; }
    public string EcoaCode { get; set; } = "";
    /// <summary>Original creditor name when a K1 segment is appended.</summary>
    public string? OriginalCreditor { get; set; }
    /// <summary>Associated consumer surname when a J1 segment is appended.</summary>
    public string? AssociatedConsumer { get; set; }
}
