using Electron2D;
using V = Electron2D.VisualShader;
using T = Electron2D.VisualShaderVaryingType;

internal static partial class GraphChecks
{
    internal static void VaryingsMetadata(string folder)
    {
        var output = System.IO.Path.Combine(folder, "modules"); Directory.CreateDirectory(output);
        using var control = ResourceLoader.Load<V>(System.IO.Path.Combine(folder, "none-0.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
        var builtinInputs = control.GetSPIRV(RenderingDevice.ShaderStage.Vertex);
        foreach (var name in VaryingGraphs)
        {
            using var graph = ResourceLoader.Load<V>(System.IO.Path.Combine(folder, name + ".e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
            Check(graph.GetShaderUniformList().Count(p => p.Name == "gain") == 1, "Shared-stage varying inputs retain one material value descriptor.");
            foreach (var stage in new[] { RenderingDevice.ShaderStage.Vertex, RenderingDevice.ShaderStage.Fragment })
                File.WriteAllBytes(System.IO.Path.Combine(output, name + (stage == RenderingDevice.ShaderStage.Vertex ? ".vert.spv" : ".frag.spv")), graph.GetSPIRV(stage));
            if (!name.StartsWith("none-", StringComparison.Ordinal))
            {
                var fragment = graph.GetSPIRV(); var vertex = graph.GetSPIRV(RenderingDevice.ShaderStage.Vertex); var changes = 0; graph.Changed += _ => changes++;
                Reject<NotSupportedException>(() => Shader.CreateFromSPIRV(fragment)); Reject<NotSupportedException>(() => graph.SetSPIRV(fragment));
                Reject<ArgumentException>(() => Shader.CreateFromSPIRV(builtinInputs, fragment)); Reject<ArgumentException>(() => graph.SetSPIRV(builtinInputs, fragment));
                Check(changes == 0 && graph.GetSPIRV().SequenceEqual(fragment) && graph.GetSPIRV(RenderingDevice.ShaderStage.Vertex).SequenceEqual(vertex), "Rejected varying programs without a producer preserve both prior stages without notification.");
            }
            else { using var fragmentOnly = Shader.CreateFromSPIRV(graph.GetSPIRV()); Check(fragmentOnly.GetSPIRV(RenderingDevice.ShaderStage.Vertex).Length == 0, "An unselected getter needs no custom producer."); }
            var source = graph.GetSPIRV(); graph.RemoveVarying("unused0");
            Check(graph.GetSPIRV().SequenceEqual(source), "Source varying edits retain the last imported immutable program.");
        }
        Console.WriteLine("Varying metadata: 26 retained programs, 52 original modules, producer guards and shared-stage descriptors passed.");
    }
    internal static void VaryingsCPU()
    {
        using var graph = new V(); var changes = 0; graph.Changed += _ => changes++;
        foreach (var name in new[] { "", "0bad", "a/b", "bad name", "имя" }) Reject<ArgumentException>(() => graph.AddVarying(name, V.VaryingMode.VertexToFragmentLight, T.Float));
        Reject<ArgumentNullException>(() => graph.AddVarying(null!, default, T.Float));
        Reject<ArgumentOutOfRangeException>(() => graph.AddVarying("bad", (V.VaryingMode)1, T.Float));
        Reject<ArgumentOutOfRangeException>(() => graph.AddVarying("bad", V.VaryingMode.Max, T.Float));
        Reject<ArgumentOutOfRangeException>(() => graph.AddVarying("bad", default, T.Max));
        Reject<ArgumentOutOfRangeException>(() => graph.AddVarying("bad", default, (T)(-1)));
        Check(changes == 0 && !graph.HasVarying("bad"), "Rejected varying declarations are atomic.");
        graph.AddVarying("_value", default, T.Vector3); Check(graph.HasVarying("_value") && changes == 1, "ASCII identifiers include a leading underscore.");
        Reject<ArgumentException>(() => graph.AddVarying("_value", default, T.Float)); Check(changes == 1, "Duplicate varying names reject without notification.");
        using var shallow = (V)graph.Duplicate(); using var deep = (V)graph.Duplicate(true);
        graph.RemoveVarying("_value"); Check(!graph.HasVarying("_value") && shallow.HasVarying("_value") && deep.HasVarying("_value"), "Each graph copy owns its declaration table.");
        Reject<ArgumentException>(() => graph.RemoveVarying("_value")); Reject<ArgumentNullException>(() => graph.HasVarying(null!));
        using var setter = new VisualShaderNodeVaryingSetter(); using var getter = new VisualShaderNodeVaryingGetter();
        Check(setter.VaryingName == "[None]" && getter.VaryingName == "[None]" && setter.VaryingType == T.Float && setter.GetInputPortDefaultValue<float>(0) == 0, "Unselected varying access defaults.");
        setter.SetInputPortDefaultValue(0, .375f); setter.VaryingType = T.Transform;
        Check(setter.GetInputPortDefaultValue<System.Numerics.Matrix4x4>(0) == System.Numerics.Matrix4x4.Identity, "Changing a setter role resets its typed input to matrix identity.");
        Reject<ArgumentException>(() => setter.SetInputPortDefaultValue(0, .5f));
        var setterChanges = 0; setter.Changed += _ => setterChanges++; setter.VaryingType = T.Transform; setter.VaryingName = "[None]"; Check(setterChanges == 0, "Equal varying authoring edits are quiet.");
        setter.VaryingName = "matrix"; using var copied = (VisualShaderNodeVaryingSetter)setter.Duplicate(); Check(copied.VaryingName == "matrix" && copied.VaryingType == T.Transform, "Varying node definitions copy their exact role/name.");
        Reject<ArgumentOutOfRangeException>(() => getter.VaryingType = T.Max); Reject<ArgumentNullException>(() => getter.VaryingName = null!);
        var table = (PropertyDescriptor<V, string[]>)graph.GetPropertyList().Single(p => p.Name == "Varyings");
        graph.AddVarying("kept", default, T.UInt);
        foreach (var entries in new[] { new[] { "0/2/duplicate", "0/2/duplicate" }, new[] { "1/0/unsupported" }, new[] { "0/8/sentinel" }, new[] { "0/00/noncanonical" }, new[] { "0/0/bad/name" }, new string[] { null! }, new string[4097] })
        { Reject<InvalidDataException>(() => table.SetValue(graph, entries)); Check(graph.HasVarying("kept") && !graph.HasVarying("duplicate"), "Malformed stored varying tables preserve the old declarations."); }
        var snapshot = table.GetValue(graph); snapshot[0] = "0/0/changed"; Check(graph.HasVarying("kept") && !graph.HasVarying("changed"), "Stored varying getters return independent arrays.");
        using (var observed = new V())
        { observed.Changed += _ => throw new InvalidOperationException("Observer"); Reject<InvalidOperationException>(() => observed.AddVarying("committed", default, T.Boolean)); Check(observed.HasVarying("committed"), "Observer failure occurs after declaration commitment."); }
        graph.Dispose(); setter.Dispose(); getter.Dispose();
        Reject<ObjectDisposedException>(() => graph.HasVarying("kept")); Reject<ObjectDisposedException>(() => graph.RemoveVarying("kept")); Reject<ObjectDisposedException>(() => graph.AddVarying("later", default, T.Float));
        Reject<ObjectDisposedException>(() => setter.VaryingName = "later"); Reject<ObjectDisposedException>(() => getter.VaryingType = T.UInt);
        Console.WriteLine("VARYINGS CPU PASS: declarations, copied state, typed defaults, atomic restore, events and disposal.");
    }
}
