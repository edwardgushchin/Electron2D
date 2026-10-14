using System.IO.Compression;

namespace Electron2D;

/// <summary>Streams standard UTF-8 ZIP archives with deflate, permissions, timestamps and ZIP64 size support.</summary>
/// <remarks>Operations serialize on the instance and may block or allocate. File changes are not transactional.
/// StartFile and AddDirectory finish any previous entry. Close and disposal finish the archive and release its file.</remarks>
public sealed class ZIPPacker : ElectronObject
{
    private sealed class Entry(byte[] name, long offset, uint stamp, uint attributes, ushort flags)
    {
        internal readonly byte[] Name = name;
        internal readonly long Offset = offset;
        internal readonly uint Stamp = stamp, Attributes = attributes;
        internal readonly ushort Flags = flags;
        internal long Length, Compressed;
        internal uint CRC = uint.MaxValue;
    }
    private readonly object _gate = new();
    private readonly List<Entry> _entries = [];
    private readonly HashSet<string> _directories = new(StringComparer.Ordinal);
    private FileStream? _stream;
    private DeflateStream? _deflate;
    private Entry? _current;
    private long _dataStart, _prefix;
    private ZIPDirectoryInfo _old;
    private int _compression = -1;
    private bool _faulted;
    /// <summary>Creates a closed archive writer with default compression.</summary>
    public ZIPPacker() { }
    /// <summary>Gets or sets the level used when the next entry starts.</summary>
    /// <value>-1 initially; every integer from -1 through 9 is valid. Zero writes uncompressed deflate blocks.</value>
    /// <exception cref="ArgumentOutOfRangeException">The level is outside -1 through 9.</exception>
    /// <exception cref="ObjectDisposedException">This writer is disposed.</exception>
    public int CompressionLevel { get { lock (_gate) { ThrowIfDisposed(); return _compression; } } set { lock (_gate) { ThrowIfDisposed(); if (value is < -1 or > 9) throw new ArgumentOutOfRangeException(nameof(value)); _compression = value; } } }
    /// <summary>Opens a destination for creation, creation after existing bytes, or appending archive entries.</summary>
    /// <param name="path">Ordinary, res:// or user:// destination path.</param>
    /// <param name="append">Creation policy; Create truncates an existing destination.</param>
    /// <exception cref="ArgumentOutOfRangeException">The policy is unknown.</exception>
    /// <exception cref="InvalidDataException">AddInZIP selected an invalid or split archive.</exception>
    /// <exception cref="IOException">The destination cannot be opened.</exception>
    /// <exception cref="ObjectDisposedException">This writer is disposed.</exception>
    public void Open(string path, ZIPAppend append = ZIPAppend.Create)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); if (!Enum.IsDefined(append)) throw new ArgumentOutOfRangeException(nameof(append)); var absolute = ZIPDirectory.ResolvePath(path); CloseCore();
            var stream = new FileStream(absolute, append == ZIPAppend.Create ? FileMode.Create : append == ZIPAppend.CreateAfter ? FileMode.OpenOrCreate : FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.Read);
            try
            {
                var old = append == ZIPAppend.AddInZIP ? ZIPDirectory.Read(stream) : default;
                if (append == ZIPAppend.AddInZIP) ZIPDirectory.Flags(stream, old);
                _prefix = append == ZIPAppend.CreateAfter ? stream.Length : old.Prefix; _old = old; stream.Position = stream.Length; _stream = stream; _faulted = false;
            }
            catch { stream.Dispose(); throw; }
        }
    }
    private void RequireOpen() { ThrowIfDisposed(); if (_stream is null) throw new InvalidOperationException("Open a ZIP destination first."); if (_faulted) throw new InvalidOperationException("The ZIP session failed; close it before reopening."); }
    private static (uint Stamp, uint Attributes) Metadata(UnixPermissionFlags permissions, ulong modifiedTime, bool directory)
    {
        if (((uint)permissions & ~4095u) != 0) throw new ArgumentOutOfRangeException(nameof(permissions));
        var time = modifiedTime == 0 ? DateTime.Now : DateTimeOffset.FromUnixTimeSeconds(checked((long)modifiedTime)).LocalDateTime;
        if (time.Year < 1980) time = new DateTime(1980, 1, 1); else if (time.Year > 2107) time = new DateTime(2107, 12, 31, 23, 59, 58);
        var stamp = (uint)((time.Year - 1980) << 25 | time.Month << 21 | time.Day << 16 | time.Hour << 11 | time.Minute << 5 | time.Second / 2);
        var mode = (uint)permissions; if (mode == 0) mode = directory ? 493u : 420u; mode |= directory ? 0x4000u : 0x8000u;
        return (stamp, mode << 16 | (directory ? 16u : 0u) | ((mode & 128) == 0 ? 1u : 0u));
    }
    /// <summary>Starts a named file and automatically adds its immediate parent directory when needed.</summary>
    /// <param name="path">UTF-8 archive name; names are not filesystem extraction destinations.</param>
    /// <param name="permissions">Unix mode bits; zero selects 0644.</param>
    /// <param name="modifiedTime">Unix seconds; zero uses current local time. DOS timestamps have two-second resolution and clamp to 1980–2107.</param>
    /// <exception cref="ArgumentException">The name is empty, contains a null or exceeds ZIP's UTF-8 name limit.</exception>
    /// <exception cref="InvalidOperationException">No destination is open, or the session failed.</exception>
    /// <exception cref="ObjectDisposedException">This writer is disposed.</exception>
    public void StartFile(string path, UnixPermissionFlags permissions = (UnixPermissionFlags)420, ulong modifiedTime = 0)
    {
        lock (_gate)
        {
            RequireOpen(); var name = ZIPDirectory.Name(path); var metadata = Metadata(permissions, modifiedTime, false);
            var separator = path.LastIndexOfAny(['/', '\\']); var parent = separator > 0 ? path[..separator] + "/" : null;
            if (parent is not null && !_directories.Contains(parent)) AddDirectory(parent, (UnixPermissionFlags)493, modifiedTime);
            Start(name, metadata.Stamp, metadata.Attributes);
        }
    }
    /// <summary>Adds an empty directory entry and finishes any active file.</summary>
    /// <param name="path">Directory archive name; a trailing slash is added.</param>
    /// <param name="permissions">Unix mode bits; zero selects 0755.</param>
    /// <param name="modifiedTime">Unix seconds; zero uses current time, with the same DOS range as StartFile.</param>
    /// <exception cref="IOException">The directory was already added in this session, or writing fails.</exception>
    /// <exception cref="InvalidOperationException">No destination is open, or the session failed.</exception>
    /// <exception cref="ObjectDisposedException">This writer is disposed.</exception>
    public void AddDirectory(string path, UnixPermissionFlags permissions = (UnixPermissionFlags)493, ulong modifiedTime = 0)
    {
        lock (_gate)
        {
            RequireOpen(); ArgumentException.ThrowIfNullOrEmpty(path); path = path.EndsWith('/') ? path : path + "/";
            var name = ZIPDirectory.Name(path); var metadata = Metadata(permissions, modifiedTime, true);
            if (_directories.Contains(path)) throw new IOException("The ZIP directory was already added in this session.");
            Start(name, metadata.Stamp, metadata.Attributes); FinishEntry(); _directories.Add(path);
        }
    }
    private void Start(byte[] name, uint stamp, uint attributes)
    {
        FinishEntry(); var stream = _stream!; var flags = (ushort)(0x800 | (_compression switch { 1 => 6, 2 => 4, 8 or 9 => 2, _ => 0 }));
        var entry = new Entry(name, stream.Position - _prefix, stamp, attributes, flags); Span<byte> header = stackalloc byte[30]; header.Clear();
        ZIPDirectory.W32(header, 0, 0x04034b50); ZIPDirectory.W16(header, 4, 45); ZIPDirectory.W16(header, 6, flags); ZIPDirectory.W16(header, 8, 8); ZIPDirectory.W32(header, 10, stamp);
        ZIPDirectory.W32(header, 18, uint.MaxValue); ZIPDirectory.W32(header, 22, uint.MaxValue); ZIPDirectory.W16(header, 26, (ushort)name.Length); ZIPDirectory.W16(header, 28, 20);
        try
        {
            stream.Write(header); stream.Write(name); Span<byte> extra = stackalloc byte[20]; extra.Clear(); ZIPDirectory.W16(extra, 0, 1); ZIPDirectory.W16(extra, 2, 16); stream.Write(extra);
            _dataStart = stream.Position; _deflate = new DeflateStream(stream, new ZLibCompressionOptions { CompressionLevel = _compression }, leaveOpen: true); _current = entry;
        }
        catch { _faulted = true; throw; }
    }
    /// <summary>Writes borrowed bytes to the currently open entry.</summary>
    /// <param name="data">Bytes consumed synchronously; the caller retains the span.</param>
    /// <exception cref="InvalidOperationException">No file is active, or the session failed.</exception>
    /// <exception cref="IOException">Compression or destination writing fails.</exception>
    /// <exception cref="ObjectDisposedException">This writer is disposed.</exception>
    public void WriteFile(ReadOnlySpan<byte> data)
    {
        lock (_gate)
        {
            RequireOpen(); if (_current is null) throw new InvalidOperationException("Start a ZIP file first.");
            try { var length = checked(_current.Length + data.Length); _deflate!.Write(data); _current.CRC = ZIPDirectory.UpdateCRC(_current.CRC, data); _current.Length = length; }
            catch { _faulted = true; throw; }
        }
    }
    /// <summary>Finishes the active entry, recording its CRC and final sizes.</summary>
    /// <exception cref="InvalidOperationException">No file is active, or the session failed.</exception>
    /// <exception cref="IOException">Compression or destination writing fails.</exception>
    /// <exception cref="ObjectDisposedException">This writer is disposed.</exception>
    public void CloseFile() { lock (_gate) { RequireOpen(); if (_current is null) throw new InvalidOperationException("No ZIP file is active."); FinishEntry(); } }
    private void FinishEntry()
    {
        if (_current is not { } entry) return;
        try
        {
            _deflate!.Dispose(); _deflate = null; var stream = _stream!;
            if (entry.Length == 0 && stream.Position == _dataStart) stream.Write([3, 0]);
            entry.Compressed = stream.Position - _dataStart; entry.CRC = ~entry.CRC; var end = stream.Position;
            stream.Position = _prefix + entry.Offset + 14; Span<byte> crc = stackalloc byte[4]; ZIPDirectory.W32(crc, 0, entry.CRC); stream.Write(crc);
            stream.Position = _prefix + entry.Offset + 30 + entry.Name.Length + 4; Span<byte> sizes = stackalloc byte[16]; ZIPDirectory.W64(sizes, 0, (ulong)entry.Length); ZIPDirectory.W64(sizes, 8, (ulong)entry.Compressed); stream.Write(sizes);
            stream.Position = end; _entries.Add(entry); _current = null;
        }
        catch { _faulted = true; throw; }
    }
    /// <summary>Finishes the current entry and archive; repeated calls on a closed writer are harmless.</summary>
    /// <exception cref="IOException">Final archive writing fails.</exception>
    /// <exception cref="ObjectDisposedException">This writer is disposed.</exception>
    public void Close() { lock (_gate) { ThrowIfDisposed(); CloseCore(); } }
    private void CloseCore()
    {
        if (_stream is not { } stream) return;
        try
        {
            if (_faulted) throw new IOException("The failed ZIP session cannot be finalized.");
            FinishEntry(); var start = stream.Position; ZIPDirectory.CopyDirectory(stream, _old);
            foreach (var entry in _entries) WriteCentral(stream, entry);
            var size = stream.Position - start; var offset = start - _prefix; var count = checked(_old.Count + _entries.Count);
            if (count >= ushort.MaxValue || size >= uint.MaxValue || offset >= uint.MaxValue)
            {
                var position = stream.Position - _prefix; Span<byte> end64 = stackalloc byte[56]; end64.Clear(); ZIPDirectory.W32(end64, 0, 0x06064b50); ZIPDirectory.W64(end64, 4, 44); ZIPDirectory.W16(end64, 12, 0x032d); ZIPDirectory.W16(end64, 14, 45);
                ZIPDirectory.W64(end64, 24, (ulong)count); ZIPDirectory.W64(end64, 32, (ulong)count); ZIPDirectory.W64(end64, 40, (ulong)size); ZIPDirectory.W64(end64, 48, (ulong)offset); stream.Write(end64);
                Span<byte> locator = stackalloc byte[20]; locator.Clear(); ZIPDirectory.W32(locator, 0, 0x07064b50); ZIPDirectory.W64(locator, 8, (ulong)position); ZIPDirectory.W32(locator, 16, 1); stream.Write(locator);
            }
            Span<byte> endRecord = stackalloc byte[22]; endRecord.Clear(); ZIPDirectory.W32(endRecord, 0, 0x06054b50); ZIPDirectory.W16(endRecord, 8, (ushort)Math.Min(count, ushort.MaxValue)); ZIPDirectory.W16(endRecord, 10, (ushort)Math.Min(count, ushort.MaxValue));
            ZIPDirectory.W32(endRecord, 12, (uint)Math.Min(size, uint.MaxValue)); ZIPDirectory.W32(endRecord, 16, (uint)Math.Min(offset, uint.MaxValue)); ZIPDirectory.W16(endRecord, 20, (ushort)(_old.Comment?.Length ?? 0)); stream.Write(endRecord); if (_old.Comment is { } comment) stream.Write(comment); stream.Flush();
        }
        finally
        {
            try { _deflate?.Dispose(); } finally { _deflate = null; _current = null; _stream = null; _entries.Clear(); _directories.Clear(); _old = default; _prefix = 0; _faulted = false; stream.Dispose(); }
        }
    }
    private static void WriteCentral(Stream stream, Entry entry)
    {
        Span<byte> header = stackalloc byte[46]; header.Clear(); Span<byte> extra = stackalloc byte[28]; extra.Clear(); var extraSize = 0;
        var length64 = entry.Length >= uint.MaxValue; var compressed64 = entry.Compressed >= uint.MaxValue; var offset64 = entry.Offset >= uint.MaxValue;
        if (length64) { ZIPDirectory.W64(extra, 4 + extraSize, (ulong)entry.Length); extraSize += 8; }
        if (compressed64) { ZIPDirectory.W64(extra, 4 + extraSize, (ulong)entry.Compressed); extraSize += 8; }
        if (offset64) { ZIPDirectory.W64(extra, 4 + extraSize, (ulong)entry.Offset); extraSize += 8; }
        ZIPDirectory.W32(header, 0, 0x02014b50); ZIPDirectory.W16(header, 4, 0x032d); ZIPDirectory.W16(header, 6, 45); ZIPDirectory.W16(header, 8, entry.Flags); ZIPDirectory.W16(header, 10, 8); ZIPDirectory.W32(header, 12, entry.Stamp); ZIPDirectory.W32(header, 16, entry.CRC);
        ZIPDirectory.W32(header, 20, compressed64 ? uint.MaxValue : (uint)entry.Compressed); ZIPDirectory.W32(header, 24, length64 ? uint.MaxValue : (uint)entry.Length); ZIPDirectory.W16(header, 28, (ushort)entry.Name.Length); ZIPDirectory.W16(header, 30, extraSize == 0 ? (ushort)0 : (ushort)(extraSize + 4)); ZIPDirectory.W32(header, 38, entry.Attributes); ZIPDirectory.W32(header, 42, offset64 ? uint.MaxValue : (uint)entry.Offset);
        stream.Write(header); stream.Write(entry.Name); if (extraSize != 0) { ZIPDirectory.W16(extra, 0, 1); ZIPDirectory.W16(extra, 2, (ushort)extraSize); stream.Write(extra[..(extraSize + 4)]); }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { try { lock (_gate) CloseCore(); } finally { base.Dispose(disposing); } }
}
