# DisplayServer.Feature

Last updated: 2026-09-22

**Inherits:** `System.Enum`

**Inherited By:** —

**Source:** [`src/Servers/Display/DisplayServer.Capabilities.cs`](../../src/Servers/Display/DisplayServer.Capabilities.cs)

**Declaration:** `public enum Feature` nested in [`DisplayServer`](DisplayServer.md)

## Description

Stable capability identifiers for [`DisplayServer.HasFeature`](DisplayServer.md#method-hasfeature). A declared identifier does not imply that the current host implements it. The query returns false for absent engine services and unknown numeric IDs; device-sensitive results can change at runtime. Numeric ID zero is deliberately absent because the deprecated global-menu capability was removed from this class under [ADR 0041](../decisions/display.md#adr-0041); native menus belong to a separately accepted service.

The current host advertises `Clipboard` while open; `Mouse` when SDL reports a mouse; and `Touchscreen` when SDL reports a touch device or `Input.EmulateTouchFromMouse` is enabled. It advertises `MouseWarp` on Windows, X11, and macOS with a mouse; `CursorShape` and `CustomCursorShape` on Windows, X11, Wayland, and macOS with a mouse; `NativeDialog`, `NativeDialogFile`, and `Ime` on those four desktop drivers; `Hidpi` on those desktop drivers with at least one display; `Icon` on Windows, X11, and macOS; and `ClipboardPrimary` on X11 and Wayland. All other values currently return `false`. Native operations can still fail after a positive query. The dummy driver and native Wayland/XWayland smoke paths exercise only a subset of these rules; actual dialog selection and other target hosts remain unverified.

## Enumeration Summary

| Value | Capability |
| --- | --- |
| [`Subwindows = 1`](#value-subwindows) | Independent native child windows. |
| [`Touchscreen = 2`](#value-touchscreen) | Touchscreen input. |
| [`Mouse = 3`](#value-mouse) | Mouse input. |
| [`MouseWarp = 4`](#value-mousewarp) | Pointer warping. |
| [`Clipboard = 5`](#value-clipboard) | Text clipboard access. |
| [`VirtualKeyboard = 6`](#value-virtualkeyboard) | An on-screen keyboard. |
| [`CursorShape = 7`](#value-cursorshape) | System cursor shapes. |
| [`CustomCursorShape = 8`](#value-customcursorshape) | Image-backed custom cursors. |
| [`NativeDialog = 9`](#value-nativedialog) | Native message dialogs. |
| [`Ime = 10`](#value-ime) | Input-method composition. |
| [`WindowTransparency = 11`](#value-windowtransparency) | Per-pixel window transparency. |
| [`Hidpi = 12`](#value-hidpi) | Display content-scale queries. |
| [`Icon = 13`](#value-icon) | Window icon changes. |
| [`NativeIcon = 14`](#value-nativeicon) | Platform-native icon resources. |
| [`Orientation = 15`](#value-orientation) | Device orientation control. |
| [`SwapBuffers = 16`](#value-swapbuffers) | Presentation swap-interval control. |
| [`ClipboardPrimary = 18`](#value-clipboardprimary) | A separate primary-selection clipboard. |
| [`TextToSpeech = 19`](#value-texttospeech) | Text-to-speech output. |
| [`ExtendToTitle = 20`](#value-extendtotitle) | Content drawn under native title controls. |
| [`ScreenCapture = 21`](#value-screencapture) | Desktop screen capture. |
| [`StatusIndicator = 22`](#value-statusindicator) | System status indicators. |
| [`NativeHelp = 23`](#value-nativehelp) | Native help-search integration. |
| [`NativeDialogInput = 24`](#value-nativedialoginput) | Native text-input dialogs. |
| [`NativeDialogFile = 25`](#value-nativedialogfile) | Native file-selection dialogs. |
| [`NativeDialogFileExtra = 26`](#value-nativedialogfileextra) | Native file dialogs with additional options and virtual paths. |
| [`WindowDrag = 27`](#value-windowdrag) | Interactive native window dragging and resizing. |
| [`ScreenExcludeFromCapture = 28`](#value-screenexcludefromcapture) | Window exclusion from ordinary screen capture. |
| [`WindowEmbedding = 29`](#value-windowembedding) | Embedding of external native windows. |
| [`NativeDialogFileMime = 30`](#value-nativedialogfilemime) | MIME-based native file-dialog filters. |
| [`EmojiAndSymbolPicker = 31`](#value-emojiandsymbolpicker) | A system emoji and symbol picker. |
| [`NativeColorPicker = 32`](#value-nativecolorpicker) | A native color picker. |
| [`SelfFittingWindows = 33`](#value-selffittingwindows) | Automatic popup fitting to display bounds. |
| [`AccessibilityScreenReader = 34`](#value-accessibilityscreenreader) | Screen-reader accessibility integration. |
| [`HdrOutput = 35`](#value-hdroutput) | High-dynamic-range presentation. |
| [`PipMode = 36`](#value-pipmode) | Picture-in-picture presentation. |

## Enumeration Descriptions

<a id="value-subwindows"></a>
### `Subwindows = 1`

Independent native child windows.

<a id="value-touchscreen"></a>
### `Touchscreen = 2`

Touchscreen input.

<a id="value-mouse"></a>
### `Mouse = 3`

Mouse input.

<a id="value-mousewarp"></a>
### `MouseWarp = 4`

Pointer warping.

<a id="value-clipboard"></a>
### `Clipboard = 5`

Text clipboard access.

<a id="value-virtualkeyboard"></a>
### `VirtualKeyboard = 6`

An on-screen keyboard.

<a id="value-cursorshape"></a>
### `CursorShape = 7`

System cursor shapes.

<a id="value-customcursorshape"></a>
### `CustomCursorShape = 8`

Image-backed custom cursors.

<a id="value-nativedialog"></a>
### `NativeDialog = 9`

Native message dialogs.

<a id="value-ime"></a>
### `Ime = 10`

Input-method composition.

<a id="value-windowtransparency"></a>
### `WindowTransparency = 11`

Per-pixel window transparency.

<a id="value-hidpi"></a>
### `Hidpi = 12`

Display content-scale queries.

<a id="value-icon"></a>
### `Icon = 13`

Window icon changes.

<a id="value-nativeicon"></a>
### `NativeIcon = 14`

Platform-native icon resources.

<a id="value-orientation"></a>
### `Orientation = 15`

Device orientation control.

<a id="value-swapbuffers"></a>
### `SwapBuffers = 16`

Presentation swap-interval control.

<a id="value-clipboardprimary"></a>
### `ClipboardPrimary = 18`

A separate primary-selection clipboard.

<a id="value-texttospeech"></a>
### `TextToSpeech = 19`

Text-to-speech output.

<a id="value-extendtotitle"></a>
### `ExtendToTitle = 20`

Content drawn under native title controls.

<a id="value-screencapture"></a>
### `ScreenCapture = 21`

Desktop screen capture.

<a id="value-statusindicator"></a>
### `StatusIndicator = 22`

System status indicators.

<a id="value-nativehelp"></a>
### `NativeHelp = 23`

Native help-search integration.

<a id="value-nativedialoginput"></a>
### `NativeDialogInput = 24`

Native text-input dialogs.

<a id="value-nativedialogfile"></a>
### `NativeDialogFile = 25`

Native file-selection dialogs.

<a id="value-nativedialogfileextra"></a>
### `NativeDialogFileExtra = 26`

Native file dialogs with additional options and virtual paths.

<a id="value-windowdrag"></a>
### `WindowDrag = 27`

Interactive native window dragging and resizing.

<a id="value-screenexcludefromcapture"></a>
### `ScreenExcludeFromCapture = 28`

Window exclusion from ordinary screen capture.

<a id="value-windowembedding"></a>
### `WindowEmbedding = 29`

Embedding of external native windows.

<a id="value-nativedialogfilemime"></a>
### `NativeDialogFileMime = 30`

MIME-based native file-dialog filters.

<a id="value-emojiandsymbolpicker"></a>
### `EmojiAndSymbolPicker = 31`

A system emoji and symbol picker.

<a id="value-nativecolorpicker"></a>
### `NativeColorPicker = 32`

A native color picker.

<a id="value-selffittingwindows"></a>
### `SelfFittingWindows = 33`

Automatic popup fitting to display bounds.

<a id="value-accessibilityscreenreader"></a>
### `AccessibilityScreenReader = 34`

Screen-reader accessibility integration.

<a id="value-hdroutput"></a>
### `HdrOutput = 35`

High-dynamic-range presentation.

<a id="value-pipmode"></a>
### `PipMode = 36`

Picture-in-picture presentation.

## Lifecycle and invariants

Values are immutable. Numeric ID 17 is intentionally unassigned. Unknown IDs return false from `HasFeature`. Only integrated native capabilities are advertised.

## Threading and interactions

The enum has no thread affinity; querying the active server requires its opening thread. `HasFeature` checks current SDL devices and driver identity where those facts determine availability.

## Verification and limitations

The SDL dummy-driver harness checks representative numeric IDs, an advertised text-clipboard round trip, an unavailable native-dialog query, and unknown-ID handling. Real desktop, mobile, and changing-device behavior has not been verified.

## Relevant decisions

[ADR 0040](../decisions/display.md#adr-0040); see the [coverage inventory](../coverage/classes/DisplayServer.md).
