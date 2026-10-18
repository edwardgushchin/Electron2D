using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static partial class MultiMeshGPUTests
{
    private static void Sessions()
    {
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshTests.Quad(Colors.White));
        using var resource = new MultiMesh { Mesh = mesh, UseColors = true, InstanceCount = 1, CustomAABB = new(0, 0, 128, 96) }; resource.SetInstanceTransform2D(0, new(0, new(8, 8))); resource.SetInstanceColor(0, Colors.Red);
        var logical = resource.GetRID(); RID firstBuffer = default, owned = default; RenderingDevice? firstDevice = null;
        for (var session = 0; session < 2; session++)
        {
            var index = session; using var window = new Window { Size = new(128, 96) }; window.AddChild(new MultiMeshInstance { MultiMesh = resource }); var frames = 0;
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black); var device = RenderingServer.GetRenderingDevice()!; var current = RenderingServer.MultiMeshGetBufferRDRID(logical);
                if (index == 0)
                {
                    firstDevice = device; firstBuffer = current; var native = resource.Buffer; native[3] = 24; native[8] = 0; native[9] = 1; device.BufferUpdate(current, 0, 48, MemoryMarshal.AsBytes(native.AsSpan()));
                    owned = RenderingServer.MultiMeshCreate(); RenderingServer.MultiMeshAllocateData(owned, 1, useIndirect: true); RenderingServer.MultiMeshGetCommandBufferRDRID(owned);
                }
                else
                {
                    Check(firstDevice!.IsDisposed && !ReferenceEquals(device, firstDevice) && current != firstBuffer && resource.GetRID() == logical, "A new renderer session retains logical resource identity and replaces native ownership.");
                    Reject<ArgumentException>(() => device.BufferGetData(firstBuffer)); Reject<ArgumentException>(() => RenderingServer.MultiMeshGetBufferRDRID(owned));
                    Check(MemoryMarshal.Cast<byte, float>(device.BufferGetData(current)).SequenceEqual(resource.Buffer), "A new session starts from authored snapshots instead of expired GPU data.");
                }
                RenderingServer.FramePostDraw += () => { using var pixels = window.GetTexture().GetImage()!; var p = pixels.GetPixel(index == 0 ? 28 : 12, 12); Check(index == 0 ? p.G > .9f && p.R < .1f : p.R > .9f && p.G < .1f, "Session-specific native storage actually renders."); frames++; window.Tree!.Quit(); };
            };
            Check(Engine.Run(window) == 0 && frames == 1 && !resource.IsDisposed, "Renderer shutdown preserves borrowed authoring resources.");
        }
        Console.WriteLine("GPU instance sessions: borrowed logical identity survives, native identity expires, owned resources retire and authored data restores.");
    }
}
