using Electron2D;

internal static partial class CanvasGlyphTests
{
    private sealed class Probe : Entity
    {
        internal Action? Drawing;
        protected override void OnDraw() => Drawing?.Invoke();
    }
    private sealed class HookTexture : Texture
    {
        internal Action? SizeQuery;
        public override int GetWidth() => 2;
        public override int GetHeight() => 1;
        public override Vector2 GetSize() { SizeQuery?.Invoke(); return new(2, 1); }
        public override Image GetImage() => Image.CreateFromData(2, 1, false, Image.Format.Rgba8, [255, 255, 255, 255, 255, 255, 255, 255]);
        public override void DrawRectRegion(CanvasItem canvasItem, Rect2 rect, Rect2 sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true) => throw new Exception("Glyph commands must use the texture identity directly.");
    }
    private static void Recording()
    {
        using var texture = new HookTexture(); using var atlas = new AtlasTexture { Atlas = texture, Region = new(1, 0, 1, 1), Margin = new(0, 0, 8, 0) };
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshTests.Quad(Colors.White));
        using var node = new Probe();
        node.Drawing = () =>
        {
            Reject<ArgumentNullException>(() => node.DrawLCDTextureRectRegion(null!, default, default));
            Reject<ArgumentException>(() => node.DrawLCDTextureRectRegion(texture, new(float.NaN, 0, 1, 1), default));
            Reject<ArgumentException>(() => node.DrawLCDTextureRectRegion(texture, new(0, 0, 1, 1), new(0, float.NaN, 1, 1)));
            Reject<ArgumentException>(() => node.DrawLCDTextureRectRegion(texture, new(0, 0, 1, 1), default, new(float.NaN, 0, 0)));
            foreach (var invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            {
                Reject<ArgumentOutOfRangeException>(() => node.DrawMSDFTextureRectRegion(texture, default, default, pixelRange: invalid));
                Reject<ArgumentOutOfRangeException>(() => node.DrawMSDFTextureRectRegion(texture, default, default, scale: invalid));
            }
            Reject<ArgumentException>(() => node.DrawMSDFTextureRectRegion(texture, default, default, outline: float.NaN));
            node.DrawMSDFTextureRectRegion(atlas, new(4, 8, -16, 8), new(0, 0, -2, 1), outline: 1.9f, pixelRange: 4, scale: 2);
            node.DrawLCDTextureRectRegion(texture, new(24, 8, 16, 8), new(0, 0, 2, 1));
            node.DrawMesh(mesh, texture);
            node.DrawLCDTextureRectRegion(texture, new(0, 0, 0, 8), new(0, 0, 2, 1));
            node.DrawMSDFTextureRectRegion(texture, new(0, 0, 8, 8), default);
        };
        node.PrepareCanvas(); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        node.AppendCanvas(vertices, batches, Transform.Identity);
        Check(batches.Count == 3 && batches[0].Glyph == new CanvasGlyph(CanvasGlyphMode.MSDF, 1, 4, 2) && batches[1].Glyph.Mode == CanvasGlyphMode.LCD && batches[2].Glyph.Mode == CanvasGlyphMode.None, "MSDF/LCD/mesh remain distinct; zero rectangles emit no triangles.");
        Check(ReferenceEquals(batches[0].Texture, texture) && vertices[0].Position == new Vector2(4, 8) && vertices[0].UV == Vector2.Zero && vertices[0].Glyph.Z == .125f, "Direct RID bypasses atlas margins and virtual drawing; destination/source signs cancel and outline truncates before scale.");
        Reject<InvalidOperationException>(() => node.DrawLCDTextureRectRegion(texture, default, default));
        using var sourceNode = new Entity(); sourceNode.AppendServerGlyph(texture, new(0, 0, 8, 8), new(0, 0, 2, 1), Colors.White, new(CanvasGlyphMode.LCD));
        using var packed = new PackedScene(); packed.Pack(sourceNode); using var restored = (Entity)packed.Instantiate();
        Check(!restored.HasCanvasCommands, "Retained glyph commands are not scene storage.");
        using var nested = new Probe();
        texture.SizeQuery = () => { texture.SizeQuery = null; nested.DrawTextureRect(texture, new(24, 0, 8, 8), false); };
        nested.Drawing = () => nested.DrawMSDFTextureRectRegion(texture, new(0, 0, 8, 8), new(0, 0, 2, 1));
        nested.PrepareCanvas(); vertices.Clear(); batches.Clear(); nested.AppendCanvas(vertices, batches, Transform.Identity);
        Check(batches.Count == 2 && batches[0].Glyph.Mode == CanvasGlyphMode.None && batches[1].Glyph.Mode == CanvasGlyphMode.MSDF, "Nested ordinary drawing during a size callback cannot inherit glyph policy.");
    }
}
