using Electron2D;

internal static partial class LightingTests
{
    internal static void Run()
    {
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(Colors.White);
        using var texture = ImageTexture.CreateFromImage(image);
        using var canvas = new CanvasTexture();
        Check(canvas.GetSize() == Vector2.One && !canvas.IsPixelOpaque(0, 0) && canvas.GetImage() is null, "Empty canvas texture queries");
        var changes = 0; canvas.Changed += _ => changes++;
        canvas.DiffuseTexture = texture; canvas.NormalTexture = texture; canvas.SpecularTexture = texture;
        var before = changes; texture.Update(image); Check(changes == before + 1, "Shared channels subscribe once");
        Check(canvas.GetSize() == new Vector2(2, 2) && canvas.IsPixelOpaque(-1, 99) && canvas.GetRID() != texture.GetRID(), "Diffuse queries and multichannel identity");
        using var copy = (CanvasTexture)canvas.Duplicate(true);
        Check(copy.DiffuseTexture != texture && copy.DiffuseTexture == copy.NormalTexture && copy.NormalTexture == copy.SpecularTexture, "Deep copied channel graph preserves sharing");
        using var view = new AtlasTexture { Atlas = canvas };
        Reject<ArgumentException>(() => canvas.DiffuseTexture = view);
        using var channelView = new AtlasTexture { Atlas = texture }; canvas.DiffuseTexture = channelView;
        Reject<ArgumentException>(() => channelView.Atlas = canvas);
        Check(channelView.Atlas == texture && canvas.DiffuseTexture == channelView, "Rejected indirect channel cycle preserves graph");
        Reject<ArgumentException>(() => canvas.NormalTexture = canvas);
        texture.Dispose(); Check(canvas.NormalTexture is null && canvas.SpecularTexture is null, "Borrowed channel disposal clears shared slots");
        using var contour = new OccluderPolygon(); var points = new[] { new Vector2(0, 0), new Vector2(8, 0), new Vector2(8, 8) }; contour.Polygon = points; points[0] = new(9, 9);
        var returned = contour.Polygon; returned[0] = new(10, 10); Check(contour.Polygon[0] == Vector2.Zero, "Contour arrays are copied in both directions");
        Reject<ArgumentException>(() => contour.Polygon = [new(float.NaN, 0)]); Check(contour.Polygon.Length == 3, "Invalid contour preserves state");
        using var light = new PointLight(); Check(light.Enabled && light.Energy == 1 && light.Height == 0 && light.ShadowFilter == LightShadowFilter.None, "Light defaults");
        light.TextureScale = 0; Check(light.TextureScale == 0.00001f, "Zero scale preserves invertible mask"); Reject<ArgumentOutOfRangeException>(() => light.Energy = float.NaN);
        using var material = new CanvasItemMaterial { LightMode = LightMode.LightOnly }; using var materialCopy = (CanvasItemMaterial)material.Duplicate(); Check(materialCopy.LightMode == LightMode.LightOnly, "Copied shading policy");
        using var root = new Entity(); var packedLight = new PointLight { Height = 14, RangeItemCullMask = uint.MaxValue, Energy = 2, Offset = new(3, 4) }; root.AddChild(packedLight); packedLight.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root); using var instance = (Entity)packed.Instantiate(); var restored = (PointLight)instance.GetChild(0); Check(restored.Height == 14 && restored.Offset == new Vector2(3, 4) && restored.RangeItemCullMask == uint.MaxValue, "Typed scene factory and descriptors");
        var interpolation = new CanvasLightInterpolation { Enabled = true };
        interpolation.Reset(new Transform(0, new Vector2(2, 0))); interpolation.Tick(new Transform(0, new Vector2(2, 0)), true); interpolation.Tick(new Transform(0, new Vector2(10, 0)), false);
        Check(interpolation.Render(Transform.Identity, .5f, true).Origin == new Vector2(6, 0), "Retained physics snapshot midpoint");
        interpolation.Rebase(new Transform(0, new Vector2(3, 0))); Check(interpolation.Render(Transform.Identity, .5f, true).Origin == new Vector2(9, 0), "Snapshot rebase preserves interpolation");
        interpolation.Enabled = false; Check(interpolation.Render(Transform.Identity, .5f, true) == Transform.Identity, "Disabled interpolation preserves canonical transform");
        Files();
        Console.WriteLine("Canvas lighting resources: defaults, borrowed lifetimes, graph copies, cycle validation and scene metadata passed.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
