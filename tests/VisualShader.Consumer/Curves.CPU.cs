using Electron2D;
using V = Electron2D.VisualShader;

internal static partial class GraphChecks
{
    internal static void CurvesCPU()
    {
        using var scalar = new VisualShaderNodeCurveTexture(); using var xyz = new VisualShaderNodeCurveXYZTexture();
        using var curve = CurveLine(-.5f, 1.5f); using var image = new CurveTexture { Curve = curve, TextureMode = CurveTexture.TextureModeEnum.Red };
        using var triples = new CurveXYZTexture { CurveX = curve, CurveY = curve, CurveZ = curve };
        Check(scalar.Texture is null && xyz.Texture is null && scalar.GetInputPortDefaultValue<float>(0) == 0 && xyz.GetInputPortDefaultValue<float>(0) == 0, "Curve samplers start with numeric zero and absent borrowed images.");
        Reject<ArgumentException>(() => scalar.SetInputPortDefaultValue(0, Vector2.One));
        Reject<ArgumentException>(() => xyz.SetInputPortDefaultValue(0, float.NaN));
        Reject<ArgumentOutOfRangeException>(() => xyz.SetInputPortDefaultValue(1, 0f));
        var changes = 0; scalar.Changed += _ => changes++;
        scalar.Texture = image; scalar.Texture = image; scalar.Size = new(40, 80); scalar.RemoveInputPortDefaultValue(0);
        xyz.Texture = triples; xyz.Size = new(90, 100); xyz.SetInputPortDefaultValue(0, .75f);
        Check(changes == 3, "Image assignments publish even for the same resource; layout size remains quiet.");
        using var shallow = (VisualShaderNodeCurveTexture)scalar.Duplicate(); using var deep = (VisualShaderNodeCurveTexture)scalar.Duplicate(true);
        using var deepXYZ = (VisualShaderNodeCurveXYZTexture)xyz.Duplicate(true);
        Check(ReferenceEquals(shallow.Texture, image) && deep.Texture is not null && !ReferenceEquals(deep.Texture, image) && !ReferenceEquals(deep.Texture.Curve, curve) && deep.Size == scalar.Size && deep.GetInputPortDefaultValue<float>(0) is null, "Scalar copies preserve layout and explicit absence with the requested resource ownership.");
        Check(deepXYZ.Texture is not null && !ReferenceEquals(deepXYZ.Texture, triples) && ReferenceEquals(deepXYZ.Texture.CurveX, deepXYZ.Texture.CurveY) && !ReferenceEquals(deepXYZ.Texture.CurveX, curve), "XYZ deep copying retains shared channel aliases within an independent graph.");
        using var graph = new V(); graph.AddNode(V.Type.Fragment, scalar, default, 2); graph.AddNode(V.Type.Vertex, scalar, default, 2); graph.AddNode(V.Type.Fragment, xyz, default, 3);
        Check(graph.CanConnectNodes(V.Type.Fragment, 2, 0, 0, 0) && graph.CanConnectNodes(V.Type.Fragment, 3, 0, 0, 0) && !graph.CanConnectNodes(V.Type.Fragment, 3, 1, 0, 0), "Each sampler exposes exactly one numeric output.");
        var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-curves-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            foreach (var flags in new[] { SaverFlags.BundleResources, SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian })
            {
                ResourceSaver.Save(graph, file, flags); using var restored = ResourceLoader.Load<V>(file, ResourceLoader.CacheMode.IgnoreDeep);
                var a = (VisualShaderNodeCurveTexture)restored.GetNode(V.Type.Fragment, 2); var b = (VisualShaderNodeCurveXYZTexture)restored.GetNode(V.Type.Fragment, 3);
                Check(ReferenceEquals(a, restored.GetNode(V.Type.Vertex, 2)) && a.Size == scalar.Size && a.GetInputPortDefaultValue<float>(0) is null && b.GetInputPortDefaultValue<float>(0) == .75f && b.Size == xyz.Size, "Portable factories preserve shared definitions, layout, defaults and absence.");
                Check(a.Texture!.TextureMode == CurveTexture.TextureModeEnum.Red && ReferenceEquals(a.Texture.Curve, b.Texture!.CurveX) && ReferenceEquals(b.Texture.CurveY, b.Texture.CurveZ), "Archive preserves one authored curve across both image types.");
            }
        }
        finally { System.IO.File.Delete(file); }
        using var retired = new CurveTexture(); retired.Dispose(); Reject<ObjectDisposedException>(() => scalar.Texture = retired);
        using var retiredXYZ = new CurveXYZTexture(); retiredXYZ.Dispose(); Reject<ObjectDisposedException>(() => xyz.Texture = retiredXYZ);
        scalar.Changed += _ => throw new InvalidOperationException("observer");
        Reject<InvalidOperationException>(() => scalar.Texture = null); Check(scalar.Texture is null, "The assignment commits before observer errors.");
        scalar.Dispose(); xyz.Dispose(); Check(!image.IsDisposed && !triples.IsDisposed && !curve.IsDisposed, "Nodes borrow their images and curves.");
        Reject<ObjectDisposedException>(() => scalar.Texture = image); Reject<ObjectDisposedException>(() => _ = xyz.Texture);
        Console.WriteLine("Curve graph CPU: typed ports, events, aliases, deep/shallow copies, compressed big-endian archives and borrowed lifetime passed.");
    }
    private static Curve CurveLine(float from, float to)
    {
        var curve = new Curve { MinValue = -2, MaxValue = 3 };
        curve.AddPoint(new(0, from), rightMode: Curve.TangentMode.Linear); curve.AddPoint(new(1, to), leftMode: Curve.TangentMode.Linear); return curve;
    }
}
