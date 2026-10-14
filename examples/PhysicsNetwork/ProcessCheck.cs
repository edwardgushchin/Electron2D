using System.Diagnostics;
using System.Text.Json;

namespace Electron2D.Examples.PhysicsNetwork;

internal static class ProcessCheck
{
    internal static async Task<int> Run(string[] args)
    {
        ProtocolChecks.Run();
        string Value(string name, string fallback)
        {
            var at = Array.IndexOf(args, name);
            if (at < 0) return fallback;
            if (at + 1 == args.Length) throw new ArgumentException("Missing value for " + name);
            return args[at + 1];
        }
        var windowed = args.Contains("--window-client");
        var clientBackend = Value("--client-backend", "gpu");
        if (clientBackend is not ("cpu" or "gpu")) throw new ArgumentException("Client backend must be cpu or gpu.");
        if (windowed && clientBackend == "cpu") throw new ArgumentException("The CPU-only process check requires headless clients.");
        var threaded = args.Contains("--threaded");
        var primary = clientBackend;
        var ticks = int.Parse(Value("--ticks", "360"));
        if (ticks is < 360 or > 1800) throw new ArgumentException("Tick count must be between 360 and 1800.");
        var lateJoinTick = int.Parse(Value("--late-join-tick", "90"));
        if (lateJoinTick < 90 || lateJoinTick >= ticks) throw new ArgumentException("Late join must be between tick 90 and the final tick.");
        var renderer = Value("--renderer", "gpu");
        if (renderer is not ("gpu" or "compatibility")) throw new ArgumentException("Renderer must be gpu or compatibility.");
        var output = System.IO.Path.GetFullPath(Value("--output", "bin/physics-network-check")); Directory.CreateDirectory(output);
        var token = Guid.NewGuid().ToString();
        var children = new List<Process>(); var drains = new List<Task>();
        try
        {
            var server = Start("server", 0, "cpu", headless: true); children.Add(server);
            var ready = await server.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15));
            if (ready is null || !ready.StartsWith("LISTENING ")) throw new InvalidOperationException("Server did not report its listening port: " + ready);
            var lateJoinReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var port = int.Parse(ready[10..]); drains.Add(Drain(server, "server", ready, lateJoinReady));
            var clientProcess = Start(primary, port, clientBackend, headless: clientBackend == "cpu");
            children.Add(clientProcess); drains.Add(Drain(clientProcess, primary, ""));
            await lateJoinReady.Task.WaitAsync(TimeSpan.FromSeconds(30));
            var late = Start("late", port, "cpu", headless: true); children.Add(late); drains.Add(Drain(late, "late", ""));
            foreach (var child in children) await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(40));
            await Task.WhenAll(drains);
            foreach (var child in children) if (child.ExitCode != 0) throw new InvalidOperationException("A child process failed; inspect the saved logs.");
            if (windowed)
            {
                var rendered = Read("gpu");
                Check(rendered.WindowFrames > 0 && rendered.WindowFPS > 0 && rendered.RenderedSmoothedSamples > 0 && rendered.RenderedInterpolatedSamples > 0 &&
                    rendered.RenderedPhysicsPreserved && File.Exists(System.IO.Path.Combine(output, "gpu-window.png")), "Actual networked window renders smoothing/interpolation while preserving portable physics");
            }
            var authority = Read("server"); Check(authority.Role == "server" && authority.Backend == "CPU" && authority.Tick == (ulong)ticks, "CPU server completed all real ticks");
            Check(authority.GPUUnavailableVerified && Read("late").GPUUnavailableVerified && Read(primary).GPUUnavailableVerified == (clientBackend == "cpu"),
                "All CPU processes exercised actual denied GPU startup before their working network worlds");
            foreach (var name in new[] { "server", primary, "late" })
            {
                var report = Read(name);
                Check(report.SeparateThread == threaded, "Every process uses the requested numerical thread policy");
                Check(report.CompletedSteps > report.WarmupSteps && report.WarmSteps + report.WarmReplaySteps <= report.CompletedSteps - report.WarmupSteps,
                    "Every process excludes its own physical warmup before measured samples");
                Check(report.JointSteps == report.CompletedSteps && report.JointKicks > 0 && report.MaxPinError < .5f && report.MaxGrooveError < .5f && report.MaxSpringLength < 130,
                    "Every authority/predicted/replayed step executes finite groove and spring constraints under real impulses");
                if (report.BackendInstrumented && report.Backend == "GPU")
                    Check(report.GPUDriver.Length > 0 && report.GPUDeviceName.Length > 0 && report.GPUSubmissions > 0 && report.GPUUploadBytes > 0 && report.GPUReadbackBytes > 0 && report.GPUWaitMS > 0,
                        "GPU client executed measured device submissions and transfers");
                if (report.BackendInstrumented && report.Backend == "CPU")
                    Check(report.NumericalSteps > 0 && report.NumericalThreadVerified, "Actual CPU intervals run on the requested owner or separate numerical lane");
            }
            Check(authority.Actors.Length == 14 && authority.Actors.Any(actor => actor.ID == 101) && authority.Actors.Any(actor => actor.ID == 102),
                "All three joint families and their endpoints survive authority and late-join topology");
            Check(authority.Actors.Any(actor => Math.Abs(actor.AngularVelocity) > .05),
                "Final authority state carries a real nonzero angular response for cross-backend restoration");
            Check(authority.InvalidHello >= 2 && authority.GenerationCommands >= 2, "Invalid session tokens and stale incarnations are rejected");
            Check(authority.AppliedCommands > 180, "Authority actually applies numbered player inputs");
            Check(authority.WarmSteps > 0 && authority.WarmStepAllocations == 0 && authority.WarmStepTotalAllocations == 0, "Prepared authority steps allocate zero bytes on owner/all threads");
            Check(authority.ForeignCommands >= 2, "Authority rejects attempts to control unowned bodies");
            Check(authority.EventSequence > 0 && authority.Removed > 0 && authority.SleepObservations > 0 && authority.WakeObservations > 0, "Authority exercises contacts, lifecycle and sleep");
            foreach (var name in new[] { primary, "late" })
            {
                var client = Read(name); Check(client.Tick == authority.Tick && client.Corrections > 5 && client.ReplayedTicks > 0, "Client corrects and replays actual physics");
                Check(client.EventSequence == authority.EventSequence && (ulong)client.ConfirmedEffects == client.EventSequence - client.EventBaseline, "Confirmed effects appear exactly once");
                Check(client.Dropped > 0 && client.Duplicated > 0 && client.Reordered > 0 && client.StalePackets > 0, "Adverse application delivery was exercised");
                Check(client.Events.SequenceEqual(authority.Events.Where(item => item.Sequence > client.EventBaseline)), "Confirmed event identities and physical contents match authority");
                Check(client.SleepObservations > 0 && client.WakeObservations > 0, "Client observes restored sleep and waking");
                Check(client.InterpolatedSamples > 0 && client.SmoothedSamples > 0 && client.PostDriftCorrections > 0 && client.DriftCorrectionDistance > 10, "Remote interpolation, local smoothing and injected divergence recovery execute");
                Check(client.AngularDriftCorrection > .1, "Injected rotation/angular-velocity divergence is corrected through real authority state");
                Check(client.WarmSteps > 0 && client.WarmReplaySteps > 0, "Allocation sample includes real warmed predicted and replayed ticks");
                Check(client.WarmStepAllocations == 0 && client.WarmReplayAllocations == 0 && client.WarmStepTotalAllocations == 0 && client.WarmReplayTotalAllocations == 0, "Prepared predicted and replayed physics steps allocate zero managed bytes");
                Check(client.MalformedPackets == 0 && client.MaxVisualOffset > 0, "Valid wire traffic and separate presentation smoothing");
                Check(client.Actors.Length == authority.Actors.Length, "Late-join/lifecycle topology converges");
                for (var i = 0; i < authority.Actors.Length; i++)
                {
                    var a = authority.Actors[i]; var b = client.Actors[i];
                    Check(a.ID == b.ID && a.Generation == b.Generation && a.Owner == b.Owner && a.Epoch == b.Epoch, "Portable identities and control epochs agree");
                    Check(Math.Abs(a.X - b.X) < .002 && Math.Abs(a.Y - b.Y) < .002 && Math.Abs(a.VX - b.VX) < .002 && Math.Abs(a.VY - b.VY) < .002 && Math.Abs(Electron2D.Mathf.AngleDifference(a.Rotation, b.Rotation)) < .002 &&
                        Math.Abs(a.AngularVelocity - b.AngularVelocity) < .002 && a.Sleeping == b.Sleeping, "Final authoritative correction converges");
                }
            }
            Check(Read(primary).Backend == (clientBackend == "gpu" ? "GPU" : "CPU") && Read(primary).JoinTick < Read("late").JoinTick && Read("late").JoinTick >= (ulong)lateJoinTick,
                "Explicit client backend and real late join");
            Console.WriteLine($"PASS: separate CPU authority, {clientBackend.ToUpperInvariant()} prediction, late CPU client, threaded={threaded}, impaired packets, lifecycle, ownership, event confirmation and final convergence."); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally { foreach (var child in children) { if (!child.HasExited) child.Kill(entireProcessTree: true); child.Dispose(); } }
        Process Start(string role, int port, string backend, bool headless)
        {
            var command = Environment.ProcessPath!; var info = new ProcessStartInfo(command) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            if (System.IO.Path.GetFileNameWithoutExtension(command) == "dotnet") info.ArgumentList.Add(typeof(ProcessCheck).Assembly.Location);
            info.ArgumentList.Add("--physics-network");
            if (threaded) info.ArgumentList.Add("--threaded");
            if (role == "server") info.ArgumentList.Add("--server");
            foreach (var argument in new[] { "--port", port.ToString(), "--backend", backend, "--ticks", ticks.ToString(), "--late-join-tick", lateJoinTick.ToString(), "--token", token, "--report", System.IO.Path.Combine(output, role + ".json"), "--impaired" }) info.ArgumentList.Add(argument);
            if (role == "gpu" && windowed)
                foreach (var argument in new[] { "--window", "--renderer", renderer, "--capture", System.IO.Path.Combine(output, "gpu-window.png") }) info.ArgumentList.Add(argument);
            if (headless)
            {
                info.Environment.Remove("DISPLAY"); info.Environment.Remove("WAYLAND_DISPLAY"); info.Environment["SDL_VIDEODRIVER"] = "dummy";
                info.Environment["VK_ICD_FILENAMES"] = "/nonexistent/e2d-no-gpu.json";
                info.Environment["VK_DRIVER_FILES"] = "/nonexistent/e2d-no-gpu.json";
                info.Environment["SDL_GPU_DRIVER"] = "electron2d-no-such-driver";
                info.Environment["XDG_SESSION_TYPE"] = "unspecified";
                info.ArgumentList.Add("--require-no-gpu");
            }
            return Process.Start(info) ?? throw new InvalidOperationException("Could not start child process.");
        }
        async Task Drain(Process process, string role, string first, TaskCompletionSource? lateJoinReady = null)
        {
            var stdout = new System.Text.StringBuilder(first).AppendLine(); var stderr = process.StandardError.ReadToEndAsync();
            while (await process.StandardOutput.ReadLineAsync() is { } line)
            {
                stdout.AppendLine(line);
                if (line.StartsWith("LATE_JOIN_READY ") && ulong.TryParse(line.AsSpan(16), out var tick) && tick >= (ulong)lateJoinTick) lateJoinReady?.TrySetResult();
            }
            lateJoinReady?.TrySetException(new IOException("Authority exited before the late-join tick."));
            await File.WriteAllTextAsync(System.IO.Path.Combine(output, role + ".log"), stdout + "\n" + await stderr);
        }
        SessionReport Read(string name) => JsonSerializer.Deserialize(File.ReadAllText(System.IO.Path.Combine(output, name + ".json")), ReportJSON.Default.SessionReport)!;
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
