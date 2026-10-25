using System.Text.Json;
using System.Text.Json.Serialization;

namespace Electron2D.Editor;

/// <summary>Selects the services exercised by a fresh immutable-snapshot scenario.</summary>
public enum SessionRunMode
{
    /// <summary>Advances the ordinary scene lifecycle without native presentation.</summary>
    Headless,
    /// <summary>Runs the ordinary native host with fixed movie timing and completed-frame capture.</summary>
    Rendered
}


internal sealed class RunRequest
{
    public int Version { get; set; } = 2;
    public SessionRunMode Mode { get; set; } = SessionRunMode.Headless;
    public int Steps { get; set; } = 60;
    public int FPS { get; set; } = 60;
    public int Width { get; set; } = 320;
    public int Height { get; set; } = 180;
    public string Backend { get; set; } = "gpu";
    public ulong Seed { get; set; } = 1;
    public int DeadlineMilliseconds { get; set; } = 30000;
    public string Scene { get; set; } = "";
    public Guid SnapshotID { get; set; }
    public string SnapshotSHA256 { get; set; } = "";
    public string SnapshotManifest { get; set; } = "";
    public ulong SceneRevision { get; set; }
    public ulong CodeRevision { get; set; }
    public int[] CaptureSteps { get; set; } = [];
    public SessionInput[] Inputs { get; set; } = [];

    internal void Validate()
    {
        if (Version != 2 || SnapshotID == Guid.Empty || SnapshotSHA256.Length != 64 || string.IsNullOrWhiteSpace(Scene) || !Enum.IsDefined(Mode) || Steps is < 1 or > 1000000 || FPS is < 1 or > 240 ||
            Width is < 2 or > 4096 || Height is < 2 or > 4096 || Backend is not ("gpu" or "compatibility") ||
            DeadlineMilliseconds is < 100 or > 3600000 || CaptureSteps is null || Inputs is null ||
            CaptureSteps.Length > 64 || Inputs.Length > 65536)
            throw new JobFailure("INVALID_REQUEST", 2, "Invalid version, mode, dimensions, timing or collection size.");
        if (Mode != SessionRunMode.Rendered && CaptureSteps.Length != 0)
            throw new JobFailure("CAPTURE_UNAVAILABLE", 7, "Image capture requires rendered mode.");
        if (CaptureSteps.Any(s => s < 1 || s > Steps) || CaptureSteps.Distinct().Count() != CaptureSteps.Length)
            throw new JobFailure("INVALID_REQUEST", 2, "Capture steps must be unique and inside the step budget.");
        if ((long)Width * Height * 4 * CaptureSteps.Length > 256L * 1024 * 1024)
            throw new JobFailure("CAPTURE_BUDGET_EXCEEDED", 2, "Requested uncompressed captures exceed the 256 MiB job budget.");
        var previous = 0; long requestBytes = 65536;
        foreach (var input in Inputs)
        {
            if (input is null || input.Step < 1 || input.Step > Steps || input.Step < previous ||
                (input.Action is null) == (input.Key is null) || input.Action is { Length: 0 or > 256 } ||
                input.Key is { } key && (!Enum.IsDefined(key) || key == Electron2D.Key.None) ||
                !float.IsFinite(input.Strength) || input.Strength is < 0 or > 1 || input.Action is not null && input.Echo)
                throw new JobFailure("INVALID_REQUEST", 2, "Inputs require ordered steps and exactly one valid action or key.");
            // Conservative escaped/indented JSON bound keeps admitted requests inside the reader's eight-MiB limit.
            requestBytes += 250 + (input.Action?.Length ?? 0) * 6L;
            if (requestBytes > 8L * 1024 * 1024) throw new JobFailure("REQUEST_TOO_LARGE", 2, "Input edges exceed the encoded request budget.");
            previous = input.Step;
        }
    }
}
/// <summary>One completed-step observation with a single typed payload.</summary>
/// <param name="Step">One-based step, or zero during configuration.</param>
/// <param name="Name">Stable bounded name.</param>
/// <param name="Number">Finite numeric payload.</param>
/// <param name="Boolean">Boolean payload.</param>
/// <param name="Text">Bounded text payload.</param>
public sealed record SessionObservation(int Step, string Name, double? Number = null, bool? Boolean = null, string? Text = null);
/// <summary>One completed-step assertion and its bounded explanation.</summary>
/// <param name="Step">One-based step or zero during configuration.</param>
/// <param name="Name">Stable bounded assertion name.</param>
/// <param name="Passed">Whether the condition passed.</param>
/// <param name="Message">Bounded explanation.</param>
public sealed record SessionAssertion(int Step, string Name, bool Passed, string Message);
/// <summary>A completed artifact path, step and exact content digest.</summary>
/// <param name="Kind">Image, assembly or log category.</param>
/// <param name="Path">Absolute owned artifact path.</param>
/// <param name="Step">Completed frame, or zero for nonframe output.</param>
/// <param name="SHA256">Exact lowercase content digest.</param>
public sealed record SessionJobArtifact(string Kind, string Path, int Step, [property: JsonPropertyName("sha256")] string SHA256);
/// <summary>A structured failure category, readable detail and optional exception type.</summary>
/// <param name="Code">Stable failure category.</param>
/// <param name="Message">Bounded readable detail.</param>
/// <param name="ExceptionType">Optional exception identity.</param>
public sealed record SessionJobDiagnostic(string Code, string Message, string? ExceptionType = null);
internal sealed class RunReport
{
    public int Version { get; set; } = 2;
    public string Operation { get; set; } = "run";
    public string Job { get; set; } = "";
    public bool Success { get; set; }
    public int ExitCode { get; set; }
    public SessionRunMode? Mode { get; set; }
    public string Project { get; set; } = "";
    public string Platform { get; set; } = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
    public string EngineVersion { get; set; } = "";
    public string SceneSHA256 { get; set; } = "";
    public Guid SnapshotID { get; set; }
    public string SnapshotSHA256 { get; set; } = "";
    public ulong SceneRevision { get; set; }
    public ulong CodeRevision { get; set; }
    public string SDKAssemblySHA256 { get; set; } = "";
    public string EngineAssemblySHA256 { get; set; } = "";
    public string ProjectAssemblySHA256 { get; set; } = "";
    public string RequestSHA256 { get; set; } = "";
    public string Renderer { get; set; } = "none";
    public string Driver { get; set; } = "none";
    public ulong Seed { get; set; }
    public int FPS { get; set; }
    public int StepBudget { get; set; }
    public int CompletedSteps { get; set; }
    public List<SessionObservation> Observations { get; set; } = [];
    public List<SessionAssertion> Assertions { get; set; } = [];
    public List<SessionJobArtifact> Artifacts { get; set; } = [];
    public List<SessionJobDiagnostic> Diagnostics { get; set; } = [];
}
internal sealed class JobFailure(string code, int exitCode, string message) : Exception(message)
{
    internal string Code { get; } = code;
    internal int ExitCode { get; } = exitCode;
}
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, UseStringEnumConverter = true)]
[JsonSerializable(typeof(RunRequest))]
[JsonSerializable(typeof(SnapshotManifest))]
[JsonSerializable(typeof(ProjectManifest))]
[JsonSerializable(typeof(RunReport))]
internal partial class JobJSON : JsonSerializerContext;

internal static class JobProtocol
{
    internal static T Read<T>(string path, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> type)
    {
        if (new FileInfo(path).Length > 8 * 1024 * 1024) throw new JobFailure("INVALID_REQUEST", 2, "JobProtocol files are limited to eight MiB.");
        using var file = File.OpenRead(path);
        return JsonSerializer.Deserialize(file, type) ?? throw new JobFailure("INVALID_REQUEST", 2, "JobProtocol data cannot be null.");
    }
    internal static void Save(string path, RunReport report)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(report, JobJSON.Default.RunReport)); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    internal static string Hash(string path)
    {
        using var file = File.OpenRead(path);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(file)).ToLowerInvariant();
    }
    internal static string ChildPath(string root, string relative)
    {
        try
        {
            var path = IOPath.GetFullPath(relative, root);
            var local = IOPath.GetRelativePath(IOPath.GetFullPath(root), path);
            if (IOPath.IsPathRooted(relative) || IOPath.IsPathRooted(local) || local == ".." || local.StartsWith(".." + IOPath.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new JobFailure("INVALID_PROJECT", 2, "Project references must stay inside the project directory.");
            var current = IOPath.GetFullPath(root);
            foreach (var component in IOPath.GetRelativePath(current, path).Split(IOPath.DirectorySeparatorChar))
            {
                current = IOPath.Combine(current, component);
                try
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                        throw new JobFailure("INVALID_PROJECT", 2, "Automatic project paths cannot traverse symbolic links.");
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
            }
            return path;
        }
        catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException)
        { throw new JobFailure("INVALID_PROJECT", 2, error.Message); }
    }
    internal static void Fail(RunReport report, Exception error)
    {
        report.Success = false;
        var failure = error as JobFailure;
        if (report.ExitCode == 0) report.ExitCode = failure?.ExitCode ?? 4;
        var message = error.Message; var type = error.GetType().FullName;
        if (report.Diagnostics.Count < 32) report.Diagnostics.Add(new(failure?.Code ?? "RUNTIME_FAILED", message[..Math.Min(message.Length, 8192)], type?[..Math.Min(type.Length, 256)]));
    }
}
