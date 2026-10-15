using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Diagnostics.Runtime;

if (args.Length != 4) throw new ArgumentException("Expected Fixture.dll, cpu|gpu, body count, checkpoint count.");
var fixture = Path.GetFullPath(args[0]);
var start = new ProcessStartInfo("dotnet") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
foreach (var argument in args) start.ArgumentList.Add(argument);
using var child = Process.Start(start) ?? throw new IOException("Could not start the memory fixture.");
var errors = child.StandardError.ReadToEndAsync();
var phases = new List<object>();
try
{
    foreach (var phase in new[] { "world", "history", "shrink", "released-history", "released-world" })
    {
        var line = await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromMinutes(1));
        if (line is null) throw new IOException("Fixture exited: " + await errors);
        var metadata = JsonSerializer.Deserialize<JsonElement>(line);
        if (metadata.GetProperty("Phase").GetString() != phase) throw new InvalidDataException(line);
        var maps = File.ReadAllText($"/proc/{child.Id}/maps");
        var lavapipe = maps.Contains("libvulkan_lvp.so");
        var nvidia = maps.Contains("libnvidia", StringComparison.OrdinalIgnoreCase);
        if (nvidia || (phase != "released-world" && lavapipe != (args[1] == "gpu")))
            throw new InvalidOperationException($"Unexpected driver mapping at {phase}: lavapipe={lavapipe}, NVIDIA={nvidia}.");
        phases.Add(new
        {
            Measurement = Inspect(child.Id, phase, metadata),
            LavapipeMapped = lavapipe,
            NVIDIAMapped = nvidia,
            CoreCLRPath = maps.Split('\n').First(line => line.Contains("libcoreclr.so")).Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1]
        });
        await child.StandardInput.WriteLineAsync("next"); await child.StandardInput.FlushAsync();
    }
    await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
    if (child.ExitCode != 0) throw new IOException("Fixture failed: " + await errors);
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        Backend = args[1],
        FixtureSHA256 = Hash(fixture),
        RuntimeSHA256 = Hash(Path.Combine(Path.GetDirectoryName(fixture)!, "Electron2D.dll")),
        ClrMD = typeof(DataTarget).Assembly.GetName().Version?.ToString(),
        ClrMDSHA256 = Hash(typeof(DataTarget).Assembly.Location),
        Phases = phases
    }, new JsonSerializerOptions { WriteIndented = true }));
}
finally
{
    if (!child.HasExited) { child.Kill(entireProcessTree: true); await child.WaitForExitAsync(); }
}

static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

static object Inspect(int pid, string phase, JsonElement metadata)
{
    using var target = DataTarget.AttachToProcess(pid, suspend: true);
    using var runtime = target.ClrVersions.Single().CreateRuntime();
    var heap = runtime.Heap;
    if (!heap.CanWalkHeap) throw new InvalidOperationException("Heap is not walkable.");
    var all = heap.EnumerateObjects().Where(o => o.IsValid && !o.IsFree).ToArray();
    var anchors = all.Single(o => o.Type?.Name == "MemoryRoots");
    var calibration = anchors.ReadObjectField("Calibration");
    var left = calibration.ReadObjectField("Left");
    var weak = calibration.ReadObjectField("Weak");
    var check = Walk(calibration, []);
    if (left.Address != calibration.ReadObjectField("Right").Address || check.Count != 3 ||
        check.Sum(o => checked((long)o.Size)) != checked((long)(calibration.Size + left.Size + weak.Size)))
        throw new InvalidDataException("Heap calibration failed: shared array or weak-target ownership.");

    var spaces = all.Where(o => o.Type?.Name == "Electron2D.PhysicsSpace").ToArray();
    if (spaces.Length != (phase == "released-world" ? 0 : 1)) throw new InvalidDataException("Unexpected retained physics spaces.");
    var roots = new List<(string Name, ClrObject Object)>();
    if (spaces.Length != 0) roots.Add(("world", spaces[0]));
    Add("map", anchors.ReadObjectField("Map")); Add("snapshot", anchors.ReadObjectField("Snapshot"));
    var points = anchors.ReadObjectField("Checkpoints").AsArray();
    for (var i = 0; i < points.Length; i++)
    {
        var point = points.GetObjectValue(i); Add($"checkpoint-{i}", point);
        Add($"checkpoint-{i}-solver", point.ReadObjectField("_state").ReadObjectField("_solver"));
    }

    // A checkpoint's source-world/owner links and a map's bindings borrow the world.
    // Cut other owner roots, then attribute shared referents to the world first and histories in order.
    // This is an explicit fixture ownership partition, not a GC-dominator retained-size calculation.
    var boundaries = roots.Select(x => x.Object.Address).ToHashSet();
    foreach (var o in all.Where(o => o.Type?.Name == "Electron2D.PhysicsSpace+Checkpoint")) boundaries.Add(o.Address);
    // Resident stores also register their device checkpoints independently of the common history list.
    foreach (var o in all.Where(o => o.Type?.Name == "Electron2D.GPUPhysicsBodyStore+Checkpoint")) boundaries.Add(o.Address);
    var seen = new HashSet<ulong>();
    var owners = new List<object>();
    foreach (var root in roots)
    {
        var cuts = new HashSet<ulong>(boundaries); cuts.Remove(root.Object.Address);
        // Public checkpoint wrappers own their common checkpoint, which must be traversed here.
        if (root.Name.StartsWith("checkpoint-") && !root.Name.EndsWith("-solver"))
        {
            var state = root.Object.ReadObjectField("_state"); cuts.Remove(state.Address);
        }
        if (root.Object.Type?.Name == "Electron2D.PhysicsSpace+GPUCheckpoint")
            cuts.Remove(root.Object.ReadObjectField("_kernel").Address);
        var objects = Walk(root.Object, cuts, seen);
        owners.Add(new
        {
            root.Name,
            Objects = objects.Count,
            Bytes = objects.Sum(o => checked((long)o.Size)),
            Types = objects.GroupBy(o => o.Type!.Name!).Select(g => new { Type = g.Key, Count = g.Count(), Bytes = g.Sum(o => checked((long)o.Size)) })
                .OrderByDescending(x => x.Bytes).ToArray()
        });
    }
    var store = all.SingleOrDefault(o => o.Type?.Name == "Electron2D.GPUPhysicsBodyStore");
    var requested = store.IsNull ? null : new
    {
        DeviceBytes = store.ReadField<long>("_deviceBufferBytes"),
        TransferBytes = store.ReadField<long>("_transferBufferBytes"),
        PeakBytes = store.ReadField<long>("_peakBufferBytes"),
        DeviceBuffers = store.ReadField<int>("_deviceBufferCount"),
        TransferBuffers = store.ReadField<int>("_transferBufferCount")
    };
    var snapshot = anchors.ReadObjectField("Snapshot");
    var union = new HashSet<ulong>();
    foreach (var root in roots) Walk(root.Object, [], union);
    if (!seen.SetEquals(union)) throw new InvalidDataException("Owner partition does not cover the full union.");
    return new
    {
        Metadata = metadata,
        ClrMDReportedRuntimeVersion = target.ClrVersions.Single().Version.ToString(),
        CalibrationBytes = check.Sum(o => checked((long)o.Size)),
        ManagedObjectBytes = seen.Sum(a => checked((long)heap.GetObject(a).Size)),
        Owners = owners,
        SnapshotCapacity = snapshot.IsNull ? 0 : snapshot.ReadObjectField("_data").AsArray().Length,
        RequestedGPU = requested
    };

    void Add(string name, ClrObject o) { if (!o.IsNull) roots.Add((name, o)); }
}

static List<ClrObject> Walk(ClrObject root, HashSet<ulong> boundaries, HashSet<ulong>? seen = null)
{
    seen ??= []; var result = new List<ClrObject>(); var pending = new Stack<ClrObject>(); pending.Push(root);
    while (pending.TryPop(out var o))
    {
        if (o.IsNull || boundaries.Contains(o.Address) || !seen.Add(o.Address)) continue;
        if (!o.IsValid || o.IsFree) throw new InvalidDataException("Invalid object in the owned graph.");
        result.Add(o);
        foreach (var next in o.EnumerateReferences(carefully: true, considerDependantHandles: false)) pending.Push(next);
    }
    return result;
}
