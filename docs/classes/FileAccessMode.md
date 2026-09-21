# FileAccessMode

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/IO/FileAccessMode.cs`](../../src/Core/IO/FileAccessMode.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum FileAccessMode`

> Specifies the operations permitted by an opened [`FileAccess`](FileAccess.md).

## Description

Specifies the operations permitted by an opened [`FileAccess`](FileAccess.md).

`FileAccessMode` defines creation, truncation, read, and write behavior for one `FileAccess` instance.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = FileAccessMode.Read;
```

## Constants

| Member | Description |
| --- | --- |
| [`Read = 1`](#f-electron2d-fileaccessmode-read) | Opens an existing file for reading from its beginning. |
| [`Write = 2`](#f-electron2d-fileaccessmode-write) | Creates or truncates a file and opens it for writing from its beginning. |
| [`ReadWrite = 3`](#f-electron2d-fileaccessmode-readwrite) | Opens an existing file for reading and writing without truncating it. |
| [`WriteRead = 7`](#f-electron2d-fileaccessmode-writeread) | Creates or truncates a file and opens it for reading and writing. |

## Constant Descriptions

<a id="f-electron2d-fileaccessmode-read"></a>
### `Read = 1`

Opens an existing file for reading from its beginning.

<a id="f-electron2d-fileaccessmode-write"></a>
### `Write = 2`

Creates or truncates a file and opens it for writing from its beginning.

<a id="f-electron2d-fileaccessmode-readwrite"></a>
### `ReadWrite = 3`

Opens an existing file for reading and writing without truncating it.

<a id="f-electron2d-fileaccessmode-writeread"></a>
### `WriteRead = 7`

Creates or truncates a file and opens it for reading and writing.

## Lifecycle, invariants, and threading

The value owns no state. `FileAccess` validates it before touching the filesystem. It does not grant permission beyond operating-system access control.

## Dependencies and limitations

The enum depends only on `System.FlagsAttribute`. `CreateTemp` creates the empty file with an internal write handle first, so the returned owner may use any of the four modes, including `Read`.

## Verification

`tests/Electron2D.Tests/Program.cs` verifies every mode's existence/truncation/read/write behavior and invalid combinations.
