using Electron2D;

internal static class RenderingDeviceScreenTests
{
    internal static void Run()
    {
        try { RunCore(); } catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
    }
    private static void RunCore()
    {
        using (var local = RenderingServer.CreateLocalRenderingDevice())
        {
            Check(local.ScreenGetWidth() == RenderingDevice.InvalidID && local.ScreenGetHeight() == RenderingDevice.InvalidID && local.ScreenGetFramebufferFormat() == RenderingDevice.InvalidID && local.DrawListBeginForScreen() == RenderingDevice.InvalidID, "Local devices have no screen.");
        }
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Native(); ForcedPresentation(); MinimizedPresentation(); Interrupted();
        Console.WriteLine("Device screen graphics checks passed.");
    }
    private static void Native()
    {
        var hdr = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_DEVICE_SCREEN_HDR") == "1";
        using var root = new Window { Size = new(96, 80), Borderless = true, UseHDR2D = hdr };
        var child = new Window { Size = new(80, 64), Borderless = true, UseHDR2D = hdr };
        root.AddChild(new ColorRect { Size = new(256, 256), Color = Colors.Green });
        child.AddChild(new ColorRect { Size = new(256, 256), Color = Colors.Magenta }); root.AddChild(child);
        RenderingDevice device = null!; RID shader = default, pipeline = default;
        var frame = 0; var warm = 0; var id = 0; var retired = 0; var initialWidth = 0; var format = 0; var resizeWait = 0; var presentationWait = 0;
        long before = 0, active = 0, idle = 0;
        root.Ready += _ =>
        {
            OS.LowProcessorUsageMode = false;
            device = RenderingServer.GetRenderingDevice()!; id = child.GetWindowID();
            Reject<InvalidOperationException>(() => device.Dispose());
            Check(id > 0, "A child native window is active.");
            format = device.ScreenGetFramebufferFormat();
            Check(format == device.ScreenGetFramebufferFormat() && format == device.ScreenGetFramebufferFormat(id), "Same native format shares one reusable device layout.");
            Check(device.FramebufferFormatGetTextureSamples(format) == RenderingDevice.TextureSamples.Samples1, "Screen targets have one sample.");
            initialWidth = device.ScreenGetWidth();
            Check(initialWidth > 0 && device.ScreenGetHeight() > 0 && device.ScreenGetWidth(id) > 0 && device.ScreenGetHeight(id) > 0, "Native graphics client dimensions.");
            Reject<ArgumentOutOfRangeException>(() => device.ScreenGetWidth(-1));
            Reject<ArgumentOutOfRangeException>(() => device.ScreenGetHeight(int.MaxValue));
            Reject<ArgumentOutOfRangeException>(() => device.ScreenGetFramebufferFormat(-1));
            Reject<ArgumentOutOfRangeException>(() => device.DrawListBeginForScreen(-1));
            Reject<ArgumentException>(() => device.DrawListBeginForScreen(clearColor: new(float.NaN, 0, 0)));
            Reject<InvalidOperationException>(() => Task.Run(() => device.ScreenGetWidth()).GetAwaiter().GetResult());
            using var module = RenderingDeviceGraphicsTests.Load("DeviceProcedural"); shader = device.ShaderCreateFromSPIRV(module);
            using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment();
            using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
            pipeline = device.RenderPipelineCreate(shader, format, -1, RenderingDevice.RenderPrimitive.Triangles, raster, blend);
            RenderingServer.FramePreDraw += Before;
            RenderingServer.FramePostDraw += After;
        };
        void Paint(int screen, Color color, bool triangle = false)
        {
            var list = device.DrawListBeginForScreen(screen, color);
            if (triangle)
            {
                device.DrawListBindRenderPipeline(list, pipeline); device.DrawListEnableScissor(list, new(0, 0, 16, 16));
                device.DrawListDraw(list, false, 1, 3);
            }
            device.DrawListEnd();
        }
        void Before()
        {
            if (frame >= 6)
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (warm < 96) { Paint(0, (warm & 1) == 0 ? Colors.Red : Colors.Blue, triangle: true); Paint(id, Colors.Blue); }
                return;
            }
            switch (frame)
            {
                case 0:
                    var childList = device.DrawListBeginForScreen(id, Colors.Blue);
                    Reject<InvalidOperationException>(() => child.Hide());
                    Reject<InvalidOperationException>(() => root.RemoveChild(child));
                    Reject<InvalidOperationException>(() => child.Dispose());
                    Check(child.Visible && !child.IsDisposed && child.Parent == root && child.GetWindowID() == id, "Rejected native retirement preserves scene and window state.");
                    device.DrawListEnd();
                    var list = device.DrawListBeginForScreen();
                    Check(device.ScreenGetWidth() > 0 && device.ScreenGetFramebufferFormat() == format, "Queries remain available within a screen list.");
                    Reject<InvalidOperationException>(() => device.DrawListBeginForScreen(id));
                    Reject<InvalidOperationException>(() => device.ComputeListBegin());
                    device.DrawListEnd(); break;
                case 1: break;
                case 2: Paint(0, Colors.Red); Paint(0, Colors.Blue); Paint(id, Colors.Red); break;
                case 3: Paint(0, Colors.Red); break;
                case 4: Paint(0, Colors.Blue); Paint(id, Colors.Transparent); break;
                case 5: Paint(0, Colors.Blue, triangle: true); Paint(id, Colors.Blue, triangle: true); break;
            }
        }
        void After()
        {
            if (frame >= 6)
            {
                if (warm is >= 32 and < 96) active += GC.GetAllocatedBytesForCurrentThread() - before;
                else if (warm >= 96) idle += GC.GetAllocatedBytesForCurrentThread() - before;
                if (++warm != 160) return;
                Check(active == 0 && idle == 0, $"Screen draws: 64 active/64 idle frames, {active}/{idle} owner managed bytes.");
                device.FreeRID(pipeline); device.FreeRID(shader); root.Tree!.Quit(); return;
            }
            var renderer = RenderingServer.Service!;
            if (!renderer.WasNativeWindowPresented(0) || !renderer.WasNativeWindowPresented(id))
            { Check(++presentationWait < 64, "Both native swapchains must receive the screen output."); return; }
            using var presented = renderer.Readback();
            using var childImage = renderer.ReadbackNativeWindow(id);
            using var canvas = root.GetTexture().GetImage()!;
            Pixel(canvas, 24, 24, hdr ? Colors.Green.SRGBToLinear() : Colors.Green);
            Check(presented.Width == device.ScreenGetWidth() && presented.Height == device.ScreenGetHeight(), "Presented dimensions match the graphics screen.");
            switch (frame++)
            {
                case 0: Pixel(presented, 24, 24, Colors.Black); Pixel(childImage, 24, 24, Colors.Blue); Paint(0, Colors.Red); break;
                case 1: Pixel(presented, 24, 24, Colors.Red); Pixel(childImage, 24, 24, Colors.Magenta); break;
                case 2:
                    Pixel(presented, 24, 24, Colors.Blue); Pixel(childImage, 24, 24, Colors.Red);
                    root.Size = new(initialWidth + 32, device.ScreenGetHeight() + 16); break;
                case 3:
                    if (device.ScreenGetWidth() == initialWidth) { Check(++resizeWait < 64, "Native resize must complete."); frame = 3; return; }
                    Pixel(presented, 24, 24, Colors.Red); Check(device.ScreenGetFramebufferFormat() == format, "Resize retains pipeline format.");
                    retired = id; child.Hide(); Reject<ArgumentOutOfRangeException>(() => device.ScreenGetWidth(retired));
                    child.Show(); id = child.GetWindowID(); Check(id > 0 && id != retired, "Reopened child has a fresh screen identity."); break;
                case 4: Pixel(presented, 24, 24, Colors.Blue); Pixel(childImage, 24, 24, Colors.Transparent); break;
                case 5:
                    Pixel(presented, 8, 8, new(1, 0, 0, .5f)); Pixel(childImage, 8, 8, new(1, 0, 0, .5f));
                    RenderingServer.ViewportSetUpdateMode(root.GetViewportRID(), ViewportUpdateMode.Disabled); break;
            }
        }
        Engine.Run(root);
        Check(root.IsDisposed && child.IsDisposed && !RenderingServer.IsAvailable, "Screen resources retire with their native session.");
        Reject<ObjectDisposedException>(() => device.ScreenGetWidth());
    }
    private sealed class Step(Action action) : Node
    { protected override void OnProcess(double delta) => action(); }
    private static void ForcedPresentation()
    {
        using var root = new Window { Size = new(96, 80), Borderless = true };
        root.AddChild(new ColorRect { Size = new(256, 256), Color = Colors.Green });
        var executed = false;
        root.Ready += _ => RenderingServer.RenderLoopEnabled = false;
        root.AddChild(new Step(() =>
        {
            if (executed) return; executed = true;
            var device = RenderingServer.GetRenderingDevice()!;
            device.DrawListBeginForScreen(clearColor: Colors.Red); device.DrawListEnd();
            RenderingServer.ForceSync(); RenderingServer.ForceDraw(false);
            using (var content = root.GetTexture().GetImage()!) Pixel(content, 24, 24, Colors.Green);
            using (var unchanged = RenderingServer.Service!.Readback()) Pixel(unchanged, 24, 24, Colors.Green);
            RenderingServer.ForceDraw();
            using (var screen = RenderingServer.Service!.Readback()) Pixel(screen, 24, 24, Colors.Red);
            RenderingServer.ForceDraw();
            using (var restored = RenderingServer.Service!.Readback()) Pixel(restored, 24, 24, Colors.Green);
            root.Tree!.Quit();
        }));
        Engine.Run(root); Check(executed && root.IsDisposed && !RenderingServer.IsAvailable, "ForceDraw(false) preserves one pending screen image, then ordinary canvas presentation resumes.");
    }
    private static void MinimizedPresentation()
    {
        if (SDL3.SDL.GetCurrentVideoDriver() != "x11") return;
        using var root = new Window { Size = new(96, 80), Borderless = true };
        root.AddChild(new ColorRect { Size = new(256, 256), Color = Colors.Green });
        var phase = 0; var attempts = 0;
        root.Ready += _ => { RenderingServer.RenderLoopEnabled = false; root.Mode = WindowMode.Minimized; };
        root.AddChild(new Step(() =>
        {
            Check(++attempts < 512, "Minimized native screen lifecycle must complete.");
            var renderer = RenderingServer.Service!; var device = RenderingServer.GetRenderingDevice()!;
            if (phase == 0)
            {
                if (root.Mode != WindowMode.Minimized) return;
                device.DrawListBeginForScreen(clearColor: Colors.Red); device.DrawListEnd();
                RenderingServer.ForceDraw();
                Check(!renderer.WasNativeWindowPresented(0) && device.HasPendingScreens && RenderingServer.HasChanged(), "An unavailable swapchain retains dirty pending screen output.");
                root.Mode = WindowMode.Windowed; phase = 1; return;
            }
            if (root.Mode == WindowMode.Minimized) return;
            RenderingServer.ForceDraw();
            if (!renderer.WasNativeWindowPresented(0)) return;
            using var screen = renderer.Readback(); Pixel(screen, 24, 24, Colors.Red);
            Check(!device.HasPendingScreens, "A native presentation consumes the pending image once.");
            phase = 2; root.Tree!.Quit();
        }));
        Engine.Run(root); Check(phase == 2 && root.IsDisposed && !RenderingServer.IsAvailable, "Native minimize/restore completes without losing screen output.");
    }
    private static void Interrupted()
    {
        using var root = new Window { Size = new(96, 80), Borderless = true };
        var child = new Window { Size = new(80, 64), Borderless = true }; root.AddChild(child);
        root.Ready += _ =>
        {
            RenderingServer.GetRenderingDevice()!.DrawListBeginForScreen(child.GetWindowID());
            throw new ArgumentException("Interrupted screen authoring.");
        };
        try { Engine.Run(root); throw new InvalidOperationException("Expected interrupted screen activation."); }
        catch (AggregateException error) { Check(error.Flatten().InnerExceptions.Count == 1 && error.Flatten().InnerExceptions[0] is ArgumentException, "Rollback preserves the original activation failure without screen-retirement errors."); }
        Check(root.IsDisposed && child.IsDisposed && !RenderingServer.IsAvailable, "Interrupted screen passes and native children retire during renderer shutdown.");
    }
    private static void Pixel(Image image, int x, int y, Color color)
    { var actual = image.GetPixel(x, y); Check(MathF.Abs(actual.R - color.R) < .015f && MathF.Abs(actual.G - color.G) < .015f && MathF.Abs(actual.B - color.B) < .015f && MathF.Abs(actual.A - color.A) < .015f, $"Screen pixel {x},{y}: {actual} != {color}."); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
