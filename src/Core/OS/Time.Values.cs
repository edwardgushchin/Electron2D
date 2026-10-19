namespace Electron2D;

/// <summary>Contains a complete proleptic Gregorian date and its weekday.</summary>
public readonly struct DateInfo
{
    internal DateInfo(long year, Time.Month month, int day, Time.Weekday weekday) { Year = year; Month = month; Day = day; Weekday = weekday; }
    /// <summary>Gets the astronomical year, including zero and negative years.</summary>
    /// <value>The signed astronomical year.</value>
    public long Year { get; }
    /// <summary>Gets the one-based month.</summary>
    /// <value>January through December.</value>
    public Time.Month Month { get; }
    /// <summary>Gets the one-based day of the month.</summary>
    /// <value>A day from one through the actual month length.</value>
    public int Day { get; }
    /// <summary>Gets the weekday, with Sunday numbered zero.</summary>
    /// <value>Sunday through Saturday.</value>
    public Time.Weekday Weekday { get; }
}

/// <summary>Contains a complete clock time without a timezone or fractional second.</summary>
public readonly struct TimeInfo
{
    internal TimeInfo(int hour, int minute, int second) { Hour = hour; Minute = minute; Second = second; }
    /// <summary>Gets the hour from zero through 23.</summary>
    /// <value>A whole number from zero through 23.</value>
    public int Hour { get; }
    /// <summary>Gets the minute from zero through 59.</summary>
    /// <value>A whole number from zero through 59.</value>
    public int Minute { get; }
    /// <summary>Gets the second from zero through 59; leap seconds are not represented.</summary>
    /// <value>A whole number from zero through 59.</value>
    public int Second { get; }
}

/// <summary>Contains typed date/time components and optional weekday and system daylight metadata.</summary>
/// <remarks>Missing input components use 1970-01-01 at midnight. A completely empty input is invalid.
/// Conversion outputs contain all six calendar components; Weekday is optional on string parsing and DST is present only on system queries.</remarks>
public readonly struct DateTimeInfo
{
    /// <summary>Gets or initializes the astronomical year; null uses 1970 as conversion input.</summary>
    /// <value>Null for an omitted component; otherwise a signed astronomical year.</value>
    public long? Year { get; init; }
    /// <summary>Gets or initializes the month; null uses January as conversion input.</summary>
    /// <value>Null for an omitted component; otherwise January through December.</value>
    public Time.Month? Month { get; init; }
    /// <summary>Gets or initializes the day of the month; null uses one as conversion input.</summary>
    /// <value>Null for an omitted component; otherwise the one-based day.</value>
    public int? Day { get; init; }
    /// <summary>Gets or initializes the hour; null uses zero as conversion input.</summary>
    /// <value>Null for an omitted component; otherwise a whole hour from zero through 23.</value>
    public int? Hour { get; init; }
    /// <summary>Gets or initializes the minute; null uses zero as conversion input.</summary>
    /// <value>Null for an omitted component; otherwise a whole minute from zero through 59.</value>
    public int? Minute { get; init; }
    /// <summary>Gets or initializes the second; null uses zero as conversion input.</summary>
    /// <value>Null for an omitted component; otherwise a whole second from zero through 59.</value>
    public int? Second { get; init; }
    /// <summary>Gets or initializes optional weekday metadata; conversion inputs ignore it.</summary>
    /// <value>Null when not requested; otherwise the calculated weekday.</value>
    public Time.Weekday? Weekday { get; init; }
    /// <summary>Gets or initializes optional daylight-saving metadata; conversion inputs ignore it.</summary>
    /// <value>Null outside system samples; otherwise the platform daylight-saving state.</value>
    public bool? DST { get; init; }
}

/// <summary>Contains the current local timezone's display name and UTC offset.</summary>
public readonly struct TimeZoneData
{
    private readonly string? _name;
    internal TimeZoneData(string name, int bias) { _name = name; Bias = bias; }
    /// <summary>Gets the platform-provided standard or daylight display name.</summary>
    /// <value>A platform display name; empty for a default value.</value>
    public string Name => _name ?? string.Empty;
    /// <summary>Gets the current offset east of UTC in whole minutes.</summary>
    /// <value>The signed current UTC offset, including daylight adjustment.</value>
    public int Bias { get; }
}
