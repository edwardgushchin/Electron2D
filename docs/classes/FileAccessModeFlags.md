# FileAccessModeFlags

Last updated: 2026-11-16

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/IO/FileAccessModeFlags.cs`](../../src/Core/IO/FileAccessModeFlags.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum FileAccessModeFlags`

> Specifies the operations permitted by an opened [`FileAccess`](FileAccess.md).

## Description

Specifies the operations permitted by an opened [`FileAccess`](FileAccess.md).

`FileAccessModeFlags` defines creation, truncation, read, and write behavior for one `FileAccess` instance.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = FileAccessModeFlags.Read;
```

## Constants

| Member | Description |
| --- | --- |
| [`Read = 1`](#f-electron2d-fileaccessmodeflags-read) | Opens an existing file for reading from its beginning. |
| [`Write = 2`](#f-electron2d-fileaccessmodeflags-write) | Creates or truncates a file and opens it for writing from its beginning. |
| [`ReadWrite = 3`](#f-electron2d-fileaccessmodeflags-readwrite) | Opens an existing file for reading and writing without truncating it. |
| [`WriteRead = 7`](#f-electron2d-fileaccessmodeflags-writeread) | Creates or truncates a file and opens it for reading and writing. |

## Constant Descriptions

<a id="f-electron2d-fileaccessmodeflags-read"></a>
### `Read = 1`

Opens an existing file for reading from its beginning.

<a id="f-electron2d-fileaccessmodeflags-write"></a>
### `Write = 2`

Creates or truncates a file and opens it for writing from its beginning.

<a id="f-electron2d-fileaccessmodeflags-readwrite"></a>
### `ReadWrite = 3`

Opens an existing file for reading and writing without truncating it.

<a id="f-electron2d-fileaccessmodeflags-writeread"></a>
### `WriteRead = 7`

Creates or truncates a file and opens it for reading and writing.

## Lifecycle, invariants, and threading

The value owns no state. `FileAccess` validates it before touching the filesystem. It does not grant permission beyond operating-system access control.

## Dependencies and limitations

The enum depends only on `System.FlagsAttribute`. `CreateTemp` creates the empty file with an internal write handle first, so the returned owner may use any of the four modes, including `Read`.

## Verification

`FileAccessModeTests`, called by the lifecycle profile, verifies all four numeric identities and ordinary, five compressed and both encrypted factories: missing/existing paths, truncation, permissions, cursor, Close/Dispose, persisted bytes and invalid combinations. Transformed factories own memory streams; there is no separate public memory-file factory. The lifecycle regression preserves mounted PCK/ZIP read-only boundaries. Windows x64 CLR is exercised; foreign/package qualification is separate.

Exact Write participates in the default-disabled [save-and-swap policy](../components/file-save-and-swap.md): creation/truncation becomes visible on close. ReadWrite and WriteRead remain direct.
