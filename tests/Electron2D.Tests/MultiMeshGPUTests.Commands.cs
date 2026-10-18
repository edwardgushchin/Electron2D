using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static partial class MultiMeshGPUTests
{
    private static void Commands(string language = "GLSL", bool wide = false, bool modern = false)
    {
        using var mesh = new ArrayMesh(); Vector2[] vertices = [Vector2.Zero, new(20, 0), new(0, 20), new(40, 0), new(60, 0), new(40, 20)];
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = vertices, Indices = [3, 4, 5, 0, 1, 2] }); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = vertices });
        byte[] Module(string name) { using var input = typeof(MultiMeshGPUTests).Assembly.GetManifestResourceStream("Electron2D.Device." + name)!; using var output = new MemoryStream(); input.CopyTo(output); return output.ToArray(); }
        var vertex = Module("DeviceInstance" + language + ".vert.spv"); using var shader = Shader.CreateFromSPIRV(modern ? MeshVertexIDTests.Modern(vertex) : vertex, Module("DeviceVertexIDGLSL.frag.spv")); using var material = new ShaderMaterial { Shader = shader }; material.SetShaderParameter("expected_first", wide ? 65536 : 0);
        using var window = new Window { Size = new(128, 96) }; var draw = new Draw { Material = material }; window.AddChild(draw); var first = wide ? 65536 : 0; var frames = 0; RID owned = default, command = default;
        uint[] words = [3, 1, 3, 3, (uint)(first + 1), 3, 1, 3, (uint)first, 0]; RenderingDevice device = null!;
        void Upload() => device.BufferUpdate(command, 0, 40, MemoryMarshal.AsBytes(words.AsSpan()));
        window.Ready += _ =>
        {
            device = RenderingServer.GetRenderingDevice()!; RenderingServer.SetDefaultClearColor(Colors.Black); owned = RenderingServer.MultiMeshCreate(); RenderingServer.MultiMeshAllocateData(owned, first + 2, true, false, true); RenderingServer.MultiMeshSetMesh(owned, mesh.GetRID()); RenderingServer.MultiMeshSetCustomAABB(owned, new(0, 0, 128, 96));
            RenderingServer.MultiMeshInstanceSetTransform2D(owned, first, new(0, new(8, 8))); RenderingServer.MultiMeshInstanceSetTransform2D(owned, first + 1, new(0, new(8, 48))); RenderingServer.MultiMeshInstanceSetColor(owned, first, Colors.Red); RenderingServer.MultiMeshInstanceSetColor(owned, first + 1, Colors.Green);
            command = RenderingServer.MultiMeshGetCommandBufferRDRID(owned); Upload(); draw.Instances = owned;
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = window.GetTexture().GetImage()!;
                void At(int x, int y, Color expected) { var c = pixels.GetPixel(x, y); Check(MathF.Abs(c.R - expected.R) < .01f && MathF.Abs(c.G - expected.G) < .01f && MathF.Abs(c.B - expected.B) < .01f, $"Indirect fields frame={frames}, {x},{y}: {c} / {expected}."); }
                if (frames == 0) { At(52, 52, Colors.Green); At(52, 12, Colors.Red); At(12, 12, Colors.Black); words[2] = 0; words[4] = (uint)first; words[3] = unchecked((uint)-3); words[6] = 0; Upload(); }
                else if (frames == 1) { At(12, 12, Colors.Red); At(52, 52, Colors.Black); At(52, 12, Colors.Black); RenderingServer.MultiMeshSetVisibleInstances(owned, 0); }
                else if (frames == 2) { At(12, 12, Colors.Black); At(12, 52, Colors.Black); RenderingServer.MultiMeshSetVisibleInstances(owned, -1); }
                else { At(12, 12, Colors.Red); At(12, 52, Colors.Green); RenderingServer.FreeRID(owned); device.FreeRID(command); window.Tree!.Quit(); }
                frames++;
            };
        };
        Check(Engine.Run(window) == 0 && frames == 4, "Indexed/nonindexed indirect fields, first instance and zero commands execute.");
        Console.WriteLine($"GPU indirect commands {language}, first={first}, modern={modern}: first index/vertex, signed base vertex, first instance, zero count and visible-prefix changes passed.");
    }
}
