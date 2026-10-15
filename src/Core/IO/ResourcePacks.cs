using System.IO.Compression;
using System.Security.Cryptography;

namespace Electron2D;

internal sealed class ResourcePacks
{
    private sealed record Entry(string Pack, long Offset, long Length, byte[]? Key, int ZIPIndex)
    {
        internal Stream Open()
        {
            if (ZIPIndex >= 0)
            {
                using var reader = new ZIPReader(); reader.Open(Pack);
                var bytes = reader.ReadFileAt(ZIPIndex);
                if (bytes.LongLength != Length) throw new InvalidDataException("A mounted ZIP entry changed length.");
                return new MemoryStream(bytes, writable: false);
            }
            var file = File.OpenRead(Pack);
            try
            {
                if (Offset < 0 || Offset > file.Length || Length > file.Length - Offset) throw new InvalidDataException("A mounted pack was truncated.");
                if (Key is null) return new PackSliceStream(file, Offset, Length);
                file.Position = Offset; return new MemoryStream(PCKFormat.Decrypt(file, Key, Length), writable: false);
            }
            catch { file.Dispose(); throw; }
            finally { if (Key is not null) file.Dispose(); }
        }
    }

    private readonly object _gate = new();
    private Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private HashSet<string> _directories = new(StringComparer.Ordinal);
    internal static ResourcePacks Runtime { get; } = new();

    internal bool Load(string pack, bool replace, long offset, ReadOnlySpan<byte> key)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        if (key.Length != 32) throw new ArgumentException("A PCK decryption key requires 32 bytes.", nameof(key));
        var absolute = ProjectSettings.GlobalizePath(pack);
        try
        {
            using var stream = File.OpenRead(absolute);
            var records = Read(stream, absolute, offset, key.ToArray());
            lock (_gate)
            {
                var next = new Dictionary<string, Entry>(_entries, StringComparer.Ordinal);
                foreach (var (name, entry) in records)
                {
                    if (entry is null) next.Remove(name);
                    else if (replace || !next.ContainsKey(name)) next[name] = entry;
                }
                var directories = new HashSet<string>(StringComparer.Ordinal) { "" };
                foreach (var name in next.Keys)
                {
                    var slash = name.IndexOf('/');
                    while (slash >= 0)
                    {
                        var parent = name[..slash];
                        if (next.ContainsKey(parent)) throw new InvalidDataException("A pack file collides with a directory.");
                        directories.Add(parent); slash = name.IndexOf('/', slash + 1);
                    }
                }
                _entries = next; _directories = directories;
            }
            return true;
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or OverflowException or CryptographicException)
        { return false; }
    }

    private static List<(string Name, Entry? Entry)> Read(FileStream stream, string pack, long offset, byte[] key)
    {
        using var reader = new BinaryReader(stream, PCKFormat.UTF8, leaveOpen: true);
        if (offset > stream.Length - 4) throw new InvalidDataException("Missing PCK header.");
        stream.Position = offset;
        if (reader.ReadUInt32() != PCKFormat.Magic)
        {
            if (offset != 0) throw new InvalidDataException("Missing PCK header at the requested offset.");
            stream.Position = Math.Max(0, stream.Length - 12);
            var size = PCKFormat.ReadOffset(reader);
            if (reader.ReadUInt32() == PCKFormat.Magic)
            {
                offset = checked(stream.Length - 12 - size);
                if (offset < 0) throw new InvalidDataException("Invalid appended PCK trailer.");
                stream.Position = offset;
                if (reader.ReadUInt32() != PCKFormat.Magic) throw new InvalidDataException("Missing appended PCK header.");
            }
            else return ReadZIP(stream, pack);
        }
        var version = reader.ReadUInt32();
        if (version is < 2 or > 4) throw new InvalidDataException("Unsupported PCK version.");
        _ = reader.ReadUInt32(); _ = reader.ReadUInt32(); _ = reader.ReadUInt32();
        var flags = reader.ReadUInt32();
        if ((flags & ~3u) != 0) throw new InvalidDataException("Unsupported PCK flags.");
        var fileBase = PCKFormat.ReadOffset(reader);
        if (version >= 3 || (flags & 2) != 0) fileBase = checked(fileBase + offset);
        if (fileBase > stream.Length) throw new InvalidDataException("Invalid PCK file base.");
        if (version >= 3)
        {
            var dir = checked(PCKFormat.ReadOffset(reader) + offset);
            if (dir < offset + 104 || dir > stream.Length - 4) throw new InvalidDataException("Invalid PCK directory offset.");
            stream.Position = dir;
        }
        else _ = PCKFormat.ReadExactly(stream, 64);
        var count = reader.ReadUInt32();
        if (count > PCKFormat.DirectoryLimit / 44) throw new InvalidDataException("Too many PCK entries.");
        using var decoded = (flags & 1) != 0 ? new MemoryStream(PCKFormat.Decrypt(stream, key, limit: PCKFormat.DirectoryLimit), writable: false) : null;
        var input = (Stream?)decoded ?? stream;
        using var directory = new BinaryReader(input, PCKFormat.UTF8, leaveOpen: true);
        var directoryStart = input.Position;
        var records = new List<(string, Entry?)>((int)count);
        for (var i = 0u; i < count; i++)
        {
            var pathLength = directory.ReadUInt32();
            if (pathLength is 0 or > 1024 * 1024) throw new InvalidDataException("Invalid PCK path length.");
            var bytes = PCKFormat.ReadExactly(input, (int)pathLength);
            var zero = Array.IndexOf(bytes, (byte)0);
            if (zero >= 0 && bytes.AsSpan(zero).IndexOfAnyExcept((byte)0) >= 0) throw new InvalidDataException("Invalid PCK path padding.");
            var name = PCKFormat.Normalize(PCKFormat.UTF8.GetString(bytes, 0, zero < 0 ? bytes.Length : zero));
            var start = checked(fileBase + PCKFormat.ReadOffset(directory)); var length = PCKFormat.ReadOffset(directory);
            _ = PCKFormat.ReadExactly(input, 16); var fileFlags = directory.ReadUInt32();
            if ((fileFlags & ~3u) != 0) throw new InvalidDataException("Unsupported PCK entry flags.");
            if ((fileFlags & 2) != 0) records.Add((name, null));
            else
            {
                var storedLength = (fileFlags & 1) != 0 ? checked(40 + (length + 15) / 16 * 16) : length;
                if (start < fileBase || storedLength > stream.Length - start) throw new InvalidDataException("Invalid PCK entry range.");
                records.Add((name, new(pack, start, length, (fileFlags & 1) != 0 ? key : null, -1)));
            }
            if (input.Position - directoryStart > PCKFormat.DirectoryLimit) throw new InvalidDataException("PCK directory exceeds its size budget.");
        }
        if (decoded is not null && decoded.Position != decoded.Length) throw new InvalidDataException("PCK encrypted directory has trailing data.");
        return records;
    }

    private static List<(string, Entry?)> ReadZIP(Stream stream, string pack)
    {
        var directory = ZIPDirectory.Read(stream);
        if (directory.Length > PCKFormat.DirectoryLimit || directory.Count > PCKFormat.DirectoryLimit / 46) throw new InvalidDataException("ZIP directory exceeds its size budget.");
        var flags = ZIPDirectory.Flags(stream, directory);
        if (flags.Any(static flag => (flag & 0x2041) != 0)) throw new InvalidDataException("Encrypted ZIP resource packs are unsupported.");
        stream.Position = directory.Prefix;
        using var archive = new ZipArchive(new ZIPViewStream(stream, directory.Prefix), ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding: ZIPDirectory.UTF8);
        var records = new List<(string, Entry?)>(); var size = 0L;
        for (var i = 0; i < archive.Entries.Count; i++)
        {
            var entry = archive.Entries[i];
            size += 44 + PCKFormat.UTF8.GetByteCount(entry.FullName);
            if (size > PCKFormat.DirectoryLimit) throw new InvalidDataException("ZIP directory exceeds its size budget.");
            if (entry.FullName.EndsWith('/')) continue;
            records.Add((PCKFormat.Normalize(entry.FullName), new(pack, 0, entry.Length, null, i)));
        }
        return records;
    }

    private static string? Name(string path)
    {
        path = ResourceUID.EnsurePath(path);
        if (!path.StartsWith("res://", StringComparison.Ordinal)) return null;
        _ = ProjectSettings.GlobalizePath(path);
        return PCKFormat.Normalize(path, directory: true);
    }

    internal bool TryOpen(string path, out Stream stream)
    {
        var name = Name(path); Entry? entry;
        lock (_gate) entry = name is not null ? _entries.GetValueOrDefault(name) : null;
        stream = entry?.Open()!; return entry is not null;
    }

    internal bool TrySize(string path, out long size)
    {
        var name = Name(path);
        lock (_gate) { if (name is not null && _entries.TryGetValue(name, out var e)) { size = e.Length; return true; } }
        size = 0; return false;
    }

    internal bool IsDirectory(string path)
    {
        var name = Name(path); lock (_gate) return name is not null && _directories.Contains(name);
    }

    internal (string Name, bool Directory)[] List(string path)
    {
        var name = Name(path); if (name is null) return [];
        var prefix = name.Length == 0 ? "" : name + "/";
        lock (_gate)
        {
            var result = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var file in _entries.Keys)
            {
                if (!file.StartsWith(prefix, StringComparison.Ordinal)) continue;
                var relative = file[prefix.Length..]; var slash = relative.IndexOf('/');
                result[slash < 0 ? relative : relative[..slash]] = slash >= 0;
            }
            return result.Select(e => (e.Key, e.Value)).ToArray();
        }
    }

    internal bool HasFileAncestor(string path)
    {
        var name = Name(path); if (name is null) return false;
        lock (_gate)
        {
            var slash = name.IndexOf('/');
            while (slash >= 0) { if (_entries.ContainsKey(name[..slash])) return true; slash = name.IndexOf('/', slash + 1); }
            return false;
        }
    }
    internal void EnsureReadablePath(string path)
    {
        if (HasFileAncestor(path)) throw new FileNotFoundException("A mounted file occupies a parent directory.", path);
    }

    internal bool IsSameFile(string first, string second, out bool packed)
    {
        var a = Name(first); var b = Name(second);
        lock (_gate)
        {
            var x = a is null ? null : _entries.GetValueOrDefault(a); var y = b is null ? null : _entries.GetValueOrDefault(b);
            packed = x is not null || y is not null; return x is not null && ReferenceEquals(x, y);
        }
    }

    internal void EnsureWritable(string path, bool descendants = false)
    {
        var name = Name(path); if (name is null) return;
        lock (_gate)
        {
            if (_entries.ContainsKey(name) || descendants && _directories.Contains(name)) throw new UnauthorizedAccessException("Mounted resource entries are read-only.");
            var slash = name.IndexOf('/');
            while (slash >= 0) { if (_entries.ContainsKey(name[..slash])) throw new UnauthorizedAccessException("A mounted file occupies a parent path."); slash = name.IndexOf('/', slash + 1); }
        }
    }

    internal static Stream OpenRead(string path) => OpenRead(path, out _);
    private static Stream OpenRead(string path, out bool packed)
    {
        Runtime.EnsureReadablePath(path);
        packed = Runtime.TryOpen(path, out var stream);
        return packed ? stream : File.OpenRead(ProjectSettings.GlobalizePath(ResourceUID.EnsurePath(path)));
    }
    internal static string CombineRelative(string path, string relative)
    {
        if (path.StartsWith("res://", StringComparison.Ordinal))
        {
            var slash = path.LastIndexOf('/');
            return "res://" + PCKFormat.Normalize(path[..(slash + 1)] + relative, directory: true);
        }
        return System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ProjectSettings.GlobalizePath(path))!, relative.Replace('\\', System.IO.Path.DirectorySeparatorChar)));
    }
    internal static byte[] ReadBytes(string path) => ReadBytes(path, out _);
    internal static byte[] ReadBytes(string path, out bool packed)
    {
        using var stream = OpenRead(path, out packed);
        if (stream.Length > Array.MaxLength) throw new IOException("The file exceeds the managed byte array limit.");
        var result = new byte[(int)stream.Length]; stream.ReadExactly(result); return result;
    }
}
