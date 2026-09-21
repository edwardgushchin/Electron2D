# FileAccessMode

Last updated: 2026-09-21

## Source and declaration

- Source: [`src/Core/IO/FileAccessMode.cs`](../../src/Core/IO/FileAccessMode.cs)
- Declaration: `[Flags] public enum FileAccessMode`
- Assembly and namespace: `Electron2D.dll`, `Electron2D`

## Responsibility

`FileAccessMode` defines creation, truncation, read, and write behavior for one `FileAccess` instance.

## Values

| Value | Number | Behavior |
| --- | ---: | --- |
| `Read` | 1 | Existing file, read-only, cursor at zero |
| `Write` | 2 | Create or truncate, write-only, cursor at zero |
| `ReadWrite` | 3 | Existing file, read/write, no truncation, cursor at zero |
| `WriteRead` | 7 | Create or truncate, read/write, cursor at zero |

Although the enum is flagged to retain the reference numeric values, only these four exact constants are valid. Other bit combinations throw `ArgumentOutOfRangeException`.

## Lifecycle, invariants, and threading

The value owns no state. `FileAccess` validates it before touching the filesystem. It does not grant permission beyond operating-system access control.

## Dependencies and limitations

The enum depends only on `System.FlagsAttribute`. `CreateTemp` creates the empty file with an internal write handle first, so the returned owner may use any of the four modes, including `Read`.

## Verification

`tests/Electron2D.Tests/Program.cs` verifies every mode's existence/truncation/read/write behavior and invalid combinations.
