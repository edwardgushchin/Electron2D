using System.Diagnostics;
using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static partial class ViewportDirectTests
{
    [LibraryImport("dwmapi.dll")]
    private static partial int DwmFlush();

    internal static void Run(string profile, string[] args)
    {
        var gpu = profile == "gpu";
        Check(gpu || (profile == "windows" && OperatingSystem.IsWindows() && args.Length == 1),
            "Use the Windows desktop with a WGC monitor reader, or the GPU rejection profile.");
        var backend = gpu ? "gpu" : "compatibility";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Engine.MaxFPS = 60;
        var window = new Window
        {
            Title = "Electron2D direct viewport",
            Size = new(480, 320),
            Position = new(360, 260),
            Borderless = true,
            AlwaysOnTop = true,
            GUIEmbedSubwindows = false
        };
        var driver = new Driver(window, gpu ? null : args[0]);
        window.AddChild(driver);
        window.Ready += _ => Check(RenderingServer.GetCurrentRenderingMethod() == backend, "The requested native backend is active.");
        try { Check(Engine.Run(window) == 0 && driver.Completed, "The direct viewport sequence completed."); }
        finally
        {
            driver.CloseReader();
            if (!window.IsDisposed) window.Dispose();
        }
        Console.WriteLine($"PASS direct viewport {profile}: {driver.Verified} assertions after initialization.");
    }

    private sealed class Driver(Window window, string? monitor) : Node
    {
        private readonly Stopwatch _deadline = Stopwatch.StartNew();
        private Process? _reader;
        private Task<string>? _readerOutput;
        private RID _red, _green, _base, _redCanvas, _redItem, _greenCanvas, _greenItem, _redTexture;
        private Window? _child;
        private long _ordinaryTextureBytes, _retiredTextureBytes;
        private int _phase;
        internal bool Completed;
        internal int Verified;

        protected override void OnReady() => ProcessEnabled = true;

        protected override void OnProcess(double delta)
        {
            if (Completed) return;
            Check(_deadline.Elapsed < TimeSpan.FromSeconds(24), $"Timed out in native direct viewport phase {_phase}.");
            if (_phase == 0) Initialize();
            else if (_phase == 1) VerifyMonitor();
            else if (_phase == 2) SwitchToChild();
            else if (_phase == 3) VerifyChild();
            else if (_phase == 4) Finish();
        }

        private void Initialize()
        {
            RenderingServer.RenderLoopEnabled = false;
            RenderingServer.SetDefaultClearColor(Colors.Blue);
            if (monitor is null)
            {
                Reject<NotSupportedException>(() => RenderingServer.ViewportSetRenderDirectToScreen(window.GetViewportRID(), true));
                RenderingServer.ForceDraw();
                using var pixels = window.GetTexture().GetImage();
                Verify(pixels is not null, "GPU rejection leaves the ordinary viewport intact.");
                Complete();
                return;
            }

            _base = View(new(480, 320), new(0, 0, 480, 320));
            _red = View(new(150, 90), new(68, 66, 104, 60));
            _green = View(new(52, 30), new(208, 66, 104, 60));
            (_redCanvas, _redItem) = Marker(_red, new(150, 90), Colors.Red);
            (_greenCanvas, _greenItem) = Marker(_green, new(52, 30), Colors.Green);
            RenderingServer.ViewportAttachToScreen(window.GetViewportRID());
            _redTexture = RenderingServer.ViewportGetTexture(_red);
            RenderingServer.ForceDraw();
            using (var pixels = RenderingServer.Texture2DGet(_redTexture))
                Verify(pixels?.Size == new Vector2i(150, 90), "The ordinary mode has an offscreen image.");

            _ordinaryTextureBytes = RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TextureMemUsed);
            using (var presented = RenderingServer.Service!.Readback())
                _retiredTextureBytes = 8L * (150 * 90 + presented.Width * presented.Height);

            Reject<ArgumentException>(() => RenderingServer.ViewportSetRenderDirectToScreen(default, true));
            Task.Run(() => Reject<InvalidOperationException>(() => RenderingServer.ViewportSetRenderDirectToScreen(_red, true))).GetAwaiter().GetResult();
            RenderingServer.ViewportSetRenderDirectToScreen(_red, true);
            RenderingServer.ViewportSetRenderDirectToScreen(_red, true);
            Verify(RenderingServer.ViewportGetTexture(_red) == _redTexture &&
                RenderingServer.Texture2DGet(_redTexture) is null, "Direct mode retires the image and retains its logical RID.");
            RenderingServer.ViewportSetUpdateMode(_red, ViewportUpdateMode.Once);
            RenderingServer.ForceDraw(swapBuffers: false);
            Verify(RenderingServer.ViewportGetUpdateMode(_red) == ViewportUpdateMode.Once, "No-swap preserves pending direct output.");
            RenderingServer.ForceDraw();
            Verify(RenderingServer.ViewportGetUpdateMode(_red) == ViewportUpdateMode.Disabled, "A direct presentation consumes Once.");
            RenderingServer.ViewportSetUpdateMode(_red, ViewportUpdateMode.WhenVisible);
            _phase++;
        }

        private void VerifyMonitor()
        {
            RenderingServer.ForceDraw();
            if (_reader is null)
            {
                var handle = DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
                var start = new ProcessStartInfo(monitor!) { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                start.ArgumentList.Add("0x" + handle.ToString("X"));
                _reader = Process.Start(start) ?? throw new InvalidOperationException("Could not start the monitor reader.");
                _readerOutput = _reader.StandardOutput.ReadToEndAsync();
                return;
            }
            if (!_reader.HasExited || _readerOutput?.IsCompletedSuccessfully != true) return;
            Console.Write(_readerOutput.Result);
            Verify(_reader.ExitCode == 0, "The independent monitor frame contains direct and ordinary markers.");
            var retired = _ordinaryTextureBytes - RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TextureMemUsed);
            Verify(retired == _retiredTextureBytes, $"The viewport and presentation texture pairs retire ({retired} bytes).");
            Console.WriteLine($"Retired viewport and presentation textures: {retired} bytes.");

            RenderingServer.ViewportSetMSAA2D(_red, Viewport.MSAA.Samples4X);
            Reject<NotSupportedException>(() => RenderingServer.ForceDraw());
            RenderingServer.ViewportSetMSAA2D(_red, Viewport.MSAA.Disabled);
            RenderingServer.ForceDraw();

            Verify(Screen(window, 176, 96) == Colors.Blue, "The native rectangle clips an oversized viewport without scaling it.");
            Reject<NotSupportedException>(() => RenderingServer.Service!.Readback());
            RenderingServer.ViewportAttachToScreen(_red, new(-16, 66, 104, 60));
            Draw(_redItem, new(16, 0, 72, 90), Colors.Red);
            RenderingServer.ForceDraw();
            Verify(Screen(window, 8, 96) == Colors.Red && Screen(window, 92, 96) == Colors.Blue,
                "Client clipping preserves coordinates for a negative destination.");

            RenderingServer.ViewportSetParentViewport(_red, _base);
            RenderingServer.ForceDraw();
            Verify(Screen(window, 8, 96) == Colors.Blue, "A parent copy replaces overlapping direct child pixels.");
            RenderingServer.ViewportSetParentViewport(_red, default);

            RenderingServer.CanvasItemAddTextureRect(_greenItem, new(0, 0, 16, 16), _redTexture);
            Reject<NotSupportedException>(() => RenderingServer.ForceDraw());
            Draw(_greenItem, new(0, 0, 52, 30), Colors.Green);

            var scene = new SubViewport { Size = new(32, 32) };
            scene.AddChild(new ColorRect { Size = new(32, 32), Color = Colors.Yellow });
            window.AddChild(scene);
            var rid = scene.GetViewportRID();
            RenderingServer.ViewportSetParentViewport(rid, default);
            RenderingServer.ViewportAttachToScreen(rid, new(400, 24, 32, 32));
            RenderingServer.ViewportSetRenderDirectToScreen(rid, true);
            RenderingServer.ForceDraw();
            Verify(Screen(window, 414, 38) == Colors.Yellow && scene.GetTexture().GetImage() is null,
                "A scene viewport also draws directly without a retained image.");
            scene.AddChild(new BackBufferCopy { CopyMode = BackBufferCopyMode.Viewport });
            Reject<NotSupportedException>(() => RenderingServer.ForceDraw());
            scene.Dispose();
            RenderingServer.ForceDraw();
            Verify(Screen(window, 8, 96) == Colors.Red, "Rejecting a screen copy leaves later native frames usable.");

            for (var i = 0; i < 8; i++) RenderingServer.ForceDraw();
            var bytes = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 32; i++) RenderingServer.ForceDraw();
            Verify(GC.GetAllocatedBytesForCurrentThread() == bytes, "Prepared mixed presentation allocates zero owner bytes.");
            _phase++;
        }

        private void SwitchToChild()
        {
            _child = new Window { Size = new(150, 90), Position = new(920, 300), Borderless = true, AlwaysOnTop = true };
            window.AddChild(_child);
            Verify(_child.GetWindowID() > 0, "The child owns a distinct native window.");
            RenderingServer.ViewportAttachToScreen(_child.GetViewportRID(), screen: -1);
            RenderingServer.ViewportAttachToScreen(_red, new(0, 0, 100, 80), _child.GetWindowID());
            Draw(_redItem, new(0, 0, 150, 90), Colors.Red);
            RenderingServer.ForceDraw();
            _phase++;
        }

        private void VerifyChild()
        {
            RenderingServer.ForceDraw();
            if (Screen(_child!, 40, 40) != Colors.Red) return;
            Verify(Screen(_child!, 120, 40) == Colors.Black && Screen(window, 8, 96) == Colors.Blue,
                "The child receives the clipped direct layer and the parent no longer receives it.");
            _child!.Dispose();
            RenderingServer.ViewportSetUpdateMode(_red, ViewportUpdateMode.Once);
            RenderingServer.ForceDraw();
            Verify(RenderingServer.ViewportGetUpdateMode(_red) == ViewportUpdateMode.Once, "A retired destination leaves Once pending.");
            _phase++;
        }

        private void Finish()
        {
            RenderingServer.ViewportAttachToScreen(_red);
            RenderingServer.ForceDraw();
            Verify(RenderingServer.ViewportGetUpdateMode(_red) == ViewportUpdateMode.Once, "An empty direct destination leaves Once pending.");
            RenderingServer.ViewportSetRenderDirectToScreen(_red, false);
            RenderingServer.ViewportAttachToScreen(_red, new(68, 66, 104, 60));
            RenderingServer.ForceDraw();
            using var restored = RenderingServer.Texture2DGet(_redTexture);
            Verify(restored?.GetPixel(60, 30) == Colors.Red && RenderingServer.ViewportGetTexture(_red) == _redTexture,
                "Disabling direct rendering recreates the offscreen image behind the same identity.");
            using var screen = RenderingServer.Service!.Readback();
            Verify(screen.GetPixel(120, 96) == Colors.Red, "The ordinary presentation can be read again.");
            foreach (var rid in new[] { _red, _green, _base, _redItem, _greenItem, _redCanvas, _greenCanvas })
                RenderingServer.FreeRID(rid);
            Reject<ArgumentException>(() => RenderingServer.ViewportSetRenderDirectToScreen(_red, true));
            Complete();
        }

        private void Complete() { Completed = true; Tree!.Quit(); }
        internal void CloseReader()
        {
            if (_reader is null) return;
            if (!_reader.HasExited) { _reader.Kill(); _reader.WaitForExit(2000); }
            _reader.Dispose();
        }
        private void Verify(bool condition, string message) { Check(condition, message); Verified++; }
        private static Color Screen(Window target, int x, int y)
        {
            Check(DwmFlush() == 0, "The desktop compositor completed the frame.");
            return DisplayServer.ScreenGetPixel(DisplayServer.WindowGetPosition(target.GetWindowID()) + new Vector2i(x, y));
        }
        private static RID View(Vector2i size, Rect2 rect)
        {
            var rid = RenderingServer.ViewportCreate();
            RenderingServer.ViewportSetSize(rid, size.X, size.Y);
            RenderingServer.ViewportSetActive(rid, true);
            RenderingServer.ViewportSetUpdateMode(rid, ViewportUpdateMode.WhenVisible);
            RenderingServer.ViewportAttachToScreen(rid, rect);
            return rid;
        }
        private static (RID Canvas, RID Item) Marker(RID view, Vector2i size, Color color)
        {
            var canvas = RenderingServer.CanvasCreate();
            var item = RenderingServer.CanvasItemCreate();
            RenderingServer.CanvasItemSetParent(item, canvas);
            RenderingServer.ViewportAttachCanvas(view, canvas);
            Draw(item, new(Vector2.Zero, size), color);
            return (canvas, item);
        }
        private static void Draw(RID item, Rect2 rect, Color color)
        {
            RenderingServer.CanvasItemClear(item);
            RenderingServer.CanvasItemAddRect(item, rect, color);
        }
    }
}
