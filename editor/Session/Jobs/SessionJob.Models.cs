namespace Electron2D.Editor;

/// <summary>The executable boundary requested from an immutable project snapshot.</summary>
public enum SessionJobKind
{
    /// <summary>Compile the exact snapshot without activating gameplay.</summary>
    Build,
    /// <summary>Compile and execute a fresh headless or rendered process.</summary>
    Run
}

/// <summary>The lifecycle state of an owned project job.</summary>
public enum SessionJobStatus
{
    /// <summary>Captured authoring inputs await materialization.</summary>
    Queued,
    /// <summary>Snapshot materialization, compilation or execution is in progress.</summary>
    Running,
    /// <summary>The requested work completed and its result was validated.</summary>
    Completed,
    /// <summary>Cancellation retired the owned process group.</summary>
    Cancelled,
    /// <summary>Snapshot, build, execution, deadline, validation or cleanup failed.</summary>
    Failed
}

/// <summary>An immutable input edge to inject before a one-based simulation step.</summary>
/// <param name="Step">One-based step inside the requested budget.</param><param name="Action">Registered action, or null when Key is selected.</param>
/// <param name="Key">Typed key, or null when Action is selected.</param><param name="Pressed">Press/release state.</param>
/// <param name="Strength">Finite action strength from zero through one.</param><param name="Echo">Whether the key edge is an echo.</param>
public sealed record SessionInput(int Step, string? Action = null, Key? Key = null, bool Pressed = true, float Strength = 1, bool Echo = false);

/// <summary>Bounded options for a build or fresh-process run; source and SDK inputs are frozen by the session.</summary>
/// <param name="Kind">Build only or build and run.</param><param name="ProjectFile">Project-relative single .NET executable project.</param>
/// <param name="SDKDirectory">Qualified managed SDK output with Electron2D.dll, this assembly and native dependencies.</param>
/// <param name="Mode">Headless or actual native rendering for Run.</param><param name="Steps">Maximum completed simulation steps.</param>
/// <param name="FPS">Fixed process/physics cadence.</param><param name="Width">Rendered width.</param><param name="Height">Rendered height.</param>
/// <param name="Backend">Explicit gpu or compatibility backend, without fallback.</param><param name="Seed">Run-local random generator seed.</param>
/// <param name="DeadlineMilliseconds">Whole-job deadline, including snapshot/build/cleanup boundaries.</param>
/// <param name="CaptureSteps">Requested completed rendered frames; copied on submission.</param><param name="Inputs">Input edges; copied on submission.</param>
public sealed record SessionJobOptions(SessionJobKind Kind, string ProjectFile, string SDKDirectory,
    SessionRunMode Mode = SessionRunMode.Headless, int Steps = 60, int FPS = 60, int Width = 320, int Height = 180,
    string Backend = "gpu", ulong Seed = 1, int DeadlineMilliseconds = 300000,
    IReadOnlyList<int>? CaptureSteps = null, IReadOnlyList<SessionInput>? Inputs = null);

/// <summary>A validated snapshot input file.</summary>
/// <param name="Path">Snapshot-relative path.</param><param name="Size">Byte length.</param><param name="SHA256">Exact lowercase content digest.</param>
public sealed record SessionSnapshotFile(string Path, long Size, string SHA256);

/// <summary>Immutable provenance for one materialized project snapshot.</summary>
/// <param name="ID">Snapshot identity.</param><param name="SHA256">Digest of the complete ordered file inventory and document tokens.</param>
/// <param name="SceneRevision">Captured scene token.</param><param name="CodeRevision">Captured source token.</param>
/// <param name="Files">Read-only input inventory, including the copied engine/tooling/native SDK.</param>
public sealed record SessionSnapshotInfo(Guid ID, string SHA256, ulong SceneRevision, ulong CodeRevision, IReadOnlyList<SessionSnapshotFile> Files);

/// <summary>The terminal, attributed outcome of an owned snapshot job.</summary>
/// <param name="ID">Job identity.</param><param name="Snapshot">Materialized immutable input, or null when snapshot preparation failed.</param>
/// <param name="Status">Terminal completion, cancellation or failure.</param><param name="ExitCode">Zero only for validated completion.</param>
/// <param name="ReportPath">Durable result JSON, separate from program output logs.</param>
/// <param name="Artifacts">Validated artifact identities.</param><param name="Diagnostics">Failure and cleanup details.</param>
/// <param name="Observations">Copied typed scenario observations.</param><param name="Assertions">Copied assertions, including retained failures.</param>
public sealed record SessionJobResult(Guid ID, SessionSnapshotInfo? Snapshot, SessionJobStatus Status, int ExitCode,
    string ReportPath, IReadOnlyList<SessionJobArtifact> Artifacts, IReadOnlyList<SessionJobDiagnostic> Diagnostics,
    IReadOnlyList<SessionObservation> Observations, IReadOnlyList<SessionAssertion> Assertions);
