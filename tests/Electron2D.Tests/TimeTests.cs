using System.Diagnostics;
using System.Globalization;
using Electron2D;
using Time = Electron2D.Time;

internal static class TimeTests
{
    private static ulong _sink;
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void Run()
    {
        Calendar(); Syntax(); SystemClock(); Lifecycle(); Allocation(); Zones();
        Console.WriteLine("Time calendar/full Unix range, typed partial values, invariant strings, system/timezone, retained clock and zero-allocation checks passed.");
    }
    private static void Calendar()
    {
        foreach (var value in new long[] { long.MinValue, long.MinValue + 86399, -9007199254740993, -62167219200, -62135596800, -86401, -86400, -1, 0, 1, 86399, 86400, 2147483647, 253402300799, 9007199254740993, long.MaxValue - 86399, long.MaxValue })
        {
            var parts = Time.GetDateTimeInfoFromUnixTime(value);
            Check(Time.GetUnixTimeFromDateTimeInfo(parts) == value && parts.DST is null, "Every signed Unix boundary roundtrips without fractional-day precision loss.");
            Check(Time.GetUnixTimeFromDateTimeString(Time.GetDateTimeStringFromUnixTime(value)) == value, "Extended years and extreme Unix seconds survive string roundtrip.");
        }
        var epoch = Time.GetDateInfoFromUnixTime(0); Check(epoch.Year == 1970 && epoch.Month == Time.Month.January && epoch.Day == 1 && epoch.Weekday == Time.Weekday.Thursday, "Epoch date and enum identities.");
        Check(Time.GetDateTimeStringFromUnixTime(-1) == "1969-12-31T23:59:59" && Time.GetTimeStringFromUnixTime(long.MinValue) == "08:29:52", "Negative seconds use floor days and nonnegative clock remainders.");
        Check(Time.GetDateStringFromUnixTime(-62167219200) == "0000-01-01" && Time.GetDateStringFromUnixTime(-62198755200) == "-001-01-01", "Astronomical year zero and negative numeric year width.");
        Check(Time.GetUnixTimeFromDateTimeString("1582-10-15") - Time.GetUnixTimeFromDateTimeString("1582-10-14") == 86400, "Gregorian conversion has no historical calendar gap.");
        var start = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (var day = 0; day < 146097; day++)
        {
            var expected = start.AddDays(day); var seconds = expected.ToUnixTimeSeconds() + 45296; var actual = Time.GetDateTimeInfoFromUnixTime(seconds);
            Check(actual.Year == expected.Year && (int)actual.Month!.Value == expected.Month && actual.Day == expected.Day && (int)actual.Weekday!.Value == (int)expected.DayOfWeek && actual.Hour == 12 && actual.Minute == 34 && actual.Second == 56 && Time.GetUnixTimeFromDateTimeInfo(actual) == seconds, "A complete Gregorian 400-year cycle matches the independent BCL calendar.");
        }
        var random = new Random(91); Span<byte> bytes = stackalloc byte[8];
        for (var i = 0; i < 4096; i++)
        {
            random.NextBytes(bytes); var seconds = BitConverter.ToInt64(bytes); var parts = Time.GetDateTimeInfoFromUnixTime(seconds);
            Check(Time.GetUnixTimeFromDateTimeInfo(parts) == seconds, "Full-width random Unix values roundtrip.");
            var date = Time.GetDateTimeInfoFromDateTimeString(Time.GetDateTimeStringFromUnixTime(seconds), true);
            Check(date.Weekday == parts.Weekday, "Large-year string weekday agrees with Unix conversion.");
        }
        foreach (var year in new long[] { -800, -400, 0, 1600, 2000, 2400 }) Check(Time.GetDateTimeInfoFromDateTimeString($"{year}-02-29", true).Day == 29, "400-year leap dates work on both sides of year zero.");
        foreach (var year in new long[] { -100, 100, 1700, 1800, 1900, 2100 }) Reject<ArgumentOutOfRangeException>(() => Time.GetUnixTimeFromDateTimeInfo(new() { Year = year, Month = Time.Month.February, Day = 29 }));
        Console.WriteLine("Calendar: 146097 independent day samples and 4096 full-width Unix values passed.");
    }
    private static void Syntax()
    {
        Check(Time.GetUnixTimeFromDateTimeInfo(new() { Minute = 1 }) == 60 && Time.GetUnixTimeFromDateTimeInfo(new() { DST = true }) == 0, "Partial fields use epoch defaults; DST is metadata.");
        Check(Time.GetDateTimeStringFromDateTimeInfo(new() { Day = 2, Weekday = (Time.Weekday)99, DST = true }, true) == "1970-01-02 00:00:00", "Input weekday and DST are ignored by numeric conversion.");
        Check(Time.GetUnixTimeFromDateTimeString("12:34:56.999") == 45296 && Time.GetDateTimeStringFromUnixTime(45296, true) == "1970-01-01 12:34:56", "Time-only parsing, decimal truncation and space formatting.");
        Check(Time.GetDateTimeInfoFromDateTimeString("2024-02-29", false) is { Hour: 0, Weekday: null, DST: null } && Time.GetDateTimeInfoFromDateTimeString("2024-02-29", true).Weekday == Time.Weekday.Thursday, "Parsed date-only clock defaults and optional weekday.");
        Check(Time.GetDateTimeInfoFromDateTimeString("+12000-01-02 03:04:05.6", true).Year == 12000, "Extended signed years parse independently of DateTime limits.");
        Check(Time.GetOffsetStringFromOffsetMinutes(-480) == "-08:00" && Time.GetOffsetStringFromOffsetMinutes(345) == "+05:45" && Time.GetOffsetStringFromOffsetMinutes(0) == "+00:00" && Time.GetOffsetStringFromOffsetMinutes(long.MinValue) == "-153722867280912930:08" && Time.GetOffsetStringFromOffsetMinutes(long.MaxValue) == "+153722867280912930:07", "Timezone offset formatting handles signs, partial hours and full signed range.");
        Reject<ArgumentException>(() => Time.GetUnixTimeFromDateTimeInfo(default)); Reject<ArgumentException>(() => Time.GetDateTimeStringFromDateTimeInfo(default, false)); Reject<ArgumentNullException>(() => Time.GetUnixTimeFromDateTimeString(null!));
        foreach (var text in new[] { "", "2024", "2024-01", "2024-01-01T", "T01:02:03", "2024-01-01T01:02:03Z", "01:02:03+01:00", "01:02:03.", "01:02:03.bad", "2024-01-01 01:02:03 extra", " 2024-01-01", "2024-01-01\0" }) Reject<FormatException>(() => Time.GetUnixTimeFromDateTimeString(text));
        foreach (var text in new[] { "2023-02-29", "2024-00-01", "2024-13-01", "2024-04-31", "2024-01-00", "24:00:00", "01:60:00", "01:00:60" }) Reject<ArgumentOutOfRangeException>(() => Time.GetUnixTimeFromDateTimeString(text));
        Reject<OverflowException>(() => Time.GetUnixTimeFromDateTimeString("999999999999999999999999-01-01"));
        Reject<ArgumentOutOfRangeException>(() => Time.GetUnixTimeFromDateTimeInfo(new() { Year = long.MaxValue }));
        Reject<ArgumentOutOfRangeException>(() => Time.GetUnixTimeFromDateTimeInfo(new() { Year = long.MinValue }));
        foreach (var culture in new[] { "ar-SA", "fr-FR", "th-TH" })
        {
            var saved = CultureInfo.CurrentCulture;
            try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture); Check(Time.GetDateTimeStringFromUnixTime(0) == "1970-01-01T00:00:00" && Time.GetUnixTimeFromDateTimeString("1970-01-01T00:00:00.9") == 0 && Time.GetOffsetStringFromOffsetMinutes(345) == "+05:45", "Calendar text is culture-invariant."); }
            finally { CultureInfo.CurrentCulture = saved; }
        }
    }
    private static void SystemClock()
    {
        var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); var value = Time.GetDateTimeInfoFromSystem(true); var seconds = Time.GetUnixTimeFromDateTimeInfo(value); var after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Check(seconds >= before && seconds <= after && value.DST == false, "UTC sample is coherent and has present false DST metadata.");
        var wall = Time.GetUnixTimeFromSystem(); Check(wall >= before && wall < after + 2, "Fractional Unix system time is UTC.");
        var local = Time.GetDateTimeInfoFromSystem(); var zone = Time.GetTimeZoneFromSystem(); var offset = DateTimeOffset.Now.Offset;
        Check(Math.Abs(Time.GetUnixTimeFromDateTimeInfo(local) - wall - offset.TotalSeconds) < 2 && zone.Bias == (int)offset.TotalMinutes && !string.IsNullOrEmpty(zone.Name) && local.DST == DateTime.Now.IsDaylightSavingTime(), "Local calendar and timezone reflect the platform offset and daylight state.");
        foreach (var utc in new[] { false, true })
        {
            var date = Time.GetDateInfoFromSystem(utc); var clock = Time.GetTimeInfoFromSystem(utc);
            Check(date.Month >= Time.Month.January && date.Month <= Time.Month.December && clock.Hour is >= 0 and <= 23 && Time.GetDateStringFromSystem(utc).Length == 10 && Time.GetTimeStringFromSystem(utc).Length == 8 && Time.GetDateTimeStringFromSystem(utc, true)[10] == ' ', "Every system return family executes.");
        }
        var first = Time.GetTicksUSEC(); Thread.Sleep(3); var later = Time.GetTicksUSEC(); var milliseconds = Time.GetTicksMSEC();
        Check(later > first && milliseconds >= later / 1000 && milliseconds - later / 1000 < 1000, "Monotonic units share an epoch and advance over elapsed host time.");
        Parallel.For(0, 4, _ => { var previous = Time.GetTicksUSEC(); for (var i = 0; i < 4096; i++) { var current = Time.GetTicksUSEC(); Check(current >= previous, "Concurrent elapsed reads never decrease."); previous = current; } });
    }
    private static void Lifecycle()
    {
        var service = Engine.GetSingleton(nameof(Time)); Check(service is Time && ReferenceEquals(service, Engine.GetSingleton(nameof(Time))), "Registry retains the same time object.");
        Reject<InvalidOperationException>(service.Dispose); Reject<InvalidOperationException>(() => Engine.UnregisterSingleton(nameof(Time)));
        Reject<InvalidOperationException>(() => Engine.UnregisterSingleton(nameof(NavigationServer))); Check(Engine.HasSingleton(nameof(NavigationServer)), "All permanent registry services preserve their identity.");
        Check(!service.IsDisposed && !typeof(Time).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Instance).Any() && typeof(Time).GetProperty("Instance") is null, "Static facade has no public instance operation alias.");
        var scale = Engine.TimeScale; using var node = new ClockNode(); using var tree = new SceneTree(node); var first = Time.GetTicksUSEC();
        try
        {
            Engine.TimeScale = 0; Engine.Start(tree); Engine.AdvanceFrame(.125); tree.Paused = true; Engine.AdvanceFrame(.125); tree.Paused = false; Engine.AdvanceFrame(.125); Engine.Stop();
            Check(node.Calls == 2 && node.AllZero && node.Last > first && Time.GetTicksUSEC() >= node.Last, "Engine-driven scene observes elapsed time while its scaled process delta is zero; pause and stop do not reset ticks.");
        }
        finally { if (Engine.MainLoop is not null) Engine.Stop(); Engine.TimeScale = scale; }
    }
    private sealed class ClockNode : Node
    {
        internal int Calls; internal bool AllZero = true; internal ulong Last;
        internal ClockNode() => ProcessEnabled = true;
        protected override void OnProcess(double delta) { Calls++; AllZero &= delta == 0; Thread.Sleep(1); Last = Time.GetTicksUSEC(); }
    }
    private static void Allocation()
    {
        static ulong Sample(int i)
        {
            var seconds = -62167219200L + i * 918731L; var value = Time.GetDateTimeInfoFromUnixTime(seconds);
            return unchecked((ulong)Time.GetUnixTimeFromDateTimeInfo(value)) ^ Time.GetTicksMSEC() ^ Time.GetTicksUSEC();
        }
        for (var i = 0; i < 512; i++) _sink ^= Sample(i);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 4096; i++) _sink ^= Sample(i); var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(bytes == 0, "Warmed numeric calendar conversions and monotonic ticks allocate zero managed owner bytes: " + bytes);
        Console.WriteLine("Prepared calendar/ticks: 4096 samples, 0 managed owner bytes.");
    }
    private static void Zones()
    {
        if (!OperatingSystem.IsLinux()) return;
        foreach (var zone in new[] { "UTC", "Asia/Kathmandu", "America/New_York" })
        {
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(TimeTests).Assembly.Location); start.Environment["ELECTRON2D_TEST_TIME"] = "zone"; start.Environment["TZ"] = zone;
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Timezone process."); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Timezone passed", StringComparison.Ordinal), error.GetAwaiter().GetResult());
        }
    }
    internal static void RunZone()
    {
        var actual = Time.GetTimeZoneFromSystem(); var expected = DateTimeOffset.Now.Offset; var zone = Environment.GetEnvironmentVariable("TZ");
        Check(actual.Bias == (int)expected.TotalMinutes && (zone != "UTC" || actual.Bias == 0) && (zone != "Asia/Kathmandu" || actual.Bias == 345) && (zone != "America/New_York" || actual.Bias is -240 or -300), "Fresh-process timezone selection and non-hour offset execute.");
        SystemClock(); Console.WriteLine("Timezone passed: " + zone + " " + actual.Bias);
    }
}
