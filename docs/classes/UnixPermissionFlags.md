# UnixPermissionFlags

Last updated: 2026-09-21

## Source and declaration

- Source: [`src/Core/IO/UnixPermissionFlags.cs`](../../src/Core/IO/UnixPermissionFlags.cs)
- Declaration: `[Flags] public enum UnixPermissionFlags`
- Assembly and namespace: `Electron2D.dll`, `Electron2D`

## Responsibility and complete values

`UnixPermissionFlags` is the typed public representation of native Unix mode bits.

| Value | Number | Meaning |
| --- | ---: | --- |
| `None` | 0 | No bits |
| `ExecuteOther` | 1 | Other execute/search |
| `WriteOther` | 2 | Other write |
| `ReadOther` | 4 | Other read |
| `ExecuteGroup` | 8 | Group execute/search |
| `WriteGroup` | 16 | Group write |
| `ReadGroup` | 32 | Group read |
| `ExecuteOwner` | 64 | Owner execute/search |
| `WriteOwner` | 128 | Owner write |
| `ReadOwner` | 256 | Owner read |
| `RestrictedDelete` | 512 | Sticky/restricted-delete bit |
| `SetGroupId` | 1024 | Set-group-ID bit |
| `SetUserId` | 2048 | Set-user-ID bit |

## Invariants and errors

Only the low 12 bits are accepted by `FileAccess.SetUnixPermissions` and the instance `UnixPermissions` property. Unknown bits throw `ArgumentOutOfRangeException`. Windows throws `PlatformNotSupportedException` instead of simulating Unix permissions.

## Threading and dependencies

The value owns no state. Operations use `System.IO.File.GetUnixFileMode` and `SetUnixFileMode`; instance access is serialized by `FileAccess`.

## Verification

The executable harness saves, changes, reads, and restores the exact mode on the current non-Windows filesystem.
