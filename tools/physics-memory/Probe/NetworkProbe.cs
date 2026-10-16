using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Diagnostics.Runtime;
using static HeapGraph;

internal static class NetworkProbe
{
    internal static async Task Run(string[] args)
    {
        if (args.Length < 2) throw new ArgumentException("Expected NetworkFixture.dll, new output directory, optional process-check arguments.");
        var preload = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_PHYSICS_HEAPTRACK_PRELOAD");
        if (preload is not null) preload = Path.GetFullPath(preload);
        if (preload is not null && (Path.GetFileName(preload) != "libheaptrack_preload.so" || !File.Exists(preload))) throw new ArgumentException("An existing heaptrack preload library is required.");
        var markers = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_PHYSICS_PHASE_MARKERS");
        if (preload is not null && (markers is null || Path.GetFileName(markers) != "libElectron2DMemoryPhases.so" || !File.Exists(markers)) || preload is null && markers is not null)
            throw new ArgumentException("Network native traces require the matching phase marker library; ordinary graph collection excludes it.");
        var folder = Path.GetFullPath(args[1]);
        if (Directory.Exists(folder)) throw new IOException("The network memory output directory must be new.");
        var gates = Path.Combine(folder, "gates"); Directory.CreateDirectory(gates);
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { args[0], "--check", "--output", Path.Combine(folder, "sessions") }.Concat(args.Skip(2)))
            start.ArgumentList.Add(argument);
        start.Environment["ELECTRON2D_TEST_PHYSICS_MEMORY"] = gates;
        var traces = Path.Combine(folder, "allocation-traces");
        if (preload is not null)
        {
            Directory.CreateDirectory(traces);
            start.Environment["ELECTRON2D_TEST_PHYSICS_HEAPTRACK_PRELOAD"] = preload;
            start.Environment["ELECTRON2D_TEST_PHYSICS_HEAPTRACK_OUTPUT"] = traces;
        }
        using var check = Process.Start(start) ?? throw new IOException("Could not start the network process check.");
        var stdout = check.StandardOutput.ReadToEndAsync(); var stderr = check.StandardError.ReadToEndAsync();
        var measured = new HashSet<string>(); var started = Stopwatch.GetTimestamp();
        var processes = new Dictionary<string, int>();
        var collectorProfiled = false; var launcherProfiled = false;
        try
        {
            while (!check.HasExited)
            {
                if (Stopwatch.GetElapsedTime(started).TotalSeconds > 100) throw new TimeoutException("Network memory check exceeded its deadline.");
                foreach (var path in Directory.EnumerateFiles(gates, "*.ready.json"))
                {
                    if (!measured.Add(path)) continue;
                    if (measured.Count == 1)
                    {
                        var collectorMaps = File.ReadAllText($"/proc/{Environment.ProcessId}/maps"); var launcherMaps = File.ReadAllText($"/proc/{check.Id}/maps");
                        collectorProfiled = collectorMaps.Contains("libheaptrack_preload.so", StringComparison.Ordinal);
                        launcherProfiled = launcherMaps.Contains("libheaptrack_preload.so", StringComparison.Ordinal);
                        if (collectorProfiled || launcherProfiled || collectorMaps.Contains("libElectron2DMemoryPhases.so", StringComparison.Ordinal) || launcherMaps.Contains("libElectron2DMemoryPhases.so", StringComparison.Ordinal))
                            throw new InvalidOperationException("Only the three session children may be allocation-profiled or phase-marked.");
                    }
                    var metadata = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(path));
                    var pid = metadata.GetProperty("PID").GetInt32();
                    if (!IsDescendant(pid, check.Id)) throw new InvalidOperationException("The probe can inspect only its own network descendants.");
                    var role = metadata.GetProperty("Role").GetString()!; var phase = metadata.GetProperty("Phase").GetString()!;
                    if (processes.TryGetValue(role, out var existing) && existing != pid) throw new InvalidDataException("A network role changed processes.");
                    processes[role] = pid;
                    var maps = File.ReadAllText($"/proc/{pid}/maps");
                    var software = maps.Contains("libvulkan_lvp.so"); var hardware = maps.Contains("libnvidia", StringComparison.OrdinalIgnoreCase);
                    if (hardware || phase is "completed" or "checkpointed" && software != (role == "gpu")) throw new InvalidOperationException("Unexpected network driver mapping.");
                    var heaptrackMapped = maps.Contains("libheaptrack_preload.so", StringComparison.Ordinal);
                    if (heaptrackMapped != (preload is not null)) throw new InvalidOperationException("Unexpected network allocation profiler mapping.");
                    var markerMapped = maps.Contains("libElectron2DMemoryPhases.so", StringComparison.Ordinal);
                    if (markerMapped != (preload is not null)) throw new InvalidOperationException("Unexpected phase marker mapping.");
                    if (preload is not null)
                    {
                        var receipts = metadata.GetProperty("TraceMarkers").EnumerateArray().ToArray();
                        var expected = phase switch { "baseline" => 1, "completed" => 4, "checkpointed" => 5, "disposed-held" => 6, "released" => 7, _ => throw new InvalidDataException("Unknown memory phase.") };
                        if (receipts.Length != expected || receipts.Where((r, i) => r.GetProperty("Phase").GetInt32() != i).Any()) throw new InvalidDataException("Incomplete native phase receipts.");
                        if (expected >= 4 && (receipts[2].GetProperty("Tick").GetUInt64() < 300 || receipts[2].GetProperty("CompletedSteps").GetInt32() < 128)) throw new InvalidDataException("The native warm boundary preceded the original warmup gate.");
                    }
                    var report = new { Metadata = metadata, LavapipeMapped = software, NVIDIAMapped = hardware, HeaptrackMapped = heaptrackMapped, PhaseMarkerMapped = markerMapped, HeapAttached = preload is null, Process = ProcessObservation.Read(pid), Measurement = preload is null ? Inspect(pid, phase) : null };
                    File.WriteAllText(Path.Combine(folder, role + "-" + phase + ".json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
                    File.WriteAllText(path[..^".ready.json".Length] + ".go", "next");
                    Console.WriteLine($"Measured {role} {phase}.");
                }
                await Task.Delay(10);
            }
            File.WriteAllText(Path.Combine(folder, "check.stdout.log"), await stdout);
            File.WriteAllText(Path.Combine(folder, "check.stderr.log"), await stderr);
            if (check.ExitCode != 0 || measured.Count != 15) throw new InvalidOperationException("The full network check or one of its fifteen memory phases failed; inspect the check logs.");
            var allocations = new List<object>();
            if (preload is not null)
            {
                foreach (var (role, pid) in processes)
                {
                    var trace = Path.Combine(traces, role + ".heaptrack.raw");
                    if (!File.Exists(trace) || new FileInfo(trace).Length == 0) throw new IOException("A network child produced no allocation trace.");
                    allocations.Add(new { Role = role, PID = pid, Path = trace, Bytes = new FileInfo(trace).Length, SHA256 = Hash(trace) });
                }
            }
            File.WriteAllText(Path.Combine(folder, "collection.json"), JsonSerializer.Serialize(new { HeapAttached = preload is null, CollectorHeaptrackMappedAtFirstGate = collectorProfiled, LauncherHeaptrackMappedAtFirstGate = launcherProfiled, HeaptrackPreloadSHA256 = preload is null ? null : Hash(preload), PhaseMarkerLibrarySHA256 = markers is null ? null : Hash(markers), AllocationTraces = allocations }, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            if (!check.HasExited) { check.Kill(entireProcessTree: true); await check.WaitForExitAsync(); }
            File.WriteAllText(Path.Combine(folder, "check.stdout.log"), await stdout);
            File.WriteAllText(Path.Combine(folder, "check.stderr.log"), await stderr);
        }
    }

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    private static bool IsDescendant(int pid, int ancestor)
    {
        while (pid > 1)
        {
            if (pid == ancestor) return true;
            var status = File.ReadAllLines($"/proc/{pid}/status");
            pid = int.Parse(status.Single(line => line.StartsWith("PPid:")).Split(':')[1]);
        }
        return false;
    }

    private static object Inspect(int pid, string phase)
    {
        using var target = DataTarget.AttachToProcess(pid, suspend: true);
        using var runtime = target.ClrVersions.Single().CreateRuntime(); var heap = runtime.Heap;
        if (!heap.CanWalkHeap) throw new InvalidOperationException("Heap is not walkable.");
        var all = heap.EnumerateObjects().Where(o => o.IsValid && !o.IsFree).ToArray();
        var calibrationBytes = Calibrate(all.Single(o => o.Type?.Name == "Calibration"));
        const string prefix = "Electron2D.Examples.PhysicsNetwork.";
        var sessions = all.Where(o => o.Type?.Name == prefix + "NetworkSession").ToArray();
        var spaces = all.Where(o => o.Type?.Name == "Electron2D.PhysicsSpace").ToArray();
        var checkpoints = all.Where(o => o.Type?.Name == "Electron2D.PhysicsCheckpoint").ToArray();
        if (phase is "baseline" or "released")
        {
            var remaining = all.Where(o => IsDerived(o.Type, "Electron2D.Node") || IsDerived(o.Type, "Electron2D.Shape") ||
                o.Type?.Name is "Electron2D.SceneTree" or "Electron2D.World" or "Electron2D.PhysicsSpace" or "Electron2D.PhysicsCheckpoint" or
                    "Electron2D.PhysicsSnapshotMap" or "Electron2D.GPUPhysicsBodyStore" or "Electron2D.CPUPhysicsWorldBackend" or
                    "Electron2D.SceneMultiplayer" or "Electron2D.ENetMultiplayerPeer" or "Electron2D.ENetConnection" ||
                o.Type?.Name == prefix + "NetworkWorld" || o.Type?.Name == prefix + "NetworkSession").ToArray();
            if (remaining.Length != 0)
                throw new InvalidDataException("Released scene/network owners still exist: " + string.Join(", ", remaining.GroupBy(o => o.Type!.Name).Select(g => g.Key + "=" + g.Count())));
            return new { CalibrationBytes = calibrationBytes, ManagedObjectBytes = 0, RemainingOwners = remaining.Length, Owners = Array.Empty<object>(), CPUWorldPool = CPUWorldPool(runtime, []) };
        }
        var session = sessions.Single(); var simulation = session.ReadObjectField("Simulation");
        var tree = simulation.ReadObjectField("Tree");
        if (checkpoints.Length != (phase == "completed" ? 0 : 4)) throw new InvalidDataException("Unexpected local scene checkpoint count.");
        var reachable = new HashSet<ulong>(); var full = Walk(session, [], reachable);
        foreach (var point in checkpoints) full.AddRange(Walk(point, [], reachable));
        var nodes = full.Where(o => IsDerived(o.Type, "Electron2D.Node")).ToList();
        var groups = new List<(string Name, List<ClrObject> Roots)>();
        Add("physics", spaces.Where(o => reachable.Contains(o.Address)).ToArray());
        Add("world-resource", simulation.ReadObjectField("World"));
        Add("scene-tree", tree); Add("scene-nodes", nodes.ToArray());
        Add("snapshot-map", simulation.ReadObjectField("Map")); Add("snapshot", session.ReadObjectField("_snapshot"));
        Add("scene-default-multiplayer", tree.ReadObjectField("_defaultMultiplayer"));
        Fields("network-routing", "_network"); Fields("enet-peer", "_peer"); Fields("delayed-packets", "_link");
        Fields("input-history", "_commands"); Fields("event-history", "_events", "_receivedEvents", "_confirmedLog");
        Fields("presentation", "_renderHistory", "_renderTicks", "_renderCounts", "_visual", "_oldPredicted", "_offsets", "_oldSpecs", "_oldRotations");
        Fields("wire-authoring", "_pendingState", "_send", "_physicsBytes", "_manifest", "_held", "_appliedTicks");
        Fields("timing-samples", "StepSamples", "CorrectionSamples");
        for (var i = 0; i < checkpoints.Length; i++)
        {
            var state = checkpoints[i].ReadObjectField("_state"); Add($"checkpoint-{i}", checkpoints[i], state);
            var solver = state.ReadObjectField("_solver");
            if (!solver.IsNull) Add($"checkpoint-{i}-solver", solver.Type?.Name == "Electron2D.PhysicsSpace+GPUCheckpoint" ? [solver, solver.ReadObjectField("_kernel")] : [solver]);
        }
        Add("simulation-residual", simulation); Add("session-residual", session);
        var boundaries = groups.SelectMany(g => g.Roots).Select(o => o.Address).ToHashSet();
        var seen = new HashSet<ulong>(); var owners = new List<object>();
        foreach (var group in groups)
        {
            var cuts = new HashSet<ulong>(boundaries); foreach (var root in group.Roots) cuts.Remove(root.Address);
            var objects = new List<ClrObject>(); foreach (var root in group.Roots) objects.AddRange(Walk(root, cuts, seen));
            owners.Add(new
            {
                group.Name,
                Objects = objects.Count,
                Bytes = objects.Sum(o => checked((long)o.Size)),
                Arrays = group.Roots.Where(o => o.IsArray).Select(o => new { Type = o.Type!.Name, Length = o.AsArray().Length, Bytes = o.Size }).ToArray(),
                Types = objects.GroupBy(o => o.Type!.Name!).Select(g => new { Type = g.Key, Count = g.Count(), Bytes = g.Sum(o => checked((long)o.Size)) }).OrderByDescending(x => x.Bytes).ToArray()
            });
        }
        if (!seen.SetEquals(reachable)) throw new InvalidDataException("Scene/network owner partition does not equal the full session graph.");
        var store = full.SingleOrDefault(o => o.Type?.Name == "Electron2D.GPUPhysicsBodyStore");
        var gpu = store.IsNull ? null : new
        {
            DeviceBytes = store.ReadField<long>("_deviceBufferBytes"),
            TransferBytes = store.ReadField<long>("_transferBufferBytes"),
            PeakBytes = store.ReadField<long>("_peakBufferBytes")
        };
        if (phase == "disposed-held" && (spaces.Any(o => !o.ReadField<bool>("_disposed")) || gpu is { DeviceBytes: not 0 } or { TransferBytes: not 0 }))
            throw new InvalidDataException("Disposed scene still owns live physics or requested GPU buffers.");
        var packet = session.ReadObjectField("_snapshot");
        return new
        {
            CalibrationBytes = calibrationBytes,
            ManagedObjectBytes = full.Sum(o => checked((long)o.Size)),
            CPUWorldPool = CPUWorldPool(runtime, reachable),
            Owners = owners,
            Nodes = nodes.Count,
            PhysicsSpaces = spaces.Length,
            DisposedSpaces = spaces.Count(o => o.ReadField<bool>("_disposed")),
            SnapshotLength = packet.ReadField<int>("_length"),
            SnapshotCapacity = packet.ReadObjectField("_data").AsArray().Length,
            RequestedGPU = gpu,
            PacketQueue = InspectQueue(session.ReadObjectField("_link"))
        };

        void Add(string name, params ClrObject[] roots) { groups.Add((name, roots.Where(o => !o.IsNull).ToList())); }
        void Fields(string name, params string[] fields) => Add(name, fields.Select(field => session.ReadObjectField(field)).ToArray());
    }

    private static object InspectQueue(ClrObject link)
    {
        string[] fields = ["_storage", "_due", "_order", "_sizes", "_peers", "_modes"];
        var arrays = fields.Select(name =>
        {
            var value = link.ReadObjectField(name);
            return new { Field = name, Type = value.Type!.Name, Length = value.AsArray().Length, Bytes = value.Size };
        }).ToArray();
        var slots = link.ReadObjectField("_sizes").AsArray();
        var sizes = slots.ReadValues<int>(0, slots.Length) ?? throw new InvalidDataException("Queue slot payloads could not be read.");
        var storage = arrays.Single(a => a.Field == "_storage");
        var capacity = slots.Length; var packetBytes = storage.Length / capacity;
        var pending = link.ReadField<int>("PendingPackets"); var bytes = link.ReadField<long>("PendingBytes");
        var peak = link.ReadField<int>("PeakPendingPackets"); var peakBytes = link.ReadField<long>("PeakPendingBytes");
        if (storage.Length % capacity != 0 || arrays.Any(a => a.Field != "_storage" && a.Length != capacity) ||
            sizes.Any(size => size < 0 || size > packetBytes) || pending != sizes.Count(size => size != 0) || bytes != sizes.Sum(size => (long)size) ||
            peak < pending || peak > capacity || peakBytes < bytes || peakBytes > storage.Length)
            throw new InvalidDataException("Packet queue occupancy, high water or retained capacities disagree.");
        return new
        {
            Capacity = capacity,
            PacketBytes = packetBytes,
            PendingPackets = pending,
            PendingBytes = bytes,
            PeakPendingPackets = peak,
            PeakPendingBytes = peakBytes,
            WrapperBytes = link.Size,
            Arrays = arrays,
            WrapperAndArrayBytes = checked((long)link.Size + arrays.Sum(a => checked((long)a.Bytes)))
        };
    }

    private static bool IsDerived(ClrType? type, string baseName)
    {
        for (; type is not null; type = type.BaseType) if (type.Name == baseName) return true;
        return false;
    }
}
