using Electron2D;
using static TextureArrayTests;

internal static partial class CanvasHDRTests
{
    private sealed class MeshPainter(ArrayMesh mesh, MultiMesh instances) : Entity
    {
        protected override void OnDraw()
        {
            DrawMesh(mesh, transform: new Transform(0, new(4, 4)), modulate: new(.5f, .5f, .5f, 1));
            DrawMultiMesh(instances);
            DrawPolygon([new(64, 4), new(80, 4), new(64, 20)], [new(.5f, .5f, .5f, 1)]);
        }
    }
    internal static void RunMesh()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData
        { Vertices = [new(0, 0), new(16, 0), new(0, 16)], Colors = [new(.5f, .5f, .5f, 1), new(.5f, .5f, .5f, 1), new(.5f, .5f, .5f, 1)] });
        using var instances = new MultiMesh { Mesh = mesh, UseColors = true, InstanceCount = 1 };
        instances.SetInstanceTransform2D(0, new(0, new(32, 4))); instances.SetInstanceColor(0, new(.5f, .5f, .5f, 1));
        var root = new Window { Size = new(128, 64), UseHDR2D = true };
        var painter = new MeshPainter(mesh, instances) { SelfModulate = new(.5f, .5f, .5f, 1) }; root.AddChild(painter);
        using var output = root.GetTexture(); var stage = 0;
        root.Ready += _ =>
        {
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = output.GetImage()!;
                var expected = Linear(.5f) * Linear(.25f);
                Pixel(pixels, 7, 7, expected, .001f);
                Pixel(pixels, 35, 7, Linear(.5f) * Linear(.5f) * Linear(.5f), .001f);
                Pixel(pixels, 67, 7, Linear(.5f) * Linear(.5f), .001f);
                if (stage++ == 0) root.SnapVerticesToPixel = true;
                else root.Tree!.Quit();
            };
        };
        Check(Engine.Run(root) == 0 && stage == 2, "HDR mesh paths complete.");
        Reject<InvalidOperationException>(() => CanvasGeometry.LinearColor(new(float.MaxValue, 0, 0, 1)));
        Check(CanvasGeometry.LinearColor(new(-.1f, .04045f, 1, .25f)).IsFinite(), "HDR signed/transfer-boundary colors remain finite.");
        Console.WriteLine("HDR mesh: separate command/vertex/instance color conversion, polygon colors, GPU instancing and snapped CPU fallback, finite limits passed.");
    }
    private static void Pixel(Image image, int x, int y, float expected, float tolerance = .002f)
    { var actual = image.GetPixel(x, y).R; Check(MathF.Abs(actual - expected) < tolerance, $"HDR pixel {x},{y}: {actual} != {expected}."); }
}
