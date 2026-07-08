namespace CreditReporting.Api.Metro2;

public enum Metro2FieldType
{
    /// <summary>Left-justified, space-padded text.</summary>
    Alpha,
    /// <summary>Right-justified, zero-padded integer (decimals are written as whole dollars).</summary>
    Numeric,
    /// <summary>yyyyMMdd; null/empty is written as eight zeros.</summary>
    Date
}

/// <summary>
/// Declares the fixed-width position of a property within a Metro 2-style record.
/// Positions are 1-based and inclusive, matching how fixed-width specs are documented.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class Metro2FieldAttribute : Attribute
{
    public Metro2FieldAttribute(int start, int length, Metro2FieldType type = Metro2FieldType.Alpha)
    {
        Start = start;
        Length = length;
        Type = type;
    }

    /// <summary>1-based start position within the record.</summary>
    public int Start { get; }
    public int Length { get; }
    public Metro2FieldType Type { get; }
}
