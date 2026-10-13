using Electron2D;

internal static class CanvasItemInterpolationTests
{
    internal static void RunManaged()
    {
        Reject<InvalidOperationException>(() => RenderingServer.CanvasItemSetInterpolated(default, true));
        Reject<InvalidOperationException>(() => RenderingServer.CanvasItemResetPhysicsInterpolation(default));
        var history = new CanvasTransformInterpolation { Enabled = true }; var a = new Transform(.2f, new(2, 3), .1f, new(4, 5)); var b = new Transform(.6f, new(3, -2), -.2f, new(20, 9));
        history.Reset(a); history.Tick(a, true); history.Tick(b, false);
        Check(history.Render(b, .5f, true).IsEqualApprox(a.InterpolateWith(b, .5f)), "Basis, skew and translation interpolate together.");
        Check(history.Render(b, .5f, false) == b, "Scene-wide disable returns canonical pose.");
        var delta = new Transform(.3f, new(2, 2), 0, new(-8, 3)); var canonical = history.Rebase(delta, b);
        Check(canonical == delta * b && history.Previous == delta * a && history.Current == delta * b, "Rebase moves previous/current/canonical together.");
        var saved = history; Reject<ArgumentException>(() => history.Rebase(new(float.NaN, Vector2.Zero), canonical)); Check(history.Previous == saved.Previous && history.Current == saved.Current, "Nonfinite rebase preserves history.");
        history.Reset(new(0, new(float.MaxValue, 0))); saved = history;
        Reject<ArgumentException>(() => history.Rebase(new(0, Vector2.One * 2, 0, Vector2.Zero), history.Current)); Check(history.Current == saved.Current && history.Previous == saved.Previous, "Overflow rejection preserves both endpoints.");
        history.Reset(a); var zero = new Transform(0, Vector2.Zero, 0, Vector2.Zero); Check(history.Rebase(zero, a) == zero && history.Current == zero && history.Previous == zero, "Finite singular rebase is valid.");
        using var parent = new Entity(); var child = new Entity(); parent.AddChild(child); child.ServerState = new(null!, false) { Interpolated = false };
        parent.PhysicsInterpolationMode = PhysicsInterpolationMode.Off; Check(child.ServerState.Interpolated is null, "An authored ancestor policy republishes inheriting scene items.");
        child.ServerState.Interpolated = true; child.ServerState.Transform = new(0, new(90, 50));
        using var packed = new PackedScene(); packed.Pack(child); using var restored = (Entity)packed.Instantiate(); Check(restored.ServerState is null && restored.Transform == Transform.Identity, "Session interpolation and retained poses stay out of scene storage.");
        for (var i = 0; i < 64; i++) { history.Tick(a, true); history.Tick(b, false); _ = history.Render(b, .5f, true); }
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1024; i++) { history.Tick(a, true); history.Tick(b, false); _ = history.Render(b, .5f, true); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "1024 warmed history cycles allocate zero managed bytes.");
        PhysicsInterpolationTests.Run();
        Console.WriteLine("Retained canvas interpolation math, guards, policy/storage and 1024 warm cycles passed.");
    }
    private sealed class FaultyTransform : Entity
    {
        internal bool Fail;
        public override Transform GetTransform() => Fail ? throw new InvalidOperationException("Transform capture failure.") : base.GetTransform();
    }
    private sealed class Paint : Entity { protected override void OnDraw() => DrawRect(new(0, 0, 8, 8), Colors.Blue); }
    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, true);
        Engine.PhysicsTicksPerSecond = 20; Engine.MaxFPS = 120;
        var window = new Window { Size = new(160, 96) }; var view = new SubViewport { Size = new(160, 96), RenderTargetUpdateMode = ViewportUpdateMode.Always }; window.AddChild(view);
        var borrowed = new Paint { Position = new(20, 48), PhysicsInterpolationMode = PhysicsInterpolationMode.Off }; window.AddChild(borrowed);
        using var rootTexture = window.GetTexture(); using var otherTexture = view.GetTexture();
        RID canvas = default, parent = default, child = default, holder = default; var frame = 0; var tick = 0; var previous = 20f; var current = 20f; var childPrevious = 4f; var childCurrent = 4f; var borrowedPrevious = 20f; var borrowedCurrent = 20f; var shift = 0f; var origin = 0f; var parentInterpolated = true; var sourceInterpolated = true; var active = true; var parentAlive = true; var samples = 0; var intermediate = 0;
        var warm = 0; long before = 0, activeBytes = 0, idleBytes = 0; Vector2 lastSize = default, frameSize = default;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Exact requested backend."); RenderingServer.SetDefaultClearColor(Colors.Black);
            canvas = RenderingServer.CanvasCreate(); parent = RenderingServer.CanvasItemCreate(); child = RenderingServer.CanvasItemCreate(); holder = RenderingServer.CanvasItemCreate();
            foreach (var target in new[] { window.GetViewportRID(), view.GetViewportRID() }) RenderingServer.ViewportAttachCanvas(target, canvas);
            RenderingServer.CanvasItemSetParent(parent, canvas); RenderingServer.CanvasItemSetParent(holder, canvas); RenderingServer.CanvasItemSetParent(child, parent);
            RenderingServer.CanvasItemSetTransform(parent, new(0, new(current, 8))); RenderingServer.CanvasItemSetTransform(child, new(0, new(childCurrent, 24)));
            RenderingServer.CanvasItemAddRect(parent, new(0, 0, 8, 8), Colors.Red); RenderingServer.CanvasItemAddRect(child, new(0, 0, 8, 8), Colors.Lime);
            RenderingServer.CanvasItemSetInterpolated(borrowed.GetCanvasItem(), true);
            RenderingServer.CanvasItemResetPhysicsInterpolation(parent); RenderingServer.CanvasItemResetPhysicsInterpolation(child); RenderingServer.CanvasItemResetPhysicsInterpolation(borrowed.GetCanvasItem());
            Reject<ArgumentException>(() => RenderingServer.CanvasItemSetInterpolated(canvas, true));
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.CanvasItemResetPhysicsInterpolation(parent)).GetAwaiter().GetResult());
            var bad = Transform.Identity; bad.Origin.X = float.NaN; Reject<ArgumentException>(() => RenderingServer.CanvasItemTransformPhysicsInterpolation(parent, bad));
            var faulty = new FaultyTransform(); var later = new Entity { Position = new(3, 4) };
            window.AddChild(faulty); window.AddChild(later);
            RenderingServer.CanvasItemSetInterpolated(faulty.GetCanvasItem(), true); RenderingServer.CanvasItemSetInterpolated(later.GetCanvasItem(), true);
            var service = RenderingServer.Service!; faulty.Fail = true;
            later.ServerState!.Interpolation.Reset(Transform.Identity);
            Reject<AggregateException>(() => service.ResetCanvasItemInterpolation(window.Tree!));
            Check(later.ServerState.Interpolation.Current == later.Transform, "A failed retained getter does not skip later resets.");
            later.ServerState.Interpolation.Current = Transform.Identity;
            Reject<AggregateException>(() => service.CanvasItemPhysicsTick(window.Tree!, false));
            Check(later.ServerState.Interpolation.Current == later.Transform, "A failed retained getter does not skip later captures.");
            Reject<AggregateException>(() => window.Tree!.PhysicsInterpolation = false);
            Check(!window.Tree!.PhysicsInterpolation, "Global mode change commits and attempts all resets despite a failed getter.");
            Reject<AggregateException>(() => window.PhysicsInterpolationMode = PhysicsInterpolationMode.Off);
            Check(later.ServerState.Interpolated is null, "A failed source getter does not skip later policy publication.");
            faulty.Fail = false; window.PhysicsInterpolationMode = PhysicsInterpolationMode.On;
            RenderingServer.CanvasItemSetInterpolated(borrowed.GetCanvasItem(), true);
            window.Tree.PhysicsInterpolation = true; faulty.QueueFree(); later.QueueFree();
            window.Tree.PhysicsFrameStarted += _ =>
            {
                tick++; previous = current; current = (tick % 2 == 0 ? 20 : 44) + shift; childPrevious = childCurrent; childCurrent = tick % 2 == 0 ? 4 : 12;
                borrowedPrevious = borrowedCurrent; borrowedCurrent = tick % 2 == 0 ? 20 : 44;
                if (parentAlive) { RenderingServer.CanvasItemSetTransform(parent, new(0, new(current, 8))); RenderingServer.CanvasItemSetTransform(child, new(0, new(childCurrent, 24))); }
                if (frame < 150 || warm < 96) borrowed.Position = new(borrowedCurrent, 48);
                else borrowedCurrent = borrowed.Position.X;
            };
            RenderingServer.FramePreDraw += () =>
            {
                if (frame < 150) return; frameSize = rootTexture.GetSize(); before = GC.GetAllocatedBytesForCurrentThread();
                if (warm < 96) RenderingServer.CanvasItemSetInterpolated(borrowed.GetCanvasItem(), true);
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (frame >= 150)
                {
                    var size = rootTexture.GetSize(); if (size != lastSize || size != frameSize) { warm = 0; activeBytes = idleBytes = 0; lastSize = size; }
                    else if (warm >= 32 && warm < 96) activeBytes += GC.GetAllocatedBytesForCurrentThread() - before;
                    else if (warm >= 96) idleBytes += GC.GetAllocatedBytesForCurrentThread() - before;
                    if (++warm == 160) { Check(activeBytes == 0 && idleBytes == 0, $"64 active/64 idle retained interpolation render intervals: {activeBytes}/{idleBytes} bytes."); RenderingServer.FreeRID(holder); RenderingServer.FreeRID(canvas); window.Tree.Quit(); }
                    if (++frame > 2048) throw new Exception("Stable interpolation warmup not reached."); return;
                }
                var fraction = (float)Engine.PhysicsInterpolationFraction;
                if (frame >= 8 && parentAlive)
                {
                    var x = origin + (active && parentInterpolated ? Mathf.Lerp(previous, current, fraction) : current);
                    var childX = x + (active ? Mathf.Lerp(childPrevious, childCurrent, fraction) : childCurrent);
                    using var pixels = rootTexture.GetImage()!; using var other = otherTexture.GetImage()!;
                    try { Pixel(pixels, (int)MathF.Floor(x + 4), 12, Colors.Red); }
                    catch (Exception error) { throw new Exception($"Retained frame={frame}, tick={tick}, fraction={fraction}, expectedX={x}, snapshots={previous}/{current}, parentMode={parentInterpolated}, active={active}, target={pixels.Width}x{pixels.Height}, window={window.Size}, rootRect={window.GetVisibleRect()}.", error); }
                    Pixel(other, (int)MathF.Floor(x + 4), 12, Colors.Red);
                    Pixel(pixels, (int)MathF.Floor(childX + 4), 36, Colors.Lime); Pixel(other, (int)MathF.Floor(childX + 4), 36, Colors.Lime);
                    var bx = active && sourceInterpolated ? Mathf.Lerp(borrowedPrevious, borrowedCurrent, fraction) : borrowedCurrent;
                    Pixel(pixels, (int)MathF.Floor(bx + 4), 52, Colors.Blue); samples++; if (active && fraction is > .05f and < .95f) intermediate++;
                }
                switch (frame++)
                {
                    case 40: RenderingServer.CanvasItemTransformPhysicsInterpolation(parent, new(0, new(8, 0))); shift += 8; previous += 8; current += 8; break;
                    case 50: RenderingServer.CanvasItemResetPhysicsInterpolation(parent); previous = current; break;
                    case 60: RenderingServer.CanvasItemSetInterpolated(parent, false); parentInterpolated = false; break;
                    case 70: window.Tree.PhysicsInterpolation = false; active = false; break;
                    case 80: window.Tree.PhysicsInterpolation = true; active = true; previous = current; childPrevious = childCurrent; borrowedPrevious = borrowedCurrent; break;
                    case 90: RenderingServer.CanvasItemSetTransform(holder, new(0, new(8, 0))); RenderingServer.CanvasItemResetPhysicsInterpolation(holder); RenderingServer.CanvasItemSetParent(parent, holder); origin = 8; break;
                    case 100: borrowed.PhysicsInterpolationMode = PhysicsInterpolationMode.On; sourceInterpolated = true; borrowedPrevious = borrowedCurrent; Check(borrowed.ServerState!.Interpolated is null, "Matching source policy republishes server override."); break;
                    case 110: borrowed.PhysicsInterpolationMode = PhysicsInterpolationMode.Off; sourceInterpolated = false; break;
                    case 130: RenderingServer.FreeRID(parent); RenderingServer.FreeRID(child); parentAlive = false; Reject<ArgumentException>(() => RenderingServer.CanvasItemResetPhysicsInterpolation(parent)); break;
                    case 149: Check(samples > 60 && intermediate > 20, "Actual physics ticks and intermediate native presentation."); break;
                }
            };
        };
        Check(Engine.Run(window) == 0 && warm == 160, "Retained interpolation host completed."); Check(window.IsDisposed && borrowed.ServerState is null, "Session history cleanup.");
        if (backend == "gpu") Siblings();
        Console.WriteLine($"Retained canvas interpolation native pixels passed ({backend}), samples={samples}, intermediate={intermediate}, warm={activeBytes}/{idleBytes} bytes.");
    }
    private sealed class Surface : Entity { protected override void OnDraw() => DrawRect(new(0, 0, 96, 32), Colors.White); }
    private static void Siblings()
    {
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White);
        using var texture = ImageTexture.CreateFromImage(image); texture.SetSizeOverride(new(64, 32));
        using var polygon = new OccluderPolygon { Closed = false, Polygon = [new(0, 0), new(0, 32)] };
        var window = new Window { Size = new(160, 96) }; window.AddChild(new Surface()); using var target = window.GetTexture();
        RID light = default, occluder = default; var phase = 0; ulong start = 0; var frames = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); var canvas = window.FindWorld()!.Canvas; RenderingServer.CanvasSetModulate(canvas, Colors.Black);
            light = RenderingServer.CanvasLightCreate(); RenderingServer.CanvasLightAttachToCanvas(light, canvas); RenderingServer.CanvasLightSetTexture(light, texture.GetRID());
            RenderingServer.CanvasLightSetTransform(light, new(0, new(32, 16))); RenderingServer.CanvasLightSetShadowEnabled(light, true); RenderingServer.CanvasLightSetInterpolated(light, true);
            occluder = RenderingServer.CanvasLightOccluderCreate(); RenderingServer.CanvasLightOccluderAttachToCanvas(occluder, canvas); RenderingServer.CanvasLightOccluderSetPolygon(occluder, polygon.GetRID());
            RenderingServer.CanvasLightOccluderSetTransform(occluder, new(0, new(8, 0))); RenderingServer.CanvasLightOccluderSetInterpolated(occluder, true);
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = target.GetImage()!; frames++;
                switch (phase)
                {
                    case 0: Pixel(pixels, 16, 16, Colors.White); RenderingServer.CanvasLightOccluderTransformPhysicsInterpolation(occluder, new(0, new(16, 0))); phase++; break;
                    case 1: Pixel(pixels, 16, 16, Colors.Black); window.Tree!.PhysicsInterpolation = false; phase++; break;
                    case 2: Pixel(pixels, 16, 16, Colors.Black); RenderingServer.CanvasLightTransformPhysicsInterpolation(light, new(0, new(16, 0))); phase++; break;
                    case 3: Pixel(pixels, 76, 16, Colors.White); Pixel(pixels, 16, 16, Colors.Black); window.Tree!.PhysicsInterpolation = true; start = window.Tree.PhysicsFrameCount; phase++; break;
                    case 4:
                        Pixel(pixels, 76, 16, Colors.White); Pixel(pixels, 16, 16, Colors.Black);
                        if (window.Tree!.PhysicsFrameCount >= start + 2) { RenderingServer.FreeRID(occluder); RenderingServer.FreeRID(light); window.Tree.Quit(); phase++; }
                        if (frames > 256) throw new Exception("Sibling rebase tick continuation timed out."); break;
                }
            };
        };
        Check(Engine.Run(window) == 0 && phase == 5, "Light/occluder rebases survive disable/enable and later ticks.");
        Console.WriteLine("Light and occluder canonical/history rebase pixels passed.");
    }
    private static void Pixel(Image image, int x, int y, Color expected) { var value = image.GetPixel(x, y); Check(MathF.Abs(value.R - expected.R) < .03f && MathF.Abs(value.G - expected.G) < .03f && MathF.Abs(value.B - expected.B) < .03f, $"Pixel {x},{y}: {value}, expected {expected}."); }
    private static void Reject<T>(Action call) where T : Exception { try { call(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}."); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
