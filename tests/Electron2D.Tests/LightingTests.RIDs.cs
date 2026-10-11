using Electron2D;

internal static partial class LightingTests
{
    private static void NativeRIDs()
    {
        using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); image.Fill(Colors.White);
        using var mask = ImageTexture.CreateFromImage(image); using var borrowed = new OccluderPolygon { Polygon = [new(24, 0), new(24, 32)], Closed = false };
        var window = new Window { Size = new(64, 32), Title = "Retained lighting validation" }; var surface = new Surface { Area = new(0, 0, 64, 32) }; window.AddChild(surface);
        var frames = 0; long bytes = 0; RID light = default, occluder = default, polygon = default, texture = default;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.CanvasSetModulate(window.FindWorld()!.Canvas, new(.1f, .1f, .1f));
            light = RenderingServer.CanvasLightCreate(); RenderingServer.CanvasLightAttachToCanvas(light, window.FindWorld()!.Canvas);
            RenderingServer.CanvasLightSetTexture(light, mask.GetRID()); RenderingServer.CanvasLightSetTextureScale(light, 4); RenderingServer.CanvasLightSetTextureOffset(light, Vector2.Zero); RenderingServer.CanvasLightSetTransform(light, new(0, new Vector2(32, 16)));
            RenderingServer.CanvasLightSetEnergy(light, .2f); RenderingServer.CanvasLightSetColor(light, Colors.Red); RenderingServer.CanvasLightSetBlendMode(light, LightBlendMode.Add); RenderingServer.CanvasLightSetItemCullMask(light, uint.MaxValue); RenderingServer.CanvasLightSetItemShadowCullMask(light, 1); RenderingServer.CanvasLightSetLayerRange(light, 0, 0); RenderingServer.CanvasLightSetZRange(light, -1, 1);
            RenderingServer.CanvasLightSetHeight(light, 8); RenderingServer.CanvasLightSetShadowFilter(light, LightShadowFilter.None); RenderingServer.CanvasLightSetShadowSmooth(light, 0); RenderingServer.CanvasLightSetShadowColor(light, default);
            polygon = RenderingServer.CanvasOccluderPolygonCreate(); RenderingServer.CanvasOccluderPolygonSetShape(polygon, borrowed.Polygon, false); RenderingServer.CanvasOccluderPolygonSetCullMode(polygon, OccluderCullMode.Disabled);
            Reject<InvalidOperationException>(() => RenderingServer.CanvasOccluderPolygonSetCullMode(borrowed.GetRID(), OccluderCullMode.Disabled)); Reject<InvalidOperationException>(() => RenderingServer.FreeRID(borrowed.GetRID()));
            occluder = RenderingServer.CanvasLightOccluderCreate(); RenderingServer.CanvasLightOccluderAttachToCanvas(occluder, window.FindWorld()!.Canvas); RenderingServer.CanvasLightOccluderSetPolygon(occluder, polygon); RenderingServer.CanvasLightOccluderSetAsSDFCollision(occluder, false); RenderingServer.CanvasLightOccluderSetLightMask(occluder, 1); RenderingServer.CanvasLightOccluderSetTransform(occluder, Transform.Identity);
            texture = RenderingServer.CanvasTextureCreate(); RenderingServer.CanvasTextureSetChannel(texture, CanvasTextureChannel.Diffuse, mask.GetRID()); RenderingServer.CanvasTextureSetShadingParameters(texture, Colors.White, 1); RenderingServer.CanvasTextureSetTextureFilter(texture, TextureFilter.Nearest); RenderingServer.CanvasTextureSetTextureRepeat(texture, TextureRepeat.Disabled);
            RenderingServer.CanvasItemClear(surface.GetCanvasItem()); RenderingServer.CanvasItemAddTextureRect(surface.GetCanvasItem(), new(0, 0, 64, 32), texture); RenderingServer.CanvasItemSetLightMask(surface.GetCanvasItem(), 1);
            RenderingServer.CanvasLightSetInterpolated(light, true); RenderingServer.CanvasLightResetPhysicsInterpolation(light); RenderingServer.CanvasLightTransformPhysicsInterpolation(light, Transform.Identity); RenderingServer.CanvasLightOccluderSetInterpolated(occluder, true); RenderingServer.CanvasLightOccluderResetPhysicsInterpolation(occluder); RenderingServer.CanvasLightOccluderTransformPhysicsInterpolation(occluder, Transform.Identity);
            RenderingServer.ViewportSetSDFOversizeAndScale(window.GetViewportRID(), CanvasSDFOversize.Percent100, CanvasSDFScale.Percent100);
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.CanvasLightSetShadowFilter(light, LightShadowFilter.Max)); Reject<ArgumentException>(() => RenderingServer.CanvasLightSetTransform(light, new(new(float.NaN, 0), Vector2.Down, Vector2.Zero)));
            RenderingServer.FramePostDraw += () =>
            {
                frames++;
                if (frames <= 4) { using var pixels = window.GetTexture().GetImage()!; Pixel(pixels, 16, 16, frames switch { 1 or 4 => new(.3f, .1f, .1f), _ => new(.1f, .1f, .1f) }); }
                if (frames == 1) RenderingServer.CanvasLightSetShadowEnabled(light, true);
                if (frames == 2) RenderingServer.CanvasLightSetEnabled(light, false);
                if (frames == 3) { RenderingServer.CanvasLightSetEnabled(light, true); RenderingServer.CanvasLightSetMode(light, CanvasLightMode.Directional); RenderingServer.CanvasLightSetShadowEnabled(light, false); }
                if (frames == 16) bytes = GC.GetAllocatedBytesForCurrentThread();
                if (frames == 40)
                {
                    bytes = GC.GetAllocatedBytesForCurrentThread() - bytes; Check(bytes == 0, $"Prepared retained-light frames allocated {bytes} B");
                    RenderingServer.CanvasItemClear(surface.GetCanvasItem()); RenderingServer.FreeRID(light); RenderingServer.FreeRID(occluder); RenderingServer.FreeRID(polygon); RenderingServer.FreeRID(texture);
                    Reject<ArgumentException>(() => RenderingServer.CanvasLightSetEnabled(light, true)); Check(!mask.IsDisposed && !borrowed.IsDisposed, "Owned teardown preserves borrowed resources"); window.Tree!.Quit();
                }
            };
        };
        Check(Engine.Run(window) == 0 && frames == 40, "Retained lighting workflow completed");
        Console.WriteLine($"Retained lighting: static APIs, point/directional pixels, contour/texture lifetimes, validation and 24 prepared owner frames={bytes} B passed.");
    }
}
