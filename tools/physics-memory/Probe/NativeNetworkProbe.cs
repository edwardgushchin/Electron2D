using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

internal static class NativeNetworkProbe
{
    private sealed class Observation(int pid)
    {
        public int PID { get; } = pid;
        public string Role { get; set; } = "";
        public string Executable { get; set; } = "";
        public bool CoreCLR { get; set; }
        public bool Lavapipe { get; set; }
        public bool Heaptrack { get; set; }
        public bool PhaseMarkers { get; set; }
        public int Samples { get; set; }
    }

    internal static async Task Run(string[] args)
    {
        if (args.Length < 2) throw new ArgumentException("Expected a NativeAOT network executable and new output directory.");
        var executable = Path.GetFullPath(args[0]);
        var preload = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_PHYSICS_HEAPTRACK_PRELOAD");
        if (preload is not null && (Path.GetFileName(preload) != "libheaptrack_preload.so" || !File.Exists(preload))) throw new ArgumentException("An existing heaptrack preload library is required.");
        var collectorMaps = File.ReadAllText($"/proc/{Environment.ProcessId}/maps");
        if (collectorMaps.Contains("libheaptrack_preload.so", StringComparison.Ordinal) || collectorMaps.Contains("libElectron2DMemoryPhases.so", StringComparison.Ordinal)) throw new InvalidOperationException("The collector must remain unprofiled and unmarked.");
        var folder = Path.GetFullPath(args[1]);
        if (Directory.Exists(folder)) throw new IOException("The native network output directory must be new.");
        Directory.CreateDirectory(folder);
        var traces = Path.Combine(folder, "allocation-traces");
        if (preload is not null) Directory.CreateDirectory(traces);
        var start = new ProcessStartInfo(executable) { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "--check", "--output", Path.Combine(folder, "sessions") }.Concat(args.Skip(2))) start.ArgumentList.Add(argument);
        if (preload is not null) start.Environment["ELECTRON2D_TEST_PHYSICS_HEAPTRACK_OUTPUT"] = traces;
        using var check = Process.Start(start) ?? throw new IOException("Could not start the native network check.");
        var stdout = check.StandardOutput.ReadToEndAsync(); var stderr = check.StandardError.ReadToEndAsync();
        var observations = new Dictionary<int, Observation>(); var timer = Stopwatch.StartNew();
        try
        {
            while (!check.HasExited)
            {
                if (timer.Elapsed.TotalSeconds > 100) throw new TimeoutException("The native network check exceeded its deadline.");
                foreach (var path in Directory.EnumerateDirectories("/proc"))
                {
                    if (!int.TryParse(Path.GetFileName(path), out var pid)) continue;
                    string maps, command, image;
                    try
                    {
                        if (pid != check.Id && !NetworkProbe.IsDescendant(pid, check.Id)) continue;
                        maps = File.ReadAllText(path + "/maps"); command = File.ReadAllText(path + "/cmdline");
                        image = File.ResolveLinkTarget(path + "/exe", returnFinalTarget: true)?.FullName ?? "";
                    }
                    catch (Exception error) when (error is IOException or UnauthorizedAccessException) { continue; }
                    if (maps.Length == 0 || command.Length == 0) continue;
                    if (maps.Contains("nvidia", StringComparison.OrdinalIgnoreCase) || maps.Contains("libvulkan_radeon.so", StringComparison.Ordinal) || maps.Contains("libvulkan_intel.so", StringComparison.Ordinal)) throw new InvalidOperationException("A hardware driver entered the native network check.");
                    var arguments = command.Split('\0', StringSplitOptions.RemoveEmptyEntries); var report = Array.IndexOf(arguments, "--report");
                    var role = pid == check.Id ? "launcher" : report >= 0 && report + 1 < arguments.Length ? Path.GetFileNameWithoutExtension(arguments[report + 1]) : "";
                    if (!observations.TryGetValue(pid, out var row)) observations[pid] = row = new(pid);
                    row.Role = role; row.Executable = image; row.Samples++;
                    row.CoreCLR |= maps.Contains("libcoreclr.so", StringComparison.Ordinal);
                    row.Lavapipe |= maps.Contains("libvulkan_lvp.so", StringComparison.Ordinal);
                    row.Heaptrack |= maps.Contains("libheaptrack_preload.so", StringComparison.Ordinal);
                    row.PhaseMarkers |= maps.Contains("libElectron2DMemoryPhases.so", StringComparison.Ordinal);
                    if (row.CoreCLR || row.PhaseMarkers || row.Executable != executable || role == "launcher" && row.Heaptrack) throw new InvalidOperationException("Unexpected native network runtime, executable or instrumentation mapping.");
                }
                await Task.Delay(20);
            }
            File.WriteAllText(Path.Combine(folder, "check.stdout.log"), await stdout);
            File.WriteAllText(Path.Combine(folder, "check.stderr.log"), await stderr);
            if (check.ExitCode != 0) throw new InvalidOperationException("The full native network check failed; inspect the saved logs.");
            var backendIndex = Array.IndexOf(args, "--client-backend");
            var primary = backendIndex >= 0 ? args[backendIndex + 1] : "gpu";
            var roles = new[] { "launcher", "server", primary, "late" };
            if (observations.Count != 4 || !observations.Values.Select(row => row.Role).Order().SequenceEqual(roles.Order())) throw new InvalidDataException("The four native network processes were not all observed.");
            var allocations = new List<object>();
            foreach (var row in observations.Values)
            {
                if (row.Lavapipe != (row.Role == "gpu") || row.Heaptrack != (preload is not null && row.Role != "launcher")) throw new InvalidDataException("A native network role has the wrong driver/profiler mappings.");
                if (preload is null || row.Role == "launcher") continue;
                var trace = Path.Combine(traces, row.Role + ".heaptrack.raw");
                if (!File.Exists(trace) || new FileInfo(trace).Length == 0) throw new IOException("A native network allocation trace is absent.");
                allocations.Add(new { row.Role, row.PID, Path = trace, Bytes = new FileInfo(trace).Length, SHA256 = Hash(trace) });
            }
            File.WriteAllText(Path.Combine(folder, "collection.json"), JsonSerializer.Serialize(new
            {
                HeapAttached = false,
                LifetimeOnly = true,
                Measurement = (object?)null,
                ExecutableSHA256 = Hash(executable),
                HeaptrackPreloadSHA256 = preload is null ? null : Hash(preload),
                Processes = observations.Values,
                AllocationTraces = allocations
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally { if (!check.HasExited) { check.Kill(entireProcessTree: true); await check.WaitForExitAsync(); } }
    }

    private static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
}
