using Electron2D;
using System.Runtime.InteropServices;
using System.Text;
using IOPath = System.IO.Path;
using Q = Electron2D.VisualShaderNodeParameter.QualifierEnum;

internal static partial class GraphChecks
{
    private static void AuthorInvalidParameters(string folder)
    {
        var output = IOPath.Combine(folder, "invalid-parameters"); Directory.CreateDirectory(output);
        void Save(string name, VisualShader graph) => ResourceSaver.Save(graph, IOPath.Combine(output, name + ".e2dres"), SaverFlags.BundleResources | SaverFlags.Compress);
        foreach (var name in new[] { "reserved-name", "bad-name", "duplicate-name", "missing-reference", "global-local-default", "slot-collision", "slot-overflow", "matrix-instance", "matrix-explicit-instance", "stale-color-output" })
        {
            using var graph = new VisualShader(); var owned = new List<VisualShaderNode>();
            void Add(VisualShaderNode n, int id) { owned.Add(n); graph.AddNode(VisualShader.Type.Fragment, n, default, id); }
            try
            {
                var first = new VisualShaderNodeFloatParameter { ParameterName = "first" }; Add(first, 2);
                switch (name)
                {
                    case "reserved-name": first.ParameterName = "TIME"; break;
                    case "bad-name": first.ParameterName = "bad-name"; break;
                    case "duplicate-name": var duplicate = new VisualShaderNodeIntParameter { ParameterName = "second" }; Add(duplicate, 3); duplicate.ParameterName = "first"; break;
                    case "missing-reference": Add(new VisualShaderNodeParameterRef { ParameterName = "absent" }, 3); graph.ConnectNodes(VisualShader.Type.Fragment, 3, 0, 0, 0); break;
                    case "global-local-default": first.Qualifier = Q.Global; first.DefaultValueEnabled = true; break;
                    case "slot-collision": first.Qualifier = Q.InstanceIndex; first.InstanceIndex = 3; Add(new VisualShaderNodeFloatParameter { ParameterName = "second", Qualifier = Q.InstanceIndex, InstanceIndex = 3 }, 3); break;
                    case "slot-overflow": first.Qualifier = Q.Instance; for (var i = 0; i < 16; i++) Add(new VisualShaderNodeFloatParameter { ParameterName = "slot" + i, Qualifier = Q.Instance }, 3 + i); break;
                    case "matrix-instance": case "matrix-explicit-instance": Add(new VisualShaderNodeMatrixParameter { Qualifier = name == "matrix-instance" ? Q.Instance : Q.InstanceIndex }, 3); break;
                    case "stale-color-output":
                        var color = new VisualShaderNodeColorParameter { ParameterName = "color_value" }; var reference = new VisualShaderNodeParameterRef { ParameterName = "color_value" }; Add(color, 3); Add(reference, 4); graph.ConnectNodes(VisualShader.Type.Fragment, 4, 1, 0, 1); reference.ParameterName = "first"; break;
                }
                if (name is not ("missing-reference" or "stale-color-output")) graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
                Save(name, graph);
            }
            finally { foreach (var item in owned) item.Dispose(); }
        }
    }
    internal static void ParameterMetadataChecks(string folder)
    {
        var path = IOPath.Combine(folder, "parameter-0-0-1-0-0-0.e2dres"); using var graph = ResourceLoader.Load<VisualShader>(path, ResourceLoader.CacheMode.IgnoreDeep);
        var vertex = graph.GetSPIRV(RenderingDevice.ShaderStage.Vertex); var fragment = graph.GetSPIRV(RenderingDevice.ShaderStage.Fragment);
        static string B(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        var name = B("p_0_1"); var records = new[]{
            $"Electron2D:hint:2:{name}:0::0",$"Electron2D:hint:1:{name}:99::0",$"Electron2D:hint:1:{name}:0::2",
            "Electron2D:hint:1:???:0::0","Electron2D:hint:1:/w==:0::0",$"Electron2D:hint:1:{B("absent")}:0::0",
            $"Electron2D:hint:1:{name}:0::1",$"Electron2D:hint:1:{name}:2::0",$"Electron2D:hint:1:{name}:0:/w==:0",
            $"Electron2D:hint:1:{name}:0::0","Electron2D:hint:1:short"
        };
        var rejected = 0;
        foreach (var record in records)
        {
            var invalid = AppendHint(fragment, record); var failed = false;
            try { graph.SetSPIRV(vertex, invalid); }
            catch (Exception error) when (error is ArgumentException or NotSupportedException) { failed = true; rejected++; }
            Check(failed && graph.GetSPIRV(RenderingDevice.ShaderStage.Vertex).SequenceEqual(vertex) && graph.GetSPIRV(RenderingDevice.ShaderStage.Fragment).SequenceEqual(fragment), "Invalid hint metadata rejects before replacing either program stage.");
        }
        var invalidFrame = AppendHint(vertex, $"Electron2D:hint:1:{B("size")}:1:{B("0,1")}:0");
        Reject<ArgumentException>(() => graph.SetSPIRV(invalidFrame, fragment));
        Check(graph.GetSPIRV(RenderingDevice.ShaderStage.Vertex).SequenceEqual(vertex), "Frame hint rejection preserves the valid vertex program.");
        Console.WriteLine($"{rejected + 1} malformed/duplicate/wrong-owner hint records rejected atomically.");
    }
    private static byte[] AppendHint(byte[] code, string record)
    {
        var words = MemoryMarshal.Cast<byte, uint>(code).ToArray(); var at = 5;
        while (at < words.Length && (words[at] & 0xffff) is 10 or 11 or 14 or 15 or 16 or 17 or 331) at += (int)(words[at] >> 16);
        var id = words[3]++; using var output = new MemoryStream(); using var writer = new BinaryWriter(output);
        writer.Write(MemoryMarshal.AsBytes(words.AsSpan(0, at))); var bytes = Encoding.UTF8.GetBytes(record + '\0');
        writer.Write((uint)((bytes.Length + 3) / 4 + 2) << 16 | 7u); writer.Write(id); writer.Write(bytes); while (output.Position % 4 != 0) writer.Write((byte)0);
        writer.Write(MemoryMarshal.AsBytes(words.AsSpan(at))); return output.ToArray();
    }
}
