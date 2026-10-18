using System.Buffers.Binary;
using Electron2D;
using static TextureArrayTests;

internal static partial class MeshTextureTests
{
    private sealed class VirtualTexture : Texture
    {
        internal int Calls;
        public override int GetWidth() => 8;
        public override int GetHeight() => 8;
        public override void DrawRect(CanvasItem item, Rect2 rect, bool tile, Color? modulate = null, bool transpose = false) { Calls++; item.DrawRect(rect, Colors.Yellow); }
        public override void DrawRectRegion(CanvasItem item, Rect2 rect, Rect2 source, Color? modulate = null, bool transpose = false, bool clipUV = true) { Calls++; item.DrawRect(rect, Colors.Cyan); }
    }
    private static void Pixel(Image image, int x, int y, Color expected)
    {
        var actual = image.GetPixel(x, y);
        Check(MathF.Abs(actual.R - expected.R) < .01f && MathF.Abs(actual.G - expected.G) < .01f && MathF.Abs(actual.B - expected.B) < .01f && MathF.Abs(actual.A - expected.A) < .01f, $"MeshTexture pixel {x},{y}: {actual}, expected {expected}.");
    }
    private static void Native(string profile)
    {
        var compatibility = profile.StartsWith("compatibility", StringComparison.Ordinal);
        var programmable = profile.Contains("glsl", StringComparison.Ordinal) || profile.Contains("hlsl", StringComparison.Ordinal) || profile.EndsWith("shader", StringComparison.Ordinal);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, compatibility ? "compatibility" : "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var mesh = Triangle(); using var replacement = Triangle(); using var source = Solid(Colors.Red); using var green = Solid(Colors.Green);
        using var view = new MeshTexture { Mesh = mesh, BaseTexture = source, ImageSize = new(16, 8) };
        using var shader = programmable ? Load(profile.Contains("hlsl", StringComparison.Ordinal) ? "CanvasHLSL" : "CanvasGLSL") : null;
        using var material = programmable ? new ShaderMaterial { Shader = shader } : null;
        if (material is not null) { mesh.SurfaceSetMaterial(0, material); replacement.SurfaceSetMaterial(0, material); }
        using var root = new Window { Size = new(400, 176), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        Action<Probe>[] draws = [
            n => view.Draw(n, default),
            n => view.DrawRect(n, new(0, 0, 32, 24), false),
            n => view.DrawRect(n, new(0, 0, 32, 24), true),
            n => view.DrawRect(n, new(0, 0, 32, 24), false, transpose: true),
            n => view.DrawRect(n, new(32, 0, -32, 24), false),
            n => view.DrawRect(n, new(0, 24, 32, -24), false),
            n => view.DrawRect(n, new(32, 24, -32, -24), false),
            n => view.DrawRectRegion(n, new(0, 0, 32, 24), new(100, -200, 0, 0)),
            n => view.Draw(n, default, transpose: true)];
        Vector2i[] inside = [new(2, 2), new(2, 2), new(2, 2), new(3, 3), new(42, 2), new(2, 28), new(42, 28), new(2, 2), new(2, 2)];
        Vector2i[] outside = [new(14, 6), new(28, 18), new(28, 18), new(42, 12), new(20, 20), new(28, 12), new(20, 12), new(28, 18), new(14, 6)];
        var probes = draws.Select((draw, i) => new Probe(view) { Action = draw, Position = new(8 + i % 5 * 72, 8 + i / 5 * 48) }).ToArray();
        foreach (var probe in probes) root.AddChild(probe);
        var sprite = new Sprite { Texture = view, Centered = false, Position = new(8, 112) }; root.AddChild(sprite);
        var control = new TextureRect { Texture = view, Position = new(80, 112), Size = new(32, 24), ExpandMode = TextureRectExpandMode.IgnoreSize }; root.AddChild(control);
        var serverRect = new Probe(view) { Position = new(152, 112), Action = n => RenderingServer.CanvasItemAddTextureRect(n.GetCanvasItem(), new(0, 0, 32, 24), view.GetRID(), tile: true) };
        var serverRegion = new Probe(view) { Position = new(224, 112), Action = n => RenderingServer.CanvasItemAddTextureRectRegion(n.GetCanvasItem(), new(0, 0, 32, 24), view.GetRID(), new(100, -200, 0, 0)) }; root.AddChild(serverRect); root.AddChild(serverRegion);
        using var virtualTexture = new VirtualTexture(); var virtualNode = new Probe(view) { Position = new(296, 112), Action = n => { RenderingServer.CanvasItemAddTextureRect(n.GetCanvasItem(), new(0, 0, 12, 12), virtualTexture.GetRID()); RenderingServer.CanvasItemAddTextureRectRegion(n.GetCanvasItem(), new(16, 0, 12, 12), virtualTexture.GetRID(), new(0, 0, 1, 1)); } }; root.AddChild(virtualNode);
        using var uvMesh = Triangle(); using var uvImage = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        uvImage.SetPixel(0, 0, Colors.Red); uvImage.SetPixel(1, 0, Colors.Green); uvImage.SetPixel(0, 1, Colors.Blue); uvImage.SetPixel(1, 1, Colors.White);
        using var uvTexture = ImageTexture.CreateFromImage(uvImage); using var uvView = new MeshTexture { Mesh = uvMesh, BaseTexture = uvTexture, ImageSize = new(16, 8) };
        root.AddChild(new Sprite { Texture = uvView, Centered = false, Position = new(296, 56) });
        view.Changed += _ => { foreach (var probe in probes) probe.QueueRedraw(); };
        if (compatibility && programmable) { Reject<NotSupportedException>(() => Engine.Run(root)); Check(root.IsDisposed, "Shader fallback rejection releases the scene."); return; }
        RID owned = default; var frame = 0; long before = 0, active = 0, idle = 0; var warmDraws = new int[probes.Length]; var vertexBytes = new byte[24];
        for (var i = 0; i < 3; i++) { BinaryPrimitives.WriteSingleLittleEndian(vertexBytes.AsSpan(i * 8), i == 1 ? 16 : 0); BinaryPrimitives.WriteSingleLittleEndian(vertexBytes.AsSpan(i * 8 + 4), i == 2 ? 8 : 0); }
        void ServerDraws()
        {
            serverRect.QueueRedraw(); serverRegion.QueueRedraw();
            RenderingServer.CanvasItemClear(owned); RenderingServer.CanvasItemAddTextureRect(owned, new(0, 0, 32, 24), view.GetRID());
        }
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); owned = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(owned, sprite.GetCanvas()); RenderingServer.CanvasItemSetTransform(owned, new(0, new(344, 112))); ServerDraws();
            Reject<InvalidOperationException>(() => RenderingServer.FreeRID(view.GetRID()));
            root.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frame is >= 24 and < 88) { BinaryPrimitives.WriteSingleLittleEndian(vertexBytes.AsSpan(20), frame % 2 == 0 ? 9 : 8); mesh.SurfaceUpdateVertexRegion(0, 0, vertexBytes); }
            };
            RenderingServer.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                if (frame is >= 24 and < 88) active += bytes; if (frame is >= 88 and < 152) idle += bytes;
                if (frame == 88) for (var i = 0; i < probes.Length; i++) warmDraws[i] = probes[i].Draws;
                if (frame is <= 5 or 151)
                {
                    using var image = RenderingServer.Service!.Readback(); var color = frame == 0 ? Colors.Red : frame == 3 ? Colors.Green : frame == 4 ? Colors.Black : Colors.Blue;
                    for (var i = 0; i < probes.Length; i++)
                    {
                        var origin = new Vector2i(8 + i % 5 * 72, 8 + i / 5 * 48);
                        Pixel(image, origin.X + inside[i].X, origin.Y + inside[i].Y, color); Pixel(image, origin.X + outside[i].X, origin.Y + outside[i].Y, Colors.Black);
                    }
                    foreach (var position in new[] { new Vector2i(10, 114), new(82, 114), new(154, 114), new(226, 114), new(346, 114) }) Pixel(image, position.X, position.Y, color);
                    Pixel(image, 22, 118, Colors.Black); Pixel(image, 108, 130, Colors.Black);
                    Pixel(image, 17, 13, frame == 2 ? Colors.Blue : Colors.Black);
                    Pixel(image, 298, 58, Colors.Red); Pixel(image, 308, 57, Colors.Green); Pixel(image, 297, 62, Colors.Blue);
                    Pixel(image, 298, 114, Colors.Yellow); Pixel(image, 314, 114, Colors.Cyan); Check(virtualTexture.Calls >= 2, "Static texture commands execute both custom drawing callbacks.");
                    if (frame is 0 or 3 && Environment.GetEnvironmentVariable("ELECTRON2D_MESH_TEXTURE_CAPTURE") is { } capture) image.SavePNG(capture + "." + profile + "." + frame + ".png");
                    if (frame == 0) { using var blue = source.GetImage()!; blue.Fill(Colors.Blue); source.Update(blue); }
                    if (frame == 1) { BinaryPrimitives.WriteSingleLittleEndian(vertexBytes.AsSpan(20), 16); mesh.SurfaceUpdateVertexRegion(0, 0, vertexBytes); }
                    if (frame == 2) { view.Mesh = replacement; view.BaseTexture = green; ServerDraws(); }
                    if (frame == 3) { view.Mesh = null; ServerDraws(); }
                    if (frame == 4) { BinaryPrimitives.WriteSingleLittleEndian(vertexBytes.AsSpan(20), 8); mesh.SurfaceUpdateVertexRegion(0, 0, vertexBytes); view.Mesh = mesh; view.BaseTexture = source; ServerDraws(); }
                }
                if (++frame == 152)
                {
                    Check(active == 0 && idle == 0 && probes.Select((p, i) => p.Draws == warmDraws[i]).All(v => v), $"Prepared native intervals bytes={active}/{idle}; idle geometry remains retained.");
                    RenderingServer.FreeRID(owned); root.Tree!.Quit();
                }
            };
        };
        Engine.Run(root); Check(root.IsDisposed && !mesh.IsDisposed && !source.IsDisposed, "Native shutdown preserves borrowed resources.");
        Console.WriteLine($"MeshTexture {profile}: nine direct geometry cases, Sprite/TextureRect/RID/custom draw, UVs, six live phases; 64 active={active} B/64 idle={idle} B passed.");
    }
}
