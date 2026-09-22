using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    /// <summary>Identifies a display-server capability for <see cref="HasFeature"/>.</summary>
    public enum Feature
    {
        /// <summary>Independent native child windows.</summary>
        Subwindows = 1,
        /// <summary>Touchscreen input.</summary>
        Touchscreen = 2,
        /// <summary>Mouse input.</summary>
        Mouse = 3,
        /// <summary>Pointer warping.</summary>
        MouseWarp = 4,
        /// <summary>Text clipboard access.</summary>
        Clipboard = 5,
        /// <summary>An on-screen keyboard.</summary>
        VirtualKeyboard = 6,
        /// <summary>System cursor shapes.</summary>
        CursorShape = 7,
        /// <summary>Image-backed custom cursors.</summary>
        CustomCursorShape = 8,
        /// <summary>Native message dialogs.</summary>
        NativeDialog = 9,
        /// <summary>Input-method composition.</summary>
        Ime = 10,
        /// <summary>Per-pixel window transparency.</summary>
        WindowTransparency = 11,
        /// <summary>Display content-scale queries.</summary>
        Hidpi = 12,
        /// <summary>Window icon changes.</summary>
        Icon = 13,
        /// <summary>Platform-native icon resources.</summary>
        NativeIcon = 14,
        /// <summary>Device orientation control.</summary>
        Orientation = 15,
        /// <summary>Presentation swap-interval control.</summary>
        SwapBuffers = 16,
        /// <summary>A separate primary-selection clipboard.</summary>
        ClipboardPrimary = 18,
        /// <summary>Text-to-speech output.</summary>
        TextToSpeech = 19,
        /// <summary>Content drawn under native title controls.</summary>
        ExtendToTitle = 20,
        /// <summary>Desktop screen capture.</summary>
        ScreenCapture = 21,
        /// <summary>System status indicators.</summary>
        StatusIndicator = 22,
        /// <summary>Native help-search integration.</summary>
        NativeHelp = 23,
        /// <summary>Native text-input dialogs.</summary>
        NativeDialogInput = 24,
        /// <summary>Native file-selection dialogs.</summary>
        NativeDialogFile = 25,
        /// <summary>Native file dialogs with additional options and virtual paths.</summary>
        NativeDialogFileExtra = 26,
        /// <summary>Interactive native window dragging and resizing.</summary>
        WindowDrag = 27,
        /// <summary>Window exclusion from ordinary screen capture.</summary>
        ScreenExcludeFromCapture = 28,
        /// <summary>Embedding of external native windows.</summary>
        WindowEmbedding = 29,
        /// <summary>MIME-based native file-dialog filters.</summary>
        NativeDialogFileMime = 30,
        /// <summary>A system emoji and symbol picker.</summary>
        EmojiAndSymbolPicker = 31,
        /// <summary>A native color picker.</summary>
        NativeColorPicker = 32,
        /// <summary>Automatic popup fitting to display bounds.</summary>
        SelfFittingWindows = 33,
        /// <summary>Screen-reader accessibility integration.</summary>
        AccessibilityScreenReader = 34,
        /// <summary>High-dynamic-range presentation.</summary>
        HdrOutput = 35,
        /// <summary>Picture-in-picture presentation.</summary>
        PipMode = 36,
    }

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
    public bool HasFeature(Feature feature)
    {
        EnsureOwner();
        var driver = SDL.GetCurrentVideoDriver();
        var desktop = driver is "windows" or "x11" or "wayland" or "cocoa";
        return feature switch
        {
            Feature.Touchscreen => IsTouchscreenAvailable(),
            Feature.Mouse => SDL.HasMouse(),
            Feature.MouseWarp => driver is "windows" or "x11" or "cocoa" && SDL.HasMouse(),
            Feature.Clipboard => true,
            Feature.CursorShape or Feature.CustomCursorShape => desktop && SDL.HasMouse(),
            Feature.NativeDialog or Feature.NativeDialogFile => desktop,
            Feature.Hidpi => desktop && GetScreenCount() > 0,
            Feature.Icon => driver is "windows" or "x11" or "cocoa" ||
                driver == "wayland" && _nativeWaylandIconAvailable,
            Feature.ClipboardPrimary => driver is "x11" or "wayland",
            Feature.Ime => desktop,
            _ => false,
        };
    }
}
