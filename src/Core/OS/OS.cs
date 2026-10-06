using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Owns process-wide operating-system operations through a retained service object.</summary>
/// <remarks>The current filesystem operation uses the Linux desktop's recoverable trash service.
/// Unavailable platform services fail explicitly. Native libraries are system facilities and remain process-owned.</remarks>
public sealed class OS : ElectronObject
{
    internal static OS Service { get; } = new();
    private OS() { }
    /// <summary>Moves a file, directory or symbolic link to the desktop trash without permanently deleting it.</summary>
    /// <param name="path">An ordinary, res:// or user:// path identifying the entry itself.</param>
    /// <remarks>This blocking operation may perform filesystem I/O and is unsuitable for a real-time callback.
    /// Linux delegates recovery metadata, mount policy, collisions and link handling to GIO. Other profiles require
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
        if (!SDL3.SDL.OpenURL(uri)) throw new InvalidOperationException("The platform rejected the resource launch request: " + SDL3.SDL.GetError());
    }
    /// <summary>Requests the file manager for a file or directory.</summary>
    /// <param name="fileOrDirPath">An ordinary filesystem path.</param><param name="openFolder">Enters a directory when true; otherwise opens its parent.</param>
    /// <remarks>Linux uses the platform folder-URI fallback. Native item selection on Windows/macOS requires its backend.</remarks>
    /// <exception cref="NotSupportedException">The profile requires a native item-selection backend.</exception>
    public static void ShellShowInFileManager(string fileOrDirPath, bool openFolder = true) => Service.ShowInFileManagerCore(fileOrDirPath, openFolder);
    private void ShowInFileManagerCore(string path, bool openFolder)
    {
        ThrowIfDisposed(); ArgumentException.ThrowIfNullOrEmpty(path); if (path.Contains('\0')) throw new ArgumentException("Path contains a null character.", nameof(path));
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()) throw new NotSupportedException("Native file-manager selection requires the platform backend.");
        var absolute = System.IO.Path.GetFullPath(path); var directory = openFolder && Directory.Exists(absolute) ? absolute : System.IO.Path.GetDirectoryName(absolute) ?? absolute; ShellOpenCore(new Uri(directory).AbsoluteUri);
    }
    private void MoveToTrashCore(string path)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(path); if (path.Length == 0 || path.Contains('\0')) throw new ArgumentException("Expected a nonempty path without null characters.", nameof(path));
        var absolute = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath(path));
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
    private sealed class TrashNative
    {
        internal static readonly Lazy<TrashNative> Instance = new(() => new());
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate nint NewFileDelegate([MarshalAs(UnmanagedType.LPUTF8Str)] string path);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int TrashDelegate(nint file, nint cancellation, out nint error);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void ReleaseDelegate(nint pointer);
        internal readonly NewFileDelegate NewFile;
        internal readonly TrashDelegate Trash;
        internal readonly ReleaseDelegate Unref, FreeError;
        private TrashNative()
        {
            if (!NativeLibrary.TryLoad("libgio-2.0.so.0", out var library)) throw new NotSupportedException("The system GIO trash service is unavailable.");
            try { NewFile = Load<NewFileDelegate>(library, "g_file_new_for_path"); Trash = Load<TrashDelegate>(library, "g_file_trash"); Unref = Load<ReleaseDelegate>(library, "g_object_unref"); FreeError = Load<ReleaseDelegate>(library, "g_error_free"); }
            catch { NativeLibrary.Free(library); throw; }
        }
        private static T Load<T>(nint library, string symbol) where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(library, symbol));
    }
    /// <inheritdoc />
    protected override void ValidateDisposal() => throw new InvalidOperationException("The process operating-system service cannot be disposed.");
}
