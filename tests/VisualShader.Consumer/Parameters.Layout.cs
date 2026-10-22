using Electron2D;
using System.Runtime.InteropServices;
using System.Text;
using IOPath = System.IO.Path;
using Q = Electron2D.VisualShaderNodeParameter.QualifierEnum;

internal static partial class GraphChecks
{
    private static class ParameterLayoutNames
    { internal static readonly string[] Slots = Enumerable.Range(0, 16).Select(i => "slot_" + i.ToString("D2", System.Globalization.CultureInfo.InvariantCulture)).ToArray(); }
    private static void AuthorParameterLayouts(string folder)
    {
        for (var observed = 0; observed < 16; observed++)
        {
            using var graph = new VisualShader(); var owned = new List<VisualShaderNode>(); var next = 10;
            var scale = new VisualShaderNodeFloatParameter { ParameterName = "p_layout_scale", DefaultValue = .5f, DefaultValueEnabled = true, Hint = VisualShaderNodeFloatParameter.HintEnum.RangeStep, Min = 0, Max = 1, Step = .125f };
            var gain = new VisualShaderNodeFloatParameter { ParameterName = "p_layout_gain", Qualifier = Q.Global };
            owned.AddRange([scale, gain]); graph.AddNode(VisualShader.Type.Vertex, scale, default, 2); graph.AddNode(VisualShader.Type.Fragment, scale, default, 2); graph.AddNode(VisualShader.Type.Fragment, gain, default, 3); graph.AddNode(VisualShader.Type.Vertex, gain, default, 3);
            int Add(VisualShader.Type stage, VisualShaderNode node) { owned.Add(node); var id = next++; graph.AddNode(stage, node, default, id); return id; }
            int Operator(VisualShader.Type stage, VisualShaderNodeFloatOp.OperatorEnum mode, int left, int right)
            { var id = Add(stage, new VisualShaderNodeFloatOp { Operator = mode }); graph.ConnectNodes(stage, left, 0, id, 0); graph.ConnectNodes(stage, right, 0, id, 1); return id; }
            try
            {
                var sum = 0;
                for (var i = 0; i < 16; i++)
                {
                    var slot = Add(VisualShader.Type.Vertex, new VisualShaderNodeFloatParameter { ParameterName = ParameterLayoutNames.Slots[i], DefaultValue = (i + 1) / 32f, DefaultValueEnabled = true, Qualifier = i == 15 ? Q.InstanceIndex : Q.Instance, InstanceIndex = 15 });
                    var weight = Add(VisualShader.Type.Vertex, new VisualShaderNodeFloatConstant { Constant = i == observed ? 1 : 0 });
                    var term = Operator(VisualShader.Type.Vertex, VisualShaderNodeFloatOp.OperatorEnum.Multiply, slot, weight);
                    sum = i == 0 ? term : Operator(VisualShader.Type.Vertex, VisualShaderNodeFloatOp.OperatorEnum.Add, sum, term);
                }
                var scaled = Operator(VisualShader.Type.Vertex, VisualShaderNodeFloatOp.OperatorEnum.Multiply, sum, 2); var lit = Operator(VisualShader.Type.Vertex, VisualShaderNodeFloatOp.OperatorEnum.Add, scaled, 3); graph.ConnectNodes(VisualShader.Type.Vertex, lit, 0, 0, 2);
                var incoming = Add(VisualShader.Type.Fragment, new VisualShaderNodeInput { InputName = "color" });
                var factor = Add(VisualShader.Type.Fragment, new VisualShaderNodeFloatConstant { Constant = .125f });
                var contribution = Operator(VisualShader.Type.Fragment, VisualShaderNodeFloatOp.OperatorEnum.Multiply, 2, factor);
                var add = Operator(VisualShader.Type.Fragment, VisualShaderNodeFloatOp.OperatorEnum.Add, incoming, 3);
                var result = Operator(VisualShader.Type.Fragment, VisualShaderNodeFloatOp.OperatorEnum.Add, add, contribution); graph.ConnectNodes(VisualShader.Type.Fragment, result, 0, 0, 0);
                ResourceSaver.Save(graph, IOPath.Combine(folder, $"parameter-layout-{observed}.e2dres"), SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
            }
            finally { foreach (var item in owned) item.Dispose(); }
        }
    }
    private static Color ParameterLayoutExpected(string name, int phase)
    {
        var index = int.Parse(name.AsSpan("parameter-layout-".Length));
        var scalar = phase == 1 ? (16 - index) / 32f : (index + 1) / 32f; var scale = phase == 1 ? .75f : .5f; var gain = phase == 1 ? .25f : .125f;
        var result = scalar * scale + 2 * gain + scale * .125f; return new(result, result, result, 1);
    }
    private static void VerifyParameterLayout(string name, VisualShader graph)
    {
        _ = name;
        Check(ReferenceEquals(graph.GetNode(VisualShader.Type.Vertex, 2), graph.GetNode(VisualShader.Type.Fragment, 2)), "One parameter declaration retains its resource alias across both stages.");
        var descriptor = graph.GetShaderUniformList().Single(); Check(descriptor.Name == "p_layout_scale" && descriptor.Hint == PropertyHint.Range && descriptor.HintString == "0,1,0.125", "Shared-stage material hint metadata agrees.");
        var words = MemoryMarshal.Cast<byte, uint>(graph.GetSPIRV(RenderingDevice.ShaderStage.Vertex));
        var members = new Dictionary<(uint, int), string>(); var offsets = new Dictionary<(uint, int), uint>();
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var count = (int)(words[at] >> 16); var op = words[at] & 0xffff;
            if (op == 6 && count >= 4)
            {
                var bytes = MemoryMarshal.AsBytes(words.Slice(at + 3, count - 3)); var end = bytes.IndexOf((byte)0);
                members[(words[at + 1], (int)words[at + 2])] = Encoding.UTF8.GetString(bytes[..end]);
            }
            if (op == 72 && count == 5 && words[at + 3] == 35) offsets[(words[at + 1], (int)words[at + 2])] = words[at + 4];
        }
        var matched = 0;
        foreach (var member in members)
        {
            if (!member.Value.StartsWith("slot_", StringComparison.Ordinal)) continue;
            var index = int.Parse(member.Value.AsSpan(5)); Check(offsets[member.Key] == index * 16u, "SPIR-V member offsets preserve all automatic slots and explicit slot fifteen."); matched++;
        }
        Check(matched == 16, "The complete per-item layout is present in the compiled interface.");
    }
    private static void WriteParameterLayout(ShaderMaterial material, Entity item, bool reset)
    {
        if (reset) material.SetShaderParameter("p_layout_scale", (Texture?)null); else material.SetShaderParameter("p_layout_scale", .75f);
        RenderingServer.GlobalShaderParameterSet("p_layout_gain", reset ? .125f : .25f);
        for (var i = 0; i < 16; i++) item.SetInstanceShaderParameter<float>(ParameterLayoutNames.Slots[i], reset ? null : (16 - i) / 32f);
    }
}
