using System.Text.Json;
using Electron2D.Tools.Batch;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
var report = new BatchReport { Operation = args.FirstOrDefault() ?? "help" };
string? output = null;
try
{
    if (args.Length < 2) throw new BatchFailure("USAGE", 2, "Use new DIRECTORY --sdk SDK; open DIRECTORY; build DIRECTORY --output OUTPUT; run DIRECTORY --request REQUEST --output OUTPUT.");
    var options = new Dictionary<string, string>(StringComparer.Ordinal);
    for (var i = 2; i < args.Length; i += 2)
        if (i + 1 >= args.Length || !args[i].StartsWith("--", StringComparison.Ordinal) || !options.TryAdd(args[i], args[i + 1]))
            throw new BatchFailure("USAGE", 2, "Options require unique --name value pairs.");
    var allowed = args[0] switch { "new" => new[] { "--sdk" }, "open" => [], "build" => ["--output"], "run" => ["--request", "--output"], _ => throw new BatchFailure("USAGE", 2, "Unknown operation.") };
    if (options.Keys.Any(k => !allowed.Contains(k))) throw new BatchFailure("USAGE", 2, "Unknown option.");
    var project = IOPath.TrimEndingDirectorySeparator(IOPath.GetFullPath(args[1])); report.Project = project;
    if (args[0] == "new")
    {
        if (!options.TryGetValue("--sdk", out var sdk)) throw new BatchFailure("USAGE", 2, "new requires --sdk pointing to the SDK source directory.");
        ProjectOperations.Create(project, IOPath.GetFullPath(sdk)); report.Success = true;
    }
    else
    {
        var manifest = ProjectOperations.Open(project);
        if (args[0] == "open") { report.Success = true; report.Artifacts.Add(new("project", IOPath.Combine(project, "electron2d.project.json"), 0, Protocol.Hash(IOPath.Combine(project, "electron2d.project.json")))); }
        else
        {
            if (!options.TryGetValue("--output", out var directory)) throw new BatchFailure("USAGE", 2, "build and run require a new --output directory.");
            var destination = IOPath.TrimEndingDirectorySeparator(IOPath.GetFullPath(directory));
            if (IOPath.Exists(destination)) throw new BatchFailure("OUTPUT_EXISTS", 2, "The output path must not already exist.");
            var parent = IOPath.GetDirectoryName(destination)!; Directory.CreateDirectory(parent);
            var temporary = IOPath.Combine(parent, ".electron2d-job-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temporary);
            try { Directory.Move(temporary, destination); output = destination; }
            finally { if (Directory.Exists(temporary)) Directory.Delete(temporary); }
            report.Job = Guid.NewGuid().ToString("N");
            BatchRequest? request = null;
            if (args[0] == "run")
            {
                var requestPath = IOPath.GetFullPath(options.GetValueOrDefault("--request", IOPath.Combine(project, "scenario.json")));
                request = Protocol.Read(requestPath, ProtocolJSON.Default.BatchRequest); request.Validate();
                File.Copy(requestPath, IOPath.Combine(output, "request.json"));
                report.Mode = request.Mode; report.FPS = request.FPS; report.StepBudget = request.Steps; report.Seed = request.Seed;
            }
            var cache = Protocol.ChildPath(project, ".electron2d"); Directory.CreateDirectory(cache);
            using var lease = new FileStream(IOPath.Combine(cache, "job.lock"), FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, FileShare.None);
            var app = await ProjectOperations.Build(project, manifest, output, cancellation.Token);
            if (request is null) { report.Success = true; report.Artifacts.Add(new("assembly", app, 0, Protocol.Hash(app))); }
            else
            {
                var exit = await ProcessJob.Run("dotnet", [app, "--electron2d-batch", IOPath.Combine(output, "request.json"), output, report.Job], project, output, "game", request.DeadlineMilliseconds, cancellation.Token);
                var path = IOPath.Combine(output, "run.json");
                if (!File.Exists(path)) throw new BatchFailure("CHILD_FAILED", 4, $"The scenario process exited {exit} without a completed report.");
                var child = Protocol.Read(path, ProtocolJSON.Default.BatchReport);
                if (child.Version != 1 || child.Job != report.Job || child.Mode != request.Mode || child.RequestSHA256 != Protocol.Hash(IOPath.Combine(output, "request.json")) || child.ProjectAssemblySHA256 != Protocol.Hash(app) || child.ExitCode != exit || child.Success != (exit == 0) || child.Artifacts is null || child.Assertions is null || child.Diagnostics is null || child.Observations is null)
                    throw new BatchFailure("INVALID_RESULT", 4, "The scenario report does not match its request, assembly or exit status.");
                report = child;
            }
        }
    }
}
catch (Exception error)
{
    if (error is OperationCanceledException) error = new BatchFailure("CANCELLED", 130, "The controller cancelled the job.");
    else if (error is JsonException or IOException or UnauthorizedAccessException or ArgumentException)
        error = new BatchFailure("INVALID_INPUT", 2, error.Message);
    Protocol.Fail(report, error);
}
if (output is not null && Directory.Exists(output))
{
    try
    {
        foreach (var name in new[] { "build.stdout.log", "build.stderr.log", "target.stdout.log", "target.stderr.log", "game.stdout.log", "game.stderr.log" })
        {
            var log = IOPath.Combine(output, name);
            if (File.Exists(log)) report.Artifacts.Add(new("log", log, 0, Protocol.Hash(log)));
        }
        Protocol.Save(IOPath.Combine(output, "report.json"), report);
    }
    catch (Exception error) { Protocol.Fail(report, new BatchFailure("REPORT_FAILED", 4, error.Message)); }
}
Console.WriteLine(JsonSerializer.Serialize(report, ProtocolJSON.Default.BatchReport));
return report.ExitCode;
