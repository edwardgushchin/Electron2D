using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using IoFileAccess = System.IO.FileAccess;

namespace Electron2D;

/// <summary>Provides seekable binary and text access to ordinary and directory-backed virtual files.</summary>
/// <remarks>
/// Instances own their stream until <see cref="Close"/> or disposal. Individual operations are serialized, but a
/// multi-call seek/read/write sequence is not atomic. File operations may block and are unsuitable for real-time frame
/// callbacks. Compressed and encrypted files are authenticated or decoded completely before their contents are exposed.
/// </remarks>
public sealed class FileAccess : ElectronObject
{
    private const byte ContainerVersion = 1;
    private const byte RawKeyEncryption = 0;
    private const byte PasswordEncryption = 1;
    private const int EncryptionKeySize = 32;
    private const int EncryptionSaltSize = 16;
    private const int EncryptionNonceSize = 12;
    private const int EncryptionTagSize = 16;
    private const int PasswordIterations = 600_000;

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static ReadOnlySpan<byte> CompressionMagic => "E2DCMP"u8;
    private static ReadOnlySpan<byte> EncryptionMagic => "E2DFILE"u8;

    private readonly object _gate = new();
    private readonly string _path;
    private readonly string _absolutePath;
    private readonly FileAccessMode _mode;
    private Stream? _stream;
    private Func<byte[], byte[]>? _encoder;
    private byte[]? _encryptionKey;
    private volatile bool _bigEndian;
    private bool _eofReached;
    private bool _dirty;
    private bool _deleteOnClose;

    private FileAccess(
        string path,
        string absolutePath,
        FileAccessMode mode,
        Stream stream,
        Func<byte[], byte[]>? encoder = null)
    {
        _path = path;
        _absolutePath = absolutePath;
        _mode = mode;
        _stream = stream;
        _encoder = encoder;
        _dirty = encoder is not null && Truncates(mode);
    }

    /// <summary>Gets or sets whether multi-byte numeric values use big-endian byte order.</summary>
    /// <value><see langword="false"/> for little-endian order by default.</value>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public bool BigEndian
    {
        get
        {
            ThrowIfDisposed();
            lock (_gate)
            {
                ThrowIfDisposed();
                return _bigEndian;
            }
        }
        set
        {
            ThrowIfDisposed();
            lock (_gate)
            {
                ThrowIfDisposed();
                _bigEndian = value;
            }
        }
    }

    /// <summary>Gets the path supplied when this file was opened.</summary>
    /// <value>The original virtual or ordinary path.</value>
    public string Path => _path;

    /// <summary>Gets the normalized absolute operating-system path.</summary>
    /// <value>The resolved path used for physical file operations.</value>
    public string AbsolutePath => _absolutePath;

    /// <summary>Gets whether the owned stream is still open.</summary>
    /// <value><see langword="true"/> until <see cref="Close"/> succeeds or releases the stream after a failure.</value>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public bool IsOpen
    {
        get
        {
            ThrowIfDisposed();
            lock (_gate)
            {
                ThrowIfDisposed();
                return _stream is not null;
            }
        }
    }

    /// <summary>Gets whether a read has attempted to move beyond the end of the file.</summary>
    /// <value><see langword="true"/> only after an incomplete read; a successful seek clears it.</value>
    /// <exception cref="InvalidOperationException">The file is closed.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public bool EofReached
    {
        get
        {
            ThrowIfDisposed();
            lock (_gate)
            {
                EnsureOpen();
                return _eofReached;
            }
        }
    }

    /// <summary>Gets the current byte offset.</summary>
    /// <value>A zero-based position from the beginning of the decoded file.</value>
    /// <exception cref="InvalidOperationException">The file is closed.</exception>
    /// <exception cref="IOException">The stream cannot report its cursor.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public long Position
    {
        get
        {
            ThrowIfDisposed();
            lock (_gate)
                return EnsureOpen().Position;
        }
    }

    /// <summary>Gets the decoded file length in bytes.</summary>
    /// <value>The number of bytes visible through this instance.</value>
    /// <exception cref="InvalidOperationException">The file is closed.</exception>
    /// <exception cref="IOException">The stream cannot report its length.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public long Length
    {
        get
        {
            ThrowIfDisposed();
            lock (_gate)
                return EnsureOpen().Length;
        }
    }

    /// <summary>Gets or sets whether the physical file has the hidden attribute.</summary>
    /// <value>The current filesystem hidden-attribute state.</value>
    /// <exception cref="InvalidOperationException">The file is closed.</exception>
    /// <exception cref="IOException">The filesystem cannot read or change the attribute.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access.</exception>
    /// <exception cref="PlatformNotSupportedException">The current platform does not expose this file attribute.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public bool Hidden
    {
        get
        {
            ThrowIfDisposed();
            lock (_gate)
            {
                EnsureOpen();
                return IsHidden(_absolutePath);
            }
        }
        set
        {
            ThrowIfDisposed();
            lock (_gate)
            {
                EnsureOpen();
                SetHidden(_absolutePath, value);
            }
        }
    }

    /// <summary>Gets or sets whether the physical file has the read-only attribute.</summary>
    /// <value>The current filesystem read-only attribute state.</value>
    /// <exception cref="InvalidOperationException">The file is closed.</exception>
    /// <exception cref="IOException">The filesystem cannot read or change the attribute.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access.</exception>
    /// <exception cref="PlatformNotSupportedException">The current platform does not expose this file attribute.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public bool ReadOnly
    {
        get
        {
            ThrowIfDisposed();
            lock (_gate)
            {
                EnsureOpen();
                return IsReadOnly(_absolutePath);
            }
        }
        set
        {
            ThrowIfDisposed();
            lock (_gate)
            {
                EnsureOpen();
                SetReadOnly(_absolutePath, value);
            }
        }
    }

    /// <summary>Gets or sets Unix permission and special-mode bits for the physical file.</summary>
    /// <value>The current Unix mode bits.</value>
    /// <exception cref="InvalidOperationException">The file is closed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value contains unknown bits.</exception>
    /// <exception cref="IOException">The filesystem cannot read or change the mode.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access.</exception>
    /// <exception cref="PlatformNotSupportedException">The current platform does not expose Unix file modes.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public UnixPermissionFlags UnixPermissions
    {
        get
        {
            ThrowIfDisposed();
            lock (_gate)
            {
                EnsureOpen();
                return GetUnixPermissions(_absolutePath);
            }
        }
        set
        {
            ThrowIfDisposed();
            lock (_gate)
            {
                EnsureOpen();
                SetUnixPermissions(_absolutePath, value);
            }
        }
    }

    /// <summary>Opens a file with the requested access mode.</summary>
    /// <param name="path">A nonempty ordinary, <c>res://</c>, or <c>user://</c> path.</param>
    /// <param name="mode">The required read, write, truncation, and creation behavior.</param>
    /// <returns>A new owner of the opened file stream.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The path is empty or <paramref name="mode"/> is invalid.</exception>
    /// <exception cref="IOException">The file cannot be opened.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access.</exception>
    /// <exception cref="NotSupportedException">The path uses an unsupported virtual scheme.</exception>
    public static FileAccess Open(string path, FileAccessMode mode)
    {
        var absolutePath = ResolvePath(path);
        ValidateMode(mode);
        var stream = OpenPhysical(absolutePath, mode);
        return new FileAccess(path, absolutePath, mode, stream);
    }

    /// <summary>Creates a uniquely named temporary file and opens it.</summary>
    /// <param name="mode">The permitted operations.</param>
    /// <param name="prefix">An optional filename prefix without directory separators. A hyphen separates it from the random name.</param>
    /// <param name="extension">An optional extension, with or without a leading period.</param>
    /// <param name="keep">Whether closing the returned instance preserves the file.</param>
    /// <returns>An open temporary file owner.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="prefix"/> or <paramref name="extension"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A filename part is invalid or the mode is invalid.</exception>
    /// <exception cref="IOException">A temporary file cannot be created.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks access to the temporary directory.</exception>
    public static FileAccess CreateTemp(
        FileAccessMode mode = FileAccessMode.ReadWrite,
        string prefix = "",
        string extension = "",
        bool keep = false)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(extension);
        ValidateMode(mode);
        ValidateTemporaryFilePart(prefix, nameof(prefix));
        ValidateTemporaryFilePart(extension, nameof(extension));
        var normalizedExtension = extension.TrimStart('.');

        for (var attempt = 0; attempt < 64; attempt++)
        {
            var randomName = RandomNumberGenerator.GetHexString(16);
            var fileName = $"{(prefix.Length == 0 ? "" : prefix + "-")}{randomName}{(normalizedExtension.Length == 0 ? "" : "." + normalizedExtension)}";
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                fileName);
            try
            {
                using (File.Open(path, FileMode.CreateNew, IoFileAccess.Write, FileShare.None))
                {
                }
            }
            catch (IOException) when (File.Exists(path))
            {
                continue;
            }

            try
            {
                var result = Open(path, mode);
                result._deleteOnClose = !keep;
                return result;
            }
            catch
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                }
                throw;
            }
        }

        throw new IOException("A unique temporary filename could not be allocated.");
    }

    /// <summary>Opens a whole-file compressed container.</summary>
    /// <param name="path">A nonempty ordinary or supported virtual path.</param>
    /// <param name="mode">The required access mode.</param>
    /// <param name="compressionMode">The codec used to decode or encode the container.</param>
    /// <returns>A seekable view of the decoded bytes.</returns>
    /// <remarks>Changes are atomically encoded to the destination by <see cref="Flush"/> or <see cref="Close"/>.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The path is empty or the mode is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="compressionMode"/> is unknown.</exception>
    /// <exception cref="NotSupportedException">The selected codec has no runtime provider.</exception>
    /// <exception cref="InvalidDataException">An existing container is malformed or uses another codec.</exception>
    /// <exception cref="IOException">The physical file cannot be read or replaced.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access.</exception>
    public static FileAccess OpenCompressed(
        string path,
        FileAccessMode mode,
        FileCompressionMode compressionMode = FileCompressionMode.FastLz)
    {
        ValidateCompressionMode(compressionMode);
        var absolutePath = ResolvePath(path);
        ValidateMode(mode);
        var data = Truncates(mode) ? [] : DecodeCompressed(File.ReadAllBytes(absolutePath), compressionMode);
        var stream = CreateMemoryStream(data);
        if (mode == FileAccessMode.Read)
            return new FileAccess(path, absolutePath, mode, stream);

        return new FileAccess(path, absolutePath, mode, stream, bytes => EncodeCompressed(bytes, compressionMode));
    }

    /// <summary>Opens a whole-file container protected by a 256-bit key and authenticated encryption.</summary>
    /// <param name="path">A nonempty ordinary or supported virtual path.</param>
    /// <param name="mode">The required access mode.</param>
    /// <param name="key">Exactly 32 key bytes.</param>
    /// <returns>A seekable view of authenticated plaintext bytes.</returns>
    /// <remarks>Writing uses a fresh random nonce for every atomic commit.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is not exactly 32 bytes or the mode is invalid.</exception>
    /// <exception cref="CryptographicException">Authentication fails.</exception>
    /// <exception cref="InvalidDataException">The encryption envelope is malformed or uses password mode.</exception>
    /// <exception cref="IOException">The physical file cannot be read or replaced.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access.</exception>
    /// <exception cref="PlatformNotSupportedException">AES-GCM is unavailable.</exception>
    public static FileAccess OpenEncrypted(string path, FileAccessMode mode, ReadOnlySpan<byte> key)
    {
        ValidateEncryptionKey(key);
        EnsureEncryptionSupported();
        var absolutePath = ResolvePath(path);
        ValidateMode(mode);
        var data = Truncates(mode) ? [] : Decrypt(File.ReadAllBytes(absolutePath), key, RawKeyEncryption, out _);
        var stream = CreateMemoryStream(data);
        CryptographicOperations.ZeroMemory(data);
        if (mode == FileAccessMode.Read)
            return new FileAccess(path, absolutePath, mode, stream);

        var keyCopy = key.ToArray();
        var result = new FileAccess(
            path,
            absolutePath,
            mode,
            stream,
            bytes => Encrypt(bytes, keyCopy, RawKeyEncryption, ReadOnlySpan<byte>.Empty));
        result._encryptionKey = keyCopy;
        return result;
    }

    /// <summary>Opens a whole-file container protected by a password and authenticated encryption.</summary>
    /// <param name="path">A nonempty ordinary or supported virtual path.</param>
    /// <param name="mode">The required access mode.</param>
    /// <param name="password">A nonempty password.</param>
    /// <returns>A seekable view of authenticated plaintext bytes.</returns>
    /// <remarks>A random salt and PBKDF2-HMAC-SHA-256 derive a 256-bit key; commits use fresh nonces.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="password"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="password"/> is empty or the mode is invalid.</exception>
    /// <exception cref="CryptographicException">Authentication fails.</exception>
    /// <exception cref="InvalidDataException">The encryption envelope is malformed or uses raw-key mode.</exception>
    /// <exception cref="IOException">The physical file cannot be read or replaced.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access.</exception>
    /// <exception cref="PlatformNotSupportedException">AES-GCM is unavailable.</exception>
    public static FileAccess OpenEncryptedWithPassword(string path, FileAccessMode mode, string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        if (password.Length == 0)
            throw new ArgumentException("An encryption password cannot be empty.", nameof(password));
        EnsureEncryptionSupported();
        var absolutePath = ResolvePath(path);
        ValidateMode(mode);

        byte[] salt;
        byte[] data;
        if (Truncates(mode))
        {
            salt = RandomNumberGenerator.GetBytes(EncryptionSaltSize);
            data = [];
        }
        else
        {
            var envelope = File.ReadAllBytes(absolutePath);
            ParseEncryptionEnvelope(envelope, PasswordEncryption, out _, out var saltOffset, out var saltLength);
            salt = envelope.AsSpan(saltOffset, saltLength).ToArray();
            var readKey = DeriveKey(password, salt);
            try
            {
                data = Decrypt(envelope, readKey, PasswordEncryption, out _);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(readKey);
            }
        }

        var stream = CreateMemoryStream(data);
        CryptographicOperations.ZeroMemory(data);
        if (mode == FileAccessMode.Read)
        {
            CryptographicOperations.ZeroMemory(salt);
            return new FileAccess(path, absolutePath, mode, stream);
        }

        var key = DeriveKey(password, salt);
        var result = new FileAccess(
            path,
            absolutePath,
            mode,
            stream,
            bytes => Encrypt(bytes, key, PasswordEncryption, salt));
        result._encryptionKey = key;
        return result;
    }

    /// <summary>Closes the file, committing buffered transformed data when necessary.</summary>
    /// <remarks>The method is idempotent. Owned buffers and key material are released even when commit or deletion fails.</remarks>
    /// <exception cref="IOException">A pending write cannot be committed or a temporary file cannot be deleted.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller cannot replace or delete the physical file.</exception>
    /// <exception cref="AggregateException">Both a primary close operation and cleanup fail.</exception>
    /// <exception cref="ObjectDisposedException">The instance is already disposed.</exception>
    public void Close()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            CloseLocked();
        }
    }

    /// <summary>Flushes pending data to the physical file.</summary>
    /// <remarks>Transformed data is encoded and atomically replaces the destination; ordinary streams are flushed to disk.</remarks>
    /// <exception cref="InvalidOperationException">The file is closed or was opened read-only.</exception>
    /// <exception cref="IOException">The data cannot be persisted.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void Flush()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            FlushLocked();
        }
    }

    /// <summary>Moves the cursor to an absolute byte offset.</summary>
    /// <param name="position">The nonnegative offset from the beginning.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The file is closed.</exception>
    /// <exception cref="IOException">Seeking fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void Seek(long position)
    {
        if (position < 0)
            throw new ArgumentOutOfRangeException(nameof(position));
        ThrowIfDisposed();
        lock (_gate)
        {
            EnsureOpen().Seek(position, SeekOrigin.Begin);
            _eofReached = false;
        }
    }

    /// <summary>Moves the cursor relative to the end of the file.</summary>
    /// <param name="offset">A signed byte offset; zero selects the end and negative values select preceding bytes.</param>
    /// <exception cref="InvalidOperationException">The file is closed.</exception>
    /// <exception cref="IOException">Seeking fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void SeekEnd(long offset = 0)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            EnsureOpen().Seek(offset, SeekOrigin.End);
            _eofReached = false;
        }
    }

    /// <summary>Changes the file length.</summary>
    /// <param name="length">The nonnegative decoded length in bytes.</param>
    /// <remarks>Extending fills the new region with zero bytes. The current cursor is unchanged.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not writable.</exception>
    /// <exception cref="IOException">Resizing fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void Resize(long length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));
        ThrowIfDisposed();
        lock (_gate)
        {
            EnsureWritable();
            EnsureOpen().SetLength(length);
            _dirty = true;
            _eofReached = false;
        }
    }

    /// <summary>Reads one unsigned byte.</summary>
    /// <returns>The next byte.</returns>
    /// <exception cref="EndOfStreamException">No complete value remains.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not readable.</exception>
    /// <exception cref="IOException">Reading fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public byte ReadByte() => ReadScalar(1, bytes => bytes[0]);

    /// <summary>Reads one unsigned 16-bit integer.</summary>
    /// <returns>The next value using <see cref="BigEndian"/> byte order.</returns>
    /// <exception cref="EndOfStreamException">No complete value remains.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not readable.</exception>
    /// <exception cref="IOException">Reading fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public ushort ReadUInt16() => ReadScalar(2, bytes => _bigEndian
        ? BinaryPrimitives.ReadUInt16BigEndian(bytes)
        : BinaryPrimitives.ReadUInt16LittleEndian(bytes));

    /// <summary>Reads one unsigned 32-bit integer.</summary>
    /// <returns>The next value using <see cref="BigEndian"/> byte order.</returns>
    /// <exception cref="EndOfStreamException">No complete value remains.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not readable.</exception>
    /// <exception cref="IOException">Reading fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public uint ReadUInt32() => ReadScalar(4, bytes => _bigEndian
        ? BinaryPrimitives.ReadUInt32BigEndian(bytes)
        : BinaryPrimitives.ReadUInt32LittleEndian(bytes));

    /// <summary>Reads one unsigned 64-bit integer.</summary>
    /// <returns>The next value using <see cref="BigEndian"/> byte order.</returns>
    /// <exception cref="EndOfStreamException">No complete value remains.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not readable.</exception>
    /// <exception cref="IOException">Reading fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public ulong ReadUInt64() => ReadScalar(8, bytes => _bigEndian
        ? BinaryPrimitives.ReadUInt64BigEndian(bytes)
        : BinaryPrimitives.ReadUInt64LittleEndian(bytes));

    /// <summary>Reads an IEEE 754 binary16 value.</summary>
    /// <returns>The next half-precision value.</returns>
    /// <exception cref="EndOfStreamException">No complete value remains.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not readable.</exception>
    /// <exception cref="IOException">Reading fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public Half ReadHalf() => BitConverter.UInt16BitsToHalf(ReadUInt16());

    /// <summary>Reads an IEEE 754 binary32 value.</summary>
    /// <returns>The next single-precision value.</returns>
    /// <exception cref="EndOfStreamException">No complete value remains.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not readable.</exception>
    /// <exception cref="IOException">Reading fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public float ReadSingle() => BitConverter.UInt32BitsToSingle(ReadUInt32());

    /// <summary>Reads an IEEE 754 binary64 value.</summary>
    /// <returns>The next double-precision value.</returns>
    /// <exception cref="EndOfStreamException">No complete value remains.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not readable.</exception>
    /// <exception cref="IOException">Reading fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public double ReadDouble() => BitConverter.UInt64BitsToDouble(ReadUInt64());

    /// <summary>Reads the engine real-number storage format.</summary>
    /// <returns>The next binary32 value used by the engine's 2D numeric types.</returns>
    /// <exception cref="EndOfStreamException">No complete value remains.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not readable.</exception>
    /// <exception cref="IOException">Reading fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public float ReadReal() => ReadSingle();

    /// <summary>Reads up to a requested number of bytes.</summary>
    /// <param name="length">The nonnegative maximum byte count.</param>
    /// <returns>A new array containing all available requested bytes.</returns>
    /// <remarks><see cref="EofReached"/> becomes true when fewer than <paramref name="length"/> bytes are available.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not readable.</exception>
    /// <exception cref="IOException">Reading fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public byte[] ReadBytes(int length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));
        ThrowIfDisposed();
        lock (_gate)
        {
            var stream = EnsureReadable();
            if (length == 0)
                return [];

            var bytes = new byte[length];
            var count = ReadAtMost(stream, bytes);
            if (count == length)
                return bytes;

            _eofReached = true;
            return bytes.AsSpan(0, count).ToArray();
        }
    }

    /// <summary>Reads UTF-8 bytes through the next LF, CR, CRLF, or null terminator.</summary>
    /// <returns>The decoded line without its terminator; an empty string after end of file.</returns>
    /// <exception cref="DecoderFallbackException">The bytes are not valid UTF-8.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not readable.</exception>
    /// <exception cref="IOException">Reading fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public string ReadLine()
    {
        ThrowIfDisposed();
        lock (_gate)
            return ReadLineLocked();
    }

    /// <summary>Reads one CSV record.</summary>
    /// <param name="delimiter">The one-character field separator.</param>
    /// <returns>The decoded fields; an empty array when called at end of file.</returns>
    /// <exception cref="ArgumentException"><paramref name="delimiter"/> is CR, LF, or a double quote.</exception>
    /// <exception cref="FormatException">The record has an unterminated or misplaced quoted field.</exception>
    /// <exception cref="DecoderFallbackException">The bytes are not valid UTF-8.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not readable.</exception>
    /// <exception cref="IOException">Reading fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public string[] ReadCsvLine(char delimiter = ',')
    {
        ValidateDelimiter(delimiter);
        ThrowIfDisposed();
        lock (_gate)
        {
            var record = ReadCsvRecordLocked();
            return record is null ? [] : ParseCsv(record, delimiter);
        }
    }

    /// <summary>Reads a length-prefixed UTF-8 string.</summary>
    /// <returns>The decoded string whose byte count is stored as an unsigned 32-bit prefix.</returns>
    /// <exception cref="EndOfStreamException">The prefix or payload is incomplete.</exception>
    /// <exception cref="DecoderFallbackException">The payload is not valid UTF-8.</exception>
    /// <exception cref="IOException">The declared length exceeds the supported managed array length.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not readable.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public string ReadPascalString()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            var stream = EnsureReadable();
            Span<byte> prefix = stackalloc byte[sizeof(uint)];
            if (ReadAtMost(stream, prefix) != prefix.Length)
            {
                _eofReached = true;
                throw new EndOfStreamException("The file ended before a complete string length was read.");
            }
            var length = _bigEndian
                ? BinaryPrimitives.ReadUInt32BigEndian(prefix)
                : BinaryPrimitives.ReadUInt32LittleEndian(prefix);
            if (length > int.MaxValue)
                throw new IOException("The length-prefixed string exceeds the supported managed array length.");
            var bytes = new byte[checked((int)length)];
            if (ReadAtMost(stream, bytes) != bytes.Length)
            {
                _eofReached = true;
                throw new EndOfStreamException("The file ended before the complete string payload was read.");
            }
            return StrictUtf8.GetString(bytes);
        }
    }

    /// <summary>Reads an exact number of bytes and decodes them as UTF-8.</summary>
    /// <param name="byteLength">The nonnegative byte count.</param>
    /// <returns>The decoded string.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="byteLength"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">The requested bytes are incomplete.</exception>
    /// <exception cref="DecoderFallbackException">The bytes are not valid UTF-8.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not readable.</exception>
    /// <exception cref="IOException">Reading fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public string ReadString(int byteLength) => StrictUtf8.GetString(ReadExact(byteLength));

    /// <summary>Reads the entire file as UTF-8 without changing the cursor.</summary>
    /// <param name="skipCarriageReturns">Whether CR bytes are omitted before decoding.</param>
    /// <returns>The decoded complete contents.</returns>
    /// <exception cref="DecoderFallbackException">The file is not valid UTF-8.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not readable.</exception>
    /// <exception cref="IOException">Reading fails or the file is too large for a managed snapshot.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public string ReadAllText(bool skipCarriageReturns = false)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            var stream = EnsureReadable();
            var original = stream.Position;
            try
            {
                stream.Position = 0;
                if (stream.Length > int.MaxValue)
                    throw new IOException("The file is too large for a managed text snapshot.");
                var bytes = new byte[checked((int)stream.Length)];
                ReadExactly(stream, bytes);
                if (!skipCarriageReturns)
                    return StrictUtf8.GetString(bytes);
                return StrictUtf8.GetString(bytes.Where(value => value != (byte)'\r').ToArray());
            }
            finally
            {
                stream.Position = original;
            }
        }
    }

    /// <summary>Writes one unsigned byte.</summary>
    /// <param name="value">The value to write.</param>
    /// <exception cref="InvalidOperationException">The file is closed or not writable.</exception>
    /// <exception cref="IOException">Writing fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void WriteByte(byte value) => WriteBytes([value]);

    /// <summary>Writes one unsigned 16-bit integer.</summary>
    /// <param name="value">The value written using <see cref="BigEndian"/> byte order.</param>
    /// <exception cref="InvalidOperationException">The file is closed or not writable.</exception>
    /// <exception cref="IOException">Writing fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void WriteUInt16(ushort value)
    {
        ThrowIfDisposed();
        Span<byte> bytes = stackalloc byte[2];
        lock (_gate)
        {
            var stream = EnsureWritable();
            if (_bigEndian)
                BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
            else
                BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
            stream.Write(bytes);
            _dirty = true;
            _eofReached = false;
        }
    }

    /// <summary>Writes one unsigned 32-bit integer.</summary>
    /// <param name="value">The value written using <see cref="BigEndian"/> byte order.</param>
    /// <exception cref="InvalidOperationException">The file is closed or not writable.</exception>
    /// <exception cref="IOException">Writing fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void WriteUInt32(uint value)
    {
        ThrowIfDisposed();
        Span<byte> bytes = stackalloc byte[4];
        lock (_gate)
        {
            var stream = EnsureWritable();
            if (_bigEndian)
                BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
            else
                BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
            stream.Write(bytes);
            _dirty = true;
            _eofReached = false;
        }
    }

    /// <summary>Writes one unsigned 64-bit integer.</summary>
    /// <param name="value">The value written using <see cref="BigEndian"/> byte order.</param>
    /// <exception cref="InvalidOperationException">The file is closed or not writable.</exception>
    /// <exception cref="IOException">Writing fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void WriteUInt64(ulong value)
    {
        ThrowIfDisposed();
        Span<byte> bytes = stackalloc byte[8];
        lock (_gate)
        {
            var stream = EnsureWritable();
            if (_bigEndian)
                BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
            else
                BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
            stream.Write(bytes);
            _dirty = true;
            _eofReached = false;
        }
    }

    /// <summary>Writes an IEEE 754 binary16 value.</summary>
    /// <param name="value">The half-precision value.</param>
    /// <exception cref="InvalidOperationException">The file is closed or not writable.</exception>
    /// <exception cref="IOException">Writing fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void WriteHalf(Half value) => WriteUInt16(BitConverter.HalfToUInt16Bits(value));

    /// <summary>Writes an IEEE 754 binary32 value.</summary>
    /// <param name="value">The single-precision value.</param>
    /// <exception cref="InvalidOperationException">The file is closed or not writable.</exception>
    /// <exception cref="IOException">Writing fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void WriteSingle(float value) => WriteUInt32(BitConverter.SingleToUInt32Bits(value));

    /// <summary>Writes an IEEE 754 binary64 value.</summary>
    /// <param name="value">The double-precision value.</param>
    /// <exception cref="InvalidOperationException">The file is closed or not writable.</exception>
    /// <exception cref="IOException">Writing fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void WriteDouble(double value) => WriteUInt64(BitConverter.DoubleToUInt64Bits(value));

    /// <summary>Writes the engine real-number storage format.</summary>
    /// <param name="value">The binary32 value used by the engine's 2D numeric types.</param>
    /// <exception cref="InvalidOperationException">The file is closed or not writable.</exception>
    /// <exception cref="IOException">Writing fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void WriteReal(float value) => WriteSingle(value);

    /// <summary>Writes bytes at the current cursor.</summary>
    /// <param name="bytes">The bytes to write.</param>
    /// <exception cref="InvalidOperationException">The file is closed or not writable.</exception>
    /// <exception cref="IOException">Writing fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            EnsureWritable().Write(bytes);
            _dirty = bytes.Length > 0 || _dirty;
            _eofReached = false;
        }
    }

    /// <summary>Writes UTF-8 text without a length prefix or terminator.</summary>
    /// <param name="value">The text to encode.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="EncoderFallbackException">The string contains invalid UTF-16.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not writable.</exception>
    /// <exception cref="IOException">Writing fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void WriteString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        WriteBytes(StrictUtf8.GetBytes(value));
    }

    /// <summary>Writes UTF-8 text followed by LF.</summary>
    /// <param name="value">The text to encode.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="EncoderFallbackException">The string contains invalid UTF-16.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not writable.</exception>
    /// <exception cref="IOException">Writing fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void WriteLine(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var bytes = StrictUtf8.GetBytes(value);
        ThrowIfDisposed();
        lock (_gate)
        {
            var stream = EnsureWritable();
            stream.Write(bytes);
            stream.WriteByte((byte)'\n');
            _dirty = true;
            _eofReached = false;
        }
    }

    /// <summary>Writes one CSV record followed by LF.</summary>
    /// <param name="values">The non-null field values.</param>
    /// <param name="delimiter">The one-character field separator.</param>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> or one of its fields is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="delimiter"/> is CR, LF, or a double quote.</exception>
    /// <exception cref="EncoderFallbackException">A field contains invalid UTF-16.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not writable.</exception>
    /// <exception cref="IOException">Writing fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void WriteCsvLine(IEnumerable<string> values, char delimiter = ',')
    {
        ArgumentNullException.ThrowIfNull(values);
        ValidateDelimiter(delimiter);
        var encoded = values.Select(value => EncodeCsvField(value ?? throw new ArgumentNullException(nameof(values)), delimiter));
        WriteLine(string.Join(delimiter, encoded));
    }

    /// <summary>Writes a UTF-8 string preceded by its unsigned 32-bit byte count.</summary>
    /// <param name="value">The text to encode.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="EncoderFallbackException">The string contains invalid UTF-16.</exception>
    /// <exception cref="InvalidOperationException">The file is closed or not writable.</exception>
    /// <exception cref="IOException">Writing fails.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void WritePascalString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var bytes = StrictUtf8.GetBytes(value);
        ThrowIfDisposed();
        lock (_gate)
        {
            var stream = EnsureWritable();
            Span<byte> prefix = stackalloc byte[sizeof(uint)];
            if (_bigEndian)
                BinaryPrimitives.WriteUInt32BigEndian(prefix, checked((uint)bytes.Length));
            else
                BinaryPrimitives.WriteUInt32LittleEndian(prefix, checked((uint)bytes.Length));
            stream.Write(prefix);
            stream.Write(bytes);
            _dirty = true;
            _eofReached = false;
        }
    }

    /// <summary>Determines whether a physical or directory-backed virtual file exists.</summary>
    /// <param name="path">The file path.</param>
    /// <returns><see langword="true"/> only when the resolved path identifies an existing file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="UnauthorizedAccessException">A virtual path escapes its configured root.</exception>
    /// <exception cref="NotSupportedException">The path uses an unsupported virtual scheme.</exception>
    public static bool FileExists(string path) => File.Exists(ResolvePath(path));

    /// <summary>Reads a complete physical or directory-backed virtual file.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>A new byte array containing the file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access or a virtual path escapes its root.</exception>
    /// <exception cref="NotSupportedException">The path uses an unsupported virtual scheme.</exception>
    public static byte[] GetFileAsBytes(string path) => File.ReadAllBytes(ResolvePath(path));

    /// <summary>Reads a complete physical or directory-backed virtual file as strict UTF-8.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>The decoded text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="DecoderFallbackException">The file is not valid UTF-8.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access or a virtual path escapes its root.</exception>
    /// <exception cref="NotSupportedException">The path uses an unsupported virtual scheme.</exception>
    public static string GetFileAsString(string path) => StrictUtf8.GetString(GetFileAsBytes(path));

    /// <summary>Gets the last-access time as Unix seconds.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>Seconds since 1970-01-01 UTC.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access or a virtual path escapes its root.</exception>
    /// <exception cref="NotSupportedException">The path uses an unsupported virtual scheme.</exception>
    public static long GetAccessTime(string path) => new DateTimeOffset(GetFileInfo(path).LastAccessTimeUtc).ToUnixTimeSeconds();

    /// <summary>Gets the last-modification time as Unix seconds.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>Seconds since 1970-01-01 UTC.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access or a virtual path escapes its root.</exception>
    /// <exception cref="NotSupportedException">The path uses an unsupported virtual scheme.</exception>
    public static long GetModifiedTime(string path) => new DateTimeOffset(GetFileInfo(path).LastWriteTimeUtc).ToUnixTimeSeconds();

    /// <summary>Gets a file's length without opening a persistent instance.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>The physical byte count.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access or a virtual path escapes its root.</exception>
    /// <exception cref="NotSupportedException">The path uses an unsupported virtual scheme.</exception>
    public static long GetSize(string path) => GetFileInfo(path).Length;

    /// <summary>Computes a file's MD5 digest.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>A lowercase hexadecimal digest.</returns>
    /// <remarks>MD5 is provided for compatibility and integrity checks, not security decisions.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access or a virtual path escapes its root.</exception>
    /// <exception cref="NotSupportedException">The path uses an unsupported virtual scheme.</exception>
    public static string GetMd5(string path) => ComputeHash(path, MD5.Create());

    /// <summary>Computes a file's SHA-256 digest.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>A lowercase hexadecimal digest.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access or a virtual path escapes its root.</exception>
    /// <exception cref="NotSupportedException">The path uses an unsupported virtual scheme.</exception>
    public static string GetSha256(string path) => ComputeHash(path, SHA256.Create());

    /// <summary>Gets whether the filesystem marks a file as hidden.</summary>
    /// <param name="path">The file path.</param>
    /// <returns><see langword="true"/> when the hidden attribute is set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The filesystem cannot read the attribute.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access or a virtual path escapes its root.</exception>
    /// <exception cref="NotSupportedException">The path uses an unsupported virtual scheme.</exception>
    /// <exception cref="PlatformNotSupportedException">The current platform does not expose this file attribute.</exception>
    public static bool IsHidden(string path)
    {
        EnsurePortableFileAttributesSupported();
        return (File.GetAttributes(ResolvePath(path)) & FileAttributes.Hidden) != 0;
    }

    /// <summary>Changes a file's hidden attribute.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="hidden">Whether to set the attribute.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The filesystem cannot change the attribute.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access or a virtual path escapes its root.</exception>
    /// <exception cref="NotSupportedException">The path uses an unsupported virtual scheme.</exception>
    /// <exception cref="PlatformNotSupportedException">The current platform does not expose this file attribute.</exception>
    public static void SetHidden(string path, bool hidden)
    {
        EnsurePortableFileAttributesSupported();
        ChangeAttribute(path, FileAttributes.Hidden, hidden);
    }

    /// <summary>Gets whether the filesystem marks a file as read-only.</summary>
    /// <param name="path">The file path.</param>
    /// <returns><see langword="true"/> when the read-only attribute is set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The filesystem cannot read the attribute.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access or a virtual path escapes its root.</exception>
    /// <exception cref="NotSupportedException">The path uses an unsupported virtual scheme.</exception>
    /// <exception cref="PlatformNotSupportedException">The current platform does not expose this file attribute.</exception>
    public static bool IsReadOnly(string path)
    {
        EnsurePortableFileAttributesSupported();
        return (File.GetAttributes(ResolvePath(path)) & FileAttributes.ReadOnly) != 0;
    }

    /// <summary>Changes a file's read-only attribute.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="readOnly">Whether to set the attribute.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The filesystem cannot change the attribute.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access or a virtual path escapes its root.</exception>
    /// <exception cref="NotSupportedException">The path uses an unsupported virtual scheme.</exception>
    /// <exception cref="PlatformNotSupportedException">The current platform does not expose this file attribute.</exception>
    public static void SetReadOnly(string path, bool readOnly)
    {
        EnsurePortableFileAttributesSupported();
        ChangeAttribute(path, FileAttributes.ReadOnly, readOnly);
    }

    /// <summary>Gets Unix mode bits for a file.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>The permission and special-mode bits.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The filesystem cannot read the mode.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access or a virtual path escapes its root.</exception>
    /// <exception cref="NotSupportedException">The path uses an unsupported virtual scheme.</exception>
    /// <exception cref="PlatformNotSupportedException">The current platform does not expose Unix file modes.</exception>
    public static UnixPermissionFlags GetUnixPermissions(string path)
    {
        if (OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Unix file modes are unavailable on Windows.");
        return (UnixPermissionFlags)(int)File.GetUnixFileMode(ResolvePath(path));
    }

    /// <summary>Sets Unix mode bits for a file.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="permissions">The permission and special-mode bits.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="permissions"/> contains unknown bits.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The filesystem cannot change the mode.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access or a virtual path escapes its root.</exception>
    /// <exception cref="NotSupportedException">The path uses an unsupported virtual scheme.</exception>
    /// <exception cref="PlatformNotSupportedException">The current platform does not expose Unix file modes.</exception>
    public static void SetUnixPermissions(string path, UnixPermissionFlags permissions)
    {
        ValidateUnixPermissions(permissions);
        if (OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Unix file modes are unavailable on Windows.");
        File.SetUnixFileMode(ResolvePath(path), (UnixFileMode)(int)permissions);
    }

    internal static void ValidateUnixPermissions(UnixPermissionFlags permissions)
    {
        if (OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Unix file modes are unavailable on Windows.");
        const UnixPermissionFlags all = (UnixPermissionFlags)4095;
        if ((permissions & ~all) != 0)
            throw new ArgumentOutOfRangeException(nameof(permissions));
    }

    /// <summary>Gets one extended attribute as bytes.</summary>
    /// <param name="path">The ordinary or supported virtual file path.</param>
    /// <param name="name">The nonempty attribute name without a Linux namespace prefix.</param>
    /// <returns>A new byte array containing the attribute value.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An argument is empty or the attribute name is invalid.</exception>
    /// <exception cref="IOException">The filesystem rejects the request.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access.</exception>
    /// <exception cref="PlatformNotSupportedException">The platform has no implemented extended-attribute backend.</exception>
    public static byte[] GetExtendedAttribute(string path, string name) =>
        ExtendedAttributes.Get(ResolvePath(path), name);

    /// <summary>Gets one extended attribute as strict UTF-8.</summary>
    /// <param name="path">The ordinary or supported virtual file path.</param>
    /// <param name="name">The nonempty attribute name without a Linux namespace prefix.</param>
    /// <returns>The decoded value.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An argument is empty or the attribute name is invalid.</exception>
    /// <exception cref="DecoderFallbackException">The value is not valid UTF-8.</exception>
    /// <exception cref="IOException">The filesystem rejects the request.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access.</exception>
    /// <exception cref="PlatformNotSupportedException">The platform has no implemented extended-attribute backend.</exception>
    public static string GetExtendedAttributeString(string path, string name) =>
        StrictUtf8.GetString(GetExtendedAttribute(path, name));

    /// <summary>Lists extended attribute names without platform namespace syntax.</summary>
    /// <param name="path">The ordinary or supported virtual file path.</param>
    /// <returns>An immutable snapshot in operating-system order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The filesystem rejects the request.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access.</exception>
    /// <exception cref="PlatformNotSupportedException">The platform has no implemented extended-attribute backend.</exception>
    public static IReadOnlyList<string> GetExtendedAttributesList(string path) =>
        Array.AsReadOnly(ExtendedAttributes.List(ResolvePath(path)));

    /// <summary>Sets one extended attribute from bytes.</summary>
    /// <param name="path">The ordinary or supported virtual file path.</param>
    /// <param name="name">The nonempty attribute name without a Linux namespace prefix.</param>
    /// <param name="value">The attribute bytes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An argument is empty or the attribute name is invalid.</exception>
    /// <exception cref="IOException">The filesystem rejects the request.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access.</exception>
    /// <exception cref="PlatformNotSupportedException">The platform has no implemented extended-attribute backend.</exception>
    public static void SetExtendedAttribute(string path, string name, ReadOnlySpan<byte> value) =>
        ExtendedAttributes.Set(ResolvePath(path), name, value);

    /// <summary>Sets one extended attribute from UTF-8 text.</summary>
    /// <param name="path">The ordinary or supported virtual file path.</param>
    /// <param name="name">The nonempty attribute name without a Linux namespace prefix.</param>
    /// <param name="value">The text value.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An argument is empty or the attribute name is invalid.</exception>
    /// <exception cref="EncoderFallbackException"><paramref name="value"/> contains invalid UTF-16.</exception>
    /// <exception cref="IOException">The filesystem rejects the request.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access.</exception>
    /// <exception cref="PlatformNotSupportedException">The platform has no implemented extended-attribute backend.</exception>
    public static void SetExtendedAttributeString(string path, string name, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        SetExtendedAttribute(path, name, StrictUtf8.GetBytes(value));
    }

    /// <summary>Removes one extended attribute.</summary>
    /// <param name="path">The ordinary or supported virtual file path.</param>
    /// <param name="name">The nonempty attribute name without a Linux namespace prefix.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An argument is empty or the attribute name is invalid.</exception>
    /// <exception cref="IOException">The filesystem rejects the request.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks filesystem access.</exception>
    /// <exception cref="PlatformNotSupportedException">The platform has no implemented extended-attribute backend.</exception>
    public static void RemoveExtendedAttribute(string path, string name) =>
        ExtendedAttributes.Remove(ResolvePath(path), name);

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_gate)
                CloseLocked();
        }

        base.Dispose(disposing);
    }

    private static void ChangeAttribute(string path, FileAttributes attribute, bool enabled)
    {
        var absolutePath = ResolvePath(path);
        var attributes = File.GetAttributes(absolutePath);
        File.SetAttributes(absolutePath, enabled ? attributes | attribute : attributes & ~attribute);
    }

    private void CloseLocked()
    {
        var stream = _stream;
        if (stream is null)
        {
            DeleteTemporaryLocked();
            return;
        }

        Exception? failure = null;
        try
        {
            if (CanWrite(_mode))
                FlushLocked();
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            _stream = null;
            _encoder = null;
            try
            {
                ZeroMemoryStream(stream);
                stream.Dispose();
            }
            catch (Exception error)
            {
                failure = failure is null ? error : new AggregateException(failure, error);
            }

            if (_encryptionKey is not null)
            {
                CryptographicOperations.ZeroMemory(_encryptionKey);
                _encryptionKey = null;
            }
        }

        if (_deleteOnClose)
        {
            try
            {
                DeleteTemporaryLocked();
            }
            catch (Exception error)
            {
                failure = failure is null ? error : new AggregateException(failure, error);
            }
        }

        if (failure is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private void DeleteTemporaryLocked()
    {
        if (!_deleteOnClose)
            return;
        File.Delete(_absolutePath);
        _deleteOnClose = false;
    }

    private static string ComputeHash(string path, HashAlgorithm algorithm)
    {
        using (algorithm)
        using (var stream = File.OpenRead(ResolvePath(path)))
            return Convert.ToHexString(algorithm.ComputeHash(stream)).ToLowerInvariant();
    }

    private static FileInfo GetFileInfo(string path)
    {
        var info = new FileInfo(ResolvePath(path));
        if (!info.Exists)
            throw new FileNotFoundException("The file does not exist.", info.FullName);
        return info;
    }

    private static void EnsurePortableFileAttributesSupported()
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsIOS() &&
            !OperatingSystem.IsMacCatalyst() && !OperatingSystem.IsFreeBSD())
        {
            throw new PlatformNotSupportedException(
                "Hidden and read-only file attributes are supported only on Apple, BSD, and Windows platforms.");
        }
    }

    private static bool CanRead(FileAccessMode mode) => mode is FileAccessMode.Read or FileAccessMode.ReadWrite or FileAccessMode.WriteRead;

    private static bool CanWrite(FileAccessMode mode) => mode is FileAccessMode.Write or FileAccessMode.ReadWrite or FileAccessMode.WriteRead;

    private static bool Truncates(FileAccessMode mode) => mode is FileAccessMode.Write or FileAccessMode.WriteRead;

    private static Stream CreateMemoryStream(ReadOnlySpan<byte> data)
    {
        var stream = new MemoryStream(Math.Max(data.Length, 256));
        stream.Write(data);
        stream.Position = 0;
        return stream;
    }

    private static byte[] DecodeCompressed(ReadOnlySpan<byte> envelope, FileCompressionMode mode)
    {
        var minimum = CompressionMagic.Length + 1 + 1 + sizeof(long);
        if (envelope.Length < minimum || !envelope.StartsWith(CompressionMagic))
            throw new InvalidDataException("The compressed file envelope is invalid or truncated.");
        var offset = CompressionMagic.Length;
        if (envelope[offset++] != ContainerVersion)
            throw new InvalidDataException("The compressed file version is unsupported.");
        if (envelope[offset++] != (byte)mode)
            throw new InvalidDataException("The compressed file uses a different codec.");
        var expectedLength = BinaryPrimitives.ReadInt64LittleEndian(envelope.Slice(offset, sizeof(long)));
        if (expectedLength < 0 || expectedLength > int.MaxValue)
            throw new InvalidDataException("The decoded length is outside the supported managed range.");
        offset += sizeof(long);

        using var input = new MemoryStream(envelope[offset..].ToArray(), writable: false);
        using var decoder = CreateCompressionStream(input, mode, CompressionMode.Decompress, leaveOpen: false);
        using var output = new MemoryStream(checked((int)expectedLength));
        decoder.CopyTo(output);
        if (output.Length != expectedLength)
            throw new InvalidDataException("The decoded length does not match the compressed envelope.");
        return output.ToArray();
    }

    private static byte[] DeriveKey(string password, ReadOnlySpan<byte> salt) => Rfc2898DeriveBytes.Pbkdf2(
        password,
        salt,
        PasswordIterations,
        HashAlgorithmName.SHA256,
        EncryptionKeySize);

    private static byte[] EncodeCompressed(ReadOnlySpan<byte> data, FileCompressionMode mode)
    {
        ValidateCompressionMode(mode);
        using var output = new MemoryStream();
        output.Write(CompressionMagic);
        output.WriteByte(ContainerVersion);
        output.WriteByte((byte)mode);
        Span<byte> length = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(length, data.Length);
        output.Write(length);
        using (var encoder = CreateCompressionStream(output, mode, CompressionMode.Compress, leaveOpen: true))
            encoder.Write(data);
        return output.ToArray();
    }

    private static Stream CreateCompressionStream(
        Stream stream,
        FileCompressionMode mode,
        CompressionMode direction,
        bool leaveOpen) => mode switch
        {
            FileCompressionMode.Deflate => new DeflateStream(stream, direction, leaveOpen),
            FileCompressionMode.Gzip => new GZipStream(stream, direction, leaveOpen),
            FileCompressionMode.Brotli => new BrotliStream(stream, direction, leaveOpen),
            FileCompressionMode.FastLz => throw new NotSupportedException("FastLZ requires a codec provider that is not integrated."),
            FileCompressionMode.Zstandard => throw new NotSupportedException("Zstandard requires a codec provider that is not integrated."),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

    private static byte[] Decrypt(
        ReadOnlySpan<byte> envelope,
        ReadOnlySpan<byte> key,
        byte expectedMode,
        out byte[] salt)
    {
        ParseEncryptionEnvelope(envelope, expectedMode, out var headerLength, out var saltOffset, out var saltLength);
        salt = envelope.Slice(saltOffset, saltLength).ToArray();
        var ciphertextLength = envelope.Length - headerLength - EncryptionTagSize;
        var plaintext = new byte[ciphertextLength];
        try
        {
            using var cipher = new AesGcm(key, EncryptionTagSize);
            cipher.Decrypt(
                envelope.Slice(headerLength - EncryptionNonceSize, EncryptionNonceSize),
                envelope.Slice(headerLength + EncryptionTagSize, ciphertextLength),
                envelope.Slice(headerLength, EncryptionTagSize),
                plaintext,
                envelope[..headerLength]);
            return plaintext;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(plaintext);
            throw;
        }
    }

    private static byte[] Encrypt(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> key,
        byte mode,
        ReadOnlySpan<byte> salt)
    {
        var headerLength = EncryptionMagic.Length + 3 + salt.Length + EncryptionNonceSize;
        var envelope = new byte[headerLength + EncryptionTagSize + plaintext.Length];
        var offset = 0;
        EncryptionMagic.CopyTo(envelope);
        offset += EncryptionMagic.Length;
        envelope[offset++] = ContainerVersion;
        envelope[offset++] = mode;
        envelope[offset++] = checked((byte)salt.Length);
        salt.CopyTo(envelope.AsSpan(offset));
        offset += salt.Length;
        RandomNumberGenerator.Fill(envelope.AsSpan(offset, EncryptionNonceSize));
        using var cipher = new AesGcm(key, EncryptionTagSize);
        cipher.Encrypt(
            envelope.AsSpan(offset, EncryptionNonceSize),
            plaintext,
            envelope.AsSpan(headerLength + EncryptionTagSize, plaintext.Length),
            envelope.AsSpan(headerLength, EncryptionTagSize),
            envelope.AsSpan(0, headerLength));
        return envelope;
    }

    private static string EncodeCsvField(string value, char delimiter)
    {
        if (value.IndexOfAny([delimiter, '"', '\r', '\n']) < 0)
            return value;
        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private Stream EnsureOpen()
    {
        ThrowIfDisposed();
        return _stream ?? throw new InvalidOperationException("The file is closed.");
    }

    private Stream EnsureReadable()
    {
        if (!CanRead(_mode))
            throw new InvalidOperationException("The file was not opened for reading.");
        return EnsureOpen();
    }

    private Stream EnsureWritable()
    {
        if (!CanWrite(_mode))
            throw new InvalidOperationException("The file was not opened for writing.");
        return EnsureOpen();
    }

    private static void EnsureEncryptionSupported()
    {
        if (!AesGcm.IsSupported)
            throw new PlatformNotSupportedException("Authenticated file encryption requires AES-GCM support.");
    }

    private void FlushLocked()
    {
        var stream = EnsureWritable();
        if (_encoder is null)
        {
            if (stream is FileStream fileStream)
                fileStream.Flush(flushToDisk: true);
            else
                stream.Flush();
            _dirty = false;
            return;
        }

        if (!_dirty)
            return;
        if (stream.Length > int.MaxValue)
            throw new IOException("The transformed file exceeds the supported managed buffer length.");
        var original = stream.Position;
        stream.Position = 0;
        var plaintext = new byte[checked((int)stream.Length)];
        ReadExactly(stream, plaintext);
        stream.Position = original;
        byte[]? envelope = null;
        try
        {
            envelope = _encoder(plaintext);
            AtomicFile.Write(_absolutePath, envelope);
            _dirty = false;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            if (_encryptionKey is not null && envelope is not null)
                CryptographicOperations.ZeroMemory(envelope);
        }
    }

    private static FileStream OpenPhysical(string path, FileAccessMode mode) => mode switch
    {
        FileAccessMode.Read => new FileStream(path, FileMode.Open, IoFileAccess.Read, FileShare.Read),
        FileAccessMode.Write => new FileStream(path, FileMode.Create, IoFileAccess.Write, FileShare.None),
        FileAccessMode.ReadWrite => new FileStream(path, FileMode.Open, IoFileAccess.ReadWrite, FileShare.None),
        FileAccessMode.WriteRead => new FileStream(path, FileMode.Create, IoFileAccess.ReadWrite, FileShare.None),
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    private static void ParseEncryptionEnvelope(
        ReadOnlySpan<byte> envelope,
        byte expectedMode,
        out int headerLength,
        out int saltOffset,
        out int saltLength)
    {
        var minimum = EncryptionMagic.Length + 3 + EncryptionNonceSize + EncryptionTagSize;
        if (envelope.Length < minimum || !envelope.StartsWith(EncryptionMagic))
            throw new InvalidDataException("The encrypted file envelope is invalid or truncated.");
        var offset = EncryptionMagic.Length;
        if (envelope[offset++] != ContainerVersion)
            throw new InvalidDataException("The encrypted file version is unsupported.");
        if (envelope[offset++] != expectedMode)
            throw new InvalidDataException("The encrypted file uses another key mode.");
        saltLength = envelope[offset++];
        if (expectedMode == RawKeyEncryption && saltLength != 0 ||
            expectedMode == PasswordEncryption && saltLength != EncryptionSaltSize)
        {
            throw new InvalidDataException("The encrypted file salt is invalid.");
        }
        saltOffset = offset;
        headerLength = offset + saltLength + EncryptionNonceSize;
        if (envelope.Length < headerLength + EncryptionTagSize)
            throw new InvalidDataException("The encrypted file envelope is truncated.");
    }

    private static string[] ParseCsv(string record, char delimiter)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        var closedQuote = false;
        for (var index = 0; index < record.Length; index++)
        {
            var character = record[index];
            if (quoted)
            {
                if (character == '"')
                {
                    if (index + 1 < record.Length && record[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        quoted = false;
                        closedQuote = true;
                    }
                }
                else
                {
                    field.Append(character);
                }
            }
            else if (character == delimiter)
            {
                fields.Add(field.ToString());
                field.Clear();
                closedQuote = false;
            }
            else if (character == '"' && field.Length == 0 && !closedQuote)
            {
                quoted = true;
            }
            else if (closedQuote || character == '"')
            {
                throw new FormatException("A CSV record contains a misplaced quote.");
            }
            else
            {
                field.Append(character);
            }
        }
        if (quoted)
            throw new FormatException("A CSV record contains an unterminated quoted field.");
        fields.Add(field.ToString());
        return fields.ToArray();
    }

    private string? ReadCsvRecordLocked()
    {
        var stream = EnsureReadable();
        var output = new MemoryStream();
        var quoted = false;
        var readAny = false;
        while (true)
        {
            var value = stream.ReadByte();
            if (value < 0)
            {
                _eofReached = true;
                if (!readAny)
                    return null;
                break;
            }

            readAny = true;
            if (value == '"')
            {
                output.WriteByte((byte)value);
                if (quoted)
                {
                    var next = stream.ReadByte();
                    if (next == '"')
                    {
                        output.WriteByte((byte)next);
                        continue;
                    }
                    quoted = false;
                    if (next < 0)
                    {
                        _eofReached = true;
                        break;
                    }
                    value = next;
                }
                else
                {
                    quoted = true;
                    continue;
                }
            }

            if (!quoted && (value == '\n' || value == 0))
                break;
            if (!quoted && value == '\r')
            {
                var next = stream.ReadByte();
                if (next >= 0 && next != '\n')
                    stream.Position--;
                break;
            }
            output.WriteByte((byte)value);
        }

        return StrictUtf8.GetString(output.ToArray());
    }

    private byte[] ReadExact(int length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));
        ThrowIfDisposed();
        lock (_gate)
        {
            var stream = EnsureReadable();
            var bytes = new byte[length];
            if (ReadAtMost(stream, bytes) != length)
            {
                _eofReached = true;
                throw new EndOfStreamException("The file ended before a complete value was read.");
            }
            return bytes;
        }
    }

    private string ReadLineLocked()
    {
        var stream = EnsureReadable();
        using var output = new MemoryStream();
        while (true)
        {
            var value = stream.ReadByte();
            if (value < 0)
            {
                _eofReached = true;
                break;
            }
            if (value is '\n' or 0)
                break;
            if (value == '\r')
            {
                var next = stream.ReadByte();
                if (next >= 0 && next != '\n')
                    stream.Position--;
                break;
            }
            output.WriteByte((byte)value);
        }
        return StrictUtf8.GetString(output.ToArray());
    }

    private T ReadScalar<T>(int size, Func<byte[], T> decode)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            var stream = EnsureReadable();
            var bytes = new byte[size];
            if (ReadAtMost(stream, bytes) != size)
            {
                _eofReached = true;
                throw new EndOfStreamException("The file ended before a complete value was read.");
            }
            return decode(bytes);
        }
    }

    private static int ReadAtMost(Stream stream, Span<byte> buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var count = stream.Read(buffer[total..]);
            if (count == 0)
                break;
            total += count;
        }
        return total;
    }

    private static void ReadExactly(Stream stream, Span<byte> buffer)
    {
        if (ReadAtMost(stream, buffer) != buffer.Length)
            throw new EndOfStreamException("The stream ended unexpectedly.");
    }

    private static string ResolvePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length == 0)
            throw new ArgumentException("A file path cannot be empty.", nameof(path));
        return ProjectSettings.Instance.GlobalizePath(path);
    }

    private static void ValidateCompressionMode(FileCompressionMode mode)
    {
        if (!Enum.IsDefined(mode))
            throw new ArgumentOutOfRangeException(nameof(mode));
        if (mode is FileCompressionMode.FastLz or FileCompressionMode.Zstandard)
            throw new NotSupportedException($"The {mode} codec provider is not integrated.");
    }

    private static void ValidateDelimiter(char delimiter)
    {
        if (delimiter is '\r' or '\n' or '"')
            throw new ArgumentException("A CSV delimiter cannot be CR, LF, or a double quote.", nameof(delimiter));
    }

    private static void ValidateEncryptionKey(ReadOnlySpan<byte> key)
    {
        if (key.Length != EncryptionKeySize)
            throw new ArgumentException("An encryption key must contain exactly 32 bytes.", nameof(key));
    }

    private static void ValidateMode(FileAccessMode mode)
    {
        if (mode is not FileAccessMode.Read and not FileAccessMode.Write and not FileAccessMode.ReadWrite and not FileAccessMode.WriteRead)
            throw new ArgumentOutOfRangeException(nameof(mode));
    }

    private static void ValidateTemporaryFilePart(string value, string parameterName)
    {
        if (value is "." or ".." || value.Any(character =>
                character < ' ' || "<>:\"/\\|?*".IndexOf(character) >= 0))
        {
            throw new ArgumentException("A temporary filename part contains an invalid character.", parameterName);
        }
    }

    private static void ZeroMemoryStream(Stream stream)
    {
        if (stream is not MemoryStream memory || !memory.TryGetBuffer(out var segment) || segment.Array is null)
            return;
        CryptographicOperations.ZeroMemory(segment.Array);
    }
}

internal static class AtomicFile
{
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Best-effort temporary-file cleanup must never replace the original persistence failure.")]
    internal static void Write(string path, ReadOnlySpan<byte> data)
    {
        var fullPath = System.IO.Path.GetFullPath(path);
        var directory = System.IO.Path.GetDirectoryName(fullPath);
        var fileName = System.IO.Path.GetFileName(fullPath);
        if (string.IsNullOrEmpty(directory) || fileName.Length == 0)
            throw new ArgumentException("The path must identify a file.", nameof(path));

        var temporaryPath = System.IO.Path.Combine(directory, $".{fileName}.{RandomNumberGenerator.GetHexString(12)}.tmp");
        try
        {
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       IoFileAccess.Write,
                       FileShare.None,
                       4096,
                       FileOptions.WriteThrough))
            {
                stream.Write(data);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        catch
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch
            {
            }
            throw;
        }
    }
}
