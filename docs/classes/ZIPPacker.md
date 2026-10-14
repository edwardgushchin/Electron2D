# ZIPPacker

Last updated: 2026-10-14

**Inherits:** [ElectronObject](ElectronObject.md). **Source:** [ZIPPacker.cs](../../src/Core/IO/ZIPPacker.cs). **Component:** [ZIP archives](../components/zip-archives.md).

## Behavior

Owns a seekable destination and streams raw deflate through the installed runtime compressor. CompressionLevel defaults to -1 and accepts every integer -1..9; changes affect the next started entry. ZIPCompressionLevel names common choices without narrowing the integer range. ZIPAppend Create truncates, CreateAfter preserves existing prefix bytes and starts an archive after them, and AddInZIP preserves existing entries and their raw central records, extra fields and archive comment before appending the new final directory. Old payloads are not decoded or recompressed.

StartFile finishes the prior entry and adds its immediate parent directory when needed. WriteFile borrows its ReadOnlySpan only for the call. CloseFile requires an active file. AddDirectory stores a slash-terminated empty directory and finishes the prior file; duplicate directories within one session throw IOException. Duplicate file names are permitted. UnixPermissionFlags metadata is portable independently of host filesystem permission support; zero selects 0644 for files or 0755 for directories. Unix seconds convert to local DOS time using the timestamp's zone offset, clamp to 1980..2107 and retain two-second precision; zero uses current time.

Writes UTF-8 names and CRC with ZIP64 local size fields. Large offsets/sizes/counts emit ZIP64 central/end metadata. Explicit Close and deterministic disposal finish the active file and archive; repeated Close is harmless. File writes are not transactional: failure may leave a partial archive, and a failed session rejects further entry writes until closed/reopened. Names are archive keys, with no automatic filesystem extraction or pack mounting. Calls serialize on one instance gate; compound caller sequences are not transactional.

## API

- `public ZIPPacker()`
- `public System.Void AddDirectory(System.String path, Electron2D.UnixPermissionFlags permissions = ExecuteOther, ReadOther, ExecuteGroup, ReadGroup, ExecuteOwner, WriteOwner, ReadOwner, System.UInt64 modifiedTime = 0)`
- `public System.Void Close()`
- `public System.Void CloseFile()`
- `protected override System.Void Dispose(System.Boolean disposing)`
- `public System.Void Open(System.String path, Electron2D.ZIPAppend append = Create)`
- `public System.Void StartFile(System.String path, Electron2D.UnixPermissionFlags permissions = ReadOther, ReadGroup, WriteOwner, ReadOwner, System.UInt64 modifiedTime = 0)`
- `public System.Void WriteFile(System.ReadOnlySpan<System.Byte> data)`
- `public System.Int32 CompressionLevel { get; set; }`

## Verification

ZIPTests checks independent .NET archive interoperability, every compression level, Unicode/duplicate/empty entries, Unix metadata and time, append/comments/prefixes, corruption and CRC, lifetime/replacement and directory-backed res paths. Linux/macOS sparse-file testing exercises offsets beyond 4 GiB without allocating their payload. Linux Wayland GPU/compatibility tests pack a bundled scene into ZIP, read it, restore it and verify actual texture pixels. Other hosts and owner acceptance remain unverified. See [ADR 0020](../decisions/core-data-io.md#adr-0020) and [coverage](../coverage/classes/ZIPPacker.md).
