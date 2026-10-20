using Electron2D;
using Counter = Electron2D.ViewportRenderInfo;
using Domain = Electron2D.ViewportRenderInfoType;
using Monitor = Electron2D.Performance.Monitor;
using Total = Electron2D.RenderingServer.RenderingInfo;

internal static class CanvasCounterTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void RunManaged()
    {
        Check(!RenderingServer.IsAvailable, "No implicit renderer creation");
        using var view = new SubViewport();
        Check(view.GetRenderInfo(Domain.Canvas, Counter.ObjectsInFrame) == 0 && RenderingServer.GetRenderingInfo(Total.TotalObjectsInFrame) == 0 && Performance.GetMonitor(Monitor.RenderTotalPrimitivesInFrame) == 0, "Unavailable/pre-frame observations are zero");
        Reject<ArgumentOutOfRangeException>(() => view.GetRenderInfo(Domain.Max, Counter.ObjectsInFrame));
        Reject<ArgumentOutOfRangeException>(() => view.GetRenderInfo((Domain)0, Counter.ObjectsInFrame));
        Reject<ArgumentOutOfRangeException>(() => view.GetRenderInfo(Domain.Canvas, Counter.Max));
        Reject<ArgumentOutOfRangeException>(() => RenderingServer.GetRenderingInfo((Total)(-1)));
        Reject<InvalidOperationException>(() => RenderingServer.ViewportGetRenderInfo(default, Domain.Canvas, Counter.ObjectsInFrame));
        view.Dispose(); Reject<ObjectDisposedException>(() => view.GetRenderInfo(Domain.Canvas, Counter.ObjectsInFrame));
        for (var i = 0; i < 8; i++) _ = RenderingServer.GetRenderingInfo(Total.TotalObjectsInFrame);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 256; i++) { _ = RenderingServer.GetRenderingInfo(Total.TotalDrawCallsInFrame); _ = Performance.GetMonitor(Monitor.RenderTotalObjectsInFrame); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared unavailable-session observations allocate zero owner bytes");
        Console.WriteLine("Canvas counter unavailable-session/selector/disposal checks passed.");
    }
    internal static void RunNative()
    {
        RunManaged();
        var old = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var renderer = Environment.GetEnvironmentVariable("ELECTRON2D_CANVAS_COUNTER_RENDERER") ?? "gpu";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        try
        {
            var window = new Window { Size = new(128, 96), Title = "Electron2D canvas counters" }; var paint = new Paint { Records = 2 }; window.AddChild(paint);
            var sub = new SubViewport { Size = new(32, 32), RenderTargetUpdateMode = ViewportUpdateMode.Always }; sub.AddChild(new Paint { Records = 1 }); window.AddChild(sub);
            var driver = new Driver(window, sub, paint, renderer); window.AddChild(driver);
            window.Ready += _ => { Check(RenderingServer.GetCurrentRenderingMethod() == renderer, "Explicit native backend"); RenderingServer.RenderLoopEnabled = false; };
            Check(Engine.Run(window) == 0 && driver.Completed, "Native counter workflow completes");
            Check(RenderingServer.GetRenderingInfo(Total.TotalObjectsInFrame) == 0 && Performance.GetMonitor(Monitor.RenderTotalDrawCallsInFrame) == 0, "Session teardown retires aggregate counts");
            Console.WriteLine("Canvas counters native pixels, failure/publication, RID, mesh/instance and 64 active/idle zero-byte intervals passed: " + renderer);
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, old); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }
    private sealed class Paint : Entity
    {
        internal int Records;
        internal bool Fail;
        internal bool Lines;
        protected override void OnDraw()
        {
            if (Fail) throw new ApplicationException("canvas counter drawing");
            if (Lines) { DrawLine(new(4, 6), new(28, 6), Colors.Cyan, -1); return; }
            for (var i = 0; i < Records; i++) DrawRect(new(4 + 20 * i, 4, 16, 16), Colors.Cyan);
        }
    }
    private static (long Objects, long Primitives, long Calls) Values(Viewport view) => (view.GetRenderInfo(Domain.Canvas, Counter.ObjectsInFrame), view.GetRenderInfo(Domain.Canvas, Counter.PrimitivesInFrame), view.GetRenderInfo(Domain.Canvas, Counter.DrawCallsInFrame));
    private static void Counts(Viewport view, long objects, long primitives, long calls)
    {
        var values = Values(view); Check(values == (objects, primitives, calls), "Viewport counts " + values + " expected " + (objects, primitives, calls));
        var rid = view.GetViewportRID();
        Check(RenderingServer.ViewportGetRenderInfo(rid, Domain.Canvas, Counter.ObjectsInFrame) == objects && RenderingServer.ViewportGetRenderInfo(rid, Domain.Canvas, Counter.PrimitivesInFrame) == primitives && RenderingServer.ViewportGetRenderInfo(rid, Domain.Canvas, Counter.DrawCallsInFrame) == calls, "RID and scene count family agree");
    }
    private static void Totals(long objects, long primitives, long calls)
    {
        Check(RenderingServer.GetRenderingInfo(Total.TotalObjectsInFrame) == objects && RenderingServer.GetRenderingInfo(Total.TotalPrimitivesInFrame) == primitives && RenderingServer.GetRenderingInfo(Total.TotalDrawCallsInFrame) == calls, "Completed frame aggregates");
        Check(Performance.GetMonitor(Monitor.RenderTotalObjectsInFrame) == objects && Performance.GetMonitor(Monitor.RenderTotalPrimitivesInFrame) == primitives && Performance.GetMonitor(Monitor.RenderTotalDrawCallsInFrame) == calls, "Performance and renderer aggregate family agree");
    }
    private sealed class Driver(Window window, SubViewport sub, Paint paint, string renderer) : Node
    {
        internal bool Completed;
        private readonly string _renderer = renderer;
        protected override void OnReady() { ProcessEnabled = true; }
        protected override void OnProcess(double delta)
        {
            Counts(window, 0, 0, 0); Totals(0, 0, 0);
            RenderingServer.ForceDraw(false); Counts(window, 2, 4, 1); Counts(sub, 1, 2, 1); Totals(3, 6, 2);
            using (var image = window.GetTexture().GetImage()!)
            {
                var c = image.GetPixel(10, 10); Check(c.R < .1f && c.G > .9f && c.B > .9f, "Actual submitted cyan geometry pixels");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CANVAS_COUNTER_CAPTURE") is { } file) image.SavePNG(file);
            }
            Task.Run(() => { Totals(3, 6, 2); Reject<InvalidOperationException>(() => Values(window)); Reject<InvalidOperationException>(() => RenderingServer.ViewportGetRenderInfo(window.GetViewportRID(), Domain.Canvas, Counter.ObjectsInFrame)); }).GetAwaiter().GetResult();
            RenderingServer.ViewportSetUpdateMode(sub.GetViewportRID(), ViewportUpdateMode.Disabled); paint.Visible = false;
            RenderingServer.ForceDraw(false); Counts(window, 0, 0, 0); Counts(sub, 1, 2, 1); Totals(0, 0, 0);
            paint.Visible = true; paint.Fail = true; paint.QueueRedraw(); Reject<ApplicationException>(() => RenderingServer.ForceDraw(false)); Counts(window, 0, 0, 0); Totals(0, 0, 0);
            paint.Fail = false; paint.Records = 4; paint.QueueRedraw();
            var subPaint = (Paint)sub.GetChild(0); subPaint.Fail = true; subPaint.QueueRedraw();
            RenderingServer.ViewportSetUpdateMode(sub.GetViewportRID(), ViewportUpdateMode.Always);
            Reject<ApplicationException>(() => RenderingServer.ForceDraw(false)); Counts(window, 0, 0, 0); Counts(sub, 1, 2, 1); Totals(0, 0, 0);
            subPaint.Fail = false; subPaint.QueueRedraw(); RenderingServer.ViewportSetUpdateMode(sub.GetViewportRID(), ViewportUpdateMode.Disabled);
            paint.Records = 3; paint.QueueRedraw(); var callback = false;
            RenderingServer.RequestFrameDrawnCallback(() => { Counts(window, 3, 6, 1); Totals(3, 6, 1); callback = true; });
            RenderingServer.ForceDraw(false); Check(callback, "Counts publish before completion callbacks");
            Action fail = () => { Totals(3, 6, 1); throw new ApplicationException("post draw"); }; RenderingServer.FramePostDraw += fail;
            Reject<ApplicationException>(() => RenderingServer.ForceDraw(false)); RenderingServer.FramePostDraw -= fail; Counts(window, 3, 6, 1); Totals(3, 6, 1);
            paint.Lines = true; paint.QueueRedraw(); RenderingServer.ForceDraw(false); Counts(window, 1, 1, 1); Totals(1, 1, 1);
            paint.Lines = false; paint.Records = 0; paint.QueueRedraw(); RenderingServer.ForceDraw(false); Counts(window, 0, 0, 0); Totals(0, 0, 0);
            OwnedViewport(); MeshInstances();
            paint.Records = 2; paint.QueueRedraw(); RenderingServer.ViewportSetUpdateMode(sub.GetViewportRID(), ViewportUpdateMode.Always);
            for (var i = 0; i < 16; i++) { paint.QueueRedraw(); RenderingServer.ForceDraw(false); _ = Values(window); Totals(3, 6, 2); }
            var before = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) { paint.QueueRedraw(); RenderingServer.ForceDraw(false); _ = Values(window); _ = Performance.GetMonitor(Monitor.RenderTotalDrawCallsInFrame); }
            var ownerActive = GC.GetAllocatedBytesForCurrentThread() - before; var allActive = GC.GetTotalAllocatedBytes(true) - all;
            before = GC.GetAllocatedBytesForCurrentThread(); all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) { RenderingServer.ForceDraw(false); _ = Values(window); _ = RenderingServer.GetRenderingInfo(Total.TotalPrimitivesInFrame); }
            var ownerIdle = GC.GetAllocatedBytesForCurrentThread() - before; var allIdle = GC.GetTotalAllocatedBytes(true) - all;
            Check(ownerActive == 0 && ownerIdle == 0 && allActive == 0 && allIdle == 0, "Prepared active/idle owner/all bytes " + ownerActive + "/" + allActive + ", " + ownerIdle + "/" + allIdle);
            Console.WriteLine("64 " + _renderer + " counter/render active + idle intervals: owner/all " + ownerActive + "/" + allActive + ", " + ownerIdle + "/" + allIdle);
            Completed = true; window.Tree!.Quit();
        }
        private static void OwnedViewport()
        {
            var view = RenderingServer.ViewportCreate(); var canvas = RenderingServer.CanvasCreate(); var item = RenderingServer.CanvasItemCreate();
            try
            {
                RenderingServer.ViewportSetSize(view, 32, 32); RenderingServer.ViewportSetActive(view, true); RenderingServer.ViewportSetUpdateMode(view, ViewportUpdateMode.Always);
                RenderingServer.ViewportAttachCanvas(view, canvas); RenderingServer.CanvasItemSetParent(item, canvas); RenderingServer.CanvasItemAddRect(item, new(2, 2, 16, 16), Colors.Cyan);
                RenderingServer.ForceDraw(false);
                Check(RenderingServer.ViewportGetRenderInfo(view, Domain.Canvas, Counter.ObjectsInFrame) == 1 && RenderingServer.ViewportGetRenderInfo(view, Domain.Canvas, Counter.PrimitivesInFrame) == 2, "Owned low-level viewport and canvas execute counters"); Totals(1, 2, 1);
                Reject<ArgumentException>(() => RenderingServer.ViewportGetRenderInfo(item, Domain.Canvas, Counter.ObjectsInFrame));
                RenderingServer.FreeRID(view); Reject<ArgumentException>(() => RenderingServer.ViewportGetRenderInfo(view, Domain.Canvas, Counter.ObjectsInFrame)); view = default;
            }
            finally { if (view.IsValid()) RenderingServer.FreeRID(view); RenderingServer.FreeRID(item); RenderingServer.FreeRID(canvas); }
        }
        private void MeshInstances()
        {
            using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = [Vector2.Zero, new(10, 0), new(10, 10), new(0, 10)], Indices = [0, 1, 2, 0, 2, 3] });
            using var instances = new MultiMesh { Mesh = mesh, InstanceCount = 3, VisibleInstanceCount = 2 };
            instances.SetInstanceTransform2D(0, new(0, new(8, 8))); instances.SetInstanceTransform2D(1, new(0, new(30, 8)));
            var node = new MultiMeshInstance { MultiMesh = instances }; window.AddChild(node);
            RenderingServer.ForceDraw(false); var values = Values(window); Check(values.Objects > 0 && values.Primitives == 4 && values.Calls > 0, "Actual mesh instancing contributes requested primitive count");
            instances.VisibleInstanceCount = 0; RenderingServer.ForceDraw(false); Counts(window, 0, 0, 0);
            window.RemoveChild(node); node.Dispose();
            if (_renderer == "gpu")
            {
                var owned = RenderingServer.MultiMeshCreate(); var canvas = RenderingServer.CanvasCreate(); var item = RenderingServer.CanvasItemCreate();
                try
                {
                    RenderingServer.MultiMeshAllocateData(owned, 3, useIndirect: true); RenderingServer.MultiMeshSetMesh(owned, mesh.GetRID());
                    RenderingServer.MultiMeshSetCustomAABB(owned, new(0, 0, 64, 64)); RenderingServer.ViewportAttachCanvas(window.GetViewportRID(), canvas);
                    RenderingServer.CanvasItemSetParent(item, canvas); RenderingServer.CanvasItemAddMultiMesh(item, owned);
                    RenderingServer.ForceDraw(false); values = Values(window);
                    Check(values.Objects > 0 && values.Primitives == 6 && values.Calls > 0, "GPU indirect indexed capacity/base geometry budget: " + values);
                }
                finally { RenderingServer.FreeRID(item); RenderingServer.FreeRID(canvas); RenderingServer.FreeRID(owned); }
            }

        }
    }
}
