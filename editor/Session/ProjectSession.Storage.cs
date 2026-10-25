using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Electron2D.Editor;

internal sealed record DocumentState
{
    public int Version { get; init; } = 1;
    public required string ScenePath { get; init; }
    public required string CodePath { get; init; }
    public Guid SceneID { get; init; } = Guid.NewGuid();
    public Guid CodeID { get; init; } = Guid.NewGuid();
    public ulong Clock { get; init; } = 1;
    public ulong SceneRevision { get; init; } = 1;
    public ulong CodeRevision { get; init; } = 1;
    public required byte[] Scene { get; init; }
    public required string SavedSceneHash { get; init; }
    public required string SceneFileHash { get; init; }
    public required string CodeFileHash { get; init; }
    public required string Code { get; init; }
    public required string SavedCode { get; init; }
    public bool BOM { get; init; }
    public bool SavedBOM { get; init; }
    public required NodeState[] Nodes { get; init; }
    public HistoryState[] History { get; init; } = [];
}

internal sealed record NodeState(Guid ID, Guid ParentID, string Name, string TypeName, ulong Revision, bool CanEdit, Transform? Transform, int Index);
internal sealed record HistoryState(Guid ID, string Label, string Origin, NodeState[] Before, NodeState[] After,
    bool Hierarchy, string? BeforeCode, string? AfterCode, Dictionary<Guid, ulong> Tokens, ulong CodeToken, ulong SceneToken, bool Applied);

[JsonSourceGenerationOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, IncludeFields = true, IgnoreReadOnlyProperties = true)]
[JsonSerializable(typeof(DocumentState))]
[JsonSerializable(typeof(NodeState[]))]
internal partial class SessionJSON : JsonSerializerContext;

internal sealed class SessionStorage : IDisposable
{
    internal const int MaxSceneBytes = 16 * 1024 * 1024;
    internal const int MaxCodeBytes = 4 * 1024 * 1024;
    internal const int MaxStateBytes = 64 * 1024 * 1024;
    internal static readonly UTF8Encoding UTF8 = new(false, true);
    private readonly FileStream _lease;
    private readonly string _directory;
    internal string Root { get; }
    internal string StatePath => Resolve(".electron2d/session/state.json", metadata: true);
    internal string EncodePath => Resolve(".electron2d/session/encode.e2dscene", metadata: true);
    internal string DecodePath => Resolve(".electron2d/session/decode.e2dscene", metadata: true);

    internal SessionStorage(string root)
    {
        Root = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(root));
        if (!Directory.Exists(Root)) throw new DirectoryNotFoundException(Root);
        CheckLinks(Root);
        _directory = Resolve(".electron2d/session", metadata: true);
        Directory.CreateDirectory(_directory);
        // Keep this inode: unlinking a live lock would admit another owner on a different inode.
        _lease = new FileStream(Resolve(".electron2d/session/owner.lock", metadata: true), FileMode.OpenOrCreate,
            System.IO.FileAccess.ReadWrite, FileShare.None);
        try
        {
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(_directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        catch { _lease.Dispose(); throw; }
    }

    internal string Resolve(string relative, bool metadata = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relative);
        if (System.IO.Path.IsPathRooted(relative)) throw new ArgumentException("Use a project-relative path.", nameof(relative));
        var full = System.IO.Path.GetFullPath(relative, Root);
        if (!full.StartsWith(Root + System.IO.Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            !metadata && System.IO.Path.GetRelativePath(Root, full).Split(System.IO.Path.DirectorySeparatorChar)[0] == ".electron2d")
            throw new ArgumentException("The document must stay inside the project, outside session metadata.", nameof(relative));
        CheckLinks(full);
        return full;
    }

    private void CheckLinks(string full)
    {
        for (var current = full; current is not null; current = System.IO.Path.GetDirectoryName(current))
            if (new FileInfo(current).LinkTarget is not null || new DirectoryInfo(current).LinkTarget is not null)
                throw new IOException("Session paths cannot contain symbolic links.");
    }

    internal static byte[] Read(string path, int limit)
    {
        using var file = new FileStream(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read);
        if (file.Length > limit) throw new InvalidDataException("Session input exceeds its bounded size.");
        var bytes = new byte[checked((int)file.Length)]; file.ReadExactly(bytes); return bytes;
    }

    internal static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    internal static string? FileHash(string path, int limit) => File.Exists(path) ? Hash(Read(path, limit)) : null;

    internal static (string Text, bool BOM) DecodeCode(byte[] bytes)
    {
        var bom = bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble);
        return (UTF8.GetString(bytes.AsSpan(bom ? 3 : 0)), bom);
    }

    internal static byte[] CodeBytes(string text, bool bom)
    {
        ArgumentNullException.ThrowIfNull(text);
        var length = UTF8.GetByteCount(text);
        if (length + (bom ? 3 : 0) > MaxCodeBytes) throw new ArgumentException("The code buffer exceeds 4 MiB.", nameof(text));
        return bom ? [.. Encoding.UTF8.Preamble, .. UTF8.GetBytes(text)] : UTF8.GetBytes(text);
    }

    internal DocumentState? ReadState()
    {
        if (!File.Exists(StatePath)) return null;
        var state = JsonSerializer.Deserialize(Read(StatePath, MaxStateBytes), SessionJSON.Default.DocumentState)
            ?? throw new InvalidDataException("Empty session recovery record.");
        if (state.Version != 1 || state.SceneID == Guid.Empty || state.CodeID == Guid.Empty || state.SceneID == state.CodeID ||
            state.Clock == 0 || state.SceneRevision > state.Clock || state.CodeRevision > state.Clock ||
            state.Nodes.Length is 0 or > 4096 || state.Nodes.Select(n => n.ID).Distinct().Count() != state.Nodes.Length ||
            state.Nodes.Any(n => n.ID == Guid.Empty || n.Revision > state.Clock) || state.History.Length > 32 ||
            state.History.Select(h => h.ID).Distinct().Count() != state.History.Length || state.Scene.Length > MaxSceneBytes)
            throw new InvalidDataException("Invalid session recovery bounds/identities/revisions.");
        CodeBytes(state.Code, state.BOM); CodeBytes(state.SavedCode, state.BOM);
        return state;
    }

    internal DocumentState Persist(DocumentState state)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(state, SessionJSON.Default.DocumentState);
        while (bytes.Length > MaxStateBytes && state.History.Length > 1)
        {
            state = state with { History = state.History[1..] };
            bytes = JsonSerializer.SerializeToUtf8Bytes(state, SessionJSON.Default.DocumentState);
        }
        if (bytes.Length > MaxStateBytes) throw new InvalidDataException("The bounded live document/group exceeds the 64 MiB recovery envelope.");
        AtomicWrite(StatePath, bytes); return state;
    }

    internal static void AtomicWrite(string path, byte[] bytes)
    {
        var temporary = path + ".session-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var file = new FileStream(temporary, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None))
            {
                file.Write(bytes); file.Flush(flushToDisk: true);
            }
            if (!OperatingSystem.IsWindows() && File.Exists(path)) File.SetUnixFileMode(temporary, File.GetUnixFileMode(path));
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public void Dispose() => _lease.Dispose();
}
