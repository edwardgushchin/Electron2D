using System.Diagnostics;
using System.Globalization;

namespace Electron2D;

/// <summary>Converts Gregorian dates and observes system wall time and independent monotonic elapsed time.</summary>
/// <remarks>Operations are thread-safe static access to one retained process-lifetime object. Calendar conversion
/// never applies a timezone or daylight adjustment. System time can change; use the monotonic ticks for elapsed intervals.</remarks>
public sealed partial class Time : ElectronObject
{
    /// <summary>Identifies a one-based calendar month.</summary>
    public enum Month
    {
        /// <summary>January, month one.</summary>
        January = 1,
        /// <summary>February, month two.</summary>
        February = 2,
        /// <summary>March, month three.</summary>
        March = 3,
        /// <summary>April, month four.</summary>
        April = 4,
        /// <summary>May, month five.</summary>
        May = 5,
        /// <summary>June, month six.</summary>
        June = 6,
        /// <summary>July, month seven.</summary>
        July = 7,
        /// <summary>August, month eight.</summary>
        August = 8,
        /// <summary>September, month nine.</summary>
        September = 9,
        /// <summary>October, month ten.</summary>
        October = 10,
        /// <summary>November, month eleven.</summary>
        November = 11,
        /// <summary>December, month twelve.</summary>
        December = 12
    }
    /// <summary>Identifies a weekday with Sunday numbered zero.</summary>
    public enum Weekday
    {
        /// <summary>Sunday, day zero.</summary>
        Sunday = 0,
        /// <summary>Monday, day one.</summary>
        Monday = 1,
        /// <summary>Tuesday, day two.</summary>
        Tuesday = 2,
        /// <summary>Wednesday, day three.</summary>
        Wednesday = 3,
        /// <summary>Thursday, day four.</summary>
        Thursday = 4,
        /// <summary>Friday, day five.</summary>
        Friday = 5,
        /// <summary>Saturday, day six.</summary>
        Saturday = 6
    }
    internal static Time Service { get; } = new();
    private readonly long _origin = Stopwatch.GetTimestamp();
    private Time() { }
    /// <inheritdoc />
    /// <remarks>The retained time service has process lifetime and cannot be disposed.</remarks>
    protected override void ValidateDisposal() => throw new InvalidOperationException("The process-wide Time service cannot be disposed.");
    /// <summary>Gets elapsed whole milliseconds since this process's retained time service initialized.</summary>
    /// <returns>A monotonic unsigned 64-bit count, independent of wall time, scene pause and Engine.TimeScale.</returns>
    public static ulong GetTicksMSEC() => Service.Elapsed(1000);
    /// <summary>Gets elapsed whole microseconds since this process's retained time service initialized.</summary>
    /// <returns>A monotonic unsigned 64-bit count, independent of loop starts and stops.</returns>
    public static ulong GetTicksUSEC() => Service.Elapsed(1000000);
    private ulong Elapsed(uint units)
    {
        var elapsed = unchecked((ulong)(Stopwatch.GetTimestamp() - _origin));
        return (ulong)((UInt128)elapsed * units / (ulong)Stopwatch.Frequency);
    }
    /// <summary>Gets current UTC seconds since the Unix epoch, including fractional seconds.</summary>
    /// <returns>Wall-clock seconds; system adjustments can move this value backwards.</returns>
    public static double GetUnixTimeFromSystem() => (DateTime.UtcNow.Ticks - DateTime.UnixEpoch.Ticks) / (double)TimeSpan.TicksPerSecond;
    /// <summary>Gets a coherent current system date and time with weekday and daylight metadata.</summary>
    /// <param name="UTC">Uses UTC when true; otherwise uses the local timezone.</param>
    /// <returns>All six calendar components, weekday and DST; UTC always reports DST false.</returns>
    public static DateTimeInfo GetDateTimeInfoFromSystem(bool UTC = false)
    {
        var now = UTC ? DateTime.UtcNow : DateTime.Now;
        return new() { Year = now.Year, Month = (Month)now.Month, Day = now.Day, Hour = now.Hour, Minute = now.Minute, Second = now.Second, Weekday = (Weekday)now.DayOfWeek, DST = !UTC && now.IsDaylightSavingTime() };
    }
    /// <summary>Gets the current system date and weekday.</summary>
    /// <param name="UTC">Uses UTC when true; otherwise uses local time.</param>
    /// <returns>A complete date from one system sample.</returns>
    public static DateInfo GetDateInfoFromSystem(bool UTC = false)
    { var value = GetDateTimeInfoFromSystem(UTC); return new(value.Year!.Value, value.Month!.Value, value.Day!.Value, value.Weekday!.Value); }
    /// <summary>Gets the current system clock time.</summary>
    /// <param name="UTC">Uses UTC when true; otherwise uses local time.</param>
    /// <returns>Whole hour, minute and second from one system sample.</returns>
    public static TimeInfo GetTimeInfoFromSystem(bool UTC = false)
    { var value = GetDateTimeInfoFromSystem(UTC); return new(value.Hour!.Value, value.Minute!.Value, value.Second!.Value); }
    /// <summary>Gets a current system date/time string.</summary>
    /// <param name="UTC">Uses UTC when true; otherwise uses local time.</param>
    /// <param name="useSpace">Separates date and time with a space instead of T.</param>
    /// <returns>An invariant year-month-day and whole-second clock string.</returns>
    public static string GetDateTimeStringFromSystem(bool UTC = false, bool useSpace = false) => GetDateTimeStringFromDateTimeInfo(GetDateTimeInfoFromSystem(UTC), useSpace);
    /// <summary>Gets the current system date as an invariant year-month-day string.</summary>
    /// <param name="UTC">Uses UTC when true; otherwise uses local time.</param>
    /// <returns>The formatted date.</returns>
    public static string GetDateStringFromSystem(bool UTC = false) => FormatDate(GetDateInfoFromSystem(UTC));
    /// <summary>Gets the current system clock as an invariant hour:minute:second string.</summary>
    /// <param name="UTC">Uses UTC when true; otherwise uses local time.</param>
    /// <returns>The formatted whole-second clock time.</returns>
    public static string GetTimeStringFromSystem(bool UTC = false) => FormatTime(GetTimeInfoFromSystem(UTC));
    /// <summary>Gets the current local timezone's display name and offset.</summary>
    /// <returns>A standard/daylight name and current signed UTC offset in minutes.</returns>
    public static TimeZoneData GetTimeZoneFromSystem()
    {
        var now = DateTimeOffset.Now; var zone = System.TimeZoneInfo.Local;
        return new(zone.IsDaylightSavingTime(now) ? zone.DaylightName : zone.StandardName, (int)now.Offset.TotalMinutes);
    }
    /// <summary>Formats a signed timezone offset measured in minutes.</summary>
    /// <param name="offsetMinutes">Any signed 64-bit offset; no timezone conversion is performed.</param>
    /// <returns>A signed hours:minutes string, with at least two hour digits.</returns>
    public static string GetOffsetStringFromOffsetMinutes(long offsetMinutes)
    {
        Int128 magnitude = offsetMinutes; if (magnitude < 0) magnitude = -magnitude;
        return string.Create(CultureInfo.InvariantCulture, $"{(offsetMinutes < 0 ? '-' : '+')}{magnitude / 60:D2}:{magnitude % 60:D2}");
    }
}
