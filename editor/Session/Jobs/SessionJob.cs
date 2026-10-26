using System.Text.Json;
using System.Text.RegularExpressions;

namespace Electron2D.Editor;

/// <summary>Owns compilation/execution of one immutable input without waiting on the authoring thread.</summary>
/// <remarks>The job executes trusted C#/MSBuild with ordinary process permissions, not a sandbox. Cancellation
/// terminates its Linux process group, including inherited workers. Inspect terminal status and diagnostics;
/// process start is not completion. Generated result/input files remain caller-owned evidence.</remarks>
public sealed class SessionJob : IAsyncDisposable
{
    private readonly ProjectSession _owner;
    private readonly CapturedProject _captured;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly CancellationTokenSource _timeout;
    private bool _disposed;
    private readonly object _gate = new();
    private SessionJobStatus _status = SessionJobStatus.Queued;
    private string _stage = "Queued";
    private SessionSnapshotInfo? _snapshot;
    internal long Sequence { get; }
    internal SessionJob(ProjectSession owner, CapturedProject captured, string output, long sequence)
    {
        _owner = owner; _captured = captured; OutputPath = output; Sequence = sequence;
        _timeout = new CancellationTokenSource(captured.Options.DeadlineMilliseconds);
        ID = Guid.ParseExact(IOPath.GetFileName(output), "N"); SnapshotID = Guid.NewGuid();
        File.WriteAllText(IOPath.Combine(output, "job.owner"), ID.ToString("N"));
        Completion = Task.Run(Execute);
    }
    /// <summary>Gets the unique owned job identity.</summary>
    public Guid ID { get; }
    /// <summary>Gets the input snapshot identity, assigned before materialization.</summary>
    public Guid SnapshotID { get; }
    /// <summary>Gets the project-owned result/input directory.</summary>
    public string OutputPath { get; }
    /// <summary>Gets current lifecycle state; it is safe to poll from any thread.</summary>
    public SessionJobStatus Status { get { lock (_gate) return _status; } }
    /// <summary>Gets a readable current stage, independent of terminal success.</summary>
    public string Stage { get { lock (_gate) return _stage; } }
    /// <summary>Gets the frozen input provenance once materialization completes.</summary>
    public SessionSnapshotInfo? Snapshot { get { lock (_gate) return _snapshot; } }
    /// <summary>Gets the terminal result; failures/cancellation are explicit result states.</summary>
    public Task<SessionJobResult> Completion { get; }
    /// <summary>Reports changed live document tokens on the session owner thread.</summary>
    public bool IsStale => _owner.IsSnapshotStale(_captured.SceneRevision, _captured.CodeRevision);
    /// <summary>Requests cancellation at the next snapshot/process boundary.</summary>
    public void Cancel() { lock (_gate) { if (!_disposed) _cancellation.Cancel(); } }

    private void SetStage(string stage) { lock (_gate) { _status = SessionJobStatus.Running; _stage = stage; } }
    private async Task<SessionJobResult> Execute()
    {
        var report = new RunReport
        {
            Operation = _captured.Options.Kind.ToString(),
            Job = ID.ToString("N"),
            Project = _captured.Root,
            SnapshotID = SnapshotID,
            SceneRevision = _captured.SceneRevision,
            CodeRevision = _captured.CodeRevision
        };
        var input = IOPath.Combine(OutputPath, "input");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token, _timeout.Token);
        try
        {
            SetStage("Freezing inputs");
            var exit = await ProcessJob.Run("dotnet", ["--version"], _captured.Root, OutputPath, "sdk-version", _captured.Options.DeadlineMilliseconds, deadline.Token);
            var version = File.ReadAllText(IOPath.Combine(OutputPath, "sdk-version.stdout.log")).Trim();
            if (exit != 0 || !Regex.IsMatch(version, @"^\d+\.\d+\.\d+([-.][a-zA-Z0-9.-]+)?$", RegexOptions.CultureInvariant))
                throw new JobFailure("INVALID_DOTNET_SDK", 3, "The project did not resolve a concrete .NET SDK version.");
            var manifest = SnapshotStore.Materialize(_captured, SnapshotID, input, version, deadline.Token);
            var manifestPath = IOPath.Combine(OutputPath, "snapshot.json");
            SessionStorage.AtomicWrite(manifestPath, JsonSerializer.SerializeToUtf8Bytes(manifest, JobJSON.Default.SnapshotManifest));
            lock (_gate) _snapshot = new(SnapshotID, manifest.SHA256, manifest.SceneRevision, manifest.CodeRevision, Array.AsReadOnly(manifest.Files));
            report.SnapshotSHA256 = manifest.SHA256;
            SetStage("Building snapshot");
            var build = IOPath.Combine(OutputPath, "build");
            var settings = new[] { "-p:UseSharedCompilation=false", "-nr:false", "-p:Configuration=Release", "-p:NuGetAudit=false",
                "-p:ImportDirectoryBuildProps=false", "-p:ImportDirectoryBuildTargets=false", "-p:EnableSourceControlManagerQueries=false",
                "-p:SourceRevisionId=" + manifest.SHA256, "-p:ArtifactsPath=" + build,
                "-p:BaseIntermediateOutputPath=" + IOPath.Combine(build, "obj") + IOPath.DirectorySeparatorChar,
                "-p:MSBuildProjectExtensionsPath=" + IOPath.Combine(build, "obj") + IOPath.DirectorySeparatorChar,
                "-p:OutputPath=" + IOPath.Combine(build, "app") + IOPath.DirectorySeparatorChar };
            var project = JobProtocol.ChildPath(input, _captured.Options.ProjectFile);
            exit = await ProcessJob.Run("dotnet", ["build", project, "--nologo", .. settings], input, OutputPath, "build", _captured.Options.DeadlineMilliseconds, deadline.Token);
            if (exit != 0) throw new JobFailure("BUILD_FAILED", 3, $"Snapshot build failed ({exit}); inspect build logs.");
            exit = await ProcessJob.Run("dotnet", ["msbuild", project, "-nologo", "-getProperty:TargetPath", .. settings], input, OutputPath, "target", 30000, deadline.Token);
            if (exit != 0) throw new JobFailure("BUILD_FAILED", 3, "The snapshot did not expose a runnable target.");
            var app = IOPath.GetFullPath(File.ReadAllText(IOPath.Combine(OutputPath, "target.stdout.log")).Trim(), input);
            if (!app.StartsWith(build + IOPath.DirectorySeparatorChar, StringComparison.Ordinal) || !File.Exists(app) ||
                IOPath.GetExtension(app) != ".dll" || !File.Exists(IOPath.ChangeExtension(app, ".runtimeconfig.json")))
                throw new JobFailure("BUILD_FAILED", 3, "The snapshot target is not an owned managed executable.");
            SnapshotStore.Verify(manifest, input, SnapshotID, manifest.SHA256);
            report.ProjectAssemblySHA256 = JobProtocol.Hash(app);
            if (_captured.Options.Kind == SessionJobKind.Run)
            {
                SetStage("Running snapshot");
                var request = new RunRequest
                {
                    SnapshotID = SnapshotID,
                    SnapshotSHA256 = manifest.SHA256,
                    SnapshotManifest = manifestPath,
                    Scene = _captured.ScenePath,
                    SceneRevision = manifest.SceneRevision,
                    CodeRevision = manifest.CodeRevision,
                    Mode = _captured.Options.Mode,
                    Steps = _captured.Options.Steps,
                    FPS = _captured.Options.FPS,
                    Width = _captured.Options.Width,
                    Height = _captured.Options.Height,
                    Backend = _captured.Options.Backend,
                    Seed = _captured.Options.Seed,
                    DeadlineMilliseconds = _captured.Options.DeadlineMilliseconds,
                    CaptureSteps = _captured.Options.CaptureSteps!.ToArray(),
                    Inputs = _captured.Options.Inputs!.ToArray()
                };
                request.Validate(); var requestPath = IOPath.Combine(OutputPath, "request.json");
                SessionStorage.AtomicWrite(requestPath, JsonSerializer.SerializeToUtf8Bytes(request, JobJSON.Default.RunRequest));
                exit = await ProcessJob.Run("dotnet", [app, "--electron2d-job", requestPath, OutputPath, ID.ToString("N")], input, OutputPath, "game", request.DeadlineMilliseconds, deadline.Token);
                var child = JobProtocol.Read(IOPath.Combine(OutputPath, "run.json"), JobJSON.Default.RunReport);
                if (child.Version != 2 || child.Job != ID.ToString("N") || child.SnapshotID != SnapshotID || child.SnapshotSHA256 != manifest.SHA256 ||
                    child.SceneRevision != manifest.SceneRevision || child.CodeRevision != manifest.CodeRevision || child.Mode != request.Mode ||
                    child.RequestSHA256 != JobProtocol.Hash(requestPath) || child.ProjectAssemblySHA256 != report.ProjectAssemblySHA256 ||
                    child.EngineAssemblySHA256 != manifest.Files.Single(f => f.Path == IOPath.Combine(".session-sdk", "Electron2D.dll")).SHA256 ||
                    child.SDKAssemblySHA256 != manifest.Files.Single(f => f.Path == IOPath.Combine(".session-sdk", "Electron2D.Editor.Session.dll")).SHA256 ||
                    child.FPS != request.FPS || child.StepBudget != request.Steps || child.Seed != request.Seed || child.CompletedSteps < 0 || child.CompletedSteps > request.Steps || child.Success && child.CompletedSteps == 0 ||
                    child.ExitCode != exit || child.Success != (exit == 0) || child.Artifacts is null || child.Assertions is null || child.Observations is null || child.Diagnostics is null)
                    throw new JobFailure("INVALID_RESULT", 4, "The result differs from its exact snapshot/request/binaries/exit status.");
                if (child.Success && (child.Assertions.Any(a => !a.Passed) || child.Assertions.Count + child.Observations.Count > 4096 ||
                    request.Mode == SessionRunMode.Rendered && (child.Renderer != request.Backend || child.Driver == "none") ||
                    request.Mode == SessionRunMode.Headless && (child.Renderer != "none" || child.Driver != "none")))
                    throw new JobFailure("INVALID_RESULT", 4, "Result claims completion without matching assertions or actual renderer identity.");
                SnapshotStore.Verify(manifest, input, SnapshotID, manifest.SHA256);
                report = child;
                foreach (var artifact in report.Artifacts)
                    if (JobProtocol.ChildPath(OutputPath, IOPath.GetRelativePath(OutputPath, artifact.Path)) != artifact.Path || JobProtocol.Hash(artifact.Path) != artifact.SHA256)
                        throw new JobFailure("INVALID_ARTIFACT", 4, "An artifact path or hash is invalid.");
                if (!child.Success) throw new JobFailure("RUN_FAILED", child.ExitCode, "The snapshot process failed; inspect structured diagnostics.");
                if (request.CaptureSteps.Any(step => !report.Artifacts.Any(a => a.Kind == "image" && a.Step == step)))
                    throw new JobFailure("CAPTURE_MISSING", 4, "The completed run did not produce every requested native image.");
            }
            report.Artifacts.Add(new("assembly", app, 0, JobProtocol.Hash(app)));
            report.Success = true;
        }
        catch (Exception error)
        {
            if (error is OperationCanceledException) error = _cancellation.IsCancellationRequested
                ? new JobFailure("CANCELLED", 130, "The owner cancelled this snapshot job.")
                : new JobFailure("DEADLINE_EXCEEDED", 124, "The snapshot job exceeded its whole-job deadline.");
            JobProtocol.Fail(report, error);
        }
        var status = report.Success ? SessionJobStatus.Completed : report.ExitCode == 130 ? SessionJobStatus.Cancelled : SessionJobStatus.Failed;
        var resultPath = IOPath.Combine(OutputPath, "result.json");
        try
        {
            foreach (var file in Directory.EnumerateFiles(OutputPath, "*.log")) report.Artifacts.Add(new("log", file, 0, JobProtocol.Hash(file)));
            JobProtocol.Save(resultPath, report);
        }
        catch (Exception error) { JobProtocol.Fail(report, new JobFailure("RESULT_WRITE_FAILED", 4, error.Message)); status = SessionJobStatus.Failed; }
        lock (_gate) { _status = status; _stage = status.ToString(); }
        return new(ID, Snapshot, status, report.ExitCode, resultPath, Array.AsReadOnly(report.Artifacts.ToArray()), Array.AsReadOnly(report.Diagnostics.ToArray()), Array.AsReadOnly(report.Observations.ToArray()), Array.AsReadOnly(report.Assertions.ToArray()));
    }

    /// <summary>Cancels and waits asynchronously for owned processes to retire; result files remain available.</summary>
    public async ValueTask DisposeAsync()
    {
        Cancel(); await Completion.ConfigureAwait(false);
        lock (_gate) { if (_disposed) return; _disposed = true; _cancellation.Dispose(); _timeout.Dispose(); }
    }
    /// <summary>Removes this completed job's generated input/build/results and shared registry handle; it never deletes source project files.</summary>
    /// <exception cref="InvalidOperationException">The owned processes have not finished retiring.</exception>
    public void RemoveResult()
    {
        if (!Completion.IsCompleted) throw new InvalidOperationException("Wait for terminal cleanup before removing generated job data.");
        var path = JobProtocol.ChildPath(_captured.Root, IOPath.GetRelativePath(_captured.Root, OutputPath));
        if (!Directory.Exists(path)) { _owner.ForgetJob(ID); return; }
        if (File.ReadAllText(IOPath.Combine(path, "job.owner")) != ID.ToString("N")) throw new InvalidDataException("The generated directory no longer belongs to this job.");
        Directory.Delete(path, recursive: true); _owner.ForgetJob(ID);
    }
}
