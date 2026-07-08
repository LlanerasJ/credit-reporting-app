using CreditReporting.Api.Metro2.Records;

namespace CreditReporting.Api.Metro2;

public interface IMetro2Parser
{
    Metro2ParseResult Parse(string content);
}

public class Metro2ParseResult
{
    public Metro2File File { get; } = new();
    public List<string> StructuralErrors { get; } = new();
}

/// <summary>
/// Reads a Metro 2-style fixed-width file back into records. Structural problems
/// (bad lengths, unknown record types, missing header/trailer) are collected
/// rather than thrown, so partially valid files can still be previewed.
/// </summary>
public class Metro2Parser : IMetro2Parser
{
    public Metro2ParseResult Parse(string content)
    {
        var result = new Metro2ParseResult();
        var lines = content.Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => l.Length > 0)
            .ToList();

        if (lines.Count == 0)
        {
            result.StructuralErrors.Add("File is empty.");
            return result;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            int lineNo = i + 1;

            if (line.Length < 5)
            {
                result.StructuralErrors.Add($"Line {lineNo}: too short to identify record type.");
                continue;
            }

            string identifier = line.Substring(4, Math.Min(7, line.Length - 4));
            if (identifier.StartsWith(Metro2HeaderRecord.Identifier))
            {
                if (line.Length != Metro2HeaderRecord.RecordLength)
                    result.StructuralErrors.Add(
                        $"Line {lineNo}: header record is {line.Length} chars, expected {Metro2HeaderRecord.RecordLength}.");
                result.File.Header = Metro2FixedWidth.Deserialize<Metro2HeaderRecord>(line);
            }
            else if (identifier.StartsWith(Metro2TrailerRecord.Identifier))
            {
                if (line.Length != Metro2TrailerRecord.RecordLength)
                    result.StructuralErrors.Add(
                        $"Line {lineNo}: trailer record is {line.Length} chars, expected {Metro2TrailerRecord.RecordLength}.");
                result.File.Trailer = Metro2FixedWidth.Deserialize<Metro2TrailerRecord>(line);
            }
            else if (line[4] == '1') // processing indicator of a base record
            {
                if (line.Length < Metro2BaseRecord.RecordLength)
                {
                    result.StructuralErrors.Add(
                        $"Line {lineNo}: base record is {line.Length} chars, expected at least {Metro2BaseRecord.RecordLength}.");
                    continue;
                }
                var record = Metro2FixedWidth.Deserialize<Metro2BaseRecord>(line);
                ParseAppendedSegments(line, lineNo, record, result);
                result.File.BaseRecords.Add(record);
            }
            else
            {
                result.StructuralErrors.Add($"Line {lineNo}: unrecognized record type.");
            }
        }

        ValidateControlTotals(result);
        return result;
    }

    private static void ParseAppendedSegments(string line, int lineNo, Metro2BaseRecord record, Metro2ParseResult result)
    {
        int pos = Metro2BaseRecord.RecordLength;
        while (pos < line.Length)
        {
            string remaining = line[pos..];
            if (remaining.Trim().Length == 0) break;

            if (remaining.StartsWith(Metro2J1Segment.Identifier) &&
                remaining.Length >= Metro2J1Segment.SegmentLength)
            {
                record.J1 = Metro2FixedWidth.Deserialize<Metro2J1Segment>(
                    remaining[..Metro2J1Segment.SegmentLength]);
                pos += Metro2J1Segment.SegmentLength;
            }
            else if (remaining.StartsWith(Metro2K1Segment.Identifier) &&
                     remaining.Length >= Metro2K1Segment.SegmentLength)
            {
                record.K1 = Metro2FixedWidth.Deserialize<Metro2K1Segment>(
                    remaining[..Metro2K1Segment.SegmentLength]);
                pos += Metro2K1Segment.SegmentLength;
            }
            else
            {
                result.StructuralErrors.Add(
                    $"Line {lineNo}: unrecognized or truncated appended segment at position {pos + 1}.");
                break;
            }
        }
    }

    private static void ValidateControlTotals(Metro2ParseResult result)
    {
        var trailer = result.File.Trailer;
        int actual = result.File.BaseRecords.Count;
        if (trailer.TotalBaseRecords != actual)
            result.StructuralErrors.Add(
                $"Trailer control total mismatch: trailer says {trailer.TotalBaseRecords:0} base records, file contains {actual}.");
    }
}
