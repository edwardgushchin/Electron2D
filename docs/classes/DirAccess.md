# DirAccess

Last updated: 2026-09-21

## Source and declaration

- Source: [`src/Core/IO/DirAccess.cs`](../../src/Core/IO/DirAccess.cs)
- Declaration: `public sealed class DirAccess : ElectronObject`
- Assembly and namespace: `Electron2D.dll`, `Electron2D`
- Domain: [Core](../domains/core.md)
- Component: [File and directory access](../components/file-access.md)

## Responsibility and ownership

`DirAccess` owns one current physical directory, one immutable access scope, and at most one in-memory listing snapshot. An accessor opened through `res://` or `user://` captures that virtual root and cannot switch scope or escape it lexically. An ordinary accessor remains filesystem-scoped.

An instance opened normally never owns the directory. `CreateTemp` is the exception: unless `keep` is true, the returned instance owns the exact created root and recursively deletes it on disposal even if its current directory later changes. This private cleanup is the only recursive-delete behavior; the public removal API deletes only a file, link, or empty directory.

## Complete public API

### Creation and filtering

| Member | Current behavior |
| --- | --- |
| `static Open(string path)` | Opens an existing ordinary, `res://`, or `user://` directory; returns non-null or throws |
| `static CreateTemp(string prefix = "", bool keep = false)` | Creates an exclusive system-temporary directory; validates a portable prefix; optionally transfers cleanup responsibility to the caller |
| `bool IncludeHidden` | Defaults false; affects active listing reads and both snapshot helpers |
| `bool IncludeNavigational` | Defaults false; includes synthetic `.` and `..` in listing and directory snapshots |

### Listing

| Member | Current behavior |
| --- | --- |
| `ListDirBegin()` | Replaces any active listing with an unsorted snapshot of the current directory |
| `GetNext()` | Returns the next visible name without its parent path; empty means no active listing or EOF and closes the listing |
| `CurrentIsDir()` | Reports the last returned entry kind; false before the first result and after end/close |
| `ListDirEnd()` | Idempotently closes the listing and clears current-entry state |
| `GetFiles()` / `GetDirectories()` | Return immutable ordinally sorted visible-name snapshots |
| `static GetFilesAt(path)` / `GetDirectoriesAt(path)` | Apply default filters to an absolute native or virtual directory |

The listing captures names and entry kinds at `ListDirBegin`; later external changes can make the snapshot stale. Its order is the filesystem enumeration order and is intentionally unspecified. Changes to either include property affect the unconsumed portion of an active snapshot.

### Current directory and filesystem queries

| Member | Current behavior |
| --- | --- |
| `ChangeDir(path)` | Resolves relative to the current directory, requires an existing directory in the captured scope, and closes an active listing |
| `GetCurrentDir(bool includeDrive = true)` | Returns the preserved virtual form for virtual scopes, otherwise the normalized native path |
| `FileExists(path)` / `DirExists(path)` | Resolve relative or in-scope absolute paths and return false only for absence |
| `static DirExistsAbsolute(path)` | Tests an absolute native or virtual directory path |
| `GetSpaceLeft()` | Returns current-user available filesystem bytes through the most specific mount; Windows queries the current path directly, including UNC paths |
| `GetFilesystemType()` | Returns the uppercase filesystem type reported for the most specific mount, or `Network Share` for a Windows UNC path |
| `IsCaseSensitive(path)` | Reads Linux directory case-fold flags, macOS volume capabilities, or Windows per-entry flags without mutating the filesystem; other targets report their default case-sensitive contract |
| `IsEquivalent(pathA, pathB)` | Compares native volume/device and file identities while following links; hard-link names compare equivalent |
| `IsBundle(path)` | On macOS, asks the native workspace whether an existing directory is a file package; other platforms throw |

### Drives

`static GetDriveCount()`, `static GetDriveName(int)`, `static GetDriveLabel(int)`, and `GetCurrentDrive()` create a fresh platform snapshot on Linux, Windows, and macOS. Windows exposes names such as `C:` and native labels (`<network>` for remote drives). macOS asks the native file manager for mounted volumes while skipping hidden volumes; labels are empty. Linux exposes `/`, eligible `/dev` mounts under user/removable mount roots, the home and desktop directories, and `file://` GTK 3 bookmarks; it deduplicates and ordinally sorts them, and labels are empty. The current drive is the most specific containing root. Invalid indices throw as a typed C# boundary. Android storage-volume discovery and iOS drive enumeration throw until their host/storage integrations exist.

### Mutations and links

| Member | Current behavior |
| --- | --- |
| `MakeDir(path)` / `static MakeDirAbsolute(path)` | Create exactly one new directory; parent must exist and existing target fails |
| `MakeDirRecursive(path)` / static absolute variant | Create all missing parents and accept existing directory intermediates |
| `Copy(source, destination, UnixPermissionFlags? = null)` / static absolute variant | Copy one file, overwrite a file destination, never create its parent, optionally apply Unix permissions |
| `Rename(source, destination)` / static absolute variant | Move a file or directory; files overwrite files, directories replace only empty directories, cross-filesystem failures propagate |
| `Remove(path)` / static absolute variant | Permanently remove one file, link, or empty directory; never recursively remove caller-selected content |
| `CreateLink(source, target)` | Create a file or directory symbolic link; relative sources stay relative and may be dangling, while virtual/absolute sources are stored as resolved physical paths |
| `IsLink(path)` / `ReadLink(path)` | Detect symbolic links on Linux/macOS and every reparse point on Windows; read symbolic-link and mount-point targets |

Static `Absolute` methods reject relative paths before filesystem access. `CopyAbsolute` and `RenameAbsolute` resolve both arguments from one locked project/user-root snapshot, so a concurrent path reconfiguration cannot split the operation across root generations. Instance mutations remain inside their captured access scope. Link APIs explicitly reject Android and iOS until those targets receive native-host verification; Windows link creation can additionally require Developer Mode or elevation.

The complete inherited identity, notification, typed-property, translation, and disposal surface is documented by [`ElectronObject`](ElectronObject.md). Reference-counted lifetime and a shared last-open error are not part of this class.

## Lifecycle and state transitions

1. `Open` resolves and validates an existing directory, captures scope/root, and publishes an accessor without an active listing.
2. `ListDirBegin` atomically replaces listing state. Each successful `GetNext` publishes one entry kind for `CurrentIsDir`; EOF or `ListDirEnd` clears it.
3. `ChangeDir` validates the new directory before committing it and invalidates listing state only after success.
4. Ordinary disposal closes listing state without altering the opened directory.
5. Non-kept temporary disposal closes listing state, recursively removes the captured owned root without following directory links, then reaches the inherited terminal state. Cleanup failures propagate while `ElectronObject` still publishes disposed state.

## Invariants and error behavior

- Paths are non-null and nonempty. Unknown schemes fail explicitly.
- Virtual confinement is lexical and is not a symbolic-link security sandbox.
- Instance scope never changes; ordinary absolute paths used by virtual accessors must still remain under the captured root.
- Static directory methods require a fully qualified native path or a supported virtual path.
- Single-directory creation never succeeds for an existing entry; recursive creation is idempotent for existing directories.
- Copy is file-only and non-atomic. A failure can leave a partial or replaced destination according to filesystem behavior.
- Public removal is permanent and nonrecursive. Symlink removal acts on the link, not its target.
- Rename atomicity, overwrite details, and cross-volume behavior are bounded by the host filesystem; the class does not simulate a copy-delete move. When replacing an empty directory requires removing it first, a failed move recreates the destination if no concurrent entry has appeared; a failed rollback is reported together with the move failure.
- `IsCaseSensitive` is read-only. Linux treats an unavailable case-fold query as case-sensitive; macOS and Windows conservatively return false when their native metadata query fails.
- `IsEquivalent` uses native file identity: Win32 handle metadata on Windows and `stat` device/inode identity on 64-bit Unix-family targets. Missing entries compare false; unsupported runtimes throw.
- Numeric error codes and thread-local open-error state are replaced by standard typed C# exceptions.

## Threading and re-entrancy

All instance fields and one-call operations are serialized by a private lock. Concurrent `GetNext` calls consume distinct entries. Compound caller sequences are not atomic: another caller can run between `GetNext` and `CurrentIsDir`, or between `ChangeDir` and a later mutation, unless callers synchronize externally. Static methods own local state; two-path static operations capture virtual roots together.

Filesystem state remains external and can change between validation and mutation. Listing snapshots, path checks, copy, rename, removal, drive queries, identity queries, and temporary cleanup are subject to ordinary TOCTOU behavior. Every operation can block or allocate and is excluded from frame, fixed-step, render, and audio hot paths.

## Dependencies and interactions

- Inherits `ElectronObject` for deterministic disposal and disposed-state ordering.
- Uses `ProjectSettings` for current virtual roots and an internal atomic root-pair snapshot.
- Reuses `UnixPermissionFlags` and `FileAccess.SetUnixPermissions` for optional copy permissions.
- Uses `System.IO` for paths, directory/file mutations, links, temporary ownership, ordinary mount metadata, and Windows logical drives.
- Uses native Win32/NT metadata for filesystem identity, directory case flags, and UNC capacity; Unix `stat` and Linux file flags for identity and case folding; and macOS Foundation/AppKit metadata for mounted volumes and file-package classification.
- Has no SDL, Scene, renderer, asset loader, pack mount, scripting, editor, physics, or external package dependency.

## Reference API coverage audit

The stable reference surface contains 2 properties and 38 methods.

| Reference area | Electron2D classification |
| --- | --- |
| Include-hidden/include-navigational properties | Implemented with live filtering over the remaining listing snapshot |
| Open and temporary creation | Implemented as non-null factories with exceptions and deterministic disposal |
| Streaming listing and sorted snapshots | Implemented; snapshots use immutable `IReadOnlyList<string>` and ordinal sorting |
| Current directory, existence, directory creation | Implemented with captured physical/resource/user scope |
| Copy, rename, remove | Implemented with typed nullable Unix permissions and standard exceptions |
| Links, bundle, filesystem/case/identity queries | Implemented with explicit platform boundaries; Linux behavior is executable-tested |
| Drive enumeration | Implemented for Linux/Windows/macOS; exact Android storage volumes and iOS enumeration are dependency-blocked |
| Numeric `Error` returns and `get_open_error` | Permanently adapted to exceptions; no mutable shared/thread-local error slot exists |
| `RefCounted` inheritance | Permanently adapted to managed memory plus `ElectronObject`/`IDisposable` |
| Packed/exported `res://` and import remapping | Deferred until pack-mount and resource-loader domains exist |
| `copy_dir` and recursive public erase | Not in the audited stable public surface; no speculative API added |

The inherited object surface follows the existing [`ElectronObject`](ElectronObject.md) coverage. Dynamic invocation, untyped properties/metadata, string-addressed signals, script attachment, manual reference counting, and cancellable deletion remain excluded or dependency-blocked by their owning decisions.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` covers null/empty/missing/relative/unknown paths, ordinary and virtual scopes, lexical traversal, current-directory changes, hidden/navigational filters, unspecified streaming versus sorted snapshots, concurrent listing consumption, single/recursive creation, copy overwrite, pre-mutation permission validation and self-copy rejection, instance/static file and directory rename with failed-move destination restoration, nonrecursive removal, relative and dangling symlink targets, hard-link and symlink equivalence on Linux, native case metadata, drive/capacity/filesystem queries, absolute helpers, temporary keep/delete ownership, current-directory changes before temporary disposal, and disposed access.

The executable evidence is Linux-only. Windows/macOS link, file-identity, drive, filesystem-type, rename, case, permission, and path-root behavior are compiled but not native-host verified. Android/iOS identity code is compiled but not device-verified; their link and drive-enumeration APIs intentionally throw until platform host/storage integration is implemented. Packed/exported resources remain absent.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0003: ElectronObject lifetime](../decisions/core-object-runtime.md#adr-0003)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0019: Typed project settings and directory-backed virtual paths](../decisions/core-data-io.md#adr-0019)
- [0020: Typed file access and transformed-file containers](../decisions/core-data-io.md#adr-0020)
- [0021: Cross-platform runtime target matrix](../decisions/product.md#adr-0021)
- [0022: Typed directory access](../decisions/core-data-io.md#adr-0022)
