using System.Diagnostics;
using System.Text.Json;

namespace Electron2D.Examples.PhysicsNetwork;

internal readonly record struct CostCounters(long Upload, long Readback, long DeviceCopy, long Submissions, long Uniform, double WaitMS, long Timestamp)
{
    internal static CostCounters Add(CostCounters a, CostCounters b) => new(a.Upload + b.Upload, a.Readback + b.Readback,
        a.DeviceCopy + b.DeviceCopy, a.Submissions + b.Submissions, a.Uniform + b.Uniform, a.WaitMS + b.WaitMS, a.Timestamp + b.Timestamp);
    internal static CostCounters Difference(CostCounters a, CostCounters b) => new(a.Upload - b.Upload, a.Readback - b.Readback,
        a.DeviceCopy - b.DeviceCopy, a.Submissions - b.Submissions, a.Uniform - b.Uniform, a.WaitMS - b.WaitMS, a.Timestamp - b.Timestamp);
    internal bool IsNonnegative => Upload >= 0 && Readback >= 0 && DeviceCopy >= 0 && Submissions >= 0 && Uniform >= 0 && WaitMS >= -1e-6 && Timestamp >= 0;
}

internal sealed class CostSampler
{
    private static readonly string[] Names = ["authority.prepare", "authority.frame", "authority.checks", "authority.sleep_reads",
        "prediction.prepare", "prediction.frame", "prediction.checks", "prediction.sleep_reads",
        "replay.prepare", "replay.frame", "replay.checks", "replay.sleep_reads",
        "snapshot.capture", "correction.prior_poses", "correction.reconcile", "correction.apply", "correction.checks",
        "correction.capture_presentation", "correction.final_poses", "presentation.update"];
    private struct Region { internal long Count; internal CostCounters Sum; internal int Samples; }
    private readonly Func<CostCounters> _capture;
    private readonly Region[] _regions = new Region[Names.Length];
    private readonly double[] _samples;
    private readonly int _capacity, _owner = Environment.CurrentManagedThreadId;
    private readonly CostCounters _initial;
    private CostCounters _start;
    private int _open = -1;
    private bool _sealed;
    internal CostSampler(Func<CostCounters> capture, int capacity = 4096)
    {
        ArgumentNullException.ThrowIfNull(capture);
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capture = capture; _capacity = capacity; _samples = new double[checked(Names.Length * capacity)]; _initial = capture();
    }
    internal void Mark(int stage, bool begin)
    {
        if (_owner != Environment.CurrentManagedThreadId || _sealed || (uint)stage >= Names.Length || (begin ? _open != -1 : _open != stage))
            throw new InvalidOperationException("Invalid cost bracket");
        var now = _capture();
        if (begin) { _start = now; _open = stage; return; }
        var delta = CostCounters.Difference(now, _start);
        if (!delta.IsNonnegative) throw new InvalidOperationException("Cost counters moved backwards");
        ref var region = ref _regions[stage]; region.Count++; region.Sum = CostCounters.Add(region.Sum, delta);
        if (region.Samples < _capacity) _samples[stage * _capacity + region.Samples++] = delta.Timestamp * 1000d / Stopwatch.Frequency;
        _open = -1;
    }
    internal void Write(string path)
    {
        if (_owner != Environment.CurrentManagedThreadId || _sealed || _open != -1) throw new InvalidOperationException("Incomplete cost brackets");
        var end = _capture(); var total = CostCounters.Difference(end, _initial); var measured = default(CostCounters);
        foreach (var region in _regions) measured = CostCounters.Add(measured, region.Sum);
        var remainder = CostCounters.Difference(total, measured);
        if (!total.IsNonnegative || !remainder.IsNonnegative) throw new InvalidOperationException("Cost accounting does not reconcile");
        _sealed = true;
        using var file = File.Create(path); using var writer = new Utf8JsonWriter(file, new() { Indented = true });
        writer.WriteStartObject(); writer.WriteNumber("schema", 1); writer.WriteNumber("sampleCapacityPerRegion", _capacity);
        writer.WriteNumber("samplePayloadBytes", (long)_samples.Length * sizeof(double));
        writer.WriteString("scope", "Successful nonoverlapping owner-side brackets; sampled wall time includes observer overhead. Samples retain the first bounded intervals, while all counter totals/counts remain exact. No GPU work is submitted by observation.");
        WriteCounters(writer, "initial", _initial with { Timestamp = 0 }); WriteCounters(writer, "total", total); WriteCounters(writer, "measured", measured); WriteCounters(writer, "unattributed", remainder);
        writer.WriteStartArray("regions");
        for (var i = 0; i < _regions.Length; i++)
        {
            ref var region = ref _regions[i]; writer.WriteStartObject(); writer.WriteString("name", Names[i]); writer.WriteNumber("count", region.Count);
            writer.WriteNumber("sampleCount", region.Samples); writer.WriteNumber("unsampledCount", region.Count - region.Samples);
            WriteCounters(writer, "cost", region.Sum); Array.Sort(_samples, i * _capacity, region.Samples);
            writer.WriteStartArray("wallPercentilesMS");
            foreach (var quantile in new[] { .5, .95, .99 }) writer.WriteNumberValue(region.Samples == 0 ? 0 : _samples[i * _capacity + (int)((region.Samples - 1) * quantile)]);
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        writer.WriteEndArray(); writer.WriteEndObject(); writer.Flush();
    }
    private static void WriteCounters(Utf8JsonWriter writer, string name, CostCounters value)
    {
        writer.WriteStartObject(name); writer.WriteNumber("uploadBytes", value.Upload); writer.WriteNumber("readbackBytes", value.Readback);
        writer.WriteNumber("deviceCopyBytes", value.DeviceCopy); writer.WriteNumber("submissions", value.Submissions); writer.WriteNumber("uniformBytes", value.Uniform);
        writer.WriteNumber("waitMS", value.WaitMS); writer.WriteNumber("wallMS", value.Timestamp * 1000d / Stopwatch.Frequency); writer.WriteEndObject();
    }
    internal static void Test()
    {
        var current = new CostCounters(10, 20, 30, 1, 4, 2, 100);
        var sampler = new CostSampler(() => current, 2);
        Reject(() => sampler.Mark(0, false)); Reject(() => sampler.Mark(-1, true));
        sampler.Mark(0, true); Reject(() => sampler.Mark(1, true)); Reject(() => sampler.Mark(1, false));
        current = new(15, 27, 33, 3, 8, 4, 200); sampler.Mark(0, false);
        Check(sampler._regions[0].Count == 1 && sampler._regions[0].Sum == new CostCounters(5, 7, 3, 2, 4, 2, 100), "Known deltas");
        for (var i = 0; i < 128; i++) { sampler.Mark(1, true); current = current with { Timestamp = current.Timestamp + 1 }; sampler.Mark(1, false); }
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) { sampler.Mark(1, true); current = current with { Timestamp = current.Timestamp + 1 }; sampler.Mark(1, false); }
        Check(GC.GetAllocatedBytesForCurrentThread() == owner && GC.GetTotalAllocatedBytes(true) == all, "Zero warm sampling bytes");
        Check(sampler._regions[1].Count == 256 && sampler._regions[1].Samples == 2, "Bounded samples retain all counts");
        Task.Run(() => Reject(() => sampler.Mark(1, true))).GetAwaiter().GetResult();
        sampler.Mark(2, true); current = current with { Upload = 0 }; Reject(() => sampler.Mark(2, false));
        current = current with { Upload = 15 }; sampler.Mark(2, false);
        current = CostCounters.Add(current, new(3, 5, 7, 1, 2, 1, 10));
        var path = System.IO.Path.GetTempFileName();
        try
        {
            sampler.Write(path); using var document = JsonDocument.Parse(File.ReadAllBytes(path)); var root = document.RootElement;
            Check(root.GetProperty("unattributed").GetProperty("uploadBytes").GetInt64() == 3 && root.GetProperty("regions")[1].GetProperty("unsampledCount").GetInt64() == 254, "Exact reconciliation and sample disclosure");
            Reject(() => sampler.Mark(0, true)); Reject(() => sampler.Write(path));
        }
        finally { File.Delete(path); }
        Console.WriteLine("Cost sampler: phase guards, known deltas, bounded samples, reconciliation and 128 warm samples at 0/0 owner/all-thread bytes passed.");
        static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        static void Reject(Action action) { try { action(); } catch (InvalidOperationException) { return; } throw new InvalidOperationException("Expected invalid cost operation"); }
    }
}
