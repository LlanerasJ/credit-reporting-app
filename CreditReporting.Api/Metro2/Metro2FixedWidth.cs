using System.Globalization;
using System.Reflection;

namespace CreditReporting.Api.Metro2;

/// <summary>
/// Reflection-based fixed-width engine: serializes any object whose properties
/// carry <see cref="Metro2FieldAttribute"/> into a fixed-length record, and back.
/// </summary>
public static class Metro2FixedWidth
{
    private const string DateFormat = "yyyyMMdd";

    private static readonly Dictionary<Type, (PropertyInfo Prop, Metro2FieldAttribute Field)[]> Cache = new();

    private static (PropertyInfo Prop, Metro2FieldAttribute Field)[] FieldsOf(Type type)
    {
        lock (Cache)
        {
            if (!Cache.TryGetValue(type, out var fields))
            {
                fields = type.GetProperties()
                    .Select(p => (Prop: p, Field: p.GetCustomAttribute<Metro2FieldAttribute>()))
                    .Where(x => x.Field is not null)
                    .Select(x => (x.Prop, x.Field!))
                    .OrderBy(x => x.Item2.Start)
                    .ToArray();
                Cache[type] = fields;
            }
            return fields;
        }
    }

    public static string Serialize(object record, int recordLength)
    {
        var buffer = new char[recordLength];
        Array.Fill(buffer, ' ');

        foreach (var (prop, field) in FieldsOf(record.GetType()))
        {
            string text = Format(prop.GetValue(record), field);
            text.CopyTo(0, buffer, field.Start - 1, field.Length);
        }
        return new string(buffer);
    }

    public static T Deserialize<T>(string line) where T : new()
    {
        var record = new T();
        foreach (var (prop, field) in FieldsOf(typeof(T)))
        {
            if (field.Start - 1 + field.Length > line.Length) continue;
            string raw = line.Substring(field.Start - 1, field.Length);
            prop.SetValue(record, Parse(raw, prop.PropertyType, field));
        }
        return record;
    }

    private static string Format(object? value, Metro2FieldAttribute field) => field.Type switch
    {
        Metro2FieldType.Numeric => FormatNumeric(value, field.Length),
        Metro2FieldType.Date => value is DateTime d
            ? d.ToString(DateFormat)
            : new string('0', field.Length),
        _ => Truncate(value?.ToString() ?? "", field.Length).PadRight(field.Length)
    };

    private static string FormatNumeric(object? value, int length)
    {
        long whole = value switch
        {
            decimal m => (long)Math.Round(m, 0, MidpointRounding.AwayFromZero),
            int i => i,
            long l => l,
            _ => 0
        };
        if (whole < 0) whole = 0;
        string text = whole.ToString(CultureInfo.InvariantCulture);
        return text.Length > length ? new string('9', length) : text.PadLeft(length, '0');
    }

    private static string Truncate(string s, int length) =>
        s.Length > length ? s[..length] : s;

    private static object? Parse(string raw, Type targetType, Metro2FieldAttribute field)
    {
        targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (targetType == typeof(DateTime))
        {
            return DateTime.TryParseExact(raw, DateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date) && date.Year > 1
                ? date
                : null;
        }
        if (targetType == typeof(decimal))
            return decimal.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var m) ? m : 0m;
        if (targetType == typeof(int))
            return int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : 0;

        return field.Type == Metro2FieldType.Numeric ? raw.Trim() : raw.TrimEnd();
    }
}
