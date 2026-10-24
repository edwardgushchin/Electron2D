using Electron2D;
using V = Electron2D.VisualShader;

internal static partial class GraphChecks
{
    internal static void OrientationCPU()
    {
        using var face = new VisualShaderNodeFaceForward(); using var rotation = new VisualShaderNodeRotationByAxis();
        for (var port = 0; port < 3; port++) Check(face.GetInputPortDefaultValue<Vector3>(port) == Vector3.Zero, "FaceForward starts with three independent zero vector defaults.");
        Check(rotation.GetInputPortDefaultValue<Vector3>(0) == Vector3.Zero && rotation.GetInputPortDefaultValue<float>(1) == 0 && rotation.GetInputPortDefaultValue<Vector3>(2) == Vector3.Zero, "Rotation input order is vector, radians and axis, including the native zero-axis default.");
        Reject<ArgumentException>(() => rotation.SetInputPortDefaultValue(1, Vector3.One)); Reject<ArgumentException>(() => rotation.SetInputPortDefaultValue(2, 1f));
        Reject<ArgumentException>(() => rotation.SetInputPortDefaultValue(1, float.NaN)); Reject<ArgumentOutOfRangeException>(() => rotation.SetInputPortDefaultValue(3, 0f));
        for (var port = 0; port < 3; port++) face.SetInputPortDefaultValue(port, new Vector3(port + 1, port + 2, port + 3));
        face.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2; face.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4;
        for (var port = 0; port < 3; port++) Check(face.GetInputPortDefaultValue<Vector4>(port) == new Vector4(port + 1, port + 2, port + 2, port + 2), "Each vector default converts independently across widths.");
        Reject<ArgumentOutOfRangeException>(() => face.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Max);
        face.RemoveInputPortDefaultValue(1); face.OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector3;
        Check(face.GetInputPortDefaultValue<Vector3>(1) == Vector3.Zero, "A missing default is recreated by the inherited width contract.");
        using var copy = (VisualShaderNodeFaceForward)face.Duplicate(true); copy.SetInputPortDefaultValue(0, Vector3.Zero);
        Check(face.GetInputPortDefaultValue<Vector3>(0) != Vector3.Zero && copy.OpType == face.OpType, "Width and independent defaults survive duplication.");
        using var graph = new V(); graph.AddNode(V.Type.Fragment, rotation, default, 2); graph.AddNode(V.Type.Fragment, rotation, default, 3); graph.AddNode(V.Type.Fragment, face, default, 4);
        using var matrix = new VisualShaderNodeMatrixOp(); graph.AddNode(V.Type.Fragment, matrix, default, 5);
        Check(graph.CanConnectNodes(V.Type.Fragment, 2, 1, 5, 0) && !graph.CanConnectNodes(V.Type.Fragment, 2, 0, 5, 0), "The second rotation output is a distinct matrix, not a vector alias.");
        var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-orientation-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(graph, file, SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
            using var loaded = ResourceLoader.Load<V>(file, ResourceLoader.CacheMode.IgnoreDeep);
            Check(loaded.GetNode(V.Type.Fragment, 2) is VisualShaderNodeRotationByAxis && ReferenceEquals(loaded.GetNode(V.Type.Fragment, 2), loaded.GetNode(V.Type.Fragment, 3)), "Compiled rotation factory preserves shared aliases.");
            Check(loaded.GetNode(V.Type.Fragment, 4) is VisualShaderNodeFaceForward restored && restored.OpType == face.OpType && restored.GetInputPortDefaultValue<Vector3>(2) == face.GetInputPortDefaultValue<Vector3>(2), "Compiled FaceForward factory preserves width/defaults in compressed big-endian archives.");
        }
        finally { System.IO.File.Delete(file); }
        Console.WriteLine("Orientation CPU: typed defaults, guards, widths, copies, matrix/vector roles and shared archives passed.");
    }
}
