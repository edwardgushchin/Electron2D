using System.Text.Json;
using System.Text.Json.Serialization;

namespace Electron2D.Tools.Batch;

/// <summary>Selects the services exercised by a compiled batch scenario.</summary>
public enum BatchMode
{
    /// <summary>Saves a detached scene without activating gameplay or opening a window.</summary>
    Authoring,
    /// <summary>Advances the ordinary scene lifecycle without native presentation.</summary>
    Headless,
    /// <summary>Runs the ordinary native host with fixed movie timing and completed-frame capture.</summary>
    Rendered
}

internal sealed record ProjectManifest(int Version, string Name, string Project);
internal sealed class BatchRequest
{
    public int Version { get; set; } = 1;
    public BatchMode Mode { get; set; } = BatchMode.Headless;
    public int Steps { get; set; } = 60;
    public int FPS { get; set; } = 60;
    public int Width { get; set; } = 320;
    public int Height { get; set; } = 180;
    public string Backend { get; set; } = "gpu";
    public ulong Seed { get; set; } = 1;
    public int DeadlineMilliseconds { get; set; } = 30000;
    public string? Scene { get; set; }
    public int[] CaptureSteps { get; set; } = [];
    public InputRequest[] Inputs { get; set; } = [];

    internal void Validate()
    {
        if (Version != 1 || !Enum.IsDefined(Mode) || Steps is < 1 or > 1000000 || FPS is < 1 or > 240 ||
            Width is < 2 or > 4096 || Height is < 2 or > 4096 || Backend is not ("gpu" or "compatibility") ||
            DeadlineMilliseconds is < 100 or > 3600000 || CaptureSteps is null || Inputs is null ||
            CaptureSteps.Length > 64 || Inputs.Length > 65536)
            throw new BatchFailure("INVALID_REQUEST", 2, "Invalid version, mode, dimensions, timing or collection size.");
        if (Mode != BatchMode.Rendered && CaptureSteps.Length != 0)
            throw new BatchFailure("CAPTURE_UNAVAILABLE", 7, "Image capture requires rendered mode.");
        if (Mode == BatchMode.Authoring && Inputs.Length != 0)
            throw new BatchFailure("INVALID_REQUEST", 2, "Authoring does not deliver gameplay input.");
        if (CaptureSteps.Any(s => s < 1 || s > Steps) || CaptureSteps.Distinct().Count() != CaptureSteps.Length)
            throw new BatchFailure("INVALID_REQUEST", 2, "Capture steps must be unique and inside the step budget.");
        var previous = 0;
        foreach (var input in Inputs)
        {
            if (input is null || input.Step < 1 || input.Step > Steps || input.Step < previous ||
                (input.Action is null) == (input.Key is null) || input.Action is { Length: 0 or > 256 } ||
                input.Key is { } key && (!Enum.IsDefined(key) || key == Electron2D.Key.None) ||
                !float.IsFinite(input.Strength) || input.Strength is < 0 or > 1 || input.Action is not null && input.Echo)
                throw new BatchFailure("INVALID_REQUEST", 2, "Inputs require ordered steps and exactly one valid action or key.");
            previous = input.Step;
        }
    }
}
internal sealed class InputRequest
{
    public int Step { get; set; }
    public string? Action { get; set; }
    public Key? Key { get; set; }
    public bool Pressed { get; set; }
    public bool Echo { get; set; }
    public float Strength { get; set; } = 1;
}
internal sealed record Observation(int Step, string Name, double? Number = null, bool? Boolean = null, string? Text = null);
internal sealed record Assertion(int Step, string Name, bool Passed, string Message);
internal sealed record Artifact(string Kind, string Path, int Step, [property: JsonPropertyName("sha256")] string SHA256);
internal sealed record Diagnostic(string Code, string Message, string? ExceptionType = null);
internal sealed class BatchReport
{
    public int Version { get; set; } = 1;
    public string Operation { get; set; } = "run";
    public string Job { get; set; } = "";
    public bool Success { get; set; }
    public int ExitCode { get; set; }
    public BatchMode? Mode { get; set; }
    public string Project { get; set; } = "";
    public string Platform { get; set; } = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
    public string EngineVersion { get; set; } = "";
    public string SceneSHA256 { get; set; } = "";
    public string EngineAssemblySHA256 { get; set; } = "";
    public string ProjectAssemblySHA256 { get; set; } = "";
    public string RequestSHA256 { get; set; } = "";
    public string Renderer { get; set; } = "none";
    public string Driver { get; set; } = "none";
    public ulong Seed { get; set; }
    public int FPS { get; set; }
    public int StepBudget { get; set; }
    public int CompletedSteps { get; set; }
    public List<Observation> Observations { get; set; } = [];
    public List<Assertion> Assertions { get; set; } = [];
    public List<Artifact> Artifacts { get; set; } = [];
    public List<Diagnostic> Diagnostics { get; set; } = [];
}
internal sealed class BatchFailure(string code, int exitCode, string message) : Exception(message)
{
    internal string Code { get; } = code;
    internal int ExitCode { get; } = exitCode;
}
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, UseStringEnumConverter = true)]
[JsonSerializable(typeof(BatchRequest))]
[JsonSerializable(typeof(ProjectManifest))]
[JsonSerializable(typeof(BatchReport))]
internal partial class ProtocolJSON : JsonSerializerContext;

internal static class Protocol
{
    internal static T Read<T>(string path, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> type)
    {
        if (new FileInfo(path).Length > 8 * 1024 * 1024) throw new BatchFailure("INVALID_REQUEST", 2, "Protocol files are limited to eight MiB.");
        using var file = File.OpenRead(path);
        return JsonSerializer.Deserialize(file, type) ?? throw new BatchFailure("INVALID_REQUEST", 2, "Protocol data cannot be null.");
    }
    internal static void Save(string path, BatchReport report)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(report, ProtocolJSON.Default.BatchReport)); File.Move(temporary, path, true); }
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
                throw new BatchFailure("INVALID_PROJECT", 2, "Project references must stay inside the project directory.");
            var current = IOPath.GetFullPath(root);
            foreach (var component in IOPath.GetRelativePath(current, path).Split(IOPath.DirectorySeparatorChar))
            {
                current = IOPath.Combine(current, component);
                try
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                        throw new BatchFailure("INVALID_PROJECT", 2, "Automatic project paths cannot traverse symbolic links.");
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
            }
            return path;
        }
        catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException)
        { throw new BatchFailure("INVALID_PROJECT", 2, error.Message); }
    }
    internal static void Fail(BatchReport report, Exception error)
    {
        report.Success = false;
        var failure = error as BatchFailure;
        if (report.ExitCode == 0) report.ExitCode = failure?.ExitCode ?? 4;
        var message = error.Message; var type = error.GetType().FullName;
        if (report.Diagnostics.Count < 32) report.Diagnostics.Add(new(failure?.Code ?? "RUNTIME_FAILED", message[..Math.Min(message.Length, 8192)], type?[..Math.Min(type.Length, 256)]));
    }
}
