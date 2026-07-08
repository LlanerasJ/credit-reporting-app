using System.Text;
using CreditReporting.Api.Metro2.Records;

namespace CreditReporting.Api.Metro2;

public interface IMetro2Writer
{
    string Write(Metro2File file);
}

/// <summary>Represents a complete Metro 2-style file ready to be serialized or validated.</summary>
public class Metro2File
{
    public Metro2HeaderRecord Header { get; set; } = new();
    public List<Metro2BaseRecord> BaseRecords { get; set; } = new();
    public Metro2TrailerRecord Trailer { get; set; } = new();
}

/// <summary>
/// Serializes a <see cref="Metro2File"/> into fixed-width text: one record per
/// line: header, N base records (each with optional appended J1/K1 segments), trailer.
/// </summary>
public class Metro2Writer : IMetro2Writer
{
    public string Write(Metro2File file)
    {
        RecomputeTrailerTotals(file);

        var sb = new StringBuilder();
        sb.AppendLine(Metro2FixedWidth.Serialize(file.Header, Metro2HeaderRecord.RecordLength));

        foreach (var record in file.BaseRecords)
        {
            sb.Append(Metro2FixedWidth.Serialize(record, Metro2BaseRecord.RecordLength));
            if (record.J1 is not null)
                sb.Append(Metro2FixedWidth.Serialize(record.J1, Metro2J1Segment.SegmentLength));
            if (record.K1 is not null)
                sb.Append(Metro2FixedWidth.Serialize(record.K1, Metro2K1Segment.SegmentLength));
            sb.AppendLine();
        }

        sb.AppendLine(Metro2FixedWidth.Serialize(file.Trailer, Metro2TrailerRecord.RecordLength));
        return sb.ToString();
    }

    private static void RecomputeTrailerTotals(Metro2File file)
    {
        file.Trailer.TotalBaseRecords = file.BaseRecords.Count;
        file.Trailer.TotalJ1Segments = file.BaseRecords.Count(r => r.J1 is not null);
        file.Trailer.TotalK1Segments = file.BaseRecords.Count(r => r.K1 is not null);
        file.Trailer.TotalSocialSecurityNumbers =
            file.BaseRecords.Count(r => !string.IsNullOrWhiteSpace(r.SocialSecurityNumber));
        file.Trailer.TotalDatesOfBirth = file.BaseRecords.Count(r => r.DateOfBirth is not null);
        file.Trailer.TotalTelephoneNumbers =
            file.BaseRecords.Count(r => !string.IsNullOrWhiteSpace(r.TelephoneNumber));
    }
}
