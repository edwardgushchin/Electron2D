using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

internal static class NativePhaseProbe
{
    internal static async Task Run(string[] args)
    {
        if (args.Length is not (5 or 7)) throw new ArgumentException("Expected Tests.dll, Fixture.dll, bodies, histories and cycles; optional heaptrack preload and trace directory.");
        var tests = Path.GetFullPath(args[0]); var fixture = Path.GetFullPath(args[1]);
        var bodies = int.Parse(args[2], CultureInfo.InvariantCulture); var histories = int.Parse(args[3], CultureInfo.InvariantCulture); var cycles = int.Parse(args[4], CultureInfo.InvariantCulture);
        var preload = args.Length == 7 ? Path.GetFullPath(args[5]) : null;
        if (preload is not null && (Path.GetFileName(preload) != "libheaptrack_preload.so" || !File.Exists(preload))) throw new ArgumentException("An existing heaptrack preload library is required.");
        if (bodies < 4 || bodies > 65536 || bodies % 4 != 0 || histories is < 1 or > 32 || cycles is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(args));
        var runtime = Hash(Path.Combine(Path.GetDirectoryName(tests)!, "Electron2D.dll"));
        if (runtime != Hash(Path.Combine(Path.GetDirectoryName(fixture)!, "Electron2D.dll"))) throw new InvalidOperationException("Native controls and public worlds must use the same runtime assembly.");
        var profiles = new List<object>();
        foreach (var mode in new[] { "formats", "reflection", "device", "pipelines", "device-pipelines", "world", "world-threaded" })
        {
            var world = mode.StartsWith("world", StringComparison.Ordinal);
            var start = new ProcessStartInfo("dotnet") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
            var trace = preload is null ? null : Path.Combine(Path.GetFullPath(args[6]), mode + ".heaptrack.raw");
            if (trace is not null)
            {
                if (File.Exists(trace)) throw new IOException("The allocation trace already exists.");
                start.Environment["LD_PRELOAD"] = preload;
                start.Environment["DUMP_HEAPTRACK_OUTPUT"] = trace;
            }
            start.ArgumentList.Add(world ? fixture : tests);
            foreach (var argument in world ? new[] { "--retire", "gpu", args[2], args[3], args[4], (mode == "world-threaded").ToString() } : new[] { "--physics-native-memory", mode, args[4] }) start.ArgumentList.Add(argument);
            using var child = Process.Start(start) ?? throw new IOException("Could not start a native-memory fixture.");
            var errors = child.StandardError.ReadToEndAsync(); var samples = new List<object>();
            try
            {
                var phases = new List<string> { "baseline" };
                if (mode == "pipelines") phases.Add("retained-device");
                for (var i = 1; i <= cycles; i++) { if (world) phases.Add($"cycle-{i}-live"); phases.Add($"cycle-{i}-retired"); }
                phases.Add(world ? "settled" : "released");
                foreach (var phase in phases)
                {
                    var line = await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromMinutes(1));
                    if (line is null) throw new IOException("Fixture exited: " + await errors);
                    var metadata = JsonSerializer.Deserialize<JsonElement>(line);
                    if (metadata.GetProperty("Phase").GetString() != phase) throw new InvalidDataException(line);
                    if (world && phase.EndsWith("-retired", StringComparison.Ordinal))
                    {
                        var cycle = metadata.GetProperty("Cycle").GetInt32();
                        if (metadata.GetProperty("Bodies").GetInt32() != 0 || metadata.GetProperty("Checkpoints").GetInt32() != 0 || metadata.GetProperty("FailedStepVerified").GetBoolean() != (cycle % 2 == 0))
                            throw new InvalidDataException("The complete world did not verify retirement and its numerical failure boundary.");
                    }
                    var maps = File.ReadAllText($"/proc/{child.Id}/maps"); var lavapipe = maps.Contains("libvulkan_lvp.so", StringComparison.Ordinal);
                    if (maps.Contains("nvidia", StringComparison.OrdinalIgnoreCase) || ((mode is "formats" or "reflection") && lavapipe) ||
                        ((phase == "retained-device" || world && phase.EndsWith("-live", StringComparison.Ordinal)) && !lavapipe))
                        throw new InvalidOperationException("Unexpected native driver mapping.");
                    samples.Add(new { Metadata = metadata, Process = ProcessObservation.Read(child.Id, mappings: true), LavapipeMapped = lavapipe, NVIDIAMapped = false });
                    Console.Error.WriteLine($"Measured native {mode}/{phase}.");
                    await child.StandardInput.WriteLineAsync("next"); await child.StandardInput.FlushAsync();
                }
                await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
                if (child.ExitCode != 0) throw new IOException("Fixture failed: " + await errors);
            }
            finally { if (!child.HasExited) { child.Kill(entireProcessTree: true); await child.WaitForExitAsync(); } }
            if (trace is not null && (!File.Exists(trace) || new FileInfo(trace).Length == 0)) throw new IOException("The child produced no allocation trace.");
            profiles.Add(new { Mode = mode, Samples = samples, StandardError = await errors, AllocationTrace = trace is null ? null : new { Path = trace, Bytes = new FileInfo(trace).Length, SHA256 = Hash(trace) } });
        }
        Console.WriteLine(JsonSerializer.Serialize(new { TestsSHA256 = Hash(tests), FixtureSHA256 = Hash(fixture), RuntimeSHA256 = runtime, HeapAttached = false, HeaptrackPreloadSHA256 = preload is null ? null : Hash(preload), Cycles = cycles, Bodies = bodies, Histories = histories, Profiles = profiles }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }
}
