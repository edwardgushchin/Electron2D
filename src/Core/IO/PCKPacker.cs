using System.Security.Cryptography;

namespace Electron2D;

/// <summary>Creates aligned PCK resource packages, encrypted entries and patch removal records.</summary>
/// <remarks>Operations are serialized, synchronous asset preparation. Entries are written immediately; Flush or
/// deterministic disposal writes the directory and closes the session. Do not modify a mounted pack file.</remarks>
public sealed class PCKPacker : ElectronObject
{
    private sealed record Entry(string Path, long Offset, long Length, byte[] MD5, uint Flags);
    private readonly object _gate = new();
    private readonly List<Entry> _entries = [];
    private FileAccess? _file;
    private Stream? _stream;
    private byte[] _key = new byte[32];
    private int _alignment;
    private long _fileBase;
    private bool _encryptDirectory, _faulted;

    /// <summary>Creates a closed resource package writer.</summary>
    public PCKPacker() { }

    /// <summary>Starts a new package after validating the options.</summary>
    /// <param name="pckPath">Ordinary, res:// or user:// destination path.</param>
    /// <param name="alignment">Positive byte alignment for the file region and every entry.</param>
    /// <param name="key">Exactly 64 hexadecimal characters defining the AES-256 key.</param>
    /// <param name="encryptDirectory">Whether the directory uses the key as well as encrypted entries.</param>
    /// <exception cref="ArgumentException">The key or path is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Alignment is not positive.</exception>
    /// <exception cref="IOException">A previous session cannot flush or the destination cannot open.</exception>
    /// <exception cref="ObjectDisposedException">This writer is disposed.</exception>
    /// <remarks>The OS save-and-swap policy defers publication until successful finalization; faulted staging is cancelled.</remarks>
    public void PCKStart(string pckPath, int alignment = 32, string key = "0000000000000000000000000000000000000000000000000000000000000000", bool encryptDirectory = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alignment);
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length != 64 || !key.All(char.IsAsciiHexDigit)) throw new ArgumentException("A PCK key requires 64 hexadecimal characters.", nameof(key));
        ArgumentException.ThrowIfNullOrEmpty(pckPath);
        lock (_gate)
        {
            ThrowIfDisposed(); Finish();
            _file = FileAccess.Open(pckPath, FileAccessModeFlags.Write); _stream = _file.BorrowStream;
            _key = Convert.FromHexString(key); _alignment = alignment; _encryptDirectory = encryptDirectory; _faulted = false;
            try
            {
                using var writer = new BinaryWriter(_stream, PCKFormat.UTF8, leaveOpen: true);
                writer.Write(PCKFormat.Magic); writer.Write(4u); writer.Write(4u); writer.Write(6u); writer.Write(0u);
                writer.Write(encryptDirectory ? 3u : 2u); writer.Write(0L); writer.Write(0L);
                for (var i = 0; i < 16; i++) writer.Write(0u);
                PCKFormat.Align(_stream, alignment); _fileBase = _stream.Position;
                _stream.Position = 24; writer.Write(_fileBase); _stream.Position = _fileBase;
            }
            catch { _faulted = true; Release(); throw; }
        }
    }

    /// <summary>Adds a source file immediately at a normalized resource path.</summary>
    /// <param name="targetPath">Relative or res:// entry name; lexical traversal cannot escape the resource root.</param>
    /// <param name="sourcePath">Readable ordinary, res:// or user:// source file.</param>
    /// <param name="encrypt">Whether to encrypt these bytes with the session key.</param>
    /// <exception cref="InvalidOperationException">No writable session is active.</exception>
    /// <exception cref="IOException">The source or destination cannot be read or written.</exception>
    /// <exception cref="ArgumentException">A path is invalid.</exception>
    /// <exception cref="ObjectDisposedException">This writer is disposed.</exception>
    public void AddFile(string targetPath, string sourcePath, bool encrypt = false)
    {
        lock (_gate) { RequireOpen(); var name = PCKFormat.Normalize(targetPath); var bytes = FileAccess.GetFileAsBytes(sourcePath); Add(name, bytes, encrypt); }
    }

    /// <summary>Consumes borrowed bytes immediately without retaining the caller's buffer.</summary>
    /// <param name="targetPath">Relative or res:// entry name.</param>
    /// <param name="data">Bytes to write, including an empty file.</param>
    /// <param name="encrypt">Whether to encrypt this entry with the session key.</param>
    /// <exception cref="InvalidOperationException">No writable session is active.</exception>
    /// <exception cref="IOException">Destination writing fails.</exception>
    /// <exception cref="ArgumentException">The target is invalid.</exception>
    /// <exception cref="ObjectDisposedException">This writer is disposed.</exception>
    public void AddFileFromBuffer(string targetPath, ReadOnlySpan<byte> data, bool encrypt = false)
    {
        lock (_gate) { RequireOpen(); Add(PCKFormat.Normalize(targetPath), data, encrypt); }
    }

    private void Add(string name, ReadOnlySpan<byte> data, bool encrypt)
    {
        var stream = _stream!; var entry = new Entry(name, stream.Position - _fileBase, data.Length, MD5.HashData(data), encrypt ? 1u : 0u);
        try
        {
            if (encrypt) PCKFormat.Encrypt(stream, data, _key); else stream.Write(data);
            PCKFormat.Align(stream, _alignment); _entries.Add(entry);
        }
        catch { _faulted = true; throw; }
    }

    /// <summary>Records removal of a previously mounted packed file at this path.</summary>
    /// <param name="targetPath">Relative or res:// entry name.</param>
    /// <exception cref="InvalidOperationException">No writable session is active.</exception>
    /// <exception cref="ArgumentException">The target is invalid.</exception>
    /// <exception cref="ObjectDisposedException">This writer is disposed.</exception>
    public void AddFileRemoval(string targetPath)
    {
        lock (_gate) { RequireOpen(); _entries.Add(new(PCKFormat.Normalize(targetPath), 0, 0, new byte[16], 2)); }
    }

    /// <summary>Writes the directory and closes the active package.</summary>
    /// <param name="verbose">Whether to print the index and resource path of every recorded entry.</param>
    /// <exception cref="InvalidOperationException">No writable session is active.</exception>
    /// <exception cref="IOException">Writing or closing fails.</exception>
    /// <exception cref="ObjectDisposedException">This writer is disposed.</exception>
    public void Flush(bool verbose = false) { lock (_gate) { RequireOpen(); Finish(verbose); } }

    private void RequireOpen() { ThrowIfDisposed(); if (_stream is null || _faulted) throw new InvalidOperationException("Start a PCK session before adding or flushing entries."); }

    private void Finish(bool verbose = false)
    {
        if (_stream is null) return;
        var complete = false;
        try
        {
            if (_faulted) return;
            using var directory = new MemoryStream();
            using (var writer = new BinaryWriter(directory, PCKFormat.UTF8, leaveOpen: true))
            {
                for (var i = 0; i < _entries.Count; i++)
                {
                    var e = _entries[i]; var bytes = PCKFormat.UTF8.GetBytes(e.Path); var padded = checked((bytes.Length + 3) / 4 * 4);
                    writer.Write(padded); writer.Write(bytes); for (var j = bytes.Length; j < padded; j++) writer.Write((byte)0);
                    writer.Write(e.Offset); writer.Write(e.Length); writer.Write(e.MD5); writer.Write(e.Flags);
                    if (directory.Length > PCKFormat.DirectoryLimit) throw new IOException("The PCK directory exceeds its size budget.");
                    if (verbose) OS.LogMessage($"[{i + 1}/{_entries.Count}] PCKPacker flush: res://{e.Path}");
                }
            }
            var start = _stream.Position;
            using var header = new BinaryWriter(_stream, PCKFormat.UTF8, leaveOpen: true);
            _stream.Position = 32; header.Write(start); _stream.Position = start; header.Write(_entries.Count);
            if (_encryptDirectory) PCKFormat.Encrypt(_stream, directory.GetBuffer().AsSpan(0, (int)directory.Length), _key);
            else directory.WriteTo(_stream);
            _stream.Flush(); complete = true;
        }
        finally { Release(complete); }
    }

    private void Release(bool complete = false)
    {
        var file = _file; _file = null; _stream = null; _entries.Clear(); CryptographicOperations.ZeroMemory(_key);
        if (!complete) file?.CancelPendingSave();
        file?.Dispose();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing) { try { lock (_gate) Finish(); } finally { base.Dispose(disposing); } }
}
