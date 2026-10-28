using System.Diagnostics;
using Electron2D;
using static MovieWriterTests;

internal static partial class RenderingRuntimeTests
{
    private sealed class TimingDelayNode : Node
    {
        internal bool Armed, Slept;
        internal TimingDelayNode() => ProcessEnabled = true;
        protected override void OnProcess(double delta) { if (!Armed) return; Armed = false; Thread.Sleep(300); Slept = true; }
    }
    internal static void RunViewportTiming(string backend)
    {
        Reject<InvalidOperationException>(() => RenderingServer.GetFrameSetupTimeCPU());
        Reject<InvalidOperationException>(() => RenderingServer.ViewportSetMeasureRenderTime(default, true));
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fps = Engine.MaxFPS;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend == "software" ? "compatibility" : backend); Engine.MaxFPS = 60;
        try { if (backend == "software") UnsupportedViewportTiming(); else NativeViewportTiming(backend); }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); Engine.MaxFPS = fps; }
        Reject<InvalidOperationException>(() => RenderingServer.ViewportGetMeasuredRenderTimeGPU(default));
    }
    private static void UnsupportedViewportTiming()
    {
        var root = new Window { Size = new(320, 240) }; root.AddChild(new ColorRect { Size = new(320, 240), Color = Colors.Red });
        root.Ready += _ =>
        {
            Reject<NotSupportedException>(() => RenderingServer.ViewportSetMeasureRenderTime(root.GetViewportRID(), true));
            RenderingServer.ViewportSetMeasureRenderTime(root.GetViewportRID(), false);
            RenderingServer.FramePostDraw += () => { Check(RenderingServer.GetFrameSetupTimeCPU() > 0 && RenderingServer.ViewportGetMeasuredRenderTimeCPU(root.GetViewportRID()) == 0, "CPU setup executes while unsupported native timers reject."); root.Tree!.Quit(); };
        };
        Check(Engine.Run(root) == 0, "Software timer rejection preserves rendering."); Console.WriteLine("Viewport timing unsupported/native-query guard passed.");
    }
    private static void NativeViewportTiming(string backend)
    {
        var root = new Window { Title = "Viewport timing " + backend, Size = new(320, 240), GUIEmbedSubwindows = false };
        root.AddChild(new ColorRect { Size = new(320, 240), Color = Colors.White });
        var sub = new SubViewport { Size = new(64, 64), RenderTargetUpdateMode = ViewportUpdateMode.Always };
        sub.AddChild(new ColorRect { Size = new(96, 80), Color = Colors.Green }); root.AddChild(sub);
        root.AddChild(new TextureRect { Position = new(32, 32), Size = new(64, 64), Texture = sub.GetTexture() });
        var child = new Window { Title = "Viewport timing child", Size = new(96, 80) }; child.AddChild(new ColorRect { Size = new(96, 80), Color = Colors.Red }); root.AddChild(child);
        var delay = new TimingDelayNode(); root.AddChild(delay);
        var childDrawArmed = false; var childDrawSlept = false;
        var childDraw = new CanvasNode { DrawAction = _ => { if (childDrawArmed) { childDrawArmed = false; Thread.Sleep(300); childDrawSlept = true; } } }; sub.AddChild(childDraw);
        RID server = default, canvas = default, item = default; RID[] ids = [];
        var phase = 0; var frames = 0; var warm = 0; var cycles = 0; long before = 0, active = -1, idle = -1;
        root.AddChild(new CanvasNode { DrawAction = node => { Reject<InvalidOperationException>(() => RenderingServer.ViewportSetMeasureRenderTime(root.GetViewportRID(), false)); node.DrawRect(new(1, 1, 2, 2), Colors.White); } });
        bool Measured()
        {
            foreach (var id in ids) if (RenderingServer.ViewportGetMeasuredRenderTimeCPU(id) <= 0 || RenderingServer.ViewportGetMeasuredRenderTimeGPU(id) <= 0) return false;
            return RenderingServer.GetFrameSetupTimeCPU() > 0;
        }
        void Pixels()
        {
            using var first = root.GetTexture().GetImage()!; using var second = sub.GetTexture().GetImage()!;
            using var third = RenderingServer.Texture2DGet(RenderingServer.ViewportGetTexture(server))!;
            using var fourth = RenderingServer.Service!.ReadbackNativeWindow(child.GetWindowID());
            Check(first.GetPixel(8, 8) == Colors.White && second.GetPixel(8, 8) == Colors.Green && third.GetPixel(8, 8) == Colors.Blue && fourth.GetPixel(8, 8) == Colors.Red, "Root, dependent SubViewport, raw RID and independent native window retain actual pixels.");
        }
        void After()
        {
            if (++frames > 220) throw new TimeoutException("Viewport timing did not complete its native phases.");
            switch (phase)
            {
                case 0:
                    Pixels(); if (!Measured()) return;
                    RenderingServer.ViewportSetMeasureRenderTime(ids[0], false);
                    Check(RenderingServer.ViewportGetMeasuredRenderTimeCPU(ids[0]) == 0 && RenderingServer.ViewportGetMeasuredRenderTimeGPU(ids[0]) == 0, "Disable clears visible measurements immediately."); phase++; break;
                case 1:
                    Check(RenderingServer.ViewportGetMeasuredRenderTimeCPU(ids[0]) == 0 && RenderingServer.ViewportGetMeasuredRenderTimeGPU(ids[1]) > 0, "Viewport timing policies are independent.");
                    RenderingServer.ViewportSetMeasureRenderTime(ids[0], true);
                    Check(RenderingServer.ViewportGetMeasuredRenderTimeGPU(ids[0]) == 0, "A fresh timing session has no stale result."); phase++; break;
                case 2:
                    if (!Measured()) return;
                    RenderingServer.ViewportSetMeasureRenderTime(ids[0], true); delay.Armed = true; childDrawArmed = true; childDraw.QueueRedraw(); phase++; break;
                case 3:
                    Pixels();
                    Check(delay.Slept && childDrawSlept && Measured(), "Process/child draw delays and real native measurements execute.");
                    Check(RenderingServer.GetFrameSetupTimeCPU() >= 250 && RenderingServer.ViewportGetMeasuredRenderTimeCPU(ids[1]) < 250 && RenderingServer.ViewportGetMeasuredRenderTimeCPU(ids[0]) < 250, "Shared child canvas authoring is measured once in common setup, outside both viewport submission scopes.");
                    var total = RenderingServer.GetFrameSetupTimeCPU(); foreach (var id in ids) total += RenderingServer.ViewportGetMeasuredRenderTimeCPU(id);
                    Check(total < 550, "A 300ms process callback and frame cap are outside rendering CPU scopes even with a measured 300ms child draw.");
                    sub.Size = new(96, 80); RenderingServer.ViewportSetSize(server, 48, 48); child.Hide(); phase++; break;
                case 4:
                    child.Show(); ids[3] = child.GetViewportRID(); RenderingServer.ViewportSetMeasureRenderTime(ids[3], true); phase++; break;
                case 5:
                    Pixels(); if (!Measured()) return;
                    Engine.MaxFPS = 0; phase++; break;
                case 6:
                    Check(Measured(), "Measured resized and remapped targets stay live.");
                    if (++warm < 32) break;
                    before = GC.GetAllocatedBytesForCurrentThread(); phase++; break;
                case 7:
                    Check(Measured(), "Prepared native measurements stay published.");
                    if (++cycles < 64) break;
                    active = GC.GetAllocatedBytesForCurrentThread() - before;
                    RenderingServer.FreeRID(server); RenderingServer.FreeRID(item); RenderingServer.FreeRID(canvas);
                    Reject<ArgumentException>(() => RenderingServer.ViewportGetMeasuredRenderTimeGPU(server));
                    ids = [root.GetViewportRID(), sub.GetViewportRID(), child.GetViewportRID()];
                    foreach (var id in ids) RenderingServer.ViewportSetMeasureRenderTime(id, false);
                    cycles = 0; warm = 0; phase++; break;
                case 8:
                    foreach (var id in ids) Check(RenderingServer.ViewportGetMeasuredRenderTimeCPU(id) == 0 && RenderingServer.ViewportGetMeasuredRenderTimeGPU(id) == 0, "Disabled sessions do not publish stale measurements.");
                    if (++warm < 16) break;
                    before = GC.GetAllocatedBytesForCurrentThread(); phase++; break;
                case 9:
                    foreach (var id in ids) { _ = RenderingServer.ViewportGetMeasuredRenderTimeCPU(id); _ = RenderingServer.ViewportGetMeasuredRenderTimeGPU(id); }
                    if (++cycles < 64) break;
                    idle = GC.GetAllocatedBytesForCurrentThread() - before;
                    Check(active == 0 && idle == 0, "Prepared measured and disabled frame intervals allocate no owner-managed bytes.");
                    RenderingServer.FramePostDraw -= After; root.Tree!.Quit(); break;
            }
        }
        root.Ready += _ =>
        {
            Reject<ArgumentException>(() => RenderingServer.ViewportGetMeasuredRenderTimeCPU(default));
            Reject<ArgumentException>(() => RenderingServer.ViewportSetMeasureRenderTime(default, true));
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.GetFrameSetupTimeCPU()).GetAwaiter().GetResult());
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.ViewportGetMeasuredRenderTimeGPU(root.GetViewportRID())).GetAwaiter().GetResult());
            server = RenderingServer.ViewportCreate(); RenderingServer.ViewportSetSize(server, 64, 64); RenderingServer.ViewportSetActive(server, true); RenderingServer.ViewportSetUpdateMode(server, ViewportUpdateMode.Always);
            canvas = RenderingServer.CanvasCreate(); item = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(item, canvas); RenderingServer.CanvasItemAddRect(item, new(0, 0, 64, 64), Colors.Blue); RenderingServer.ViewportAttachCanvas(server, canvas);
            ids = [root.GetViewportRID(), sub.GetViewportRID(), server, child.GetViewportRID()];
            foreach (var id in ids) { Check(RenderingServer.ViewportGetMeasuredRenderTimeCPU(id) == 0, "Measurements are disabled initially."); RenderingServer.ViewportSetMeasureRenderTime(id, true); }
            RenderingServer.FramePostDraw += After;
        };
        Check(Engine.Run(root) == 0 && phase == 9 && active == 0 && idle == 0, "All native timing and lifetime phases complete.");
        Console.WriteLine($"Viewport timing {backend}: root/SubViewport/RID/native-child pixels, process/capture/owner guards, resize/remap/retirement, 64 active/64 idle frames {active}/{idle} B passed.");
    }
}
