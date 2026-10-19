# Time

Last updated: 2026-10-19

- Declaration: `public sealed partial class Time : ElectronObject`
- Sources: [service and system clock](../../src/Core/OS/Time.cs), [calendar and parsing](../../src/Core/OS/Time.Calendar.cs)
- Inherits: [ElectronObject](ElectronObject.md)
- Component: [Time service](../components/time-service.md)

## Description

A thread-safe static facade over one retained process-lifetime object. Engine.GetSingleton("Time") returns the same identity; consumers cannot dispose or unregister it. Calls work before any window or Engine loop starts. The service has no mutable clock settings.

Calendar conversions use the proleptic Gregorian calendar and astronomical year numbering, including year zero and negative years. Signed 64-bit Unix seconds roundtrip exactly. Integer arithmetic avoids DateTime's year range and floating-point day rounding. Conversion does not apply a timezone or daylight adjustment; weekday and DST input metadata are ignored. Leap seconds reject.

DateTimeInfo input components are nullable. Missing fields resolve to 1970-01-01 at midnight; entirely empty input rejects. Date/time parsing accepts a date, a clock or their T/space-separated combination, resolves missing parts to the epoch, and discards decimal seconds fractions. Invalid dates, malformed tokens and timezone suffixes reject. Text uses invariant decimal digits. A year has at least four characters including a negative sign; expanded years remain untruncated.

System queries return current local or UTC wall time. They can move when the system clock changes. DST is present only on system DateTimeInfo results and is false for UTC. TimeZoneData holds the platform's current standard/daylight display name and current offset east of UTC in minutes. The underlying platform owns timezone data.

GetTicksMSEC and GetTicksUSEC observe the same Stopwatch origin at retained-service initialization. They never use the wall clock. Engine.TimeScale, scene pause, loop stop/start and native-session teardown do not reset them. Numeric calendar conversion and warmed ticks reuse value storage; returned strings intentionally allocate.

## Example

```csharp
var saved = new DateTimeInfo
{
    Year = 2024,
    Month = Time.Month.February,
    Day = 29
};
long unixSeconds = Time.GetUnixTimeFromDateTimeInfo(saved);
string stamp = Time.GetDateTimeStringFromUnixTime(unixSeconds);
ulong started = Time.GetTicksUSEC();
// Perform work, then measure Time.GetTicksUSEC() - started.
```

## Enumeration summary

| Declaration | Meaning |
| --- | --- |
| `public enum Time.Month` | [January 1 through December 12](Time.Month.md). |
| `public enum Time.Weekday` | [Sunday 0 through Saturday 6](Time.Weekday.md). |

## Method summary

| Signature | Description |
| --- | --- |
| [`public static ulong GetTicksMSEC()`](#getticksmsec) | Gets elapsed whole milliseconds since this process's retained time service initialized. |
| [`public static ulong GetTicksUSEC()`](#getticksusec) | Gets elapsed whole microseconds since this process's retained time service initialized. |
| [`public static double GetUnixTimeFromSystem()`](#getunixtimefromsystem) | Gets current UTC seconds since the Unix epoch, including fractional seconds. |
| [`public static DateTimeInfo GetDateTimeInfoFromSystem(bool UTC = false)`](#getdatetimeinfofromsystem) | Gets a coherent current system date and time with weekday and daylight metadata. |
| [`public static DateInfo GetDateInfoFromSystem(bool UTC = false)`](#getdateinfofromsystem) | Gets the current system date and weekday. |
| [`public static TimeInfo GetTimeInfoFromSystem(bool UTC = false)`](#gettimeinfofromsystem) | Gets the current system clock time. |
| [`public static string GetDateTimeStringFromSystem(bool UTC = false, bool useSpace = false)`](#getdatetimestringfromsystem) | Gets a current system date/time string. |
| [`public static string GetDateStringFromSystem(bool UTC = false)`](#getdatestringfromsystem) | Gets the current system date as an invariant year-month-day string. |
| [`public static string GetTimeStringFromSystem(bool UTC = false)`](#gettimestringfromsystem) | Gets the current system clock as an invariant hour:minute:second string. |
| [`public static TimeZoneData GetTimeZoneFromSystem()`](#gettimezonefromsystem) | Gets the current local timezone's display name and offset. |
| [`public static string GetOffsetStringFromOffsetMinutes(long offsetMinutes)`](#getoffsetstringfromoffsetminutes) | Formats a signed timezone offset measured in minutes. |
| [`public static DateTimeInfo GetDateTimeInfoFromUnixTime(long unixTime)`](#getdatetimeinfofromunixtime) | Converts signed Unix seconds to an exact proleptic Gregorian date and time. |
| [`public static DateInfo GetDateInfoFromUnixTime(long unixTime)`](#getdateinfofromunixtime) | Converts signed Unix seconds to their Gregorian date and weekday. |
| [`public static TimeInfo GetTimeInfoFromUnixTime(long unixTime)`](#gettimeinfofromunixtime) | Converts signed Unix seconds to a clock time within its floor-divided day. |
| [`public static string GetDateTimeStringFromUnixTime(long unixTime, bool useSpace = false)`](#getdatetimestringfromunixtime) | Formats signed Unix seconds as an invariant Gregorian date and clock string. |
| [`public static string GetDateStringFromUnixTime(long unixTime)`](#getdatestringfromunixtime) | Formats signed Unix seconds as an invariant Gregorian date string. |
| [`public static string GetTimeStringFromUnixTime(long unixTime)`](#gettimestringfromunixtime) | Formats signed Unix seconds as an invariant clock string. |
| [`public static long GetUnixTimeFromDateTimeInfo(DateTimeInfo dateTime)`](#getunixtimefromdatetimeinfo) | Converts typed date/time components to signed Unix seconds. |
| [`public static string GetDateTimeStringFromDateTimeInfo(DateTimeInfo dateTime, bool useSpace)`](#getdatetimestringfromdatetimeinfo) | Formats typed Gregorian date/time components using epoch defaults for missing components. |
| [`public static DateTimeInfo GetDateTimeInfoFromDateTimeString(string dateTime, bool weekday)`](#getdatetimeinfofromdatetimestring) | Parses an invariant date, time or combined date/time string. |
| [`public static long GetUnixTimeFromDateTimeString(string dateTime)`](#getunixtimefromdatetimestring) | Parses an invariant date/time string and converts its components to Unix seconds. |

## Protected extension point

`protected override void ValidateDisposal()` always throws InvalidOperationException. It preserves the process-lifetime service identity and cannot be overridden further.

## Method Descriptions

<a id="getticksmsec"></a>
### GetTicksMSEC

`public static ulong GetTicksMSEC()`

Gets elapsed whole milliseconds since this process's retained time service initialized.

**Returns:** A monotonic unsigned 64-bit count, independent of wall time, scene pause and Engine.TimeScale.

<a id="getticksusec"></a>
### GetTicksUSEC

`public static ulong GetTicksUSEC()`

Gets elapsed whole microseconds since this process's retained time service initialized.

**Returns:** A monotonic unsigned 64-bit count, independent of loop starts and stops.

<a id="getunixtimefromsystem"></a>
### GetUnixTimeFromSystem

`public static double GetUnixTimeFromSystem()`

Gets current UTC seconds since the Unix epoch, including fractional seconds.

**Returns:** Wall-clock seconds; system adjustments can move this value backwards.

<a id="getdatetimeinfofromsystem"></a>
### GetDateTimeInfoFromSystem

`public static DateTimeInfo GetDateTimeInfoFromSystem(bool UTC = false)`

Gets a coherent current system date and time with weekday and daylight metadata.

**UTC:** Uses UTC when true; otherwise uses the local timezone.

**Returns:** All six calendar components, weekday and DST; UTC always reports DST false.

<a id="getdateinfofromsystem"></a>
### GetDateInfoFromSystem

`public static DateInfo GetDateInfoFromSystem(bool UTC = false)`

Gets the current system date and weekday.

**UTC:** Uses UTC when true; otherwise uses local time.

**Returns:** A complete date from one system sample.

<a id="gettimeinfofromsystem"></a>
### GetTimeInfoFromSystem

`public static TimeInfo GetTimeInfoFromSystem(bool UTC = false)`

Gets the current system clock time.

**UTC:** Uses UTC when true; otherwise uses local time.

**Returns:** Whole hour, minute and second from one system sample.

<a id="getdatetimestringfromsystem"></a>
### GetDateTimeStringFromSystem

`public static string GetDateTimeStringFromSystem(bool UTC = false, bool useSpace = false)`

Gets a current system date/time string.

**UTC:** Uses UTC when true; otherwise uses local time.

**useSpace:** Separates date and time with a space instead of T.

**Returns:** An invariant year-month-day and whole-second clock string.

<a id="getdatestringfromsystem"></a>
### GetDateStringFromSystem

`public static string GetDateStringFromSystem(bool UTC = false)`

Gets the current system date as an invariant year-month-day string.

**UTC:** Uses UTC when true; otherwise uses local time.

**Returns:** The formatted date.

<a id="gettimestringfromsystem"></a>
### GetTimeStringFromSystem

`public static string GetTimeStringFromSystem(bool UTC = false)`

Gets the current system clock as an invariant hour:minute:second string.

**UTC:** Uses UTC when true; otherwise uses local time.

**Returns:** The formatted whole-second clock time.

<a id="gettimezonefromsystem"></a>
### GetTimeZoneFromSystem

`public static TimeZoneData GetTimeZoneFromSystem()`

Gets the current local timezone's display name and offset.

**Returns:** A standard/daylight name and current signed UTC offset in minutes.

<a id="getoffsetstringfromoffsetminutes"></a>
### GetOffsetStringFromOffsetMinutes

`public static string GetOffsetStringFromOffsetMinutes(long offsetMinutes)`

Formats a signed timezone offset measured in minutes.

**offsetMinutes:** Any signed 64-bit offset; no timezone conversion is performed.

**Returns:** A signed hours:minutes string, with at least two hour digits.

<a id="getdatetimeinfofromunixtime"></a>
### GetDateTimeInfoFromUnixTime

`public static DateTimeInfo GetDateTimeInfoFromUnixTime(long unixTime)`

Converts signed Unix seconds to an exact proleptic Gregorian date and time.

**unixTime:** Whole seconds relative to 1970-01-01; negative values are supported.

**Returns:** All calendar components and weekday. DST is absent and no timezone conversion occurs.

<a id="getdateinfofromunixtime"></a>
### GetDateInfoFromUnixTime

`public static DateInfo GetDateInfoFromUnixTime(long unixTime)`

Converts signed Unix seconds to their Gregorian date and weekday.

**unixTime:** Whole seconds relative to the Unix epoch.

**Returns:** An exact date throughout the signed 64-bit Unix range, including astronomical year zero.

<a id="gettimeinfofromunixtime"></a>
### GetTimeInfoFromUnixTime

`public static TimeInfo GetTimeInfoFromUnixTime(long unixTime)`

Converts signed Unix seconds to a clock time within its floor-divided day.

**unixTime:** Whole seconds relative to the Unix epoch.

**Returns:** A nonnegative hour, minute and second; minus one becomes 23:59:59.

<a id="getdatetimestringfromunixtime"></a>
### GetDateTimeStringFromUnixTime

`public static string GetDateTimeStringFromUnixTime(long unixTime, bool useSpace = false)`

Formats signed Unix seconds as an invariant Gregorian date and clock string.

**unixTime:** Whole seconds relative to the Unix epoch.

**useSpace:** Uses a space instead of T between date and clock.

**Returns:** A whole-second date/time string without a timezone suffix.

<a id="getdatestringfromunixtime"></a>
### GetDateStringFromUnixTime

`public static string GetDateStringFromUnixTime(long unixTime)`

Formats signed Unix seconds as an invariant Gregorian date string.

**unixTime:** Whole seconds relative to the Unix epoch.

**Returns:** A year-month-day string with a minimum four-character year.

<a id="gettimestringfromunixtime"></a>
### GetTimeStringFromUnixTime

`public static string GetTimeStringFromUnixTime(long unixTime)`

Formats signed Unix seconds as an invariant clock string.

**unixTime:** Whole seconds relative to the Unix epoch.

**Returns:** An hour:minute:second string.

<a id="getunixtimefromdatetimeinfo"></a>
### GetUnixTimeFromDateTimeInfo

`public static long GetUnixTimeFromDateTimeInfo(DateTimeInfo dateTime)`

Converts typed date/time components to signed Unix seconds.

**dateTime:** Present components override epoch defaults; weekday and DST metadata are ignored.

**Returns:** Whole seconds in the same timezone as the supplied components.

**System.ArgumentException:** The input is entirely empty.

**System.ArgumentOutOfRangeException:** A component or resulting timestamp is out of range.

<a id="getdatetimestringfromdatetimeinfo"></a>
### GetDateTimeStringFromDateTimeInfo

`public static string GetDateTimeStringFromDateTimeInfo(DateTimeInfo dateTime, bool useSpace)`

Formats typed Gregorian date/time components using epoch defaults for missing components.

**dateTime:** Nonempty components; weekday and DST metadata are ignored.

**useSpace:** Uses a space instead of T between date and clock.

**Returns:** An invariant whole-second string without a timezone suffix.

**System.ArgumentException:** The components are empty or invalid.

<a id="getdatetimeinfofromdatetimestring"></a>
### GetDateTimeInfoFromDateTimeString

`public static DateTimeInfo GetDateTimeInfoFromDateTimeString(string dateTime, bool weekday)`

Parses an invariant date, time or combined date/time string.

**dateTime:** A Gregorian date and/or clock separated by T or space. A decimal seconds fraction is ignored.

**weekday:** Calculates weekday when true; otherwise returns absent weekday metadata.

**Returns:** All calendar components, with missing date/time parts resolved to the Unix epoch.

**System.ArgumentException:** The input is null or contains invalid calendar components.

**System.FormatException:** The string is malformed or contains a timezone suffix.

**System.OverflowException:** A numeric component exceeds its integer range.

<a id="getunixtimefromdatetimestring"></a>
### GetUnixTimeFromDateTimeString

`public static long GetUnixTimeFromDateTimeString(string dateTime)`

Parses an invariant date/time string and converts its components to Unix seconds.

**dateTime:** A date, time or combination without timezone conversion; fractional seconds are discarded.

**Returns:** Whole signed Unix seconds in the input's own timezone.

**System.ArgumentException:** The input has invalid components.

**System.FormatException:** The input has invalid syntax.

**System.OverflowException:** A numeric input component exceeds its integer range.

## Verification and limits

TimeTests covers all 21 operations, a complete 146097-day Gregorian cycle against BCL dates, 4096 full-width random Unix values and signed extrema, optional fields, invalid dates/strings, invariant cultures, fractional seconds, offset extrema, current system samples, fresh timezone processes, registry/disposal, cross-thread monotonicity and Engine-driven zero-scale/paused scenes. A warmed 4096-sample numeric calendar/ticks bracket measures managed owner allocations. Foreign timezone databases, externally adjusted wall time, native allocation and owner acceptance remain separate evidence.
