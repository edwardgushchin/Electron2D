using Electron2D;

internal static class NinePatchTests
{
    internal static void Run()
    {
        using var patch = new NinePatchRect();
        Check(patch.Texture is null && patch.DrawCenter && patch.RegionRect == default && patch.MouseFilter == MouseFilter.Ignore &&
            patch.AxisStretchHorizontal == AxisStretchMode.Stretch && patch.AxisStretchVertical == AxisStretchMode.Stretch && patch.GetMinimumSize() == Vector2.Zero,
            "Nine-patch defaults and pointer-ignore policy.");
        for (var side = 0; side < 4; side++) { patch.SetPatchMargin((Side)side, side + 1); Check(patch.GetPatchMargin((Side)side) == side + 1, "Side projection."); }
        Check(patch.GetMinimumSize() == new Vector2(4, 6), "Intrinsic minimum is the margin sums.");
        patch.PatchMarginLeft = -4; Check(patch.GetPatchMargin(Side.Left) == -4 && patch.GetMinimumSize().X == -1, "Signed margin storage is preserved.");
        Reject<ArgumentOutOfRangeException>(() => patch.SetPatchMargin((Side)4, 2));
        Reject<ArgumentOutOfRangeException>(() => patch.AxisStretchHorizontal = (AxisStretchMode)3);
        Reject<ArgumentException>(() => patch.RegionRect = new(float.NaN, 0, 1, 1));
        using var image = Image.CreateEmpty(5, 5, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image);
        var changes = 0; patch.TextureChanged += () => changes++; patch.Texture = texture; patch.Texture = texture;
        Check(changes == 1, "Only replacement emits TextureChanged."); image.Fill(Colors.Red); texture.Update(image); Check(changes == 1, "Content update is not a texture replacement.");
        using var root = new Node(); root.AddChild(patch); patch.Owner = root; patch.Name = "Patch";
        using var packed = new PackedScene(); packed.Pack(root); using var copy = packed.Instantiate();
        Check(copy.GetNodeOrNull("Patch") is NinePatchRect other && other.PatchMarginLeft == -4 && other.Texture is not null && other.DrawCenter, "Exact type and borrowed resource descriptors pack.");
        using var tree = new SceneTree(root);
        Check(Task.Run(() => Capture(() => patch.DrawCenter = false)).Result is InvalidOperationException, "Attached property guards owner thread.");
        root.RemoveChild(patch); texture.Dispose(); Check(patch.Texture is null && changes == 2, "Disposed borrowed textures clear without owner disposal.");
        using var fail = new NinePatchRect(); fail.TextureChanged += () => throw new ApplicationException("expected");
        using var replacement = ImageTexture.CreateFromImage(image); Reject<AggregateException>(() => fail.Texture = replacement);
        Check(ReferenceEquals(fail.Texture, replacement), "Callback failure retains committed texture."); fail.Dispose();
        VerifyGeometryOracle();
        Console.WriteLine("Nine-patch state, minimum size, resources, scene and geometry oracle passed.");
    }

    private static void VerifyGeometryOracle()
    {
        using var image = Image.CreateEmpty(5, 5, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image);
        var vertices = new List<CanvasVertex>();
        for (var h = 0; h < 3; h++) for (var v = 0; v < 3; v++)
            {
                vertices.Clear();
                var command = new CanvasCommand(false, Vector2.Zero, new(11, 9), Colors.White, 0, false, Transform.Identity, texture, new(0, 0, 5, 5),
                    NinePatch: new(new(1, 1), new(1, 1), (AxisStretchMode)h, (AxisStretchMode)v, true));
                CanvasGeometry.Append(vertices, command, Transform.Identity, Colors.White);
                for (var y = 0; y < 9; y++) for (var x = 0; x < 11; x++)
                    {
                        var p = new Vector2(x + .5f, y + .5f); var uv = FindUV(vertices, p);
                        var expected = new Vector2(MapAxis(p.X, 11, 5, 1, 1, h), MapAxis(p.Y, 9, 5, 1, 1, v)) / 5;
                        Check((uv - expected).Length() < 0.0002f, $"Independent pixel-axis oracle h={h} v={v} at {p}: {uv}/{expected}");
                    }
            }
    }
    internal static float MapAxis(float pixel, float draw, float texture, float begin, float end, int mode)
    {
        if (pixel < begin) return pixel;
        if (pixel >= draw - end) return texture - (draw - pixel);
        var source = texture - begin - end; var destination = draw - begin - end;
        if (mode == 0) return begin + (pixel - begin) / destination * source;
        if (mode == 1) return begin + Mathf.PosMod(pixel - begin, source);
        var count = MathF.Max(1, MathF.Floor(destination / MathF.Max(source, .0000001f) + .5f));
        return begin + Mathf.PosMod((pixel - begin) / destination * count, 1) * source;
    }
    private static Vector2 FindUV(List<CanvasVertex> vertices, Vector2 p)
    {
        for (var i = vertices.Count - 3; i >= 0; i -= 3)
        {
            var a = vertices[i]; var b = vertices[i + 1]; var c = vertices[i + 2]; var ab = b.Position - a.Position; var ac = c.Position - a.Position;
            var denominator = ab.Cross(ac); if (denominator == 0) continue;
            var v = (p - a.Position).Cross(ac) / denominator; var w = ab.Cross(p - a.Position) / denominator;
            if (v >= -0.00001f && w >= -0.00001f && v + w <= 1.00001f) return a.UV + (b.UV - a.UV) * v + (c.UV - a.UV) * w;
        }
        throw new InvalidOperationException("Oracle pixel is not covered.");
    }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
