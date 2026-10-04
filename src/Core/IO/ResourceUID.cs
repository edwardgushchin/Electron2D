using System.Buffers.Binary;
using System.Security.Cryptography;
namespace Electron2D;
/// <summary>Maintains typed portable resource identities and their registered file paths.</summary>
/// <remarks>Static operations use a permanent retained identity/path catalog. Saved archives retain UIDs;
/// importing or loading registers them in each process. This catalog owns no resources.</remarks>
public sealed class ResourceUID : ElectronObject
{
    /// <summary>Identifies an absent or invalid UID.</summary>
    public const long InvalidID = -1;
    internal static readonly ResourceUID Runtime = new();
    private readonly object _gate = new();
    private readonly Dictionary<long, string> _paths = new();
    private ResourceUID()
    { }
    /// <summary>Generates a nonnegative identity absent from the registered catalog.</summary><returns>A random portable UID; registration remains explicit.</returns>
    public static long CreateID()
    {
        Span<byte> bytes = stackalloc byte[8];
        lock (Runtime._gate)
        {
            long id;
            do
            {
                System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
                id = BinaryPrimitives.ReadInt64LittleEndian(bytes) & long.MaxValue;
            }
            while (Runtime._paths.ContainsKey(id));
            return id;
        }
    }
    /// <summary>Derives a stable identity from the path and current project name.</summary><param name="path">Portable resource path.</param><returns>A deterministic nonnegative UID.</returns>
    public static long CreateIDForPath(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(ProjectSettings.Get(ProjectSettings.ApplicationName) + "\0" + path));
        return BinaryPrimitives.ReadInt64LittleEndian(bytes) & long.MaxValue;
    }
    /// <summary>Registers a previously absent identity.</summary><param name="id">Nonnegative UID.</param><param name="path">Resource path.</param>
    public static void AddID(long id, string path)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(id);
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (path.StartsWith("uid://", StringComparison.Ordinal)) throw new ArgumentException("A UID must map to a file path.");
        lock (Runtime._gate) if (!Runtime._paths.TryAdd(id, path)) throw new InvalidOperationException("Resource UID already registered.");
    }
    /// <summary>Assigns or replaces an identity's path.</summary><param name="id">Nonnegative UID.</param><param name="path">Resource path.</param>
    public static void SetID(long id, string path)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(id);
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (path.StartsWith("uid://", StringComparison.Ordinal)) throw new ArgumentException("A UID must map to a file path.");
        lock (Runtime._gate) Runtime._paths[id] = path;
    }
    /// <summary>Reports registered identity membership.</summary><param name="id">UID.</param><returns>Whether it is known.</returns>
    public static bool HasID(long id)
    {
        lock (Runtime._gate) return Runtime._paths.ContainsKey(id);
    }
    /// <summary>Reads a registered identity's path.</summary><param name="id">UID.</param><returns>The registered path.</returns>
    public static string GetIDPath(long id)
    {
        lock (Runtime._gate) return Runtime._paths.TryGetValue(id, out var path) ? path : throw new KeyNotFoundException("Resource UID is not registered.");
    }
    /// <summary>Removes one registered identity.</summary><param name="id">Known UID.</param>
    public static void RemoveID(long id)
    {
        lock (Runtime._gate) if (!Runtime._paths.Remove(id)) throw new KeyNotFoundException("Resource UID is not registered.");
    }
    /// <summary>Formats a nonnegative UID in portable uid:// notation.</summary><param name="id">UID or InvalidID.</param><returns>Portable identity text.</returns>
    public static string IDToText(long id)
    {
        if (id == InvalidID) return "uid://<invalid>";
        ArgumentOutOfRangeException.ThrowIfNegative(id);
        Span<char> chars = stackalloc char[14];
        var n = chars.Length;
        do
        {
            chars[--n] = "abcdefghijklmnopqrstuvwxy012345678"[(int)(id % 34)];
            id /= 34;
        }
        while (id > 0);
        return "uid://" + new string(chars[n..]);
    }
    /// <summary>Parses portable identity text.</summary><param name="textID">uid:// token.</param><returns>UID or InvalidID for malformed/overflow input.</returns>
    public static long TextToID(string textID)
    {
        ArgumentNullException.ThrowIfNull(textID);
        if (!textID.StartsWith("uid://", StringComparison.Ordinal) || textID.Length <= 6 || textID == "uid://<invalid>") return InvalidID;
        long id = 0;
        try
        {
            foreach (var c in textID.AsSpan(6))
            {
                var digit = "abcdefghijklmnopqrstuvwxy012345678".IndexOf(c);
                if (digit < 0) return InvalidID;
                id = checked(id * 34 + digit);
            }
            return id;
        }
        catch (OverflowException)
        {
            return InvalidID;
        }
    }
    /// <summary>Resolves UID notation while preserving ordinary paths.</summary><param name="pathOrUID">Path or UID token.</param><returns>Registered path; unknown tokens throw.</returns>
    public static string EnsurePath(string pathOrUID)
    {
        ArgumentNullException.ThrowIfNull(pathOrUID);
        return pathOrUID.StartsWith("uid://", StringComparison.Ordinal) ? GetIDPath(TextToID(pathOrUID)) : pathOrUID;
    }
    internal static long FindIDForPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.StartsWith("uid://", StringComparison.Ordinal)) return TextToID(path);
        lock (Runtime._gate) foreach (var pair in Runtime._paths) if (pair.Value == path) return pair.Key;
        return InvalidID;
    }
    /// <summary>Converts a known path to portable UID notation and preserves unknown paths.</summary><param name="path">Resource path or UID notation.</param><returns>UID notation when a registered identity exists.</returns>
    public static string PathToUID(string path)
    {
        var id = FindIDForPath(path);
        return id == InvalidID ? path : IDToText(id);
    }
    /// <summary>Resolves UID notation to a path and preserves ordinary paths.</summary><param name="uid">Path or UID notation.</param><returns>Resolved path.</returns>
    public static string UIDToPath(string uid) => EnsurePath(uid);
    /// <inheritdoc />
    protected override void ValidateDisposal() => throw new InvalidOperationException("The resource-identity service is permanent.");
}
