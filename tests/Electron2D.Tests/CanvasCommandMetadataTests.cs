using Electron2D;
using System.Runtime.InteropServices;
using System.Text;
using static Electron2D.MaterialLightingProgram;

internal static class CanvasCommandMetadataTests
{
    internal static void Run(string folder)
    {
        void Check(bool value, string message) { if (!value) throw new Exception(message); }
        void Reject(Action action)
        {
            try { action(); }
            catch (Exception error) when (error is ArgumentException or NotSupportedException or InvalidDataException) { return; }
            throw new Exception("Renderer-owned command metadata was accepted as user data.");
        }
        using (var input = new VisualShaderNodeInput())
            foreach (var name in new[] { "region_rect", "specular_shininess" })
            {
                input.InputName = name;
                foreach (var stage in Enum.GetValues<VisualShader.Type>().Where(stage => stage != VisualShader.Type.Max))
                    Check(input.OutputsFor(stage).Single() == (stage == VisualShader.Type.Fragment || name == "specular_shininess" && stage == VisualShader.Type.Light ? VisualShaderNode.PortType.Vector4 : VisualShaderNode.PortType.Scalar), "Command inputs retain exact borrowing-stage roles.");
            }
        using var graph = ResourceLoader.Load<VisualShader>(System.IO.Path.Combine(folder, "metadata-global.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
        using var material = new ShaderMaterial { Shader = graph }; material.SetShaderParameter("component", 2f);
        var original = graph.GetSPIRV(); var vertex = graph.GetSPIRV(RenderingDevice.ShaderStage.Vertex); var changed = 0; graph.Changed += _ => changed++;
        foreach (var name in new[] { "REGION_RECT", "CANVAS_SPECULAR" })
        {
            Check(!graph.GetShaderUniformList().Any(p => p.Name == name) && !material.GetPropertyList().Any(p => p.Name == "shader_parameter/" + name), "Command fields remain renderer-owned.");
            Reject(() => material.SetShaderParameter(name, Vector4.One)); Reject(() => material.GetShaderParameter<Vector4>(name));
            Reject(() => graph.SetDefaultTextureParameter(name, null));
            Reject(() => new ShaderGlobalParameters().Add(name, RenderingServer.GlobalShaderParameterType.Vec4, Vector4.One));
            using var reserved = new VisualShader(); using var parameter = new VisualShaderNodeVec4Parameter { ParameterName = name };
            reserved.AddNode(VisualShader.Type.Fragment, parameter, default, 2); reserved.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0); Reject(() => reserved.GenerateSources());
            foreach (var role in new[] { "shape", "Globals", "Instance" })
            {
                var module = new Module(original);
                var member = module.Instructions.Single(i => Op(i) == 6 && Encoding.UTF8.GetString(MemoryMarshal.AsBytes(i.AsSpan(3))).TrimEnd('\0') == name);
                if (role == "shape") module.Instructions.Single(i => Op(i) == 30 && i[1] == member[1])[2 + member[2]] = module.Instructions.First(i => Op(i) == 22)[1];
                else
                {
                    var bytes = Encoding.UTF8.GetBytes(role); var padded = new byte[(bytes.Length + 4) & ~3]; bytes.CopyTo(padded, 0);
                    var index = module.Instructions.FindIndex(i => Op(i) == 5 && i[1] == member[1]);
                    module.Instructions[index] = Instruction(5, [member[1], .. MemoryMarshal.Cast<byte, uint>(padded).ToArray()]);
                }
                Reject(() => graph.SetSPIRV(vertex, module.Write()));
                Check(changed == 0 && graph.GetSPIRV().SequenceEqual(original) && graph.GetSPIRV(RenderingDevice.ShaderStage.Vertex).SequenceEqual(vertex), "Invalid command fields reject both stages atomically.");
            }
            var state = material.GetCanvasState()!; var uniform = name == "REGION_RECT" ? state.Program.RegionRectUniform! : state.Program.CanvasSpecularUniform!;
            var lastFrame = new Vector4(9, 8, 7, 6); MemoryMarshal.Write(state.Buffers[uniform.Buffer].AsSpan(uniform.Offset), in lastFrame);
            using var copy = (ShaderMaterial)material.Duplicate(); var copied = copy.GetCanvasState()!;
            Check(copy.GetShaderParameter<float>("component") == 2 && MemoryMarshal.Read<Vector4>(copied.Buffers[uniform.Buffer].AsSpan(uniform.Offset)) == Vector4.Zero, "Material copies preserve user data and reset renderer snapshots.");
        }
        using var image = Image.CreateEmpty(8, 4, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
        var vertices = new List<CanvasVertex>(); var command = new CanvasCommand(false, Vector2.Zero, new(-16, 16), Colors.White, 0, false, Transform.Identity, texture, new(.25f, .25f, .5f, .5f));
        var result = CanvasGeometry.Append(vertices, command, Transform.Identity, Colors.White);
        Check(result.TextureRegion == new Rect2(.25f, .25f, -.5f, .5f) && vertices.Count == 6, "Rect metadata preserves the original signed source independently of geometry expansion.");
        vertices.Clear(); result = CanvasGeometry.Append(vertices, command with { Source = new(.25f, .25f, -.5f, .5f), Transpose = true }, Transform.Identity, Colors.White);
        Check(result.TextureRegion == new Rect2(.25f, .25f, .5f, .5f), "Source and destination flips cancel; transpose does not reorder region components.");
        vertices.Clear();
        result = CanvasGeometry.Append(vertices, command with { Source = new(2, 1, -4, 2), NinePatch = new(Vector2.One, Vector2.One, AxisStretchMode.Stretch, AxisStretchMode.Stretch, true) }, Transform.Identity, Colors.White);
        Check(result.TextureRegion == new Rect2(.25f, .25f, -.5f, .5f) && vertices.Count > 6, "Reflected nine-patch geometry retains the original signed complete source independently of its pieces.");
        Console.WriteLine("Command metadata contract: ownership, field shape/roles, atomic replacement, material copying and signed rect capture passed.");
    }
}
