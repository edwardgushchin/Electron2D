using Electron2D;

internal static partial class LightingTests
{
    private static void NativeSDF()
    {
        using var input = typeof(LightingTests).Assembly.GetManifestResourceStream("TestShaders.SDFProbe.spv")!;
        using var memory = new MemoryStream(); input.CopyTo(memory); using var shader = Shader.CreateFromSPIRV(memory.ToArray());
        using var material = new ShaderMaterial { Shader = shader };
        Reject<ArgumentException>(() => material.SetShaderParameter("SDF_COORDS", Vector4.One));
        using var contour = new OccluderPolygon { Polygon = [new(10, 10), new(26, 10), new(26, 26), new(10, 26)] };
        using var tileContour = new OccluderPolygon { Polygon = [new(-8, -8), new(8, -8), new(8, 8), new(-8, 8)] };
        using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); image.Fill(Colors.White);
        using var texture = ImageTexture.CreateFromImage(image); using var source = new TileSetAtlasSource { Texture = texture, UseTexturePadding = false }; source.CreateTile(Vector2i.Zero);
        using var set = new TileSet(); set.AddOcclusionLayer(); set.AddSource(source, 0); var data = source.GetTileData(Vector2i.Zero, 0); data.AddOccluderPolygon(0); data.SetOccluderPolygon(0, 0, tileContour);
        var tile = new TileMapLayer { Name = "Tiles", TileSet = set }; tile.SetCell(new(3, 1), 0, Vector2i.Zero);
        var window = new Window { Size = new(80, 40), Title = "Canvas distance field validation", SDFScale = CanvasSDFScale.Percent100, SDFOversize = CanvasSDFOversize.Percent100 };
        var occluder = new LightOccluder { Occluder = contour }; window.AddChild(occluder); window.AddChild(tile); window.AddChild(new Surface { Name = "FieldProbe", Area = new(0, 0, 80, 40), Material = material, ZIndex = 100 });
        var frames = 0; long bytes = 0;
        window.Ready += _ => RenderingServer.FramePostDraw += () =>
        {
            frames++;
            if (frames < 7)
            {
                using var pixels = window.GetTexture().GetImage()!;
                var inside = pixels.GetPixel(18, 18); var tileInside = pixels.GetPixel(56, 24);
                Check(inside.R > .9f == (frames is 1 or 6), $"Scene SDF participation and sign at frame {frames}: {inside}");
                Check(tileInside.R > .9f == (frames == 2), $"Tile SDF policy and enablement at frame {frames}: {tileInside}");
                if (frames == 1) { Check(MathF.Abs(inside.G - 7.5f / 16) < .015f, "Signed distance preserves pixel units"); var outside = pixels.GetPixel(34, 18); Check(outside.R < .1f && MathF.Abs(outside.G - 8.5f / 16) < .015f, "Outside distance"); }
            }
            if (frames == 1) { occluder.SDFCollision = false; set.SetOcclusionLayerSDFCollision(0, true); }
            if (frames == 2) tile.OcclusionEnabled = false;
            if (frames == 3) { occluder.SDFCollision = true; contour.Closed = false; }
            if (frames == 4) { contour.Closed = true; occluder.Hide(); }
            if (frames == 5) { occluder.Show(); window.SDFScale = CanvasSDFScale.Percent50; window.SDFOversize = CanvasSDFOversize.Percent200; }
            if (frames == 16) bytes = GC.GetAllocatedBytesForCurrentThread();
            if (frames == 40) { bytes = GC.GetAllocatedBytesForCurrentThread() - bytes; Check(bytes == 0, $"Prepared SDF owner frames allocated {bytes} B"); window.Tree!.Quit(); }
        };
        Check(Engine.Run(window) == 0 && frames == 40, "SDF shader workflow completed");
        Console.WriteLine($"Canvas SDF pixels: signed distances, scene/tile policies, open contours, visibility, scale/oversize and 24 prepared owner frames={bytes} B passed.");
    }
}
