using Mathf = Electron2D.Mathf;
using IOPath = System.IO.Path;
using Electron2D;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EngineFileAccess = Electron2D.FileAccess;
using EngineTimer = Electron2D.Timer;

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_DISPLAY_CLIPBOARD_CHILD") == "1")
{
    DisplayServerClipboardNativeTests.RunChild();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_DISPLAY_NATIVE") == "1")
{
    DisplayServerNativeSmokeTests.Run();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_POINTER_PIXELS") == "1")
{
    using var display = DisplayServer.Open("Pointer pixel checks", new Vector2i(320, 240));
    DisplayServerPointerPixelNativeTests.Run(display);
    Console.WriteLine("Pointer pixel native checks passed.");
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_TOUCH_INPUT") == "1")
{
    using var display = DisplayServer.Open("Touch input checks", new Vector2i(320, 240));
    DisplayServerTouchNativeTests.Run(display);
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_INPUT_POINTER") == "1")
{
    InputPointerNativeTests.Run();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_GAMEPAD") == "1")
{
    InputGamepadNativeTests.Run();
    InputGamepadNativeTests.RunSceneDelivery();
    InputGamepadNativeTests.RunProjectSetting();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_GAMEPAD_IGNORE") == "1")
{
    InputGamepadNativeTests.RunIgnoredDevice();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_INPUT_MODIFIERS") == "1")
{
    VerifyDisplayServerPointerModifiers();
    Console.WriteLine("Input modifier native checks passed.");
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_CONTROL_HOVER") == "1")
{
    ControlHoverNativeTests.Run();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_DISPLAY_IME_MOVE") == "1")
{
    DisplayServerImeNativeTests.RunMove();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_DISPLAY_THEME_PORTAL") == "1")
{
    DisplayServerThemePortalNativeTests.Run();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_DISPLAY_SCREENSAVER") == "1")
{
    DisplayServerScreenSaverNativeTests.Run();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_DISPLAY_ATTENTION") == "1")
{
    DisplayServerAttentionNativeTests.Run();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_DISPLAY_DIALOG_NATIVE") == "1")
{
    DisplayServerDialogNativeTests.Run();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_DISPLAY_WINDOW_EVENTS") == "1")
{
    DisplayServerWindowEventNativeTests.Run();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_DISPLAY_SCALE_MOVE") == "1")
{
    DisplayServerScaleMoveNativeTests.Run();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_DISPLAY_POINTER_FOCUS") == "1")
{
    DisplayServerPointerFocusNativeTests.Run();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_DISPLAY_POINTER_CONFINE") == "1")
{
    DisplayServerPointerFocusNativeTests.RunConfinement();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_IMAGE_CODECS") == "1")
{
    ImageCodecTests.Run();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_RESOURCE_LOADER") == "1")
{
    ResourceLoaderTests.Run();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_RENDER_HANDLES") == "1")
{
    RenderingNativeHandleTests.Run();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_RENDER") == "1")
{
    RenderingRuntimeTests.Run();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_INTERPOLATION_NATIVE") == "1")
{
    PhysicsInterpolationNativeTests.Run();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_SPRITE") == "1")
{
    SpriteTests.Run();
    return;
}

if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_WINDOW") == "1")
{
    WindowRuntimeTests.Run();
    return;
}

SceneDiagnosticsTests.Run();
GeometryTests.Run();
PathTests.Run();
CurveTests.Run();
CurveTextureTests.Run();
GradientTests.Run();
RandomNumberGeneratorTests.Run();
RegExTests.Run();
XMLParserTests.Run();
BitMapTests.Run();
NoiseTests.Run();
NoiseTextureTests.Run();
FastNoiseLiteTests.Run();
AnimatedSpriteTests.Run();
AnimatedTextureTests.Run();
RenderingRuntimeTests.VerifyAtlasResources();
SceneHierarchyTests.Run();
SceneChangeTests.Run();
NodeTreeDiagnosticsTests.Run();
NodeUniqueNameTests.Run();
NodeReplacementTests.Run();
PhysicsInterpolationTests.Run();
PhysicsBodyTests.Run();
CapsuleShapeTests.Run();
SegmentShapeTests.Run();
SeparationRayShapeTests.Run();
ShapeCollisionTests.Run();
OneWayCollisionTests.Run();
CollisionPolygonTests.Run();
PhysicsQueryTests.Run();
PhysicsShapeQueryTests.Run();
PhysicsMotionTests.Run();
PhysicsCollisionExceptionTests.Run();
CharacterBodyTests.Run();
RayCastTests.Run();
ShapeCastTests.Run();
ConvexPolygonShapeTests.Run();
ConcavePolygonShapeTests.Run();
AnimatableBodyTests.Run();
PhysicsMaterialTests.Run();
AreaTests.Run();
PhysicsAreaFieldTests.Run();
RigidBodyForceTests.Run();
RigidBodyContactTests.Run();
PhysicsBodyStateTests.Run();
ShapeOwnerTests.Run();
ShapePairEventTests.Run();
CollisionDisableModeTests.Run();
AStarTests.Run();
AStarGridTests.Run();
RemoteTransformTests.Run();
ControlLayoutTests.Run();
ControlInputTests.Run();
ControlFocusNavigationTests.Run();
ControlRecursiveBehaviorTests.Run();
ControlClipTests.Run();
ControlHoverTests.Run();
CanvasLifecycleTests.Run();
CanvasSamplingTests.Run();
CanvasPixelSnapTests.Run();
CanvasCoordinateTests.Run();
CameraTests.Run();
JsonTests.Run();
ParallaxTests.Run();
LegacyParallaxTests.Run();
CanvasLayerTests.Run();
CanvasMaskTests.Run();
CanvasTransformNotificationTests.Run();
CanvasPolygonTests.Run();
PolygonTests.Run();
LineTests.Run();
CanvasStrokeTests.Run();
CanvasTimingTests.Run();
VerifyInstanceIds();
VerifyLifetime();
WeakRefTests.Run();
VerifyNotificationsAndProperties();
VerifyEventConnections();
VerifyTranslations();
NodeLocalizationTests.Run();
TranslationDomainTests.Run();
LocalizationProjectSettingsTests.Run();
VerifyMathF();
VerifyColors();
SpriteTests.Run();
VerifyImages();
VerifyVector2Values();
VerifyVector2iValues();
VerifyVector3Values();
VerifyVector3iValues();
VerifyVector4Values();
VerifyVector4iValues();
VerifyRectangles();
VerifyIntegerRectangles();
VerifyTransforms();
VerifyConfigFiles();
VerifyFileAccess();
VerifyDirAccess();
VerifyProjectSettings();
VerifyResources();
VerifyPackedScenes();
VerifyInputMapConfiguration();
VerifyInputMapMatching();
VerifyInputEventActionValues();
VerifyInputText();
VerifyControllerValues();
VerifyControllerText();
VerifyTouchGestureText();
VerifyInput();
InputActionSettingsTests.Run();
VerifyInputEmulation();
VerifyEngine();
VerifyMainLoop();
VerifyNodeHierarchyAndTransforms();
VerifyProcessing();
VerifySceneTree();
VerifySceneTreeGroupsEventsAndTimers();
SceneTreeTimerTests.Run();
VerifyTimers();
VerifyTweens();
VerifyTweenInterpolation();
VerifyTweenTypeLifetime();
VerifyTweenCallbackIntervals();
VerifyTweenMethods();
VerifyTweenProperties();
VerifyTweenSubtweens();
VerifyTweenAwaits();
VerifySceneTreeFailureSafety();
if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_DISPLAY") == "1")
{
    VerifyDisplayServer();
    VerifyDisplayServerPointerModifiers();
    DisplayServerDialogTests.Run();
    DisplayServerClipboardTests.Run();
    using (var display = DisplayServer.Open("Icon checks", new Vector2i(64, 64), hidden: true))
        DisplayServerIconTests.Run(display);
    WindowRuntimeTests.Run();
}

Console.WriteLine("Electron2D checks passed.");

static void VerifyDisplayServer()
{
    Require(DisplayServer.Instance is null, "No display server is open before initialization.");
    Expect<ArgumentOutOfRangeException>(() => DisplayServer.Open("invalid", new Vector2i(0, 40)),
        "A native window requires positive dimensions.");

    using (var display = DisplayServer.Open("Initial", new Vector2i(320, 240), hidden: true))
    {
        Require(ReferenceEquals(DisplayServer.Instance, display), "Opening registers the process display server.");
        var expectedBackendName = SDL3.SDL.GetCurrentVideoDriver() switch
        {
            "dummy" => "headless",
            "wayland" => "Wayland",
            "x11" => "X11",
            var driver => driver,
        };
        Require(display.GetName() == expectedBackendName && display.GetScreenCount() > 0,
            "The backend reports its public name and an active display.");
        DisplayServerKeyboardNativeTests.Run(display);
        var systemTheme = SDL3.SDL.GetSystemTheme();
        Require(display.IsDarkMode() == (systemTheme == SDL3.SDL.SystemTheme.Dark) &&
                display.IsDarkModeSupported() == (systemTheme != SDL3.SDL.SystemTheme.Unknown),
            "Theme queries reflect the native light, dark, or unknown state.");
        Expect<InvalidOperationException>(() => Task.Run(display.IsDarkMode).GetAwaiter().GetResult(),
            "Theme queries remain on the opening thread.");
        Expect<InvalidOperationException>(() => Task.Run(display.IsDarkModeSupported).GetAwaiter().GetResult(),
            "Theme support queries remain on the opening thread.");
        Require(display.HasHardwareKeyboard(),
            "Desktop hosts report hardware keyboard support independent of attached devices.");
        var priorTouchEmulation = Input.Instance.EmulateTouchFromMouse;
        try
        {
            Input.Instance.EmulateTouchFromMouse = false;
            var nativeTouchAvailable = display.IsTouchscreenAvailable();
            Input.Instance.EmulateTouchFromMouse = true;
            Require(display.IsTouchscreenAvailable(),
                "Mouse-to-touch emulation makes touchscreen input available without native touch hardware.");
            Input.Instance.EmulateTouchFromMouse = false;
            Require(display.IsTouchscreenAvailable() == nativeTouchAvailable,
                "Disabling emulation restores the native touch-device result.");
        }
        finally
        {
            Input.Instance.EmulateTouchFromMouse = priorTouchEmulation;
        }
        var wasKeptOn = display.ScreenIsKeptOn();
        try
        {
            display.ScreenSetKeepOn(!wasKeptOn);
            Require(display.ScreenIsKeptOn() != wasKeptOn,
                "Accepted screen blanking requests are observable through the public API.");
            display.ScreenSetKeepOn(wasKeptOn);
        }
        catch (InvalidOperationException)
        {
            Require(display.ScreenIsKeptOn() == wasKeptOn,
                "A rejected screen blanking request does not pretend to change native state.");
        }
        Require((int)DisplayServer.Feature.Mouse == 3 &&
                (int)DisplayServer.Feature.ClipboardPrimary == 18 &&
                (int)DisplayServer.Feature.PipMode == 36 &&
                display.HasFeature(DisplayServer.Feature.Clipboard) &&
                !display.HasFeature(DisplayServer.Feature.NativeDialog) &&
                !display.HasFeature((DisplayServer.Feature)999),
            "Feature IDs remain stable and capability queries do not advertise absent services.");
        Require((int)DisplayServer.HandleType.DisplayHandle == 0 &&
                (int)DisplayServer.HandleType.WindowHandle == 1,
            "Native handle categories retain their display/window numeric identities.");
        Expect<NotSupportedException>(() => display.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle),
            "The dummy video driver has no operating-system window handle.");
        Expect<ArgumentOutOfRangeException>(() => display.WindowGetNativeHandle((DisplayServer.HandleType)99),
            "Undefined native handle categories are rejected.");
        Expect<ArgumentOutOfRangeException>(() => display.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, 1),
            "Native handles belong only to the main window.");
        Expect<InvalidOperationException>(() => Task.Run(() =>
            display.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle)).GetAwaiter().GetResult(),
            "Native handles are queried only on the opening thread.");
        display.ClipboardSet("DisplayServer clipboard probe");
        Require(display.ClipboardHas() && display.ClipboardGet() == "DisplayServer clipboard probe",
            "The advertised text clipboard round-trips through the dummy native driver.");
        var mainScreen = display.WindowGetCurrentScreen();
        var nativeRefreshRate = SDL3.SDL.GetCurrentDisplayMode(
            SDL3.SDL.GetDisplays(out _)![mainScreen])?.RefreshRate ?? 0f;
        Require(display.ScreenGetRefreshRate(mainScreen) ==
                (float.IsFinite(nativeRefreshRate) && nativeRefreshRate > 0f ? nativeRefreshRate : -1f),
            "A missing or nonpositive native refresh rate is reported as unavailable.");
        Require(display.WindowGetCurrentScreen(DisplayServer.InvalidWindowId) == DisplayServer.InvalidScreen,
            "An unknown window has no current screen.");
        var unchangedPosition = display.WindowGetPosition();
        var unchangedMode = display.WindowGetMode();
        display.WindowSetCurrentScreen(mainScreen);
        display.WindowSetCurrentScreen(DisplayServer.ScreenOfMainWindow);
        Require(display.WindowGetPosition() == unchangedPosition && display.WindowGetMode() == unchangedMode,
            "Selecting the current screen, including its selector, leaves the window unchanged.");
        Expect<ArgumentOutOfRangeException>(() => display.WindowSetCurrentScreen(int.MaxValue),
            "An unknown target display is rejected before a move.");
        Expect<ArgumentOutOfRangeException>(() => display.WindowSetCurrentScreen(mainScreen, 1),
            "An unknown window ID is rejected even when the target is the current screen.");
        Require(mainScreen >= 0 && display.ScreenGetPosition() == display.ScreenGetPosition(mainScreen) &&
                display.ScreenGetSize() == display.ScreenGetSize(mainScreen) &&
                display.ScreenGetScale() == display.ScreenGetScale(mainScreen) &&
                display.ScreenGetRefreshRate() == display.ScreenGetRefreshRate(mainScreen),
            "Default screen queries select the display containing the main window.");
        Require(display.ScreenGetSize(DisplayServer.ScreenPrimary) ==
                display.ScreenGetSize(display.GetPrimaryScreen()) &&
                display.ScreenGetSize(DisplayServer.ScreenWithKeyboardFocus).X > 0 &&
                display.ScreenGetSize(DisplayServer.ScreenWithMouseFocus).X > 0,
            "Negative screen selectors resolve to connected displays.");
        Require(display.ScreenGetPosition(int.MaxValue) == Vector2i.Zero &&
                display.ScreenGetSize(int.MaxValue) == Vector2i.Zero &&
                display.ScreenGetScale(int.MaxValue) == 1f &&
                display.ScreenGetRefreshRate(int.MaxValue) == -1f,
            "Invalid screen queries return their documented fallbacks.");
        var screenPosition = display.ScreenGetPosition();
        Require(display.GetScreenFromRect(new Rect2(screenPosition.X, screenPosition.Y, 1, 1)) == mainScreen &&
                display.GetScreenFromRect(new Rect2(screenPosition.X, screenPosition.Y, 0.5f, 0.5f)) ==
                DisplayServer.InvalidScreen &&
                display.GetScreenFromRect(new Rect2(screenPosition.X, screenPosition.Y, 0, 1)) ==
                DisplayServer.InvalidScreen &&
                display.GetScreenFromRect(new Rect2(float.NaN, 0, 1, 1)) == DisplayServer.InvalidScreen,
            "Screen overlap requires at least one whole pixel of finite intersection.");
        Require(display.WindowGetTitle() == "Initial" && display.WindowGetSize() == new Vector2i(320, 240),
            "The native window exposes its initial title and logical size.");
        Require(display.WindowGetMinSize() == new Vector2i(64, 64),
            "The main window starts with its documented minimum size.");
        display.WindowSetTitle("Updated");
        Expect<ArgumentNullException>(() => display.WindowSetTitle(null!),
            "A null title is rejected without changing the native title.");
        Expect<ArgumentOutOfRangeException>(() => display.WindowSetTitle("Wrong window", 1),
            "A title request for an unknown window is rejected.");
        display.WindowSetSize(new Vector2i(400, 300));
        Require(display.WindowGetTitle() == "Updated" && display.WindowGetSize() == new Vector2i(400, 300),
            "Native title and size mutations are observable.");
        Expect<InvalidOperationException>(() => DisplayServer.Open("duplicate", new Vector2i(100, 100)),
            "Only one native display server can own the process window.");
        Expect<ArgumentOutOfRangeException>(() => display.WindowGetSize(1),
            "Unknown window identifiers fail explicitly.");
        Expect<InvalidOperationException>(() => Task.Run(display.GetScreenCount).GetAwaiter().GetResult(),
            "Native display calls remain on the opening thread.");

        var quitCount = 0;
        display.QuitRequested += () => quitCount++;
        var quit = new SDL3.SDL.Event { Type = (uint)SDL3.SDL.EventType.Quit };
        Require(SDL3.SDL.PushEvent(ref quit), "The native queue accepts a quit request.");
        display.ProcessEvents();
        Require(quitCount == 1, "The display event pump delivers quit requests synchronously.");
        Action failingQuit = () => throw new InvalidOperationException("injected display callback failure");
        display.QuitRequested += failingQuit;
        Require(SDL3.SDL.PushEvent(ref quit) && SDL3.SDL.PushEvent(ref quit),
            "The native queue accepts multiple quit requests.");
        try
        {
            display.ProcessEvents();
            throw new Exception("The display pump must aggregate callback failures.");
        }
        catch (AggregateException errors)
        {
            Require(errors.InnerExceptions.Count == 2 && quitCount == 3,
                "Callback failure does not prevent later native events from being delivered.");
        }
        display.QuitRequested -= failingQuit;
        Action reenter = () => Expect<InvalidOperationException>(display.ProcessEvents,
            "The display event pump rejects callback re-entry.");
        display.QuitRequested += reenter;
        Require(SDL3.SDL.PushEvent(ref quit), "The native queue accepts a re-entry probe.");
        display.ProcessEvents();
        display.QuitRequested -= reenter;

        var themeDelivery = new List<string>();
        Action themeChanged = () => themeDelivery.Add("theme");
        Action themeOrderedQuit = () => themeDelivery.Add("quit");
        display.SystemThemeChanged += themeChanged;
        display.QuitRequested += themeOrderedQuit;
        var nativeThemeChanged = new SDL3.SDL.Event { Type = (uint)SDL3.SDL.EventType.SystemThemeChanged };
        try
        {
            Require(SDL3.SDL.PushEvent(ref nativeThemeChanged) && SDL3.SDL.PushEvent(ref quit) &&
                    SDL3.SDL.PushEvent(ref nativeThemeChanged),
                "The native queue accepts system theme changes between quit requests.");
            display.ProcessEvents();
            Require(themeDelivery.SequenceEqual(["theme", "quit", "theme"]),
                "Global theme events retain queue order among other global events.");

            Action failingTheme = () => throw new InvalidOperationException("injected theme callback failure");
            display.SystemThemeChanged += failingTheme;
            try
            {
                Require(SDL3.SDL.PushEvent(ref nativeThemeChanged) && SDL3.SDL.PushEvent(ref quit),
                    "The native queue accepts a failing theme callback followed by quit.");
                try
                {
                    display.ProcessEvents();
                    throw new Exception("A failing theme callback must be reported after the queue drains.");
                }
                catch (AggregateException errors)
                {
                    Require(errors.InnerExceptions.Count == 1 &&
                            themeDelivery.SequenceEqual(["theme", "quit", "theme", "theme", "quit"]),
                        "A theme callback failure is aggregated after later global events are delivered.");
                }
            }
            finally
            {
                display.SystemThemeChanged -= failingTheme;
            }
        }
        finally
        {
            display.SystemThemeChanged -= themeChanged;
            display.QuitRequested -= themeOrderedQuit;
        }

        Require(!display.WindowGetFlag(DisplayServer.WindowFlag.ResizeDisabled) &&
                (int)DisplayServer.WindowFlag.ResizeDisabled == 0 &&
                (int)DisplayServer.WindowFlag.Borderless == 1 &&
                (int)DisplayServer.WindowFlag.Transparent == 3 &&
                (int)DisplayServer.WindowFlag.NoFocus == 4 &&
                (int)DisplayServer.WindowFlag.Max == 13 &&
                (int)DisplayServer.WindowMode.ExclusiveFullscreen == 4,
            "The native main window starts resizable and flag IDs match the public contract.");
        display.WindowSetFlag(DisplayServer.WindowFlag.ResizeDisabled, true);
        display.ProcessEvents();
        var observedResizable = (SDL3.SDL.GetWindowFlags(SDL3.SDL.GetWindows(out _)![0]) &
            SDL3.SDL.WindowFlags.Resizable) != 0;
        Require(display.WindowGetFlag(DisplayServer.WindowFlag.ResizeDisabled) == !observedResizable,
            "The resize-disabled flag is the inverse of the observed native resizable flag.");
        display.WindowSetFlag(DisplayServer.WindowFlag.ResizeDisabled, false);
        Expect<NotSupportedException>(() => display.WindowSetFlag(DisplayServer.WindowFlag.Transparent, true),
            "A defined flag requiring the absent renderer rejects mutation explicitly.");
        Expect<NotSupportedException>(() => display.WindowGetFlag(DisplayServer.WindowFlag.Transparent),
            "A defined but unavailable flag is not reported as native state.");
        Expect<ArgumentOutOfRangeException>(() => display.WindowSetMode((DisplayServer.WindowMode)99),
            "Unknown window modes are rejected before native mutation.");
        Expect<ArgumentOutOfRangeException>(() => display.WindowSetFlag(DisplayServer.WindowFlag.Max, true),
            "The terminal enum marker is not a policy.");
        Expect<ArgumentOutOfRangeException>(() => display.WindowSetFlag((DisplayServer.WindowFlag)100, true),
            "An unknown window flag is rejected before native mutation.");
        Require((int)DisplayServer.CursorShape.Drag == 6 &&
                (int)DisplayServer.CursorShape.CanDrop == 7 &&
                (int)DisplayServer.CursorShape.Forbidden == 8 &&
                (int)DisplayServer.CursorShape.VSize == 9 &&
                (int)DisplayServer.CursorShape.HSize == 10 &&
                (int)DisplayServer.CursorShape.BDiagSize == 11 &&
                (int)DisplayServer.CursorShape.FDiagSize == 12 &&
                (int)DisplayServer.CursorShape.Move == 13 &&
                (int)DisplayServer.CursorShape.Help == 16 &&
                (int)DisplayServer.CursorShape.Max == 17 &&
                (int)DisplayServer.MouseMode.Max == 5,
            "Pointer enum values retain their complete public identities.");
        Expect<ArgumentOutOfRangeException>(() => display.CursorSetShape(DisplayServer.CursorShape.Max),
            "The cursor shape terminal marker cannot be selected.");
        Expect<ArgumentOutOfRangeException>(() => display.CursorSetCustomImage(null, DisplayServer.CursorShape.Max),
            "The cursor shape terminal marker is not a custom slot.");
        Expect<ArgumentOutOfRangeException>(() => display.MouseSetMode(DisplayServer.MouseMode.Max),
            "The mouse-mode terminal marker cannot be applied.");
        Require(!display.HasFeature(DisplayServer.Feature.MouseWarp),
            "The dummy backend does not advertise pointer warping.");
        Expect<NotSupportedException>(() => display.WarpMouse(new Vector2i(20, 20)),
            "An unavailable pointer warp rejects use before native mutation.");

        using (var icon = Image.CreateFromData(2, 2, false, Image.Format.Rgba8,
            [255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 255, 255]))
        {
            Expect<InvalidOperationException>(() => display.WindowSetIcon(icon),
                "The dummy video backend reports unsupported icon installation explicitly.");
            Expect<ArgumentOutOfRangeException>(() => display.CursorSetCustomImage(icon,
                hotspot: new Vector2i(2, 0)), "A cursor hotspot must remain inside its image.");
        }

        var windows = SDL3.SDL.GetWindows(out var windowCount);
        Require(windows is { Length: 1 } && windowCount == 1, "The display owns exactly one native window.");
        var nativeWindowId = SDL3.SDL.GetWindowID(windows![0]);
        var pointerDelivery = new List<string>();
        Action entered = () => pointerDelivery.Add("enter");
        Action exited = () => pointerDelivery.Add("exit");
        Action orderedQuit = () => pointerDelivery.Add("quit");
        display.WindowMouseEntered += entered;
        display.WindowMouseExited += exited;
        display.QuitRequested += orderedQuit;
        try
        {
            var pointerWindowEvent = new SDL3.SDL.Event
            {
                Window = new SDL3.SDL.WindowEvent
                {
                    Type = SDL3.SDL.EventType.WindowMouseEnter,
                    WindowID = nativeWindowId,
                },
            };
            Require(SDL3.SDL.PushEvent(ref pointerWindowEvent) && SDL3.SDL.PushEvent(ref quit),
                "The native queue accepts pointer-enter and quit events.");
            pointerWindowEvent.Window.Type = SDL3.SDL.EventType.WindowMouseLeave;
            Require(SDL3.SDL.PushEvent(ref pointerWindowEvent),
                "The native queue accepts pointer-leave events.");
            Expect<InvalidOperationException>(() => Task.Run(display.ProcessEvents).GetAwaiter().GetResult(),
                "Window callbacks may be pumped only on the opening thread.");
            Require(pointerDelivery.Count == 0, "An off-thread pump leaves queued window events untouched.");
            display.ProcessEvents();
            Require(pointerDelivery.SequenceEqual(["enter", "quit", "exit"]),
                "Pointer focus callbacks follow native event order among other window events.");

            pointerWindowEvent.Window.Type = SDL3.SDL.EventType.WindowMouseEnter;
            pointerWindowEvent.Window.WindowID = nativeWindowId + 1;
            Require(SDL3.SDL.PushEvent(ref pointerWindowEvent),
                "The native queue accepts a foreign-window pointer event.");
            display.ProcessEvents();
            Require(pointerDelivery.Count == 3, "Foreign-window pointer events are ignored.");

            pointerWindowEvent.Window.WindowID = nativeWindowId;
            Action failingEnter = () => throw new InvalidOperationException("injected pointer-enter failure");
            display.WindowMouseEntered += failingEnter;
            try
            {
                Require(SDL3.SDL.PushEvent(ref pointerWindowEvent),
                    "The native queue accepts a failing pointer-enter event.");
                pointerWindowEvent.Window.Type = SDL3.SDL.EventType.WindowMouseLeave;
                Require(SDL3.SDL.PushEvent(ref pointerWindowEvent),
                    "The native queue accepts a following pointer-leave event.");
                try
                {
                    display.ProcessEvents();
                    throw new Exception("A failing pointer callback must be reported after draining the queue.");
                }
                catch (AggregateException errors)
                {
                    Require(errors.InnerExceptions.Count == 1 &&
                            pointerDelivery.SequenceEqual(["enter", "quit", "exit", "enter", "exit"]),
                        "A pointer callback failure is aggregated after later window events are delivered.");
                }
            }
            finally
            {
                display.WindowMouseEntered -= failingEnter;
            }
        }
        finally
        {
            display.WindowMouseEntered -= entered;
            display.WindowMouseExited -= exited;
            display.QuitRequested -= orderedQuit;
        }

        var dpiDelivery = new List<string>();
        Action dpiChanged = () => dpiDelivery.Add("dpi");
        Action dpiOrderedQuit = () => dpiDelivery.Add("quit");
        display.WindowDpiChanged += dpiChanged;
        display.QuitRequested += dpiOrderedQuit;
        var scaleChanged = new SDL3.SDL.Event
        {
            Window = new SDL3.SDL.WindowEvent
            {
                Type = SDL3.SDL.EventType.WindowDisplayScaleChanged,
                WindowID = nativeWindowId,
            },
        };
        try
        {
            Require(SDL3.SDL.PushEvent(ref scaleChanged) && SDL3.SDL.PushEvent(ref quit) &&
                    SDL3.SDL.PushEvent(ref scaleChanged),
                "The native queue accepts content-scale changes around a quit request.");
            display.ProcessEvents();
            Require(dpiDelivery.SequenceEqual(["dpi", "quit", "dpi"]),
                "Content-scale callbacks retain native event order.");

            scaleChanged.Window.WindowID = nativeWindowId + 1;
            Require(SDL3.SDL.PushEvent(ref scaleChanged),
                "The native queue accepts a foreign-window content-scale event.");
            display.ProcessEvents();
            Require(dpiDelivery.Count == 3, "Foreign-window content-scale changes are ignored.");

            scaleChanged.Window.WindowID = nativeWindowId;
            Action failingDpi = () => throw new InvalidOperationException("injected content-scale callback failure");
            display.WindowDpiChanged += failingDpi;
            try
            {
                Require(SDL3.SDL.PushEvent(ref scaleChanged) && SDL3.SDL.PushEvent(ref quit),
                    "The native queue accepts a failing content-scale callback followed by quit.");
                try
                {
                    display.ProcessEvents();
                    throw new Exception("A failing content-scale callback must be reported after the queue drains.");
                }
                catch (AggregateException errors)
                {
                    Require(errors.InnerExceptions.Count == 1 &&
                            dpiDelivery.SequenceEqual(["dpi", "quit", "dpi", "dpi", "quit"]),
                        "A content-scale callback failure is aggregated after later events are delivered.");
                }
            }
            finally
            {
                display.WindowDpiChanged -= failingDpi;
            }
        }
        finally
        {
            display.WindowDpiChanged -= dpiChanged;
            display.QuitRequested -= dpiOrderedQuit;
        }

        display.ProcessEvents();
        var initialRect = new Rect2i(display.WindowGetPosition(), display.WindowGetSize());
        var movedPosition = initialRect.Position + new Vector2i(17, 19);
        var resizedSize = initialRect.Size + new Vector2i(23, 29);
        var rectDelivery = new List<Rect2i>();
        var rectOrder = new List<string>();
        Action<Rect2i> rectChanged = rect =>
        {
            rectDelivery.Add(rect);
            rectOrder.Add("rect");
        };
        Action rectOrderedQuit = () => rectOrder.Add("quit");
        display.WindowRectChanged += rectChanged;
        display.QuitRequested += rectOrderedQuit;
        var rectEvent = new SDL3.SDL.Event
        {
            Window = new SDL3.SDL.WindowEvent
            {
                Type = SDL3.SDL.EventType.WindowMoved,
                WindowID = nativeWindowId,
                Data1 = movedPosition.X,
                Data2 = movedPosition.Y,
            },
        };
        try
        {
            Require(SDL3.SDL.PushEvent(ref rectEvent) && SDL3.SDL.PushEvent(ref rectEvent) &&
                    SDL3.SDL.PushEvent(ref quit),
                "The native queue accepts duplicate moves followed by quit.");
            rectEvent.Window.Type = SDL3.SDL.EventType.WindowResized;
            rectEvent.Window.Data1 = resizedSize.X;
            rectEvent.Window.Data2 = resizedSize.Y;
            Require(SDL3.SDL.PushEvent(ref rectEvent), "The native queue accepts a following resize.");
            rectEvent.Window.WindowID = nativeWindowId + 1;
            rectEvent.Window.Data1++;
            Require(SDL3.SDL.PushEvent(ref rectEvent), "The native queue accepts a foreign-window resize.");
            Expect<InvalidOperationException>(() => Task.Run(display.ProcessEvents).GetAwaiter().GetResult(),
                "Rectangle callbacks may be pumped only on the opening thread.");
            Require(rectDelivery.Count == 0, "An off-thread pump leaves rectangle events queued.");
            display.ProcessEvents();
            Require(rectDelivery.SequenceEqual([
                    new Rect2i(movedPosition, initialRect.Size),
                    new Rect2i(movedPosition, resizedSize),
                ]) && rectOrder.SequenceEqual(["rect", "quit", "rect"]),
                "Move and resize callbacks deliver full intermediate rectangles in queue order; duplicate and foreign events are ignored.");

            rectEvent.Window.WindowID = nativeWindowId;
            rectEvent.Window.Type = SDL3.SDL.EventType.WindowMoved;
            rectEvent.Window.Data1 = movedPosition.X + 5;
            rectEvent.Window.Data2 = movedPosition.Y + 7;
            var throwOnNextRect = true;
            Action<Rect2i> failingRect = rect =>
            {
                if (throwOnNextRect)
                {
                    throwOnNextRect = false;
                    throw new InvalidOperationException("injected rectangle callback failure");
                }
            };
            display.WindowRectChanged += failingRect;
            try
            {
                Require(SDL3.SDL.PushEvent(ref rectEvent), "The native queue accepts a failing rectangle callback.");
                rectEvent.Window.Type = SDL3.SDL.EventType.WindowResized;
                rectEvent.Window.Data1 = resizedSize.X + 11;
                rectEvent.Window.Data2 = resizedSize.Y + 13;
                Require(SDL3.SDL.PushEvent(ref rectEvent) && SDL3.SDL.PushEvent(ref quit),
                    "The native queue accepts resize and quit after a failing rectangle callback.");
                try
                {
                    display.ProcessEvents();
                    throw new Exception("A failing rectangle callback must be reported after the queue drains.");
                }
                catch (AggregateException errors)
                {
                    Require(errors.InnerExceptions.Count == 1 && rectDelivery.Count == 4 &&
                            rectDelivery[2] == new Rect2i(new Vector2i(movedPosition.X + 5, movedPosition.Y + 7),
                                resizedSize) &&
                            rectDelivery[3] == new Rect2i(new Vector2i(movedPosition.X + 5, movedPosition.Y + 7),
                                new Vector2i(resizedSize.X + 11, resizedSize.Y + 13)) &&
                            rectOrder.SequenceEqual(["rect", "quit", "rect", "rect", "rect", "quit"]),
                        "The rectangle cache commits before callbacks, and callback failure does not stop later events.");
                }
            }
            finally
            {
                display.WindowRectChanged -= failingRect;
            }
        }
        finally
        {
            display.WindowRectChanged -= rectChanged;
            display.QuitRequested -= rectOrderedQuit;
        }

        Input.Instance.ReleasePressedEvents();
        var keyDown = new SDL3.SDL.Event
        {
            Key = new SDL3.SDL.KeyboardEvent
            {
                Type = SDL3.SDL.EventType.KeyDown,
                WindowID = nativeWindowId,
                Key = SDL3.SDL.Keycode.A,
                Scancode = SDL3.SDL.Scancode.A,
                Down = true,
            },
        };
        Require(SDL3.SDL.PushEvent(ref keyDown), "The native queue accepts a keyboard press.");
        display.ProcessEvents();
        Require(Input.Instance.IsKeyPressed(Key.A) && Input.Instance.IsPhysicalKeyPressed(Key.A),
            "The event pump commits logical and physical key state.");
        keyDown.Key.Type = SDL3.SDL.EventType.KeyUp;
        keyDown.Key.Down = false;
        Require(SDL3.SDL.PushEvent(ref keyDown), "The native queue accepts a keyboard release.");
        display.ProcessEvents();
        Require(!Input.Instance.IsKeyPressed(Key.A) && !Input.Instance.IsPhysicalKeyPressed(Key.A),
            "The event pump releases logical and physical key state.");
        keyDown.Key.Key = SDL3.SDL.Keycode.Escape;
        keyDown.Key.Scancode = SDL3.SDL.Scancode.Escape;
        keyDown.Key.Type = SDL3.SDL.EventType.KeyDown;
        keyDown.Key.Down = true;
        Require(SDL3.SDL.PushEvent(ref keyDown), "The native queue accepts Escape.");
        display.ProcessEvents();
        Require(Input.Instance.IsKeyPressed(Key.Escape), "Escape maps to the special key identity.");
        keyDown.Key.Type = SDL3.SDL.EventType.KeyUp;
        keyDown.Key.Down = false;
        Require(SDL3.SDL.PushEvent(ref keyDown), "The native queue accepts Escape release.");
        display.ProcessEvents();
        Require(!Input.Instance.IsKeyPressed(Key.Escape), "Escape releases its special key identity.");
        keyDown.Key.Key = SDL3.SDL.Keycode.A;
        var unicodeKeyMapper = typeof(DisplayServer).GetMethod("MapKeycode",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Require(unicodeKeyMapper is not null &&
                (Key)unicodeKeyMapper.Invoke(null, new object[] { (SDL3.SDL.Keycode)'й' })! == (Key)'Й',
            "Native non-Latin key labels retain their Unicode scalar identity.");
        keyDown.Key.Scancode = SDL3.SDL.Scancode.B;
        keyDown.Key.Type = SDL3.SDL.EventType.KeyDown;
        keyDown.Key.Down = true;
        Require(SDL3.SDL.PushEvent(ref keyDown), "The native queue accepts a layout-label probe.");
        display.ProcessEvents();
        Require(Input.Instance.IsKeyPressed(Key.A) && Input.Instance.IsKeyLabelPressed(Key.B) &&
                !Input.Instance.IsKeyLabelPressed(Key.A),
            "A key event records its localized label independently of its logical keycode.");
        keyDown.Key.Type = SDL3.SDL.EventType.KeyUp;
        keyDown.Key.Down = false;
        Require(SDL3.SDL.PushEvent(ref keyDown), "The native queue accepts the label-probe release.");
        display.ProcessEvents();
        Require(!Input.Instance.IsKeyLabelPressed(Key.B), "The localized key label is released.");
        keyDown.Key.Scancode = SDL3.SDL.Scancode.A;
        keyDown.Key.Type = SDL3.SDL.EventType.KeyDown;
        keyDown.Key.Down = true;
        Require(SDL3.SDL.PushEvent(ref keyDown) && SDL3.SDL.PushEvent(ref quit),
            "The native queue accepts an input-discard probe.");
        var quitsBeforeDiscard = quitCount;
        display.ForceProcessAndDropEvents();
        Require(!Input.Instance.IsKeyPressed(Key.A) && quitCount == quitsBeforeDiscard + 1,
            "Forced window processing discards input while preserving quit delivery.");

        Input.Instance.UseAccumulatedInput = true;
        var motion = new SDL3.SDL.Event
        {
            Motion = new SDL3.SDL.MouseMotionEvent
            {
                Type = SDL3.SDL.EventType.MouseMotion,
                WindowID = nativeWindowId,
                Timestamp = 1_000_000_000,
                X = 10,
                XRel = 2,
            },
        };
        Require(SDL3.SDL.PushEvent(ref motion), "The native queue accepts pointer motion.");
        motion.Motion.Timestamp = 2_000_000_000;
        motion.Motion.X = 13;
        motion.Motion.XRel = 3;
        Require(SDL3.SDL.PushEvent(ref motion), "The native queue accepts consecutive pointer motion.");
        display.ProcessEvents();
        Require(NearlyEqual(Input.Instance.LastMouseVelocity.X, 2.5f),
            "Accumulated pointer motion combines relative travel across the batch.");
        Input.Instance.UseAccumulatedInput = false;
        motion.Motion.Timestamp = 3_000_000_000;
        motion.Motion.XRel = 4;
        Require(SDL3.SDL.PushEvent(ref motion), "The native queue accepts direct pointer motion.");
        display.ProcessEvents();
        Require(NearlyEqual(Input.Instance.LastMouseVelocity.X, 4f),
            "Disabling accumulation dispatches each pointer motion separately.");
        Input.Instance.UseAccumulatedInput = true;
        motion.Motion.WindowID = nativeWindowId + 1;
        motion.Motion.XRel = 100;
        Require(SDL3.SDL.PushEvent(ref motion), "The native queue accepts foreign-window pointer motion.");
        display.ProcessEvents();
        Require(NearlyEqual(Input.Instance.LastMouseVelocity.X, 4f),
            "Pointer motion from an unowned window cannot enter the accumulated input stream.");
        Input.Instance.FlushBufferedEvents();
        VerifyDisplayServerDropBatches(display);
        DisplayServerFocusEventsTests.Run(display);
        DisplayServerCloseEventsTests.Run(display);
    }

    Require(DisplayServer.Instance is null, "Disposal unregisters the native display server.");
    using var reopened = DisplayServer.Open("Reopened", new Vector2i(120, 80), hidden: true);
    Require(reopened.WindowGetSize() == new Vector2i(120, 80),
        "The native video subsystem can reopen after deterministic disposal.");
    reopened.Dispose();
    Expect<ObjectDisposedException>(() => reopened.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle),
        "Disposed display servers cannot expose native handles.");
    Expect<ObjectDisposedException>(() => reopened.IsDarkMode(),
        "Disposed display servers cannot query the system theme.");
}

static void VerifyDisplayServerDropBatches(DisplayServer display)
{
    var dispatch = typeof(DisplayServer).GetMethod("DispatchDrop",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
        ?? throw new Exception("The native drop adapter is missing.");
    void Drop(SDL3.SDL.EventType type, string? path = null) =>
        dispatch.Invoke(display, [type, path]);

    var deliveries = new List<string[]>();
    void Capture(IReadOnlyList<string> files) => deliveries.Add(files.ToArray());
    display.FilesDropped += Capture;
    try
    {
        Drop(SDL3.SDL.EventType.DropBegin);
        Drop(SDL3.SDL.EventType.DropFile, "first.txt");
        Drop(SDL3.SDL.EventType.DropFile, "second.txt");
        Require(deliveries.Count == 0, "File-drop callbacks wait for the completed batch.");
        Drop(SDL3.SDL.EventType.DropComplete);
        Require(deliveries.Count == 1 && deliveries[0].SequenceEqual(["first.txt", "second.txt"]),
            "A multi-file drop emits one ordered path snapshot.");

        Drop(SDL3.SDL.EventType.DropFile, "single.txt");
        Drop(SDL3.SDL.EventType.DropBegin);
        Drop(SDL3.SDL.EventType.DropComplete);
        Require(deliveries.Count == 2 && deliveries[1].SequenceEqual(["single.txt"]),
            "A standalone file emits one path and an empty batch emits nothing.");

        Action<IReadOnlyList<string>> fail = _ => throw new InvalidOperationException("injected drop failure");
        display.FilesDropped += fail;
        Drop(SDL3.SDL.EventType.DropBegin);
        Drop(SDL3.SDL.EventType.DropFile, "failed.txt");
        Expect<System.Reflection.TargetInvocationException>(() => Drop(SDL3.SDL.EventType.DropComplete),
            "A failing callback propagates after its batch state is cleared.");
        display.FilesDropped -= fail;
        Drop(SDL3.SDL.EventType.DropBegin);
        Drop(SDL3.SDL.EventType.DropFile, "next.txt");
        Drop(SDL3.SDL.EventType.DropComplete);
        Require(deliveries.Count == 4 && deliveries[2].SequenceEqual(["failed.txt"]) &&
                deliveries[3].SequenceEqual(["next.txt"]),
            "A failing drop callback cannot contaminate the following batch.");

        Drop(SDL3.SDL.EventType.DropBegin);
        Drop(SDL3.SDL.EventType.DropFile, "abandoned.txt");
        Drop(SDL3.SDL.EventType.DropBegin);
        Drop(SDL3.SDL.EventType.DropFile, "kept.txt");
        Drop(SDL3.SDL.EventType.DropComplete);
        Require(deliveries.Count == 5 && deliveries[4].SequenceEqual(["kept.txt"]),
            "An interrupted drop is discarded rather than reported as completed.");

        Drop(SDL3.SDL.EventType.DropBegin);
        Drop(SDL3.SDL.EventType.DropFile, "closing.txt");
        var close = new SDL3.SDL.Event
        {
            Window = new SDL3.SDL.WindowEvent
            {
                Type = SDL3.SDL.EventType.WindowCloseRequested,
                WindowID = SDL3.SDL.GetWindowID(SDL3.SDL.GetWindows(out _)![0]),
            },
        };
        Require(SDL3.SDL.PushEvent(ref close), "The native queue accepts a window-close request.");
        display.ProcessEvents();
        Drop(SDL3.SDL.EventType.DropComplete);
        Require(deliveries.Count == 5, "Closing the window discards its unfinished file drop.");
    }
    finally
    {
        display.FilesDropped -= Capture;
    }
}

static void VerifyMathF()
{
    var expectedOverloads = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        [nameof(Mathf.Abs)] = 3,
        [nameof(Mathf.Acos)] = 2,
        [nameof(Mathf.Acosh)] = 2,
        [nameof(Mathf.AngleDifference)] = 2,
        [nameof(Mathf.Asin)] = 2,
        [nameof(Mathf.Asinh)] = 2,
        [nameof(Mathf.Atan)] = 2,
        [nameof(Mathf.Atan2)] = 2,
        [nameof(Mathf.Atanh)] = 2,
        [nameof(Mathf.BezierDerivative)] = 2,
        [nameof(Mathf.BezierInterpolate)] = 2,
        [nameof(Mathf.Ceil)] = 2,
        [nameof(Mathf.CeilToInt)] = 2,
        [nameof(Mathf.Clamp)] = 3,
        [nameof(Mathf.Cos)] = 2,
        [nameof(Mathf.Cosh)] = 2,
        [nameof(Mathf.CubicInterpolate)] = 2,
        [nameof(Mathf.CubicInterpolateAngle)] = 2,
        [nameof(Mathf.CubicInterpolateAngleInTime)] = 2,
        [nameof(Mathf.CubicInterpolateInTime)] = 2,
        [nameof(Mathf.DBToLinear)] = 2,
        [nameof(Mathf.DecimalCount)] = 2,
        [nameof(Mathf.DegToRad)] = 2,
        [nameof(Mathf.Ease)] = 2,
        [nameof(Mathf.Exp)] = 2,
        [nameof(Mathf.Floor)] = 2,
        [nameof(Mathf.FloorToInt)] = 2,
        [nameof(Mathf.InverseLerp)] = 2,
        [nameof(Mathf.IsEqualApprox)] = 4,
        [nameof(Mathf.IsFinite)] = 2,
        [nameof(Mathf.IsInf)] = 2,
        [nameof(Mathf.IsNaN)] = 2,
        [nameof(Mathf.IsZeroApprox)] = 2,
        [nameof(Mathf.Lerp)] = 2,
        [nameof(Mathf.LerpAngle)] = 2,
        [nameof(Mathf.LinearToDB)] = 2,
        [nameof(Mathf.Log)] = 2,
        [nameof(Mathf.Max)] = 3,
        [nameof(Mathf.Min)] = 3,
        [nameof(Mathf.MoveToward)] = 2,
        [nameof(Mathf.NearestPo2)] = 1,
        [nameof(Mathf.PingPong)] = 2,
        [nameof(Mathf.PosMod)] = 3,
        [nameof(Mathf.Pow)] = 2,
        [nameof(Mathf.RadToDeg)] = 2,
        [nameof(Mathf.Remap)] = 2,
        [nameof(Mathf.RotateToward)] = 2,
        [nameof(Mathf.Round)] = 2,
        [nameof(Mathf.RoundToInt)] = 2,
        [nameof(Mathf.Sign)] = 3,
        [nameof(Mathf.Sin)] = 2,
        [nameof(Mathf.SinCos)] = 2,
        [nameof(Mathf.Sinh)] = 2,
        [nameof(Mathf.SmoothStep)] = 2,
        [nameof(Mathf.Snapped)] = 2,
        [nameof(Mathf.Sqrt)] = 2,
        [nameof(Mathf.StepDecimals)] = 1,
        [nameof(Mathf.Tan)] = 2,
        [nameof(Mathf.Tanh)] = 2,
        [nameof(Mathf.Wrap)] = 3,
    };
    var actualOverloads = typeof(Mathf)
        .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.DeclaredOnly)
        .GroupBy(method => method.Name, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
    Require(actualOverloads.Count == expectedOverloads.Count && expectedOverloads.All(pair =>
            actualOverloads.TryGetValue(pair.Key, out var count) && count == pair.Value) &&
            actualOverloads.Values.Sum() == 127,
        "Mathf must expose the complete audited method family without missing or extra overloads.");
    var constants = typeof(Mathf)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static |
                   System.Reflection.BindingFlags.DeclaredOnly)
        .Where(field => field.IsLiteral)
        .Select(field => field.Name)
        .OrderBy(name => name, StringComparer.Ordinal)
        .ToArray();
    Require(constants.SequenceEqual(new[] { "E", "Epsilon", "Inf", "NaN", "Pi", "Sqrt2", "Tau" }),
        "Mathf must expose exactly the audited constant surface.");

    Require(Mathf.Pi == System.MathF.PI && Mathf.Tau == System.MathF.Tau && Mathf.E == System.MathF.E &&
            Mathf.Sqrt2 == System.MathF.Sqrt(2f) && Mathf.Epsilon == 0.000001f &&
            Mathf.IsInf(Mathf.Inf) && Mathf.IsNaN(Mathf.NaN),
        "Mathf constants must retain their documented single-precision values.");
    Require(Mathf.Abs(-7) == 7 && Mathf.Abs(-2.5f) == 2.5f && Mathf.Abs(-2.5d) == 2.5d,
        "Absolute-value overloads must cover integer and floating-point inputs.");
    Expect<OverflowException>(() => _ = Mathf.Abs(int.MinValue),
        "Integer absolute value must expose two's-complement overflow.");

    Require(NearlyEqual(Mathf.Acos(0f), Mathf.Pi / 2f) && NearlyEqual(Mathf.Asin(1f), Mathf.Pi / 2f) &&
            NearlyEqual(Mathf.Atan(1f), Mathf.Pi / 4f) && NearlyEqual(Mathf.Atan2(1f, -1f), 3f * Mathf.Pi / 4f) &&
            NearlyEqual((float)Mathf.Acos(0d), Mathf.Pi / 2f) && NearlyEqual((float)Mathf.Asin(1d), Mathf.Pi / 2f) &&
            NearlyEqual((float)Mathf.Atan(1d), Mathf.Pi / 4f) && NearlyEqual((float)Mathf.Atan2(1d, -1d), 3f * Mathf.Pi / 4f) &&
            Mathf.IsNaN(Mathf.Acos(2f)) && Mathf.IsNaN(Mathf.Asin(2d)),
        "Inverse trigonometric overloads must preserve radians, quadrants, and invalid-domain NaN values.");
    Require(NearlyEqual(Mathf.Acosh(Mathf.Cosh(2f)), 2f) && NearlyEqual(Mathf.Asinh(Mathf.Sinh(2f)), 2f) &&
            NearlyEqual(Mathf.Atanh(Mathf.Tanh(0.5f)), 0.5f) &&
            NearlyEqual((float)Mathf.Acosh(Mathf.Cosh(2d)), 2f) && NearlyEqual((float)Mathf.Asinh(Mathf.Sinh(2d)), 2f) &&
            NearlyEqual((float)Mathf.Atanh(Mathf.Tanh(0.5d)), 0.5f),
        "Hyperbolic and inverse-hyperbolic overloads must round-trip ordinary values.");

    Require(Mathf.Ceil(1.1f) == 2f && Mathf.Ceil(1.1d) == 2d && Mathf.CeilToInt(-1.1f) == -1 &&
            Mathf.CeilToInt(-1.1d) == -1 && Mathf.Floor(-1.1f) == -2f && Mathf.Floor(-1.1d) == -2d &&
            Mathf.FloorToInt(-1.1f) == -2 && Mathf.FloorToInt(-1.1d) == -2 &&
            Mathf.Round(2.5f) == 2f && Mathf.Round(3.5d) == 4d &&
            Mathf.RoundToInt(2.5f) == 2 && Mathf.RoundToInt(3.5d) == 4,
        "Rounding families must distinguish direction and use midpoint-to-even rounding.");
    _ = Mathf.CeilToInt(float.NaN);
    _ = Mathf.FloorToInt(double.PositiveInfinity);
    _ = Mathf.RoundToInt(float.MaxValue);
    Require(Mathf.Clamp(5, 0, 4) == 4 && Mathf.Clamp(-1f, 0f, 1f) == 0f && Mathf.Clamp(2d, 0d, 1d) == 1d,
        "Clamp overloads must apply inclusive bounds.");
    Expect<ArgumentException>(() => _ = Mathf.Clamp(1, 2, 0), "Integer clamp must reject reversed bounds.");
    Expect<ArgumentException>(() => _ = Mathf.Clamp(1f, 2f, 0f), "Float clamp must reject reversed bounds.");
    Expect<ArgumentException>(() => _ = Mathf.Clamp(1d, 2d, 0d), "Double clamp must reject reversed bounds.");

    var (sinFloat, cosFloat) = Mathf.SinCos(Mathf.Pi / 6f);
    var (sinDouble, cosDouble) = Mathf.SinCos(Math.PI / 6d);
    Require(NearlyEqual(Mathf.Sin(Mathf.Pi / 6f), 0.5f) && NearlyEqual(Mathf.Cos(Mathf.Pi / 3f), 0.5f) &&
            NearlyEqual(Mathf.Tan(Mathf.Pi / 4f), 1f) && NearlyEqual(sinFloat, 0.5f) &&
            NearlyEqual(cosFloat, Mathf.Sqrt(3f) / 2f) &&
            NearlyEqual((float)Mathf.Sin(Math.PI / 6d), 0.5f) && NearlyEqual((float)Mathf.Cos(Math.PI / 3d), 0.5f) &&
            NearlyEqual((float)Mathf.Tan(Math.PI / 4d), 1f) && NearlyEqual((float)sinDouble, 0.5f) &&
            NearlyEqual((float)cosDouble, Mathf.Sqrt(3f) / 2f),
        "Trigonometric and combined sine/cosine overloads must agree.");

    Require(NearlyEqual(Mathf.CubicInterpolate(0f, 2f, -2f, 4f, 0.5f), 1f) &&
            Mathf.CubicInterpolate(0d, 2d, -2d, 4d, 0.5d) == 1d &&
            NearlyEqual(Mathf.CubicInterpolateInTime(0f, 2f, -2f, 4f, 0.5f, 1f, -1f, 2f), 1f) &&
            Mathf.CubicInterpolateInTime(0d, 2d, -2d, 4d, 0.5d, 1d, -1d, 2d) == 1d &&
            Mathf.CubicInterpolateAngle(0f, Mathf.Tau - 0.2f, -0.2f, Mathf.Tau - 0.4f, 0f) == 0f &&
            Mathf.CubicInterpolateAngle(0d, Math.Tau - 0.2d, -0.2d, Math.Tau - 0.4d, 0d) == 0d &&
            Mathf.CubicInterpolateAngleInTime(0f, Mathf.Tau - 0.2f, -0.2f, Mathf.Tau - 0.4f, 0f, 1f, -1f, 2f) == 0f &&
            Mathf.CubicInterpolateAngleInTime(0d, Math.Tau - 0.2d, -0.2d, Math.Tau - 0.4d, 0d, 1d, -1d, 2d) == 0d,
        "Cubic interpolation must cover scalar, timed, and shortest-path angular forms.");
    Require(NearlyEqual(Mathf.BezierInterpolate(0f, 1f, 1f, 0f, 0.5f), 0.75f) &&
            Mathf.BezierInterpolate(0d, 1d, 1d, 0d, 0.5d) == 0.75d &&
            NearlyEqual(Mathf.BezierDerivative(0f, 1f, 1f, 0f, 0.5f), 0f) &&
            Mathf.BezierDerivative(0d, 1d, 1d, 0d, 0.5d) == 0d,
        "Bezier value and derivative overloads must preserve the cubic curve contract.");

    Require(NearlyEqual(Mathf.DBToLinear(0f), 1f) && NearlyEqual((float)Mathf.DBToLinear(0d), 1f) &&
            NearlyEqual(Mathf.LinearToDB(1f), 0f) && NearlyEqual((float)Mathf.LinearToDB(1d), 0f) &&
            NearlyEqual(Mathf.RadToDeg(Mathf.Pi), 180f) && NearlyEqual(Mathf.DegToRad(180f), Mathf.Pi) &&
            NearlyEqual((float)Mathf.RadToDeg(Math.PI), 180f) && NearlyEqual((float)Mathf.DegToRad(180d), Mathf.Pi),
        "Audio-scale and angle-unit conversions must round-trip their neutral anchors.");
    Require(Mathf.DecimalCount(1.25d) == 2 && Mathf.DecimalCount(1.2300m) == 4,
        "DecimalCount must report converted double scale and retained decimal trailing zeros.");
    Expect<OverflowException>(() => _ = Mathf.DecimalCount(double.NaN),
        "DecimalCount must reject non-finite double values.");

    Require(Mathf.Ease(-1f, 1f) == 0f && Mathf.Ease(2d, 1d) == 1d &&
            NearlyEqual(Mathf.Ease(0.5f, 2f), 0.25f) && Mathf.Ease(0.5d, 0d) == 0d &&
            NearlyEqual(Mathf.Exp(1f), Mathf.E) && NearlyEqual((float)Mathf.Exp(1d), Mathf.E) &&
            NearlyEqual(Mathf.Log(Mathf.E), 1f) && NearlyEqual((float)Mathf.Log(Math.E), 1f) &&
            Mathf.Pow(2f, 3f) == 8f && Mathf.Pow(2d, 3d) == 8d &&
            Mathf.Sqrt(9f) == 3f && Mathf.Sqrt(9d) == 3d,
        "Ease, exponential, logarithm, power, and root families must cover float and double paths.");

    Require(Mathf.InverseLerp(10f, 20f, 15f) == 0.5f && Mathf.InverseLerp(10d, 20d, 15d) == 0.5d &&
            Mathf.Lerp(10f, 20f, 1.5f) == 25f && Mathf.Lerp(10d, 20d, -0.5d) == 5d &&
            Mathf.Remap(5f, 0f, 10f, 10f, 20f) == 15f && Mathf.Remap(5d, 0d, 10d, 10d, 20d) == 15d &&
            Mathf.IsNaN(Mathf.InverseLerp(1f, 1f, 1f)),
        "Linear interpolation and remapping must remain unbounded and expose equal-bound IEEE behavior.");
    Require(Mathf.IsEqualApprox(1f, 1f + (Mathf.Epsilon * 0.5f)) &&
            !Mathf.IsEqualApprox(0f, Mathf.Epsilon) && Mathf.IsEqualApprox(float.PositiveInfinity, float.PositiveInfinity) &&
            !Mathf.IsEqualApprox(float.NaN, float.NaN) && Mathf.IsEqualApprox(1f, 1.1f, 0.2f) &&
            !Mathf.IsEqualApprox(1f, 1.1f, -1f) &&
            Mathf.IsEqualApprox(1d, 1d + 5e-15d) && !Mathf.IsEqualApprox(0d, 1e-14d) &&
            Mathf.IsEqualApprox(1d, 1.1d, 0.2d) && !Mathf.IsEqualApprox(1d, 1.1d, double.NaN),
        "Approximate equality must use strict scale-aware or explicit tolerances and exact infinity handling.");
    Require(Mathf.IsFinite(1f) && Mathf.IsFinite(1d) && !Mathf.IsFinite(float.NaN) &&
            Mathf.IsInf(float.NegativeInfinity) && Mathf.IsInf(double.PositiveInfinity) &&
            Mathf.IsNaN(float.NaN) && Mathf.IsNaN(double.NaN) &&
            Mathf.IsZeroApprox(Mathf.Epsilon * 0.5f) && !Mathf.IsZeroApprox(Mathf.Epsilon) &&
            Mathf.IsZeroApprox(5e-15d) && !Mathf.IsZeroApprox(1e-14d),
        "Classification and zero-approximation predicates must enforce strict documented boundaries.");

    Require(Mathf.Max(2, 1) == 2 && Mathf.Max(2f, 1f) == 2f && Mathf.Max(2d, 1d) == 2d &&
            Mathf.Min(2, 1) == 1 && Mathf.Min(2f, 1f) == 1f && Mathf.Min(2d, 1d) == 1d &&
            Mathf.MoveToward(0f, 10f, 3f) == 3f && Mathf.MoveToward(0d, 2d, 3d) == 2d &&
            Mathf.MoveToward(0f, 10f, -3f) == -3f,
        "Min/max and move-toward overloads must preserve scalar ordering and signed deltas.");
    Expect<ArithmeticException>(() => _ = Mathf.MoveToward(float.NaN, 1f, 1f),
        "MoveToward must expose unordered input through its sign operation.");
    Require(Mathf.NearestPo2(-1) == 0 && Mathf.NearestPo2(0) == 0 && Mathf.NearestPo2(1) == 1 &&
            Mathf.NearestPo2(5) == 8 && Mathf.NearestPo2(int.MaxValue) == int.MinValue,
        "NearestPo2 must retain its exact 32-bit zero and overflow behavior.");
    Require(Mathf.PosMod(-5, 3) == 1 && Mathf.PosMod(5, -3) == -1 &&
            Mathf.PosMod(-5f, 3f) == 1f && Mathf.PosMod(5d, -3d) == -1d &&
            Mathf.IsNaN(Mathf.PosMod(1f, 0f)) && Mathf.IsNaN(Mathf.PosMod(1d, 0d)),
        "Positive modulus must use the divisor's sign and preserve floating-point zero-divisor behavior.");
    Expect<DivideByZeroException>(() => _ = Mathf.PosMod(1, 0), "Integer positive modulus must reject a zero divisor.");
    Expect<OverflowException>(() => _ = Mathf.PosMod(int.MinValue, -1),
        "Integer positive modulus must expose signed division overflow.");

    Require(NearlyEqual(Mathf.AngleDifference(0f, 3f * Mathf.Pi / 2f), -Mathf.Pi / 2f) &&
            NearlyEqual((float)Mathf.AngleDifference(0d, 3d * Math.PI / 2d), -Mathf.Pi / 2f) &&
            Mathf.AngleDifference(0f, Mathf.Pi) == -Mathf.Pi &&
            Mathf.AngleDifference(Mathf.Pi, 0f) == Mathf.Pi &&
            NearlyEqual(Mathf.LerpAngle(0f, 3f * Mathf.Pi / 2f, 0.5f), -Mathf.Pi / 4f) &&
            NearlyEqual((float)Mathf.LerpAngle(0d, 3d * Math.PI / 2d, 0.5d), -Mathf.Pi / 4f) &&
            Mathf.RotateToward(0f, Mathf.Pi, 0.25f) == -0.25f &&
            Mathf.RotateToward(0d, Math.PI, 0.25d) == -0.25d,
        "Angular difference, interpolation, and movement must follow the shortest-path tie rule.");
    Require(Mathf.Sign(-2) == -1 && Mathf.Sign(0f) == 0 && Mathf.Sign(2d) == 1,
        "Sign overloads must return negative one, zero, or positive one.");
    Expect<ArithmeticException>(() => _ = Mathf.Sign(float.NaN), "Float sign must reject NaN.");
    Expect<ArithmeticException>(() => _ = Mathf.Sign(double.NaN), "Double sign must reject NaN.");

    Require(Mathf.SmoothStep(0f, 1f, -1f) == 0f && Mathf.SmoothStep(0d, 1d, 2d) == 1d &&
            Mathf.SmoothStep(2f, 2f, 10f) == 2f && Mathf.StepDecimals(0.1d) == 1 &&
            Mathf.StepDecimals(0.01d) == 2 && Mathf.StepDecimals(1d) == 0 &&
            Mathf.Snapped(5.1f, 2f) == 6f && Mathf.Snapped(-5.1d, 2d) == -6d &&
            Mathf.Snapped(1.25f, 0f) == 1.25f,
        "Smooth-step, decimal-step, and snapping helpers must preserve boundary behavior.");
    Require(Mathf.Wrap(6, 0, 5) == 1 && Mathf.Wrap(-1f, 0f, 5f) == 4f &&
            Mathf.Wrap(6d, 0d, 5d) == 1d && Mathf.Wrap(7, 3, 3) == 3 &&
            Mathf.Wrap(7f, 3f, 3f + (Mathf.Epsilon * 0.5f)) == 3f &&
            Mathf.PingPong(7f, 5f) == 3f && Mathf.PingPong(7d, 5d) == 3d &&
            Mathf.PingPong(7f, 0f) == 0f && Mathf.PingPong(7d, -5d) == 3d,
        "Wrapping and triangle-wave helpers must cover negative, degenerate, and reflected intervals.");
    Expect<OverflowException>(() => _ = Mathf.Wrap(int.MinValue, 0, -1),
        "Integer wrapping must expose the extreme managed remainder overflow.");

    Require(!new Vector2(Mathf.Epsilon, 0f).IsZeroApprox() &&
            new Vector2(Mathf.Epsilon * 0.5f, 0f).IsZeroApprox() &&
            !new Vector4(Mathf.Epsilon, 0f, 0f, 0f).IsZeroApprox() &&
            !default(Color).IsEqualApprox(new Color(Mathf.Epsilon, 0f, 0f, 0f)) &&
            !default(Rect2).IsEqualApprox(new Rect2(Mathf.Epsilon, 0f, 0f, 0f)) &&
            !default(Transform).IsEqualApprox(new Transform(Mathf.Epsilon, 0f, 0f, 0f, 0f, 0f)),
        "Migrated geometry must use the authoritative Mathf epsilon boundary.");

    _ = ExerciseMathfHotPath(32);
    var allocationStart = GC.GetAllocatedBytesForCurrentThread();
    var accumulator = ExerciseMathfHotPath(10_000);
    Require(GC.GetAllocatedBytesForCurrentThread() == allocationStart && Mathf.IsFinite(accumulator),
        "Mathf hot-path scalar operations must not allocate managed memory.");
}

static float ExerciseMathfHotPath(int iterations)
{
    var accumulator = 0f;
    for (var index = 0; index < iterations; index++)
    {
        accumulator += Mathf.CubicInterpolate(0f, 1f, -1f, 2f, index / (float)iterations);
        accumulator += Mathf.SinCos(index).Sin;
        accumulator += Mathf.DecimalCount(1.25m) + Mathf.StepDecimals(0.01d);
    }

    return accumulator;
}

static void VerifyColors()
{
    Require(Marshal.SizeOf<Color>() == 16 && typeof(Color).IsDefined(typeof(SerializableAttribute), inherit: false),
        "Color must be a serializable sequential four-float value type.");
    Require(default(Color) == new Color() && default(Color) == new Color(0f, 0f, 0f, 0f) &&
            Colors.Black == new Color(0f, 0f, 0f, 1f) && Colors.Transparent == new Color(1f, 1f, 1f, 0f),
        "Zero initialization, opaque black, and transparent white must remain distinct.");

    var rgba = new Color(0.1f, 0.2f, 0.3f, 0.4f);
    Require(new Color(rgba) == new Color(0.1f, 0.2f, 0.3f, 1f) && new Color(rgba, 0.7f).A == 0.7f,
        "The copy-and-alpha constructor must copy RGB and replace alpha.");
    Require(new Color(0x12345678u).ToRGBA32() == 0x12345678u &&
            new Color(0x123456789abcdef0ul).ToRGBA64() == 0x123456789abcdef0ul,
        "Packed RGBA constructors and encoders must be inverse for byte- and word-aligned values.");

    var channels = new Color();
    channels.R8 = 306;
    channels.G8 = -51;
    channels.B8 = 128;
    channels.A8 = 255;
    Require(channels.R8 == 306 && channels.G8 == -51 && channels.B8 == 128 && channels.A8 == 255 &&
            NearlyEqual(channels.R, 1.2f) && NearlyEqual(channels.G, -0.2f),
        "8-bit-scale properties must preserve typed binding overbright and negative values without clamping.");
    Require(new Color(float.NaN, float.PositiveInfinity, float.NegativeInfinity).R8 == 0 &&
            new Color(float.NaN, float.PositiveInfinity, float.NegativeInfinity).G8 == int.MaxValue &&
            new Color(float.NaN, float.PositiveInfinity, float.NegativeInfinity).B8 == int.MinValue,
        "8-bit-scale getters must define NaN and overflow deterministically.");
    channels[0] = 0.25f;
    channels[1] = 0.5f;
    channels[2] = 0.75f;
    channels[3] = 1f;
    Require(channels == new Color(0.25f, 0.5f, 0.75f, 1f),
        "The component indexer must map indices zero through three to RGBA.");
    Expect<ArgumentOutOfRangeException>(() => _ = channels[-1],
        "The color indexer getter must reject negative indices.");
    Expect<ArgumentOutOfRangeException>(() => channels[4] = 0f,
        "The color indexer setter must reject indices above three.");

    var red = Colors.Red;
    red.ToHSV(out var redHue, out var redSaturation, out var redValue);
    Require(NearlyEqual(redHue, 0f) && NearlyEqual(redSaturation, 1f) && NearlyEqual(redValue, 1f) &&
            ColorNearlyEqual(Color.FromHSV(1f / 3f, 1f, 1f, 0.25f), new Color(0f, 1f, 0f, 0.25f)) &&
            ColorNearlyEqual(Color.FromHSV(0.7f, 0f, 0.4f, 0.3f), new Color(0.4f, 0.4f, 0.4f, 0.3f)),
        "HSV conversion must cover chromatic and achromatic colors and preserve alpha.");
    var hsvMutable = new Color(1f, 0f, 0f, 0.35f) { H = 2f / 3f };
    var saturationMutable = new Color(1f, 0f, 0f, 0.35f) { S = 0f };
    var valueMutable = new Color(1f, 0f, 0f, 0.35f) { V = 0.5f };
    Require(ColorNearlyEqual(hsvMutable, new Color(0f, 0f, 1f, 0.35f)) &&
            ColorNearlyEqual(saturationMutable, new Color(1f, 1f, 1f, 0.35f)) &&
            ColorNearlyEqual(valueMutable, new Color(0.5f, 0f, 0f, 0.35f)),
        "HSV property setters must reconstruct RGB while preserving alpha.");

    Require(NearlyEqual(Colors.Red.OKHSLH, 0.0812f, 0.001f) &&
            NearlyEqual(Colors.Red.OKHSLS, 1f, 0.001f) &&
            NearlyEqual(Colors.Red.OKHSLL, 0.5681f, 0.001f) &&
            NearlyEqual(Colors.Green.OKHSLH, 0.3958f, 0.001f) &&
            NearlyEqual(Colors.Blue.OKHSLH, 0.7335f, 0.001f),
        "OKHSL primary-color anchors must match the perceptual reference transform.");
    foreach (var sample in new[]
             {
                 Colors.Red, Colors.Green, Colors.Blue, new Color(0.12f, 0.47f, 0.83f, 0.6f),
                 new Color(0.2f, 0.2f, 0.2f, 0.4f), Colors.White, Colors.Black
             })
    {
        var roundTrip = Color.FromOKHSL(sample.OKHSLH, sample.OKHSLS, sample.OKHSLL, sample.A);
        Require(ColorNearlyEqual(roundTrip, sample, 0.002f),
            "OKHSL conversion must round-trip ordinary sRGB colors.");
    }
    var okGrid = new[] { 0.1f, 0.3f, 0.5f, 0.7f, 0.9f };
    foreach (var gridRed in okGrid)
        foreach (var gridGreen in okGrid)
            foreach (var gridBlue in okGrid)
            {
                var sample = new Color(gridRed, gridGreen, gridBlue, 0.37f);
                var roundTrip = Color.FromOKHSL(sample.OKHSLH, sample.OKHSLS, sample.OKHSLL, sample.A);
                Require(ColorNearlyEqual(roundTrip, sample, 0.003f),
                    $"OKHSL must round-trip a broad ordinary sRGB grid: {sample} -> ({sample.OKHSLH}, {sample.OKHSLS}, {sample.OKHSLL}) -> {roundTrip}.");
            }
    var saturatedDarkBlue = new Color(0f, 0f, 0.25f, 0.37f);
    var clampedDarkBlue = Color.FromOKHSL(
        saturatedDarkBlue.OKHSLH,
        saturatedDarkBlue.OKHSLS,
        saturatedDarkBlue.OKHSLL,
        saturatedDarkBlue.A);
    var okMutable = new Color(0.3f, 0.6f, 0.9f, 0.42f);
    okMutable.OKHSLL = 0.4f;
    Require(NearlyEqual(new Color(0.25f, 0.25f, 0.25f).OKHSLS, 0f) &&
            saturatedDarkBlue.OKHSLS == 1f &&
            ColorNearlyEqual(clampedDarkBlue, new Color(0.000297069f, 0.022376226f, 0.216744155f, 0.37f), 0.000001f) &&
            okMutable.A == 0.42f &&
            Color.FromOKHSL(0.5f, 0.5f, 0.5f, -1f).A == 0f &&
            Color.FromOKHSL(0.5f, 0.5f, 0.5f, 2f).A == 1f &&
            Color.FromOKHSL(float.NaN, float.NaN, float.NaN, float.NaN) == default,
        "OKHSL must define achromatic saturation and clamp all constructed components.");

    Require(NearlyEqual(new Color(1f, 1f, 1f).Luminance, 1f) &&
            NearlyEqual(new Color(1f, 0f, 0f).Luminance, 0.2126f),
        "Luminance must use linear RGB coefficients and ignore alpha.");
    var blend = new Color(0f, 0f, 1f, 0.5f).Blend(new Color(1f, 0f, 0f, 0.5f));
    Require(ColorNearlyEqual(blend, new Color(2f / 3f, 0f, 1f / 3f, 0.75f)) &&
            default(Color).Blend(default) == default,
        "Blend must implement straight-alpha source-over and define the zero-alpha result.");

    Require(new Color(-1f, 0.4f, 2f, 3f).Clamp() == new Color(0f, 0.4f, 1f, 1f) &&
            new Color(0.1f, 0.4f, 0.8f, 0.9f).Clamp(
                new Color(0.2f, 0.3f, 0.4f, 0.5f), new Color(0.7f, 0.6f, 0.5f, 0.8f)) ==
            new Color(0.2f, 0.4f, 0.5f, 0.8f),
        "Clamp must apply default and custom componentwise bounds.");
    Expect<ArgumentException>(() => rgba.Clamp(new Color(1f, 0f, 0f), new Color(0f, 1f, 1f)),
        "Clamp must reject a componentwise reversed bound.");
    Require(ColorNearlyEqual(rgba.Darkened(2f), new Color(-0.1f, -0.2f, -0.3f, 0.4f)) &&
            ColorNearlyEqual(rgba.Lightened(2f), new Color(1.9f, 1.8f, 1.7f, 0.4f)) &&
            ColorNearlyEqual(rgba.Lerp(Colors.White, 2f), new Color(1.9f, 1.8f, 1.7f, 1.6f)) &&
            ColorNearlyEqual(rgba.Inverted(), new Color(0.9f, 0.8f, 0.7f, 0.4f)),
        "Color adjustment operations must preserve their documented unbounded interpolation behavior.");

    var srgbThreshold = new Color(0.04045f, 0.5f, 1f, 0.25f).SRGBToLinear();
    Require(NearlyEqual(srgbThreshold.R, 0.0031308f, 0.000001f) &&
            NearlyEqual(srgbThreshold.G, 0.214041f, 0.00001f) && srgbThreshold.A == 0.25f &&
            ColorNearlyEqual(srgbThreshold.LinearToSRGB(), new Color(0.04045f, 0.5f, 1f, 0.25f), 0.00001f),
        "sRGB transfer functions must cover their nonlinear branch and preserve alpha.");

    var packed = new Color(0.1f, 0.2f, 0.3f, 0.4f);
    Require(packed.ToRGBA32() == 0x1a334c66u && packed.ToARGB32() == 0x661a334cu &&
            packed.ToABGR32() == 0x664c331au &&
            new Color(1f, 0f, 1f, 0.5f).ToRGBA64() == 0xffff0000ffff8000ul &&
            new Color(-1f, 2f, float.NaN, float.PositiveInfinity).ToRGBA32() == 0x00ff00ffu,
        "Packed integer encoders must use the documented channel ordering and midpoint rounding.");
    for (var byteValue = 0; byteValue <= byte.MaxValue; byteValue++)
    {
        var byteColor = Color.Color8((byte)byteValue, (byte)byteValue, (byte)byteValue, (byte)byteValue);
        var repeated = (uint)byteValue * 0x01010101u;
        Require(byteColor.R8 == byteValue && byteColor.G8 == byteValue &&
                byteColor.B8 == byteValue && byteColor.A8 == byteValue && byteColor.ToRGBA32() == repeated,
            "Every byte channel must survive Color8, integer-scale access, and RGBA packing.");
    }
    var rgbe = (24u << 27) | (3u << 18) | (2u << 9) | 1u;
    Require(Color.FromRGBE9995(rgbe) == new Color(1f, 2f, 3f, 1f),
        "RGBE9995 decoding must apply the shared exponent to all mantissas.");

    Require(Color.HTMLIsValid("#abc") && Color.HTMLIsValid("abcd") && Color.HTMLIsValid("A1b2C3") &&
            Color.HTMLIsValid("#10203040") && !Color.HTMLIsValid("") && !Color.HTMLIsValid("#") &&
            !Color.HTMLIsValid("#12xz") && !Color.HTMLIsValid("##ffffff"),
        "HTML validation must accept only optional-hash 3, 4, 6, and 8 digit hexadecimal forms.");
    Require(Color.FromHTML("#abc") == Color.Color8(0xaa, 0xbb, 0xcc) &&
            Color.FromHTML("abcd") == Color.Color8(0xaa, 0xbb, 0xcc, 0xdd) &&
            Color.FromHTML("10203040") == Color.Color8(0x10, 0x20, 0x30, 0x40) &&
            Color.FromHTML(ReadOnlySpan<char>.Empty) == Colors.Black &&
            new Color(-1f, 0.5f, 2f, 0.5f).ToHTML() == "0080ff80" &&
            new Color(1f, 0.5f, 0f).ToHTML(includeAlpha: false) == "ff8000",
        "HTML parsing and formatting must handle shorthand, alpha, the empty compatibility case, clamping, and lowercase output.");
    Expect<ArgumentOutOfRangeException>(() => Color.FromHTML("12"),
        "HTML parsing must reject an invalid length.");
    Expect<ArgumentOutOfRangeException>(() => Color.FromHTML("12xz"),
        "HTML parsing must reject a non-hexadecimal character.");
    Expect<ArgumentNullException>(() => _ = new Color((string)null!),
        "The string constructor must reject null.");

    Require(new Color("dark slate-gray") == Colors.DarkSlateGray &&
            new Color("Rebecca_Purple", 0.25f) == new Color(Colors.RebeccaPurple, 0.25f) &&
            Color.FromString("not-a-color", rgba) == rgba &&
            Colors.Aqua == Colors.Cyan && Colors.Fuchsia == Colors.Magenta && Colors.Green == Colors.Lime &&
            Colors.Gray.ToRGBA32() == 0xbebebeffu && Colors.WebGray.ToRGBA32() == 0x808080ffu &&
            Colors.Maroon.ToRGBA32() == 0xb03060ffu && Colors.WebMaroon.ToRGBA32() == 0x800000ffu,
        "Named parsing must normalize supported separators, replace alpha, provide fallback, and preserve aliases.");
    Expect<ArgumentOutOfRangeException>(() => _ = new Color("not-a-color"),
        "The string constructor must reject an unknown color name.");
    Expect<ArgumentNullException>(() => Color.FromString(null!, rgba),
        "Named fallback parsing must reject null.");

    var namedProperties = typeof(Colors).GetProperties(
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
    Require(namedProperties.Length == 146 && namedProperties.All(property => property.PropertyType == typeof(Color)),
        "Colors must expose the complete 146-property named catalog.");
    foreach (var property in namedProperties)
    {
        var value = (Color)property.GetValue(null)!;
        Require(new Color(property.Name) == value,
            "Every public named property must be discoverable by the string constructor.");
    }
    Parallel.ForEach(namedProperties, property =>
    {
        var value = (Color)property.GetValue(null)!;
        Require(Color.FromString(property.Name, default) == value,
            "Concurrent named lookup must be deterministic.");
    });

    Require(ColorNearlyEqual(rgba + rgba, new Color(0.2f, 0.4f, 0.6f, 0.8f)) &&
            rgba - rgba == default && +rgba == rgba && ColorNearlyEqual(-rgba, new Color(0.9f, 0.8f, 0.7f, 0.6f)) &&
            rgba * 2f == 2f * rgba && rgba * 2 == rgba * 2f &&
            ColorNearlyEqual(rgba * rgba, new Color(0.01f, 0.04f, 0.09f, 0.16f)) &&
            ColorNearlyEqual((rgba * 2f) / 2f, rgba) && rgba / 2 == rgba / 2f &&
            ColorNearlyEqual(rgba / rgba, Colors.White),
        "Arithmetic operators must act componentwise, including alpha.");
    var divisionByZero = Colors.White / 0f;
    Require(float.IsPositiveInfinity(divisionByZero.R) && float.IsPositiveInfinity(divisionByZero.A),
        "Scalar zero division must retain IEEE 754 behavior.");
    Require(new Color(0f, 1f, 1f) < new Color(1f, 0f, 0f) &&
            new Color(0f, 1f, 1f) <= new Color(0f, 1f, 1f) &&
            new Color(1f, 0f, 0f) > new Color(0f, 1f, 1f) &&
            new Color(1f, 0f, 0f) >= new Color(1f, 0f, 0f),
        "Relational operators must compare RGBA lexicographically.");
    var nanColor = new Color(float.NaN, 0f, 0f);
    Require(nanColor != new Color(float.NaN, 0f, 0f) && !(nanColor < Colors.Black) && !(nanColor > Colors.Black) &&
            !(nanColor <= Colors.Black) && !(nanColor >= Colors.Black) &&
            new Color(float.PositiveInfinity, 0f, 0f).IsEqualApprox(new Color(float.PositiveInfinity, 0f, 0f)) &&
            !nanColor.IsEqualApprox(new Color(float.NaN, 0f, 0f)),
        "NaN and infinity comparisons must follow exact and approximate floating-point contracts.");
    Require(new Color(1f, 2f, 3f, 4f).GetHashCode() == new Color(1f, 2f, 3f, 4f).GetHashCode() &&
            new Color(1f, 2f, 3f, 4f).IsEqualApprox(new Color(1.000001f, 2f, 3f, 4f)),
        "Equal colors must hash equally and approximate equality must tolerate small relative error.");

    var previousCulture = CultureInfo.CurrentCulture;
    try
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        Require(new Color(1.5f, 2.5f, 3.5f, 4.5f).ToString("F1") == "(1.5, 2.5, 3.5, 4.5)",
            "Color formatting must use invariant culture.");
    }
    finally
    {
        CultureInfo.CurrentCulture = previousCulture;
    }

    var colorKey = new ConfigKey<Color>("graphics", "tint");
    using (var config = new ConfigFile())
    {
        var stored = new Color(1f, 0.5f, 0f, 1f);
        config.SetValue(colorKey, stored);
        Require(config.EncodeToText() == "[graphics]\n\ntint={\"R\":1,\"G\":0.5,\"B\":0,\"A\":1}\n" &&
                config.GetValue(colorKey) == stored,
            "ConfigFile must use the stable finite R/G/B/A color schema.");
        Expect<JsonException>(() => config.SetValue(colorKey, new Color(float.NaN, 0f, 0f)),
            "ConfigFile must reject non-finite color components before mutation.");
        Require(config.GetValue(colorKey) == stored,
            "Failed color serialization must preserve the prior configuration token.");
        config.Parse("[graphics]\ntint={\"R\":1,\"G\":0,\"B\":0}\n");
        Expect<InvalidDataException>(() => config.GetValue(colorKey),
            "ConfigFile must reject a color with missing fields during typed decoding.");
        config.Parse("[graphics]\ntint={\"R\":1,\"G\":0,\"B\":0,\"A\":1,\"X\":0}\n");
        Expect<InvalidDataException>(() => config.GetValue(colorKey),
            "ConfigFile must reject unknown color fields during typed decoding.");
        config.Parse("[graphics]\ntint={\"R\":1,\"R\":0,\"G\":0,\"B\":0,\"A\":1}\n");
        Expect<InvalidDataException>(() => config.GetValue(colorKey),
            "ConfigFile must reject duplicate color fields during typed decoding.");
        config.Parse("[graphics]\ntint={\"R\":\"red\",\"G\":0,\"B\":0,\"A\":1}\n");
        Expect<InvalidDataException>(() => config.GetValue(colorKey),
            "ConfigFile must reject nonnumeric color fields during typed decoding.");
    }

    using (var scene = new PackedScene())
    {
        var source = new ColorPackedNode { Name = "ColorRoot", Tint = new Color(0.2f, 0.4f, 1.5f, 0.7f) };
        scene.Pack(source);
        source.Dispose();
        using var instance = (ColorPackedNode)scene.Instantiate();
        Require(instance.Tint == new Color(0.2f, 0.4f, 1.5f, 0.7f),
            "PackedScene must preserve Color stored properties including HDR components.");
    }

    _ = ExerciseColorHotPath(32);
    var beforeAllocations = GC.GetAllocatedBytesForCurrentThread();
    var hotResult = ExerciseColorHotPath(10_000);
    var allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocations;
    Require(allocated == 0 && float.IsFinite(hotResult.R),
        "Warmed numeric color operations must not allocate managed memory.");
}

static void VerifyImages()
{
    var expectedMethods = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        [nameof(Image.AdjustBCS)] = 1,
        [nameof(Image.BlendRect)] = 1,
        [nameof(Image.BlendRectMask)] = 1,
        [nameof(Image.BlitRect)] = 1,
        [nameof(Image.BlitRectMask)] = 1,
        [nameof(Image.BumpMapToNormalMap)] = 1,
        [nameof(Image.ClearMipmaps)] = 1,
        [nameof(Image.ComputeImageMetrics)] = 1,
        [nameof(Image.Convert)] = 1,
        [nameof(Image.CopyFrom)] = 1,
        [nameof(Image.Create)] = 1,
        [nameof(Image.CreateEmpty)] = 1,
        [nameof(Image.CreateFromData)] = 1,
        [nameof(Image.Crop)] = 1,
        [nameof(Image.DetectAlpha)] = 1,
        [nameof(Image.DetectUsedChannels)] = 1,
        [nameof(Image.Fill)] = 1,
        [nameof(Image.FillRect)] = 1,
        [nameof(Image.FixAlphaEdges)] = 1,
        [nameof(Image.FlipX)] = 1,
        [nameof(Image.FlipY)] = 1,
        [nameof(Image.GenerateMipmaps)] = 1,
        [nameof(Image.GetData)] = 1,
        [nameof(Image.GetMipmapOffset)] = 1,
        [nameof(Image.GetPixel)] = 2,
        [nameof(Image.GetRegion)] = 1,
        [nameof(Image.GetUsedRect)] = 1,
        [nameof(Image.LinearToSRGB)] = 1,
        [nameof(Image.Load)] = 1,
        [nameof(Image.LoadFromFile)] = 1,
        [nameof(Image.LoadPNGFromBuffer)] = 1,
        [nameof(Image.LoadJPGFromBuffer)] = 1,
        [nameof(Image.LoadWebPFromBuffer)] = 1,
        [nameof(Image.LoadBMPFromBuffer)] = 1,
        [nameof(Image.LoadTGAFromBuffer)] = 1,
        [nameof(Image.LoadSVGFromBuffer)] = 1,
        [nameof(Image.LoadSVGFromString)] = 1,
        [nameof(Image.NormalMapToXY)] = 1,
        [nameof(Image.PremultiplyAlpha)] = 1,
        [nameof(Image.Resize)] = 1,
        [nameof(Image.ResizeToPowerOfTwo)] = 1,
        [nameof(Image.RGBEToSRGB)] = 1,
        [nameof(Image.Rotate180)] = 1,
        [nameof(Image.Rotate90)] = 1,
        [nameof(Image.SavePNG)] = 1,
        [nameof(Image.SavePNGToBuffer)] = 1,
        [nameof(Image.SaveJPG)] = 1,
        [nameof(Image.SaveJPGToBuffer)] = 1,
        [nameof(Image.SetData)] = 1,
        [nameof(Image.SetPixel)] = 2,
        [nameof(Image.ShrinkX2)] = 1,
        [nameof(Image.SRGBToLinear)] = 1,
    };
    var actualMethods = typeof(Image)
        .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly)
        .Where(method => !method.IsSpecialName)
        .GroupBy(method => method.Name, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
    Require(expectedMethods.Count == actualMethods.Count && expectedMethods.All(pair =>
            actualMethods.TryGetValue(pair.Key, out var count) && count == pair.Value),
        "Image must expose exactly the audited image-processing and codec method surface.");
    var expectedProperties = new[]
    {
        nameof(Image.DataSize), nameof(Image.HasMipmaps), nameof(Image.Height), nameof(Image.IsCompressed),
        nameof(Image.IsEmpty), nameof(Image.IsInvisible), nameof(Image.MipmapCount), nameof(Image.PixelFormat),
        nameof(Image.Size), nameof(Image.Width),
    };
    Require(typeof(Image).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                                       System.Reflection.BindingFlags.DeclaredOnly)
            .Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)
            .SequenceEqual(expectedProperties),
        "Image must expose exactly the audited property surface.");

    using (var empty = new Image())
    {
        Require(empty.IsEmpty && empty.Width == 0 && empty.Height == 0 && empty.Size == Vector2i.Zero &&
                empty.PixelFormat == Image.Format.L8 && !empty.HasMipmaps && empty.MipmapCount == 0 &&
                empty.DataSize == 0 && empty.GetData().Length == 0 && empty.IsInvisible &&
                empty.DetectAlpha() == Image.AlphaMode.None,
            "A default image must expose one canonical empty state.");
        Expect<InvalidOperationException>(() => empty.GetPixel(0, 0), "An empty image must reject pixel access.");
        Expect<InvalidOperationException>(() => empty.GenerateMipmaps(), "An empty image must reject mipmap generation.");
    }

    Require(Image.MaxWidth == 16_777_216 && Image.MaxHeight == 16_777_216 &&
            Enum.GetValues<Image.Format>().Length == 48 && (int)Image.Format.Max == 47 &&
            Enum.GetValues<Image.Interpolation>().Length == 5 && Enum.GetValues<Image.AlphaMode>().Length == 3 &&
            Enum.GetValues<Image.UsedChannels>().Length == 6 && Enum.GetValues<Image.CompressSource>().Length == 3 &&
            Enum.GetValues<Image.CompressMode>().Length == 6 && (int)Image.CompressMode.Max == 5 &&
            Enum.GetValues<Image.ASTCFormat>().Length == 2,
        "Image constants and nested enum identities must remain stable.");
    Require(typeof(Image).GetNestedTypes(System.Reflection.BindingFlags.Public).Length == 7 &&
            (int)Image.CompressMode.S3tc == 0 && (int)Image.CompressMode.Astc == 4 &&
            (int)Image.ASTCFormat.Format4X4 == 0 && (int)Image.ASTCFormat.Format8X8 == 1,
        "Image must expose the complete audited nested enum family and stable identities.");
    Require((int)ClockDirection.Clockwise == 0 && (int)ClockDirection.CounterClockwise == 1,
        "ClockDirection values must remain stable.");

    Expect<ArgumentOutOfRangeException>(() => Image.CreateEmpty(0, 1, false, Image.Format.Rgba8),
        "Image creation must reject zero width.");
    Expect<ArgumentOutOfRangeException>(() => Image.CreateEmpty(1, -1, false, Image.Format.Rgba8),
        "Image creation must reject negative height.");
    Expect<ArgumentOutOfRangeException>(() => Image.CreateEmpty(16_385, 16_385, false, Image.Format.R8),
        "Image creation must enforce the pixel-count ceiling.");
    Expect<ArgumentOutOfRangeException>(() => Image.CreateEmpty(16_384, 16_384, false, Image.Format.Rgba16I),
        "Image creation must reject byte counts beyond a managed array before allocation.");
    Expect<ArgumentOutOfRangeException>(() => Image.CreateEmpty(1, 1, false, Image.Format.Max),
        "Image creation must reject the format sentinel.");
    Expect<ArgumentException>(() => Image.CreateFromData(2, 2, false, Image.Format.Rgba8, new byte[15]),
        "Raw image creation must require an exact byte count.");

    var uncompressedSizes = new Dictionary<Image.Format, int>
    {
        [Image.Format.L8] = 1,
        [Image.Format.La8] = 2,
        [Image.Format.R8] = 1,
        [Image.Format.Rg8] = 2,
        [Image.Format.Rgb8] = 3,
        [Image.Format.Rgba8] = 4,
        [Image.Format.Rgba4444] = 2,
        [Image.Format.Rgb565] = 2,
        [Image.Format.Rf] = 4,
        [Image.Format.Rgf] = 8,
        [Image.Format.Rgbf] = 12,
        [Image.Format.Rgbaf] = 16,
        [Image.Format.Rh] = 2,
        [Image.Format.Rgh] = 4,
        [Image.Format.Rgbh] = 6,
        [Image.Format.Rgbah] = 8,
        [Image.Format.Rgbe9995] = 4,
        [Image.Format.R16] = 2,
        [Image.Format.Rg16] = 4,
        [Image.Format.Rgb16] = 6,
        [Image.Format.Rgba16] = 8,
        [Image.Format.R16I] = 2,
        [Image.Format.Rg16I] = 4,
        [Image.Format.Rgb16I] = 6,
        [Image.Format.Rgba16I] = 8,
    };

    foreach (var pair in uncompressedSizes)
    {
        using var image = Image.CreateEmpty(2, 3, false, pair.Key);
        Require(image.DataSize == pair.Value * 6 && !image.IsCompressed,
            $"{pair.Key} must use its documented uncompressed pixel size.");
        var sample = pair.Key >= Image.Format.R16I
            ? new Color(10f, 20f, 30f, 40f)
            : new Color(0.2f, 0.4f, 0.6f, 0.8f);
        image.SetPixel(1, 2, sample);
        var decoded = image.GetPixel(new Vector2i(1, 2));
        Require(float.IsFinite(decoded.R) && float.IsFinite(decoded.G) && float.IsFinite(decoded.B) && float.IsFinite(decoded.A),
            $"{pair.Key} pixel encoding must produce finite decoded components.");
    }

    var compressedFormats = Enum.GetValues<Image.Format>()
        .Where(format => format is >= Image.Format.Dxt1 and <= Image.Format.Astc8X8Hdr)
        .ToArray();
    Require(compressedFormats.Length == 22, "The complete compressed storage family must be represented.");
    foreach (var format in compressedFormats)
    {
        using var image = Image.CreateEmpty(4, 4, false, format);
        Require(image.IsCompressed && image.DataSize is 8 or 16,
            $"{format} must use one correctly sized 4x4-or-larger storage block.");
        Expect<InvalidOperationException>(() => image.GetPixel(0, 0),
            $"{format} must reject direct pixel access without decompression.");
        Expect<InvalidOperationException>(() => image.Fill(Colors.Red),
            $"{format} must reject pixel mutation without decompression.");
        Require(!image.IsInvisible &&
                image.DetectAlpha() == (format is Image.Format.Dxt3 or Image.Format.Dxt5
                    ? Image.AlphaMode.Blend
                    : Image.AlphaMode.None),
            $"{format} alpha metadata queries must not require a CPU decoder.");
    }

    using (var compressedMipmaps = Image.CreateEmpty(8, 8, true, Image.Format.Dxt1))
    {
        Require(compressedMipmaps.DataSize == 56 && compressedMipmaps.MipmapCount == 3 &&
                compressedMipmaps.GetMipmapOffset(1) == 32 && compressedMipmaps.GetMipmapOffset(3) == 48,
            "Compressed mip layouts must respect block minima at every level.");
    }

    using (var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8))
    {
        var changes = 0;
        image.Changed += _ => changes++;
        image.Fill(new Color(0.25f, 0.5f, 0.75f, 1f));
        Require(ColorNearlyEqual(image.GetPixel(3, 3), new Color(63 / 255f, 127 / 255f, 191 / 255f, 1f), 0.0001f) && changes == 1,
            "Fill must encode the complete buffer and publish one post-commit change.");
        var exported = image.GetData();
        exported[0] = 255;
        Require(image.GetData()[0] == 63, "GetData must not expose mutable engine storage.");

        image.Fill(new Color(0f, 0f, 0f, 0f));
        Require(image.IsInvisible, "An alpha-capable image with zero alpha must be invisible.");
        image.FillRect(new Rect2i(-1, -1, 3, 3), Colors.Red);
        Require(image.GetPixel(0, 0).R == 1f && image.GetPixel(1, 1).R == 1f && image.GetPixel(2, 2).A == 0f,
            "FillRect must clip a half-open rectangle to image bounds.");
        Require(image.DetectAlpha() == Image.AlphaMode.Bit && !image.IsInvisible &&
                image.GetUsedRect() == new Rect2i(0, 0, 2, 2) && image.DetectUsedChannels() == Image.UsedChannels.Rgba,
            "Alpha and used-region detection must inspect the base level.");
        Expect<ArgumentOutOfRangeException>(() => image.DetectUsedChannels((Image.CompressSource)3),
            "Channel detection must reject its source sentinel.");

        image.GenerateMipmaps();
        Require(image.HasMipmaps && image.MipmapCount == 2 && image.DataSize == 84 &&
                image.GetMipmapOffset(0) == 0 && image.GetMipmapOffset(1) == 64 && image.GetMipmapOffset(2) == 80,
            "Mipmap generation must produce the complete 4x4, 2x2, 1x1 chain.");
        var priorMip = image.GetData()[image.GetMipmapOffset(1)];
        image.SetPixel(0, 0, Colors.Blue);
        Require(image.GetData()[image.GetMipmapOffset(1)] == priorMip,
            "Per-pixel edits must leave existing mip bytes intact until explicit regeneration.");
        image.GenerateMipmaps();
        Require(image.GetData()[image.GetMipmapOffset(1)] != priorMip,
            "Explicit regeneration must refresh a stale mip level from edited base pixels.");
        image.ClearMipmaps();
        Require(!image.HasMipmaps && image.DataSize == 64, "ClearMipmaps must retain only the base level.");

        var before = image.GetData();
        Expect<ArgumentException>(() => image.SetData(2, 2, false, Image.Format.Rgba8, new byte[15]),
            "SetData must reject malformed data before mutation.");
        Require(image.GetData().SequenceEqual(before), "A rejected SetData call must preserve image state.");
    }

    var supplied = new byte[] { 1, 2, 3, 4 };
    using (var copied = Image.CreateFromData(1, 1, false, Image.Format.Rgba8, supplied))
    using (var target = new Image())
    {
        supplied[0] = 99;
        Require(copied.GetData()[0] == 1, "CreateFromData must copy caller storage.");
        target.CopyFrom(copied);
        copied.SetPixel(0, 0, Colors.White);
        Require(target.GetData()[0] == 1 && target.PixelFormat == Image.Format.Rgba8,
            "CopyFrom must snapshot independent pixel storage while preserving a valid format.");
    }

    using (var onePixel = Image.CreateEmpty(1, 1, true, Image.Format.Rgba8))
    {
        Require(!onePixel.HasMipmaps && onePixel.MipmapCount == 0 && onePixel.GetMipmapOffset(0) == 0,
            "A 1x1 image must canonicalize an impossible mip chain to base-only storage.");
        Expect<ArgumentOutOfRangeException>(() => onePixel.GetMipmapOffset(1),
            "Mipmap offsets must reject levels that are not stored.");
    }

    using (var integerAlpha = Image.CreateEmpty(2, 1, false, Image.Format.Rgba16I))
    using (var integerSource = Image.CreateEmpty(1, 1, false, Image.Format.Rgba16I))
    {
        integerAlpha.Fill(new Color(0f, 0f, 1000f, 65535f));
        integerAlpha.SetPixel(0, 0, new Color(0f, 0f, 0f, 32768f));
        Require(integerAlpha.DetectAlpha() == Image.AlphaMode.Blend &&
                integerAlpha.DetectUsedChannels() == Image.UsedChannels.Rgba,
            "Integer alpha detection must use the 16-bit opaque endpoint, not normalized one.");
        integerSource.Fill(new Color(1000f, 0f, 0f, 32768f));
        integerAlpha.BlendRect(integerSource, new Rect2i(0, 0, 1, 1), new Vector2i(1, 0));
        var mixed = integerAlpha.GetPixel(1, 0);
        Require(mixed.R is >= 499f and <= 501f && mixed.B is >= 499f and <= 501f && mixed.A == 65535f,
            "Integer-alpha compositing must normalize only alpha during straight-alpha mixing.");
    }

    using (var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8))
    {
        image.SetPixel(0, 0, Colors.Red);
        image.SetPixel(1, 0, Colors.Green);
        image.SetPixel(0, 1, Colors.Blue);
        image.SetPixel(1, 1, Colors.White);
        image.FlipX();
        Require(image.GetPixel(0, 0).G > 0f && image.GetPixel(1, 0).R == 1f,
            "FlipX must reverse each row.");
        image.FlipY();
        Require(image.GetPixel(0, 0) == Colors.White, "FlipY must reverse row order.");
        image.Rotate180();
        Require(image.GetPixel(1, 1) == Colors.White, "Rotate180 must reverse both axes.");
        image.Rotate90(ClockDirection.Clockwise);
        Require(image.Size == new Vector2i(2, 2), "Rotate90 must swap dimensions while preserving pixel count.");
        Expect<ArgumentOutOfRangeException>(() => image.Rotate90((ClockDirection)7),
            "Rotate90 must reject an undefined direction.");

        using var region = image.GetRegion(new Rect2i(1, 1, 4, 4));
        Require(region.Size == Vector2i.One && region.GetPixel(0, 0) == image.GetPixel(1, 1),
            "GetRegion must return only the clipped source intersection.");
        using var emptyRegion = image.GetRegion(new Rect2i(9, 9, 1, 1));
        Require(emptyRegion.IsEmpty, "GetRegion must return an empty image for a disjoint rectangle.");
    }

    using (var rectangular = Image.CreateEmpty(3, 2, false, Image.Format.R8))
    {
        rectangular.SetPixel(0, 0, new Color(1f, 0f, 0f));
        rectangular.Rotate90(ClockDirection.Clockwise);
        Require(rectangular.Size == new Vector2i(2, 3) && rectangular.GetPixel(1, 0).R == 1f,
            "Clockwise rotation must map a rectangular image's top-left pixel to its top-right corner.");
        rectangular.Rotate90(ClockDirection.CounterClockwise);
        Require(rectangular.Size == new Vector2i(3, 2) && rectangular.GetPixel(0, 0).R == 1f,
            "Counterclockwise rotation must invert a prior clockwise quarter turn.");
    }

    foreach (var interpolation in Enum.GetValues<Image.Interpolation>())
    {
        using var image = Image.CreateEmpty(3, 2, true, Image.Format.Rgba8);
        image.Fill(Colors.Red);
        image.Resize(7, 5, interpolation);
        Require(image.Size == new Vector2i(7, 5) && image.HasMipmaps && ColorNearlyEqual(image.GetPixel(6, 4), Colors.Red, 0.01f),
            $"{interpolation} resizing must preserve dimensions, mipmap policy, and a constant field.");
    }

    using (var image = Image.CreateEmpty(3, 5, false, Image.Format.Rgba8))
    {
        image.ResizeToPowerOfTwo();
        Require(image.Size == new Vector2i(4, 8), "ResizeToPowerOfTwo must round dimensions independently.");
        image.ResizeToPowerOfTwo(square: true, Image.Interpolation.Nearest);
        Require(image.Size == new Vector2i(8, 8), "Square power-of-two resizing must use the larger dimension.");
        image.ShrinkX2();
        Require(image.Size == new Vector2i(4, 4), "ShrinkX2 must halve both dimensions.");
        image.Crop(6, 3);
        Require(image.Size == new Vector2i(6, 3) && image.GetPixel(5, 2) == default,
            "Crop must fill expanded pixels with transparent black.");
    }

    using (var largeFormat = Image.CreateEmpty(1, 1, false, Image.Format.Rgba16I))
    {
        Expect<ArgumentOutOfRangeException>(() => largeFormat.Crop(16_384, 16_384),
            "Crop must reject byte-count overflow before changing state.");
        Expect<ArgumentOutOfRangeException>(() => largeFormat.Resize(16_384, 16_384),
            "Resize must reject byte-count overflow before resampling.");
        Require(largeFormat.Size == Vector2i.One,
            "Failed size changes must preserve the original image.");
    }

    using (var destination = Image.CreateEmpty(3, 2, true, Image.Format.Rgba8))
    using (var source = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8))
    using (var mask = Image.CreateEmpty(2, 2, false, Image.Format.La8))
    {
        destination.Fill(new Color(0f, 0f, 1f, 1f));
        source.Fill(new Color(1f, 0f, 0f, 0.5f));
        mask.Fill(new Color(1f, 1f, 1f, 0f));
        mask.SetPixel(1, 0, Colors.White);
        destination.BlendRectMask(source, mask, new Rect2i(0, 0, 2, 2), new Vector2i(1, 0));
        Require(ColorNearlyEqual(destination.GetPixel(2, 0), new Color(0.5f, 0f, 0.5f, 1f), 0.01f) &&
                destination.GetPixel(1, 0).B == 1f && destination.HasMipmaps,
            "Masked blending must honor mask alpha, clipping, straight alpha, and mipmap rebuilding.");
        destination.BlitRectMask(source, mask, new Rect2i(0, 0, 2, 2), Vector2i.Zero);
        Require(destination.GetPixel(1, 0).R == 1f && destination.GetPixel(0, 0).B == 1f,
            "Masked blitting must copy only selected source pixels without blending.");
        destination.BlendRect(source, new Rect2i(0, 0, 1, 1), Vector2i.Zero);
        Require(destination.GetPixel(0, 0).R > 0f && destination.GetPixel(0, 0).B > 0f,
            "Unmasked blending must composite straight-alpha source and destination colors.");
        source.Fill(Colors.Green);
        destination.BlitRect(source, new Rect2i(0, 0, 2, 2), new Vector2i(-1, 0));
        Require(destination.GetPixel(0, 0).G > 0f, "BlitRect must clip negative destinations while keeping source alignment.");
        using var wrongFormat = Image.CreateEmpty(1, 1, false, Image.Format.Rgb8);
        Expect<ArgumentException>(() => destination.BlitRect(wrongFormat, new Rect2i(0, 0, 1, 1), Vector2i.Zero),
            "BlitRect must reject format mismatch.");
    }

    using (var image = Image.CreateEmpty(3, 1, false, Image.Format.Rgba8))
    {
        image.SetPixel(0, 0, new Color(1f, 0f, 0f, 0f));
        image.SetPixel(1, 0, new Color(0f, 1f, 0f, 1f));
        image.SetPixel(2, 0, new Color(0f, 0f, 1f, 0.5f));
        Require(image.DetectAlpha() == Image.AlphaMode.Blend && image.DetectUsedChannels() == Image.UsedChannels.Rgba,
            "Alpha detection must distinguish fractional transparency.");
        image.FixAlphaEdges();
        Require(image.GetPixel(0, 0).G == 1f && image.GetPixel(0, 0).A == 0f,
            "FixAlphaEdges must copy the nearest opaque RGB without changing alpha.");
        image.PremultiplyAlpha();
        Require(image.GetData()[10] == 127, "PremultiplyAlpha must use deterministic 8-bit rounding.");
        image.AdjustBCS(1f, 1f, 0f);
        var desaturated = image.GetPixel(1, 0);
        Require(desaturated.R == desaturated.G && desaturated.G == desaturated.B,
            "Zero saturation must collapse RGB to its arithmetic mean.");
        Expect<ArgumentOutOfRangeException>(() => image.AdjustBCS(float.NaN, 1f, 1f),
            "Color adjustment must reject non-finite factors.");
    }

    using (var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8))
    {
        image.SetPixel(0, 0, new Color(0.5f, 0.25f, 0.75f, 1f));
        var original = image.GetPixel(0, 0);
        image.SRGBToLinear();
        image.LinearToSRGB();
        Require(ColorNearlyEqual(image.GetPixel(0, 0), original, 0.01f),
            "sRGB conversions must approximately round-trip normalized RGB8 data.");
        image.Convert(Image.Format.Rgbaf);
        Require(image.PixelFormat == Image.Format.Rgbaf && image.DataSize == 16 &&
                ColorNearlyEqual(image.GetPixel(0, 0), original, 0.01f),
            "Convert must preserve decoded color while changing raw layout.");
        Expect<InvalidOperationException>(() => image.Convert(Image.Format.Dxt1),
            "Convert must reject compression without a compression backend.");
    }

    using (var bump = Image.CreateEmpty(2, 2, false, Image.Format.L8))
    {
        bump.Fill(Colors.Black);
        bump.SetPixel(1, 0, Colors.White);
        bump.BumpMapToNormalMap(1f);
        Require(bump.PixelFormat == Image.Format.Rgba8 && bump.GetPixel(0, 0).A == 1f,
            "Bump-map conversion must produce opaque RGBA8 normals.");
        bump.NormalMapToXY();
        Require(bump.PixelFormat == Image.Format.La8 && bump.DataSize == 8,
            "NormalMapToXY must pack X and Y into two channels.");
    }

    using (var rgbe = Image.CreateEmpty(2, 1, true, Image.Format.Rgbe9995))
    {
        rgbe.SetPixel(0, 0, new Color(2f, 1f, 0.5f));
        rgbe.SetPixel(1, 0, new Color(0.25f, 0.5f, 1f));
        using var srgb = rgbe.RGBEToSRGB();
        Require(srgb.PixelFormat == Image.Format.Rgb8 && srgb.Size == rgbe.Size && srgb.HasMipmaps,
            "RGBE conversion must return an RGB8 copy and preserve mipmap policy.");
    }

    using (var first = Image.CreateEmpty(2, 1, false, Image.Format.Rgba8))
    using (var second = Image.CreateEmpty(2, 1, false, Image.Format.Rgba8))
    {
        first.Fill(Colors.Black);
        second.Fill(Colors.Black);
        var identical = first.ComputeImageMetrics(second, useLuma: false);
        Require(identical.Maximum == 0d && identical.RootMeanSquared == 0d && identical.PeakSignalToNoiseRatio == 500d,
            "Identical image metrics must report zero error and capped peak SNR.");
        second.SetPixel(0, 0, Colors.White);
        var componentMetrics = first.ComputeImageMetrics(second, useLuma: false);
        Require(componentMetrics.Maximum == 255d && componentMetrics.Mean == 95.625d &&
                componentMetrics.MeanSquared == 24_384.375d,
            "RGBA metrics must count each channel over the common base-level area.");
        var different = first.ComputeImageMetrics(second, useLuma: true);
        Require(different.Maximum == 255d && different.Mean > 0d && different.RootMeanSquared > 0d,
            "Image metrics must report nonzero luma error.");
    }

    using (var hdr = Image.CreateEmpty(1, 1, false, Image.Format.Rgbaf))
    using (var reference = Image.CreateEmpty(1, 1, false, Image.Format.Rgbaf))
    {
        hdr.SetPixel(0, 0, new Color(2f, 0f, 0f, 1f));
        Expect<InvalidOperationException>(() => hdr.ComputeImageMetrics(reference, useLuma: false),
            "Eight-bit error metrics must reject HDR components outside normalized range.");
    }

    using (var source = Image.CreateEmpty(2, 2, true, Image.Format.Rgba8))
    {
        source.Fill(new Color(0.2f, 0.4f, 0.6f, 0.8f));
        using var duplicate = (Image)source.Duplicate(deep: true);
        duplicate.SetPixel(0, 0, Colors.Red);
        Require(source.GetPixel(0, 0) != duplicate.GetPixel(0, 0) && duplicate.HasMipmaps &&
                duplicate.GetPropertyList().Select(property => property.Name).Contains(nameof(Image.DataSize)),
            "Image duplication must own an independent buffer and expose typed image descriptors.");
    }

    using (var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8))
    {
        Action<Resource> throwing = _ => throw new InvalidOperationException("expected image observer failure");
        image.Changed += throwing;
        Expect<InvalidOperationException>(() => image.SetPixel(0, 0, Colors.Red),
            "A throwing image observer must propagate after commit.");
        image.Changed -= throwing;
        Require(image.GetPixel(0, 0) == Colors.Red,
            "Image state must remain committed when a change observer throws.");
    }

    var disposed = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
    disposed.Dispose();
    Expect<ObjectDisposedException>(() => _ = disposed.Width, "Disposed images must reject reads.");
    Expect<ObjectDisposedException>(() => disposed.Fill(Colors.Red), "Disposed images must reject writes.");

    using var concurrent = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
    Parallel.For(0, 256, index => concurrent.SetPixel(index % 16, index / 16, Colors.White));
    Require(Enumerable.Range(0, 256).All(index => concurrent.GetPixel(index % 16, index / 16) == Colors.White),
        "Concurrent per-pixel mutations must serialize without lost writes or torn reads.");
}

static Color ExerciseColorHotPath(int iterations)
{
    var value = new Color(0.1f, 0.2f, 0.3f, 0.4f);
    var destination = new Color(0.9f, 0.8f, 0.7f, 0.6f);
    for (var index = 0; index < iterations; index++)
    {
        value = value.Lerp(destination, 0.0001f).SRGBToLinear().LinearToSRGB();
        value = (value * 1.00001f).Clamp(new Color(-10f, -10f, -10f, -10f), new Color(10f, 10f, 10f, 10f));
    }

    return value;
}

static bool ColorNearlyEqual(Color left, Color right, float epsilon = 0.0001f) =>
    NearlyEqual(left.R, right.R, epsilon) && NearlyEqual(left.G, right.G, epsilon) &&
    NearlyEqual(left.B, right.B, epsilon) && NearlyEqual(left.A, right.A, epsilon);

static void VerifyVector2Values()
{
    Require(Marshal.SizeOf<Vector2>() == 8 && typeof(Vector2).IsDefined(typeof(SerializableAttribute), false) &&
            typeof(Vector2).StructLayoutAttribute?.Value == LayoutKind.Sequential,
        "Vector2 must be a serializable sequential two-float value type.");
    Require(default(Vector2) == Vector2.Zero && Vector2.One == new Vector2(1f, 1f) &&
            Vector2.Up == new Vector2(0f, -1f) && Vector2.Down == new Vector2(0f, 1f) &&
            Vector2.Left == new Vector2(-1f, 0f) && Vector2.Right == new Vector2(1f, 0f) &&
            float.IsPositiveInfinity(Vector2.Inf.X),
        "Vector2 constants must use screen-space directions and stable values.");

    var indexed = new Vector2(1f, 2f);
    indexed[0] = 3f;
    indexed[1] = 4f;
    var (x, y) = indexed;
    Require(indexed == new Vector2(3f, 4f) && x == 3f && y == 4f &&
            (int)Vector2.Axis.X == 0 && (int)Vector2.Axis.Y == 1,
        "Vector2 indexing, axes, and deconstruction must preserve component order.");
    Expect<ArgumentOutOfRangeException>(() => _ = indexed[-1], "Vector2 must reject negative indices.");
    Expect<ArgumentOutOfRangeException>(() => indexed[2] = 0f, "Vector2 must reject indices after Y.");

    var value = new Vector2(3f, 4f);
    Require(value.LengthSquared() == 25f && value.Length() == 5f && value.Normalized().IsNormalized() &&
            Vector2.Zero.Normalized() == Vector2.Zero && value.Dot(Vector2.Right) == 3f &&
            Vector2.Right.Cross(Vector2.Down) == 1f && value.Aspect() == 0.75f,
        "Vector2 length, normalization, dot, cross, and aspect operations must be stable.");
    Require(new Vector2(float.NaN, 1f).Normalized() == Vector2.Zero &&
            new Vector2(1f, float.PositiveInfinity).Normalized() == Vector2.Zero &&
            Vector2.Zero.DirectionTo(new Vector2(float.NaN, 1f)) == Vector2.Zero,
        "Vector2 normalization and direction must reject non-finite components.");
    Require(NearlyEqual(Vector2.Right.Angle(), 0f) && NearlyEqual(Vector2.Right.AngleTo(Vector2.Down), System.MathF.PI / 2f) &&
            NearlyEqual(Vector2.Zero.AngleToPoint(Vector2.Down), System.MathF.PI / 2f) &&
            Vector2.FromAngle(System.MathF.PI / 2f).IsEqualApprox(Vector2.Down) &&
            Vector2.Right.Rotated(System.MathF.PI / 2f).IsEqualApprox(Vector2.Down) &&
            Vector2.Right.Orthogonal() == Vector2.Up,
        "Vector2 angular operations must follow clockwise screen coordinates.");
    Require(new Vector2(-1.2f, 2.2f).Abs() == new Vector2(1.2f, 2.2f) &&
            new Vector2(1.2f, -2.2f).Ceil() == new Vector2(2f, -2f) &&
            new Vector2(1.8f, -2.2f).Floor() == new Vector2(1f, -3f) &&
            new Vector2(1.5f, 2.5f).Round() == new Vector2(2f, 2f) &&
            new Vector2(-2f, 0f).Sign() == new Vector2(-1f, 0f),
        "Vector2 component rounding, sign, and absolute operations must match scalar behavior.");
    Expect<ArithmeticException>(() => new Vector2(float.NaN, 0f).Sign(), "Vector2 Sign must reject NaN.");
    Require(new Vector2(5f, -2f).Clamp(new Vector2(0f, -1f), new Vector2(4f, 1f)) == new Vector2(4f, -1f) &&
            new Vector2(5f, -2f).Clamp(0f, 3f) == new Vector2(3f, 0f),
        "Vector2 clamp overloads must clamp every component.");
    Expect<ArgumentException>(() => new Vector2(1f, 2f).Clamp(2f, 1f), "Vector2 must reject reversed scalar clamp bounds.");
    Expect<ArgumentException>(() => new Vector2(1f, 2f).Clamp(new Vector2(2f, 0f), new Vector2(1f, 3f)),
        "Vector2 must reject reversed component clamp bounds.");

    Require(Vector2.Zero.DistanceSquaredTo(value) == 25f && Vector2.Zero.DistanceTo(value) == 5f &&
            Vector2.Zero.DirectionTo(new Vector2(0f, 2f)) == Vector2.Down &&
            Vector2.Zero.DirectionTo(Vector2.Zero) == Vector2.Zero &&
            Vector2.Zero.Lerp(new Vector2(4f, 8f), 0.25f) == new Vector2(1f, 2f) &&
            new Vector2(10f, 0f).LimitLength(3f) == new Vector2(3f, 0f) &&
            new Vector2(2f, 0f).LimitLength() == Vector2.Right &&
            Vector2.Right.MoveToward(new Vector2(4f, 0f), 2f) == new Vector2(3f, 0f),
        "Vector2 distance and interpolation operations must cover zero and nonzero vectors.");
    Require(Vector2.Zero.MoveToward(new Vector2(0.000005f, 0f), 0f) == new Vector2(0.000005f, 0f) &&
            Vector2.Zero.MoveToward(new Vector2(0.00002f, 0f), 0f) == Vector2.Zero &&
            Vector2.Zero.MoveToward(new Vector2(0.000005f, 0f), -1f) == new Vector2(0.000005f, 0f),
        "Vector2 MoveToward uses the source 1e-5 proximity threshold even for a negative step.");
    Require(new Vector2(1f, 5f).Max(new Vector2(3f, 2f)) == new Vector2(3f, 5f) &&
            new Vector2(1f, 5f).Max(4f) == new Vector2(4f, 5f) &&
            new Vector2(1f, 5f).Min(new Vector2(3f, 2f)) == new Vector2(1f, 2f) &&
            new Vector2(1f, 5f).Min(2f) == new Vector2(1f, 2f) &&
            Vector2.One.MaxAxisIndex() == Vector2.Axis.X && Vector2.One.MinAxisIndex() == Vector2.Axis.Y,
        "Vector2 min/max methods and tie-breaking axis rules must be stable.");
    Require(new Vector2(-1f, 7f).PosMod(4f) == new Vector2(3f, 3f) &&
            new Vector2(-1f, 7f).PosMod(new Vector2(4f, 3f)) == new Vector2(3f, 1f) &&
            new Vector2(3f, 4f).Project(Vector2.Right) == new Vector2(3f, 0f) &&
            new Vector2(1f, 1f).Slide(Vector2.Up) == Vector2.Right &&
            new Vector2(1f, 1f).Reflect(Vector2.Up) == new Vector2(-1f, 1f) &&
            new Vector2(1f, 1f).Bounce(Vector2.Up) == new Vector2(1f, -1f),
        "Vector2 modulus, projection, slide, reflection, and bounce semantics must be stable.");
    Require(Vector2.Right.Slerp(Vector2.Down, 0.5f).IsEqualApprox(new Vector2(System.MathF.Sqrt(0.5f), System.MathF.Sqrt(0.5f))) &&
            Vector2.Zero.Slerp(Vector2.One, 0.5f) == new Vector2(0.5f, 0.5f) &&
            new Vector2(5.1f, -5.1f).Snapped(2f) == new Vector2(6f, -6f) &&
            new Vector2(5.1f, -5.1f).Snapped(new Vector2(2f, 5f)) == new Vector2(6f, -5f),
        "Vector2 spherical interpolation and snapping must handle fallback and signed values.");
    var bezier = Vector2.Zero.BezierInterpolate(Vector2.Right, Vector2.One, Vector2.Down, 0.5f);
    var derivative = Vector2.Zero.BezierDerivative(Vector2.Right, Vector2.One, Vector2.Down, 0.5f);
    Require(bezier.IsEqualApprox(new Vector2(0.75f, 0.5f)) && derivative.IsEqualApprox(new Vector2(0f, 1.5f)) &&
            Vector2.Zero.CubicInterpolate(new Vector2(2f, 2f), new Vector2(-2f, -2f), new Vector2(4f, 4f), 0.5f)
                .IsEqualApprox(new Vector2(1f, 1f)) &&
            Vector2.Zero.CubicInterpolateInTime(new Vector2(2f, 2f), new Vector2(-2f, -2f), new Vector2(4f, 4f), 0.5f, 1f, -1f, 2f)
                .IsEqualApprox(new Vector2(1f, 1f)),
        "Vector2 cubic and Bezier interpolation must preserve symmetric fixtures.");
    Require(new Vector2(2f, 4f).Inverse() == new Vector2(0.5f, 0.25f) &&
            new Vector2(Mathf.Epsilon * 0.5f, -Mathf.Epsilon * 0.5f).IsZeroApprox() &&
            new Vector2(1f, 2f).IsFinite() && !new Vector2(float.PositiveInfinity, 0f).IsFinite() &&
            new Vector2(1f, 2f).IsEqualApprox(new Vector2(1.000001f, 2f)),
        "Vector2 reciprocal, finite, zero, and approximate predicates must be stable.");
    Require(new Vector2(1f, 2f) + new Vector2(3f, 4f) == new Vector2(4f, 6f) &&
            +value == value && -Vector2.One == new Vector2(-1f, -1f) &&
            Vector2.One * 2f == 2f * Vector2.One && Vector2.One * 2 == new Vector2(2f, 2f) &&
            Vector2.One * new Vector2(2f, 3f) == new Vector2(2f, 3f) &&
            new Vector2(4f, 6f) / 2f == new Vector2(2f, 3f) &&
            new Vector2(4f, 6f) / 2 == new Vector2(2f, 3f) &&
            new Vector2(4f, 6f) / new Vector2(2f, 3f) == new Vector2(2f, 2f) &&
            new Vector2(5f, -5f) % 3f == new Vector2(2f, -2f) &&
            new Vector2(5f, 8f) % new Vector2(3f, 5f) == new Vector2(2f, 3f),
        "Vector2 arithmetic operators must be componentwise.");
    Require(new Vector2(0f, 9f) < new Vector2(1f, -9f) && new Vector2(1f, 2f) <= new Vector2(1f, 2f) &&
            new Vector2(2f, 0f) > new Vector2(1f, 99f) && new Vector2(2f, 0f) >= new Vector2(2f, 0f) &&
            value.Equals((object)new Vector2(3f, 4f)) && value.GetHashCode() == new Vector2(3f, 4f).GetHashCode(),
        "Vector2 equality, hashing, and lexicographic ordering must be stable.");

    var integer = new Vector2i(7, -8);
    Require(new Vector2(integer) == new Vector2(7f, -8f) && (Vector2)integer == new Vector2(7f, -8f) &&
            (Vector2i)new Vector2(7.9f, -8.9f) == integer,
        "Vector2 and Vector2i conversions must widen implicitly and truncate explicitly.");
    Expect<ArgumentOutOfRangeException>(() => _ = (Vector2i)new Vector2(float.NaN, 0f),
        "Vector2 to Vector2i conversion must reject non-finite values.");
    Expect<ArgumentOutOfRangeException>(() => _ = (Vector2i)new Vector2(2147483648f, 0f),
        "Vector2 to Vector2i conversion must reject out-of-range values.");
    VerifyInvariantString(() => new Vector2(1.5f, -2.5f).ToString("F1"), "(1.5, -2.5)", "Vector2");
    Expect<FormatException>(() => _ = value.ToString("Q"), "Vector2 must reject invalid numeric formats.");

    var key = new ConfigKey<Vector2>("math", "vector2");
    using var config = new ConfigFile();
    config.SetValue(key, value);
    Require(config.GetValue(key) == value && config.EncodeToText() == "[math]\n\nvector2={\"X\":3,\"Y\":4}\n",
        "ConfigFile must preserve the strict Vector2 schema.");
    Expect<JsonException>(() => config.SetValue(key, new Vector2(float.NaN, 0f)), "ConfigFile must reject non-finite Vector2 values.");
    config.Parse("[math]\nvector2={\"X\":1}\n");
    Expect<InvalidDataException>(() => config.GetValue(key), "ConfigFile must reject incomplete Vector2 values.");
    config.Parse("[math]\nvector2={\"X\":1,\"Y\":2,\"Z\":3}\n");
    Expect<InvalidDataException>(() => config.GetValue(key), "ConfigFile must reject unknown Vector2 fields.");
}

static void VerifyVector2iValues()
{
    Require(Marshal.SizeOf<Vector2i>() == 8 && typeof(Vector2i).IsDefined(typeof(SerializableAttribute), false) &&
            Vector2i.MinValue.X == int.MinValue && Vector2i.MaxValue.Y == int.MaxValue &&
            Vector2i.Zero == default && Vector2i.One == new Vector2i(1, 1) &&
            Vector2i.Up == new Vector2i(0, -1) && Vector2i.Down == new Vector2i(0, 1) &&
            Vector2i.Left == new Vector2i(-1, 0) && Vector2i.Right == new Vector2i(1, 0),
        "Vector2i layout and constants must be stable.");
    var value = new Vector2i(3, 4);
    var (x, y) = value;
    Require(value[0] == 3 && value[1] == 4 && x == 3 && y == 4 && value.LengthSquared() == 25 && value.Length() == 5f &&
            value.DistanceSquaredTo(Vector2i.Zero) == 25 && value.DistanceTo(Vector2i.Zero) == 5f && value.Aspect() == 0.75f,
        "Vector2i indexing, deconstruction, length, distance, and aspect must be stable.");
    var copy = value;
    copy[0] = -7;
    copy[1] = 9;
    Require(copy == new Vector2i(-7, 9) && value == new Vector2i(3, 4),
        "Vector2i component writes must not mutate a copied value.");
    Expect<ArgumentOutOfRangeException>(() => _ = value[2], "Vector2i must reject indices after Y.");
    Expect<ArgumentOutOfRangeException>(() => _ = value[-1], "Vector2i must reject negative indices.");
    Expect<ArgumentOutOfRangeException>(() => copy[2] = 1, "Vector2i must reject out-of-range component writes.");
    Require(float.IsPositiveInfinity(new Vector2i(1, 0).Aspect()) && float.IsNaN(Vector2i.Zero.Aspect()),
        "Vector2i aspect must preserve IEEE zero-division results.");
    var large = new Vector2i(50_000, 0);
    Require(typeof(Vector2i).GetMethod(nameof(Vector2i.LengthSquared))!.ReturnType == typeof(long) &&
            large.LengthSquared() == 2_500_000_000L && large.DistanceSquaredTo(Vector2i.Zero) == 2_500_000_000L &&
            large.Length() == 50_000f && large.DistanceTo(Vector2i.Zero) == 50_000f,
        "Vector2i norms must widen before squaring and expose the signed 64-bit result.");
    Require(new Vector2i(-1_500_000_000, 0).DistanceSquaredTo(new Vector2i(1_500_000_000, 0)) == 9_000_000_000_000_000_000L &&
            new Vector2i(-1_500_000_000, 0).DistanceTo(new Vector2i(1_500_000_000, 0)) == 3_000_000_000f,
        "Vector2i distance must widen before subtracting components across the Int32 span.");
    Require(Vector2i.MaxValue.LengthSquared() == 2L * int.MaxValue * int.MaxValue &&
            float.IsFinite(Vector2i.MinValue.Length()) &&
            System.MathF.Abs(Vector2i.MinValue.Length() - (float)(Math.Sqrt(2d) * 2_147_483_648d)) <= 512f &&
            float.IsFinite(Vector2i.MinValue.DistanceTo(Vector2i.MaxValue)) &&
            System.MathF.Abs(Vector2i.MinValue.DistanceTo(Vector2i.MaxValue) - (float)(Math.Sqrt(2d) * uint.MaxValue)) <= 512f,
        "Vector2i lengths and distances must remain finite across the entire component range.");
    Expect<OverflowException>(() => _ = Vector2i.MinValue.LengthSquared(),
        "Vector2i squared length must reject a result above Int64.MaxValue.");
    Expect<OverflowException>(() => _ = Vector2i.MinValue.DistanceSquaredTo(Vector2i.MaxValue),
        "Vector2i squared distance must reject a widened difference above Int64.MaxValue.");
    Require(new Vector2i(-3, 4).Abs() == value && new Vector2i(-3, 0).Sign() == new Vector2i(-1, 0) &&
            new Vector2i(5, -2).Clamp(0, 4) == new Vector2i(4, 0) &&
            new Vector2i(5, -2).Clamp(new Vector2i(1, -1), new Vector2i(4, 3)) == new Vector2i(4, -1),
        "Vector2i absolute, sign, and clamp methods must be componentwise.");
    Expect<OverflowException>(() => Vector2i.MinValue.Abs(), "Vector2i Abs must surface minimum-integer overflow.");
    Expect<ArgumentException>(() => value.Clamp(2, 1), "Vector2i must reject reversed clamp bounds.");
    Require(new Vector2i(1, 5).Max(new Vector2i(3, 2)) == new Vector2i(3, 5) &&
            new Vector2i(1, 5).Max(4) == new Vector2i(4, 5) &&
            new Vector2i(1, 5).Min(new Vector2i(3, 2)) == new Vector2i(1, 2) &&
            new Vector2i(1, 5).Min(2) == new Vector2i(1, 2) &&
            Vector2i.One.MaxAxisIndex() == Vector2i.Axis.X && Vector2i.One.MinAxisIndex() == Vector2i.Axis.Y &&
            new Vector2i(5, -5).Snapped(2) == new Vector2i(6, -4) &&
            new Vector2i(5, -5).Snapped(new Vector2i(2, 5)) == new Vector2i(6, -5),
        "Vector2i min, max, axis tie-breaking, and snapping must be stable.");
    Require(new Vector2i(5, -5).Snapped(-2) == new Vector2i(4, -6) &&
            new Vector2i(5, -5).Snapped(new Vector2i(-2, 0)) == new Vector2i(4, -5) &&
            value.Snapped(0) == value,
        "Vector2i negative and zero snapping steps must match the scalar formula.");
    Expect<OverflowException>(() => _ = Vector2i.MaxValue.Snapped(2),
        "Vector2i snapping must reject results outside Int32 range.");
    Require(value + Vector2i.One == new Vector2i(4, 5) && +value == value && value - Vector2i.One == new Vector2i(2, 3) &&
            -value == new Vector2i(-3, -4) && value * 2 == 2 * value &&
            value * 0.5f == 0.5f * value && value / 2f == new Vector2(1.5f, 2f) &&
            value * new Vector2i(2, 3) == new Vector2i(6, 12) &&
            new Vector2i(7, -7) / 2 == new Vector2i(3, -3) &&
            new Vector2i(8, 9) / new Vector2i(2, 3) == new Vector2i(4, 3) &&
            new Vector2i(7, -7) % 3 == new Vector2i(1, -1) &&
            new Vector2i(7, 8) % new Vector2i(3, 5) == new Vector2i(1, 3),
        "Vector2i arithmetic must use componentwise integer rules.");
    Require(Vector2i.MaxValue + Vector2i.One == Vector2i.MinValue && -new Vector2i(int.MinValue, 0) == new Vector2i(int.MinValue, 0),
        "Vector2i ordinary overflow must wrap deterministically.");
    Expect<DivideByZeroException>(() => _ = value / 0, "Vector2i division must reject zero.");
    Require(float.IsPositiveInfinity((new Vector2i(1, 0) / 0f).X) &&
            float.IsNaN((new Vector2i(1, 0) / 0f).Y),
        "Vector2i floating-point division must retain IEEE zero-division behavior.");
    Expect<DivideByZeroException>(() => _ = value % new Vector2i(1, 0), "Vector2i remainder must reject zero components.");
    Expect<OverflowException>(() => _ = new Vector2i(int.MinValue, 0) / -1, "Vector2i division must surface minimum-integer overflow.");
    Expect<OverflowException>(() => _ = new Vector2i(int.MinValue, 0) % -1,
        "Vector2i remainder must surface minimum-integer overflow.");
    Require(new Vector2i(1, 2) < new Vector2i(1, 3) && new Vector2i(1, 2) <= new Vector2i(1, 2) &&
            new Vector2i(2, 0) > new Vector2i(1, 99) && new Vector2i(2, 0) >= new Vector2i(2, 0) &&
            value.Equals((object)new Vector2i(3, 4)) && value.GetHashCode() == new Vector2i(3, 4).GetHashCode(),
        "Vector2i equality, hashing, and ordering must be stable.");
    VerifyInvariantString(() => new Vector2i(12, -34).ToString("D3"), "(012, -034)", "Vector2i");
    Expect<FormatException>(() => _ = value.ToString("Q"), "Vector2i must reject invalid numeric formats.");

    var key = new ConfigKey<Vector2i>("math", "vector2i");
    using var config = new ConfigFile();
    config.SetValue(key, value);
    Require(config.GetValue(key) == value && config.EncodeToText() == "[math]\n\nvector2i={\"X\":3,\"Y\":4}\n",
        "ConfigFile must preserve the strict Vector2i schema.");
    config.Parse("[math]\nvector2i={\"X\":1.5,\"Y\":2}\n");
    Expect<InvalidDataException>(() => config.GetValue(key), "ConfigFile must reject non-integer Vector2i fields.");
}

static void VerifyVector3Values()
{
    VerifyVector3CoreValues();
    VerifyVector3ComponentMethods();
    VerifyVector3Geometry();
    Require(Marshal.SizeOf<Vector3>() == 12 && Marshal.SizeOf<Vector3i>() == 12 &&
            Vector3.Zero == default && Vector3i.Zero == default && Vector3.Right == new Vector3(1, 0, 0) &&
            Vector3i.Forward == new Vector3i(0, 0, -1), "Three-component values have sequential layouts and stable constants.");
    var value = new Vector3(3, 4, 12);
    var (x, y, z) = value;
    Require((x, y, z) == (3f, 4f, 12f) && value[2] == 12 && value.Length() == 13 &&
            value.LengthSquared() == 169 && value.Dot(Vector3.Up) == 4 &&
            Vector3.Right.Cross(Vector3.Up) == Vector3.Back &&
            Vector3.Right.Rotated(Vector3.Back, Mathf.Pi / 2).IsEqualApprox(Vector3.Up) &&
            Vector3.Right.AngleTo(Vector3.Up) == Mathf.Pi / 2 &&
            Vector3.Right.SignedAngleTo(Vector3.Up, Vector3.Back) == Mathf.Pi / 2 &&
            Vector3.Right.Slide(Vector3.Up) == Vector3.Right &&
            Vector3.Right.Project(Vector3.Up) == Vector3.Zero &&
            Vector3.Back.OctahedronEncode().IsEqualApprox(new Vector2(.5f, .5f)) &&
            Vector3.OctahedronDecode(new Vector2(.5f, .5f)).IsEqualApprox(Vector3.Back),
        "Three-component vector arithmetic, geometry and packing retain all axes.");
    Require(new Vector3(float.NaN, 1f, 2f).Normalized() == Vector3.Zero &&
            new Vector3(1f, float.NegativeInfinity, 2f).Normalized() == Vector3.Zero &&
            Vector3.Zero.DirectionTo(new Vector3(1f, 2f, float.PositiveInfinity)) == Vector3.Zero,
        "Vector3 normalization and direction must reject non-finite components.");
    var longVector = new Vector3(7f, 11f, 13f);
    Require(longVector.LimitLength(0.7f) == longVector / longVector.Length() * 0.7f &&
            longVector.LimitLength(100f) == longVector &&
            longVector.LimitLength(-0.7f) == longVector / longVector.Length() * -0.7f &&
            Vector3.Zero.LimitLength() == Vector3.Zero,
        "Vector3 length limiting follows source division-then-multiplication order and signed limits.");
    Require(Vector3.Zero.MoveToward(new Vector3(0.000005f, 0f, 0f), 0f) == new Vector3(0.000005f, 0f, 0f) &&
            Vector3.Zero.MoveToward(new Vector3(0.00002f, 0f, 0f), 0f) == Vector3.Zero &&
            Vector3.Zero.MoveToward(new Vector3(0.000005f, 0f, 0f), -1f) == new Vector3(0.000005f, 0f, 0f),
        "Vector3 MoveToward uses the source 1e-5 proximity threshold even for a negative step.");
    Require(Vector3.Forward.OctahedronEncode() == Vector2.One &&
            Vector3.Right.OctahedronEncode() == new Vector2(1f, 0.5f) &&
            Vector3.OctahedronDecode(new Vector2(2f, 0.5f)) == new Vector3(2f, -1f, -2f).Normalized() &&
            Vector3.OctahedronDecode(new Vector2(float.NaN, 0.5f)) == Vector3.Zero,
        "Octahedral packing preserves axis corners and clamps out-of-square decode correction.");
    var zeroPacked = Vector3.Zero.OctahedronEncode();
    Require(float.IsNaN(zeroPacked.X) && float.IsNaN(zeroPacked.Y),
        "Octahedral encoding of the zero vector retains the source's undefined numeric result.");
    Require(Vector3.One.MinAxisIndex() == Vector3.Axis.Z && Vector3.One.MaxAxisIndex() == Vector3.Axis.X &&
            new Vector3i(1, 2, 3) * new Vector3i(2, 3, 4) == new Vector3i(2, 6, 12) &&
            -new Vector3i(1, 2, 3) == new Vector3i(-1, -2, -3) &&
            new Vector3i(3, 4, 12).LengthSquared() == 169 &&
            new Vector3i(1, 2, 3).DistanceSquaredTo(new Vector3i(4, 6, 6)) == 34 &&
            (Vector3i)new Vector3(1.9f, -2.9f, 3.9f) == new Vector3i(1, -2, 3) &&
            new Vector3(new Vector3i(1, 2, 3)) == new Vector3(1, 2, 3),
        "Integer arithmetic, norms, ties and typed conversions retain all axes.");
    Require(float.IsFinite(Vector3i.MinValue.DistanceTo(Vector3i.MaxValue)) &&
            float.IsFinite(Vector3i.MinValue.Length()), "Full-range integer distances remain finite.");
    Expect<OverflowException>(() => _ = Vector3i.MinValue.DistanceSquaredTo(Vector3i.MaxValue), "Squared integer distances reject overflow.");
    Expect<OverflowException>(() => _ = Vector3i.MinValue.LengthSquared(), "Squared integer lengths reject overflow.");
    Expect<ArgumentOutOfRangeException>(() => _ = value[3], "A fourth vector component does not exist.");
    Expect<ArgumentOutOfRangeException>(() => _ = (Vector3i)new Vector3(0, 0, float.NaN), "Nonfinite integer conversion is rejected.");
    using var config = new ConfigFile();
    var floatKey = new ConfigKey<Vector3>("math", "triple");
    var intKey = new ConfigKey<Vector3i>("math", "triplei");
    config.SetValue(floatKey, value); config.SetValue(intKey, new Vector3i(1, 2, 3));
    Require(config.GetValue(floatKey) == value && config.GetValue(intKey) == new Vector3i(1, 2, 3) &&
            config.EncodeToText().Contains("triple={\"X\":3,\"Y\":4,\"Z\":12}", StringComparison.Ordinal),
        "Configuration stores exact three-component schemas.");
    Expect<JsonException>(() => config.SetValue(floatKey, Vector3.Inf), "Configuration rejects nonfinite triples.");
    config.Parse("[math]\ntriple={\"X\":1,\"Y\":2,\"Z\":3,\"W\":4}\n");
    Expect<InvalidDataException>(() => config.GetValue(floatKey), "Configuration rejects a fourth field.");
}

static void VerifyVector3iValues()
{
    Require(Marshal.SizeOf<Vector3i>() == 12 && Vector3i.Zero == default &&
            Vector3i.One == new Vector3i(1, 1, 1) &&
            Vector3i.MinValue == new Vector3i(int.MinValue, int.MinValue, int.MinValue) &&
            Vector3i.MaxValue == new Vector3i(int.MaxValue, int.MaxValue, int.MaxValue) &&
            Vector3i.Right == new Vector3i(1, 0, 0) && Vector3i.Left == new Vector3i(-1, 0, 0) &&
            Vector3i.Up == new Vector3i(0, 1, 0) && Vector3i.Down == new Vector3i(0, -1, 0) &&
            Vector3i.Forward == new Vector3i(0, 0, -1) && Vector3i.Back == new Vector3i(0, 0, 1) &&
            (int)Vector3i.Axis.X == 0 && (int)Vector3i.Axis.Y == 1 && (int)Vector3i.Axis.Z == 2,
        "Vector3i layout, constants and axis identities retain the three-component contract.");
    var value = new Vector3i(3, 4, 12);
    var copy = value;
    copy[0] = -7;
    copy[1] = 9;
    copy[2] = 5;
    var (x, y, z) = value;
    Require(value == new Vector3i(3, 4, 12) && copy == new Vector3i(-7, 9, 5) &&
            (x, y, z) == (3, 4, 12) && value[0] == 3 && value[1] == 4 && value[2] == 12,
        "Vector3i indexed mutation does not alias value copies or reorder components.");
    Expect<ArgumentOutOfRangeException>(() => _ = value[-1], "A negative Vector3i index fails explicitly.");
    Expect<ArgumentOutOfRangeException>(() => copy[3] = 1, "A fourth Vector3i component cannot be assigned.");
    Require(copy == new Vector3i(-7, 9, 5), "A rejected Vector3i index write leaves state intact.");

    Require(new Vector3i(new Vector3(1.9f, -2.9f, 3.9f)) == new Vector3i(1, -2, 3) &&
            (Vector3i)new Vector3((float)int.MinValue, 0f, 0f) ==
                new Vector3i(int.MinValue, 0, 0) &&
            (Vector3)new Vector3i(16_777_217, int.MinValue, int.MaxValue) ==
                new Vector3(16_777_216f, int.MinValue, 2_147_483_648f),
        "Vector3i floating conversions truncate finite components and expose float precision loss.");
    Expect<ArgumentOutOfRangeException>(() => _ = (Vector3i)new Vector3(float.NaN, 0f, 0f),
        "Vector3i conversion rejects NaN before casting.");
    Expect<ArgumentOutOfRangeException>(() => _ = (Vector3i)new Vector3(0f, float.PositiveInfinity, 0f),
        "Vector3i conversion rejects infinity before casting.");
    Expect<ArgumentOutOfRangeException>(() => _ = (Vector3i)new Vector3(0f, 0f, (float)int.MaxValue),
        "Vector3i conversion rejects the rounded-up Int32 maximum.");

    Require(value.LengthSquared() == 169L && value.Length() == 13f &&
            value.DistanceSquaredTo(Vector3i.Zero) == 169L && value.DistanceTo(Vector3i.Zero) == 13f &&
            new Vector3i(int.MaxValue, int.MaxValue, 0).LengthSquared() ==
                2L * int.MaxValue * int.MaxValue &&
            new Vector3i(-1_500_000_000, 0, 0).DistanceSquaredTo(
                new Vector3i(1_500_000_000, 0, 0)) == 9_000_000_000_000_000_000L &&
            float.IsFinite(Vector3i.MinValue.Length()) &&
            float.IsFinite(Vector3i.MinValue.DistanceTo(Vector3i.MaxValue)),
        "Vector3i norms widen before multiplication/subtraction and ordinary distances stay finite.");
    Expect<OverflowException>(() => _ = Vector3i.MinValue.LengthSquared(),
        "Three minimum components exceed the exact Int64 squared-length limit.");
    Expect<OverflowException>(() => _ = Vector3i.MinValue.DistanceSquaredTo(Vector3i.MaxValue),
        "Full-span three-axis squared distance rejects Int64 overflow.");

    Require(new Vector3i(-3, 4, -5).Abs() == new Vector3i(3, 4, 5) &&
            new Vector3i(int.MinValue, 0, int.MaxValue).Sign() == new Vector3i(-1, 0, 1) &&
            new Vector3i(5, -2, 8).Clamp(new Vector3i(1, -1, 2), new Vector3i(4, 3, 7)) ==
                new Vector3i(4, -1, 7) &&
            new Vector3i(5, -2, 8).Clamp(0, 4) == new Vector3i(4, 0, 4) &&
            new Vector3i(1, 5, -1).Max(new Vector3i(3, 2, 0)) == new Vector3i(3, 5, 0) &&
            new Vector3i(1, 5, -1).Min(2) == new Vector3i(1, 2, -1) &&
            new Vector3i(1, 5, -1).Max(2) == new Vector3i(2, 5, 2) &&
            new Vector3i(1, 5, -1).Min(new Vector3i(3, 2, 0)) == new Vector3i(1, 2, -1),
        "Vector3i componentwise absolute/sign/clamp/min/max preserve every axis.");
    Expect<OverflowException>(() => _ = Vector3i.MinValue.Abs(), "Int32 minimum absolute value fails explicitly.");
    Expect<ArgumentException>(() => _ = value.Clamp(2, 1), "Reversed scalar clamp bounds fail explicitly.");
    Expect<ArgumentException>(() => _ = value.Clamp(new Vector3i(0, 5, 0), new Vector3i(9, 4, 20)),
        "Reversed bounds on one vector axis fail explicitly.");
    Require(Vector3i.One.MinAxisIndex() == Vector3i.Axis.Z &&
            Vector3i.One.MaxAxisIndex() == Vector3i.Axis.X &&
            new Vector3i(1, 2, 2).MaxAxisIndex() == Vector3i.Axis.Y &&
            new Vector3i(2, 1, 1).MinAxisIndex() == Vector3i.Axis.Z,
        "Vector3i axis ties match the pinned first-maximum and last-minimum rules.");

    Require(new Vector3i(5, -5, 7).Snapped(2) == new Vector3i(6, -4, 8) &&
            new Vector3i(5, -5, 7).Snapped(new Vector3i(2, 0, 3)) ==
                new Vector3i(6, -5, 6) &&
            new Vector3i(5, -5, 7).Snapped(-2) == new Vector3i(4, -6, 6) &&
            value.Snapped(0) == value,
        "Vector3i snapping respects midpoint, negative and independent zero steps.");
    Expect<OverflowException>(() => _ = Vector3i.MaxValue.Snapped(2),
        "Vector3i snapping rejects an Int32-overflowing rounded result.");

    Require(value + Vector3i.One == new Vector3i(4, 5, 13) &&
            value - Vector3i.One == new Vector3i(2, 3, 11) &&
            +value == value && -value == new Vector3i(-3, -4, -12) &&
            value * new Vector3i(2, 3, 4) == new Vector3i(6, 12, 48) &&
            value * 2 == 2 * value && value * .5f == .5f * value &&
            value / 2f == new Vector3(1.5f, 2f, 6f) &&
            new Vector3i(7, -7, 9) / 2 == new Vector3i(3, -3, 4) &&
            new Vector3i(8, 9, -10) / new Vector3i(2, 3, -2) == new Vector3i(4, 3, 5) &&
            new Vector3i(7, -7, 9) % 3 == new Vector3i(1, -1, 0) &&
            new Vector3i(7, 8, -9) % new Vector3i(3, 5, 4) == new Vector3i(1, 3, -1),
        "Vector3i arithmetic uses wrapping components and truncated integer division/remainder.");
    Require(Vector3i.MaxValue + Vector3i.One == Vector3i.MinValue &&
            -new Vector3i(int.MinValue, 0, 0) == new Vector3i(int.MinValue, 0, 0) &&
            new Vector3i(int.MaxValue, 0, 0) * 2 == new Vector3i(-2, 0, 0) &&
            float.IsPositiveInfinity((new Vector3i(1, -1, 0) / 0f).X) &&
            float.IsNegativeInfinity((new Vector3i(1, -1, 0) / 0f).Y) &&
            float.IsNaN((new Vector3i(1, -1, 0) / 0f).Z),
        "Ordinary Int32 overflow wraps while floating division by zero follows IEEE behavior.");
    Expect<DivideByZeroException>(() => _ = value / 0, "Integer scalar division rejects zero.");
    Expect<DivideByZeroException>(() => _ = value % new Vector3i(1, 1, 0),
        "Integer component remainder rejects zero.");
    Expect<OverflowException>(() => _ = new Vector3i(int.MinValue, 0, 0) / -1,
        "Int32 minimum division by -1 surfaces managed overflow.");
    Expect<OverflowException>(() => _ = new Vector3i(int.MinValue, 0, 0) % -1,
        "Int32 minimum remainder by -1 surfaces managed overflow.");

    var same = new Vector3i(3, 4, 12);
    Require(value == same && !(value != same) &&
            new Vector3i(1, 2, 3) < new Vector3i(1, 2, 4) &&
            new Vector3i(1, 2, 3) <= new Vector3i(1, 2, 3) &&
            new Vector3i(2, -100, 0) > new Vector3i(1, 100, 100) &&
            new Vector3i(2, 0, 0) >= new Vector3i(2, 0, 0) &&
            value.Equals((object)same) && value.GetHashCode() == same.GetHashCode(),
        "Vector3i equality, hashing and lexicographic ordering retain X/Y/Z precedence.");
    VerifyInvariantString(() => new Vector3i(12, -34, 5).ToString("D3"), "(012, -034, 005)", "Vector3i");
    Expect<FormatException>(() => _ = value.ToString("Q"), "Vector3i rejects invalid numeric formats.");
    using var config = new ConfigFile();
    var key = new ConfigKey<Vector3i>("math", "vector3i");
    config.SetValue(key, value);
    Require(config.GetValue(key) == value &&
            config.EncodeToText() == "[math]\n\nvector3i={\"X\":3,\"Y\":4,\"Z\":12}\n",
        "ConfigFile retains the exact three-component integer schema.");
    config.Parse("[math]\nvector3i={\"X\":1.5,\"Y\":2,\"Z\":3}\n");
    Expect<InvalidDataException>(() => config.GetValue(key),
        "ConfigFile rejects a fractional integer-vector component.");
}

static void VerifyVector3CoreValues()
{
    Require(Vector3.Zero == default && Vector3.One == new Vector3(1f, 1f, 1f) &&
            Vector3.Right == new Vector3(1f, 0f, 0f) && Vector3.Left == new Vector3(-1f, 0f, 0f) &&
            Vector3.Up == new Vector3(0f, 1f, 0f) && Vector3.Down == new Vector3(0f, -1f, 0f) &&
            Vector3.Forward == new Vector3(0f, 0f, -1f) && Vector3.Back == new Vector3(0f, 0f, 1f) &&
            float.IsPositiveInfinity(Vector3.Inf.X) && float.IsPositiveInfinity(Vector3.Inf.Y) &&
            float.IsPositiveInfinity(Vector3.Inf.Z) &&
            (int)Vector3.Axis.X == 0 && (int)Vector3.Axis.Y == 1 && (int)Vector3.Axis.Z == 2,
        "Vector3 constants and axis identities retain the pinned component values.");

    var value = new Vector3(1.5f, -2f, 0f);
    var copy = value;
    copy[0] = -3f;
    copy[1] = 4f;
    copy[2] = .5f;
    Require(value == new Vector3(1.5f, -2f, 0f) && copy == new Vector3(-3f, 4f, .5f) &&
            copy.X == -3f && copy.Y == 4f && copy.Z == .5f &&
            new Vector3(new Vector3i(16_777_217, int.MinValue, int.MaxValue)) ==
                new Vector3(16_777_216f, int.MinValue, 2_147_483_648f) &&
            (Vector3)new Vector3i(-2, 3, 4) == new Vector3(-2f, 3f, 4f),
        "Value copies, mutable indexes and integer-to-float conversion retain component order and rounding.");
    Expect<ArgumentOutOfRangeException>(() => _ = copy[-1], "Negative Vector3 indexes fail explicitly.");
    Expect<ArgumentOutOfRangeException>(() => copy[3] = 1f, "A fourth Vector3 component cannot be assigned.");
    Require(copy == new Vector3(-3f, 4f, .5f), "A rejected index assignment leaves every component intact.");

    Require(value + copy == new Vector3(-1.5f, 2f, .5f) &&
            value - copy == new Vector3(4.5f, -6f, -.5f) &&
            value * copy == new Vector3(-4.5f, -8f, 0f) &&
            value / copy == new Vector3(-.5f, -.5f, 0f) &&
            value * -2f == new Vector3(-3f, 4f, 0f) &&
            -2f * value == value * -2f && value * -2 == value * -2f &&
            value * int.MaxValue == value * (float)int.MaxValue &&
            value / 2 == new Vector3(.75f, -1f, 0f) &&
            value / int.MaxValue == value / (float)int.MaxValue &&
            +value == value && -value == new Vector3(-1.5f, 2f, 0f),
        "Vector3 arithmetic and integer scalar conversion match componentwise float operations.");
    var dividedByZero = value / 0f;
    var dividedByZeroComponents = value / Vector3.Zero;
    Require(float.IsPositiveInfinity(dividedByZero.X) && float.IsNegativeInfinity(dividedByZero.Y) &&
            float.IsNaN(dividedByZero.Z) &&
            float.IsPositiveInfinity(dividedByZeroComponents.X) &&
            float.IsNegativeInfinity(dividedByZeroComponents.Y) &&
            float.IsNaN(dividedByZeroComponents.Z) &&
            BitConverter.SingleToInt32Bits((-Vector3.Zero).X) == int.MinValue,
        "Zero division preserves IEEE infinities/NaN and unary negation preserves signed zero.");

    var low = new Vector3(1f, 2f, 3f);
    var highZ = new Vector3(1f, 2f, 4f);
    var highY = new Vector3(1f, 3f, -100f);
    var highX = new Vector3(2f, -100f, -100f);
    var notOrdered = new Vector3(float.NaN, 2f, 3f);
    var lowCopy = low;
    var unorderedCopy = notOrdered;
    Require(low < highZ && low < highY && low < highX && highX > highY &&
            low <= lowCopy && low >= lowCopy && low <= highZ && highZ >= low &&
            low == new Vector3(1f, 2f, 3f) && low != highZ &&
            new Vector3(-0f, 0f, 0f) == Vector3.Zero &&
            notOrdered != unorderedCopy && !(notOrdered == unorderedCopy) &&
            !(notOrdered < low) && !(notOrdered > low) &&
            !(notOrdered <= low) && !(notOrdered >= low),
        "Vector3 comparisons use X/Y/Z lexicographic ordering and IEEE NaN/zero equality.");
}

static void VerifyVector3ComponentMethods()
{
    var signed = new Vector3(-0f, -2f, float.NaN).Abs();
    Require(BitConverter.SingleToInt32Bits(signed.X) == 0 && signed.Y == 2f && float.IsNaN(signed.Z) &&
            new Vector3(-1.5f, 1.5f, 2.5f).Floor() == new Vector3(-2f, 1f, 2f) &&
            new Vector3(-1.5f, 1.5f, 2.5f).Ceil() == new Vector3(-1f, 2f, 3f) &&
            new Vector3(-1.5f, 1.5f, 2.5f).Round() == new Vector3(-2f, 2f, 2f) &&
            new Vector3(-2f, 0f, 3f).Sign() == new Vector3(-1f, 0f, 1f),
        "Componentwise absolute, floor, ceiling, midpoint-to-even rounding and sign retain scalar behavior.");
    Expect<ArithmeticException>(() => new Vector3(1f, float.NaN, 0f).Sign(),
        "Sign follows the canonical scalar NaN error contract.");

    var value = new Vector3(-2f, .5f, 5f);
    Require(value.Clamp(new Vector3(-1f, 0f, 1f), new Vector3(1f, 1f, 4f)) ==
                new Vector3(-1f, .5f, 4f) &&
            value.Clamp(0f, 2f) == new Vector3(0f, .5f, 2f) &&
            value.Max(new Vector3(0f, -1f, 4f)) == new Vector3(0f, .5f, 5f) &&
            value.Min(new Vector3(0f, -1f, 4f)) == new Vector3(-2f, -1f, 4f) &&
            value.Max(2f) == new Vector3(2f, 2f, 5f) &&
            value.Min(2f) == new Vector3(-2f, .5f, 2f) &&
            float.IsNaN(new Vector3(float.NaN, 0f, 0f).Max(Vector3.One).X),
        "Vector and scalar clamp/min/max act independently on X, Y and Z, including scalar NaN policy.");
    Expect<ArgumentException>(() => value.Clamp(2f, 1f), "Reversed scalar clamp bounds fail explicitly.");
    Expect<ArgumentException>(() => value.Clamp(new Vector3(0f, 2f, 0f), Vector3.One),
        "A reversed bound on any vector component fails explicitly.");

    Require(Vector3.One.MinAxisIndex() == Vector3.Axis.Z &&
            Vector3.One.MaxAxisIndex() == Vector3.Axis.X &&
            new Vector3(0f, 2f, 2f).MaxAxisIndex() == Vector3.Axis.Y &&
            new Vector3(float.NaN, 1f, 2f).MinAxisIndex() == Vector3.Axis.Y &&
            new Vector3(1f, float.NaN, 2f).MinAxisIndex() == Vector3.Axis.Z &&
            new Vector3(1f, 2f, float.NaN).MinAxisIndex() == Vector3.Axis.Z &&
            new Vector3(float.NaN, 1f, 2f).MaxAxisIndex() == Vector3.Axis.X &&
            new Vector3(1f, float.NaN, 2f).MaxAxisIndex() == Vector3.Axis.Z,
        "Axis ties and unordered NaN comparisons follow the pinned X/Y/Z branch order.");

    var reciprocal = new Vector3(2f, -4f, -0f).Inverse();
    Require(reciprocal.X == .5f && reciprocal.Y == -.25f &&
            float.IsNegativeInfinity(reciprocal.Z) &&
            Vector3.One.IsFinite() && !new Vector3(0f, float.NaN, 0f).IsFinite() &&
            !new Vector3(0f, 0f, float.PositiveInfinity).IsFinite() &&
            Vector3.Right.IsNormalized() &&
            new Vector3(MathF.Sqrt(1.0005f), 0f, 0f).IsNormalized() &&
            !new Vector3(MathF.Sqrt(1.002f), 0f, 0f).IsNormalized(),
        "Reciprocal, finite and unit predicates preserve IEEE and the separate unit tolerance.");
    Require(new Vector3(.5e-6f, 0f, -.5e-6f).IsZeroApprox() &&
            !new Vector3(1e-6f, 0f, 0f).IsZeroApprox() &&
            Vector3.One.IsEqualApprox(new Vector3(1f + .5e-6f, 1f, 1f)) &&
            !Vector3.One.IsEqualApprox(new Vector3(1f + 2e-6f, 1f, 1f)) &&
            Vector3.Inf.IsEqualApprox(Vector3.Inf) &&
            !new Vector3(float.NaN, 0f, 0f).IsEqualApprox(new Vector3(float.NaN, 0f, 0f)),
        "Approximate vector predicates use canonical strict component tolerance and exact infinity identity.");

    Require(new Vector3(-1f, 7f, -7f).PosMod(4f) == new Vector3(3f, 3f, 1f) &&
            new Vector3(-1f, 7f, -7f).PosMod(new Vector3(4f, -4f, 3f)) ==
                new Vector3(3f, -1f, 2f) &&
            float.IsNaN(Vector3.One.PosMod(0f).X) &&
            new Vector3(1.25f, -1.25f, 2.6f).Snapped(.5f) == new Vector3(1.5f, -1f, 2.5f) &&
            new Vector3(1.25f, -1.25f, 2.6f).Snapped(new Vector3(.5f, 0f, 2f)) ==
                new Vector3(1.5f, -1.25f, 2f),
        "Positive modulus and scalar/vector snapping honor signed divisors, zero and midpoint rules.");
    var unchanged = new Vector3(float.NaN, 1f, -0f).Snapped(0f);
    Require(float.IsNaN(unchanged.X) && unchanged.Y == 1f &&
            BitConverter.SingleToInt32Bits(unchanged.Z) == int.MinValue,
        "Zero-step snapping retains source NaN and signed-zero components.");
}

static void VerifyVector3Geometry()
{
    var value = new Vector3(3f, 4f, 12f);
    Require(value.LengthSquared() == 169f && value.Length() == 13f &&
            value.DistanceSquaredTo(Vector3.Zero) == 169f && value.DistanceTo(Vector3.Zero) == 13f &&
            value.Dot(new Vector3(-2f, 1f, .5f)) == 4f &&
            Vector3.Right.Cross(Vector3.Up) == Vector3.Back &&
            Vector3.Up.Cross(Vector3.Right) == Vector3.Forward &&
            Vector3.Zero.Cross(value) == Vector3.Zero &&
            float.IsPositiveInfinity(new Vector3(float.MaxValue, 0f, 0f).LengthSquared()) &&
            float.IsPositiveInfinity(new Vector3(float.MaxValue, 0f, 0f).DistanceTo(Vector3.Zero)),
        "Vector3 norms, distances, dot and cross products retain order and IEEE overflow.");

    Require(Vector3.Right.AngleTo(Vector3.Up) == Mathf.Pi / 2f &&
            Vector3.Right.SignedAngleTo(Vector3.Up, Vector3.Back) == Mathf.Pi / 2f &&
            Vector3.Right.SignedAngleTo(Vector3.Up, Vector3.Forward) == -Mathf.Pi / 2f &&
            Vector3.Right.SignedAngleTo(Vector3.Up, Vector3.Zero) == Mathf.Pi / 2f &&
            Vector3.Zero.AngleTo(Vector3.Zero) == 0f &&
            Vector3.Zero.Lerp(new Vector3(4f, 8f, 12f), .25f) == new Vector3(1f, 2f, 3f) &&
            Vector3.Zero.Lerp(Vector3.One, -1f) == -Vector3.One,
        "Unsigned/signed angle and linear interpolation preserve orientation, zero and extrapolation.");

    var start = new Vector3(0f, 10f, -2f);
    var control1 = Vector3.Zero;
    var control2 = new Vector3(10f, 10f, 4f);
    var end = new Vector3(10f, 0f, 2f);
    Require(start.BezierInterpolate(control1, control2, end, 0f) == start &&
            start.BezierInterpolate(control1, control2, end, 1f) == end &&
            start.BezierInterpolate(control1, control2, end, .5f) == new Vector3(5f, 5f, 1.5f) &&
            start.BezierDerivative(control1, control2, end, .5f) == new Vector3(15f, 0f, 6f),
        "Bezier interpolation and derivative retain all three independent control coordinates.");

    var pre = new Vector3(-1f, 0f, 0f);
    var a = new Vector3(0f, 1f, 0f);
    var b = new Vector3(1f, 2f, 0f);
    var post = new Vector3(2f, 3f, 0f);
    Require(a.CubicInterpolate(b, pre, post, .5f) == new Vector3(.5f, 1.5f, 0f) &&
            a.CubicInterpolateInTime(b, pre, post, .5f, 1f, -1f, 2f) == new Vector3(.5f, 1.5f, 0f) &&
            a.CubicInterpolateInTime(b, pre, post, .5f, 0f, 0f, 0f).IsFinite(),
        "Catmull-Rom and time-aware cubic interpolation handle uniform and degenerate timestamps.");

    var normal = new Vector3(.6f, .8f, 0f);
    var extreme = new Vector3(float.MaxValue, 0f, 0f);
    var reflected = extreme.Reflect(normal);
    Require(reflected.IsFinite() && reflected.X < 0f && reflected.Y > 0f && reflected.Z == 0f &&
            extreme.Bounce(normal) == -reflected &&
            new Vector3(3f, 4f, 5f).Slide(Vector3.Up) == new Vector3(3f, 0f, 5f) &&
            new Vector3(3f, 4f, 5f).Project(new Vector3(0f, 2f, 0f)) == new Vector3(0f, 4f, 0f),
        "Reflection multiplies the normal before the dot scalar; bounce, slide and nonunit projection follow it.");
    var undefinedProjection = Vector3.One.Project(Vector3.Zero);
    Require(float.IsNaN(undefinedProjection.X) && float.IsNaN(undefinedProjection.Y) &&
            float.IsNaN(undefinedProjection.Z),
        "Projection onto zero retains the source's IEEE undefined result.");
    var reflected2 = new Vector2(float.MaxValue, 0f).Reflect(new Vector2(.6f, .8f));
    Require(reflected2.IsFinite() && reflected2.X < 0f && reflected2.Y > 0f &&
            new Vector2(float.MaxValue, 0f).Bounce(new Vector2(.6f, .8f)) == -reflected2,
        "The Vector2 sibling preserves the same source multiplication order at extreme magnitudes.");

    Require(Vector3.Right.Rotated(Vector3.Back, Mathf.Pi / 2f).IsEqualApprox(Vector3.Up) &&
            Vector3.Right.Rotated(new Vector3(0f, 0f, 2f), Mathf.Pi / 2f).IsEqualApprox(new Vector3(0f, 2f, 0f)) &&
            Vector3.Right.Slerp(Vector3.Up, .5f).IsEqualApprox(
                new Vector3(MathF.Sqrt(.5f), MathF.Sqrt(.5f), 0f)) &&
            new Vector3(2f, 0f, 0f).Slerp(new Vector3(0f, 4f, 0f), .5f).IsEqualApprox(
                new Vector3(3f * MathF.Sqrt(.5f), 3f * MathF.Sqrt(.5f), 0f)) &&
            Vector3.Zero.Slerp(new Vector3(4f, 0f, 0f), .25f) == Vector3.Right &&
            Vector3.Right.Slerp(Vector3.Left, .5f) == Vector3.Zero,
        "Rotation and spherical interpolation preserve axis orientation, length, zero and antiparallel fallbacks.");
    if (OperatingSystem.IsLinux())
        Require(new Vector3(1.0618035f, -.669906f, 2.0769434f).Rotated(
                new Vector3(-.41317213f, -.8195237f, -.3970765f), .6213446f) ==
                new Vector3(-.22717518f, -.18116105f, 2.4094536f),
            "The pinned row-by-row rotation order must retain Linux float rounding.");
}

static void VerifyVector4Values()
{
    VerifyVector4RemainingValues();
    Require(Marshal.SizeOf<Vector4>() == 16 && typeof(Vector4).IsDefined(typeof(SerializableAttribute), false) &&
            Vector4.Zero == default && Vector4.One == new Vector4(1f, 1f, 1f, 1f) && float.IsPositiveInfinity(Vector4.Inf.W),
        "Vector4 layout and constants must be stable.");
    var value = new Vector4(1f, 2f, 3f, 4f);
    var (x, y, z, w) = value;
    Require(value[0] == 1f && value[3] == 4f && (x, y, z, w) == (1f, 2f, 3f, 4f) &&
            (int)Vector4.Axis.W == 3,
        "Vector4 indexing, axes, and deconstruction must preserve component order.");
    Expect<ArgumentOutOfRangeException>(() => _ = value[4], "Vector4 must reject indices after W.");
    Require(value.LengthSquared() == 30f && NearlyEqual(value.Length(), System.MathF.Sqrt(30f)) && value.Normalized().IsNormalized() &&
            Vector4.Zero.Normalized() == Vector4.Zero && value.Dot(Vector4.One) == 10f &&
            Vector4.Zero.DirectionTo(Vector4.Zero) == Vector4.Zero && Vector4.Zero.DirectionTo(Vector4.One).IsNormalized() &&
            Vector4.Zero.DistanceSquaredTo(value) == 30f && NearlyEqual(Vector4.Zero.DistanceTo(value), System.MathF.Sqrt(30f)),
        "Vector4 length, normalization, dot, direction, and distance operations must be stable.");
    Require(new Vector4(float.NaN, 1f, 2f, 3f).Normalized() == Vector4.Zero &&
            new Vector4(1f, 2f, float.PositiveInfinity, 3f).Normalized() == Vector4.Zero &&
            Vector4.Zero.DirectionTo(new Vector4(1f, 2f, 3f, float.NegativeInfinity)) == Vector4.Zero,
        "Vector4 normalization and direction must reject non-finite components.");
    Require(new Vector4(-1.2f, 2.2f, -3.2f, 4.2f).Abs() == new Vector4(1.2f, 2.2f, 3.2f, 4.2f) &&
            new Vector4(1.2f, -2.2f, 3.2f, -4.2f).Ceil() == new Vector4(2f, -2f, 4f, -4f) &&
            new Vector4(1.8f, -2.2f, 3.8f, -4.2f).Floor() == new Vector4(1f, -3f, 3f, -5f) &&
            new Vector4(1.5f, 2.5f, -1.5f, -2.5f).Round() == new Vector4(2f, 2f, -2f, -2f) &&
            new Vector4(-2f, 0f, 3f, -4f).Sign() == new Vector4(-1f, 0f, 1f, -1f),
        "Vector4 rounding, sign, and absolute methods must be componentwise.");
    Expect<ArithmeticException>(() => new Vector4(0f, 0f, float.NaN, 0f).Sign(), "Vector4 Sign must reject NaN.");
    Require(value.Clamp(2f, 3f) == new Vector4(2f, 2f, 3f, 3f) &&
            value.Clamp(new Vector4(0f, 0f, 4f, 0f), new Vector4(2f, 3f, 5f, 3f)) == new Vector4(1f, 2f, 4f, 3f),
        "Vector4 clamp overloads must be componentwise.");
    Expect<ArgumentException>(() => value.Clamp(2f, 1f), "Vector4 must reject reversed clamp bounds.");
    Require(Vector4.Zero.Lerp(new Vector4(2f, 4f, 6f, 8f), 0.5f) == value &&
            value.Max(2.5f) == new Vector4(2.5f, 2.5f, 3f, 4f) &&
            value.Max(new Vector4(0f, 3f, 2f, 5f)) == new Vector4(1f, 3f, 3f, 5f) &&
            value.Min(2.5f) == new Vector4(1f, 2f, 2.5f, 2.5f) &&
            value.Min(new Vector4(0f, 3f, 2f, 5f)) == new Vector4(0f, 2f, 2f, 4f) &&
            Vector4.One.MaxAxisIndex() == Vector4.Axis.X && Vector4.One.MinAxisIndex() == Vector4.Axis.W,
        "Vector4 interpolation, extrema, and axis tie-breaking must be stable.");
    Require(new Vector4(-1f, 7f, -5f, 9f).PosMod(4f) == new Vector4(3f, 3f, 3f, 1f) &&
            new Vector4(-1f, 7f, -5f, 9f).PosMod(new Vector4(4f, 3f, 2f, 5f)) == new Vector4(3f, 1f, 1f, 4f) &&
            new Vector4(5.1f, -5.1f, 3.1f, -3.1f).Snapped(2f) == new Vector4(6f, -6f, 4f, -4f) &&
            new Vector4(5.1f, -5.1f, 3.1f, -3.1f).Snapped(new Vector4(2f, 5f, 2f, 3f)) == new Vector4(6f, -5f, 4f, -3f),
        "Vector4 positive modulus and snapping must handle signed values.");
    Require(value.Inverse() == new Vector4(1f, 0.5f, 1f / 3f, 0.25f) && value.IsFinite() &&
            !new Vector4(float.NaN, 0f, 0f, 0f).IsFinite() &&
            new Vector4(Mathf.Epsilon * 0.5f, 0f, 0f, 0f).IsZeroApprox() &&
            value.IsEqualApprox(new Vector4(1.000001f, 2f, 3f, 4f)),
        "Vector4 inverse and numeric predicates must be stable.");
    Require(Vector4.Zero.CubicInterpolate(new Vector4(2f, 2f, 2f, 2f), new Vector4(-2f, -2f, -2f, -2f), new Vector4(4f, 4f, 4f, 4f), 0.5f)
                .IsEqualApprox(Vector4.One) &&
            Vector4.Zero.CubicInterpolateInTime(new Vector4(2f, 2f, 2f, 2f), new Vector4(-2f, -2f, -2f, -2f), new Vector4(4f, 4f, 4f, 4f), 0.5f, 1f, -1f, 2f)
                .IsEqualApprox(Vector4.One),
        "Vector4 cubic interpolation methods must preserve symmetric fixtures.");
    Require(value + Vector4.One == new Vector4(2f, 3f, 4f, 5f) && +value == value && value - Vector4.One == new Vector4(0f, 1f, 2f, 3f) &&
            -value == new Vector4(-1f, -2f, -3f, -4f) && value * 2f == 2f * value &&
            value * 2 == new Vector4(2f, 4f, 6f, 8f) && value / 2 == new Vector4(0.5f, 1f, 1.5f, 2f) &&
            value * Vector4.One == value && value / 2f == new Vector4(0.5f, 1f, 1.5f, 2f) &&
            value / value == Vector4.One && new Vector4(5f, -5f, 8f, -8f) % 3f == new Vector4(2f, -2f, 2f, -2f) &&
            new Vector4(5f, 8f, 9f, 10f) % new Vector4(3f, 5f, 4f, 6f) == new Vector4(2f, 3f, 1f, 4f),
        "Vector4 arithmetic operators must be componentwise.");
    Require(new Vector4(1f, 2f, 3f, 4f) < new Vector4(1f, 2f, 3f, 5f) && value <= new Vector4(1f, 2f, 3f, 4f) &&
            new Vector4(2f, 0f, 0f, 0f) > value && value >= new Vector4(1f, 2f, 3f, 4f) &&
            value.Equals((object)new Vector4(1f, 2f, 3f, 4f)) && value.GetHashCode() == new Vector4(1f, 2f, 3f, 4f).GetHashCode(),
        "Vector4 equality, hashing, and lexicographic ordering must be stable.");
    var nan = new Vector4(float.NaN, 0f, 0f, 0f);
    Require(!(nan < Vector4.Zero) && !(nan <= Vector4.Zero) && !(nan > Vector4.Zero) && !(nan >= Vector4.Zero),
        "Vector4 relational operators must preserve unordered NaN comparisons.");
    var integer = new Vector4i(1, -2, 3, -4);
    Require(new Vector4(integer) == new Vector4(1f, -2f, 3f, -4f) && (Vector4)integer == new Vector4(1f, -2f, 3f, -4f) &&
            (Vector4i)new Vector4(1.9f, -2.9f, 3.9f, -4.9f) == integer,
        "Vector4 conversions must widen implicitly and truncate explicitly.");
    VerifyInvariantString(() => new Vector4(1.5f, 2.5f, 3.5f, 4.5f).ToString("F1"), "(1.5, 2.5, 3.5, 4.5)", "Vector4");
    Expect<FormatException>(() => _ = value.ToString("Q"), "Vector4 must reject invalid numeric formats.");

    var key = new ConfigKey<Vector4>("math", "vector4");
    using var config = new ConfigFile();
    config.SetValue(key, value);
    Require(config.GetValue(key) == value && config.EncodeToText() == "[math]\n\nvector4={\"X\":1,\"Y\":2,\"Z\":3,\"W\":4}\n",
        "ConfigFile must preserve the strict Vector4 schema.");
    Expect<JsonException>(() => config.SetValue(key, new Vector4(0f, 0f, 0f, float.PositiveInfinity)),
        "ConfigFile must reject non-finite Vector4 values.");
    config.Parse("[math]\nvector4={\"X\":1,\"Y\":2,\"Z\":3}\n");
    Expect<InvalidDataException>(() => config.GetValue(key), "ConfigFile must reject incomplete Vector4 values.");
}

static void VerifyVector4RemainingValues()
{
    Require(Vector4.Zero == default && Vector4.One == new Vector4(1f, 1f, 1f, 1f) &&
            float.IsPositiveInfinity(Vector4.Inf.X) && float.IsPositiveInfinity(Vector4.Inf.Y) &&
            float.IsPositiveInfinity(Vector4.Inf.Z) && float.IsPositiveInfinity(Vector4.Inf.W) &&
            (int)Vector4.Axis.X == 0 && (int)Vector4.Axis.Y == 1 &&
            (int)Vector4.Axis.Z == 2 && (int)Vector4.Axis.W == 3,
        "Vector4 constants and axis identities retain every pinned component.");
    var value = new Vector4(1.5f, -2f, 0f, 4f);
    var copy = value;
    copy[0] = -3f;
    copy[1] = 4f;
    copy[2] = .5f;
    copy[3] = -2f;
    Require(value == new Vector4(1.5f, -2f, 0f, 4f) &&
            copy == new Vector4(-3f, 4f, .5f, -2f) &&
            copy.X == -3f && copy.Y == 4f && copy.Z == .5f && copy.W == -2f &&
            new Vector4(new Vector4i(16_777_217, int.MinValue, int.MaxValue, -2)) ==
                new Vector4(16_777_216f, int.MinValue, 2_147_483_648f, -2f),
        "Vector4 copies, mutable indexes and integer construction retain W and float rounding.");
    Expect<ArgumentOutOfRangeException>(() => _ = copy[-1], "Negative Vector4 indexes fail explicitly.");
    Expect<ArgumentOutOfRangeException>(() => copy[4] = 1f, "A fifth Vector4 component cannot be assigned.");
    Require(copy == new Vector4(-3f, 4f, .5f, -2f), "Rejected indexing leaves Vector4 copies intact.");

    Require(value + copy == new Vector4(-1.5f, 2f, .5f, 2f) &&
            value - copy == new Vector4(4.5f, -6f, -.5f, 6f) &&
            value * copy == new Vector4(-4.5f, -8f, 0f, -8f) &&
            value / copy == new Vector4(-.5f, -.5f, 0f, -2f) &&
            value * -2f == new Vector4(-3f, 4f, 0f, -8f) &&
            -2f * value == value * -2f && value * int.MaxValue == value * (float)int.MaxValue &&
            value / int.MaxValue == value / (float)int.MaxValue &&
            +value == value && -value == new Vector4(-1.5f, 2f, 0f, -4f),
        "Vector4 component/scalar arithmetic and integer scalar conversion use float operations.");
    var divided = value / 0f;
    var componentDivided = value / Vector4.Zero;
    Require(float.IsPositiveInfinity(divided.X) && float.IsNegativeInfinity(divided.Y) &&
            float.IsNaN(divided.Z) && float.IsPositiveInfinity(divided.W) &&
            float.IsPositiveInfinity(componentDivided.X) &&
            float.IsNegativeInfinity(componentDivided.Y) && float.IsNaN(componentDivided.Z) &&
            float.IsPositiveInfinity(componentDivided.W) &&
            BitConverter.SingleToInt32Bits((-Vector4.Zero).W) == int.MinValue,
        "Vector4 division by zero and unary negation retain IEEE infinity, NaN and signed zero.");

    var lower = new Vector4(1f, 2f, 3f, 4f);
    var higherW = new Vector4(1f, 2f, 3f, 5f);
    var higherZ = new Vector4(1f, 2f, 4f, -100f);
    var higherY = new Vector4(1f, 3f, -100f, -100f);
    var higherX = new Vector4(2f, -100f, -100f, -100f);
    var same = new Vector4(1f, 2f, 3f, 4f);
    var unordered = new Vector4(float.NaN, 2f, 3f, 4f);
    var unorderedCopy = unordered;
    Require(lower < higherW && lower < higherZ && lower < higherY && lower < higherX &&
            higherX > higherY && lower <= same && lower >= same &&
            lower == same && lower != higherW && new Vector4(-0f, 0f, 0f, 0f) == Vector4.Zero &&
            unordered != unorderedCopy && !(unordered == unorderedCopy) &&
            !(unordered < lower) && !(unordered > lower) &&
            !(unordered <= lower) && !(unordered >= lower),
        "Vector4 relational operators compare X/Y/Z/W lexicographically with IEEE NaN and zero rules.");

    var signed = new Vector4(-0f, -1.5f, 2.5f, float.NaN).Abs();
    Require(BitConverter.SingleToInt32Bits(signed.X) == 0 && signed.Y == 1.5f &&
            signed.Z == 2.5f && float.IsNaN(signed.W) &&
            new Vector4(-1.5f, 1.5f, 2.5f, -2.5f).Floor() == new Vector4(-2f, 1f, 2f, -3f) &&
            new Vector4(-1.5f, 1.5f, 2.5f, -2.5f).Ceil() == new Vector4(-1f, 2f, 3f, -2f) &&
            new Vector4(-1.5f, 1.5f, 2.5f, -2.5f).Round() == new Vector4(-2f, 2f, 2f, -2f) &&
            new Vector4(-2f, 0f, 3f, -4f).Sign() == new Vector4(-1f, 0f, 1f, -1f),
        "Vector4 componentwise rounding and signs follow accepted Mathf behavior on W as well.");

    var bounds = new Vector4(-2f, .5f, 5f, 8f);
    Require(bounds.Clamp(new Vector4(-1f, 0f, 1f, 2f), new Vector4(1f, 1f, 4f, 6f)) ==
                new Vector4(-1f, .5f, 4f, 6f) &&
            bounds.Max(2f) == new Vector4(2f, 2f, 5f, 8f) &&
            bounds.Min(2f) == new Vector4(-2f, .5f, 2f, 2f) &&
            bounds.Max(new Vector4(0f, -1f, 4f, 9f)) == new Vector4(0f, .5f, 5f, 9f) &&
            bounds.Min(new Vector4(0f, -1f, 4f, 9f)) == new Vector4(-2f, -1f, 4f, 8f) &&
            float.IsNaN(new Vector4(0f, 0f, 0f, float.NaN).Min(Vector4.One).W),
        "Vector4 clamp and scalar/vector extrema retain all components and accepted NaN policy.");
    Expect<ArgumentException>(() => bounds.Clamp(new Vector4(0f, 0f, 0f, 5f), Vector4.One),
        "A reversed bound on W fails before returning a value.");
    Require(Vector4.One.MaxAxisIndex() == Vector4.Axis.X &&
            Vector4.One.MinAxisIndex() == Vector4.Axis.W &&
            new Vector4(1f, 2f, 2f, 1f).MaxAxisIndex() == Vector4.Axis.Y &&
            new Vector4(float.NaN, 1f, 2f, 3f).MinAxisIndex() == Vector4.Axis.X &&
            new Vector4(1f, 2f, 3f, float.NaN).MaxAxisIndex() == Vector4.Axis.Z,
        "Vector4 axis selection follows its pinned loop for ties and NaN, unlike the Vector3 branch.");

    var reciprocal = new Vector4(2f, -4f, 0f, -0f).Inverse();
    Require(reciprocal.X == .5f && reciprocal.Y == -.25f &&
            float.IsPositiveInfinity(reciprocal.Z) && float.IsNegativeInfinity(reciprocal.W) &&
            new Vector4(0f, 0f, 0f, 3f).LengthSquared() == 9f &&
            new Vector4(0f, 0f, 0f, 3f).Dot(new Vector4(0f, 0f, 0f, 4f)) == 12f &&
            new Vector4(0f, 0f, 0f, 3f).DistanceSquaredTo(Vector4.Zero) == 9f &&
            new Vector4(0f, 0f, 0f, 3f).DistanceTo(Vector4.Zero) == 3f &&
            new Vector4(0f, 0f, 0f, 2f).Normalized() == new Vector4(0f, 0f, 0f, 1f) &&
            Vector4.Zero.DirectionTo(new Vector4(0f, 0f, 0f, 2f)) == new Vector4(0f, 0f, 0f, 1f) &&
            float.IsPositiveInfinity(new Vector4(0f, 0f, 0f, float.MaxValue).Length()) &&
            Vector4.Zero.Lerp(Vector4.One, -1f) == -Vector4.One,
        "Vector4 reciprocal, norms, dot, direction and interpolation include W and IEEE overflow.");
    Require(Vector4.One.IsFinite() && !new Vector4(0f, 0f, 0f, float.NaN).IsFinite() &&
            !new Vector4(0f, 0f, 0f, float.PositiveInfinity).IsFinite() &&
            new Vector4(MathF.Sqrt(1.0005f), 0f, 0f, 0f).IsNormalized() &&
            !new Vector4(MathF.Sqrt(1.002f), 0f, 0f, 0f).IsNormalized() &&
            new Vector4(0f, 0f, 0f, .5e-6f).IsZeroApprox() &&
            !new Vector4(0f, 0f, 0f, 1e-6f).IsZeroApprox() &&
            Vector4.One.IsEqualApprox(new Vector4(1f, 1f, 1f, 1f + .5e-6f)) &&
            !Vector4.One.IsEqualApprox(new Vector4(1f, 1f, 1f, 1f + 2e-6f)) &&
            Vector4.Inf.IsEqualApprox(Vector4.Inf) &&
            !new Vector4(float.NaN, 0f, 0f, 0f).IsEqualApprox(new Vector4(float.NaN, 0f, 0f, 0f)),
        "Vector4 finite, unit and strict approximate predicates retain all components and infinity identity.");

    var modded = new Vector4(-1f, 7f, -7f, 9f).PosMod(new Vector4(4f, -4f, 3f, 0f));
    Require(modded.X == 3f && modded.Y == -1f && modded.Z == 2f && float.IsNaN(modded.W) &&
            new Vector4(1.25f, -1.25f, 2.6f, -2.6f).Snapped(.5f) ==
                new Vector4(1.5f, -1f, 2.5f, -2.5f) &&
            new Vector4(1.25f, -1.25f, 2.6f, -2.6f).Snapped(new Vector4(.5f, 0f, 2f, 0f)) ==
                new Vector4(1.5f, -1.25f, 2f, -2.6f),
        "Vector4 modulus and snapping preserve signed divisors, zero and midpoint behavior per component.");
    var zeroStep = new Vector4(float.NaN, 1f, 2f, -0f).Snapped(0f);
    Require(float.IsNaN(zeroStep.X) && zeroStep.Y == 1f && zeroStep.Z == 2f &&
            BitConverter.SingleToInt32Bits(zeroStep.W) == int.MinValue,
        "Zero-step Vector4 snapping retains NaN and W signed zero.");

    var pre = new Vector4(-1f, 0f, 1f, 10f);
    var start = new Vector4(0f, 1f, 2f, 11f);
    var end = new Vector4(1f, 2f, 3f, 12f);
    var post = new Vector4(2f, 3f, 4f, 13f);
    Require(start.CubicInterpolate(end, pre, post, .5f) == new Vector4(.5f, 1.5f, 2.5f, 11.5f) &&
            start.CubicInterpolateInTime(end, pre, post, .5f, 1f, -1f, 2f) ==
                new Vector4(.5f, 1.5f, 2.5f, 11.5f) &&
            start.CubicInterpolateInTime(end, pre, post, .5f, 0f, 0f, 0f).IsFinite(),
        "Vector4 cubic paths preserve distinct W values and degenerate time fallback.");
}

static void VerifyVector4iValues()
{
    Require(Marshal.SizeOf<Vector4i>() == 16 && typeof(Vector4i).IsDefined(typeof(SerializableAttribute), false) &&
            Vector4i.Zero == default && Vector4i.One == new Vector4i(1, 1, 1, 1) &&
            Vector4i.MinValue == new Vector4i(int.MinValue, int.MinValue, int.MinValue, int.MinValue) &&
            Vector4i.MaxValue == new Vector4i(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue) &&
            (int)Vector4i.Axis.X == 0 && (int)Vector4i.Axis.Y == 1 &&
            (int)Vector4i.Axis.Z == 2 && (int)Vector4i.Axis.W == 3,
        "Vector4i layout and constants must be stable.");
    var value = new Vector4i(1, 2, 3, 4);
    var (x, y, z, w) = value;
    Require(value[0] == 1 && value[3] == 4 && (x, y, z, w) == (1, 2, 3, 4) &&
            value.LengthSquared() == 30 && NearlyEqual(value.Length(), System.MathF.Sqrt(30f)) &&
            value.DistanceSquaredTo(Vector4i.Zero) == 30 && NearlyEqual(value.DistanceTo(Vector4i.Zero), System.MathF.Sqrt(30f)),
        "Vector4i indexing, deconstruction, length, and distance operations must be stable.");
    var mutableCopy = value;
    mutableCopy[0] = -7;
    mutableCopy[1] = 8;
    mutableCopy[2] = 9;
    mutableCopy[3] = 10;
    Require(value == new Vector4i(1, 2, 3, 4) && mutableCopy == new Vector4i(-7, 8, 9, 10),
        "Mutable Vector4i indexing preserves copy independence across all four coordinates.");
    Expect<ArgumentOutOfRangeException>(() => mutableCopy[4] = 1,
        "A fifth Vector4i component cannot be assigned.");
    Require(mutableCopy == new Vector4i(-7, 8, 9, 10),
        "Rejected Vector4i index writes leave the previous value intact.");
    var large = new Vector4i(50_000, 50_000, 50_000, 50_000);
    Require(typeof(Vector4i).GetMethod(nameof(Vector4i.DistanceSquaredTo))!.ReturnType == typeof(long) &&
            large.LengthSquared() == 10_000_000_000L &&
            large.DistanceSquaredTo(Vector4i.Zero) == 10_000_000_000L &&
            large.Length() == 100_000f && large.DistanceTo(Vector4i.Zero) == 100_000f,
        "Vector4i norms must widen all four components before squaring.");
    Require(new Vector4i(-1_500_000_000, 0, 0, 0).DistanceSquaredTo(new Vector4i(1_500_000_000, 0, 0, 0)) == 9_000_000_000_000_000_000L &&
            new Vector4i(-1_500_000_000, 0, 0, 0).DistanceTo(new Vector4i(1_500_000_000, 0, 0, 0)) == 3_000_000_000f,
        "Vector4i distance must widen before subtracting components across the Int32 span.");
    Require(float.IsFinite(Vector4i.MinValue.Length()) &&
            System.MathF.Abs(Vector4i.MinValue.Length() - (float)(2d * 2_147_483_648d)) <= 512f &&
            float.IsFinite(Vector4i.MinValue.DistanceTo(Vector4i.MaxValue)) &&
            System.MathF.Abs(Vector4i.MinValue.DistanceTo(Vector4i.MaxValue) - (float)(2d * uint.MaxValue)) <= 1024f,
        "Vector4i lengths and distances must remain finite across the entire component range.");
    Require(new Vector4i(int.MaxValue, int.MaxValue, 0, 0).LengthSquared() ==
                2L * int.MaxValue * int.MaxValue &&
            new Vector4i(0, 0, 0, int.MaxValue).LengthSquared() == (long)int.MaxValue * int.MaxValue,
        "Vector4i checked squares preserve the last fitting Int64 boundary and W contribution.");
    Expect<OverflowException>(() => _ = new Vector4i(int.MinValue, int.MinValue, 0, 0).LengthSquared(),
        "Two minimum components already exceed signed Int64 by one.");
    Expect<OverflowException>(() => _ = Vector4i.MinValue.LengthSquared(),
        "Vector4i squared length must reject a result above Int64.MaxValue.");
    Expect<OverflowException>(() => _ = Vector4i.MinValue.DistanceSquaredTo(Vector4i.MaxValue),
        "Vector4i squared distance must reject a widened difference above Int64.MaxValue.");
    Expect<ArgumentOutOfRangeException>(() => _ = value[-1], "Vector4i must reject negative indices.");
    Require(new Vector4i(-1, -2, -3, -4).Abs() == value && new Vector4i(-1, 0, 3, -4).Sign() == new Vector4i(-1, 0, 1, -1) &&
            value.Clamp(2, 3) == new Vector4i(2, 2, 3, 3) &&
            value.Clamp(new Vector4i(0, 0, 4, 0), new Vector4i(2, 3, 5, 3)) == new Vector4i(1, 2, 4, 3),
        "Vector4i absolute, sign, and clamp methods must be componentwise.");
    Expect<OverflowException>(() => Vector4i.MinValue.Abs(), "Vector4i Abs must surface minimum-integer overflow.");
    Expect<ArgumentException>(() => value.Clamp(2, 1), "Vector4i must reject reversed scalar clamp bounds.");
    Expect<ArgumentException>(() => value.Clamp(new Vector4i(0, 3, 0, 0), new Vector4i(2, 2, 4, 5)),
        "Vector4i must reject reversed component clamp bounds.");
    Require(value.Max(2) == new Vector4i(2, 2, 3, 4) && value.Max(new Vector4i(0, 3, 2, 5)) == new Vector4i(1, 3, 3, 5) &&
            value.Min(2) == new Vector4i(1, 2, 2, 2) && value.Min(new Vector4i(0, 3, 2, 5)) == new Vector4i(0, 2, 2, 4) &&
            Vector4i.One.MaxAxisIndex() == Vector4i.Axis.X && Vector4i.One.MinAxisIndex() == Vector4i.Axis.W &&
            new Vector4i(1, 2, 2, 2).MaxAxisIndex() == Vector4i.Axis.Y &&
            new Vector4i(2, 1, 1, 1).MinAxisIndex() == Vector4i.Axis.W &&
            new Vector4i(5, -5, 3, -3).Snapped(2) == new Vector4i(6, -4, 4, -2) &&
            new Vector4i(5, -5, 3, -3).Snapped(new Vector4i(2, 5, 2, 3)) == new Vector4i(6, -5, 4, -3),
        "Vector4i min, max, axis tie-breaking, and snapping must be stable.");
    Require(new Vector4i(5, -5, 7, -7).Snapped(-2) == new Vector4i(4, -6, 6, -8) &&
            new Vector4i(5, -5, 7, -7).Snapped(new Vector4i(-2, 0, 3, 0)) ==
                new Vector4i(4, -5, 6, -7) && value.Snapped(0) == value,
        "Vector4i negative and zero snap steps follow the scalar formula independently on W.");
    Expect<OverflowException>(() => _ = Vector4i.MaxValue.Snapped(2),
        "Vector4i snapping rejects a rounded component beyond Int32.MaxValue.");
    Require(value + Vector4i.One == new Vector4i(2, 3, 4, 5) && +value == value && value - Vector4i.One == new Vector4i(0, 1, 2, 3) &&
            -value == new Vector4i(-1, -2, -3, -4) && value * 2 == 2 * value && value * Vector4i.One == value &&
            value * 0.5f == 0.5f * value && value / 2f == new Vector4(0.5f, 1f, 1.5f, 2f) &&
            new Vector4i(2, 4, 6, 8) / 2 == value && new Vector4i(2, 6, 12, 20) / value == new Vector4i(2, 3, 4, 5) &&
            new Vector4i(5, -5, 8, -8) % 3 == new Vector4i(2, -2, 2, -2) &&
            new Vector4i(5, 8, 9, 10) % new Vector4i(3, 5, 4, 6) == new Vector4i(2, 3, 1, 4),
        "Vector4i arithmetic must be componentwise.");
    Require(Vector4i.MaxValue + Vector4i.One == Vector4i.MinValue,
        "Vector4i ordinary overflow must wrap deterministically.");
    Require(-new Vector4i(0, 0, 0, int.MinValue) == new Vector4i(0, 0, 0, int.MinValue) &&
            new Vector4i(0, 0, 0, int.MaxValue) * 2 == new Vector4i(0, 0, 0, -2) &&
            float.IsPositiveInfinity((new Vector4i(1, -1, 0, 0) / 0f).X) &&
            float.IsNegativeInfinity((new Vector4i(1, -1, 0, 0) / 0f).Y) &&
            float.IsNaN((new Vector4i(1, -1, 0, 0) / 0f).W),
        "W wrapping and floating division by zero retain accepted C# and IEEE boundaries.");
    Expect<DivideByZeroException>(() => _ = value / new Vector4i(1, 1, 0, 1), "Vector4i division must reject zero components.");
    Expect<DivideByZeroException>(() => _ = value % 0, "Vector4i remainder must reject a zero scalar.");
    Expect<OverflowException>(() => _ = new Vector4i(int.MinValue, 0, 0, 0) / -1,
        "Vector4i division must surface minimum-integer overflow.");
    Expect<OverflowException>(() => _ = new Vector4i(int.MinValue, 0, 0, 0) % -1,
        "Vector4i remainder must surface minimum-integer overflow.");
    Require(value < new Vector4i(1, 2, 3, 5) && value <= new Vector4i(1, 2, 3, 4) &&
            value < new Vector4i(1, 2, 4, -100) &&
            value < new Vector4i(1, 3, -100, -100) &&
            new Vector4i(2, 0, 0, 0) > value && value >= new Vector4i(1, 2, 3, 4) &&
            value.Equals((object)new Vector4i(1, 2, 3, 4)) && value.GetHashCode() == new Vector4i(1, 2, 3, 4).GetHashCode(),
        "Vector4i equality, hashing, and lexicographic ordering must be stable.");
    Require((Vector4i)new Vector4((float)int.MinValue, 0f, 0f, 0f) ==
                new Vector4i(int.MinValue, 0, 0, 0) &&
            (Vector4)new Vector4i(16_777_217, int.MinValue, int.MaxValue, -2) ==
                new Vector4(16_777_216f, int.MinValue, 2_147_483_648f, -2f),
        "Vector4i conversions keep the lower Int32 bound and expose large-int float rounding.");
    Expect<ArgumentOutOfRangeException>(() => _ = (Vector4i)new Vector4(0f, 0f, float.NaN, 0f),
        "Vector4 to Vector4i conversion must reject non-finite values.");
    Expect<ArgumentOutOfRangeException>(() => _ = (Vector4i)new Vector4(0f, 0f, 2147483648f, 0f),
        "Vector4 to Vector4i conversion must reject out-of-range values.");
    VerifyInvariantString(() => new Vector4i(1, 2, 3, 4).ToString("D2"), "(01, 02, 03, 04)", "Vector4i");
    Expect<FormatException>(() => _ = value.ToString("Q"), "Vector4i must reject invalid numeric formats.");

    var key = new ConfigKey<Vector4i>("math", "vector4i");
    using var config = new ConfigFile();
    config.SetValue(key, value);
    Require(config.GetValue(key) == value && config.EncodeToText() == "[math]\n\nvector4i={\"X\":1,\"Y\":2,\"Z\":3,\"W\":4}\n",
        "ConfigFile must preserve the strict Vector4i schema.");
    config.Parse("[math]\nvector4i={\"X\":1,\"Y\":2,\"Z\":3,\"W\":4,\"Q\":5}\n");
    Expect<InvalidDataException>(() => config.GetValue(key), "ConfigFile must reject unknown Vector4i fields.");

    using var scene = new PackedScene();
    var source = new ColorPackedNode
    {
        Name = "VectorRoot",
        PackedVector2 = new Vector2(1.5f, -2.5f),
        PackedVector2i = new Vector2i(3, -4),
        PackedVector3 = new Vector3(.5f, 1.5f, 2.5f),
        PackedVector3i = new Vector3i(3, -4, 5),
        PackedVector4 = new Vector4(1f, 2f, 3f, 4f),
        PackedVector4i = new Vector4i(5, 6, 7, 8),
    };
    scene.Pack(source);
    source.Dispose();
    using var instance = (ColorPackedNode)scene.Instantiate();
    Require(instance.PackedVector2 == new Vector2(1.5f, -2.5f) && instance.PackedVector2i == new Vector2i(3, -4) &&
            instance.PackedVector3 == new Vector3(.5f, 1.5f, 2.5f) && instance.PackedVector3i == new Vector3i(3, -4, 5) &&
            instance.PackedVector4 == new Vector4(1f, 2f, 3f, 4f) && instance.PackedVector4i == new Vector4i(5, 6, 7, 8),
        "PackedScene must preserve all six stored vector value types.");

    _ = ExerciseVectorHotPath(32);
    var beforeAllocations = GC.GetAllocatedBytesForCurrentThread();
    var hotResult = ExerciseVectorHotPath(10_000);
    var allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocations;
    Require(allocated == 0 && float.IsFinite(hotResult),
        "Warmed vector numeric operations must not allocate managed memory.");
}

static float ExerciseVectorHotPath(int iterations)
{
    var vector2 = new Vector2(0.25f, -0.5f);
    var vector2I = new Vector2i(3, -5);
    var vector4 = new Vector4(0.25f, -0.5f, 0.75f, -1f);
    var vector4I = new Vector4i(3, -5, 7, -9);
    for (var index = 0; index < iterations; index++)
    {
        vector2 = vector2.Rotated(0.00001f).Lerp(Vector2.One, 0.00001f);
        vector2I = (vector2I + Vector2i.One) - Vector2i.One;
        vector4 = vector4.Lerp(Vector4.One, 0.00001f).Snapped(0.000001f);
        vector4I = (vector4I + Vector4i.One) - Vector4i.One;
    }

    return vector2.X + vector2I.X + vector4.X + vector4I.X;
}

static void VerifyInvariantString(Func<string> valueFactory, string expected, string typeName)
{
    var previousCulture = CultureInfo.CurrentCulture;
    try
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        Require(valueFactory() == expected, $"{typeName} formatting must use invariant culture.");
    }
    finally
    {
        CultureInfo.CurrentCulture = previousCulture;
    }
}

static void VerifyRectangles()
{
    Require(Marshal.SizeOf<Rect2>() == 16 && typeof(Rect2).IsDefined(typeof(SerializableAttribute), inherit: false) &&
            typeof(Rect2).StructLayoutAttribute?.Value == LayoutKind.Sequential,
        "Rect2 must be a serializable sequential four-float value type.");
    Require(default(Rect2) == new Rect2(Vector2.Zero, Vector2.Zero) &&
            new Rect2(new Vector2(1f, 2f), new Vector2(3f, 4f)) == new Rect2(1f, 2f, 3f, 4f) &&
            new Rect2(new Vector2(1f, 2f), 3f, 4f) == new Rect2(1f, 2f, new Vector2(3f, 4f)),
        "Zero initialization and every typed constructor must preserve position and size.");
    Require((int)Side.Left == 0 && (int)Side.Top == 1 && (int)Side.Right == 2 && (int)Side.Bottom == 3,
        "Side numeric values must remain stable.");

    var mutable = new Rect2(new Vector2(1f, 2f), new Vector2(3f, 4f));
    var independentCopy = mutable;
    mutable.Position = new Vector2(2f, 3f);
    mutable.Size = new Vector2(5f, 6f);
    Require(mutable.End == new Vector2(7f, 9f) && independentCopy == new Rect2(1f, 2f, 3f, 4f),
        "Position and Size mutation must update the computed end.");
    mutable.End = new Vector2(10f, 12f);
    Require(mutable.Position == new Vector2(2f, 3f) && mutable.Size == new Vector2(8f, 9f),
        "Assigning End must preserve Position and derive Size.");
    Require(new Rect2(0f, 0f, 3f, 4f).Area == 12f &&
            new Rect2(0f, 0f, -3f, -4f).Area == 12f &&
            !new Rect2(0f, 0f, -3f, -4f).HasArea(),
        "Area must remain a signed product while HasArea requires two positive components.");

    var normalized = new Rect2(25f, 25f, -100f, -50f).Abs();
    Require(normalized == new Rect2(-75f, -25f, 100f, 50f),
        "Abs must move the origin and normalize both size components.");
    Require(new Rect2(float.NegativeInfinity, 0f, float.PositiveInfinity, 1f).Abs().Position.X ==
                float.NegativeInfinity,
        "Abs must select the size contribution before adding it to the position.");
    var outer = new Rect2(0f, 0f, 10f, 10f);
    Require(outer.Encloses(new Rect2(0f, 0f, 10f, 10f)) &&
            outer.Encloses(new Rect2(2f, 3f, 4f, 5f)) &&
            !outer.Encloses(new Rect2(-1f, 3f, 4f, 5f)),
        "Encloses must accept coincident edges and reject an escaped edge.");
    Require(new Rect2(0f, 0f, 5f, 5f).Expand(new Vector2(-2f, 7f)) == new Rect2(-2f, 0f, 7f, 7f) &&
            outer.Expand(new Vector2(10f, 10f)) == outer,
        "Expand must grow only the edges needed to include a point.");
    Require(new Rect2(1f, 2f, 3f, 4f).GetCenter() == new Vector2(2.5f, 4f) &&
            new Rect2(1f, 2f, 3f, 4f).GetSupport(new Vector2(1f, -1f)) == new Vector2(4f, 2f) &&
            new Rect2(1f, 2f, 3f, 4f).GetSupport(Vector2.Zero) == new Vector2(1f, 2f),
        "Center and support mapping must use the documented edges.");

    var baseRect = new Rect2(1f, 2f, 3f, 4f);
    Require(baseRect.Grow(2f) == new Rect2(-1f, 0f, 7f, 8f) &&
            baseRect.Grow(-1f) == new Rect2(2f, 3f, 1f, 2f) &&
            baseRect.Grow(-3f) == new Rect2(4f, 5f, -3f, -2f) &&
            baseRect.GrowIndividual(1f, 2f, 3f, 4f) == new Rect2(0f, 0f, 7f, 10f),
        "Grow operations must move origins and add the matching side amounts.");
    Require(float.IsPositiveInfinity(new Rect2(0f, 0f, -float.MaxValue, 1f).Grow(float.MaxValue).Size.X) &&
            new Rect2(0f, 0f, float.MaxValue, float.MaxValue)
                .GrowIndividual(float.MaxValue, float.MaxValue, -float.MaxValue, -float.MaxValue).Size ==
                new Vector2(float.MaxValue, float.MaxValue),
        "Grow doubles a shared amount before adding it; individual sides combine before size addition.");
    Require(baseRect.GrowSide(Side.Left, 1f) == new Rect2(0f, 2f, 4f, 4f) &&
            baseRect.GrowSide(Side.Top, 1f) == new Rect2(1f, 1f, 3f, 5f) &&
            baseRect.GrowSide(Side.Right, 1f) == new Rect2(1f, 2f, 4f, 4f) &&
            baseRect.GrowSide(Side.Bottom, 1f) == new Rect2(1f, 2f, 3f, 5f) &&
            baseRect.GrowSide((Side)99, 1f) == baseRect,
        "GrowSide must cover every side and leave undefined values unchanged.");

    Require(outer.HasArea() && !new Rect2(0f, 0f, 0f, 1f).HasArea() &&
            !new Rect2(0f, 0f, 1f, -1f).HasArea(),
        "HasArea must reject zero and negative size components.");
    Require(outer.HasPoint(Vector2.Zero) && outer.HasPoint(new Vector2(9.999f, 9.999f)) &&
            !outer.HasPoint(new Vector2(10f, 5f)) && !outer.HasPoint(new Vector2(5f, 10f)) &&
            !outer.HasPoint(new Vector2(-0.001f, 5f)),
        "HasPoint must include left/top edges and exclude right/bottom edges.");
    Require(outer.HasPoint(new Vector2(float.NaN, 5f)) &&
            outer.HasPoint(new Vector2(5f, float.NaN)) &&
            !outer.HasPoint(new Vector2(float.NaN, 11f)),
        "HasPoint retains the ordered rejection behavior for NaN coordinates.");

    var overlap = new Rect2(8f, 4f, 5f, 8f);
    var touching = new Rect2(10f, 2f, 4f, 3f);
    var containedEmpty = new Rect2(5f, 6f, 0f, 0f);
    Require(outer.Intersects(overlap) && outer.Intersection(overlap) == new Rect2(8f, 4f, 2f, 6f) &&
            !outer.Intersects(touching) && outer.Intersects(touching, includeBorders: true) &&
            outer.Intersection(touching) == default && !outer.Intersects(new Rect2(11f, 0f, 1f, 1f)) &&
            outer.Intersects(containedEmpty) && outer.Intersection(containedEmpty) == containedEmpty,
        "Intersection tests must distinguish positive overlap, touching borders, and separation.");
    Require(outer.Intersects(new Rect2(float.NaN, 2f, 1f, 1f)) &&
            outer.Intersects(new Rect2(float.NaN, 2f, 1f, 1f), includeBorders: true) &&
            !outer.Intersects(new Rect2(float.NaN, 11f, 1f, 1f)),
        "Intersects retains the ordered rejection behavior for NaN rectangle coordinates.");
    Require(outer.Merge(overlap) == new Rect2(0f, 0f, 13f, 12f),
        "Merge must return the smallest enclosing rectangle.");
    var exact = new Rect2(1f, 2f, 3f, 4f);
    var nanRect = new Rect2(float.NaN, 2f, 3f, 4f);
    Require(exact == new Rect2(1f, 2f, 3f, 4f) && exact != new Rect2(1f, 2f, 3f, 5f) &&
            exact.Equals((object)new Rect2(1f, 2f, 3f, 4f)) &&
            exact.GetHashCode() == new Rect2(1f, 2f, 3f, 4f).GetHashCode() &&
            exact.IsEqualApprox(new Rect2(1.000001f, 2f, 3f, 4f)) &&
            new Rect2(float.PositiveInfinity, 0f, 1f, 1f).IsEqualApprox(
                new Rect2(float.PositiveInfinity, 0f, 1f, 1f)) &&
            nanRect != new Rect2(float.NaN, 2f, 3f, 4f) && !nanRect.IsEqualApprox(nanRect),
        "Exact and approximate equality must define finite, infinity, and NaN behavior.");
    Require(exact.IsFinite() && !nanRect.IsFinite() &&
            !new Rect2(0f, 0f, float.NegativeInfinity, 1f).IsFinite() &&
            !new Rect2(0f, float.NaN, 1f, 1f).IsFinite() &&
            !new Rect2(0f, 0f, 1f, float.PositiveInfinity).IsFinite(),
        "IsFinite must inspect every position and size component.");

    var previousCulture = CultureInfo.CurrentCulture;
    try
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        Require(new Rect2(1.5f, 2.5f, 3.5f, 4.5f).ToString() == "(1.5, 2.5), (3.5, 4.5)" &&
                new Rect2(1.5f, 2.5f, 3.5f, 4.5f).ToString("F1") == "(1.5, 2.5), (3.5, 4.5)",
            "Rect2 formatting must use invariant culture.");
        Expect<FormatException>(() => _ = new Rect2(1f, 2f, 3f, 4f).ToString("Q"),
            "Rect2 formatting must surface invalid numeric formats.");
    }
    finally
    {
        CultureInfo.CurrentCulture = previousCulture;
    }

    var rectangleKey = new ConfigKey<Rect2>("geometry", "bounds");
    using (var config = new ConfigFile())
    {
        var stored = new Rect2(1f, 2f, 3f, 4f);
        config.SetValue(rectangleKey, stored);
        Require(config.EncodeToText() ==
                "[geometry]\n\nbounds={\"Position\":{\"X\":1,\"Y\":2},\"Size\":{\"X\":3,\"Y\":4}}\n" &&
                config.GetValue(rectangleKey) == stored,
            "ConfigFile must use the stable finite Position/Size rectangle schema.");
        Expect<JsonException>(() => config.SetValue(rectangleKey, new Rect2(float.NaN, 0f, 1f, 1f)),
            "ConfigFile must reject non-finite rectangle components before mutation.");
        Require(config.GetValue(rectangleKey) == stored,
            "Failed rectangle serialization must preserve the prior configuration token.");

        config.Parse("[geometry]\nbounds={\"Position\":{\"X\":1,\"Y\":2}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject a rectangle with a missing field.");
        config.Parse("[geometry]\nbounds={\"Position\":{\"X\":1,\"Y\":2},\"Size\":{\"X\":3,\"Y\":4},\"End\":{\"X\":4,\"Y\":6}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject unknown rectangle fields.");
        config.Parse("[geometry]\nbounds={\"Position\":{\"X\":1,\"X\":2,\"Y\":2},\"Size\":{\"X\":3,\"Y\":4}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject duplicate rectangle vector components.");
        config.Parse("[geometry]\nbounds={\"Position\":{\"X\":1,\"Y\":2},\"Position\":{\"X\":1,\"Y\":2},\"Size\":{\"X\":3,\"Y\":4}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject duplicate rectangle fields.");
        config.Parse("[geometry]\nbounds={\"Position\":{\"X\":1},\"Size\":{\"X\":3,\"Y\":4}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject incomplete rectangle vectors.");
        config.Parse("[geometry]\nbounds={\"Position\":{\"X\":\"left\",\"Y\":2},\"Size\":{\"X\":3,\"Y\":4}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject nonnumeric rectangle vector components.");
        config.Parse("[geometry]\nbounds={\"Position\":{\"X\":1e999,\"Y\":2},\"Size\":{\"X\":3,\"Y\":4}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject non-finite numeric rectangle components.");
    }

    using (var scene = new PackedScene())
    {
        var source = new ColorPackedNode
        {
            Name = "GeometryRoot",
            Bounds = new Rect2(-2f, -3f, 8f, 9f),
        };
        scene.Pack(source);
        source.Dispose();
        using var instance = (ColorPackedNode)scene.Instantiate();
        Require(instance.Bounds == new Rect2(-2f, -3f, 8f, 9f),
            "PackedScene must preserve stored Rect2 properties.");
    }

    _ = ExerciseRectHotPath(32);
    var beforeAllocations = GC.GetAllocatedBytesForCurrentThread();
    var hotResult = ExerciseRectHotPath(10_000);
    var allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocations;
    Require(allocated == 0 && hotResult.IsFinite(),
        "Warmed rectangle geometry operations must not allocate managed memory.");
}

static Rect2 ExerciseRectHotPath(int iterations)
{
    var value = new Rect2(1f, 2f, 3f, 4f);
    var bounds = new Rect2(-100f, -100f, 200f, 200f);
    for (var index = 0; index < iterations; index++)
    {
        value = value.Grow(0.0001f).Intersection(bounds);
        value = value.Merge(new Rect2(1f, 2f, 3f, 4f));
    }

    return value;
}

static void VerifyIntegerRectangles()
{
    Require(Marshal.SizeOf<Rect2i>() == 16 && typeof(Rect2i).IsDefined(typeof(SerializableAttribute), inherit: false) &&
            typeof(Rect2i).StructLayoutAttribute?.Value == LayoutKind.Sequential,
        "Rect2i must be a serializable sequential four-integer value type.");
    Require(default(Rect2i) == new Rect2i(Vector2i.Zero, Vector2i.Zero) &&
            new Rect2i(new Vector2i(1, 2), new Vector2i(3, 4)) == new Rect2i(1, 2, 3, 4) &&
            new Rect2i(new Vector2i(1, 2), 3, 4) == new Rect2i(1, 2, new Vector2i(3, 4)),
        "Zero initialization and every Rect2i constructor must preserve position and size.");

    var mutable = new Rect2i(new Vector2i(1, 2), new Vector2i(3, 4));
    var independentCopy = mutable;
    mutable.Position = new Vector2i(2, 3);
    mutable.Size = new Vector2i(5, 6);
    Require(mutable.End == new Vector2i(7, 9) && independentCopy == new Rect2i(1, 2, 3, 4),
        "Rect2i Position and Size mutation must update the computed end.");
    mutable.End = new Vector2i(10, 12);
    Require(mutable.Position == new Vector2i(2, 3) && mutable.Size == new Vector2i(8, 9),
        "Assigning Rect2i.End must preserve Position and derive Size.");
    var wrappedEnd = new Rect2i(int.MaxValue, int.MinValue, 0, 0);
    wrappedEnd.End = new Vector2i(int.MinValue, int.MaxValue);
    Require(wrappedEnd.Position == new Vector2i(int.MaxValue, int.MinValue) &&
            wrappedEnd.Size == new Vector2i(1, -1),
        "Assigning Rect2i.End wraps the derived size without moving Position.");
    Require(new Rect2i(0, 0, 3, 4).Area == 12 && new Rect2i(0, 0, -3, -4).Area == 12 &&
            !new Rect2i(0, 0, -3, -4).HasArea() &&
            new Rect2i(int.MaxValue, 0, 1, 1).End.X == int.MinValue &&
            new Rect2i(0, 0, int.MaxValue, 2).Area == -2,
        "Rect2i area, end, and ordinary overflow must use documented 32-bit behavior.");

    var normalized = new Rect2i(25, 25, -100, -50).Abs();
    Require(normalized == new Rect2i(-75, -25, 100, 50),
        "Rect2i.Abs must move the origin and normalize both size components.");
    Require(new Rect2i(int.MaxValue, 0, 1, 2).Abs() == new Rect2i(int.MaxValue, 0, 1, 2) &&
            new Rect2i(int.MinValue, 0, -1, 2).Abs() == new Rect2i(int.MaxValue, 0, 1, 2),
        "Rect2i.Abs must select negative size before position addition, even across wraparound.");
    Expect<OverflowException>(() => _ = new Rect2i(0, 0, int.MinValue, 1).Abs(),
        "Rect2i.Abs must expose minimum-integer absolute-value overflow.");

    var outer = new Rect2i(0, 0, 10, 10);
    Require(outer.Encloses(new Rect2i(0, 0, 10, 10)) &&
            outer.Encloses(new Rect2i(2, 3, 4, 5)) &&
            !outer.Encloses(new Rect2i(-1, 3, 4, 5)),
        "Rect2i.Encloses must accept coincident edges and reject an escaped edge.");
    Require(new Rect2i(0, 0, 5, 5).Expand(new Vector2i(-2, 7)) == new Rect2i(-2, 0, 7, 7) &&
            outer.Expand(new Vector2i(10, 10)) == outer &&
            new Rect2i(1, 2, 3, 5).GetCenter() == new Vector2i(2, 4) &&
            new Rect2i(0, 0, -3, -5).GetCenter() == new Vector2i(-1, -2),
        "Rect2i expansion and integer center rounding must preserve edge semantics.");

    var baseRect = new Rect2i(1, 2, 3, 4);
    Require(baseRect.Grow(2) == new Rect2i(-1, 0, 7, 8) &&
            baseRect.Grow(-1) == new Rect2i(2, 3, 1, 2) &&
            baseRect.GrowIndividual(1, 2, 3, 4) == new Rect2i(0, 0, 7, 10),
        "Rect2i growth must move origins and add matching side amounts.");
    Require(baseRect.GrowSide(Side.Left, 1) == new Rect2i(0, 2, 4, 4) &&
            baseRect.GrowSide(Side.Top, 1) == new Rect2i(1, 1, 3, 5) &&
            baseRect.GrowSide(Side.Right, 1) == new Rect2i(1, 2, 4, 4) &&
            baseRect.GrowSide(Side.Bottom, 1) == new Rect2i(1, 2, 3, 5) &&
            baseRect.GrowSide((Side)99, 1) == baseRect &&
            new Rect2i(int.MinValue, 0, 1, 1).Grow(1).Position.X == int.MaxValue,
        "Rect2i.GrowSide must cover every side, undefined values, and unchecked overflow.");

    Require(outer.HasArea() && !new Rect2i(0, 0, 0, 1).HasArea() &&
            !new Rect2i(0, 0, 1, -1).HasArea(),
        "Rect2i.HasArea must reject zero and negative size components.");
    Require(outer.HasPoint(Vector2i.Zero) && outer.HasPoint(new Vector2i(9, 9)) &&
            !outer.HasPoint(new Vector2i(10, 5)) && !outer.HasPoint(new Vector2i(5, 10)) &&
            !outer.HasPoint(new Vector2i(-1, 5)),
        "Rect2i.HasPoint must include left/top edges and exclude right/bottom edges.");

    var overlap = new Rect2i(8, 4, 5, 8);
    var touching = new Rect2i(10, 2, 4, 3);
    var containedEmpty = new Rect2i(5, 6, 0, 0);
    Require(outer.Intersects(overlap) && outer.Intersection(overlap) == new Rect2i(8, 4, 2, 6) &&
            !outer.Intersects(touching) && outer.Intersection(touching) == default &&
            !outer.Intersects(new Rect2i(11, 0, 1, 1)) && outer.Intersects(containedEmpty) &&
            outer.Intersection(containedEmpty) == containedEmpty,
        "Rect2i intersections must distinguish positive overlap, touching borders, separation, and contained emptiness.");
    Require(outer.Merge(overlap) == new Rect2i(0, 0, 13, 12),
        "Rect2i.Merge must return the smallest enclosing rectangle.");

    var exact = new Rect2i(1, 2, 3, 4);
    Require(exact == new Rect2i(1, 2, 3, 4) && exact != new Rect2i(1, 2, 3, 5) &&
            exact.Equals((object)new Rect2i(1, 2, 3, 4)) && exact.Equals(new Rect2i(1, 2, 3, 4)) &&
            exact.GetHashCode() == new Rect2i(1, 2, 3, 4).GetHashCode(),
        "Rect2i exact equality and hashing must use position and size.");
    Require((Rect2)exact == new Rect2(1f, 2f, 3f, 4f) &&
            (Rect2i)new Rect2(1.9f, -2.9f, 3.9f, -4.9f) == new Rect2i(1, -2, 3, -4) &&
            (Rect2i)new Rect2(int.MinValue, 0f, 1f, 1f) == new Rect2i(int.MinValue, 0, 1, 1) &&
            (Rect2)new Rect2i(16_777_217, 0, int.MaxValue, 1) ==
                new Rect2(16_777_216f, 0f, 2_147_483_648f, 1f),
        "Rect2 and Rect2i conversions must widen implicitly and truncate explicitly.");
    Expect<ArgumentOutOfRangeException>(() => _ = (Rect2i)new Rect2(float.NaN, 0f, 1f, 1f),
        "Rect2 to Rect2i conversion must reject non-finite components.");
    Expect<ArgumentOutOfRangeException>(() => _ = (Rect2i)new Rect2(2147483648f, 0f, 1f, 1f),
        "Rect2 to Rect2i conversion must reject out-of-range components.");
    VerifyInvariantString(() => new Rect2i(1, 2, 3, 4).ToString("D2"), "(01, 02), (03, 04)", "Rect2i");
    Expect<FormatException>(() => _ = exact.ToString("Q"),
        "Rect2i formatting must surface invalid numeric formats.");

    var rectangleKey = new ConfigKey<Rect2i>("geometry", "integer_bounds");
    using (var config = new ConfigFile())
    {
        var stored = new Rect2i(-2, -3, 8, 9);
        config.SetValue(rectangleKey, stored);
        Require(config.EncodeToText() ==
                "[geometry]\n\ninteger_bounds={\"Position\":{\"X\":-2,\"Y\":-3},\"Size\":{\"X\":8,\"Y\":9}}\n" &&
                config.GetValue(rectangleKey) == stored,
            "ConfigFile must preserve the strict Rect2i Position/Size schema.");

        config.Parse("[geometry]\ninteger_bounds={\"Position\":{\"X\":1,\"Y\":2}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject a Rect2i with a missing field.");
        config.Parse("[geometry]\ninteger_bounds={\"Position\":{\"X\":1,\"Y\":2},\"Size\":{\"X\":3,\"Y\":4},\"End\":{\"X\":4,\"Y\":6}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject unknown Rect2i fields.");
        config.Parse("[geometry]\ninteger_bounds={\"Position\":{\"X\":1,\"X\":2,\"Y\":2},\"Size\":{\"X\":3,\"Y\":4}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject duplicate Rect2i vector components.");
        config.Parse("[geometry]\ninteger_bounds={\"Position\":{\"X\":1,\"Y\":2},\"Position\":{\"X\":1,\"Y\":2},\"Size\":{\"X\":3,\"Y\":4}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject duplicate Rect2i fields.");
        config.Parse("[geometry]\ninteger_bounds={\"Position\":{\"X\":1,\"Y\":2},\"Size\":{\"X\":2147483648,\"Y\":4}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject out-of-range Rect2i components.");
        config.Parse("[geometry]\ninteger_bounds={\"Position\":{\"X\":1.5,\"Y\":2},\"Size\":{\"X\":3,\"Y\":4}}\n");
        Expect<InvalidDataException>(() => config.GetValue(rectangleKey),
            "ConfigFile must reject non-integer Rect2i components.");
    }

    using (var scene = new PackedScene())
    {
        var source = new ColorPackedNode
        {
            Name = "IntegerGeometryRoot",
            BoundsI = new Rect2i(-2, -3, 8, 9),
        };
        scene.Pack(source);
        source.Dispose();
        using var instance = (ColorPackedNode)scene.Instantiate();
        Require(instance.BoundsI == new Rect2i(-2, -3, 8, 9),
            "PackedScene must preserve stored Rect2i properties.");
    }

    _ = ExerciseRect2iHotPath(32);
    var beforeAllocations = GC.GetAllocatedBytesForCurrentThread();
    var hotResult = ExerciseRect2iHotPath(10_000);
    var allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocations;
    Require(allocated == 0 && hotResult.HasArea(),
        "Warmed Rect2i geometry operations must not allocate managed memory.");
}

static Rect2i ExerciseRect2iHotPath(int iterations)
{
    var value = new Rect2i(1, 2, 3, 4);
    var bounds = new Rect2i(-100_000, -100_000, 200_000, 200_000);
    for (var index = 0; index < iterations; index++)
    {
        value = value.Grow(1).Intersection(bounds);
        value = value.Merge(new Rect2i(1, 2, 3, 4));
    }

    return value;
}

static void VerifyTransforms()
{
    Require(Marshal.SizeOf<Transform>() == 24 &&
            typeof(Transform).IsDefined(typeof(SerializableAttribute), inherit: false) &&
            typeof(Transform).StructLayoutAttribute?.Value == LayoutKind.Sequential,
        "Transform must be a serializable sequential six-float value type.");
    Require(default(Transform) == new Transform(Vector2.Zero, Vector2.Zero, Vector2.Zero) &&
            default(Transform) != Transform.Identity &&
            Transform.Identity == new Transform(1f, 0f, 0f, 1f, 0f, 0f) &&
            Transform.FlipX == new Transform(-1f, 0f, 0f, 1f, 0f, 0f) &&
            Transform.FlipY == new Transform(1f, 0f, 0f, -1f, 0f, 0f),
        "Zero initialization and the three standard transforms must remain distinct and stable.");

    var indexed = new Transform(Vector2.Right, Vector2.Down, new Vector2(2f, 3f));
    indexed[0] = new Vector2(4f, 5f);
    indexed[1, 0] = 6f;
    indexed[2, 1] = 7f;
    Require(indexed.X == new Vector2(4f, 5f) && indexed.Y == new Vector2(6f, 1f) &&
            indexed.Origin == new Vector2(2f, 7f) && indexed[0, 1] == 5f,
        "Column and component indexers must read and mutate the same sequential storage.");
    Expect<ArgumentOutOfRangeException>(() => _ = indexed[-1],
        "The column indexer must reject negative indices.");
    Expect<ArgumentOutOfRangeException>(() => indexed[3] = Vector2.Zero,
        "The column indexer must reject indices after Origin.");
    Expect<ArgumentOutOfRangeException>(() => _ = indexed[0, 2],
        "The component indexer must reject rows after Y.");
    Expect<ArgumentOutOfRangeException>(() => indexed[3, 0] = 1f,
        "The component indexer must reject invalid columns before mutation.");

    var quarterTurn = new Transform(System.MathF.PI * 0.5f, new Vector2(3f, 4f));
    Require(VectorNearlyEqual(quarterTurn.X, Vector2.Down) &&
            VectorNearlyEqual(quarterTurn.Y, -Vector2.Right) &&
            VectorNearlyEqual(quarterTurn * new Vector2(2f, 1f), new Vector2(2f, 6f)) &&
            NearlyEqual(quarterTurn.Rotation, System.MathF.PI * 0.5f),
        "Rotation construction and point transformation must use clockwise screen-space columns.");

    var decomposed = new Transform(0.4f, new Vector2(2f, -3f), 0.2f, new Vector2(5f, 6f));
    Require(NearlyEqual(decomposed.Rotation, 0.4f) &&
            VectorNearlyEqual(decomposed.Scale, new Vector2(2f, -3f)) &&
            NearlyEqual(decomposed.Skew, 0.2f) && decomposed.Origin == new Vector2(5f, 6f) &&
            NearlyEqual(Transform.FlipX.Determinant(), -1f) &&
            default(Transform).Scale == Vector2.Zero && NearlyEqual(default(Transform).Skew, 0f),
        "Rotation, signed scale, skew, origin, and reflection determinant must decompose consistently.");

    var basis = new Transform(new Vector2(2f, 1f), new Vector2(-1f, 3f), new Vector2(100f, 200f));
    Require(basis.BasisXform(new Vector2(4f, 5f)) == new Vector2(3f, 19f) &&
            VectorNearlyEqual(quarterTurn.BasisXformInv(quarterTurn.BasisXform(new Vector2(4f, 5f))), new Vector2(4f, 5f)),
        "Basis transforms must ignore Origin and the inverse shortcut must invert orthonormal bases.");

    var affine = new Transform(0.35f, new Vector2(2f, 3f), 0.25f, new Vector2(4f, -2f));
    var affineInverse = affine.AffineInverse();
    var point = new Vector2(8f, -5f);
    Require(TransformNearlyEqual(affine * affineInverse, Transform.Identity) &&
            TransformNearlyEqual(affineInverse * affine, Transform.Identity) &&
            VectorNearlyEqual(affineInverse * (affine * point), point),
        "AffineInverse must invert rotation, non-uniform scale, skew, and translation.");
    Expect<InvalidOperationException>(
        () => new Transform(Vector2.Right, Vector2.Right, Vector2.Zero).AffineInverse(),
        "AffineInverse must reject an exactly singular basis.");

    var orthonormalInverse = quarterTurn.Inverse();
    Require(VectorNearlyEqual(orthonormalInverse * (quarterTurn * point), point) &&
            VectorNearlyEqual((quarterTurn * point) * quarterTurn, point),
        "Inverse and reverse point multiplication must invert an orthonormal transform.");
    var rectangle = new Rect2(1f, 2f, 3f, 4f);
    var transformedRectangle = quarterTurn * rectangle;
    var scaledRectangle = new Transform(new Vector2(2f, 0f), new Vector2(0f, 3f),
        new Vector2(5f, -4f)) * rectangle;
    Require(transformedRectangle.IsEqualApprox(new Rect2(-3f, 5f, 4f, 3f)) &&
            (transformedRectangle * quarterTurn).IsEqualApprox(rectangle) &&
            Transform.Identity * new Rect2(4f, 6f, -3f, -4f) == rectangle &&
            scaledRectangle == new Rect2(7f, 2f, 6f, 12f),
        "Rectangle operators must transform all corners, support orthonormal reversal, and normalize negative sizes.");

    var parent = new Transform(0.6f, new Vector2(4f, 5f));
    var child = new Transform(-0.2f, new Vector2(2f, 3f));
    Require(VectorNearlyEqual((parent * child) * point, parent * (child * point)),
        "Transform multiplication must compose parent and child in application order.");

    var localFrame = new Transform(new Vector2(2f, 0f), new Vector2(0f, 3f), new Vector2(1f, 2f));
    var rotatedGlobal = localFrame.Rotated(System.MathF.PI * 0.5f);
    var rotatedLocal = localFrame.RotatedLocal(System.MathF.PI * 0.5f);
    Require(VectorNearlyEqual(rotatedGlobal.X, new Vector2(0f, 2f)) &&
            VectorNearlyEqual(rotatedGlobal.Y, new Vector2(-3f, 0f)) &&
            VectorNearlyEqual(rotatedGlobal.Origin, new Vector2(-2f, 1f)) &&
            VectorNearlyEqual(rotatedLocal.X, new Vector2(0f, 3f)) &&
            VectorNearlyEqual(rotatedLocal.Y, new Vector2(-2f, 0f)) &&
            rotatedLocal.Origin == localFrame.Origin,
        "Global and local rotation must multiply on opposite sides.");

    var rotatedFrame = new Transform(System.MathF.PI * 0.5f, new Vector2(10f, 20f));
    Require(rotatedFrame.Translated(Vector2.Right).Origin == new Vector2(11f, 20f) &&
            VectorNearlyEqual(rotatedFrame.TranslatedLocal(Vector2.Right).Origin, new Vector2(10f, 21f)),
        "Global and local translation must distinguish world offsets from basis-relative offsets.");
    var scaledGlobal = rotatedFrame.Scaled(new Vector2(2f, 3f));
    var scaledLocal = rotatedFrame.ScaledLocal(new Vector2(2f, 3f));
    Require(VectorNearlyEqual(scaledGlobal.X, new Vector2(0f, 3f)) &&
            VectorNearlyEqual(scaledGlobal.Y, new Vector2(-2f, 0f)) &&
            scaledGlobal.Origin == new Vector2(20f, 60f) &&
            VectorNearlyEqual(scaledLocal.X, new Vector2(0f, 2f)) &&
            VectorNearlyEqual(scaledLocal.Y, new Vector2(-3f, 0f)) &&
            scaledLocal.Origin == rotatedFrame.Origin,
        "Global scale must scale rows and origin while local scale must scale basis columns only.");

    var start = new Transform(170f * System.MathF.PI / 180f, new Vector2(1f, -1f), 0f, Vector2.Zero);
    var finish = new Transform(-170f * System.MathF.PI / 180f, new Vector2(3f, -3f), 0.2f, new Vector2(10f, 20f));
    var midpoint = start.InterpolateWith(finish, 0.5f);
    var extrapolated = start.InterpolateWith(finish, 2f);
    Require(VectorNearlyEqual(midpoint.X, new Vector2(-2f, 0f), 0.001f) &&
            VectorNearlyEqual(midpoint.Scale, new Vector2(2f, -2f), 0.001f) &&
            midpoint.Origin == new Vector2(5f, 10f) &&
            extrapolated.Origin == new Vector2(20f, 40f) &&
            start.InterpolateWith(finish, 0f).IsEqualApprox(start) &&
            start.InterpolateWith(finish, 1f).IsEqualApprox(finish),
        "Interpolation must use the shortest angular path, preserve reflected scale, and allow extrapolation.");

    Require(Transform.Identity.IsConformal() &&
            new Transform(Vector2.One * 2f, new Vector2(-2f, 2f), Vector2.Zero).IsConformal() &&
            Transform.FlipX.IsConformal() &&
            !new Transform(new Vector2(2f, 0f), Vector2.Down, Vector2.Zero).IsConformal() &&
            !new Transform(Vector2.Right, new Vector2(1f, 1f), Vector2.Zero).IsConformal(),
        "Conformal checks must accept uniform rotation/reflection and reject non-uniform scale or skew.");
    Require(Transform.Identity.IsFinite() &&
            !new Transform(new Vector2(float.NaN, 0f), Vector2.Down, Vector2.Zero).IsFinite() &&
            !new Transform(Vector2.Right, Vector2.Down, new Vector2(float.PositiveInfinity, 0f)).IsFinite(),
        "IsFinite must inspect every basis and origin component.");

    var orthonormalized = basis.Orthonormalized();
    var zeroOrthonormalized = default(Transform).Orthonormalized();
    Require(VectorNearlyEqual(orthonormalized.X, new Vector2(0.8944272f, 0.4472136f)) &&
            NearlyEqual(orthonormalized.X.Dot(orthonormalized.Y), 0f) &&
            NearlyEqual(orthonormalized.X.Length(), 1f) && NearlyEqual(orthonormalized.Y.Length(), 1f) &&
            orthonormalized.Origin == basis.Origin && zeroOrthonormalized == default,
        "Orthonormalized must preserve Origin and keep degenerate zero axes finite.");

    var lookingDown = Transform.Identity.LookingAt(Vector2.Down);
    var lookingScaled = affine.LookingAt(new Vector2(9f, 3f));
    Require(NearlyEqual(lookingDown.Rotation, System.MathF.PI * 0.5f) && lookingDown.Origin == Vector2.Zero &&
            Transform.Identity.LookingAt() == Transform.Identity.LookingAt(Vector2.Zero) &&
            VectorNearlyEqual(lookingDown.Scale, Vector2.One) &&
            lookingScaled.Origin == affine.Origin && VectorNearlyEqual(lookingScaled.Scale, Vector2.One) &&
            NearlyEqual(lookingScaled.Skew, 0f) && NearlyEqual(lookingScaled.Rotation, 0.7553597f),
        "LookingAt must use affine-local scale compensation while preserving Origin and removing scale and skew.");
    Expect<InvalidOperationException>(() => default(Transform).LookingAt(Vector2.One),
        "LookingAt must surface a singular source basis.");

    var sourcePoints = new[] { Vector2.Zero, Vector2.Right, new Vector2(2f, -3f) };
    var transformedPoints = quarterTurn * sourcePoints;
    var restoredPoints = transformedPoints * quarterTurn;
    Require(transformedPoints.Length == sourcePoints.Length && restoredPoints.Length == sourcePoints.Length &&
            sourcePoints.Where((source, index) => !VectorNearlyEqual(source, restoredPoints[index])).Count() == 0 &&
            (Transform.Identity * Array.Empty<Vector2>()).Length == 0,
        "Array operators must return complete transformed copies in source order.");
    Vector2[] nullPoints = null!;
    Expect<ArgumentNullException>(() => _ = Transform.Identity * nullPoints,
        "Forward array transformation must reject null explicitly.");
    Expect<ArgumentNullException>(() => _ = nullPoints * Transform.Identity,
        "Inverse array transformation must reject null explicitly.");

    var scalar = new Transform(1f, 2f, 3f, 4f, 5f, 6f);
    Require((scalar * 2f) / 2f == scalar && (scalar * 2) / 2 == scalar &&
            !(scalar / 0f).IsFinite(),
        "Scalar arithmetic must affect every component and retain IEEE division behavior.");
    var approximate = new Transform(1.000001f, 0f, 0f, 1f, 0f, 0f);
    var nanTransform = new Transform(new Vector2(float.NaN, 0f), Vector2.Down, Vector2.Zero);
    var signedZeroTransform = new Transform(-0f, 0f, 0f, -0f, 0f, -0f);
    Require(Transform.Identity == new Transform(1f, 0f, 0f, 1f, 0f, 0f) &&
            Transform.Identity != approximate && Transform.Identity.IsEqualApprox(approximate) &&
            new Transform(new Vector2(float.PositiveInfinity, 0f), Vector2.Down, Vector2.Zero).IsEqualApprox(
                new Transform(new Vector2(float.PositiveInfinity, 0f), Vector2.Down, Vector2.Zero)) &&
            nanTransform != new Transform(new Vector2(float.NaN, 0f), Vector2.Down, Vector2.Zero) &&
            !nanTransform.IsEqualApprox(new Transform(new Vector2(float.NaN, 0f), Vector2.Down, Vector2.Zero)) &&
            signedZeroTransform == default && signedZeroTransform.GetHashCode() == default(Transform).GetHashCode() &&
            scalar.Equals((object)new Transform(1f, 2f, 3f, 4f, 5f, 6f)) &&
            scalar.GetHashCode() == new Transform(1f, 2f, 3f, 4f, 5f, 6f).GetHashCode(),
        "Exact and approximate equality must define finite, infinity, and NaN behavior.");

    var previousCulture = CultureInfo.CurrentCulture;
    try
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        Require(new Transform(1.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f).ToString("F1") ==
                "[X: (1.5, 2.5), Y: (3.5, 4.5), O: (5.5, 6.5)]",
            "Transform formatting must use invariant culture.");
        Expect<FormatException>(() => _ = scalar.ToString("Q"),
            "Transform formatting must surface invalid numeric formats.");
    }
    finally
    {
        CultureInfo.CurrentCulture = previousCulture;
    }

    var transformKey = new ConfigKey<Transform>("geometry", "transform");
    using (var config = new ConfigFile())
    {
        config.SetValue(transformKey, scalar);
        Require(config.EncodeToText() ==
                "[geometry]\n\ntransform={\"X\":{\"X\":1,\"Y\":2},\"Y\":{\"X\":3,\"Y\":4},\"Origin\":{\"X\":5,\"Y\":6}}\n" &&
                config.GetValue(transformKey) == scalar,
            "ConfigFile must use the stable finite X/Y/Origin transform schema.");
        Expect<JsonException>(() => config.SetValue(
                transformKey,
                new Transform(new Vector2(float.NaN, 0f), Vector2.Down, Vector2.Zero)),
            "ConfigFile must reject non-finite transform components before mutation.");
        Require(config.GetValue(transformKey) == scalar,
            "Failed transform serialization must preserve the prior configuration token.");

        config.Parse("[geometry]\ntransform={\"X\":{\"X\":1,\"Y\":2},\"Y\":{\"X\":3,\"Y\":4}}\n");
        Expect<InvalidDataException>(() => config.GetValue(transformKey),
            "ConfigFile must reject a transform with a missing field.");
        config.Parse("[geometry]\ntransform={\"X\":{\"X\":1,\"Y\":2},\"Y\":{\"X\":3,\"Y\":4},\"Origin\":{\"X\":5,\"Y\":6},\"Extra\":0}\n");
        Expect<InvalidDataException>(() => config.GetValue(transformKey),
            "ConfigFile must reject unknown transform fields.");
        config.Parse("[geometry]\ntransform={\"X\":{\"X\":1,\"X\":2,\"Y\":2},\"Y\":{\"X\":3,\"Y\":4},\"Origin\":{\"X\":5,\"Y\":6}}\n");
        Expect<InvalidDataException>(() => config.GetValue(transformKey),
            "ConfigFile must reject duplicate transform vector components.");
        config.Parse("[geometry]\ntransform={\"X\":{\"X\":1,\"Y\":2},\"X\":{\"X\":1,\"Y\":2},\"Y\":{\"X\":3,\"Y\":4},\"Origin\":{\"X\":5,\"Y\":6}}\n");
        Expect<InvalidDataException>(() => config.GetValue(transformKey),
            "ConfigFile must reject duplicate transform fields.");
        config.Parse("[geometry]\ntransform={\"X\":{\"X\":1},\"Y\":{\"X\":3,\"Y\":4},\"Origin\":{\"X\":5,\"Y\":6}}\n");
        Expect<InvalidDataException>(() => config.GetValue(transformKey),
            "ConfigFile must reject incomplete transform vectors.");
        config.Parse("[geometry]\ntransform={\"X\":{\"X\":\"right\",\"Y\":2},\"Y\":{\"X\":3,\"Y\":4},\"Origin\":{\"X\":5,\"Y\":6}}\n");
        Expect<InvalidDataException>(() => config.GetValue(transformKey),
            "ConfigFile must reject nonnumeric transform vector components.");
        config.Parse("[geometry]\ntransform={\"X\":{\"X\":1,\"Y\":2,\"Z\":3},\"Y\":{\"X\":3,\"Y\":4},\"Origin\":{\"X\":5,\"Y\":6}}\n");
        Expect<InvalidDataException>(() => config.GetValue(transformKey),
            "ConfigFile must reject unknown transform vector components.");
        config.Parse("[geometry]\ntransform={\"X\":{\"X\":1e100,\"Y\":2},\"Y\":{\"X\":3,\"Y\":4},\"Origin\":{\"X\":5,\"Y\":6}}\n");
        Expect<InvalidDataException>(() => config.GetValue(transformKey),
            "ConfigFile must reject transform numbers outside the finite single-precision range.");
    }

    using (var scene = new PackedScene())
    {
        var source = new ColorPackedNode { Name = "TransformRoot", PackedTransform = affine };
        scene.Pack(source);
        source.Dispose();
        using var instance = (ColorPackedNode)scene.Instantiate();
        Require(instance.PackedTransform == affine,
            "PackedScene must preserve stored Transform properties.");
    }

    _ = ExerciseTransformHotPath(32);
    var beforeAllocations = GC.GetAllocatedBytesForCurrentThread();
    var hotResult = ExerciseTransformHotPath(10_000);
    var allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocations;
    Require(allocated == 0 && hotResult.IsFinite(),
        "Warmed transform math operations must not allocate managed memory.");
}

static Transform ExerciseTransformHotPath(int iterations)
{
    var value = new Transform(0.1f, new Vector2(1.2f, 0.8f), 0.05f, new Vector2(2f, 3f));
    for (var index = 0; index < iterations; index++)
    {
        value = value.RotatedLocal(0.00001f).TranslatedLocal(new Vector2(0.00001f, -0.00001f));
        value = value.AffineInverse().AffineInverse();
    }

    return value;
}

static bool TransformNearlyEqual(Transform left, Transform right, float epsilon = 0.0001f) =>
    VectorNearlyEqual(left.X, right.X, epsilon) && VectorNearlyEqual(left.Y, right.Y, epsilon) &&
    VectorNearlyEqual(left.Origin, right.Origin, epsilon);

static void VerifyConfigText()
{
    var root = new ConfigKey<int>(string.Empty, "root");
    var repeated = new ConfigKey<int>("Alpha", "id");
    var empty = new ConfigKey<string>("Alpha", string.Empty);
    var escaped = new ConfigKey<string>("section]=;\"\\\n", "key=;\"\\\n");
    using var config = new ConfigFile();
    config.SetValue(repeated, 1);
    config.SetValue(empty, "empty name");
    config.SetValue(escaped, "line\nbreak;=]");
    config.SetValue(root, 2);
    var encoded = config.EncodeToText();
    Require(encoded.StartsWith("root=2\n\n[Alpha]\n\n", StringComparison.Ordinal) &&
            encoded.Contains("\"\"=\"empty name\"", StringComparison.Ordinal) &&
            encoded.Contains("[\"section", StringComparison.Ordinal) &&
            !encoded.Contains('\r'),
        "Text encoding must put sectionless entries first, quote unsafe names and normalize line endings.");

    using var decoded = new ConfigFile();
    decoded.Parse("\uFEFF; ignored\r\n" + encoded.Replace("\n", "\r\n", StringComparison.Ordinal));
    Require(decoded.GetValue(root) == 2 && decoded.GetValue(repeated) == 1 &&
            decoded.GetValue(empty) == "empty name" && decoded.GetValue(escaped) == "line\nbreak;=]" &&
            decoded.EncodeToText() == encoded,
        "Parsing must discard comments and BOM while preserving complex names, values and stable encoding.");

    decoded.Parse("; ignored\n[Alpha]\nid=3\n[Alpha]\nid=4\nnew=5\n");
    Require(decoded.GetValue(repeated) == 4 && decoded.GetValue(empty) == "empty name" &&
            decoded.GetValue(new ConfigKey<int>("Alpha", "new")) == 5 &&
            decoded.GetSections().SequenceEqual([string.Empty, "Alpha", escaped.Section]) &&
            decoded.GetSectionKeys("Alpha").SequenceEqual(["id", string.Empty, "new"]),
        "Repeated sections and keys merge last values without reordering surviving entries.");
    var beforeFailure = decoded.EncodeToText();
    foreach (var invalid in new[]
             {
                 "fresh=1\n[unclosed", "fresh=1\nnot-an-assignment", "fresh=1\n\"bad=name=1",
                 "fresh=1\nname=", "fresh=1\nname={",
             })
    {
        Expect<FormatException>(() => decoded.Parse(invalid),
            "Malformed headers, identifiers, assignments and JSON values must fail.");
        Require(decoded.EncodeToText() == beforeFailure &&
                !decoded.HasSectionKey(new ConfigKey<int>(string.Empty, "fresh")),
            "A later text error must leave the entire prior document unchanged.");
    }
    decoded.Parse(string.Empty);
    Require(decoded.EncodeToText() == beforeFailure,
        "Parsing an empty document must merge no entries and preserve current state.");
}

static void VerifyConfigFiles()
{
    VerifyConfigText();
    var rootKey = new ConfigKey<int>(string.Empty, "root");
    var answerKey = new ConfigKey<int>("gameplay", "answer");
    var enabledKey = new ConfigKey<bool>("gameplay", "enabled");
    var nameKey = new ConfigKey<string>("display settings", "player=name");
    var positionKey = new ConfigKey<Vector2>("gameplay", "position");
    var numbersKey = new ConfigKey<List<int>>("gameplay", "numbers");
    var profileKey = new ConfigKey<ConfigProfile>("gameplay", "profile");
    var nullableKey = new ConfigKey<string>("gameplay", "nullable");
    var missingKey = new ConfigKey<int>("missing", "value");

    Expect<ArgumentNullException>(() => new ConfigKey<int>(null!, "key"),
        "Configuration key sections must reject null.");
    Expect<ArgumentNullException>(() => new ConfigKey<int>("section", null!),
        "Configuration key names must reject null.");
    var emptyNameKey = new ConfigKey<int>("state", string.Empty);
    Expect<NotSupportedException>(() => new ConfigKey<object>("section", "untyped"),
        "Configuration keys must reject object as an implicit universal value.");
    Expect<NotSupportedException>(() => new ConfigKey<List<object>>("section", "nested-untyped"),
        "Configuration keys must reject universal values nested in typed containers.");
    Expect<NotSupportedException>(() => new ConfigKey<TestObject>("section", "engine_object"),
        "Configuration keys must reject engine objects.");
    Require(rootKey.ToString() == "root" && answerKey.ToString() == "gameplay/answer",
        "Configuration keys must expose stable diagnostic paths.");

    using (var state = new ConfigFile())
    {
        var first = new ConfigKey<int>("state", "first");
        var second = new ConfigKey<int>("state", "second");
        var tail = new ConfigKey<int>("tail", "value");
        var root = new ConfigKey<int>(string.Empty, "root");
        var absentList = new ConfigKey<List<int>>("absent", "list");
        var fallback = new List<int> { 4 };
        Require(ReferenceEquals(state.GetValue(absentList, fallback), fallback) &&
                !state.HasSection("absent") && !state.HasSectionKey(absentList),
            "A missing typed value returns the caller's fallback without creating a section.");
        state.SetValue(first, 1);
        state.SetValue(second, 2);
        state.SetValue(emptyNameKey, 3);
        state.SetValue(tail, 4);
        state.SetValue(root, 5);
        var sectionsSnapshot = state.GetSections();
        var keysSnapshot = state.GetSectionKeys("state");
        state.SetValue(first, 6);
        Require(state.GetValue(first) == 6 && state.GetValue(emptyNameKey) == 3 &&
                state.HasSectionKey(emptyNameKey) &&
                state.GetSections().SequenceEqual([string.Empty, "state", "tail"]) &&
                state.GetSectionKeys("state").SequenceEqual(["first", "second", string.Empty]),
            "Replacement preserves insertion order and empty names remain addressable.");
        using (var roundTrip = new ConfigFile())
        {
            roundTrip.Parse(state.EncodeToText());
            Require(roundTrip.GetValue(emptyNameKey) == 3 &&
                    roundTrip.GetSectionKeys("state").SequenceEqual(["first", "second", string.Empty]),
                "An empty typed name round-trips through quoted configuration text.");
        }
        state.EraseSectionKey(first);
        state.EraseSectionKey(second);
        state.EraseSectionKey(emptyNameKey);
        Require(!state.HasSection("state") && !state.HasSectionKey(emptyNameKey) &&
                sectionsSnapshot.SequenceEqual([string.Empty, "state", "tail"]) &&
                keysSnapshot.SequenceEqual(["first", "second", string.Empty]),
            "Removing the final entry drops its section without mutating enumeration snapshots.");
        state.SetValue(first, 7);
        Require(state.GetSections().SequenceEqual([string.Empty, "tail", "state"]),
            "Recreating a named section appends it after surviving sections.");
        state.EraseSection("tail");
        state.Clear();
        Require(state.GetSections().Count == 0 && !state.HasSection(string.Empty) &&
                state.EncodeToText() == string.Empty,
            "Clear removes sectionless and named entries without leaving empty sections.");
    }

    using var config = new ConfigFile();
    Require(config.GetSections().Count == 0 && config.EncodeToText().Length == 0,
        "A new configuration must be empty.");
    Expect<ArgumentNullException>(() => config.Parse(null!),
        "Configuration parsing must reject null input.");
    Expect<ArgumentNullException>(() => config.HasSection(null!),
        "Section lookup must reject null names.");
    Expect<ArgumentNullException>(() => config.GetValue<int>(null!),
        "Typed configuration lookup must reject a null key.");
    Expect<ArgumentNullException>(() => config.Save(null!),
        "Configuration saving must reject a null path.");
    Expect<ArgumentException>(() => config.Save(string.Empty),
        "Configuration saving must reject an empty path.");
    Expect<ArgumentNullException>(() => config.SaveEncryptedPass("unused", null!),
        "Password encryption must reject a null password before file access.");
    Require(!config.HasSection("gameplay") && !config.HasSectionKey(answerKey) &&
            !config.TryGetValue(answerKey, out _) && config.GetValue(answerKey, 41) == 41,
        "Missing configuration entries must support lookup, try-get, and fallback semantics.");
    Expect<KeyNotFoundException>(() => config.GetValue(answerKey),
        "Required lookup must reject a missing configuration entry.");
    Expect<KeyNotFoundException>(() => config.GetSectionKeys("missing"),
        "Key enumeration must reject a missing section.");
    Expect<KeyNotFoundException>(() => config.EraseSection("missing"),
        "Section removal must reject a missing section.");
    Expect<KeyNotFoundException>(() => config.EraseSectionKey(missingKey),
        "Entry removal must reject a missing key.");

    var numbers = new List<int> { 1, 2, 3 };
    config.SetValue(answerKey, 42);
    config.SetValue(enabledKey, true);
    config.SetValue(nameKey, "Ada\nLovelace");
    config.SetValue(positionKey, new Vector2(1.5f, -2.25f));
    config.SetValue(numbersKey, numbers);
    config.SetValue(profileKey, new ConfigProfile("pilot", 3));
    config.SetValue(rootKey, 7);
    numbers.Add(4);

    var loadedNumbers = config.GetValue(numbersKey);
    loadedNumbers.Add(99);
    Require(config.GetValue(answerKey) == 42 && config.GetValue(enabledKey) &&
            config.GetValue(nameKey) == "Ada\nLovelace" &&
            VectorNearlyEqual(config.GetValue(positionKey), new Vector2(1.5f, -2.25f)) &&
            config.GetValue(numbersKey).SequenceEqual([1, 2, 3]) &&
            config.GetValue(profileKey) == new ConfigProfile("pilot", 3),
        "Typed configuration values must round-trip and remain independent serialized snapshots.");
    Require(config.TryGetValue(answerKey, out var answer) && answer == 42 &&
            config.GetSections().SequenceEqual([string.Empty, "gameplay", "display settings"]) &&
            config.GetSectionKeys("gameplay").SequenceEqual(["answer", "enabled", "position", "numbers", "profile"]),
        "Configuration enumeration must preserve insertion order and place sectionless entries first.");

    var wrongTypeKey = new ConfigKey<string>("gameplay", "answer");
    Expect<InvalidDataException>(() => config.GetValue(wrongTypeKey),
        "Configuration lookup must reject a stored token that is incompatible with the typed key.");

    config.SetValue(nullableKey, "present");
    config.SetValue(nullableKey, null);
    config.SetValue(nullableKey, null);
    Require(!config.HasSectionKey(nullableKey),
        "Assigning null must remove an entry idempotently.");
    config.EraseSectionKey(enabledKey);
    Require(!config.HasSectionKey(enabledKey) && config.HasSection("gameplay"),
        "Removing one entry must preserve a nonempty section.");

    var cyclicKey = new ConfigKey<CyclicConfigValue>("gameplay", "cycle");
    var cyclic = new CyclicConfigValue();
    cyclic.Next = cyclic;
    Expect<JsonException>(() => config.SetValue(cyclicKey, cyclic),
        "Configuration assignment must surface unsupported cyclic serialization.");
    Require(!config.HasSectionKey(cyclicKey),
        "Failed serialization must not mutate the document.");

    var beforeFailedParse = config.EncodeToText();
    Expect<FormatException>(() => config.Parse("new_value=1\nnot-an-assignment"),
        "Malformed text must fail parsing.");
    Require(config.EncodeToText() == beforeFailedParse && !config.HasSectionKey(new ConfigKey<int>(string.Empty, "new_value")),
        "Parsing must be transactional when a later line is malformed.");

    var removedByParseKey = new ConfigKey<string>("network settings", "remove_me");
    config.SetValue(removedByParseKey, "present");
    config.Parse(
        "\uFEFF; ignored comment\n" +
        "sectionless=11\n" +
        "[\"network settings\"]\n" +
        "\"host=name\"=\"localhost\"\n" +
        "port=7777\n" +
        "remove_me=null\n");
    var sectionlessKey = new ConfigKey<int>(string.Empty, "sectionless");
    var hostKey = new ConfigKey<string>("network settings", "host=name");
    var portKey = new ConfigKey<int>("network settings", "port");
    Require(config.GetValue(sectionlessKey) == 11 && config.GetValue(hostKey) == "localhost" &&
            config.GetValue(portKey) == 7777 && !config.HasSectionKey(removedByParseKey) &&
            config.GetValue(answerKey) == 42,
        "Parsing must support comments, a BOM, quoted identifiers, sectionless entries, null removal, and merge semantics.");

    var escapedSectionKey = new ConfigKey<string>("line\nbreak", "key\nname");
    config.SetValue(escapedSectionKey, "value\nwith\nlines");
    var encoded = config.EncodeToText();
    using (var decoded = new ConfigFile())
    {
        decoded.Parse(encoded);
        Require(decoded.GetValue(escapedSectionKey) == "value\nwith\nlines" && decoded.EncodeToText() == encoded,
            "Encoding must quote unsafe identifiers and produce a stable parseable document.");
    }

    config.EraseSection("network settings");
    Require(!config.HasSection("network settings"),
        "Section removal must remove all of its entries.");

    config.Clear();
    Parallel.For(0, 128, index =>
    {
        var key = new ConfigKey<int>("parallel", $"key-{index}");
        config.SetValue(key, index);
        Require(config.GetValue(key) == index, "Concurrent typed configuration access must retain assigned values.");
    });
    Require(config.GetSectionKeys("parallel").Count == 128,
        "Concurrent configuration writes must not lose entries.");

    config.Clear();
    var secretKey = new ConfigKey<string>("account", "secret");
    config.SetValue(secretKey, "classified-value");
    config.SetValue(answerKey, 42);

    var directory = IOPath.Combine(IOPath.GetTempPath(), $"electron2d-config-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    try
    {
        var plainPath = IOPath.Combine(directory, "settings.cfg");
        config.Save(plainPath);
        config.SetValue(answerKey, 43);
        config.Save(plainPath);
        using (var loaded = new ConfigFile())
        {
            loaded.SetValue(rootKey, 9);
            loaded.Load(plainPath);
            Require(loaded.GetValue(secretKey) == "classified-value" && loaded.GetValue(answerKey) == 43 &&
                    loaded.GetValue(rootKey) == 9,
                "Plain saves must replace an existing destination, and loading must merge without clearing unrelated entries.");
        }

        config.SetValue(answerKey, 42);
        config.Save(plainPath);

        var savedBytes = File.ReadAllBytes(plainPath);
        Require(!savedBytes.AsSpan().StartsWith(Encoding.UTF8.Preamble) &&
                Encoding.UTF8.GetString(savedBytes) == config.EncodeToText(),
            "Plain saves must use UTF-8 without a BOM and replace the destination with the encoded snapshot.");

        var invalidUtf8Path = IOPath.Combine(directory, "invalid-utf8.cfg");
        File.WriteAllBytes(invalidUtf8Path, [0xff]);
        var beforeInvalidLoad = config.EncodeToText();
        Expect<DecoderFallbackException>(() => config.Load(invalidUtf8Path),
            "Plain loading must reject invalid UTF-8.");
        Require(config.EncodeToText() == beforeInvalidLoad,
            "A failed plain load must preserve existing state.");

        var bomPath = IOPath.Combine(directory, "bom-crlf.cfg");
        File.WriteAllText(bomPath, "\uFEFF; comment\r\nloaded=7\r\n", new UTF8Encoding(false));
        using (var loaded = new ConfigFile())
        {
            loaded.SetValue(answerKey, 99);
            loaded.Load(bomPath);
            Require(loaded.GetValue(new ConfigKey<int>(string.Empty, "loaded")) == 7 &&
                    loaded.GetValue(answerKey) == 99 &&
                    loaded.EncodeToText() == "loaded=7\n\n[gameplay]\n\nanswer=99\n",
                "Plain load must accept a UTF-8 BOM and CRLF while merging prior entries and normalizing text.");
        }

        var malformedPath = IOPath.Combine(directory, "malformed.cfg");
        File.WriteAllText(malformedPath, "fresh=1\nnot-an-assignment", new UTF8Encoding(false));
        Expect<FormatException>(() => config.Load(malformedPath),
            "A malformed plain file must fail after UTF-8 decoding.");
        Expect<FileNotFoundException>(() => config.Load(IOPath.Combine(directory, "absent.cfg")),
            "Plain load must report a missing operating-system file.");
        Require(config.EncodeToText() == beforeInvalidLoad &&
                !config.HasSectionKey(new ConfigKey<int>(string.Empty, "fresh")),
            "Malformed and missing files must leave all prior entries unchanged.");

        using (var empty = new ConfigFile())
        {
            var emptyPath = IOPath.Combine(directory, "empty.cfg");
            empty.Save(emptyPath);
            Require(File.ReadAllBytes(emptyPath).Length == 0,
                "Saving an empty document must create an empty UTF-8 file.");
        }

        Expect<DirectoryNotFoundException>(() => config.Save(IOPath.Combine(directory, "missing", "settings.cfg")),
            "Saving must not create an absent destination directory implicitly.");
        var directoryTarget = IOPath.Combine(directory, "directory-target");
        Directory.CreateDirectory(directoryTarget);
        Expect<IOException>(() => config.Save(directoryTarget),
            "Atomic saving must surface a destination that cannot be replaced by a file.");
        Require(!Directory.EnumerateFiles(directory, "*.tmp", SearchOption.TopDirectoryOnly).Any(),
            "A failed atomic replacement must clean up its temporary file.");

        var rawKey = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        var rawEncryptedPath = IOPath.Combine(directory, "settings-key.bin");
        config.SaveEncrypted(rawEncryptedPath, rawKey);
        var encryptedBytes = File.ReadAllBytes(rawEncryptedPath);
        Require(encryptedBytes.AsSpan().StartsWith("E2DCFG"u8) &&
                encryptedBytes[6] == 1 && encryptedBytes[7] == 0 && encryptedBytes[8] == 0 &&
                encryptedBytes.Length >= 37 &&
                encryptedBytes.AsSpan().IndexOf(Encoding.UTF8.GetBytes("classified-value")) < 0,
            "Raw-key saves must use the versioned authenticated header without exposing plaintext.");
        var secondRawPath = IOPath.Combine(directory, "settings-key-2.bin");
        config.SaveEncrypted(secondRawPath, rawKey);
        Require(!encryptedBytes.AsSpan(9, 12).SequenceEqual(File.ReadAllBytes(secondRawPath).AsSpan(9, 12)),
            "Raw-key saves must generate an independent nonce even when the key and document are unchanged.");

        using (var loaded = new ConfigFile())
        {
            loaded.LoadEncrypted(rawEncryptedPath, rawKey);
            Require(loaded.GetValue(secretKey) == "classified-value" && loaded.GetValue(answerKey) == 42,
                "Raw-key encrypted configurations must authenticate, decrypt, parse, and merge.");
        }

        var wrongRawKey = Enumerable.Repeat((byte)0xff, 32).ToArray();
        using (var wrongKeyConfig = new ConfigFile())
        {
            Expect<CryptographicException>(() => wrongKeyConfig.LoadEncrypted(rawEncryptedPath, wrongRawKey),
                "Raw-key loading must reject a wrong key.");
            Require(wrongKeyConfig.GetSections().Count == 0,
                "Authentication failure must not mutate the destination configuration.");
        }

        var tamperedPath = IOPath.Combine(directory, "settings-tampered.bin");
        var tamperedBytes = (byte[])encryptedBytes.Clone();
        tamperedBytes[^1] ^= 0x01;
        File.WriteAllBytes(tamperedPath, tamperedBytes);
        using (var tampered = new ConfigFile())
        {
            Expect<CryptographicException>(() => tampered.LoadEncrypted(tamperedPath, rawKey),
                "Authenticated loading must reject modified ciphertext.");
        }
        tamperedBytes = (byte[])encryptedBytes.Clone();
        tamperedBytes[21] ^= 0x01;
        File.WriteAllBytes(tamperedPath, tamperedBytes);
        using (var tamperedTag = new ConfigFile())
        {
            Expect<CryptographicException>(() => tamperedTag.LoadEncrypted(tamperedPath, rawKey),
                "Authenticated loading must reject a modified tag.");
        }

        var malformedHeaderPath = IOPath.Combine(directory, "settings-bad-header.bin");
        var malformedHeader = (byte[])encryptedBytes.Clone();
        malformedHeader[6] = 2;
        File.WriteAllBytes(malformedHeaderPath, malformedHeader);
        Expect<InvalidDataException>(() => config.LoadEncrypted(malformedHeaderPath, rawKey),
            "Unsupported encrypted-envelope versions must fail before decryption.");
        malformedHeader[6] = 1;
        malformedHeader[8] = 1;
        File.WriteAllBytes(malformedHeaderPath, malformedHeader);
        Expect<InvalidDataException>(() => config.LoadEncrypted(malformedHeaderPath, rawKey),
            "Raw-key envelopes must reject an unexpected salt length.");
        File.WriteAllBytes(malformedHeaderPath, encryptedBytes[..20]);
        Expect<InvalidDataException>(() => config.LoadEncrypted(malformedHeaderPath, rawKey),
            "Truncated authenticated headers must fail before state changes.");

        byte[] AuthenticatedRawEnvelope(byte[] content)
        {
            const int headerLength = 21;
            const int tagLength = 16;
            var fixture = new byte[headerLength + tagLength + content.Length];
            encryptedBytes.AsSpan(0, headerLength).CopyTo(fixture);
            System.Security.Cryptography.RandomNumberGenerator.Fill(fixture.AsSpan(9, 12));
            using var cipher = new AesGcm(rawKey, tagLength);
            cipher.Encrypt(fixture.AsSpan(9, 12), content,
                fixture.AsSpan(headerLength + tagLength, content.Length),
                fixture.AsSpan(headerLength, tagLength), fixture.AsSpan(0, headerLength));
            return fixture;
        }

        var authenticatedBadTextPath = IOPath.Combine(directory, "settings-authenticated-bad-text.bin");
        File.WriteAllBytes(authenticatedBadTextPath, AuthenticatedRawEnvelope("fresh=1\nnot-an-assignment"u8.ToArray()));
        var beforeAuthenticatedFailure = config.EncodeToText();
        Expect<FormatException>(() => config.LoadEncrypted(authenticatedBadTextPath, rawKey),
            "Authenticated but malformed text must fail parsing after decryption.");
        File.WriteAllBytes(authenticatedBadTextPath, AuthenticatedRawEnvelope([0xff]));
        Expect<InvalidDataException>(() => config.LoadEncrypted(authenticatedBadTextPath, rawKey),
            "Authenticated but invalid UTF-8 must fail decoding after decryption.");
        Require(config.EncodeToText() == beforeAuthenticatedFailure &&
                !config.HasSectionKey(new ConfigKey<int>(string.Empty, "fresh")),
            "Authenticated plaintext errors must not publish partial configuration state.");

        Expect<ArgumentException>(() => config.SaveEncrypted(rawEncryptedPath, new byte[31]),
            "Raw-key encryption must require exactly 256 key bits.");
        Expect<ArgumentException>(() => config.LoadEncrypted(rawEncryptedPath, new byte[31]),
            "Raw-key decryption must require exactly 256 key bits before file access.");

        var passwordPath = IOPath.Combine(directory, "settings-password.bin");
        var secondPasswordPath = IOPath.Combine(directory, "settings-password-2.bin");
        config.SaveEncryptedPass(passwordPath, "correct horse battery staple");
        config.SaveEncryptedPass(secondPasswordPath, "correct horse battery staple");
        var passwordEnvelope = File.ReadAllBytes(passwordPath);
        var secondPasswordEnvelope = File.ReadAllBytes(secondPasswordPath);
        Require(passwordEnvelope.AsSpan().StartsWith("E2DCFG"u8) &&
                passwordEnvelope[6] == 1 && passwordEnvelope[7] == 1 && passwordEnvelope[8] == 16 &&
                !passwordEnvelope.AsSpan(9, 16).SequenceEqual(secondPasswordEnvelope.AsSpan(9, 16)) &&
                !passwordEnvelope.AsSpan(25, 12).SequenceEqual(secondPasswordEnvelope.AsSpan(25, 12)),
            "Password saves must use the versioned mode with fresh salt and nonce material.");
        var independentlyDerived = Rfc2898DeriveBytes.Pbkdf2(
            "correct horse battery staple", passwordEnvelope.AsSpan(9, 16), 600_000,
            HashAlgorithmName.SHA256, 32);
        var independentlyDecoded = new byte[passwordEnvelope.Length - 53];
        try
        {
            using var cipher = new AesGcm(independentlyDerived, 16);
            cipher.Decrypt(passwordEnvelope.AsSpan(25, 12), passwordEnvelope.AsSpan(53),
                passwordEnvelope.AsSpan(37, 16), independentlyDecoded, passwordEnvelope.AsSpan(0, 37));
            Require(Encoding.UTF8.GetString(independentlyDecoded) == config.EncodeToText(),
                "An independent PBKDF2/AES-GCM decoder must recover the saved text from the documented envelope.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(independentlyDecoded);
            CryptographicOperations.ZeroMemory(independentlyDerived);
        }

        using (var loaded = new ConfigFile())
        {
            loaded.LoadEncryptedPass(passwordPath, "correct horse battery staple");
            Require(loaded.GetValue(secretKey) == "classified-value",
                "Password-encrypted configurations must derive, authenticate, decrypt, and parse successfully.");
        }

        using (var wrongPassword = new ConfigFile())
        {
            Expect<CryptographicException>(() => wrongPassword.LoadEncryptedPass(passwordPath, "wrong password"),
                "Password-encrypted loading must reject a wrong password.");
            Expect<InvalidDataException>(() => wrongPassword.LoadEncrypted(passwordPath, rawKey),
                "Raw-key loading must reject a password-mode envelope.");
            Expect<InvalidDataException>(() => wrongPassword.LoadEncryptedPass(rawEncryptedPath, "password"),
                "Password loading must reject a raw-key envelope.");
        }

        var tamperedPasswordPath = IOPath.Combine(directory, "settings-password-tampered.bin");
        var tamperedPassword = (byte[])passwordEnvelope.Clone();
        tamperedPassword[9] ^= 0x01;
        File.WriteAllBytes(tamperedPasswordPath, tamperedPassword);
        Expect<CryptographicException>(() => config.LoadEncryptedPass(tamperedPasswordPath, "correct horse battery staple"),
            "A changed authenticated salt must invalidate a password envelope.");
        var invalidPasswordHeader = (byte[])passwordEnvelope.Clone();
        invalidPasswordHeader[8] = 15;
        File.WriteAllBytes(tamperedPasswordPath, invalidPasswordHeader);
        Expect<InvalidDataException>(() => config.LoadEncryptedPass(tamperedPasswordPath, "correct horse battery staple"),
            "An invalid password salt length must fail before key derivation.");

        Expect<ArgumentException>(() => config.SaveEncryptedPass(passwordPath, string.Empty),
            "Password encryption must reject an empty password.");
        Expect<ArgumentException>(() => config.LoadEncryptedPass(passwordPath, string.Empty),
            "Password decryption must reject an empty password before file access.");
        Expect<DirectoryNotFoundException>(() => config.SaveEncryptedPass(
            IOPath.Combine(directory, "missing", "encrypted.cfg"), "correct horse battery staple"),
            "Failed encrypted replacement must report an absent directory after clearing owned buffers.");

        var malformedEnvelopePath = IOPath.Combine(directory, "malformed.bin");
        File.WriteAllBytes(malformedEnvelopePath, [1, 2, 3]);
        using (var malformed = new ConfigFile())
        {
            Expect<InvalidDataException>(() => malformed.LoadEncrypted(malformedEnvelopePath, rawKey),
                "Encrypted loading must reject a malformed envelope before mutation.");
        }

        Require(!Directory.EnumerateFiles(directory, "*.tmp", SearchOption.TopDirectoryOnly).Any(),
            "Successful and failed atomic saves must not leave temporary files.");
    }
    finally
    {
        Directory.Delete(directory, recursive: true);
    }

    var concurrentDispose = new ConfigFile();
    var concurrentDisposeKey = new ConfigKey<int>("state", "value");
    Exception? concurrentDisposeError = null;
    using (var start = new Barrier(2))
    {
        Parallel.Invoke(
            () =>
            {
                start.SignalAndWait();
                for (var index = 0; index < 10_000; index++)
                {
                    try
                    {
                        concurrentDispose.SetValue(concurrentDisposeKey, index);
                        _ = concurrentDispose.GetValue(concurrentDisposeKey);
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                    catch (Exception error)
                    {
                        Interlocked.CompareExchange(ref concurrentDisposeError, error, null);
                        return;
                    }
                }
            },
            () =>
            {
                start.SignalAndWait();
                concurrentDispose.Dispose();
            });
    }

    Require(concurrentDispose.IsDisposed && concurrentDisposeError is null,
        "Concurrent configuration access and disposal must terminate with only the documented disposal rejection.");

    config.Dispose();
    Expect<ObjectDisposedException>(config.Clear, "Disposed configurations must reject mutation.");
    Expect<ObjectDisposedException>(() => config.HasSection("section"),
        "Disposed configurations must reject reads.");
    Expect<ObjectDisposedException>(() => config.Save("unused.cfg"),
        "Disposed configurations must reject file operations before touching the path.");
}

static void VerifyFileAccess()
{
    Expect<ArgumentNullException>(() => EngineFileAccess.Open(null!, FileAccessModeFlags.Read),
        "File access must reject null paths.");
    Expect<ArgumentException>(() => EngineFileAccess.Open(string.Empty, FileAccessModeFlags.Read),
        "File access must reject empty paths.");
    Expect<ArgumentOutOfRangeException>(() => EngineFileAccess.Open("unused", (FileAccessModeFlags)5),
        "File access must reject unknown mode combinations.");
    Expect<NotSupportedException>(() => EngineFileAccess.FileExists("uid://123"),
        "Resource-identity paths must remain explicit until their resolver exists.");
    Expect<NotSupportedException>(() => EngineFileAccess.FileExists("pipe://channel"),
        "Pipe paths must remain explicit until their platform backend exists.");
    Expect<ArgumentException>(() => EngineFileAccess.CreateTemp(prefix: "bad/name"),
        "Temporary-file prefixes must reject directory separators.");
    Expect<ArgumentException>(() => EngineFileAccess.CreateTemp(extension: "bad/name"),
        "Temporary-file extensions must reject directory separators.");

    var directory = IOPath.Combine(IOPath.GetTempPath(), $"electron2d-file-access-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    try
    {
        var path = IOPath.Combine(directory, "data.bin");
        Expect<FileNotFoundException>(() => EngineFileAccess.Open(path, FileAccessModeFlags.Read),
            "Read mode must require an existing file.");
        Expect<FileNotFoundException>(() => EngineFileAccess.GetSize(path),
            "Static size lookup must reject a missing file.");
        Expect<FileNotFoundException>(() => EngineFileAccess.GetAccessTime(path),
            "Static access-time lookup must reject a missing file.");
        Expect<FileNotFoundException>(() => EngineFileAccess.GetModifiedTime(path),
            "Static modification-time lookup must reject a missing file.");
        Expect<DirectoryNotFoundException>(() => EngineFileAccess.Open(
                IOPath.Combine(directory, "missing", "data.bin"), FileAccessModeFlags.Write),
            "Write mode must not create missing directories.");

        using (var file = EngineFileAccess.Open(path, FileAccessModeFlags.WriteRead))
        {
            Require(file.IsOpen && file.Path == path && file.AbsolutePath == IOPath.GetFullPath(path) &&
                    file.Position == 0 && file.Length == 0 && !file.EOFReached && !file.BigEndian,
                "A newly truncated file must expose its identity, cursor, length, and byte order.");

            file.WriteByte(0x7f);
            file.WriteUInt16(0x1234);
            file.WriteUInt32(0x12345678);
            file.WriteUInt64(0x0123456789abcdef);
            file.WriteHalf((Half)1.5f);
            file.WriteSingle(-2.25f);
            file.WriteDouble(Math.PI);
            file.WriteReal((float)-Math.E);
            file.BigEndian = true;
            file.WriteUInt16(0xabcd);
            file.WriteUInt32(0x89abcdef);
            file.WriteUInt64(0xfedcba9876543210);
            file.WriteHalf((Half)(-0.5f));
            file.WriteSingle(123.25f);
            file.WriteDouble(-456.5);
            file.WritePascalString("строка");
            file.WriteLine("line\r");
            file.WriteCSVLine(["plain", "with,delimiter", "quote\"value", "two\nlines"]);
            var length = file.Length;
            Require(length > 0 && file.Position == length,
                "Writes must advance the cursor and grow the file.");
            file.Flush();

            file.Seek(0);
            file.BigEndian = false;
            Require(file.ReadByte() == 0x7f && file.ReadUInt16() == 0x1234 &&
                    file.ReadUInt32() == 0x12345678 && file.ReadUInt64() == 0x0123456789abcdef &&
                    file.ReadHalf() == (Half)1.5f && file.ReadSingle() == -2.25f &&
                    file.ReadDouble() == Math.PI && file.ReadReal() == (float)-Math.E,
                "Little-endian numeric values must round-trip exactly.");
            file.BigEndian = true;
            Require(file.ReadUInt16() == 0xabcd && file.ReadUInt32() == 0x89abcdef &&
                    file.ReadUInt64() == 0xfedcba9876543210 && file.ReadHalf() == (Half)(-0.5f) &&
                    file.ReadSingle() == 123.25f && file.ReadDouble() == -456.5,
                "Big-endian numeric values must round-trip exactly.");
            Require(file.ReadPascalString() == "строка" && file.ReadLine() == "line" &&
                    file.ReadCSVLine().SequenceEqual(["plain", "with,delimiter", "quote\"value", "two\nlines"]),
                "Length-prefixed strings, CRLF handling, and quoted multiline CSV fields must round-trip.");
            Require(!file.EOFReached && file.Position == file.Length,
                "Reaching the exact end must not mark EOF before another read is attempted.");
            Expect<EndOfStreamException>(() => file.ReadByte(),
                "A scalar read beyond the end must fail.");
            Require(file.EOFReached, "An incomplete read must mark EOF.");
            file.SeekEnd(-1);
            Require(!file.EOFReached && file.Position == file.Length - 1,
                "A successful seek from the end must clear EOF.");
            var beforeText = file.Position;
            Expect<DecoderFallbackException>(() => file.ReadAllText(),
                "Whole-text reading must reject binary data that is not valid UTF-8.");
            Require(file.Position == beforeText,
                "Whole-text reading must preserve the cursor when decoding fails.");
            file.Resize(length + 3);
            file.SeekEnd(-3);
            Require(file.ReadBytes(3).SequenceEqual(new byte[] { 0, 0, 0 }),
                "Extending a file must fill the new region with zero bytes.");
            file.Resize(length);
        }

        using (var writeOnly = EngineFileAccess.Open(path, FileAccessModeFlags.Write))
        {
            Require(writeOnly.Length == 0, "Write mode must truncate an existing file.");
            Expect<InvalidOperationException>(() => writeOnly.ReadByte(),
                "Write-only files must reject reads.");
            writeOnly.WriteString("abcdef");
        }
        using (var readOnly = EngineFileAccess.Open(path, FileAccessModeFlags.Read))
        {
            Expect<InvalidOperationException>(() => readOnly.WriteByte(1),
                "Read-only files must reject writes.");
            Require(readOnly.ReadString(6) == "abcdef", "Raw UTF-8 strings must round-trip.");
            Require(readOnly.ReadBytes(1).Length == 0 && readOnly.EOFReached,
                "A short buffer read must return available bytes and mark EOF.");
            readOnly.Close();
            readOnly.Close();
            Require(!readOnly.IsOpen, "Closing must be idempotent and publish the closed state.");
            Expect<InvalidOperationException>(() => readOnly.Seek(0),
                "Closed files must reject cursor operations.");
        }

        using (var readWrite = EngineFileAccess.Open(path, FileAccessModeFlags.ReadWrite))
        {
            readWrite.WriteString("XY");
        }
        Require(EngineFileAccess.GetFileAsString(path) == "XYcdef",
            "Read-write mode must preserve length and overwrite from the beginning.");
        Require(EngineFileAccess.FileExists(path) && EngineFileAccess.GetSize(path) == 6 &&
                EngineFileAccess.GetFileAsBytes(path).SequenceEqual(Encoding.UTF8.GetBytes("XYcdef")) &&
                EngineFileAccess.GetAccessTime(path) > 0 && EngineFileAccess.GetModifiedTime(path) > 0,
            "Static file inspection must resolve contents, size, and timestamps.");
        Require(EngineFileAccess.GetMD5(path) == Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes("XYcdef"))).ToLowerInvariant() &&
                EngineFileAccess.GetSHA256(path) == Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("XYcdef"))).ToLowerInvariant(),
            "Static digest helpers must return lowercase MD5 and SHA-256 values.");

        var linePath = IOPath.Combine(directory, "lines.txt");
        File.WriteAllBytes(linePath, Encoding.UTF8.GetBytes("first\rsecond\0third\n"));
        using (var lines = EngineFileAccess.Open(linePath, FileAccessModeFlags.Read))
        {
            Require(lines.ReadLine() == "first" && lines.ReadLine() == "second" && lines.ReadLine() == "third",
                "Line reads must recognize CR, null, and LF terminators.");
            var lineCursor = lines.Position;
            Require(lines.ReadAllText(skipCarriageReturns: true) == "firstsecond\0third\n" &&
                    lines.Position == lineCursor,
                "Whole-text reads must optionally remove CR bytes without changing the cursor.");
        }

        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
        {
            var originalReadOnly = EngineFileAccess.IsReadOnly(path);
            EngineFileAccess.SetReadOnly(path, !originalReadOnly);
            Require(EngineFileAccess.IsReadOnly(path) != originalReadOnly,
                "The read-only attribute must be mutable on supported platforms.");
            EngineFileAccess.SetReadOnly(path, originalReadOnly);
        }
        else
        {
            Expect<PlatformNotSupportedException>(() => EngineFileAccess.IsReadOnly(path),
                "Unsupported platforms must not simulate a read-only file attribute.");
            Expect<PlatformNotSupportedException>(() => EngineFileAccess.IsHidden(path),
                "Unsupported platforms must not simulate a hidden file attribute.");
        }
        if (!OperatingSystem.IsWindows())
        {
            var originalPermissions = EngineFileAccess.GetUnixPermissions(path);
            var restrictedPermissions = UnixPermissionFlags.ReadOwner | UnixPermissionFlags.WriteOwner;
            EngineFileAccess.SetUnixPermissions(path, restrictedPermissions);
            Require(EngineFileAccess.GetUnixPermissions(path) == restrictedPermissions,
                "Unix permission bits must round-trip exactly.");
            EngineFileAccess.SetUnixPermissions(path, originalPermissions);
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsWindows())
        {
            using (var attributes = EngineFileAccess.Open(path, FileAccessModeFlags.ReadWrite))
            {
                if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
                    Require(attributes.ReadOnly == EngineFileAccess.IsReadOnly(path) &&
                            attributes.Hidden == EngineFileAccess.IsHidden(path),
                        "Opened files must expose the current physical attribute state on supported platforms.");
                if (!OperatingSystem.IsWindows())
                    Require(attributes.UnixPermissions == EngineFileAccess.GetUnixPermissions(path),
                        "Opened files must expose current Unix permission bits.");
            }

            const string attributeName = "electron2d.test";
            EngineFileAccess.SetExtendedAttributeString(path, attributeName, "значение");
            Require(EngineFileAccess.GetExtendedAttributeString(path, attributeName) == "значение" &&
                    EngineFileAccess.GetExtendedAttributesList(path).Contains(attributeName, StringComparer.Ordinal),
                "Extended attributes must support text values and enumeration without platform namespace syntax.");
            EngineFileAccess.SetExtendedAttribute(path, attributeName, [0, 1, 2, 255]);
            Require(EngineFileAccess.GetExtendedAttribute(path, attributeName).SequenceEqual(new byte[] { 0, 1, 2, 255 }),
                "Extended attributes must preserve arbitrary bytes.");
            EngineFileAccess.RemoveExtendedAttribute(path, attributeName);
            Require(!EngineFileAccess.GetExtendedAttributesList(path).Contains(attributeName, StringComparer.Ordinal),
                "Extended attributes must be removable.");
            Expect<IOException>(() => EngineFileAccess.GetExtendedAttribute(path, attributeName),
                "Reading a removed extended attribute must fail explicitly.");
        }

        string temporaryPath;
        using (var readOnlyTemporary = EngineFileAccess.CreateTemp(
                   FileAccessModeFlags.Read, prefix: "electron2d", extension: ".dat"))
        {
            temporaryPath = readOnlyTemporary.AbsolutePath;
            Require(temporaryPath.EndsWith(".dat", StringComparison.Ordinal) && readOnlyTemporary.Length == 0,
                "Temporary files must support read-only mode, prefixes, and normalized extensions.");
        }
        Require(!File.Exists(temporaryPath), "Read-only temporary files must be deleted on close.");

        var temporary = EngineFileAccess.CreateTemp();
        temporaryPath = temporary.AbsolutePath;
        temporary.WriteString("temporary");
        temporary.Close();
        Require(!File.Exists(temporaryPath), "Temporary files must be deleted on close by default.");
        using (var keptTemporary = EngineFileAccess.CreateTemp(keep: true))
        {
            temporaryPath = keptTemporary.AbsolutePath;
            keptTemporary.WriteString("kept");
        }
        Require(File.Exists(temporaryPath), "Kept temporary files must survive disposal.");
        File.Delete(temporaryPath);

        foreach (var compression in new[]
                 {
                     FileCompressionMode.Deflate,
                     FileCompressionMode.Gzip,
                     FileCompressionMode.Brotli
                 })
        {
            var compressedPath = IOPath.Combine(directory, $"compressed-{compression}.bin");
            using (var compressed = EngineFileAccess.OpenCompressed(compressedPath, FileAccessModeFlags.WriteRead, compression))
            {
                compressed.WriteLine("compressible compressible compressible");
                compressed.WriteUInt32(123456789);
                compressed.Flush();
                compressed.Seek(0);
                Require(compressed.ReadLine() == "compressible compressible compressible" &&
                        compressed.ReadUInt32() == 123456789,
                    $"{compression} data must remain readable after an intermediate commit.");
            }
            using var decoded = EngineFileAccess.OpenCompressed(compressedPath, FileAccessModeFlags.Read, compression);
            Require(decoded.ReadLine() == "compressible compressible compressible" &&
                    decoded.ReadUInt32() == 123456789,
                $"{compression} containers must round-trip across instances.");
        }
        Expect<InvalidDataException>(() => EngineFileAccess.OpenCompressed(
                IOPath.Combine(directory, "compressed-Deflate.bin"), FileAccessModeFlags.Read, FileCompressionMode.Gzip),
            "Compressed access must reject a container opened with the wrong codec.");
        Expect<NotSupportedException>(() => EngineFileAccess.OpenCompressed(
                IOPath.Combine(directory, "fastlz.bin"), FileAccessModeFlags.Write, FileCompressionMode.FastLz),
            "Unavailable FastLZ support must fail explicitly.");
        Expect<NotSupportedException>(() => EngineFileAccess.OpenCompressed(
                IOPath.Combine(directory, "zstd.bin"), FileAccessModeFlags.Write, FileCompressionMode.Zstandard),
            "Unavailable Zstandard support must fail explicitly.");
        var corruptCompressedPath = IOPath.Combine(directory, "corrupt-compressed.bin");
        File.WriteAllBytes(corruptCompressedPath, [1, 2, 3]);
        Expect<InvalidDataException>(() => EngineFileAccess.OpenCompressed(
                corruptCompressedPath, FileAccessModeFlags.Read, FileCompressionMode.Deflate),
            "Malformed compressed envelopes must be rejected before exposure.");

        var encryptionKey = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        var encryptedPath = IOPath.Combine(directory, "encrypted.bin");
        using (var encrypted = EngineFileAccess.OpenEncrypted(encryptedPath, FileAccessModeFlags.WriteRead, encryptionKey))
        {
            encrypted.WriteLine("secret text");
            encrypted.WriteUInt64(ulong.MaxValue);
            encrypted.Flush();
            encrypted.Seek(0);
            Require(encrypted.ReadLine() == "secret text" && encrypted.ReadUInt64() == ulong.MaxValue,
                "Raw-key encrypted data must remain readable after an intermediate authenticated commit.");
        }
        Require(File.ReadAllBytes(encryptedPath).AsSpan().IndexOf(Encoding.UTF8.GetBytes("secret text")) < 0,
            "Encrypted files must not contain plaintext payloads.");
        using (var encrypted = EngineFileAccess.OpenEncrypted(encryptedPath, FileAccessModeFlags.Read, encryptionKey))
            Require(encrypted.ReadLine() == "secret text" && encrypted.ReadUInt64() == ulong.MaxValue,
                "Raw-key encrypted files must authenticate and round-trip.");
        Expect<ArgumentException>(() => EngineFileAccess.OpenEncrypted(encryptedPath, FileAccessModeFlags.Read, new byte[31]),
            "Raw-key encryption must require exactly 256 key bits.");
        Expect<CryptographicException>(() => EngineFileAccess.OpenEncrypted(
                encryptedPath, FileAccessModeFlags.Read, Enumerable.Repeat((byte)0xff, 32).ToArray()),
            "Encrypted files must reject a wrong raw key.");
        var tamperedEncryptedPath = IOPath.Combine(directory, "tampered-encrypted.bin");
        var tampered = File.ReadAllBytes(encryptedPath);
        tampered[^1] ^= 1;
        File.WriteAllBytes(tamperedEncryptedPath, tampered);
        Expect<CryptographicException>(() => EngineFileAccess.OpenEncrypted(
                tamperedEncryptedPath, FileAccessModeFlags.Read, encryptionKey),
            "Encrypted files must reject modified ciphertext.");

        var passwordPath = IOPath.Combine(directory, "password.bin");
        using (var encrypted = EngineFileAccess.OpenEncryptedWithPassword(
                   passwordPath, FileAccessModeFlags.Write, "correct horse battery staple"))
            encrypted.WriteString("password secret");
        using (var encrypted = EngineFileAccess.OpenEncryptedWithPassword(
                   passwordPath, FileAccessModeFlags.Read, "correct horse battery staple"))
            Require(encrypted.ReadAllText() == "password secret",
                "Password-derived encrypted files must authenticate and round-trip.");
        Expect<InvalidDataException>(() => EngineFileAccess.OpenEncrypted(passwordPath, FileAccessModeFlags.Read, encryptionKey),
            "Encrypted access must reject raw-key/password mode confusion.");
        Expect<CryptographicException>(() => EngineFileAccess.OpenEncryptedWithPassword(
                passwordPath, FileAccessModeFlags.Read, "wrong password"),
            "Password-derived encrypted files must reject a wrong password.");
        Expect<ArgumentException>(() => EngineFileAccess.OpenEncryptedWithPassword(
                passwordPath, FileAccessModeFlags.Read, string.Empty),
            "Password encryption must reject an empty password.");

        var virtualName = $"electron2d-file-access-{Guid.NewGuid():N}.tmp";
        var virtualPath = $"res://{virtualName}";
        var physicalVirtualPath = IOPath.Combine(ProjectSettings.Instance.ProjectRoot, virtualName);
        try
        {
            using (var virtualFile = EngineFileAccess.Open(virtualPath, FileAccessModeFlags.Write))
                virtualFile.WriteString("virtual");
            Require(File.ReadAllText(physicalVirtualPath) == "virtual" && EngineFileAccess.FileExists(virtualPath),
                "Directory-backed project paths must resolve for instance and static access.");
        }
        finally
        {
            File.Delete(physicalVirtualPath);
        }

        Parallel.For(0, 32, _ =>
        {
            Require(EngineFileAccess.GetSHA256(path).Length == 64,
                "Independent static hash operations must be safe concurrently.");
        });

        var concurrentPath = IOPath.Combine(directory, "concurrent.bin");
        using (var concurrent = EngineFileAccess.Open(concurrentPath, FileAccessModeFlags.WriteRead))
        {
            Parallel.For(0, 128, index => concurrent.WritePascalString(index.ToString("D3", CultureInfo.InvariantCulture)));
            Require(concurrent.Length == 128 * 7,
                "Concurrent Pascal writes must keep each prefix and payload in one serialized operation.");
            concurrent.Seek(0);
            var values = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < 128; index++)
                values.Add(concurrent.ReadPascalString());
            Require(values.SetEquals(Enumerable.Range(0, 128).Select(index => index.ToString("D3", CultureInfo.InvariantCulture))),
                "Concurrent compound writes must remain independently readable without interleaving.");
        }

        var failedPath = IOPath.Combine(directory, "absent", "compressed.bin");
        var failedCommit = EngineFileAccess.OpenCompressed(failedPath, FileAccessModeFlags.Write, FileCompressionMode.Deflate);
        failedCommit.WriteString("pending");
        Expect<DirectoryNotFoundException>(failedCommit.Close,
            "A transformed commit must surface a missing destination directory.");
        Require(!failedCommit.IsOpen,
            "A failed close must still release the transformed stream.");
        failedCommit.Dispose();

        var disposed = EngineFileAccess.Open(path, FileAccessModeFlags.Read);
        disposed.Dispose();
        Expect<ObjectDisposedException>(() => disposed.Close(),
            "Disposed file access must reject public operations.");
    }
    finally
    {
        Directory.Delete(directory, recursive: true);
    }
}

static void VerifyDirAccess()
{
    Expect<ArgumentNullException>(() => DirAccess.Open(null!),
        "Directory opening must reject null paths.");
    Expect<ArgumentException>(() => DirAccess.Open(string.Empty),
        "Directory opening must reject empty paths.");
    Expect<DirectoryNotFoundException>(() => DirAccess.Open(IOPath.Combine(IOPath.GetTempPath(), Guid.NewGuid().ToString("N"))),
        "Directory opening must reject missing directories.");
    Expect<NotSupportedException>(() => DirAccess.Open("uid://missing"),
        "Directory opening must reject unknown virtual schemes.");
    Expect<ArgumentException>(() => DirAccess.GetFilesAt("relative"),
        "Static directory operations must reject relative paths.");
    Expect<ArgumentException>(() => DirAccess.CreateTemp("../invalid"),
        "Temporary directory prefixes must reject separators.");

    var root = IOPath.Combine(IOPath.GetTempPath(), $"electron2d-dir-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);
    try
    {
        var alphaPath = IOPath.Combine(root, "alpha.txt");
        var zetaPath = IOPath.Combine(root, "zeta.txt");
        var hiddenPath = IOPath.Combine(root, ".hidden.txt");
        File.WriteAllText(zetaPath, "zeta");
        File.WriteAllText(alphaPath, "alpha");
        File.WriteAllText(hiddenPath, "hidden");
        Directory.CreateDirectory(IOPath.Combine(root, "visible-dir"));
        Directory.CreateDirectory(IOPath.Combine(root, ".hidden-dir"));

        using (var directory = DirAccess.Open(root))
        {
            Require(!directory.IncludeHidden && !directory.IncludeNavigational &&
                    directory.GetCurrentDir() == IOPath.GetFullPath(root) &&
                    !directory.CurrentIsDir() && directory.GetNext().Length == 0,
                "An opened directory must expose default filters, its normalized path, and an inactive listing.");
            Require(directory.FileExists("alpha.txt") && directory.DirExists("visible-dir") &&
                    !directory.FileExists("missing") && !directory.DirExists("missing"),
                "Relative file and directory existence must resolve from the current directory.");

            Require(directory.GetFiles().SequenceEqual(["alpha.txt", "zeta.txt"]) &&
                    directory.GetDirectories().SequenceEqual(["visible-dir"]),
                "Sorted snapshots must exclude hidden and navigational entries by default.");
            directory.IncludeHidden = true;
            directory.IncludeNavigational = true;
            Require(directory.GetFiles().SequenceEqual([".hidden.txt", "alpha.txt", "zeta.txt"]) &&
                    directory.GetDirectories().SequenceEqual([".", "..", ".hidden-dir", "visible-dir"]),
                "Snapshot filters must include hidden and navigational entries when enabled.");

            directory.ListDirBegin();
            var streamed = new Dictionary<string, bool>(StringComparer.Ordinal);
            while (true)
            {
                var name = directory.GetNext();
                if (name.Length == 0)
                    break;
                streamed.Add(name, directory.CurrentIsDir());
            }
            Require(streamed.Count == 7 && streamed["."] && streamed[".."] && streamed["visible-dir"] &&
                    streamed[".hidden-dir"] && !streamed["alpha.txt"] && !directory.CurrentIsDir(),
                "Streaming enumeration must return names, preserve entry kinds, and clear state at EOF.");
            directory.ListDirEnd();
            directory.ListDirEnd();

            directory.IncludeHidden = false;
            directory.IncludeNavigational = false;
            directory.ListDirBegin();
            var concurrentNames = new System.Collections.Concurrent.ConcurrentBag<string>();
            Parallel.For(0, 16, _ =>
            {
                while (true)
                {
                    var name = directory.GetNext();
                    if (name.Length == 0)
                        break;
                    concurrentNames.Add(name);
                }
            });
            Require(concurrentNames.Order(StringComparer.Ordinal).SequenceEqual(
                    new[] { "alpha.txt", "visible-dir", "zeta.txt" }.Order(StringComparer.Ordinal)),
                "Concurrent GetNext calls must consume every visible snapshot entry at most once.");

            directory.MakeDir("single");
            Expect<IOException>(() => directory.MakeDir("single"),
                "Single-directory creation must reject an existing target.");
            Expect<DirectoryNotFoundException>(() => directory.MakeDir("missing/child"),
                "Single-directory creation must require an existing parent.");
            directory.MakeDirRecursive("nested/child");
            directory.MakeDirRecursive("nested/child");
            Require(directory.DirExists("single") && directory.DirExists("nested/child"),
                "Recursive directory creation must create missing parents and be idempotent.");

            directory.ChangeDir("nested/child");
            Require(directory.GetCurrentDir() == IOPath.Combine(root, "nested", "child"),
                "Relative directory changes must update the current native path.");
            directory.ChangeDir("../..");
            Expect<InvalidOperationException>(() => directory.ChangeDir("res://"),
                "A physical accessor must reject a virtual scope switch.");

            directory.Copy("alpha.txt", "copy.txt");
            Require(File.ReadAllText(IOPath.Combine(root, "copy.txt")) == "alpha",
                "File copying must preserve contents.");
            File.WriteAllText(IOPath.Combine(root, "copy.txt"), "old");
            directory.Copy("zeta.txt", "copy.txt");
            Require(File.ReadAllText(IOPath.Combine(root, "copy.txt")) == "zeta",
                "File copying must overwrite an existing destination.");
            if (OperatingSystem.IsWindows())
            {
                Expect<PlatformNotSupportedException>(() =>
                        directory.Copy("alpha.txt", "copy.txt", UnixPermissionFlags.ReadOwner),
                    "Copy permissions must fail explicitly when Unix modes are unavailable.");
            }
            else
            {
                Expect<ArgumentOutOfRangeException>(() =>
                        directory.Copy("alpha.txt", "copy.txt", (UnixPermissionFlags)(1 << 20)),
                    "Copy permissions must reject unknown bits before filesystem mutation.");
            }
            Require(File.ReadAllText(IOPath.Combine(root, "copy.txt")) == "zeta",
                "Invalid copy permissions must not replace the destination.");
            Expect<ArgumentException>(() => directory.Copy("copy.txt", "./copy.txt"),
                "File copying must reject an equivalent source and destination path.");

            File.WriteAllText(IOPath.Combine(root, "rename-source.txt"), "new");
            File.WriteAllText(IOPath.Combine(root, "rename-target.txt"), "old");
            directory.Rename("rename-source.txt", "rename-target.txt");
            Require(!File.Exists(IOPath.Combine(root, "rename-source.txt")) &&
                    File.ReadAllText(IOPath.Combine(root, "rename-target.txt")) == "new",
                "File rename must move and overwrite atomically where the filesystem supports it.");
            Directory.CreateDirectory(IOPath.Combine(root, "rename-dir-source"));
            Directory.CreateDirectory(IOPath.Combine(root, "rename-dir-target"));
            directory.Rename("rename-dir-source", "rename-dir-target");
            Require(Directory.Exists(IOPath.Combine(root, "rename-dir-target")),
                "Directory rename must replace an empty destination directory.");
            Directory.CreateDirectory(IOPath.Combine(root, "rollback-source", "child"));
            Expect<IOException>(() => directory.Rename("rollback-source", "rollback-source/child"),
                "A directory cannot be moved into itself.");
            Require(Directory.Exists(IOPath.Combine(root, "rollback-source", "child")),
                "A failed directory move must restore an empty destination removed for overwrite.");

            File.WriteAllText(IOPath.Combine(root, "single", "child.txt"), "child");
            Expect<IOException>(() => directory.Remove("single"),
                "Directory removal must reject a nonempty directory.");
            directory.Remove("single/child.txt");
            directory.Remove("single");
            Expect<FileNotFoundException>(() => directory.Remove("single"),
                "Removal must reject a missing entry instead of silently succeeding.");

            if (OperatingSystem.IsLinux() || OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            {
                directory.CreateLink("alpha.txt", "alpha-link");
                Require(directory.IsLink("alpha-link") && directory.ReadLink("alpha-link").Length > 0 &&
                        directory.IsEquivalent("alpha.txt", "alpha-link"),
                    "Symbolic links must expose their target and compare equivalent to it.");
                directory.Remove("alpha-link");
                Require(File.Exists(alphaPath), "Removing a symbolic link must preserve its target.");

                directory.CreateLink("visible-dir", "directory-link");
                Require(directory.GetDirectories().Contains("directory-link", StringComparer.Ordinal),
                    "A link to a directory must be classified as a directory during enumeration.");
                directory.Remove("directory-link");
                Require(Directory.Exists(IOPath.Combine(root, "visible-dir")),
                    "Removing a directory link must not traverse or delete its target.");

                File.WriteAllText(IOPath.Combine(root, "dangling-source.txt"), "source");
                directory.CreateLink("dangling-source.txt", "dangling-link");
                File.Delete(IOPath.Combine(root, "dangling-source.txt"));
                Require(directory.IsLink("dangling-link"),
                    "Link detection must recognize a dangling symbolic link.");
                directory.Remove("dangling-link");
                Require(!directory.IsLink("dangling-link"),
                    "A dangling symbolic link must be removable without a target.");

                directory.CreateLink("never-created.txt", "direct-dangling-link");
                Require(directory.IsLink("direct-dangling-link") &&
                        directory.ReadLink("direct-dangling-link") == "never-created.txt",
                    "Link creation must preserve a relative target and allow it to be dangling.");
                directory.Remove("direct-dangling-link");
            }

            if (OperatingSystem.IsLinux())
            {
                var hardLink = IOPath.Combine(root, "alpha-hard-link");
                Require(TestNativeLinks.CreateHardLink(alphaPath, hardLink) == 0 &&
                        directory.IsEquivalent("alpha.txt", "alpha-hard-link"),
                    "Filesystem identity must recognize distinct hard-link names.");
                File.Delete(hardLink);
            }

            var alternateAlpha = IOPath.Combine(root, "ALPHA.TXT");
            var expectedCaseSensitivity = !File.Exists(alternateAlpha);
            Require(directory.IsCaseSensitive("alpha.txt") == expectedCaseSensitivity,
                "Case-sensitivity detection must follow the current directory policy.");
            Require(directory.GetSpaceLeft() >= 0 && directory.GetFilesystemType().Length > 0,
                "Filesystem queries must expose byte capacity and a filesystem identifier.");
            if (OperatingSystem.IsWindows())
            {
                using var extendedPath = DirAccess.Open(@"\\?\" + IOPath.GetFullPath(root));
                Require(extendedPath.GetFilesystemType() != "Network Share",
                    "An extended-length local path must not be classified as a network share.");
            }
            if (!OperatingSystem.IsMacOS())
            {
                Expect<PlatformNotSupportedException>(() => directory.IsBundle("visible-dir"),
                    "Bundle classification must reject unsupported hosts explicitly.");
            }
            if (OperatingSystem.IsLinux() || OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            {
                var driveCount = DirAccess.GetDriveCount();
                var currentDrive = directory.GetCurrentDrive();
                Require(driveCount > 0 && currentDrive >= 0 && currentDrive < driveCount &&
                        DirAccess.GetDriveName(currentDrive).Length > 0,
                    "Drive enumeration must identify the current directory's filesystem root.");
                _ = DirAccess.GetDriveLabel(currentDrive);
                Expect<ArgumentOutOfRangeException>(() => DirAccess.GetDriveName(driveCount),
                    "Drive lookup must reject an invalid index.");
            }

            var absoluteSingle = IOPath.Combine(root, "absolute-single");
            var absoluteRecursive = IOPath.Combine(root, "absolute", "recursive");
            DirAccess.MakeDirAbsolute(absoluteSingle);
            DirAccess.MakeDirRecursiveAbsolute(absoluteRecursive);
            Require(DirAccess.DirExistsAbsolute(absoluteSingle) && DirAccess.DirExistsAbsolute(absoluteRecursive) &&
                    DirAccess.GetFilesAt(root).Contains("alpha.txt", StringComparer.Ordinal) &&
                    DirAccess.GetDirectoriesAt(root).Contains("absolute", StringComparer.Ordinal),
                "Absolute helpers must create, inspect, and list native paths.");
            var absoluteRenameSource = IOPath.Combine(root, "absolute-rename-source.txt");
            var absoluteRenameTarget = IOPath.Combine(root, "absolute-rename-target.txt");
            File.WriteAllText(absoluteRenameSource, "renamed");
            DirAccess.RenameAbsolute(absoluteRenameSource, absoluteRenameTarget);
            Require(!File.Exists(absoluteRenameSource) && File.ReadAllText(absoluteRenameTarget) == "renamed",
                "Absolute rename must move a native file.");
            DirAccess.RemoveAbsolute(absoluteRenameTarget);
            DirAccess.RemoveAbsolute(absoluteSingle);
        }

        var virtualName = $"electron2d-dir-access-{Guid.NewGuid():N}";
        var virtualRoot = IOPath.Combine(ProjectSettings.Instance.ProjectRoot, virtualName);
        Directory.CreateDirectory(virtualRoot);
        try
        {
            File.WriteAllText(IOPath.Combine(virtualRoot, "source.txt"), "virtual");
            using var virtualDirectory = DirAccess.Open($"res://{virtualName}");
            Require(virtualDirectory.GetCurrentDir() == $"res://{virtualName}" &&
                    virtualDirectory.FileExists("source.txt"),
                "Resource-scoped directory access must preserve its virtual prefix.");
            Expect<InvalidOperationException>(() => virtualDirectory.ChangeDir("user://"),
                "Resource-scoped access must reject user-data scope switches.");
            Expect<UnauthorizedAccessException>(() => virtualDirectory.ChangeDir("res://../outside"),
                "Virtual directory changes must reject lexical root traversal.");
            DirAccess.CopyAbsolute($"res://{virtualName}/source.txt", $"res://{virtualName}/copy.txt");
            Require(File.ReadAllText(IOPath.Combine(virtualRoot, "copy.txt")) == "virtual",
                "Absolute virtual copying must resolve both paths through one project-root snapshot.");
        }
        finally
        {
            Directory.Delete(virtualRoot, recursive: true);
        }

        string disposableTempPath;
        var disposableTemp = DirAccess.CreateTemp("electron2d");
        disposableTempPath = disposableTemp.GetCurrentDir();
        disposableTemp.MakeDirRecursive("nested/child");
        File.WriteAllText(IOPath.Combine(disposableTempPath, "nested", "child", "data.txt"), "temporary");
        if (OperatingSystem.IsLinux() || OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            disposableTemp.CreateLink(IOPath.Combine(root, "visible-dir"), "nested/external-link");
        disposableTemp.ChangeDir(IOPath.GetTempPath());
        disposableTemp.Dispose();
        Require(!Directory.Exists(disposableTempPath) && Directory.Exists(IOPath.Combine(root, "visible-dir")),
            "Temporary disposal must delete the captured owned root after directory changes without traversing links.");

        string keptTempPath;
        using (var keptTemp = DirAccess.CreateTemp("electron2d", keep: true))
        {
            keptTempPath = keptTemp.GetCurrentDir();
            keptTemp.MakeDir("kept");
        }
        Require(Directory.Exists(keptTempPath), "Kept temporary directories must survive disposal.");
        Directory.Delete(keptTempPath, recursive: true);

        var disposed = DirAccess.Open(root);
        disposed.Dispose();
        Expect<ObjectDisposedException>(() => disposed.GetFiles(),
            "Disposed directory access must reject public operations.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

static void VerifyProjectSettings()
{
    Expect<ArgumentNullException>(() => new ProjectSetting<int>(null!, 1),
        "Project setting names must reject null.");
    Expect<ArgumentException>(() => new ProjectSetting<int>("missing_category", 1),
        "Project setting names must require a category path.");
    Expect<ArgumentException>(() => new ProjectSetting<int>("invalid/name.debug", 1),
        "Project setting names must reserve periods for feature overrides.");
    Expect<NotSupportedException>(() => new ProjectSetting<object>("invalid/object", new object()),
        "Project settings must reject universal object values.");
    Expect<ArgumentOutOfRangeException>(() => new ProjectSetting<int>("invalid/default", 0, value => value > 0),
        "Project setting validators must accept their default.");

    var root = IOPath.Combine(IOPath.GetTempPath(), $"electron2d-project-{Guid.NewGuid():N}");
    var user = IOPath.Combine(IOPath.GetTempPath(), $"electron2d-user-{Guid.NewGuid():N}");
    var secondRoot = IOPath.Combine(IOPath.GetTempPath(), $"electron2d-project-{Guid.NewGuid():N}");
    var secondUser = IOPath.Combine(IOPath.GetTempPath(), $"electron2d-user-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);
    Directory.CreateDirectory(user);
    Directory.CreateDirectory(secondRoot);
    Directory.CreateDirectory(secondUser);

    try
    {
        Expect<DirectoryNotFoundException>(() => new ProjectSettings(IOPath.Combine(root, "missing"), user),
            "ProjectSettings must reject an absent project root.");
        Expect<ArgumentException>(() => new ProjectSettings(root, root),
            "ProjectSettings must reject identical resource and user roots.");

        using var settings = new ProjectSettings(root, user);
        Require(settings.ProjectRoot == IOPath.GetFullPath(root) && settings.UserDataRoot == IOPath.GetFullPath(user) &&
                settings.ProjectFilePath == IOPath.Combine(root, ProjectSettings.ProjectFileName) &&
                settings.OverrideFilePath == IOPath.Combine(root, ProjectSettings.OverrideFileName) &&
                settings.ProjectDataPath == IOPath.Combine(root, ProjectSettings.ProjectDataDirectoryName),
            "ProjectSettings must expose normalized project paths without creating hidden state.");
        Require(settings.HasSetting(ProjectSettings.ApplicationName) &&
                settings.HasSetting(ProjectSettings.ApplicationVersion) &&
                settings.HasSetting(ProjectSettings.PhysicsTicksPerSecond) &&
                settings.Get(ProjectSettings.PhysicsTicksPerSecond) == 60,
            "Every registry must contain the implemented built-in settings and their defaults.");
        Expect<InvalidOperationException>(() => settings.Unregister(ProjectSettings.ApplicationName),
            "Built-in project settings must not be removable.");

        var score = new ProjectSetting<int>("gameplay/score", 10, value => value > 0);
        var title = new ProjectSetting<string>("application/title", "Untitled");
        var checkpoints = new ProjectSetting<List<int>>("gameplay/checkpoints", [1, 2]);
        var duplicateScore = new ProjectSetting<int>("gameplay/score", 10);
        var propertyListChanges = 0;
        settings.PropertyListChanged += _ => propertyListChanges++;
        settings.Register(score);
        settings.Register(title);
        settings.Register(checkpoints);
        Require(propertyListChanges == 3 && settings.HasSetting(score) && !settings.HasSetting(duplicateScore) &&
                settings.Get(score) == 10 && settings.Get(title) == "Untitled",
            "Registration must publish tooling changes and expose typed defaults only through the exact definition.");
        Expect<InvalidOperationException>(() => settings.Register(duplicateScore),
            "Duplicate setting names must be rejected regardless of definition identity.");
        Expect<InvalidOperationException>(() => settings.Get(duplicateScore),
            "A different definition with the same name must not access a registered setting.");

        var callbackFailureSetting = new ProjectSetting<int>("test/property_callback_failure", 1);
        void ThrowingPropertyListHandler(ElectronObject _) =>
            throw new InvalidOperationException("expected property list failure");
        settings.PropertyListChanged += ThrowingPropertyListHandler;
        Require(Capture(() => settings.Register(callbackFailureSetting)) is InvalidOperationException &&
                settings.HasSetting(callbackFailureSetting),
            "A property-list callback failure must propagate after registration has committed.");
        settings.PropertyListChanged -= ThrowingPropertyListHandler;
        settings.Unregister(callbackFailureSetting);
        settings.Save();
        settings.FlushChanges();

        var firstDefault = checkpoints.DefaultValue;
        firstDefault.Add(3);
        var firstRead = settings.Get(checkpoints);
        firstRead.Add(4);
        Require(checkpoints.DefaultValue.SequenceEqual([1, 2]) && settings.Get(checkpoints).SequenceEqual([1, 2]),
            "Mutable setting defaults and reads must be independent serialized snapshots.");

        var version = settings.Version;
        settings.Set(score, 20);
        settings.Set(score, 20);
        Require(settings.Get(score) == 20 && settings.Version == version + 1 &&
                settings.GetChangedSettings().SequenceEqual([score.Name]) &&
                settings.CheckChangedSettingsInGroup("gameplay/") &&
                !settings.CheckChangedSettingsInGroup("display/"),
            "Setting writes must validate, snapshot, dirty, and version only an actual serialized change.");
        Expect<ArgumentOutOfRangeException>(() => settings.Set(score, 0),
            "Setting validators must reject invalid writes.");
        Require(settings.Get(score) == 20,
            "A rejected setting write must preserve the previous value.");

        settings.SetInitialValue(score, 7);
        Require(settings.Get(score) == 20,
            "Changing a revert value must not change the current setting.");
        var scoreProperty = settings.GetPropertyList()
            .OfType<PropertyDescriptor<ProjectSettings, int>>()
            .Single(property => property.Name == score.Name);
        Require(scoreProperty.TryGetRevertValue(settings, out var initialScore) && initialScore == 7 &&
                settings.PropertyCanRevert(scoreProperty),
            "Registered settings must expose typed tooling descriptors with the current initial value.");
        settings.RevertProperty(scoreProperty);
        Require(settings.Get(score) == 7,
            "Typed property reversion must store the current initial value.");
        settings.Reset(score);
        Require(settings.Get(score) == 7,
            "Reset must retain a non-default initial value as the effective explicit value.");

        settings.SetOrder(score, -10);
        settings.SetAsBasic(score, true);
        settings.SetRestartIfChanged(score, true);
        Require(settings.GetOrder(score) == -10 && settings.IsBasic(score) && settings.IsRestartRequired(score) &&
                settings.GetSettingNames().First() == score.Name,
            "Setting metadata must retain order, basic-view, and restart-required flags.");
        settings.SetAsInternal(title, true);
        Require(settings.IsInternal(title) && !settings.GetSettingNames(includeInternal: false).Contains(title.Name) &&
                settings.GetPropertyList().All(property => property.Name != title.Name),
            "Internal settings must be hidden from ordinary tooling discovery while remaining registered.");
        settings.SetAsInternal(title, false);

        settings.SetFeatureOverride(score, "desktop", 30);
        settings.SetFeatureOverride(score, "portable", 40);
        Require(settings.Get(score) == 7 && settings.GetWithOverride(score, ["portable"]) == 40 &&
                settings.GetWithOverride(score, ["desktop", "portable"]) == 30 &&
                settings.GetFeatureOverrides(score).SequenceEqual(["desktop", "portable"]),
            "Feature overrides must preserve base access, explicit lookup, and deterministic insertion precedence.");
        settings.FlushChanges();
        Require(settings.AddCustomFeature("unmatched") && !settings.FlushChanges() &&
                settings.RemoveCustomFeature("unmatched") && !settings.FlushChanges(),
            "A custom feature that changes no selected override must not queue a settings notification.");
        Require(settings.AddCustomFeature("PORTABLE") && settings.HasFeature("portable") &&
                settings.GetWithOverride(score) == 40 && !settings.AddCustomFeature("portable"),
            "Custom feature tags must be normalized, idempotent, and participate in current-feature lookup.");
        Require(settings.RemoveCustomFeature("portable") && !settings.HasFeature("portable") &&
                settings.GetWithOverride(score) == 7,
            "Removing a custom feature must immediately restore base lookup.");
        Require(settings.ClearFeatureOverride(score, "desktop") &&
                !settings.ClearFeatureOverride(score, "desktop") &&
                settings.GetFeatureOverrides(score).SequenceEqual(["portable"]),
            "Feature override removal must report whether stored state changed.");
        Expect<ArgumentException>(() => settings.AddCustomFeature("bad feature"),
            "Feature tags must reject whitespace.");
        Expect<ArgumentException>(() => settings.AddCustomFeature("bad..feature"),
            "Feature tags must reject empty dotted segments.");
        var activeFeatures = settings.GetActiveFeatures();
#if DEBUG
        const string buildFeature = "debug";
        const string otherFeature = "release";
#else
        const string buildFeature = "release";
        const string otherFeature = "debug";
#endif
        Require(activeFeatures.Contains("dotnet") && activeFeatures.Contains(buildFeature) &&
                !activeFeatures.Contains(otherFeature),
            "Tests must expose the feature tag for their build configuration.");

        var settingsEvents = 0;
        var reenterEvent = true;
        void OnSettingsChanged(ProjectSettings sender)
        {
            settingsEvents++;
            Require(sender.GetChangedSettings().Count > 0,
                "SettingsChanged handlers must observe the batch currently being delivered.");
            if (reenterEvent)
            {
                reenterEvent = false;
                sender.Set(title, "Changed from event");
                Require(sender.GetChangedSettings().Contains(title.Name) && !sender.FlushChanges(),
                    "A handler-created batch must remain visible but cannot be delivered recursively.");
            }
        }

        settings.SettingsChanged += OnSettingsChanged;
        Require(settings.FlushChanges() && settingsEvents == 1 && settings.GetChangedSettings().SequenceEqual([title.Name]) &&
                settings.FlushChanges() && settingsEvents == 2 && settings.GetChangedSettings().Count == 0 &&
                !settings.FlushChanges() && settings.Get(title) == "Changed from event",
            "SettingsChanged must expose and clear each delivered batch while retaining handler-created changes for the next flush.");
        settings.SettingsChanged -= OnSettingsChanged;

        settings.Set(title, "Before failing handler");
        void ThrowingSettingsHandler(ProjectSettings _) => throw new InvalidOperationException("expected settings event failure");
        settings.SettingsChanged += ThrowingSettingsHandler;
        Require(Capture(() => settings.FlushChanges()) is InvalidOperationException && !settings.FlushChanges(),
            "A SettingsChanged handler failure must propagate after consuming that pending invocation.");
        settings.SettingsChanged -= ThrowingSettingsHandler;

        Require(settings.GlobalizePath("res://assets/player.png") == IOPath.Combine(root, "assets", "player.png") &&
                settings.GlobalizePath("user://save/game.json") == IOPath.Combine(user, "save", "game.json") &&
                settings.LocalizePath(IOPath.Combine(root, "assets", "player.png")) == "res://assets/player.png" &&
                settings.LocalizePath(IOPath.Combine(user, "save", "game.json")) == "user://save/game.json",
            "Virtual paths must round-trip against their configured project and user roots.");
        Expect<UnauthorizedAccessException>(() => settings.GlobalizePath("res://../escape.txt"),
            "Project virtual paths must reject parent traversal.");
        Expect<UnauthorizedAccessException>(() => settings.LocalizePath("user://../escape.txt"),
            "Localization must validate already-virtual paths instead of preserving traversal.");
        Expect<NotSupportedException>(() => settings.GlobalizePath("remote://asset"),
            "Unknown virtual path schemes must be rejected.");

        settings.Save();
        var projectText = File.ReadAllText(settings.ProjectFilePath, Encoding.UTF8);
        using (var savedDocument = new ConfigFile())
        {
            savedDocument.Load(settings.ProjectFilePath);
            Require(File.Exists(settings.ProjectFilePath) && settings.GetChangedSettings().Count == 0 &&
                        !savedDocument.HasSectionKey(new ConfigKey<int>("gameplay", "score")) &&
                        savedDocument.HasSectionKey(new ConfigKey<int>("gameplay", "score.portable")) &&
                        projectText.IndexOf("[gameplay]", StringComparison.Ordinal) <
                        projectText.IndexOf("[application]", StringComparison.Ordinal),
                "A successful save must omit the initial value, retain overrides, apply setting order, and clear unsaved tracking.");
        }
        using (var initialReload = new ProjectSettings(root, user))
        {
            initialReload.Register(score);
            initialReload.SetInitialValue(score, 7);
            initialReload.Load();
            Require(initialReload.Get(score) == 7,
                "A value omitted from persistence must resolve to the registry's current initial value after loading.");
        }

        settings.SaveCustom("user://custom.cfg");
        Require(File.Exists(IOPath.Combine(user, "custom.cfg")),
            "Custom persistence must accept a confined user virtual path.");
        var nested = IOPath.Combine(root, "nested", "deeper");
        Directory.CreateDirectory(nested);
        Require(ProjectSettings.FindProjectRoot(nested) == root,
            "Project root discovery must search parent directories for the nearest project file.");

        settings.Set(score, 11);
        Expect<DirectoryNotFoundException>(() => settings.SaveCustom(IOPath.Combine(root, "missing", "custom.cfg")),
            "A failed custom save must surface an absent destination directory.");
        Require(settings.GetChangedSettings().Contains(score.Name),
            "A failed save must preserve the pending change batch.");
        Expect<InvalidOperationException>(() => settings.ConfigurePaths(secondRoot, secondUser),
            "A failed save must preserve unsaved-value tracking.");
        settings.Save();
        Require(settings.GetChangedSettings().Contains(score.Name),
            "Saving must not consume the change-notification batch.");
        settings.FlushChanges();

        using (var baseDocument = new ConfigFile())
        {
            baseDocument.Load(settings.ProjectFilePath);
            baseDocument.SetValue(new ConfigKey<int>("late", "value"), 77);
            baseDocument.Save(settings.ProjectFilePath);
        }

        using (var overrideDocument = new ConfigFile())
        {
            overrideDocument.SetValue(new ConfigKey<int>("gameplay", "score"), 99);
            overrideDocument.Save(settings.OverrideFilePath);
        }

        using (var loaded = new ProjectSettings(root, user))
        {
            loaded.Register(score);
            loaded.Load();
            Require(loaded.Get(score) == 99 && loaded.GetChangedSettings().Count == 0,
                "Project loading must merge the conventional override file and start from a clean state.");

            var late = new ProjectSetting<int>("late/value", 1);
            loaded.Register(late);
            Require(loaded.Get(late) == 77,
                "Unknown loaded values must remain available for later typed registration.");

            loaded.Set(late, 78);
            var validCustomPath = IOPath.Combine(root, "valid-custom.cfg");
            File.WriteAllText(validCustomPath, "[gameplay]\n\nscore=88\n", new UTF8Encoding(false));
            loaded.LoadCustom(validCustomPath);
            Require(loaded.Get(score) == 88 && loaded.Get(late) == 78 &&
                    loaded.GetChangedSettings().SequenceEqual([late.Name]),
                "A custom merge must retain pre-existing unsaved values and their unsaved tracking.");

            var invalidPath = IOPath.Combine(root, "invalid.cfg");
            File.WriteAllText(invalidPath, "[gameplay]\n\nscore=0\n", new UTF8Encoding(false));
            Expect<ArgumentOutOfRangeException>(() => loaded.LoadCustom(invalidPath),
                "Custom loading must reject values that fail a registered validator.");
            Require(loaded.Get(score) == 88,
                "A validator failure must leave the complete settings document unchanged.");

            var reentrantValidation = false;
            var reentrant = new ProjectSetting<int>("test/reentrant", 1, value =>
            {
                if (reentrantValidation)
                    loaded.Set(score, 5);
                return value > 0;
            });
            loaded.Register(reentrant);
            var reentrantPath = IOPath.Combine(root, "reentrant.cfg");
            File.WriteAllText(reentrantPath, "[test]\n\nreentrant=2\n", new UTF8Encoding(false));
            reentrantValidation = true;
            Expect<InvalidOperationException>(() => loaded.LoadCustom(reentrantPath),
                "A validator must not mutate project settings re-entrantly during transactional loading.");
            Require(loaded.Get(score) == 88 && loaded.Get(reentrant) == 1,
                "Rejected re-entrant loading must preserve every current value.");

            loaded.Set(score, 13);
            Expect<InvalidOperationException>(() => loaded.ConfigurePaths(secondRoot, secondUser),
                "Path reconfiguration must reject unsaved changes.");
            loaded.Save();
            loaded.ConfigurePaths(secondRoot, secondUser);
            Require(loaded.ProjectRoot == IOPath.GetFullPath(secondRoot) && loaded.Get(score) == 10,
                "Clean path reconfiguration must retain definitions while clearing loaded values.");
        }

        Parallel.For(1, 129, value =>
        {
            settings.Set(score, value);
            Require(settings.Get(score) > 0, "Concurrent setting access must preserve validator invariants.");
        });

        settings.Unregister(checkpoints);
        Require(!settings.HasSetting(checkpoints) && propertyListChanges >= 8,
            "Custom setting removal must update registration and tooling discovery.");

        var disposable = new ProjectSettings(root, user);
        disposable.Dispose();
        Expect<ObjectDisposedException>(() => disposable.Get(ProjectSettings.ApplicationName),
            "Disposed isolated registries must reject reads.");
        Expect<InvalidOperationException>(ProjectSettings.Instance.Dispose,
            "The process-wide ProjectSettings registry must reject disposal.");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
        Directory.Delete(user, recursive: true);
        Directory.Delete(secondRoot, recursive: true);
        Directory.Delete(secondUser, recursive: true);
    }
}

static void VerifyInputMapConfiguration()
{
    const string action = "tests.input.map.configuration";
    var map = InputMap.Instance;
    var input = Input.Instance;
    input.ReleasePressedEvents();
    if (map.HasAction(action))
        map.EraseAction(action);

    var before = map.GetActions();
    using var binding = new InputEventKey { Keycode = Key.F1 };
    using var duplicate = new InputEventKey { Keycode = Key.F1 };
    using var absent = new InputEventKey { Keycode = Key.F2 };
    using var incompatible = new InputEventMouseMotion();
    try
    {
        map.AddAction(action);
        Require(map.HasAction(action) && !map.HasAction(action.ToUpperInvariant()) &&
                map.ActionGetDeadzone(action) == 0.2f &&
                !before.Contains(action) && map.GetActions().Last() == action,
            "Action registration must preserve case, default deadzone and snapshot order.");
        Expect<NotSupportedException>(() => ((IList<string>)map.GetActions()).Add("invalid"),
            "The action-name snapshot must be immutable.");
        Expect<InvalidOperationException>(() => map.AddAction(action),
            "Duplicate action registration must fail without changing the existing action.");
        Expect<ArgumentOutOfRangeException>(() => map.ActionSetDeadzone(action, -0.01f),
            "Negative deadzones must be rejected before mutation.");
        Expect<ArgumentOutOfRangeException>(() => map.ActionSetDeadzone(action, float.PositiveInfinity),
            "Infinite deadzones must be rejected before mutation.");
        Require(map.ActionGetDeadzone(action) == 0.2f,
            "Failed deadzone changes must leave the action intact.");
        map.ActionSetDeadzone(action, 0f);
        Require(map.ActionGetDeadzone(action) == 0f, "Zero is a valid action deadzone.");
        input.ActionPress(action);
        map.ActionSetDeadzone(action, 1f);
        Require(map.ActionGetDeadzone(action) == 1f && !input.IsActionPressed(action),
            "One is a valid deadzone and changing it invalidates prior action state.");

        Expect<ArgumentException>(() => map.ActionAddEvent(action, incompatible),
            "Only action-compatible events may be registered.");
        map.ActionAddEvent(action, binding);
        var snapshot = map.ActionGetEvents(action);
        map.ActionAddEvent(action, duplicate);
        Require(map.ActionHasEvent(action, duplicate) && map.ActionGetEvents(action).Count == 1 &&
                snapshot.Count == 1 && ReferenceEquals(snapshot[0], binding),
            "Exact duplicate bindings must collapse while snapshots retain live references.");
        Expect<NotSupportedException>(() => ((IList<InputEvent>)snapshot).Clear(),
            "The binding snapshot must be immutable.");
        input.ActionPress(action);
        map.ActionEraseEvent(action, absent);
        Require(input.IsActionPressed(action) && map.ActionGetEvents(action).Count == 1,
            "Erasing an absent binding must not change the map or pressed state.");
        map.ActionEraseEvent(action, duplicate);
        Require(!input.IsActionPressed(action) && map.ActionGetEvents(action).Count == 0 &&
                snapshot.Count == 1 && ReferenceEquals(snapshot[0], binding),
            "Erasing an exact binding must invalidate action state and preserve old snapshots.");
        map.ActionAddEvent(action, binding);
        input.ActionPress(action);
        map.ActionEraseEvents(action);
        Require(!input.IsActionPressed(action) && map.ActionGetEvents(action).Count == 0,
            "Erasing every binding must invalidate action state.");
        input.ActionPress(action);
        map.EraseAction(action);
        Require(!map.HasAction(action) && !map.GetActions().Contains(action) && !input.IsAnythingPressed(),
            "Erasing an action must remove its registration and pressed contribution.");
        Expect<KeyNotFoundException>(() => map.EraseAction(action),
            "Erasing an unknown action must report the missing registration.");
        Expect<KeyNotFoundException>(() => map.ActionGetDeadzone(action),
            "Deadzone lookup must report a missing action.");
        Expect<KeyNotFoundException>(() => map.ActionGetEvents(action),
            "Binding lookup must report a missing action.");
        Expect<KeyNotFoundException>(() => map.ActionEraseEvents(action),
            "Binding removal must report a missing action.");
    }
    finally
    {
        if (map.HasAction(action))
            map.EraseAction(action);
        input.ReleasePressedEvents();
    }
}

static void VerifyInputMapMatching()
{
    const string inner = "tests.input.map.match.inner";
    const string outer = "tests.input.map.match.outer";
    var map = InputMap.Instance;
    foreach (var action in new[] { inner, outer })
        if (map.HasAction(action))
            map.EraseAction(action);

    using var key = new InputEventKey { Keycode = Key.F3 };
    using var synthetic = new InputEventAction { Action = inner, Device = InputMap.AllDevices };
    try
    {
        map.AddAction(inner);
        map.ActionAddEvent(inner, key);
        map.AddAction(outer);
        map.ActionAddEvent(outer, synthetic);
        Require(synthetic.IsMatch(key) && !map.ActionHasEvent(outer, key),
            "A synthetic event may publicly match an action source without making that source an exact binding.");
        map.ActionAddEvent(outer, key);
        Require(map.ActionGetEvents(outer).Count == 2 && map.ActionHasEvent(outer, key),
            "Exact binding lookup must keep physical and synthetic event families distinct.");
        map.ActionEraseEvent(outer, key);
        Require(map.ActionGetEvents(outer).Count == 1 && ReferenceEquals(map.ActionGetEvents(outer)[0], synthetic),
            "Exact binding removal must retain the synthetic event when given a physical source.");
        map.ActionEraseEvents(outer);

        var cases = new (InputEvent Binding, InputEvent Same, InputEvent Different)[]
        {
            (new InputEventKey { Keycode = Key.F4 }, new InputEventKey { Keycode = Key.F4 }, new InputEventKey { Keycode = Key.F5 }),
            (new InputEventMouseButton { ButtonIndex = MouseButton.Left }, new InputEventMouseButton { ButtonIndex = MouseButton.Left }, new InputEventMouseButton { ButtonIndex = MouseButton.Right }),
            (new InputEventJoypadButton { ButtonIndex = JoyButton.A }, new InputEventJoypadButton { ButtonIndex = JoyButton.A }, new InputEventJoypadButton { ButtonIndex = JoyButton.B }),
            (new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = -1f }, new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = -0.25f }, new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = 1f }),
            (new InputEventAction { Action = inner }, new InputEventAction { Action = inner }, new InputEventAction { Action = outer }),
        };
        try
        {
            foreach (var (binding, same, different) in cases)
            {
                Require(binding.IsMatch(same) && !binding.IsMatch(different),
                    "Each bindable event family must distinguish exact identities and axis directions.");
                map.ActionAddEvent(outer, binding);
                Require(map.ActionHasEvent(outer, same) && !map.ActionHasEvent(outer, different),
                    "Exact binding lookup must use each event family's action-match identity.");
                map.ActionEraseEvent(outer, different);
                Require(map.ActionGetEvents(outer).Count == 1,
                    "An unrelated event must not remove an existing binding.");
                map.ActionEraseEvent(outer, same);
                Require(map.ActionGetEvents(outer).Count == 0,
                    "An equivalent event must remove its exact binding.");
            }
            Require(cases[3].Binding.IsMatch(cases[3].Different, exactMatch: false),
                "Non-exact controller-axis matching must ignore direction.");
            using var extraModifier = new InputEventKey { Keycode = Key.F4, ControlPressed = true };
            Require(cases[0].Binding.IsMatch(extraModifier, exactMatch: false) &&
                    !cases[0].Binding.IsMatch(extraModifier),
                "Non-exact key matching ignores extra modifiers while exact matching keeps them.");
        }
        finally
        {
            map.ActionEraseEvents(outer);
            foreach (var (binding, same, different) in cases)
            {
                binding.Dispose();
                same.Dispose();
                different.Dispose();
            }
        }

        using var requiredKey = new InputEventKey { Keycode = Key.F6, ControlPressed = true };
        using var keyExtra = new InputEventKey { Keycode = Key.F6, ControlPressed = true, ShiftPressed = true, Pressed = true };
        using var keyMissing = new InputEventKey { Keycode = Key.F6, Pressed = true };
        using var keyRelease = new InputEventKey { Keycode = Key.F6 };
        using var keyEcho = new InputEventKey { Keycode = Key.F6, ControlPressed = true, Pressed = true, Echo = true };
        map.ActionAddEvent(outer, requiredKey);
        Require(keyExtra.IsActionPressed(outer) && !keyExtra.IsActionPressed(outer, exactMatch: true) &&
                !keyMissing.IsAction(outer) && keyRelease.IsActionReleased(outer) &&
                !keyRelease.IsActionReleased(outer, exactMatch: true) &&
                !keyEcho.IsActionPressed(outer) && keyEcho.IsActionPressed(outer, allowEcho: true),
            "Key actions must handle extra, missing and released modifiers plus repeated presses.");
        map.ActionEraseEvents(outer);

        using var physical = new InputEventKey { PhysicalKeycode = Key.F7, Location = KeyLocation.Left };
        using var physicalLeft = new InputEventKey { PhysicalKeycode = Key.F7, Location = KeyLocation.Left, Pressed = true };
        using var physicalRight = new InputEventKey { PhysicalKeycode = Key.F7, Location = KeyLocation.Right, Pressed = true };
        map.ActionAddEvent(outer, physical);
        Require(physical.IsMatch(physicalLeft) && !physical.IsMatch(physicalRight) &&
                map.EventIsAction(physicalLeft, outer) && !map.EventIsAction(physicalRight, outer),
            "Physical-key actions must preserve left/right location identity.");
        map.ActionEraseEvents(outer);

        using var label = new InputEventKey { KeyLabel = Key.F8 };
        using var labeledSource = new InputEventKey { Keycode = Key.F9, KeyLabel = Key.F8, Pressed = true };
        map.ActionAddEvent(outer, label);
        Require(map.EventIsAction(labeledSource, outer) && label.IsMatch(labeledSource),
            "Label-only bindings must use the key label even when the logical code differs.");
        map.ActionEraseEvents(outer);

        using var mouse = new InputEventMouseButton { ButtonIndex = MouseButton.Left, ControlPressed = true };
        using var mouseExtra = new InputEventMouseButton { ButtonIndex = MouseButton.Left, ControlPressed = true, ShiftPressed = true, Pressed = true };
        using var mouseCanceled = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Canceled = true };
        map.ActionAddEvent(outer, mouse);
        Require(mouseExtra.IsActionPressed(outer) && !mouseExtra.IsActionPressed(outer, exactMatch: true) &&
                mouseCanceled.IsActionReleased(outer) && !mouseCanceled.IsActionReleased(outer, exactMatch: true),
            "Mouse-button actions must handle modifiers and canceled releases.");
        map.ActionEraseEvents(outer);

        using var button = new InputEventJoypadButton { Device = InputMap.AllDevices, ButtonIndex = JoyButton.B };
        using var buttonSource = new InputEventJoypadButton { Device = 7, ButtonIndex = JoyButton.B, Pressed = true };
        map.ActionAddEvent(outer, button);
        Require(buttonSource.IsActionPressed(outer, exactMatch: true) &&
                buttonSource.GetActionStrength(outer) == 1f,
            "An all-device controller button must match a concrete device at full strength.");
        button.Device = 3;
        Require(!map.EventIsAction(buttonSource, outer),
            "A concrete controller binding must reject events from another device.");
        map.ActionEraseEvents(outer);

        using var axis = new InputEventJoypadMotion { Device = InputMap.AllDevices, Axis = JoyAxis.LeftX, AxisValue = -1f };
        using var axisSource = new InputEventJoypadMotion { Device = 7, Axis = JoyAxis.LeftX, AxisValue = -0.6f };
        using var axisOpposite = new InputEventJoypadMotion { Device = 7, Axis = JoyAxis.LeftX, AxisValue = 0.6f };
        using var axisBelowDeadzone = new InputEventJoypadMotion { Device = 7, Axis = JoyAxis.LeftX, AxisValue = -0.1f };
        map.ActionAddEvent(outer, axis);
        Require(axisSource.IsActionPressed(outer, exactMatch: true) &&
                NearlyEqual(axisSource.GetActionStrength(outer), 0.5f) &&
                map.EventIsAction(axisOpposite, outer) && axisOpposite.IsActionReleased(outer) &&
                !axisOpposite.IsAction(outer, exactMatch: true) &&
                axisOpposite.GetActionStrength(outer) == 0f &&
                axisBelowDeadzone.IsActionReleased(outer),
            "Controller-axis actions must apply direction and deadzone to effective press and strength.");
        map.ActionAddEvent(outer, axisOpposite);
        Require(axisOpposite.IsActionReleased(outer) && axisOpposite.GetActionStrength(outer) == 0f,
            "Non-exact action queries must use the first matching axis binding in registration order.");
        map.ActionEraseEvent(outer, axis);
        Require(axisOpposite.IsActionPressed(outer) && NearlyEqual(axisOpposite.GetActionStrength(outer), 0.5f),
            "Removing the earlier opposite-axis binding must expose the later matching direction.");
        map.ActionEraseEvents(outer);

        using var direct = new InputEventAction { Action = outer, Pressed = true, Strength = 0.3f };
        Require(map.EventIsAction(direct, outer, exactMatch: true) &&
                direct.IsActionPressed(outer) && NearlyEqual(direct.GetActionStrength(outer), 0.3f) &&
                !direct.IsAction(inner) && !direct.IsAction(outer + ".missing"),
            "A direct action event must match its registered name and preserve its configured strength.");
        direct.Pressed = false;
        Require(direct.IsActionReleased(outer) && direct.GetActionStrength(outer) == 0f,
            "A direct action release must contribute zero strength.");
        Expect<KeyNotFoundException>(() => map.EventIsAction(direct, outer + ".missing"),
            "Action-map queries must validate registration even for direct events.");
        using var motion = new InputEventMouseMotion();
        Require(!motion.IsMatch(key), "Non-bindable events must retain the base non-match behavior.");
        using var unnamed = new InputEventAction();
        Require(!unnamed.IsMatch(key), "An unnamed synthetic event cannot match a physical source.");
        Expect<ArgumentNullException>(() => key.IsMatch(null!),
            "Binding comparison must reject a null event.");
        using var disposed = new InputEventKey { Keycode = Key.F3 };
        disposed.Dispose();
        Expect<ObjectDisposedException>(() => key.IsMatch(disposed),
            "Binding comparison must reject a disposed event.");
    }
    finally
    {
        if (map.HasAction(outer))
            map.EraseAction(outer);
        if (map.HasAction(inner))
            map.EraseAction(inner);
    }
}

static void VerifyInputEventActionValues()
{
    const string name = "tests.input.action.values";
    var map = InputMap.Instance;
    if (map.HasAction(name))
        map.EraseAction(name);

    using var action = new InputEventAction();
    using var selfBinding = new InputEventAction { Action = name };
    using var firstKey = new InputEventKey { Keycode = Key.F10 };
    using var secondKey = new InputEventKey { Keycode = Key.F11 };
    var properties = action.GetPropertyList();
    var actionProperty = properties.Single(property => property.Name == nameof(InputEventAction.Action));
    var indexProperty = properties.Single(property => property.Name == nameof(InputEventAction.EventIndex));
    var pressedProperty = properties.Single(property => property.Name == nameof(InputEventAction.Pressed));
    var strengthProperty = properties.Single(property => property.Name == nameof(InputEventAction.Strength));
    Require(action.Action == string.Empty && action.EventIndex == -1 && !action.Pressed &&
            action.Strength == 1f && action.AsText() == string.Empty &&
            !action.PropertyCanRevert(actionProperty) && !action.PropertyCanRevert(indexProperty) &&
            !action.PropertyCanRevert(pressedProperty) && !action.PropertyCanRevert(strengthProperty),
        "Direct action defaults and stored-property revert values must match construction.");

    var changes = 0;
    action.Changed += _ => changes++;
    action.Action = name;
    action.EventIndex = 31;
    action.Pressed = true;
    action.Strength = 0.25f;
    using var copy = (InputEventAction)action.Duplicate();
    Require(changes == 4 && copy.Action == name && copy.EventIndex == 31 && copy.Pressed &&
            copy.Strength == 0.25f && action.PropertyCanRevert(actionProperty) &&
            action.PropertyCanRevert(indexProperty) && action.PropertyCanRevert(pressedProperty) &&
            action.PropertyCanRevert(strengthProperty),
        "Direct action values must notify, duplicate and expose their stored revert state.");

    Expect<ArgumentNullException>(() => action.Action = null!, "An action name cannot be null.");
    Expect<ArgumentOutOfRangeException>(() => action.Strength = float.NaN, "NaN strength must fail.");
    Expect<ArgumentOutOfRangeException>(() => action.Strength = float.PositiveInfinity,
        "Infinite strength must fail.");
    Require(changes == 4 && action.Action == name && action.EventIndex == 31 &&
            action.Strength == 0.25f,
        "Invalid assignments must leave the event and change count intact.");
    action.Strength = -4f;
    Require(action.Strength == 0f && action.Pressed, "A zero-strength direct press remains pressed.");
    action.Strength = 4f;
    Require(action.Strength == 1f, "Finite action strength clamps to one.");
    action.EventIndex = int.MinValue;
    using var negativeIndexCopy = (InputEventAction)action.Duplicate();
    Require(action.EventIndex == int.MinValue && negativeIndexCopy.EventIndex == int.MinValue,
        "The event retains and copies a signed negative index before dispatch interprets it.");
    action.EventIndex = int.MaxValue;
    Require(action.EventIndex == int.MaxValue,
        "The event retains a large positive index before the input source-capacity boundary.");
    action.EventIndex = 0;
    Require(action.EventIndex == 0, "The first explicit binding index is valid.");
    action.EventIndex = -1;
    Require(action.EventIndex == -1, "The unindexed source sentinel is valid.");

    try
    {
        Require(action.AsText() == name, "An unregistered action description falls back to its name.");
        map.AddAction(name);
        map.ActionAddEvent(name, selfBinding);
        Require(action.AsText() == name, "A synthetic self-binding must not recurse into its description.");
        Input.Instance.ActionPress(name);
        selfBinding.Action = name + ".other";
        Require(!Input.Instance.IsActionPressed(name),
            "Editing a registered direct-action binding must invalidate its cached action state.");
        selfBinding.Action = name;
        map.ActionAddEvent(name, firstKey);
        map.ActionAddEvent(name, secondKey);
        Require(action.AsText() == firstKey.AsText(),
            "A direct action description uses its first concrete binding.");
        map.ActionEraseEvent(name, firstKey);
        Require(action.AsText() == secondKey.AsText(),
            "Removing the first concrete binding exposes the next description.");
        map.ActionEraseEvents(name);
        Require(action.AsText() == name, "Removing all bindings restores the action-name fallback.");
    }
    finally
    {
        if (map.HasAction(name))
            map.EraseAction(name);
    }

    action.RevertProperty(actionProperty);
    action.RevertProperty(indexProperty);
    action.RevertProperty(pressedProperty);
    action.RevertProperty(strengthProperty);
    Require(action.Action == string.Empty && action.EventIndex == -1 && !action.Pressed &&
            action.Strength == 1f && action.AsText() == string.Empty,
        "Reverting all direct action descriptors must restore the constructor state.");
    using var disposed = new InputEventAction();
    disposed.Dispose();
    Expect<ObjectDisposedException>(() => disposed.AsText(), "Disposed direct action text must fail.");
}

static void VerifyInputText()
{
    const string empty = "tests.input.text.empty";
    const string keys = "tests.input.text.keys";
    var map = InputMap.Instance;
    foreach (var action in new[] { empty, keys })
        if (map.HasAction(action))
            map.EraseAction(action);

    using var unset = new InputEventKey();
    using var space = new InputEventKey { Keycode = Key.Space };
    using var function = new InputEventKey { Keycode = Key.F1 };
    using var mouseBinding = new InputEventMouseButton { ButtonIndex = MouseButton.Left };
    using var mouseDouble = new InputEventMouseButton
    {
        ButtonIndex = MouseButton.Left,
        ControlPressed = true,
        DoubleClick = true,
    };
    using var wheel = new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp };
    using var thumb = new InputEventMouseButton { ButtonIndex = MouseButton.XButton1 };
    using var noButton = new InputEventMouseButton();
    using var unknownButton = new InputEventMouseButton { ButtonIndex = (MouseButton)10 };
    using var negativeButton = new InputEventMouseButton { ButtonIndex = (MouseButton)(-4) };
    using var mouseMotion = new InputEventMouseMotion
    {
        Position = new Vector2(1f, 2f),
        Velocity = new Vector2(3f, 4f),
    };
    using var nonfiniteMotion = new InputEventMouseMotion
    {
        Position = new Vector2(float.NaN, float.PositiveInfinity),
        Velocity = new Vector2(float.NegativeInfinity, 0f),
    };
    using var fractionalMotion = new InputEventMouseMotion
    {
        Position = new Vector2(1.2345678f, 12.345678f),
        Velocity = new Vector2(0.0000001f, -0f),
    };
    using var boundaryMotion = new InputEventMouseMotion
    {
        Position = new Vector2(BitConverter.Int32BitsToSingle(0x4479ffff),
            BitConverter.Int32BitsToSingle(unchecked((int)0x5c4bef60))),
    };
    using var physical = new InputEventKey { PhysicalKeycode = Key.A };
    using var physicalFunction = new InputEventKey { PhysicalKeycode = Key.F1 };
    using var label = new InputEventKey { KeyLabel = Key.A };
    using var symbol = new InputEventKey { Keycode = Key.Exclamation };
    using var keypad = new InputEventKey { Keycode = Key.Keypad1 };
    using var unknown = new InputEventKey { Keycode = (Key)0x110000 };
    using var specialMarker = new InputEventKey { Keycode = Key.Special };
    using var printable = new InputEventKey { Keycode = (Key)0x20AC };
    using var modifiedSpace = new InputEventKey { Keycode = Key.Space, ControlPressed = true };
    using var embeddedModifiers = new InputEventKey
    {
        Keycode = (Key)((int)Key.A | (int)KeyModifierMask.Control | (int)KeyModifierMask.Shift),
    };
    using var meta = new InputEventKey { Keycode = Key.A, MetaPressed = true };
    using var alt = new InputEventKey { Keycode = Key.A, AltPressed = true };
    using var left = new InputEventKey { Location = KeyLocation.Left };
    using var right = new InputEventKey { Location = KeyLocation.Right };
    using var synthetic = new InputEventAction { Action = keys };
    foreach (var (button, expected) in new (MouseButton Button, string Text)[]
    {
        (MouseButton.Left, "Left Mouse Button"),
        (MouseButton.Right, "Right Mouse Button"),
        (MouseButton.Middle, "Middle Mouse Button"),
        (MouseButton.WheelUp, "Mouse Wheel Up"),
        (MouseButton.WheelDown, "Mouse Wheel Down"),
        (MouseButton.WheelLeft, "Mouse Wheel Left"),
        (MouseButton.WheelRight, "Mouse Wheel Right"),
        (MouseButton.XButton1, "Mouse Thumb Button 1"),
        (MouseButton.XButton2, "Mouse Thumb Button 2"),
    })
    {
        using var knownButton = new InputEventMouseButton { ButtonIndex = button };
        Require(knownButton.AsText() == expected,
            "Each defined mouse button must use its pinned display name.");
    }
    Require(unset.AsText() == "(unset)" && unset.AsTextKeycode() == "(unset)" &&
            unset.AsTextPhysicalKeycode() == "(unset)" && unset.AsTextKeyLabel() == "(unset)" &&
            space.AsText() == "Space" && physical.AsText() == "A - Physical" &&
            physicalFunction.AsText() == "F1" && physicalFunction.AsTextPhysicalKeycode() == "F1" &&
            label.AsText() == "A - Unicode" && symbol.AsText() == "Exclam" &&
            keypad.AsText() == "Kp 1" && unknown.AsText() == "\uFFFD" &&
            specialMarker.AsText() == "\uFFFD" && printable.AsText() == "€" &&
            modifiedSpace.AsTextKeycode() == "Ctrl+Space" &&
            embeddedModifiers.AsTextKeycode() == "Ctrl+Shift+A" &&
            meta.AsText() == (OperatingSystem.IsMacOS() ? "Command+A" : OperatingSystem.IsWindows() ? "Windows+A" : "Meta+A") &&
            alt.AsText() == (OperatingSystem.IsMacOS() ? "Option+A" : "Alt+A") &&
            unset.AsTextLocation() == string.Empty && left.AsTextLocation() == "left" &&
            right.AsTextLocation() == "right",
        "Key text must preserve the pinned names, unset fallback, origin suffixes and location casing.");
    Require(mouseBinding.AsText() == "Left Mouse Button" &&
            mouseDouble.AsText() == "Ctrl+Left Mouse Button (Double Click)" &&
            wheel.AsText() == "Mouse Wheel Up" && thumb.AsText() == "Mouse Thumb Button 1" &&
            noButton.AsText() == "Button #0" && unknownButton.AsText() == "Button #10" &&
            negativeButton.AsText() == "Button #-4" &&
            mouseMotion.AsText() == "Mouse motion at position ((1.0, 2.0)) with velocity ((3.0, 4.0))" &&
            nonfiniteMotion.AsText() == "Mouse motion at position ((nan, inf)) with velocity ((-inf, 0.0))" &&
            fractionalMotion.AsText() == "Mouse motion at position ((1.234568, 12.34568)) with velocity ((0.0, 0.0))",
        "Mouse text must retain named/unknown buttons, double clicks and positional motion wording.");
    Require(boundaryMotion.AsText() ==
            "Mouse motion at position ((1000.0, 229610463472648192.0)) with velocity ((0.0, 0.0))",
        "Vector text must use float precision at decimal powers and retain large integral digits.");

    var previousCulture = TranslationServer.Culture;
    var previousEnabled = TranslationServer.Enabled;
    var previousMapTranslation = map.CanTranslateMessages;
    var previousMapDomain = map.TranslationDomain;
    var previousUnsetTranslation = unset.CanTranslateMessages;
    try
    {
        map.CanTranslateMessages = true;
        map.TranslationDomain = string.Empty;
        unset.CanTranslateMessages = true;
        map.AddAction(empty);
        map.AddAction(keys);
        Expect<KeyNotFoundException>(() => map.GetActionDescription(empty + ".missing"),
            "Action descriptions must reject an unregistered name.");
        map.ActionAddEvent(keys, synthetic);
        Require(map.GetActionDescription(keys) == "Action has no bound inputs",
            "A synthetic-only action has no concrete binding description.");
        map.ActionAddEvent(keys, space);
        map.ActionAddEvent(keys, function);
        map.ActionAddEvent(keys, mouseBinding);
        Require(map.GetActionDescription(empty) == "Action has no bound inputs" &&
                map.GetActionDescription(keys) == $"Space or F1 or {mouseBinding.AsText()}" &&
                synthetic.AsText() == "Space",
            "Action descriptions must skip synthetic bindings and list concrete names in order.");

        TranslationServer.Clear();
        TranslationServer.Enabled = true;
        TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-FR");
        var french = CultureInfo.GetCultureInfo("fr");
        TranslationServer.AddTranslation(french, "", "Action has no bound inputs", "Action sans touche");
        TranslationServer.AddTranslation(french, "", " or ", " ou ");
        TranslationServer.AddTranslation(french, "", "unset", "non défini");
        TranslationServer.AddTranslation(french, "", "Physical", "Physique");
        TranslationServer.AddTranslation(french, "", "Left Mouse Button", "Bouton gauche");
        TranslationServer.AddTranslation(french, "", "Double Click", "Double clic");
        TranslationServer.AddTranslation(french, "", "Button", "Bouton");
        TranslationServer.AddTranslation(french, "", "Mouse motion at position (%s) with velocity (%s)",
            "Mouvement à (%s), vitesse (%s)");
        Require(map.GetActionDescription(empty) == "Action sans touche" &&
                map.GetActionDescription(keys) == $"Space ou F1 ou {mouseBinding.AsText()}" &&
                unset.AsText() == "(non défini)" && physical.AsText() == "A - Physique" &&
                mouseBinding.AsText() == "Bouton gauche" &&
                mouseDouble.AsText() == "Ctrl+Bouton gauche (Double clic)" &&
                unknownButton.AsText() == "Bouton #10" &&
                mouseMotion.AsText() == "Mouvement à ((1.0, 2.0)), vitesse ((3.0, 4.0))",
            "Runtime descriptions must resolve engine words through the active translation catalog.");
        TranslationServer.AddTranslation(french, "", "Mouse motion at position (%s) with velocity (%s)",
            "Mouvement sans arguments");
        Require(mouseMotion.AsText() == "Mouse motion at position ((1.0, 2.0)) with velocity ((3.0, 4.0))",
            "A malformed translated motion template must fall back to the source wording.");
        mouseBinding.CanTranslateMessages = false;
        mouseMotion.CanTranslateMessages = false;
        Require(mouseBinding.AsText() == "Left Mouse Button" &&
                mouseMotion.AsText() == "Mouse motion at position ((1.0, 2.0)) with velocity ((3.0, 4.0))",
            "Disabling translation on mouse events must restore source descriptions.");
        map.CanTranslateMessages = false;
        unset.CanTranslateMessages = false;
        Require(map.GetActionDescription(empty) == "Action has no bound inputs" &&
                map.GetActionDescription(keys) == $"Space or F1 or {mouseBinding.AsText()}" &&
                unset.AsText() == "(unset)",
            "Disabling translation on a description owner must return its source wording.");
    }
    finally
    {
        map.CanTranslateMessages = previousMapTranslation;
        map.TranslationDomain = previousMapDomain;
        unset.CanTranslateMessages = previousUnsetTranslation;
        TranslationServer.Clear();
        TranslationServer.Culture = previousCulture;
        TranslationServer.Enabled = previousEnabled;
        if (map.HasAction(keys))
            map.EraseAction(keys);
        if (map.HasAction(empty))
            map.EraseAction(empty);
    }
}

static void VerifyControllerValues()
{
    using var motion = new InputEventJoypadMotion();
    using var button = new InputEventJoypadButton();
    Require(motion.Axis == JoyAxis.LeftX && motion.AxisValue == 0f && motion.IsReleased() &&
            button.ButtonIndex == JoyButton.A && button.Pressure == 0f && button.IsReleased(),
        "Controller events retain their constructor and raw press defaults.");

    var motionChanges = 0;
    motion.Changed += _ => motionChanges++;
    motion.Axis = JoyAxis.Invalid;
    Require(motion.AsText() == "Joypad Motion on Axis -1 (Unknown Joypad Axis) with Value 0.00",
        "The accepted invalid-axis sentinel must have a safe numeric description.");
    motion.Axis = JoyAxis.Max;
    Require(motion.AsText() == "Joypad Motion on Axis 10 (Unknown Joypad Axis) with Value 0.00",
        "The accepted maximum-axis sentinel must have a safe numeric description.");
    Expect<ArgumentOutOfRangeException>(() => motion.Axis = (JoyAxis)(-2),
        "Axis values below the invalid sentinel must fail before mutation.");
    Expect<ArgumentOutOfRangeException>(() => motion.Axis = (JoyAxis)11,
        "Axis values above the maximum sentinel must fail before mutation.");
    Require(motion.Axis == JoyAxis.Max && motionChanges == 2,
        "Rejected axes leave the stored value and change count intact.");

    motion.Axis = JoyAxis.LeftX;
    motion.AxisValue = 0.499f;
    Require(motion.IsReleased(), "Axis motion below the fixed toggle threshold is released.");
    motion.AxisValue = 0.5f;
    Require(motion.IsPressed(), "Axis motion at the fixed toggle threshold is pressed.");
    motion.AxisValue = -0.5f;
    Require(motion.IsPressed(), "The toggle threshold uses absolute axis magnitude.");
    motion.AxisValue = BitConverter.Int32BitsToSingle(0x7fc00000);
    Require(float.IsNaN(motion.AxisValue) && motion.IsReleased() && motion.AsText().EndsWith("Value nan", StringComparison.Ordinal),
        "Positive NaN is stored, releases the toggle state and uses source numeric text.");
    motion.AxisValue = BitConverter.Int32BitsToSingle(unchecked((int)0xffc00000));
    Require(motion.AsText().EndsWith("Value -nan", StringComparison.Ordinal),
        "Negative NaN retains its sign in fixed-decimal source text.");
    motion.AxisValue = float.NegativeInfinity;
    Require(float.IsNegativeInfinity(motion.AxisValue) && motion.IsPressed() &&
            motion.AsText().EndsWith("Value -inf", StringComparison.Ordinal),
        "Infinite axis motion is stored, presses the toggle state and uses source numeric text.");
    motion.AxisValue = -1.5f;
    using var copiedMotion = (InputEventJoypadMotion)motion.Duplicate();
    Require(copiedMotion.AxisValue == -1.5f && copiedMotion.IsPressed(),
        "Out-of-range source motion and derived press state survive duplication.");
    var axisValueProperty = motion.GetPropertyList().OfType<PropertyDescriptor<InputEventJoypadMotion, float>>()
        .Single(property => property.Name == nameof(InputEventJoypadMotion.AxisValue));
    motion.RevertProperty(axisValueProperty);
    Require(motion.AxisValue == 0f && motion.IsReleased(),
        "Axis-value descriptor revert recomputes the raw press state.");

    var buttonChanges = 0;
    button.Changed += _ => buttonChanges++;
    button.ButtonIndex = (JoyButton)int.MinValue;
    Require(button.AsText() == "Joypad Button -2147483648" && buttonChanges == 1,
        "The button setter retains arbitrary signed identities without indexing descriptions.");
    button.ButtonIndex = (JoyButton)int.MaxValue;
    button.Pressed = true;
    button.Pressure = -1.25f;
    Require(button.IsPressed() && button.Pressure == -1.25f && buttonChanges == 2 &&
            button.AsText() == "Joypad Button 2147483647, Pressure: -1.25",
        "Button pressure and press state retain source values without content-change emission.");
    button.Pressure = float.NaN;
    Require(float.IsNaN(button.Pressure) && button.AsText().EndsWith("Pressure: nan", StringComparison.Ordinal),
        "NaN pressure is retained and formatted safely.");
    button.Pressure = float.PositiveInfinity;
    using var copiedButton = (InputEventJoypadButton)button.Duplicate();
    Require(float.IsPositiveInfinity(copiedButton.Pressure) && copiedButton.IsPressed() &&
            copiedButton.ButtonIndex == (JoyButton)int.MaxValue && buttonChanges == 2,
        "Arbitrary pressure and button state survive duplication without new change notifications.");
    var pressureProperty = button.GetPropertyList().OfType<PropertyDescriptor<InputEventJoypadButton, float>>()
        .Single(property => property.Name == nameof(InputEventJoypadButton.Pressure));
    button.RevertProperty(pressureProperty);
    Require(button.Pressure == 0f && buttonChanges == 2,
        "Pressure descriptor revert restores zero without content-change emission.");

    void ThrowOnChange(Resource _) => throw new InvalidOperationException("expected controller observer failure");
    motion.Changed += ThrowOnChange;
    Expect<InvalidOperationException>(() => motion.AxisValue = 0.5f,
        "A throwing axis observer must see a committed value and raw press state.");
    Require(motion.AxisValue == 0.5f && motion.IsPressed(),
        "The axis value and raw press state remain committed after an observer failure.");
}

static void VerifyControllerText()
{
    using var button = new InputEventJoypadButton { ButtonIndex = JoyButton.A };
    using var dpad = new InputEventJoypadButton { ButtonIndex = JoyButton.DpadUp };
    using var extended = new InputEventJoypadButton { ButtonIndex = JoyButton.Misc2 };
    using var rawButton = new InputEventJoypadButton { ButtonIndex = (JoyButton)127 };
    using var pressure = new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressure = 0.1f };
    using var fullPressure = new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressure = 1f };
    using var tinyPressure = new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressure = float.Epsilon };
    using var axis = new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = -0.257f };
    using var rawAxis = new InputEventJoypadMotion { Axis = (JoyAxis)9 };
    using var unknownAxis = new InputEventJoypadMotion { Axis = JoyAxis.Max };
    using var axisOne = new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = 1f };
    using var axisNegativeOne = new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = -1f };
    using var axisMidpoint = new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = 0.125f };
    Require(button.AsText() == "Joypad Button 0 (Bottom Action, Sony Cross, Xbox A, Nintendo B)" &&
            dpad.AsText() == "Joypad Button 11 (D-pad Up)" &&
            extended.AsText() == "Joypad Button 21" && rawButton.AsText() == "Joypad Button 127" &&
            pressure.AsText() == "Joypad Button 0 (Bottom Action, Sony Cross, Xbox A, Nintendo B), Pressure: 0.10000000149012" &&
            fullPressure.AsText() == "Joypad Button 0 (Bottom Action, Sony Cross, Xbox A, Nintendo B), Pressure: 1.0" &&
            tinyPressure.AsText() == "Joypad Button 0 (Bottom Action, Sony Cross, Xbox A, Nintendo B), Pressure: 0.0" &&
            axis.AsText() == "Joypad Motion on Axis 0 (Left Stick X-Axis, Joystick 0 X-Axis) with Value -0.26" &&
            rawAxis.AsText() == "Joypad Motion on Axis 9 (Joystick 4 Y-Axis) with Value 0.00" &&
            axisOne.AsText() == "Joypad Motion on Axis 0 (Left Stick X-Axis, Joystick 0 X-Axis) with Value 1.00" &&
            axisNegativeOne.AsText() == "Joypad Motion on Axis 0 (Left Stick X-Axis, Joystick 0 X-Axis) with Value -1.00" &&
            axisMidpoint.AsText() == "Joypad Motion on Axis 0 (Left Stick X-Axis, Joystick 0 X-Axis) with Value 0.12",
        "Controller text must retain numbered descriptions, safe extended IDs, pressure and two-decimal axes.");

    var previousCulture = TranslationServer.Culture;
    var previousEnabled = TranslationServer.Enabled;
    try
    {
        TranslationServer.Clear();
        TranslationServer.Enabled = true;
        TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-FR");
        var french = CultureInfo.GetCultureInfo("fr");
        TranslationServer.AddTranslation(french, "", "Joypad Button %d", "Bouton de manette %d");
        TranslationServer.AddTranslation(french, "", "Bottom Action, Sony Cross, Xbox A, Nintendo B", "Bouton inférieur");
        TranslationServer.AddTranslation(french, "", "Pressure:", "Pression :");
        TranslationServer.AddTranslation(french, "", "Joypad Motion on Axis %d (%s) with Value %.2f", "Axe %d (%s) : %.2f");
        TranslationServer.AddTranslation(french, "", "Left Stick X-Axis, Joystick 0 X-Axis", "Axe X gauche");
        TranslationServer.AddTranslation(french, "", "Unknown Joypad Axis", "Axe inconnu");
        Require(button.AsText() == "Bouton de manette 0 (Bouton inférieur)" &&
                pressure.AsText() == "Bouton de manette 0 (Bouton inférieur), Pression : 0.10000000149012" &&
                axis.AsText() == "Axe 0 (Axe X gauche) : -0.26" &&
                unknownAxis.AsText() == "Axe 10 (Axe inconnu) : 0.00",
            "Controller descriptions must resolve translated templates, known labels and unknown-axis fallback.");
        TranslationServer.AddTranslation(french, "", "Joypad Button %d", "Bouton sans index");
        TranslationServer.AddTranslation(french, "", "Joypad Motion on Axis %d (%s) with Value %.2f", "Axe sans valeurs");
        Require(button.AsText() == "Joypad Button 0 (Bouton inférieur)" &&
                axis.AsText() == "Joypad Motion on Axis 0 (Axe X gauche) with Value -0.26",
            "Malformed translated controller templates must fall back to the source templates.");
        button.CanTranslateMessages = false;
        axis.CanTranslateMessages = false;
        Require(button.AsText() == "Joypad Button 0 (Bottom Action, Sony Cross, Xbox A, Nintendo B)" &&
                axis.AsText() == "Joypad Motion on Axis 0 (Left Stick X-Axis, Joystick 0 X-Axis) with Value -0.26",
            "Disabling translation on controller events must restore source descriptions.");
    }
    finally
    {
        TranslationServer.Clear();
        TranslationServer.Culture = previousCulture;
        TranslationServer.Enabled = previousEnabled;
    }
}

static void VerifyTouchGestureText()
{
    using var touch = new InputEventScreenTouch
    {
        Index = 3,
        Position = new Vector2(1f, 2f),
        Pressed = true,
        DoubleTap = true,
    };
    using var drag = new InputEventScreenDrag
    {
        Index = 4,
        Position = new Vector2(1f, 2f),
        Velocity = new Vector2(3f, 4f),
    };
    using var magnify = new InputEventMagnifyGesture { Position = new Vector2(1f, 2f), Factor = 0.1f };
    using var nonfiniteMagnify = new InputEventMagnifyGesture
    {
        Position = new Vector2(float.NaN, 0f),
        Factor = float.PositiveInfinity,
    };
    using var pan = new InputEventPanGesture { Position = new Vector2(1f, 2f), Delta = new Vector2(3f, 4f) };
    Require(touch.AsText() == "Screen touched at ((1.0, 2.0)) with 3 touch points" &&
            drag.AsText() == "Screen dragged with 4 touch points at position ((1.0, 2.0)) with velocity of ((3.0, 4.0))" &&
            magnify.AsText() == "Magnify Gesture at ((1.0, 2.0)) with factor 0.10000000149012" &&
            nonfiniteMagnify.AsText() == "Magnify Gesture at ((nan, 0.0)) with factor inf" &&
            pan.AsText() == "Pan Gesture at ((1.0, 2.0)) with delta ((3.0, 4.0))",
        "Touch and gesture text must retain source status, signed index, vector and factor formats.");
    magnify.Factor = BitConverter.Int32BitsToSingle(unchecked((int)0x5c4bef60));
    Require(magnify.AsText() == "Magnify Gesture at ((1.0, 2.0)) with factor 229610463472648192.0",
        "Float-to-double factor text must retain every large integral digit.");
    magnify.Factor = 0.1f;
    touch.Canceled = true;
    Require(touch.AsText() == "Screen canceled at ((1.0, 2.0)) with 3 touch points",
        "Cancellation must take precedence over a stored touch press in text.");
    touch.Canceled = false;
    touch.Pressed = false;
    touch.Index = -2;
    Require(touch.AsText() == "Screen released at ((1.0, 2.0)) with -2 touch points",
        "Touch text must preserve signed contact indexes and release state.");
    touch.Index = 3;
    touch.Pressed = true;
    drag.Index = -4;
    Require(drag.AsText() == "Screen dragged with -4 touch points at position ((1.0, 2.0)) with velocity of ((3.0, 4.0))",
        "Drag text must preserve a signed contact index.");
    drag.Index = 4;

    var previousCulture = TranslationServer.Culture;
    var previousEnabled = TranslationServer.Enabled;
    try
    {
        TranslationServer.Clear();
        TranslationServer.Enabled = true;
        TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-FR");
        var french = CultureInfo.GetCultureInfo("fr");
        TranslationServer.AddTranslation(french, "", "touched", "touché");
        TranslationServer.AddTranslation(french, "", "canceled", "annulé");
        TranslationServer.AddTranslation(french, "", "released", "relâché");
        TranslationServer.AddTranslation(french, "", "Screen %s at (%s) with %s touch points", "Écran %s à (%s), contact %s");
        TranslationServer.AddTranslation(french, "", "Screen dragged with %s touch points at position (%s) with velocity of (%s)",
            "Glissé %s à (%s), vitesse (%s)");
        TranslationServer.AddTranslation(french, "", "Magnify Gesture at (%s) with factor %s", "Zoom à (%s), facteur %s");
        TranslationServer.AddTranslation(french, "", "Pan Gesture at (%s) with delta (%s)", "Panoramique à (%s), delta (%s)");
        Require(touch.AsText() == "Écran touché à ((1.0, 2.0)), contact 3" &&
                drag.AsText() == "Glissé 4 à ((1.0, 2.0)), vitesse ((3.0, 4.0))" &&
                magnify.AsText() == "Zoom à ((1.0, 2.0)), facteur 0.10000000149012" &&
                pan.AsText() == "Panoramique à ((1.0, 2.0)), delta ((3.0, 4.0))",
            "Touch and gesture descriptions must resolve translated source templates.");
        touch.Canceled = true;
        Require(touch.AsText() == "Écran annulé à ((1.0, 2.0)), contact 3",
            "Translated cancellation must take precedence over a stored touch press.");
        touch.Canceled = false;
        touch.Pressed = false;
        Require(touch.AsText() == "Écran relâché à ((1.0, 2.0)), contact 3",
            "Translated touch release must use its own status word.");
        touch.Pressed = true;
        TranslationServer.AddTranslation(french, "", "Pan Gesture at (%s) with delta (%s)", "Pan sans arguments");
        Require(pan.AsText() == "Pan Gesture at ((1.0, 2.0)) with delta ((3.0, 4.0))",
            "An invalid translated gesture template must fall back to source wording.");
        touch.CanTranslateMessages = false;
        Require(touch.AsText() == "Screen touched at ((1.0, 2.0)) with 3 touch points",
            "Disabling translation on a touch event must restore its source text.");
    }
    finally
    {
        TranslationServer.Clear();
        TranslationServer.Culture = previousCulture;
        TranslationServer.Enabled = previousEnabled;
    }
}

static void VerifyInput()
{
    const string jump = "tests.input.jump";
    const string left = "tests.input.left";
    const string right = "tests.input.right";
    const string up = "tests.input.up";
    const string down = "tests.input.down";
    const string full = "tests.input.full";
    var map = InputMap.Instance;
    var input = Input.Instance;
    var actionNames = new[] { jump, left, right, up, down, full };
    var bindings = new List<InputEvent>();

    input.ReleasePressedEvents();
    foreach (var action in actionNames)
    {
        if (map.HasAction(action))
            map.EraseAction(action);
    }

    try
    {
        Require(ReferenceEquals(input, Input.Instance) && ReferenceEquals(map, InputMap.Instance),
            "Input and InputMap must be process-wide singletons.");
        Require((int)Input.MouseModeEnum.ConfinedHidden == 4 && (int)Input.CursorShape.Help == 16,
            "Input pointer enums retain the reference numeric identities.");
        Expect<InvalidOperationException>(() => _ = input.MouseMode,
            "Native pointer mode cannot be queried without a display.");
        Expect<InvalidOperationException>(() => input.SetDefaultCursorShape(),
            "Cursor requests cannot silently succeed without a display.");
        Expect<ArgumentException>(() => input.WarpMouse(new Vector2(float.NaN, 0)),
            "Pointer warping rejects nonfinite coordinates before requesting a display.");
        Expect<InvalidOperationException>(input.Dispose, "The process-wide Input service must reject disposal.");
        Expect<InvalidOperationException>(map.Dispose, "The process-wide InputMap service must reject disposal.");
        Expect<ArgumentException>(() => map.AddAction(" "), "Input action names must reject whitespace.");
        Expect<ArgumentOutOfRangeException>(() => map.AddAction("tests.invalid", float.NaN),
            "Action deadzones must reject NaN.");
        var disposedEvent = new InputEventKey();
        disposedEvent.Dispose();
        Expect<ObjectDisposedException>(() => input.ParseInputEvent(disposedEvent),
            "Input parsing must reject a disposed event before changing state.");
        Expect<ObjectDisposedException>(() => _ = disposedEvent.Keycode,
            "Concrete input-event properties must reject access after disposal.");

        var propertyCases = new (InputEvent Event, string[] Names)[]
        {
            (new InputEventAction(), [nameof(InputEvent.Device), nameof(InputEventAction.Action), nameof(InputEventAction.EventIndex), nameof(InputEventAction.Pressed), nameof(InputEventAction.Strength)]),
            (new InputEventKey(), [nameof(InputEvent.Device), nameof(InputEventFromWindow.WindowID), nameof(InputEventWithModifiers.AltPressed), nameof(InputEventWithModifiers.ShiftPressed), nameof(InputEventWithModifiers.ControlPressed), nameof(InputEventWithModifiers.MetaPressed), nameof(InputEventWithModifiers.CommandOrControlAutoremap), nameof(InputEventKey.Pressed), nameof(InputEventKey.Echo), nameof(InputEventKey.Keycode), nameof(InputEventKey.PhysicalKeycode), nameof(InputEventKey.KeyLabel), nameof(InputEventKey.Unicode), nameof(InputEventKey.Location)]),
            (new InputEventMouseButton(), [nameof(InputEventMouse.ButtonMask), nameof(InputEventMouse.Position), nameof(InputEventMouse.GlobalPosition), nameof(InputEventMouseButton.ButtonIndex), nameof(InputEventMouseButton.Pressed), nameof(InputEventMouseButton.Canceled), nameof(InputEventMouseButton.DoubleClick), nameof(InputEventMouseButton.Factor)]),
            (new InputEventMouseMotion(), [nameof(InputEventMouseMotion.PenInverted), nameof(InputEventMouseMotion.Pressure), nameof(InputEventMouseMotion.Relative), nameof(InputEventMouseMotion.ScreenRelative), nameof(InputEventMouseMotion.Velocity), nameof(InputEventMouseMotion.ScreenVelocity), nameof(InputEventMouseMotion.Tilt)]),
            (new InputEventJoypadButton(), [nameof(InputEventJoypadButton.ButtonIndex), nameof(InputEventJoypadButton.Pressed), nameof(InputEventJoypadButton.Pressure)]),
            (new InputEventJoypadMotion(), [nameof(InputEventJoypadMotion.Axis), nameof(InputEventJoypadMotion.AxisValue)]),
            (new InputEventScreenTouch(), [nameof(InputEventScreenTouch.Index), nameof(InputEventScreenTouch.Position), nameof(InputEventScreenTouch.Pressed), nameof(InputEventScreenTouch.Canceled), nameof(InputEventScreenTouch.DoubleTap)]),
            (new InputEventScreenDrag(), [nameof(InputEventScreenDrag.Index), nameof(InputEventScreenDrag.PenInverted), nameof(InputEventScreenDrag.Position), nameof(InputEventScreenDrag.Pressure), nameof(InputEventScreenDrag.Relative), nameof(InputEventScreenDrag.ScreenRelative), nameof(InputEventScreenDrag.Velocity), nameof(InputEventScreenDrag.ScreenVelocity), nameof(InputEventScreenDrag.Tilt)]),
            (new InputEventMagnifyGesture(), [nameof(InputEventGesture.Position), nameof(InputEventMagnifyGesture.Factor)]),
            (new InputEventPanGesture(), [nameof(InputEventGesture.Position), nameof(InputEventPanGesture.Delta)]),
        };
        try
        {
            foreach (var (inputEvent, names) in propertyCases)
            {
                var descriptors = inputEvent.GetPropertyList();
                Require(names.All(name => descriptors.Single(property => property.Name == name).IsStored),
                    $"{inputEvent.GetType().Name} must expose every event value through stored typed property descriptors.");
                Require(!inputEvent.PropertyCanRevert(descriptors.Single(property => property.Name == nameof(InputEvent.Device))),
                    $"{inputEvent.GetType().Name} must report its constructor device as the descriptor default.");
            }

            var describedKey = (InputEventKey)propertyCases[1].Event;
            var keycodeProperty = describedKey.GetPropertyList()
                .OfType<PropertyDescriptor<InputEventKey, Key>>()
                .Single(property => property.Name == nameof(InputEventKey.Keycode));
            keycodeProperty.SetValue(describedKey, Key.Enter);
            Require(describedKey.Keycode == Key.Enter,
                "An input-event property descriptor must update the validated public property.");
            using var duplicatedKey = (InputEventKey)describedKey.Duplicate();
            Require(duplicatedKey.Keycode == Key.Enter && duplicatedKey.Device == InputEvent.DeviceIdKeyboard,
                "Input-event duplication must preserve concrete stored state and exact runtime type.");
            var windowProperty = describedKey.GetPropertyList()
                .OfType<PropertyDescriptor<InputEventFromWindow, long>>()
                .Single(property => property.Name == nameof(InputEventFromWindow.WindowID));
            Require(describedKey.WindowID == DisplayServer.MainWindowId && !describedKey.PropertyCanRevert(windowProperty),
                "Window events default to the primary window ID in the inherited typed descriptor.");
            var observedWindowID = 0L;
            describedKey.Changed += _ => observedWindowID = describedKey.WindowID;
            windowProperty.SetValue(describedKey, long.MaxValue);
            using var copiedWindowKey = (InputEventKey)describedKey.Duplicate();
            Require(observedWindowID == long.MaxValue && copiedWindowKey.WindowID == long.MaxValue &&
                describedKey.PropertyCanRevert(windowProperty),
                "Window IDs retain their signed 64-bit range across mutation, change delivery and duplication.");
            describedKey.WindowID = long.MinValue;
            Require(describedKey.WindowID == long.MinValue && observedWindowID == long.MinValue,
                "Negative window IDs are retained without validation or narrowing.");
            describedKey.RevertProperty(windowProperty);
            Require(describedKey.WindowID == DisplayServer.MainWindowId && !describedKey.PropertyCanRevert(windowProperty),
                "Reverting the inherited window descriptor restores the primary ID.");
            var describedTouch = (InputEventScreenTouch)propertyCases[6].Event;
            describedTouch.WindowID = long.MaxValue;
            using var copiedWindowTouch = (InputEventScreenTouch)describedTouch.Duplicate();
            Require(copiedWindowTouch.WindowID == long.MaxValue,
                "The direct touch subclass retains the same inherited window identity on duplication.");
            Require(describedKey.IsActionType() && !propertyCases[3].Event.IsActionType(),
                "Only the sealed key, button, axis, and direct-action event families may be action bindings.");
            foreach (var (inputEvent, _) in propertyCases)
            {
                var bindable = inputEvent is InputEventAction or InputEventKey or InputEventMouseButton or
                    InputEventJoypadButton or InputEventJoypadMotion;
                Require(inputEvent.IsActionType() == bindable && !inputEvent.IsEcho(),
                    $"{inputEvent.GetType().Name} reports its pinned action-family and echo defaults.");
            }
            describedKey.Echo = true;
            Require(describedKey.IsEcho() &&
                    !describedKey.Accumulate(propertyCases[0].Event) &&
                    ReferenceEquals(describedKey.XformedBy(Transform.Identity), describedKey),
                "Only key repeat overrides echo; a non-positional key keeps the base merge and transform results.");
            var directAction = (InputEventAction)propertyCases[0].Event;
            var deviceProperty = directAction.GetPropertyList()
                .OfType<PropertyDescriptor<InputEvent, int>>()
                .Single(property => property.Name == nameof(InputEvent.Device));
            Require(directAction.Device == 0 && !directAction.PropertyCanRevert(deviceProperty),
                "The base input event device defaults to zero in the typed descriptor.");
            directAction.Device = int.MinValue;
            using var copiedDirectAction = (InputEventAction)directAction.Duplicate();
            Require(copiedDirectAction.Device == int.MinValue && directAction.PropertyCanRevert(deviceProperty),
                "Device IDs retain their signed range across event copies.");
            directAction.RevertProperty(deviceProperty);
            Require(directAction.Device == 0 && ReferenceEquals(directAction.XformedBy(Transform.Identity), directAction),
                "Device revert and non-positional action transforms retain the source event.");
            var stateButton = (InputEventMouseButton)propertyCases[2].Event;
            foreach (var pressed in new[] { false, true })
            {
                foreach (var canceled in new[] { false, true })
                {
                    stateButton.Pressed = pressed;
                    stateButton.Canceled = canceled;
                    Require(stateButton.IsCanceled() == canceled &&
                            stateButton.IsPressed() == (pressed && !canceled) &&
                            stateButton.IsReleased() == (!pressed && !canceled),
                        "Base event state queries use the complete pressed/canceled truth table.");
                }
            }
        }
        finally
        {
            foreach (var (inputEvent, _) in propertyCases)
                inputEvent.Dispose();
        }

        using (var modifiers = new InputEventKey())
        using (var copiedModifiers = new InputEventMouseButton())
        using (var gestureModifiers = new InputEventPanGesture())
        {
            Require(modifiers.Device == InputEvent.DeviceIdKeyboard &&
                    copiedModifiers.Device == InputEvent.DeviceIdMouse && gestureModifiers.Device == 0 &&
                    modifiers.GetModifiersMask() == 0 && !modifiers.IsCommandOrControlPressed(),
                "Modifier-bearing events retain their distinct inherited device defaults and empty mask.");
            var deviceProperty = modifiers.GetPropertyList()
                .OfType<PropertyDescriptor<InputEvent, int>>()
                .Single(property => property.Name == nameof(InputEvent.Device));
            modifiers.Device = 9;
            modifiers.RevertProperty(deviceProperty);
            Require(modifiers.Device == InputEvent.DeviceIdKeyboard,
                "The inherited device descriptor reverts to the modifier-bearing keyboard default.");

            modifiers.AltPressed = true;
            modifiers.ShiftPressed = true;
            modifiers.ControlPressed = true;
            modifiers.MetaPressed = true;
            Require(modifiers.GetModifiersMask() == (KeyModifierMask.Control | KeyModifierMask.Shift |
                    KeyModifierMask.Alt | KeyModifierMask.Meta),
                "The modifier mask includes each independently stored bit.");
            var listChanges = 0;
            copiedModifiers.PropertyListChanged += _ => listChanges++;
            modifiers.CommandOrControlAutoremap = true;
            copiedModifiers.SetModifiersFromEvent(modifiers);
            Require(listChanges == 1 && copiedModifiers.CommandOrControlAutoremap &&
                    copiedModifiers.GetModifiersMask() == modifiers.GetModifiersMask() &&
                    copiedModifiers.IsCommandOrControlPressed() &&
                    copiedModifiers.Device == InputEvent.DeviceIdMouse,
                "Modifier copying commits autoremap, updates its property schema and preserves device identity.");
            copiedModifiers.SetModifiersFromEvent(modifiers);
            Require(listChanges == 1, "Copying an unchanged autoremap policy does not invalidate the property list.");
            Expect<InvalidOperationException>(() => copiedModifiers.ControlPressed = false,
                "Direct Control assignment is rejected while portable autoremap is enabled.");
            Expect<InvalidOperationException>(() => copiedModifiers.MetaPressed = false,
                "Direct Meta assignment is rejected while portable autoremap is enabled.");
            modifiers.CommandOrControlAutoremap = false;
            copiedModifiers.SetModifiersFromEvent(modifiers);
            Require(listChanges == 2 && !copiedModifiers.IsCommandOrControlPressed() &&
                    copiedModifiers.GetModifiersMask() == (KeyModifierMask.Alt | KeyModifierMask.Shift),
                "Disabling autoremap clears concrete command/control bits and updates copied metadata.");
        }

        using (var motion = new InputEventMouseMotion())
        using (var button = new InputEventMouseButton())
        {
            var deviceProperty = motion.GetPropertyList()
                .OfType<PropertyDescriptor<InputEvent, int>>()
                .Single(property => property.Name == nameof(InputEvent.Device));
            Require(motion.Device == InputEvent.DeviceIdMouse && button.Device == InputEvent.DeviceIdMouse &&
                    motion.ButtonMask == MouseButtonMask.None && motion.Position == Vector2.Zero &&
                    motion.GlobalPosition == Vector2.Zero && !motion.PropertyCanRevert(deviceProperty),
                "Both mouse subclasses inherit the pinned device and value defaults.");
            motion.Device = 23;
            motion.RevertProperty(deviceProperty);
            Require(motion.Device == InputEvent.DeviceIdMouse,
                "The inherited device descriptor restores the mouse default.");

            var rawMask = MouseButtonMask.Left | (MouseButtonMask)int.MinValue;
            var observedMask = MouseButtonMask.None;
            motion.Changed += _ => observedMask = motion.ButtonMask;
            motion.ButtonMask = rawMask;
            motion.Position = new Vector2(float.NaN, float.PositiveInfinity);
            motion.GlobalPosition = new Vector2(float.NegativeInfinity, 7f);
            using var copiedMotion = (InputEventMouseMotion)motion.Duplicate();
            Require(observedMask == rawMask && copiedMotion.ButtonMask == rawMask &&
                    float.IsNaN(copiedMotion.Position.X) && float.IsPositiveInfinity(copiedMotion.Position.Y) &&
                    float.IsNegativeInfinity(copiedMotion.GlobalPosition.X) && copiedMotion.GlobalPosition.Y == 7f,
                "Mouse fields preserve arbitrary source bits and coordinates across notification and duplication.");
            Expect<ArgumentOutOfRangeException>(() => motion.XformedBy(Transform.Identity),
                "A non-finite local mouse position is rejected at the positional transform boundary.");

            button.ButtonMask = rawMask;
            button.Position = new Vector2(2f, 3f);
            button.GlobalPosition = new Vector2(float.NaN, 4f);
            using var transformedButton = (InputEventMouseButton)button.XformedBy(Transform.Identity);
            Require(transformedButton.ButtonMask == rawMask &&
                    float.IsNaN(transformedButton.GlobalPosition.X) && transformedButton.GlobalPosition.Y == 4f &&
                    transformedButton.Position == button.Position,
                "Mouse transforms preserve the source global position and bitfield on the button subclass.");
        }

        using (var invalidAction = new InputEventAction())
        using (var invalidKey = new InputEventKey())
        using (var invalidMotion = new InputEventMouseMotion())
        using (var invalidTouch = new InputEventScreenTouch())
        {
            Expect<ArgumentOutOfRangeException>(() => invalidAction.Strength = float.NaN,
                "Direct action strength must reject NaN.");
            Expect<ArgumentOutOfRangeException>(() => invalidKey.Unicode = 0xD800,
                "Keyboard Unicode values must reject surrogate code points.");
            invalidKey.CommandOrControlAutoremap = true;
            Expect<InvalidOperationException>(() => invalidKey.ControlPressed = true,
                "Portable command-or-control mode must reject direct control mutation.");
            Require(!invalidMotion.PenInverted && invalidMotion.Pressure == 0f &&
                    invalidMotion.Relative == Vector2.Zero && invalidMotion.ScreenRelative == Vector2.Zero &&
                    invalidMotion.Velocity == Vector2.Zero && invalidMotion.ScreenVelocity == Vector2.Zero &&
                    invalidMotion.Tilt == Vector2.Zero,
                "Mouse-motion values begin at the pinned zero defaults.");
            invalidMotion.PenInverted = true;
            invalidMotion.Pressure = 1.01f;
            invalidMotion.Tilt = new Vector2(0f, float.PositiveInfinity);
            invalidMotion.Relative = new Vector2(float.NaN, 2f);
            invalidMotion.ScreenRelative = new Vector2(float.NegativeInfinity, 3f);
            invalidMotion.Velocity = new Vector2(float.PositiveInfinity, 4f);
            invalidMotion.ScreenVelocity = new Vector2(float.NaN, 5f);
            using var copiedMotionValues = (InputEventMouseMotion)invalidMotion.Duplicate();
            Require(copiedMotionValues.PenInverted && copiedMotionValues.Pressure == 1.01f &&
                    float.IsPositiveInfinity(copiedMotionValues.Tilt.Y) &&
                    float.IsNaN(copiedMotionValues.Relative.X) && float.IsNegativeInfinity(copiedMotionValues.ScreenRelative.X) &&
                    float.IsPositiveInfinity(copiedMotionValues.Velocity.X) && float.IsNaN(copiedMotionValues.ScreenVelocity.X),
                "Mouse motion retains source pressure, tilt, local and screen vectors without normalization.");
            Expect<ArgumentOutOfRangeException>(() => invalidMotion.XformedBy(Transform.Identity),
                "Non-finite local motion is rejected when a positional transform is requested.");
            using var signedDrag = new InputEventScreenDrag();
            Require(invalidTouch.Index == 0 && signedDrag.Index == 0,
                "Touch and drag indexes must default to zero.");
            invalidTouch.Index = int.MinValue;
            signedDrag.Index = int.MinValue;
            using var copiedTouch = (InputEventScreenTouch)invalidTouch.Duplicate();
            using var copiedDrag = (InputEventScreenDrag)signedDrag.Duplicate();
            Require(invalidTouch.Index == int.MinValue && copiedTouch.Index == int.MinValue &&
                    signedDrag.Index == int.MinValue && copiedDrag.Index == int.MinValue,
                "Touch and drag indexes must preserve signed values across duplication.");
            invalidTouch.Position = new Vector2(float.NaN, float.PositiveInfinity);
            invalidTouch.Pressed = true;
            invalidTouch.Canceled = true;
            invalidTouch.DoubleTap = true;
            using var copiedTouchState = (InputEventScreenTouch)invalidTouch.Duplicate();
            Require(float.IsNaN(copiedTouchState.Position.X) && float.IsPositiveInfinity(copiedTouchState.Position.Y) &&
                    copiedTouchState.Canceled && copiedTouchState.DoubleTap &&
                    !copiedTouchState.Pressed && !copiedTouchState.IsReleased(),
                "Touch values retain source coordinates and canceled press state across duplication.");
            invalidTouch.Canceled = false;
            Require(invalidTouch.Pressed && invalidTouch.IsPressed(),
                "Clearing touch cancellation restores the stored press.");
            Expect<ArgumentOutOfRangeException>(() => invalidTouch.XformedBy(Transform.Identity),
                "A non-finite touch position is rejected only at the positional transform boundary.");

            signedDrag.PenInverted = true;
            signedDrag.Position = new Vector2(float.NaN, 3f);
            signedDrag.Pressure = 1.5f;
            signedDrag.Relative = new Vector2(float.PositiveInfinity, 4f);
            signedDrag.ScreenRelative = new Vector2(float.NegativeInfinity, 5f);
            signedDrag.Velocity = new Vector2(float.NaN, 6f);
            signedDrag.ScreenVelocity = new Vector2(float.PositiveInfinity, 7f);
            signedDrag.Tilt = new Vector2(2f, float.NaN);
            using var copiedDragState = (InputEventScreenDrag)signedDrag.Duplicate();
            Require(copiedDragState.PenInverted && copiedDragState.Pressure == 1.5f &&
                    float.IsNaN(copiedDragState.Position.X) && float.IsPositiveInfinity(copiedDragState.Relative.X) &&
                    float.IsNegativeInfinity(copiedDragState.ScreenRelative.X) && float.IsNaN(copiedDragState.Velocity.X) &&
                    float.IsPositiveInfinity(copiedDragState.ScreenVelocity.X) && copiedDragState.Tilt.X == 2f &&
                    float.IsNaN(copiedDragState.Tilt.Y),
                "Drag values retain source pen and motion components across duplication.");
            Expect<ArgumentOutOfRangeException>(() => signedDrag.XformedBy(Transform.Identity),
                "A non-finite drag position is rejected only at the positional transform boundary.");
        }

        using (var mouseButton = new InputEventMouseButton())
        {
            Require(mouseButton.ButtonIndex == MouseButton.None && mouseButton.Factor == 1f &&
                    !mouseButton.Pressed && !mouseButton.Canceled && !mouseButton.DoubleClick,
                "Mouse-button defaults match the pinned reference values.");
            var observedFactor = 1f;
            mouseButton.Changed += _ => observedFactor = mouseButton.Factor;
            mouseButton.ButtonIndex = (MouseButton)int.MinValue;
            mouseButton.Factor = -2f;
            Require(observedFactor == -2f, "A negative source factor commits before change delivery.");
            mouseButton.Factor = float.NaN;
            mouseButton.DoubleClick = true;
            mouseButton.Pressed = true;
            mouseButton.Canceled = true;
            using var copiedButton = (InputEventMouseButton)mouseButton.Duplicate();
            Require(copiedButton.ButtonIndex == (MouseButton)int.MinValue && float.IsNaN(copiedButton.Factor) &&
                    copiedButton.DoubleClick && copiedButton.Canceled && !copiedButton.Pressed &&
                    !copiedButton.IsPressed() && !copiedButton.IsReleased(),
                "Unknown button identity, arbitrary factor, double click and canceled press survive copying.");
            mouseButton.Canceled = false;
            Require(mouseButton.Pressed && mouseButton.IsPressed(),
                "Clearing cancellation restores the stored press state.");
        }

        using (var magnify = new InputEventMagnifyGesture())
        using (var pan = new InputEventPanGesture())
        {
            var deviceProperty = magnify.GetPropertyList()
                .OfType<PropertyDescriptor<InputEvent, int>>()
                .Single(property => property.Name == nameof(InputEvent.Device));
            Require(magnify.Device == 0 && pan.Device == 0 && magnify.Position == Vector2.Zero &&
                    pan.Position == Vector2.Zero && magnify.Factor == 1f && pan.Delta == Vector2.Zero &&
                    !magnify.PropertyCanRevert(deviceProperty),
                "Both gesture types use the overridden touch-device default and pinned value defaults.");
            magnify.Device = 7;
            using var copiedDeviceGesture = (InputEventMagnifyGesture)magnify.Duplicate();
            Require(copiedDeviceGesture.Device == 7 && magnify.PropertyCanRevert(deviceProperty),
                "The inherited device value survives gesture duplication.");
            magnify.RevertProperty(deviceProperty);
            Require(magnify.Device == 0, "The inherited device descriptor reverts to the gesture default.");

            var observedFactor = 1f;
            magnify.Changed += _ => observedFactor = magnify.Factor;
            magnify.Factor = -2f;
            Require(observedFactor == -2f, "Factor change observers see the committed source value.");
            magnify.Factor = 0f;
            using var zeroFactorCopy = (InputEventMagnifyGesture)magnify.Duplicate();
            Require(zeroFactorCopy.Factor == 0f, "Zero magnification factors are stored and copied.");
            magnify.Factor = float.PositiveInfinity;
            using var infiniteFactorCopy = (InputEventMagnifyGesture)magnify.Duplicate();
            Require(float.IsPositiveInfinity(infiniteFactorCopy.Factor),
                "The event retains a non-finite source factor without altering it.");
            magnify.Factor = float.NaN;
            Require(float.IsNaN(magnify.Factor), "The event retains a NaN source factor.");

            magnify.Position = new Vector2(float.NaN, 3f);
            using var positionCopy = (InputEventMagnifyGesture)magnify.Duplicate();
            Require(float.IsNaN(positionCopy.Position.X) && positionCopy.Position.Y == 3f,
                "Gesture position stores and duplicates source components verbatim.");
            Expect<ArgumentOutOfRangeException>(() => magnify.XformedBy(Transform.Identity),
                "A non-finite source position is rejected when entering the positional transform boundary.");
            pan.Delta = new Vector2(float.PositiveInfinity, float.NaN);
            using var deltaCopy = (InputEventPanGesture)pan.Duplicate();
            Require(float.IsPositiveInfinity(deltaCopy.Delta.X) && float.IsNaN(deltaCopy.Delta.Y),
                "Pan delta is stored and duplicated without normalization.");
        }

        map.AddAction(jump, 0.25f);
        map.AddAction(left);
        map.AddAction(right);
        map.AddAction(up);
        map.AddAction(down);
        map.AddAction(full);

        for (var index = 0; index < Input.MaxEventsPerAction; index++)
        {
            var binding = new InputEventKey { Keycode = (Key)('A' + index) };
            bindings.Add(binding);
            map.ActionAddEvent(full, binding);
        }
        using (var overflowBinding = new InputEventKey { Keycode = Key.F12 })
            Expect<InvalidOperationException>(() => map.ActionAddEvent(full, overflowBinding),
                "A 33rd distinct binding must fail without changing the action.");
        Require(map.ActionGetEvents(full).Count == Input.MaxEventsPerAction,
            "Rejected binding overflow must preserve the existing 32 bindings.");
        using (var overflowAction = new InputEventAction { Action = full, Pressed = true })
        {
            Expect<InvalidOperationException>(() => input.ParseInputEvent(overflowAction),
                "An unindexed direct action event must not exceed the 32-source action limit.");
            Require(!input.IsActionPressed(full),
                "A rejected direct action event must not mutate action state.");
        }
        using (var explicitOverflow = new InputEventAction
        {
            Action = jump,
            EventIndex = Input.MaxEventsPerAction,
            Pressed = true,
        })
        {
            Expect<InvalidOperationException>(() => input.ParseInputEvent(explicitOverflow),
                "Parsing an explicit index outside the 32-source range must fail before state mutation.");
            Require(!input.IsActionPressed(jump),
                "A rejected indexed direct event must leave its action released.");
        }

        var syntheticBinding = new InputEventAction { Action = up };
        bindings.Add(syntheticBinding);
        map.ActionAddEvent(up, syntheticBinding);
        Require(map.GetActionDescription(up) == "Action has no bound inputs" && syntheticBinding.AsText() == up,
            "Action descriptions must omit synthetic indirection while a synthetic event falls back to its name.");

        var jumpBinding = new InputEventKey { Keycode = Key.Space };
        bindings.Add(jumpBinding);
        map.ActionAddEvent(jump, jumpBinding);
        map.ActionAddEvent(jump, jumpBinding);
        Require(map.ActionGetEvents(jump).Count == 1 && map.ActionHasEvent(jump, jumpBinding) &&
                map.EventIsAction(new InputEventKey { Keycode = Key.Space, Pressed = true }, jump),
            "InputMap must ignore duplicate bindings and match a configured key.");

        var allDeviceBinding = new InputEventJoypadButton
        {
            Device = InputMap.AllDevices,
            ButtonIndex = JoyButton.B,
        };
        bindings.Add(allDeviceBinding);
        map.ActionAddEvent(up, allDeviceBinding);
        using (var deviceBinding = new InputEventJoypadButton { Device = 7, ButtonIndex = JoyButton.B })
        {
            Require(map.ActionHasEvent(up, deviceBinding),
                "An all-device binding must match exact binding queries from a concrete controller.");
            map.ActionAddEvent(up, deviceBinding);
            Require(map.ActionGetEvents(up).Count == 2,
                "A concrete-device binding already covered by an earlier all-device binding must be deduplicated.");
            Require(map.GetActionDescription(up) == allDeviceBinding.AsText(),
                "An action description must list each concrete binding once and omit synthetic indirection.");
        }

        var throwingBinding = new InputEventKey { Keycode = Key.L };
        throwingBinding.Changed += _ => throw new InvalidOperationException("expected binding observer failure");
        bindings.Add(throwingBinding);
        map.ActionAddEvent(left, throwingBinding);
        using (var throwingBindingPress = new InputEventKey { Keycode = Key.L, Pressed = true })
        {
            input.ParseInputEvent(throwingBindingPress);
            Require(input.IsActionPressed(left), "The failure-injection binding must first contribute to its action.");
            Expect<InvalidOperationException>(() => throwingBinding.Keycode = Key.M,
                "A throwing public binding observer must propagate after the binding changes.");
            Require(!input.IsActionPressed(left) && throwingBinding.Keycode == Key.M,
                "Internal action invalidation must precede fallible public binding observers.");
        }

        var disposableBinding = new InputEventKey { Keycode = Key.D };
        bindings.Add(disposableBinding);
        map.ActionAddEvent(down, disposableBinding);
        using (var disposableBindingPress = new InputEventKey { Keycode = Key.D, Pressed = true })
        {
            input.ParseInputEvent(disposableBindingPress);
            Require(input.IsActionPressed(down), "The disposable binding must first contribute to its action.");
            disposableBinding.Dispose();
            Require(!input.IsActionPressed(down) && map.ActionGetEvents(down).Count == 0,
                "Disposing a registered binding must remove it and clear its cached contribution.");
        }

        var modifiedBinding = new InputEventKey { Keycode = Key.J, ControlPressed = true };
        bindings.Add(modifiedBinding);
        map.ActionAddEvent(jump, modifiedBinding);
        using (var extraModifier = new InputEventKey
        {
            Keycode = Key.J,
            ControlPressed = true,
            ShiftPressed = true,
            Pressed = true,
        })
        {
            Require(extraModifier.IsAction(jump) && !extraModifier.IsAction(jump, exactMatch: true),
                "Non-exact action matching must allow extra modifiers while exact matching rejects them.");
        }

        var log = new List<string>();
        var root = new InputProbeNode("root", log)
        {
            InputEnabled = true,
            UnhandledKeyInputEnabled = true,
            UnhandledInputEnabled = true,
            PhysicsProcessEnabled = true,
            ObservedAction = jump,
        };
        var child = new InputProbeNode("child", log)
        {
            InputEnabled = true,
            UnhandledKeyInputEnabled = true,
            UnhandledInputEnabled = true,
            PhysicsProcessEnabled = true,
            ObservedAction = jump,
        };
        root.AddChild(child);

        using (var tree = new SceneTree(root))
        using (var press = new InputEventKey { Keycode = Key.Space, Pressed = true })
        {
            Engine.Instance.Start(tree);
            var wrongThreadError = Task.Run(() => Capture(() => input.ParseInputEvent(press))).Result;
            Require(wrongThreadError is InvalidOperationException && !input.IsKeyPressed(Key.Space) &&
                    !input.IsActionPressed(jump),
                "Off-owner input delivery must fail before raw or mapped state changes.");
            input.ParseInputEvent(press);
            Require(log.SequenceEqual([
                    "child:input", "root:input",
                    "child:key", "root:key",
                    "child:unhandled", "root:unhandled",
                ]) && child.SawPressedState && root.SawPressedState,
                "Scene input must run child-first by stage after committing state.");
            Require(input.IsKeyPressed(Key.Space) && input.IsActionPressed(jump) &&
                    input.IsActionJustPressed(jump) && input.IsActionJustPressedByEvent(jump, press) &&
                    input.GetActionStrength(jump) == 1f && input.GetActionRawStrength(jump) == 1f,
                "A key press must update raw, mapped, transition, and strength state.");

            tree.ProcessFrame(0d);
            Require(!input.IsActionJustPressed(jump),
                "The process transition must clear after the first process frame.");
            using (var frameRelease = new InputEventKey { Keycode = Key.Space, Pressed = false })
            {
                root.PhysicsInputAttempt = frameRelease;
                tree.PhysicsFrame(0d);
                root.PhysicsInputAttempt = null;
                Require(root.PhysicsSawJustPressed && child.PhysicsSawJustPressed &&
                        root.FrameInputError is InvalidOperationException && input.IsActionPressed(jump) &&
                        !input.IsActionJustPressed(jump),
                    "The physics lane must observe its transition while nested input fails before changing state.");
            }

            log.Clear();
            child.HandleInput = true;
            using var release = new InputEventKey { Keycode = Key.Space, Pressed = false };
            input.ParseInputEvent(release);
            Require(log.SequenceEqual(["child:input"]) && !input.IsKeyPressed(Key.Space) &&
                    !input.IsActionPressed(jump) && input.IsActionJustReleased(jump) &&
                    input.IsActionJustReleasedByEvent(jump, release),
                "Handled input must stop the current and later stages while preserving the committed release.");
            Expect<InvalidOperationException>(tree.SetInputAsHandled,
                "Input handled state must not escape synchronous dispatch.");

            child.HandleInput = false;
            child.ReenterInput = true;
            log.Clear();
            input.ParseInputEvent(press);
            Require(child.ReentryError is InvalidOperationException,
                "Input parsing must reject callback re-entry without corrupting outer delivery.");

            child.ReenterInput = false;
            child.ThrowOnInput = true;
            log.Clear();
            Expect<AggregateException>(() => input.ParseInputEvent(release),
                "A throwing node input callback must be reported after the remaining eligible nodes run.");
            Require(log.Contains("root:input") && !input.IsActionPressed(jump),
                "Input callback failures must not roll back state or skip unrelated nodes.");
            child.ThrowOnInput = false;

            tree.Paused = true;
            log.Clear();
            press.Pressed = true;
            input.ParseInputEvent(press);
            Require(log.Count == 0 && input.IsActionPressed(jump),
                "Paused-ineligible nodes must skip input callbacks after state is committed.");
            tree.Paused = false;

            root.InputEnabled = false;
            root.UnhandledKeyInputEnabled = false;
            root.UnhandledInputEnabled = false;
            child.InputEnabled = false;
            child.UnhandledKeyInputEnabled = false;
            child.UnhandledInputEnabled = false;
            for (var index = 0; index < 16; index++)
            {
                press.Pressed = true;
                input.ParseInputEvent(press);
                press.Pressed = false;
                input.ParseInputEvent(press);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < 256; index++)
            {
                press.Pressed = true;
                input.ParseInputEvent(press);
                press.Pressed = false;
                input.ParseInputEvent(press);
            }
            Require(GC.GetAllocatedBytesForCurrentThread() == allocatedBefore,
                "Warmed mapped input parsing and scene traversal must not allocate managed memory.");

            Engine.Instance.Stop();
        }

        using (var remapPress = new InputEventKey { Keycode = Key.Space, Pressed = true })
        {
            input.ParseInputEvent(remapPress);
            Require(input.IsActionPressed(jump), "A mapped source must be active before a live binding edit.");
            jumpBinding.Keycode = Key.Enter;
            Require(!input.IsActionPressed(jump),
                "Mutating a registered live binding must invalidate cached contributions for that action.");
            jumpBinding.Keycode = Key.Space;
        }

        input.ActionPress(right, 0.75f);
        input.ActionPress(down, 1f);
        Require(input.IsActionPressed(right) && input.GetActionStrength(right) == 0.75f &&
                VectorNearlyEqual(input.GetVector(left, right, up, down, 0f), new Vector2(0.6f, 0.8f)),
            "Synthetic action state and vector composition must preserve analog magnitude and clamp diagonals.");
        input.ActionRelease(right);
        input.ActionRelease(down);
        Expect<ArgumentOutOfRangeException>(() => input.GetVector(left, right, up, down, float.NaN),
            "Vector deadzones must reject NaN.");
        Expect<KeyNotFoundException>(() => input.ActionPress("tests.input.missing"),
            "Synthetic action changes must reject unregistered names.");

        using (var direct = new InputEventAction { Action = jump, Pressed = true, Strength = 0.4f })
        {
            input.ParseInputEvent(direct);
            Require(input.IsActionPressed(jump) && input.GetActionStrength(jump) == 0.4f,
                "A direct action event must update its registered action without a hardware binding.");
            direct.Pressed = false;
            input.ParseInputEvent(direct);
            Require(!input.IsActionPressed(jump), "A direct action release must remove its synthetic event source.");
        }
        using (var negative = new InputEventAction { Action = jump, Device = 4, EventIndex = int.MinValue, Pressed = true })
        using (var release = new InputEventAction { Action = jump, Device = 4, EventIndex = -1 })
        {
            input.ParseInputEvent(negative);
            Require(input.IsActionPressed(jump), "A negative index selects the action's post-binding source slot.");
            input.ParseInputEvent(release);
            Require(!input.IsActionPressed(jump),
                "Another negative index on the same device must release that source slot.");
        }
        using (var weak = new InputEventAction { Action = jump, Device = 10, EventIndex = 0, Pressed = true, Strength = 0f })
        using (var strong = new InputEventAction { Action = jump, Device = 10, EventIndex = 1, Pressed = true, Strength = 0.6f })
        {
            input.ParseInputEvent(weak);
            Require(input.IsActionPressed(jump) && input.GetActionStrength(jump) == 0f,
                "A zero-strength indexed action source remains logically pressed.");
            input.ParseInputEvent(strong);
            Require(input.IsActionPressed(jump) && input.GetActionStrength(jump) == 0.6f,
                "Independent direct action indexes combine their maximum strength.");
            weak.Pressed = false;
            input.ParseInputEvent(weak);
            Require(input.IsActionPressed(jump) && input.GetActionStrength(jump) == 0.6f,
                "Releasing one direct action index must preserve another pressed source.");
            strong.Pressed = false;
            input.ParseInputEvent(strong);
            Require(!input.IsActionPressed(jump),
                "Releasing the final indexed source must clear the action press.");
        }

        using (var motion = new InputEventMouseMotion
        {
            ButtonMask = MouseButtonMask.Left,
            Position = new Vector2(5f, 6f),
            Velocity = new Vector2(20f, 30f),
            ScreenVelocity = new Vector2(40f, 50f),
        })
        {
            input.ParseInputEvent(motion);
            Require(input.MouseButtonMask == MouseButtonMask.Left &&
                    input.IsMouseButtonPressed(MouseButton.Left) &&
                    !input.IsMouseButtonPressed(MouseButton.WheelUp) &&
                    input.LastMouseVelocity == new Vector2(20f, 30f) &&
                    input.LastMouseScreenVelocity == new Vector2(40f, 50f),
                "Mouse motion must publish held buttons and both velocity coordinate spaces.");
            Expect<ArgumentOutOfRangeException>(() => input.IsMouseButtonPressed((MouseButton)99),
                "Mouse-button queries must reject unknown identifiers.");
        }

        using (var button = new InputEventJoypadButton { Device = 2, ButtonIndex = JoyButton.A, Pressed = true })
        using (var axis = new InputEventJoypadMotion { Device = 2, Axis = JoyAxis.LeftX, AxisValue = -0.7f })
        using (var rawButton = new InputEventJoypadButton { Device = 2, ButtonIndex = (JoyButton)int.MinValue, Pressed = true })
        using (var rawAxis = new InputEventJoypadMotion { Device = 2, Axis = JoyAxis.Max, AxisValue = 1.5f })
        {
            input.ParseInputEvent(button);
            input.ParseInputEvent(axis);
            input.ParseInputEvent(rawButton);
            input.ParseInputEvent(rawAxis);
            Require(input.IsJoyButtonPressed(JoyButton.A, 2) && NearlyEqual(input.GetJoyAxis(JoyAxis.LeftX, 2), -0.7f),
                "Controller events must retain per-device button and axis state.");
            Require(input.IsJoyButtonPressed((JoyButton)int.MinValue, 2) &&
                    input.GetJoyAxis(JoyAxis.Max, 2) == 1.5f &&
                    !input.IsJoyButtonPressed(JoyButton.Invalid, 2) &&
                    input.GetJoyAxis(JoyAxis.Invalid, 2) == 0f &&
                    !input.IsJoyButtonPressed((JoyButton)int.MinValue, 3) &&
                    input.GetJoyAxis(JoyAxis.Max, 3) == 0f,
                "Controller queries retain arbitrary signed raw identities and source values.");
            Expect<ArgumentOutOfRangeException>(() => input.IsJoyButtonPressed(JoyButton.A, -1),
                "Controller button queries reject negative physical device IDs.");
            Expect<ArgumentOutOfRangeException>(() => input.GetJoyAxis(JoyAxis.LeftX, -1),
                "Controller axis queries reject negative physical device IDs.");
            rawButton.Pressed = false;
            rawAxis.AxisValue = float.PositiveInfinity;
            input.ParseInputEvent(rawButton);
            input.ParseInputEvent(rawAxis);
            Require(!input.IsJoyButtonPressed((JoyButton)int.MinValue, 2) &&
                    float.IsPositiveInfinity(input.GetJoyAxis(JoyAxis.Max, 2)),
                "Raw controller queries reflect button release and non-finite axis updates.");
            rawAxis.AxisValue = 0f;
            input.ParseInputEvent(rawAxis);
            Require(input.GetJoyAxis(JoyAxis.Max, 2) == 0f,
                "A later resting axis event replaces the prior raw value.");
        }

        var vectorBinding = new InputEventJoypadMotion { Axis = JoyAxis.RightX, AxisValue = 1f };
        bindings.Add(vectorBinding);
        map.ActionSetDeadzone(right, 0.5f);
        map.ActionAddEvent(right, vectorBinding);
        using (var vectorAxis = new InputEventJoypadMotion { Axis = JoyAxis.RightX, AxisValue = 0.75f })
        {
            input.ParseInputEvent(vectorAxis);
            Require(vectorAxis.IsPressed() &&
                    NearlyEqual(input.GetActionStrength(right), 0.5f) &&
                    NearlyEqual(input.GetActionRawStrength(right), 0.75f) &&
                    VectorNearlyEqual(input.GetVector(left, right, up, down, 0f), new Vector2(0.75f, 0f)),
                "Axis raw presses use the fixed toggle threshold while actions use their own deadzone and vectors use raw strengths.");
        }

        using (var firstMotion = new InputEventMouseMotion
        {
            Position = new Vector2(1f, 1f),
            Relative = new Vector2(1f, 2f),
            Velocity = new Vector2(3f, 4f),
        })
        using (var secondMotion = new InputEventMouseMotion
        {
            Position = new Vector2(2f, 3f),
            Relative = new Vector2(4f, 5f),
            Velocity = new Vector2(6f, 7f),
        })
        {
            var accumulatedChanges = 0;
            firstMotion.Changed += _ => accumulatedChanges++;
            Require(firstMotion.Accumulate(secondMotion) && firstMotion.Position == secondMotion.Position &&
                    firstMotion.Relative == new Vector2(5f, 7f) && firstMotion.Velocity == secondMotion.Velocity &&
                    accumulatedChanges == 1,
                "Compatible mouse motion must accumulate displacement and keep the newest position and velocity.");
            using var transformed = firstMotion.XformedBy(new Transform(0f, new Vector2(10f, 20f)));
            Require(transformed is InputEventMouseMotion transformedMotion &&
                    transformedMotion.Position == new Vector2(12f, 23f) &&
                    transformedMotion.Relative == new Vector2(5f, 7f),
                "Positional events must produce transformed copies without mutating the source.");
            var nonFiniteTransform = Transform.Identity;
            nonFiniteTransform.X.X = float.NaN;
            Expect<ArgumentOutOfRangeException>(() => firstMotion.XformedBy(nonFiniteTransform),
                "Positional input transforms must reject non-finite matrices before duplication.");

            firstMotion.Changed += _ => throw new InvalidOperationException("expected accumulated change failure");
            using var thirdMotion = new InputEventMouseMotion
            {
                Position = new Vector2(8f, 9f),
                Relative = new Vector2(1f, 1f),
                Velocity = new Vector2(10f, 11f),
            };
            Expect<InvalidOperationException>(() => firstMotion.Accumulate(thirdMotion),
                "A throwing motion observer must propagate after one atomic accumulation commit.");
            Require(firstMotion.Position == thirdMotion.Position && firstMotion.Relative == new Vector2(6f, 8f) &&
                    firstMotion.Velocity == thirdMotion.Velocity,
                "A failed accumulation notification must not expose partially committed motion state.");
        }


        using (var firstOverflowMotion = new InputEventMouseMotion
        {
            Relative = new Vector2(float.MaxValue, 0f),
            ScreenRelative = new Vector2(float.MaxValue, 0f),
        })
        using (var secondOverflowMotion = new InputEventMouseMotion
        {
            Relative = new Vector2(float.MaxValue, 0f),
            ScreenRelative = new Vector2(float.MaxValue, 0f),
        })
        {
            Require(firstOverflowMotion.Accumulate(secondOverflowMotion) &&
                    float.IsPositiveInfinity(firstOverflowMotion.Relative.X) &&
                    float.IsPositiveInfinity(firstOverflowMotion.ScreenRelative.X),
                "Accumulation retains source arithmetic overflow without altering the payload contract.");
        }

        using (var firstOverflowDrag = new InputEventScreenDrag
        {
            Index = 3,
            Relative = new Vector2(float.MaxValue, 0f),
            ScreenRelative = new Vector2(float.MaxValue, 0f),
        })
        using (var secondOverflowDrag = new InputEventScreenDrag
        {
            Index = 3,
            Relative = new Vector2(float.MaxValue, 0f),
            ScreenRelative = new Vector2(float.MaxValue, 0f),
        })
        {
            Require(firstOverflowDrag.Accumulate(secondOverflowDrag) &&
                    float.IsPositiveInfinity(firstOverflowDrag.Relative.X) &&
                    float.IsPositiveInfinity(firstOverflowDrag.ScreenRelative.X),
                "Drag accumulation retains source arithmetic overflow for one contact.");
        }

        using (var firstDrag = new InputEventScreenDrag
        {
            Index = 3,
            Position = new Vector2(1f, 2f),
            Relative = new Vector2(2f, 3f),
            ScreenRelative = new Vector2(4f, 5f),
        })
        using (var secondDrag = new InputEventScreenDrag
        {
            Index = 3,
            Position = new Vector2(6f, 7f),
            Relative = new Vector2(8f, 9f),
            ScreenRelative = new Vector2(10f, 11f),
        })
        {
            Require(firstDrag.Accumulate(secondDrag) && firstDrag.Position == secondDrag.Position &&
                    firstDrag.Relative == new Vector2(10f, 12f) &&
                    firstDrag.ScreenRelative == new Vector2(14f, 16f),
                "Equal touch contacts must atomically accumulate local and screen deltas.");
            using var transformedDrag = firstDrag.XformedBy(new Transform(
                new Vector2(2f, 0f),
                new Vector2(0f, 3f),
                Vector2.Zero));
            Require(transformedDrag is InputEventScreenDrag transformedScreenDrag &&
                    transformedScreenDrag.Position == new Vector2(12f, 21f) &&
                    transformedScreenDrag.Relative == new Vector2(20f, 36f) &&
                    transformedScreenDrag.ScreenRelative == firstDrag.ScreenRelative,
                "Touch-drag transforms must change local values and preserve screen-space deltas.");
            using var differentDrag = new InputEventScreenDrag { Index = 4 };
            Require(!firstDrag.Accumulate(differentDrag),
                "Different touch contacts must not accumulate.");
        }

        using (var pan = new InputEventPanGesture
        {
            Position = new Vector2(1f, 2f),
            Delta = new Vector2(3f, 4f),
        })
        using (var transformedPan = pan.XformedBy(new Transform(
            new Vector2(2f, 0f),
            new Vector2(0f, 3f),
            Vector2.Zero)))
        {
            Require(transformedPan is InputEventPanGesture transformedGesture &&
                    transformedGesture.Position == new Vector2(2f, 6f) &&
                    transformedGesture.Delta == pan.Delta,
                "Pan transforms must move the gesture position without rescaling its platform-reported delta.");
        }

        input.ReleasePressedEvents();
        Require(!input.IsAnythingPressed() && input.MouseButtonMask == MouseButtonMask.None,
            "ReleasePressedEvents must clear all raw input families and action sources.");
    }
    finally
    {
        if (Engine.Instance.MainLoop is not null)
            Engine.Instance.Stop();
        input.ReleasePressedEvents();
        foreach (var action in actionNames)
        {
            if (map.HasAction(action))
                map.EraseAction(action);
        }
        foreach (var binding in bindings)
            binding.Dispose();
    }
}

static void VerifyInputEmulation()
{
    const string click = "tests.input.emulation.click";
    var input = Input.Instance;
    var map = InputMap.Instance;
    input.ReleasePressedEvents();
    input.EmulateMouseFromTouch = true;
    input.EmulateTouchFromMouse = false;
    if (map.HasAction(click))
        map.EraseAction(click);
    map.AddAction(click);
    using var binding = new InputEventMouseButton
    {
        Device = InputMap.AllDevices,
        ButtonIndex = MouseButton.Left,
    };
    map.ActionAddEvent(click, binding);
    using (var canceledButton = new InputEventMouseButton
    {
        ButtonIndex = MouseButton.Left,
        Pressed = true,
        Canceled = true,
    })
    {
        Require(canceledButton.IsAction(click) && !canceledButton.IsActionPressed(click) &&
                canceledButton.IsActionReleased(click) && !canceledButton.IsPressed() && !canceledButton.IsReleased(),
            "A canceled matching press has a released action status while neither raw press nor release is active.");
        Expect<KeyNotFoundException>(() => canceledButton.IsActionReleased(click + ".missing"),
            "A canceled event still validates the requested action name through the shared map.");
    }
    var probe = new InputEmulationProbeNode { InputEnabled = true };
    using var tree = new SceneTree(probe);
    Engine.Instance.Start(tree);
    try
    {
        using var first = new InputEventScreenTouch
        {
            Device = 2,
            Index = 7,
            WindowID = 0,
            Position = new Vector2(20f, 30f),
            Pressed = true,
        };
        input.ParseInputEvent(first);
        Require(probe.Events.Count == 2 &&
                probe.Events[0] is InputEventMouseButton
                {
                    Device: InputEvent.DeviceIdEmulation, ButtonIndex: MouseButton.Left,
                    ButtonMask: MouseButtonMask.Left, Position: { X: 20f, Y: 30f }, Pressed: true,
                } && probe.Events[1] is InputEventScreenTouch { Device: 2, Index: 7 } &&
                input.IsMouseButtonPressed(MouseButton.Left) && input.IsActionPressed(click),
            "The first touch must commit and deliver an emulated left click before the source touch.");
        probe.Clear();

        using var second = new InputEventScreenTouch
        {
            Device = 3,
            Index = 7,
            WindowID = 0,
            Position = new Vector2(50f, 60f),
            Pressed = true,
        };
        input.ParseInputEvent(second);
        Require(probe.Events.Count == 1 && probe.Events[0] is InputEventScreenTouch { Device: 3 },
            "A second physical contact with the same index on another device stays touch-only.");
        probe.Clear();

        using var drag = new InputEventScreenDrag
        {
            Device = 2,
            Index = 7,
            WindowID = 0,
            Position = new Vector2(25f, 34f),
            Relative = new Vector2(5f, 4f),
            ScreenRelative = new Vector2(5f, 4f),
            Velocity = new Vector2(50f, 40f),
            ScreenVelocity = new Vector2(50f, 40f),
            Pressure = 0.5f,
        };
        input.ParseInputEvent(drag);
        Require(probe.Events.Count == 2 && probe.Events[0] is InputEventMouseMotion
        {
            Device: InputEvent.DeviceIdEmulation, ButtonMask: MouseButtonMask.Left,
            Relative: { X: 5f, Y: 4f }, Pressure: 0.5f,
        } && probe.Events[1] is InputEventScreenDrag { Device: 2, Index: 7 } &&
                input.LastMouseVelocity == new Vector2(50f, 40f),
            "The tracked contact's drag must become emulated mouse motion with velocity and pressure.");
        probe.Clear();

        first.Pressed = false;
        input.EmulateMouseFromTouch = false;
        input.ParseInputEvent(first);
        Require(probe.Events.Count == 2 && probe.Events[0] is InputEventMouseButton
        { Device: InputEvent.DeviceIdEmulation, Pressed: false } &&
                !input.IsMouseButtonPressed(MouseButton.Left) && !input.IsActionPressed(click),
            "Turning emulation off during a contact must still release its synthetic button.");
        probe.Clear();
        second.Pressed = false;
        input.ParseInputEvent(second);
        Require(probe.Events.Count == 1 && probe.Events[0] is InputEventScreenTouch,
            "The other contact must not inherit mouse ownership after the first releases.");
        probe.Clear();

        input.EmulateTouchFromMouse = true;
        using var mousePress = new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            ButtonMask = MouseButtonMask.Left,
            Position = new Vector2(12f, 14f),
            GlobalPosition = new Vector2(112f, 114f),
            Pressed = true,
        };
        input.ParseInputEvent(mousePress);
        Require(probe.Events.Count == 2 && probe.Events[0] is InputEventScreenTouch
        {
            Device: InputEvent.DeviceIdEmulation, Index: 0, Pressed: true,
            Position: { X: 12f, Y: 14f }
        } &&
                probe.Events[1] is InputEventMouseButton { Device: InputEvent.DeviceIdMouse },
            "A physical left click must deliver touch index zero before its source mouse event.");
        probe.Clear();

        using var otherMousePress = new InputEventMouseButton
        {
            Device = 4,
            ButtonIndex = MouseButton.Left,
            ButtonMask = MouseButtonMask.Left,
            Position = new Vector2(70f, 80f),
            Pressed = true,
        };
        input.ParseInputEvent(otherMousePress);
        Require(probe.Events.Count == 1 && probe.Events[0] is InputEventMouseButton { Device: 4 },
            "Another mouse device cannot take over an active emulated touch contact.");
        probe.Clear();
        otherMousePress.Pressed = false;
        input.ParseInputEvent(otherMousePress);
        Require(probe.Events.Count == 1 && probe.Events[0] is InputEventMouseButton { Device: 4 },
            "Another mouse device cannot end the active emulated touch contact.");
        probe.Clear();

        using var mouseDrag = new InputEventMouseMotion
        {
            ButtonMask = MouseButtonMask.Left,
            Position = new Vector2(17f, 16f),
            GlobalPosition = new Vector2(117f, 116f),
            Relative = new Vector2(5f, 2f),
            ScreenRelative = new Vector2(5f, 2f),
            Velocity = new Vector2(50f, 20f),
            ScreenVelocity = new Vector2(50f, 20f),
        };
        input.ParseInputEvent(mouseDrag);
        Require(probe.Events.Count == 2 && probe.Events[0] is InputEventScreenDrag
        {
            Device: InputEvent.DeviceIdEmulation, Index: 0,
            Relative: { X: 5f, Y: 2f }
        } &&
                probe.Events[1] is InputEventMouseMotion { Device: InputEvent.DeviceIdMouse },
            "A held left-button mouse motion must deliver an emulated touch drag.");
        probe.Clear();

        input.EmulateTouchFromMouse = false;
        mousePress.Pressed = false;
        input.ParseInputEvent(mousePress);
        Require(probe.Events.Count == 2 && probe.Events[0] is InputEventScreenTouch
        { Device: InputEvent.DeviceIdEmulation, Pressed: false } &&
                !input.IsMouseButtonPressed(MouseButton.Left),
            "Disabling touch emulation during a press must still end the contact on release.");
        probe.Clear();

        input.EmulateMouseFromTouch = true;
        first.Pressed = true;
        probe.ThrowOnEmulated = true;
        Expect<AggregateException>(() => input.ParseInputEvent(first),
            "A failing synthetic callback must be reported after source delivery.");
        Require(probe.Events.Count == 2 && input.IsMouseButtonPressed(MouseButton.Left) &&
                input.IsActionPressed(click),
            "A synthetic callback failure must not undo state or skip the physical touch callback.");
        probe.ThrowOnEmulated = false;
        probe.Clear();
        first.Pressed = false;
        input.ParseInputEvent(first);
        probe.Clear();

        using var emulatedTouch = new InputEventScreenTouch
        {
            Device = InputEvent.DeviceIdEmulation,
            Index = 0,
            Pressed = true,
        };
        input.ParseInputEvent(emulatedTouch);
        Require(probe.Events.Count == 1 && !input.IsMouseButtonPressed(MouseButton.Left),
            "An emulated event must never recursively generate the opposite pointer family.");
    }
    finally
    {
        Engine.Instance.Stop();
        probe.Clear();
        input.ReleasePressedEvents();
        input.EmulateMouseFromTouch = true;
        input.EmulateTouchFromMouse = false;
        map.EraseAction(click);
    }
}

static void VerifyDisplayServerPointerModifiers()
{
    using var display = DisplayServer.Open("Pointer modifiers", new Vector2i(320, 240), hidden: true);
    var probe = new InputEmulationProbeNode { InputEnabled = true, PhysicsProcessEnabled = true };
    using var tree = new SceneTree(probe);
    var previousModifiers = SDL3.SDL.GetModState();
    var previousAccumulation = Input.Instance.UseAccumulatedInput;
    Engine.Instance.Start(tree);
    try
    {
        Input.Instance.UseAccumulatedInput = false;
        SDL3.SDL.SetModState(SDL3.SDL.Keymod.Shift | SDL3.SDL.Keymod.Ctrl);
        var windows = SDL3.SDL.GetWindows(out var count);
        Require(windows is { Length: 1 } && count == 1,
            "The pointer modifier test needs one native window.");
        var id = SDL3.SDL.GetWindowID(windows![0]);
        var motion = new SDL3.SDL.Event
        {
            Motion = new SDL3.SDL.MouseMotionEvent
            {
                Type = SDL3.SDL.EventType.MouseMotion,
                WindowID = id,
                X = 13f,
                Y = 17f,
                XRel = 3f,
                YRel = 4f,
            },
        };
        var button = new SDL3.SDL.Event
        {
            Button = new SDL3.SDL.MouseButtonEvent
            {
                Type = SDL3.SDL.EventType.MouseButtonDown,
                WindowID = id,
                Button = 1,
                Down = true,
                X = 13f,
                Y = 17f,
            },
        };
        Require(SDL3.SDL.PushEvent(ref motion) && SDL3.SDL.PushEvent(ref button),
            "The native queue accepts pointer modifier probes.");
        display.ProcessEvents();
        Require(probe.Events.Count == 2 &&
                probe.Events[0] is InputEventMouseMotion first &&
                first.ShiftPressed && first.ControlPressed &&
                first.Position == first.GlobalPosition &&
                probe.Events[1] is InputEventMouseButton second &&
                second.ShiftPressed && second.ControlPressed &&
                second.Position == second.GlobalPosition,
            "Native mouse motion and buttons preserve keyboard modifiers and root-window coordinates.");

        probe.Clear();
        var modifiedKey = new SDL3.SDL.Event
        {
            Key = new SDL3.SDL.KeyboardEvent
            {
                Type = SDL3.SDL.EventType.KeyDown,
                WindowID = id,
                Key = SDL3.SDL.Keycode.A,
                Scancode = SDL3.SDL.Scancode.A,
                Mod = SDL3.SDL.Keymod.Shift,
                Down = true,
            },
        };
        var unmodifiedKey = modifiedKey;
        unmodifiedKey.Key.Type = SDL3.SDL.EventType.KeyUp;
        unmodifiedKey.Key.Mod = SDL3.SDL.Keymod.None;
        unmodifiedKey.Key.Down = false;
        SDL3.SDL.SetModState(SDL3.SDL.Keymod.None);
        Require(SDL3.SDL.PushEvent(ref modifiedKey) && SDL3.SDL.PushEvent(ref motion) &&
                SDL3.SDL.PushEvent(ref unmodifiedKey),
            "The native queue accepts a mixed keyboard and pointer sequence.");
        display.ProcessEvents();
        Require(probe.Events.Count == 3 &&
                probe.Events[1] is InputEventMouseMotion { ShiftPressed: true, ControlPressed: false },
            "Queued pointer modifiers follow the preceding keyboard snapshot, not the final global state.");
        probe.Clear();
        var keyLocations = new (SDL3.SDL.Scancode Scancode, KeyLocation Location)[]
        {
            (SDL3.SDL.Scancode.LCtrl, KeyLocation.Left),
            (SDL3.SDL.Scancode.LShift, KeyLocation.Left),
            (SDL3.SDL.Scancode.LAlt, KeyLocation.Left),
            (SDL3.SDL.Scancode.LGUI, KeyLocation.Left),
            (SDL3.SDL.Scancode.RCtrl, KeyLocation.Right),
            (SDL3.SDL.Scancode.RShift, KeyLocation.Right),
            (SDL3.SDL.Scancode.RAlt, KeyLocation.Right),
            (SDL3.SDL.Scancode.RGUI, KeyLocation.Right),
            (SDL3.SDL.Scancode.A, KeyLocation.Unspecified),
        };
        foreach (var (scancode, _) in keyLocations)
        {
            var locationKey = new SDL3.SDL.Event
            {
                Key = new SDL3.SDL.KeyboardEvent
                {
                    Type = SDL3.SDL.EventType.KeyDown,
                    WindowID = id,
                    Key = SDL3.SDL.GetKeyFromScancode(scancode, SDL3.SDL.Keymod.None, false),
                    Scancode = scancode,
                    Down = true,
                },
            };
            Require(SDL3.SDL.PushEvent(ref locationKey), "The native queue accepts a side-specific key press.");
            locationKey.Key.Type = SDL3.SDL.EventType.KeyUp;
            locationKey.Key.Down = false;
            Require(SDL3.SDL.PushEvent(ref locationKey), "The native queue accepts a side-specific key release.");
        }
        display.ProcessEvents();
        Require(probe.Events.Count == keyLocations.Length * 2,
            "Every queued side-specific key press and release reaches input callbacks.");
        for (var i = 0; i < keyLocations.Length; i++)
        {
            Require(probe.Events[2 * i] is InputEventKey { Pressed: true } pressed &&
                    pressed.Location == keyLocations[i].Location &&
                    probe.Events[2 * i + 1] is InputEventKey { Pressed: false } released &&
                    released.Location == keyLocations[i].Location,
                "Key location follows the SDL physical modifier side on both press and release.");
        }
        probe.Clear();
        var selfModifiers = new (SDL3.SDL.Scancode Scancode, SDL3.SDL.Keymod OwnNative,
            SDL3.SDL.Keymod OtherNative, KeyModifierMask Own, KeyModifierMask Other)[]
        {
            (SDL3.SDL.Scancode.LShift, SDL3.SDL.Keymod.Shift, SDL3.SDL.Keymod.Ctrl, KeyModifierMask.Shift, KeyModifierMask.Control),
            (SDL3.SDL.Scancode.RShift, SDL3.SDL.Keymod.Shift, SDL3.SDL.Keymod.Ctrl, KeyModifierMask.Shift, KeyModifierMask.Control),
            (SDL3.SDL.Scancode.LCtrl, SDL3.SDL.Keymod.Ctrl, SDL3.SDL.Keymod.Alt, KeyModifierMask.Control, KeyModifierMask.Alt),
            (SDL3.SDL.Scancode.RCtrl, SDL3.SDL.Keymod.Ctrl, SDL3.SDL.Keymod.Alt, KeyModifierMask.Control, KeyModifierMask.Alt),
            (SDL3.SDL.Scancode.LAlt, SDL3.SDL.Keymod.Alt, SDL3.SDL.Keymod.GUI, KeyModifierMask.Alt, KeyModifierMask.Meta),
            (SDL3.SDL.Scancode.RAlt, SDL3.SDL.Keymod.Alt, SDL3.SDL.Keymod.GUI, KeyModifierMask.Alt, KeyModifierMask.Meta),
            (SDL3.SDL.Scancode.LGUI, SDL3.SDL.Keymod.GUI, SDL3.SDL.Keymod.Shift, KeyModifierMask.Meta, KeyModifierMask.Shift),
            (SDL3.SDL.Scancode.RGUI, SDL3.SDL.Keymod.GUI, SDL3.SDL.Keymod.Shift, KeyModifierMask.Meta, KeyModifierMask.Shift),
        };
        foreach (var (scancode, _, otherNative, _, _) in selfModifiers)
        {
            var ownSide = scancode switch
            {
                SDL3.SDL.Scancode.LShift => SDL3.SDL.Keymod.LShift,
                SDL3.SDL.Scancode.RShift => SDL3.SDL.Keymod.RShift,
                SDL3.SDL.Scancode.LCtrl => SDL3.SDL.Keymod.LCtrl,
                SDL3.SDL.Scancode.RCtrl => SDL3.SDL.Keymod.RCtrl,
                SDL3.SDL.Scancode.LAlt => SDL3.SDL.Keymod.LAlt,
                SDL3.SDL.Scancode.RAlt => SDL3.SDL.Keymod.RAlt,
                SDL3.SDL.Scancode.LGUI => SDL3.SDL.Keymod.LGUI,
                SDL3.SDL.Scancode.RGUI => SDL3.SDL.Keymod.RGUI,
                _ => throw new InvalidOperationException(),
            };
            var modifierKey = new SDL3.SDL.Event
            {
                Key = new SDL3.SDL.KeyboardEvent
                {
                    Type = SDL3.SDL.EventType.KeyDown,
                    WindowID = id,
                    Key = SDL3.SDL.GetKeyFromScancode(scancode, SDL3.SDL.Keymod.None, false),
                    Scancode = scancode,
                    Mod = ownSide | otherNative,
                    Down = true,
                },
            };
            Require(SDL3.SDL.PushEvent(ref modifierKey), "The native queue accepts a self-modifier press.");
            modifierKey.Key.Type = SDL3.SDL.EventType.KeyUp;
            modifierKey.Key.Down = false;
            Require(SDL3.SDL.PushEvent(ref modifierKey), "The native queue accepts a self-modifier release.");
        }
        display.ProcessEvents();
        Require(probe.Events.Count == selfModifiers.Length * 2,
            "Every self-modifier probe reaches both key callback phases.");
        for (var i = 0; i < selfModifiers.Length * 2; i++)
        {
            var (_, _, _, own, other) = selfModifiers[i / 2];
            Require(probe.Events[i] is InputEventKey keyEvent &&
                    (keyEvent.GetModifiersMask() & own) == 0 &&
                    (keyEvent.GetModifiersMask() & other) != 0,
                "A modifier key never marks itself as its own modifier while preserving other held modifiers.");
        }
        probe.Clear();
        foreach (var (scancode, ownNative, _, _, _) in selfModifiers)
        {
            var modifierKey = new SDL3.SDL.Event
            {
                Key = new SDL3.SDL.KeyboardEvent
                {
                    Type = SDL3.SDL.EventType.KeyDown,
                    WindowID = id,
                    Key = SDL3.SDL.GetKeyFromScancode(scancode, SDL3.SDL.Keymod.None, false),
                    Scancode = scancode,
                    Mod = ownNative,
                    Down = true,
                },
            };
            Require(SDL3.SDL.PushEvent(ref modifierKey), "The native queue accepts an opposite-side modifier press.");
            modifierKey.Key.Type = SDL3.SDL.EventType.KeyUp;
            modifierKey.Key.Down = false;
            Require(SDL3.SDL.PushEvent(ref modifierKey), "The native queue accepts an opposite-side modifier release.");
        }
        display.ProcessEvents();
        Require(probe.Events.Count == selfModifiers.Length * 2,
            "Every opposite-side modifier probe reaches both key callback phases.");
        for (var i = 0; i < selfModifiers.Length * 2; i++)
        {
            var own = selfModifiers[i / 2].Own;
            Require(probe.Events[i] is InputEventKey keyEvent &&
                    (keyEvent.GetModifiersMask() & own) != 0,
                "The opposite-side held key keeps its shared modifier family active.");
        }
        probe.Clear();
        SDL3.SDL.SetModState(SDL3.SDL.Keymod.Shift | SDL3.SDL.Keymod.Ctrl);

        probe.ThrowOnWheel = true;
        var wheel = new SDL3.SDL.Event
        {
            Wheel = new SDL3.SDL.MouseWheelEvent
            {
                Type = SDL3.SDL.EventType.MouseWheel,
                WindowID = id,
                X = 1f,
                Y = 1f,
                MouseX = 13f,
                MouseY = 17f,
            },
        };
        Require(SDL3.SDL.PushEvent(ref wheel), "The native queue accepts a wheel failure probe.");
        try
        {
            display.ProcessEvents();
            throw new Exception("Both wheel callback failures must be reported.");
        }
        catch (AggregateException errors)
        {
            Require(errors.Flatten().InnerExceptions.Count == 2 &&
                    probe.Events.Count == 4 &&
                    probe.Events[0] is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true } &&
                    probe.Events[1] is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: false } &&
                    probe.Events[2] is InputEventMouseButton { ButtonIndex: MouseButton.WheelRight, Pressed: true } &&
                    probe.Events[3] is InputEventMouseButton { ButtonIndex: MouseButton.WheelRight, Pressed: false },
                "A failing wheel axis still releases its button, delivers the other axis, and preserves both callback failures.");
        }
        probe.ThrowOnWheel = false;

        probe.Clear();
        var key = new SDL3.SDL.Event
        {
            Key = new SDL3.SDL.KeyboardEvent
            {
                Type = SDL3.SDL.EventType.KeyDown,
                WindowID = id,
                Key = SDL3.SDL.Keycode.A,
                Scancode = SDL3.SDL.Scancode.A,
                Down = true,
            },
        };
        Require(SDL3.SDL.PushEvent(ref key), "The native queue accepts an input preflight probe.");
        probe.DisplayPumpAttempt = display;
        Engine.Instance.AdvanceFrame(0.1d);
        Require(probe.DisplayPumpError is InvalidOperationException &&
                !Input.Instance.IsKeyPressed(Key.A) && probe.Events.Count == 0,
            "Pumping inside a frame callback rejects before consuming queued native input.");
        display.ProcessEvents();
        Require(Input.Instance.IsKeyPressed(Key.A) && probe.Events.Count == 1,
            "The retained native input is delivered when pumping becomes valid.");
        key.Key.Type = SDL3.SDL.EventType.KeyUp;
        key.Key.Down = false;
        Require(SDL3.SDL.PushEvent(ref key), "The native queue accepts the preflight key release.");
        display.ProcessEvents();
    }
    finally
    {
        SDL3.SDL.SetModState(previousModifiers);
        Input.Instance.UseAccumulatedInput = previousAccumulation;
        Input.Instance.ReleasePressedEvents();
        Engine.Instance.Stop();
        probe.Clear();
    }
}

static void VerifyEngine()
{
    var engine = Engine.Instance;
    Require(ReferenceEquals(engine, Engine.Instance), "Engine must be a process-wide singleton.");
    Require(!engine.IsDisposed, "The process-wide Engine must remain live.");
    Require(engine.HasSingleton(nameof(Engine)) && ReferenceEquals(engine.GetSingleton<Engine>(nameof(Engine)), engine) &&
            engine.HasSingleton(nameof(ProjectSettings)) &&
            ReferenceEquals(engine.GetSingleton<ProjectSettings>(nameof(ProjectSettings)), ProjectSettings.Instance) &&
            ReferenceEquals(engine.GetSingleton<Input>(nameof(Input)), Input.Instance) &&
            ReferenceEquals(engine.GetSingleton<InputMap>(nameof(InputMap)), InputMap.Instance) &&
            engine.GetSingletonList().SequenceEqual([nameof(Engine), nameof(ProjectSettings), nameof(Input), nameof(InputMap)]),
        "All built-in process services must be present in the global singleton registry.");
    Expect<InvalidOperationException>(() => engine.UnregisterSingleton(nameof(Engine)),
        "The built-in Engine registry entry must not be removable.");
    Expect<InvalidOperationException>(() => engine.UnregisterSingleton(nameof(ProjectSettings)),
        "The built-in ProjectSettings registry entry must not be removable.");
    Expect<InvalidOperationException>(() => engine.UnregisterSingleton(nameof(Input)),
        "The built-in Input registry entry must not be removable.");
    Expect<InvalidOperationException>(() => engine.UnregisterSingleton(nameof(InputMap)),
        "The built-in InputMap registry entry must not be removable.");
    Expect<InvalidOperationException>(engine.Dispose, "The process-wide Engine must reject disposal.");
    Require(engine.PhysicsTicksPerSecond == 60 && engine.MaxPhysicsStepsPerFrame == 8 &&
            DoubleNearlyEqual(engine.PhysicsJitterFix, 0.5d) && DoubleNearlyEqual(engine.TimeScale, 1d),
        "Engine timing settings must expose their documented defaults.");
    var processSettings = ProjectSettings.Instance;
    processSettings.SetFeatureOverride(ProjectSettings.PhysicsTicksPerSecond, "tests", 30);
    processSettings.AddCustomFeature("tests");
    Require(engine.PhysicsTicksPerSecond == 30,
        "Engine timing must read active feature overrides from process-wide project settings.");
    processSettings.RemoveCustomFeature("tests");
    processSettings.ClearFeatureOverride(ProjectSettings.PhysicsTicksPerSecond, "tests");
    processSettings.FlushChanges();
    Expect<ArgumentOutOfRangeException>(() => engine.PhysicsTicksPerSecond = 0,
        "Physics tick frequency must reject zero.");
    Expect<ArgumentOutOfRangeException>(() => engine.MaxPhysicsStepsPerFrame = -1,
        "Maximum physics steps must reject negative values.");
    Expect<ArgumentOutOfRangeException>(() => engine.PhysicsJitterFix = double.NaN,
        "Physics jitter fix must reject NaN.");
    Expect<ArgumentOutOfRangeException>(() => engine.TimeScale = double.PositiveInfinity,
        "Time scale must reject infinity.");
    Expect<ArgumentOutOfRangeException>(() => engine.TimeScale = -double.Epsilon,
        "Time scale must reject negative values.");
    engine.PhysicsJitterFix = -1d;
    Require(engine.PhysicsJitterFix == 0d, "A negative physics jitter fix must clamp to zero.");

    var engineProperties = engine.GetPropertyList();
    Require(engineProperties.Any(property => property.Name == nameof(Engine.PhysicsTicksPerSecond)) &&
            engineProperties.Any(property => property.Name == nameof(Engine.TimeScale)) &&
            engineProperties.Any(property => property.Name == nameof(Engine.ProcessFrames)),
        "Engine timing settings and metrics must participate in typed property discovery.");
    Require(!string.IsNullOrWhiteSpace(engine.ArchitectureName) &&
            engine.VersionInfo.AssemblyVersion == typeof(Engine).Assembly.GetName().Version &&
            !string.IsNullOrWhiteSpace(engine.VersionInfo.InformationalVersion) &&
            engine.VersionInfo.ToString() == engine.VersionInfo.InformationalVersion,
        "Engine build information must describe the loaded assembly and process architecture.");

    using (var registered = new TestObject())
    {
        engine.RegisterSingleton("tests.primary", registered);
        Require(engine.HasSingleton("tests.primary") &&
                ReferenceEquals(engine.GetSingleton("tests.primary"), registered) &&
                ReferenceEquals(engine.GetSingleton<TestObject>("tests.primary"), registered) &&
                engine.GetSingletonList().SequenceEqual([nameof(Engine), nameof(ProjectSettings), nameof(Input), nameof(InputMap), "tests.primary"]),
            "Engine singleton lookup must preserve identity, type, and registration order.");
        Expect<InvalidOperationException>(() => engine.RegisterSingleton("tests.primary", registered),
            "Engine singleton names must be unique.");
        Expect<InvalidCastException>(() => engine.GetSingleton<Resource>("tests.primary"),
            "Typed engine singleton lookup must reject an incompatible type.");
        engine.UnregisterSingleton("tests.primary");
        Require(!engine.HasSingleton("tests.primary") && !registered.IsDisposed,
            "Unregistering an engine singleton must not dispose the registered object.");
    }

    Expect<ArgumentNullException>(() => engine.HasSingleton(null!), "Engine singleton names must reject null.");
    Expect<ArgumentException>(() => engine.HasSingleton("  "), "Engine singleton names must reject whitespace.");
    Expect<KeyNotFoundException>(() => engine.GetSingleton("tests.missing"),
        "Engine singleton lookup must report an absent name.");
    Expect<KeyNotFoundException>(() => engine.UnregisterSingleton("tests.missing"),
        "Engine singleton removal must report an absent name.");
    var disposedSingleton = new TestObject();
    disposedSingleton.Dispose();
    Expect<ObjectDisposedException>(() => engine.RegisterSingleton("tests.disposed", disposedSingleton),
        "Engine singleton registration must reject disposed objects.");

    Parallel.For(0, 32, index => engine.RegisterSingleton($"tests.concurrent.{index}", new TestObject()));
    Require(engine.GetSingletonList().Count == 36, "Concurrent singleton registration must not lose entries.");
    Parallel.For(0, 32, index =>
    {
        var name = $"tests.concurrent.{index}";
        var instance = engine.GetSingleton(name);
        engine.UnregisterSingleton(name);
        instance.Dispose();
    });
    Require(engine.GetSingletonList().SequenceEqual([nameof(Engine), nameof(ProjectSettings), nameof(Input), nameof(InputMap)]),
        "Concurrent singleton removal must preserve only the built-in registry entries.");

    engine.PhysicsTicksPerSecond = 10;
    engine.MaxPhysicsStepsPerFrame = 3;
    engine.PhysicsJitterFix = 0d;
    engine.TimeScale = 2d;

    using (var loop = new EngineProbeMainLoop())
    {
        var projectSettingsEvents = 0;
        void OnProjectSettingsChanged(ProjectSettings _) => projectSettingsEvents++;
        processSettings.SettingsChanged += OnProjectSettingsChanged;
        processSettings.Set(ProjectSettings.ApplicationVersion, "frame-test");
        engine.Start(loop);
        Require(loop.InitializeCount == 1 && ReferenceEquals(loop.MainLoopDuringInitialize, loop) &&
                ReferenceEquals(engine.MainLoop, loop),
            "Engine.Start must publish and initialize an uninitialized MainLoop exactly once.");
        Expect<InvalidOperationException>(() => engine.Start(loop), "Engine must reject a second active MainLoop.");
        Expect<ArgumentOutOfRangeException>(() => engine.AdvanceFrame(double.NaN),
            "Engine frame scheduling must reject NaN.");
        Expect<ArgumentOutOfRangeException>(() => engine.AdvanceFrame(-double.Epsilon),
            "Engine frame scheduling must reject negative elapsed time.");

        Require(!engine.AdvanceFrame(0.05d) && loop.PhysicsDeltas.Count == 0 &&
                loop.ProcessDeltas.Count == 1 && DoubleNearlyEqual(loop.ProcessDeltas[0], 0.1d) &&
                DoubleNearlyEqual(engine.PhysicsInterpolationFraction, 0.5d) && projectSettingsEvents == 1,
            "A half fixed interval must run one scaled process callback, expose interpolation, and flush project settings once.");
        processSettings.SettingsChanged -= OnProjectSettingsChanged;
        processSettings.Set(ProjectSettings.ApplicationVersion, string.Empty);
        processSettings.FlushChanges();
        Require(!engine.AdvanceFrame(0.05d) && loop.PhysicsDeltas.Count == 1 &&
                DoubleNearlyEqual(loop.PhysicsDeltas[0], 0.2d) && !engine.IsInPhysicsFrame &&
                loop.ObservedPhysicsFrameState && !loop.ObservedProcessFrameState &&
                loop.Order.TakeLast(2).SequenceEqual(["physics", "process"]),
            "A complete fixed interval must run a scaled fixed callback before process and restore physics state.");
        Require(engine.ProcessFrames == 2 && engine.PhysicsFrames == 1,
            "Engine frame counters must reflect completed process and started fixed callbacks.");

        loop.ReenterEngine = true;
        engine.AdvanceFrame(0.1d);
        Require(loop.AdvanceReentryError is InvalidOperationException && loop.StopReentryError is InvalidOperationException,
            "Engine callbacks must reject frame and stop re-entry without poisoning the runtime.");
        loop.ReenterEngine = false;

        var physicsBeforeCap = engine.PhysicsFrames;
        engine.AdvanceFrame(1000.05d);
        Require(engine.PhysicsFrames - physicsBeforeCap <= 3 && engine.PhysicsInterpolationFraction is >= 0d and <= 1d,
            "A long host stall must obey the fixed-step catch-up cap and preserve a bounded interpolation fraction.");

        loop.PhysicsResult = true;
        loop.ProcessResult = true;
        var physicsBeforeStopRequest = loop.PhysicsDeltas.Count;
        Require(engine.AdvanceFrame(0.3d) && loop.ProcessDeltas.Count >= 4 &&
                loop.PhysicsDeltas.Count == physicsBeforeStopRequest + 1,
            "A fixed-step stop request must skip remaining fixed work but still run process and combine stop requests.");
        loop.PhysicsResult = false;
        loop.ProcessResult = false;

        var processFramesBeforeFailure = engine.ProcessFrames;
        loop.ThrowOnProcess = true;
        Require(Capture(() => engine.AdvanceFrame(0d)) is InvalidOperationException &&
                engine.ProcessFrames == processFramesBeforeFailure && ReferenceEquals(engine.MainLoop, loop),
            "A process callback failure must not count completion or detach the running loop.");
        loop.ThrowOnProcess = false;
        engine.AdvanceFrame(0d);

        Require(Task.Run(() => Capture(() => engine.AdvanceFrame(0d))).GetAwaiter().GetResult() is InvalidOperationException &&
                Task.Run(() => Capture(engine.Stop)).GetAwaiter().GetResult() is InvalidOperationException,
            "Engine frame execution and stop must reject a non-owner thread.");

        engine.Stop();
        Require(loop.FinalizeCount == 1 && ReferenceEquals(loop.MainLoopDuringFinalize, loop) &&
                engine.MainLoop is null && !loop.IsDisposed,
            "Engine.Stop must finalize and detach, but not dispose, the MainLoop.");
        Expect<InvalidOperationException>(() => engine.Start(loop),
            "Engine must reject reattaching a finalized MainLoop and return to idle.");
        Expect<InvalidOperationException>(() => engine.AdvanceFrame(0d),
            "Engine must reject frames after stop.");
        Expect<InvalidOperationException>(engine.Stop, "Engine must reject stop while idle.");
    }

    engine.TimeScale = 1d;
    engine.PhysicsTicksPerSecond = 60;
    engine.MaxPhysicsStepsPerFrame = 8;
    engine.PhysicsJitterFix = 0.5d;

    using (var failedInitialize = new EngineProbeMainLoop { ThrowOnInitialize = true })
    {
        Require(Capture(() => engine.Start(failedInitialize)) is InvalidOperationException && engine.MainLoop is null,
            "Failed loop initialization must return Engine to idle and clear its MainLoop.");
    }

    var disposedLoop = new EngineProbeMainLoop();
    disposedLoop.Dispose();
    Expect<ObjectDisposedException>(() => engine.Start(disposedLoop),
        "Engine must reject a disposed MainLoop and return to idle.");

    using (var wrongThreadLoop = new EngineProbeMainLoop())
    {
        Require(Task.Run(() => Capture(() => engine.Start(wrongThreadLoop))).GetAwaiter().GetResult() is InvalidOperationException &&
                engine.MainLoop is null,
            "Engine.Start must preserve MainLoop owner-thread affinity and roll back a failed attachment.");
    }

    using (var lifecycleReentry = new EngineProbeMainLoop
    {
        ReenterDuringInitialize = true,
        ReenterDuringFinalize = true
    })
    {
        engine.Start(lifecycleReentry);
        Require(lifecycleReentry.StartDuringInitializeError is InvalidOperationException,
            "Engine startup must reject lifecycle re-entry.");
        engine.Stop();
        Require(lifecycleReentry.StartDuringFinalizeError is InvalidOperationException &&
                lifecycleReentry.StopDuringFinalizeError is InvalidOperationException,
            "Engine shutdown must reject lifecycle re-entry.");
    }

    var failedFinalize = new EngineProbeMainLoop { ThrowOnFinalize = true };
    engine.Start(failedFinalize);
    Require(Capture(engine.Stop) is InvalidOperationException && engine.MainLoop is null,
        "Failed loop finalization must still detach the terminal MainLoop.");
    failedFinalize.Dispose();

    using (var physicsFailure = new EngineProbeMainLoop { ThrowOnPhysics = true })
    {
        var physicsFramesBeforeFailure = engine.PhysicsFrames;
        engine.Start(physicsFailure);
        Require(Capture(() => engine.AdvanceFrame(1d / 60d)) is InvalidOperationException &&
                engine.PhysicsFrames == physicsFramesBeforeFailure + 1 && !engine.IsInPhysicsFrame &&
                ReferenceEquals(engine.MainLoop, physicsFailure),
            "A fixed callback failure must count its start, clear physics state, and leave Engine running.");
        physicsFailure.ThrowOnPhysics = false;
        engine.AdvanceFrame(0d);
        engine.Stop();
    }

    using (var fpsLoop = new EmptyMainLoop())
    {
        engine.Start(fpsLoop);
        for (var index = 0; index < 10; index++)
            engine.AdvanceFrame(0.1d);
        Require(DoubleNearlyEqual(engine.FramesPerSecond, 10d),
            "Engine must publish completed process frames per unscaled host second.");

        for (var index = 0; index < 16; index++)
            engine.AdvanceFrame(0d);
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 128; index++)
            engine.AdvanceFrame(0d);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Require(allocatedBytes == 0,
            $"A warmed idle Engine frame must not allocate; observed {allocatedBytes} bytes.");
        engine.Stop();
    }

    using (var frequencyChange = new EngineProbeMainLoop())
    {
        engine.PhysicsTicksPerSecond = 10;
        engine.PhysicsJitterFix = 0d;
        engine.TimeScale = 0d;
        engine.Start(frequencyChange);
        engine.AdvanceFrame(0.05d);
        engine.PhysicsTicksPerSecond = 20;
        engine.AdvanceFrame(0.025d);
        Require(frequencyChange.PhysicsDeltas.Count == 0 && frequencyChange.ProcessDeltas.All(delta => delta == 0d) &&
                DoubleNearlyEqual(engine.PhysicsInterpolationFraction, 0.5d),
            $"A tick-frequency change must re-baseline old fractional time, while zero time scale freezes delivered deltas. Physics={frequencyChange.PhysicsDeltas.Count}, process=[{string.Join(',', frequencyChange.ProcessDeltas)}], interpolation={engine.PhysicsInterpolationFraction}.");
        engine.Stop();
    }

    using (var jitterTolerance = new EngineProbeMainLoop())
    {
        engine.PhysicsTicksPerSecond = 10;
        engine.PhysicsJitterFix = 0.5d;
        engine.TimeScale = 1d;
        engine.Start(jitterTolerance);
        engine.AdvanceFrame(0.05d);
        Require(jitterTolerance.PhysicsDeltas.Count == 1 && engine.PhysicsInterpolationFraction == 0d,
            "The default jitter tolerance must permit a stable fixed callback at a half-step boundary.");
        engine.Stop();
    }

    engine.PhysicsTicksPerSecond = 60;
    engine.PhysicsJitterFix = 0.5d;
    engine.TimeScale = 1d;

    var root = new Entity();
    var tree = new SceneTree(root);
    engine.Start(tree);
    engine.AdvanceFrame(0d);
    engine.Stop();
    Require(root.IsDisposed && !tree.IsDisposed,
        "Engine must attach an already initialized SceneTree and finalize its owned hierarchy exactly once.");
    tree.Dispose();
}

static void VerifyMainLoop()
{
    using (var empty = new EmptyMainLoop())
    {
        empty.Initialize();
        Require(!empty.Process(0d) && !empty.PhysicsProcess(0d),
            "The default MainLoop frame hooks must perform no work and return no stop request.");
        empty.FinalizeLoop();
    }

    using (var loop = new TestMainLoop())
    {
        Expect<InvalidOperationException>(() => loop.Process(0d), "Process must require successful initialization.");
        Expect<InvalidOperationException>(() => loop.PhysicsProcess(0d), "PhysicsProcess must require successful initialization.");
        Expect<InvalidOperationException>(loop.FinalizeLoop, "FinalizeLoop must require successful initialization.");
        Expect<ArgumentOutOfRangeException>(() => loop.Process(double.NaN), "Process delta must reject NaN before execution.");

        loop.Initialize();
        Require(loop.Log.SequenceEqual(["initialize"]), "Initialize must invoke its hook exactly once.");
        Expect<InvalidOperationException>(loop.Initialize, "Initialize must reject a second call.");

        loop.ProcessResult = true;
        loop.PhysicsResult = false;
        Require(loop.Process(0.25d), "Process must return the stop request from its callback.");
        Require(!loop.PhysicsProcess(0.5d), "PhysicsProcess must return its callback result.");
        Require(loop.Log.TakeLast(2).SequenceEqual(["process:0.25", "physics:0.5"]),
            "Frame callbacks must receive their supplied deltas.");
        Expect<ArgumentOutOfRangeException>(() => loop.Process(-double.Epsilon), "Process delta must reject negatives.");
        Expect<ArgumentOutOfRangeException>(() => loop.PhysicsProcess(double.PositiveInfinity),
            "PhysicsProcess delta must reject infinity.");

        loop.ReenterProcess = true;
        loop.Process(0d);
        Require(loop.ReentryError is InvalidOperationException, "A frame callback must not re-enter frame execution.");
        loop.ReenterProcess = false;

        loop.DisposeDuringProcess = true;
        loop.Process(0d);
        Require(loop.DisposeError is InvalidOperationException && !loop.IsDisposed,
            "Disposal from a frame callback must be rejected without poisoning the loop.");
        loop.DisposeDuringProcess = false;

        loop.FinalizeDuringProcess = true;
        loop.Process(0d);
        Require(loop.FinalizeError is InvalidOperationException,
            "Finalization from a frame callback must be rejected without poisoning the loop.");
        loop.FinalizeDuringProcess = false;

        loop.ThrowOnProcess = true;
        Require(Capture(() => loop.Process(0d)) is InvalidOperationException,
            "A process callback failure must propagate.");
        loop.ThrowOnProcess = false;
        Require(loop.Process(0d), "A process callback failure must leave a running loop usable.");

        var permissions = new List<string>();
        loop.OnRequestPermissionsResult += (sender, permission, granted) =>
        {
            Require(ReferenceEquals(sender, loop), "The permission event must identify its loop.");
            permissions.Add($"{permission}:{granted}");
        };
        loop.PublishPermission("camera", granted: true);
        Require(permissions.SequenceEqual(["camera:True"]), "Permission results must preserve their typed payload.");
        Expect<ArgumentNullException>(() => loop.PublishPermission(null!, granted: false),
            "Permission publication must reject a null platform name.");

        loop.Notify(MainLoop.NotificationApplicationPaused);
        Require(loop.Notifications.Contains(MainLoop.NotificationApplicationPaused),
            "MainLoop must retain inherited numeric notification dispatch.");

        loop.FinalizeLoop();
        Require(loop.Log.Last() == "finalize" && loop.FinalizeCount == 1,
            "FinalizeLoop must invoke its hook once.");
        Expect<InvalidOperationException>(loop.FinalizeLoop, "FinalizeLoop must reject a second call.");
        Expect<InvalidOperationException>(() => loop.Process(0d), "A finalized loop must reject later frames.");
        Expect<InvalidOperationException>(() => loop.PublishPermission("camera", granted: false),
            "A finalized loop must reject permission publication.");
    }

    var disposedLoop = new TestMainLoop();
    disposedLoop.Initialize();
    disposedLoop.Dispose();
    Require(disposedLoop.IsDisposed && disposedLoop.FinalizeCount == 1,
        "Disposing a running loop must finalize it exactly once.");

    var neverInitialized = new TestMainLoop();
    neverInitialized.Dispose();
    Require(neverInitialized.FinalizeCount == 0,
        "Disposing an uninitialized loop must not invoke finalization.");

    var lifecycleReentry = new TestMainLoop
    {
        DisposeDuringInitialize = true,
        DisposeDuringFinalize = true,
        ReenterInitialize = true
    };
    lifecycleReentry.Initialize();
    Require(lifecycleReentry.InitializeReentryError is InvalidOperationException &&
            lifecycleReentry.InitializeDisposeError is InvalidOperationException && !lifecycleReentry.IsDisposed,
        "Initialization must reject recursive initialization and disposal before changing object lifetime.");
    lifecycleReentry.FinalizeLoop();
    Require(lifecycleReentry.FinalizeDisposeError is InvalidOperationException && !lifecycleReentry.IsDisposed,
        "Finalization must reject disposal until its callback reaches terminal state.");
    lifecycleReentry.Dispose();

    using (var handlerFailure = new TestMainLoop())
    {
        handlerFailure.Initialize();
        var laterHandlerRan = false;
        handlerFailure.OnRequestPermissionsResult += (_, _, _) => throw new InvalidOperationException("expected permission failure");
        handlerFailure.OnRequestPermissionsResult += (_, _, _) => laterHandlerRan = true;
        Require(Capture(() => handlerFailure.PublishPermission("camera", true)) is InvalidOperationException &&
                !laterHandlerRan && !handlerFailure.IsDisposed,
            "A permission handler failure must propagate synchronously and preserve loop lifetime.");
        handlerFailure.Process(0d);
    }

    var initializationFailure = new TestMainLoop { ThrowOnInitialize = true };
    Require(Capture(initializationFailure.Initialize) is InvalidOperationException,
        "An initialization failure must propagate.");
    Expect<InvalidOperationException>(initializationFailure.Initialize,
        "Failed initialization must be terminal instead of retrying user code.");
    Expect<InvalidOperationException>(() => initializationFailure.Process(0d),
        "A loop with failed initialization must reject frames.");
    initializationFailure.Dispose();
    Require(initializationFailure.FinalizeCount == 0 && initializationFailure.IsDisposed,
        "Disposal after failed initialization must not invoke the unmatched finalize hook.");

    var finalizationFailure = new TestMainLoop { ThrowOnFinalize = true };
    finalizationFailure.Initialize();
    Require(Capture(finalizationFailure.FinalizeLoop) is InvalidOperationException,
        "A finalization failure must propagate.");
    Expect<InvalidOperationException>(finalizationFailure.FinalizeLoop,
        "Failed finalization must still leave a terminal loop.");
    finalizationFailure.Dispose();
    Require(finalizationFailure.IsDisposed && finalizationFailure.FinalizeCount == 1,
        "Disposal must not retry a failed finalize hook.");

    var wrongThreadLoop = new TestMainLoop();
    Require(Task.Run(() => Capture(wrongThreadLoop.Initialize)).GetAwaiter().GetResult() is InvalidOperationException,
        "Initialization must reject a non-owner thread.");
    wrongThreadLoop.Initialize();
    Require(Task.Run(() => Capture(() => wrongThreadLoop.Process(0d))).GetAwaiter().GetResult() is InvalidOperationException &&
            Task.Run(() => Capture(() => wrongThreadLoop.PublishPermission("camera", true))).GetAwaiter().GetResult() is InvalidOperationException &&
            Task.Run(() => Capture(wrongThreadLoop.FinalizeLoop)).GetAwaiter().GetResult() is InvalidOperationException &&
            Task.Run(() => Capture(wrongThreadLoop.Dispose)).GetAwaiter().GetResult() is InvalidOperationException &&
            !wrongThreadLoop.IsDisposed,
        "Frames, permission results, finalization, and disposal must retain owner-thread affinity.");
    wrongThreadLoop.Dispose();

    Require(MainLoop.NotificationOsMemoryWarning == 2009 && MainLoop.NotificationTranslationChanged == 2010 &&
            MainLoop.NotificationWmAbout == 2011 && MainLoop.NotificationCrash == 2012 &&
            MainLoop.NotificationOsImeUpdate == 2013 && MainLoop.NotificationApplicationResumed == 2014 &&
            MainLoop.NotificationApplicationPaused == 2015 && MainLoop.NotificationApplicationFocusIn == 2016 &&
            MainLoop.NotificationApplicationFocusOut == 2017 && MainLoop.NotificationTextServerChanged == 2018 &&
            MainLoop.NotificationApplicationPipModeEntered == 2019 && MainLoop.NotificationApplicationPipModeExited == 2020,
        "MainLoop system notification identifiers must retain their stable values.");
    Require(Entity.NotificationOsMemoryWarning == MainLoop.NotificationOsMemoryWarning &&
            Entity.NotificationTranslationChanged == MainLoop.NotificationTranslationChanged &&
            Entity.NotificationWmAbout == MainLoop.NotificationWmAbout && Entity.NotificationCrash == MainLoop.NotificationCrash &&
            Entity.NotificationOsImeUpdate == MainLoop.NotificationOsImeUpdate &&
            Entity.NotificationApplicationResumed == MainLoop.NotificationApplicationResumed &&
            Entity.NotificationApplicationPaused == MainLoop.NotificationApplicationPaused &&
            Entity.NotificationApplicationFocusIn == MainLoop.NotificationApplicationFocusIn &&
            Entity.NotificationApplicationFocusOut == MainLoop.NotificationApplicationFocusOut &&
            Entity.NotificationTextServerChanged == MainLoop.NotificationTextServerChanged &&
            Entity.NotificationApplicationPipModeEntered == MainLoop.NotificationApplicationPipModeEntered &&
            Entity.NotificationApplicationPipModeExited == MainLoop.NotificationApplicationPipModeExited,
        "Entity system notification aliases must match MainLoop.");

    var notificationLog = new List<string>();
    var notificationRoot = new SystemNotificationNode("root", notificationLog);
    notificationRoot.AddChild(new SystemNotificationNode("child", notificationLog));
    using (var tree = new SceneTree(notificationRoot))
    {
        Require(notificationLog.SequenceEqual(["root:2010", "child:2010"]),
            "Tree entry must notify nodes that their translated messages may have changed.");
        notificationLog.Clear();
        Require(tree is MainLoop, "SceneTree must implement the MainLoop contract.");
        Expect<InvalidOperationException>(tree.Initialize, "SceneTree construction must complete MainLoop initialization.");
        Require(!tree.Process(0d) && tree.ProcessFrameCount == 1,
            "Driving SceneTree through MainLoop.Process must run its process pipeline without requesting termination.");
        tree.Notify(MainLoop.NotificationTranslationChanged);
        Require(notificationLog.SequenceEqual(["root:2010", "child:2010"]),
            "SceneTree must propagate system notifications through the active hierarchy in depth-first order.");
    }

    var failingNotificationLog = new List<string>();
    var failingNotificationRoot = new SystemNotificationNode("root", failingNotificationLog);
    failingNotificationRoot.AddChild(new SystemNotificationNode("child", failingNotificationLog));
    using (var tree = new SceneTree(failingNotificationRoot))
    {
        failingNotificationLog.Clear();
        failingNotificationRoot.ThrowOnSystem = true;
        Require(Capture(() => tree.Notify(MainLoop.NotificationApplicationPaused)) is AggregateException &&
                failingNotificationLog.SequenceEqual(["root:2015", "child:2015"]),
            "A failing system-notification callback must not prevent later live nodes from receiving it.");
    }

    var finalizeOnEnter = new FinalizeOnEnterNode();
    using (var tree = new SceneTree(finalizeOnEnter))
    {
        Require(finalizeOnEnter.FinalizeError is InvalidOperationException && !tree.IsDisposed,
            "Finalization requested from construction lifecycle must be rejected before changing loop state.");
        tree.Process(0d);
    }

    var finalizedRoot = new Entity();
    var finalizedTree = new SceneTree(finalizedRoot);
    var finalizedTimer = finalizedTree.CreateTimer(1d);
    finalizedTree.FinalizeLoop();
    Require(finalizedRoot.IsDisposed && finalizedTimer.IsDisposed && finalizedTree.NodeCount == 0 && !finalizedTree.IsDisposed,
        "Explicit SceneTree finalization must release its owned hierarchy and timers before object disposal.");
    Expect<InvalidOperationException>(() => finalizedTree.Process(0d),
        "An explicitly finalized SceneTree must reject later frames.");
    Expect<ObjectDisposedException>(() => finalizedTree.Defer(static () => { }),
        "An explicitly finalized SceneTree must reject new deferred work.");
    finalizedTree.Dispose();

    using var allocationTree = new SceneTree(new Entity());
    for (var index = 0; index < 16; index++)
        allocationTree.Process(0d);
    var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
    for (var index = 0; index < 128; index++)
    {
        allocationTree.Process(0d);
        allocationTree.PhysicsProcess(0d);
    }
    var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
    Require(allocatedBytes == 0, $"An idle SceneTree process hot path must not allocate after warm-up; observed {allocatedBytes} bytes.");
}

static void VerifyInstanceIds()
{
    const int objectCount = 10_000;
    var instanceIds = new ulong[objectCount];

    Parallel.For(0, objectCount, index => instanceIds[index] = new TestObject().InstanceID);

    Require(!instanceIds.Contains(0UL), "Instance IDs must be non-zero.");
    Require(instanceIds.Distinct().Count() == objectCount, "Instance IDs must be unique.");
}

static void VerifyLifetime()
{
    var instance = new TestObject();
    var disposedEvents = 0;

    Require(instance.ClassName == nameof(TestObject), "ClassName must contain the runtime type name.");
    Require(instance.ToString() == $"{nameof(TestObject)}#{instance.InstanceID}", "ToString must identify the instance.");

    instance.Disposed += sender =>
    {
        Require(ReferenceEquals(sender, instance), "Disposed must identify its sender.");
        Interlocked.Increment(ref disposedEvents);
    };

    Parallel.For(0, 1_000, _ => instance.Dispose());

    Require(instance.IsDisposed, "Dispose must mark the instance as disposed.");
    Require(instance.DisposeCount == 1, "Dispose(bool) must run exactly once.");
    Require(disposedEvents == 1, "Disposed must be raised exactly once.");
    Require(instance.Notifications.Count(notification => notification == ElectronObject.NotificationPreDelete) == 1,
        "Dispose must deliver one pre-delete notification.");
    Require(instance.CanTranslateDuringPreDelete,
        "The disposing thread must be able to inspect object state during pre-delete callbacks.");

    Expect<ObjectDisposedException>(instance.Use, "ThrowIfDisposed must reject access after disposal.");
}

static void VerifyNotificationsAndProperties()
{
    using var instance = new TestObject();
    var propertyListChanges = 0;
    var scriptChanges = 0;

    instance.Notify(1234);
    Require(instance.Notifications.Contains(1234), "Notify must invoke OnNotification.");

    instance.PropertyListChanged += _ => propertyListChanges++;
    instance.AnnouncePropertyListChanged();
    Require(propertyListChanges == 1, "PropertyListChanged must be raised by the protected notifier.");

    instance.ScriptChanged += sender =>
    {
        Require(ReferenceEquals(sender, instance), "ScriptChanged must identify its sender.");
        scriptChanges++;
    };
    instance.AnnounceScriptChanged();
    Require(scriptChanges == 1, "ScriptChanged must be raised by the protected notifier.");

    var properties = instance.GetPropertyList();
    var valueProperty = properties.OfType<PropertyDescriptor<TestObject, int>>().Single(property => property.Name == nameof(TestObject.Value));

    Require(valueProperty.GetValue(instance) == 0, "A property descriptor must read its value.");
    valueProperty.SetValue(instance, 42);
    Require(instance.Value == 42, "A property descriptor must write its value.");
    Require(instance.PropertyCanRevert(valueProperty), "A changed property must be revertible.");
    Require(valueProperty.TryGetRevertValue(instance, out var revertValue) && revertValue == 0,
        "A property descriptor must expose its typed revert value.");

    instance.RevertProperty(valueProperty);
    Require(instance.Value == 0, "RevertProperty must restore the descriptor's revert value.");

    Expect<ArgumentOutOfRangeException>(
        () => valueProperty.SetValue(instance, -1),
        "A property validator must reject an invalid value.");

    using var duplicateProperties = new DuplicatePropertyObject();
    Expect<InvalidOperationException>(
        () => duplicateProperties.GetPropertyList(),
        "GetPropertyList must reject duplicate property names.");
}

static void VerifyEventConnections()
{
    using var source = new TestObject();
    var duplicateCalls = 0;
    Action<ElectronObject> duplicateHandler = _ => duplicateCalls++;

    source.ScriptChanged += duplicateHandler;
    source.ScriptChanged += duplicateHandler;
    source.AnnounceScriptChanged();
    Require(duplicateCalls == 2, "Adding the same C# event handler twice must deliver it twice.");

    source.ScriptChanged -= duplicateHandler;
    source.AnnounceScriptChanged();
    Require(duplicateCalls == 3, "Removing one duplicate subscription must leave one occurrence connected.");

    source.ScriptChanged -= duplicateHandler;
    source.AnnounceScriptChanged();
    Require(duplicateCalls == 3, "Removing both duplicate subscriptions must disconnect the handler.");

    var reusableCalls = 0;
    var reusable = EventConnection.Subscribe<ElectronObject>(
        handler => source.ScriptChanged += handler,
        handler => source.ScriptChanged -= handler,
        sender =>
        {
            Require(ReferenceEquals(sender, source), "A managed subscription must preserve typed event arguments.");
            reusableCalls++;
        });

    Require(reusable.IsConnected, "A new managed subscription must be connected.");
    source.AnnounceScriptChanged();
    reusable.Dispose();
    reusable.Dispose();
    source.AnnounceScriptChanged();
    Require(reusableCalls == 1 && !reusable.IsConnected,
        "Disposal must idempotently disconnect a reusable subscription.");

    var oneShotCalls = 0;
    using var oneShot = EventConnection.Subscribe<ElectronObject>(
        handler => source.ScriptChanged += handler,
        handler => source.ScriptChanged -= handler,
        _ =>
        {
            oneShotCalls++;
            source.AnnounceScriptChanged();
        },
        oneShot: true);

    source.AnnounceScriptChanged();
    source.AnnounceScriptChanged();
    Require(oneShotCalls == 1 && !oneShot.IsConnected,
        "A one-shot subscription must disconnect before its handler and reject re-entrant delivery.");

    using var throwingSource = new TestObject();
    var throwingCalls = 0;
    using var throwingOneShot = EventConnection.Subscribe<ElectronObject>(
        handler => throwingSource.ScriptChanged += handler,
        handler => throwingSource.ScriptChanged -= handler,
        _ =>
        {
            throwingCalls++;
            throw new InvalidOperationException("expected one-shot failure");
        },
        oneShot: true);

    Require(Capture(throwingSource.AnnounceScriptChanged) is InvalidOperationException,
        "A synchronous handler exception must propagate to the event publisher.");
    Require(Capture(throwingSource.AnnounceScriptChanged) is null && throwingCalls == 1 && !throwingOneShot.IsConnected,
        "A throwing one-shot handler must remain disconnected.");

    var concurrentSource = new EventSource();
    var concurrentCalls = 0;
    using var concurrentOneShot = EventConnection.Subscribe(
        handler => concurrentSource.Pulse += handler,
        handler => concurrentSource.Pulse -= handler,
        () => Interlocked.Increment(ref concurrentCalls),
        oneShot: true);

    Parallel.For(0, 1_000, _ => concurrentSource.RaisePulse());
    Require(concurrentCalls == 1 && !concurrentOneShot.IsConnected,
        "Concurrent emissions must consume a one-shot connection exactly once.");

    var root = new Entity { Name = "event-root" };
    using var tree = new SceneTree(root);
    var deferredSource = new EventSource();
    var deferredValues = new List<int>();

    using var deferred = EventConnection.Subscribe<int>(
        handler => deferredSource.Value += handler,
        handler => deferredSource.Value -= handler,
        value => deferredValues.Add(value),
        defer: tree.Defer);

    deferredSource.RaiseValue(1);
    deferredSource.RaiseValue(2);
    Require(deferredValues.Count == 0, "Deferred subscriptions must not invoke handlers during event emission.");
    tree.FlushDeferred();
    Require(deferredValues.SequenceEqual([1, 2]), "Deferred subscriptions must capture event arguments at emission time.");

    deferredSource.RaiseValue(3);
    deferred.Dispose();
    tree.FlushDeferred();
    Require(deferredValues.SequenceEqual([1, 2]),
        "Disposing a deferred subscription must cancel callbacks that have not started.");

    var deferredOneShotCalls = 0;
    using var deferredOneShot = EventConnection.Subscribe<int>(
        handler => deferredSource.Value += handler,
        handler => deferredSource.Value -= handler,
        _ => deferredOneShotCalls++,
        oneShot: true,
        defer: tree.Defer);

    deferredSource.RaiseValue(4);
    deferredSource.RaiseValue(5);
    Require(!deferredOneShot.IsConnected && deferredOneShotCalls == 0,
        "A deferred one-shot subscription must disconnect on its first accepted emission.");
    tree.FlushDeferred();
    Require(deferredOneShotCalls == 1, "A deferred one-shot subscription must invoke exactly once at the safe point.");

    var cancelledOneShotCalls = 0;
    var cancelledOneShot = EventConnection.Subscribe<int>(
        handler => deferredSource.Value += handler,
        handler => deferredSource.Value -= handler,
        _ => cancelledOneShotCalls++,
        oneShot: true,
        defer: tree.Defer);

    deferredSource.RaiseValue(6);
    cancelledOneShot.Dispose();
    tree.FlushDeferred();
    Require(cancelledOneShotCalls == 0, "Disposal must cancel an accepted deferred one-shot callback before it starts.");

    var deferredFailureCalls = 0;
    using var deferredFailure = EventConnection.Subscribe<int>(
        handler => deferredSource.Value += handler,
        handler => deferredSource.Value -= handler,
        _ =>
        {
            deferredFailureCalls++;
            throw new InvalidOperationException("expected deferred failure");
        },
        oneShot: true,
        defer: tree.Defer);

    deferredSource.RaiseValue(7);
    Require(Capture(tree.FlushDeferred) is AggregateException && deferredFailureCalls == 1 && !deferredFailure.IsConnected,
        "Deferred handler failures must be reported by the scheduler without reconnecting one-shot subscriptions.");

    Action? partiallyAttached = null;
    Expect<InvalidOperationException>(
        () => EventConnection.Subscribe(
            handler =>
            {
                partiallyAttached += handler;
                throw new InvalidOperationException("expected subscription failure");
            },
            handler => partiallyAttached -= handler,
            () => { }),
        "A failing subscription accessor must propagate its exception.");
    Require(partiallyAttached is null, "A partially attached subscription must be rolled back.");

    var subscriptionAndRollbackFailure = Capture(() => EventConnection.Subscribe(
        _ => throw new InvalidOperationException("expected subscription failure"),
        _ => throw new InvalidOperationException("expected rollback failure"),
        () => { }));
    Require(subscriptionAndRollbackFailure is AggregateException { InnerExceptions.Count: 2 },
        "Subscription and rollback failures must both be preserved.");

    Action? removalFailureSource = null;
    var removalFailureCalls = 0;
    var removalFailure = EventConnection.Subscribe(
        handler => removalFailureSource += handler,
        _ => throw new InvalidOperationException("expected removal failure"),
        () => removalFailureCalls++);

    Expect<InvalidOperationException>(removalFailure.Dispose,
        "A failing removal accessor must propagate its exception.");
    removalFailureSource?.Invoke();
    Require(!removalFailure.IsConnected && removalFailureCalls == 0,
        "A connection must remain terminal and inert when its removal accessor fails.");

    Action? oneShotRemovalFailureSource = null;
    var oneShotRemovalFailureCalls = 0;
    using var oneShotRemovalFailure = EventConnection.Subscribe(
        handler => oneShotRemovalFailureSource += handler,
        _ => throw new InvalidOperationException("expected one-shot removal failure"),
        () => oneShotRemovalFailureCalls++,
        oneShot: true);
    Require(Capture(() => oneShotRemovalFailureSource?.Invoke()) is InvalidOperationException,
        "A one-shot removal failure must propagate before the user handler runs.");
    Require(Capture(() => oneShotRemovalFailureSource?.Invoke()) is null &&
            !oneShotRemovalFailure.IsConnected &&
            oneShotRemovalFailureCalls == 0,
        "A one-shot connection must remain consumed and inert when removal fails.");

    var schedulingSource = new EventSource();
    using var schedulingFailure = EventConnection.Subscribe(
        handler => schedulingSource.Pulse += handler,
        handler => schedulingSource.Pulse -= handler,
        () => { },
        defer: _ => throw new InvalidOperationException("expected scheduler failure"));
    Require(Capture(schedulingSource.RaisePulse) is InvalidOperationException && schedulingFailure.IsConnected,
        "A reusable connection must remain connected when its scheduler rejects one emission.");
    schedulingFailure.Dispose();

    using var oneShotSchedulingFailure = EventConnection.Subscribe(
        handler => schedulingSource.Pulse += handler,
        handler => schedulingSource.Pulse -= handler,
        () => { },
        oneShot: true,
        defer: _ => throw new InvalidOperationException("expected one-shot scheduler failure"));
    Require(Capture(schedulingSource.RaisePulse) is InvalidOperationException && !oneShotSchedulingFailure.IsConnected,
        "A one-shot connection must remain consumed when its scheduler rejects delivery.");

    var pairSource = new EventSource();
    EventSource? observedSource = null;
    var observedValue = 0;
    using var pairConnection = EventConnection.Subscribe<EventSource, int>(
        handler => pairSource.Pair += handler,
        handler => pairSource.Pair -= handler,
        (sender, value) =>
        {
            observedSource = sender;
            observedValue = value;
        });
    pairSource.RaisePair(42);
    Require(ReferenceEquals(observedSource, pairSource) && observedValue == 42,
        "Two-argument subscriptions must preserve sender-first event arguments.");

    Expect<ArgumentNullException>(
        () => EventConnection.Subscribe(null!, _ => { }, () => { }),
        "Subscribe must reject a null add accessor.");
    Expect<ArgumentNullException>(
        () => EventConnection.Subscribe(_ => { }, null!, () => { }),
        "Subscribe must reject a null remove accessor.");
    Expect<ArgumentNullException>(
        () => EventConnection.Subscribe(_ => { }, _ => { }, null!),
        "Subscribe must reject a null handler.");
}

static void VerifyTranslations()
{
    var previousCulture = TranslationServer.Culture;
    var previousEnabled = TranslationServer.Enabled;

    try
    {
        TranslationServer.Clear();
        TranslationServer.Enabled = true;
        TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-FR");
        TranslationServer.AddTranslation(CultureInfo.GetCultureInfo("fr"), "game", "Hello", "Bonjour");
        TranslationServer.AddPluralTranslation(
            CultureInfo.GetCultureInfo("fr"),
            "game",
            "apple",
            "apples",
            count => count > 1 ? "pommes" : "pomme");

        using var instance = new TestObject { TranslationDomain = "game" };

        Require(instance.Tr("Hello") == "Bonjour", "Tr must use parent-culture fallback.");
        Require(instance.Tr("Unknown") == "Unknown", "Tr must return the source message when no translation exists.");
        Require(instance.TrN("apple", "apples", 0) == "pomme", "TrN must use the registered culture-specific selector.");
        Require(instance.TrN("apple", "apples", 2) == "pommes", "TrN must resolve plural messages.");

        using var catalog = new Translation { Locale = "fr" };
        catalog.AddMessage("Play", "Jouer", "menu");
        catalog.AddPluralMessage("pear", ["poire", "poires"], "fruit");
        catalog.PluralSelector = count => count > 1 ? 1 : 0;
        Require(catalog.GetMessageCount() == 2 && catalog.GetMessageList().Contains("menu\u0004Play") &&
                catalog.GetTranslatedMessageList().Contains("poires") && catalog.GetMessage("Play") == string.Empty,
            "Translation resources must retain contextual messages and all plural forms.");
        using var duplicate = (Translation)catalog.Duplicate();
        catalog.AddMessage("Play", "Démarrer", "menu");
        Require(duplicate.GetMessage("Play", "menu") == "Jouer" &&
                duplicate.GetPluralMessage("pear", "pears", 2, "fruit") == "poires",
            "Translation duplication must copy message containers and the plural selector.");
        TranslationServer.AddTranslation(catalog, "game");
        Require(instance.Tr("Play", "menu") == "Démarrer" &&
                instance.TrN("pear", "pears", 0, "fruit") == "poire" &&
                instance.TrN("pear", "pears", 2, "fruit") == "poires",
            "Registered resource catalogs must participate in contextual and plural lookup with parent-culture fallback.");
        catalog.EraseMessage("Play", "menu");
        Require(instance.Tr("Play", "menu") == "Play", "Resource edits must become visible without re-registration.");
        TranslationServer.RemoveTranslation(catalog, "game");
        Require(instance.TrN("pear", "pears", 2, "fruit") == "pears", "Removing a catalog must stop its lookup without disposing it.");
        TranslationServer.AddTranslation(catalog, "game");
        catalog.Dispose();
        Require(instance.TrN("pear", "pears", 2, "fruit") == "pears", "Disposing a catalog must unregister it.");

        using var english = new Translation();
        english.AddPluralMessage("box", ["box", "boxes"]);
        Require(english.GetPluralMessage("box", "boxes", 1) == "box" &&
                english.GetPluralMessage("box", "boxes", 2) == "boxes",
            "English catalogs must use the source fallback plural rule.");
        Expect<ArgumentException>(() => english.AddPluralMessage("empty", []),
            "A plural catalog entry must contain at least one form.");
        using var noRule = new Translation { Locale = "fr" };
        noRule.AddPluralMessage("pear", ["poire", "poires"]);
        Expect<InvalidOperationException>(() => noRule.GetPluralMessage("pear", "pears", 2),
            "Non-English plural lookup must fail explicitly when no selector has been supplied.");

        using var source = new Translation { Locale = "fr" };
        var longValue = string.Concat(Enumerable.Repeat("économie locale ", 40));
        source.AddMessage("Compressed", longValue);
        source.AddMessage("Short", "Oui");
        source.AddMessage("ContextOnly", "Contexte", "menu");
        source.AddPluralMessage("Plural", ["Premier", "Seconds"]);
        using var optimized = new OptimizedTranslation();
        Require(!optimized.Generate(null) && optimized.GetMessageList().Length == 0,
            "Null generation leaves an optimized catalog empty.");
        Require(optimized.Generate(source) && optimized.Locale == "fr" &&
                optimized.GetMessage("Compressed") == longValue && optimized.GetMessage("Short") == "Oui" &&
                optimized.GetMessage("ContextOnly", "menu") == string.Empty &&
                optimized.GetPluralMessage("Plural", "Plurals", 2) == "Premier" &&
                optimized.GetMessageCount() == 0 && optimized.GetMessageList().Length == 0 &&
                optimized.GetTranslatedMessageList().Contains(longValue),
            "Optimized catalogs resolve compressed and raw singular values without retaining source keys or contextual/plural behavior.");
        using var emptySource = new Translation { Locale = "de" };
        emptySource.AddMessage("ContextOnly", "Nur Kontext", "menu");
        Require(!optimized.Generate(emptySource) && optimized.Locale == "fr" && optimized.GetMessage("Short") == "Oui",
            "Generation without eligible messages preserves the prior catalog.");
        using var optimizedCopy = (OptimizedTranslation)optimized.Duplicate();
        source.AddMessage("Compressed", "changed");
        optimized.Dispose();
        Require(optimizedCopy.GetMessage("Compressed") == longValue &&
                optimizedCopy.GetTranslatedMessageList().Contains("Oui"),
            "Optimized copies retain independent lookup data after source edits and original disposal.");
        for (var index = 0; index < 64; index++)
            _ = optimizedCopy.GetMessage(index % 2 == 0 ? "Compressed" : "Short");
        var optimizedAllocationsBefore = GC.GetAllocatedBytesForCurrentThread();
        string lastOptimizedMessage = string.Empty;
        for (var index = 0; index < 1_024; index++)
            lastOptimizedMessage = optimizedCopy.GetMessage(index % 2 == 0 ? "Compressed" : "Short");
        var optimizedAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - optimizedAllocationsBefore;
        Require(lastOptimizedMessage == "Oui" && optimizedAllocatedBytes == 0,
            $"Warmed optimized translation lookup must not allocate managed memory; observed {optimizedAllocatedBytes} bytes.");
        TranslationServer.AddTranslation(optimizedCopy, "game");
        Require(instance.Tr("Compressed") == longValue && instance.Tr("ContextOnly", "menu") == "ContextOnly",
            "Optimized catalogs participate in registered domain lookup without contextual entries.");
        TranslationServer.RemoveTranslation(optimizedCopy, "game");
        Expect<ObjectDisposedException>(() => optimized.GetMessageList(),
            "Disposed optimized catalogs reject inherited list access.");

        instance.CanTranslateMessages = false;
        Require(instance.Tr("Hello") == "Hello", "Per-object translation disabling must return the source message.");
    }
    finally
    {
        TranslationServer.Clear();
        TranslationServer.Culture = previousCulture;
        TranslationServer.Enabled = previousEnabled;
    }
}

static void VerifyNodeHierarchyAndTransforms()
{
    using var spatialDefault = new Entity();
    Require(spatialDefault.Transform == Transform.Identity &&
            spatialDefault.Position == Vector2.Zero && spatialDefault.Scale == Vector2.One &&
            NearlyEqual(spatialDefault.Rotation, 0f) && NearlyEqual(spatialDefault.RotationDegrees, 0f) &&
            NearlyEqual(spatialDefault.Skew, 0f) && NearlyEqual(spatialDefault.GlobalRotation, 0f) &&
            NearlyEqual(spatialDefault.GlobalRotationDegrees, 0f) && NearlyEqual(spatialDefault.GlobalSkew, 0f),
        "Entity starts with identity local/global rotation, scale, skew and position.");
    var root = new TransformNode
    {
        Name = "root",
        Position = new Vector2(10f, 5f),
        RotationDegrees = 90f
    };
    var first = new TransformNode { Name = "first", Position = new Vector2(2f, 0f) };
    var second = new TransformNode { Name = "second", Position = new Vector2(-3f, 1f) };
    var mover = new TransformNode { Name = "mover", Position = new Vector2(4f, 2f) };

    var addedChildren = new List<(Node Source, Node Child)>();
    root.ChildAdded += (source, child) => addedChildren.Add((source, child));

    root.AddChild(first);
    root.AddChild(second);
    first.AddChild(mover);

    Require(addedChildren.SequenceEqual([(root, first), (root, second)]),
        "ChildAdded must provide the publishing parent before the added child.");

    var enteredChildren = new List<(Node Source, Node Child)>();
    root.ChildEnteredTree += (source, child) => enteredChildren.Add((source, child));

    using var tree = new SceneTree(root);

    Require(enteredChildren.SequenceEqual([(root, first), (root, second)]),
        "ChildEnteredTree must provide the publishing parent before the entering child.");

    Require(VectorNearlyEqual(first.GlobalPosition, new Vector2(10f, 7f)), "A child global position must include its parent transform.");
    Require(VectorNearlyEqual(first.ToGlobal(Vector2.Zero), first.GlobalPosition), "ToGlobal must transform the local origin.");
    Require(VectorNearlyEqual(first.ToLocal(first.ToGlobal(new Vector2(3f, -2f))), new Vector2(3f, -2f)),
        "ToLocal must invert ToGlobal.");

    first.GlobalPosition = new Vector2(4f, 9f);
    Require(VectorNearlyEqual(first.Position, new Vector2(4f, 6f)), "Setting GlobalPosition must solve the local position.");

    using var coordinateParent = new Entity
    {
        Transform = new Transform(new Vector2(3f, 1f), new Vector2(-2f, 5f), new Vector2(4f, -6f)),
    };
    var coordinateChild = new Entity
    {
        Transform = new Transform(new Vector2(1.3f, 0.2f), new Vector2(-0.4f, 0.7f), new Vector2(2f, 3f)),
    };
    coordinateParent.AddChild(coordinateChild);
    var originalBasis = coordinateChild.Transform;
    coordinateChild.GlobalPosition = new Vector2(17.3f, -9.7f);
    Require(coordinateChild.Transform.X == originalBasis.X &&
            coordinateChild.Transform.Y == originalBasis.Y &&
            VectorNearlyEqual(coordinateChild.GlobalPosition, new Vector2(17.3f, -9.7f)),
        "GlobalPosition changes only local translation under a nonorthogonal parent.");
    coordinateChild.GlobalTranslate(new Vector2(2f, -3f));
    Require(coordinateChild.Transform.X == originalBasis.X &&
            coordinateChild.Transform.Y == originalBasis.Y &&
            VectorNearlyEqual(coordinateChild.GlobalPosition, new Vector2(19.3f, -12.7f)),
        "GlobalTranslate preserves the local basis and adds a world-space offset.");
    var localBeforeTranslate = coordinateChild.Position;
    var globalBeforeTranslate = coordinateChild.GlobalPosition;
    var localOffset = new Vector2(1f, -2f);
    coordinateChild.Translate(localOffset);
    Require(coordinateChild.Position == localBeforeTranslate + localOffset &&
            VectorNearlyEqual(coordinateChild.GlobalPosition,
                globalBeforeTranslate + coordinateParent.Transform.BasisXform(localOffset)),
        "Translate adds in parent coordinates while the parent's basis determines global displacement.");
    var localPoint = new Vector2(3f, -2f);
    Require(VectorNearlyEqual(coordinateChild.ToLocal(coordinateChild.ToGlobal(localPoint)), localPoint),
        "ToGlobal and ToLocal compose through a nonsingular nonorthogonal ancestor.");
    var desiredGlobal = new Transform(new Vector2(0.6f, 0.8f), new Vector2(-1f, 2f),
        new Vector2(11f, -2f));
    coordinateChild.GlobalTransform = desiredGlobal;
    Require(TransformNearlyEqual(coordinateChild.GlobalTransform, desiredGlobal) &&
            TransformNearlyEqual(coordinateChild.GetRelativeTransformToParent(coordinateParent),
                coordinateChild.Transform) &&
            coordinateChild.GetRelativeTransformToParent(coordinateChild) == Transform.Identity,
        "GlobalTransform solves the parent-local matrix and relative queries preserve the local product.");
    coordinateChild.TopLevel = true;
    coordinateChild.GlobalPosition = new Vector2(5f, 7f);
    Require(coordinateChild.Position == new Vector2(5f, 7f) &&
            coordinateChild.GetRelativeTransformToParent(coordinateParent) == coordinateChild.Transform,
        "TopLevel bypasses global parent coordinates without breaking the structural relative-transform query.");
    coordinateChild.GlobalTransform = desiredGlobal;
    Require(coordinateChild.Transform == desiredGlobal,
        "TopLevel global transform assignment writes the local matrix directly.");
    using var relativeRoot = new Entity
    {
        Transform = new Transform(new Vector2(2f, 0f), new Vector2(0f, 3f), new Vector2(6f, 8f)),
    };
    relativeRoot.AddChild(coordinateParent);
    Require(TransformNearlyEqual(coordinateChild.GetRelativeTransformToParent(relativeRoot),
            coordinateParent.Transform * coordinateChild.Transform),
        "Relative transform multiplies the full spatial ancestor chain even across TopLevel.");
    var beforeInvalidPosition = coordinateChild.Transform;
    Expect<ArgumentOutOfRangeException>(() => coordinateChild.GlobalPosition = new Vector2(float.NaN, 0f),
        "GlobalPosition rejects non-finite input before mutation.");
    Require(coordinateChild.Transform == beforeInvalidPosition,
        "Rejected GlobalPosition assignment preserves the local transform.");
    Expect<ArgumentOutOfRangeException>(() => coordinateChild.Transform = new Transform(
        new Vector2(float.NaN, 0f), Vector2.Down, Vector2.Zero),
        "Raw Transform rejects non-finite columns before mutation.");
    Expect<ArgumentOutOfRangeException>(() => coordinateChild.ToGlobal(new Vector2(float.NaN, 0f)),
        "ToGlobal rejects a non-finite point before projection.");
    Require(coordinateChild.Transform == beforeInvalidPosition,
        "Rejected transform and point operations preserve local state.");
    Expect<ArgumentNullException>(() => coordinateChild.GetRelativeTransformToParent(null!),
        "Relative transform requires an explicit ancestor.");
    using var disposedAncestor = new Entity();
    disposedAncestor.Dispose();
    Expect<ObjectDisposedException>(() => coordinateChild.GetRelativeTransformToParent(disposedAncestor),
        "Relative transform rejects a disposed ancestor before traversing the chain.");
    using (var coordinateTree = new SceneTree(relativeRoot))
    {
        Expect<InvalidOperationException>(() => Task.Run(() => _ = coordinateChild.Position).GetAwaiter().GetResult(),
            "Attached Position reads run on the scene owner thread.");
        Expect<InvalidOperationException>(() => Task.Run(() => _ = coordinateChild.Transform).GetAwaiter().GetResult(),
            "Attached Transform reads run on the scene owner thread.");
        Expect<InvalidOperationException>(() => Task.Run(() =>
            coordinateChild.GetRelativeTransformToParent(coordinateParent)).GetAwaiter().GetResult(),
            "Attached relative-transform queries run on the scene owner thread.");
    }

    first.Scale = new Vector2(2f, 3f);
    first.Skew = 0.2f;
    first.Rotation = 0.4f;
    var composed = first.Transform;
    first.Transform = composed;
    Require(VectorNearlyEqual(first.Scale, new Vector2(2f, 3f)) && NearlyEqual(first.Rotation, 0.4f) && NearlyEqual(first.Skew, 0.2f),
        "Transform decomposition must preserve rotation, scale, and skew.");

    first.ForceUpdateTransform(); mover.ForceUpdateTransform();
    first.NotifyLocalTransformChanges = true;
    first.NotifyTransformChanges = true;
    mover.NotifyTransformChanges = true;
    first.Notifications.Clear();
    mover.Notifications.Clear();
    first.Position += Vector2.One;
    first.ForceUpdateTransform(); mover.ForceUpdateTransform();
    Require(first.Notifications.Contains(Entity.NotificationLocalTransformChanged) &&
            first.Notifications.Contains(Entity.NotificationTransformChanged) &&
            mover.Notifications.Contains(Entity.NotificationTransformChanged),
        "A local transform change must notify the node and affected descendants.");

    mover.TopLevel = true; mover.ForceUpdateTransform();
    mover.Notifications.Clear();
    first.Position += Vector2.One;
    Require(!mover.Notifications.Contains(Entity.NotificationTransformChanged),
        "A top-level node must ignore ancestor transform changes.");
    mover.TopLevel = false;

    var beforeReparent = mover.GlobalTransform;
    mover.Reparent(second, keepGlobalTransform: true);
    Require(TransformNearlyEqual(mover.GlobalTransform, beforeReparent), "Reparent must preserve the global transform by default.");
    Require(root.GetNode("second/mover") == mover && root.GetNode("/root/second/mover") == mover,
        "Relative and absolute paths must resolve the same node.");
    Require(root.GetPathTo(mover) == "second/mover" && mover.GetPath() == "/root/second/mover",
        "Entity paths must describe the hierarchy.");
    Require(root.FindChild("MOV*") == mover && mover.FindParent("SEC*") == second,
        "Wildcard hierarchy search must find descendants and parents case-insensitively.");

    mover.AddToGroup("actors");
    Require(tree.GetFirstNodeInGroup("actors") == mover && tree.GetNodesInGroup("actors").SequenceEqual([mover]),
        "SceneTree group queries must return members in tree order.");
    Require(mover.RemoveFromGroup("actors") && !mover.IsInGroup("actors"), "Group removal must update membership.");

    var third = new Entity { Name = "third" };
    second.AddSibling(third);
    root.MoveChild(third, 0);
    Require(root.GetChild(0) == third && third.GetIndex() == 0, "Sibling insertion and child reordering must be observable.");

    (Node Source, Node Child)? exitingChild = null;
    (Node Source, Node Child)? removedChild = null;
    root.ChildExitingTree += (source, child) => exitingChild = (source, child);
    root.ChildRemoved += (source, child) => removedChild = (source, child);
    Require(root.RemoveChild(third), "A direct child must be removable for event verification.");
    Require(exitingChild == (root, third) && removedChild == (root, third),
        "Child exit and removal events must provide the publishing parent before the affected child.");
    root.AddChild(third);

    using var duplicate = new Entity { Name = "first" };
    Expect<InvalidOperationException>(() => root.AddChild(duplicate), "Sibling names must be unique.");

    var visibilityEvents = 0;
    mover.VisibilityChanged += _ => visibilityEvents++;
    root.Hide();
    Require(!mover.IsVisibleInTree && visibilityEvents == 1, "Ancestor visibility must affect descendants and notify them.");
    root.Show();
    Require(mover.IsVisibleInTree && visibilityEvents == 2, "Showing an ancestor must restore effective visibility.");

    root.ZIndex = 3;
    second.ZIndex = 2;
    mover.ZIndex = 1;
    Require(mover.EffectiveZIndex == 6, "Relative Z indices must accumulate through the hierarchy.");
    mover.ZAsRelative = false;
    Require(mover.EffectiveZIndex == 1, "An absolute Z index must ignore ancestors.");
    Expect<ArgumentOutOfRangeException>(() => mover.ZIndex = Entity.MaximumZIndex + 1, "ZIndex must enforce its documented range.");

    var motion = new Entity { Name = "motion" };
    motion.MoveLocalX(3f);
    motion.Rotate(System.MathF.PI / 2f);
    motion.Translate(Vector2.Right);
    motion.ApplyScale(new Vector2(2f, 4f));
    Require(VectorNearlyEqual(motion.Position, new Vector2(4f, 0f)) && VectorNearlyEqual(motion.Scale, new Vector2(2f, 4f)),
        "Local movement, rotation, translation, and scaling helpers must compose.");
    motion.LookAt(new Vector2(4f, 11f));
    Require(NearlyEqual(motion.GlobalRotation, System.MathF.PI / 2f), "LookAt must point the local +X axis at a global point.");
    var singularMotion = motion.Transform;
    singularMotion.X = Vector2.Zero;
    motion.Transform = singularMotion;
    Expect<InvalidOperationException>(() => motion.ToLocal(Vector2.Zero), "A singular transform cannot convert a global point to local space.");
    motion.Dispose();

    using var scaleNode = new Entity { Scale = new Vector2(0f, -0.000001f) };
    Require(scaleNode.Scale == new Vector2(0.00001f, 0.00001f),
        "Near-zero scale components must receive the reference positive minimum.");
    scaleNode.ApplyScale(Vector2.Zero);
    Require(scaleNode.Scale == new Vector2(0.00001f, 0.00001f),
        "ApplyScale must use the same scale boundary.");
    var originalScale = scaleNode.Transform;
    Expect<ArgumentOutOfRangeException>(() => scaleNode.Scale = new Vector2(float.NaN, 1f),
        "Scale rejects non-finite components before changing the transform.");
    Require(scaleNode.Transform == originalScale, "Rejected Scale assignments preserve state.");
    scaleNode.GlobalScale = Vector2.Zero;
    Require(scaleNode.Scale == new Vector2(0.00001f, 0.00001f),
        "GlobalScale without a canvas parent applies the local scale boundary.");
    using var scaleParent = new Entity { Scale = new Vector2(2f, 3f) };
    var scaleChild = new Entity();
    scaleParent.AddChild(scaleChild);
    scaleChild.GlobalScale = Vector2.Zero;
    Require(scaleChild.Scale == new Vector2(0.00001f, 0.00001f),
        "GlobalScale must apply the local setter's minimum after parent conversion.");
    using var reflectedParent = new Entity { Scale = new Vector2(2f, -3f) };
    var reflectedChild = new Entity();
    reflectedParent.AddChild(reflectedChild);
    reflectedChild.GlobalScale = Vector2.One;
    Require(VectorNearlyEqual(reflectedChild.Scale, new Vector2(0.5f, 1f / 3f)) &&
            VectorNearlyEqual(reflectedChild.GlobalScale, new Vector2(1f, -1f)),
        "GlobalScale preserves reflected global basis directions before local decomposition.");

    using var tinyAxis = new Entity
    {
        Transform = new Transform(new Vector2(0.0000001f, 0f), Vector2.Down, Vector2.Zero),
    };
    tinyAxis.MoveLocalX(2f);
    tinyAxis.MoveLocalY(3f);
    Require(tinyAxis.Position == new Vector2(2f, 3f),
        "MoveLocalX and MoveLocalY normalize nonzero basis axes, including a very small one.");
    Expect<ArgumentOutOfRangeException>(() => tinyAxis.MoveLocalY(float.NaN),
        "Local movement rejects an invalid delta before changing position.");
    Require(tinyAxis.Position == new Vector2(2f, 3f), "Rejected local movement preserves position.");
    using var scaledMotion = new Entity { Scale = new Vector2(2f, 3f) };
    scaledMotion.MoveLocalX(1f, scaled: true);
    scaledMotion.MoveLocalY(1f);
    scaledMotion.MoveLocalY(1f, scaled: true);
    Require(scaledMotion.Position == new Vector2(2f, 4f),
        "Scaled local movement retains basis length while the default normalizes it.");
    using var zeroAxis = new Entity
    {
        Transform = new Transform(Vector2.Zero, Vector2.Down, Vector2.Zero),
    };
    using (var zeroTree = new SceneTree(zeroAxis))
    {
        zeroAxis.NotifyLocalTransformChanges = true;
        var notifications = 0;
        zeroAxis.LocalTransformChanged += _ => notifications++;
        zeroAxis.MoveLocalX(5f);
        Require(zeroAxis.Position == Vector2.Zero && notifications == 1,
            "An exactly zero axis still commits the unchanged position and emits its local notification.");
    }

    using var singularParent = new Entity { Transform = new Transform(Vector2.Zero, Vector2.Down, Vector2.Zero) };
    var singularChild = new Entity { Position = new Vector2(2f, 3f) };
    singularParent.AddChild(singularChild);
    var originalLocalTransform = singularChild.Transform;
    Expect<InvalidOperationException>(() => singularChild.GlobalTransform = Transform.Identity,
        "A singular parent must reject global transform assignment.");
    Require(singularChild.Transform == originalLocalTransform,
        "A failed global transform assignment must preserve local state.");
    Expect<InvalidOperationException>(() => singularChild.GlobalPosition = new Vector2(8f, 9f),
        "A singular parent must reject global position assignment.");
    Require(singularChild.Transform == originalLocalTransform,
        "A failed global position assignment must preserve local state.");
    Expect<InvalidOperationException>(() => singularChild.GlobalRotation = 0.4f,
        "A singular parent rejects global rotation before local mutation.");
    Expect<InvalidOperationException>(() => singularChild.GlobalSkew = 0.2f,
        "A singular parent rejects global skew before local mutation.");
    Require(singularChild.Transform == originalLocalTransform,
        "Rejected global angle assignments preserve the local transform.");

    using var angularParent = new Entity
    {
        Position = new Vector2(3f, -4f),
        Rotation = 0.3f,
        Scale = new Vector2(2f, 3f),
        Skew = 0.2f,
    };
    var angularChild = new Entity
    {
        Position = new Vector2(5f, 7f),
        Rotation = 0.4f,
        Scale = new Vector2(1.5f, 0.8f),
        Skew = -0.1f,
    };
    angularParent.AddChild(angularChild);
    var preservedScale = angularChild.Scale;
    var preservedSkew = angularChild.Skew;
    var preservedPosition = angularChild.Position;
    angularChild.GlobalRotation = 0.75f;
    Require(VectorNearlyEqual(angularChild.Scale, preservedScale) &&
            NearlyEqual(angularChild.Skew, preservedSkew) &&
            angularChild.Position == preservedPosition,
        "GlobalRotation modifies only the child's local rotation after parent conversion.");
    var preservedRotation = angularChild.Rotation;
    preservedScale = angularChild.Scale;
    angularChild.GlobalSkew = -0.25f;
    Require(NearlyEqual(angularChild.Rotation, preservedRotation) &&
            VectorNearlyEqual(angularChild.Scale, preservedScale) &&
            angularChild.Position == preservedPosition,
        "GlobalSkew modifies only the child's local skew after parent conversion.");
    var rotationBeforeRotate = angularChild.Rotation;
    preservedScale = angularChild.Scale;
    preservedSkew = angularChild.Skew;
    angularChild.Rotate(0.2f);
    Require(NearlyEqual(angularChild.Rotation, rotationBeforeRotate + 0.2f) &&
            VectorNearlyEqual(angularChild.Scale, preservedScale) &&
            NearlyEqual(angularChild.Skew, preservedSkew),
        "Rotate adds radians while preserving local scale and skew.");
    angularChild.RotationDegrees = -90f;
    Require(NearlyEqual(angularChild.Rotation, -Mathf.Pi * 0.5f) &&
            NearlyEqual(angularChild.RotationDegrees, -90f) &&
            VectorNearlyEqual(angularChild.Scale, preservedScale) &&
            NearlyEqual(angularChild.Skew, preservedSkew),
        "RotationDegrees converts signed degrees while preserving local scale and skew.");
    preservedRotation = angularChild.Rotation;
    angularChild.Skew = 0.15f;
    Require(NearlyEqual(angularChild.Rotation, preservedRotation) &&
            NearlyEqual(angularChild.Skew, 0.15f) &&
            VectorNearlyEqual(angularChild.Scale, preservedScale),
        "Local Skew changes only the basis skew, preserving rotation and scale.");
    var localYTarget = angularChild.ToGlobal(Vector2.Down);
    Require(NearlyEqual(angularChild.GetAngleTo(localYTarget), Mathf.Pi * 0.5f),
        "GetAngleTo compensates local scale through a transformed point under a skewed parent.");
    preservedRotation = angularChild.Rotation;
    angularChild.LookAt(localYTarget);
    Require(NearlyEqual(angularChild.Rotation, preservedRotation + Mathf.Pi * 0.5f) &&
            NearlyEqual(angularChild.Skew, 0.15f) &&
            VectorNearlyEqual(angularChild.Scale, preservedScale),
        "LookAt rotates by the compensated angle without changing local scale or skew.");
    preservedRotation = angularChild.Rotation;
    angularChild.LookAt(angularChild.GlobalPosition);
    Require(NearlyEqual(angularChild.Rotation, preservedRotation),
        "Looking at the current global origin adds a zero angle.");
    preservedScale = angularChild.Scale;
    preservedSkew = angularChild.Skew;
    angularChild.GlobalRotationDegrees = 30f;
    Require(VectorNearlyEqual(angularChild.Scale, preservedScale) &&
            NearlyEqual(angularChild.Skew, preservedSkew) &&
            NearlyEqual(angularChild.GlobalRotationDegrees, Mathf.RadToDeg(angularChild.GlobalRotation)),
        "GlobalRotationDegrees delegates through global radians without altering local scale or skew.");
    var beforeInvalidAngles = angularChild.Transform;
    Expect<ArgumentOutOfRangeException>(() => angularChild.Rotation = float.NaN,
        "Rotation rejects non-finite values.");
    Expect<ArgumentOutOfRangeException>(() => angularChild.GlobalSkew = float.PositiveInfinity,
        "GlobalSkew rejects non-finite values.");
    Expect<ArgumentOutOfRangeException>(() => angularChild.GetAngleTo(new Vector2(float.NaN, 0f)),
        "GetAngleTo rejects non-finite targets.");
    Expect<ArgumentOutOfRangeException>(() => angularChild.LookAt(new Vector2(0f, float.NaN)),
        "LookAt rejects non-finite targets before rotation.");
    Require(angularChild.Transform == beforeInvalidAngles,
        "Rejected angle operations preserve the local transform.");
    using (var angularTree = new SceneTree(angularParent))
    {
        Expect<InvalidOperationException>(() => Task.Run(() => angularChild.Rotate(float.NaN)).GetAwaiter().GetResult(),
            "An attached rotation checks the scene owner before invalid numeric input.");
        Expect<InvalidOperationException>(() => Task.Run(() =>
            angularChild.GetAngleTo(new Vector2(float.NaN, 0f))).GetAwaiter().GetResult(),
            "An attached angle query checks the scene owner before invalid numeric input.");
    }

    using var reflectedAngleParent = new Entity { Scale = new Vector2(2f, -3f), Skew = 0.1f };
    var reflectedAngleChild = new Entity { Rotation = 0.2f, Scale = new Vector2(1.5f, 0.75f) };
    reflectedAngleParent.AddChild(reflectedAngleChild);
    var reflectedLocalRotation = reflectedAngleChild.Rotation;
    var reflectedLocalScale = reflectedAngleChild.Scale;
    reflectedAngleChild.GlobalSkew = 0.4f;
    Require(NearlyEqual(reflectedAngleChild.Rotation, reflectedLocalRotation) &&
            VectorNearlyEqual(reflectedAngleChild.Scale, reflectedLocalScale),
        "GlobalSkew preserves local rotation and scale under a reflected parent.");
    using var reflectedDirection = new Entity { Transform = Transform.FlipY };
    Require(NearlyEqual(reflectedDirection.GetAngleTo(reflectedDirection.ToGlobal(Vector2.Down)),
            -Mathf.Pi * 0.5f),
        "GetAngleTo retains the negative local scale sign after reflection.");
    using var singularAngle = new Entity
    {
        Transform = new Transform(Vector2.Zero, Vector2.Down, Vector2.Zero),
    };
    Expect<InvalidOperationException>(() => singularAngle.GetAngleTo(Vector2.Right),
        "GetAngleTo rejects a singular global basis.");
    Expect<InvalidOperationException>(() => singularAngle.LookAt(Vector2.Right),
        "LookAt fails before rotation when the global basis is singular.");
    Require(singularAngle.Transform.X == Vector2.Zero,
        "Failed angular queries leave a singular raw matrix unchanged.");
    using var coincidentNode = new Entity { NotifyLocalTransformChanges = true };
    using (var coincidentTree = new SceneTree(coincidentNode))
    {
        var localNotifications = 0;
        coincidentNode.LocalTransformChanged += _ => localNotifications++;
        coincidentNode.LookAt(coincidentNode.GlobalPosition);
        Require(NearlyEqual(coincidentNode.Rotation, 0f) && localNotifications == 1,
            "Coincident LookAt commits a zero rotation and delivers enabled local notification.");
    }

    using var storedEntity = new Entity
    {
        Name = "Spatial",
        Position = new Vector2(3f, 4f),
        RotationDegrees = 30f,
        Scale = new Vector2(2f, 3f),
        Skew = 0.2f,
    };
    using var packedEntity = new PackedScene();
    packedEntity.Pack(storedEntity);
    using var copiedEntity = (Entity)packedEntity.Instantiate();
    Require(TransformNearlyEqual(copiedEntity.Transform, storedEntity.Transform) &&
            copiedEntity.Position == storedEntity.Position &&
            NearlyEqual(copiedEntity.RotationDegrees, storedEntity.RotationDegrees) &&
            VectorNearlyEqual(copiedEntity.Scale, storedEntity.Scale) &&
            NearlyEqual(copiedEntity.Skew, storedEntity.Skew),
        "PackedScene restores the stored spatial position, degree rotation, scale and skew.");
    using var hotAngles = new Entity();
    for (var index = 0; index < 32; index++)
    {
        hotAngles.Rotation = 0.2f;
        hotAngles.Skew = 0.1f;
        _ = hotAngles.GetAngleTo(new Vector2(3f, 4f));
    }
    var beforeAngleAllocations = GC.GetAllocatedBytesForCurrentThread();
    for (var index = 0; index < 10_000; index++)
    {
        hotAngles.Rotation = 0.2f;
        hotAngles.Skew = 0.1f;
        _ = hotAngles.GetAngleTo(new Vector2(3f, 4f));
    }
    Require(GC.GetAllocatedBytesForCurrentThread() == beforeAngleAllocations,
        "Warmed detached spatial angle setters and queries allocate no managed memory.");
}

static void VerifyProcessing()
{
    var log = new List<string>();
    var root = new Entity { Name = "root" };
    var early = new ProcessingNode("early", log)
    {
        ProcessEnabled = true,
        PhysicsProcessEnabled = true,
        ProcessPriority = -10,
        PhysicsProcessPriority = 10,
        ProcessMode = ProcessMode.Always
    };
    var late = new ProcessingNode("late", log)
    {
        ProcessEnabled = true,
        PhysicsProcessEnabled = true,
        ProcessPriority = 10,
        PhysicsProcessPriority = -10
    };
    var pausedOnly = new ProcessingNode("paused", log)
    {
        ProcessEnabled = true,
        ProcessMode = ProcessMode.WhenPaused
    };
    var inheritedMode = new TransformNode { Name = "inherited-mode" };

    root.AddChild(early);
    root.AddChild(late);
    root.AddChild(pausedOnly);
    root.AddChild(inheritedMode);

    using var tree = new SceneTree(root);
    tree.ProcessFrame(0.25d);
    Require(log.SequenceEqual(["process:early:0.25", "process:late:0.25"]),
        "Process callbacks must use priority order and skip WhenPaused nodes while running.");

    log.Clear();
    tree.PhysicsFrame(0.5d);
    Require(log.SequenceEqual(["physics:late:0.5", "physics:early:0.5"]),
        "Physics callbacks must use their independent priority order.");

    log.Clear();
    tree.Paused = true;
    tree.ProcessFrame(0.125d);
    Require(log.SequenceEqual(["process:early:0.125", "process:paused:0.125"]),
        "Pause-aware processing must honor Always, Pausable, and WhenPaused modes.");
    Require(DoubleNearlyEqual(early.ProcessDeltaTime, 0.125d), "ProcessDeltaTime must retain the delivered frame delta.");
    Expect<ArgumentOutOfRangeException>(() => tree.ProcessFrame(double.NaN), "Frame delta must be finite.");

    inheritedMode.Notifications.Clear();
    root.ProcessMode = ProcessMode.Disabled;
    Require(inheritedMode.Notifications.Contains(Entity.NotificationDisabled) && !inheritedMode.CanProcess(),
        "Disabling an inherited process mode must notify and disable affected descendants.");
    root.ProcessMode = ProcessMode.Inherit;
    Require(inheritedMode.Notifications.Contains(Entity.NotificationEnabled),
        "Restoring an inherited process mode must notify affected descendants.");
}

static void VerifySceneTree()
{
    var manualLifecycle = new List<string>();
    var manuallyNotifiedRoot = new RecordingNode("manual", manualLifecycle);
    manuallyNotifiedRoot.Notify(Entity.NotificationReady);
    using (var manuallyNotifiedTree = new SceneTree(manuallyNotifiedRoot))
    {
        Require(manualLifecycle.Count(item => item == "ready:manual") == 2,
            "Manual lifecycle notification must not consume SceneTree's one-shot ready state.");
    }

    var queuedRoot = new Entity();
    queuedRoot.QueueFree();
    Expect<ArgumentException>(
        () => new SceneTree(queuedRoot),
        "SceneTree must reject a root already queued for deletion.");
    queuedRoot.CancelFree();
    queuedRoot.Dispose();

    var lifecycle = new List<string>();
    var root = new RecordingNode("root", lifecycle);
    var child = new RecordingNode("child", lifecycle);
    var grandchild = new RecordingNode("grandchild", lifecycle);

    child.AddChild(grandchild);
    root.AddChild(child);

    Expect<InvalidOperationException>(
        () => grandchild.AddChild(root),
        "AddChild must reject a cycle before mutating the hierarchy.");

    using var tree = new SceneTree(root);

    Expect<InvalidOperationException>(root.Dispose, "An active SceneTree root must be disposed through its owning tree.");
    Require(!root.IsDisposed && ReferenceEquals(tree.Root, root), "Rejected root disposal must leave tree ownership intact.");

    using (var otherTree = new SceneTree(new Entity { Name = "other-root" }))
    {
        Expect<InvalidOperationException>(
            () => root.AddChild(otherTree.Root),
            "A SceneTree root must not be reparented into another tree.");

        Require(otherTree.Root.Parent is null && ReferenceEquals(otherTree.Root.Tree, otherTree),
            "Rejected cross-tree parenting must not mutate either tree.");
    }

    Require(lifecycle.SequenceEqual(
    [
        "enter:root",
        "enter:child",
        "enter:grandchild",
        "ready:grandchild",
        "ready:child",
        "ready:root"
    ]), "SceneTree lifecycle order must match the documented parent-enter/child-ready order.");

    Require(root.RemoveChild(child), "RemoveChild must detach a direct child.");
    Require(lifecycle.TakeLast(2).SequenceEqual(["exit:grandchild", "exit:child"]),
        "Removing a subtree must deliver exit child-first.");
    root.AddChild(child);
    Require(lifecycle.Count(item => item == "ready:child") == 1 && lifecycle.Count(item => item == "ready:grandchild") == 1,
        "Ready must run only once when a subtree is detached and reattached.");
    child.RequestReady();
    root.RemoveChild(child);
    root.AddChild(child);
    Require(lifecycle.Count(item => item == "ready:child") == 2 && lifecycle.Count(item => item == "ready:grandchild") == 1,
        "RequestReady must re-arm only the requested node for its next attachment.");

    var firstBatch = 0;
    var secondBatch = 0;
    var deferredValue = 0;

    tree.Defer(() =>
    {
        firstBatch++;
        tree.Defer(() => secondBatch++);
    });
    tree.SetDeferred(value => deferredValue = value, 42);
    tree.FlushDeferred();

    Require(firstBatch == 1 && secondBatch == 0 && deferredValue == 42,
        "FlushDeferred must execute one captured batch and typed deferred setters.");

    tree.FlushDeferred();
    Require(secondBatch == 1, "Work deferred during a flush must run in the next batch.");

    var wrongThreadFlush = Task.Run(() => Capture(tree.FlushDeferred)).GetAwaiter().GetResult();
    Require(wrongThreadFlush is InvalidOperationException, "FlushDeferred must reject a non-owner thread.");

    var wrongThreadDispose = Task.Run(() => Capture(tree.Dispose)).GetAwaiter().GetResult();
    Require(wrongThreadDispose is InvalidOperationException && !tree.IsDisposed,
        "SceneTree disposal must reject a non-owner thread before disposal starts.");

    var wrongThreadNodeDispose = Task.Run(() => Capture(child.Dispose)).GetAwaiter().GetResult();
    Require(wrongThreadNodeDispose is InvalidOperationException && !child.IsDisposed,
        "Attached Entity disposal must reject a non-owner thread before disposal starts.");

    var directLifecycle = new List<string>();
    var directRoot = new RecordingNode("direct-root", directLifecycle);
    var directlyDisposed = new RecordingNode("direct-child", directLifecycle);
    directRoot.AddChild(directlyDisposed);
    using (var directTree = new SceneTree(directRoot))
    {
        directLifecycle.Clear();
        directlyDisposed.Dispose();
        Require(directlyDisposed.IsDisposed && directRoot.ChildCount == 0 && directLifecycle.SequenceEqual(["exit:direct-child"]),
            "Direct disposal must detach an attached node while allowing its exit callback to inspect node state.");
    }

    child.QueueFree();
    Require(child.IsQueuedForDeletion && child.CancelFree(), "CancelFree must cancel queued deletion.");
    tree.FlushDeferred();
    Require(!child.IsDisposed && ReferenceEquals(child.Parent, root), "Cancelled deletion must preserve the node.");

    child.QueueFree();
    tree.FlushDeferred();
    Require(child.IsDisposed && grandchild.IsDisposed, "Queued deletion must dispose the complete subtree.");
    Require(root.Children.Count == 0, "Queued deletion must remove the node from its parent.");
}

static void VerifySceneTreeGroupsEventsAndTimers()
{
    var groupLog = new List<string>();
    var root = new GroupNode("root", groupLog);
    var child = new GroupNode("child", groupLog);
    var grandchild = new GroupNode("grandchild", groupLog);
    child.AddChild(grandchild);
    root.AddChild(child);

    foreach (var node in new[] { root, child, grandchild })
        node.AddToGroup("actors");

    using var tree = new SceneTree(root);
    var treeProperties = tree.GetPropertyList().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
    Require(treeProperties.IsSupersetOf([nameof(SceneTree.Root), nameof(SceneTree.Paused), nameof(SceneTree.NodeCount),
        nameof(SceneTree.ProcessFrameCount), nameof(SceneTree.PhysicsFrameCount), nameof(SceneTree.HasDeferredWork)]),
        "SceneTree must expose its typed public state through property descriptors.");
    Require(tree.NodeCount == 3 && tree.GetNodeCountInGroup("actors") == 3 && tree.HasGroup("actors"),
        "SceneTree counts and group presence must reflect the active hierarchy.");

    tree.CallGroup("actors", node => groupLog.Add($"call:{node.Name}"));
    Require(groupLog.SequenceEqual(["call:root", "call:child", "call:grandchild"]),
        "Immediate group calls must follow hierarchy order.");

    groupLog.Clear();
    tree.CallGroup("actors", node => groupLog.Add($"reverse:{node.Name}"), GroupCallFlags.Reverse);
    Require(groupLog.SequenceEqual(["reverse:grandchild", "reverse:child", "reverse:root"]),
        "Reverse group calls must visit descendants before ancestors.");

    tree.SetGroup("actors", static (node, visible) => ((CanvasItem)node).Visible = visible, false);
    Require(tree.GetNodesInGroup("actors").All(node => !((CanvasItem)node).Visible),
        "Typed group setters must apply the captured value to every current member.");

    tree.NotifyGroup("actors", 9_001);
    Require(groupLog.TakeLast(3).SequenceEqual(["notify:root:9001", "notify:child:9001", "notify:grandchild:9001"]),
        "Group notifications must use hierarchy order.");

    groupLog.Clear();
    Action<Node> uniqueCall = node => groupLog.Add($"unique:{node.Name}");
    var uniqueFlags = GroupCallFlags.Deferred | GroupCallFlags.Unique;
    tree.CallGroup("actors", uniqueCall, uniqueFlags);
    tree.CallGroup("actors", uniqueCall, uniqueFlags);
    tree.FlushDeferred();
    Require(groupLog.SequenceEqual(["unique:root", "unique:child", "unique:grandchild"]),
        "Equal unique deferred group calls must execute only once.");
    Expect<ArgumentException>(
        () => tree.CallGroup("actors", uniqueCall, GroupCallFlags.Unique),
        "Unique group calls must require deferred scheduling.");

    var nodeEvents = new List<string>();
    var treeChanges = 0;
    tree.NodeAdded += (_, node) => nodeEvents.Add($"added:{node.Name}");
    tree.NodeRemoved += (_, node) => nodeEvents.Add($"removed:{node.Name}");
    tree.NodeRenamed += (_, node) => nodeEvents.Add($"renamed:{node.Name}");
    tree.TreeChanged += _ => treeChanges++;

    var added = new Entity { Name = "added" };
    var addedChild = new Entity { Name = "added-child" };
    added.AddChild(addedChild);
    root.AddChild(added);
    added.Name = "renamed";
    root.RemoveChild(added);

    Require(nodeEvents.SequenceEqual(["added:added", "added:added-child", "renamed:renamed", "removed:added-child", "removed:renamed"]),
        "Tree node events must describe parent-first entry, rename, and child-first exit.");
    Require(treeChanges == 3, "TreeChanged must report insertion, rename, and removal once each.");
    added.Dispose();

    var frameStarts = new List<string>();
    tree.ProcessFrameStarted += _ => frameStarts.Add("process");
    tree.PhysicsFrameStarted += _ => frameStarts.Add("physics");
    tree.ProcessFrame(0.01d);
    tree.PhysicsFrame(0.02d);
    Require(frameStarts.SequenceEqual(["process", "physics"]) &&
            tree.ProcessFrameCount == 1 && tree.PhysicsFrameCount == 1,
        "Frame events and counters must advance once per valid frame attempt.");

    var timeoutCount = 0;
    var pausableTimer = tree.CreateTimer(0.5d, processAlways: false);
    var timerProperties = pausableTimer.GetPropertyList().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
    Require(timerProperties.IsSupersetOf([nameof(SceneTreeTimer.TimeLeft), nameof(SceneTreeTimer.ProcessAlways),
        nameof(SceneTreeTimer.ProcessInPhysics)]),
        "SceneTreeTimer must expose its typed public state through property descriptors.");
    pausableTimer.Timeout += _ => timeoutCount++;
    tree.Paused = true;
    tree.ProcessFrame(0.5d);
    Require(timeoutCount == 0 && DoubleNearlyEqual(pausableTimer.TimeLeft, 0.5d),
        "A pause-aware timer must not advance while the tree is paused.");
    tree.Paused = false;
    tree.ProcessFrame(0.25d);
    tree.ProcessFrame(0.25d);
    Require(timeoutCount == 1 && pausableTimer.IsDisposed,
        "A timer must emit once and dispose itself when its delay reaches zero.");

    var physicsTimeouts = 0;
    var physicsTimer = tree.CreateTimer(0.1d, processInPhysics: true);
    physicsTimer.Timeout += _ => physicsTimeouts++;
    tree.ProcessFrame(1d);
    Require(physicsTimeouts == 0, "A physics timer must ignore process frames.");
    tree.PhysicsFrame(0.1d);
    Require(physicsTimeouts == 1 && physicsTimer.IsDisposed, "A physics timer must expire in the physics lane.");

    var laterTimerRan = false;
    tree.CreateTimer(0d).Timeout += _ => throw new InvalidOperationException("expected timer failure");
    tree.CreateTimer(0d).Timeout += _ => laterTimerRan = true;
    Require(Capture(() => tree.ProcessFrame(0d)) is AggregateException && laterTimerRan,
        "A failing timeout handler must not stop later timers or the frame safe point.");
    Expect<ArgumentOutOfRangeException>(() => tree.CreateTimer(double.PositiveInfinity),
        "Timer duration must be finite.");

    using var queuedObject = new TestObject();
    tree.QueueDelete(queuedObject);
    tree.FlushDeferred();
    Require(queuedObject.IsDisposed, "QueueDelete must dispose a detached engine object in the deletion phase.");

    tree.Defer(tree.FlushDeferred);
    Require(Capture(tree.FlushDeferred) is AggregateException && !tree.IsDisposed,
        "Re-entrant flush must be rejected without poisoning the tree.");
    tree.Defer(tree.Dispose);
    Require(Capture(tree.FlushDeferred) is AggregateException && !tree.IsDisposed,
        "Disposal from a frame or flush callback must be rejected before lifetime changes.");
}

static void VerifyTimers()
{
    Require((int)TimerProcessCallback.Physics == 0 && (int)TimerProcessCallback.Idle == 1 &&
            Entity.NotificationInternalProcess == 25 && Entity.NotificationInternalPhysicsProcess == 26,
        "Timer process lanes and internal Entity notifications must retain their stable identities.");

    using (var detached = new EngineTimer())
    {
        Require(detached.ProcessCallback == TimerProcessCallback.Idle && DoubleNearlyEqual(detached.WaitTime, 1d) &&
                !detached.OneShot && !detached.Autostart && !detached.Paused && !detached.IgnoreTimeScale &&
                detached.TimeLeft == 0d && detached.IsStopped(),
            "A Timer must begin stopped with the documented defaults.");

        var descriptors = detached.GetPropertyList().ToDictionary(property => property.Name, StringComparer.Ordinal);
        Require(descriptors[nameof(EngineTimer.ProcessCallback)].IsStored &&
                descriptors[nameof(EngineTimer.WaitTime)].IsStored &&
                descriptors[nameof(EngineTimer.OneShot)].IsStored &&
                descriptors[nameof(EngineTimer.Autostart)].IsStored &&
                descriptors[nameof(EngineTimer.IgnoreTimeScale)].IsStored &&
                !descriptors[nameof(EngineTimer.Paused)].IsStored &&
                descriptors[nameof(EngineTimer.TimeLeft)].IsReadOnly &&
                !descriptors[nameof(EngineTimer.TimeLeft)].IsStored,
            "Timer descriptors must distinguish stored configuration from runtime-only state.");

        detached.Autostart = true;
        detached.Stop();
        Require(!detached.Autostart && detached.IsStopped(),
            "Stopping a detached Timer must clear autostart without emitting a timeout.");
        Expect<InvalidOperationException>(detached.Start,
            "A detached Timer must reject Start while allowing Stop.");

        foreach (var invalid in new[] { 0d, -1d, double.NaN, double.PositiveInfinity })
        {
            Expect<ArgumentOutOfRangeException>(() => detached.WaitTime = invalid,
                "Timer.WaitTime must reject non-positive and non-finite values.");
        }

        Expect<ArgumentOutOfRangeException>(
            () => detached.ProcessCallback = (TimerProcessCallback)99,
            "Timer must reject undefined callback lanes.");
        Require(DoubleNearlyEqual(detached.WaitTime, 1d) && detached.ProcessCallback == TimerProcessCallback.Idle,
            "Rejected Timer configuration must preserve prior state.");
    }

    var root = new Entity { Name = "timer-root" };
    var timer = new EngineTimer { Name = "timer", WaitTime = 0.5d };
    root.AddChild(timer);
    using (var tree = new SceneTree(root))
    {
        var timeoutCount = 0;
        var timeoutObservedReload = false;
        timer.Timeout += source =>
        {
            timeoutCount++;
            timeoutObservedReload = !source.IsStopped() && source.TimeLeft <= source.WaitTime;
        };

        timer.Start();
        tree.ProcessFrame(0.2d);
        Require(timeoutCount == 0 && DoubleNearlyEqual(timer.TimeLeft, 0.3d) && !timer.IsStopped(),
            "A running process Timer must expose its remaining countdown.");
        timer.WaitTime = 0.25d;
        Require(DoubleNearlyEqual(timer.TimeLeft, 0.3d),
            "Changing WaitTime must not reset the active countdown.");
        tree.ProcessFrame(0.3d);
        Require(timeoutCount == 0 && timer.IsStopped() && timer.TimeLeft == 0d,
            "An exact-zero countdown is observably stopped but waits for a negative internal remainder before timeout.");
        tree.ProcessFrame(0.01d);
        Require(timeoutCount == 1 && timeoutObservedReload && DoubleNearlyEqual(timer.TimeLeft, 0.24d),
            "A repeating Timer must reload the negative remainder before synchronous timeout delivery.");

        timer.OneShot = true;
        timer.Start(0.1d);
        timeoutObservedReload = false;
        tree.ProcessFrame(0.1d);
        Require(timeoutCount == 1 && timer.TimeLeft == 0d,
            "A one-shot Timer must not emit at the exact-zero boundary.");
        tree.ProcessFrame(0.01d);
        Require(timeoutCount == 2 && timer.IsStopped() && timer.TimeLeft == 0d && !timeoutObservedReload,
            "A one-shot Timer must stop before timeout delivery after the countdown becomes negative.");

        timer.OneShot = false;
        timer.Start(0.25d);
        timer.Paused = true;
        tree.ProcessFrame(1d);
        Require(DoubleNearlyEqual(timer.TimeLeft, 0.25d) && timeoutCount == 2,
            "A locally paused Timer must preserve its countdown.");
        timer.Start(0.2d);
        tree.ProcessFrame(1d);
        Require(DoubleNearlyEqual(timer.TimeLeft, 0.2d),
            "Starting a paused Timer must reset without resuming it.");
        timer.Paused = false;
        tree.ProcessFrame(0.2d);
        Require(timeoutCount == 2 && timer.TimeLeft == 0d,
            "Resumed timers retain strict-negative expiry at an exact-zero step.");
        tree.ProcessFrame(0.01d);
        Require(timeoutCount == 3,
            "Unpausing a running Timer must resume its preserved countdown.");

        timer.OneShot = true;
        timer.ProcessCallback = TimerProcessCallback.Idle;
        timer.Start(0.2d);
        tree.ProcessFrame(0.05d);
        timer.ProcessCallback = TimerProcessCallback.Physics;
        tree.ProcessFrame(1d);
        Require(!timer.IsStopped() && DoubleNearlyEqual(timer.TimeLeft, 0.15d),
            "Moving a running Timer to the physics lane must preserve its countdown and ignore process frames.");
        tree.PhysicsFrame(0.2d);
        Require(timer.IsStopped() && timeoutCount == 4,
            "A physics Timer must expire only in its selected lane.");

        timer.ProcessCallback = TimerProcessCallback.Idle;
        timer.OneShot = false;
        timer.Start(0.1d);
        tree.ProcessFrame(0.35d);
        Require(timeoutCount == 5 && timer.TimeLeft == 0d,
            "A repeating Timer must emit at most once per frame even when one delta spans several periods.");
        tree.ProcessFrame(0d);
        Require(timeoutCount == 6 && timer.TimeLeft == 0d,
            "An overshooting repeat frame must retain its negative remainder and catch up once on the next frame.");

        timer.OneShot = true;
        timer.Start(0.1d);
        Expect<ArgumentOutOfRangeException>(() => timer.Start(0d),
            "Timer.Start(duration) must reject a non-positive duration without changing the active countdown.");
        Require(DoubleNearlyEqual(timer.WaitTime, 0.1d) && DoubleNearlyEqual(timer.TimeLeft, 0.1d),
            "A rejected Timer restart must preserve wait and countdown state.");
        tree.Paused = true;
        tree.ProcessFrame(1d);
        Require(!timer.IsStopped() && DoubleNearlyEqual(timer.TimeLeft, 0.1d),
            "A Timer must honor inherited scene-tree pause policy.");
        timer.ProcessMode = ProcessMode.Always;
        tree.ProcessFrame(0.11d);
        Require(timer.IsStopped() && timeoutCount == 7,
            "Always-processing mode must allow a Timer to advance while the tree is paused.");
        tree.Paused = false;

        timer.Start(0.5d);
        timer.Stop();
        tree.ProcessFrame(1d);
        Require(timer.IsStopped() && timeoutCount == 7,
            "Stop must disable internal processing and must not emit Timeout.");

        var wrongThreadWait = Task.Run(() => Capture(() => timer.WaitTime = 1d)).GetAwaiter().GetResult();
        var wrongThreadStart = Task.Run(() => Capture(timer.Start)).GetAwaiter().GetResult();
        var wrongThreadStop = Task.Run(() => Capture(timer.Stop)).GetAwaiter().GetResult();
        Require(wrongThreadWait is InvalidOperationException && wrongThreadStart is InvalidOperationException &&
                wrongThreadStop is InvalidOperationException,
            "Attached Timer mutation must retain scene-tree owner-thread affinity.");
    }

    var autostartRoot = new Entity { Name = "autostart-root" };
    using (var autostartTree = new SceneTree(autostartRoot))
    {
        var autostartTimer = new EngineTimer
        {
            Name = "autostart-timer",
            Autostart = true,
            OneShot = true,
            WaitTime = 0.2d,
        };
        var autostartTimeouts = 0;
        autostartTimer.Timeout += _ => autostartTimeouts++;
        autostartRoot.AddChild(autostartTimer);
        Require(!autostartTimer.Autostart && !autostartTimer.IsStopped() &&
                DoubleNearlyEqual(autostartTimer.TimeLeft, 0.2d),
            "Autostart must start during ready delivery and clear itself.");
        autostartTree.ProcessFrame(0.2d);
        Require(autostartTimeouts == 0 && autostartTimer.TimeLeft == 0d,
            "Autostart timers also wait for a negative remainder.");
        autostartTree.ProcessFrame(0.01d);
        Require(autostartTimeouts == 1 && autostartTimer.IsStopped(),
            "An automatically started one-shot Timer must expire normally.");
    }

    var order = new List<string>();
    var orderRoot = new Entity { Name = "order-root" };
    var orderingTimer = new ProcessingTimer(order)
    {
        Name = "ordering-timer",
        OneShot = true,
        WaitTime = 0.1d,
        ProcessEnabled = true,
        ProcessPriority = -1,
    };
    var laterNode = new ProcessingNode("later", order) { ProcessEnabled = true, ProcessPriority = 1 };
    orderingTimer.Timeout += _ =>
    {
        order.Add("timeout");
        throw new InvalidOperationException("expected Timer timeout failure");
    };
    orderRoot.AddChild(orderingTimer);
    orderRoot.AddChild(laterNode);
    using (var orderTree = new SceneTree(orderRoot))
    {
        orderingTimer.Start();
        Require(Capture(() => orderTree.ProcessFrame(0.11d)) is AggregateException &&
                order.SequenceEqual(["timeout", "process:timer:0.11", "process:later:0.11"]),
            "Internal timeout failures must not suppress the node's public callback or later scheduled nodes.");
    }

    var disableLog = new List<string>();
    var disableRoot = new Entity();
    var disablingTimer = new ProcessingTimer(disableLog)
    {
        OneShot = true,
        WaitTime = 0.1d,
        ProcessEnabled = true,
    };
    disableRoot.AddChild(disablingTimer);
    using (var disableTree = new SceneTree(disableRoot))
    {
        disablingTimer.Timeout += source => source.ProcessEnabled = false;
        disablingTimer.Start();
        disableTree.ProcessFrame(0.11d);
        Require(disableLog.Count == 0,
            "A Timer that disables its public callback during timeout must not receive that callback later in the frame.");
    }

    var removalLog = new List<string>();
    var removalRoot = new Entity();
    var removedTimer = new ProcessingTimer(removalLog)
    {
        OneShot = true,
        WaitTime = 0.1d,
        ProcessEnabled = true,
    };
    removalRoot.AddChild(removedTimer);
    using (var removalTree = new SceneTree(removalRoot))
    {
        removedTimer.Timeout += _ => removalRoot.RemoveChild(removedTimer);
        removedTimer.Start();
        removalTree.ProcessFrame(0.11d);
        Require(removedTimer.Tree is null && removalLog.Count == 0,
            "A Timer removed by its internal timeout callback must not receive its public callback later in the frame.");
        removedTimer.Dispose();
    }

    using (var packed = new PackedScene())
    {
        var source = new EngineTimer
        {
            Name = "packed-timer",
            ProcessCallback = TimerProcessCallback.Physics,
            WaitTime = 2.5d,
            OneShot = true,
            Autostart = true,
            Paused = true,
            IgnoreTimeScale = true,
        };
        packed.Pack(source);
        source.Dispose();
        using var instance = (EngineTimer)packed.Instantiate();
        Require(instance.Name == "packed-timer" && instance.ProcessCallback == TimerProcessCallback.Physics &&
                DoubleNearlyEqual(instance.WaitTime, 2.5d) && instance.OneShot && instance.Autostart &&
                instance.IgnoreTimeScale && !instance.Paused && instance.IsStopped(),
            "PackedScene must preserve Timer configuration without persisting runtime pause or countdown state.");
    }

    var engine = Engine.Instance;
    var previousTimeScale = engine.TimeScale;
    var scaledRoot = new Entity { Name = "scaled-timer-root" };
    var scaledTimer = new EngineTimer { Name = "scaled", Autostart = true, OneShot = true, WaitTime = 0.1d };
    var unscaledTimer = new EngineTimer
    {
        Name = "unscaled",
        Autostart = true,
        OneShot = true,
        WaitTime = 0.1d,
        IgnoreTimeScale = true,
    };
    var unscaledPhysicsTimer = new EngineTimer
    {
        Name = "unscaled-physics",
        Autostart = true,
        OneShot = true,
        WaitTime = 0.5d,
        IgnoreTimeScale = true,
        ProcessCallback = TimerProcessCallback.Physics,
    };
    scaledRoot.AddChild(scaledTimer);
    scaledRoot.AddChild(unscaledTimer);
    scaledRoot.AddChild(unscaledPhysicsTimer);
    using (var scaledTree = new SceneTree(scaledRoot))
    {
        var unscaledTimeouts = 0;
        var physicsFrames = 0;
        var physicsProcessStep = 0d;
        scaledTree.PhysicsFrameStarted += source =>
        {
            physicsFrames++;
            physicsProcessStep = source.CurrentUnscaledProcessStep ?? 0d;
        };
        unscaledTimer.Timeout += _ => unscaledTimeouts++;
        unscaledPhysicsTimer.Timeout += _ => unscaledTimeouts++;
        try
        {
            engine.TimeScale = 0d;
            engine.Start(scaledTree);
            _ = engine.AdvanceFrame(1d);
            Require(!scaledTimer.IsStopped() && DoubleNearlyEqual(scaledTimer.TimeLeft, 0.1d) &&
                    unscaledTimer.IsStopped() && unscaledPhysicsTimer.IsStopped() && unscaledTimeouts == 2 &&
                    physicsFrames > 1 && physicsProcessStep > 0.05d && physicsFrames / 60d < 0.5d,
                "IgnoreTimeScale must subtract Engine's process step in both lanes, even when scaled time is frozen.");
            engine.Stop();
        }
        finally
        {
            if (ReferenceEquals(engine.MainLoop, scaledTree))
                engine.Stop();
            engine.TimeScale = previousTimeScale;
        }
    }

    var allocationRoot = new Entity();
    var allocationTimer = new EngineTimer { WaitTime = 100d };
    allocationRoot.AddChild(allocationTimer);
    using (var allocationTree = new SceneTree(allocationRoot))
    {
        allocationTimer.Start();
        for (var index = 0; index < 16; index++)
            allocationTree.ProcessFrame(0d);
        var beforeNotify = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 128; index++)
            allocationTimer.Notify(Entity.NotificationInternalProcess);
        var notifyAllocated = GC.GetAllocatedBytesForCurrentThread() - beforeNotify;
        allocationTimer.Stop();
        var beforeStopped = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 128; index++)
            allocationTree.ProcessFrame(0d);
        var stoppedAllocated = GC.GetAllocatedBytesForCurrentThread() - beforeStopped;
        allocationTimer.Start();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 128; index++)
            allocationTree.ProcessFrame(0d);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(notifyAllocated == 0 && stoppedAllocated == 0 && allocated == 0,
            $"A warmed Timer process hot path must not allocate managed memory; observed {allocated} bytes after {notifyAllocated} notification bytes and {stoppedAllocated} stopped bytes.");
    }
}

static void VerifyTweens()
{
    Require((int)Tween.TweenProcessMode.Physics == 0 && (int)Tween.TweenProcessMode.Idle == 1 &&
            (int)Tween.TweenPauseMode.Bound == 0 && (int)Tween.TweenPauseMode.Stop == 1 &&
            (int)Tween.TweenPauseMode.Process == 2 && (int)Tween.TransitionType.Spring == 11 &&
            (int)Tween.EaseType.OutIn == 3,
        "Tween process, pause, transition, and easing identities must remain stable.");
    Require(Enum.GetValues<Tween.TweenProcessMode>().Select(value => (int)value).SequenceEqual(Enumerable.Range(0, 2)) &&
            Enum.GetValues<Tween.TweenPauseMode>().Select(value => (int)value).SequenceEqual(Enumerable.Range(0, 3)) &&
            Enum.GetValues<Tween.TransitionType>().Select(value => (int)value).SequenceEqual(Enumerable.Range(0, 12)) &&
            Enum.GetValues<Tween.EaseType>().Select(value => (int)value).SequenceEqual(Enumerable.Range(0, 4)),
        "Every public tween policy and curve identity must be contiguous and match its pinned numeric value.");
    Require(DoubleNearlyEqual(Tween.InterpolateValue(10d, 10d, 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.InOut), 15d) &&
            Tween.InterpolateValue(new Vector2(1f, 2f), new Vector2(2f, 4f), 1d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == new Vector2(3f, 6f) &&
            Tween.InterpolateValue(new Vector3(1f, 2f, 3f), new Vector3(2f, 4f, 6f), 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == new Vector3(2f, 4f, 6f) &&
            Tween.InterpolateValue(new Vector3i(1, 2, 3), Vector3i.One, 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == new Vector3i(2, 3, 4) &&
            Tween.InterpolateValue(Transform.Identity, new Transform(0f, new Vector2(4f, 6f)), 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In).Origin == new Vector2(2f, 3f) &&
            Tween.InterpolateValue(0, 1, 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == 1 &&
            Tween.InterpolateValue(0, -1, 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == -1 &&
            Tween.InterpolateValue(false, true, 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) &&
            Tween.InterpolateValue(2, 6, 0d, 0d,
                Tween.TransitionType.Bounce, Tween.EaseType.Out) == 8,
        "Typed manual interpolation must support scalar, vector, transform-composition, and zero-duration final values.");
    foreach (var transition in Enum.GetValues<Tween.TransitionType>())
    {
        foreach (var ease in Enum.GetValues<Tween.EaseType>())
        {
            var start = Tween.InterpolateValue(0d, 1d, 0d, 1d, transition, ease);
            var middle = Tween.InterpolateValue(0d, 1d, 0.5d, 1d, transition, ease);
            var end = Tween.InterpolateValue(0d, 1d, 1d, 1d, transition, ease);
            Require(start == 0d && double.IsFinite(middle) && end == 1d,
                $"Transition {transition}/{ease} must preserve finite endpoints.");
        }
    }
    Require(DoubleNearlyEqual(Tween.InterpolateValue(0d, 1d, 0.25d, 1d,
                Tween.TransitionType.Expo, Tween.EaseType.InOut), 0.015125d) &&
            DoubleNearlyEqual(Tween.InterpolateValue(0d, 1d, 0.25d, 1d,
                Tween.TransitionType.Elastic, Tween.EaseType.InOut), 0.011969444423734025d) &&
            DoubleNearlyEqual(Tween.InterpolateValue(0d, 1d, 0.25d, 1d,
                Tween.TransitionType.Back, Tween.EaseType.InOut), -0.09968184375d),
        "Transition-specific in-out curves must retain their reference equations.");
    Expect<NotSupportedException>(() => Tween.InterpolateValue(DateTime.UnixEpoch, DateTime.UnixEpoch, 0d, 1d,
            Tween.TransitionType.Linear, Tween.EaseType.In),
        "Manual interpolation must reject unsupported value types.");
    Expect<ArgumentOutOfRangeException>(() => Tween.InterpolateValue(0d, 1d, double.NaN, 1d,
            Tween.TransitionType.Linear, Tween.EaseType.In),
        "Manual interpolation must reject non-finite elapsed time.");

    using (var detached = new Entity())
        Expect<InvalidOperationException>(() => _ = detached.CreateTween(), "A detached node must not create a tween.");

    var root = new Entity { Name = "tween-root" };
    var target = new Entity { Name = "target" };
    root.AddChild(target);
    using var tree = new SceneTree(root);

    var eventLog = new List<string>();
    var sequential = target.CreateTween();
    var property = sequential.TweenProperty(target, static node => node.Position,
        static (node, value) => node.Position = value, new Vector2(10f, 20f), 1d);
    property.Finished += _ => eventLog.Add("property");
    sequential.StepFinished += (_, step) => eventLog.Add($"step:{step}");
    sequential.Finished += _ => eventLog.Add("finished");
    sequential.TweenCallback(() => eventLog.Add("callback"));
    Require(tree.GetProcessedTweens().SequenceEqual([sequential]) && sequential.IsRunning() && sequential.IsValid(),
        "A created tween must be valid, running, and discoverable before its first frame.");
    tree.ProcessFrame(0.5d);
    Require(target.Position == new Vector2(5f, 10f) && eventLog.Count == 0,
        "A property tweener must interpolate from the frame-start value.");
    tree.ProcessFrame(0.5d);
    Require(target.Position == new Vector2(10f, 20f) && eventLog.SequenceEqual(["property", "step:0"]) &&
            sequential.IsValid(),
        "An exact step boundary must leave the following zero-duration step for a later nonzero frame.");
    tree.ProcessFrame(0.1d);
    Require(target.Position == new Vector2(10f, 20f) &&
            eventLog.SequenceEqual(["property", "step:0", "callback", "step:1", "finished"]) &&
            sequential.IsValid() && !sequential.IsRunning() && sequential.HasTweeners() && sequential.GetLoopsLeft() == 0 &&
            tree.GetProcessedTweens().SequenceEqual([sequential]),
        "The finishing step must deliver events while the stopped tween remains registered.");
    tree.ProcessFrame(0.1d);
    Require(!sequential.IsValid() && !sequential.HasTweeners() && tree.GetProcessedTweens().Count == 0,
        "The next matching frame must unregister and clear a finished tween.");
    Expect<InvalidOperationException>(() => sequential.TweenInterval(1d),
        "An invalid tween must reject new tweeners.");

    var parallelValues = new List<double>();
    var parallel = tree.CreateTween().SetParallel();
    parallel.TweenMethod(parallelValues.Add, 0d, 1d, 1d);
    parallel.TweenCallback(() => eventLog.Add("parallel-callback")).SetDelay(0.5d);
    parallel.Chain().TweenCallback(() => eventLog.Add("after-parallel"));
    tree.ProcessFrame(0.5d);
    Require(parallelValues.Count == 1 && DoubleNearlyEqual(parallelValues[0], 0.5d) &&
            eventLog[^1] == "parallel-callback",
        "Parallel tweeners must share a step and advance together.");
    tree.ProcessFrame(0.5d);
    Require(parallel.IsValid(), "An exact parallel-step boundary must defer the following step.");
    tree.ProcessFrame(0.1d);
    Require(eventLog[^1] == "after-parallel" && parallel.IsValid() && !parallel.IsRunning(),
        "Chain must restore sequential appending after a parallel group.");
    tree.ProcessFrame(0.1d);
    Require(!parallel.IsValid(), "A finished parallel sequence must leave the tree on the following frame.");

    var oneShotLog = new List<string>();
    var oneShot = tree.CreateTween();
    oneShot.TweenInterval(1d);
    oneShot.Parallel().TweenCallback(() => oneShotLog.Add("paired"));
    oneShot.TweenCallback(() => oneShotLog.Add("later"));
    tree.ProcessFrame(0.5d);
    Require(oneShotLog.SequenceEqual(["paired"]), "Parallel must join only the next append.");
    tree.ProcessFrame(0.5d);
    tree.ProcessFrame(0.1d);
    Require(oneShotLog.SequenceEqual(["paired", "later"]) && oneShot.IsValid() && !oneShot.IsRunning(),
        "A later append must return to sequential grouping.");
    tree.ProcessFrame(0.1d);
    Require(!oneShot.IsValid(), "The one-shot grouping tween must leave on the next frame.");

    var firstCurve = 0d;
    var secondCurve = 0d;
    var curveDefaults = tree.CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
    curveDefaults.TweenMethod(value => firstCurve = value, 0d, 1d, 1d);
    curveDefaults.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
    curveDefaults.TweenMethod(value => secondCurve = value, 0d, 1d, 1d);
    tree.ProcessFrame(0.5d);
    Require(DoubleNearlyEqual(firstCurve, 0.25d) && secondCurve == 0d,
        "Transition and ease defaults must be captured when each tweener is appended.");
    tree.ProcessFrame(0.5d);
    tree.ProcessFrame(0.5d);
    Require(DoubleNearlyEqual(secondCurve, 0.875d),
        "Later method tweeners must use the changed transition and ease defaults.");
    curveDefaults.Kill();

    target.Position = Vector2.Zero;
    var restart = target.CreateTween();
    restart.TweenProperty(target, static node => node.Position,
        static (node, value) => node.Position = value, new Vector2(10f, 0f), 1d);
    tree.ProcessFrame(0.5d);
    restart.Stop();
    target.Position = new Vector2(2f, 0f);
    restart.Play();
    tree.ProcessFrame(0.5d);
    Require(target.Position == new Vector2(6f, 0f) && DoubleNearlyEqual(restart.GetTotalElapsedTime(), 0.5d),
        "Stop and Play must restart property interpolation from the then-current value.");
    restart.Kill();

    var suspended = tree.CreateTween();
    Require(suspended.IsValid() && suspended.IsRunning() && !suspended.HasTweeners() &&
            suspended.GetTotalElapsedTime() == 0d,
        "A new tween must be registered and running before any tweener is appended.");
    suspended.TweenInterval(1d);
    tree.ProcessFrame(0.25d);
    suspended.Pause();
    tree.ProcessFrame(0.5d);
    Require(suspended.IsValid() && !suspended.IsRunning() && DoubleNearlyEqual(suspended.GetTotalElapsedTime(), 0.25d) &&
            tree.GetProcessedTweens().Contains(suspended),
        "Pause must retain the current step and elapsed time while the tree keeps the tween registered.");
    suspended.Play();
    tree.ProcessFrame(0.25d);
    Require(suspended.IsRunning() && DoubleNearlyEqual(suspended.GetTotalElapsedTime(), 0.5d),
        "Play must resume the paused step without resetting elapsed time.");
    suspended.Kill();
    suspended.Kill();
    var killedSnapshot = tree.GetProcessedTweens();
    Require(!suspended.IsValid() && !suspended.IsRunning() && suspended.HasTweeners() &&
            tree.GetProcessedTweens().Contains(suspended),
        "Kill must be idempotent, invalidate immediately and retain its registry entry until a matching step.");
    suspended.Stop();
    suspended.Pause();
    Require(!suspended.IsValid() && !suspended.IsRunning() && suspended.HasTweeners() &&
            !suspended.CustomStep(0.1d) && tree.GetProcessedTweens().Contains(suspended),
        "Stop, Pause and CustomStep on an invalid tween must not restore registration.");
    Expect<InvalidOperationException>(suspended.Play,
        "Play must reject an invalid tween even after Stop clears its dead flag.");
    tree.ProcessFrame(0.1d);
    Require(!tree.GetProcessedTweens().Contains(suspended) && !suspended.HasTweeners() &&
            killedSnapshot.Contains(suspended),
        "The next matching tree step must clear a killed tween without changing prior snapshots.");

    var disposedKilled = tree.CreateTween();
    disposedKilled.TweenInterval(1d);
    disposedKilled.Kill();
    Require(tree.GetProcessedTweens().Contains(disposedKilled),
        "A killed tween must remain registered before explicit disposal or its next lane step.");
    disposedKilled.Dispose();
    Require(!tree.GetProcessedTweens().Contains(disposedKilled),
        "Disposal must remove a previously killed tween without waiting for another frame.");

    var stoppedLoops = tree.CreateTween().SetLoops(3);
    stoppedLoops.TweenInterval(1d);
    tree.ProcessFrame(1.1d);
    Require(stoppedLoops.GetLoopsLeft() == 2 && DoubleNearlyEqual(stoppedLoops.GetTotalElapsedTime(), 1.1d),
        "Loop progress and elapsed time must include the delivered overshoot.");
    stoppedLoops.Stop();
    Require(stoppedLoops.IsValid() && !stoppedLoops.IsRunning() && stoppedLoops.GetLoopsLeft() == 2 &&
            stoppedLoops.GetTotalElapsedTime() == 0d && tree.GetProcessedTweens().Contains(stoppedLoops),
        "Stop must reset elapsed time while retaining the last observable loop count until restart.");
    stoppedLoops.Play();
    tree.ProcessFrame(0.25d);
    Require(stoppedLoops.IsRunning() && stoppedLoops.GetLoopsLeft() == 3 &&
            DoubleNearlyEqual(stoppedLoops.GetTotalElapsedTime(), 0.25d),
        "The first frame after Stop and Play must restart the loop count and sequence cursor.");
    stoppedLoops.Kill();

    var restartedFromFinishCount = 0;
    var restartedFromFinish = tree.CreateTween();
    restartedFromFinish.Finished += tween =>
    {
        restartedFromFinishCount++;
        if (restartedFromFinishCount == 1)
        {
            tween.Stop();
            tween.Play();
        }
    };
    restartedFromFinish.TweenInterval(0.1d);
    tree.ProcessFrame(0.2d);
    Require(restartedFromFinishCount == 1 && restartedFromFinish.IsValid() && restartedFromFinish.IsRunning(),
        "A Finished subscriber must be able to stop and restart the still-registered tween.");
    tree.ProcessFrame(0.2d);
    Require(restartedFromFinishCount == 2 && restartedFromFinish.IsValid() && !restartedFromFinish.IsRunning(),
        "The restarted tween must finish once more before tree removal.");
    tree.ProcessFrame(0d);
    Require(!restartedFromFinish.IsValid() && !restartedFromFinish.HasTweeners(),
        "A finished tween must leave the tree and clear tweeners on the next matching frame.");

    var overshootTime = tree.CreateTween();
    overshootTime.TweenInterval(0.2d);
    tree.ProcessFrame(0.5d);
    Require(DoubleNearlyEqual(overshootTime.GetTotalElapsedTime(), 0.5d) && overshootTime.IsValid(),
        "Total elapsed time must include the full final-frame delta, including overshoot.");
    Expect<InvalidOperationException>(overshootTime.Play,
        "Play must reject a finished tween until Stop resets it.");
    tree.ProcessFrame(0d);
    Require(DoubleNearlyEqual(overshootTime.GetTotalElapsedTime(), 0.5d) && !overshootTime.IsValid(),
        "Tree removal must retain the final elapsed-time query value.");

    using (var configuredHolder = new TweenValueHolder { Value = 2d })
    {
        var configured = tree.CreateTween();
        configured.TweenProperty(configuredHolder, static value => value.Value,
                static (value, current) => value.Value = current, 3d, 1d)
            .From(4d)
            .AsRelative()
            .SetDelay(0.1d)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In)
            .SetCustomInterpolator(static weight => weight * 2d);
        tree.ProcessFrame(0.6d);
        Require(DoubleNearlyEqual(configuredHolder.Value, 5.5d),
            "Property configuration must apply explicit starts, relative ends, delays, transition overrides, and custom weight mapping.");
        configured.Kill();

        configuredHolder.Value = 4d;
        var fromCurrent = tree.CreateTween();
        fromCurrent.TweenProperty(configuredHolder, static value => value.Value,
                static (value, current) => value.Value = current, 10d, 1d)
            .FromCurrent();
        configuredHolder.Value = 8d;
        tree.ProcessFrame(0.5d);
        Require(DoubleNearlyEqual(configuredHolder.Value, 7d),
            "FromCurrent must use the value captured when the property tweener was appended.");
        fromCurrent.Kill();
    }

    var loopCallbacks = 0;
    var loopEvents = 0;
    var loops = tree.CreateTween().SetLoops(2);
    loops.LoopFinished += (_, completed) => loopEvents += completed;
    loops.TweenCallback(() => loopCallbacks++);
    tree.ProcessFrame(0.1d);
    Require(loopCallbacks == 2 && loopEvents == 1 && loops.IsValid() && !loops.IsRunning(),
        "A finite loop count must describe total sequence executions and omit LoopFinished after the final loop.");
    tree.ProcessFrame(0.1d);
    Require(!loops.IsValid(), "The completed finite loop must unregister on the following frame.");

    var counted = tree.CreateTween().SetLoops(3);
    counted.TweenInterval(1d);
    Require(counted.GetLoopsLeft() == 3, "A new finite tween must report all scheduled executions.");
    tree.ProcessFrame(1d);
    Require(counted.GetLoopsLeft() == 2, "Completing one loop must decrement the remaining count.");
    counted.Kill();

    var infinite = tree.CreateTween().SetLoops();
    Require(infinite.GetLoopsLeft() == -1, "An infinite tween must report the infinite remaining-count sentinel.");
    var infiniteSpeed = false;
    infinite.LoopFinished += (_, _) =>
    {
        infiniteSpeed = !infiniteSpeed;
        infinite.SetSpeedScale(infiniteSpeed ? 2d : 1d);
    };
    infinite.TweenCallback(static () => { });
    Require(Capture(() => tree.ProcessFrame(0.1d)) is AggregateException && !infinite.IsValid(),
        "A zero-duration infinite loop must be stopped even when a loop callback changes speed scale.");

    var negativeLoops = tree.CreateTween().SetLoops(-1);
    negativeLoops.TweenInterval(1d);
    tree.ProcessFrame(1d);
    Require(negativeLoops.IsValid() && negativeLoops.GetLoopsLeft() == -1,
        "A negative loop count must retain infinite repetition after the first loop.");
    negativeLoops.Kill();

    var pausedValue = 0d;
    var pausedTween = tree.CreateTween();
    pausedTween.TweenMethod(value => pausedValue = value, 0d, 1d, 1d);
    tree.Paused = true;
    tree.ProcessFrame(0.5d);
    Require(pausedValue == 0d, "An unbound Bound tween must stop with a paused tree.");
    pausedTween.SetPauseMode(Tween.TweenPauseMode.Process);
    tree.ProcessFrame(0.5d);
    Require(DoubleNearlyEqual(pausedValue, 0.5d), "Process pause mode must ignore tree pause.");
    tree.Paused = false;
    pausedTween.Kill();

    var stoppedValue = 0d;
    var stoppedByTree = tree.CreateTween().SetPauseMode(Tween.TweenPauseMode.Stop);
    stoppedByTree.TweenMethod(value => stoppedValue = value, 0d, 1d, 1d);
    tree.Paused = true;
    tree.ProcessFrame(0.5d);
    Require(stoppedValue == 0d, "Stop pause mode must halt with a paused tree.");
    tree.Paused = false;
    tree.ProcessFrame(0.5d);
    Require(DoubleNearlyEqual(stoppedValue, 0.5d), "Stop pause mode must resume after the tree unpauses.");
    stoppedByTree.Kill();

    var alwaysNode = new Entity { ProcessMode = ProcessMode.Always };
    root.AddChild(alwaysNode);
    var boundPauseValue = 0d;
    var boundPause = alwaysNode.CreateTween();
    boundPause.TweenMethod(value => boundPauseValue = value, 0d, 1d, 1d);
    tree.Paused = true;
    tree.ProcessFrame(0.5d);
    Require(DoubleNearlyEqual(boundPauseValue, 0.5d),
        "Bound pause mode must follow a bound node's effective Always process policy.");
    boundPause.SetPauseMode(Tween.TweenPauseMode.Stop);
    tree.ProcessFrame(0.5d);
    Require(DoubleNearlyEqual(boundPauseValue, 0.5d),
        "Switching to Stop must override a bound node's process policy while the tree is paused.");
    tree.Paused = false;
    boundPause.Kill();
    alwaysNode.Dispose();

    var speedPolicyValue = 0d;
    var scaled = tree.CreateTween().SetSpeedScale(0d);
    scaled.TweenMethod(value => speedPolicyValue = value, 0d, 1d, 1d);
    tree.ProcessFrame(0.5d);
    Require(scaled.IsRunning() && speedPolicyValue == 0d && scaled.GetTotalElapsedTime() == 0d,
        "Zero speed must freeze progression without pausing the tween.");
    scaled.SetSpeedScale(2d);
    tree.ProcessFrame(0.25d);
    Require(DoubleNearlyEqual(speedPolicyValue, 0.5d) && DoubleNearlyEqual(scaled.GetTotalElapsedTime(), 0.5d),
        "Changing speed while running must scale the next delivered delta.");
    scaled.Kill();

    var reverseSpeedValue = 0d;
    var reverseSpeed = tree.CreateTween().SetSpeedScale(-1d);
    reverseSpeed.TweenMethod(value => reverseSpeedValue = value, 0d, 1d, 1d);
    tree.ProcessFrame(0.5d);
    Require(reverseSpeed.IsRunning() && reverseSpeedValue == 0d &&
            DoubleNearlyEqual(reverseSpeed.GetTotalElapsedTime(), -0.5d),
        "Negative speed must reduce elapsed time without advancing the current tweener.");
    reverseSpeed.Kill();

    var physicsValue = 0d;
    var physics = tree.CreateTween().SetProcessMode(Tween.TweenProcessMode.Physics);
    physics.TweenMethod(value => physicsValue = value, 0d, 1d, 1d);
    tree.ProcessFrame(0.5d);
    Require(physicsValue == 0d, "A physics tween must ignore process frames.");
    tree.PhysicsFrame(0.5d);
    Require(DoubleNearlyEqual(physicsValue, 0.5d), "A physics tween must advance after physics callbacks.");
    physics.Kill();

    var killedPhysics = tree.CreateTween().SetProcessMode(Tween.TweenProcessMode.Physics);
    killedPhysics.TweenInterval(1d);
    killedPhysics.Kill();
    Require(!killedPhysics.IsValid() && tree.GetProcessedTweens().Contains(killedPhysics),
        "A killed physics tween must remain in the registry until an eligible physics step.");
    tree.ProcessFrame(0.1d);
    Require(tree.GetProcessedTweens().Contains(killedPhysics),
        "An idle frame must not sweep a killed physics tween.");
    tree.PhysicsFrame(0.1d);
    Require(!tree.GetProcessedTweens().Contains(killedPhysics) && !killedPhysics.HasTweeners(),
        "The next physics frame must clear the killed physics tween.");

    var bound = new Entity { Name = "bound" };
    root.AddChild(bound);
    var boundFinished = false;
    var boundTween = bound.CreateTween();
    boundTween.Finished += _ => boundFinished = true;
    boundTween.TweenInterval(1d);
    bound.Dispose();
    tree.ProcessFrame(0.5d);
    Require(!boundTween.IsValid() && !boundFinished,
        "Disposing a bound node must kill the tween without reporting ordinary completion.");

    using (var foreignTree = new SceneTree(new Entity()))
    {
        var crossTree = tree.CreateTween();
        Expect<ArgumentException>(() => crossTree.BindNode(foreignTree.Root),
            "A tween must reject a node owned by another scene tree.");
        crossTree.Kill();
    }

    using (var detachedBound = new Entity())
    {
        var detachedValue = 0d;
        var detachedTween = tree.CreateTween().BindNode(detachedBound);
        detachedTween.TweenMethod(value => detachedValue = value, 0d, 1d, 1d);
        tree.ProcessFrame(0.5d);
        Require(detachedValue == 0d && detachedTween.IsValid(),
            "Binding a detached node must retain but suspend its tween.");
        root.AddChild(detachedBound);
        tree.ProcessFrame(0.5d);
        Require(DoubleNearlyEqual(detachedValue, 0.5d),
            "Binding must resume progression when its node enters the owning tree.");
        detachedTween.Kill();
        root.RemoveChild(detachedBound);
    }

    var disposedTarget = new TweenValueHolder();
    var disposedTargetFinished = false;
    var disposedTargetTween = tree.CreateTween();
    disposedTargetTween.TweenProperty(disposedTarget, static value => value.Value,
            static (value, current) => value.Value = current, 1d, 1d)
        .Finished += _ => disposedTargetFinished = true;
    disposedTarget.Dispose();
    tree.ProcessFrame(0.1d);
    Require(disposedTargetFinished && disposedTargetTween.IsValid() && !disposedTargetTween.IsRunning(),
        "A disposed property target must finish its tweener without invoking an invalid setter.");
    tree.ProcessFrame(0.1d);
    Require(!disposedTargetTween.IsValid() && !disposedTargetTween.HasTweeners(),
        "Tree removal must clear a completed property tween.");

    var manualValue = 0d;
    var manual = tree.CreateTween();
    manual.TweenMethod(value => manualValue = value, 0d, 1d, 1d);
    manual.Pause();
    Require(manual.CustomStep(0.25d) && DoubleNearlyEqual(manualValue, 0.25d) && !manual.IsRunning(),
        "CustomStep must advance a paused tween without changing its paused state.");
    Require(manual.CustomStep(1d) && manual.IsValid() && !manual.IsRunning(),
        "CustomStep must report the finishing step while retaining registration.");
    Require(!manual.CustomStep(0d) && manual.IsValid() && manual.HasTweeners() &&
            tree.GetProcessedTweens().Contains(manual),
        "The next manual step must report completion while the tree retains the tween.");
    tree.ProcessFrame(0d);
    Require(!manual.IsValid() && !manual.HasTweeners() && !tree.GetProcessedTweens().Contains(manual),
        "The next matching tree frame must unregister a manually finished tween.");
    Require(!manual.CustomStep(0d), "Further manual steps on an invalid tween must remain terminal.");

    var signedStepValue = 0d;
    var signedStep = tree.CreateTween();
    signedStep.TweenMethod(value => signedStepValue = value, 0d, 1d, 1d);
    signedStep.Pause();
    Expect<ArgumentOutOfRangeException>(() => signedStep.CustomStep(double.NaN),
        "Manual stepping must reject a non-finite delta before changing tween state.");
    Require(signedStep.CustomStep(-0.25d) && signedStepValue == 0d &&
            DoubleNearlyEqual(signedStep.GetTotalElapsedTime(), -0.25d) && !signedStep.IsRunning(),
        "A negative manual step must reduce accumulated time without advancing tweeners or unpausing.");
    Require(signedStep.CustomStep(0.5d) && DoubleNearlyEqual(signedStepValue, 0.5d) &&
            DoubleNearlyEqual(signedStep.GetTotalElapsedTime(), 0.25d),
        "A later positive manual step must advance the paused sequence from its retained cursor.");
    signedStep.Kill();

    var failingManual = tree.CreateTween();
    failingManual.TweenCallback(() => throw new InvalidOperationException("expected manual tween failure"));
    Require(Capture(() => failingManual.CustomStep(0.1d)) is AggregateException &&
            !tree.GetProcessedTweens().Contains(failingManual),
        "A failing custom step must unregister its invalid tween.");

    using var source = new TweenEventSource();
    var awaited = false;
    var awaitTween = tree.CreateTween();
    awaitTween.TweenAwait(source, handler => source.Fired += handler, handler => source.Fired -= handler);
    awaitTween.TweenCallback(() => awaited = true);
    tree.ProcessFrame(0.1d);
    source.Emit();
    tree.ProcessFrame(0.1d);
    Require(!awaited && awaitTween.IsValid(), "An event wait must consume the frame in which it observes the event.");
    tree.ProcessFrame(0.1d);
    Require(awaited && awaitTween.IsValid() && !awaitTween.IsRunning(),
        "An awaited typed event must release the sequence on the following step.");
    tree.ProcessFrame(0.1d);
    Require(!awaitTween.IsValid(), "A finished event wait must unregister on the next frame.");

    var timedOut = false;
    var timeoutTween = tree.CreateTween();
    timeoutTween.TweenAwait(source, handler => source.Fired += handler, handler => source.Fired -= handler).SetTimeout(0.2d);
    timeoutTween.TweenCallback(() => timedOut = true);
    tree.ProcessFrame(0.3d);
    Require(timedOut && timeoutTween.IsValid() && !timeoutTween.IsRunning(),
        "An event wait timeout must preserve overshoot for later steps.");
    tree.ProcessFrame(0.1d);
    Require(!timeoutTween.IsValid(), "A completed timeout sequence must unregister on the next frame.");

    var preEmitted = false;
    var preEmittedTween = tree.CreateTween();
    preEmittedTween.TweenAwait(source, handler => source.Fired += handler, handler => source.Fired -= handler);
    preEmittedTween.TweenCallback(() => preEmitted = true);
    source.Emit();
    tree.ProcessFrame(0.1d);
    Require(!preEmitted && preEmittedTween.IsValid(),
        "An event observed before the wait starts must be cleared when the step begins.");
    source.Emit();
    tree.ProcessFrame(0.1d);
    Require(!preEmitted && preEmittedTween.IsValid(),
        "An event observed by an active wait must consume its completion frame.");
    tree.ProcessFrame(0.1d);
    Require(preEmitted && preEmittedTween.IsValid() && !preEmittedTween.IsRunning(),
        "An active event observation must release the following sequence step on the next frame.");
    tree.ProcessFrame(0.1d);
    Require(!preEmittedTween.IsValid(), "The finished observed-event tween must unregister next frame.");

    var nestedLog = new List<string>();
    var childTween = tree.CreateTween();
    childTween.TweenInterval(0.2d);
    childTween.TweenCallback(() => nestedLog.Add("child"));
    var parentTween = tree.CreateTween();
    parentTween.TweenCallback(() => nestedLog.Add("before"));
    parentTween.TweenSubtween(childTween);
    parentTween.TweenCallback(() => nestedLog.Add("after"));
    Require(!tree.GetProcessedTweens().Contains(childTween),
        "Appending a subtween must remove it from independent tree processing.");
    tree.ProcessFrame(0.3d);
    tree.ProcessFrame(0.1d);
    Require(nestedLog.SequenceEqual(["before", "child", "after"]) &&
            parentTween.IsValid() && childTween.IsValid() && !parentTween.IsRunning(),
        "A finished subtween must forward unused time to the parent's next step before tree removal.");
    tree.ProcessFrame(0.1d);
    Require(!parentTween.IsValid() && !childTween.IsValid(),
        "A completed parent must unregister and terminate its nested tween on the next frame.");

    var dynamicNestedCalls = 0;
    var nestingController = tree.CreateTween();
    var dynamicParent = tree.CreateTween();
    var dynamicChild = tree.CreateTween();
    dynamicChild.TweenMethod(_ => dynamicNestedCalls++, 0d, 1d, 1d);
    nestingController.TweenCallback(() => dynamicParent.TweenSubtween(dynamicChild));
    tree.ProcessFrame(0.1d);
    Require(dynamicNestedCalls == 1 && !tree.GetProcessedTweens().Contains(dynamicChild),
        "A tween nested after frame capture must not also run as a stale top-level snapshot entry.");
    dynamicParent.Kill();
    Require(!dynamicParent.IsValid() && !dynamicChild.IsValid(),
        "Killing a parent tween must invalidate its nested tween immediately.");

    var migratedLaneValue = 0d;
    var laneController = tree.CreateTween();
    var migratedLane = tree.CreateTween();
    migratedLane.TweenMethod(value => migratedLaneValue = value, 0d, 1d, 1d);
    laneController.TweenCallback(() => migratedLane.SetProcessMode(Tween.TweenProcessMode.Physics));
    tree.ProcessFrame(0.1d);
    Require(migratedLaneValue == 0d,
        "A captured tween moved to another lane before its turn must not run in the stale lane.");
    tree.PhysicsFrame(0.1d);
    Require(DoubleNearlyEqual(migratedLaneValue, 0.1d),
        "A tween moved between lanes must run on the next matching captured lane.");
    migratedLane.Kill();

    var laterTweenRan = false;
    var failing = tree.CreateTween();
    failing.TweenCallback(() => throw new InvalidOperationException("expected tween failure"));
    tree.CreateTween().TweenCallback(() => laterTweenRan = true);
    tree.Defer(() => eventLog.Add("deferred-after-tween-failure"));
    Require(Capture(() => tree.ProcessFrame(0.1d)) is AggregateException && laterTweenRan &&
            eventLog[^1] == "deferred-after-tween-failure" && !failing.IsValid(),
        "A failing tween callback must invalidate that tween without stopping later tweens or deferred work.");

    var completionSiblingRan = false;
    var afterCompletionEventsRan = false;
    var tweenerEventFailure = tree.CreateTween().SetParallel();
    tweenerEventFailure.TweenCallback(static () => { }).Finished +=
        _ => throw new InvalidOperationException("expected tweener completion failure");
    tweenerEventFailure.TweenCallback(() => completionSiblingRan = true);
    var stepEventFailure = tree.CreateTween();
    stepEventFailure.StepFinished += (_, _) => throw new InvalidOperationException("expected step failure");
    stepEventFailure.TweenCallback(static () => { });
    var loopEventFailure = tree.CreateTween().SetLoops(2);
    loopEventFailure.LoopFinished += (_, _) => throw new InvalidOperationException("expected loop failure");
    loopEventFailure.TweenCallback(static () => { });
    var finishedEventFailure = tree.CreateTween();
    finishedEventFailure.Finished += _ => throw new InvalidOperationException("expected finished failure");
    finishedEventFailure.TweenCallback(static () => { });
    tree.CreateTween().TweenCallback(() => afterCompletionEventsRan = true);
    Require(Capture(() => tree.ProcessFrame(0.1d)) is AggregateException && completionSiblingRan &&
            afterCompletionEventsRan && !tweenerEventFailure.IsValid() && !stepEventFailure.IsValid() &&
            !loopEventFailure.IsValid() && !finishedEventFailure.IsValid(),
        "Tweener, step, loop, and tween completion failures must invalidate only their sequences and preserve later work.");

    using (var cancellationSource = new TweenEventSource())
    {
        var cancellationFailure = tree.CreateTween();
        cancellationFailure.TweenAwait(cancellationSource,
            handler => cancellationSource.Fired += handler,
            handler =>
            {
                cancellationSource.Fired -= handler;
                throw new InvalidOperationException("expected cancellation failure");
            });
        Require(Capture(cancellationFailure.Kill) is AggregateException && !cancellationFailure.IsValid() &&
                tree.GetProcessedTweens().Contains(cancellationFailure),
            "Cancellation accessor failures must leave a terminal tween for the next registry sweep.");
        tree.ProcessFrame(0.1d);
        Require(!tree.GetProcessedTweens().Contains(cancellationFailure),
            "The next matching frame must clear a failed cancellation entry.");
    }

    var spawned = false;
    var spawnedRan = false;
    tree.CreateTween().TweenCallback(() =>
    {
        spawned = true;
        tree.CreateTween().TweenCallback(() => spawnedRan = true);
    });
    tree.ProcessFrame(0.1d);
    Require(spawned && !spawnedRan, "A tween created during tween processing must wait for the next frame snapshot.");
    tree.ProcessFrame(0.1d);
    Require(spawnedRan, "A tween deferred by the processing snapshot must run on the next matching frame.");

    var validation = tree.CreateTween();
    Expect<ArgumentOutOfRangeException>(() => validation.SetSpeedScale(double.PositiveInfinity),
        "Tween speed must reject non-finite values.");
    Expect<ArgumentOutOfRangeException>(() => validation.SetProcessMode((Tween.TweenProcessMode)99),
        "Tween process mode must reject undefined values.");
    Expect<ArgumentOutOfRangeException>(() => validation.SetPauseMode((Tween.TweenPauseMode)99),
        "Tween pause mode must reject undefined values.");
    Expect<ArgumentOutOfRangeException>(() => validation.SetTrans((Tween.TransitionType)99),
        "Tween transition defaults must reject undefined curves.");
    Expect<ArgumentOutOfRangeException>(() => validation.SetEase((Tween.EaseType)99),
        "Tween ease defaults must reject undefined directions.");
    Expect<InvalidOperationException>(() => Task.Run(() => validation.Pause()).GetAwaiter().GetResult(),
        "Tween mutation must require the scene-tree owner thread.");
    validation.Kill();

    var empty = tree.CreateTween();
    Require(Capture(() => tree.ProcessFrame(0d)) is AggregateException && !empty.IsValid(),
        "A matching zero-delta frame must reject and invalidate an empty tween.");

    var engine = Engine.Instance;
    var previousTimeScale = engine.TimeScale;
    using (var scaleTree = new SceneTree(new Entity()))
    {
        var scaledValue = 0d;
        var originalValue = 0d;
        scaleTree.CreateTween().TweenMethod(value => scaledValue = value, 0d, 1d, 0.1d);
        scaleTree.CreateTween().SetIgnoreTimeScale().TweenMethod(value => originalValue = value, 0d, 1d, 0.1d);
        var restoredScaleValue = 0d;
        scaleTree.CreateTween().SetIgnoreTimeScale().SetIgnoreTimeScale(false)
            .TweenMethod(value => restoredScaleValue = value, 0d, 1d, 0.1d);
        engine.Start(scaleTree);
        try
        {
            engine.TimeScale = 0d;
            engine.AdvanceFrame(1d);
            Require(scaledValue == 0d && DoubleNearlyEqual(originalValue, 1d) && restoredScaleValue == 0d,
                "Time-scale bypass must use the original delta and its false setting must restore scaled time.");
        }
        finally
        {
            engine.TimeScale = previousTimeScale;
            if (ReferenceEquals(engine.MainLoop, scaleTree))
                engine.Stop();
        }
    }

    var finalizedTree = new SceneTree(new Entity());
    var finalizedTween = finalizedTree.CreateTween();
    finalizedTween.TweenInterval(1d);
    finalizedTree.FinalizeLoop();
    Require(!finalizedTween.IsValid() && Capture(() => _ = finalizedTree.CreateTween()) is ObjectDisposedException,
        "Scene-tree finalization must invalidate active tweens and reject new ones.");
    finalizedTree.Dispose();

    using var allocationTree = new SceneTree(new Entity());
    using var holder = new TweenValueHolder();
    var allocationTween = allocationTree.CreateTween();
    allocationTween.TweenMethod(value => holder.Value = value, 0d, 1d, 1_000d);
    for (var index = 0; index < 16; index++)
        allocationTree.ProcessFrame(0.001d);
    var before = GC.GetAllocatedBytesForCurrentThread();
    for (var index = 0; index < 128; index++)
        allocationTree.ProcessFrame(0.001d);
    var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    Require(allocated == 0,
        $"A warmed active-tween frame path must not allocate managed memory; observed {allocated} bytes.");
}

static void VerifyTweenInterpolation()
{
    // Pinned easing equations sampled at elapsed fractions 0.25 and 0.75 in enum order.
    double[] expected =
    [
        0.25d, 0.25d, 0.25d, 0.25d,
        0.076120467488713262d, 0.38268343236508978d, 0.14644660940672621d, 0.35355339059327373d,
        0.0009765625d, 0.7626953125d, 0.015625d, 0.484375d,
        0.00390625d, 0.68359375d, 0.03125d, 0.46875d,
        0.0625d, 0.4375d, 0.125d, 0.375d,
        0.0045242717280199029d, 0.82404652800806644d, 0.015125d, 0.48485937499999993d,
        -0.0055242716334749425d, 0.91161168420415184d, 0.011969447209942691d, 0.5078125056307734d,
        0.015625d, 0.578125d, 0.0625d, 0.4375d,
        0.031754163448145745d, 0.66143782776614768d, 0.066987298107780702d, 0.4330127018922193d,
        0.027343755587935226d, 0.47265625d, 0.11718749441206355d, 0.38281250558793645d,
        -0.0641365647315979d, 0.8174096941947937d, -0.099681839346885681d, 0.54384875297546387d,
        0.013654858548767468d, 0.66333670901566d, -0.02550790093275257d, 0.52550790093275257d,
        0.75d, 0.75d, 0.75d, 0.75d,
        0.61731656763491016d, 0.92387953251128674d, 0.85355339059327373d, 0.64644660940672627d,
        0.2373046875d, 0.9990234375d, 0.984375d, 0.515625d,
        0.31640625d, 0.99609375d, 0.96875d, 0.53125d,
        0.5625d, 0.9375d, 0.875d, 0.625d,
        0.17577669529663689d, 0.99547020400025188d, 0.98486718749999991d, 0.51512500000000006d,
        0.08838831428314653d, 1.0055242717280188d, 0.98803055279005736d, 0.4921874943692266d,
        0.421875d, 0.984375d, 0.9375d, 0.5625d,
        0.33856217223385232d, 0.96824583655185426d, 0.9330127018922193d, 0.5669872981077807d,
        0.52734375d, 0.97265624441206477d, 0.88281250558793645d, 0.61718749441206355d,
        0.1825903058052063d, 1.0641365647315979d, 1.0996818393468857d, 0.45615124702453613d,
        0.33666329098434d, 0.98634514145123253d, 1.0255079009327526d, 0.47449209906724743d,
    ];
    var index = 0;
    foreach (var elapsed in new[] { 0.25d, 0.75d })
    {
        foreach (var transition in Enum.GetValues<Tween.TransitionType>())
        {
            foreach (var ease in Enum.GetValues<Tween.EaseType>())
            {
                var actual = Tween.InterpolateValue(0d, 1d, elapsed, 1d, transition, ease);
                Require(double.IsFinite(actual) && Math.Abs(actual - expected[index]) <= 0.00001d,
                    $"Curve {transition}/{ease} at {elapsed} differed from pinned easing sample {index}.");
                index++;
            }
        }
    }

    Require(Tween.InterpolateValue(1f, 2f, 0.25d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == 1.5f &&
            DoubleNearlyEqual(Tween.InterpolateValue(1d, 2d, 0.25d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.Out), 1.5d) &&
            Tween.InterpolateValue(false, true, 0.49d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == false &&
            Tween.InterpolateValue(false, true, 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) &&
            Tween.InterpolateValue(true, false, 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) &&
            !Tween.InterpolateValue(true, false, 0.51d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In),
        "Scalar and boolean interpolation must use the declared starting value, delta and half threshold.");
    Require(Tween.InterpolateValue(0, 1, 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == 1 &&
            Tween.InterpolateValue(0, -1, 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == -1 &&
            Tween.InterpolateValue(0L, 1L, 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == 1L &&
            Tween.InterpolateValue(0L, -1L, 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == -1L,
        "Signed integer midpoint interpolation must round away from zero.");
    Require(Tween.InterpolateValue(long.MaxValue - 1, 1L, 0d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == long.MaxValue - 1 &&
            Tween.InterpolateValue(long.MaxValue - 1, 1L, 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == long.MaxValue &&
            Tween.InterpolateValue(long.MaxValue - 1, 1L, 1d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == long.MaxValue &&
            Tween.InterpolateValue(long.MinValue + 1, -1L, 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == long.MinValue,
        "Long interpolation near the upper boundary must retain exact endpoints and midpoint rounding.");

    using (var longTree = new SceneTree(new Entity()))
    using (var longTarget = new TweenValueHolder { LargeInteger = long.MaxValue - 1 })
    {
        var methodValue = 0L;
        longTree.CreateTween().TweenMethod(value => methodValue = value,
            long.MaxValue - 1, long.MaxValue, 1d).SetDelay(0.25d);
        longTree.CreateTween().TweenProperty(longTarget, static value => value.LargeInteger,
            static (value, current) => value.LargeInteger = current, long.MaxValue, 1d);
        longTree.ProcessFrame(0.25d);
        Require(methodValue == long.MaxValue - 1 && longTarget.LargeInteger == long.MaxValue - 1,
            "Long method start and property quarter-step must retain exact near-boundary values.");
        longTree.ProcessFrame(0.5d);
        Require(methodValue == long.MaxValue && longTarget.LargeInteger == long.MaxValue,
            "Method and property tweeners must share exact near-boundary long interpolation.");
    }

    Require(Tween.InterpolateValue(new Vector2(1f, 2f), new Vector2(2f, 4f), 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == new Vector2(2f, 4f) &&
            Tween.InterpolateValue(new Vector2i(1, 2), new Vector2i(1, 3), 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == new Vector2i(2, 4) &&
            Tween.InterpolateValue(new Vector3(1f, 2f, 3f), new Vector3(2f, 4f, 6f), 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == new Vector3(2f, 4f, 6f) &&
            Tween.InterpolateValue(new Vector3i(1, 2, 3), Vector3i.One, 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == new Vector3i(2, 3, 4) &&
            Tween.InterpolateValue(new Vector4(1f, 2f, 3f, 4f), new Vector4(2f, 4f, 6f, 8f), 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == new Vector4(2f, 4f, 6f, 8f) &&
            Tween.InterpolateValue(new Vector4i(1, 2, 3, 4), Vector4i.One, 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == new Vector4i(2, 3, 4, 5),
        "All declared vector families must interpolate by initial value plus delta.");

    Require(Tween.InterpolateValue(new Color(0f, 0.2f, 0.4f, 1f),
                new Color(1f, 0.2f, -0.2f, 0f), 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In)
            .IsEqualApprox(new Color(0.5f, 0.3f, 0.3f, 1f)) &&
            Tween.InterpolateValue(new Rect2(0f, 0f, 2f, 4f), new Rect2(4f, 6f, 2f, 2f), 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == new Rect2(2f, 3f, 3f, 5f) &&
            Tween.InterpolateValue(new Rect2i(0, 0, 2, 4), new Rect2i(3, 5, 3, 5), 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In) == new Rect2i(2, 3, 4, 7) &&
            Tween.InterpolateValue(Transform.Identity, new Transform(0f, new Vector2(4f, 6f)), 0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In).Origin == new Vector2(2f, 3f),
        "Color, rectangle and 2D affine values must follow their typed interpolation and composition contracts.");

    var skewedFinal = new Transform(new Vector2(1f, 2f), new Vector2(3f, 4f), new Vector2(5f, 6f));
    Require(Tween.InterpolateValue(Transform.Identity, skewedFinal, -100d, 0d,
                Tween.TransitionType.Bounce, Tween.EaseType.Out) == skewedFinal &&
            Tween.InterpolateValue(2, 6, 0d, 0d,
                Tween.TransitionType.Bounce, Tween.EaseType.Out) == 8 &&
            DoubleNearlyEqual(Tween.InterpolateValue(10d, 20d, 0.25d, -1d,
                Tween.TransitionType.Linear, Tween.EaseType.Out), 5d) &&
            DoubleNearlyEqual(Tween.InterpolateValue(0d, 10d, -0.5d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In), -5d) &&
            DoubleNearlyEqual(Tween.InterpolateValue(0d, 10d, 2d, 1d,
                Tween.TransitionType.Linear, Tween.EaseType.In), 20d),
        "Zero duration must return the exact final value; signed duration and out-of-range elapsed time extrapolate.");

    Expect<ArgumentOutOfRangeException>(() => Tween.InterpolateValue(0d, 1d, double.NaN, 1d,
            Tween.TransitionType.Linear, Tween.EaseType.In),
        "Manual interpolation must reject non-finite elapsed time.");
    Expect<ArgumentOutOfRangeException>(() => Tween.InterpolateValue(0d, 1d, 0d, double.PositiveInfinity,
            Tween.TransitionType.Linear, Tween.EaseType.In),
        "Manual interpolation must reject non-finite duration.");
    Expect<ArgumentOutOfRangeException>(() => Tween.InterpolateValue(0d, 1d, 0d, 1d,
            (Tween.TransitionType)99, Tween.EaseType.In),
        "Manual interpolation must reject an undefined transition.");
    Expect<ArgumentOutOfRangeException>(() => Tween.InterpolateValue(0d, 1d, 0d, 1d,
            Tween.TransitionType.Linear, (Tween.EaseType)99),
        "Manual interpolation must reject an undefined ease.");
    Expect<NotSupportedException>(() => Tween.InterpolateValue(DateTime.UnixEpoch, DateTime.UnixEpoch, 0d, 1d,
            Tween.TransitionType.Linear, Tween.EaseType.In),
        "A value without built-in addition and interpolation must be rejected.");
    Expect<NotSupportedException>(() => Tween.InterpolateValue("start", "delta", 0d, 0d,
            Tween.TransitionType.Linear, Tween.EaseType.In),
        "Zero duration must not invent an untyped string interpolation contract.");
    Expect<OverflowException>(() => Tween.InterpolateValue(int.MaxValue, 1, 0d, 0d,
            Tween.TransitionType.Linear, Tween.EaseType.In),
        "Typed integer addition must reject a final value outside Int32.");
    Expect<OverflowException>(() => Tween.InterpolateValue(int.MaxValue - 100, 100, 0.75d, 1d,
            Tween.TransitionType.Back, Tween.EaseType.InOut),
        "A curve overshoot outside Int32 must fail at the integer result boundary.");
    Require(double.IsNaN(Tween.InterpolateValue(0d, 1d, 2d, 1d,
            Tween.TransitionType.Circ, Tween.EaseType.In)),
        "An out-of-domain circular extrapolation must retain its non-finite mathematical result.");

    for (var warmup = 0; warmup < 16; warmup++)
        _ = Tween.InterpolateValue(0d, 1d, 0.25d, 1d, Tween.TransitionType.Quad, Tween.EaseType.In);
    var allocationBefore = GC.GetAllocatedBytesForCurrentThread();
    var sum = 0d;
    for (var sample = 0; sample < 128; sample++)
        sum += Tween.InterpolateValue(0d, 1d, 0.25d, 1d, Tween.TransitionType.Quad, Tween.EaseType.In);
    Require(sum > 0d && GC.GetAllocatedBytesForCurrentThread() == allocationBefore,
        "Warmed typed scalar interpolation must not allocate managed memory.");
    for (var warmup = 0; warmup < 16; warmup++)
        _ = Tween.InterpolateValue(long.MaxValue - 1, 1L, 0.5d, 1d,
            Tween.TransitionType.Linear, Tween.EaseType.In);
    allocationBefore = GC.GetAllocatedBytesForCurrentThread();
    var longSum = 0L;
    for (var sample = 0; sample < 128; sample++)
        longSum ^= Tween.InterpolateValue(long.MaxValue - 1, 1L, 0.5d, 1d,
            Tween.TransitionType.Linear, Tween.EaseType.In);
    Require(longSum == 0L && GC.GetAllocatedBytesForCurrentThread() == allocationBefore,
        "Warmed near-boundary long interpolation must not allocate managed memory.");
}

static void VerifyTweenTypeLifetime()
{
    using var tree = new SceneTree(new Entity());
    var callbackCalls = 0;
    var owner = tree.CreateTween();
    var ownedTask = owner.TweenCallback(() => callbackCalls++);
    Expect<InvalidOperationException>(() => Task.Run(owner.Dispose).GetAwaiter().GetResult(),
        "Tween disposal must require its creating scene-tree thread.");
    Require(owner.IsValid() && tree.GetProcessedTweens().Contains(owner),
        "Rejecting off-thread disposal must preserve the live tween and registry entry.");
    owner.Dispose();
    Require(owner.IsDisposed && ownedTask.IsDisposed && !tree.GetProcessedTweens().Contains(owner),
        "Explicit tween disposal must release owned tweeners and unregister immediately.");
    tree.ProcessFrame(0.1d);
    Require(callbackCalls == 0, "Disposed tweens must not invoke queued callbacks.");

    var individualCalls = 0;
    var individualOwner = tree.CreateTween();
    var individualTask = individualOwner.TweenCallback(() => individualCalls++);
    Expect<InvalidOperationException>(() => Task.Run(individualTask.Dispose).GetAwaiter().GetResult(),
        "Attached tweener disposal must require its owner's thread.");
    Require(!individualTask.IsDisposed, "Rejected tweener disposal must leave its state intact.");
    individualTask.Dispose();
    tree.ProcessFrame(0.1d);
    Require(individualCalls == 0 && individualTask.IsDisposed && !individualOwner.IsRunning(),
        "An explicitly disposed tweener must be skipped without invoking its callback.");

    using var source = new TweenEventSource();
    var removalCount = 0;
    var waitOwner = tree.CreateTween();
    var wait = waitOwner.TweenAwait(source, handler => source.Fired += handler,
        handler => { removalCount++; source.Fired -= handler; });
    wait.Dispose();
    Require(removalCount == 1 && wait.IsDisposed,
        "Direct await-tweener disposal must disconnect its owned subscription once.");
    tree.ProcessFrame(0.1d);
    Require(!waitOwner.IsRunning(), "A disposed await tweener must not hold its parent step open.");

    var reentryOwner = tree.CreateTween();
    Exception? disposalInCallback = null;
    reentryOwner.TweenCallback(() => disposalInCallback = Capture(reentryOwner.Dispose));
    tree.ProcessFrame(0.1d);
    Require(disposalInCallback is InvalidOperationException && !reentryOwner.IsDisposed &&
            reentryOwner.IsValid() && !reentryOwner.IsRunning(),
        "Disposal from a tween callback must be rejected before mutation while completion continues.");
}

static void VerifyTweenCallbackIntervals()
{
    using var tree = new SceneTree(new Entity());
    var invalid = tree.CreateTween();
    Expect<ArgumentNullException>(() => invalid.TweenCallback(null!), "A null callback must be rejected before append.");
    Expect<ArgumentOutOfRangeException>(() => invalid.TweenInterval(double.NaN),
        "An interval must reject a non-finite duration before append.");
    Require(!invalid.HasTweeners(), "Rejected callback and interval appends must leave the tween empty.");
    invalid.Kill();

    var events = new List<string>();
    var ordered = tree.CreateTween();
    var delayed = ordered.TweenCallback(() => events.Add("callback"));
    Require(ReferenceEquals(delayed.SetDelay(0.5d), delayed), "SetDelay must return the same callback tweener.");
    Expect<ArgumentOutOfRangeException>(() => delayed.SetDelay(double.PositiveInfinity),
        "A callback delay must reject non-finite values without changing its previous delay.");
    Expect<InvalidOperationException>(() => Task.Run(() => delayed.SetDelay(0.1d)).GetAwaiter().GetResult(),
        "A callback delay mutation must require the tween owner thread.");
    delayed.Finished += sender =>
    {
        Require(ReferenceEquals(sender, delayed), "Tweener.Finished must pass the completing tweener.");
        events.Add("tweener");
    };
    ordered.StepFinished += (_, step) => events.Add($"step:{step}");
    ordered.Finished += _ => events.Add("finished");
    ordered.TweenCallback(() => events.Add("next"));
    tree.ProcessFrame(0d);
    tree.ProcessFrame(0.49d);
    Require(events.Count == 0, "A callback must wait through zero and sub-delay frames.");
    tree.ProcessFrame(0.01d);
    Require(events.SequenceEqual(["callback", "tweener", "step:0"]) && ordered.IsRunning(),
        "A callback must fire at the exact delay boundary, before tweener and step completion events.");
    tree.ProcessFrame(0.01d);
    Require(events.SequenceEqual(["callback", "tweener", "step:0", "next", "step:1", "finished"]),
        "An exact callback boundary must defer the next zero-duration step to later positive time.");

    var signedCallbackEvents = new List<string>();
    var signedCallback = tree.CreateTween();
    signedCallback.TweenCallback(() => signedCallbackEvents.Add("callback")).SetDelay(-0.2d);
    signedCallback.TweenInterval(0.25d).Finished += _ => signedCallbackEvents.Add("interval");
    signedCallback.TweenCallback(() => signedCallbackEvents.Add("after"));
    tree.ProcessFrame(0.1d);
    Require(signedCallbackEvents.SequenceEqual(["callback"]) &&
            DoubleNearlyEqual(signedCallback.GetTotalElapsedTime(), 0.1d),
        "A negative callback delay must fire immediately without forwarding more than the delivered frame delta.");
    tree.ProcessFrame(0.15d);
    Require(signedCallbackEvents.SequenceEqual(["callback", "interval"]),
        "The following interval must consume only actual delivered time and defer the next exact-boundary callback.");
    tree.ProcessFrame(0.1d);
    Require(signedCallbackEvents.SequenceEqual(["callback", "interval", "after"]),
        "The final callback must run on a later positive frame after the interval boundary.");

    var intervalEvents = new List<string>();
    var negativeInterval = tree.CreateTween();
    negativeInterval.TweenInterval(-0.2d).Finished += _ => intervalEvents.Add("interval");
    negativeInterval.TweenCallback(() => intervalEvents.Add("after"));
    tree.ProcessFrame(0.1d);
    Require(intervalEvents.SequenceEqual(["interval", "after"]),
        "A negative interval must finish on the first positive frame while the next task receives no more than that frame delta.");

    var intervalFinishes = 0;
    var exactInterval = tree.CreateTween();
    exactInterval.TweenInterval(0.5d).Finished += _ => intervalFinishes++;
    tree.ProcessFrame(0.25d);
    Require(intervalFinishes == 0, "An interval must remain active below its duration.");
    tree.ProcessFrame(0.25d);
    Require(intervalFinishes == 1 && exactInterval.IsValid() && !exactInterval.IsRunning(),
        "An interval must emit one completion at its exact duration boundary.");

    var zeroIntervalEvents = new List<string>();
    var zeroInterval = tree.CreateTween();
    zeroInterval.TweenInterval(0d).Finished += _ => zeroIntervalEvents.Add("interval");
    zeroInterval.TweenCallback(() => zeroIntervalEvents.Add("callback"));
    tree.ProcessFrame(0d);
    Require(zeroIntervalEvents.Count == 0, "A zero interval must still wait for a positive processing delta.");
    tree.ProcessFrame(0.1d);
    Require(zeroIntervalEvents.SequenceEqual(["interval", "callback"]),
        "A zero interval must pass its full positive frame delta to the next step.");

    var loopCallbacks = 0;
    var loopTweenerFinishes = 0;
    var loopEvents = new List<string>();
    var looped = tree.CreateTween().SetLoops(3);
    var repeating = looped.TweenCallback(() => loopCallbacks++).SetDelay(0.1d);
    repeating.Finished += _ => { loopTweenerFinishes++; loopEvents.Add("tweener"); };
    looped.StepFinished += (sender, step) =>
    {
        Require(ReferenceEquals(sender, looped), "StepFinished must pass the owning tween.");
        loopEvents.Add($"step:{step}");
    };
    looped.LoopFinished += (sender, completed) =>
    {
        Require(ReferenceEquals(sender, looped), "LoopFinished must pass the owning tween.");
        loopEvents.Add($"loop:{completed}");
    };
    looped.Finished += sender =>
    {
        Require(ReferenceEquals(sender, looped), "Finished must pass the owning tween.");
        loopEvents.Add("finished");
    };
    tree.ProcessFrame(0.35d);
    Require(loopCallbacks == 3 && loopTweenerFinishes == 3 && !looped.IsRunning() &&
            loopEvents.SequenceEqual(["tweener", "step:0", "loop:1", "tweener", "step:0", "loop:2",
                "tweener", "step:0", "finished"]),
        "Tweener, step, loop and final events must fire in order with one-based loop counts on each execution.");

    var liveDelayCalls = 0;
    var liveDelay = tree.CreateTween();
    var adjustable = liveDelay.TweenCallback(() => liveDelayCalls++).SetDelay(1d);
    tree.ProcessFrame(0.25d);
    Expect<ArgumentOutOfRangeException>(() => adjustable.SetDelay(double.NaN),
        "Rejecting a live non-finite delay must preserve the previous threshold.");
    adjustable.SetDelay(0.5d);
    tree.ProcessFrame(0.25d);
    Require(liveDelayCalls == 1, "Changing a callback delay during playback must affect its active threshold.");

    var directTarget = new TweenEventSource();
    var unavailableFinishes = 0;
    var unavailable = tree.CreateTween();
    unavailable.TweenCallback(directTarget.Emit).Finished += _ => unavailableFinishes++;
    directTarget.Dispose();
    tree.ProcessFrame(0.1d);
    Require(directTarget.EmitCount == 0 && unavailableFinishes == 1 && !unavailable.IsRunning(),
        "A disposed direct callback target must finish its tweener without invoking the target.");

    var canceledCalls = 0;
    var canceledFinishes = 0;
    var canceled = tree.CreateTween();
    canceled.TweenCallback(() => canceledCalls++).Finished += _ => canceledFinishes++;
    canceled.Kill();
    tree.ProcessFrame(0.1d);
    Require(canceledCalls == 0 && canceledFinishes == 0 && !canceled.IsValid(),
        "Killing an unstarted callback must neither call it nor emit tweener completion.");

    var failedFinishes = 0;
    var siblingRan = false;
    var failing = tree.CreateTween();
    failing.TweenCallback(() => throw new InvalidOperationException("expected callback failure"))
        .Finished += _ => failedFinishes++;
    tree.CreateTween().TweenCallback(() => siblingRan = true);
    Require(Capture(() => tree.ProcessFrame(0.1d)) is AggregateException &&
            failedFinishes == 0 && siblingRan && !failing.IsValid(),
        "A throwing callback must not emit tweener completion or prevent a later tween from running.");
}

static void VerifyTweenMethods()
{
    using var tree = new SceneTree(new Entity());
    var rejected = tree.CreateTween();
    Expect<ArgumentNullException>(() => rejected.TweenMethod<double>(null!, 0d, 1d, 1d),
        "A null method callback must be rejected before append.");
    Expect<ArgumentOutOfRangeException>(() => rejected.TweenMethod(static (double _) => { }, 0d, 1d, double.NaN),
        "A non-finite method duration must be rejected before append.");
    Expect<NotSupportedException>(() => rejected.TweenMethod(static (DateTime _) => { },
            DateTime.UnixEpoch, DateTime.UnixEpoch.AddDays(1), 1d),
        "An unsupported typed value must require an explicit interpolator.");
    Require(!rejected.HasTweeners(), "Rejected method appends must leave the tween empty.");
    rejected.Kill();

    var samples = new List<double>();
    var basic = tree.CreateTween();
    var basicTweener = basic.TweenMethod(samples.Add, 0d, 10d, 1d);
    var basicFinishes = 0;
    basicTweener.Finished += _ => basicFinishes++;
    tree.ProcessFrame(0d);
    Require(samples.Count == 0, "A zero-delta frame must not call a method tweener.");
    tree.ProcessFrame(0.25d);
    Require(samples.Count == 1 && DoubleNearlyEqual(samples[0], 2.5d),
        "A method tweener must deliver the built-in interpolated value on an active frame.");
    tree.ProcessFrame(0.75d);
    Require(samples.Count == 2 && samples[1] == 10d && basicFinishes == 1 && !basic.IsRunning(),
        "The exact final method value must be delivered once before tweener completion.");

    var delayedSamples = new List<double>();
    var delayed = tree.CreateTween();
    var delayedMethod = delayed.TweenMethod(delayedSamples.Add, 0d, 1d, 1d);
    Require(ReferenceEquals(delayedMethod.SetDelay(0.5d), delayedMethod),
        "Method SetDelay must return its own tweener.");
    Expect<ArgumentOutOfRangeException>(() => delayedMethod.SetDelay(double.PositiveInfinity),
        "A non-finite method delay must preserve the previous threshold.");
    Expect<InvalidOperationException>(() => Task.Run(() => delayedMethod.SetDelay(0.1d))
            .GetAwaiter().GetResult(),
        "Method delay mutation must require the tween owner thread.");
    tree.ProcessFrame(0.49d);
    Require(delayedSamples.Count == 0, "A method callback must not run before its delay.");
    tree.ProcessFrame(0.01d);
    Require(delayedSamples.SequenceEqual([0d]),
        "A method callback must receive its starting value at the exact delay boundary.");
    tree.ProcessFrame(0.5d);
    Require(delayedSamples.Count == 2 && DoubleNearlyEqual(delayedSamples[^1], 0.5d),
        "The method's duration must start after its delay.");
    delayed.Kill();

    var signedValue = double.NaN;
    var signedNext = false;
    var signed = tree.CreateTween();
    signed.TweenMethod(value => signedValue = value, 0d, 10d, -1d).SetDelay(-0.2d);
    signed.TweenCallback(() => signedNext = true);
    tree.ProcessFrame(0.1d);
    Require(signedValue == 10d && signedNext && DoubleNearlyEqual(signed.GetTotalElapsedTime(), 0.1d),
        "Negative method duration and delay must deliver the final value on the first positive step.");

    var zeroValue = double.NaN;
    var zero = tree.CreateTween();
    zero.TweenMethod(value => zeroValue = value, 1d, 2d, 0d);
    tree.ProcessFrame(0d);
    Require(double.IsNaN(zeroValue), "A zero-duration method still needs positive frame time.");
    tree.ProcessFrame(0.1d);
    Require(zeroValue == 2d, "A zero-duration method must deliver its final value on the first positive frame.");

    var curveValue = 0d;
    var curves = tree.CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
    var curveMethod = curves.TweenMethod(value => curveValue = value, 0d, 1d, 1d);
    Require(ReferenceEquals(curveMethod.SetTrans(Tween.TransitionType.Cubic), curveMethod) &&
            ReferenceEquals(curveMethod.SetEase(Tween.EaseType.Out), curveMethod),
        "Per-method curve overrides must return the same tweener.");
    Expect<ArgumentOutOfRangeException>(() => curveMethod.SetTrans((Tween.TransitionType)99),
        "An undefined method transition must not replace the current curve.");
    Expect<ArgumentOutOfRangeException>(() => curveMethod.SetEase((Tween.EaseType)99),
        "An undefined method ease must not replace the current direction.");
    Expect<InvalidOperationException>(() => Task.Run(() => curveMethod.SetEase(Tween.EaseType.In))
            .GetAwaiter().GetResult(),
        "Method curve mutation must require the tween owner thread.");
    Expect<InvalidOperationException>(() => Task.Run(() => curveMethod.SetTrans(Tween.TransitionType.Linear))
            .GetAwaiter().GetResult(),
        "Method transition mutation must require the tween owner thread.");
    tree.ProcessFrame(0.25d);
    Require(DoubleNearlyEqual(curveValue, 0.578125d),
        "Cubic/Out must override the owning tween's Quad/In default before interpolation.");
    curveMethod.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    tree.ProcessFrame(0.25d);
    Require(DoubleNearlyEqual(curveValue, 0.5d),
        "Changing a method curve while running must affect the next interpolation sample.");
    tree.ProcessFrame(0.5d);
    Require(curveValue == 1d, "Curve overrides must still deliver the exact final value.");

    var liveValue = double.NaN;
    var live = tree.CreateTween();
    var liveMethod = live.TweenMethod(value => liveValue = value, 0d, 1d, 1d).SetDelay(1d);
    tree.ProcessFrame(0.25d);
    Require(double.IsNaN(liveValue), "A delayed method must remain silent before its threshold.");
    Expect<ArgumentOutOfRangeException>(() => liveMethod.SetDelay(double.NaN),
        "Rejecting a live non-finite method delay must preserve the previous threshold.");
    liveMethod.SetDelay(0.25d);
    tree.ProcessFrame(0.25d);
    Require(DoubleNearlyEqual(liveValue, 0.25d),
        "A live delay change must use accumulated time and the new threshold.");
    live.Kill();

    var firstDate = DateTime.UnixEpoch;
    var lastDate = firstDate.AddDays(1);
    var receivedDate = DateTime.MinValue;
    var custom = tree.CreateTween();
    custom.TweenMethod(value => receivedDate = value, firstDate, lastDate, 1d,
        (from, to, weight) => weight < 0.5d ? from : to);
    tree.ProcessFrame(0.25d);
    Require(receivedDate == firstDate, "An explicit typed interpolator must support a non-built-in value type.");
    tree.ProcessFrame(0.25d);
    Require(receivedDate == lastDate, "The explicit interpolator must receive the current eased weight.");
    custom.Kill();

    using var directTarget = new TweenValueHolder();
    var unavailableFinishes = 0;
    var unavailable = tree.CreateTween();
    unavailable.TweenMethod(directTarget.SetValue, 0d, 1d, 1d).Finished += _ => unavailableFinishes++;
    directTarget.Dispose();
    tree.ProcessFrame(0.1d);
    Require(directTarget.Value == 0d && unavailableFinishes == 1 && !unavailable.IsRunning(),
        "A disposed direct method target must finish without invoking its callback.");

    var loopCalls = 0;
    var loopFinishes = 0;
    var looped = tree.CreateTween().SetLoops(2);
    looped.TweenMethod(_ => loopCalls++, 0d, 1d, 0.1d).Finished += _ => loopFinishes++;
    tree.ProcessFrame(0.25d);
    Require(loopCalls == 2 && loopFinishes == 2 && !looped.IsRunning(),
        "A method tweener must reset and finish once per loop with its final value.");

    var failedFinishes = 0;
    var siblingRan = false;
    var failing = tree.CreateTween();
    failing.TweenMethod<double>(_ => throw new InvalidOperationException("expected method failure"), 0d, 1d, 1d)
        .Finished += _ => failedFinishes++;
    tree.CreateTween().TweenCallback(() => siblingRan = true);
    Require(Capture(() => tree.ProcessFrame(0.1d)) is AggregateException &&
            failedFinishes == 0 && siblingRan && !failing.IsValid(),
        "A throwing method callback must not finish its tweener or suppress later SceneTree work.");

    var interpolatorFinished = false;
    var interpolatorSiblingRan = false;
    var invalidInterpolator = tree.CreateTween();
    invalidInterpolator.TweenMethod<DateTime>(static _ => { }, firstDate, lastDate, 1d,
            static (_, _, _) => throw new InvalidOperationException("expected interpolator failure"))
        .Finished += _ => interpolatorFinished = true;
    tree.CreateTween().TweenCallback(() => interpolatorSiblingRan = true);
    Require(Capture(() => tree.ProcessFrame(0.1d)) is AggregateException && !interpolatorFinished &&
            interpolatorSiblingRan && !invalidInterpolator.IsValid(),
        "A throwing typed interpolator must invalidate its tween without emitting completion or suppressing later work.");
}

static void VerifyTweenProperties()
{
    static double Read(TweenValueHolder value) => value.Value;
    static void Write(TweenValueHolder target, double value) => target.Value = value;

    using var tree = new SceneTree(new Entity());
    using var holder = new TweenValueHolder();
    var rejected = tree.CreateTween();
    Expect<ArgumentNullException>(() => rejected.TweenProperty<TweenValueHolder, double>(null!, Read, Write, 1d, 1d),
        "A null property target must be rejected before append.");
    Expect<ArgumentNullException>(() => rejected.TweenProperty<TweenValueHolder, double>(holder, null!, Write, 1d, 1d),
        "A null property getter must be rejected before append.");
    Expect<ArgumentNullException>(() => rejected.TweenProperty<TweenValueHolder, double>(holder, Read, null!, 1d, 1d),
        "A null property setter must be rejected before append.");
    Expect<ArgumentOutOfRangeException>(() => rejected.TweenProperty(holder, Read, Write, 1d, double.NaN),
        "A non-finite property duration must be rejected before append.");
    Expect<NotSupportedException>(() => rejected.TweenProperty<TweenValueHolder, DateTime>(holder,
            static _ => DateTime.UnixEpoch, static (_, _) => { }, DateTime.UnixEpoch.AddDays(1), 1d),
        "An unsupported property value must require an explicit typed interpolator.");
    Expect<InvalidOperationException>(() => rejected.TweenProperty<TweenValueHolder, double>(holder,
            static _ => throw new InvalidOperationException("expected append getter failure"), Write, 1d, 1d),
        "A getter failure during append must propagate before the tweener is stored.");
    Require(!rejected.HasTweeners(), "Rejected property appends must leave the sequence empty.");
    rejected.Kill();

    holder.Value = 2d;
    var getterReads = 0;
    var basic = tree.CreateTween();
    basic.TweenProperty(holder, value => { getterReads++; return value.Value; }, Write, 10d, 1d);
    Require(getterReads == 1, "The typed getter must capture the append-time value.");
    holder.Value = 4d;
    tree.ProcessFrame(0.5d);
    Require(getterReads == 2 && DoubleNearlyEqual(holder.Value, 7d),
        "Default property interpolation must recapture the value when its step starts.");
    tree.ProcessFrame(0.5d);
    Require(holder.Value == 10d && !basic.IsRunning(),
        "A property tweener must write the exact final value at completion.");

    holder.Value = 2d;
    var chainedFrom = tree.CreateTween();
    var configuredFrom = chainedFrom.TweenProperty(holder, Read, Write, 10d, 1d);
    Require(ReferenceEquals(configuredFrom.From(4d).FromCurrent(), configuredFrom),
        "From and FromCurrent must return the same configured tweener.");
    holder.Value = 8d;
    tree.ProcessFrame(0.5d);
    Require(DoubleNearlyEqual(holder.Value, 7d),
        "FromCurrent must retain an explicit From value already stored by the tweener.");
    chainedFrom.Kill();

    holder.Value = 2d;
    var delayed = tree.CreateTween();
    var delayedProperty = delayed.TweenProperty(holder, Read, Write, 10d, 1d).SetDelay(0.5d);
    Require(ReferenceEquals(delayedProperty, delayedProperty.SetDelay(0.5d)),
        "Property SetDelay must return its own tweener.");
    Expect<ArgumentOutOfRangeException>(() => delayedProperty.SetDelay(double.PositiveInfinity),
        "A non-finite property delay must preserve the previous threshold.");
    tree.ProcessFrame(0.25d);
    holder.Value = 6d;
    tree.ProcessFrame(0.25d);
    Require(holder.Value == 6d, "A delayed default property must capture the current value at the exact delay boundary.");
    tree.ProcessFrame(0.5d);
    Require(DoubleNearlyEqual(holder.Value, 8d), "A delayed property must interpolate from its delay-end capture.");
    delayed.Kill();

    holder.Value = 2d;
    var changedDelay = tree.CreateTween();
    var changedDelayProperty = changedDelay.TweenProperty(holder, Read, Write, 10d, 1d).SetDelay(1d);
    tree.ProcessFrame(0.25d);
    holder.Value = 7d;
    Expect<InvalidOperationException>(() => Task.Run(() => changedDelayProperty.SetDelay(0d))
            .GetAwaiter().GetResult(),
        "Property delay mutation must require the tween owner thread.");
    changedDelayProperty.SetDelay(0d);
    tree.ProcessFrame(0.25d);
    Require(DoubleNearlyEqual(holder.Value, 6d),
        "Changing an active delay to zero must keep its append-time start instead of recapturing the property.");
    changedDelay.Kill();

    holder.Value = 2d;
    var changedDelayFrom = tree.CreateTween();
    var changedDelayFromProperty = changedDelayFrom.TweenProperty(holder, Read, Write, 10d, 1d).SetDelay(1d);
    tree.ProcessFrame(0.25d);
    changedDelayFromProperty.SetDelay(0d).From(4d);
    tree.ProcessFrame(0.25d);
    Require(DoubleNearlyEqual(holder.Value, 8d),
        "A live From after a delay changes to zero must preserve the original active displacement.");
    changedDelayFrom.Kill();

    holder.Value = 2d;
    var nearZero = tree.CreateTween();
    nearZero.TweenProperty(holder, Read, Write, 10d, 1d).SetDelay(0.000005d);
    tree.ProcessFrame(0d);
    holder.Value = 6d;
    tree.ProcessFrame(0.5d);
    Require(Math.Abs(holder.Value - 5.99996d) < 0.000001d,
        "A delay below the pinned zero-approximation threshold must capture at step start.");
    nearZero.Kill();

    holder.Value = 2d;
    var atThreshold = tree.CreateTween();
    atThreshold.TweenProperty(holder, Read, Write, 10d, 1d).SetDelay(0.00001d);
    tree.ProcessFrame(0d);
    holder.Value = 6d;
    tree.ProcessFrame(0.5d);
    Require(Math.Abs(holder.Value - 7.99996d) < 0.000001d,
        "A delay at the pinned threshold must capture when the delay expires.");
    atThreshold.Kill();

    holder.Value = 2d;
    var negativeDelay = tree.CreateTween();
    negativeDelay.TweenProperty(holder, Read, Write, 10d, 1d).SetDelay(-0.2d);
    tree.ProcessFrame(0d);
    holder.Value = 6d;
    tree.ProcessFrame(0.1d);
    Require(DoubleNearlyEqual(holder.Value, 7.2d),
        "A negative property delay must capture at its first positive step and use signed elapsed time.");
    negativeDelay.Kill();

    holder.Value = 2d;
    var relative = tree.CreateTween();
    relative.TweenProperty(holder, Read, Write, 3d, 1d).AsRelative();
    holder.Value = 8d;
    tree.ProcessFrame(0.5d);
    Require(DoubleNearlyEqual(holder.Value, 9.5d),
        "An undelayed relative property must add its delta to the step-start value.");
    relative.Kill();

    holder.Value = 2d;
    var delayedRelative = tree.CreateTween();
    delayedRelative.TweenProperty(holder, Read, Write, 3d, 1d).AsRelative().SetDelay(0.5d);
    tree.ProcessFrame(0.25d);
    holder.Value = 8d;
    tree.ProcessFrame(0.25d);
    Require(holder.Value == 8d, "A delayed relative property must recapture its initial value at delay end.");
    tree.ProcessFrame(0.5d);
    Require(DoubleNearlyEqual(holder.Value, 6.5d),
        "A delayed relative property must retain the final value resolved from its append-time base.");
    tree.ProcessFrame(0.5d);
    Require(holder.Value == 5d, "A delayed relative property must write its fixed relative final value.");

    holder.Value = 1d;
    var explicitLoops = tree.CreateTween().SetLoops(2);
    var explicitFinishes = 0;
    explicitLoops.TweenProperty(holder, Read, Write, 3d, 0.1d).From(4d).AsRelative()
        .Finished += _ => explicitFinishes++;
    tree.ProcessFrame(0.25d);
    Require(holder.Value == 7d && explicitFinishes == 2,
        "An explicit relative From start must be reused for each loop execution.");

    holder.Value = 0d;
    var liveRelative = tree.CreateTween().SetLoops(2);
    var liveRelativeProperty = liveRelative.TweenProperty(holder, Read, Write, 3d, 0.1d);
    tree.ProcessFrame(0.05d);
    liveRelativeProperty.AsRelative();
    tree.ProcessFrame(0.15d);
    Require(holder.Value == 6d,
        "Enabling relative mode during a step must preserve that step's final value and affect the next loop.");

    holder.Value = 0d;
    var liveFrom = tree.CreateTween();
    var liveFromProperty = liveFrom.TweenProperty(holder, Read, Write, 10d, 1d);
    tree.ProcessFrame(0.25d);
    liveFromProperty.From(4d);
    tree.ProcessFrame(0.25d);
    Require(DoubleNearlyEqual(holder.Value, 9d),
        "A live From change must shift intermediate interpolation while retaining the active displacement.");
    tree.ProcessFrame(0.5d);
    Require(holder.Value == 10d,
        "The final property write must still use the originally configured final value after live From.");

    holder.Value = 0d;
    var liveCustomFrom = tree.CreateTween();
    var liveCustomProperty = liveCustomFrom.TweenProperty(holder, Read, Write, 10d, 1d)
        .SetCustomInterpolator(static weight => weight);
    tree.ProcessFrame(0.25d);
    liveCustomProperty.From(4d);
    tree.ProcessFrame(0.25d);
    Require(DoubleNearlyEqual(holder.Value, 7d),
        "A custom-weight property must interpolate toward its fixed final value after a live From change.");
    liveCustomFrom.Kill();

    var unsupportedRelative = tree.CreateTween();
    var unsupportedRelativeProperty = unsupportedRelative.TweenProperty<TweenValueHolder, DateTime>(holder,
        static _ => DateTime.UnixEpoch, static (_, _) => { }, DateTime.UnixEpoch.AddDays(1), 1d,
        static (from, to, weight) => weight < 0.5d ? from : to);
    Expect<NotSupportedException>(() => unsupportedRelativeProperty.AsRelative(),
        "Relative mode must require a built-in typed addition contract even with custom interpolation.");
    unsupportedRelative.Kill();

    holder.Value = 0d;
    var custom = tree.CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
    var customProperty = custom.TweenProperty(holder, Read, Write, 10d, 1d);
    Require(ReferenceEquals(customProperty.SetCustomInterpolator(static weight => weight * 2d), customProperty),
        "A custom interpolator setter must return its own property tweener.");
    Expect<ArgumentNullException>(() => customProperty.SetCustomInterpolator(null!),
        "A null custom interpolator must leave the previous mapping intact.");
    tree.ProcessFrame(0.5d);
    Require(DoubleNearlyEqual(holder.Value, 5d),
        "A custom interpolator must receive the already-eased weight before the setter.");
    tree.ProcessFrame(0.5d);
    Require(DoubleNearlyEqual(holder.Value, 20d),
        "A custom interpolator must also map the final weight and allow overshoot.");

    holder.Value = 0d;
    var curves = tree.CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
    var curveProperty = curves.TweenProperty(holder, Read, Write, 10d, 1d);
    Require(ReferenceEquals(curveProperty.SetTrans(Tween.TransitionType.Cubic), curveProperty) &&
            ReferenceEquals(curveProperty.SetEase(Tween.EaseType.Out), curveProperty),
        "Per-property transition and ease overrides must return their tweener.");
    Expect<ArgumentOutOfRangeException>(() => curveProperty.SetTrans((Tween.TransitionType)99),
        "An undefined property transition must preserve the current curve.");
    Expect<ArgumentOutOfRangeException>(() => curveProperty.SetEase((Tween.EaseType)99),
        "An undefined property ease must preserve the current direction.");
    Expect<InvalidOperationException>(() => Task.Run(() => curveProperty.SetTrans(Tween.TransitionType.Linear))
            .GetAwaiter().GetResult(),
        "Property transition mutation must require the owner thread.");
    Expect<InvalidOperationException>(() => Task.Run(() => curveProperty.SetEase(Tween.EaseType.In))
            .GetAwaiter().GetResult(),
        "Property ease mutation must require the owner thread.");
    tree.ProcessFrame(0.25d);
    Require(DoubleNearlyEqual(holder.Value, 5.78125d),
        "Cubic/Out must override the parent Quad/In curve at a property interpolation sample.");
    curveProperty.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    tree.ProcessFrame(0.25d);
    Require(DoubleNearlyEqual(holder.Value, 5d),
        "Live property curve changes must affect the next interpolation sample.");
    tree.ProcessFrame(0.5d);
    Require(holder.Value == 10d, "Property curve overrides must still write the exact final value.");

    holder.Value = 1d;
    var signed = tree.CreateTween();
    var signedFinished = 0;
    var signedNext = false;
    signed.TweenProperty(holder, Read, Write, 10d, -1d).SetDelay(-0.2d)
        .Finished += _ => signedFinished++;
    signed.TweenCallback(() => signedNext = true);
    tree.ProcessFrame(0.1d);
    Require(holder.Value == 10d && signedFinished == 1 && signedNext,
        "Negative property duration and delay must deliver the final value on the first positive step.");

    holder.Value = 1d;
    var zero = tree.CreateTween();
    zero.TweenProperty(holder, Read, Write, 2d, 0d);
    tree.ProcessFrame(0d);
    Require(holder.Value == 1d, "A zero-duration property still needs positive processing time.");
    tree.ProcessFrame(0.1d);
    Require(holder.Value == 2d, "A zero-duration property must write its final value on the first positive frame.");

    holder.Flag = false;
    var relativeFlag = tree.CreateTween();
    relativeFlag.TweenProperty(holder, static value => value.Flag,
        static (value, current) => value.Flag = current, true, 1d).AsRelative();
    tree.ProcessFrame(0.5d);
    Require(holder.Flag, "A relative boolean property must use the typed replacement addition contract.");
    relativeFlag.Kill();

    holder.Integer = int.MinValue;
    var fullSpan = tree.CreateTween();
    fullSpan.TweenProperty(holder, static value => value.Integer,
        static (value, current) => value.Integer = current, int.MaxValue, 1d);
    tree.ProcessFrame(0.5d);
    Require(holder.Integer == -1,
        "A full-span integer property must keep the double-based interpolation path when its delta exceeds Int32.");
    tree.ProcessFrame(0.5d);
    Require(holder.Integer == int.MaxValue, "A full-span integer property must write its exact final value.");

    holder.Integer = int.MinValue;
    var fullSpanFrom = tree.CreateTween();
    var fullSpanFromProperty = fullSpanFrom.TweenProperty(holder, static value => value.Integer,
        static (value, current) => value.Integer = current, int.MaxValue, 1d);
    tree.ProcessFrame(0.25d);
    fullSpanFromProperty.From(0);
    tree.ProcessFrame(0.25d);
    Require(holder.Integer == 1_073_741_824,
        "A live From with an unrepresentable Int32 displacement must retain typed endpoint interpolation.");
    tree.ProcessFrame(0.5d);
    Require(holder.Integer == int.MaxValue,
        "A full-span live From must still write the configured final integer value.");

    using var spatial = new Entity();
    var vectorTween = tree.CreateTween();
    var vectorProperty = vectorTween.TweenProperty(spatial, static node => node.Position,
        static (node, value) => node.Position = value, new Vector2(10f, 20f), 1d);
    tree.ProcessFrame(0.25d);
    vectorProperty.From(new Vector2(4f, 8f));
    tree.ProcessFrame(0.25d);
    Require(spatial.Position == new Vector2(9f, 18f),
        "Live From must preserve the active vector displacement for intermediate samples.");
    tree.ProcessFrame(0.5d);
    Require(spatial.Position == new Vector2(10f, 20f),
        "Live From must still write the fixed vector final value.");

    spatial.Transform = Transform.Identity;
    var transformTween = tree.CreateTween();
    var transformProperty = transformTween.TweenProperty(spatial, static node => node.Transform,
        static (node, value) => node.Transform = value,
        new Transform(0f, new Vector2(10f, 0f)), 1d);
    tree.ProcessFrame(0.25d);
    transformProperty.From(new Transform(0f, new Vector2(4f, 0f)));
    tree.ProcessFrame(0.25d);
    Require(spatial.Transform.Origin == new Vector2(9f, 0f),
        "Live From must compose the active affine displacement for intermediate transform samples.");
    tree.ProcessFrame(0.5d);
    Require(spatial.Transform.Origin == new Vector2(10f, 0f),
        "The transform property must still write its configured final transform.");

    spatial.Transform = new Transform(0f, new Vector2(2f, 0f));
    var relativeTransform = tree.CreateTween();
    relativeTransform.TweenProperty(spatial, static node => node.Transform,
        static (node, value) => node.Transform = value,
        new Transform(0f, new Vector2(3f, 0f)), 1d).AsRelative();
    tree.ProcessFrame(0.5d);
    Require(spatial.Transform.Origin == new Vector2(3.5f, 0f),
        "A relative transform property must compose its step-start transform with the configured delta.");
    tree.ProcessFrame(0.5d);
    Require(spatial.Transform.Origin == new Vector2(5f, 0f),
        "A relative transform property must write its composed final value.");

    var firstDate = DateTime.UnixEpoch;
    var lastDate = firstDate.AddDays(1);
    var dateValue = DateTime.MinValue;
    var customTyped = tree.CreateTween();
    customTyped.TweenProperty<TweenValueHolder, DateTime>(holder, _ => firstDate,
        (_, value) => dateValue = value, lastDate, 1d,
        (from, to, weight) => weight < 0.5d ? from : to)
        .SetCustomInterpolator(static weight => weight);
    tree.ProcessFrame(0.25d);
    Require(dateValue == firstDate,
        "A custom typed property interpolator must support a value without a built-in interpolation contract.");
    tree.ProcessFrame(0.25d);
    Require(dateValue == lastDate,
        "The custom typed property interpolator must receive the current eased weight.");
    customTyped.Kill();

    holder.Value = 0d;
    var inheritedCurve = tree.CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
    inheritedCurve.TweenProperty(holder, Read, Write, 10d, 1d);
    inheritedCurve.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
    tree.ProcessFrame(0.5d);
    Require(DoubleNearlyEqual(holder.Value, 2.5d),
        "A property must retain the parent's transition and ease captured when it was appended.");
    inheritedCurve.Kill();

    var startGetterReads = 0;
    var getterSiblingRan = false;
    var startGetterFailure = tree.CreateTween();
    startGetterFailure.TweenProperty(holder, value =>
        {
            if (++startGetterReads == 2)
                throw new InvalidOperationException("expected start getter failure");
            return value.Value;
        }, Write, 10d, 1d);
    tree.CreateTween().TweenCallback(() => getterSiblingRan = true);
    Require(Capture(() => tree.ProcessFrame(0.1d)) is AggregateException && startGetterReads == 2 &&
            getterSiblingRan && !startGetterFailure.IsValid(),
        "A getter failure at step start must invalidate only its tween and preserve later work.");

    var setterFinished = false;
    var setterSiblingRan = false;
    var setterFailure = tree.CreateTween();
    setterFailure.TweenProperty(holder, Read,
            static (_, _) => throw new InvalidOperationException("expected setter failure"), 10d, 1d)
        .Finished += _ => setterFinished = true;
    tree.CreateTween().TweenCallback(() => setterSiblingRan = true);
    Require(Capture(() => tree.ProcessFrame(0.1d)) is AggregateException && !setterFinished &&
            setterSiblingRan && !setterFailure.IsValid(),
        "A property setter failure must omit tweener completion and preserve later work.");

    var customFailureFinished = false;
    var customSiblingRan = false;
    var customFailure = tree.CreateTween();
    customFailure.TweenProperty(holder, Read, Write, 10d, 1d)
        .SetCustomInterpolator(static _ => throw new InvalidOperationException("expected custom curve failure"))
        .Finished += _ => customFailureFinished = true;
    tree.CreateTween().TweenCallback(() => customSiblingRan = true);
    Require(Capture(() => tree.ProcessFrame(0.1d)) is AggregateException && !customFailureFinished &&
            customSiblingRan && !customFailure.IsValid(),
        "A custom property interpolator failure must invalidate its tween without suppressing later work.");

    using var allocationTree = new SceneTree(new Entity());
    using var allocationTarget = new TweenValueHolder();
    allocationTree.CreateTween().TweenProperty(allocationTarget, Read, Write, 1d, 1_000d);
    for (var index = 0; index < 16; index++)
        allocationTree.ProcessFrame(0.001d);
    var before = GC.GetAllocatedBytesForCurrentThread();
    for (var index = 0; index < 128; index++)
        allocationTree.ProcessFrame(0.001d);
    Require(GC.GetAllocatedBytesForCurrentThread() == before,
        "A warmed active property-tween frame must not allocate managed memory.");
}

static void VerifyTweenSubtweens()
{
    using var tree = new SceneTree(new Entity());
    var rejected = tree.CreateTween();
    Expect<ArgumentNullException>(() => rejected.TweenSubtween(null!),
        "A null child tween must be rejected before append.");
    Expect<ArgumentException>(() => rejected.TweenSubtween(rejected),
        "A tween cannot contain itself.");
    Require(!rejected.HasTweeners() && tree.GetProcessedTweens().Contains(rejected),
        "Rejected subtween appends must preserve the parent and its registry entry.");
    rejected.Kill();

    var offThreadParent = tree.CreateTween();
    var offThreadChild = tree.CreateTween();
    offThreadChild.TweenInterval(1d);
    Expect<InvalidOperationException>(() => Task.Run(() => offThreadParent.TweenSubtween(offThreadChild))
            .GetAwaiter().GetResult(),
        "Subtween ownership transfer must require the parent owner thread.");
    Require(tree.GetProcessedTweens().Contains(offThreadChild),
        "Rejected off-thread transfer must retain the child's original registration.");
    offThreadParent.Kill();
    offThreadChild.Kill();

    var events = new List<string>();
    var child = tree.CreateTween();
    child.TweenInterval(0.2d);
    child.TweenCallback(() => events.Add("child"));
    var parent = tree.CreateTween();
    parent.TweenCallback(() => events.Add("before"));
    var nested = parent.TweenSubtween(child);
    var nestedFinishes = 0;
    nested.Finished += _ => nestedFinishes++;
    parent.TweenCallback(() => events.Add("after"));
    Require(!tree.GetProcessedTweens().Contains(child) && tree.GetProcessedTweens().Contains(parent),
        "Appending a child must transfer it from the tree registry to its parent.");
    tree.ProcessFrame(0.3d);
    Require(events.SequenceEqual(["before", "child"]) && nestedFinishes == 0,
        "A child's finishing frame must not yet release the parent's subtween step.");
    tree.ProcessFrame(0.1d);
    Require(events.SequenceEqual(["before", "child", "after"]) && nestedFinishes == 1 &&
            parent.IsValid() && !parent.IsRunning(),
        "The next frame must finish the nested step and forward unused time to the parent's following callback.");
    tree.ProcessFrame(0.1d);
    Require(!parent.IsValid() && !child.IsValid(),
        "The completed parent's registry sweep must terminate its nested child.");

    var loopedChildCalls = 0;
    var loopedSubtweenFinishes = 0;
    var loopedChild = tree.CreateTween();
    loopedChild.TweenCallback(() => loopedChildCalls++).SetDelay(0.1d);
    var loopedParent = tree.CreateTween().SetLoops(2);
    loopedParent.TweenSubtween(loopedChild).Finished += _ => loopedSubtweenFinishes++;
    tree.ProcessFrame(0.1d);
    tree.ProcessFrame(0.1d);
    tree.ProcessFrame(0.1d);
    Require(loopedChildCalls == 2 && loopedSubtweenFinishes == 2 && !loopedParent.IsRunning(),
        "Each parent loop must stop, restart and complete its child once.");

    var delayedValue = 0d;
    var delayedChild = tree.CreateTween();
    delayedChild.TweenMethod(value => delayedValue = value, 0d, 1d, 1d);
    var delayedParent = tree.CreateTween();
    var delayedStep = delayedParent.TweenSubtween(delayedChild);
    Require(ReferenceEquals(delayedStep.SetDelay(0.5d), delayedStep),
        "Subtween SetDelay must return its own tweener.");
    Expect<ArgumentOutOfRangeException>(() => delayedStep.SetDelay(double.NaN),
        "A non-finite subtween delay must preserve the previous threshold.");
    Expect<InvalidOperationException>(() => Task.Run(() => delayedStep.SetDelay(0d)).GetAwaiter().GetResult(),
        "Subtween delay mutation must require the owner thread.");
    tree.ProcessFrame(0.25d);
    Require(delayedValue == 0d && delayedChild.GetTotalElapsedTime() == 0d,
        "A parent delay must leave its child untouched before the threshold.");
    tree.ProcessFrame(0.25d);
    Require(DoubleNearlyEqual(delayedValue, 0.25d) &&
            DoubleNearlyEqual(delayedChild.GetTotalElapsedTime(), 0.25d),
        "At the parent delay boundary, the child must receive the full delivered frame delta.");
    delayedParent.Kill();

    var killedDuring = tree.CreateTween();
    killedDuring.TweenInterval(1d);
    var killedDuringParent = tree.CreateTween();
    var afterKill = false;
    killedDuringParent.TweenSubtween(killedDuring);
    killedDuringParent.TweenCallback(() => afterKill = true);
    tree.ProcessFrame(0.25d);
    killedDuring.Kill();
    tree.ProcessFrame(0.25d);
    Require(afterKill && !killedDuringParent.IsRunning(),
        "Killing an active child must release its remaining parent time to the following step.");

    var liveValue = 0d;
    var liveChild = tree.CreateTween();
    liveChild.TweenMethod(value => liveValue = value, 0d, 1d, 1d);
    var liveParent = tree.CreateTween();
    var liveDelay = liveParent.TweenSubtween(liveChild).SetDelay(1d);
    tree.ProcessFrame(0.25d);
    liveDelay.SetDelay(0.25d);
    tree.ProcessFrame(0.25d);
    Require(DoubleNearlyEqual(liveValue, 0.25d),
        "Changing an active subtween delay must use the new threshold and full child frame delta.");
    liveParent.Kill();

    var negativeValue = 0d;
    var negativeChild = tree.CreateTween();
    negativeChild.TweenMethod(value => negativeValue = value, 0d, 1d, 1d);
    var negativeParent = tree.CreateTween();
    negativeParent.TweenSubtween(negativeChild).SetDelay(-0.2d);
    tree.ProcessFrame(0.1d);
    Require(DoubleNearlyEqual(negativeValue, 0.1d) &&
            DoubleNearlyEqual(negativeChild.GetTotalElapsedTime(), 0.1d),
        "A negative subtween delay must start on the first positive frame without inflating child time.");
    negativeParent.Kill();

    var policyValue = 0d;
    var policyChild = tree.CreateTween()
        .SetProcessMode(Tween.TweenProcessMode.Physics)
        .SetPauseMode(Tween.TweenPauseMode.Stop)
        .SetSpeedScale(2d);
    policyChild.TweenMethod(value => policyValue = value, 0d, 1d, 1d);
    var policyParent = tree.CreateTween()
        .SetPauseMode(Tween.TweenPauseMode.Process)
        .SetSpeedScale(2d);
    policyParent.TweenSubtween(policyChild);
    tree.Paused = true;
    tree.ProcessFrame(0.1d);
    Require(DoubleNearlyEqual(policyValue, 0.4d),
        "The parent pause/lane policy must drive its child while both speed multipliers still apply.");
    tree.Paused = false;
    policyParent.Kill();

    var skippedCalls = 0;
    var invalidChild = tree.CreateTween();
    invalidChild.TweenCallback(() => skippedCalls++);
    tree.ProcessFrame(0.25d);
    invalidChild.Kill();
    var skippedParent = tree.CreateTween();
    var skippedStep = skippedParent.TweenSubtween(invalidChild);
    var skippedFinishes = 0;
    var afterSkipped = false;
    skippedStep.Finished += _ => skippedFinishes++;
    skippedParent.TweenCallback(() => afterSkipped = true);
    tree.ProcessFrame(0.1d);
    Require(skippedCalls == 1 && skippedFinishes == 1 && afterSkipped &&
            invalidChild.GetTotalElapsedTime() == 0d,
        "An invalid child must be reset then skipped, allowing the parent's next step to run.");

    var disposedBefore = tree.CreateTween();
    disposedBefore.TweenInterval(1d);
    var disposedBeforeParent = tree.CreateTween();
    var disposedBeforeFinished = false;
    disposedBeforeParent.TweenSubtween(disposedBefore).Finished += _ => disposedBeforeFinished = true;
    disposedBefore.Dispose();
    tree.ProcessFrame(0.1d);
    Require(disposedBeforeFinished, "A child disposed before its nested step must finish that step safely.");

    var disposedDuring = tree.CreateTween();
    disposedDuring.TweenInterval(1d);
    var disposedDuringParent = tree.CreateTween();
    var afterDisposal = false;
    disposedDuringParent.TweenSubtween(disposedDuring);
    disposedDuringParent.TweenCallback(() => afterDisposal = true);
    tree.ProcessFrame(0.25d);
    disposedDuring.Dispose();
    tree.ProcessFrame(0.25d);
    Require(afterDisposal && !disposedDuringParent.IsRunning(),
        "Disposing an active child between frames must release the parent without querying disposed child state.");

    using var otherTree = new SceneTree(new Entity());
    var transferredCalls = 0;
    var transferred = otherTree.CreateTween();
    transferred.TweenCallback(() => transferredCalls++);
    var transferParent = tree.CreateTween();
    transferParent.TweenSubtween(transferred);
    Require(!otherTree.GetProcessedTweens().Contains(transferred),
        "A cross-tree child on the same owner thread must leave its original registry.");
    otherTree.ProcessFrame(0.1d);
    Require(transferredCalls == 0, "The original tree must not independently process a transferred child.");
    tree.ProcessFrame(0.1d);
    Require(transferredCalls == 1, "The parent tree must drive a transferred child on its own frame.");
    transferParent.Kill();

    var duplicateParent = tree.CreateTween();
    var duplicateChild = tree.CreateTween();
    duplicateChild.TweenInterval(1d);
    duplicateParent.TweenSubtween(duplicateChild);
    Expect<ArgumentException>(() => duplicateParent.TweenSubtween(duplicateChild),
        "A child cannot be nested twice.");
    Expect<ArgumentException>(() => duplicateChild.TweenSubtween(duplicateParent),
        "Nested tween cycles must be rejected before ownership changes.");
    duplicateParent.Kill();

    var startedParent = tree.CreateTween();
    startedParent.TweenInterval(1d);
    tree.ProcessFrame(0.1d);
    var lateChild = tree.CreateTween();
    lateChild.TweenInterval(1d);
    Expect<InvalidOperationException>(() => startedParent.TweenSubtween(lateChild),
        "A parent must reject appending a child after its processing starts.");
    Require(tree.GetProcessedTweens().Contains(lateChild),
        "Rejecting a late child must preserve its original tree registration.");
    startedParent.Kill();
    lateChild.Kill();

    var processingChild = tree.CreateTween();
    var availableParent = tree.CreateTween();
    availableParent.TweenInterval(1d);
    Exception? processingChildError = null;
    processingChild.TweenCallback(() => processingChildError = Capture(() => availableParent.TweenSubtween(processingChild)));
    tree.ProcessFrame(0.1d);
    Require(processingChildError is InvalidOperationException,
        "A child cannot transfer into another tween while its callback is processing.");
    availableParent.Kill();

    var failedChild = tree.CreateTween();
    failedChild.TweenCallback(() => throw new InvalidOperationException("expected child failure"));
    var failedParent = tree.CreateTween().SetParallel();
    var parallelSiblingRan = false;
    var laterTweenRan = false;
    failedParent.TweenSubtween(failedChild);
    failedParent.TweenCallback(() => parallelSiblingRan = true);
    tree.CreateTween().TweenCallback(() => laterTweenRan = true);
    Require(Capture(() => tree.ProcessFrame(0.1d)) is AggregateException && parallelSiblingRan &&
            laterTweenRan && !failedParent.IsValid() && !failedChild.IsValid(),
        "A child failure must invalidate its parent while preserving parallel siblings and later tree work.");
}

static void VerifyTweenAwaits()
{
    using var tree = new SceneTree(new Entity());
    using var source = new TweenEventSource();
    var rejected = tree.CreateTween();
    Expect<ArgumentNullException>(() => rejected.TweenAwait(null!,
            handler => source.Fired += handler, handler => source.Fired -= handler),
        "A null event publisher must be rejected before append.");
    Expect<ArgumentNullException>(() => rejected.TweenAwait(source, null!, handler => source.Fired -= handler),
        "A null subscription accessor must be rejected before append.");
    Expect<ArgumentNullException>(() => rejected.TweenAwait(source, handler => source.Fired += handler, null!),
        "A null removal accessor must be rejected before append.");
    Expect<ArgumentNullException>(() => rejected.TweenAwait<int>(source, null!, static _ => { }),
        "The one-argument overload must validate its typed subscription accessor.");
    Expect<ArgumentNullException>(() => rejected.TweenAwait<int, string>(source, static _ => { }, null!),
        "The two-argument overload must validate its typed removal accessor.");
    using (var disposed = new TweenEventSource())
    {
        disposed.Dispose();
        Expect<ObjectDisposedException>(() => rejected.TweenAwait(disposed, static _ => { }, static _ => { }),
            "A disposed event publisher must be rejected before subscribing.");
    }
    Action? partlySubscribed = null;
    Expect<InvalidOperationException>(() => rejected.TweenAwait(source,
            handler => { partlySubscribed = handler; throw new InvalidOperationException("expected subscription failure"); },
            handler => { if (ReferenceEquals(partlySubscribed, handler)) partlySubscribed = null; }),
        "A throwing subscription must report its failure after attempting rollback.");
    Require(partlySubscribed is null && !rejected.HasTweeners(),
        "A failed event subscription must leave no wrapper or tweener behind.");
    rejected.Kill();

    var added = 0;
    var removed = 0;
    var finished = 0;
    var afterEvent = false;
    var awaited = tree.CreateTween();
    awaited.TweenAwait(source,
            handler => { added++; source.Fired += handler; },
            handler => { removed++; source.Fired -= handler; })
        .Finished += _ => finished++;
    awaited.TweenCallback(() => afterEvent = true);
    Require(added == 1, "TweenAwait must subscribe when appended, before its step starts.");
    source.Emit();
    tree.ProcessFrame(0.1d);
    Require(finished == 0 && !afterEvent,
        "An event observed before the wait starts must be cleared at step start.");
    source.Emit();
    tree.ProcessFrame(0.1d);
    Require(finished == 1 && !afterEvent && removed == 0,
        "An observed event must finish its wait but consume that frame and retain the subscription for replay.");
    tree.ProcessFrame(0.1d);
    Require(afterEvent && removed == 0,
        "The following callback must run in the next positive frame without disconnecting early.");
    tree.ProcessFrame(0d);
    Require(removed == 1 && !awaited.IsValid(),
        "Tree removal must disconnect the wait exactly once.");

    Action<int>? oneArgument = null;
    Action<int, string>? twoArguments = null;
    var oneFinishes = 0;
    var twoFinishes = 0;
    var one = tree.CreateTween();
    one.TweenAwait<int>(source, handler => oneArgument += handler, handler => oneArgument -= handler)
        .Finished += _ => oneFinishes++;
    var two = tree.CreateTween();
    two.TweenAwait<int, string>(source, handler => twoArguments += handler, handler => twoArguments -= handler)
        .Finished += _ => twoFinishes++;
    tree.ProcessFrame(0.1d);
    Task.Run(() => oneArgument?.Invoke(42)).GetAwaiter().GetResult();
    twoArguments?.Invoke(7, "ready");
    Require(oneFinishes == 0 && twoFinishes == 0,
        "Typed event receipt must only set state; completion remains on the owner thread.");
    tree.ProcessFrame(0.1d);
    Require(oneFinishes == 1 && twoFinishes == 1 && !one.IsRunning() && !two.IsRunning(),
        "Both typed argument overloads must release their waits on the next owner frame.");
    tree.ProcessFrame(0d);
    Require(oneArgument is null && twoArguments is null,
        "Tree cleanup must remove wrappers for both typed event arities.");

    var zeroTimeoutFinished = 0;
    var zeroTimeoutNext = false;
    var zeroTimeout = tree.CreateTween();
    var zeroWait = zeroTimeout.TweenAwait(source,
        handler => source.Fired += handler, handler => source.Fired -= handler);
    Require(ReferenceEquals(zeroWait.SetTimeout(0d), zeroWait),
        "SetTimeout must return its own await tweener.");
    zeroWait.Finished += _ => zeroTimeoutFinished++;
    zeroTimeout.TweenCallback(() => zeroTimeoutNext = true);
    tree.ProcessFrame(0d);
    Require(zeroTimeoutFinished == 0, "A zero timeout still needs positive processing time.");
    tree.ProcessFrame(0.2d);
    Require(zeroTimeoutFinished == 1 && zeroTimeoutNext,
        "A zero timeout must finish on the first positive frame and forward its full delta.");

    var indefiniteFinished = 0;
    var indefinite = tree.CreateTween();
    indefinite.TweenAwait(source, handler => source.Fired += handler, handler => source.Fired -= handler)
        .SetTimeout(-1d).Finished += _ => indefiniteFinished++;
    tree.ProcessFrame(10d);
    Require(indefiniteFinished == 0 && indefinite.IsRunning(),
        "A negative timeout must disable expiry even after a long frame.");
    source.Emit();
    tree.ProcessFrame(0.1d);
    Require(indefiniteFinished == 1,
        "A wait with timeout disabled must still finish when its event arrives.");

    var liveTimedOut = false;
    var liveTimeout = tree.CreateTween();
    var adjustable = liveTimeout.TweenAwait(source,
        handler => source.Fired += handler, handler => source.Fired -= handler).SetTimeout(0.2d);
    adjustable.Finished += _ => liveTimedOut = true;
    tree.ProcessFrame(0.1d);
    Expect<ArgumentOutOfRangeException>(() => adjustable.SetTimeout(double.NaN),
        "A non-finite timeout must preserve the previous setting.");
    Expect<InvalidOperationException>(() => Task.Run(() => adjustable.SetTimeout(0d)).GetAwaiter().GetResult(),
        "Timeout mutation must require the tween owner thread.");
    adjustable.SetTimeout(-2d);
    tree.ProcessFrame(0.5d);
    Require(!liveTimedOut && liveTimeout.IsRunning(),
        "Changing a live timeout to a negative value must disable expiry.");
    adjustable.SetTimeout(0d);
    tree.ProcessFrame(0.1d);
    Require(liveTimedOut, "Re-enabling a live timeout must compare accumulated elapsed time immediately.");

    Action? independentlyCleared = null;
    var externallyCleared = tree.CreateTween();
    externallyCleared.TweenAwait(source, handler => independentlyCleared += handler,
            handler => independentlyCleared -= handler).SetTimeout(0.3d);
    tree.ProcessFrame(0.1d);
    independentlyCleared = null;
    tree.ProcessFrame(0.1d);
    Require(externallyCleared.IsRunning(),
        "A typed subscription token cannot observe a publisher that independently clears its event list.");
    tree.ProcessFrame(0.2d);
    Require(!externallyCleared.IsRunning(),
        "A timeout must still release a wait after an independently cleared event list.");

    var raceNext = false;
    var timeoutRace = tree.CreateTween();
    timeoutRace.TweenAwait(source, handler => source.Fired += handler, handler => source.Fired -= handler)
        .SetTimeout(0.1d);
    timeoutRace.TweenCallback(() => raceNext = true);
    tree.ProcessFrame(0d);
    source.Emit();
    tree.ProcessFrame(0.2d);
    Require(raceNext,
        "When event and timeout are both ready, timeout must take priority and forward overshoot.");

    var lostSource = new TweenEventSource();
    var lostFinished = 0;
    var afterLoss = false;
    var lost = tree.CreateTween();
    lost.TweenAwait(lostSource,
            handler => lostSource.Fired += handler, handler => lostSource.Fired -= handler)
        .Finished += _ => lostFinished++;
    lost.TweenCallback(() => afterLoss = true);
    tree.ProcessFrame(0.1d);
    lostSource.Dispose();
    tree.ProcessFrame(0.1d);
    Require(lostFinished == 1 && afterLoss,
        "Disposing an event publisher must finish the wait and leave the delivered delta for later steps.");

    var loopFinishes = 0;
    var looped = tree.CreateTween().SetLoops(2);
    looped.TweenAwait(source, handler => source.Fired += handler, handler => source.Fired -= handler)
        .Finished += _ => loopFinishes++;
    tree.ProcessFrame(0.1d);
    source.Emit();
    tree.ProcessFrame(0.1d);
    Require(loopFinishes == 1 && looped.IsRunning(),
        "The first awaited event must finish one loop and reset the wait for the next loop.");
    tree.ProcessFrame(0.1d);
    Require(loopFinishes == 1, "A loop restart must clear the previous event receipt.");
    source.Emit();
    tree.ProcessFrame(0.1d);
    Require(loopFinishes == 2 && !looped.IsRunning(),
        "A second event must finish the replayed wait and final loop.");

    var canceledFinishes = 0;
    var canceledRemovals = 0;
    var canceled = tree.CreateTween();
    canceled.TweenAwait(source, handler => source.Fired += handler,
            handler => { canceledRemovals++; source.Fired -= handler; })
        .Finished += _ => canceledFinishes++;
    canceled.Kill();
    source.Emit();
    tree.ProcessFrame(0.1d);
    Require(canceledFinishes == 0 && canceledRemovals == 1 && !canceled.IsValid(),
        "Killing an unstarted wait must disconnect once without emitting completion.");

    var failedLaterRan = false;
    var failed = tree.CreateTween();
    failed.TweenAwait(source, handler => source.Fired += handler, handler => source.Fired -= handler)
        .Finished += _ => throw new InvalidOperationException("expected wait completion failure");
    tree.ProcessFrame(0.1d);
    source.Emit();
    tree.CreateTween().TweenCallback(() => failedLaterRan = true);
    Require(Capture(() => tree.ProcessFrame(0.1d)) is AggregateException &&
            failedLaterRan && !failed.IsValid(),
        "A failing wait completion subscriber must invalidate only its sequence and preserve later tree work.");
}

static void VerifySceneTreeFailureSafety()
{
    var enterRoot = new FailingLifecycleNode
    {
        Name = "enter-root",
        AddChildOnEnter = true,
        CreateTimerOnEnter = true,
        CreateTweenOnEnter = true,
        ThrowOnEnter = true
    };
    var enterChild = new Entity { Name = "enter-child" };
    enterRoot.AddChild(enterChild);
    Require(Capture(() => new SceneTree(enterRoot)) is AggregateException,
        "A failing enter callback must fail construction with aggregated context.");
    Require(enterRoot.Tree is null && enterChild.Tree is null && enterRoot.AddedChild?.Tree is null &&
            enterRoot.AddedChild?.IsNodeReady == false && enterRoot.CreatedTimer?.IsDisposed == true &&
            enterRoot.CreatedTween?.IsValid() == false &&
            !enterRoot.IsDisposed && !enterChild.IsDisposed,
        "Failed construction must roll back all tree membership without taking caller ownership.");
    var escapedTree = enterRoot.CapturedTree!;
    Require(escapedTree.IsDisposed && Capture(() => escapedTree.Defer(static () => { })) is ObjectDisposedException &&
            Capture(() => escapedTree.CreateTimer(1d)) is ObjectDisposedException &&
            Capture(() => _ = escapedTree.CreateTween()) is ObjectDisposedException &&
            enterRoot.TimerCreationDuringRollbackError is ObjectDisposedException,
        "A tree escaped from failed construction must be terminal before rollback callbacks can enqueue new work.");
    escapedTree.Dispose();
    Require(!enterRoot.IsDisposed && !enterChild.IsDisposed,
        "Disposing an escaped failed-construction tree must not take ownership of the caller's hierarchy.");
    enterRoot.Dispose();

    var readyRoot = new FailingLifecycleNode { Name = "ready-root", AddChildOnReady = true, ThrowOnReady = true };
    var readyChild = new Entity { Name = "ready-child" };
    readyRoot.AddChild(readyChild);
    Require(Capture(() => new SceneTree(readyRoot)) is AggregateException,
        "A failing ready callback must fail construction.");
    Require(readyRoot.Tree is null && readyChild.Tree is null && readyRoot.AddedChild?.Tree is null &&
            !readyRoot.IsNodeReady && !readyChild.IsNodeReady && readyRoot.AddedChild?.IsNodeReady == false,
        "Failed ready delivery must restore ready state consumed by the activation attempt.");
    readyRoot.Dispose();

    var disposeRoot = new FailingLifecycleNode { Name = "dispose-root", ThrowOnExit = true };
    var failingChild = new FailingLifecycleNode { Name = "failing-child", ThrowOnDispose = true };
    var laterChild = new Entity { Name = "later-child" };
    disposeRoot.AddChild(failingChild);
    disposeRoot.AddChild(laterChild);
    var failingTree = new SceneTree(disposeRoot);
    Require(Capture(failingTree.Dispose) is AggregateException,
        "Tree disposal must report lifecycle and descendant cleanup failures.");
    Require(failingTree.IsDisposed && disposeRoot.IsDisposed && failingChild.IsDisposed && laterChild.IsDisposed &&
            disposeRoot.Tree is null && laterChild.Tree is null && !failingTree.HasDeferredWork,
        "Tree disposal must finish every teardown stage despite user callback failures.");

    var teardownRoot = new TeardownQueueNode { Name = "teardown-root" };
    var teardownTree = new SceneTree(teardownRoot);
    teardownTree.Dispose();
    Require(teardownRoot.QueueWasRejected && teardownRoot.PauseWasRejected && teardownTree.IsDisposed &&
            !teardownTree.HasDeferredWork,
        "Work and pause mutation during teardown must be rejected instead of touching partial state.");

    var mutationRoot = new Entity { Name = "mutation-root" };
    var exitingChild = new Entity { Name = "exiting-child" };
    mutationRoot.AddChild(exitingChild);
    using (var mutationTree = new SceneTree(mutationRoot))
    {
        Exception? nestedRemovalError = null;
        Exception? reparentError = null;
        Exception? disposalError = null;
        exitingChild.TreeExiting += node =>
        {
            nestedRemovalError = Capture(() => mutationRoot.RemoveChild(node));
            reparentError = Capture(() => node.Reparent(mutationRoot));
            disposalError = Capture(node.Dispose);
        };

        Require(mutationRoot.RemoveChild(exitingChild), "The outer removal must complete after rejected exit re-entry.");
        Require(nestedRemovalError is InvalidOperationException && reparentError is InvalidOperationException &&
                disposalError is InvalidOperationException && exitingChild.Tree is null && exitingChild.Parent is null &&
                !exitingChild.IsDisposed,
            "Removal, reparenting, and disposal must be rejected while exit callbacks are in progress.");
        exitingChild.Dispose();
    }

    var reattachRoot = new Entity { Name = "reattach-root" };
    var reattachTree = new SceneTree(reattachRoot);
    Exception? reattachError = null;
    reattachRoot.TreeExited += node => reattachError = Capture(() => new SceneTree(node));
    reattachTree.Dispose();
    Require(reattachError is AggregateException && reattachRoot.IsDisposed && reattachRoot.Tree is null,
        "A node must not re-enter another tree from its in-progress exit callback.");

    var enteringRoot = new Entity { Name = "entering-root" };
    var enteringFirst = new Entity { Name = "entering-first" };
    var enteringSecond = new Entity { Name = "entering-second" };
    enteringRoot.AddChild(enteringFirst);
    enteringRoot.AddChild(enteringSecond);
    Exception? enteringRemovalError = null;
    enteringFirst.TreeEntered += node => enteringRemovalError = Capture(() => enteringRoot.RemoveChild(node));
    enteringFirst.TreeEntered += _ => enteringRoot.RemoveChild(enteringSecond);
    using (var enteringTree = new SceneTree(enteringRoot))
    {
        Require(enteringRemovalError is InvalidOperationException && ReferenceEquals(enteringFirst.Tree, enteringTree) &&
                enteringSecond.Tree is null && enteringSecond.Parent is null && !enteringSecond.IsNodeReady,
            "Entry re-entry must be rejected and a removed snapshot sibling must not enter or become ready.");
    }
    enteringSecond.Dispose();

    var readySnapshotRoot = new Entity { Name = "ready-snapshot-root" };
    var readyFirst = new Entity { Name = "ready-first" };
    var readySecond = new Entity { Name = "ready-second" };
    readySnapshotRoot.AddChild(readyFirst);
    readySnapshotRoot.AddChild(readySecond);
    readyFirst.Ready += _ => readySnapshotRoot.RemoveChild(readySecond);
    using (var readySnapshotTree = new SceneTree(readySnapshotRoot))
    {
        Require(readySecond.Tree is null && readySecond.Parent is null && !readySecond.IsNodeReady,
            "A removed ready snapshot sibling must not receive ready after it leaves the tree.");
    }
    readySecond.Dispose();

    var nestedReadyRoot = new Entity { Name = "nested-ready-root" };
    var nestedReadyParent = new Entity { Name = "nested-ready-parent" };
    var nestedReadyChild = new Entity { Name = "nested-ready-child" };
    Exception? readyParentRemovalError = null;
    Exception? readyParentDisposalError = null;
    nestedReadyParent.AddChild(nestedReadyChild);
    nestedReadyRoot.AddChild(nestedReadyParent);
    nestedReadyChild.Ready += _ =>
    {
        readyParentRemovalError = Capture(() => nestedReadyRoot.RemoveChild(nestedReadyParent));
        readyParentDisposalError = Capture(nestedReadyParent.Dispose);
    };
    using (var nestedReadyTree = new SceneTree(nestedReadyRoot))
    {
        Require(readyParentRemovalError is InvalidOperationException &&
                readyParentDisposalError is InvalidOperationException && nestedReadyParent.IsNodeReady &&
                ReferenceEquals(nestedReadyParent.Tree, nestedReadyTree),
            "A descendant ready callback must not remove or dispose its subtree root during ready delivery.");
    }

    var deletionRoot = new Entity { Name = "deletion-root" };
    var queueFreeFailure = new Entity { Name = "queue-free-failure" };
    var queueDeleteFailure = new Entity { Name = "queue-delete-failure" };
    deletionRoot.AddChild(queueFreeFailure);
    deletionRoot.AddChild(queueDeleteFailure);
    using (var deletionTree = new SceneTree(deletionRoot))
    {
        queueFreeFailure.TreeExiting += _ => throw new InvalidOperationException("expected queued exit failure");
        queueDeleteFailure.TreeExiting += _ => throw new InvalidOperationException("expected queued exit failure");
        queueFreeFailure.QueueFree();
        deletionTree.QueueDelete(queueDeleteFailure);
        Require(Capture(deletionTree.FlushDeferred) is AggregateException && queueFreeFailure.IsDisposed &&
                queueDeleteFailure.IsDisposed && queueFreeFailure.Parent is null && queueDeleteFailure.Parent is null,
            "Queued deletion must finish disposal after detach callbacks fail.");
    }

    var oldRoot = new Entity { Name = "old-root" };
    var newRoot = new Entity { Name = "new-root" };
    var transferred = new Entity { Name = "transferred" };
    oldRoot.AddChild(transferred);
    using (var oldTree = new SceneTree(oldRoot))
    using (var newTree = new SceneTree(newRoot))
    {
        transferred.QueueFree();
        transferred.Reparent(newRoot);
        oldTree.FlushDeferred();
        Require(!transferred.IsDisposed && transferred.IsQueuedForDeletion && ReferenceEquals(transferred.Tree, newTree),
            "A stale deletion in the old tree must not consume a request transferred to the new tree.");
        newTree.FlushDeferred();
        Require(transferred.IsDisposed && transferred.Parent is null,
            "The destination tree must execute a transferred queued deletion.");
    }

    var disposingParent = new Entity { Name = "disposing-parent" };
    var disposingChild = new Entity { Name = "disposing-child" };
    var lateChild = new Entity { Name = "late-child" };
    Exception? disposalMutationError = null;
    disposingParent.AddChild(disposingChild);
    disposingChild.Disposed += _ => disposalMutationError = Capture(() => disposingParent.AddChild(lateChild));
    disposingParent.Dispose();
    Require(disposalMutationError is ObjectDisposedException && disposingParent.IsDisposed && disposingChild.IsDisposed &&
            lateChild.Parent is null && !lateChild.IsDisposed,
        "A disposing parent must reject re-entrant child insertion and leave the candidate detached.");
    lateChild.Dispose();

    var transferParent = new Entity { Name = "transfer-parent" };
    var transferTrigger = new Entity { Name = "transfer-trigger" };
    var transferCandidate = new Entity { Name = "transfer-candidate" };
    var transferDestination = new Entity { Name = "transfer-destination" };
    Exception? disposalTransferError = null;
    transferParent.AddChild(transferTrigger);
    transferParent.AddChild(transferCandidate);
    transferTrigger.Disposed += _ => disposalTransferError = Capture(() => transferCandidate.Reparent(transferDestination));
    transferParent.Dispose();
    Require(disposalTransferError is ObjectDisposedException && transferCandidate.IsDisposed &&
            transferDestination.ChildCount == 0,
        "A child cannot escape its disposing parent's ownership through a re-entrant reparent.");
    transferDestination.Dispose();

    var preDeleteParent = new PreDeleteReparentNode { Name = "pre-delete-parent" };
    var preDeleteChild = new Entity { Name = "pre-delete-child" };
    var preDeleteDestination = new Entity { Name = "pre-delete-destination" };
    preDeleteParent.Target = preDeleteChild;
    preDeleteParent.Destination = preDeleteDestination;
    preDeleteParent.AddChild(preDeleteChild);
    preDeleteParent.Dispose();
    Require(preDeleteParent.ReparentError is ObjectDisposedException && preDeleteChild.IsDisposed &&
            preDeleteDestination.ChildCount == 0,
        "Pre-delete callbacks must not transfer children out of the disposal ownership snapshot.");
    preDeleteDestination.Dispose();

    var exitOwnershipRoot = new Entity { Name = "exit-ownership-root" };
    var exitMutator = new ExitSiblingMutationNode { Name = "exit-mutator" };
    var exitOwnedSibling = new Entity { Name = "exit-owned-sibling" };
    var exitDestination = new Entity { Name = "exit-destination" };
    exitMutator.Sibling = exitOwnedSibling;
    exitMutator.Destination = exitDestination;
    exitOwnershipRoot.AddChild(exitMutator);
    exitOwnershipRoot.AddChild(exitOwnedSibling);
    new SceneTree(exitOwnershipRoot).Dispose();
    Require(exitMutator.RemoveError is InvalidOperationException &&
            exitMutator.ReparentError is InvalidOperationException &&
            exitMutator.DisposeError is InvalidOperationException && exitOwnedSibling.IsDisposed &&
            exitDestination.ChildCount == 0,
        "Exit callbacks must not remove sibling nodes from the hierarchy owned by tree disposal.");
    exitDestination.Dispose();

    var lifecycleRoot = new Entity { Name = "lifecycle-root" };
    using (var lifecycleTree = new SceneTree(lifecycleRoot))
    {
        var enteringNode = new Entity { Name = "runtime-entering" };
        Exception? enterFlushError = null;
        Exception? enterDisposeError = null;
        enteringNode.TreeEntered += node =>
        {
            node.QueueFree();
            enterFlushError = Capture(lifecycleTree.FlushDeferred);
            enterDisposeError = Capture(lifecycleTree.Dispose);
        };
        lifecycleRoot.AddChild(enteringNode);
        Require(enterFlushError is InvalidOperationException && enterDisposeError is InvalidOperationException &&
                enteringNode.IsQueuedForDeletion && !lifecycleTree.IsDisposed,
            "Flush and tree disposal must be rejected during runtime entry without consuming queued deletion.");
        lifecycleTree.FlushDeferred();
        Require(enteringNode.IsDisposed, "Queued deletion from runtime entry must execute at the next safe point.");

        var exitingNode = new Entity { Name = "runtime-exiting" };
        Exception? exitFlushError = null;
        lifecycleRoot.AddChild(exitingNode);
        exitingNode.TreeExiting += node =>
        {
            node.QueueFree();
            exitFlushError = Capture(lifecycleTree.FlushDeferred);
        };
        lifecycleRoot.RemoveChild(exitingNode);
        Require(exitFlushError is InvalidOperationException && exitingNode.IsQueuedForDeletion && exitingNode.Tree is null,
            "Flush must be rejected during runtime exit without consuming queued deletion.");
        lifecycleTree.FlushDeferred();
        Require(exitingNode.IsDisposed, "A queued node detached before its safe point must still be disposed.");
    }

    var pauseRoot = new ReentrantPauseNode { Name = "pause-root" };
    using (var pauseTree = new SceneTree(pauseRoot))
    {
        Require(Capture(() => pauseTree.Paused = true) is AggregateException &&
                pauseRoot.ReentryError is InvalidOperationException && pauseTree.Paused,
            "An opposite pause transition must be rejected during pause notification without corrupting final state.");
    }

    var pauseMutationRoot = new Entity { Name = "pause-mutation-root" };
    var pauseMutator = new PauseMutationNode { Name = "pause-mutator" };
    var pauseRemoved = new PauseMutationNode { Name = "pause-removed" };
    pauseMutator.Target = pauseRemoved;
    pauseMutationRoot.AddChild(pauseMutator);
    pauseMutationRoot.AddChild(pauseRemoved);
    using (var pauseMutationTree = new SceneTree(pauseMutationRoot))
    {
        pauseMutationTree.Paused = true;
        Require(pauseRemoved.IsDisposed && pauseRemoved.PauseNotifications == 0,
            "Pause traversal must skip a captured node removed and disposed by an earlier notification.");
    }

    var pauseReparentRoot = new Entity { Name = "pause-reparent-root" };
    var pauseReparenter = new PauseReparentNode { Name = "pause-reparenter" };
    var pauseMoved = new PauseMutationNode { Name = "pause-moved" };
    var pauseDestination = new Entity { Name = "pause-destination" };
    pauseReparenter.Target = pauseMoved;
    pauseReparenter.Destination = pauseDestination;
    pauseReparentRoot.AddChild(pauseReparenter);
    pauseReparentRoot.AddChild(pauseMoved);
    pauseReparentRoot.AddChild(pauseDestination);
    using (var pauseReparentTree = new SceneTree(pauseReparentRoot))
    {
        pauseReparentTree.Paused = true;
        Require(ReferenceEquals(pauseMoved.Parent, pauseDestination) && pauseMoved.PauseNotifications == 1,
            "Pause traversal must notify a node at most once when an earlier callback reparents it into a later branch.");
    }

    var pauseBarrierRoot = new PauseBarrierNode { Name = "pause-barrier-root" };
    using (var pauseBarrierTree = new SceneTree(pauseBarrierRoot))
    {
        pauseBarrierTree.Paused = true;
        Require(pauseBarrierRoot.FlushError is InvalidOperationException &&
                pauseBarrierRoot.DisposeError is InvalidOperationException && !pauseBarrierTree.IsDisposed,
            "Flush and tree disposal must be rejected during pause notification delivery.");
    }

    for (var iteration = 0; iteration < 256; iteration++)
    {
        var queueRaceRoot = new Entity { Name = $"queue-race-root-{iteration}" };
        var queueRaceChild = new Entity { Name = "queue-race-child" };
        queueRaceRoot.AddChild(queueRaceChild);
        using var queueRaceTree = new SceneTree(queueRaceRoot);
        using var queueRaceStart = new ManualResetEventSlim();
        var queueTask = Task.Run(() =>
        {
            queueRaceStart.Wait();
            queueRaceChild.QueueFree();
        });
        queueRaceStart.Set();
        queueRaceTree.FlushDeferred();
        queueTask.Wait();
        queueRaceTree.FlushDeferred();
        Require(queueRaceChild.IsDisposed && !queueRaceChild.IsQueuedForDeletion,
            "Concurrent QueueFree publication and flush must not lose the deletion request.");
    }

    for (var iteration = 0; iteration < 64; iteration++)
    {
        var raceTree = new SceneTree(new Entity { Name = $"race-{iteration}" });
        using var start = new ManualResetEventSlim();
        var workers = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            start.Wait();

            try
            {
                raceTree.Defer(static () => { });
            }
            catch (ObjectDisposedException)
            {
            }
        })).ToArray();

        start.Set();
        raceTree.Dispose();
        Task.WaitAll(workers);
        Require(!raceTree.HasDeferredWork, "Concurrent enqueue/disposal must never leave accepted work stranded.");
    }
}

static void VerifyResources()
{
    using var resource = new Resource();
    Require(!resource.ResourceLocalToScene && resource.ResourceName.Length == 0 &&
            resource.ResourcePath.Length == 0 && resource.ResourceSceneUniqueID.Length == 0 && resource.IsBuiltIn,
        "A resource must start unnamed, pathless, built-in, and not local to a scene.");

    var propertyNames = resource.GetPropertyList().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
    Require(propertyNames.IsSupersetOf([
        nameof(Resource.ResourceLocalToScene),
        nameof(Resource.ResourceName),
        nameof(Resource.ResourcePath),
        nameof(Resource.ResourceSceneUniqueID)
    ]), "The typed property list must expose all resource properties.");

    var changes = 0;
    resource.Changed += sender =>
    {
        Require(ReferenceEquals(sender, resource), "Changed must identify its resource.");
        changes++;
    };
    resource.ResourceName = "data";
    resource.ResourceName = "data";
    resource.ResourceLocalToScene = true;
    resource.ResourceSceneUniqueID = "Data_42";
    Require(changes == 2, "Every resource-name assignment, and no configuration-only assignment, must emit Changed.");

    Expect<ArgumentNullException>(() => resource.ResourceName = null!, "A resource name must reject null.");
    Expect<ArgumentNullException>(() => resource.ResourceSceneUniqueID = null!, "A scene ID must reject null.");
    Expect<ArgumentException>(() => resource.ResourceSceneUniqueID = "bad-id", "A scene ID must reject punctuation.");
    Require(resource.ResourceSceneUniqueID == "Data_42", "A rejected scene ID must not change stored state.");

    var generatedIds = new string[1_024];
    Parallel.For(0, generatedIds.Length, index => generatedIds[index] = Resource.GenerateSceneUniqueID());
    Require(generatedIds.All(id => id.Length == 5 && id.All(character =>
            character is >= 'a' and <= 'y' or >= '0' and <= '8')) && generatedIds.Distinct().Count() > 1,
        "Generated scene IDs must use the documented compact alphabet and be safe under concurrent calls.");

    var prefix = $"memory://resource-tests/{Guid.NewGuid():N}";
    var ownedPath = $"{prefix}/owned";
    using var contender = new Resource { ResourcePath = $"{prefix}/previous" };
    resource.ResourcePath = ownedPath;
    Expect<InvalidOperationException>(() => contender.ResourcePath = ownedPath,
        "Assigning a path owned by another live resource must fail.");
    Require(contender.ResourcePath == $"{prefix}/previous" && resource.ResourcePath == ownedPath,
        "A failed path assignment must leave both owners unchanged.");

    contender.TakeOverPath(ownedPath);
    Require(resource.ResourcePath.Length == 0 && contender.ResourcePath == ownedPath,
        "Taking over a path must atomically clear the previous owner.");

    using var rawA = new TestResource();
    using var rawB = new TestResource();
    rawA.SetPathCache($"{prefix}/raw");
    rawB.SetPathCache($"{prefix}/raw");
    Require(rawA.ResourcePath == rawB.ResourcePath && rawA.PathCacheSetCount == 1 && rawB.PathCacheSetCount == 1,
        "Raw path-cache assignment must bypass uniqueness and invoke the typed hook.");
    rawA.ResourcePath = $"{prefix}/raw";
    Expect<InvalidOperationException>(() => rawB.ResourcePath = $"{prefix}/raw",
        "Assigning an unchanged raw path must still attempt to claim cache ownership.");
    rawB.TakeOverPath($"{prefix}/raw");
    Require(rawA.ResourcePath.Length == 0 && rawB.ResourcePath == $"{prefix}/raw",
        "Taking over an unchanged raw path must transfer its registered owner.");

    resource.ResourcePath = string.Empty;
    Require(resource.IsBuiltIn, "A pathless resource must be built-in.");
    resource.ResourcePath = $"{prefix}/external";
    Require(!resource.IsBuiltIn, "A standalone external path must not be built-in.");
    resource.ResourcePath = $"{prefix}/external::nested";
    Require(resource.IsBuiltIn, "An embedded-resource path must be built-in.");
    resource.ResourcePath = $"local://{Guid.NewGuid():N}";
    Require(resource.IsBuiltIn, "A local-resource path must be built-in.");

    var released = new Resource { ResourcePath = $"{prefix}/released" };
    released.Dispose();
    using var replacement = new Resource { ResourcePath = $"{prefix}/released" };

    var racers = Enumerable.Range(0, 32).Select(_ => new Resource()).ToArray();
    var winners = 0;
    var occupiedFailures = 0;
    Parallel.ForEach(racers, candidate =>
    {
        try
        {
            candidate.ResourcePath = $"{prefix}/race";
            Interlocked.Increment(ref winners);
        }
        catch (InvalidOperationException)
        {
            Interlocked.Increment(ref occupiedFailures);
        }
    });
    Require(winners == 1 && occupiedFailures == racers.Length - 1,
        "Concurrent path claims must select exactly one owner.");
    foreach (var racer in racers)
        racer.Dispose();

    using var throwingName = new Resource();
    throwingName.Changed += _ => throw new InvalidOperationException("expected change failure");
    Require(Capture(() => throwingName.ResourceName = "committed") is InvalidOperationException &&
            throwingName.ResourceName == "committed",
        "A throwing change handler must propagate after the name is committed.");

    using var setup = new SetupProbeResource();
#pragma warning disable CS0618
    setup.SetupLocalToSceneRequested += _ => setup.Order.Add("event");
    setup.SetupLocalToScene();
#pragma warning restore CS0618
    Require(setup.Order.SequenceEqual(["event", "hook"]),
        "Scene-local setup must publish its compatibility event before the virtual hook.");

    using var failingSetup = new SetupProbeResource { ThrowInHook = true };
#pragma warning disable CS0618
    failingSetup.SetupLocalToSceneRequested += _ => throw new ArgumentException("expected event failure");
    var setupError = Capture(failingSetup.SetupLocalToScene);
#pragma warning restore CS0618
    Require(setupError is AggregateException { InnerExceptions.Count: 2 } && failingSetup.Order.SequenceEqual(["hook"]),
        "Scene-local setup must attempt the hook and aggregate failures after a throwing event.");

    using var plainDuplicate = resource.Duplicate();
    Require(plainDuplicate.GetType() == typeof(Resource) && plainDuplicate.ResourceName == resource.ResourceName &&
            plainDuplicate.ResourceLocalToScene == resource.ResourceLocalToScene &&
            plainDuplicate.ResourcePath.Length == 0 && plainDuplicate.ResourceSceneUniqueID.Length == 0,
        "A base resource duplicate must copy stored content but not path identity.");

    using var root = new TestResource
    {
        ResourceName = "root",
        ResourceLocalToScene = true,
        ResourcePath = $"{prefix}/root",
        ResourceSceneUniqueID = "root_1",
        Value = 7,
        Numbers = [1, 2, 3]
    };
    using var embedded = new TestResource { Value = 11, Numbers = [4] };
    using var external = new TestResource { Value = 13, ResourcePath = $"{prefix}/child" };
    root.First = embedded;
    root.Second = embedded;
    root.External = external;
    root.Always = external;
    root.Never = embedded;
    embedded.First = root;

    using var shallow = (TestResource)root.Duplicate();
    Require(ReferenceEquals(shallow.Numbers, root.Numbers) && ReferenceEquals(shallow.First, embedded) &&
            shallow.Always is not null && !ReferenceEquals(shallow.Always, external) && ReferenceEquals(shallow.Never, embedded) &&
            shallow.ResourcePath.Length == 0 && shallow.ResourceSceneUniqueID.Length == 0,
        "Shallow duplication must honor default, forced, and never-duplicate typed properties while clearing identity.");

    using var containerDeep = (TestResource)root.DuplicateDeep(DeepDuplicateMode.None);
    Require(!ReferenceEquals(containerDeep.Numbers, root.Numbers) && containerDeep.Numbers.SequenceEqual(root.Numbers) &&
            ReferenceEquals(containerDeep.First, embedded) && ReferenceEquals(containerDeep.External, external) &&
            containerDeep.Always is not null && !ReferenceEquals(containerDeep.Always, external) &&
            ReferenceEquals(containerDeep.Never, embedded),
        "Deep duplication with None must clone containers, share default resources, and honor explicit overrides.");

    using var internalDeep = (TestResource)root.Duplicate(deep: true);
    Require(internalDeep.First is not null && !ReferenceEquals(internalDeep.First, embedded) &&
            ReferenceEquals(internalDeep.First, internalDeep.Second) && ReferenceEquals(internalDeep.First.First, internalDeep) &&
            ReferenceEquals(internalDeep.External, external) && internalDeep.Always is not null &&
            !ReferenceEquals(internalDeep.Always, external) && ReferenceEquals(internalDeep.Never, embedded) &&
            !ReferenceEquals(internalDeep.First.Numbers, embedded.Numbers),
        "Internal deep duplication must preserve aliases and cycles while sharing external nested resources.");

    using var allDeep = (TestResource)root.DuplicateDeep(DeepDuplicateMode.All);
    Require(allDeep.External is not null && !ReferenceEquals(allDeep.External, external) &&
            ReferenceEquals(allDeep.Always, allDeep.External) && ReferenceEquals(allDeep.Never, embedded) &&
            allDeep.External.ResourcePath.Length == 0,
        "All-mode deep duplication must duplicate external resources once, honor never-copy fields, and clear path identity.");
    Expect<ArgumentOutOfRangeException>(() => root.DuplicateDeep((DeepDuplicateMode)99),
        "Deep duplication must reject unknown policies.");

    using var copySource = new TestResource
    {
        ResourceName = "source",
        ResourceLocalToScene = true,
        ResourceSceneUniqueID = "source_1",
        Value = 21,
        Numbers = [8, 9],
        First = embedded
    };
    using var copyTarget = new TestResource
    {
        ResourceName = "target",
        ResourcePath = $"{prefix}/copy-target",
        ResourceSceneUniqueID = "target_1",
        Transient = 99
    };
    var copyChanges = 0;
    copyTarget.Changed += _ => copyChanges++;
    copyTarget.CopyFromResource(copySource);
    Require(copyTarget.ResourceName == "source" && copyTarget.ResourceLocalToScene && copyTarget.Value == 21 &&
            ReferenceEquals(copyTarget.Numbers, copySource.Numbers) && ReferenceEquals(copyTarget.First, embedded) &&
            copyTarget.ResourcePath == $"{prefix}/copy-target" && copyTarget.ResourceSceneUniqueID == "target_1" &&
            copyTarget.Transient == 0 && copyTarget.ResetCount == 1 && copyChanges == 1,
        "CopyFromResource must reset state, shallow-copy stored data, preserve target identity, and coalesce changes.");
    copyTarget.CopyFromResource(copyTarget);
    Require(copyChanges == 1, "Copying a resource from itself must be a no-op.");
    Expect<ArgumentException>(() => copyTarget.CopyFromResource(resource),
        "CopyFromResource must require the exact same runtime type.");

    using var concurrentCopySource = new Resource { ResourceName = "concurrent-source" };
    using var concurrentCopyTarget = new Resource();
    var concurrentCopyChanges = 0;
    concurrentCopyTarget.Changed += _ => Interlocked.Increment(ref concurrentCopyChanges);
    Parallel.For(0, 256, _ => concurrentCopyTarget.CopyFromResource(concurrentCopySource));
    Require(concurrentCopyChanges == 256,
        "Concurrent copy batches must serialize and publish one coalesced change per operation.");

    using var resetSource = new ResetFailureResource();
    using var resetTarget = new ResetFailureResource { ThrowOnReset = true };
    var failedCopyChanges = 0;
    resetTarget.Changed += _ => failedCopyChanges++;
    Require(Capture(() => resetTarget.CopyFromResource(resetSource)) is InvalidOperationException && failedCopyChanges == 1,
        "A failed non-transactional copy must still report a possibly partial state change exactly once.");

    using var dualFailureTarget = new ResetFailureResource { ThrowOnReset = true };
    dualFailureTarget.Changed += _ => throw new ArgumentException("expected change failure");
    Require(Capture(() => dualFailureTarget.CopyFromResource(resetSource)) is AggregateException { InnerExceptions.Count: 2 },
        "CopyFromResource must aggregate operation and final change-handler failures.");

    using var unsupported = new UnsupportedResource();
    Expect<NotSupportedException>(() => unsupported.Duplicate(),
        "A derived resource without explicit duplication hooks must not silently lose custom state.");

    using var wrongFactory = new WrongFactoryResource();
    WrongFactoryResource.LastCreated = null;
    Expect<InvalidOperationException>(() => wrongFactory.Duplicate(),
        "A duplication factory must return the exact source runtime type.");
    Require(WrongFactoryResource.LastCreated is { IsDisposed: true },
        "A rejected factory result must be disposed during duplication rollback.");

    using var dirtyCopy = new DirtyCopyResource();
    DirtyCopyResource.LastCreated = null;
    Expect<InvalidOperationException>(() => dirtyCopy.Duplicate(),
        "A custom copier must not assign external identity to a duplicate.");
    Require(DirtyCopyResource.LastCreated is { IsDisposed: true },
        "A duplicate that violates post-copy identity must be disposed during rollback.");

    using var selfFactory = new SelfFactoryResource();
    Expect<InvalidOperationException>(() => selfFactory.Duplicate(),
        "A duplication factory must not return its source.");
    Require(!selfFactory.IsDisposed, "Rejecting a self-returning factory must not dispose the source.");

    FailingDuplicateResource.Created.Clear();
    using var failingChild = new FailingDuplicateResource();
    using var failingRoot = new FailingDuplicateResource { Child = failingChild, ThrowOnCopy = true };
    Require(Capture(() => failingRoot.DuplicateDeep(DeepDuplicateMode.All)) is InvalidOperationException &&
            FailingDuplicateResource.Created.Count == 2 && FailingDuplicateResource.Created.All(item => item.IsDisposed),
        "A failed graph duplication must dispose every partially created resource.");

    using var cleanupFailure = new CleanupFailureResource();
    var cleanupFailureError = Capture(() => cleanupFailure.Duplicate());
    Require(cleanupFailureError is AggregateException { InnerExceptions.Count: 2 } &&
            CleanupFailureResource.LastCreated is { IsDisposed: true },
        "Duplication must aggregate its original failure with cleanup failures after finalizing partial targets.");

    var disposablePath = $"{prefix}/disposed";
    var disposable = new Resource { ResourcePath = disposablePath };
    disposable.Dispose();
    Expect<ObjectDisposedException>(disposable.EmitChanged, "Disposed resources must reject change publication.");
    using var afterDispose = new Resource { ResourcePath = disposablePath };
}

static void VerifyPackedScenes()
{
    var prefix = $"memory://packed-scenes/{Guid.NewGuid():N}";
    using var scene = new PackedScene { ResourcePath = $"{prefix}/main.scene" };
    var liveState = scene.GetState();
    Require(!scene.CanInstantiate() && liveState.GetNodeCount() == 0 &&
            ReferenceEquals(liveState, scene.GetState()) && liveState.GetPath() == scene.ResourcePath,
        "A new packed scene must expose one live empty state with its resource path.");
    Expect<InvalidOperationException>(() => scene.Instantiate(),
        "An empty packed scene must not instantiate.");
    Expect<ArgumentOutOfRangeException>(() => scene.Instantiate((PackedSceneEditState)99),
        "Packed-scene instantiation must reject unknown edit states.");
    Expect<NotSupportedException>(() => scene.Instantiate(PackedSceneEditState.Instance),
        "Runtime packed-scene instantiation must reject editor-only modes.");

    using var nestedLocal = new PackedTestResource
    {
        ResourceLocalToScene = true,
        ResourcePath = $"{prefix}/nested.resource",
        Value = 31
    };
    using var local = new PackedTestResource
    {
        ResourceLocalToScene = true,
        Value = 17,
        Child = nestedLocal
    };

    var root = new PackedTestNode { Name = "Root", Value = 7, Data = local, TranslationDomain = "scene" };
    var child = new PackedTestNode { Name = "Child", Value = 11, Data = local };
    var grandchild = new PackedTestNode { Name = "Grandchild", Value = 13 };
    var unowned = new PackedTestNode { Name = "RuntimeOnly", Value = 99 };
    var prunedGrandchild = new PackedTestNode { Name = "Pruned", Value = 101 };
    root.AddChild(child);
    child.Owner = root;
    child.AddChild(grandchild);
    grandchild.Owner = root;
    root.AddChild(unowned);
    unowned.AddChild(prunedGrandchild);
    prunedGrandchild.Owner = root;
    child.AddToGroup("persistent", persistent: true);
    child.AddToGroup("runtime-only");

    scene.Pack(root);
    Require(scene.CanInstantiate() && liveState.GetNodeCount() == 3 &&
            liveState.GetNodeName(0) == "Root" && liveState.GetNodeName(1) == "Child" &&
            liveState.GetNodeName(2) == "Grandchild" && liveState.GetNodePath(0) == "." &&
            liveState.GetNodePath(2) == "Child/Grandchild" && liveState.GetNodePath(2, forParent: true) == "Child" &&
            liveState.GetNodeOwnerPath(0).Length == 0 && liveState.GetNodeOwnerPath(1) == "." &&
            liveState.GetNodeGroups(1).SequenceEqual(["persistent"]) && liveState.GetNodeIndex(1) == -1 &&
            liveState.GetNodeInstance(1) is null && !liveState.IsNodeInstancePlaceholder(1) &&
            liveState.GetNodeInstancePlaceholder(1).Length == 0 && liveState.GetBaseSceneState() is null &&
            liveState.GetConnectionCount() == 0,
        "Scene state must expose the owned DFS hierarchy and its runtime-authored metadata.");
    var valuePropertyIndex = Enumerable.Range(0, liveState.GetNodePropertyCount(1))
        .Single(index => liveState.GetNodePropertyName(1, index) == nameof(PackedTestNode.Value));
    Require(liveState.GetNodePropertyType(1, valuePropertyIndex) == typeof(int) &&
            liveState.GetNodePropertyValue<int>(1, valuePropertyIndex) == 11 &&
            liveState.GetNodeType(1) == nameof(PackedTestNode),
        "Scene state must expose strongly typed stored properties and node types.");
    Expect<InvalidCastException>(() => liveState.GetNodePropertyValue<string>(1, valuePropertyIndex),
        "Scene state must reject an incompatible requested property type.");
    Expect<ArgumentOutOfRangeException>(() => liveState.GetNodeName(3),
        "Scene state must reject an invalid node index.");
    Expect<ArgumentOutOfRangeException>(() => liveState.GetNodePropertyName(1, 999),
        "Scene state must reject an invalid property index.");

    Expect<ArgumentNullException>(() => scene.Pack(null!),
        "Packing a null root must be rejected.");
    Require(scene.CanInstantiate() && liveState.GetNodeCount() == 3,
        "Rejecting a null pack root must preserve the previous scene.");
    root.Dispose();

    using (var instance = (PackedTestNode)scene.Instantiate())
    {
        var instanceChild = (PackedTestNode)instance.Children[0];
        var instanceGrandchild = (PackedTestNode)instanceChild.Children[0];
        Require(instance.Parent is null && instance.Tree is null && instance.Name == "Root" && instance.Value == 7 &&
                instance.TranslationDomain == "scene" && instance.SceneFilePath == scene.ResourcePath &&
                instance.SceneNotifications == 1 && instanceChild.SceneNotifications == 0 &&
                instanceGrandchild.SceneNotifications == 0 && ReferenceEquals(instanceChild.Owner, instance) &&
                ReferenceEquals(instanceGrandchild.Owner, instance) && instanceChild.IsInGroup("persistent") &&
                !instanceChild.IsInGroup("runtime-only") && instance.FindChild("RuntimeOnly") is null,
            "Instantiation must restore properties, owners, persistent groups, source path, and root-only notification.");
        Require(instance.Data is not null && instanceChild.Data is not null &&
                ReferenceEquals(instance.Data, instanceChild.Data) && !ReferenceEquals(instance.Data, local) &&
                instance.Data.Value == 17 && instance.Data.SetupCount == 1 &&
                ReferenceEquals(instance.Data.GetLocalScene(), instance) && instance.Data.Child is not null &&
                !ReferenceEquals(instance.Data.Child, nestedLocal) && instance.Data.Child.Value == 31 &&
                instance.Data.Child.SetupCount == 1 && ReferenceEquals(instance.Data.Child.GetLocalScene(), instance),
            "Scene-local resource duplication must preserve aliases, include nested external local resources, and set up once.");
    }
    Require(local.SetupCount == 0 && nestedLocal.SetupCount == 0,
        "Instantiating a scene must not set up source resources.");

    using (var second = (PackedTestNode)scene.Instantiate())
    {
        Require(second.Data is not null && !ReferenceEquals(second.Data, local),
            "Each scene instance must receive an independent scene-local resource graph.");
    }

    using (var duplicate = (PackedScene)scene.Duplicate())
    using (var duplicateInstance = (PackedTestNode)duplicate.Instantiate())
    {
        Require(duplicate.CanInstantiate() && duplicateInstance.Value == 7 && duplicate.GetState().GetPath().Length == 0,
            "Packed-scene duplication must preserve immutable scene data while clearing resource identity.");
    }

    using (var failingLocal = new PackedTestResource { ResourceLocalToScene = true, ThrowOnSetup = true })
    {
        var failingSetupRoot = new PackedTestNode { Name = "FailingSetup", Data = failingLocal };
        scene.Pack(failingSetupRoot);
        failingSetupRoot.Dispose();
        PackedTestResource.Created.Clear();
        Expect<AggregateException>(() => scene.Instantiate(),
            "A scene-local setup failure must abort instantiation.");
        Require(PackedTestResource.Created.Count == 1 && PackedTestResource.Created.All(resource => resource.IsDisposed),
            "A scene-local setup failure must dispose every partial resource duplicate.");
    }

    var replacementRoot = new PackedTestNode { Name = "Replacement", Value = 23 };
    scene.Pack(replacementRoot);
    replacementRoot.Dispose();
    Require(liveState.GetNodeCount() == 1 && liveState.GetNodeName(0) == "Replacement",
        "A previously returned live scene state must observe a successful repack.");

    var unsupportedRoot = new PackedTestNode { Name = "UnsupportedRoot" };
    var unsupportedChild = new UnsupportedPackedNode { Name = "UnsupportedChild" };
    unsupportedRoot.AddChild(unsupportedChild);
    unsupportedChild.Owner = unsupportedRoot;
    Expect<NotSupportedException>(() => scene.Pack(unsupportedRoot),
        "Packing must reject a derived node that has no explicit reusable factory.");
    Require(!scene.CanInstantiate() && liveState.GetNodeCount() == 0,
        "A failure after capture starts must leave the packed scene empty.");
    unsupportedRoot.Dispose();

    var movingRoot = new MovingCaptureNode { Name = "Moving" };
    using var destination = new Entity { Name = "Destination" };
    MovingCaptureNode.Destination = destination;
    Expect<InvalidOperationException>(() => scene.Pack(movingRoot),
        "A stored-property getter must not move a captured node through another parent.");
    Require(movingRoot.Parent is null && destination.Children.Count == 0 && !scene.CanInstantiate(),
        "Rejected capture-time hierarchy mutation must leave both hierarchies and packed state unchanged.");
    movingRoot.Dispose();
    MovingCaptureNode.Destination = null;

    var capturingFactoryRoot = new CapturingPackedFactoryNode { Name = "CapturingFactory" };
    Expect<InvalidOperationException>(() => scene.Pack(capturingFactoryRoot),
        "Packing must reject a scene factory that captures source state.");
    Require(!scene.CanInstantiate(),
        "Rejecting a capturing scene factory must leave the packed scene empty.");
    capturingFactoryRoot.Dispose();

    var wrongFactoryRoot = new WrongPackedFactoryNode { Name = "WrongFactory" };
    scene.Pack(wrongFactoryRoot);
    wrongFactoryRoot.Dispose();
    WrongPackedFactoryNode.LastCreated = null;
    Expect<InvalidOperationException>(() => scene.Instantiate(),
        "Instantiation must reject a factory result of the wrong runtime type.");
    Require(WrongPackedFactoryNode.LastCreated is { IsDisposed: true },
        "Instantiation rollback must dispose a rejected factory result.");

    var activeFactorySource = new ActiveFactoryPackedNode { Name = "ActiveFactory" };
    scene.Pack(activeFactorySource);
    activeFactorySource.Dispose();
    ActiveFactoryPackedNode.ActivationError = null;
    ActiveFactoryPackedNode.ExistingTreeActivationError = null;
    var activeFactoryDestination = new Entity { Name = "ActiveFactoryDestination" };
    using (var activeFactoryTree = new SceneTree(activeFactoryDestination))
    {
        ActiveFactoryPackedNode.Destination = activeFactoryDestination;
        using var activeFactoryInstance = scene.Instantiate();
        Require(ActiveFactoryPackedNode.ActivationError is InvalidOperationException &&
                ActiveFactoryPackedNode.ExistingTreeActivationError is AggregateException existingTreeError &&
                existingTreeError.Flatten().InnerExceptions.Any(error => error is InvalidOperationException) &&
                activeFactoryInstance is ActiveFactoryPackedNode { EnterCount: 0, Parent: null, Tree: null } &&
                activeFactoryDestination.Children.Count == 0,
            "A node factory must not activate its result in a new or existing tree before returning it.");
        ActiveFactoryPackedNode.Destination = null;
    }

    var escapingSource = new EscapingPackedNode { Name = "Escaping" };
    using var escapeDestination = new Entity { Name = "EscapeDestination" };
    scene.Pack(escapingSource);
    escapingSource.Dispose();
    EscapingPackedNode.Destination = escapeDestination;
    EscapingPackedNode.LastCreated = null;
    Expect<InvalidOperationException>(() => scene.Instantiate(),
        "Instantiation must reject a callback that moves a created root into an external hierarchy.");
    Require(escapeDestination.Children.Count == 0 && EscapingPackedNode.LastCreated is { IsDisposed: true },
        "Failed instantiation must detach and dispose a node that escaped through a callback.");
    EscapingPackedNode.Destination = null;

    var treeEscapingSource = new TreeEscapingPackedNode { Name = "TreeEscaping" };
    scene.Pack(treeEscapingSource);
    treeEscapingSource.Dispose();
    TreeEscapingPackedNode.LastCreated = null;
    Expect<InvalidOperationException>(() => scene.Instantiate(),
        "A scene-instantiation callback must not activate its unfinished root in a SceneTree.");
    Require(TreeEscapingPackedNode.LastCreated is { IsDisposed: true, Tree: null },
        "Rejecting premature SceneTree activation must leave no live escaped root.");

    var activeEscapeSource = new ActiveTreeEscapingPackedNode { Name = "ActiveTreeEscaping" };
    scene.Pack(activeEscapeSource);
    activeEscapeSource.Dispose();
    var activeEscapeRoot = new Entity { Name = "ActiveEscapeDestination" };
    using (var activeEscapeTree = new SceneTree(activeEscapeRoot))
    {
        ActiveTreeEscapingPackedNode.Destination = activeEscapeRoot;
        ActiveTreeEscapingPackedNode.LastCreated = null;
        var activeEscapeError = Capture(() => scene.Instantiate());
        Require(activeEscapeError is AggregateException activeEscapeAggregate &&
                activeEscapeAggregate.Flatten().InnerExceptions.Any(error => error is InvalidOperationException) &&
                activeEscapeRoot.Children.Count == 0 &&
                ActiveTreeEscapingPackedNode.LastCreated is { IsDisposed: true, Tree: null, EnterCount: 0 },
            "An unfinished scene instance must not enter an active tree, run enter callbacks, or remain attached.");
        ActiveTreeEscapingPackedNode.Destination = null;
    }

    var returningSource = new SourceReturningPackedNode { Name = "SourceReturning" };
    SourceReturningPackedNode.Source = returningSource;
    scene.Pack(returningSource);
    Expect<InvalidOperationException>(() => scene.Instantiate(),
        "A factory must not return its captured source node.");
    Require(!returningSource.IsDisposed && returningSource.Parent is null,
        "Rejecting a source-returning factory must not dispose the source.");
    SourceReturningPackedNode.Source = null;
    returningSource.Dispose();

    var singletonSource = new SingletonPackedNode { Name = "Singleton" };
    SingletonPackedNode.Cached = null;
    scene.Pack(singletonSource);
    singletonSource.Dispose();
    using (var singletonInstance = scene.Instantiate())
    {
        Expect<InvalidOperationException>(() => scene.Instantiate(),
            "A factory must not issue the same live node to two scene instances.");
        Require(!singletonInstance.IsDisposed,
            "Rejecting a reused factory result must not dispose the already issued instance.");
    }
    SingletonPackedNode.Cached = null;

    var raceRoot = new PackedTestNode { Name = "RaceRoot", Value = 47 };
    scene.Pack(raceRoot);
    for (var index = 0; index < 64; index++)
    {
        var racedState = scene.GetState();
        Parallel.Invoke(racedState.Dispose, () => scene.Pack(raceRoot));
        Require(!scene.GetState().IsDisposed && scene.CanInstantiate(),
            "Disposing a cached scene state must not break a concurrent repack.");
    }
    Parallel.For(0, 64, index => scene.ResourcePath = $"{prefix}/concurrent-{index}.scene");
    Require(scene.GetState().GetPath() == scene.ResourcePath,
        "Concurrent path callbacks must leave scene-state path synchronized with the resource.");
    raceRoot.Dispose();

    scene.ResourcePath = $"{prefix}/final.scene";
    var survivingState = scene.GetState();
    scene.Dispose();
    Require(survivingState.GetNodeCount() == 1 && survivingState.GetPath() == $"{prefix}/final.scene",
        "An external scene-state reference must survive disposal of its packed-scene resource.");
    survivingState.Dispose();
}

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static bool NearlyEqual(float left, float right, float epsilon = 0.0001f) => System.MathF.Abs(left - right) <= epsilon;

static bool DoubleNearlyEqual(double left, double right, double epsilon = 0.0000001d) => Math.Abs(left - right) <= epsilon;

static bool VectorNearlyEqual(Vector2 left, Vector2 right, float epsilon = 0.0001f) => left.DistanceTo(right) <= epsilon;

static void Expect<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

static Exception? Capture(Action action)
{
    try
    {
        action();
        return null;
    }
    catch (Exception error)
    {
        return error;
    }
}

sealed class CyclicConfigValue
{
    public CyclicConfigValue? Next { get; set; }
}

sealed record ConfigProfile(string Name, int Level);

static class TestNativeLinks
{
    [DllImport("libc", EntryPoint = "link", SetLastError = true)]
    internal static extern int CreateHardLink(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string existingPath,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string newPath);
}

sealed class ColorPackedNode : Entity
{
    private static readonly PropertyDescriptor<ColorPackedNode, Color> TintProperty = new(
        nameof(Tint),
        node => node.Tint,
        (node, value) => node.Tint = value,
        _ => Colors.White,
        stored: true);
    private static readonly PropertyDescriptor<ColorPackedNode, Rect2> BoundsProperty = new(
        nameof(Bounds),
        node => node.Bounds,
        (node, value) => node.Bounds = value,
        _ => default,
        stored: true);
    private static readonly PropertyDescriptor<ColorPackedNode, Rect2i> BoundsIProperty = new(
        nameof(BoundsI),
        node => node.BoundsI,
        (node, value) => node.BoundsI = value,
        _ => default,
        stored: true);
    private static readonly PropertyDescriptor<ColorPackedNode, Transform> TransformProperty = new(
        nameof(PackedTransform),
        node => node.PackedTransform,
        (node, value) => node.PackedTransform = value,
        _ => Transform.Identity,
        stored: true);
    private static readonly PropertyDescriptor<ColorPackedNode, Vector2> Vector2Property = new(
        nameof(PackedVector2),
        node => node.PackedVector2,
        (node, value) => node.PackedVector2 = value,
        _ => Vector2.Zero,
        stored: true);
    private static readonly PropertyDescriptor<ColorPackedNode, Vector2i> Vector2iProperty = new(
        nameof(PackedVector2i),
        node => node.PackedVector2i,
        (node, value) => node.PackedVector2i = value,
        _ => Vector2i.Zero,
        stored: true);
    private static readonly PropertyDescriptor<ColorPackedNode, Vector3> Vector3Property = new(
        nameof(PackedVector3), node => node.PackedVector3, (node, value) => node.PackedVector3 = value,
        _ => Vector3.Zero, stored: true);
    private static readonly PropertyDescriptor<ColorPackedNode, Vector3i> Vector3iProperty = new(
        nameof(PackedVector3i), node => node.PackedVector3i, (node, value) => node.PackedVector3i = value,
        _ => Vector3i.Zero, stored: true);
    private static readonly PropertyDescriptor<ColorPackedNode, Vector4> Vector4Property = new(
        nameof(PackedVector4),
        node => node.PackedVector4,
        (node, value) => node.PackedVector4 = value,
        _ => Vector4.Zero,
        stored: true);
    private static readonly PropertyDescriptor<ColorPackedNode, Vector4i> Vector4iProperty = new(
        nameof(PackedVector4i),
        node => node.PackedVector4i,
        (node, value) => node.PackedVector4i = value,
        _ => Vector4i.Zero,
        stored: true);

    private Color _tint = Colors.White;
    private Rect2 _bounds;
    private Rect2i _boundsI;
    private Transform _transform = Transform.Identity;
    private Vector2 _vector2;
    private Vector2i _vector2I;
    private Vector3 _vector3;
    private Vector3i _vector3I;
    private Vector4 _vector4;
    private Vector4i _vector4I;

    public Color Tint
    {
        get => _tint;
        set
        {
            EnsureMutable();
            _tint = value;
        }
    }

    public Rect2 Bounds
    {
        get => _bounds;
        set
        {
            EnsureMutable();
            _bounds = value;
        }
    }

    public Rect2i BoundsI
    {
        get => _boundsI;
        set
        {
            EnsureMutable();
            _boundsI = value;
        }
    }

    public Transform PackedTransform
    {
        get => _transform;
        set
        {
            EnsureMutable();
            _transform = value;
        }
    }

    public Vector2 PackedVector2
    {
        get => _vector2;
        set
        {
            EnsureMutable();
            _vector2 = value;
        }
    }

    public Vector2i PackedVector2i
    {
        get => _vector2I;
        set
        {
            EnsureMutable();
            _vector2I = value;
        }
    }

    public Vector4 PackedVector4
    {
        get => _vector4;
        set
        {
            EnsureMutable();
            _vector4 = value;
        }
    }

    public Vector3 PackedVector3
    {
        get => _vector3;
        set { EnsureMutable(); _vector3 = value; }
    }

    public Vector3i PackedVector3i
    {
        get => _vector3I;
        set { EnsureMutable(); _vector3I = value; }
    }

    public Vector4i PackedVector4i
    {
        get => _vector4I;
        set
        {
            EnsureMutable();
            _vector4I = value;
        }
    }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors()
            .Append(TintProperty)
            .Append(BoundsProperty)
            .Append(BoundsIProperty)
            .Append(TransformProperty)
            .Append(Vector2Property)
            .Append(Vector2iProperty)
            .Append(Vector3Property)
            .Append(Vector3iProperty)
            .Append(Vector4Property)
            .Append(Vector4iProperty);

    private static Entity CreateNode() => new ColorPackedNode();
}

sealed class PackedTestNode : Entity
{
    private static readonly PropertyDescriptor<PackedTestNode, int> ValueProperty = new(
        nameof(Value),
        node => node.Value,
        (node, value) => node.Value = value,
        _ => 0,
        stored: true);
    private static readonly PropertyDescriptor<PackedTestNode, PackedTestResource?> DataProperty = new(
        nameof(Data),
        node => node.Data,
        (node, value) => node.Data = value,
        _ => null,
        stored: true);

    private int _value;
    private PackedTestResource? _data;

    public int Value
    {
        get => _value;
        set
        {
            EnsureMutable();
            _value = value;
        }
    }

    public PackedTestResource? Data
    {
        get => _data;
        set
        {
            EnsureMutable();
            _data = value;
        }
    }

    public int SceneNotifications { get; private set; }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Append(ValueProperty).Append(DataProperty);

    protected override void OnNotification(int what)
    {
        if (what == NotificationSceneInstantiated)
            SceneNotifications++;

        base.OnNotification(what);
    }

    private static Entity CreateNode() => new PackedTestNode();
}

sealed class PackedTestResource : Resource
{
    public static List<PackedTestResource> Created { get; } = [];

    public int Value { get; set; }

    public PackedTestResource? Child { get; set; }

    public int SetupCount { get; private set; }

    public bool ThrowOnSetup { get; set; }

    protected override Resource CreateDuplicateInstance()
    {
        var resource = new PackedTestResource();
        Created.Add(resource);
        return resource;
    }

    protected override void CopyCustomStateTo(
        Resource target,
        bool deep,
        DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource,
        Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var typedTarget = (PackedTestResource)target;
        typedTarget.Value = Value;
        typedTarget.Child = (PackedTestResource?)duplicateSubresource(Child);
        typedTarget.ThrowOnSetup = ThrowOnSetup;
    }

    protected override void OnSetupLocalToScene()
    {
        SetupCount++;
        if (ThrowOnSetup)
            throw new InvalidOperationException("expected local setup failure");
    }
}

sealed class UnsupportedPackedNode : Entity
{
}

sealed class MovingCaptureNode : Entity
{
    private static readonly PropertyDescriptor<MovingCaptureNode, int> MovingProperty = new(
        "MovingValue",
        node => MoveDuringCapture(node),
        (node, _) => node.EnsureMutable(),
        _ => 0,
        stored: true);

    public static Entity? Destination { get; set; }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Append(MovingProperty);

    private static int MoveDuringCapture(MovingCaptureNode node)
    {
        Destination?.AddChild(node);
        return 0;
    }

    private static Entity CreateNode() => new MovingCaptureNode();
}

sealed class WrongPackedFactoryNode : Entity
{
    public static Entity? LastCreated { get; set; }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    private static Entity CreateNode() => LastCreated = new Entity();
}

sealed class CapturingPackedFactoryNode : Entity
{
    protected override Func<Node> CreateSceneInstanceFactory() =>
        () => new CapturingPackedFactoryNode { Name = Name };
}

sealed class ActiveFactoryPackedNode : Entity
{
    public static Exception? ActivationError { get; set; }

    public static Entity? Destination { get; set; }

    public static Exception? ExistingTreeActivationError { get; set; }

    public int EnterCount { get; private set; }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    private static Entity CreateNode()
    {
        var node = new ActiveFactoryPackedNode();
        ActivationError = Capture(() => _ = new SceneTree(node));
        ExistingTreeActivationError = Capture(() => Destination?.AddChild(node));
        node.Parent?.RemoveChild(node);
        return node;
    }

    protected override void OnEnterTree() => EnterCount++;

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }
}

sealed class EscapingPackedNode : Entity
{
    public static Entity? Destination { get; set; }

    public static EscapingPackedNode? LastCreated { get; set; }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    protected override void OnNotification(int what)
    {
        if (what == NotificationSceneInstantiated)
            Destination?.AddChild(this);

        base.OnNotification(what);
    }

    private static Entity CreateNode() => LastCreated = new EscapingPackedNode();
}

sealed class SourceReturningPackedNode : Entity
{
    public static SourceReturningPackedNode? Source { get; set; }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    private static Entity CreateNode() => Source!;
}

sealed class TreeEscapingPackedNode : Entity
{
    public static TreeEscapingPackedNode? LastCreated { get; set; }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    protected override void OnNotification(int what)
    {
        if (what == NotificationSceneInstantiated)
            _ = new SceneTree(this);

        base.OnNotification(what);
    }

    private static Entity CreateNode() => LastCreated = new TreeEscapingPackedNode();
}

sealed class ActiveTreeEscapingPackedNode : Entity
{
    public static Entity? Destination { get; set; }

    public static ActiveTreeEscapingPackedNode? LastCreated { get; set; }

    public int EnterCount { get; private set; }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    protected override void OnNotification(int what)
    {
        if (what == NotificationSceneInstantiated)
            Destination?.AddChild(this);

        base.OnNotification(what);
    }

    protected override void OnEnterTree() => EnterCount++;

    private static Entity CreateNode() => LastCreated = new ActiveTreeEscapingPackedNode();
}

sealed class SingletonPackedNode : Entity
{
    public static SingletonPackedNode? Cached { get; set; }

    protected override Func<Node> CreateSceneInstanceFactory() => CreateNode;

    private static Entity CreateNode() => Cached ??= new SingletonPackedNode();
}

sealed class TestResource : Resource
{
    public int Value { get; set; }

    public List<int> Numbers { get; set; } = [];

    public TestResource? First { get; set; }

    public TestResource? Second { get; set; }

    public TestResource? External { get; set; }

    public TestResource? Always { get; set; }

    public TestResource? Never { get; set; }

    public int Transient { get; set; }

    public int ResetCount { get; private set; }

    public int PathCacheSetCount { get; private set; }

    protected override Resource CreateDuplicateInstance() => new TestResource();

    protected override void CopyCustomStateTo(
        Resource target,
        bool deep,
        DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource,
        Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var typedTarget = (TestResource)target;
        typedTarget.Value = Value;
        typedTarget.Numbers = deep ? [.. Numbers] : Numbers;
        typedTarget.First = (TestResource?)duplicateSubresource(First);
        typedTarget.Second = (TestResource?)duplicateSubresource(Second);
        typedTarget.External = (TestResource?)duplicateSubresource(External);
        typedTarget.Always = (TestResource?)forceDuplicateSubresource(Always);
        typedTarget.Never = Never;
    }

    protected override void OnResetState()
    {
        ResetCount++;
        Transient = 0;
    }

    protected override void OnPathCacheSet(string path) => PathCacheSetCount++;
}

sealed class SetupProbeResource : Resource
{
    public List<string> Order { get; } = [];

    public bool ThrowInHook { get; init; }

    protected override void OnSetupLocalToScene()
    {
        Order.Add("hook");
        if (ThrowInHook)
            throw new InvalidOperationException("expected setup failure");
    }
}

sealed class ResetFailureResource : Resource
{
    public bool ThrowOnReset { get; init; }

    protected override void OnResetState()
    {
        if (ThrowOnReset)
            throw new InvalidOperationException("expected reset failure");
    }
}

sealed class UnsupportedResource : Resource
{
}

sealed class WrongFactoryResource : Resource
{
    public static Resource? LastCreated { get; set; }

    protected override Resource CreateDuplicateInstance() => LastCreated = new Resource();

    protected override void CopyCustomStateTo(
        Resource target,
        bool deep,
        DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource,
        Func<Resource?, Resource?> forceDuplicateSubresource)
    {
    }
}

sealed class SelfFactoryResource : Resource
{
    protected override Resource CreateDuplicateInstance() => this;

    protected override void CopyCustomStateTo(
        Resource target,
        bool deep,
        DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource,
        Func<Resource?, Resource?> forceDuplicateSubresource)
    {
    }
}

sealed class DirtyCopyResource : Resource
{
    public static DirtyCopyResource? LastCreated { get; set; }

    protected override Resource CreateDuplicateInstance() => LastCreated = new DirtyCopyResource();

    protected override void CopyCustomStateTo(
        Resource target,
        bool deep,
        DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource,
        Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        target.ResourcePath = $"memory://dirty-copy/{Guid.NewGuid():N}";
    }
}

sealed class FailingDuplicateResource : Resource
{
    public static List<FailingDuplicateResource> Created { get; } = [];

    public FailingDuplicateResource? Child { get; set; }

    public bool ThrowOnCopy { get; init; }

    protected override Resource CreateDuplicateInstance()
    {
        var created = new FailingDuplicateResource();
        Created.Add(created);
        return created;
    }

    protected override void CopyCustomStateTo(
        Resource target,
        bool deep,
        DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource,
        Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        ((FailingDuplicateResource)target).Child = (FailingDuplicateResource?)duplicateSubresource(Child);
        if (ThrowOnCopy)
            throw new InvalidOperationException("expected duplication failure");
    }
}

sealed class CleanupFailureResource : Resource
{
    public static CleanupFailureResource? LastCreated { get; private set; }

    public bool ThrowOnDispose { get; init; }

    protected override Resource CreateDuplicateInstance() =>
        LastCreated = new CleanupFailureResource { ThrowOnDispose = true };

    protected override void CopyCustomStateTo(
        Resource target,
        bool deep,
        DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource,
        Func<Resource?, Resource?> forceDuplicateSubresource) =>
        throw new InvalidOperationException("expected duplication failure");

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && ThrowOnDispose)
            throw new ArgumentException("expected cleanup failure");
    }
}

sealed class EventSource
{
    public event Action? Pulse;

    public event Action<int>? Value;

    public event Action<EventSource, int>? Pair;

    public void RaisePulse() => Pulse?.Invoke();

    public void RaiseValue(int value) => Value?.Invoke(value);

    public void RaisePair(int value) => Pair?.Invoke(this, value);
}

sealed class TestMainLoop : MainLoop
{
    public bool DisposeDuringFinalize { get; set; }

    public bool DisposeDuringInitialize { get; set; }

    public bool DisposeDuringProcess { get; set; }

    public Exception? DisposeError { get; private set; }

    public int FinalizeCount { get; private set; }

    public bool FinalizeDuringProcess { get; set; }

    public Exception? FinalizeError { get; private set; }

    public Exception? FinalizeDisposeError { get; private set; }

    public Exception? InitializeDisposeError { get; private set; }

    public Exception? InitializeReentryError { get; private set; }

    public List<string> Log { get; } = [];

    public List<int> Notifications { get; } = [];

    public bool PhysicsResult { get; set; }

    public bool ProcessResult { get; set; }

    public bool ReenterInitialize { get; set; }

    public bool ReenterProcess { get; set; }

    public Exception? ReentryError { get; private set; }

    public bool ThrowOnFinalize { get; set; }

    public bool ThrowOnInitialize { get; set; }

    public bool ThrowOnProcess { get; set; }

    public void PublishPermission(string permission, bool granted) => NotifyRequestPermissionsResult(permission, granted);

    protected override void OnInitialize()
    {
        Log.Add("initialize");

        if (ReenterInitialize)
            InitializeReentryError = Capture(Initialize);

        if (DisposeDuringInitialize)
            InitializeDisposeError = Capture(Dispose);

        if (ThrowOnInitialize)
            throw new InvalidOperationException("expected initialization failure");
    }

    protected override bool OnProcess(double delta)
    {
        Log.Add($"process:{delta}");

        if (ReenterProcess)
            ReentryError = Capture(() => Process(delta));

        if (DisposeDuringProcess)
            DisposeError = Capture(Dispose);

        if (FinalizeDuringProcess)
            FinalizeError = Capture(FinalizeLoop);

        if (ThrowOnProcess)
            throw new InvalidOperationException("expected process failure");

        return ProcessResult;
    }

    protected override bool OnPhysicsProcess(double delta)
    {
        Log.Add($"physics:{delta}");
        return PhysicsResult;
    }

    protected override void OnFinalize()
    {
        FinalizeCount++;
        Log.Add("finalize");

        if (DisposeDuringFinalize)
            FinalizeDisposeError = Capture(Dispose);

        if (ThrowOnFinalize)
            throw new InvalidOperationException("expected finalization failure");
    }

    protected override void OnNotification(int what)
    {
        Notifications.Add(what);
        base.OnNotification(what);
    }

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }
}

sealed class EmptyMainLoop : MainLoop
{
}

sealed class EngineProbeMainLoop : MainLoop
{
    public Exception? AdvanceReentryError { get; private set; }

    public int FinalizeCount { get; private set; }

    public int InitializeCount { get; private set; }

    public MainLoop? MainLoopDuringFinalize { get; private set; }

    public MainLoop? MainLoopDuringInitialize { get; private set; }

    public List<string> Order { get; } = [];

    public bool ObservedPhysicsFrameState { get; private set; }

    public bool ObservedProcessFrameState { get; private set; }

    public List<double> PhysicsDeltas { get; } = [];

    public bool PhysicsResult { get; set; }

    public List<double> ProcessDeltas { get; } = [];

    public bool ProcessResult { get; set; }

    public bool ReenterEngine { get; set; }

    public bool ReenterDuringFinalize { get; set; }

    public bool ReenterDuringInitialize { get; set; }

    public Exception? StartDuringFinalizeError { get; private set; }

    public Exception? StartDuringInitializeError { get; private set; }

    public Exception? StopDuringFinalizeError { get; private set; }

    public Exception? StopReentryError { get; private set; }

    public bool ThrowOnFinalize { get; set; }

    public bool ThrowOnInitialize { get; set; }

    public bool ThrowOnPhysics { get; set; }

    public bool ThrowOnProcess { get; set; }

    protected override void OnInitialize()
    {
        InitializeCount++;
        MainLoopDuringInitialize = Engine.Instance.MainLoop;

        if (ReenterDuringInitialize)
            StartDuringInitializeError = Capture(() => Engine.Instance.Start(this));

        if (ThrowOnInitialize)
            throw new InvalidOperationException("expected engine initialization failure");
    }

    protected override bool OnPhysicsProcess(double delta)
    {
        PhysicsDeltas.Add(delta);
        Order.Add("physics");
        ObservedPhysicsFrameState |= Engine.Instance.IsInPhysicsFrame;

        if (ThrowOnPhysics)
            throw new InvalidOperationException("expected engine physics failure");

        return PhysicsResult;
    }

    protected override bool OnProcess(double delta)
    {
        ProcessDeltas.Add(delta);
        Order.Add("process");
        ObservedProcessFrameState |= Engine.Instance.IsInPhysicsFrame;

        if (ReenterEngine)
        {
            AdvanceReentryError = Capture(() => Engine.Instance.AdvanceFrame(0d));
            StopReentryError = Capture(Engine.Instance.Stop);
        }

        if (ThrowOnProcess)
            throw new InvalidOperationException("expected engine process failure");

        return ProcessResult;
    }

    protected override void OnFinalize()
    {
        FinalizeCount++;
        MainLoopDuringFinalize = Engine.Instance.MainLoop;

        if (ReenterDuringFinalize)
        {
            StartDuringFinalizeError = Capture(() => Engine.Instance.Start(this));
            StopDuringFinalizeError = Capture(Engine.Instance.Stop);
        }

        if (ThrowOnFinalize)
            throw new InvalidOperationException("expected engine finalization failure");
    }

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }
}

sealed class SystemNotificationNode : Entity
{
    private readonly List<string> _log;

    public SystemNotificationNode(string name, List<string> log)
    {
        Name = name;
        _log = log;
    }

    public bool ThrowOnSystem { get; set; }

    protected override void OnNotification(int what)
    {
        if (what is >= MainLoop.NotificationOsMemoryWarning and <= MainLoop.NotificationApplicationPipModeExited)
        {
            _log.Add($"{Name}:{what}");

            if (ThrowOnSystem)
                throw new InvalidOperationException("expected system notification failure");
        }

        base.OnNotification(what);
    }
}

sealed class FinalizeOnEnterNode : Entity
{
    public Exception? FinalizeError { get; private set; }

    protected override void OnEnterTree()
    {
        FinalizeError = Capture(Tree!.FinalizeLoop);
        base.OnEnterTree();
    }

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }
}

sealed class TestObject : ElectronObject
{
    private static readonly PropertyDescriptor<TestObject, int> ValueProperty = new(
        nameof(Value),
        instance => instance.Value,
        (instance, value) => instance.Value = value,
        _ => 0,
        (_, value) => value >= 0);

    private int _disposeCount;

    public int DisposeCount => Volatile.Read(ref _disposeCount);

    public bool CanTranslateDuringPreDelete { get; private set; }

    public List<int> Notifications { get; } = [];

    public int Value { get; private set; }

    public void Use() => ThrowIfDisposed();

    public void AnnouncePropertyListChanged() => NotifyPropertyListChanged();

    public void AnnounceScriptChanged() => NotifyScriptChanged();

    protected override void OnNotification(int what)
    {
        if (what == NotificationPreDelete)
            CanTranslateDuringPreDelete = CanTranslateMessages;

        Notifications.Add(what);
        base.OnNotification(what);
    }

    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(ValueProperty);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Interlocked.Increment(ref _disposeCount);

        base.Dispose(disposing);
    }
}

sealed class RecordingNode : Entity
{
    private readonly List<string> _lifecycle;

    public RecordingNode(string name, List<string> lifecycle)
    {
        Name = name;
        _lifecycle = lifecycle;
    }

    protected override void OnEnterTree() => _lifecycle.Add($"enter:{Name}");

    protected override void OnExitTree() => _lifecycle.Add($"exit:{Name}");

    protected override void OnReady() => _lifecycle.Add($"ready:{Name}");
}

sealed class DuplicatePropertyObject : ElectronObject
{
    private static readonly PropertyDescriptor<DuplicatePropertyObject, int> First = new("Duplicate", _ => 1);
    private static readonly PropertyDescriptor<DuplicatePropertyObject, int> Second = new("Duplicate", _ => 2);

    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(First).Append(Second);
}

sealed class TransformNode : Entity
{
    public List<int> Notifications { get; } = [];

    protected override void OnNotification(int what)
    {
        Notifications.Add(what);
        base.OnNotification(what);
    }
}

sealed class ProcessingNode : Entity
{
    private readonly List<string> _log;

    public ProcessingNode(string name, List<string> log)
    {
        Name = name;
        _log = log;
    }

    protected override void OnProcess(double delta) => _log.Add($"process:{Name}:{delta}");

    protected override void OnPhysicsProcess(double delta) => _log.Add($"physics:{Name}:{delta}");
}

sealed class ProcessingTimer(List<string> log) : EngineTimer
{
    protected override void OnProcess(double delta) => log.Add($"process:timer:{delta}");
}

sealed class GroupNode : Entity
{
    private readonly List<string> _log;

    public GroupNode(string name, List<string> log)
    {
        Name = name;
        _log = log;
    }

    protected override void OnNotification(int what)
    {
        if (what == 9_001)
            _log.Add($"notify:{Name}:{what}");

        base.OnNotification(what);
    }
}

sealed class FailingLifecycleNode : Entity
{
    public bool AddChildOnEnter { get; init; }

    public bool AddChildOnReady { get; init; }

    public bool CreateTimerOnEnter { get; init; }

    public bool CreateTweenOnEnter { get; init; }

    public bool ThrowOnEnter { get; init; }

    public bool ThrowOnReady { get; init; }

    public bool ThrowOnExit { get; init; }

    public bool ThrowOnDispose { get; init; }

    public Entity? AddedChild { get; private set; }

    public SceneTreeTimer? CreatedTimer { get; private set; }

    public Tween? CreatedTween { get; private set; }

    public SceneTree? CapturedTree { get; private set; }

    public Exception? TimerCreationDuringRollbackError { get; private set; }

    protected override void OnEnterTree()
    {
        CapturedTree = Tree;

        if (CreateTimerOnEnter)
        {
            CreatedTimer = Tree!.CreateTimer(1d);
            CreatedTimer.Disposed += _ => TimerCreationDuringRollbackError = CaptureTimerCreation();
        }

        if (CreateTweenOnEnter)
        {
            CreatedTween = Tree!.CreateTween();
            CreatedTween.TweenInterval(1d);
        }

        if (AddChildOnEnter)
        {
            AddedChild = new Entity { Name = "added-during-enter" };
            AddChild(AddedChild);
        }

        if (ThrowOnEnter)
            throw new InvalidOperationException("expected enter failure");
    }

    private Exception? CaptureTimerCreation()
    {
        try
        {
            CapturedTree!.CreateTimer(1d);
            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }

    protected override void OnReady()
    {
        if (AddChildOnReady)
        {
            AddedChild = new Entity { Name = "added-during-ready" };
            AddChild(AddedChild);
        }

        if (ThrowOnReady)
            throw new InvalidOperationException("expected ready failure");
    }

    protected override void OnExitTree()
    {
        if (ThrowOnExit)
            throw new InvalidOperationException("expected exit failure");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && ThrowOnDispose)
            throw new InvalidOperationException("expected node disposal failure");
    }
}

sealed class TeardownQueueNode : Entity
{
    public bool QueueWasRejected { get; private set; }

    public bool PauseWasRejected { get; private set; }

    protected override void OnExitTree()
    {
        try
        {
            Tree!.Defer(static () => { });
        }
        catch (ObjectDisposedException)
        {
            QueueWasRejected = true;
        }

        try
        {
            Tree!.Paused = true;
        }
        catch (ObjectDisposedException)
        {
            PauseWasRejected = true;
        }
    }
}

sealed class ReentrantPauseNode : Entity
{
    public Exception? ReentryError { get; private set; }

    protected override void OnNotification(int what)
    {
        if (what == NotificationPaused)
        {
            try
            {
                Tree!.Paused = false;
            }
            catch (Exception error)
            {
                ReentryError = error;
                throw;
            }
        }

        base.OnNotification(what);
    }
}

sealed class PauseMutationNode : Entity
{
    public Entity? Target { get; set; }

    public int PauseNotifications { get; private set; }

    protected override void OnNotification(int what)
    {
        if (what == NotificationPaused)
        {
            PauseNotifications++;
            Target?.Dispose();
        }

        base.OnNotification(what);
    }
}

sealed class PauseReparentNode : Entity
{
    public Entity? Target { get; set; }

    public Entity? Destination { get; set; }

    protected override void OnNotification(int what)
    {
        if (what == NotificationPaused)
            Target!.Reparent(Destination!);

        base.OnNotification(what);
    }
}

sealed class PauseBarrierNode : Entity
{
    public Exception? FlushError { get; private set; }

    public Exception? DisposeError { get; private set; }

    protected override void OnNotification(int what)
    {
        if (what == NotificationPaused)
        {
            FlushError = Capture(Tree!.FlushDeferred);
            DisposeError = Capture(Tree.Dispose);
        }

        base.OnNotification(what);
    }

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }
}

sealed class InputEmulationProbeNode : Entity
{
    public List<InputEvent> Events { get; } = [];

    public bool ThrowOnEmulated { get; set; }

    public bool ThrowOnWheel { get; set; }

    public DisplayServer? DisplayPumpAttempt { get; set; }

    public Exception? DisplayPumpError { get; private set; }

    public void Clear()
    {
        foreach (var @event in Events)
            @event.Dispose();
        Events.Clear();
    }

    protected override void OnInput(InputEvent @event)
    {
        Events.Add((InputEvent)@event.Duplicate());
        if (ThrowOnEmulated && @event.Device == InputEvent.DeviceIdEmulation)
            throw new InvalidOperationException("injected synthetic input callback failure");
        if (ThrowOnWheel && @event is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp })
            throw new InvalidOperationException("injected wheel callback failure");
    }

    protected override void OnPhysicsProcess(double delta)
    {
        var display = DisplayPumpAttempt;
        if (display is null)
            return;
        DisplayPumpAttempt = null;
        try
        {
            display.ProcessEvents();
        }
        catch (Exception error)
        {
            DisplayPumpError = error;
        }
    }
}

sealed class InputProbeNode(string id, List<string> log) : Entity
{
    public string ObservedAction { get; set; } = string.Empty;

    public bool HandleInput { get; set; }

    public bool ReenterInput { get; set; }

    public bool ThrowOnInput { get; set; }

    public bool SawPressedState { get; private set; }

    public bool PhysicsSawJustPressed { get; private set; }

    public Exception? ReentryError { get; private set; }

    public InputEvent? PhysicsInputAttempt { get; set; }

    public Exception? FrameInputError { get; private set; }

    protected override void OnInput(InputEvent @event)
    {
        log.Add($"{id}:input");
        if (ObservedAction.Length != 0)
            SawPressedState |= Input.Instance.IsActionPressed(ObservedAction);

        if (ReenterInput)
        {
            using var nested = new InputEventMouseMotion();
            try
            {
                Input.Instance.ParseInputEvent(nested);
            }
            catch (Exception error)
            {
                ReentryError = error;
            }
        }

        if (HandleInput)
            Tree!.SetInputAsHandled();

        if (ThrowOnInput)
            throw new InvalidOperationException("expected input failure");
    }

    protected override void OnUnhandledKeyInput(InputEventKey @event) => log.Add($"{id}:key");

    protected override void OnUnhandledInput(InputEvent @event) => log.Add($"{id}:unhandled");

    protected override void OnPhysicsProcess(double delta)
    {
        if (ObservedAction.Length != 0)
            PhysicsSawJustPressed |= Input.Instance.IsActionJustPressed(ObservedAction);

        if (PhysicsInputAttempt is not null)
        {
            try
            {
                Input.Instance.ParseInputEvent(PhysicsInputAttempt);
            }
            catch (Exception error)
            {
                FrameInputError = error;
            }
        }
    }
}

sealed class TweenEventSource : ElectronObject
{
    public event Action? Fired;

    public int EmitCount { get; private set; }

    public void Emit()
    {
        EmitCount++;
        Fired?.Invoke();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Fired = null;
        base.Dispose(disposing);
    }
}

sealed class TweenValueHolder : ElectronObject
{
    public double Value { get; set; }

    public bool Flag { get; set; }

    public int Integer { get; set; }

    public long LargeInteger { get; set; }

    public void SetValue(double value) => Value = value;
}

sealed class PreDeleteReparentNode : Entity
{
    public Entity? Target { get; set; }

    public Entity? Destination { get; set; }

    public Exception? ReparentError { get; private set; }

    protected override void OnNotification(int what)
    {
        if (what == NotificationPreDelete)
        {
            try
            {
                Target!.Reparent(Destination!);
            }
            catch (Exception error)
            {
                ReparentError = error;
            }
        }

        base.OnNotification(what);
    }
}

sealed class ExitSiblingMutationNode : Entity
{
    public Entity? Sibling { get; set; }

    public Entity? Destination { get; set; }

    public Exception? RemoveError { get; private set; }

    public Exception? ReparentError { get; private set; }

    public Exception? DisposeError { get; private set; }

    protected override void OnExitTree()
    {
        RemoveError = Capture(() => Parent!.RemoveChild(Sibling!));
        ReparentError = Capture(() => Sibling!.Reparent(Destination!));
        DisposeError = Capture(Sibling!.Dispose);
        base.OnExitTree();
    }

    private static Exception? Capture(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }
}
