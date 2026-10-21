using Electron2D;

internal static class CanvasDistanceFieldTests
{
    private sealed class Painter(Texture texture, Mesh mesh) : Entity
    {
        protected override void OnDraw()
        {
            DrawTextureRect(texture, new(0, 0, 8, 8), false);
            DrawMSDFTextureRectRegion(texture, new(8, 0, 8, 8), new(0, 0, 1, 1), pixelRange: 8);
            DrawLCDTextureRectRegion(texture, new(16, 0, 8, 8), new(0, 0, 1, 1));
            DrawRect(new(24, 0, 8, 8), Colors.Blue); DrawMesh(mesh, texture);
        }
    }
    internal static void Run()
    {
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData { Vertices = [Vector2.Zero, Vector2.Right, Vector2.Down] });
        using var node = new Painter(texture, mesh); using var child = new Entity(); node.AddChild(child);
        // Isolate replay policy; the public consumer verifies real renderer ownership and session cleanup.
        node.ServerState = new(null!, false) { DistanceFieldMode = true };
        try
        {
            Check(node.ResolveCanvasGlyph(texture) == new CanvasGlyph(CanvasGlyphMode.MSDF), "Retained mode selects MSDF with default parameters.");
            Check(node.ResolveCanvasGlyph(null).Mode == CanvasGlyphMode.None && child.ResolveCanvasGlyph(texture).Mode == CanvasGlyphMode.None, "Untextured commands and children retain their policy.");
            var explicitGlyph = new CanvasGlyph(CanvasGlyphMode.MSDF, 2, 8, 2);
            Check(node.ResolveCanvasGlyph(texture, explicitGlyph) == explicitGlyph && node.ResolveCanvasGlyph(texture, new(CanvasGlyphMode.LCD)).Mode == CanvasGlyphMode.LCD, "Explicit drawing wins over item mode.");
            node.PrepareCanvas(); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); node.AppendCanvas(vertices, batches, Transform.Identity);
            Check(batches.Count == 5 && batches[0].Glyph.Mode == CanvasGlyphMode.MSDF && batches[1].Glyph.PixelRange == 8 && batches[2].Glyph.Mode == CanvasGlyphMode.LCD && batches[3].Glyph.Mode == CanvasGlyphMode.None && batches[4].Glyph.Mode == CanvasGlyphMode.MSDF, "All five command roles retain independent decoding.");
            foreach (var batch in batches)
                if (batch.Glyph.Mode != CanvasGlyphMode.None)
                    for (var i = batch.First; i < batch.First + batch.Count; i++) Check(vertices[i].Glyph == batch.Glyph.Parameters, "Ordinary and mesh vertex payloads carry the complete glyph parameters.");
            node.ServerState.DistanceFieldMode = false; vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, Transform.Identity);
            Check(batches[0].Glyph.Mode == CanvasGlyphMode.None && batches[^1].Glyph.Mode == CanvasGlyphMode.None && batches[1].Glyph.PixelRange == 8 && batches[2].Glyph.Mode == CanvasGlyphMode.LCD, "Clearing item mode preserves explicit commands without rerecording.");
            var proxy = new CanvasMesh(mesh, Transform.Identity, Colors.White); vertices.Clear(); batches.Clear();
            proxy.Append(vertices, batches, texture, Transform.Identity, Colors.White, null, BlendMode.Mix, TextureFilter.Nearest, TextureRepeat.Disabled, 1, null, false, skinOwner: node);
            node.ServerState.DistanceFieldMode = true;
            proxy.Append(vertices, batches, texture, Transform.Identity, Colors.White, null, BlendMode.Mix, TextureFilter.Nearest, TextureRepeat.Disabled, 1, null, false, skinOwner: node);
            Check(batches.Count == 2 && batches[0].Glyph.Mode == CanvasGlyphMode.None && batches[1].Glyph.Mode == CanvasGlyphMode.MSDF, "Plain and encoded mesh ranges cannot merge into a wrong decoding policy.");
            Check(!node.GetPropertyList().Any(p => p.Name == "DistanceFieldMode"), "Renderer state is not authored node storage.");
        }
        finally { node.ServerState = null; }
        Console.WriteLine("Retained MSDF replay: explicit precedence, noninheritance, untextured policy, complete vertex payloads and mixed mesh batches passed without a renderer.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
