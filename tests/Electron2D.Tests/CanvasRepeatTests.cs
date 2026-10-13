using Electron2D;

internal static class CanvasRepeatTests
{
    internal static void Run(string backend)
    {
        using (var source = new Parallax())
        {
            source.ServerState = new(null!, false) { ItemRepeatSize = new(17, 19), ItemRepeatTimes = 4 };
            source.RepeatSize = new(8, 0); Check(source.ServerState.ItemRepeatSize is null, "Authored parallax size republishes its repetition.");
            source.ServerState.ItemRepeatSize = new(17, 19); source.RepeatTimes = 3; Check(source.ServerState.ItemRepeatSize is null, "Authored parallax count republishes its repetition.");
        }
        using (var source = new ParallaxLayer()) { source.ServerState = new(null!, false) { ItemRepeatSize = new(17, 19), ItemRepeatTimes = 4 }; source.MotionMirroring = new(8, 0); Check(source.ServerState.ItemRepeatSize is null, "Legacy parallax authored period republishes."); }
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        var window = new Window { Size = new(128, 96) }; var sub = new SubViewport { Size = new(128, 96), RenderTargetUpdateMode = ViewportUpdateMode.Always }; window.AddChild(sub);
        RID canvas = default, root = default, child = default, nested = default; var phase = 0; long before = 0, allocated = 0; var entered = 0; var exited = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            canvas = RenderingServer.CanvasCreate(); RenderingServer.ViewportAttachCanvas(window.GetViewportRID(), canvas); RenderingServer.ViewportAttachCanvas(sub.GetViewportRID(), canvas);
            root = RenderingServer.CanvasItemCreate(); child = RenderingServer.CanvasItemCreate(); nested = RenderingServer.CanvasItemCreate();
            RenderingServer.CanvasItemSetParent(root, canvas); RenderingServer.CanvasItemSetParent(child, root); RenderingServer.CanvasItemSetParent(nested, child);
            RenderingServer.CanvasItemSetTransform(root, new(0, new(48, 16)));
            RenderingServer.CanvasItemAddRect(child, new(0, 0, 4, 4), Colors.Red); RenderingServer.CanvasItemSetTransform(nested, new(0, new(0, 8))); RenderingServer.CanvasItemAddRect(nested, new(0, 0, 4, 4), Colors.Blue);
            RenderingServer.CanvasSetItemRepeat(root, new(16, 0), 2);
            RenderingServer.CanvasItemSetVisibilityNotifier(child, true, new(0, 0, 4, 4), () => entered++, () => exited++);
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.CanvasSetItemRepeat(root, new(16, 0), -1));
            Reject<ArgumentException>(() => RenderingServer.CanvasSetItemRepeat(root, new(float.NaN, 0), 2));
            Reject<ArgumentException>(() => RenderingServer.CanvasSetItemRepeat(canvas, new(16, 0), 2));
            Reject<ArgumentException>(() => RenderingServer.CanvasSetItemMirroring(canvas, child, new(16, 0)));
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.CanvasSetItemRepeat(root, new(16, 0), 2)).GetAwaiter().GetResult());
            RenderingServer.FramePreDraw += () => { if (phase >= 20) { before = GC.GetAllocatedBytesForCurrentThread(); RenderingServer.CanvasSetItemRepeat(root, new(16, 0), 2 + (phase & 1)); } };
            RenderingServer.FramePostDraw += () =>
            {
                if (phase >= 20) { allocated += GC.GetAllocatedBytesForCurrentThread() - before; if (++phase == 84) { Check(entered == 1 && exited == 0, "Repeated visibility is OR-combined across submitted viewports."); Check(allocated == 0, "64 warmed mutation/render intervals allocate zero managed bytes."); RenderingServer.FreeRID(root); RenderingServer.FreeRID(child); RenderingServer.FreeRID(nested); RenderingServer.FreeRID(canvas); window.Tree!.Quit(); } return; }
                using var image = window.GetTexture().GetImage()!; using var second = sub.GetTexture().GetImage()!;
                if (phase < 2) { foreach (var x in new[] { 33, 49, 65 }) { Pixel(image, x, 17, Colors.Red); Pixel(image, x, 25, Colors.Blue); Pixel(second, x, 17, Colors.Red); } }
                switch (phase++)
                {
                    case 0: RenderingServer.CanvasSetItemRepeat(root, new(-16, 0), 2); break;
                    case 1: RenderingServer.CanvasSetItemRepeat(root, new(16, 0), 3); break;
                    case 2: Pixel(image, 33, 17, Colors.Red); Pixel(image, 81, 17, Colors.Red); Pixel(image, 17, 17, Colors.Black); RenderingServer.CanvasSetItemRepeat(child, new(0, 16), 2); break;
                    case 3: Pixel(image, 49, 1, Colors.Red); Pixel(image, 49, 17, Colors.Red); Pixel(image, 49, 33, Colors.Red); Pixel(image, 33, 17, Colors.Black); RenderingServer.CanvasSetItemRepeat(child, Vector2.Zero, 2); break;
                    case 4: Pixel(image, 33, 17, Colors.Red); Pixel(image, 81, 17, Colors.Red); RenderingServer.CanvasSetItemRepeat(root, new(16, 0), 0); break;
                    case 5: Pixel(image, 49, 17, Colors.Red); Pixel(image, 33, 17, Colors.Black); RenderingServer.CanvasSetItemMirroring(canvas, root, new(16, 0)); break;
                    case 6: Pixel(image, 49, 17, Colors.Red); Pixel(image, 65, 17, Colors.Red); Pixel(image, 33, 17, Colors.Black); RenderingServer.CanvasItemSetTransform(root, new(Mathf.Pi / 2, new(48, 16))); break;
                    case 7: Pixel(image, 46, 18, Colors.Red); Pixel(image, 46, 34, Colors.Red); Pixel(image, 62, 18, Colors.Black); RenderingServer.CanvasItemSetTransform(root, new(0, new(48, 16))); RenderingServer.CanvasSetItemRepeat(root, new(16, 16), 2); break;
                    case 8: foreach (var y in new[] { 1, 17, 33 }) foreach (var x in new[] { 33, 49, 65 }) Pixel(image, x, y, Colors.Red); RenderingServer.CanvasSetItemRepeat(root, new(16, 0), int.MaxValue); break;
                    case 9: Pixel(image, 1, 17, Colors.Red); Pixel(image, 113, 17, Colors.Red); RenderingServer.CanvasSetItemRepeat(root, new(16, 0), 2); RenderingServer.CanvasItemSetCustomRect(child, true, new(0, 0, 4, 4)); RenderingServer.CanvasItemSetTransform(root, new(0, new(-16, 16))); break;
                    case 10: Pixel(image, 1, 17, Colors.Red); Pixel(image, 17, 17, Colors.Black); RenderingServer.CanvasItemSetTransform(root, new(0, new(48, 16))); RenderingServer.CanvasItemSetParent(child, canvas); break;
                    case 11: Pixel(image, 1, 1, Colors.Red); Pixel(image, 33, 17, Colors.Black); RenderingServer.CanvasItemSetParent(child, root); RenderingServer.CanvasItemSetCustomRect(root, true, new(0, 0, 4, 4)); RenderingServer.CanvasItemSetClip(root, true); break;
                    case 12: Pixel(image, 49, 17, Colors.Red); Pixel(image, 33, 17, Colors.Black); Pixel(image, 49, 25, Colors.Black); RenderingServer.CanvasItemSetClip(root, false); RenderingServer.CanvasItemSetSelfModulate(child, new(1, 1, 1, .5f)); RenderingServer.CanvasSetItemRepeat(root, new(1, 0), 2); break;
                    case 13: Pixel(image, 49, 17, new(.875f, 0, 0)); RenderingServer.CanvasItemSetSelfModulate(child, Colors.White); RenderingServer.CanvasItemSetTransform(root, new(new Vector2(2, 0), new Vector2(.5f, 1), new Vector2(48, 16))); RenderingServer.CanvasSetItemRepeat(root, new(16, 0), 2); break;
                    case 14: Pixel(image, 18, 17, Colors.Red); Pixel(image, 50, 17, Colors.Red); Pixel(image, 82, 17, Colors.Red); RenderingServer.CanvasItemSetTransform(root, new(0, new(48, 16))); break;
                }
            };
        };
        Check(Engine.Run(window) == 0 && phase == 84 && !RenderingServer.IsAvailable, "Repeat host teardown.");
        Console.WriteLine("Canvas repeats: inherited/nested, zero/negative/odd/grid counts, int-max culling, local rotation, mirroring, custom bounds/clips, reparenting, two viewports and 64 zero-byte mutation/render intervals passed: " + backend);
    }
    private static void Pixel(Image image, int x, int y, Color expected) { var actual = image.GetPixel(x, y); Check(MathF.Abs(actual.R - expected.R) < .06f && MathF.Abs(actual.G - expected.G) < .06f && MathF.Abs(actual.B - expected.B) < .06f, $"Pixel {x},{y}: {actual} expected {expected}"); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException(typeof(T).Name + " required."); }
}
