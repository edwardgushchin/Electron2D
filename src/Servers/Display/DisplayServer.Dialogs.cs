using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    /// <summary>Selects the kind of native file chooser.</summary>
    public enum FileDialogMode
    {
        /// <summary>Selects one existing file.</summary>
        OpenFile = 0,
        /// <summary>Selects multiple existing files.</summary>
        OpenFiles = 1,
        /// <summary>Selects one directory.</summary>
        OpenDirectory = 2,
        /// <summary>Selects either a file or a directory; unavailable in the current native backend.</summary>
        OpenAny = 3,
        /// <summary>Selects a destination file, which need not already exist.</summary>
        SaveFile = 4,
    }

    private static readonly ConcurrentDictionary<nint, DialogRequest> DialogRequests = new();
    private static readonly SDL.DialogFileCallback NativeDialogCallback = OnNativeDialogCompleted;
    private static int _nextDialogRequestId;

    private readonly object _dialogGate = new();
    private readonly Queue<DialogCompletion> _dialogCompletions = new();
    private int _pendingNativeDialogs;
    private bool _dialogsDisposed;

    /// <summary>Shows a modal native message with buttons in the supplied order.</summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="description">Dialog body text.</param>
    /// <param name="buttons">One or more nonempty button labels.</param>
    /// <param name="callback">Receives the zero-based index reported by the native dialog. Dismissal without a button has platform-specific reporting.</param>
    /// <remarks>The native message box blocks the owner thread; the callback runs on that thread before this method returns. Native appearance, button placement, and dismissal reporting depend on the operating system. On the verified Linux Wayland host, closing a one-button dialog reports button index zero.</remarks>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="ArgumentException">No valid button label was supplied.</exception>
    /// <exception cref="ObjectDisposedException">The display server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The native dialog could not be shown or the call is off the owner thread.</exception>
    public void DialogShow(string title, string description, IReadOnlyList<string> buttons, Action<int> callback)
    {
        EnsureOwner();
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(buttons);
        ArgumentNullException.ThrowIfNull(callback);
        if (buttons.Count == 0)
            throw new ArgumentException("At least one button is required.", nameof(buttons));

        var buttonSize = Marshal.SizeOf<SDL.MessageBoxButtonData>();
        var nativeButtons = Marshal.AllocHGlobal(checked(buttons.Count * buttonSize));
        var written = 0;
        try
        {
            for (var index = 0; index < buttons.Count; index++)
            {
                var label = buttons[index];
                if (string.IsNullOrEmpty(label))
                    throw new ArgumentException("Button labels must be nonempty.", nameof(buttons));
                var button = new SDL.MessageBoxButtonData { ButtonID = index, Text = label };
                Marshal.StructureToPtr(button, nativeButtons + index * buttonSize, false);
                written++;
            }

            var message = new SDL.MessageBoxData
            {
                Flags = SDL.MessageBoxFlags.Information,
                Window = GetWindow(MainWindowId),
                Title = title,
                Message = description,
                NumButtons = buttons.Count,
                Buttons = nativeButtons,
            };
            if (!SDL.ShowMessageBox(in message, out var selected))
                throw SdlFailure("show a native message dialog");
            callback(selected);
        }
        finally
        {
            for (var index = 0; index < written; index++)
                Marshal.DestroyStructure<SDL.MessageBoxButtonData>(nativeButtons + index * buttonSize);
            Marshal.FreeHGlobal(nativeButtons);
        }
    }

    /// <summary>Opens a native file or folder chooser and delivers its result during a later event pump.</summary>
    /// <param name="title">Requested dialog title; the native platform may ignore it.</param>
    /// <param name="currentDirectory">Initial filesystem directory, or an empty string for the platform default.</param>
    /// <param name="filename">Initial filename for file modes, or an empty string. Linux uses it only for SaveFile.</param>
    /// <param name="showHidden">Requests hidden files. This SDL chooser ignores the preference; on Linux this matches the native contract.</param>
    /// <param name="mode">The selection mode. <see cref="FileDialogMode.OpenAny"/> has no equivalent in the current native backend.</param>
    /// <param name="filters">Extension filters such as <c>*.png,*.jpg;Images</c>. File modes accept but do not apply a trailing MIME section; MIME-only filters are unavailable. Folder mode ignores filters.</param>
    /// <param name="callback">Receives success, selected paths, and the selected filter index. Cancellation and native failure both deliver false and an empty path list.</param>
    /// <param name="parentWindowId">The parent window ID; only <see cref="MainWindowId"/> is owned.</param>
    /// <remarks>The native callback may run on another thread. Paths are copied immediately and the typed callback runs only on the owner thread during <see cref="ProcessEvents"/>. A native failure still completes the callback, then the event pump reports it in an aggregate exception. Disposal is rejected until the native chooser completes and its result has been pumped. The operating system may ignore the title, initial location, or filters; Android may return content URIs instead of filesystem paths.</remarks>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="ArgumentException">A filter is malformed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The mode or window ID is invalid.</exception>
    /// <exception cref="NotSupportedException">The mode is <see cref="FileDialogMode.OpenAny"/> or a file-mode filter contains only MIME types.</exception>
    /// <exception cref="ObjectDisposedException">The display server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The native chooser could not be launched or the call is off the owner thread.</exception>
    public void FileDialogShow(
        string title,
        string currentDirectory,
        string filename,
        bool showHidden,
        FileDialogMode mode,
        IReadOnlyList<string> filters,
        Action<bool, IReadOnlyList<string>, int> callback,
        int parentWindowId = MainWindowId)
    {
        EnsureOwner();
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(currentDirectory);
        ArgumentNullException.ThrowIfNull(filename);
        ArgumentNullException.ThrowIfNull(filters);
        ArgumentNullException.ThrowIfNull(callback);
        var window = GetWindow(parentWindowId);
        var nativeMode = mode switch
        {
            FileDialogMode.OpenFile or FileDialogMode.OpenFiles => SDL.FileDialogType.OpenFile,
            FileDialogMode.OpenDirectory => SDL.FileDialogType.OpenFolder,
            FileDialogMode.SaveFile => SDL.FileDialogType.SaveFile,
            FileDialogMode.OpenAny => throw new NotSupportedException("The native chooser cannot select files and folders together."),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown file dialog mode."),
        };

        var nativeFilters = nativeMode == SDL.FileDialogType.OpenFolder ? [] : CreateDialogFilters(filters);
        var request = new DialogRequest(this, callback, nativeFilters);
        var properties = SDL.CreateProperties();
        if (properties == 0)
        {
            request.DisposeFilters();
            throw SdlFailure("create file dialog properties");
        }

        try
        {
            var location = filename.Length == 0 || nativeMode == SDL.FileDialogType.OpenFolder ||
                (OperatingSystem.IsLinux() && nativeMode != SDL.FileDialogType.SaveFile)
                ? currentDirectory
                : currentDirectory.Length == 0 ? filename : Path.Combine(currentDirectory, filename);
            if (!SDL.SetPointerProperty(properties, SDL.Props.FileDialogWindowPointer, window) ||
                !SDL.SetStringProperty(properties, SDL.Props.FileDialogTitleString, title) ||
                !SDL.SetStringProperty(properties, SDL.Props.FileDialogLocationString, location) ||
                !SDL.SetBooleanProperty(properties, SDL.Props.FileDialogManyBoolean, mode == FileDialogMode.OpenFiles) ||
                nativeFilters.Length > 0 &&
                (!SDL.SetPointerProperty(properties, SDL.Props.FileDialogFiltersPointer, request.FiltersPointer) ||
                 !SDL.SetNumberProperty(properties, SDL.Props.FileDialogNFiltersNumber, nativeFilters.Length)))
                throw SdlFailure("configure a native file dialog");

            nint requestId;
            do
            {
                requestId = Interlocked.Increment(ref _nextDialogRequestId);
            } while (requestId == 0 || !DialogRequests.TryAdd(requestId, request));
            lock (_dialogGate)
                _pendingNativeDialogs++;
            try
            {
                SDL.ShowFileDialogWithProperties(nativeMode, NativeDialogCallback, requestId, properties);
            }
            catch
            {
                if (DialogRequests.TryRemove(requestId, out _))
                {
                    request.DisposeFilters();
                    lock (_dialogGate)
                        _pendingNativeDialogs--;
                }
                throw;
            }
        }
        catch
        {
            request.DisposeFilters();
            throw;
        }
        finally
        {
            SDL.DestroyProperties(properties);
        }
    }

    private static SDL.DialogFileFilter[] CreateDialogFilters(IReadOnlyList<string> filters)
    {
        var native = new SDL.DialogFileFilter[filters.Count];
        var count = 0;
        try
        {
            for (var index = 0; index < filters.Count; index++)
            {
                var entry = filters[index] ?? throw new ArgumentException("A filter is null.", nameof(filters));
                var parts = entry.Split(';');
                if (parts.Length >= 3 && parts[0].Trim().Length == 0 && parts[2].Trim().Length > 0)
                    throw new NotSupportedException("The native chooser cannot apply a MIME-only filter.");
                var patterns = parts[0].Split(',');
                var extensions = new string[patterns.Length];
                for (var part = 0; part < patterns.Length; part++)
                {
                    var pattern = patterns[part].Trim();
                    if (pattern is "*" or "*.*")
                    {
                        if (patterns.Length != 1)
                            throw new ArgumentException("The all-files filter must stand alone.", nameof(filters));
                        extensions[part] = "*";
                        continue;
                    }
                    if (!pattern.StartsWith("*.", StringComparison.Ordinal) || pattern.Length <= 2 ||
                        !pattern.AsSpan(2).ToString().All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.'))
                        throw new ArgumentException($"Invalid extension filter at index {index}.", nameof(filters));
                    extensions[part] = pattern[2..];
                }
                var label = parts.Length > 1 && parts[1].Length > 0 ? parts[1] : parts[0];
                native[index] = new SDL.DialogFileFilter(label, string.Join(';', extensions));
                count++;
            }
            return native;
        }
        catch
        {
            for (var index = 0; index < count; index++)
                native[index].Dispose();
            throw;
        }
    }

    private static void OnNativeDialogCompleted(nint userdata, nint fileList, int filter)
    {
        if (!DialogRequests.TryRemove(userdata, out var request))
            return;
        string[] paths = [];
        string? error = null;
        try
        {
            if (fileList == 0)
                error = SDL.GetError() ?? "The native file dialog failed.";
            else
            {
                var selected = new List<string>();
                for (var index = 0; ; index++)
                {
                    var path = Marshal.ReadIntPtr(fileList, checked(index * IntPtr.Size));
                    if (path == 0)
                        break;
                    selected.Add(Marshal.PtrToStringUTF8(path) ?? string.Empty);
                }
                paths = [.. selected];
            }
        }
        catch (Exception exception)
        {
            error = exception.Message;
        }
        finally
        {
            request.DisposeFilters();
            request.Owner.QueueDialogCompletion(new DialogCompletion(request.Callback, paths, filter, error));
        }
    }

    private void QueueDialogCompletion(DialogCompletion completion)
    {
        lock (_dialogGate)
        {
            if (!_dialogsDisposed)
                _dialogCompletions.Enqueue(completion);
            _pendingNativeDialogs--;
        }
    }

    private void DrainDialogCallbacks()
    {
        EnsureOwner();
        DialogCompletion[] batch;
        lock (_dialogGate)
        {
            batch = _dialogCompletions.ToArray();
            _dialogCompletions.Clear();
        }
        List<Exception>? failures = null;
        foreach (var result in batch)
        {
            if (result.Error is not null)
                (failures ??= []).Add(new InvalidOperationException($"Native file dialog failed: {result.Error}"));
            try
            {
                result.Callback(result.Paths.Length > 0, result.Paths, result.Filter);
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }
        if (failures is not null)
            throw new AggregateException("File dialog callbacks failed.", failures);
    }

    private void ValidateDialogDisposal()
    {
        lock (_dialogGate)
            if (_pendingNativeDialogs != 0 || _dialogCompletions.Count != 0)
                throw new InvalidOperationException("Complete and pump native file dialogs before disposing the display server.");
    }

    private void DisposeDialogs()
    {
        lock (_dialogGate)
        {
            _dialogsDisposed = true;
            _dialogCompletions.Clear();
        }
    }

    private sealed class DialogRequest(
        DisplayServer owner,
        Action<bool, IReadOnlyList<string>, int> callback,
        SDL.DialogFileFilter[] filters)
    {
        private readonly GCHandle _filterPin = filters.Length == 0 ? default : GCHandle.Alloc(filters, GCHandleType.Pinned);
        private int _filtersDisposed;

        public DisplayServer Owner { get; } = owner;
        public Action<bool, IReadOnlyList<string>, int> Callback { get; } = callback;
        public nint FiltersPointer => _filterPin.IsAllocated ? _filterPin.AddrOfPinnedObject() : 0;

        public void DisposeFilters()
        {
            if (Interlocked.Exchange(ref _filtersDisposed, 1) != 0)
                return;
            if (_filterPin.IsAllocated)
                _filterPin.Free();
            foreach (var filter in filters)
                filter.Dispose();
        }
    }

    private readonly record struct DialogCompletion(
        Action<bool, IReadOnlyList<string>, int> Callback,
        string[] Paths,
        int Filter,
        string? Error);
}
