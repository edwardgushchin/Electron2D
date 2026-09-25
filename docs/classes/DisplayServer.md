# DisplayServer

Last updated: 2026-09-25

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

**Source:** `src/Servers/Display/DisplayServer*.cs`

**Declaration:** `public sealed partial class DisplayServer : ElectronObject`

Owns one native SDL video connection and its main window.

## Description

`Open` creates the sole process display server and a resizable high-density main window with a 64×64 minimum size in client pixels on Wayland or native window units on other desktop backends. Android uses its fullscreen surface size and does not set a minimum. A visible Wayland window presents a neutral blank surface so the compositor maps it before scene rendering exists. The caller owns it and must dispose it on the opening SDL main thread. Screen indexes are a live snapshot and may change after hotplug; negative selector constants choose the main-window, primary, keyboard-focus, or mouse-focus screen. Wayland keyboard-focus screen queries resolve to the primary screen because the protocol does not expose process-wide keyboard focus. Invalid screen queries return documented fallback values. Only window ID 0 is owned. Window and screen positions use SDL's platform-native desktop coordinates; macOS may use logical points. Wayland window size, bounds, and pointer coordinates use client pixels, while the implementation converts size requests to SDL's logical units. Wayland has no reliable global top-level window placement, so position and hit-testing methods reject that driver. Window-manager requests may be asynchronous or denied. Native failures throw `InvalidOperationException`; invalid IDs and values normally throw argument exceptions, except `WindowGetCurrentScreen` returns `InvalidScreen` for an unknown ID. Defined but unavailable window flags throw `NotSupportedException`. Exclusive fullscreen selects a native display mode where supported and can fail when no suitable mode is available; Wayland requests ordinary compositor fullscreen instead. `HasFeature` reports only capabilities integrated for the active driver.

`ProcessEvents` must be called by the host before `Engine` advances a frame. It commits typed keyboard, pointer, wheel, and touch events through `Input` before synchronous scene callbacks. Both axes of one wheel event are attempted even when the first axis callback fails; failures from both axes are retained. Callback failures are collected while later native events continue, then an `AggregateException` is thrown. The pump rejects re-entry and disposal during delivery. Input events are disposed after synchronous dispatch; consumers duplicate one they need to retain. Focus loss sends the window callback, then the application focus notification, then releases pressed state even if either callback throws; it also cancels tracked contacts.

`IsDarkMode` reads the current native light/dark theme on the opening thread. On Linux Wayland and X11, `IsDarkModeSupported` instead reports whether the desktop Settings portal supports appearance queries, even when the current preference is unset; this capability is checked when the server opens. Other drivers report support when SDL identifies a light or dark theme. `SystemThemeChanged` delivers the native theme-change notification through the same owner-thread event pump and callback-failure handling as other native events. Call `IsDarkMode` again after a notification to observe the current preference.

`WindowDpiChanged` forwards a native change of the main window's display content scale. It carries no numeric value and does not measure physical dots per inch. Query `ScreenGetScale()` with its default main-window selector after delivery; on Wayland an indexed screen scale is the separate integer output scale and can differ from the window's fractional scale.

`WindowRectChanged` delivers a complete `Rect2i` for the main window's client area after a native move or size change. Its position uses the driver's desktop coordinates; on Wayland the position is conventionally `(0, 0)` because a reliable global top-level position is unavailable. Its size uses client pixels on Wayland and native window units on other drivers. Each callback receives the rectangle as it stood at that event in native queue order; duplicate rectangles and events for other windows are ignored. The Wayland convention does not make the global-position query or setter available.

Native key events carry independent logical, physical, and unmodified layout-label identities. Left/right control, shift, alt, and GUI scancodes set the corresponding `InputEventKey.Location`; all other scancodes use `Unspecified`. The native adapter leaves `InputEventKey.Unicode` at zero while committed text is delivered separately. SDL key events contain no produced text scalar; text-input events may contain multiple scalars or an IME commit without identifying a corresponding key press. Populating native key-event Unicode requires a per-key Unicode source with verified IME/composition semantics in the first native keyboard/text adapter slice.

Native message dialogs block the owner thread and return a typed button index through a callback. Native file choosers return asynchronously, possibly from an SDL worker thread; `ProcessEvents` delivers copied paths to game callbacks on the owner thread. A server with a pending chooser or an undelivered chooser result cannot be disposed; keep pumping until its callback runs. A dropped group of files produces one ordered `FilesDropped` list when SDL reports completion; interrupted groups are discarded. Wayland does not expose a reliable global desktop pointer position, so `MouseGetPosition` reads the last window-relative SDL pointer state; the mouse-focus screen selector uses index zero.

The active server is a process singleton, not an `Engine` owned object. `ElectronObject` supplies identity, disposal state, numeric notifications, translation, and typed property discovery; see its [complete inherited reference](ElectronObject.md). Native SDL handles are private and released deterministically. Other threads may read `Instance`; all instance methods, event pumping, and disposal require the opening thread. Opening a server does not start a loop or renderer; the user-facing host example supplies a loop and Linux native packaging.

## Examples

```csharp
using Electron2D;

using var display = DisplayServer.Open("My game", new Vector2i(800, 600));
using var tree = new SceneTree(new Node());
Engine.Instance.Start(tree);
try
{
    display.ProcessEvents();
    Engine.Instance.AdvanceFrame(0);
}
finally
{
    Engine.Instance.Stop();
}
```

The first executable example supplies elapsed time, runs this cycle until exit, and leaves presentation for the rendering slice.

To request a native file, keep pumping while the chooser is open (the snippet uses the `display` variable from the preceding example):

```csharp
display.FileDialogShow("Open image", "", "", false,
    DisplayServer.FileDialogMode.OpenFile,
    ["*.png;PNG images"],
    (accepted, paths, filterIndex) =>
    {
        if (accepted)
            Console.WriteLine(paths[0]);
    });
```

## API Summary

### Constants

| Signature | Contract |
| --- | --- |
| [`public const int MainWindowId = 0`](#constant-mainwindowid) | Identifies the main window. |
| [`public const int InvalidWindowId = -1`](#constant-invalidwindowid) | Identifies a window that does not exist. |
| [`public const int InvalidScreen = -1`](#constant-invalidscreen) | Identifies a display that does not exist. |
| [`public const int ScreenWithMouseFocus = -4`](#constant-screenwithmousefocus) | Selects the display containing the mouse pointer. |
| [`public const int ScreenWithKeyboardFocus = -3`](#constant-screenwithkeyboardfocus) | Selects the keyboard-focused main window's display, or the primary screen on Wayland and when focus is absent. |
| [`public const int ScreenPrimary = -2`](#constant-screenprimary) | Selects the primary display. |
| [`public const int ScreenOfMainWindow = -1`](#constant-screenofmainwindow) | Selects the display containing the main window. |

### Properties

| Signature | Contract |
| --- | --- |
| [`public static DisplayServer? Instance { get; }`](#property-instance) | Gets the active display server, if one has been opened. |

### Methods

| Signature | Contract |
| --- | --- |
| [`public bool HasFeature(Feature feature)`](#method-hasfeature) | Reports whether the current backend exposes an executable capability. |
| [`public void ProcessEvents()`](#method-processevents) | Drains native events and commits typed keyboard, mouse, touch and controller state before game callbacks. |
| [`public void ForceProcessAndDropEvents()`](#method-forceprocessanddropevents) | Processes native window events while discarding pending keyboard, pointer, touch, and text input. |
| [`public void DialogShow(string title, string description, IReadOnlyList<string> buttons, Action<int> callback)`](#method-dialogshow) | Shows a blocking native message dialog. |
| [`public void FileDialogShow(string title, string currentDirectory, string filename, bool showHidden, FileDialogMode mode, IReadOnlyList<string> filters, Action<bool, IReadOnlyList<string>, int> callback, int parentWindowId = MainWindowId)`](#method-filedialogshow) | Opens an asynchronous native file or folder chooser. |
| [`public Key KeyboardGetKeycodeFromPhysical(Key physical)`](#method-keyboardgetkeycodefromphysical) | Maps a physical key to the logical key reported for the current keyboard layout. |
| [`public Key KeyboardGetLabelFromPhysical(Key physical)`](#method-keyboardgetlabelfromphysical) | Maps a physical key to its localized label in the current keyboard layout. |
| [`public void WindowSetIcon(Image image, int windowId = MainWindowId)`](#method-windowseticon) | Sets an icon specifically for the main window. |
| [`public void SetIcon(Image image)`](#method-seticon) | Sets the application-default icon while the main window has no override. |
| [`public MouseMode MouseGetMode()`](#method-mousegetmode) | Gets the last successfully requested mouse mode. |
| [`public void MouseSetMode(MouseMode mode)`](#method-mousesetmode) | Requests cursor visibility, capture, and confinement as one mode. |
| [`public Vector2i MouseGetPosition()`](#method-mousegetposition) | Gets desktop pointer coordinates or the window-relative position on Wayland. |
| [`public MouseButtonMask MouseGetButtonState()`](#method-mousegetbuttonstate) | Gets the mouse buttons currently reported as held by SDL. |
| [`public void WarpMouse(Vector2i position)`](#method-warpmouse) | Requests a client-area pointer move when the backend supports warping. |
| [`public CursorShape CursorGetShape()`](#method-cursorgetshape) | Gets the last successfully selected standard pointer shape. |
| [`public void CursorSetShape(CursorShape shape)`](#method-cursorsetshape) | Selects a standard pointer shape from the native cursor theme. |
| [`public void CursorSetCustomImage(Resource? image, CursorShape shape = CursorShape.Arrow, Vector2 hotspot = default)`](#method-cursorsetcustomimage) | Sets or clears the image used for one pointer shape. |
| [`public string IMEGetText()`](#method-imegettext) | Gets the most recently received native IME composition text. |
| [`public Vector2i IMEGetSelection()`](#method-imegetselection) | Gets the current composition selection. |
| [`public void WindowSetIMEActive(bool active, int windowId = MainWindowId)`](#method-windowsetimeactive) | Enables or disables native text input for the main window. |
| [`public void WindowSetIMEPosition(Vector2i position, int windowId = MainWindowId)`](#method-windowsetimeposition) | Moves the native IME candidate area to a window-local text caret. |
| [`public bool IsTouchscreenAvailable()`](#method-istouchscreenavailable) | Gets whether touch input is available from a device or mouse emulation. |
| [`public int WindowGetCurrentScreen(int windowId = MainWindowId)`](#method-windowgetcurrentscreen) | Gets the current screen index containing the main window. |
| [`public int GetKeyboardFocusScreen()`](#method-getkeyboardfocusscreen) | Gets the index of the display with keyboard focus. |
| [`public int GetScreenFromRect(Rect2 rectangle)`](#method-getscreenfromrect) | Gets the display containing the largest portion of a desktop rectangle. |
| [`public int[] GetWindowList()`](#method-getwindowlist) | Gets a snapshot of the engine-owned native window IDs. |
| [`public nint WindowGetNativeHandle(HandleType handleType, int windowId = MainWindowId)`](#method-windowgetnativehandle) | Gets a borrowed operating-system display, window or graphics-context identity. |
| [`public int GetWindowAtScreenPosition(Vector2i position)`](#method-getwindowatscreenposition) | Finds the engine-owned window at a desktop position. |
| [`public void WindowSetCurrentScreen(int screen, int windowId = MainWindowId)`](#method-windowsetcurrentscreen) | Requests that the main window move to another connected display. |
| [`public float ScreenGetRefreshRate(int screen = ScreenOfMainWindow)`](#method-screengetrefreshrate) | Gets the current mode's refresh rate in hertz, or `-1` when unavailable. |
| [`public float ScreenGetMaxScale()`](#method-screengetmaxscale) | Gets the largest reported content scale among connected displays. |
| [`public Vector2i WindowGetMinSize(int windowId = MainWindowId)`](#method-windowgetminsize) | Gets the requested minimum client size. |
| [`public void WindowSetMinSize(Vector2i size, int windowId = MainWindowId)`](#method-windowsetminsize) | Requests minimum client dimensions. |
| [`public Vector2i WindowGetMaxSize(int windowId = MainWindowId)`](#method-windowgetmaxsize) | Gets the requested maximum client size. |
| [`public void WindowSetMaxSize(Vector2i size, int windowId = MainWindowId)`](#method-windowsetmaxsize) | Requests maximum client dimensions. |
| [`public WindowMode WindowGetMode(int windowId = MainWindowId)`](#method-windowgetmode) | Gets the main window's current native mode. |
| [`public void WindowSetMode(WindowMode mode, int windowId = MainWindowId)`](#method-windowsetmode) | Requests a native main-window mode. |
| [`public bool WindowIsFocused(int windowId = MainWindowId)`](#method-windowisfocused) | Gets whether the main window currently has keyboard focus. |
| [`public bool WindowGetFlag(WindowFlag flag, int windowId = MainWindowId)`](#method-windowgetflag) | Reads a supported native window flag. |
| [`public void WindowSetFlag(WindowFlag flag, bool enabled, int windowId = MainWindowId)`](#method-windowsetflag) | Requests a supported native window policy. |
| [`public bool WindowIsMaximizeAllowed(int windowId = MainWindowId)`](#method-windowismaximizeallowed) | Gets whether the current SDL resize policy permits a maximize request. |
| [`public void WindowMoveToForeground(int windowId = MainWindowId)`](#method-windowmovetoforeground) | Requests native foreground on supported drivers; does nothing on Wayland. |
| [`public void WindowRequestAttention(int windowId = MainWindowId)`](#method-windowrequestattention) | Requests user attention until the main window receives focus. |
| [`public Vector2i WindowGetPositionWithDecorations(int windowId = MainWindowId)`](#method-windowgetpositionwithdecorations) | Gets the main-window position including its left and top decorations. |
| [`public Vector2i WindowGetSizeWithDecorations(int windowId = MainWindowId)`](#method-windowgetsizewithdecorations) | Gets the main-window size including native decorations. |
| [`public void WindowSetTaskbarProgressState(ProgressState state, int windowId = MainWindowId)`](#method-windowsettaskbarprogressstate) | Requests a taskbar progress state where the desktop supports it. |
| [`public void WindowSetTaskbarProgressValue(float value, int windowId = MainWindowId)`](#method-windowsettaskbarprogressvalue) | Requests a taskbar progress fraction where the desktop supports it. |
| [`public static DisplayServer Open(string title, Vector2i size, bool hidden = false)`](#method-open) | Opens the native video subsystem and creates the main window. |
| [`public string GetName()`](#method-getname) | Gets the current display backend name. |
| [`public int GetScreenCount()`](#method-getscreencount) | Gets the current number of connected displays. |
| [`public bool IsDarkMode()`](#method-isdarkmode) | Reports whether the current native system theme is dark. |
| [`public bool IsDarkModeSupported()`](#method-isdarkmodesupported) | Reports Linux Wayland/X11 Settings portal support, or a known native theme on other drivers. |
| [`public bool HasHardwareKeyboard()`](#method-hashardwarekeyboard) | Reports whether a physical keyboard is connected. |
| [`public bool ScreenIsKeptOn()`](#method-screeniskepton) | Reports whether native screen blanking is disabled. |
| [`public void ScreenSetKeepOn(bool enable)`](#method-screensetkeepon) | Requests screen wakefulness for this process. |
| [`public int GetPrimaryScreen()`](#method-getprimaryscreen) | Gets the index of the current primary display. |
| [`public Vector2i ScreenGetPosition(int screen = ScreenOfMainWindow)`](#method-screengetposition) | Gets the global desktop position of a display. |
| [`public Vector2i ScreenGetSize(int screen = ScreenOfMainWindow)`](#method-screengetsize) | Gets the full size of a display. |
| [`public Rect2i ScreenGetUsableRect(int screen = ScreenOfMainWindow)`](#method-screengetusablerect) | Gets the usable desktop rectangle of a display. |
| [`public float ScreenGetScale(int screen = ScreenOfMainWindow)`](#method-screengetscale) | Gets the display content scale reported by the native video system. |
| [`public void WindowSetTitle(string title, int windowId = MainWindowId)`](#method-windowsettitle) | Requests a new main-window title. |
| [`public Vector2i WindowGetSize(int windowId = MainWindowId)`](#method-windowgetsize) | Gets the main window's client size. |
| [`public void WindowSetSize(Vector2i size, int windowId = MainWindowId)`](#method-windowsetsize) | Requests a new main-window client size. |
| [`public Vector2i WindowGetPosition(int windowId = MainWindowId)`](#method-windowgetposition) | Gets the main window's global desktop position. |
| [`public void WindowSetPosition(Vector2i position, int windowId = MainWindowId)`](#method-windowsetposition) | Requests a global desktop position for the main window. |
| [`public bool ClipboardHas()`](#method-clipboardhas) | Gets whether the system clipboard currently contains text. |
| [`public string ClipboardGet()`](#method-clipboardget) | Gets text from the system clipboard. |
| [`public void ClipboardSet(string text)`](#method-clipboardset) | Replaces system clipboard text. |
| [`public string ClipboardGetPrimary()`](#method-clipboardgetprimary) | Gets text from the platform's primary selection. |
| [`public void ClipboardSetPrimary(string text)`](#method-clipboardsetprimary) | Replaces text in the platform's primary selection. |

### Events

| Signature | Contract |
| --- | --- |
| [`public event Action? QuitRequested`](#event-quitrequested) | Occurs when the operating system requests that the application quit. |
| [`public event Action? SystemThemeChanged`](#event-systemthemechanged) | Occurs when SDL reports a native system-theme change. |
| [`public event Action? CloseRequested`](#event-closerequested) | Occurs when the main window receives a close request. |
| [`public event Action? WindowMouseEntered`](#event-windowmouseentered) | Occurs when the pointer enters the main window. |
| [`public event Action? WindowMouseExited`](#event-windowmouseexited) | Occurs when the pointer leaves the main window. |
| [`public event Action? WindowDpiChanged`](#event-windowdpichanged) | Occurs when the main window's native display content scale changes. |
| [`public event Action<Rect2i>? WindowRectChanged`](#event-windowrectchanged) | Occurs after a change to the main window's complete client rectangle. |
| [`public event Action<bool>? WindowFocusChanged`](#event-windowfocuschanged) | Occurs when the main window gains or loses keyboard focus. |
| [`public event Action<string>? TextInput`](#event-textinput) | Occurs when the platform commits text input, including text composed through an IME. |
| [`public event Action<string, Vector2i>? TextEditing`](#event-textediting) | Occurs after the native input method updates its uncommitted composition. |
| [`public event Action<IReadOnlyList<string>>? FilesDropped`](#event-filesdropped) | Occurs once for a completed ordered group of dropped files. |

### Enumerations

| Signature | Contract |
| --- | --- |
| [`public enum Feature`](#enum-feature) | Identifies a display-server capability for `HasFeature`. |
| [`public enum HandleType`](#enum-handletype) | Selects a borrowed native display or window identity; see the [enum reference](DisplayServer.HandleType.md). |
| [`public enum FileDialogMode`](#enum-filedialogmode) | Selects a native file chooser mode; see the [enum reference](DisplayServer.FileDialogMode.md). |
| [`public enum MouseMode`](#enum-mousemode) | Defines cursor visibility and window confinement. |
| [`public enum CursorShape`](#enum-cursorshape) | Identifies standard pointer shapes supported by the native cursor theme. |
| [`public enum WindowFlag`](#enum-windowflag) | Selects a main-window policy by its stable display-server ID. |
| [`public enum WindowMode`](#enum-windowmode) | Identifies the current presentation state of a native window. |
| [`public enum ProgressState`](#enum-progressstate) | Identifies a native taskbar progress indication. |

### Protected extension points

| Signature | Contract |
| --- | --- |
| [`protected override void ValidateDisposal()`](#protected-validatedisposal) | Rejects off-thread or in-pump disposal before the lifetime transition. |
| [`protected override void Dispose(bool disposing)`](#protected-dispose) | Releases native cursor/window/video ownership, then invokes base cleanup. |

## Member Descriptions

### Property Descriptions

<a id="property-instance"></a>
#### `public static DisplayServer? Instance { get; }`

Gets the active display server, if one has been opened.

**Value:** The active instance or `null` after disposal.

**Source:** `src/Servers/Display/DisplayServer.cs`.

### Method Descriptions

<a id="method-dialogshow"></a>
#### `public void DialogShow(string title, string description, IReadOnlyList<string> buttons, Action<int> callback)`

Shows an operating-system message box with the requested title, body text, and ordered button labels. The native call blocks the opening thread until a user chooses a button or closes the dialog. The callback runs on that thread before the method returns and receives the native zero-based button index. Dismissal without a button is reported according to the native toolkit; the verified Wayland host reported `0` when a one-button dialog was closed with its title-bar button, so callers cannot distinguish that action from choosing button zero. A callback exception propagates after native dialog resources are released.

**Errors:** Null arguments throw `ArgumentNullException`; zero buttons or an empty label throws `ArgumentException`; a disposed server throws `ObjectDisposedException`; an off-thread call or native failure throws `InvalidOperationException`.

**Source:** `src/Servers/Display/DisplayServer.Dialogs.cs`.

<a id="method-filedialogshow"></a>
#### `public void FileDialogShow(string title, string currentDirectory, string filename, bool showHidden, FileDialogMode mode, IReadOnlyList<string> filters, Action<bool, IReadOnlyList<string>, int> callback, int parentWindowId = MainWindowId)`

Opens an OS file or folder chooser. `OpenFile`, `OpenFiles`, `OpenDirectory`, and `SaveFile` select the corresponding native picker; `OpenAny` is explicitly unsupported. `currentDirectory` requests the initial location. On Linux, `filename` is used only for `SaveFile`, and `showHidden` is accepted but ignored by the native chooser. On other platforms SDL can combine `currentDirectory` and `filename` for file modes. The platform may ignore the title or initial location. `filters` use entries such as `*.png,*.jpg;Images;image/png,image/jpeg`; extension patterns are applied, the optional MIME section is ignored, and an all-files entry may be `*` or `*.*`. A valid MIME-only entry such as `;Images;image/png` throws `NotSupportedException` before native UI opens. Folder selection ignores filters and `filename`. The native OS may ignore filters or the selected-filter index.

The native result may arrive on another thread. File names are copied from SDL-owned callback memory immediately, then the callback runs on the opening thread during a later `ProcessEvents` call. Success passes `true`, an ordered path snapshot, and the filter index; cancellation passes `false` and an empty list. A native failure also calls back with `false` and an empty list, then reports `InvalidOperationException` through the event pump aggregate. Callback exceptions are aggregated after later queued dialog callbacks. The native filter array remains pinned until completion; `Dispose` rejects a pending chooser or one whose result has not yet been pumped. Android may return content URIs that ordinary directory-backed `FileAccess` cannot yet resolve.

**Errors:** Null arguments throw `ArgumentNullException`; malformed file-mode filters throw `ArgumentException`; an unknown mode or window ID throws `ArgumentOutOfRangeException`; `OpenAny` and MIME-only file-mode filters throw `NotSupportedException`; a disposed server throws `ObjectDisposedException`; an off-thread call or native launch failure throws `InvalidOperationException`.

**Source:** `src/Servers/Display/DisplayServer.Dialogs.cs`.

<a id="method-hashardwarekeyboard"></a>
#### `public bool HasHardwareKeyboard()`

On Android and iOS, reports the native probe for a connected keyboard; on other targets reports `true`, matching the desktop input contract. The mobile result may change as devices are attached or removed. Call on the opening thread.

**Returns:** Mobile keyboard presence or `true` on other targets.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-isdarkmode"></a>
#### `public bool IsDarkMode()`

Reads the current native system theme on the opening thread; it does not cache the last event. A dark result returns `true`; light and unknown results return `false`. On Linux Wayland and X11, an available Settings portal is also required. Query again after `SystemThemeChanged` to observe the current theme.

**Returns:** `true` only when SDL currently reports a dark system theme.

**Errors:** `InvalidOperationException` off the opening thread; `ObjectDisposedException` while disposing or after disposal.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-isdarkmodesupported"></a>
#### `public bool IsDarkModeSupported()`

On Linux Wayland and X11, returns whether the desktop Settings portal supports appearance queries, independently of whether the user has chosen dark, light, or no preference. This capability is queried once when the server opens through the session D-Bus and requires Settings interface version one or newer. An unavailable portal, session bus, or native D-Bus library yields `false`; reopen the server to refresh that capability. On other drivers, reports whether SDL currently identifies the theme as light or dark, so an unknown theme yields `false`. Call on the opening thread.

**Returns:** On Linux Wayland and X11, `true` for a supported Settings portal; on other drivers, `true` for a known light or dark theme.

**Errors:** `InvalidOperationException` off the opening thread; `ObjectDisposedException` while disposing or after disposal.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-screeniskepton"></a>
#### `public bool ScreenIsKeptOn()`

Reports whether this process has disabled native screen blanking. This is a process-wide setting and does not guarantee that an operating-system power policy will keep a display on. Call on the opening thread.

On the verified Linux Wayland host, the returned request state corresponded to `org.freedesktop.ScreenSaver` inhibition and release messages from the process.

**Returns:** `true` while the native screen saver is disabled.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-screensetkeepon"></a>
#### `public void ScreenSetKeepOn(bool enable)`

Requests a process-wide screen-blanking policy. `enable: true` inhibits blanking; `false` permits normal power saving. The native subsystem releases the inhibition on shutdown. Call on the opening thread.

The Linux Wayland native test observed corresponding `UnInhibit`, `Inhibit`, and `UnInhibit` calls on the session bus while changing this state. Compositor power policies can still override the request.

**Errors:** `InvalidOperationException` if the native backend rejects the request.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-hasfeature"></a>
#### `public bool HasFeature(Feature feature)`

Reports whether the current backend advertises an integrated display capability.

**Returns:** `true` for an integrated capability advertised for the current native driver or device state.

- `feature`: The display capability to query.

**Remarks:** A false result may mean that the platform offers a service that this engine has not integrated. Clipboard access is available while open; mouse and touchscreen results follow connected devices, with touchscreen also available when mouse-to-touch emulation is enabled. Pointer warp, cursor shapes, content scale, icon changes, primary selection, and input-method composition follow the native driver rules in the [Feature reference](DisplayServer.Feature.md). Other defined values and unknown numeric IDs return false. A positive `Ime` or `NativeDialogFile` result on Wayland currently identifies an integrated SDL path, not a proven available text-input protocol or file chooser. Native operations can still fail after a positive query. The first native keyboard/text adapter and Linux FileChooser bridge must supply non-UI capability probes before this query is complete.

**Source:** `src/Servers/Display/DisplayServer.Capabilities.cs`.

<a id="method-processevents"></a>
#### `public void ProcessEvents()`

Drains native events and commits typed keyboard, mouse, touch and controller state before game callbacks.

SDL joystick/gamepad additions and removals update [Input](Input.md) metadata before its connection callback. Standardized gamepad and raw joystick button/axis events enter the same typed action and SceneTree path. Already connected devices are available before the first pump and announced there. `ForceProcessAndDropEvents` drops controller input while retaining connection changes.

**Remarks:** The host calls this on the opening thread before advancing each Engine frame. Each input event is owned and disposed by this server after synchronous delivery; handlers must duplicate an event they need to retain. A key event excludes the pressed or released modifier key's own bit while preserving other held modifier bits. Malformed native pointer and touch values are rejected before tracked button, contact or timestamp state changes. Failures are collected while later queued events continue, then thrown together after the queue drains. Re-entry is rejected. No rendering or game frame is advanced here.

**Errors:** `ObjectDisposedException` if the server is disposed; `InvalidOperationException` if called off the opening thread, re-entered, or the active main loop cannot accept input; `AggregateException` after malformed native pointer/touch input or callbacks fail. Preflight-rejected calls leave the native queue untouched.

**Source:** `src/Servers/Display/DisplayServer.Events.cs`.

<a id="method-forceprocessanddropevents"></a>
#### `public void ForceProcessAndDropEvents()`

Processes native window events while discarding pending keyboard, pointer, touch, and text input.

**Remarks:** Tracked pressed state is released before draining the queue; window and quit callbacks still run.

**Errors:** `InvalidOperationException` — Called off the owner thread or re-entered.; `AggregateException` — One or more delivered callbacks failed..

**Source:** `src/Servers/Display/DisplayServer.Events.cs`.

<a id="method-keyboardgetkeycodefromphysical"></a>
#### `public Key KeyboardGetKeycodeFromPhysical(Key physical)`

Maps a physical key to the logical key reported for the current keyboard layout.

**Returns:** The current logical key, or the supplied physical key when no layout mapping is available.

- `physical`: A physical key identity.

**Remarks:** The result reflects the active SDL keyboard layout and modifier state at the time of the call without SDL key-event options. An unmapped non-Latin symbol falls back to the physical key; localized Unicode belongs to `KeyboardGetLabelFromPhysical`. Modifier bits supplied with `physical` are preserved. Reverse Tab is normalized to Tab, as in key events. The physical US bracket positions use `BraceLeft` and `BraceRight`; the grave position uses `Section`, and the ISO key beside left Shift uses `QuoteLeft`. SDL keypad keycodes do not distinguish Num Lock or shifted navigation symbols on Wayland, so these keys can retain their physical identity instead of the layout's logical symbol.

**Errors:** `ObjectDisposedException` if disposed; `InvalidOperationException` off the opening thread.

**Source:** `src/Servers/Display/DisplayServer.Events.cs`.

<a id="method-keyboardgetlabelfromphysical"></a>
#### `public Key KeyboardGetLabelFromPhysical(Key physical)`

Maps a physical key to its localized label in the current keyboard layout.

**Returns:** The current label key, including a non-Latin Unicode scalar where applicable, or the supplied physical key when no layout mapping is available.

- `physical`: A physical key identity.

**Remarks:** The label uses the active SDL layout and modifier state without key-event options. Modifier bits supplied with `physical` are preserved. Key events derive their labels from the same mapping using each event's modifier state. A real layout switch and non-Latin label have not yet been checked on Wayland. SDL keypad keycodes do not expose the Num Lock printable label, so a physical keypad identity can be returned in its place.

**Errors:** `ObjectDisposedException` if disposed; `InvalidOperationException` off the opening thread.

**Source:** `src/Servers/Display/DisplayServer.Events.cs`.

<a id="method-windowseticon"></a>
#### `public void WindowSetIcon(Image image, int windowId = MainWindowId)`

Sets an icon specifically for the main window.

- `image`: A live nonempty source image.
- `windowId`: The main-window ID, zero.

**Remarks:** The image is copied and converted to eight-bit RGBA before native submission; the caller retains it. A successful call overrides subsequent `SetIcon` changes for this window. Wayland requires a square image and a compositor with the window-icon protocol.

**Errors:** `ArgumentNullException` for null, `ArgumentException` for an empty image or a nonsquare Wayland image, `ArgumentOutOfRangeException` for an ID other than zero, `InvalidOperationException` for a native failure or wrong thread, and `ObjectDisposedException` after the server or image is disposed.

**Source:** `src/Servers/Display/DisplayServer.Icons.cs`.

<a id="method-seticon"></a>
#### `public void SetIcon(Image image)`

Sets the application-default icon for the main window while it has no explicit window icon.

- `image`: A live nonempty source image.

**Remarks:** The image is copied before submission. After a successful `WindowSetIcon`, this call validates the new image but preserves the window-specific icon. Wayland requires a square image and compositor window-icon support. The API does not yet propagate the default icon to future engine windows, because this server owns only one window.

**Errors:** `ArgumentNullException` for null, `ArgumentException` for an empty image or a nonsquare Wayland image, `InvalidOperationException` for a native failure or wrong thread, and `ObjectDisposedException` after the server or image is disposed.

**Source:** `src/Servers/Display/DisplayServer.Icons.cs`.

<a id="method-mousegetmode"></a>
#### `public MouseMode MouseGetMode()`

Gets the last successfully requested mouse mode.

**Returns:** The mode owned by this display server.

**Source:** `src/Servers/Display/DisplayServer.Pointer.cs`.

<a id="method-mousesetmode"></a>
#### `public void MouseSetMode(MouseMode mode)`

Requests cursor visibility, capture, and confinement as one mode.

- `mode`: The mode to apply to the main window.

**Remarks:** Repeating the current mode does not resubmit native operations. Window managers may release grabs while the window lacks focus. If any native change fails, the prior mode is restored on a best-effort basis and the original native error is reported.

**Errors:** `ArgumentOutOfRangeException` for an unknown mode; `InvalidOperationException` when a native operation fails; `AggregateException` when both the operation and rollback fail.

**Source:** `src/Servers/Display/DisplayServer.Pointer.cs`.

<a id="method-mousegetposition"></a>
#### `public Vector2i MouseGetPosition()`

Gets the current global desktop mouse position where the driver exposes it. On Wayland, where the compositor does not provide reliable global coordinates, reads the last position relative to the main window from SDL's pointer state and converts it to client pixels.

**Returns:** Pointer coordinates in platform-native desktop units, or window-relative client pixels truncated to integers on Wayland.

**Source:** `src/Servers/Display/DisplayServer.Pointer.cs`.

<a id="method-mousegetbuttonstate"></a>
#### `public MouseButtonMask MouseGetButtonState()`

Gets the mouse buttons currently reported as held by SDL.

**Returns:** A mask of non-wheel buttons.

**Source:** `src/Servers/Display/DisplayServer.Pointer.cs`.

<a id="method-warpmouse"></a>
#### `public void WarpMouse(Vector2i position)`

Requests a pointer move to a position in the main window's client area when pointer warping is available.

- `position`: Target coordinates relative to the client area's upper-left corner in native client units.

**Remarks:** Check `HasFeature(Feature.MouseWarp)` before requesting a move. Wayland currently reports this capability unavailable and rejects the call before reaching SDL. This prevents a temporary synthetic position that the compositor does not honor. On an advertised backend, the platform may still ignore a request under its input or remote-desktop policy; a normal return alone does not prove physical movement.

**Errors:** `NotSupportedException` when this display backend does not advertise pointer warping.

**Source:** `src/Servers/Display/DisplayServer.Pointer.cs`.

<a id="method-cursorgetshape"></a>
#### `public CursorShape CursorGetShape()`

Gets the last successfully selected standard pointer shape.

**Returns:** The current server-owned shape.

**Source:** `src/Servers/Display/DisplayServer.Pointer.cs`.

<a id="method-cursorsetshape"></a>
#### `public void CursorSetShape(CursorShape shape)`

Selects a standard pointer shape from the native cursor theme.

- `shape`: Shape to apply.

**Errors:** `ArgumentOutOfRangeException` — `shape` is unknown..

**Source:** `src/Servers/Display/DisplayServer.Pointer.cs`.

<a id="method-cursorsetcustomimage"></a>
#### `public void CursorSetCustomImage(Resource? image, CursorShape shape = CursorShape.Arrow, Vector2 hotspot = default)`

Sets or clears the image used for one pointer shape.

- `image`: A live `Image` or readable `Texture` to copy into a native cursor, or `null` to restore the system shape. Other Resource types are rejected.
- `shape`: The pointer shape slot to customize.
- `hotspot`: The active point relative to the image's upper-left corner; each component is truncated toward zero when submitted to the native cursor.

**Remarks:** Image pixels are copied before this method returns. Source images must be at most 256×256 pixels, and the hotspot must lie inside them. The default hotspot is the upper-left pixel. Other cursor slots are unaffected. Texture conversion calls `GetImage` and disposes the returned temporary image on success or failure. Dimensions and hotspots use its original pixels, independently of logical size overrides. The supplied resource remains caller-owned; later updates require another call. Custom image callbacks may fail; native access rechecks the display lifetime after the callback.

**Errors:** `ArgumentException` — The resource type is unsupported, has no readable image, or the image is empty or exceeds 256×256 pixels. `ArgumentOutOfRangeException` — The shape or hotspot is invalid. `InvalidOperationException` — The caller is off the owner thread or native cursor creation/installation fails. `ObjectDisposedException` — The server, source resource or returned image is disposed. `NotSupportedException` — The image needs unavailable pixel conversion or decompression. Custom `GetImage` failures propagate; validation/conversion failure preserves the previously installed cursor unless the callback itself mutates or disposes the display.

**Source:** `src/Servers/Display/DisplayServer.Pointer.cs`.

<a id="method-imegettext"></a>
#### `public string IMEGetText()`

Gets the most recently received native IME composition text.

**Returns:** The active composition, or an empty string before composition begins or after it commits.

**Source:** `src/Servers/Display/DisplayServer.Text.cs`.

<a id="method-imegetselection"></a>
#### `public Vector2i IMEGetSelection()`

Gets the current composition selection.

**Returns:** The zero-based Unicode-codepoint start and length reported by the input method. Unknown negative native offsets become zero.

**Source:** `src/Servers/Display/DisplayServer.Text.cs`.

<a id="method-windowsetimeactive"></a>
#### `public void WindowSetIMEActive(bool active, int windowId = MainWindowId)`

Enables or disables native text input for the main window.

- `active`: Whether to accept committed text and composition updates.
- `windowId`: The main-window ID, zero.

**Remarks:** The host should enable text input only while a text field owns focus.

**Source:** `src/Servers/Display/DisplayServer.Text.cs`.

<a id="method-windowsetimeposition"></a>
#### `public void WindowSetIMEPosition(Vector2i position, int windowId = MainWindowId)`

Moves the native IME candidate area to a window-local text caret.

- `position`: Caret position in client pixels on Wayland, or platform-native window coordinates elsewhere.
- `windowId`: The main-window ID, zero.

**Remarks:** On Wayland, the client-pixel position is rounded to SDL logical window coordinates using the current window pixel density. The native candidate area is 1×10 window-coordinate units with cursor offset zero. Text input must be active for a candidate popup to appear. Native readback passed at densities 1 and 1.25; an actual input method and visible popup placement remain unverified.

**Errors:** `ArgumentOutOfRangeException` for an invalid window ID; `ObjectDisposedException` during disposal; `OverflowException` if the converted coordinate is unrepresentable; `InvalidOperationException` for an off-thread call or native density/area failure.

**Source:** `src/Servers/Display/DisplayServer.Text.cs`.

<a id="method-istouchscreenavailable"></a>
#### `public bool IsTouchscreenAvailable()`

Gets whether SDL currently reports a touch device or `Input.Instance.EmulateTouchFromMouse` is enabled. The emulation setting makes this query true even without touch hardware. Read it on the opening thread.

**Returns:** `true` when native or emulated touch is available.

**Errors:** `InvalidOperationException` off the opening thread; `ObjectDisposedException` during or after disposal.

**Source:** `src/Servers/Display/DisplayServer.Text.cs`.

<a id="method-windowgetcurrentscreen"></a>
#### `public int WindowGetCurrentScreen(int windowId = MainWindowId)`

Gets the current screen index containing the main window.

**Returns:** The zero-based screen index or `InvalidScreen` when the window has no display.

- `windowId`: The window ID; only zero identifies an owned window.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-getkeyboardfocusscreen"></a>
#### `public int GetKeyboardFocusScreen()`

Gets the index of the display with keyboard focus.

**Returns:** The main window's screen when it has focus on a backend with global focus information; otherwise the primary screen. Wayland always returns the primary screen.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-getscreenfromrect"></a>
#### `public int GetScreenFromRect(Rect2 rectangle)`

Gets the display containing the largest portion of a desktop rectangle.

**Returns:** The zero-based index of the screen with the greatest overlap after its area is truncated to whole pixels, or `InvalidScreen` when no overlap reaches one pixel. Ties keep the first display.

- `rectangle`: Desktop rectangle in the public screen-position coordinates. On Wayland, logical output origins and physical screen widths can make adjacent screen rectangles overlap; the first screen wins an equal-area tie.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-getwindowlist"></a>
#### `public int[] GetWindowList()`

Gets a snapshot of the engine-owned native window IDs.

**Returns:** A one-element snapshot containing `MainWindowId`.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-windowgetnativehandle"></a>
#### `public nint WindowGetNativeHandle(HandleType handleType, int windowId = MainWindowId)`

Gets a borrowed operating-system identity through SDL window properties. `DisplayHandle` is an X11 `Display*` or Wayland `wl_display*`; `WindowHandle` is an X11 window ID, Wayland `wl_surface*`, Win32 `HWND`, or Cocoa `NSWindow*`. The result is pointer-sized and nonzero. It is neither the SDL window pointer nor an owned handle.

- `handleType`: One of the declared [HandleType](DisplayServer.HandleType.md) values. Linux compatibility rendering adds `OpenGLContext`, `EGLDisplay`, `EGLConfig`, `GLXVisualID` and `GLXFBConfig`; unavailable identities fail explicitly.
- `windowId`: Main-window ID, zero; other IDs are invalid.

**Returns:** The borrowed pointer or platform window ID represented as `nint`. Never free or close it. Requery after native window state changes and stop using it after disposal.

**Errors:** `ArgumentOutOfRangeException` for an unknown handle type or window ID; `NotSupportedException` when the active driver cannot provide that identity; `InvalidOperationException` for a call from another thread, failed SDL properties, or an absent native property; `ObjectDisposedException` after disposal.

**Threading and limitations:** Opening SDL main thread only. Android Activity handles need JNI local-reference ownership and iOS view-controller handles need a UIKit bridge; both remain blocked on their native host slices. Native views remain blocked. Linux compatibility GL/EGL/GLX identities are captured from the window-associated renderer context; they expire at renderer shutdown. Getters neither select a context nor return a foreign current context. GPU/software renderers and non-Linux graphics paths reject these queries. Callers must not destroy, replace or mutate the engine context or its graphics state. See [ADR 0042](../decisions/display.md#adr-0042). See the [HandleType reference](DisplayServer.HandleType.md).

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-getwindowatscreenposition"></a>
#### `public int GetWindowAtScreenPosition(Vector2i position)`

Finds the engine-owned window at a desktop position.

**Returns:** `MainWindowId` when the point is inside the visible main window's tested rectangle; otherwise `InvalidWindowId`.

- `position`: Global point in platform-native desktop coordinates.

**Remarks:** Hidden or minimized windows are never hit. On X11, the tested rectangle is the half-open client area from `WindowGetPosition` and `WindowGetSize`; the title bar and border do not count. Other supported desktop drivers use the half-open decorated bounds from `WindowGetPositionWithDecorations` and `WindowGetSizeWithDecorations`.

**Errors:** `NotSupportedException` on Wayland, where the compositor does not expose a reliable global window position.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-windowsetcurrentscreen"></a>
#### `public void WindowSetCurrentScreen(int screen, int windowId = MainWindowId)`

Requests that the main window move to another connected display. Choosing its current display does nothing.

- `screen`: Zero-based display index or a negative display selector.
- `windowId`: The main-window ID, zero.

**Remarks:** A floating window preserves its position relative to the source display origin, then clamps its position in the target work area so part of it remains visible. A maximized window is restored, moved, then maximized again; desktop fullscreen moves to the target display bounds, while exclusive fullscreen selects a target-display mode. The window manager may apply or deny a request asynchronously. Observe the result with `WindowGetCurrentScreen`.

**Errors:** `ArgumentOutOfRangeException` for an unavailable screen or foreign window ID; `NotSupportedException` on Wayland when relocation is requested; `InvalidOperationException` for a failed native query, move, or synchronization; `AggregateException` if both a mode transition and rollback fail. The same-screen no-op also applies on Wayland.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-screengetrefreshrate"></a>
#### `public float ScreenGetRefreshRate(int screen = ScreenOfMainWindow)`

Gets the current screen refresh rate. Uses the precise display-mode numerator and denominator when SDL provides them; otherwise uses its floating-point rate.

**Returns:** The current display-mode refresh rate in hertz, or `-1` for an invalid screen, unavailable mode, nonpositive rate, or nonfinite rate.

- `screen`: Display index or one of the negative display selectors; defaults to the main window's display.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-screengetmaxscale"></a>
#### `public float ScreenGetMaxScale()`

Gets the largest reported content scale among connected displays.

**Returns:** The maximum positive logical-to-physical ratio, at least one when no screen is connected.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-windowgetminsize"></a>
#### `public Vector2i WindowGetMinSize(int windowId = MainWindowId)`

Gets the requested minimum size of the main window, initially 64×64 client pixels on Wayland or native window units elsewhere.

**Returns:** The minimum client dimensions in pixels on Wayland or native window units elsewhere; zero means no bound on that axis.

- `windowId`: The main-window ID, zero.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-windowsetminsize"></a>
#### `public void WindowSetMinSize(Vector2i size, int windowId = MainWindowId)`

Requests minimum client dimensions for the main window. Wayland converts each pixel bound to native logical units using window pixel density, rounding minimums upward and maximums downward, then reapplies them after a display-scale change.

- `size`: Nonnegative per-axis lower bounds; zero leaves that axis unbounded.
- `windowId`: The main-window ID, zero.

**Errors:** `ArgumentOutOfRangeException` if a bound is negative, exceeds a nonzero maximum, or cannot coexist with that maximum after density conversion. Rejection preserves the previous native constraint.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-windowgetmaxsize"></a>
#### `public Vector2i WindowGetMaxSize(int windowId = MainWindowId)`

Gets the requested maximum size of the main window.

**Returns:** The maximum client dimensions in pixels on Wayland or native window units elsewhere; zero means no bound on that axis.

- `windowId`: The main-window ID, zero.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-windowsetmaxsize"></a>
#### `public void WindowSetMaxSize(Vector2i size, int windowId = MainWindowId)`

Requests maximum client dimensions for the main window. Wayland converts each pixel bound to native logical units using window pixel density, rounding minimums upward and maximums downward, then reapplies them after a display-scale change.

- `size`: Nonnegative per-axis upper bounds; zero leaves that axis unbounded.
- `windowId`: The main-window ID, zero.

**Errors:** `ArgumentOutOfRangeException` if a bound is negative, falls below a nonzero minimum, or cannot be represented without violating a pixel limit at the current density. Rejection preserves the previous native constraint.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-windowgetmode"></a>
#### `public WindowMode WindowGetMode(int windowId = MainWindowId)`

Gets the main window's current native mode.

**Returns:** The SDL-reported state, distinguishing an exclusive display mode from borderless desktop fullscreen. On Wayland, the requested minimized state can persist until the window regains focus.

- `windowId`: The main-window ID, zero.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-windowsetmode"></a>
#### `public void WindowSetMode(WindowMode mode, int windowId = MainWindowId)`

Requests a native main-window mode.

- `mode`: Windowed, minimized, maximized, borderless fullscreen, or exclusive fullscreen.
- `windowId`: The main-window ID, zero.

**Remarks:** On Wayland, exclusive fullscreen requests ordinary compositor fullscreen and, when accepted, is observed as `Fullscreen`. The Wayland protocol cannot reliably restore a minimized window programmatically; SDL may keep reporting `Minimized` until the window regains focus. Other drivers choose the closest available native mode to the current logical window size. Transitions may complete asynchronously; call `WindowGetMode` for the reported state. A failed entry request restores the previous fullscreen selection or reports both failures.

**Errors:** `ArgumentOutOfRangeException` — `mode` is unknown; `InvalidOperationException` — no suitable native mode exists or the transition fails; `AggregateException` — transition and rollback both fail.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-windowisfocused"></a>
#### `public bool WindowIsFocused(int windowId = MainWindowId)`

Gets whether the main window currently has focus. Wayland uses pointer focus; other drivers use the native keyboard-focus flag.

**Returns:** `true` when the active driver's native focus query identifies the main window.

- `windowId`: The main-window ID, zero.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-windowgetflag"></a>
#### `public bool WindowGetFlag(WindowFlag flag, int windowId = MainWindowId)`

Reads a supported native window flag.

**Returns:** Whether the policy is enabled in the observed native flags.

**Remarks:** On Wayland, the main window cannot apply `AlwaysOnTop` or `NoFocus`; both flags throw `NotSupportedException` rather than reporting an inert SDL flag.

- `flag`: Policy to inspect.
- `windowId`: The main-window ID, zero.

**Errors:** `ArgumentOutOfRangeException` — `flag` is not a defined policy.; `NotSupportedException` — The defined policy requires a display capability that is not integrated..

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-windowsetflag"></a>
#### `public void WindowSetFlag(WindowFlag flag, bool enabled, int windowId = MainWindowId)`

Requests a supported native window policy.

- `flag`: Policy to change.
- `enabled`: Whether to enable the policy.
- `windowId`: The main-window ID, zero.

**Remarks:** A platform may reject the policy or apply it asynchronously. On Wayland, the main window rejects `AlwaysOnTop` and `NoFocus` before calling SDL, avoiding silent success or a native flag mutation followed by backend failure.

**Errors:** `ArgumentOutOfRangeException` — `flag` is not a defined policy.; `NotSupportedException` — The defined policy requires a display capability that is not integrated..

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-windowismaximizeallowed"></a>
#### `public bool WindowIsMaximizeAllowed(int windowId = MainWindowId)`

Gets whether the current SDL resize policy permits a maximize request.

**Returns:** `true` when the window is resizable, regardless of its current mode.

The native window manager can independently disable maximization. This method does not query Wayland `wm_capabilities`, so the result is only the SDL resize-policy subset of the native answer. A fullscreen window can leave fullscreen before requesting maximization.

- `windowId`: The main-window ID, zero.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-windowmovetoforeground"></a>
#### `public void WindowMoveToForeground(int windowId = MainWindowId)`

Requests that the window manager bring the main window to the foreground.

- `windowId`: The main-window ID, zero.

**Remarks:** On Wayland this is deliberately a no-op because its standard top-level window protocol has no foreground request. Other window managers may refuse the native raise request under their focus policy. The call validates the main-window ID even on Wayland.

**Errors:** `ArgumentOutOfRangeException` for an ID other than zero; `InvalidOperationException` for a wrong-thread call or native raise failure; `ObjectDisposedException` during or after disposal.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-windowrequestattention"></a>
#### `public void WindowRequestAttention(int windowId = MainWindowId)`

Requests user attention until the main window receives focus.

- `windowId`: The main-window ID, zero.

**Remarks:** On Wayland this submits an activation token without an input serial; the compositor decides whether and how to indicate urgency. The platform may show no effect if the window already has focus. The method reports a native request, not a guarantee of a visible indication.

**Errors:** `ArgumentOutOfRangeException` for an ID other than zero; `InvalidOperationException` for a wrong-thread call or native attention failure; `ObjectDisposedException` during or after disposal.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-windowgetpositionwithdecorations"></a>
#### `public Vector2i WindowGetPositionWithDecorations(int windowId = MainWindowId)`

Gets the main-window position including its left and top decorations.

**Returns:** The outer upper-left position in platform-native desktop coordinates.

- `windowId`: The main-window ID, zero.

**Errors:** `NotSupportedException` on Wayland, where the compositor does not expose a reliable global window position.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-windowgetsizewithdecorations"></a>
#### `public Vector2i WindowGetSizeWithDecorations(int windowId = MainWindowId)`

Gets the main-window size including native decorations.

**Returns:** Outer window dimensions in platform-native window units. Wayland returns the client size in pixels because the compositor does not expose reliable top-level decoration dimensions.

- `windowId`: The main-window ID, zero.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-windowsettaskbarprogressstate"></a>
#### `public void WindowSetTaskbarProgressState(ProgressState state, int windowId = MainWindowId)`

Requests the taskbar progress state for the main window. On Wayland this call rejects the request because desktop entry and taskbar notification integration are not yet verified. On other native drivers it delegates to SDL, whose success means the request was accepted by SDL and does not prove that a desktop taskbar displayed it.

- `state`: The indication to show.
- `windowId`: The main-window ID, zero.

**Errors:** `ArgumentOutOfRangeException` for an unknown `state` or non-main `windowId`; `NotSupportedException` on Wayland; `InvalidOperationException` for an off-thread call or native rejection; `ObjectDisposedException` after disposal.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-windowsettaskbarprogressvalue"></a>
#### `public void WindowSetTaskbarProgressValue(float value, int windowId = MainWindowId)`

Requests the taskbar progress fraction for the main window. On Wayland this call rejects the request because desktop entry and taskbar notification integration are not yet verified. On other native drivers it delegates to SDL, whose success does not prove that a desktop taskbar displayed the value.

- `value`: A finite fraction from zero through one.
- `windowId`: The main-window ID, zero.

**Errors:** `ArgumentOutOfRangeException` for a nonfinite or out-of-range `value` or non-main `windowId`; `NotSupportedException` on Wayland; `InvalidOperationException` for an off-thread call or native rejection; `ObjectDisposedException` after disposal.

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="method-open"></a>
#### `public static DisplayServer Open(string title, Vector2i size, bool hidden = false)`

Opens the native video subsystem and creates the main window.

**Returns:** The process's active display server.

- `title`: Initial UTF-8 window title.
- `size`: Positive initial dimensions in native window units, which are logical on Wayland.
- `hidden`: Whether the window starts hidden.

**Remarks:** The caller owns and must dispose the returned server on the opening thread. The native window manager may constrain requested dimensions smaller than the 64×64 client-pixel minimum on Wayland or native-unit minimum elsewhere. A visible Wayland window presents a blank surface until rendering takes ownership and refreshes it on size or scale changes. On a Wayland session inheriting an X11-only GTK backend, `Open` selects the Wayland backend before video initialization. When GTK 3 is available, it keeps the selected title-bar theme and fills its border box to prevent a transparent seam.

**Errors:** `ArgumentNullException` — `title` is null.; `ArgumentOutOfRangeException` — `size` has a nonpositive component.; `InvalidOperationException` — Another server is active, the call is off SDL's main thread, or SDL fails to open video or create the window..

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-getname"></a>
#### `public string GetName()`

Gets the current display backend name.

**Returns:** The active backend's public name. The SDL `wayland`, `x11`, and `dummy` drivers are reported as `Wayland`, `X11`, and `headless`, respectively.

**Errors:** `ObjectDisposedException` — The server is disposing or disposed.; `InvalidOperationException` — The caller is not the owner thread..

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-getscreencount"></a>
#### `public int GetScreenCount()`

Gets the current number of connected displays.

**Returns:** The display count, which may change after hotplug events.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-getprimaryscreen"></a>
#### `public int GetPrimaryScreen()`

Gets the index of the current primary display.

**Returns:** Index zero on Wayland, which has no primary-display identity; otherwise the zero-based native primary index or `InvalidScreen` when no display matches.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-screengetposition"></a>
#### `public Vector2i ScreenGetPosition(int screen = ScreenOfMainWindow)`

Gets the global desktop position of a display.

**Returns:** The upper-left position in platform-native desktop coordinates, or zero if the display is invalid.

- `screen`: Display index or one of the negative display selectors; defaults to the main window's display.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-screengetsize"></a>
#### `public Vector2i ScreenGetSize(int screen = ScreenOfMainWindow)`

Gets the full size of a display.

**Returns:** The physical pixel dimensions on Wayland, reconstructed from SDL's logical bounds and current mode pixel density; platform-native desktop units elsewhere. An invalid display returns zero.

- `screen`: Display index or one of the negative display selectors; defaults to the main window's display.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-screengetusablerect"></a>
#### `public Rect2i ScreenGetUsableRect(int screen = ScreenOfMainWindow)`

Gets the usable desktop rectangle of a display.

**Returns:** On Wayland, the output position combined with its physical pixel dimensions; the compositor can express the position in logical global coordinates. Other drivers return the native work area after platform-reserved bars are excluded. An invalid display returns an empty rectangle.

- `screen`: Display index or one of the negative display selectors; defaults to the main window's display.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-screengetscale"></a>
#### `public float ScreenGetScale(int screen = ScreenOfMainWindow)`

Gets the display content scale reported by the native video system. On Wayland, the default main-window selector reads the window's current scale; a direct display index rounds the current display mode's pixel density upward to the integer output scale. A 1.25-scale output therefore reports two by index, while its window can report 1.25. A newly created or hidden window can temporarily report another scale before the compositor assigns its preferred fractional scale. Its value can change after the first buffer is presented or the window moves. X11 returns one.

**Returns:** A positive scale factor, or one if the display is invalid. On Wayland, the default selector is the window's current fractional factor; an indexed display returns its integer output factor.

- `screen`: Display index or one of the negative display selectors; defaults to the main window's display.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-windowsettitle"></a>
#### `public void WindowSetTitle(string title, int windowId = MainWindowId)`

Requests a new main-window title.

- `title`: New UTF-8 title.
- `windowId`: The main-window ID, zero.

**Errors:** `ArgumentNullException` for a null title, `ArgumentOutOfRangeException` for a window ID other than zero, `InvalidOperationException` for a call from another thread or a failed native request, and `ObjectDisposedException` after disposal.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-windowgetsize"></a>
#### `public Vector2i WindowGetSize(int windowId = MainWindowId)`

Gets the main window's client size.

**Returns:** Client pixels on Wayland or platform-native window dimensions elsewhere.

- `windowId`: The main-window ID, zero.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-windowsetsize"></a>
#### `public void WindowSetSize(Vector2i size, int windowId = MainWindowId)`

Requests a new main-window client size.

- `size`: Client pixels on Wayland or platform-native window dimensions elsewhere. Wayland clamps each nonpositive component to one and converts the request to native logical units; other drivers require positive components.
- `windowId`: The main-window ID, zero.

**Errors:** On drivers other than Wayland, `ArgumentOutOfRangeException` if a dimension is not positive.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-windowgetposition"></a>
#### `public Vector2i WindowGetPosition(int windowId = MainWindowId)`

Gets the main window's global desktop position.

**Returns:** The upper-left position in platform-native desktop coordinates.

- `windowId`: The main-window ID, zero.

**Errors:** `NotSupportedException` on Wayland, where the compositor does not expose a reliable global window position.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-windowsetposition"></a>
#### `public void WindowSetPosition(Vector2i position, int windowId = MainWindowId)`

Requests a global desktop position for the main window.

- `position`: Upper-left position in platform-native desktop coordinates.
- `windowId`: The main-window ID, zero.

**Errors:** `NotSupportedException` on Wayland, where top-level windows cannot choose a global desktop position.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-clipboardhas"></a>
#### `public bool ClipboardHas()`

Gets whether the ordinary system clipboard currently returns a nonempty text string. The primary selection is a separate source.

**Returns:** `true` exactly when `ClipboardGet()` returns a nonempty string.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-clipboardget"></a>
#### `public string ClipboardGet()`

Gets text from the system clipboard.

**Returns:** The current text or an empty string when no text is available.

**Remarks:** On Wayland, only a client with keyboard focus receives a clipboard offer. A hidden or unfocused process can return an empty string even when a focused process reads nonempty text.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-clipboardset"></a>
#### `public void ClipboardSet(string text)`

Replaces system clipboard text.

- `text`: Text to place on the clipboard.

**Remarks:** On Wayland, another process may receive the text only after a recent input event supplies the compositor's selection serial. The native backend's success result does not prove cross-process ownership. The caller must keep pumping events while serving a selection. Publication remains unverified until a focused publisher with a recent serial serves nonempty text to a separate focused reader in an isolated compositor.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-clipboardgetprimary"></a>
#### `public string ClipboardGetPrimary()`

Gets text from the platform's primary selection.

**Returns:** The selected text, or an empty string when the backend has no primary selection.

**Remarks:** Primary selection is separate from the clipboard. On Wayland, its offer likewise requires keyboard focus; a hidden or unfocused process can read an empty string. Platforms without it may report an empty value.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="method-clipboardsetprimary"></a>
#### `public void ClipboardSetPrimary(string text)`

Replaces text in the platform's primary selection.

- `text`: The selection text.

**Remarks:** Wayland primary selection has the same recent-input and event-pump requirements as the ordinary clipboard. It may be unavailable on some compositors. Publication remains unverified until a focused publisher with a recent serial serves nonempty text to a separate focused reader in an isolated compositor.

**Errors:** `ArgumentNullException` — `text` is null.; `InvalidOperationException` — The native backend rejects the selection..

**Source:** `src/Servers/Display/DisplayServer.cs`.

### Event Descriptions

<a id="event-quitrequested"></a>
#### `public event Action? QuitRequested`

Occurs when the operating system requests that the application quit.

**Remarks:** Delivery is synchronous on the opening thread in native queue order during `ProcessEvents` or `ForceProcessAndDropEvents`. Other windows' close requests are ignored. The server does not close automatically. A failing handler does not prevent later queued events from being delivered; the pump reports its failure afterward in an `AggregateException`.

**Source:** `src/Servers/Display/DisplayServer.Events.cs`.

<a id="event-systemthemechanged"></a>
#### `public event Action? SystemThemeChanged`

Occurs when SDL reports a native system-theme change. The event carries no theme value; query `IsDarkMode` or `IsDarkModeSupported` for the currently reported value.

**Remarks:** Delivery is synchronous on the opening thread during `ProcessEvents` or `ForceProcessAndDropEvents`, in native queue order. A failing handler does not stop later events; `ProcessEvents` reports callback failures in an `AggregateException` after draining the queue. Notifications depend on the active native backend and may cover fewer system-appearance changes than the operating system supports.

**Source:** `src/Servers/Display/DisplayServer.Events.cs`.

<a id="event-closerequested"></a>
#### `public event Action? CloseRequested`

Occurs when the main window receives a close request.

**Remarks:** Delivery is synchronous on the opening thread during `ProcessEvents` or `ForceProcessAndDropEvents`, in native queue order. Events from other native windows are ignored. The server does not close automatically. A failing handler does not stop later events; its failure is included in the pump's `AggregateException`. A real Wayland compositor close request was delivered while the native window remained alive.

**Source:** `src/Servers/Display/DisplayServer.Events.cs`.

<a id="event-windowmouseentered"></a>
#### `public event Action? WindowMouseEntered`

Occurs when the pointer enters the main window.

**Remarks:** Delivery follows native event order during `ProcessEvents` or `ForceProcessAndDropEvents`. Only effective main-window pointer transitions notify; events for other native windows are ignored. A failing handler does not stop later events; the failure is included in the pump's `AggregateException`. A real Wayland pointer entry was observed.

**Source:** `src/Servers/Display/DisplayServer.Events.cs`.

<a id="event-windowmouseexited"></a>
#### `public event Action? WindowMouseExited`

Occurs when the pointer leaves the main window.

**Remarks:** Delivery follows native event order during `ProcessEvents` or `ForceProcessAndDropEvents`. Only effective main-window pointer transitions notify; events for other native windows are ignored. A failing handler does not stop later events; the failure is included in the pump's `AggregateException`. A real Wayland pointer exit was observed.

**Source:** `src/Servers/Display/DisplayServer.Events.cs`.

<a id="event-windowdpichanged"></a>
#### `public event Action? WindowDpiChanged`

Occurs when SDL reports a display-content-scale change for the main window. The event carries no scale value. Query `ScreenGetScale()` with its default main-window selector to read the window's reported scale, including fractional Wayland values.

**Remarks:** Delivery is synchronous on the opening thread during `ProcessEvents` or `ForceProcessAndDropEvents`, in native queue order. Events for other native windows are ignored. A failing handler does not stop later events; the failure is included in the pump's `AggregateException`. The native content-scale signal can occur when moving a window between displays; it is not a measurement of physical dots per inch. On Wayland the corresponding window notification is also triggered by an effective scale change. Ordering with a simultaneous rectangle update and first rendered buffer remains unverified.

**Source:** `src/Servers/Display/DisplayServer.Events.cs`.

<a id="event-windowrectchanged"></a>
#### `public event Action<Rect2i>? WindowRectChanged`

Occurs when a native move or resize changes the observed client rectangle of the main window. The `Rect2i` argument contains the complete position and size after that individual event: position is in the driver's desktop coordinates and size is in client pixels on Wayland or native window units elsewhere. On Wayland, the position is conventionally `(0, 0)` because the compositor does not disclose a reliable global top-level position.

**Remarks:** The opening thread delivers callbacks synchronously during `ProcessEvents` or `ForceProcessAndDropEvents`, in native queue order. The server commits the changed rectangle before calling handlers, so a move followed by a resize produces successive full-rectangle snapshots. Unchanged rectangles and events for other native windows do not notify. A failing handler does not stop later native events; the pump reports callback failures in an `AggregateException` after draining the queue. `WindowGetPosition` and `WindowSetPosition` still reject Wayland.

**Source:** `src/Servers/Display/DisplayServer.Events.cs`.

<a id="event-windowfocuschanged"></a>
#### `public event Action<bool>? WindowFocusChanged`

Occurs when the main window gains or loses keyboard focus.

**Remarks:** Delivery is synchronous on the opening thread during `ProcessEvents` or `ForceProcessAndDropEvents`, in native queue order; foreign-window events are ignored. The window callback precedes the application focus notification. On focus loss, tracked pressed input is released after both deliveries, including when either handler throws. Touch contacts and stale modifier state are cleared before delivery. A real Wayland session delivered focus gain and loss.

**Source:** `src/Servers/Display/DisplayServer.Events.cs`.

<a id="event-textinput"></a>
#### `public event Action<string>? TextInput`

Occurs when the platform commits text input, including text composed through an IME.

**Remarks:** Delivery is synchronous during `ProcessEvents`; the input event retains no string state.

**Source:** `src/Servers/Display/DisplayServer.Events.cs`.

<a id="event-textediting"></a>
#### `public event Action<string, Vector2i>? TextEditing`

Occurs after the native input method updates its uncommitted composition.

**Remarks:** The text and Unicode-codepoint selection have already committed to `IMEGetText` and `IMEGetSelection`; unknown negative native offsets become zero.

**Source:** `src/Servers/Display/DisplayServer.Events.cs`.

<a id="event-filesdropped"></a>
#### `public event Action<IReadOnlyList<string>>? FilesDropped`

Occurs once when the operating system completes a group of dropped files on the main window. Paths preserve native order. A single unbracketed file event is delivered as a one-item list; an empty group delivers nothing.

**Remarks:** Paths are copied from native event memory before the callback runs. A new group discards an unfinished previous group; only completion publishes a bracketed group. Delivery is synchronous during `ProcessEvents` on the opening thread, and callback failures are aggregated after the event drain.

**Source:** `src/Servers/Display/DisplayServer.Events.cs`.

### Enumeration Descriptions

<a id="enum-handletype"></a>
#### `public enum HandleType`

Selects a borrowed OS display, window or graphics-context identity for `WindowGetNativeHandle`. The engine owns the underlying native objects. See the [complete enum reference](DisplayServer.HandleType.md).

| Value | Meaning |
| --- | --- |
| `DisplayHandle = 0` | X11 or Wayland display connection. |
| `WindowHandle = 1` | Native main-window identity. |
| `OpenGLContext = 3` | Linux compatibility renderer's GL context. |
| `EGLDisplay = 4` | EGL display associated with that context. |
| `EGLConfig = 5` | EGL framebuffer configuration associated with that context. |
| `GLXVisualID = 6` | X11/GLX visual ID associated with that context. |
| `GLXFBConfig = 7` | X11/GLX framebuffer configuration associated with that context. |

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="enum-filedialogmode"></a>
#### `public enum FileDialogMode`

Selects the native chooser operation. The stable numeric values match the public display contract.

| Value | Meaning |
| --- | --- |
| `OpenFile = 0` | Select one existing file. |
| `OpenFiles = 1` | Select multiple existing files. |
| `OpenDirectory = 2` | Select one directory. |
| `OpenAny = 3` | Select a file or directory; currently unsupported by SDL. |
| `SaveFile = 4` | Select a destination file that need not exist. |

**Source:** `src/Servers/Display/DisplayServer.Dialogs.cs`.

<a id="enum-feature"></a>
#### `public enum Feature`

Identifies a display-server capability for `HasFeature`.

| Value | Meaning |
| --- | --- |
| `Subwindows = 1` | Independent native child windows. |
| `Touchscreen = 2` | Touchscreen input. |
| `Mouse = 3` | Mouse input. |
| `MouseWarp = 4` | Pointer warping. |
| `Clipboard = 5` | Text clipboard access. |
| `VirtualKeyboard = 6` | An on-screen keyboard. |
| `CursorShape = 7` | System cursor shapes. |
| `CustomCursorShape = 8` | Image-backed custom cursors. |
| `NativeDialog = 9` | Native message dialogs. |
| `Ime = 10` | Input-method composition. |
| `WindowTransparency = 11` | Per-pixel window transparency. |
| `Hidpi = 12` | Display content-scale queries. |
| `Icon = 13` | Window icon changes. |
| `NativeIcon = 14` | Platform-native icon resources. |
| `Orientation = 15` | Device orientation control. |
| `SwapBuffers = 16` | Presentation swap-interval control. |
| `ClipboardPrimary = 18` | A separate primary-selection clipboard. |
| `TextToSpeech = 19` | Text-to-speech output. |
| `ExtendToTitle = 20` | Content drawn under native title controls. |
| `ScreenCapture = 21` | Desktop screen capture. |
| `StatusIndicator = 22` | System status indicators. |
| `NativeHelp = 23` | Native help-search integration. |
| `NativeDialogInput = 24` | Native text-input dialogs. |
| `NativeDialogFile = 25` | Native file-selection dialogs. |
| `NativeDialogFileExtra = 26` | Native file dialogs with additional options and virtual paths. |
| `WindowDrag = 27` | Interactive native window dragging and resizing. |
| `ScreenExcludeFromCapture = 28` | Window exclusion from ordinary screen capture. |
| `WindowEmbedding = 29` | Embedding of external native windows. |
| `NativeDialogFileMime = 30` | MIME-based native file-dialog filters. |
| `EmojiAndSymbolPicker = 31` | A system emoji and symbol picker. |
| `NativeColorPicker = 32` | A native color picker. |
| `SelfFittingWindows = 33` | Automatic popup fitting to display bounds. |
| `AccessibilityScreenReader = 34` | Screen-reader accessibility integration. |
| `HdrOutput = 35` | High-dynamic-range presentation. |
| `PipMode = 36` | Picture-in-picture presentation. |

**Source:** `src/Servers/Display/DisplayServer.Capabilities.cs`.

<a id="enum-mousemode"></a>
#### `public enum MouseMode`

Defines cursor visibility and window confinement.

| Value | Meaning |
| --- | --- |
| `Visible = 0` | The pointer is visible and free to leave the window. |
| `Hidden = 1` | The pointer is hidden and free to leave the window. |
| `Captured = 2` | The pointer is hidden, captured, and reports relative motion. |
| `Confined = 3` | The visible pointer is confined to the main window. |
| `ConfinedHidden = 4` | The hidden pointer is confined to the main window. |
| `Max = 5` | The number of pointer modes; not a selectable mode. |

**Source:** `src/Servers/Display/DisplayServer.Pointer.cs`.

<a id="enum-cursorshape"></a>
#### `public enum CursorShape`

Identifies standard pointer shapes supported by the native cursor theme.

| Value | Meaning |
| --- | --- |
| `Arrow = 0` | The default pointer arrow. |
| `IBeam = 1` | The text-selection I-beam. |
| `PointingHand = 2` | The pointing hand for links. |
| `Cross = 3` | The crosshair for precise positioning. |
| `Wait = 4` | The nonblocking wait indicator, usually paired with an arrow. |
| `Busy = 5` | The blocking wait indicator, usually replacing the arrow. |
| `Drag = 6` | The dragging hand pointer. |
| `CanDrop = 7` | The pointer indicating that a dragged item can be dropped. |
| `Forbidden = 8` | The pointer indicating that a dragged item cannot be dropped. |
| `VSize = 9` | The vertical-resize pointer. |
| `HSize = 10` | The horizontal-resize pointer. |
| `BDiagSize = 11` | The northeast-southwest diagonal-resize pointer. |
| `FDiagSize = 12` | The northwest-southeast diagonal-resize pointer. |
| `Move = 13` | The four-direction move pointer. |
| `VSplit = 14` | The vertical split-resize pointer. |
| `HSplit = 15` | The horizontal split-resize pointer. |
| `Help = 16` | The help pointer. |
| `Max = 17` | The number of pointer shapes; not a selectable shape. |

**Source:** `src/Servers/Display/DisplayServer.Pointer.cs`.

<a id="enum-windowflag"></a>
#### `public enum WindowFlag`

Selects a main-window policy by its stable display-server ID.

| Value | Meaning |
| --- | --- |
| `ResizeDisabled = 0` | Whether dragging the window border is prevented from resizing it. |
| `Borderless = 1` | Whether the window has no native border and title bar. |
| `AlwaysOnTop = 2` | Whether the window stays above ordinary windows. |
| `Transparent = 3` | Whether the window background can be transparent; requires transparent window creation and a renderer. |
| `NoFocus = 4` | Whether the window is prevented from receiving keyboard focus. |
| `Popup = 5` | Whether the window is a transient menu popup; requires multiple-window ownership. |
| `ExtendToTitle = 6` | Whether content extends under the native title bar; requires platform title-bar integration. |
| `MousePassthrough = 7` | Whether mouse input passes to an underlying application window; requires native hit-test integration. |
| `SharpCorners = 8` | Whether native rounded window corners are suppressed; requires platform window-style integration. |
| `ExcludeFromCapture = 9` | Whether ordinary screen capture excludes the window; requires platform capture-policy integration. |
| `PopupWmHint = 10` | Whether the window manager treats the window as a popup; requires multiple-window ownership. |
| `MinimizeDisabled = 11` | Whether native minimization controls are disabled; requires platform window-style integration. |
| `MaximizeDisabled = 12` | Whether native maximization controls are disabled; requires platform window-style integration. |
| `Max = 13` | The number of defined policy identifiers; not a window policy. |

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="enum-windowmode"></a>
#### `public enum WindowMode`

Identifies the current presentation state of a native window.

| Value | Meaning |
| --- | --- |
| `Windowed = 0` | A decorated or undecorated floating window. |
| `Minimized = 1` | A window hidden by the window manager and represented in its task list. |
| `Maximized = 2` | A window expanded to the work area with its border retained. |
| `Fullscreen = 3` | A borderless window covering its current display without a video-mode change. |
| `ExclusiveFullscreen = 4` | A selected native video mode where supported; ordinary compositor fullscreen on Wayland. |

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

<a id="enum-progressstate"></a>
#### `public enum ProgressState`

Identifies a native taskbar progress indication.

| Value | Meaning |
| --- | --- |
| `None = 0` | Removes taskbar progress. |
| `Indeterminate = 1` | Shows activity without a numerical fraction. |
| `Normal = 2` | Shows normal progress. |
| `Error = 3` | Shows an error state. |
| `Paused = 4` | Shows a paused state. |

**Source:** `src/Servers/Display/DisplayServer.Windows.cs`.

### Constant Descriptions

<a id="constant-mainwindowid"></a>
#### `public const int MainWindowId = 0`

Identifies the main window.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="constant-invalidwindowid"></a>
#### `public const int InvalidWindowId = -1`

Identifies a window that does not exist.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="constant-invalidscreen"></a>
#### `public const int InvalidScreen = -1`

Identifies a display that does not exist.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="constant-screenwithmousefocus"></a>
#### `public const int ScreenWithMouseFocus = -4`

Selects the display containing the mouse pointer. On Wayland this always selects display index zero, including when this server's window does not have native mouse focus.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="constant-screenwithkeyboardfocus"></a>
#### `public const int ScreenWithKeyboardFocus = -3`

Selects the keyboard-focused main window's display where the backend exposes global keyboard focus. Wayland and the absence of focus resolve to the primary screen.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="constant-screenprimary"></a>
#### `public const int ScreenPrimary = -2`

Selects the primary display.

**Source:** `src/Servers/Display/DisplayServer.cs`.

<a id="constant-screenofmainwindow"></a>
#### `public const int ScreenOfMainWindow = -1`

Selects the display containing the main window.

**Source:** `src/Servers/Display/DisplayServer.cs`.

### Protected extension point descriptions

<a id="protected-validatedisposal"></a>
#### `protected override void ValidateDisposal()`

Checks owner thread and rejects disposal while `ProcessEvents` is delivering. The base disposal transition does not start after rejection.

<a id="protected-dispose"></a>
#### `protected override void Dispose(bool disposing)`

When `disposing` is true, restores the default pointer, releases cursors and the window, quits the video subsystem, clears the process singleton, then calls the base implementation. This hook is reached only from owner-thread disposal.

## Lifecycle and invariants

- Closed → `Open` initializes video and creates one window → active → owner-thread `Dispose` releases it → closed. A second open fails while active. Failed creation unwinds video initialization.
- `ProcessEvents` is synchronous and non-reentrant; window events, including content-scale and complete client-rectangle changes for this window, are delivered in native queue order. State changes precede callbacks. Both wheel axes are dispatched even when one callback fails. Native file-dialog results are delivered on the owner thread after the captured native event drain. Callback failures are aggregated after the queue drains.
- Native system-theme notifications use the same ordered owner-thread callback path. `IsDarkMode` reads current SDL state; on Linux Wayland and X11, it also requires the cached Settings portal capability. `IsDarkModeSupported` caches that capability at open time on Linux Wayland and X11 and otherwise queries SDL theme state.
- A pending native file chooser keeps its callback and filter memory alive until SDL completes it; owner-thread disposal is rejected while one is pending or its copied result remains undelivered.
- Main-window ID is zero. Invalid window IDs, dimensions, modes, flags, and cursor hotspots are rejected before their corresponding native operation. Invalid screen queries return their documented fallback values; a setter that requires a real screen rejects an invalid selector.
- SDL screen and window getters report observed state; setters are requests to the window manager.
- `Input` owns committed input state. Focus loss clears tracked touch contacts, delivers window and application notifications, then releases pressed inputs even when a callback fails.
- SDL-owned window and resource handles stay private. Borrowed operating-system display/window and supported graphics-context identities may cross the public API through `WindowGetNativeHandle`; the server still owns their lifetime and requires deterministic owner-thread disposal.

## Dependencies and interactions

Internal SDL3-CS source is compiled into `Electron2D.dll`; the Linux native SDL3 library is supplied by the runtime project's transitive package. Linux Wayland/X11 theme-capability detection also calls the system `libdbus-1.so.3` through `LinuxPortalThemeSupport`; it has no managed package or external command dependency. Uses `Input` and its typed event hierarchy, `Image` for icon pixel copies and `Image`/`Texture` for cursor pixel copies, `Engine` only through host calls, and `ElectronObject` lifetime semantics. The display server itself owns no rendering device or scene tree.

On GNOME Wayland, libdecor supplies the title bar. The verified session inherited `GDK_BACKEND=x11`, causing `libdecor-gtk` initialization to fail and libdecor to use its Cairo style. `Open` selects the Wayland GTK backend before SDL initializes and restores the inherited value if startup fails or SDL selects another driver. With GTK 3 available, it installs and later removes a process-local style provider that fills the default title bar's border box. The visible Stillglass-Dark window retained its themed, focus-colored controls without the transparent one-pixel seam. The desktop theme is never changed.

## Verification

The Wayland native smoke accepted a 256-pixel-wide cursor with a hotspot on its last pixel, installed a 2×2 custom cursor with a fractional hotspot, retained it after the source image was disposed, rejected a nonfinite hotspot and a 257-pixel-wide image without replacing the cursor, reused the custom shape after a shape switch, and restored the system cursor when cleared. The test observes SDL's active native cursor identity; it does not inspect the compositor's rendered pixels or hotspot location.

`bash tests/Electron2D.Tests/check-attention-wayland.sh` passed on the current Wayland host. A client protocol trace showed no activation request from `WindowMoveToForeground` and one token request, commit, and activation without an input serial from `WindowRequestAttention`; both methods rejected a foreign window ID. The check does not establish whether the compositor displayed an urgency indicator.

The full native Wayland smoke passed with `DisplayServerTaskbarProgressNativeTests`: both valid taskbar setters rejected the unsupported backend, invalid enum/value/window inputs retained their validation errors, and SDL progress state remained unchanged. No visible desktop taskbar indication is verified.

`SDL_VIDEODRIVER=wayland ELECTRON2D_TEST_DISPLAY_SCREENSAVER=1 dotnet run --project tests/Electron2D.Tests -c Release --no-build` passed. A simultaneous session-bus monitor observed `Inhibit`, `UnInhibit`, `Inhibit`, and `UnInhibit` calls from that process during initialization and the false/true/false requests. This verifies the request path on the current host, not an extended idle timeout or physical display power transition.

`env -u LD_LIBRARY_PATH ELECTRON2D_TEST_DISPLAY_NATIVE=1 ELECTRON2D_TEST_PORTAL_SETTINGS=1 SDL_VIDEODRIVER=wayland dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release --no-restore` passed against this host's Settings portal version two. Repeating with `DBUS_SESSION_BUS_ADDRESS=unix:path=/tmp/electron2d-no-session-bus` and `ELECTRON2D_TEST_PORTAL_SETTINGS=0` passed the unavailable-bus branch. The same two branches also passed with `SDL_VIDEODRIVER=x11`. Neither test changes the actual color-scheme preference or generates a physical theme-change event.

`/usr/bin/python3 -B tests/Electron2D.Tests/check-theme-wayland.py` passed with a private session bus and hidden Wayland window. Version zero rejected support even with a dark value; version one supported an unset value while reporting `Unknown` and `IsDarkMode() == false`. The mock portal then sent three native `SettingChanged` signals (light, unset, dark); SDL updated its value before each typed callback, and `IsDarkMode()` matched it inside the callback. The test does not change or verify this GNOME session's actual preference.

`DisplayServerCloseEventsTests` passed with the SDL dummy driver and on Wayland. Synthetic close requests verified main-window filtering, queue order around quit, no automatic window destruction, delivery while dropping input, and continuation after a throwing close callback. `ELECTRON2D_TEST_DISPLAY_WINDOW_EVENTS=1` then opened a visible Wayland window without pushing events: a real compositor close request reached `CloseRequested` once, and the native window stayed alive. That session also delivered two focus gains, one focus loss, one pointer entry, and one pointer exit to the typed events; the synthetic tests cover their ordering, filtering, and failure behavior.

`ELECTRON2D_TEST_DISPLAY_DIALOG_NATIVE=1` opened a visible Wayland message box. Selecting `Choose` returned index `1` on the opening thread before `DialogShow` returned. Closing a second one-button message box through its title bar returned `0` on that host; the first test's assertion that dismissal must return `-1` failed because neither the native toolkit nor the pinned API promises that value. The current opt-in test accepts both observed button-zero and explicit unselected-close results. File chooser acceptance, cancellation, and selected-filter behavior remain unverified on Wayland.

`DisplayServerKeyboardNativeTests` passed within the Wayland native smoke after the physical-key correction. It checks number-row Shift mapping, US bracket/grave/ISO physical identities, modifier preservation, and press/release state for a brace-position scancode. A separate native SDL probe returned the same keypad keycodes with Num Lock off, Num Lock on, and Shift held; the active keymap's navigation and printable keypad labels remain outside the current SDL conversion.

`env -u LD_LIBRARY_PATH ELECTRON2D_TEST_DISPLAY=1 SDL_VIDEODRIVER=dummy dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release` exercises open/reopen, owner-thread rejection, screen selector/default/fallback behavior including the unavailable refresh-rate result, theme-query mapping to SDL light/dark/unknown state, theme-event queue order and callback-failure continuation, capability IDs and clipboard round trip, window queries and mutations, cursor/mouse-mode IDs and terminal-marker rejection, supported and unsupported window-policy handling, pointer enter/exit and content-scale event order, foreign-window filtering and callback-failure continuation, re-entry rejection, key and pointer modifier ordering, mouse/touch emulation, both wheel axes after one callback fails, file-drop batching, dialog argument and callback lifetime, image conversion, and unsupported icon failure. The separate `ELECTRON2D_TEST_DISPLAY_NATIVE=1` path in the published Linux x64 test executable opened a visible window, queried the screen and borrowed handles, changed title and minimum size, and pumped events on this machine's Wayland and XWayland backends with its packaged SDL and no `LD_LIBRARY_PATH`. The native Wayland smoke checks all five mouse modes against SDL state, synthetic pointer coordinates at pixel density 1.25, a held-button mask at rest, rejection of an unsupported warp without synthetic movement, and a read-only cross-process clipboard and primary-selection comparison with a visible keyboard-focused child while both selections held stable nonempty text. A separate user-assisted visible-window test confirmed real focused pointer motion, left-button press/release, and relative motion under `Captured`; all five modes matched native SDL cursor and grab state. An earlier focused warp assertion failed: SDL briefly reported the target while the physical pointer remained elsewhere. The Wayland path now rejects the unavailable capability before SDL is called. A separate 25-second `Confined` run reached all four edges of a 600×450 client area (`x=0…599`, `y=0…449`) without losing mouse focus while the user pushed outward. It also checks the primary-screen keyboard-focus fallback and SDL text-input activation/deactivation. A prior temporary X11 consumer exercised the mouse-focus screen query. A private headless Mutter compositor did not focus the SDL publisher or deliver an input serial, so no clipboard setter publication was exercised. Its missing input-capable virtual seat is the trigger for a focused cross-process publication check that keeps the user's clipboard untouched. These checks do not validate real operating-system theme, physical DPI behavior, full compositor behavior, platform focus policy, icon display, successful real pointer warping, cross-process clipboard publication after a valid recent-input serial, native file chooser selection, real IME composition, touch hardware, high-DPI positioning, other-target packaging, or other operating systems. The published user example starts on Wayland with its packaged native SDL and no development library path. Dummy host checks cover loop ordering, callback failures, and teardown; user-assisted physical arrow-key input, scene movement, Escape exit, and window-close exit passed in the example; remaining dialog/compositor checks still need native acceptance.

The current Wayland native smoke checked three monitors, including one with 1.25 compositor scaling: SDL's logical 1536×864 bounds and 1.25 pixel density produce the physical 1920×1080 screen size. The indexed scale is two there, and `ScreenGetMaxScale()` is two; a Wayland protocol trace independently observed `wl_output.scale` values two, one, and one. SDL display content scale was one on all three displays and cannot supply the indexed value. A Wayland protocol trace also confirmed that all three SDL `xdg-output logical_position` values equal their `wl_output.geometry` positions on this host. The latest test window ran at pixel density 1.25 with a 320×240 logical / 400×300 pixel buffer. Native size requests, constraints, client-rectangle callbacks, and synthetic pointer-event pixel conversion passed at that density. A separate visible SDL software-renderer test then moved between density 1.25 and 1 and back. Two native scale events arrived, and observed pixel size plus native minimum/maximum limits matched the public values after both moves. A newly created or hidden window can expose a provisional scale before its preferred fractional scale arrives; first-buffer scale-event ordering remains for the first renderer integration. Synthetic IME checks cover the 1×10 candidate area, composition and commit state before callbacks, negative selection offsets, and cleanup; a real IME popup was not exercised. Native window checks exercised Wayland focus, minimum/maximum constraints, size clamping, borderless and resize-disabled toggles, maximized-to-windowed and fullscreen-to-windowed restoration, and ordinary fullscreen mapping for an exclusive request. A targeted Wayland run passed checks that reject unsupported topmost/focus flags without SDL state mutation and treat fullscreen as compatible with a later maximize request; the full Wayland native smoke passed twice after the initial-scale expectation was corrected. A minimize-and-restore attempt exposed the protocol limitation: programmatic restoration is unreliable, so minimization runs last in the smoke test.

`env -u LD_LIBRARY_PATH ELECTRON2D_TEST_DISPLAY_IME_MOVE=1 SDL_VIDEODRIVER=wayland dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release --no-restore` passed after a user moved the visible test window onto the 1.25-scale monitor. SDL then reported density 1.25, and the native candidate area was `(100, 60, 1, 10)` with cursor offset zero for the public client-pixel position `(125, 75)`. This checked native coordinate conversion, not a visible input-method popup.

The same Wayland screen probe returned precise refresh rates of 143.98, 143.997, and 143.997 Hz, matching the observed `wl_output.mode` values. SDL's rounded float had reported 144 for the latter two outputs.

The native Wayland handle probe compared the two public borrowed values with SDL's exact `wl_display` and `wl_surface` window properties; both matched and were nonzero.

The dummy run also verifies successive full `Rect2i` snapshots for move and resize, order relative to quit, duplicate and foreign-window filtering, rejection of an off-thread pump without consuming its queue, and delivery after a failing rectangle handler with the failure aggregated. A native Wayland size request produced a real client-rectangle callback and pixel-size readback at density 1.25. A user-driven move/resize sequence and Wayland's zero-position convention remain unverified.

The X11 native smoke in `tests/Electron2D.Tests/DisplayServerNativeSmokeTests.cs` checked half-open client hit bounds and rejection of a point in the title bar or border when SDL reported nonzero border size. It also moved a normal window between two XWayland displays, checked the resulting screen, position within two units of the relative-offset formula, and preserved mode, then restored the original position. The dummy suite checked same-screen no-op and invalid screen/window rejection. Maximized, desktop fullscreen, and exclusive multi-display moves remain unverified on a real display. The dummy suite checked independent logical and scancode-derived label key state and release, non-Latin label conversion, and left/right/unspecified location for nine scancodes on press and release; a physical keyboard layout was not exercised.

## Known limitations

The complete coverage and exact implementation triggers for absent services are tracked in [the coverage inventory](../coverage/classes/DisplayServer.md). The retired `accessibility_*` and `global_menu_*` entry points and their obsolete enum vocabulary are permanently excluded from this class under [ADR 0041](../decisions/display.md#adr-0041); accessibility and menu behavior belongs to separately accepted services. Borrowed OS display/window identities are available on the listed desktop drivers; five Linux compatibility GL/EGL/GLX identities now execute with native ownership checks; native views, mobile identities and other-platform graphics integration remain incomplete under [ADR 0042](../decisions/display.md#adr-0042). Four current accessibility preference queries remain in this class's blocked coverage. The typed main-window events cover close, focus, pointer enter/exit, complete client-rectangle changes, and native content-scale changes; `WindowDpiChanged` is a content-scale event; its ordering with a simultaneous rectangle update and the first rendered buffer remains unverified. Other `WindowEvent` identities require the native-host or renderer integrations named in the coverage register. The current theme API reports SDL's light/dark/unknown preference and change events, plus Linux Wayland/X11 Settings portal capability at server creation. A portal becoming available after creation requires reopening to refresh support. Other drivers use SDL's known-theme result; Windows' native theme API availability is not yet proven equivalent to that result. Accent colors, high contrast, and other appearance settings are absent; an unset portal preference, physical appearance transitions, and native change delivery remain unverified. Window/Engine.Run has executable initial rendering/presentation. Advanced text/option dialogs, speech, mobile hosts, multiwindow scene composition, tablet integration, keyboard layout switching, image clipboard codecs and additional platform-specific controls remain absent without public compatibility stubs. Native file dialogs cannot combine file and folder selection or control hidden-file visibility; on Linux the `showHidden` parameter is accepted and ignored. MIME-only filters reject before native UI opens. The first native Linux portal FileChooser bridge must implement MIME terms and verify the selected-filter result on Wayland; combined file-or-folder selection requires a separately approved chooser capability. Wayland has no reliable global pointer or top-level window-position query; `WindowRectChanged` uses a conventional zero position there, and the mouse-focus screen selector returns `InvalidScreen` when this server's window does not own mouse focus. Linux x64 native library packaging is implemented for the user example. Windows, macOS, Android, iOS, and a Web host and browser-compatible display backend remain unresolved.

## Relevant decisions

- [ADR 0040: SDL display server ownership and event pump](../decisions/display.md#adr-0040)
- [ADR 0041: retired service API boundary](../decisions/display.md#adr-0041)
- [ADR 0042: borrowed native window handles](../decisions/display.md#adr-0042)
- [ADR 0043: system theme queries and typed change notification](../decisions/display.md#adr-0043)
- [ADR 0044: window scale notification and event failure completeness](../decisions/display.md#adr-0044)
- [ADR 0038: typed input and native adapter trigger](../decisions/input.md#adr-0038)
- [ADR 0028: renderer boundary](../decisions/rendering.md#adr-0028)
- [ADR 0021: runtime target matrix](../decisions/product.md#adr-0021)
