# FileAccess

Last updated: 2026-10-04

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Source:** [`src/Core/IO/FileAccess.cs`](../../src/Core/IO/FileAccess.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class FileAccess : ElectronObject`

> Provides seekable binary and text access to ordinary and directory-backed virtual files.

## Description

Provides seekable binary and text access to ordinary and directory-backed virtual files.

`FileAccess` owns one seekable physical stream or decoded in-memory view. It supplies blocking binary/text I/O, virtual-path resolution, file metadata, hashing, Linux/macOS/Windows extended attributes, temporary files, whole-file compression, and authenticated whole-file encryption. The instance exclusively owns and closes its stream; it never owns `ProjectSettings`.

Instances own their stream until [`FileAccess.Close`](FileAccess.md#m-electron2d-fileaccess-close) or disposal. Individual operations are serialized, but a
multi-call seek/read/write sequence is not atomic. File operations may block and are unsuitable for real-time frame
callbacks. Compressed and encrypted files are authenticated or decoded completely before their contents are exposed.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using FileAccess file = FileAccess.Open("user://save.dat", FileAccessModeFlags.Write);
file.WriteString("ready");
```

## Properties

| Member | Description |
| --- | --- |
| [`public bool BigEndian { get; set; }`](#p-electron2d-fileaccess-bigendian) | Gets or sets whether multi-byte numeric values use big-endian byte order. |
| [`public string Path { get; }`](#p-electron2d-fileaccess-path) | Gets the path supplied when this file was opened. |
| [`public string AbsolutePath { get; }`](#p-electron2d-fileaccess-absolutepath) | Gets the normalized absolute operating-system path. |
| [`public bool IsOpen { get; }`](#p-electron2d-fileaccess-isopen) | Gets whether the owned stream is still open. |
| [`public bool EOFReached { get; }`](#p-electron2d-fileaccess-eofreached) | Gets whether a read has attempted to move beyond the end of the file. |
| [`public long Position { get; }`](#p-electron2d-fileaccess-position) | Gets the current byte offset. |
| [`public long Length { get; }`](#p-electron2d-fileaccess-length) | Gets the decoded file length in bytes. |
| [`public bool Hidden { get; set; }`](#p-electron2d-fileaccess-hidden) | Gets or sets whether the physical file has the hidden attribute. |
| [`public bool ReadOnly { get; set; }`](#p-electron2d-fileaccess-readonly) | Gets or sets whether the physical file has the read-only attribute. |
| [`public UnixPermissionFlags UnixPermissions { get; set; }`](#p-electron2d-fileaccess-unixpermissions) | Gets or sets Unix permission and special-mode bits for the physical file. |

## Methods

| Member | Description |
| --- | --- |
| [`public static FileAccess Open(string path, FileAccessModeFlags mode)`](#m-electron2d-fileaccess-open-system-string-electron2d-fileaccessmode) | Opens a file with the requested access mode. |
| [`public static FileAccess CreateTemp(FileAccessModeFlags mode = FileAccessModeFlags.ReadWrite, string prefix = "", string extension = "", bool keep = false)`](#m-electron2d-fileaccess-createtemp-electron2d-fileaccessmode-system-string-system-string-system-boolean) | Creates a uniquely named temporary file and opens it. |
| [`public static FileAccess OpenCompressed(string path, FileAccessModeFlags mode, FileCompressionMode compressionMode = FileCompressionMode.FastLz)`](#m-electron2d-fileaccess-opencompressed-system-string-electron2d-fileaccessmode-electron2d-filecompressionmode) | Opens a whole-file compressed container. |
| [`public static FileAccess OpenEncrypted(string path, FileAccessModeFlags mode, ReadOnlySpan<byte> key)`](#m-electron2d-fileaccess-openencrypted-system-string-electron2d-fileaccessmode-system-readonlyspan-system-byte) | Opens a whole-file container protected by a 256-bit key and authenticated encryption. |
| [`public static FileAccess OpenEncryptedWithPassword(string path, FileAccessModeFlags mode, string password)`](#m-electron2d-fileaccess-openencryptedwithpassword-system-string-electron2d-fileaccessmode-system-string) | Opens a whole-file container protected by a password and authenticated encryption. |
| [`public void Close()`](#m-electron2d-fileaccess-close) | Closes the file, committing buffered transformed data when necessary. |
| [`public void Flush()`](#m-electron2d-fileaccess-flush) | Flushes pending data to the physical file. |
| [`public void Seek(long position)`](#m-electron2d-fileaccess-seek-system-int64) | Moves the cursor to an absolute byte offset. |
| [`public void SeekEnd(long offset = 0)`](#m-electron2d-fileaccess-seekend-system-int64) | Moves the cursor relative to the end of the file. |
| [`public void Resize(long length)`](#m-electron2d-fileaccess-resize-system-int64) | Changes the file length. |
| [`public byte ReadByte()`](#m-electron2d-fileaccess-readbyte) | Reads one unsigned byte. |
| [`public ushort ReadUInt16()`](#m-electron2d-fileaccess-readuint16) | Reads one unsigned 16-bit integer. |
| [`public uint ReadUInt32()`](#m-electron2d-fileaccess-readuint32) | Reads one unsigned 32-bit integer. |
| [`public ulong ReadUInt64()`](#m-electron2d-fileaccess-readuint64) | Reads one unsigned 64-bit integer. |
| [`public Half ReadHalf()`](#m-electron2d-fileaccess-readhalf) | Reads an IEEE 754 binary16 value. |
| [`public float ReadSingle()`](#m-electron2d-fileaccess-readsingle) | Reads an IEEE 754 binary32 value. |
| [`public double ReadDouble()`](#m-electron2d-fileaccess-readdouble) | Reads an IEEE 754 binary64 value. |
| [`public float ReadReal()`](#m-electron2d-fileaccess-readreal) | Reads the engine real-number storage format. |
| [`public byte[] ReadBytes(int length)`](#m-electron2d-fileaccess-readbytes-system-int32) | Reads up to a requested number of bytes. |
| [`public string ReadLine()`](#m-electron2d-fileaccess-readline) | Reads UTF-8 bytes through the next LF, CR, CRLF, or null terminator. |
| [`public string[] ReadCSVLine(char delimiter = ',')`](#m-electron2d-fileaccess-readcsvline-system-char) | Reads one CSV record. |
| [`public string ReadPascalString()`](#m-electron2d-fileaccess-readpascalstring) | Reads a length-prefixed UTF-8 string. |
| [`public string ReadAllText(bool skipCarriageReturns = false)`](#m-electron2d-fileaccess-readalltext-system-boolean) | Reads the entire file as UTF-8 without changing the cursor. |
| [`public void WriteByte(byte value)`](#m-electron2d-fileaccess-writebyte-system-byte) | Writes one unsigned byte. |
| [`public void WriteUInt16(ushort value)`](#m-electron2d-fileaccess-writeuint16-system-uint16) | Writes one unsigned 16-bit integer. |
| [`public void WriteUInt32(uint value)`](#m-electron2d-fileaccess-writeuint32-system-uint32) | Writes one unsigned 32-bit integer. |
| [`public void WriteUInt64(ulong value)`](#m-electron2d-fileaccess-writeuint64-system-uint64) | Writes one unsigned 64-bit integer. |
| [`public void WriteHalf(Half value)`](#m-electron2d-fileaccess-writehalf-system-half) | Writes an IEEE 754 binary16 value. |
| [`public void WriteSingle(float value)`](#m-electron2d-fileaccess-writesingle-system-single) | Writes an IEEE 754 binary32 value. |
| [`public void WriteDouble(double value)`](#m-electron2d-fileaccess-writedouble-system-double) | Writes an IEEE 754 binary64 value. |
| [`public void WriteReal(float value)`](#m-electron2d-fileaccess-writereal-system-single) | Writes the engine real-number storage format. |
| [`public void WriteBytes(ReadOnlySpan<byte> bytes)`](#m-electron2d-fileaccess-writebytes-system-readonlyspan-system-byte) | Writes bytes at the current cursor. |
| [`public void WriteString(string value)`](#m-electron2d-fileaccess-writestring-system-string) | Writes UTF-8 text without a length prefix or terminator. |
| [`public void WriteLine(string value)`](#m-electron2d-fileaccess-writeline-system-string) | Writes UTF-8 text followed by LF. |
| [`public void WriteCSVLine(IEnumerable<string> values, char delimiter = ',')`](#m-electron2d-fileaccess-writecsvline-system-collections-generic-ienumerable-system-string-system-char) | Writes one CSV record followed by LF. |
| [`public void WritePascalString(string value)`](#m-electron2d-fileaccess-writepascalstring-system-string) | Writes a UTF-8 string preceded by its unsigned 32-bit byte count. |
| [`public static bool FileExists(string path)`](#m-electron2d-fileaccess-fileexists-system-string) | Determines whether a physical or directory-backed virtual file exists. |
| [`public static byte[] GetFileAsBytes(string path)`](#m-electron2d-fileaccess-getfileasbytes-system-string) | Reads a complete physical or directory-backed virtual file. |
| [`public static string GetFileAsString(string path)`](#m-electron2d-fileaccess-getfileasstring-system-string) | Reads a complete physical or directory-backed virtual file as strict UTF-8. |
| [`public static long GetAccessTime(string path)`](#m-electron2d-fileaccess-getaccesstime-system-string) | Gets the last-access time as Unix seconds. |
| [`public static long GetModifiedTime(string path)`](#m-electron2d-fileaccess-getmodifiedtime-system-string) | Gets the last-modification time as Unix seconds. |
| [`public static long GetSize(string path)`](#m-electron2d-fileaccess-getsize-system-string) | Gets a file's length without opening a persistent instance. |
| [`public static string GetMD5(string path)`](#m-electron2d-fileaccess-getmd5-system-string) | Computes a file's MD5 digest. |
| [`public static string GetSHA256(string path)`](#m-electron2d-fileaccess-getsha256-system-string) | Computes a file's SHA-256 digest. |
| [`public static bool IsHidden(string path)`](#m-electron2d-fileaccess-ishidden-system-string) | Gets whether the filesystem marks a file as hidden. |
| [`public static void SetHidden(string path, bool hidden)`](#m-electron2d-fileaccess-sethidden-system-string-system-boolean) | Changes a file's hidden attribute. |
| [`public static bool IsReadOnly(string path)`](#m-electron2d-fileaccess-isreadonly-system-string) | Gets whether the filesystem marks a file as read-only. |
| [`public static void SetReadOnly(string path, bool readOnly)`](#m-electron2d-fileaccess-setreadonly-system-string-system-boolean) | Changes a file's read-only attribute. |
| [`public static UnixPermissionFlags GetUnixPermissions(string path)`](#m-electron2d-fileaccess-getunixpermissions-system-string) | Gets Unix mode bits for a file. |
| [`public static void SetUnixPermissions(string path, UnixPermissionFlags permissions)`](#m-electron2d-fileaccess-setunixpermissions-system-string-electron2d-unixpermissionflags) | Sets Unix mode bits for a file. |
| [`public static byte[] GetExtendedAttribute(string path, string name)`](#m-electron2d-fileaccess-getextendedattribute-system-string-system-string) | Gets one extended attribute as bytes. |
| [`public static string GetExtendedAttributeString(string path, string name)`](#m-electron2d-fileaccess-getextendedattributestring-system-string-system-string) | Gets one extended attribute as strict UTF-8. |
| [`public static IReadOnlyList<string> GetExtendedAttributesList(string path)`](#m-electron2d-fileaccess-getextendedattributeslist-system-string) | Lists extended attribute names without platform namespace syntax. |
| [`public static void SetExtendedAttribute(string path, string name, ReadOnlySpan<byte> value)`](#m-electron2d-fileaccess-setextendedattribute-system-string-system-string-system-readonlyspan-system-byte) | Sets one extended attribute from bytes. |
| [`public static void SetExtendedAttributeString(string path, string name, string value)`](#m-electron2d-fileaccess-setextendedattributestring-system-string-system-string-system-string) | Sets one extended attribute from UTF-8 text. |
| [`public static void RemoveExtendedAttribute(string path, string name)`](#m-electron2d-fileaccess-removeextendedattribute-system-string-system-string) | Removes one extended attribute. |
| [`protected override void Dispose(bool disposing)`](#m-electron2d-fileaccess-dispose-system-boolean) | Releases resources owned by a derived class. |

## Property Descriptions

<a id="p-electron2d-fileaccess-bigendian"></a>
### `public bool BigEndian { get; set; }`

Gets or sets whether multi-byte numeric values use big-endian byte order.

**Value:** `false` for little-endian order by default.

**Exceptions**

- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="p-electron2d-fileaccess-path"></a>
### `public string Path { get; }`

Gets the path supplied when this file was opened.

**Value:** The original virtual or ordinary path.

<a id="p-electron2d-fileaccess-absolutepath"></a>
### `public string AbsolutePath { get; }`

Gets the normalized absolute operating-system path.

**Value:** The resolved path used for physical file operations.

<a id="p-electron2d-fileaccess-isopen"></a>
### `public bool IsOpen { get; }`

Gets whether the owned stream is still open.

**Value:** `true` until [`FileAccess.Close`](FileAccess.md#m-electron2d-fileaccess-close) succeeds or releases the stream after a failure.

**Exceptions**

- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="p-electron2d-fileaccess-eofreached"></a>
### `public bool EOFReached { get; }`

Gets whether a read has attempted to move beyond the end of the file.

**Value:** `true` only after an incomplete read; a successful seek clears it.

**Exceptions**

- `InvalidOperationException`: The file is closed.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="p-electron2d-fileaccess-position"></a>
### `public long Position { get; }`

Gets the current byte offset.

**Value:** A zero-based position from the beginning of the decoded file.

**Exceptions**

- `InvalidOperationException`: The file is closed.
- `IO.IOException`: The stream cannot report its cursor.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="p-electron2d-fileaccess-length"></a>
### `public long Length { get; }`

Gets the decoded file length in bytes.

**Value:** The number of bytes visible through this instance.

**Exceptions**

- `InvalidOperationException`: The file is closed.
- `IO.IOException`: The stream cannot report its length.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="p-electron2d-fileaccess-hidden"></a>
### `public bool Hidden { get; set; }`

Gets or sets whether the physical file has the hidden attribute.

**Value:** The current filesystem hidden-attribute state.

**Exceptions**

- `InvalidOperationException`: The file is closed.
- `IO.IOException`: The filesystem cannot read or change the attribute.
- `UnauthorizedAccessException`: The caller lacks filesystem access.
- `PlatformNotSupportedException`: The current platform does not expose this file attribute.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="p-electron2d-fileaccess-readonly"></a>
### `public bool ReadOnly { get; set; }`

Gets or sets whether the physical file has the read-only attribute.

**Value:** The current filesystem read-only attribute state.

**Exceptions**

- `InvalidOperationException`: The file is closed.
- `IO.IOException`: The filesystem cannot read or change the attribute.
- `UnauthorizedAccessException`: The caller lacks filesystem access.
- `PlatformNotSupportedException`: The current platform does not expose this file attribute.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="p-electron2d-fileaccess-unixpermissions"></a>
### `public UnixPermissionFlags UnixPermissions { get; set; }`

Gets or sets Unix permission and special-mode bits for the physical file.

**Value:** The current Unix mode bits.

**Exceptions**

- `InvalidOperationException`: The file is closed.
- `ArgumentOutOfRangeException`: The assigned value contains unknown bits.
- `IO.IOException`: The filesystem cannot read or change the mode.
- `UnauthorizedAccessException`: The caller lacks filesystem access.
- `PlatformNotSupportedException`: The current platform does not expose Unix file modes.
- `ObjectDisposedException`: The instance is disposing or disposed.

## Method Descriptions

<a id="m-electron2d-fileaccess-open-system-string-electron2d-fileaccessmode"></a>
### `public static FileAccess Open(string path, FileAccessModeFlags mode)`

Opens a file with the requested access mode.

**Parameters**

- `path`: A nonempty ordinary, `res://`, or `user://` path.
- `mode`: The required read, write, truncation, and creation behavior.

**Returns:** A new owner of the opened file stream.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: The path is empty or `mode` is invalid.
- `IO.IOException`: The file cannot be opened.
- `UnauthorizedAccessException`: The caller lacks filesystem access.
- `NotSupportedException`: The path uses an unsupported virtual scheme.

<a id="m-electron2d-fileaccess-createtemp-electron2d-fileaccessmode-system-string-system-string-system-boolean"></a>
### `public static FileAccess CreateTemp(FileAccessModeFlags mode = FileAccessModeFlags.ReadWrite, string prefix = "", string extension = "", bool keep = false)`

Creates a uniquely named temporary file and opens it.

**Parameters**

- `mode`: The permitted operations.
- `prefix`: An optional filename prefix without directory separators. A hyphen separates it from the random name.
- `extension`: An optional extension, with or without a leading period.
- `keep`: Whether closing the returned instance preserves the file.

**Returns:** An open temporary file owner.

**Exceptions**

- `ArgumentNullException`: `prefix` or `extension` is `null`.
- `ArgumentException`: A filename part is invalid or the mode is invalid.
- `IO.IOException`: A temporary file cannot be created.
- `UnauthorizedAccessException`: The caller lacks access to the temporary directory.

<a id="m-electron2d-fileaccess-opencompressed-system-string-electron2d-fileaccessmode-electron2d-filecompressionmode"></a>
### `public static FileAccess OpenCompressed(string path, FileAccessModeFlags mode, FileCompressionMode compressionMode = FileCompressionMode.FastLz)`

Opens a whole-file compressed container.

**Parameters**

- `path`: A nonempty ordinary or supported virtual path.
- `mode`: The required access mode.
- `compressionMode`: The codec used to decode or encode the container.

**Returns:** A seekable view of the decoded bytes.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: The path is empty or the mode is invalid.
- `ArgumentOutOfRangeException`: `compressionMode` is unknown.
- `NotSupportedException`: The selected codec has no runtime provider.
- `IO.InvalidDataException`: An existing container is malformed or uses another codec.
- `IO.IOException`: The physical file cannot be read or replaced.
- `UnauthorizedAccessException`: The caller lacks filesystem access.

**Remarks:** Changes are atomically encoded to the destination by [`FileAccess.Flush`](FileAccess.md#m-electron2d-fileaccess-flush) or [`FileAccess.Close`](FileAccess.md#m-electron2d-fileaccess-close).

<a id="m-electron2d-fileaccess-openencrypted-system-string-electron2d-fileaccessmode-system-readonlyspan-system-byte"></a>
### `public static FileAccess OpenEncrypted(string path, FileAccessModeFlags mode, ReadOnlySpan<byte> key)`

Opens a whole-file container protected by a 256-bit key and authenticated encryption.

**Parameters**

- `path`: A nonempty ordinary or supported virtual path.
- `mode`: The required access mode.
- `key`: Exactly 32 key bytes.

**Returns:** A seekable view of authenticated plaintext bytes.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `key` is not exactly 32 bytes or the mode is invalid.
- `Security.Cryptography.CryptographicException`: Authentication fails.
- `IO.InvalidDataException`: The encryption envelope is malformed or uses password mode.
- `IO.IOException`: The physical file cannot be read or replaced.
- `UnauthorizedAccessException`: The caller lacks filesystem access.
- `PlatformNotSupportedException`: AES-GCM is unavailable.

**Remarks:** Writing uses a fresh random nonce for every atomic commit.

<a id="m-electron2d-fileaccess-openencryptedwithpassword-system-string-electron2d-fileaccessmode-system-string"></a>
### `public static FileAccess OpenEncryptedWithPassword(string path, FileAccessModeFlags mode, string password)`

Opens a whole-file container protected by a password and authenticated encryption.

**Parameters**

- `path`: A nonempty ordinary or supported virtual path.
- `mode`: The required access mode.
- `password`: A nonempty password.

**Returns:** A seekable view of authenticated plaintext bytes.

**Exceptions**

- `ArgumentNullException`: `path` or `password` is `null`.
- `ArgumentException`: `password` is empty or the mode is invalid.
- `Security.Cryptography.CryptographicException`: Authentication fails.
- `IO.InvalidDataException`: The encryption envelope is malformed or uses raw-key mode.
- `IO.IOException`: The physical file cannot be read or replaced.
- `UnauthorizedAccessException`: The caller lacks filesystem access.
- `PlatformNotSupportedException`: AES-GCM is unavailable.

**Remarks:** A random salt and PBKDF2-HMAC-SHA-256 derive a 256-bit key; commits use fresh nonces.

<a id="m-electron2d-fileaccess-close"></a>
### `public void Close()`

Closes the file, committing buffered transformed data when necessary.

**Exceptions**

- `IO.IOException`: A pending write cannot be committed or a temporary file cannot be deleted.
- `UnauthorizedAccessException`: The caller cannot replace or delete the physical file.
- `AggregateException`: Both a primary close operation and cleanup fail.
- `ObjectDisposedException`: The instance is already disposed.

**Remarks:** The method is idempotent. Owned buffers and key material are released even when commit or deletion fails.

<a id="m-electron2d-fileaccess-flush"></a>
### `public void Flush()`

Flushes pending data to the physical file.

**Exceptions**

- `InvalidOperationException`: The file is closed or was opened read-only.
- `IO.IOException`: The data cannot be persisted.
- `ObjectDisposedException`: The instance is disposing or disposed.

**Remarks:** Transformed data is encoded and atomically replaces the destination; ordinary streams are flushed to disk.

<a id="m-electron2d-fileaccess-seek-system-int64"></a>
### `public void Seek(long position)`

Moves the cursor to an absolute byte offset.

**Parameters**

- `position`: The nonnegative offset from the beginning.

**Exceptions**

- `ArgumentOutOfRangeException`: `position` is negative.
- `InvalidOperationException`: The file is closed.
- `IO.IOException`: Seeking fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-seekend-system-int64"></a>
### `public void SeekEnd(long offset = 0)`

Moves the cursor relative to the end of the file.

**Parameters**

- `offset`: A signed byte offset; zero selects the end and negative values select preceding bytes.

**Exceptions**

- `InvalidOperationException`: The file is closed.
- `IO.IOException`: Seeking fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-resize-system-int64"></a>
### `public void Resize(long length)`

Changes the file length.

**Parameters**

- `length`: The nonnegative decoded length in bytes.

**Exceptions**

- `ArgumentOutOfRangeException`: `length` is negative.
- `InvalidOperationException`: The file is closed or not writable.
- `IO.IOException`: Resizing fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

**Remarks:** Extending fills the new region with zero bytes. The current cursor is unchanged.

<a id="m-electron2d-fileaccess-readbyte"></a>
### `public byte ReadByte()`

Reads one unsigned byte.

**Returns:** The next byte.

**Exceptions**

- `IO.EndOfStreamException`: No complete value remains.
- `InvalidOperationException`: The file is closed or not readable.
- `IO.IOException`: Reading fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-readuint16"></a>
### `public ushort ReadUInt16()`

Reads one unsigned 16-bit integer.

**Returns:** The next value using [`FileAccess.BigEndian`](FileAccess.md#p-electron2d-fileaccess-bigendian) byte order.

**Exceptions**

- `IO.EndOfStreamException`: No complete value remains.
- `InvalidOperationException`: The file is closed or not readable.
- `IO.IOException`: Reading fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-readuint32"></a>
### `public uint ReadUInt32()`

Reads one unsigned 32-bit integer.

**Returns:** The next value using [`FileAccess.BigEndian`](FileAccess.md#p-electron2d-fileaccess-bigendian) byte order.

**Exceptions**

- `IO.EndOfStreamException`: No complete value remains.
- `InvalidOperationException`: The file is closed or not readable.
- `IO.IOException`: Reading fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-readuint64"></a>
### `public ulong ReadUInt64()`

Reads one unsigned 64-bit integer.

**Returns:** The next value using [`FileAccess.BigEndian`](FileAccess.md#p-electron2d-fileaccess-bigendian) byte order.

**Exceptions**

- `IO.EndOfStreamException`: No complete value remains.
- `InvalidOperationException`: The file is closed or not readable.
- `IO.IOException`: Reading fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-readhalf"></a>
### `public Half ReadHalf()`

Reads an IEEE 754 binary16 value.

**Returns:** The next half-precision value.

**Exceptions**

- `IO.EndOfStreamException`: No complete value remains.
- `InvalidOperationException`: The file is closed or not readable.
- `IO.IOException`: Reading fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-readsingle"></a>
### `public float ReadSingle()`

Reads an IEEE 754 binary32 value.

**Returns:** The next single-precision value.

**Exceptions**

- `IO.EndOfStreamException`: No complete value remains.
- `InvalidOperationException`: The file is closed or not readable.
- `IO.IOException`: Reading fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-readdouble"></a>
### `public double ReadDouble()`

Reads an IEEE 754 binary64 value.

**Returns:** The next double-precision value.

**Exceptions**

- `IO.EndOfStreamException`: No complete value remains.
- `InvalidOperationException`: The file is closed or not readable.
- `IO.IOException`: Reading fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-readreal"></a>
### `public float ReadReal()`

Reads the engine real-number storage format.

**Returns:** The next binary32 value used by the engine's 2D numeric types.

**Exceptions**

- `IO.EndOfStreamException`: No complete value remains.
- `InvalidOperationException`: The file is closed or not readable.
- `IO.IOException`: Reading fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-readbytes-system-int32"></a>
### `public byte[] ReadBytes(int length)`

Reads up to a requested number of bytes.

**Parameters**

- `length`: The nonnegative maximum byte count.

**Returns:** A new array containing all available requested bytes.

**Exceptions**

- `ArgumentOutOfRangeException`: `length` is negative.
- `InvalidOperationException`: The file is closed or not readable.
- `IO.IOException`: Reading fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

**Remarks:** [`FileAccess.EOFReached`](FileAccess.md#p-electron2d-fileaccess-eofreached) becomes true when fewer than `length` bytes are available.

<a id="m-electron2d-fileaccess-readline"></a>
### `public string ReadLine()`

Reads UTF-8 bytes through the next LF, CR, CRLF, or null terminator.

**Returns:** The decoded line without its terminator; an empty string after end of file.

**Exceptions**

- `Text.DecoderFallbackException`: The bytes are not valid UTF-8.
- `InvalidOperationException`: The file is closed or not readable.
- `IO.IOException`: Reading fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-readcsvline-system-char"></a>
### `public string[] ReadCSVLine(char delimiter = ',')`

Reads one CSV record.

**Parameters**

- `delimiter`: The one-character field separator.

**Returns:** The decoded fields; an empty array when called at end of file.

**Exceptions**

- `ArgumentException`: `delimiter` is CR, LF, or a double quote.
- `FormatException`: The record has an unterminated or misplaced quoted field.
- `Text.DecoderFallbackException`: The bytes are not valid UTF-8.
- `InvalidOperationException`: The file is closed or not readable.
- `IO.IOException`: Reading fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-readpascalstring"></a>
### `public string ReadPascalString()`

Reads a length-prefixed UTF-8 string.

**Returns:** The decoded string whose byte count is stored as an unsigned 32-bit prefix.

**Exceptions**

- `IO.EndOfStreamException`: The prefix or payload is incomplete.
- `Text.DecoderFallbackException`: The payload is not valid UTF-8.
- `IO.IOException`: The declared length exceeds the supported managed array length.
- `InvalidOperationException`: The file is closed or not readable.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-readalltext-system-boolean"></a>
### `public string ReadAllText(bool skipCarriageReturns = false)`

Reads the entire file as UTF-8 without changing the cursor.

**Parameters**

- `skipCarriageReturns`: Whether CR bytes are omitted before decoding.

**Returns:** The decoded complete contents.

**Exceptions**

- `Text.DecoderFallbackException`: The file is not valid UTF-8.
- `InvalidOperationException`: The file is closed or not readable.
- `IO.IOException`: Reading fails or the file is too large for a managed snapshot.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-writebyte-system-byte"></a>
### `public void WriteByte(byte value)`

Writes one unsigned byte.

**Parameters**

- `value`: The value to write.

**Exceptions**

- `InvalidOperationException`: The file is closed or not writable.
- `IO.IOException`: Writing fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-writeuint16-system-uint16"></a>
### `public void WriteUInt16(ushort value)`

Writes one unsigned 16-bit integer.

**Parameters**

- `value`: The value written using [`FileAccess.BigEndian`](FileAccess.md#p-electron2d-fileaccess-bigendian) byte order.

**Exceptions**

- `InvalidOperationException`: The file is closed or not writable.
- `IO.IOException`: Writing fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-writeuint32-system-uint32"></a>
### `public void WriteUInt32(uint value)`

Writes one unsigned 32-bit integer.

**Parameters**

- `value`: The value written using [`FileAccess.BigEndian`](FileAccess.md#p-electron2d-fileaccess-bigendian) byte order.

**Exceptions**

- `InvalidOperationException`: The file is closed or not writable.
- `IO.IOException`: Writing fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-writeuint64-system-uint64"></a>
### `public void WriteUInt64(ulong value)`

Writes one unsigned 64-bit integer.

**Parameters**

- `value`: The value written using [`FileAccess.BigEndian`](FileAccess.md#p-electron2d-fileaccess-bigendian) byte order.

**Exceptions**

- `InvalidOperationException`: The file is closed or not writable.
- `IO.IOException`: Writing fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-writehalf-system-half"></a>
### `public void WriteHalf(Half value)`

Writes an IEEE 754 binary16 value.

**Parameters**

- `value`: The half-precision value.

**Exceptions**

- `InvalidOperationException`: The file is closed or not writable.
- `IO.IOException`: Writing fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-writesingle-system-single"></a>
### `public void WriteSingle(float value)`

Writes an IEEE 754 binary32 value.

**Parameters**

- `value`: The single-precision value.

**Exceptions**

- `InvalidOperationException`: The file is closed or not writable.
- `IO.IOException`: Writing fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-writedouble-system-double"></a>
### `public void WriteDouble(double value)`

Writes an IEEE 754 binary64 value.

**Parameters**

- `value`: The double-precision value.

**Exceptions**

- `InvalidOperationException`: The file is closed or not writable.
- `IO.IOException`: Writing fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-writereal-system-single"></a>
### `public void WriteReal(float value)`

Writes the engine real-number storage format.

**Parameters**

- `value`: The binary32 value used by the engine's 2D numeric types.

**Exceptions**

- `InvalidOperationException`: The file is closed or not writable.
- `IO.IOException`: Writing fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-writebytes-system-readonlyspan-system-byte"></a>
### `public void WriteBytes(ReadOnlySpan<byte> bytes)`

Writes bytes at the current cursor.

**Parameters**

- `bytes`: The bytes to write.

**Exceptions**

- `InvalidOperationException`: The file is closed or not writable.
- `IO.IOException`: Writing fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-writestring-system-string"></a>
### `public void WriteString(string value)`

Writes UTF-8 text without a length prefix or terminator.

**Parameters**

- `value`: The text to encode.

**Exceptions**

- `ArgumentNullException`: `value` is `null`.
- `Text.EncoderFallbackException`: The string contains invalid UTF-16.
- `InvalidOperationException`: The file is closed or not writable.
- `IO.IOException`: Writing fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-writeline-system-string"></a>
### `public void WriteLine(string value)`

Writes UTF-8 text followed by LF.

**Parameters**

- `value`: The text to encode.

**Exceptions**

- `ArgumentNullException`: `value` is `null`.
- `Text.EncoderFallbackException`: The string contains invalid UTF-16.
- `InvalidOperationException`: The file is closed or not writable.
- `IO.IOException`: Writing fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-writecsvline-system-collections-generic-ienumerable-system-string-system-char"></a>
### `public void WriteCSVLine(IEnumerable<string> values, char delimiter = ',')`

Writes one CSV record followed by LF.

**Parameters**

- `values`: The non-null field values.
- `delimiter`: The one-character field separator.

**Exceptions**

- `ArgumentNullException`: `values` or one of its fields is `null`.
- `ArgumentException`: `delimiter` is CR, LF, or a double quote.
- `Text.EncoderFallbackException`: A field contains invalid UTF-16.
- `InvalidOperationException`: The file is closed or not writable.
- `IO.IOException`: Writing fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-writepascalstring-system-string"></a>
### `public void WritePascalString(string value)`

Writes a UTF-8 string preceded by its unsigned 32-bit byte count.

**Parameters**

- `value`: The text to encode.

**Exceptions**

- `ArgumentNullException`: `value` is `null`.
- `Text.EncoderFallbackException`: The string contains invalid UTF-16.
- `InvalidOperationException`: The file is closed or not writable.
- `IO.IOException`: Writing fails.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-fileaccess-fileexists-system-string"></a>
### `public static bool FileExists(string path)`

Determines whether a physical or directory-backed virtual file exists.

**Parameters**

- `path`: The file path.

**Returns:** `true` only when the resolved path identifies an existing file.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `UnauthorizedAccessException`: A virtual path escapes its configured root.
- `NotSupportedException`: The path uses an unsupported virtual scheme.

<a id="m-electron2d-fileaccess-getfileasbytes-system-string"></a>
### `public static byte[] GetFileAsBytes(string path)`

Reads a complete physical or directory-backed virtual file.

**Parameters**

- `path`: The file path.

**Returns:** A new byte array containing the file.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The file cannot be read.
- `UnauthorizedAccessException`: The caller lacks filesystem access or a virtual path escapes its root.
- `NotSupportedException`: The path uses an unsupported virtual scheme.

<a id="m-electron2d-fileaccess-getfileasstring-system-string"></a>
### `public static string GetFileAsString(string path)`

Reads a complete physical or directory-backed virtual file as strict UTF-8.

**Parameters**

- `path`: The file path.

**Returns:** The decoded text.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `Text.DecoderFallbackException`: The file is not valid UTF-8.
- `IO.IOException`: The file cannot be read.
- `UnauthorizedAccessException`: The caller lacks filesystem access or a virtual path escapes its root.
- `NotSupportedException`: The path uses an unsupported virtual scheme.

<a id="m-electron2d-fileaccess-getaccesstime-system-string"></a>
### `public static long GetAccessTime(string path)`

Gets the last-access time as Unix seconds.

**Parameters**

- `path`: The file path.

**Returns:** Seconds since 1970-01-01 UTC.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.FileNotFoundException`: The file does not exist.
- `UnauthorizedAccessException`: The caller lacks filesystem access or a virtual path escapes its root.
- `NotSupportedException`: The path uses an unsupported virtual scheme.

<a id="m-electron2d-fileaccess-getmodifiedtime-system-string"></a>
### `public static long GetModifiedTime(string path)`

Gets the last-modification time as Unix seconds.

**Parameters**

- `path`: The file path.

**Returns:** Seconds since 1970-01-01 UTC.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.FileNotFoundException`: The file does not exist.
- `UnauthorizedAccessException`: The caller lacks filesystem access or a virtual path escapes its root.
- `NotSupportedException`: The path uses an unsupported virtual scheme.

<a id="m-electron2d-fileaccess-getsize-system-string"></a>
### `public static long GetSize(string path)`

Gets a file's length without opening a persistent instance.

**Parameters**

- `path`: The file path.

**Returns:** The physical byte count.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.FileNotFoundException`: The file does not exist.
- `UnauthorizedAccessException`: The caller lacks filesystem access or a virtual path escapes its root.
- `NotSupportedException`: The path uses an unsupported virtual scheme.

<a id="m-electron2d-fileaccess-getmd5-system-string"></a>
### `public static string GetMD5(string path)`

Computes a file's MD5 digest.

**Parameters**

- `path`: The file path.

**Returns:** A lowercase hexadecimal digest.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The file cannot be read.
- `UnauthorizedAccessException`: The caller lacks filesystem access or a virtual path escapes its root.
- `NotSupportedException`: The path uses an unsupported virtual scheme.

**Remarks:** MD5 is provided for compatibility and integrity checks, not security decisions.

<a id="m-electron2d-fileaccess-getsha256-system-string"></a>
### `public static string GetSHA256(string path)`

Computes a file's SHA-256 digest.

**Parameters**

- `path`: The file path.

**Returns:** A lowercase hexadecimal digest.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The file cannot be read.
- `UnauthorizedAccessException`: The caller lacks filesystem access or a virtual path escapes its root.
- `NotSupportedException`: The path uses an unsupported virtual scheme.

<a id="m-electron2d-fileaccess-ishidden-system-string"></a>
### `public static bool IsHidden(string path)`

Gets whether the filesystem marks a file as hidden.

**Parameters**

- `path`: The file path.

**Returns:** `true` when the hidden attribute is set.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The filesystem cannot read the attribute.
- `UnauthorizedAccessException`: The caller lacks filesystem access or a virtual path escapes its root.
- `NotSupportedException`: The path uses an unsupported virtual scheme.
- `PlatformNotSupportedException`: The current platform does not expose this file attribute.

<a id="m-electron2d-fileaccess-sethidden-system-string-system-boolean"></a>
### `public static void SetHidden(string path, bool hidden)`

Changes a file's hidden attribute.

**Parameters**

- `path`: The file path.
- `hidden`: Whether to set the attribute.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The filesystem cannot change the attribute.
- `UnauthorizedAccessException`: The caller lacks filesystem access or a virtual path escapes its root.
- `NotSupportedException`: The path uses an unsupported virtual scheme.
- `PlatformNotSupportedException`: The current platform does not expose this file attribute.

<a id="m-electron2d-fileaccess-isreadonly-system-string"></a>
### `public static bool IsReadOnly(string path)`

Gets whether the filesystem marks a file as read-only.

**Parameters**

- `path`: The file path.

**Returns:** `true` when the read-only attribute is set.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The filesystem cannot read the attribute.
- `UnauthorizedAccessException`: The caller lacks filesystem access or a virtual path escapes its root.
- `NotSupportedException`: The path uses an unsupported virtual scheme.
- `PlatformNotSupportedException`: The current platform does not expose this file attribute.

<a id="m-electron2d-fileaccess-setreadonly-system-string-system-boolean"></a>
### `public static void SetReadOnly(string path, bool readOnly)`

Changes a file's read-only attribute.

**Parameters**

- `path`: The file path.
- `readOnly`: Whether to set the attribute.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The filesystem cannot change the attribute.
- `UnauthorizedAccessException`: The caller lacks filesystem access or a virtual path escapes its root.
- `NotSupportedException`: The path uses an unsupported virtual scheme.
- `PlatformNotSupportedException`: The current platform does not expose this file attribute.

<a id="m-electron2d-fileaccess-getunixpermissions-system-string"></a>
### `public static UnixPermissionFlags GetUnixPermissions(string path)`

Gets Unix mode bits for a file.

**Parameters**

- `path`: The file path.

**Returns:** The permission and special-mode bits.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The filesystem cannot read the mode.
- `UnauthorizedAccessException`: The caller lacks filesystem access or a virtual path escapes its root.
- `NotSupportedException`: The path uses an unsupported virtual scheme.
- `PlatformNotSupportedException`: The current platform does not expose Unix file modes.

<a id="m-electron2d-fileaccess-setunixpermissions-system-string-electron2d-unixpermissionflags"></a>
### `public static void SetUnixPermissions(string path, UnixPermissionFlags permissions)`

Sets Unix mode bits for a file.

**Parameters**

- `path`: The file path.
- `permissions`: The permission and special-mode bits.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentOutOfRangeException`: `permissions` contains unknown bits.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The filesystem cannot change the mode.
- `UnauthorizedAccessException`: The caller lacks filesystem access or a virtual path escapes its root.
- `NotSupportedException`: The path uses an unsupported virtual scheme.
- `PlatformNotSupportedException`: The current platform does not expose Unix file modes.

<a id="m-electron2d-fileaccess-getextendedattribute-system-string-system-string"></a>
### `public static byte[] GetExtendedAttribute(string path, string name)`

Gets one extended attribute as bytes.

**Parameters**

- `path`: The ordinary or supported virtual file path.
- `name`: The nonempty attribute name without a Linux namespace prefix.

**Returns:** A new byte array containing the attribute value.

**Exceptions**

- `ArgumentNullException`: An argument is `null`.
- `ArgumentException`: An argument is empty or the attribute name is invalid.
- `IO.IOException`: The filesystem rejects the request.
- `UnauthorizedAccessException`: The caller lacks filesystem access.
- `PlatformNotSupportedException`: The platform has no implemented extended-attribute backend.

<a id="m-electron2d-fileaccess-getextendedattributestring-system-string-system-string"></a>
### `public static string GetExtendedAttributeString(string path, string name)`

Gets one extended attribute as strict UTF-8.

**Parameters**

- `path`: The ordinary or supported virtual file path.
- `name`: The nonempty attribute name without a Linux namespace prefix.

**Returns:** The decoded value.

**Exceptions**

- `ArgumentNullException`: An argument is `null`.
- `ArgumentException`: An argument is empty or the attribute name is invalid.
- `Text.DecoderFallbackException`: The value is not valid UTF-8.
- `IO.IOException`: The filesystem rejects the request.
- `UnauthorizedAccessException`: The caller lacks filesystem access.
- `PlatformNotSupportedException`: The platform has no implemented extended-attribute backend.

<a id="m-electron2d-fileaccess-getextendedattributeslist-system-string"></a>
### `public static IReadOnlyList<string> GetExtendedAttributesList(string path)`

Lists extended attribute names without platform namespace syntax.

**Parameters**

- `path`: The ordinary or supported virtual file path.

**Returns:** An immutable snapshot in operating-system order.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The filesystem rejects the request.
- `UnauthorizedAccessException`: The caller lacks filesystem access.
- `PlatformNotSupportedException`: The platform has no implemented extended-attribute backend.

<a id="m-electron2d-fileaccess-setextendedattribute-system-string-system-string-system-readonlyspan-system-byte"></a>
### `public static void SetExtendedAttribute(string path, string name, ReadOnlySpan<byte> value)`

Sets one extended attribute from bytes.

**Parameters**

- `path`: The ordinary or supported virtual file path.
- `name`: The nonempty attribute name without a Linux namespace prefix.
- `value`: The attribute bytes.

**Exceptions**

- `ArgumentNullException`: `path` or `name` is `null`.
- `ArgumentException`: An argument is empty or the attribute name is invalid.
- `IO.IOException`: The filesystem rejects the request.
- `UnauthorizedAccessException`: The caller lacks filesystem access.
- `PlatformNotSupportedException`: The platform has no implemented extended-attribute backend.

<a id="m-electron2d-fileaccess-setextendedattributestring-system-string-system-string-system-string"></a>
### `public static void SetExtendedAttributeString(string path, string name, string value)`

Sets one extended attribute from UTF-8 text.

**Parameters**

- `path`: The ordinary or supported virtual file path.
- `name`: The nonempty attribute name without a Linux namespace prefix.
- `value`: The text value.

**Exceptions**

- `ArgumentNullException`: An argument is `null`.
- `ArgumentException`: An argument is empty or the attribute name is invalid.
- `Text.EncoderFallbackException`: `value` contains invalid UTF-16.
- `IO.IOException`: The filesystem rejects the request.
- `UnauthorizedAccessException`: The caller lacks filesystem access.
- `PlatformNotSupportedException`: The platform has no implemented extended-attribute backend.

<a id="m-electron2d-fileaccess-removeextendedattribute-system-string-system-string"></a>
### `public static void RemoveExtendedAttribute(string path, string name)`

Removes one extended attribute.

**Parameters**

- `path`: The ordinary or supported virtual file path.
- `name`: The nonempty attribute name without a Linux namespace prefix.

**Exceptions**

- `ArgumentNullException`: An argument is `null`.
- `ArgumentException`: An argument is empty or the attribute name is invalid.
- `IO.IOException`: The filesystem rejects the request.
- `UnauthorizedAccessException`: The caller lacks filesystem access.
- `PlatformNotSupportedException`: The platform has no implemented extended-attribute backend.

<a id="m-electron2d-fileaccess-dispose-system-boolean"></a>
### `protected override void Dispose(bool disposing)`

Releases resources owned by a derived class.

**Parameters**

- `disposing`: `true` when called from [`ElectronObject.Dispose`](ElectronObject.md#m-electron2d-electronobject-dispose).

**Remarks:** Overrides release managed resources when `disposing` is true and then call the base implementation.

## Inherited API

Public and protected members inherited from [ElectronObject](ElectronObject.md). Their lifecycle and error contracts remain applicable unless this page states an override.

## Lifecycle and state transitions

1. A static open validates path, exact mode, codec/key/password, and existing-file requirements before returning ownership.
2. Raw files operate directly. Compressed/encrypted files decode fully into a seekable memory stream before the instance is published.
3. Writes mark transformed state dirty. `Flush` encodes a complete snapshot and atomically replaces the physical destination; the in-memory cursor is restored.
4. `Close` commits when writable, releases/zeroes memory, removes a disposable temporary file, and enters the closed state. `Dispose` then enters the inherited terminal state.

Closed instances retain path diagnostics and reject stream operations with `InvalidOperationException`. Disposed instances reject guarded operations with `ObjectDisposedException`.

## Invariants and error behavior

- Only the four exact `FileAccessModeFlags` values are accepted.
- Read and write permissions are enforced independently of the underlying in-memory stream's technical capabilities.
- Write modes truncate; read-write mode requires and preserves an existing file.
- No destination directory is implicitly created.
- Endianness affects every multi-byte numeric value and the Pascal byte-count prefix.
- Compression envelopes record version, codec, and decoded length; malformed, mismatched, or truncated envelopes fail before publication.
- Raw encryption requires exactly 32 key bytes. Password mode uses a random 16-byte salt, 600,000 PBKDF2-HMAC-SHA-256 iterations, and a 32-byte derived key.
- AES-256-GCM uses a fresh 12-byte nonce, a 16-byte tag, and authenticates the complete envelope header. Wrong credentials, mode confusion, and tampering fail before plaintext exposure.
- Atomic transformed commits use a flushed same-directory temporary file and replacement. Failure preserves any previous destination but cannot promise crash durability on every filesystem.
- Unknown virtual schemes and lexical root traversal are rejected by `ProjectSettings`.

Filesystem, authorization, decoder, compression, cryptographic, and disposed/closed failures are reported with standard typed C# exceptions. Numeric error-return state and a process-wide last-open error are intentionally absent.

## Threading and re-entrancy

All instance state/stream operations are serialized by one private lock. One call is atomic with respect to other calls, including Pascal prefix+payload operations and transformed flush. A caller-owned sequence such as `Seek` followed by `ReadBytes` is not atomic against another caller unless externally synchronized. Static operations own only local state and are safe concurrently. Blocking I/O, hashing, compression, PBKDF2, and allocation make the component unsuitable for frame, physics, audio, or render hot paths.

## Dependencies and interactions

- Inherits `ElectronObject` for deterministic lifetime and disposed-state ordering.
- Uses `ProjectSettings.GlobalizePath` for directory-backed virtual paths.
- Uses .NET file, UTF-8, hash, compression, PBKDF2, random, and AES-GCM primitives.
- Reuses the same internal atomic-replacement helper as `ConfigFile`.
- Uses Linux libc xattrs, macOS libSystem xattrs, and Windows alternate data streams for the extended-attribute surface.
- Has no SDL, Scene, renderer, resource loader, scripting, editor, physics, or external package dependency.

## Verification and known limits

`tests/Electron2D.Tests/Program.cs` covers invalid paths/modes, all four access modes, truncation/preservation, little/big-endian primitives, half/float/double/real, buffers, strict UTF-8, CR/LF/null lines, CSV/Pascal strings, EOF and seek transitions, resize, cursor preservation on decode failure, close/dispose, static metadata/hashes and missing-file errors, explicit unsupported Linux hidden/read-only attributes, Unix permissions, Linux xattrs, temporary ownership including prefix/extension/read-only mode, three codecs, malformed/mismatched compression, raw/password encryption, wrong keys/passwords/mode confusion, tampering, virtual project paths, concurrent static hashing and compound Pascal writes, and commit failure cleanup.

The checks ran on Linux with its active filesystem. They do not prove the implemented macOS xattr or Windows alternate-stream backends, and no Android/iOS extended-attribute backend or native-host verification is currently claimed. Web browser storage behavior is unimplemented and unverified; its capability boundary belongs to the first Web host slice. These checks also do not prove every filesystem's crash durability, very-large-file memory behavior for transformed whole-file access, hostile TOCTOU races, or protection after process compromise. FastLZ, Zstandard, and packed archives remain explicitly unimplemented.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0003: ElectronObject lifetime](../decisions/core-object-runtime.md#adr-0003)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0019: Typed project settings and directory-backed virtual paths](../decisions/core-data-io.md#adr-0019)
- [0020: Typed file access and transformed-file containers](../decisions/core-data-io.md#adr-0020)
