using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Owns process-wide operating-system operations through a retained service object.</summary>
/// <remarks>Independent process launch/capture and owned native pipes are cold operations; child termination is explicit.
/// Process environment edits synchronize managed and SDL child launch paths. Desktop directory queries do not create folders.
/// The trash operation uses recoverable desktop Linux and Windows system services.
/// Unavailable platform services fail explicitly. Native libraries are system facilities and remain process-owned.</remarks>
public sealed partial class OS : ElectronObject
{
    internal static OS Service { get; } = new();
    private OS() { }

    /// <summary>Returns the current combined managed and tracked native CPU allocation size in bytes.</summary>
    /// <returns>A nonnegative 64-bit byte count, or zero in Release.</returns>
    /// <remarks>Debug requires the instrumented Linux x64 managed runtime and matching native bundle. This query needs no window.
    /// The ledger includes dynamic and frozen managed objects and tracked native CPU payloads.
    /// GPU storage, stacks, JIT code, unused GC capacity, allocator bookkeeping and allocations outside the ledger are excluded.</remarks>
    /// <exception cref="PlatformNotSupportedException">The installed runtime or native bundle lacks compatible combined accounting in Debug.</exception>
    /// <exception cref="DllNotFoundException">The native bundle cannot be loaded in Debug.</exception>
    public static long GetStaticMemoryUsage()
    {
#if DEBUG
        return NativeAllocation.ProcessSnapshot().Current;
#else
        return 0;
#endif
    }

    /// <summary>Returns the greatest simultaneous combined managed and tracked native CPU allocation size in bytes.</summary>
    /// <returns>A nonnegative 64-bit byte count, or zero in Release.</returns>
    /// <remarks>Uses the same accounting scope and runtime requirements as <see cref="GetStaticMemoryUsage"/>.
    /// The process retains this peak after memory is released, including allocations that end between queries.</remarks>
    /// <exception cref="PlatformNotSupportedException">The installed runtime or native bundle lacks compatible combined accounting in Debug.</exception>
    /// <exception cref="DllNotFoundException">The native bundle cannot be loaded in Debug.</exception>
    public static long GetStaticMemoryPeakUsage()
    {
#if DEBUG
        return NativeAllocation.ProcessSnapshot().Peak;
#else
        return 0;
#endif
    }

    /// <summary>Moves a file, directory or symbolic link to the desktop trash without permanently deleting it.</summary>
    /// <param name="path">An ordinary, res:// or user:// path identifying the entry itself.</param>
    /// <remarks>This blocking operation may perform filesystem I/O and is unsuitable for a real-time callback.
    /// Linux delegates recovery metadata, mount policy, collisions and link handling to GIO. Windows uses the Shell recycle service on a private STA without changing the caller apartment; permanent-delete requests are refused before commitment. Other profiles require
    /// their native trash backend; a failed request preserves the source and is never replaced with permanent deletion.</remarks>
    /// <exception cref="ArgumentException">The path is empty or contains a null character.</exception>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    /// <exception cref="NotSupportedException">The platform or system trash service is unavailable.</exception>
    /// <exception cref="IOException">The native service cannot trash the entry.</exception>
    public static void MoveToTrash(string path) => Service.MoveToTrashCore(path);
    /// <summary>Requests the platform's default application for a URI or filesystem resource.</summary>
    /// <param name="uri">A nonempty URI or ordinary filesystem path.</param>
    /// <exception cref="ArgumentException">The resource identifier is empty or contains a null character.</exception>
    /// <exception cref="InvalidOperationException">The native platform rejects the launch request.</exception>
    public static void ShellOpen(string uri) => Service.ShellOpenCore(uri);
    private void ShellOpenCore(string uri)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(uri); if (uri.Length == 0 || uri.Contains('\0')) throw new ArgumentException("Expected a nonempty resource identifier.", nameof(uri));
        uri = System.IO.Path.IsPathRooted(uri) ? new Uri(System.IO.Path.GetFullPath(uri)).AbsoluteUri : Uri.TryCreate(uri, UriKind.Absolute, out var parsed) ? parsed.AbsoluteUri : new Uri(System.IO.Path.GetFullPath(uri)).AbsoluteUri;
        LaunchResource(uri);
    }

    private static void LaunchResource(string uri)
    {
        if (!SDL3.SDL.OpenURL(uri)) throw new InvalidOperationException("The platform rejected the resource launch request: " + SDL3.SDL.GetError());
    }
    /// <summary>Requests the file manager for a file or directory.</summary>
    /// <param name="fileOrDirPath">An ordinary filesystem path. Globalize project paths before calling.</param><param name="openFolder">Enters a directory when true; otherwise selects the item in its parent.</param>
    /// <remarks>Windows uses native Shell item identities on a temporary STA worker, preserving the caller's COM apartment.
    /// Linux uses the desktop FileManager1 service via GIO. When the service, session bus address, or GIO is unavailable,
    /// Linux opens the requested directory or the item's containing directory through the default application.
    /// The fallback does not select an item. The macOS selection backend is unavailable.
    /// This cold synchronous operation requests the external application; its UI may finish opening afterward.</remarks>
    /// <exception cref="ArgumentException">The path is empty, contains a null character, or is not a valid ordinary path.</exception>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    /// <exception cref="IOException">The target does not exist or the native request fails.</exception>
    /// <exception cref="InvalidOperationException">The default application rejects the fallback launch.</exception>
    /// <exception cref="NotSupportedException">The profile requires a native item-selection backend.</exception>
    public static void ShellShowInFileManager(string fileOrDirPath, bool openFolder = true) => Service.ShowInFileManagerCore(fileOrDirPath, openFolder);
    private void ShowInFileManagerCore(string path, bool openFolder)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (path.Contains('\0'))
            throw new ArgumentException("Path contains a null character.", nameof(path));

        var absolute = System.IO.Path.GetFullPath(path);
        if (OperatingSystem.IsWindows())
        {
            WindowsFileManager.Show(absolute, openFolder);
            return;
        }

        if (OperatingSystem.IsMacOS())
            throw new NotSupportedException("Native file-manager selection requires the platform backend.");

        var isDirectory = Directory.Exists(absolute);
        var enterDirectory = openFolder && isDirectory;
        var desktopLinux = OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid();
        if (desktopLinux)
        {
            if (!isDirectory && !File.Exists(absolute))
                throw new FileNotFoundException("The file-manager target does not exist.", absolute);

            if (LinuxFileManager.TryShow(absolute, enterDirectory))
                return;

            if (isDirectory && absolute.Length > 1)
                absolute = absolute.TrimEnd('/');
        }

        var directory = enterDirectory ? absolute : System.IO.Path.GetDirectoryName(absolute) ?? absolute;
        // Escape raw Linux filenames, where a percent escape may be literal path text.
        var uri = desktopLinux ? "file://" + string.Join('/', directory.Split('/').Select(Uri.EscapeDataString)) : new Uri(directory).AbsoluteUri;
        LaunchResource(uri);
    }
    private void MoveToTrashCore(string path)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(path); if (path.Length == 0 || path.Contains('\0')) throw new ArgumentException("Expected a nonempty path without null characters.", nameof(path));
        var absolute = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath(path));
        if (OperatingSystem.IsWindows()) { WindowsTrash.Move(absolute); return; }
        if (!OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) throw new NotSupportedException("This platform requires a native recoverable trash backend.");
        var native = TrashNative.Instance.Value; var file = native.NewFile(absolute);
        if (file == 0) throw new IOException("The native trash service could not create a file identity.");
        try
        {
            if (native.Trash(file, 0, out var error) != 0) return;
            try { throw new IOException(error == 0 ? "The native trash request failed." : Marshal.PtrToStringUTF8(Marshal.PtrToStructure<NativeError>(error).Message)); }
            finally { if (error != 0) native.FreeError(error); }
        }
        finally { native.Unref(file); }
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeError { internal uint Domain; internal int Code; internal nint Message; }
    private sealed unsafe class TrashNative
    {
        internal static readonly Lazy<TrashNative> Instance = new(() => new());
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate nint NewFileDelegate(byte* path);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int TrashDelegate(nint file, nint cancellation, out nint error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void ReleaseDelegate(nint pointer);
        private readonly NewFileDelegate _newFile;
        internal readonly TrashDelegate Trash;
        internal readonly ReleaseDelegate Unref, FreeError;
        internal nint NewFile(string path)
        {
            Span<byte> buffer = stackalloc byte[NativeUTF8StringMarshaller.In.BufferSize];
            scoped NativeUTF8StringMarshaller.In text = new();
            try { text.FromManaged(path, buffer); return _newFile(text.ToUnmanaged()); }
            finally { text.Free(); }
        }
        private TrashNative()
        {
            if (!NativeLibrary.TryLoad("libgio-2.0.so.0", out var library)) throw new NotSupportedException("The system GIO trash service is unavailable.");
            try { _newFile = Load<NewFileDelegate>(library, "g_file_new_for_path"); Trash = Load<TrashDelegate>(library, "g_file_trash"); Unref = Load<ReleaseDelegate>(library, "g_object_unref"); FreeError = Load<ReleaseDelegate>(library, "g_error_free"); }
            catch { NativeLibrary.Free(library); throw; }
        }
        private static T Load<T>(nint library, string symbol) where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(library, symbol));
    }
    /// <inheritdoc />
    protected override void ValidateDisposal() => throw new InvalidOperationException("The process operating-system service cannot be disposed.");
}
