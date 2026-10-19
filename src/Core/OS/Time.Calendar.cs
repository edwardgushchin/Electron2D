using System.Globalization;

namespace Electron2D;

public sealed partial class Time
{
    private const long EpochDay = 719528;
    private static Int128 FloorDivide(Int128 value, int divisor) { var result = value / divisor; return value % divisor < 0 ? result - 1 : result; }
    private static Int128 YearDay(long year) { Int128 y = year; return y * 365 + FloorDivide(y + 3, 4) - FloorDivide(y + 99, 100) + FloorDivide(y + 399, 400); }
    private static int MonthLength(long year, Month month) => month == Month.February ? year % 4 == 0 && (year % 100 != 0 || year % 400 == 0) ? 29 : 28 : month is Month.April or Month.June or Month.September or Month.November ? 30 : 31;
    private static Weekday DayWeek(Int128 day) { var value = (int)((day + 4) % 7); return (Weekday)(value < 0 ? value + 7 : value); }
    /// <summary>Converts signed Unix seconds to an exact proleptic Gregorian date and time.</summary>
    /// <param name="unixTime">Whole seconds relative to 1970-01-01; negative values are supported.</param>
    /// <returns>All calendar components and weekday. DST is absent and no timezone conversion occurs.</returns>
    public static DateTimeInfo GetDateTimeInfoFromUnixTime(long unixTime)
    {
        var date = GetDateInfoFromUnixTime(unixTime); var clock = GetTimeInfoFromUnixTime(unixTime);
        return new() { Year = date.Year, Month = date.Month, Day = date.Day, Weekday = date.Weekday, Hour = clock.Hour, Minute = clock.Minute, Second = clock.Second };
    }
    /// <summary>Converts signed Unix seconds to their Gregorian date and weekday.</summary>
    /// <param name="unixTime">Whole seconds relative to the Unix epoch.</param>
    /// <returns>An exact date throughout the signed 64-bit Unix range, including astronomical year zero.</returns>
    public static DateInfo GetDateInfoFromUnixTime(long unixTime)
    {
        var day = (long)FloorDivide(unixTime, 86400); var absolute = day + EpochDay;
        var cycle = (long)FloorDivide(absolute, 146097); var within = absolute - cycle * 146097;
        var year = cycle * 400 + within / 366;
        while (YearDay(year + 1) <= absolute) year++;
        var ordinal = (int)(absolute - YearDay(year)); var month = Month.January;
        while (ordinal >= MonthLength(year, month)) { ordinal -= MonthLength(year, month); month++; }
        return new(year, month, ordinal + 1, DayWeek(day));
    }
    /// <summary>Converts signed Unix seconds to a clock time within its floor-divided day.</summary>
    /// <param name="unixTime">Whole seconds relative to the Unix epoch.</param>
    /// <returns>A nonnegative hour, minute and second; minus one becomes 23:59:59.</returns>
    public static TimeInfo GetTimeInfoFromUnixTime(long unixTime)
    { var seconds = (int)(unixTime % 86400); if (seconds < 0) seconds += 86400; return new(seconds / 3600, seconds / 60 % 60, seconds % 60); }
    /// <summary>Formats signed Unix seconds as an invariant Gregorian date and clock string.</summary>
    /// <param name="unixTime">Whole seconds relative to the Unix epoch.</param>
    /// <param name="useSpace">Uses a space instead of T between date and clock.</param>
    /// <returns>A whole-second date/time string without a timezone suffix.</returns>
    public static string GetDateTimeStringFromUnixTime(long unixTime, bool useSpace = false) => GetDateTimeStringFromDateTimeInfo(GetDateTimeInfoFromUnixTime(unixTime), useSpace);
    /// <summary>Formats signed Unix seconds as an invariant Gregorian date string.</summary>
    /// <param name="unixTime">Whole seconds relative to the Unix epoch.</param>
    /// <returns>A year-month-day string with a minimum four-character year.</returns>
    public static string GetDateStringFromUnixTime(long unixTime) => FormatDate(GetDateInfoFromUnixTime(unixTime));
    /// <summary>Formats signed Unix seconds as an invariant clock string.</summary>
    /// <param name="unixTime">Whole seconds relative to the Unix epoch.</param>
    /// <returns>An hour:minute:second string.</returns>
    public static string GetTimeStringFromUnixTime(long unixTime) => FormatTime(GetTimeInfoFromUnixTime(unixTime));
    private static string FormatDate(DateInfo date) => string.Create(CultureInfo.InvariantCulture, $"{date.Year.ToString(date.Year < 0 ? "D3" : "D4", CultureInfo.InvariantCulture)}-{(int)date.Month:D2}-{date.Day:D2}");
    private static string FormatTime(TimeInfo time) => string.Create(CultureInfo.InvariantCulture, $"{time.Hour:D2}:{time.Minute:D2}:{time.Second:D2}");
    private static DateTimeInfo Resolve(DateTimeInfo value)
    {
        if (value.Year is null && value.Month is null && value.Day is null && value.Hour is null && value.Minute is null && value.Second is null && value.Weekday is null && value.DST is null) throw new ArgumentException("Date/time components cannot be empty.", nameof(value));
        var year = value.Year ?? 1970; var month = value.Month ?? Month.January; var day = value.Day ?? 1;
        var hour = value.Hour ?? 0; var minute = value.Minute ?? 0; var second = value.Second ?? 0;
        if (month < Month.January || month > Month.December || day < 1 || day > MonthLength(year, month) || hour is < 0 or > 23 || minute is < 0 or > 59 || second is < 0 or > 59) throw new ArgumentOutOfRangeException(nameof(value), "Invalid Gregorian date or clock time; leap seconds are not supported.");
        return new() { Year = year, Month = month, Day = day, Hour = hour, Minute = minute, Second = second, Weekday = value.Weekday, DST = value.DST };
    }
    private static Int128 CalendarDay(DateTimeInfo value)
    {
        var year = value.Year!.Value; var month = value.Month!.Value; var day = YearDay(year) - EpochDay + value.Day!.Value - 1;
        for (var current = Month.January; current < month; current++) day += MonthLength(year, current);
        return day;
    }
    /// <summary>Converts typed date/time components to signed Unix seconds.</summary>
    /// <param name="dateTime">Present components override epoch defaults; weekday and DST metadata are ignored.</param>
    /// <returns>Whole seconds in the same timezone as the supplied components.</returns>
    /// <exception cref="ArgumentException">The input is entirely empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A component or resulting timestamp is out of range.</exception>
    public static long GetUnixTimeFromDateTimeInfo(DateTimeInfo dateTime)
    {
        var value = Resolve(dateTime); var seconds = CalendarDay(value) * 86400 + value.Hour!.Value * 3600 + value.Minute!.Value * 60 + value.Second!.Value;
        if (seconds < long.MinValue || seconds > long.MaxValue) throw new ArgumentOutOfRangeException(nameof(dateTime), "Date/time does not fit signed 64-bit Unix seconds.");
        return (long)seconds;
    }
    /// <summary>Formats typed Gregorian date/time components using epoch defaults for missing components.</summary>
    /// <param name="dateTime">Nonempty components; weekday and DST metadata are ignored.</param>
    /// <param name="useSpace">Uses a space instead of T between date and clock.</param>
    /// <returns>An invariant whole-second string without a timezone suffix.</returns>
    /// <exception cref="ArgumentException">The components are empty or invalid.</exception>
    public static string GetDateTimeStringFromDateTimeInfo(DateTimeInfo dateTime, bool useSpace)
    {
        var value = Resolve(dateTime); var year = value.Year!.Value;
        return string.Create(CultureInfo.InvariantCulture, $"{year.ToString(year < 0 ? "D3" : "D4", CultureInfo.InvariantCulture)}-{(int)value.Month!.Value:D2}-{value.Day!.Value:D2}{(useSpace ? ' ' : 'T')}{value.Hour!.Value:D2}:{value.Minute!.Value:D2}:{value.Second!.Value:D2}");
    }
    /// <summary>Parses an invariant date, time or combined date/time string.</summary>
    /// <param name="dateTime">A Gregorian date and/or clock separated by T or space. A decimal seconds fraction is ignored.</param>
    /// <param name="weekday">Calculates weekday when true; otherwise returns absent weekday metadata.</param>
    /// <returns>All calendar components, with missing date/time parts resolved to the Unix epoch.</returns>
    /// <exception cref="ArgumentException">The input is null or contains invalid calendar components.</exception>
    /// <exception cref="FormatException">The string is malformed or contains a timezone suffix.</exception>
    /// <exception cref="OverflowException">A numeric component exceeds its integer range.</exception>
    public static DateTimeInfo GetDateTimeInfoFromDateTimeString(string dateTime, bool weekday)
    {
        ArgumentNullException.ThrowIfNull(dateTime); var text = dateTime.AsSpan();
        var separator = text.IndexOfAny('T', ' '); var date = ReadOnlySpan<char>.Empty; var clock = ReadOnlySpan<char>.Empty;
        if (separator >= 0) { date = text[..separator]; clock = text[(separator + 1)..]; if (date.IsEmpty || clock.IsEmpty) throw new FormatException("Expected both date and clock."); }
        else if (text.Contains(':')) clock = text;
        else date = text;
        long year = 1970; var month = Month.January; var day = 1; var hour = 0; var minute = 0; var second = 0;
        if (!date.IsEmpty)
        {
            var start = date[0] is '-' or '+' ? 1 : 0; var first = date[start..].IndexOf('-');
            if (first < 1) throw new FormatException("Expected year-month-day."); first += start;
            var rest = date[(first + 1)..]; var next = rest.IndexOf('-');
            if (next < 1) throw new FormatException("Expected year-month-day.");
            year = ParseNumber(date[..first], true); month = (Month)checked((int)ParseNumber(rest[..next], false)); day = checked((int)ParseNumber(rest[(next + 1)..], false));
        }
        if (!clock.IsEmpty)
        {
            var first = clock.IndexOf(':'); if (first < 1) throw new FormatException("Expected hour:minute:second.");
            var rest = clock[(first + 1)..]; var next = rest.IndexOf(':'); if (next < 1) throw new FormatException("Expected hour:minute:second.");
            var last = rest[(next + 1)..]; var fraction = last.IndexOf('.');
            if (fraction >= 0) { if (fraction == last.Length - 1) throw new FormatException("Empty seconds fraction."); foreach (var character in last[(fraction + 1)..]) if (character is < '0' or > '9') throw new FormatException("Invalid seconds fraction."); last = last[..fraction]; }
            hour = checked((int)ParseNumber(clock[..first], false)); minute = checked((int)ParseNumber(rest[..next], false)); second = checked((int)ParseNumber(last, false));
        }
        if (date.IsEmpty && clock.IsEmpty) throw new FormatException("Empty date/time string.");
        var value = Resolve(new() { Year = year, Month = month, Day = day, Hour = hour, Minute = minute, Second = second });
        return weekday ? new() { Year = year, Month = month, Day = day, Hour = hour, Minute = minute, Second = second, Weekday = DayWeek(CalendarDay(value)) } : value;
    }
    private static long ParseNumber(ReadOnlySpan<char> text, bool signed)
    {
        var start = signed && !text.IsEmpty && text[0] is '-' or '+' ? 1 : 0;
        if (start == text.Length) throw new FormatException("Expected a decimal component.");
        foreach (var character in text[start..]) if (character is < '0' or > '9') throw new FormatException("Expected decimal digits without timezone suffixes.");
        return long.Parse(text, signed ? NumberStyles.AllowLeadingSign : NumberStyles.None, CultureInfo.InvariantCulture);
    }
    /// <summary>Parses an invariant date/time string and converts its components to Unix seconds.</summary>
    /// <param name="dateTime">A date, time or combination without timezone conversion; fractional seconds are discarded.</param>
    /// <returns>Whole signed Unix seconds in the input's own timezone.</returns>
    /// <exception cref="ArgumentException">The input has invalid components.</exception>
    /// <exception cref="FormatException">The input has invalid syntax.</exception>
    /// <exception cref="OverflowException">A numeric input component exceeds its integer range.</exception>
    public static long GetUnixTimeFromDateTimeString(string dateTime) => GetUnixTimeFromDateTimeInfo(GetDateTimeInfoFromDateTimeString(dateTime, false));
}
