using System.Diagnostics;
using System.Text.Json;
using Electron2D;
using Electron2D.Editor;
using IOPath = System.IO.Path;

internal static class JobContracts
{
    private static int _checks;
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); _checks++; }
    private static void Reject(Action action, string message)
    {
        try { action(); } catch (Exception) { _checks++; return; }
        throw new InvalidOperationException(message);
    }
    internal static int Run(string output, bool inputOnly = false)
    {
        Directory.CreateDirectory(output);
        var project = SessionProject.Create(IOPath.Combine(output, "Game"), AppContext.BaseDirectory);
        using (var owner = project.OpenSession())
        using (var human = owner.Connect("human"))
        using (var agent = owner.Connect("agent"))
        {
            var original = human.InspectCode().Text;
            var observed = human.InspectCode(); SessionCodeEdit? pending = null; var rejected = false;
            using (var binding = human.BindCodeBuffer(() => { var value = pending; pending = null; return value; }, (_, _) => rejected = true))
            {
                Reject(() => agent.BindCodeBuffer(() => null), "A second code ingress was admitted.");
                pending = new(observed.ID, observed.Revision, "// pending manual input\n" + original);
                Check(agent.InspectCode().Text.StartsWith("// pending manual input", StringComparison.Ordinal), "Agent query missed pending manual text.");
                var current = agent.InspectCode(); binding.Flush(); Check(agent.InspectCode().Revision == current.Revision, "Empty ingress changed a revision.");
                pending = new(observed.ID, observed.Revision, "stale");
                Reject(() => agent.InspectScene(), "Stale pending input overwrote newer code.");
                Check(rejected && agent.InspectCode().Text == current.Text, "Rejected draft lost the committed source.");
            }
            using (human.BindCodeBuffer(() => { agent.Apply("forbidden reader mutation", new SessionCodeEdit(observed.ID, observed.Revision, "bad")); return null; }))
                Reject(() => agent.InspectCode(), "Code reader reentered a mutation.");
            SetCode(human, original);
            var options = new SessionJobOptions(SessionJobKind.Run, project.ProjectFile, AppContext.BaseDirectory, Steps: 2);
            Reject(() => Start(human, options with { Steps = 0 }), "Zero step budget accepted.");
            Reject(() => Start(human, options with { Inputs = [new(1, Key: Key.None)] }), "Invalid key accepted.");
            Reject(() => Start(human, options with { Mode = SessionRunMode.Rendered, Width = 4096, Height = 4096, CaptureSteps = [1, 2, 3, 4, 5], Steps = 5 }), "Oversized captures accepted.");
            File.CreateSymbolicLink(IOPath.Combine(project.Root, "unsafe.cs"), IOPath.Combine(project.Root, project.CodeFile));
            try { Reject(() => Start(human, options), "Source symbolic link accepted."); } finally { File.Delete(IOPath.Combine(project.Root, "unsafe.cs")); }
            Reject(() => Start(human, options with { Inputs = Enumerable.Repeat(new SessionInput(1, Action: new string('a', 256)), 5000).ToArray() }), "Oversized encoded input request accepted.");
            if (inputOnly) { Console.WriteLine($"PASS {_checks} pending-input and admission contracts."); return 0; }
            Case("build", original, options with { Kind = SessionJobKind.Build }, 0);
            Case("compile-error", "this is not valid C#", options, 3);
            // A compiling failure in BeforeStep must preserve snapshot attribution and its original diagnostic.
            Case("before-step-failure", original.Replace("    protected override void AfterStep", "    protected override void BeforeStep(SessionRunContext context) => throw new InvalidOperationException(\"fixture callback failure\");\n    protected override void AfterStep", StringComparison.Ordinal), options, 4, "RUNTIME_FAILED");
            Case("caught-assertion", original.Replace("context.Check(\"finite-position\", float.IsFinite(square.Position.X), \"The snapshot pose must remain finite.\");", "try { context.Check(\"caught\", false, \"sticky failure\"); } catch { }", StringComparison.Ordinal), options, 5);
            Case("missing-action", original, options with { Inputs = [new(1, Action: "unregistered-fixture-action")] }, 2, "INVALID_INPUT");
            Case("no-complete", original.Replace("context.Complete();", "context.Observe(\"unfinished\", true);", StringComparison.Ordinal), options, 6, "BUDGET_EXHAUSTED");
            var marker = IOPath.Combine(output, "cancel-pids"); SetCode(human, HangingSource(original, marker));
            var cancelled = Start(human, options); WaitFor(() => File.Exists(marker + ".shell"), 30000, "Run did not start before cancellation.");
            Reject(() => Start(human, options), "Concurrent session job accepted.");
            cancelled.Cancel(); Finish(cancelled, "cancel", 130, "CANCELLED"); CheckDead(marker);
            marker = IOPath.Combine(output, "timeout-pids"); SetCode(human, HangingSource(original, marker));
            var timed = Start(human, options with { DeadlineMilliseconds = 45000 }); Finish(timed, "deadline", 124, "DEADLINE_EXCEEDED");
            Check(File.Exists(marker + ".shell"), "Deadline did not exercise a nonreturning callback."); CheckDead(marker);
            SetCode(human, original);
            var frozen = Start(human, options);
            WaitFor(() => frozen.Stage == "Building snapshot" || frozen.Completion.IsCompleted, 30000, "Snapshot never materialized.");
            Check(!frozen.Completion.IsCompleted, "Snapshot fixture completed before mutation.");
            var frozenCode = IOPath.Combine(frozen.OutputPath, "input", project.CodeFile); File.SetAttributes(frozenCode, FileAttributes.Normal); File.AppendAllText(frozenCode, "\n// changed frozen input");
            Finish(frozen, "frozen-change", 4, "SNAPSHOT_CHANGED");
            var csproj = IOPath.Combine(project.Root, project.ProjectFile); var xml = File.ReadAllText(csproj);
            File.WriteAllText(csproj, xml.Replace("net10.0", "net8.0", StringComparison.Ordinal));
            try { Case("unsupported-profile", original, options, 7, "PROJECT_PROFILE_UNAVAILABLE"); } finally { File.WriteAllText(csproj, xml); }
            SetCode(human, original);

            void Case(string name, string code, SessionJobOptions settings, int exit, string? diagnostic = null)
            { SetCode(human, code); Finish(Start(human, settings), name, exit, diagnostic); }
        }
        // A killed authoring owner cannot leave the compiler/game session alive or retain the document lease.
        var death = IOPath.Combine(output, "owner-death-pids");
        using var unrelated = Process.Start("/bin/sleep", "120")!;
        try
        {
            var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var arg in new[] { typeof(JobContracts).Assembly.Location, "--owner-child", project.Root, death }) start.ArgumentList.Add(arg);
            using var child = Process.Start(start)!;
            try
            {
                WaitFor(() => File.Exists(death + ".shell") || child.HasExited, 30000, "Owner fixture did not launch its run.");
                if (child.HasExited) throw new InvalidOperationException(child.StandardError.ReadToEnd() + child.StandardOutput.ReadToEnd());
                child.Kill(); child.WaitForExit(); CheckDead(death);
                using var reopened = project.OpenSession(); using var client = reopened.Connect("reopened");
                Check(client.InspectCode().Text.Contains("fixture owner", StringComparison.Ordinal), "Unsaved code was not recovered after owner death.");
                Check(!unrelated.HasExited, "Owned cleanup killed an unrelated process.");
            }
            finally { if (!child.HasExited) { child.Kill(); child.WaitForExit(); } }
        }
        finally { if (!unrelated.HasExited) { unrelated.Kill(); unrelated.WaitForExit(); } }
        Console.WriteLine($"PASS {_checks} job/input/process contracts."); return 0;

        void Finish(SessionJob job, string name, int expectedExit, string? diagnostic = null)
        {
            var result = job.Completion.GetAwaiter().GetResult();
            Console.WriteLine($"{name}: {result.Status} exit={result.ExitCode} {string.Join(',', result.Diagnostics.Select(d => d.Code))}");
            Check(result.ExitCode == expectedExit && result.Status == (expectedExit == 0 ? SessionJobStatus.Completed : expectedExit == 130 ? SessionJobStatus.Cancelled : SessionJobStatus.Failed), name + " returned an unexpected outcome.");
            if (diagnostic is not null) Check(result.Diagnostics.Any(d => d.Code == diagnostic), name + " lost its original structured diagnostic.");
            var evidence = IOPath.Combine(output, name); Directory.CreateDirectory(evidence);
            foreach (var file in Directory.EnumerateFiles(job.OutputPath)) File.Copy(file, IOPath.Combine(evidence, IOPath.GetFileName(file)), true);
            job.DisposeAsync().AsTask().GetAwaiter().GetResult(); job.RemoveResult(); Check(!Directory.Exists(job.OutputPath), "Completed result payload was not removed.");
        }
    }
    internal static int OwnerChild(string root, string marker)
    {
        var project = SessionProject.Open(root); using var owner = project.OpenSession(); using var client = owner.Connect("fixture owner");
        SetCode(client, "// fixture owner\n" + HangingSource(client.InspectCode().Text, marker));
        var job = Start(client, new(SessionJobKind.Run, project.ProjectFile, AppContext.BaseDirectory, Steps: 2));
        var result = job.Completion.GetAwaiter().GetResult(); Console.Error.WriteLine(JsonSerializer.Serialize(result)); return 1;
    }
    private static SessionJob Start(SessionClient client, SessionJobOptions options) => client.StartJob(options, client.InspectScene().Revision, client.InspectCode().Revision);
    private static void SetCode(SessionClient client, string text) { var source = client.InspectCode(); client.Apply("fixture source", new SessionCodeEdit(source.ID, source.Revision, text)); }
    private static string HangingSource(string source, string marker) => source.Replace("    protected override void AfterStep", $$"""
            protected override void BeforeStep(SessionRunContext context)
            {
                System.IO.File.WriteAllText({{JsonSerializer.Serialize(marker)}}, Environment.ProcessId.ToString());
                var start = new System.Diagnostics.ProcessStartInfo("/bin/sh") { UseShellExecute = false };
                start.ArgumentList.Add("-c"); start.ArgumentList.Add("sleep 120 & echo $$ $! > \"$1\"; wait");
                start.ArgumentList.Add("fixture"); start.ArgumentList.Add({{JsonSerializer.Serialize(marker + ".shell")}});
                System.Diagnostics.Process.Start(start);
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            }
            protected override void AfterStep
        """, StringComparison.Ordinal);
    private static void CheckDead(string marker)
    {
        var pids = (File.ReadAllText(marker) + " " + File.ReadAllText(marker + ".shell")).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
        WaitFor(() => pids.All(pid => !Live(pid)), 10000, "Owned game/child/grandchild survived process retirement.");
        Check(pids.Length == 3, "The process fixture did not exercise a grandchild.");
    }
    private static bool Live(int pid)
    {
        try { var stat = File.ReadAllText($"/proc/{pid}/stat"); return stat[(stat.LastIndexOf(')') + 2)..][0] != 'Z'; }
        catch (FileNotFoundException) { return false; } catch (DirectoryNotFoundException) { return false; }
    }
    private static void WaitFor(Func<bool> ready, int milliseconds, string message)
    {
        var elapsed = Stopwatch.StartNew(); while (!ready()) { if (elapsed.ElapsedMilliseconds > milliseconds) throw new TimeoutException(message); Thread.Sleep(20); }
    }
}
