using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static partial class MultiMeshGPUTests
{
    private static void Interpolation()
    {
        var interpolation = ProjectSettings.Get(ProjectSettings.PhysicsInterpolation); var fps = Engine.MaxFPS; var ticks = Engine.PhysicsTicksPerSecond;
        ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, true); Engine.MaxFPS = 120; Engine.PhysicsTicksPerSecond = 30;
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshTests.Quad(Colors.White));
        using var window = new Window { Size = new(128, 96) }; var resources = new List<MultiMesh>();
        var pairs = new List<(SubViewport Reference, SubViewport GPU, MultiMesh Source, MultiMesh Target, float[] Current, float[] Previous, string Name)>();
        foreach (var name in new[] { "fast", "high", "reflected", "singular", "half turn" })
        {
            var quality = name == "fast" ? MultiMesh.PhysicsInterpolationQuality.Fast : MultiMesh.PhysicsInterpolationQuality.High;
            MultiMesh Source() { var value = new MultiMesh { Mesh = mesh, UseColors = true, UseCustomData = true, InstanceCount = 1, PhysicsInterpolationQualityMode = quality, CustomAABB = new(0, 0, 128, 96) }; resources.Add(value); return value; }
            var source = Source(); var target = Source();
            source.SetInstanceTransform2D(0, new(.2f, new Vector2(.8f, name == "reflected" ? -.6f : .6f), .1f, new(32, 24))); source.SetInstanceColor(0, new(.2f, .8f, .4f, .7f)); source.SetInstanceCustomData(0, Colors.Blue); var previous = source.Buffer;
            source.SetInstanceTransform2D(0, name == "singular" ? new(Vector2.Zero, new(0, 1), new(56, 40)) : new(name == "half turn" ? .2f - Mathf.Pi : 1.1f, new Vector2(1.2f, name == "reflected" ? -1 : 1), .3f, new(56, 40))); source.SetInstanceColor(0, new(.8f, .3f, .7f, .9f)); source.SetInstanceCustomData(0, Colors.Red); var current = source.Buffer;
            SubViewport View(MultiMesh value) { var view = new SubViewport { Size = new(128, 96), RenderTargetUpdateMode = ViewportUpdateMode.Always }; view.AddChild(new MultiMeshInstance { MultiMesh = value }); window.AddChild(view); return view; }
            pairs.Add((View(source), View(target), source, target, current, previous, name));
        }
        var frames = 0; var checkedFrame = false;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); var device = RenderingServer.GetRenderingDevice()!;
            RenderingServer.FramePreDraw += () =>
            {
                foreach (var pair in pairs)
                {
                    pair.Source.SetBufferInterpolated(pair.Current, pair.Previous); pair.Target.SetBufferInterpolated(pair.Previous, pair.Previous);
                    var rid = RenderingServer.MultiMeshGetBufferRDRID(pair.Target.GetRID()); device.BufferUpdate(rid, 0, 64, MemoryMarshal.AsBytes(pair.Current.AsSpan()));
                }
            };
            RenderingServer.FramePostDraw += () =>
            {
                Check(++frames < 500, "A fractional interpolation frame must arrive."); if (Engine.PhysicsInterpolationFraction is < .2 or > .6) return;
                foreach (var pair in pairs)
                {
                    using var a = pair.Reference.GetTexture().GetImage()!; using var b = pair.GPU.GetTexture().GetImage()!; var occupied = 0;
                    for (var y = 0; y < 96; y++) for (var x = 0; x < 128; x++) { var p = a.GetPixel(x, y); var q = b.GetPixel(x, y); if (p.R + p.G + p.B > .01f) occupied++; Check(MathF.Abs(p.R - q.R) < .012f && MathF.Abs(p.G - q.G) < .012f && MathF.Abs(p.B - q.B) < .012f, $"GPU {pair.Name} interpolation at f={Engine.PhysicsInterpolationFraction}, {x},{y}: {p} / {q}."); }
                    Check(occupied > 20, "Interpolation oracle contains visible geometry.");
                    Check(pair.Target.Buffer.SequenceEqual(pair.Previous), "GPU interpolation retains independent authored snapshots.");
                }
                checkedFrame = true; window.Tree!.Quit();
            };
        };
        try { Check(Engine.Run(window) == 0 && checkedFrame, "Actual fractional native GPU-buffer interpolation completed."); }
        finally { foreach (var resource in resources) resource.Dispose(); ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, interpolation); Engine.MaxFPS = fps; Engine.PhysicsTicksPerSecond = ticks; }
        Console.WriteLine("GPU instance interpolation: actual fractional Fast/High, reflected, singular and half-turn poses/color matched independent authored replay.");
    }
}
