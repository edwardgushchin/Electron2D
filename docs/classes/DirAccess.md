# DirAccess

Last updated: 2026-09-22

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Source:** [`src/Core/IO/DirAccess.cs`](../../src/Core/IO/DirAccess.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class DirAccess : ElectronObject`

> Provides blocking access to directories and their contents.

## Description

Provides blocking access to directories and their contents.

`DirAccess` owns one current physical directory, one immutable access scope, and at most one in-memory listing snapshot. An accessor opened through `res://` or `user://` captures that virtual root and cannot switch scope or escape it lexically. An ordinary accessor remains filesystem-scoped.

An instance opened normally never owns the directory. `CreateTemp` is the exception: unless `keep` is true, the returned instance owns the exact created root and recursively deletes it on disposal even if its current directory later changes. This private cleanup is the only recursive-delete behavior; the public removal API deletes only a file, link, or empty directory.

Instances retain one current directory and one access scope. Relative paths resolve from that directory;
`res://` and `user://` instances cannot leave their configured root or switch scopes. Operations are
serialized per instance, may block, and are unsuitable for real-time callbacks.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using DirAccess directory = DirAccess.Open("user://");
foreach (string fileName in directory.GetFiles())
    Console.WriteLine(fileName);
```

## Properties

| Member | Description |
| --- | --- |
| [`public bool IncludeHidden { get; set; }`](#p-electron2d-diraccess-includehidden) | Gets or sets whether hidden entries are returned by directory enumeration. |
| [`public bool IncludeNavigational { get; set; }`](#p-electron2d-diraccess-includenavigational) | Gets or sets whether the navigational entries `.` and `..` are enumerated. |

## Methods

| Member | Description |
| --- | --- |
| [`public static DirAccess Open(string path)`](#m-electron2d-diraccess-open-system-string) | Opens an existing ordinary or directory-backed virtual directory. |
| [`public static DirAccess CreateTemp(string prefix = "", bool keep = false)`](#m-electron2d-diraccess-createtemp-system-string-system-boolean) | Creates and opens a uniquely named temporary directory. |
| [`public void ListDirBegin()`](#m-electron2d-diraccess-listdirbegin) | Starts a new unsorted directory listing and closes any previous listing. |
| [`public string GetNext()`](#m-electron2d-diraccess-getnext) | Returns the next visible name from the active directory listing. |
| [`public bool CurrentIsDir()`](#m-electron2d-diraccess-currentisdir) | Reports whether the entry returned by the last successful [`DirAccess.GetNext`](DirAccess.md#m-electron2d-diraccess-getnext) call is a directory. |
| [`public void ListDirEnd()`](#m-electron2d-diraccess-listdirend) | Closes the active directory listing. |
| [`public IReadOnlyList<string> GetFiles()`](#m-electron2d-diraccess-getfiles) | Returns a sorted snapshot of visible file names in the current directory. |
| [`public static IReadOnlyList<string> GetFilesAt(string path)`](#m-electron2d-diraccess-getfilesat-system-string) | Returns a sorted snapshot of file names from an absolute or virtual directory path. |
| [`public IReadOnlyList<string> GetDirectories()`](#m-electron2d-diraccess-getdirectories) | Returns a sorted snapshot of visible directory names in the current directory. |
| [`public static IReadOnlyList<string> GetDirectoriesAt(string path)`](#m-electron2d-diraccess-getdirectoriesat-system-string) | Returns a sorted snapshot of directory names from an absolute or virtual directory path. |
| [`public static int GetDriveCount()`](#m-electron2d-diraccess-getdrivecount) | Gets the number of logical drives, mounted volumes, and platform filesystem shortcuts. |
| [`public static string GetDriveName(int index)`](#m-electron2d-diraccess-getdrivename-system-int32) | Gets the root name of one visible drive or mounted filesystem. |
| [`public static string GetDriveLabel(int index)`](#m-electron2d-diraccess-getdrivelabel-system-int32) | Gets the volume label of one visible drive or mounted filesystem. |
| [`public int GetCurrentDrive()`](#m-electron2d-diraccess-getcurrentdrive) | Gets the current directory's index in the current drive snapshot. |
| [`public void ChangeDir(string path)`](#m-electron2d-diraccess-changedir-system-string) | Changes the current directory within this instance's access scope. |
| [`public string GetCurrentDir(bool includeDrive = true)`](#m-electron2d-diraccess-getcurrentdir-system-boolean) | Gets the current directory in this instance's original access scope. |
| [`public void MakeDir(string path)`](#m-electron2d-diraccess-makedir-system-string) | Creates one directory whose parent already exists. |
| [`public static void MakeDirAbsolute(string path)`](#m-electron2d-diraccess-makedirabsolute-system-string) | Creates one directory at an absolute native or virtual path. |
| [`public void MakeDirRecursive(string path)`](#m-electron2d-diraccess-makedirrecursive-system-string) | Creates a directory and every missing parent in its path. |
| [`public static void MakeDirRecursiveAbsolute(string path)`](#m-electron2d-diraccess-makedirrecursiveabsolute-system-string) | Creates an absolute directory and every missing parent in its path. |
| [`public bool FileExists(string path)`](#m-electron2d-diraccess-fileexists-system-string) | Reports whether a file exists at a relative or in-scope absolute path. |
| [`public bool DirExists(string path)`](#m-electron2d-diraccess-direxists-system-string) | Reports whether a directory exists at a relative or in-scope absolute path. |
| [`public static bool DirExistsAbsolute(string path)`](#m-electron2d-diraccess-direxistsabsolute-system-string) | Reports whether an absolute native or virtual path identifies an existing directory. |
| [`public long GetSpaceLeft()`](#m-electron2d-diraccess-getspaceleft) | Gets the available bytes on the filesystem containing the current directory. |
| [`public void Copy(string source, string destination, UnixPermissionFlags? permissions = null)`](#m-electron2d-diraccess-copy-system-string-system-string-system-nullable-electron2d-unixpermissionflags) | Copies a file and optionally replaces its Unix permission bits. |
| [`public static void CopyAbsolute(string source, string destination, UnixPermissionFlags? permissions = null)`](#m-electron2d-diraccess-copyabsolute-system-string-system-string-system-nullable-electron2d-unixpermissionflags) | Copies a file between absolute native or virtual paths. |
| [`public void Rename(string source, string destination)`](#m-electron2d-diraccess-rename-system-string-system-string) | Renames or moves one file or directory within this instance's access scope. |
| [`public static void RenameAbsolute(string source, string destination)`](#m-electron2d-diraccess-renameabsolute-system-string-system-string) | Renames or moves one file or directory between absolute native or virtual paths. |
| [`public void Remove(string path)`](#m-electron2d-diraccess-remove-system-string) | Permanently removes a file, symbolic link, or empty directory. |
| [`public static void RemoveAbsolute(string path)`](#m-electron2d-diraccess-removeabsolute-system-string) | Permanently removes a file, symbolic link, or empty directory at an absolute path. |
| [`public bool IsLink(string path)`](#m-electron2d-diraccess-islink-system-string) | Reports whether a path identifies a symbolic link or platform reparse point. |
| [`public string ReadLink(string path)`](#m-electron2d-diraccess-readlink-system-string) | Reads the stored target of a symbolic link. |
| [`public void CreateLink(string source, string target)`](#m-electron2d-diraccess-createlink-system-string-system-string) | Creates a symbolic link to a file or directory path. |
| [`public bool IsBundle(string path)`](#m-electron2d-diraccess-isbundle-system-string) | Reports whether a directory is a macOS-style bundle. |
| [`public string GetFilesystemType()`](#m-electron2d-diraccess-getfilesystemtype) | Gets the filesystem type containing the current directory. |
| [`public bool IsCaseSensitive(string path)`](#m-electron2d-diraccess-iscasesensitive-system-string) | Reports whether the directory containing a path uses case-sensitive names. |
| [`public bool IsEquivalent(string pathA, string pathB)`](#m-electron2d-diraccess-isequivalent-system-string-system-string) | Reports whether two paths resolve to the same filesystem object. |
| [`protected override void Dispose(bool disposing)`](#m-electron2d-diraccess-dispose-system-boolean) | Releases resources owned by a derived class. |

## Property Descriptions

<a id="p-electron2d-diraccess-includehidden"></a>
### `public bool IncludeHidden { get; set; }`

Gets or sets whether hidden entries are returned by directory enumeration.

**Value:** `false` by default. Dot-prefixed names are hidden on Unix-family platforms; the hidden
filesystem attribute is honored on every platform.

**Exceptions**

- `ObjectDisposedException`: The instance is disposing or disposed.

**Remarks:** The value affects subsequent [`DirAccess.GetNext`](DirAccess.md#m-electron2d-diraccess-getnext) calls in an active listing and snapshot helpers.

<a id="p-electron2d-diraccess-includenavigational"></a>
### `public bool IncludeNavigational { get; set; }`

Gets or sets whether the navigational entries `.` and `..` are enumerated.

**Value:** `false` by default.

**Exceptions**

- `ObjectDisposedException`: The instance is disposing or disposed.

**Remarks:** The value affects subsequent [`DirAccess.GetNext`](DirAccess.md#m-electron2d-diraccess-getnext) calls in an active listing and [`DirAccess.GetDirectories`](DirAccess.md#m-electron2d-diraccess-getdirectories).

## Method Descriptions

<a id="m-electron2d-diraccess-open-system-string"></a>
### `public static DirAccess Open(string path)`

Opens an existing ordinary or directory-backed virtual directory.

**Parameters**

- `path`: A nonempty ordinary, `res://`, or `user://` path.

**Returns:** A new directory owner positioned at `path`.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.DirectoryNotFoundException`: The resolved directory does not exist.
- `IO.IOException`: The directory metadata cannot be read.
- `UnauthorizedAccessException`: A virtual path escapes its configured root or access is denied.
- `NotSupportedException`: `path` uses an unsupported virtual scheme.

<a id="m-electron2d-diraccess-createtemp-system-string-system-boolean"></a>
### `public static DirAccess CreateTemp(string prefix = "", bool keep = false)`

Creates and opens a uniquely named temporary directory.

**Parameters**

- `prefix`: An optional portable name prefix. A hyphen is appended before the random suffix.
- `keep`: Whether disposal preserves the directory and its contents.

**Returns:** An owner positioned at the new temporary directory.

**Exceptions**

- `ArgumentNullException`: `prefix` is `null`.
- `ArgumentException`: `prefix` is not a portable filename prefix.
- `IO.IOException`: The temporary directory cannot be created.
- `UnauthorizedAccessException`: The caller lacks access to the system temporary directory.

**Remarks:** Unless `keep` is true, disposal recursively deletes the complete owned directory.

<a id="m-electron2d-diraccess-listdirbegin"></a>
### `public void ListDirBegin()`

Starts a new unsorted directory listing and closes any previous listing.

**Exceptions**

- `IO.DirectoryNotFoundException`: The current directory no longer exists.
- `IO.IOException`: The directory cannot be enumerated.
- `UnauthorizedAccessException`: The caller lacks directory-listing access.
- `ObjectDisposedException`: The instance is disposing or disposed.

**Remarks:** The visible entry metadata is captured when this method returns. Entries can subsequently disappear or change;
use [`DirAccess.GetFiles`](DirAccess.md#m-electron2d-diraccess-getfiles) or [`DirAccess.GetDirectories`](DirAccess.md#m-electron2d-diraccess-getdirectories) for sorted snapshots.

<a id="m-electron2d-diraccess-getnext"></a>
### `public string GetNext()`

Returns the next visible name from the active directory listing.

**Returns:** The entry name without its parent path, or an empty string after the listing is exhausted or absent.

**Exceptions**

- `ObjectDisposedException`: The instance is disposing or disposed.

**Remarks:** Exhaustion closes the listing automatically. Enumeration order is filesystem-dependent.

<a id="m-electron2d-diraccess-currentisdir"></a>
### `public bool CurrentIsDir()`

Reports whether the entry returned by the last successful [`DirAccess.GetNext`](DirAccess.md#m-electron2d-diraccess-getnext) call is a directory.

**Returns:** `true` for a directory, including `.` and `..`; otherwise `false`.

**Exceptions**

- `ObjectDisposedException`: The instance is disposing or disposed.

**Remarks:** Returns `false` before the first entry and after listing exhaustion or closure.

<a id="m-electron2d-diraccess-listdirend"></a>
### `public void ListDirEnd()`

Closes the active directory listing.

**Exceptions**

- `ObjectDisposedException`: The instance is disposing or disposed.

**Remarks:** The method is idempotent.

<a id="m-electron2d-diraccess-getfiles"></a>
### `public IReadOnlyList<string> GetFiles()`

Returns a sorted snapshot of visible file names in the current directory.

**Returns:** An ordinally sorted read-only snapshot containing names without parent paths.

**Exceptions**

- `IO.DirectoryNotFoundException`: The current directory no longer exists.
- `IO.IOException`: The directory cannot be enumerated.
- `UnauthorizedAccessException`: The caller lacks directory-listing access.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-diraccess-getfilesat-system-string"></a>
### `public static IReadOnlyList<string> GetFilesAt(string path)`

Returns a sorted snapshot of file names from an absolute or virtual directory path.

**Parameters**

- `path`: A fully qualified native path, `res://` path, or `user://` path.

**Returns:** An ordinally sorted read-only snapshot containing names without parent paths.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty, invalid, or relative.
- `IO.DirectoryNotFoundException`: The resolved directory does not exist.
- `IO.IOException`: The directory cannot be enumerated.
- `UnauthorizedAccessException`: The caller lacks directory-listing access.
- `NotSupportedException`: `path` uses an unsupported virtual scheme.

<a id="m-electron2d-diraccess-getdirectories"></a>
### `public IReadOnlyList<string> GetDirectories()`

Returns a sorted snapshot of visible directory names in the current directory.

**Returns:** An ordinally sorted read-only snapshot containing names without parent paths.

**Exceptions**

- `IO.DirectoryNotFoundException`: The current directory no longer exists.
- `IO.IOException`: The directory cannot be enumerated.
- `UnauthorizedAccessException`: The caller lacks directory-listing access.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-diraccess-getdirectoriesat-system-string"></a>
### `public static IReadOnlyList<string> GetDirectoriesAt(string path)`

Returns a sorted snapshot of directory names from an absolute or virtual directory path.

**Parameters**

- `path`: A fully qualified native path, `res://` path, or `user://` path.

**Returns:** An ordinally sorted read-only snapshot containing names without parent paths.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty, invalid, or relative.
- `IO.DirectoryNotFoundException`: The resolved directory does not exist.
- `IO.IOException`: The directory cannot be enumerated.
- `UnauthorizedAccessException`: The caller lacks directory-listing access.
- `NotSupportedException`: `path` uses an unsupported virtual scheme.

<a id="m-electron2d-diraccess-getdrivecount"></a>
### `public static int GetDriveCount()`

Gets the number of logical drives, mounted volumes, and platform filesystem shortcuts.

**Returns:** The current platform snapshot length.

**Exceptions**

- `IO.IOException`: Drive enumeration fails.
- `UnauthorizedAccessException`: The runtime cannot enumerate drives.
- `PlatformNotSupportedException`: The target has no integrated storage-volume enumerator.

**Remarks:** Linux includes the home and desktop directories plus file bookmarks from the GTK 3 bookmark file.

<a id="m-electron2d-diraccess-getdrivename-system-int32"></a>
### `public static string GetDriveName(int index)`

Gets the root name of one visible drive or mounted filesystem.

**Parameters**

- `index`: A zero-based index in the current drive snapshot.

**Returns:** A Windows drive name such as `C:`, or an absolute mounted-volume or shortcut path.

**Exceptions**

- `ArgumentOutOfRangeException`: `index` is outside the current snapshot.
- `IO.IOException`: Drive enumeration fails.
- `UnauthorizedAccessException`: The runtime cannot enumerate drives.
- `PlatformNotSupportedException`: The target has no integrated storage-volume enumerator.

<a id="m-electron2d-diraccess-getdrivelabel-system-int32"></a>
### `public static string GetDriveLabel(int index)`

Gets the volume label of one visible drive or mounted filesystem.

**Parameters**

- `index`: A zero-based index in the current drive snapshot.

**Returns:** The Windows volume label or `<network>` for a remote drive; empty on other platforms.

**Exceptions**

- `ArgumentOutOfRangeException`: `index` is outside the current snapshot.
- `IO.IOException`: The label cannot be queried.
- `UnauthorizedAccessException`: The caller lacks access to the drive metadata.
- `PlatformNotSupportedException`: The target has no integrated storage-volume enumerator.

<a id="m-electron2d-diraccess-getcurrentdrive"></a>
### `public int GetCurrentDrive()`

Gets the current directory's index in the current drive snapshot.

**Returns:** The zero-based matching drive index, or `-1` if no drive root contains the current directory.

**Exceptions**

- `IO.IOException`: Drive enumeration fails.
- `UnauthorizedAccessException`: The runtime cannot enumerate drives.
- `PlatformNotSupportedException`: The target has no integrated storage-volume enumerator.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-diraccess-changedir-system-string"></a>
### `public void ChangeDir(string path)`

Changes the current directory within this instance's access scope.

**Parameters**

- `path`: A relative path or an absolute path in the same scope.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.DirectoryNotFoundException`: The resolved directory does not exist.
- `IO.IOException`: The directory metadata cannot be read.
- `InvalidOperationException`: `path` belongs to another access scope.
- `UnauthorizedAccessException`: The path escapes a virtual root or access is denied.
- `NotSupportedException`: `path` uses an unsupported virtual scheme.
- `ObjectDisposedException`: The instance is disposing or disposed.

**Remarks:** A successful change closes an active listing.

<a id="m-electron2d-diraccess-getcurrentdir-system-boolean"></a>
### `public string GetCurrentDir(bool includeDrive = true)`

Gets the current directory in this instance's original access scope.

**Parameters**

- `includeDrive`: Whether a native filesystem result includes its drive prefix.

**Returns:** A `res://` or `user://` path for virtual scopes, otherwise a normalized native path.

**Exceptions**

- `ObjectDisposedException`: The instance is disposing or disposed.

**Remarks:** `includeDrive` affects only native Windows-style roots.

<a id="m-electron2d-diraccess-makedir-system-string"></a>
### `public void MakeDir(string path)`

Creates one directory whose parent already exists.

**Parameters**

- `path`: A relative path or an absolute path in this instance's scope.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The entry already exists or the directory cannot be created.
- `IO.DirectoryNotFoundException`: The parent directory does not exist.
- `UnauthorizedAccessException`: The path escapes a virtual root or access is denied.
- `InvalidOperationException`: `path` belongs to another access scope.
- `NotSupportedException`: `path` uses an unsupported virtual scheme.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-diraccess-makedirabsolute-system-string"></a>
### `public static void MakeDirAbsolute(string path)`

Creates one directory at an absolute native or virtual path.

**Parameters**

- `path`: A fully qualified native path, `res://` path, or `user://` path.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty, invalid, or relative.
- `IO.IOException`: The entry already exists or the directory cannot be created.
- `IO.DirectoryNotFoundException`: The parent directory does not exist.
- `UnauthorizedAccessException`: The virtual path escapes its root or access is denied.
- `NotSupportedException`: `path` uses an unsupported virtual scheme.

<a id="m-electron2d-diraccess-makedirrecursive-system-string"></a>
### `public void MakeDirRecursive(string path)`

Creates a directory and every missing parent in its path.

**Parameters**

- `path`: A relative path or an absolute path in this instance's scope.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The directory tree cannot be created.
- `UnauthorizedAccessException`: The path escapes a virtual root or access is denied.
- `InvalidOperationException`: `path` belongs to another access scope.
- `NotSupportedException`: `path` uses an unsupported virtual scheme.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-diraccess-makedirrecursiveabsolute-system-string"></a>
### `public static void MakeDirRecursiveAbsolute(string path)`

Creates an absolute directory and every missing parent in its path.

**Parameters**

- `path`: A fully qualified native path, `res://` path, or `user://` path.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty, invalid, or relative.
- `IO.IOException`: The directory tree cannot be created.
- `UnauthorizedAccessException`: The virtual path escapes its root or access is denied.
- `NotSupportedException`: `path` uses an unsupported virtual scheme.

<a id="m-electron2d-diraccess-fileexists-system-string"></a>
### `public bool FileExists(string path)`

Reports whether a file exists at a relative or in-scope absolute path.

**Parameters**

- `path`: The file path to test.

**Returns:** `true` only when the resolved path identifies an existing file.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The entry metadata cannot be read.
- `UnauthorizedAccessException`: The path escapes a virtual root or access is denied.
- `InvalidOperationException`: `path` belongs to another access scope.
- `NotSupportedException`: `path` uses an unsupported virtual scheme.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-diraccess-direxists-system-string"></a>
### `public bool DirExists(string path)`

Reports whether a directory exists at a relative or in-scope absolute path.

**Parameters**

- `path`: The directory path to test.

**Returns:** `true` only when the resolved path identifies an existing directory.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The entry metadata cannot be read.
- `UnauthorizedAccessException`: The path escapes a virtual root or access is denied.
- `InvalidOperationException`: `path` belongs to another access scope.
- `NotSupportedException`: `path` uses an unsupported virtual scheme.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-diraccess-direxistsabsolute-system-string"></a>
### `public static bool DirExistsAbsolute(string path)`

Reports whether an absolute native or virtual path identifies an existing directory.

**Parameters**

- `path`: A fully qualified native path, `res://` path, or `user://` path.

**Returns:** `true` only when the resolved directory exists.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty, invalid, or relative.
- `IO.IOException`: The entry metadata cannot be read.
- `UnauthorizedAccessException`: The virtual path escapes its root or access is denied.
- `NotSupportedException`: `path` uses an unsupported virtual scheme.

<a id="m-electron2d-diraccess-getspaceleft"></a>
### `public long GetSpaceLeft()`

Gets the available bytes on the filesystem containing the current directory.

**Returns:** The space available to the current user.

**Exceptions**

- `IO.IOException`: The containing drive cannot report available space.
- `UnauthorizedAccessException`: The caller lacks access to drive metadata.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-diraccess-copy-system-string-system-string-system-nullable-electron2d-unixpermissionflags"></a>
### `public void Copy(string source, string destination, UnixPermissionFlags? permissions = null)`

Copies a file and optionally replaces its Unix permission bits.

**Parameters**

- `source`: A relative or in-scope absolute source file path.
- `destination`: A relative or in-scope absolute destination file path.
- `permissions`: Optional Unix mode bits applied after a successful copy.

**Exceptions**

- `ArgumentNullException`: A path is `null`.
- `ArgumentException`: A path is empty, invalid, or resolves to the same path as the other argument.
- `IO.FileNotFoundException`: The source file does not exist.
- `IO.DirectoryNotFoundException`: A destination parent does not exist.
- `IO.IOException`: The copy fails.
- `UnauthorizedAccessException`: A path escapes a virtual root or access is denied.
- `InvalidOperationException`: A path belongs to another access scope.
- `NotSupportedException`: A path uses an unsupported virtual scheme.
- `PlatformNotSupportedException`: `permissions` is supplied on a platform without Unix modes.
- `ArgumentOutOfRangeException`: `permissions` contains unknown mode bits.
- `ObjectDisposedException`: The instance is disposing or disposed.

**Remarks:** The destination file is overwritten. Its parent directory is never created implicitly.

<a id="m-electron2d-diraccess-copyabsolute-system-string-system-string-system-nullable-electron2d-unixpermissionflags"></a>
### `public static void CopyAbsolute(string source, string destination, UnixPermissionFlags? permissions = null)`

Copies a file between absolute native or virtual paths.

**Parameters**

- `source`: A fully qualified native path, `res://` path, or `user://` path.
- `destination`: A fully qualified native path, `res://` path, or `user://` path.
- `permissions`: Optional Unix mode bits applied after a successful copy.

**Exceptions**

- `ArgumentNullException`: A path is `null`.
- `ArgumentException`: A path is empty, invalid, relative, or resolves to the same path as the other argument.
- `IO.FileNotFoundException`: The source file does not exist.
- `IO.DirectoryNotFoundException`: A destination parent does not exist.
- `IO.IOException`: The copy fails.
- `UnauthorizedAccessException`: A virtual path escapes its root or access is denied.
- `NotSupportedException`: A path uses an unsupported virtual scheme.
- `PlatformNotSupportedException`: `permissions` is supplied on a platform without Unix modes.
- `ArgumentOutOfRangeException`: `permissions` contains unknown mode bits.

**Remarks:** The two paths may belong to different virtual scopes. The destination parent is not created.

<a id="m-electron2d-diraccess-rename-system-string-system-string"></a>
### `public void Rename(string source, string destination)`

Renames or moves one file or directory within this instance's access scope.

**Parameters**

- `source`: A relative or in-scope absolute source path.
- `destination`: A relative or in-scope absolute destination path.

**Exceptions**

- `ArgumentNullException`: A path is `null`.
- `ArgumentException`: A path is empty, invalid, or resolves to the same path as the other argument.
- `IO.FileNotFoundException`: The source entry does not exist.
- `IO.IOException`: The move fails, crosses filesystems, or would replace a nonempty directory.
- `UnauthorizedAccessException`: A path escapes a virtual root or access is denied.
- `InvalidOperationException`: A path belongs to another access scope.
- `NotSupportedException`: A path uses an unsupported virtual scheme.
- `ObjectDisposedException`: The instance is disposing or disposed.

**Remarks:** Files replace existing files. Directories may replace only an existing empty directory.

<a id="m-electron2d-diraccess-renameabsolute-system-string-system-string"></a>
### `public static void RenameAbsolute(string source, string destination)`

Renames or moves one file or directory between absolute native or virtual paths.

**Parameters**

- `source`: A fully qualified native path, `res://` path, or `user://` path.
- `destination`: A fully qualified native path, `res://` path, or `user://` path.

**Exceptions**

- `ArgumentNullException`: A path is `null`.
- `ArgumentException`: A path is empty, invalid, relative, or resolves to the same path as the other argument.
- `IO.FileNotFoundException`: The source entry does not exist.
- `IO.IOException`: The move fails, crosses filesystems, or would replace a nonempty directory.
- `UnauthorizedAccessException`: A virtual path escapes its root or access is denied.
- `NotSupportedException`: A path uses an unsupported virtual scheme.

**Remarks:** Files replace existing files. Directories may replace only an existing empty directory.

<a id="m-electron2d-diraccess-remove-system-string"></a>
### `public void Remove(string path)`

Permanently removes a file, symbolic link, or empty directory.

**Parameters**

- `path`: A relative or in-scope absolute path.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.FileNotFoundException`: The entry does not exist.
- `IO.IOException`: A directory is nonempty or removal otherwise fails.
- `UnauthorizedAccessException`: The path escapes a virtual root or access is denied.
- `InvalidOperationException`: `path` belongs to another access scope.
- `NotSupportedException`: `path` uses an unsupported virtual scheme.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-diraccess-removeabsolute-system-string"></a>
### `public static void RemoveAbsolute(string path)`

Permanently removes a file, symbolic link, or empty directory at an absolute path.

**Parameters**

- `path`: A fully qualified native path, `res://` path, or `user://` path.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty, invalid, or relative.
- `IO.FileNotFoundException`: The entry does not exist.
- `IO.IOException`: A directory is nonempty or removal otherwise fails.
- `UnauthorizedAccessException`: The virtual path escapes its root or access is denied.
- `NotSupportedException`: `path` uses an unsupported virtual scheme.

<a id="m-electron2d-diraccess-islink-system-string"></a>
### `public bool IsLink(string path)`

Reports whether a path identifies a symbolic link or platform reparse point.

**Parameters**

- `path`: A relative or in-scope absolute path.

**Returns:** `true` for a Windows reparse point or a symbolic link on other supported platforms.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `UnauthorizedAccessException`: The path escapes a virtual root or access is denied.
- `InvalidOperationException`: `path` belongs to another access scope.
- `NotSupportedException`: `path` uses an unsupported virtual scheme.
- `PlatformNotSupportedException`: The platform has no verified symbolic-link backend.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-diraccess-readlink-system-string"></a>
### `public string ReadLink(string path)`

Reads the stored target of a symbolic link.

**Parameters**

- `path`: A relative or in-scope absolute link path.

**Returns:** The target stored in the link, which can be relative.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.FileNotFoundException`: The entry does not exist or is not a symbolic link.
- `IO.IOException`: The link cannot be read.
- `UnauthorizedAccessException`: The path escapes a virtual root or access is denied.
- `InvalidOperationException`: `path` belongs to another access scope.
- `NotSupportedException`: `path` uses an unsupported virtual scheme.
- `PlatformNotSupportedException`: The platform has no verified symbolic-link backend.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-diraccess-createlink-system-string-system-string"></a>
### `public void CreateLink(string source, string target)`

Creates a symbolic link to a file or directory path.

**Parameters**

- `source`: A relative or in-scope absolute path stored as the link target.
- `target`: A relative or in-scope absolute path for the new link.

**Exceptions**

- `ArgumentNullException`: A path is `null`.
- `ArgumentException`: A path is empty or invalid.
- `IO.IOException`: The target exists or link creation fails.
- `UnauthorizedAccessException`: A path escapes a virtual root or access is denied.
- `InvalidOperationException`: A path belongs to another access scope.
- `NotSupportedException`: A path uses an unsupported virtual scheme.
- `PlatformNotSupportedException`: The platform has no verified symbolic-link backend.
- `ObjectDisposedException`: The instance is disposing or disposed.

**Remarks:** Relative source paths remain relative and may be dangling. Virtual and absolute source paths are stored as resolved
physical paths. Windows may require Developer Mode or elevation; a missing source is created as a file link.

<a id="m-electron2d-diraccess-isbundle-system-string"></a>
### `public bool IsBundle(string path)`

Reports whether a directory is a macOS-style bundle.

**Parameters**

- `path`: A relative or in-scope absolute directory path.

**Returns:** `true` when the native macOS workspace classifies the existing directory as a file package.

**Exceptions**

- `PlatformNotSupportedException`: The current platform is not macOS.
- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.IOException`: The directory metadata cannot be read.
- `UnauthorizedAccessException`: The path escapes a virtual root or access is denied.
- `InvalidOperationException`: `path` belongs to another access scope.
- `NotSupportedException`: `path` uses an unsupported virtual scheme.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-diraccess-getfilesystemtype"></a>
### `public string GetFilesystemType()`

Gets the filesystem type containing the current directory.

**Returns:** An uppercase filesystem identifier when supplied by the operating system, or `Network Share` for a Windows UNC path.

**Exceptions**

- `IO.IOException`: The filesystem metadata cannot be queried.
- `UnauthorizedAccessException`: The caller lacks access to drive metadata.
- `ObjectDisposedException`: The instance is disposing or disposed.

<a id="m-electron2d-diraccess-iscasesensitive-system-string"></a>
### `public bool IsCaseSensitive(string path)`

Reports whether the directory containing a path uses case-sensitive names.

**Parameters**

- `path`: A relative or in-scope absolute path.

**Returns:** `true` when two names differing only by case identify distinct entries.

**Exceptions**

- `ArgumentNullException`: `path` is `null`.
- `ArgumentException`: `path` is empty or invalid.
- `IO.FileNotFoundException`: The path does not identify an existing file or directory.
- `IO.IOException`: The entry metadata cannot be read.
- `UnauthorizedAccessException`: The path escapes a virtual root or access is denied.
- `InvalidOperationException`: `path` belongs to another access scope.
- `NotSupportedException`: `path` uses an unsupported virtual scheme.
- `ObjectDisposedException`: The instance is disposing or disposed.

**Remarks:** The query reads platform filesystem metadata and never creates an entry. Linux reads the directory case-fold flag,
macOS reads the volume case-sensitivity setting, and Windows reads the per-entry case-sensitivity flag.

<a id="m-electron2d-diraccess-isequivalent-system-string-system-string"></a>
### `public bool IsEquivalent(string pathA, string pathB)`

Reports whether two paths resolve to the same filesystem object.

**Parameters**

- `pathA`: The first relative or in-scope absolute path.
- `pathB`: The second relative or in-scope absolute path.

**Returns:** `true` when both paths have the same volume/device and file identity.

**Exceptions**

- `ArgumentNullException`: A path is `null`.
- `ArgumentException`: A path is empty or invalid.
- `IO.IOException`: A symbolic-link chain cannot be resolved.
- `UnauthorizedAccessException`: A path escapes a virtual root or access is denied.
- `InvalidOperationException`: A path belongs to another access scope.
- `NotSupportedException`: A path uses an unsupported virtual scheme.
- `PlatformNotSupportedException`: Native filesystem identity is unavailable for this runtime.
- `ObjectDisposedException`: The instance is disposing or disposed.

**Remarks:** Symbolic links are followed. Distinct hard-link names are therefore equivalent.

<a id="m-electron2d-diraccess-dispose-system-boolean"></a>
### `protected override void Dispose(bool disposing)`

Releases resources owned by a derived class.

**Parameters**

- `disposing`: `true` when called from [`ElectronObject.Dispose`](ElectronObject.md#m-electron2d-electronobject-dispose).

**Remarks:** Overrides release managed resources when `disposing` is true and then call the base implementation.

## Inherited API

Public and protected members inherited from [ElectronObject](ElectronObject.md). Their lifecycle and error contracts remain applicable unless this page states an override.

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

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` covers null/empty/missing/relative/unknown paths, ordinary and virtual scopes, lexical traversal, current-directory changes, hidden/navigational filters, unspecified streaming versus sorted snapshots, concurrent listing consumption, single/recursive creation, copy overwrite, pre-mutation permission validation and self-copy rejection, instance/static file and directory rename with failed-move destination restoration, nonrecursive removal, relative and dangling symlink targets, hard-link and symlink equivalence on Linux, native case metadata, drive/capacity/filesystem queries, absolute helpers, temporary keep/delete ownership, current-directory changes before temporary disposal, and disposed access.

The executable evidence is Linux-only. Windows/macOS link, file-identity, drive, filesystem-type, rename, case, permission, and path-root behavior are compiled but not native-host verified. Android/iOS identity code is compiled but not device-verified; their link and drive-enumeration APIs intentionally throw until platform host/storage integration is implemented. Web browser directory/storage behavior is unimplemented and unverified; its capability boundary belongs to the first Web host slice. Packed/exported resources remain absent.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0003: ElectronObject lifetime](../decisions/core-object-runtime.md#adr-0003)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0019: Typed project settings and directory-backed virtual paths](../decisions/core-data-io.md#adr-0019)
- [0020: Typed file access and transformed-file containers](../decisions/core-data-io.md#adr-0020)
- [0021: Runtime and editor target platforms](../decisions/product.md#adr-0021)
- [0022: Typed directory access](../decisions/core-data-io.md#adr-0022)
