using System.Diagnostics;
using System.Text.Json;

namespace Electron2D.Examples.PhysicsNetwork;

internal static class ProcessCheck
{
    internal static async Task<int> Run(string[] args)
    {
        ProtocolChecks.Run();
        var location = Array.IndexOf(args, "--output");
        var output = System.IO.Path.GetFullPath(location >= 0 ? args[location + 1] : "bin/physics-network-check"); Directory.CreateDirectory(output);
        var token = Guid.NewGuid().ToString();
        var children = new List<Process>(); var drains = new List<Task>();
        try
        {
            var server = Start("server", 0, "cpu", headless: true); children.Add(server);
            var ready = await server.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15));
            if (ready is null || !ready.StartsWith("LISTENING ")) throw new InvalidOperationException("Server did not report its listening port: " + ready);
            var port = int.Parse(ready[10..]); drains.Add(Drain(server, "server", ready));
            var gpu = Start("gpu", port, "gpu", headless: false); children.Add(gpu); drains.Add(Drain(gpu, "gpu", ""));
            await Task.Delay(2500);
            var late = Start("late", port, "cpu", headless: true); children.Add(late); drains.Add(Drain(late, "late", ""));
            foreach (var child in children) await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(40));
            await Task.WhenAll(drains);
            foreach (var child in children) if (child.ExitCode != 0) throw new InvalidOperationException("A child process failed; inspect the saved logs.");
            var authority = Read("server"); Check(authority.Role == "server" && authority.Backend == "CPU" && authority.Tick == 360, "CPU server completed all real ticks");
            Check(authority.InvalidHello >= 2 && authority.GenerationCommands >= 2, "Invalid session tokens and stale incarnations are rejected");
            Check(authority.AppliedCommands > 180, "Authority actually applies numbered player inputs");
            Check(authority.WarmSteps > 0 && authority.WarmStepAllocations == 0 && authority.WarmStepTotalAllocations == 0, "Prepared authority steps allocate zero bytes on owner/all threads");
            Check(authority.ForeignCommands >= 2, "Authority rejects attempts to control unowned bodies");
            Check(authority.EventSequence > 0 && authority.Removed > 0 && authority.SleepObservations > 0 && authority.WakeObservations > 0, "Authority exercises contacts, lifecycle and sleep");
            foreach (var name in new[] { "gpu", "late" })
            {
                var client = Read(name); Check(client.Tick == authority.Tick && client.Corrections > 5 && client.ReplayedTicks > 0, "Client corrects and replays actual physics");
                Check(client.EventSequence == authority.EventSequence && (ulong)client.ConfirmedEffects == client.EventSequence - client.EventBaseline, "Confirmed effects appear exactly once");
                Check(client.Dropped > 0 && client.Duplicated > 0 && client.Reordered > 0 && client.StalePackets > 0, "Adverse application delivery was exercised");
                Check(client.Events.SequenceEqual(authority.Events.Where(item => item.Sequence > client.EventBaseline)), "Confirmed event identities and physical contents match authority");
                Check(client.SleepObservations > 0 && client.WakeObservations > 0, "Client observes restored sleep and waking");
                Check(client.InterpolatedSamples > 0 && client.SmoothedSamples > 0 && client.PostDriftCorrections > 0 && client.DriftCorrectionDistance > 10, "Remote interpolation, local smoothing and injected divergence recovery execute");
                Check(client.WarmSteps > 0 && client.WarmReplaySteps > 0, "Allocation sample includes real warmed predicted and replayed ticks");
                Check(client.WarmStepAllocations == 0 && client.WarmReplayAllocations == 0 && client.WarmStepTotalAllocations == 0 && client.WarmReplayTotalAllocations == 0, "Prepared predicted and replayed physics steps allocate zero managed bytes");
                Check(client.MalformedPackets == 0 && client.MaxVisualOffset > 0, "Valid wire traffic and separate presentation smoothing");
                Check(client.Actors.Length == authority.Actors.Length, "Late-join/lifecycle topology converges");
                for (var i = 0; i < authority.Actors.Length; i++)
                {
                    var a = authority.Actors[i]; var b = client.Actors[i];
                    Check(a.ID == b.ID && a.Generation == b.Generation && a.Owner == b.Owner && a.Epoch == b.Epoch, "Portable identities and control epochs agree");
                    Check(Math.Abs(a.X - b.X) < .002 && Math.Abs(a.Y - b.Y) < .002 && Math.Abs(a.VX - b.VX) < .002 && Math.Abs(a.VY - b.VY) < .002 && a.Sleeping == b.Sleeping, "Final authoritative correction converges");
                }
            }
            Check(Read("gpu").Backend == "GPU" && Read("late").JoinTick > 60, "Independent GPU client and real late join");
            Console.WriteLine("PASS: separate CPU authority, GPU prediction, late CPU client, impaired packets, lifecycle, ownership, event confirmation and final convergence."); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally { foreach (var child in children) { if (!child.HasExited) child.Kill(entireProcessTree: true); child.Dispose(); } }
        Process Start(string role, int port, string backend, bool headless)
        {
            var command = Environment.ProcessPath!; var info = new ProcessStartInfo(command) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            if (System.IO.Path.GetFileNameWithoutExtension(command) == "dotnet") info.ArgumentList.Add(typeof(ProcessCheck).Assembly.Location);
            info.ArgumentList.Add("--physics-network");
            if (role == "server") info.ArgumentList.Add("--server");
            foreach (var argument in new[] { "--port", port.ToString(), "--backend", backend, "--ticks", "360", "--token", token, "--report", System.IO.Path.Combine(output, role + ".json"), "--impaired" }) info.ArgumentList.Add(argument);
            if (headless)
            {
                info.Environment.Remove("DISPLAY"); info.Environment.Remove("WAYLAND_DISPLAY"); info.Environment["SDL_VIDEODRIVER"] = "dummy";
                info.Environment["VK_ICD_FILENAMES"] = "/nonexistent/e2d-no-gpu.json";
            }
            return Process.Start(info) ?? throw new InvalidOperationException("Could not start child process.");
        }
        async Task Drain(Process process, string role, string first)
        {
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(stdout, stderr); await File.WriteAllTextAsync(System.IO.Path.Combine(output, role + ".log"), first + "\n" + stdout.Result + "\n" + stderr.Result);
        }
        SessionReport Read(string name) => JsonSerializer.Deserialize(File.ReadAllText(System.IO.Path.Combine(output, name + ".json")), ReportJSON.Default.SessionReport)!;
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
