using Electron2D;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;

internal static partial class GraphChecks
{
    internal static void RerouteCPU()
    {
        using var graph = new V();
        using var first = new VisualShaderNodeReroute(); using var second = new VisualShaderNodeReroute();
        using var branch = new VisualShaderNodeReroute(); using var matrix = new VisualShaderNodeMatrixConstant();
        using var vector = new VisualShaderNodeVec3Constant(); using var scalar = new VisualShaderNodeFloatOp();
        using var matrixSink = new VisualShaderNodeDeterminant(); using var image = new VisualShaderNodeImageParameter();
        VisualShaderNode[] definitions = [first, second, branch, matrix, vector, scalar, matrixSink, image];
        for (var i = 0; i < definitions.Length; i++) graph.AddNode(V.Type.Fragment, definitions[i], default, i + 2);
        Check(first.GetPortType() == P.Scalar && first.GetInputPortDefaultValue<float>(0) == 0, "A reroute starts with a typed scalar zero.");
        graph.ConnectNodes(V.Type.Fragment, 2, 0, 3, 0); graph.ConnectNodes(V.Type.Fragment, 2, 0, 4, 0);
        var changes = 0; first.Changed += _ => changes++;
        Check(graph.CanConnectNodes(V.Type.Fragment, 5, 0, 2, 0) && changes == 0 && first.GetPortType() == P.Scalar, "Connection feasibility never mutates definitions.");
        graph.ConnectNodes(V.Type.Fragment, 5, 0, 2, 0);
        Check(definitions.Take(3).Cast<VisualShaderNodeReroute>().All(n => n.GetPortType() == P.Matrix) && changes == 1, "A matrix role propagates through both branches.");
        Check(first.GetInputPortDefaultValue<System.Numerics.Matrix4x4>(0) == System.Numerics.Matrix4x4.Identity, "New matrix roles receive identity defaults.");
        Check(!graph.CanConnectNodes(V.Type.Fragment, 6, 0, 2, 0), "A role change cannot invalidate an existing upstream matrix edge.");
        graph.ConnectNodes(V.Type.Fragment, 3, 0, 8, 0);
        Check(!graph.CanConnectNodes(V.Type.Fragment, 6, 0, 2, 0), "An incompatible terminal rejects a branch-wide role change.");
        Reject<ArgumentException>(() => graph.ConnectNodes(V.Type.Fragment, 6, 0, 2, 0));
        Check(first.GetPortType() == P.Matrix && changes == 1 && !graph.IsNodeConnection(V.Type.Fragment, 6, 0, 2, 0), "Rejected edits preserve types, defaults, edges and notifications.");
        graph.DisconnectNodes(V.Type.Fragment, 3, 0, 8, 0); graph.DisconnectNodes(V.Type.Fragment, 5, 0, 2, 0);
        graph.ConnectNodes(V.Type.Fragment, 6, 0, 2, 0);
        Check(first.GetPortType() == P.Vector3 && second.GetPortType() == P.Vector3 && branch.GetPortType() == P.Vector3, "Disconnected branches can select a new numeric role.");
        first.SetInputPortDefaultValue(0, new Vector3(2, 3, 4)); graph.DisconnectNodes(V.Type.Fragment, 6, 0, 2, 0);
        Check(first.GetPortType() == P.Vector3 && first.GetInputPortDefaultValue<Vector3>(0) == new Vector3(2, 3, 4), "Disconnection retains the role and authored default.");
        Reject<ArgumentException>(() => first.SetInputPortDefaultValue(0, 1f)); Reject<ArgumentOutOfRangeException>(() => first.SetInputPortDefaultValue(1, Vector3.Zero));
        graph.ConnectNodes(V.Type.Fragment, 9, 0, 2, 0);
        Check(first.GetPortType() == P.Sampler && second.GetPortType() == P.Sampler && first.GetDefaultInputValues().Count == 0, "Opaque sampler roles erase numeric defaults throughout a branch.");
        Reject<ArgumentException>(() => first.SetInputPortDefaultValue(0, 0f));
        Check(!graph.CanConnectNodes(V.Type.Fragment, 4, 0, 2, 0), "Ordinary connections reject reroute cycles.");
        graph.ConnectNodesForced(V.Type.Fragment, 4, 0, 2, 0);
        Check(!graph.CanConnectNodes(V.Type.Fragment, 6, 0, 2, 0), "Feasibility terminates and preserves incoming sampler constraints even when a forced subgraph already contains a cycle.");
        graph.DisconnectNodes(V.Type.Fragment, 4, 0, 2, 0);
        graph.DisconnectNodes(V.Type.Fragment, 9, 0, 2, 0);
        graph.ConnectNodes(V.Type.Fragment, 2, 0, 7, 0);
        Check(first.GetPortType() == P.Scalar && second.GetPortType() == P.Scalar, "An unbound source adapts its downstream chain to a terminal input.");
        graph.RemoveNode(V.Type.Fragment, 7);
        graph.ConnectNodes(V.Type.Fragment, 5, 0, 2, 0);
        first.RemoveInputPortDefaultValue(0);
        using (var copy = (VisualShaderNodeReroute)first.Duplicate(true))
            Check(copy.GetPortType() == P.Matrix && copy.GetDefaultInputValues().Count == 0, "Duplication preserves both role and explicitly absent defaults.");
        graph.AddNode(V.Type.Vertex, first, default, 2);
        var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-reroute-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(graph, file, SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
            using var loaded = ResourceLoader.Load<V>(file, ResourceLoader.CacheMode.IgnoreDeep);
            var restored = (VisualShaderNodeReroute)loaded.GetNode(V.Type.Fragment, 2);
            Check(restored.GetPortType() == P.Matrix && restored.GetDefaultInputValues().Count == 0 && ReferenceEquals(restored, loaded.GetNode(V.Type.Vertex, 2)), "Fresh archive factories restore dynamic descriptors, absence and shared cross-stage aliases.");
        }
        finally { System.IO.File.Delete(file); }
        using (var empty = new V())
        using (var dangling = new VisualShaderNodeReroute())
        using (var sink = new VisualShaderNodeDeterminant())
        {
            empty.AddNode(V.Type.Fragment, dangling, default, 2); empty.AddNode(V.Type.Fragment, sink, default, 3);
            Check(empty.CanConnectNodes(V.Type.Fragment, 2, 0, 3, 0), "A dangling scalar reroute can become a matrix source.");
            empty.ConnectNodes(V.Type.Fragment, 2, 0, 3, 0);
            var descriptor = (PropertyDescriptor<VisualShaderNodeReroute, P>)dangling.GetPropertyList().Single(p => p.Name == "PortType");
            Reject<ArgumentOutOfRangeException>(() => descriptor.SetValue(dangling, P.Max));
            Check(dangling.GetPortType() == P.Matrix, "Invalid stored role sentinels do not mutate the node.");
        }
        using (var observed = new V())
        using (var a = new VisualShaderNodeReroute())
        using (var b = new VisualShaderNodeReroute())
        {
            observed.AddNode(V.Type.Fragment, a, default, 2); observed.AddNode(V.Type.Fragment, b, default, 3); observed.AddNode(V.Type.Fragment, matrix, default, 4);
            observed.ConnectNodes(V.Type.Fragment, 2, 0, 3, 0);
            a.Changed += _ => throw new InvalidOperationException("observer"); var delivered = false;
            b.Changed += _ => { delivered = true; Check(observed.IsNodeConnection(V.Type.Fragment, 4, 0, 2, 0), "Observers see the complete new edge."); };
            Reject<AggregateException>(() => observed.ConnectNodes(V.Type.Fragment, 4, 0, 2, 0));
            Check(delivered && a.GetPortType() == P.Matrix && b.GetPortType() == P.Matrix, "Failing observers cannot interrupt branch commitment or later notifications.");
        }
        using var disposed = new VisualShaderNodeReroute(); disposed.Dispose(); Reject<ObjectDisposedException>(() => disposed.GetPortType());
        using (var stale = new V())
        using (var input = new VisualShaderNodeInput { InputName = "uv" })
        using (var route = new VisualShaderNodeReroute())
        {
            stale.AddNode(V.Type.Fragment, input, default, 2); stale.AddNode(V.Type.Fragment, route, default, 3); stale.AddNode(V.Type.Fragment, vector, default, 4);
            stale.ConnectNodes(V.Type.Fragment, 2, 0, 3, 0); input.Dispose();
            Check(!stale.CanConnectNodes(V.Type.Fragment, 4, 0, 3, 0), "Disposed upstream providers reject inference without querying their metadata.");
        }
        Console.WriteLine("Reroute CPU: branch inference, atomic rejection, dangling/forced/cyclic edits, defaults, observers, copies and portable shared archives passed.");
    }
}
