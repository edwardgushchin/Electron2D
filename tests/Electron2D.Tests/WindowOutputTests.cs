using Electron2D;
using SDL = SDL3.SDL;

internal static partial class WindowOutputTests
{
    private static nint _rootNative;
    private static bool _nativeHDR = true;
    private static float _white = 2, _headroom = 4;

    internal static void Run(string mode)
    {
        if (mode == "storage") { ReadStorage(Environment.GetEnvironmentVariable("ELECTRON2D_HDR_OUTPUT_SCENE")!); return; }
        if (mode == "managed") { Managed(); return; }
        if (mode == "transfer") { Transfer(); return; }
        if (mode == "screens") { Screens(); return; }
        Native(mode);
    }

    private static void Managed()
    {
        using var window = new Window(); using var texture = window.GetTexture();
        using var alias = (ViewportTexture)texture.Duplicate();
        Check(!window.HDROutputRequested && window.GetOutputMaxLinearValue() == 1, "Cold output policy.");
        var notices = 0; alias.Changed += _ => notices++;
        var rid = texture.GetRID();
        window.HDROutputRequested = true;
        Check(!window.UseHDR2D && texture.PixelFormat == Image.Format.Rgbah && alias.PixelFormat == Image.Format.Rgbah && notices == 1,
            "An HDR request forces effective half-float metadata while retaining canvas authoring.");
        window.HDROutputRequested = true;
        Check(notices == 1 && texture.GetRID() == rid && window.GetOutputMaxLinearValue() == 1, "Equal requests and cold output are stable.");
        using var scene = new PackedScene(); scene.Pack(window);
        using var copy = (Window)scene.Instantiate();
        Check(copy.HDROutputRequested && !copy.UseHDR2D && copy.GetTexture().PixelFormat == Image.Format.Rgbah, "Typed scene copy retains independent policies.");
        var projectRequest = ProjectSettings.Get(ProjectSettings.HDROutputRequested);
        try
        {
            ProjectSettings.Set(ProjectSettings.HDROutputRequested, true);
            using var automatic = new Window(); automatic.ApplyProjectHDROutput();
            using var explicitWindow = new Window { HDROutputRequested = false }; explicitWindow.ApplyProjectHDROutput();
            Check(automatic.HDROutputRequested && !explicitWindow.HDROutputRequested, "Project request and explicit false precedence.");
        }
        finally { ProjectSettings.Set(ProjectSettings.HDROutputRequested, projectRequest); }
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron-output-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = System.IO.Path.Combine(folder, "output.e2dscene"); ResourceSaver.Save(scene, path);
            var info = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (System.IO.Path.GetFileNameWithoutExtension(info.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                info.ArgumentList.Add(System.IO.Path.Combine(AppContext.BaseDirectory, typeof(WindowOutputTests).Assembly.GetName().Name + ".dll"));
            info.Environment["ELECTRON2D_TEST_HDR_OUTPUT"] = "storage"; info.Environment["ELECTRON2D_HDR_OUTPUT_SCENE"] = path;
            using var child = System.Diagnostics.Process.Start(info)!; var stdout = child.StandardOutput.ReadToEnd(); var stderr = child.StandardError.ReadToEnd(); child.WaitForExit();
            Check(child.ExitCode == 0, $"Fresh HDR output scene: {stdout} {stderr}");
        }
        finally { Directory.Delete(folder, recursive: true); }
        window.HDROutputRequested = false;
        Check(texture.PixelFormat == Image.Format.Rgba8 && notices == 2, "Clearing the request restores authored byte metadata.");
        window.UseHDR2D = true; window.HDROutputRequested = true; window.HDROutputRequested = false;
        Check(window.UseHDR2D && texture.PixelFormat == Image.Format.Rgbah, "Request removal retains explicit HDR canvas authoring.");
        Reject<InvalidOperationException>(() => DisplayServer.WindowGetOutputMaxLinearValue());
        window.Dispose(); Reject<ObjectDisposedException>(() => window.GetOutputMaxLinearValue());
        using var retired = new Window(); using var retiredView = retired.GetTexture();
        retiredView.Changed += _ => retired.Dispose(); retired.HDROutputRequested = true;
        Check(retired.IsDisposed, "Retirement from a metadata observer stops subsequent owner traversal.");
        Console.WriteLine("HDR output managed policy, metadata, copy, fresh-process file and lifetime checks passed.");
    }

    private static void ReadStorage(string path)
    {
        using var scene = ResourceLoader.Load<PackedScene>(path)!; using var window = (Window)scene.Instantiate();
        Check(window.HDROutputRequested && !window.UseHDR2D && window.GetTexture().PixelFormat == Image.Format.Rgbah && window.GetOutputMaxLinearValue() == 1,
            "Fresh HDR request needs no native device and retains its distinct canvas authoring.");
    }

    private sealed class Probe(Action callback) : Node
    {
        protected override void OnNotification(int what)
        { if (what == NotificationWmOutputMaxLinearValueChanged && IsInsideTree) callback(); }
    }

    private static (bool, float, float) NativeMetadata(nint window)
    {
        if (_rootNative == 0) _rootNative = window;
        return window == _rootNative ? (_nativeHDR, _white, _headroom) : (true, 1, 8);
    }

    private static void StateEvent()
    {
        var value = new SDL.Event { Window = new() { Type = SDL.EventType.WindowHDRStateChanged, WindowID = SDL.GetWindowID(_rootNative) } };
        Check(SDL.PushEvent(ref value), "Native HDR-state event enqueue.");
    }

    private static void Native(string mode)
    {
        var controlled = mode != "native";
        var compatibility = mode.StartsWith("compatibility", StringComparison.Ordinal);
        var selectedBackend = mode.StartsWith("gpu", StringComparison.Ordinal) || mode == "compatibility-gpu" ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, compatibility ? "compatibility" : "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, mode.EndsWith("-separate", StringComparison.Ordinal));
        using var world = new World(selectedBackend);
        using var image = Image.CreateEmpty(1, 1, false, compatibility ? Image.Format.Rgba8 : Image.Format.Rgbaf); image.Fill(new(2.5f, .125f, .25f, 1));
        using var source = ImageTexture.CreateFromImage(image);
        var window = new Window { Size = new(128, 80), World = world, GUIEmbedSubwindows = true };
        if (mode == "project") ProjectSettings.Set(ProjectSettings.HDROutputRequested, true);
        else window.HDROutputRequested = !compatibility;
        window.AddChild(new Sprite { Texture = source, Centered = false, Scale = new(16, 16) });
        var embedded = new Window { Size = new(16, 16), Position = new(80, 32), Borderless = true }; window.AddChild(embedded);
        var native = new Window { Size = new(48, 32), ForceNative = true }; window.AddChild(native);
        native.AddChild(new Sprite { Texture = source, Centered = false, Scale = new(16, 16) });
        using var nativeTexture = native.GetTexture();
        var shape = new CircleShape { Radius = 2 };
        var body = new RigidBody { Position = new(48, 16), LinearVelocity = new(1, 0), CanSleep = false };
        body.AddChild(new CollisionShape { Shape = shape }); window.AddChild(body);
        var bodyRID = body.GetRID(); var shapeRID = shape.GetRID();
        using var texture = window.GetTexture(); var identity = texture.GetRID();
        var order = new List<int>(); var expected = 4f; var frames = 0; long before = 0, beforeAll = 0, allocated = 0, allAllocated = 0;
        window.AddChild(new Probe(() => { Check(window.GetOutputMaxLinearValue() == (controlled ? expected : DisplayServer.WindowGetOutputMaxLinearValue()), "Value precedes Node notification."); order.Add(1); }));
        embedded.AddChild(new Probe(() => { Check(embedded.GetOutputMaxLinearValue() == (controlled ? expected : DisplayServer.WindowGetOutputMaxLinearValue()), "Embedded output value precedes notification."); order.Add(4); }));
        window.OutputMaxLinearValueChanged += value => { if (window.IsInsideTree) { Check(value == (controlled ? expected : window.GetOutputMaxLinearValue()), "Window output payload."); order.Add(2); } };
        embedded.OutputMaxLinearValueChanged += value => { if (embedded.IsInsideTree) { Check(value == (controlled ? expected : embedded.GetOutputMaxLinearValue()), "Embedded output payload."); order.Add(5); } };
        _rootNative = 0; _nativeHDR = true; _white = 2; _headroom = 4;
        if (controlled) GPUCanvasBackend.OutputForTesting = NativeMetadata;
        try
        {
            window.Ready += _ =>
            {
                var events = DisplayServer.GetWindowEvents();
                var nativeID = native.GetWindowID(); var nativeEvents = DisplayServer.GetWindowEvents(nativeID);
                events.OutputMaxLinearValueChanged += value => { Check(value == (controlled ? expected : DisplayServer.WindowGetOutputMaxLinearValue()), "Scoped output payload."); order.Add(3); };
                Check(world.PhysicsBackend == selectedBackend && world.RequestedPhysicsBackend == selectedBackend && world.PhysicsFallbackReason is null, "The requested complete physical backend actually runs without fallback.");
                if (compatibility)
                {
                    Reject<NotSupportedException>(() => window.HDROutputRequested = true);
                    Check(!window.HDROutputRequested && !DisplayServer.WindowIsHDROutputEnabled() && !DisplayServer.HasFeature(DisplayServer.Feature.HdrOutput), "Compatibility rejects before commitment.");
                }
                else
                {
                    Check(DisplayServer.WindowIsHDROutputRequested() && !window.UseHDR2D, "Requested output keeps independent authored canvas flag.");
                    if (controlled) Check(DisplayServer.WindowIsHDROutputSupported() && DisplayServer.WindowIsHDROutputEnabled() && window.GetOutputMaxLinearValue() == 4, "Controlled native HDR selection.");
                }
                Reject<InvalidOperationException>(() => Task.Run(() => DisplayServer.WindowGetOutputMaxLinearValue()).GetAwaiter().GetResult());
                Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowSetHDROutputReferenceLuminance(0));
                Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowSetHDROutputMaxLuminance(float.NaN));
                window.Tree!.ProcessFrameStarted += _ =>
                {
                    before = GC.GetAllocatedBytesForCurrentThread(); beforeAll = GC.GetTotalAllocatedBytes(true);
                    Check(window.GetOutputMaxLinearValue() == DisplayServer.WindowGetOutputMaxLinearValue(), "Prepared output reads agree.");
                    DisplayServer.WindowGetHDROutputReferenceLuminance(); DisplayServer.WindowGetHDROutputMaxLuminance();
                    DisplayServer.WindowGetHDROutputCurrentReferenceLuminance(); DisplayServer.WindowGetHDROutputCurrentMaxLuminance();
                    DisplayServer.WindowIsHDROutputRequested(); DisplayServer.WindowIsHDROutputSupported(); DisplayServer.WindowIsHDROutputEnabled();
                };
                RenderingServer.FramePostDraw += () =>
                {
                    if (frames >= 32) { allocated += GC.GetAllocatedBytesForCurrentThread() - before; allAllocated += GC.GetTotalAllocatedBytes(true) - beforeAll; }
                    frames++;
                    if (frames == 1 && controlled && !compatibility)
                    {
                        using var pixels = texture.GetImage()!;
                        Check(pixels.PixelFormat == Image.Format.Rgbah && MathF.Abs(pixels.GetPixel(4, 4).R - 2.5f) < .002f, "Requested output retains actual HDR target values.");
                        Check(DisplayServer.WindowGetHDROutputCurrentReferenceLuminance() == 160 && DisplayServer.WindowGetHDROutputCurrentMaxLuminance() == 640, "Automatic proportional luminance.");
                        order.Clear(); expected = 2; DisplayServer.WindowSetHDROutputMaxLuminance(320);
                        Check(order.SequenceEqual(new[] { 1, 2, 4, 5, 3 }), "Notification, Window, embedded and scoped output order.");
                        order.Clear(); DisplayServer.WindowSetHDROutputMaxLuminance(320); Check(order.Count == 0, "Equal luminance is silent.");
                        expected = 4; DisplayServer.WindowSetHDROutputReferenceLuminance(80);
                        Check(DisplayServer.WindowGetHDROutputReferenceLuminance() == 80 && DisplayServer.WindowGetHDROutputMaxLuminance() == 320, "Authored luminance reads.");
                        var vsync = DisplayServer.WindowGetVSyncMode(); DisplayServer.WindowSetVSyncMode(vsync);
                        Check(DisplayServer.WindowIsHDROutputEnabled() && window.GetOutputMaxLinearValue() == 4, "VSync changes preserve output composition.");
                        expected = 2; DisplayServer.WindowSetHDROutputReferenceLuminance(-1);
                        expected = 4; DisplayServer.WindowSetHDROutputMaxLuminance(-1);
                        if (System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported) NativeFailure(() => window.HDROutputRequested = false);
                        Check(window.HDROutputRequested && DisplayServer.WindowIsHDROutputEnabled() && window.GetOutputMaxLinearValue() == 4,
                            "Rejected native composition leaves request, output and luminance committed to their old values.");
                        var failure = new ArithmeticException("output observer");
                        Action<float> failed = _ => throw failure; window.OutputMaxLinearValueChanged += failed;
                        expected = 2; order.Clear(); Reject<AggregateException>(() => DisplayServer.WindowSetHDROutputMaxLuminance(320));
                        window.OutputMaxLinearValueChanged -= failed;
                        Check(window.GetOutputMaxLinearValue() == 2 && order.Contains(3) && order.Contains(5), "Observer errors preserve committed state and independent recipients.");
                        window.SetBlockSignals(true); expected = 4; order.Clear(); DisplayServer.WindowSetHDROutputMaxLuminance(640); window.SetBlockSignals(false);
                        Check(order.Contains(1) && !order.Contains(2) && order.Contains(3), "Blocking Window signals preserves Node and scoped delivery.");
                        Action<float> replace = value => { if (value == 2) { expected = 3; DisplayServer.WindowSetHDROutputMaxLuminance(480); } };
                        window.OutputMaxLinearValueChanged += replace;
                        expected = 2; order.Clear(); DisplayServer.WindowSetHDROutputMaxLuminance(320);
                        window.OutputMaxLinearValueChanged -= replace;
                        Check(window.GetOutputMaxLinearValue() == 3 && order.SequenceEqual(new[] { 1, 2, 1, 2, 4, 5, 3 }), "Reentrant replacement supersedes stale embedded/scoped delivery.");
                        expected = 4; DisplayServer.WindowSetHDROutputMaxLuminance(-1);
                        _nativeHDR = false; expected = 1; StateEvent();
                    }
                    if (frames == 2 && controlled && !compatibility)
                    {
                        Check(!DisplayServer.WindowIsHDROutputEnabled() && window.HDROutputRequested && window.GetOutputMaxLinearValue() == 1, "Native SDR transition retains the request.");
                        Check(native.GetOutputMaxLinearValue() == 1 && !native.HDROutputRequested, "Native child output remains independent.");
                        _nativeHDR = true; _headroom = 6; expected = 6; StateEvent();
                    }
                    if (frames == 3 && controlled && !compatibility)
                    {
                        Check(DisplayServer.WindowIsHDROutputEnabled() && window.GetOutputMaxLinearValue() == 6, "Native HDR return reapplies requested mode.");
                        expected = 1; window.HDROutputRequested = false;
                    }
                    if (frames == 4 && controlled && !compatibility)
                    {
                        Check(texture.PixelFormat == Image.Format.Rgba8 && texture.GetRID() == identity, "Output removal restores authored target and stable identity.");
                        expected = 6; window.HDROutputRequested = true;
                    }
                    if (frames == 6 && controlled && !compatibility)
                    {
                        native.HDROutputRequested = true;
                        Check(native.GetOutputMaxLinearValue() == 8 && DisplayServer.WindowIsHDROutputEnabled(nativeID) && window.GetOutputMaxLinearValue() == 6, "Native child selects its own HDR output.");
                        DisplayServer.WindowSetHDROutputReferenceLuminance(160, nativeID);
                        Check(native.GetOutputMaxLinearValue() == 4 && window.GetOutputMaxLinearValue() == 6, "Native child luminance is independent.");
                    }
                    if (frames == 7 && controlled && !compatibility)
                    {
                        using var childPixels = nativeTexture.GetImage()!;
                        Check(childPixels.PixelFormat == Image.Format.Rgbah && MathF.Abs(childPixels.GetPixel(4, 4).R - 2.5f) < .002f, "Independent native child retains real HDR pixel values.");
                        native.Hide(); Check(nativeEvents.IsDisposed && native.GetOutputMaxLinearValue() == 1, "Native closure retires the publisher and active output.");
                        Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowIsHDROutputEnabled(nativeID));
                        native.Show(); Check(native.GetWindowID() != nativeID && native.GetOutputMaxLinearValue() == 8, "Reopening retains the request with fresh native policy and identity.");
                        nativeID = native.GetWindowID();
                    }
                    if (frames == 8 && controlled && !compatibility) { _nativeHDR = false; expected = 1; StateEvent(); }
                    if (frames == 9 && controlled && !compatibility)
                    {
                        Check(!DisplayServer.WindowIsHDROutputEnabled() && DisplayServer.WindowIsHDROutputEnabled(nativeID) && native.GetOutputMaxLinearValue() == 8, "A root capability change does not change independent native children.");
                        _nativeHDR = true; expected = 6; StateEvent();
                    }
                    if (frames == 96)
                    {
                        Check(allocated == 0 && allAllocated == 0, $"Prepared HDR output frames allocated {allocated}/{allAllocated} owner/all-thread bytes.");
                        Check(body.Position.Y > 16, "The selected complete physics backend advances the real body.");
                        Check(body.GetRID() == bodyRID && shape.GetRID() == shapeRID, "Output changes retain physical object/shape identities.");
                        if (controlled && !compatibility)
                        {
                            expected = 1; window.Hide();
                            window.HDROutputRequested = false; DisplayServer.WindowSetHDROutputMaxLuminance(400); window.HDROutputRequested = true;
                            Check(window.HDROutputRequested && !DisplayServer.WindowIsHDROutputEnabled() && window.GetOutputMaxLinearValue() == 1,
                                "An unclaimed hidden root retains mutable HDR policy without a native swapchain request.");
                        }
                        window.Tree!.Quit();
                    }
                };
            };
            Check(Engine.Run(window) == 0 && frames == 96 && window.IsDisposed, "Output host lifecycle.");
            if (compatibility)
            {
                using var unsupported = new Window { Size = new(48, 32), HDROutputRequested = true };
                Reject<NotSupportedException>(() => Engine.Run(unsupported));
                Check(unsupported.IsDisposed && !RenderingServer.IsAvailable, "An initial unsupported HDR request cleans up the complete host.");
            }
            Console.WriteLine($"HDR output {mode}: native selection/events, real pixels and physics, 32 warm + 64 frames, {allocated}/{allAllocated} owner/all-thread bytes.");
        }
        finally { GPUCanvasBackend.OutputForTesting = null; shape.Dispose(); if (!window.IsDisposed) window.Dispose(); }
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
