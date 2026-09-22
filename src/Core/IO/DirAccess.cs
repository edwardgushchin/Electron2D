using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Electron2D;

/// <summary>Provides blocking access to directories and their contents.</summary>
/// <remarks>
/// Instances retain one current directory and one access scope. Relative paths resolve from that directory;
/// <c>res://</c> and <c>user://</c> instances cannot leave their configured root or switch scopes. Operations are
/// serialized per instance, may block, and are unsuitable for real-time callbacks.
/// </remarks>
public sealed class DirAccess : ElectronObject
{
    private readonly object _gate = new();
    private readonly AccessScope _scope;
    private readonly string? _scopeRoot;
    private string _currentDirectory;
    private DirectoryEntry[]? _listing;
    private int _listingIndex;
    private bool _currentIsDirectory;
    private bool _hasCurrentEntry;
    private bool _includeHidden;
    private bool _includeNavigational;
    private string? _temporaryPath;
    private bool _keepTemporary;

    private DirAccess(string currentDirectory, AccessScope scope, string? scopeRoot)
    {
        _currentDirectory = currentDirectory;
        _scope = scope;
        _scopeRoot = scopeRoot;
    }

    /// <summary>Gets or sets whether hidden entries are returned by directory enumeration.</summary>
    /// <value>
    /// <see langword="false"/> by default. Dot-prefixed names are hidden on Unix-family platforms; the hidden
    /// filesystem attribute is honored on every platform.
    /// </value>
    /// <remarks>The value affects subsequent <see cref="GetNext"/> calls in an active listing and snapshot helpers.</remarks>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public bool IncludeHidden
    {
        get
        {
            ThrowIfDisposed();
            lock (_gate)
            {
                ThrowIfDisposed();
                return _includeHidden;
            }
        }
        set
        {
            ThrowIfDisposed();
            lock (_gate)
            {
                ThrowIfDisposed();
                _includeHidden = value;
            }
        }
    }

    /// <summary>Gets or sets whether the navigational entries <c>.</c> and <c>..</c> are enumerated.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <remarks>The value affects subsequent <see cref="GetNext"/> calls in an active listing and <see cref="GetDirectories"/>.</remarks>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public bool IncludeNavigational
    {
        get
        {
            ThrowIfDisposed();
            lock (_gate)
            {
                ThrowIfDisposed();
                return _includeNavigational;
            }
        }
        set
        {
            ThrowIfDisposed();
            lock (_gate)
            {
                ThrowIfDisposed();
                _includeNavigational = value;
            }
        }
    }

    /// <summary>Opens an existing ordinary or directory-backed virtual directory.</summary>
    /// <param name="path">A nonempty ordinary, <c>res://</c>, or <c>user://</c> path.</param>
    /// <returns>A new directory owner positioned at <paramref name="path"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="DirectoryNotFoundException">The resolved directory does not exist.</exception>
    /// <exception cref="IOException">The directory metadata cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">A virtual path escapes its configured root or access is denied.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unsupported virtual scheme.</exception>
    public static DirAccess Open(string path)
    {
        var roots = ProjectSettings.Instance.GetPathRootsSnapshot();
        var (absolutePath, scope, root) = ResolveOpenPath(path, roots);
        RequireExistingDirectory(absolutePath);
        return new DirAccess(absolutePath, scope, root);
    }

    /// <summary>Creates and opens a uniquely named temporary directory.</summary>
    /// <param name="prefix">An optional portable name prefix. A hyphen is appended before the random suffix.</param>
    /// <param name="keep">Whether disposal preserves the directory and its contents.</param>
    /// <returns>An owner positioned at the new temporary directory.</returns>
    /// <remarks>Unless <paramref name="keep"/> is true, disposal recursively deletes the complete owned directory.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="prefix"/> is not a portable filename prefix.</exception>
    /// <exception cref="IOException">The temporary directory cannot be created.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks access to the system temporary directory.</exception>
    public static DirAccess CreateTemp(string prefix = "", bool keep = false)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ValidateTemporaryPrefix(prefix);
        var directory = Directory.CreateTempSubdirectory(prefix.Length == 0 ? null : prefix + "-");
        var result = new DirAccess(directory.FullName, AccessScope.FileSystem, scopeRoot: null)
        {
            _temporaryPath = directory.FullName,
            _keepTemporary = keep
        };
        return result;
    }

    /// <summary>Starts a new unsorted directory listing and closes any previous listing.</summary>
    /// <remarks>
    /// The visible entry metadata is captured when this method returns. Entries can subsequently disappear or change;
    /// use <see cref="GetFiles"/> or <see cref="GetDirectories"/> for sorted snapshots.
    /// </remarks>
    /// <exception cref="DirectoryNotFoundException">The current directory no longer exists.</exception>
    /// <exception cref="IOException">The directory cannot be enumerated.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks directory-listing access.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void ListDirBegin()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            _listing = CaptureEntries(_currentDirectory, includeNavigationalCandidates: true);
            _listingIndex = 0;
            ClearCurrentEntryLocked();
        }
    }

    /// <summary>Returns the next visible name from the active directory listing.</summary>
    /// <returns>The entry name without its parent path, or an empty string after the listing is exhausted or absent.</returns>
    /// <remarks>Exhaustion closes the listing automatically. Enumeration order is filesystem-dependent.</remarks>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public string GetNext()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            while (_listing is not null && _listingIndex < _listing.Length)
            {
                var entry = _listing[_listingIndex++];
                if (!_includeNavigational && entry.IsNavigational || !_includeHidden && entry.IsHidden)
                    continue;

                _currentIsDirectory = entry.IsDirectory;
                _hasCurrentEntry = true;
                return entry.Name;
            }

            EndListingLocked();
            return string.Empty;
        }
    }

    /// <summary>Reports whether the entry returned by the last successful <see cref="GetNext"/> call is a directory.</summary>
    /// <returns><see langword="true"/> for a directory, including <c>.</c> and <c>..</c>; otherwise <see langword="false"/>.</returns>
    /// <remarks>Returns <see langword="false"/> before the first entry and after listing exhaustion or closure.</remarks>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public bool CurrentIsDir()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            return _hasCurrentEntry && _currentIsDirectory;
        }
    }

    /// <summary>Closes the active directory listing.</summary>
    /// <remarks>The method is idempotent.</remarks>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void ListDirEnd()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            EndListingLocked();
        }
    }

    /// <summary>Returns a sorted snapshot of visible file names in the current directory.</summary>
    /// <returns>An ordinally sorted read-only snapshot containing names without parent paths.</returns>
    /// <exception cref="DirectoryNotFoundException">The current directory no longer exists.</exception>
    /// <exception cref="IOException">The directory cannot be enumerated.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks directory-listing access.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public IReadOnlyList<string> GetFiles() => GetContents(directories: false);

    /// <summary>Returns a sorted snapshot of file names from an absolute or virtual directory path.</summary>
    /// <param name="path">A fully qualified native path, <c>res://</c> path, or <c>user://</c> path.</param>
    /// <returns>An ordinally sorted read-only snapshot containing names without parent paths.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty, invalid, or relative.</exception>
    /// <exception cref="DirectoryNotFoundException">The resolved directory does not exist.</exception>
    /// <exception cref="IOException">The directory cannot be enumerated.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks directory-listing access.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unsupported virtual scheme.</exception>
    public static IReadOnlyList<string> GetFilesAt(string path)
    {
        using var directory = OpenAbsolute(path);
        return directory.GetFiles();
    }

    /// <summary>Returns a sorted snapshot of visible directory names in the current directory.</summary>
    /// <returns>An ordinally sorted read-only snapshot containing names without parent paths.</returns>
    /// <exception cref="DirectoryNotFoundException">The current directory no longer exists.</exception>
    /// <exception cref="IOException">The directory cannot be enumerated.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks directory-listing access.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public IReadOnlyList<string> GetDirectories() => GetContents(directories: true);

    /// <summary>Returns a sorted snapshot of directory names from an absolute or virtual directory path.</summary>
    /// <param name="path">A fully qualified native path, <c>res://</c> path, or <c>user://</c> path.</param>
    /// <returns>An ordinally sorted read-only snapshot containing names without parent paths.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty, invalid, or relative.</exception>
    /// <exception cref="DirectoryNotFoundException">The resolved directory does not exist.</exception>
    /// <exception cref="IOException">The directory cannot be enumerated.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks directory-listing access.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unsupported virtual scheme.</exception>
    public static IReadOnlyList<string> GetDirectoriesAt(string path)
    {
        using var directory = OpenAbsolute(path);
        return directory.GetDirectories();
    }

    /// <summary>Gets the number of logical drives, mounted volumes, and platform filesystem shortcuts.</summary>
    /// <returns>The current platform snapshot length.</returns>
    /// <remarks>Linux includes the home and desktop directories plus file bookmarks from the GTK 3 bookmark file.</remarks>
    /// <exception cref="IOException">Drive enumeration fails.</exception>
    /// <exception cref="UnauthorizedAccessException">The runtime cannot enumerate drives.</exception>
    /// <exception cref="PlatformNotSupportedException">The target has no integrated storage-volume enumerator.</exception>
    public static int GetDriveCount() => GetDriveEntries().Length;

    /// <summary>Gets the root name of one visible drive or mounted filesystem.</summary>
    /// <param name="index">A zero-based index in the current drive snapshot.</param>
    /// <returns>A Windows drive name such as <c>C:</c>, or an absolute mounted-volume or shortcut path.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the current snapshot.</exception>
    /// <exception cref="IOException">Drive enumeration fails.</exception>
    /// <exception cref="UnauthorizedAccessException">The runtime cannot enumerate drives.</exception>
    /// <exception cref="PlatformNotSupportedException">The target has no integrated storage-volume enumerator.</exception>
    public static string GetDriveName(int index) => GetDrive(index).Name;

    /// <summary>Gets the volume label of one visible drive or mounted filesystem.</summary>
    /// <param name="index">A zero-based index in the current drive snapshot.</param>
    /// <returns>The Windows volume label or <c>&lt;network&gt;</c> for a remote drive; empty on other platforms.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the current snapshot.</exception>
    /// <exception cref="IOException">The label cannot be queried.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks access to the drive metadata.</exception>
    /// <exception cref="PlatformNotSupportedException">The target has no integrated storage-volume enumerator.</exception>
    public static string GetDriveLabel(int index) => GetDrive(index).Label;

    /// <summary>Gets the current directory's index in the current drive snapshot.</summary>
    /// <returns>The zero-based matching drive index, or <c>-1</c> if no drive root contains the current directory.</returns>
    /// <exception cref="IOException">Drive enumeration fails.</exception>
    /// <exception cref="UnauthorizedAccessException">The runtime cannot enumerate drives.</exception>
    /// <exception cref="PlatformNotSupportedException">The target has no integrated storage-volume enumerator.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public int GetCurrentDrive()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            var drives = GetDriveEntries();
            var currentPath = OperatingSystem.IsWindows()
                ? NormalizeWindowsExtendedPath(_currentDirectory)
                : _currentDirectory;
            var bestIndex = -1;
            var bestRootLength = -1;
            for (var index = 0; index < drives.Length; index++)
            {
                var root = drives[index].Root;
                if (IsWithin(root, currentPath) && root.Length > bestRootLength)
                {
                    bestIndex = index;
                    bestRootLength = root.Length;
                }
            }
            return bestIndex;
        }
    }

    /// <summary>Changes the current directory within this instance's access scope.</summary>
    /// <param name="path">A relative path or an absolute path in the same scope.</param>
    /// <remarks>A successful change closes an active listing.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="DirectoryNotFoundException">The resolved directory does not exist.</exception>
    /// <exception cref="IOException">The directory metadata cannot be read.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="path"/> belongs to another access scope.</exception>
    /// <exception cref="UnauthorizedAccessException">The path escapes a virtual root or access is denied.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unsupported virtual scheme.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void ChangeDir(string path)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            var resolved = ResolveLocked(path);
            RequireExistingDirectory(resolved);
            _currentDirectory = resolved;
            EndListingLocked();
        }
    }

    /// <summary>Gets the current directory in this instance's original access scope.</summary>
    /// <param name="includeDrive">Whether a native filesystem result includes its drive prefix.</param>
    /// <returns>A <c>res://</c> or <c>user://</c> path for virtual scopes, otherwise a normalized native path.</returns>
    /// <remarks><paramref name="includeDrive"/> affects only native Windows-style roots.</remarks>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public string GetCurrentDir(bool includeDrive = true)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_scope is AccessScope.Resources or AccessScope.UserData)
            {
                var prefix = _scope == AccessScope.Resources ? "res://" : "user://";
                var relative = Path.GetRelativePath(_scopeRoot!, _currentDirectory);
                return relative == "." ? prefix : prefix + relative.Replace('\\', '/');
            }

            if (includeDrive)
                return _currentDirectory;
            var root = Path.GetPathRoot(_currentDirectory);
            return string.IsNullOrEmpty(root) || root == Path.DirectorySeparatorChar.ToString()
                ? _currentDirectory
                : Path.DirectorySeparatorChar + _currentDirectory[root.Length..];
        }
    }

    /// <summary>Creates one directory whose parent already exists.</summary>
    /// <param name="path">A relative path or an absolute path in this instance's scope.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The entry already exists or the directory cannot be created.</exception>
    /// <exception cref="DirectoryNotFoundException">The parent directory does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">The path escapes a virtual root or access is denied.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="path"/> belongs to another access scope.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unsupported virtual scheme.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void MakeDir(string path)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            MakeSingleDirectory(ResolveLocked(path));
        }
    }

    /// <summary>Creates one directory at an absolute native or virtual path.</summary>
    /// <param name="path">A fully qualified native path, <c>res://</c> path, or <c>user://</c> path.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty, invalid, or relative.</exception>
    /// <exception cref="IOException">The entry already exists or the directory cannot be created.</exception>
    /// <exception cref="DirectoryNotFoundException">The parent directory does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">The virtual path escapes its root or access is denied.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unsupported virtual scheme.</exception>
    public static void MakeDirAbsolute(string path) => MakeSingleDirectory(ResolveStaticAbsolute(path));

    /// <summary>Creates a directory and every missing parent in its path.</summary>
    /// <param name="path">A relative path or an absolute path in this instance's scope.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The directory tree cannot be created.</exception>
    /// <exception cref="UnauthorizedAccessException">The path escapes a virtual root or access is denied.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="path"/> belongs to another access scope.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unsupported virtual scheme.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void MakeDirRecursive(string path)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            Directory.CreateDirectory(ResolveLocked(path));
        }
    }

    /// <summary>Creates an absolute directory and every missing parent in its path.</summary>
    /// <param name="path">A fully qualified native path, <c>res://</c> path, or <c>user://</c> path.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty, invalid, or relative.</exception>
    /// <exception cref="IOException">The directory tree cannot be created.</exception>
    /// <exception cref="UnauthorizedAccessException">The virtual path escapes its root or access is denied.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unsupported virtual scheme.</exception>
    public static void MakeDirRecursiveAbsolute(string path) => Directory.CreateDirectory(ResolveStaticAbsolute(path));

    /// <summary>Reports whether a file exists at a relative or in-scope absolute path.</summary>
    /// <param name="path">The file path to test.</param>
    /// <returns><see langword="true"/> only when the resolved path identifies an existing file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The entry metadata cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The path escapes a virtual root or access is denied.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="path"/> belongs to another access scope.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unsupported virtual scheme.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public bool FileExists(string path)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            return IsExistingFile(ResolveLocked(path));
        }
    }

    /// <summary>Reports whether a directory exists at a relative or in-scope absolute path.</summary>
    /// <param name="path">The directory path to test.</param>
    /// <returns><see langword="true"/> only when the resolved path identifies an existing directory.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The entry metadata cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The path escapes a virtual root or access is denied.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="path"/> belongs to another access scope.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unsupported virtual scheme.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public bool DirExists(string path)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            return IsExistingDirectory(ResolveLocked(path));
        }
    }

    /// <summary>Reports whether an absolute native or virtual path identifies an existing directory.</summary>
    /// <param name="path">A fully qualified native path, <c>res://</c> path, or <c>user://</c> path.</param>
    /// <returns><see langword="true"/> only when the resolved directory exists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty, invalid, or relative.</exception>
    /// <exception cref="IOException">The entry metadata cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The virtual path escapes its root or access is denied.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unsupported virtual scheme.</exception>
    public static bool DirExistsAbsolute(string path) => IsExistingDirectory(ResolveStaticAbsolute(path));

    /// <summary>Gets the available bytes on the filesystem containing the current directory.</summary>
    /// <returns>The space available to the current user.</returns>
    /// <exception cref="IOException">The containing drive cannot report available space.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks access to drive metadata.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public long GetSpaceLeft()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            return OperatingSystem.IsWindows()
                ? GetWindowsAvailableSpace(_currentDirectory)
                : GetDriveForPath(_currentDirectory).AvailableFreeSpace;
        }
    }

    /// <summary>Copies a file and optionally replaces its Unix permission bits.</summary>
    /// <param name="source">A relative or in-scope absolute source file path.</param>
    /// <param name="destination">A relative or in-scope absolute destination file path.</param>
    /// <param name="permissions">Optional Unix mode bits applied after a successful copy.</param>
    /// <remarks>The destination file is overwritten. Its parent directory is never created implicitly.</remarks>
    /// <exception cref="ArgumentNullException">A path is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A path is empty, invalid, or resolves to the same path as the other argument.</exception>
    /// <exception cref="FileNotFoundException">The source file does not exist.</exception>
    /// <exception cref="DirectoryNotFoundException">A destination parent does not exist.</exception>
    /// <exception cref="IOException">The copy fails.</exception>
    /// <exception cref="UnauthorizedAccessException">A path escapes a virtual root or access is denied.</exception>
    /// <exception cref="InvalidOperationException">A path belongs to another access scope.</exception>
    /// <exception cref="NotSupportedException">A path uses an unsupported virtual scheme.</exception>
    /// <exception cref="PlatformNotSupportedException"><paramref name="permissions"/> is supplied on a platform without Unix modes.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="permissions"/> contains unknown mode bits.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void Copy(string source, string destination, UnixPermissionFlags? permissions = null)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            CopyPhysical(ResolveLocked(source), ResolveLocked(destination), permissions);
        }
    }

    /// <summary>Copies a file between absolute native or virtual paths.</summary>
    /// <param name="source">A fully qualified native path, <c>res://</c> path, or <c>user://</c> path.</param>
    /// <param name="destination">A fully qualified native path, <c>res://</c> path, or <c>user://</c> path.</param>
    /// <param name="permissions">Optional Unix mode bits applied after a successful copy.</param>
    /// <remarks>The two paths may belong to different virtual scopes. The destination parent is not created.</remarks>
    /// <exception cref="ArgumentNullException">A path is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A path is empty, invalid, relative, or resolves to the same path as the other argument.</exception>
    /// <exception cref="FileNotFoundException">The source file does not exist.</exception>
    /// <exception cref="DirectoryNotFoundException">A destination parent does not exist.</exception>
    /// <exception cref="IOException">The copy fails.</exception>
    /// <exception cref="UnauthorizedAccessException">A virtual path escapes its root or access is denied.</exception>
    /// <exception cref="NotSupportedException">A path uses an unsupported virtual scheme.</exception>
    /// <exception cref="PlatformNotSupportedException"><paramref name="permissions"/> is supplied on a platform without Unix modes.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="permissions"/> contains unknown mode bits.</exception>
    public static void CopyAbsolute(string source, string destination, UnixPermissionFlags? permissions = null)
    {
        ValidateAbsolutePath(source);
        ValidateAbsolutePath(destination);
        var roots = ProjectSettings.Instance.GetPathRootsSnapshot();
        CopyPhysical(
            ResolveStaticAbsolute(source, roots),
            ResolveStaticAbsolute(destination, roots),
            permissions);
    }

    /// <summary>Renames or moves one file or directory within this instance's access scope.</summary>
    /// <param name="source">A relative or in-scope absolute source path.</param>
    /// <param name="destination">A relative or in-scope absolute destination path.</param>
    /// <remarks>Files replace existing files. Directories may replace only an existing empty directory.</remarks>
    /// <exception cref="ArgumentNullException">A path is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A path is empty, invalid, or resolves to the same path as the other argument.</exception>
    /// <exception cref="FileNotFoundException">The source entry does not exist.</exception>
    /// <exception cref="IOException">The move fails, crosses filesystems, or would replace a nonempty directory.</exception>
    /// <exception cref="UnauthorizedAccessException">A path escapes a virtual root or access is denied.</exception>
    /// <exception cref="InvalidOperationException">A path belongs to another access scope.</exception>
    /// <exception cref="NotSupportedException">A path uses an unsupported virtual scheme.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void Rename(string source, string destination)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            MovePhysical(ResolveLocked(source), ResolveLocked(destination));
        }
    }

    /// <summary>Renames or moves one file or directory between absolute native or virtual paths.</summary>
    /// <param name="source">A fully qualified native path, <c>res://</c> path, or <c>user://</c> path.</param>
    /// <param name="destination">A fully qualified native path, <c>res://</c> path, or <c>user://</c> path.</param>
    /// <remarks>Files replace existing files. Directories may replace only an existing empty directory.</remarks>
    /// <exception cref="ArgumentNullException">A path is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A path is empty, invalid, relative, or resolves to the same path as the other argument.</exception>
    /// <exception cref="FileNotFoundException">The source entry does not exist.</exception>
    /// <exception cref="IOException">The move fails, crosses filesystems, or would replace a nonempty directory.</exception>
    /// <exception cref="UnauthorizedAccessException">A virtual path escapes its root or access is denied.</exception>
    /// <exception cref="NotSupportedException">A path uses an unsupported virtual scheme.</exception>
    public static void RenameAbsolute(string source, string destination)
    {
        ValidateAbsolutePath(source);
        ValidateAbsolutePath(destination);
        var roots = ProjectSettings.Instance.GetPathRootsSnapshot();
        MovePhysical(ResolveStaticAbsolute(source, roots), ResolveStaticAbsolute(destination, roots));
    }

    /// <summary>Permanently removes a file, symbolic link, or empty directory.</summary>
    /// <param name="path">A relative or in-scope absolute path.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="FileNotFoundException">The entry does not exist.</exception>
    /// <exception cref="IOException">A directory is nonempty or removal otherwise fails.</exception>
    /// <exception cref="UnauthorizedAccessException">The path escapes a virtual root or access is denied.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="path"/> belongs to another access scope.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unsupported virtual scheme.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void Remove(string path)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            RemovePhysical(ResolveLocked(path));
        }
    }

    /// <summary>Permanently removes a file, symbolic link, or empty directory at an absolute path.</summary>
    /// <param name="path">A fully qualified native path, <c>res://</c> path, or <c>user://</c> path.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty, invalid, or relative.</exception>
    /// <exception cref="FileNotFoundException">The entry does not exist.</exception>
    /// <exception cref="IOException">A directory is nonempty or removal otherwise fails.</exception>
    /// <exception cref="UnauthorizedAccessException">The virtual path escapes its root or access is denied.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unsupported virtual scheme.</exception>
    public static void RemoveAbsolute(string path) => RemovePhysical(ResolveStaticAbsolute(path));

    /// <summary>Reports whether a path identifies a symbolic link or platform reparse point.</summary>
    /// <param name="path">A relative or in-scope absolute path.</param>
    /// <returns><see langword="true"/> for a Windows reparse point or a symbolic link on other supported platforms.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="UnauthorizedAccessException">The path escapes a virtual root or access is denied.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="path"/> belongs to another access scope.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unsupported virtual scheme.</exception>
    /// <exception cref="PlatformNotSupportedException">The platform has no verified symbolic-link backend.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public bool IsLink(string path)
    {
        ThrowIfDisposed();
        EnsureLinkSupport();
        lock (_gate)
        {
            ThrowIfDisposed();
            var resolved = ResolveLocked(path);
            if (OperatingSystem.IsWindows())
            {
                return TryGetAttributes(resolved, out var attributes) &&
                       (attributes & FileAttributes.ReparsePoint) != 0;
            }
            return GetLinkTarget(resolved) is not null;
        }
    }

    /// <summary>Reads the stored target of a symbolic link.</summary>
    /// <param name="path">A relative or in-scope absolute link path.</param>
    /// <returns>The target stored in the link, which can be relative.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="FileNotFoundException">The entry does not exist or is not a symbolic link.</exception>
    /// <exception cref="IOException">The link cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The path escapes a virtual root or access is denied.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="path"/> belongs to another access scope.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unsupported virtual scheme.</exception>
    /// <exception cref="PlatformNotSupportedException">The platform has no verified symbolic-link backend.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public string ReadLink(string path)
    {
        ThrowIfDisposed();
        EnsureLinkSupport();
        lock (_gate)
        {
            ThrowIfDisposed();
            var resolved = ResolveLocked(path);
            return GetLinkTarget(resolved) ?? throw new FileNotFoundException("The path is not a symbolic link.", resolved);
        }
    }

    /// <summary>Creates a symbolic link to a file or directory path.</summary>
    /// <param name="source">A relative or in-scope absolute path stored as the link target.</param>
    /// <param name="target">A relative or in-scope absolute path for the new link.</param>
    /// <remarks>
    /// Relative source paths remain relative and may be dangling. Virtual and absolute source paths are stored as resolved
    /// physical paths. Windows may require Developer Mode or elevation; a missing source is created as a file link.
    /// </remarks>
    /// <exception cref="ArgumentNullException">A path is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A path is empty or invalid.</exception>
    /// <exception cref="IOException">The target exists or link creation fails.</exception>
    /// <exception cref="UnauthorizedAccessException">A path escapes a virtual root or access is denied.</exception>
    /// <exception cref="InvalidOperationException">A path belongs to another access scope.</exception>
    /// <exception cref="NotSupportedException">A path uses an unsupported virtual scheme.</exception>
    /// <exception cref="PlatformNotSupportedException">The platform has no verified symbolic-link backend.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public void CreateLink(string source, string target)
    {
        ThrowIfDisposed();
        EnsureLinkSupport();
        lock (_gate)
        {
            ThrowIfDisposed();
            var resolvedSource = ResolveLocked(source);
            var resolvedTarget = ResolveLocked(target);
            var storedSource = Path.IsPathFullyQualified(source) || source.Contains("://", StringComparison.Ordinal)
                ? resolvedSource
                : source;
            if (TryGetAttributes(resolvedSource, out var attributes) &&
                (attributes & FileAttributes.Directory) != 0)
            {
                Directory.CreateSymbolicLink(resolvedTarget, storedSource);
            }
            else
            {
                File.CreateSymbolicLink(resolvedTarget, storedSource);
            }
        }
    }

    /// <summary>Reports whether a directory is a macOS-style bundle.</summary>
    /// <param name="path">A relative or in-scope absolute directory path.</param>
    /// <returns><see langword="true"/> when the native macOS workspace classifies the existing directory as a file package.</returns>
    /// <exception cref="PlatformNotSupportedException">The current platform is not macOS.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The directory metadata cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The path escapes a virtual root or access is denied.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="path"/> belongs to another access scope.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unsupported virtual scheme.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public bool IsBundle(string path)
    {
        ThrowIfDisposed();
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("Bundle classification is available only on macOS.");
        lock (_gate)
        {
            ThrowIfDisposed();
            var resolved = ResolveLocked(path);
            if (!IsExistingDirectory(resolved))
                return false;
            return GetMacBundleStatus(resolved);
        }
    }

    /// <summary>Gets the filesystem type containing the current directory.</summary>
    /// <returns>An uppercase filesystem identifier when supplied by the operating system, or <c>Network Share</c> for a Windows UNC path.</returns>
    /// <exception cref="IOException">The filesystem metadata cannot be queried.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller lacks access to drive metadata.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public string GetFilesystemType()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            if (OperatingSystem.IsWindows() && IsWindowsNetworkShare(_currentDirectory))
                return "Network Share";
            var path = OperatingSystem.IsWindows()
                ? NormalizeWindowsExtendedPath(_currentDirectory)
                : _currentDirectory;
            return GetDriveForPath(path).DriveFormat.ToUpperInvariant();
        }
    }

    /// <summary>Reports whether the directory containing a path uses case-sensitive names.</summary>
    /// <param name="path">A relative or in-scope absolute path.</param>
    /// <returns><see langword="true"/> when two names differing only by case identify distinct entries.</returns>
    /// <remarks>
    /// The query reads platform filesystem metadata and never creates an entry. Linux reads the directory case-fold flag,
    /// macOS reads the volume case-sensitivity setting, and Windows reads the per-entry case-sensitivity flag.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="FileNotFoundException">The path does not identify an existing file or directory.</exception>
    /// <exception cref="IOException">The entry metadata cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The path escapes a virtual root or access is denied.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="path"/> belongs to another access scope.</exception>
    /// <exception cref="NotSupportedException"><paramref name="path"/> uses an unsupported virtual scheme.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public bool IsCaseSensitive(string path)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            return ProbeCaseSensitivity(ResolveLocked(path));
        }
    }

    /// <summary>Reports whether two paths resolve to the same filesystem object.</summary>
    /// <param name="pathA">The first relative or in-scope absolute path.</param>
    /// <param name="pathB">The second relative or in-scope absolute path.</param>
    /// <returns><see langword="true"/> when both paths have the same volume/device and file identity.</returns>
    /// <remarks>Symbolic links are followed. Distinct hard-link names are therefore equivalent.</remarks>
    /// <exception cref="ArgumentNullException">A path is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A path is empty or invalid.</exception>
    /// <exception cref="IOException">A symbolic-link chain cannot be resolved.</exception>
    /// <exception cref="UnauthorizedAccessException">A path escapes a virtual root or access is denied.</exception>
    /// <exception cref="InvalidOperationException">A path belongs to another access scope.</exception>
    /// <exception cref="NotSupportedException">A path uses an unsupported virtual scheme.</exception>
    /// <exception cref="PlatformNotSupportedException">Native filesystem identity is unavailable for this runtime.</exception>
    /// <exception cref="ObjectDisposedException">The instance is disposing or disposed.</exception>
    public bool IsEquivalent(string pathA, string pathB)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            var first = GetFileIdentity(ResolveLocked(pathA));
            var second = GetFileIdentity(ResolveLocked(pathB));
            return first.HasValue && second.HasValue && first.Value == second.Value;
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_gate)
            {
                EndListingLocked();
                if (!_keepTemporary && _temporaryPath is not null)
                {
                    if (IsExistingDirectory(_temporaryPath))
                        Directory.Delete(_temporaryPath, recursive: true);
                    _temporaryPath = null;
                }
            }
        }

        base.Dispose(disposing);
    }

    private IReadOnlyList<string> GetContents(bool directories)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            var names = CaptureEntries(_currentDirectory, includeNavigationalCandidates: directories)
                .Where(entry => entry.IsDirectory == directories &&
                                (_includeHidden || !entry.IsHidden) &&
                                (_includeNavigational || !entry.IsNavigational))
                .Select(entry => entry.Name)
                .Order(StringComparer.Ordinal)
                .ToArray();
            return Array.AsReadOnly(names);
        }
    }

    private string ResolveLocked(string path)
    {
        ValidatePath(path);
        if (path.StartsWith("res://", StringComparison.Ordinal))
        {
            EnsureScope(AccessScope.Resources);
            return ResolveWithinRoot(_scopeRoot!, path[6..]);
        }
        if (path.StartsWith("user://", StringComparison.Ordinal))
        {
            EnsureScope(AccessScope.UserData);
            return ResolveWithinRoot(_scopeRoot!, path[7..]);
        }
        if (path.Contains("://", StringComparison.Ordinal))
            throw new NotSupportedException($"Virtual path scheme in '{path}' is not supported.");

        var resolved = Path.GetFullPath(path, _currentDirectory);
        if (_scopeRoot is not null && !IsWithin(_scopeRoot, resolved))
            throw new UnauthorizedAccessException("The path escapes this directory access scope.");
        return resolved;
    }

    private void EnsureScope(AccessScope requested)
    {
        if (_scope != requested)
            throw new InvalidOperationException("A directory accessor cannot switch between filesystem, resource, and user-data scopes.");
    }

    private static (string Path, AccessScope Scope, string? Root) ResolveOpenPath(
        string path,
        (string ProjectRoot, string UserDataRoot) roots)
    {
        ValidatePath(path);
        if (path.StartsWith("res://", StringComparison.Ordinal))
        {
            var root = roots.ProjectRoot;
            return (ResolveWithinRoot(root, path[6..]), AccessScope.Resources, root);
        }
        if (path.StartsWith("user://", StringComparison.Ordinal))
        {
            var root = roots.UserDataRoot;
            return (ResolveWithinRoot(root, path[7..]), AccessScope.UserData, root);
        }
        if (path.Contains("://", StringComparison.Ordinal))
            throw new NotSupportedException($"Virtual path scheme in '{path}' is not supported.");
        return (Path.GetFullPath(path), AccessScope.FileSystem, null);
    }

    private static DirAccess OpenAbsolute(string path)
    {
        ValidateAbsolutePath(path);
        var roots = ProjectSettings.Instance.GetPathRootsSnapshot();
        var (resolved, scope, root) = ResolveOpenPath(path, roots);
        RequireExistingDirectory(resolved);
        return new DirAccess(resolved, scope, root);
    }

    private static string ResolveStaticAbsolute(string path)
    {
        ValidateAbsolutePath(path);
        return ResolveStaticAbsolute(path, ProjectSettings.Instance.GetPathRootsSnapshot());
    }

    private static string ResolveStaticAbsolute(
        string path,
        (string ProjectRoot, string UserDataRoot) roots)
    {
        if (path.StartsWith("res://", StringComparison.Ordinal))
            return ResolveWithinRoot(roots.ProjectRoot, path[6..]);
        if (path.StartsWith("user://", StringComparison.Ordinal))
            return ResolveWithinRoot(roots.UserDataRoot, path[7..]);
        if (path.Contains("://", StringComparison.Ordinal))
            throw new NotSupportedException($"Virtual path scheme in '{path}' is not supported.");
        return Path.GetFullPath(path);
    }

    private static void ValidateAbsolutePath(string path)
    {
        ValidatePath(path);
        if (!path.StartsWith("res://", StringComparison.Ordinal) &&
            !path.StartsWith("user://", StringComparison.Ordinal) &&
            !path.Contains("://", StringComparison.Ordinal) &&
            !Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException("A static directory operation requires an absolute or virtual path.", nameof(path));
        }
    }

    private static string ResolveWithinRoot(string root, string relativePath)
    {
        var nativeRelative = relativePath.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(nativeRelative))
            throw new UnauthorizedAccessException("A virtual path cannot contain an absolute suffix.");
        var resolved = Path.GetFullPath(Path.Combine(root, nativeRelative));
        if (!IsWithin(root, resolved))
            throw new UnauthorizedAccessException("A virtual path cannot escape its configured root.");
        return resolved;
    }

    private static bool IsWithin(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path);
        return !Path.IsPathRooted(relative) && relative != ".." &&
               !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
               !relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static void ValidatePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length == 0)
            throw new ArgumentException("A directory path cannot be empty.", nameof(path));
    }

    private static void ValidateTemporaryPrefix(string prefix)
    {
        if (prefix is "." or ".." || prefix.Any(character =>
                character < ' ' || "<>:\"/\\|?*".IndexOf(character) >= 0))
        {
            throw new ArgumentException("A temporary directory prefix contains an invalid character.", nameof(prefix));
        }
    }

    private static DirectoryEntry[] CaptureEntries(string directory, bool includeNavigationalCandidates)
    {
        var entries = new List<DirectoryEntry>();
        if (includeNavigationalCandidates)
        {
            entries.Add(new DirectoryEntry(".", IsDirectory: true, IsHidden: false, IsNavigational: true));
            entries.Add(new DirectoryEntry("..", IsDirectory: true, IsHidden: false, IsNavigational: true));
        }

        foreach (var path in Directory.EnumerateFileSystemEntries(directory))
        {
            var name = Path.GetFileName(path);
            var attributes = File.GetAttributes(path);
            entries.Add(new DirectoryEntry(
                name,
                (attributes & FileAttributes.Directory) != 0,
                !OperatingSystem.IsWindows() && name.StartsWith(".", StringComparison.Ordinal) ||
                (attributes & FileAttributes.Hidden) != 0,
                IsNavigational: false));
        }
        return entries.ToArray();
    }

    private static void MakeSingleDirectory(string path)
    {
        if (TryGetAttributes(path, out _))
            throw new IOException($"An entry already exists at '{path}'.");
        var parent = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(path));
        if (string.IsNullOrEmpty(parent) || !IsExistingDirectory(parent))
            throw new DirectoryNotFoundException($"Parent directory '{parent}' does not exist.");
        Directory.CreateDirectory(path);
    }

    private static void CopyPhysical(string source, string destination, UnixPermissionFlags? permissions)
    {
        EnsureDistinct(source, destination);
        if (permissions.HasValue)
            FileAccess.ValidateUnixPermissions(permissions.Value);
        if (!TryGetAttributes(source, out var sourceAttributes) ||
            (sourceAttributes & FileAttributes.Directory) != 0)
        {
            throw new FileNotFoundException("The source file does not exist.", source);
        }
        File.Copy(source, destination, overwrite: true);
        if (permissions.HasValue)
            FileAccess.SetUnixPermissions(destination, permissions.Value);
    }

    private static void MovePhysical(string source, string destination)
    {
        EnsureDistinct(source, destination);
        var attributes = GetExistingAttributes(source);
        if ((attributes & FileAttributes.Directory) == 0)
        {
            File.Move(source, destination, overwrite: true);
            return;
        }

        var removedEmptyDestination = false;
        if (TryGetAttributes(destination, out var destinationAttributes))
        {
            if ((destinationAttributes & FileAttributes.Directory) == 0)
                throw new IOException("A directory cannot replace a file.");
            if ((destinationAttributes & FileAttributes.ReparsePoint) == 0 &&
                Directory.EnumerateFileSystemEntries(destination).Any())
            {
                throw new IOException("A directory cannot replace a nonempty directory.");
            }
            Directory.Delete(destination);
            removedEmptyDestination = true;
        }
        try
        {
            Directory.Move(source, destination);
        }
        catch (Exception moveError) when (removedEmptyDestination)
        {
            try
            {
                if (!TryGetAttributes(destination, out _))
                    Directory.CreateDirectory(destination);
            }
            catch (Exception restoreError)
            {
                throw new AggregateException("The directory move and destination restoration both failed.", moveError, restoreError);
            }
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(moveError).Throw();
            throw;
        }
    }

    private static void RemovePhysical(string path)
    {
        var attributes = GetExistingAttributes(path);
        if ((attributes & FileAttributes.Directory) != 0)
            Directory.Delete(path, recursive: false);
        else
            File.Delete(path);
    }

    private static FileAttributes GetExistingAttributes(string path)
    {
        if (TryGetAttributes(path, out var attributes))
            return attributes;
        throw new FileNotFoundException("The filesystem entry does not exist.", path);
    }

    private static bool TryGetAttributes(string path, out FileAttributes attributes)
    {
        try
        {
            attributes = File.GetAttributes(path);
            return true;
        }
        catch (FileNotFoundException)
        {
            attributes = default;
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            attributes = default;
            return false;
        }
    }

    private static bool IsExistingDirectory(string path) =>
        TryGetAttributes(path, out var attributes) && (attributes & FileAttributes.Directory) != 0;

    private static bool IsExistingFile(string path) =>
        TryGetAttributes(path, out var attributes) && (attributes & FileAttributes.Directory) == 0;

    private static void RequireExistingDirectory(string path)
    {
        if (!IsExistingDirectory(path))
            throw new DirectoryNotFoundException($"Directory '{path}' does not exist.");
    }

    private static void EnsureDistinct(string source, string destination)
    {
        if (StringComparer.Ordinal.Equals(
                Path.TrimEndingDirectorySeparator(source),
                Path.TrimEndingDirectorySeparator(destination)))
        {
            throw new ArgumentException("Source and destination paths must be different.", nameof(destination));
        }
    }

    private static string? GetLinkTarget(string path)
    {
        var fileTarget = new FileInfo(path).LinkTarget;
        return fileTarget ?? new DirectoryInfo(path).LinkTarget;
    }

    private static bool ProbeCaseSensitivity(string path)
    {
        if (!TryGetAttributes(path, out _))
            throw new FileNotFoundException("The filesystem entry does not exist.", path);

        if (OperatingSystem.IsLinux())
            return GetLinuxCaseSensitivity(path);
        if (OperatingSystem.IsMacOS())
        {
            const int caseSensitive = 11;
            return PathConfApple(path, caseSensitive) > 0;
        }
        if (OperatingSystem.IsWindows())
            return GetWindowsCaseSensitivity(path);
        return true;
    }

    private static bool GetLinuxCaseSensitivity(string path)
    {
        const int openReadOnlyNonBlocking = 0x800;
        var descriptor = OpenUnix(path, openReadOnlyNonBlocking);
        if (descriptor < 0)
            return true;

        try
        {
            var request = (nuint)(0x80000000u | ((uint)IntPtr.Size << 16) | ((uint)'f' << 8) | 1u);
            return IOCTLUnix(descriptor, request, out var flags) < 0 || (flags & 0x40000000L) == 0;
        }
        finally
        {
            _ = CloseUnix(descriptor);
        }
    }

    private static bool GetWindowsCaseSensitivity(string path)
    {
        using var handle = CreateFile(
            path,
            desiredAccess: 0,
            shareMode: 1 | 2 | 4,
            securityAttributes: IntPtr.Zero,
            creationDisposition: 3,
            flagsAndAttributes: 0x02000000,
            templateFile: IntPtr.Zero);
        if (handle.IsInvalid)
            return false;

        var status = NTQueryInformationFile(
            handle,
            out _,
            out var information,
            (uint)Marshal.SizeOf<WindowsCaseSensitiveInformation>(),
            fileInformationClass: 71);
        return status >= 0 && (information.Flags & 1) != 0;
    }

    private static bool GetMacBundleStatus(string path)
    {
        var framework = NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit");
        try
        {
            var workspaceClass = ObjCGetClass("NSWorkspace");
            if (workspaceClass == IntPtr.Zero)
                throw new IOException("The macOS workspace could not classify the directory.");
            var sharedWorkspace = ObjCSend(workspaceClass, SelRegisterName("sharedWorkspace"));
            var nativePath = CFStringCreateWithCString(IntPtr.Zero, path, 0x08000100);
            if (sharedWorkspace == IntPtr.Zero || nativePath == IntPtr.Zero)
                throw new IOException("The macOS workspace could not classify the directory.");
            try
            {
                return ObjCSendBool(
                    sharedWorkspace,
                    SelRegisterName("isFilePackageAtPath:"),
                    nativePath) != 0;
            }
            finally
            {
                CFRelease(nativePath);
            }
        }
        finally
        {
            NativeLibrary.Free(framework);
        }
    }

    private static FileIdentity? GetFileIdentity(string path)
    {
        if (OperatingSystem.IsWindows())
            return GetWindowsFileIdentity(path);
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst())
            return GetUnixFileIdentity(path, appleLayout: true);
        if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid() || OperatingSystem.IsFreeBSD())
            return GetUnixFileIdentity(path, appleLayout: false);
        throw new PlatformNotSupportedException("Filesystem object identity is not implemented on this platform.");
    }

    private static FileIdentity? GetUnixFileIdentity(string path, bool appleLayout)
    {
        if (!Environment.Is64BitProcess)
            throw new PlatformNotSupportedException("Filesystem object identity requires a 64-bit runtime.");

        var buffer = Marshal.AllocHGlobal(512);
        try
        {
            var result = appleLayout ? StatApple(path, buffer) : StatUnix(path, buffer);
            if (result == 0)
            {
                var device = appleLayout
                    ? unchecked((uint)Marshal.ReadInt32(buffer, 0))
                    : unchecked((ulong)Marshal.ReadInt64(buffer, 0));
                var inode = unchecked((ulong)Marshal.ReadInt64(buffer, 8));
                return new FileIdentity(device, inode);
            }

            var error = Marshal.GetLastPInvokeError();
            if (error is 2 or 20)
                return null;
            if (error is 1 or 13)
                throw new UnauthorizedAccessException(new Win32Exception(error).Message);
            throw new IOException(new Win32Exception(error).Message, new Win32Exception(error));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static FileIdentity? GetWindowsFileIdentity(string path)
    {
        using var handle = CreateFile(
            path,
            desiredAccess: 0,
            shareMode: 1 | 2 | 4,
            securityAttributes: IntPtr.Zero,
            creationDisposition: 3,
            flagsAndAttributes: 0x02000000,
            templateFile: IntPtr.Zero);
        if (handle.IsInvalid)
        {
            var error = Marshal.GetLastPInvokeError();
            if (error is 2 or 3)
                return null;
            if (error == 5)
                throw new UnauthorizedAccessException(new Win32Exception(error).Message);
            throw new IOException(new Win32Exception(error).Message, new Win32Exception(error));
        }

        if (!GetFileInformationByHandle(handle, out var information))
        {
            var error = Marshal.GetLastPInvokeError();
            throw new IOException(new Win32Exception(error).Message, new Win32Exception(error));
        }

        return new FileIdentity(
            information.VolumeSerialNumber,
            ((ulong)information.FileIndexHigh << 32) | information.FileIndexLow);
    }

    private static void EnsureLinkSupport()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("Symbolic-link operations are implemented only on Linux, Windows, and macOS.");
    }

    private static DriveEntry[] GetDriveEntries()
    {
        if (OperatingSystem.IsLinux())
            return GetLinuxDriveEntries();
        if (OperatingSystem.IsWindows())
        {
            return DriveInfo.GetDrives()
                .Select(drive => new DriveEntry(
                    Path.TrimEndingDirectorySeparator(drive.Name),
                    drive.DriveType == DriveType.Network ? "<network>" : drive.IsReady ? drive.VolumeLabel : string.Empty,
                    drive.RootDirectory.FullName))
                .ToArray();
        }
        if (OperatingSystem.IsMacOS())
            return GetMacDriveEntries();
        throw new PlatformNotSupportedException("Drive enumeration requires a platform storage-volume backend.");
    }

    private static DriveEntry GetDrive(int index)
    {
        var drives = GetDriveEntries();
        if ((uint)index >= (uint)drives.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        return drives[index];
    }

    private static DriveEntry[] GetLinuxDriveEntries()
    {
        var paths = new HashSet<string>(StringComparer.Ordinal) { Path.DirectorySeparatorChar.ToString() };
        var mountTable = System.IO.File.Exists("/proc/self/mounts") ? "/proc/self/mounts" : "/etc/mtab";
        if (System.IO.File.Exists(mountTable))
        {
            foreach (var line in System.IO.File.ReadLines(mountTable))
            {
                var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length < 2 || !fields[0].StartsWith("/dev", StringComparison.Ordinal))
                    continue;
                var mount = DecodeMountPath(fields[1]);
                if (IsLinuxDriveMount(mount, "/media") ||
                    IsLinuxDriveMount(mount, "/mnt") ||
                    IsLinuxDriveMount(mount, "/home") ||
                    IsLinuxDriveMount(mount, "/run/media"))
                {
                    paths.Add(mount);
                }
            }
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (home.Length > 0)
            paths.Add(Path.GetFullPath(home));

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (desktop.Length > 0)
            paths.Add(Path.GetFullPath(desktop));

        var config = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (config.Length == 0 && home.Length > 0)
            config = Path.Combine(home, ".config");
        var bookmarks = Path.Combine(config, "gtk-3.0", "bookmarks");
        if (System.IO.File.Exists(bookmarks))
        {
            foreach (var line in System.IO.File.ReadLines(bookmarks))
            {
                var uriText = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (uriText is not null && Uri.TryCreate(uriText, UriKind.Absolute, out var uri) && uri.IsFile)
                    paths.Add(Path.GetFullPath(uri.LocalPath));
            }
        }

        return paths.Order(StringComparer.Ordinal)
            .Select(path => new DriveEntry(path, string.Empty, path))
            .ToArray();
    }

    private static string DecodeMountPath(string path) => path
        .Replace("\\040", " ", StringComparison.Ordinal)
        .Replace("\\011", "\t", StringComparison.Ordinal)
        .Replace("\\012", "\n", StringComparison.Ordinal)
        .Replace("\\134", "\\", StringComparison.Ordinal);

    private static bool IsLinuxDriveMount(string mount, string root) =>
        StringComparer.Ordinal.Equals(mount, root) || mount.StartsWith(root + "/", StringComparison.Ordinal);

    private static DriveEntry[] GetMacDriveEntries()
    {
        var framework = NativeLibrary.Load("/System/Library/Frameworks/Foundation.framework/Foundation");
        var pool = ObjCAutoreleasePoolPush();
        try
        {
            var managerClass = ObjCGetClass("NSFileManager");
            if (managerClass == IntPtr.Zero)
                throw new IOException("The macOS file manager is unavailable.");
            var manager = ObjCSend(managerClass, SelRegisterName("defaultManager"));
            var volumes = ObjCSendWithPointerAndUnsigned(
                manager,
                SelRegisterName("mountedVolumeURLsIncludingResourceValuesForKeys:options:"),
                IntPtr.Zero,
                2);
            if (volumes == IntPtr.Zero)
                throw new IOException("Mounted macOS volumes could not be enumerated.");

            var count = ObjCSendUnsigned(volumes, SelRegisterName("count"));
            var result = new DriveEntry[checked((int)count)];
            for (nuint index = 0; index < count; index++)
            {
                var url = ObjCSendWithUnsigned(volumes, SelRegisterName("objectAtIndex:"), index);
                var nativePath = ObjCSend(url, SelRegisterName("path"));
                var utf8 = ObjCSend(nativePath, SelRegisterName("UTF8String"));
                var path = Marshal.PtrToStringUTF8(utf8) ??
                           throw new IOException("A mounted macOS volume has no path.");
                result[checked((int)index)] = new DriveEntry(path, string.Empty, path);
            }
            return result;
        }
        finally
        {
            ObjCAutoreleasePoolPop(pool);
            NativeLibrary.Free(framework);
        }
    }

    private static DriveInfo GetDriveForPath(string path)
    {
        DriveInfo? best = null;
        var bestRootLength = -1;
        foreach (var drive in DriveInfo.GetDrives())
        {
            var root = drive.RootDirectory.FullName;
            if (IsWithin(root, path) && root.Length > bestRootLength)
            {
                best = drive;
                bestRootLength = root.Length;
            }
        }

        return best ?? throw new IOException($"Path '{path}' has no visible filesystem root.");
    }

    private static long GetWindowsAvailableSpace(string path)
    {
        var queryPath = IsWindowsNetworkShare(path) && !Path.EndsInDirectorySeparator(path)
            ? path + '\\'
            : path;
        if (!GetDiskFreeSpaceEx(queryPath, out var available, out _, out _))
        {
            var error = Marshal.GetLastPInvokeError();
            if (error == 5)
                throw new UnauthorizedAccessException(new Win32Exception(error).Message);
            throw new IOException(new Win32Exception(error).Message, new Win32Exception(error));
        }
        return available > long.MaxValue ? long.MaxValue : (long)available;
    }

    private static bool IsWindowsNetworkShare(string path)
    {
        if (path.StartsWith("\\\\?\\UNC\\", StringComparison.OrdinalIgnoreCase))
            return true;
        if (path.StartsWith("\\\\?\\", StringComparison.Ordinal) ||
            path.StartsWith("\\\\.\\", StringComparison.Ordinal))
        {
            return false;
        }
        return path.StartsWith("\\\\", StringComparison.Ordinal);
    }

    private static string NormalizeWindowsExtendedPath(string path)
    {
        if (path.StartsWith("\\\\?\\UNC\\", StringComparison.OrdinalIgnoreCase))
            return "\\\\" + path[8..];
        return path.StartsWith("\\\\?\\", StringComparison.Ordinal)
            ? path[4..]
            : path;
    }

    private void EndListingLocked()
    {
        _listing = null;
        _listingIndex = 0;
        ClearCurrentEntryLocked();
    }

    private void ClearCurrentEntryLocked()
    {
        _currentIsDirectory = false;
        _hasCurrentEntry = false;
    }

    private enum AccessScope
    {
        FileSystem,
        Resources,
        UserData
    }

    private readonly record struct DirectoryEntry(
        string Name,
        bool IsDirectory,
        bool IsHidden,
        bool IsNavigational);

    private readonly record struct FileIdentity(ulong Volume, ulong File);

    private readonly record struct DriveEntry(string Name, string Label, string Root);

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowsFileInformation
    {
        internal uint FileAttributes;
        internal long CreationTime;
        internal long LastAccessTime;
        internal long LastWriteTime;
        internal uint VolumeSerialNumber;
        internal uint FileSizeHigh;
        internal uint FileSizeLow;
        internal uint NumberOfLinks;
        internal uint FileIndexHigh;
        internal uint FileIndexLow;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowsIoStatusBlock
    {
        internal IntPtr Status;
        internal nuint Information;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowsCaseSensitiveInformation
    {
        internal uint Flags;
    }

    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    private static extern int OpenUnix(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        int flags);

    [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
    private static extern int IOCTLUnix(int descriptor, nuint request, out long flags);

    [DllImport("libc", EntryPoint = "close", SetLastError = true)]
    private static extern int CloseUnix(int descriptor);

    [DllImport("libSystem.B.dylib", EntryPoint = "pathconf", SetLastError = true)]
    private static extern long PathConfApple(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        int name);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation", EntryPoint = "CFStringCreateWithCString")]
    private static extern IntPtr CFStringCreateWithCString(
        IntPtr allocator,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string value,
        uint encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation", EntryPoint = "CFRelease")]
    private static extern void CFRelease(IntPtr value);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_getClass")]
    private static extern IntPtr ObjCGetClass([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr SelRegisterName([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjCSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern byte ObjCSendBool(IntPtr receiver, IntPtr selector, IntPtr argument);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern nuint ObjCSendUnsigned(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjCSendWithUnsigned(IntPtr receiver, IntPtr selector, nuint argument);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr ObjCSendWithPointerAndUnsigned(
        IntPtr receiver,
        IntPtr selector,
        IntPtr pointerArgument,
        nuint unsignedArgument);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_autoreleasePoolPush")]
    private static extern IntPtr ObjCAutoreleasePoolPush();

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_autoreleasePoolPop")]
    private static extern void ObjCAutoreleasePoolPop(IntPtr pool);

    [DllImport("libc", EntryPoint = "stat", SetLastError = true)]
    private static extern int StatUnix(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        IntPtr buffer);

    [DllImport("libSystem.B.dylib", EntryPoint = "stat", SetLastError = true)]
    private static extern int StatApple(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        IntPtr buffer);

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(
        SafeFileHandle file,
        out WindowsFileInformation information);

    [DllImport("kernel32.dll", EntryPoint = "GetDiskFreeSpaceExW", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetDiskFreeSpaceEx(
        string directoryName,
        out ulong freeBytesAvailable,
        out ulong totalNumberOfBytes,
        out ulong totalNumberOfFreeBytes);

    [DllImport("ntdll.dll", EntryPoint = "NtQueryInformationFile")]
    private static extern int NTQueryInformationFile(
        SafeFileHandle file,
        out WindowsIoStatusBlock ioStatusBlock,
        out WindowsCaseSensitiveInformation fileInformation,
        uint length,
        int fileInformationClass);
}
