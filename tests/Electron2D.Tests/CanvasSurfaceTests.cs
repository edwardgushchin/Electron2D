using Electron2D;
using System.Runtime.InteropServices;
using System.Text;
using static Electron2D.MaterialLightingProgram;

internal static partial class CanvasLightGraphTests
{
    private static void SurfaceContract(VisualShader graph)
    {
        void Check(bool value, string message) { if (!value) throw new Exception(message); }
        void Reject(Action action)
        {
            try { action(); }
            catch (Exception error) when (error is ArgumentException or NotSupportedException or InvalidDataException) { return; }
            throw new Exception("A renderer-owned normal basis was accepted as user data.");
        }
        using var material = new ShaderMaterial { Shader = graph };
        var original = graph.GetSPIRV(); var vertex = graph.GetSPIRV(RenderingDevice.ShaderStage.Vertex);
        foreach (var name in new[] { "NORMAL_MODEL_BASIS", "NORMAL_CANVAS_BASIS" })
        {
            Check(!graph.GetShaderUniformList().Any(p => p.Name == name) && !material.GetPropertyList().Any(p => p.Name == "shader_parameter/" + name), "Normal bases are renderer-owned properties.");
            Reject(() => material.SetShaderParameter(name, Vector4.One));
            Reject(() => material.GetShaderParameter<Vector4>(name));
            Reject(() => graph.SetDefaultTextureParameter(name, null));
            Reject(() => new ShaderGlobalParameters().Add(name, RenderingServer.GlobalShaderParameterType.Vec4, Vector4.One));
            using var reserved = new VisualShader(); using var parameter = new VisualShaderNodeVec4Parameter { ParameterName = name };
            reserved.AddNode(VisualShader.Type.Fragment, parameter, default, 2); reserved.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
            Reject(() => reserved.GenerateSources());
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
                Check(graph.GetSPIRV().SequenceEqual(original), "A rejected normal basis retains the original shader.");
            }
        }
        var model = new Transform(new(2, 0), new(1, 3), new(8, 9)); var instance = new Transform(.3f, new Vector2(4, 5));
        var matrices = new CanvasMatrices(model, Transform.Identity).Instance(instance);
        Check(matrices.Model == model * instance && matrices.NormalModel == model, "Instance poses affect vertex matrices and preserve the fragment normal basis.");
        Check(matrices.Local(instance).NormalModel == model * instance, "Draw-local poses affect both vertex and normal matrices.");
        Check(CanvasMatrices.NormalBasis(new(Vector2.Zero, new(0, -2), new(99, 99))) == new Vector4(0, 0, 0, -1), "Degenerate axes remain finite; translation does not affect normals.");
        Console.WriteLine("Canvas normal bases: material/global/graph ownership, malformed shapes/roles, atomic replacement and instance/degenerate bases passed.");
    }
}
