using System.IO.Compression;

namespace Electron2D;

/// <summary>Reads named ZIP entries from an owned file session.</summary>
/// <remarks>Operations serialize on the instance and may block or allocate. Names are archive keys; no files are
/// extracted automatically. Returned arrays belong to the caller. Reads verify decoded length and CRC before returning.</remarks>
public sealed class ZIPReader : ElectronObject
{
    private readonly object _gate = new();
    private FileStream? _stream;
    private ZipArchive? _archive;
    private ZipArchiveEntry[] _entries = [];
    private ushort[] _flags = [];
    /// <summary>Creates a closed archive reader.</summary>
    public ZIPReader() { }
    /// <summary>Opens an ordinary, res:// or user:// file, replacing the previous session after validation.</summary>
    /// <param name="path">Archive file path.</param>
    /// <exception cref="InvalidDataException">The archive or central directory is malformed or split across disks.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    /// <exception cref="ObjectDisposedException">This reader is disposed.</exception>
    public void Open(string path)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); var stream = new FileStream(ZIPDirectory.ResolvePath(path), FileMode.Open, System.IO.FileAccess.Read, FileShare.Read);
            ZipArchive? archive = null;
            try
            {
                var info = ZIPDirectory.Read(stream); var flags = ZIPDirectory.Flags(stream, info);
                stream.Position = info.Prefix; archive = new ZipArchive(new ZIPViewStream(stream, info.Prefix), ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding: ZIPDirectory.UTF8);
                var entries = archive.Entries.ToArray();
                if (entries.Length != flags.Length) throw new InvalidDataException("ZIP entry count does not match its directory.");
                CloseCore(); _stream = stream; _archive = archive; _entries = entries; _flags = flags;
            }
            catch { archive?.Dispose(); stream.Dispose(); throw; }
        }
    }
    /// <summary>Closes the current session; repeated calls are harmless.</summary>
    /// <exception cref="ObjectDisposedException">This reader is disposed.</exception>
    public void Close() { lock (_gate) { ThrowIfDisposed(); CloseCore(); } }
    private void CloseCore()
    {
        try { _archive?.Dispose(); }
        finally { _archive = null; _entries = []; _flags = []; _stream?.Dispose(); _stream = null; }
    }
    private void RequireOpen() { ThrowIfDisposed(); if (_archive is null) throw new InvalidOperationException("Open a ZIP archive first."); }
    private int Find(string path, bool caseSensitive)
    {
        ArgumentNullException.ThrowIfNull(path); var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        for (var i = 0; i < _entries.Length; i++) if (string.Equals(_entries[i].FullName, path, comparison)) return i;
        return -1;
    }
    /// <summary>Returns names in central-directory order, including directories and duplicate names.</summary>
    /// <returns>A caller-owned array of archive names.</returns>
    /// <exception cref="InvalidOperationException">No session is open.</exception>
    /// <exception cref="ObjectDisposedException">This reader is disposed.</exception>
    public string[] GetFiles() { lock (_gate) { RequireOpen(); var names = new string[_entries.Length]; for (var i = 0; i < names.Length; i++) names[i] = _entries[i].FullName; return names; } }
    /// <summary>Tests whether an archive name exists.</summary>
    /// <param name="path">Literal archive name.</param>
    /// <param name="caseSensitive">True uses ordinal exact matching; false uses ordinal case-insensitive matching.</param>
    /// <returns>True for a matching name, regardless of its compression method.</returns>
    /// <exception cref="InvalidOperationException">No session is open.</exception>
    /// <exception cref="ObjectDisposedException">This reader is disposed.</exception>
    public bool FileExists(string path, bool caseSensitive = true) { lock (_gate) { RequireOpen(); return Find(path, caseSensitive) >= 0; } }
    /// <summary>Returns the archive's compression-speed hint.</summary>
    /// <param name="path">Literal archive name.</param>
    /// <param name="caseSensitive">Selects ordinal exact or case-insensitive matching.</param>
    /// <returns>1, 2, 6 or 9 from ZIP flags; -1 when the name is absent. ZIP does not retain every original encoder level.</returns>
    /// <exception cref="InvalidOperationException">No session is open.</exception>
    /// <exception cref="ObjectDisposedException">This reader is disposed.</exception>
    public int GetCompressionLevel(string path, bool caseSensitive = true)
    { lock (_gate) { RequireOpen(); var index = Find(path, caseSensitive); return index < 0 ? -1 : (_flags[index] & 6) switch { 6 => 1, 4 => 2, 2 => 9, _ => 6 }; } }
    /// <summary>Reads one complete entry and validates its decoded length and CRC.</summary>
    /// <param name="path">Literal archive name; the first matching duplicate is selected.</param>
    /// <param name="caseSensitive">Selects ordinal exact or case-insensitive matching.</param>
    /// <returns>A caller-owned byte array, empty for an empty file or directory.</returns>
    /// <exception cref="FileNotFoundException">The name is absent.</exception>
    /// <exception cref="InvalidDataException">Data is corrupt, encrypted, unsupported, truncated or too large for a managed array.</exception>
    /// <exception cref="InvalidOperationException">No session is open.</exception>
    /// <exception cref="ObjectDisposedException">This reader is disposed.</exception>
    public byte[] ReadFile(string path, bool caseSensitive = true)
    {
        lock (_gate)
        {
            RequireOpen(); var index = Find(path, caseSensitive); if (index < 0) throw new FileNotFoundException("The ZIP entry is absent.", path);
            return ReadFileAt(index);
        }
    }
    internal byte[] ReadFileAt(int index)
    {
        lock (_gate)
        {
            RequireOpen();
            var entry = _entries[index]; if (entry.Length > Array.MaxLength) throw new InvalidDataException("ZIP entry exceeds managed array capacity.");
            using var decoded = entry.Open(); var bytes = GC.AllocateUninitializedArray<byte>((int)entry.Length); var read = 0; uint crc = uint.MaxValue;
            while (read < bytes.Length)
            {
                var count = decoded.Read(bytes.AsSpan(read)); if (count == 0) throw new InvalidDataException("ZIP entry ended before its declared length.");
                crc = ZIPDirectory.UpdateCRC(crc, bytes.AsSpan(read, count)); read += count;
            }
            if (decoded.ReadByte() != -1 || ~crc != entry.Crc32) throw new InvalidDataException("ZIP entry length or CRC is invalid.");
            return bytes;
        }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { try { lock (_gate) CloseCore(); } finally { base.Dispose(disposing); } }
}
