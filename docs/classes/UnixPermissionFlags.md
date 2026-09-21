# UnixPermissionFlags

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/IO/UnixPermissionFlags.cs`](../../src/Core/IO/UnixPermissionFlags.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum UnixPermissionFlags`

> Represents Unix file permission and special-mode bits.

## Description

Represents Unix file permission and special-mode bits.

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

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = UnixPermissionFlags.None;
```

## Constants

| Member | Description |
| --- | --- |
| [`None = 0`](#f-electron2d-unixpermissionflags-none) | No permission bits are set. |
| [`ExecuteOther = 1`](#f-electron2d-unixpermissionflags-executeother) | Other users may execute the file or search the directory. |
| [`WriteOther = 2`](#f-electron2d-unixpermissionflags-writeother) | Other users may write the file or directory. |
| [`ReadOther = 4`](#f-electron2d-unixpermissionflags-readother) | Other users may read the file or directory. |
| [`ExecuteGroup = 8`](#f-electron2d-unixpermissionflags-executegroup) | Group members may execute the file or search the directory. |
| [`WriteGroup = 16`](#f-electron2d-unixpermissionflags-writegroup) | Group members may write the file or directory. |
| [`ReadGroup = 32`](#f-electron2d-unixpermissionflags-readgroup) | Group members may read the file or directory. |
| [`ExecuteOwner = 64`](#f-electron2d-unixpermissionflags-executeowner) | The owner may execute the file or search the directory. |
| [`WriteOwner = 128`](#f-electron2d-unixpermissionflags-writeowner) | The owner may write the file or directory. |
| [`ReadOwner = 256`](#f-electron2d-unixpermissionflags-readowner) | The owner may read the file or directory. |
| [`RestrictedDelete = 512`](#f-electron2d-unixpermissionflags-restricteddelete) | Restricts deletion or renaming in a directory to owners and privileged users. |
| [`SetGroupId = 1024`](#f-electron2d-unixpermissionflags-setgroupid) | Uses the directory group for new entries or applies the file group identity on execution. |
| [`SetUserId = 2048`](#f-electron2d-unixpermissionflags-setuserid) | Applies the file owner identity on execution. |

## Constant Descriptions

<a id="f-electron2d-unixpermissionflags-none"></a>
### `None = 0`

No permission bits are set.

<a id="f-electron2d-unixpermissionflags-executeother"></a>
### `ExecuteOther = 1`

Other users may execute the file or search the directory.

<a id="f-electron2d-unixpermissionflags-writeother"></a>
### `WriteOther = 2`

Other users may write the file or directory.

<a id="f-electron2d-unixpermissionflags-readother"></a>
### `ReadOther = 4`

Other users may read the file or directory.

<a id="f-electron2d-unixpermissionflags-executegroup"></a>
### `ExecuteGroup = 8`

Group members may execute the file or search the directory.

<a id="f-electron2d-unixpermissionflags-writegroup"></a>
### `WriteGroup = 16`

Group members may write the file or directory.

<a id="f-electron2d-unixpermissionflags-readgroup"></a>
### `ReadGroup = 32`

Group members may read the file or directory.

<a id="f-electron2d-unixpermissionflags-executeowner"></a>
### `ExecuteOwner = 64`

The owner may execute the file or search the directory.

<a id="f-electron2d-unixpermissionflags-writeowner"></a>
### `WriteOwner = 128`

The owner may write the file or directory.

<a id="f-electron2d-unixpermissionflags-readowner"></a>
### `ReadOwner = 256`

The owner may read the file or directory.

<a id="f-electron2d-unixpermissionflags-restricteddelete"></a>
### `RestrictedDelete = 512`

Restricts deletion or renaming in a directory to owners and privileged users.

<a id="f-electron2d-unixpermissionflags-setgroupid"></a>
### `SetGroupId = 1024`

Uses the directory group for new entries or applies the file group identity on execution.

<a id="f-electron2d-unixpermissionflags-setuserid"></a>
### `SetUserId = 2048`

Applies the file owner identity on execution.

## Invariants and errors

Only the low 12 bits are accepted by `FileAccess.SetUnixPermissions` and the instance `UnixPermissions` property. Unknown bits throw `ArgumentOutOfRangeException`. Windows throws `PlatformNotSupportedException` instead of simulating Unix permissions.

## Threading and dependencies

The value owns no state. Operations use `System.IO.File.GetUnixFileMode` and `SetUnixFileMode`; instance access is serialized by `FileAccess`.

## Verification

The executable harness saves, changes, reads, and restores the exact mode on the current non-Windows filesystem.
