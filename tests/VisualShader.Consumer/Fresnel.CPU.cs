using Electron2D;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;

internal static partial class GraphChecks
{
    internal static void FresnelCPU()
    {
        using var value = new VisualShaderNodeFresnel();
        Check(value.GetInputPortDefaultValue<Vector3>(0) is null && value.GetInputPortDefaultValue<Vector3>(1) is null &&
            value.GetInputPortDefaultValue<bool>(2) == false && value.GetInputPortDefaultValue<float>(3) == 1, "Fresnel starts with absent normal/view, false inversion and power one.");
        Reject<ArgumentException>(() => value.SetInputPortDefaultValue(0, 1f));
        Reject<ArgumentException>(() => value.SetInputPortDefaultValue(2, 1f));
        Reject<ArgumentException>(() => value.SetInputPortDefaultValue(3, Vector3.One));
        Reject<ArgumentException>(() => value.SetInputPortDefaultValue(3, float.NaN));
        Reject<ArgumentOutOfRangeException>(() => value.SetInputPortDefaultValue(4, 0f));
        var changed = 0; value.Changed += _ => changed++;
        value.SetInputPortDefaultValue(0, new Vector3(2, -3, .5f)); value.SetInputPortDefaultValue(1, new Vector3(.25f, .5f, 2));
        value.SetInputPortDefaultValue(2, true); value.SetInputPortDefaultValue(3, -2f);
        using var copy = (VisualShaderNodeFresnel)value.Duplicate(true); copy.RemoveInputPortDefaultValue(0);
        Check(changed == 4 && copy.GetInputPortDefaultValue<Vector3>(0) is null && value.GetInputPortDefaultValue<Vector3>(0) == new Vector3(2, -3, .5f) && copy.GetInputPortDefaultValue<float>(3) == -2, "Copies retain independent typed defaults and explicit absence.");
        using var graph = new V(); graph.AddNode(V.Type.Fragment, copy, default, 2); graph.AddNode(V.Type.Fragment, copy, default, 3); graph.AddNode(V.Type.Vertex, value, default, 2);
        Check(graph.CanConnectNodes(V.Type.Fragment, 2, 0, 0, 0) && !graph.CanConnectNodes(V.Type.Fragment, 2, 1, 0, 0), "Fresnel has one numeric scalar output.");
        var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-fresnel-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(graph, file, SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
            using var restored = ResourceLoader.Load<V>(file, ResourceLoader.CacheMode.IgnoreDeep);
            var shared = (VisualShaderNodeFresnel)restored.GetNode(V.Type.Fragment, 2);
            Check(ReferenceEquals(shared, restored.GetNode(V.Type.Fragment, 3)) && shared.GetInputPortDefaultValue<Vector3>(0) is null && shared.GetInputPortDefaultValue<bool>(2) == true && shared.GetInputPortDefaultValue<float>(3) == -2,
                "Portable factories retain borrowed aliases, typed defaults and absence.");
            Check(((VisualShaderNodeFresnel)restored.GetNode(V.Type.Vertex, 2)).GetInputPortDefaultValue<Vector3>(0) == new Vector3(2, -3, .5f), "Separate stage definitions retain their authored normal.");
        }
        finally { System.IO.File.Delete(file); }
        value.Dispose(); Reject<ObjectDisposedException>(() => value.GetInputPortDefaultValue<float>(3));
        Console.WriteLine("Fresnel CPU: typed/absent defaults, guards, notifications, copies, output role and portable shared archives passed.");
    }
}
