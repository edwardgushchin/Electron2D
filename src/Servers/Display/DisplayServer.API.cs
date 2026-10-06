namespace Electron2D;

public sealed partial class DisplayServer
{
    /// <summary>Gets whether a native service is currently available.</summary>
    /// <value>True while a service is published; this observation does not reserve its lifetime.</value>
    public static bool IsAvailable => Service is not null;

    private static DisplayServer RequireService() => Service ?? throw new InvalidOperationException("The DisplayServer service is not active.");

    /// <summary>Reports whether the current backend advertises an integrated display capability.</summary>
    /// <param name="feature">The display capability to query.</param>
    /// <returns><see langword="true"/> for an integrated capability advertised for the current native driver or device state.</returns>
    /// <remarks>
    /// A false result may mean that the platform offers a service that this engine has not integrated. Text clipboard
    /// access is available while open. Mouse follows connected devices; touchscreen also follows mouse-to-touch emulation. Pointer warp, cursor
    /// shapes, content scale, native dialogs, primary selection, and input-method composition are advertised only on
    /// the native drivers listed in the feature reference. On Wayland, icon support becomes known after a successful native icon request;
    /// a false result before that request does not prove the compositor lacks the icon protocol. Other defined values and unknown numeric values return
    /// false. On Wayland, a positive <see cref="Feature.Ime"/> or <see cref="Feature.NativeDialogFile"/> result does not yet
    /// prove that the compositor text-input protocol or native file chooser is available. Native operations can still fail after a positive query.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static bool HasFeature(Feature feature) => RequireService().HasFeatureCore(feature);

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
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void DialogShow(string title, string description, IReadOnlyList<string> buttons, Action<int> callback) => RequireService().DialogShowCore(title, description, buttons, callback);

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
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void FileDialogShow(
        string title,
        string currentDirectory,
        string filename,
        bool showHidden,
        FileDialogMode mode,
        IReadOnlyList<string> filters,
        Action<bool, IReadOnlyList<string>, int> callback,
        int parentWindowId = MainWindowId) => RequireService().FileDialogShowCore(title, currentDirectory, filename, showHidden, mode, filters, callback, parentWindowId);

    /// <summary>Occurs when the operating system requests that the application quit.</summary>
    /// <remarks>Delivery is synchronous during <see cref="ProcessEvents"/>. The server does not close automatically.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static event Action? QuitRequested
    {
        add => RequireService().QuitRequestedCore += value;
        remove => RequireService().QuitRequestedCore -= value;
    }

    /// <summary>Occurs when the native system theme changes.</summary>
    /// <remarks>Delivery is synchronous in native event order during <see cref="ProcessEvents"/>. Query <see cref="IsDarkMode"/> after delivery to read the current theme.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static event Action? SystemThemeChanged
    {
        add => RequireService().SystemThemeChangedCore += value;
        remove => RequireService().SystemThemeChangedCore -= value;
    }

    /// <summary>Occurs when the main window receives a close request.</summary>
    /// <remarks>Delivery is synchronous in native queue order during <see cref="ProcessEvents"/> or
    /// <see cref="ForceProcessAndDropEvents"/> for the main window only. The server does not close automatically.
    /// A failing handler does not prevent later queued events from being delivered; the pump reports its failure afterward.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static event Action? CloseRequested
    {
        add => RequireService().CloseRequestedCore += value;
        remove => RequireService().CloseRequestedCore -= value;
    }

    /// <summary>Occurs when the pointer enters the main window.</summary>
    /// <remarks>Only effective pointer transitions for the main window notify, in native queue order during <see cref="ProcessEvents"/> or <see cref="ForceProcessAndDropEvents"/>. A failing handler does not prevent later queued events from being delivered.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static event Action? WindowMouseEntered
    {
        add => RequireService().WindowMouseEnteredCore += value;
        remove => RequireService().WindowMouseEnteredCore -= value;
    }

    /// <summary>Occurs when the pointer leaves the main window.</summary>
    /// <remarks>Only effective pointer transitions for the main window notify, in native queue order during <see cref="ProcessEvents"/> or <see cref="ForceProcessAndDropEvents"/>. A failing handler does not prevent later queued events from being delivered.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static event Action? WindowMouseExited
    {
        add => RequireService().WindowMouseExitedCore += value;
        remove => RequireService().WindowMouseExitedCore -= value;
    }

    /// <summary>Occurs when the main window's native display content scale changes.</summary>
    /// <remarks>Delivery follows native event order during <see cref="ProcessEvents"/>. The notification does not include a DPI value: native content scale may change when a window moves between displays, and it is not a measurement of physical dots per inch. A failing handler does not prevent later queued events from being delivered.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static event Action? WindowDpiChanged
    {
        add => RequireService().WindowDpiChangedCore += value;
        remove => RequireService().WindowDpiChangedCore -= value;
    }

    /// <summary>Occurs when the main window's observed client rectangle changes.</summary>
    /// <remarks>Receives the full rectangle with client-pixel dimensions on Wayland, Android, iOS, tvOS and browsers, and platform-native window dimensions elsewhere, after the changed position or size has been committed and in native event order during <see cref="ProcessEvents"/>. On Wayland, the position is conventionally zero because the compositor does not disclose a reliable global position. Unchanged rectangles and events from other windows do not notify. Delivery is confined to the opening thread; a failing handler does not prevent later queued events from being delivered.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static event Action<Rect2i>? WindowRectChanged
    {
        add => RequireService().WindowRectChangedCore += value;
        remove => RequireService().WindowRectChangedCore -= value;
    }

    /// <summary>Occurs when the main window gains or loses keyboard focus.</summary>
    /// <remarks>Delivery is synchronous for the main window during <see cref="ProcessEvents"/> or <see cref="ForceProcessAndDropEvents"/>. The window callback precedes the application notification. Losing focus releases tracked pressed input afterward, even when either callback fails; failures are reported after later queued events are delivered.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static event Action<bool>? WindowFocusChanged
    {
        add => RequireService().WindowFocusChangedCore += value;
        remove => RequireService().WindowFocusChangedCore -= value;
    }

    /// <summary>Occurs when the platform commits text input, including text composed through an IME.</summary>
    /// <remarks>Delivery is synchronous during <see cref="ProcessEvents"/>; the input event retains no string state.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static event Action<string>? TextInput
    {
        add => RequireService().TextInputCore += value;
        remove => RequireService().TextInputCore -= value;
    }

    /// <summary>Occurs after the native input method updates its uncommitted composition.</summary>
    /// <remarks>The text and Unicode-codepoint selection have already committed to <see cref="IMEGetText"/> and <see cref="IMEGetSelection"/>. Unknown negative native selection offsets become zero.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static event Action<string, Vector2i>? TextEditing
    {
        add => RequireService().TextEditingCore += value;
        remove => RequireService().TextEditingCore -= value;
    }

    /// <summary>Occurs when the operating system finishes dropping one or more files onto the main window.</summary>
    /// <remarks>Paths are copied from native event memory in arrival order. One completed drop produces one callback, even when it contains multiple files. The returned array belongs to the caller and remains valid after delivery.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static event Action<IReadOnlyList<string>>? FilesDropped
    {
        add => RequireService().FilesDroppedCore += value;
        remove => RequireService().FilesDroppedCore -= value;
    }

    /// <summary>Drains native events and commits typed keyboard, mouse, touch and controller state before game callbacks.</summary>
    /// <remarks>
    /// The host calls this on the opening thread before advancing each Engine frame. Each input event is owned and
    /// disposed by this server after synchronous delivery; handlers must duplicate an event they need to retain.
    /// Malformed native pointer and touch values are rejected before tracked button, contact or timestamp state changes.
    /// Controller connections update Input metadata before its connection event; device input uses borrowed typed events.
    /// Callback failures are collected while later queued events continue, then thrown together after the queue drains.
    /// Re-entry is rejected. No rendering or game frame is advanced here.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The server has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Called off the opening thread, re-entered, or called while the active main loop cannot accept input. Rejected calls leave the native queue untouched.</exception>
    /// <exception cref="AggregateException">One or more native pointer/touch values or game callbacks failed; later queued events still run.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void ProcessEvents() => RequireService().ProcessEventsCore();

    /// <summary>Processes native window and controller connection events while discarding pending keyboard, pointer, touch, text and controller input.</summary>
    /// <remarks>Tracked pressed state is released before draining the queue; window and quit callbacks still run.</remarks>
    /// <exception cref="InvalidOperationException">Called off the owner thread or re-entered.</exception>
    /// <exception cref="AggregateException">One or more delivered callbacks failed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void ForceProcessAndDropEvents() => RequireService().ForceProcessAndDropEventsCore();

    /// <summary>Maps a physical key to the logical key reported for the current keyboard layout.</summary>
    /// <param name="physical">A physical key identity.</param>
    /// <returns>The current logical key, or the supplied physical key when no layout mapping is available.</returns>
    /// <remarks>The result reflects the active keyboard layout and modifier state at the time of the call. Modifier bits in <paramref name="physical"/> are preserved. Reverse Tab is normalized to Tab, as in key events. On Wayland, SDL's keypad lookup does not distinguish Num Lock or shifted navigation symbols, so those keys can retain their physical identity instead of the current logical symbol.</remarks>
    /// <exception cref="ObjectDisposedException">The server is disposed.</exception>
    /// <exception cref="InvalidOperationException">The call is not on the opening thread.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Key KeyboardGetKeycodeFromPhysical(Key physical) => RequireService().KeyboardGetKeycodeFromPhysicalCore(physical);

    /// <summary>Maps a physical key to its localized label in the current keyboard layout.</summary>
    /// <param name="physical">A physical key identity.</param>
    /// <returns>The current label key, including a non-Latin Unicode scalar where applicable, or the supplied physical key when no layout mapping is available.</returns>
    /// <remarks>The label follows the current layout and modifier state without event-keycode remapping. Modifier bits in <paramref name="physical"/> are preserved; key events use the same label mapping with their event modifier state. On Wayland, SDL's keypad lookup does not expose Num Lock's printable label and can retain the physical keypad identity.</remarks>
    /// <exception cref="ObjectDisposedException">The server is disposed.</exception>
    /// <exception cref="InvalidOperationException">The call is not on the opening thread.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Key KeyboardGetLabelFromPhysical(Key physical) => RequireService().KeyboardGetLabelFromPhysicalCore(physical);

    /// <summary>Sets an icon specifically for the main window.</summary>
    /// <param name="image">A live nonempty source image.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>The image is copied and converted to eight-bit RGBA before native submission; the caller retains it. A successful call overrides subsequent <see cref="SetIcon"/> changes for this window. Wayland requires a square image and a compositor that supports window icons.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="image"/> is empty or is not square on Wayland.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="windowId"/> is not the main-window ID.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread or the native icon request fails.</exception>
    /// <exception cref="ObjectDisposedException">The display server or image has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void WindowSetIcon(Image image, int windowId = MainWindowId) => RequireService().WindowSetIconCore(image, windowId);

    /// <summary>Sets the default icon for the engine-owned main window unless it has an explicit icon.</summary>
    /// <param name="image">A live nonempty source image.</param>
    /// <remarks>The image is copied before native submission. A prior successful <see cref="WindowSetIcon"/> call keeps its window-specific icon. The current host owns only one window and does not install a desktop-launcher icon. Wayland requires a square image and a compositor that supports window icons.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="image"/> is empty or is not square on Wayland.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread or the native icon request fails.</exception>
    /// <exception cref="ObjectDisposedException">The display server or image has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void SetIcon(Image image) => RequireService().SetIconCore(image);

    /// <summary>Gets the current mouse mode.</summary>
    /// <returns>The mode owned by this display server.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static MouseMode MouseGetMode() => RequireService().MouseGetModeCore();

    /// <summary>Requests cursor visibility, capture, and confinement as one mode.</summary>
    /// <param name="mode">The mode to apply to the main window.</param>
    /// <remarks>
    /// Repeating the current mode does not resubmit native operations. Window managers may release grabs while the
    /// window lacks focus. If any native change fails, the prior mode is restored on a best-effort basis and the
    /// original native error is reported.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is unknown.</exception>
    /// <exception cref="InvalidOperationException">A native pointer-mode operation fails.</exception>
    /// <exception cref="AggregateException">Both a native pointer-mode operation and its rollback fail.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void MouseSetMode(MouseMode mode) => RequireService().MouseSetModeCore(mode);

    /// <summary>Gets the last reported mouse cursor position.</summary>
    /// <returns>Desktop coordinates on other desktop profiles; on Wayland, Android, iOS, tvOS and browsers, the last position in physical pixels relative to the main window.</returns>
    /// <remarks>Pixel-surface profiles do not use a global desktop pointer position. The window-relative SDL position is scaled to the physical client pixels used by <see cref="WindowGetSize(int)"/> and truncated toward zero.</remarks>
    /// <exception cref="InvalidOperationException">The native window pixel density cannot be read.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Vector2i MouseGetPosition() => RequireService().MouseGetPositionCore();

    /// <summary>Gets the mouse buttons currently reported as held by SDL.</summary>
    /// <returns>A mask of non-wheel buttons.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static MouseButtonMask MouseGetButtonState() => RequireService().MouseGetButtonStateCore();

    /// <summary>Requests a pointer move within the main window's client area when the backend supports warping.</summary>
    /// <param name="position">Target coordinates relative to the client area's upper-left corner in native client units.</param>
    /// <remarks>
    /// The request is available only when <see cref="HasFeature(Feature)"/> reports <see cref="Feature.MouseWarp"/>.
    /// On an advertised backend, the platform may still ignore movement under its input or remote-desktop policy.
    /// </remarks>
    /// <exception cref="NotSupportedException">Pointer warping is unavailable on the current backend.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void WarpMouse(Vector2i position) => RequireService().WarpMouseCore(position);

    /// <summary>Gets the last successfully selected standard pointer shape.</summary>
    /// <returns>The current server-owned shape.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static CursorShape CursorGetShape() => RequireService().CursorGetShapeCore();

    /// <summary>Selects a standard pointer shape from the native cursor theme.</summary>
    /// <param name="shape">Shape to apply.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="shape"/> is unknown.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void CursorSetShape(CursorShape shape) => RequireService().CursorSetShapeCore(shape);

    /// <summary>Sets or clears the image used for one pointer shape.</summary>
    /// <param name="image">A live Image or readable Texture to copy into a native cursor, or <see langword="null"/> to restore the system shape.</param>
    /// <param name="shape">The pointer shape slot to customize.</param>
    /// <param name="hotspot">The active point relative to the image's upper-left corner, truncated to a pixel on native submission.</param>
    /// <remarks>Pixels are copied before this method returns; the resource remains caller-owned. Texture.GetImage
    /// supplies a temporary owned image, which is disposed after conversion. Cursor dimensions and hotspots use
    /// this image's pixels, independently of the texture's logical size override. Images must be at most 256 by 256
    /// pixels. The default hotspot is the top-left pixel. Other cursor slots are unaffected. Later resource changes
    /// require another call to refresh the cursor.</remarks>
    /// <exception cref="ArgumentException">The resource is neither Image nor Texture, has no readable nonempty image, or exceeds the cursor size limit.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The shape or hotspot is invalid.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread or the native cursor operation fails.</exception>
    /// <exception cref="ObjectDisposedException">The display server, source resource or returned image is disposing or disposed.</exception>
    /// <exception cref="NotSupportedException">The image requires an unavailable pixel conversion, including decompression.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void CursorSetCustomImage(Resource? image, CursorShape shape = CursorShape.Arrow,
        Vector2 hotspot = default) => RequireService().CursorSetCustomImageCore(image, shape, hotspot);

    /// <summary>Gets the most recently received native IME composition text.</summary>
    /// <returns>The active composition, or an empty string before composition begins or after it commits.</returns>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static string IMEGetText() => RequireService().IMEGetTextCore();

    /// <summary>Gets the current composition selection.</summary>
    /// <returns>The zero-based Unicode-codepoint start and length reported by the input method. Unknown negative offsets become zero.</returns>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Vector2i IMEGetSelection() => RequireService().IMEGetSelectionCore();

    /// <summary>Enables or disables native text input for the main window.</summary>
    /// <param name="active">Whether to accept committed text and composition updates.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>The host should enable text input only while a text field owns focus.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The window ID is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is off the opening thread or native text input could not change state.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void WindowSetIMEActive(bool active, int windowId = MainWindowId) => RequireService().WindowSetIMEActiveCore(active, windowId);

    /// <summary>Moves the native IME candidate area to a window-local text caret.</summary>
    /// <param name="position">Caret position in client pixels on Wayland, or platform-native window coordinates elsewhere.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>On Wayland, the requested client-pixel position is converted to SDL logical window coordinates using the current pixel density. The native candidate area is one by ten window-coordinate units with a zero cursor offset. Text input must be active for a candidate popup to be shown.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The window ID is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="OverflowException">The pixel position cannot be represented in native window coordinates.</exception>
    /// <exception cref="InvalidOperationException">The caller is off the opening thread or the native density or area request fails.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void WindowSetIMEPosition(Vector2i position, int windowId = MainWindowId) => RequireService().WindowSetIMEPositionCore(position, windowId);

    /// <summary>Gets whether touch input is available from a device or mouse emulation.</summary>
    /// <returns><see langword="true"/> when a touch device is connected or mouse-to-touch emulation is enabled.</returns>
    /// <remarks>Reads the current <see cref="Input.EmulateTouchFromMouse"/> setting and native device list on the opening thread.</remarks>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static bool IsTouchscreenAvailable() => RequireService().IsTouchscreenAvailableCore();

    /// <summary>Gets the current screen index containing the main window.</summary>
    /// <param name="windowId">The window ID; only zero identifies an owned window.</param>
    /// <returns>The zero-based screen index or <see cref="InvalidScreen"/> when the window has no display.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static int WindowGetCurrentScreen(int windowId = MainWindowId) => RequireService().WindowGetCurrentScreenCore(windowId);

    /// <summary>Gets the index of the display with keyboard focus.</summary>
    /// <returns>The main window's screen when it has focus on a backend with global focus information; otherwise the primary screen.</returns>
    /// <remarks>Wayland does not expose a process-wide keyboard-focus window, so its result is always the primary screen.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static int GetKeyboardFocusScreen() => RequireService().GetKeyboardFocusScreenCore();

    /// <summary>Gets the display containing the largest portion of a desktop rectangle.</summary>
    /// <param name="rectangle">Desktop rectangle in platform-native coordinates.</param>
    /// <returns>The zero-based index of the screen with the greatest whole-pixel overlap area, or <see cref="InvalidScreen"/> when no overlap reaches one pixel.</returns>
    /// <remarks>On Wayland, SDL output origins may be in logical desktop coordinates while the public output sizes use physical pixels; cross-output selection can therefore be ambiguous at mixed scales.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static int GetScreenFromRect(Rect2 rectangle) => RequireService().GetScreenFromRectCore(rectangle);

    /// <summary>Gets a snapshot of the engine-owned native window IDs.</summary>
    /// <returns>A one-element snapshot containing <see cref="MainWindowId"/>.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static int[] GetWindowList() => RequireService().GetWindowListCore();

    /// <summary>Gets a borrowed operating-system or graphics-context handle for the main window.</summary>
    /// <param name="handleType">The native handle category to query.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>A nonzero pointer or platform window ID represented as a pointer-sized integer.</returns>
    /// <remarks>
    /// The caller does not own the returned handle and must never destroy or release it. Query it again after native
    /// window state changes; it is invalid after this server is disposed. Native interop with it must obey the
    /// platform's thread rules and this server's owner-thread boundary. Display handles are available on X11 and
    /// Wayland; window handles are available on X11, Wayland, Windows, and macOS. Graphics-context identities
    /// are available with the Linux compatibility renderer's GL/EGL/GLX driver as applicable. They are invalid
    /// after renderer shutdown. Querying does not change the current context. The caller must not destroy,
    /// replace or mutate the renderer's context or graphics state. GPU and software renderers have no GL identity.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="handleType"/> or <paramref name="windowId"/> is not defined or owned.</exception>
    /// <exception cref="NotSupportedException">The requested native handle is unavailable on the active video driver.</exception>
    /// <exception cref="InvalidOperationException">The call is made from another thread, or SDL cannot retrieve window properties or the native handle is currently absent.</exception>
    /// <exception cref="ObjectDisposedException">The display server has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static nint WindowGetNativeHandle(HandleType handleType, int windowId = MainWindowId) => RequireService().WindowGetNativeHandleCore(handleType, windowId);

    /// <summary>Finds the engine-owned window at a desktop position.</summary>
    /// <param name="position">Global desktop point in platform-native coordinates.</param>
    /// <returns><see cref="MainWindowId"/> when the point is inside the visible main window; otherwise <see cref="InvalidWindowId"/>.</returns>
    /// <remarks>On X11, only the client area is counted; the native border and title bar are excluded. Other desktop drivers use decorated bounds.</remarks>
    /// <exception cref="NotSupportedException">The active Wayland compositor does not expose a reliable global window position.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static int GetWindowAtScreenPosition(Vector2i position) => RequireService().GetWindowAtScreenPositionCore(position);

    /// <summary>Requests that the main window move to another connected display.</summary>
    /// <param name="screen">A zero-based display index or a negative display selector.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>A request for the current screen does nothing. A floating window retains its offset from the source display, clamped so part of it remains in the target work area. Fullscreen and maximized windows keep their mode. The window manager may apply or deny a move asynchronously.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="screen"/> or <paramref name="windowId"/> does not identify an available display or the main window.</exception>
    /// <exception cref="NotSupportedException">Wayland does not allow this top-level window to choose another display.</exception>
    /// <exception cref="InvalidOperationException">A native display query or move fails, or the window state fails to synchronize.</exception>
    /// <exception cref="AggregateException">A transfer fails and restoring the previous window mode also fails.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void WindowSetCurrentScreen(int screen, int windowId = MainWindowId) => RequireService().WindowSetCurrentScreenCore(screen, windowId);

    /// <summary>Gets the current screen refresh rate.</summary>
    /// <param name="screen">Display index or one of the negative display selectors; defaults to the main window's display.</param>
    /// <returns>The current display-mode refresh rate in hertz, using the precise rational value when available, or minus one when unavailable.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static float ScreenGetRefreshRate(int screen = ScreenOfMainWindow) => RequireService().ScreenGetRefreshRateCore(screen);

    /// <summary>Gets the largest reported content scale among connected displays.</summary>
    /// <returns>The maximum positive logical-to-physical ratio, at least one even when no display is connected.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static float ScreenGetMaxScale() => RequireService().ScreenGetMaxScaleCore();

    /// <summary>Gets the requested minimum size of the main window.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>The requested pixel dimensions on Wayland or native window dimensions elsewhere; zero means no bound on that axis.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Vector2i WindowGetMinSize(int windowId = MainWindowId) => RequireService().WindowGetMinSizeCore(windowId);

    /// <summary>Requests minimum dimensions for the main window.</summary>
    /// <param name="size">Nonnegative lower bounds in client pixels on Wayland and native window coordinates elsewhere; zero leaves that axis unbounded.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>Wayland rounds each pixel lower bound upward to native window coordinates and reapplies it when the window pixel density changes.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A bound is negative, exceeds a nonzero maximum, or cannot coexist with it after native pixel-density conversion.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void WindowSetMinSize(Vector2i size, int windowId = MainWindowId) => RequireService().WindowSetMinSizeCore(size, windowId);

    /// <summary>Gets the requested maximum size of the main window.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>The requested pixel dimensions on Wayland or native window dimensions elsewhere; zero means no bound on that axis.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Vector2i WindowGetMaxSize(int windowId = MainWindowId) => RequireService().WindowGetMaxSizeCore(windowId);

    /// <summary>Requests maximum dimensions for the main window.</summary>
    /// <param name="size">Nonnegative upper bounds in client pixels on Wayland and native window coordinates elsewhere; zero leaves that axis unbounded.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>Wayland rounds each pixel upper bound downward to native window coordinates and reapplies it when the window pixel density changes.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A bound is negative, falls below a nonzero minimum, or cannot represent a finite maximum after native pixel-density conversion.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void WindowSetMaxSize(Vector2i size, int windowId = MainWindowId) => RequireService().WindowSetMaxSizeCore(size, windowId);

    /// <summary>Gets the main window's current native mode.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>The state reported by SDL; an exclusive display mode is distinguished from desktop fullscreen. Wayland can retain a requested minimized state until the window regains focus.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static WindowMode WindowGetMode(int windowId = MainWindowId) => RequireService().WindowGetModeCore(windowId);

    /// <summary>Requests a native main-window mode.</summary>
    /// <param name="mode">Windowed, minimized, maximized, borderless fullscreen, or exclusive fullscreen.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>On Wayland, exclusive fullscreen uses the compositor's ordinary fullscreen request and is observed as <see cref="WindowMode.Fullscreen"/> if accepted. Wayland has no reliable programmatic restoration from a minimized state; the minimized flag can remain set until the window regains focus. Elsewhere exclusive mode chooses the closest native mode to the current logical window size. Mode transitions may complete asynchronously; call <see cref="WindowGetMode"/> for the reported state.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is unknown.</exception>
    /// <exception cref="InvalidOperationException">No suitable exclusive mode exists or the native window manager rejects a transition.</exception>
    /// <exception cref="AggregateException">A fullscreen transition and restoration of the previous mode both fail.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void WindowSetMode(WindowMode mode, int windowId = MainWindowId) => RequireService().WindowSetModeCore(mode, windowId);

    /// <summary>Gets whether the main window currently has native focus.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns><see langword="true"/> when the window is focused.</returns>
    /// <remarks>On Wayland, focus follows SDL's mouse-focused window; tablet and touch focus are not merged into this query. Other video drivers use keyboard focus.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static bool WindowIsFocused(int windowId = MainWindowId) => RequireService().WindowIsFocusedCore(windowId);

    /// <summary>Reads a supported native window flag.</summary>
    /// <param name="flag">Policy to inspect.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>Whether the policy is enabled in the observed native flags.</returns>
    /// <remarks>On Wayland, <see cref="WindowFlag.AlwaysOnTop"/> and <see cref="WindowFlag.NoFocus"/> are unavailable for the main window.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="flag"/> is not a defined policy.</exception>
    /// <exception cref="NotSupportedException">The defined policy requires a display capability that is not integrated.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static bool WindowGetFlag(WindowFlag flag, int windowId = MainWindowId) => RequireService().WindowGetFlagCore(flag, windowId);

    /// <summary>Requests a supported native window policy.</summary>
    /// <param name="flag">Policy to change.</param>
    /// <param name="enabled">Whether to enable the policy.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>A platform may reject the policy or apply it asynchronously. Wayland cannot apply <see cref="WindowFlag.AlwaysOnTop"/> to an ordinary top-level window, and SDL can change its internal focusable flag before rejecting <see cref="WindowFlag.NoFocus"/> there; both requests are rejected before native mutation.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="flag"/> is not a defined policy.</exception>
    /// <exception cref="NotSupportedException">The defined policy requires a display capability that is not integrated.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void WindowSetFlag(WindowFlag flag, bool enabled, int windowId = MainWindowId) => RequireService().WindowSetFlagCore(flag, enabled, windowId);

    /// <summary>Gets whether the current SDL resize policy permits a maximize request.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns><see langword="true"/> when the window is resizable, regardless of its current mode.</returns>
    /// <remarks>The Wayland compositor may independently disable maximization through its window-manager capabilities; SDL does not expose that native policy here. A fullscreen window can leave fullscreen before requesting maximization.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static bool WindowIsMaximizeAllowed(int windowId = MainWindowId) => RequireService().WindowIsMaximizeAllowedCore(windowId);

    /// <summary>Requests that the window manager bring the main window to the foreground.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>Wayland has no standard foreground request for an existing top-level window, so this is a no-op there. Other window managers may refuse a native raise request under their focus policy.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="windowId"/> does not identify the main window.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread or the native raise request fails.</exception>
    /// <exception cref="ObjectDisposedException">The display server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void WindowMoveToForeground(int windowId = MainWindowId) => RequireService().WindowMoveToForegroundCore(windowId);

    /// <summary>Requests user attention until the main window receives focus.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>On Wayland this submits an activation request without an input serial, allowing the compositor to mark the window urgent without switching focus. The platform chooses how, or whether, to display the request; a focused window may show no effect.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="windowId"/> does not identify the main window.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread or the native attention request fails.</exception>
    /// <exception cref="ObjectDisposedException">The display server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void WindowRequestAttention(int windowId = MainWindowId) => RequireService().WindowRequestAttentionCore(windowId);

    /// <summary>Gets the main-window position including its left and top decorations.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>The outer upper-left position in platform-native desktop coordinates.</returns>
    /// <exception cref="NotSupportedException">The active Wayland compositor does not expose a reliable global window position.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Vector2i WindowGetPositionWithDecorations(int windowId = MainWindowId) => RequireService().WindowGetPositionWithDecorationsCore(windowId);

    /// <summary>Gets the main-window size including native decorations.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>Outer window dimensions in platform-native window coordinates; on Wayland, the client size in pixels.</returns>
    /// <remarks>Wayland does not provide a reliable server-side decoration size for a top-level window.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Vector2i WindowGetSizeWithDecorations(int windowId = MainWindowId) => RequireService().WindowGetSizeWithDecorationsCore(windowId);

    /// <summary>Requests a taskbar progress state for the main window where the desktop supports it.</summary>
    /// <param name="state">The indication to show.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="state"/> is unknown.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="windowId"/> does not identify the main window.</exception>
    /// <exception cref="InvalidOperationException">The call is off the opening thread or a supported native backend rejects the request.</exception>
    /// <exception cref="NotSupportedException">Taskbar progress integration is unavailable on the active Wayland backend.</exception>
    /// <exception cref="ObjectDisposedException">The display server has been disposed.</exception>
    /// <remarks>Wayland needs a verified desktop entry and a desktop environment that accepts progress notifications. A successful native setter alone does not establish that the indication is visible.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void WindowSetTaskbarProgressState(ProgressState state, int windowId = MainWindowId) => RequireService().WindowSetTaskbarProgressStateCore(state, windowId);

    /// <summary>Requests a taskbar progress fraction for the main window where the desktop supports it.</summary>
    /// <param name="value">A finite fraction from zero through one.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is outside the supported range.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="windowId"/> does not identify the main window.</exception>
    /// <exception cref="InvalidOperationException">The call is off the opening thread or a supported native backend rejects the request.</exception>
    /// <exception cref="NotSupportedException">Taskbar progress integration is unavailable on the active Wayland backend.</exception>
    /// <exception cref="ObjectDisposedException">The display server has been disposed.</exception>
    /// <remarks>Wayland needs a verified desktop entry and a desktop environment that accepts progress notifications. A successful native setter alone does not establish that the value is visible.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void WindowSetTaskbarProgressValue(float value, int windowId = MainWindowId) => RequireService().WindowSetTaskbarProgressValueCore(value, windowId);

    /// <summary>Gets the display backend name.</summary>
    /// <returns>The active backend's public name, such as <c>Wayland</c> or <c>X11</c>.</returns>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread or the video driver is unavailable.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static string GetName() => RequireService().GetNameCore();

    /// <summary>Reports whether the operating system currently requests a dark appearance.</summary>
    /// <returns><see langword="true"/> only when the native system theme is dark.</returns>
    /// <remarks>On Linux Wayland and X11, the system Settings portal must be available. An unknown or unset preference returns <see langword="false"/> without classifying the theme as light. Query on the opening thread after processing native events to observe changes.</remarks>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static bool IsDarkMode() => RequireService().IsDarkModeCore();

    /// <summary>Reports whether the system can provide a light or dark appearance preference.</summary>
    /// <returns><see langword="true"/> on Linux Wayland or X11 when the desktop Settings portal is available, even if no appearance is preferred; on other drivers, when the native theme is known.</returns>
    /// <remarks>The Linux check queries the Settings portal when the server opens and requires interface version one or newer. A missing portal or native library returns <see langword="false"/>. On other drivers, an unknown theme returns <see langword="false"/>.</remarks>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static bool IsDarkModeSupported() => RequireService().IsDarkModeSupportedCore();

    /// <summary>Gets the current number of connected displays.</summary>
    /// <returns>The display count, which may change after hotplug events.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static int GetScreenCount() => RequireService().GetScreenCountCore();

    /// <summary>Reports whether hardware keyboard input is available.</summary>
    /// <returns>On Android and iOS, whether SDL detects a connected keyboard; on other targets, <see langword="true"/>.</returns>
    /// <remarks>The mobile result can change when devices are attached or removed. Query it on the opening thread.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static bool HasHardwareKeyboard() => RequireService().HasHardwareKeyboardCore();

    /// <summary>Gets whether the display is being kept awake by this process.</summary>
    /// <returns><see langword="true"/> while native screen blanking is disabled.</returns>
    /// <remarks>This reports SDL's process-wide screensaver setting, not an operating-system promise that the display stays on.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static bool ScreenIsKeptOn() => RequireService().ScreenIsKeptOnCore();

    /// <summary>Requests that the display remain awake or resume normal power-saving behavior.</summary>
    /// <param name="enable">Whether to inhibit native screen blanking.</param>
    /// <remarks>The request applies to this process and is automatically released when SDL video quits.</remarks>
    /// <exception cref="InvalidOperationException">The native video backend rejects the request.</exception>
    /// <exception cref="AggregateException">The native request fails and its previous state cannot be restored.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void ScreenSetKeepOn(bool enable) => RequireService().ScreenSetKeepOnCore(enable);

    /// <summary>Gets the index of the current primary display.</summary>
    /// <returns>The zero-based index or <see cref="InvalidScreen"/> when no display matches.</returns>
    /// <remarks>Wayland does not expose a primary display and uses index zero.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static int GetPrimaryScreen() => RequireService().GetPrimaryScreenCore();

    /// <summary>Gets the global desktop position of a display.</summary>
    /// <param name="screen">Display index or one of the negative display selectors; defaults to the main window's display.</param>
    /// <returns>The upper-left position in platform-native desktop coordinates, or zero if the display is invalid.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Vector2i ScreenGetPosition(int screen = ScreenOfMainWindow) => RequireService().ScreenGetPositionCore(screen);

    /// <summary>Gets the full size of a display.</summary>
    /// <param name="screen">Display index or one of the negative display selectors; defaults to the main window's display.</param>
    /// <returns>The display size in pixels on Wayland or platform-native desktop units elsewhere; zero if the display is invalid.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Vector2i ScreenGetSize(int screen = ScreenOfMainWindow) => RequireService().ScreenGetSizeCore(screen);

    /// <summary>Gets the usable desktop rectangle of a display.</summary>
    /// <param name="screen">Display index or one of the negative display selectors; defaults to the main window's display.</param>
    /// <returns>The work area after platform-reserved bars are excluded; empty if the display is invalid. On Wayland, the full display position and physical pixel size are returned.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Rect2i ScreenGetUsableRect(int screen = ScreenOfMainWindow) => RequireService().ScreenGetUsableRectCore(screen);

    /// <summary>Gets the content scale of a display or, on Wayland, the main window.</summary>
    /// <param name="screen">Display index or one of the negative display selectors; defaults to the main window's display.</param>
    /// <returns>A positive content scale, or one if the display is invalid. A Wayland display index reports an integer scale; the main-window selector can report a fractional scale.</returns>
    /// <remarks>On Wayland, indexed scales round the current display mode's pixel density upward. The main-window selector reads its current window scale. Before the compositor assigns a preferred fractional scale to a newly created or hidden window, that value can be provisional; it can change after the first buffer is presented or the window moves to another output. Call this method on the opening thread.</remarks>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static float ScreenGetScale(int screen = ScreenOfMainWindow) => RequireService().ScreenGetScaleCore(screen);

    /// <summary>Requests a new main-window title.</summary>
    /// <param name="title">New UTF-8 title.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <exception cref="ArgumentNullException"><paramref name="title"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="windowId"/> does not identify the main window.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread or the native title request fails.</exception>
    /// <exception cref="ObjectDisposedException">The display server has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void WindowSetTitle(string title, int windowId = MainWindowId) => RequireService().WindowSetTitleCore(title, windowId);

    /// <summary>Gets the main window's client size.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>Pixel dimensions on Wayland, Android, iOS, tvOS and browsers; platform-native window dimensions elsewhere.</returns>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Vector2i WindowGetSize(int windowId = MainWindowId) => RequireService().WindowGetSizeCore(windowId);

    /// <summary>Requests a new main-window client size.</summary>
    /// <param name="size">Requested pixel dimensions on Wayland, Android, iOS, tvOS and browsers; platform-native window dimensions elsewhere.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <remarks>Wayland clamps each requested component below one to one before applying native minimum-size constraints. Other video drivers require positive dimensions.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is not positive on a non-Wayland video driver.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void WindowSetSize(Vector2i size, int windowId = MainWindowId) => RequireService().WindowSetSizeCore(size, windowId);

    /// <summary>Gets the main window's global desktop position.</summary>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <returns>The upper-left position in platform-native desktop coordinates.</returns>
    /// <exception cref="NotSupportedException">The active Wayland compositor does not expose a reliable global window position.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static Vector2i WindowGetPosition(int windowId = MainWindowId) => RequireService().WindowGetPositionCore(windowId);

    /// <summary>Requests a global desktop position for the main window.</summary>
    /// <param name="position">Upper-left position in platform-native desktop coordinates.</param>
    /// <param name="windowId">The main-window ID, zero.</param>
    /// <exception cref="NotSupportedException">Wayland does not allow this top-level window to choose its desktop position.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void WindowSetPosition(Vector2i position, int windowId = MainWindowId) => RequireService().WindowSetPositionCore(position, windowId);

    /// <summary>Gets whether the system clipboard currently contains nonempty text.</summary>
    /// <returns><see langword="true"/> when <see cref="ClipboardGet"/> returns nonempty text.</returns>
    /// <remarks>Reads the clipboard on the opening thread. An unavailable text selection returns <see langword="false"/>.</remarks>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static bool ClipboardHas() => RequireService().ClipboardHasCore();

    /// <summary>Gets text from the system clipboard.</summary>
    /// <returns>The current text or an empty string when no text is available.</returns>
    /// <remarks>Reads the system clipboard on the opening thread. Clipboard and primary-selection text are separate. On Wayland, the compositor sends a clipboard offer to a client with keyboard focus; a hidden or unfocused client may receive no text.</remarks>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static string ClipboardGet() => RequireService().ClipboardGetCore();

    /// <summary>Replaces system clipboard text.</summary>
    /// <param name="text">Text to place on the clipboard.</param>
    /// <remarks>Requests replacement of the system clipboard on the opening thread. This does not change the primary selection. On Wayland, the compositor may require a recent input event before it accepts clipboard ownership; a successful native call alone does not prove another process can read the new text.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread or the native backend rejects the replacement.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void ClipboardSet(string text) => RequireService().ClipboardSetCore(text);

    /// <summary>Gets text from the platform's primary selection.</summary>
    /// <returns>The selected text, or an empty string when the backend has no primary selection.</returns>
    /// <remarks>Primary selection is separate from the system clipboard and is available on Linux/Wayland. Read it on the opening thread. Wayland sends the selection offer only to a client with keyboard focus; a hidden or unfocused client may receive no text. Platforms without it may report an empty value.</remarks>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static string ClipboardGetPrimary() => RequireService().ClipboardGetPrimaryCore();

    /// <summary>Replaces text in the platform's primary selection.</summary>
    /// <param name="text">The selection text.</param>
    /// <remarks>Requests replacement of the separate primary selection on the opening thread. The system clipboard is unchanged. On Wayland, the compositor may require a recent input event before it accepts selection ownership; a successful native call alone does not prove another process can read the new text.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">The server is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the opening thread or the native backend rejects the selection.</exception>
    /// <exception cref="InvalidOperationException">The native service is not active.</exception>
    public static void ClipboardSetPrimary(string text) => RequireService().ClipboardSetPrimaryCore(text);

}
