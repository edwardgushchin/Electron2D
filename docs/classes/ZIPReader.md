# ZIPReader

Last updated: 2026-10-14

**Inherits:** [ElectronObject](ElectronObject.md). **Source:** [ZIPReader.cs](../../src/Core/IO/ZIPReader.cs). **Component:** [ZIP archives](../components/zip-archives.md).

## Behavior

Owns one read-only ZIP file session. Open resolves ordinary/res:// and user:// paths and validates a replacement before releasing the previous session. GetFiles returns caller-owned names in central-directory order; directories and duplicate names remain visible. FileExists and ReadFile use ordinal exact lookup by default, or ordinal case-insensitive Unicode lookup when requested. The first matching duplicate is selected. Missing ReadFile throws FileNotFoundException; missing GetCompressionLevel returns -1. Returned compression hints are 1/2/6/9 and do not reconstruct arbitrary original encoder levels.

ReadFile returns caller-owned bytes only after complete decoded length and CRC validation. Empty entries return empty arrays. Entries exceeding managed array capacity, corrupt/truncated data and unsupported/encrypted payloads fail explicitly. Reader supports standard and ZIP64 directories, including prefixed archives. Close is idempotent, and disposal closes its file. Calls serialize on one instance gate; compound caller sequences are not transactional.

## API

- `public ZIPReader()`
- `public System.Void Close()`
- `protected override System.Void Dispose(System.Boolean disposing)`
- `public System.Boolean FileExists(System.String path, System.Boolean caseSensitive = true)`
- `public System.Int32 GetCompressionLevel(System.String path, System.Boolean caseSensitive = true)`
- `public System.String[] GetFiles()`
- `public System.Void Open(System.String path)`
- `public System.Byte[] ReadFile(System.String path, System.Boolean caseSensitive = true)`

## Verification

ZIPTests checks independent .NET archive interoperability, every compression level, Unicode/duplicate/empty entries, Unix metadata and time, append/comments/prefixes, corruption and CRC, lifetime/replacement and directory-backed res paths. Linux/macOS sparse-file testing exercises offsets beyond 4 GiB without allocating their payload. Linux Wayland GPU/compatibility tests pack a bundled scene into ZIP, read it, restore it and verify actual texture pixels. Other hosts and owner acceptance remain unverified. See [ADR 0020](../decisions/core-data-io.md#adr-0020) and [coverage](../coverage/classes/ZIPReader.md).
