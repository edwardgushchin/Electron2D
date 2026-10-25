using Electron2D;
using System.Text.Json;

internal static class LineGeometryTests
{
    internal static void Run()
    {
        LineTests.Run();
        using var line = new Line { Points = [new(0, 0), new(30, 0)], Width = 10, Antialiased = true, BeginCapMode = LineCapMode.Round, EndCapMode = LineCapMode.Round };
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        void Capture(Transform transform) { line.PrepareCanvas(); vertices.Clear(); batches.Clear(); line.AppendCanvas(vertices, batches, transform); }
        Capture(Transform.Identity);
        Check(vertices.Any(v => v.Position.X < -5 && v.Color.A == 0) && vertices.Any(v => v.Position.X > 35 && v.Color.A == 0), "Both round caps receive full boundary smoothing.");
        Check(vertices.Where(v => v.Color.A == 0).All(v => v.Position.X is < 0 or > 30 || MathF.Abs(v.Position.Y) >= 5), "Cap/body seams and triangulation diagonals never emit an interior feather.");
        Capture(new Transform(new(4, 0), new(0, 2), Vector2.Zero));
        Check(vertices.Where(v => v.Color.A == 0).All(v => MathF.Abs(v.Position.Y) <= 11.01f), "Boundary smoothing stays one framebuffer pixel under scaling.");
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image); line.Texture = texture; line.Antialiased = false;
        Capture(Transform.Identity); Check(vertices.All(v => v.UV == Vector2.Zero), "TextureMode.None supplies the default zero UV.");
        line.BeginCapMode = line.EndCapMode = LineCapMode.None; line.Texture = null; Capture(Transform.Identity);
        Check(vertices.Select(v => v.VertexID).Distinct().Order().SequenceEqual(new uint[] { 0, 1, 2, 3 }) && vertices.GroupBy(v => v.Position).All(g => g.Select(v => v.VertexID).Distinct().Count() == 1), "Indexed strips preserve source vertex identity across expanded triangles.");
        var original = vertices.ToArray(); line.Points = [new(0, 0), new(0, 0), new(30, 0), new(30, 0)]; Capture(Transform.Identity);
        Check(vertices.SequenceEqual(original) && line.GetPointCount() == 4, "Consecutive duplicate authoring points preserve storage and produce the same geometry.");
        line.Points = [new(0, 0), new(30, 0), new(0, 0)]; Capture(Transform.Identity);
        Check(vertices.Count > 0 && vertices.All(v => v.Position.IsFinite()), "An exact reversal remains a finite bounded strip.");
        line.Points = [new(0, 0), new(30, 0)];
        line.BeginCapMode = line.EndCapMode = LineCapMode.Round; line.Texture = texture;
        line.TextureMode = LineTextureMode.Stretch; Capture(Transform.Identity); Check(vertices.Any(v => v.UV.X > 0 && v.UV.Y > 0), "Round cap UVs preserve both texture components.");
        using var zero = new Curve(); line.WidthCurve = zero; line.Closed = true; line.Points = [new(0, 0), new(20, 0), new(20, 20)]; Capture(Transform.Identity);
        Check(vertices.All(v => v.Position.IsFinite() && v.UV.IsFinite()), "Closed zero-width curves remain finite.");
        Console.WriteLine("Line strip geometry, full cap boundary, transform-invariant smoothing and zero-width closure passed.");
    }
    internal static void Dump(string path)
    {
        var rows = new List<object>();
        Vector2[][] paths = [[new(0, 0), new(40, 0)], [new(0, 0), new(30, 0), new(30, 25)], [new(0, 0), new(30, 0), new(30, -25)], [new(0, 0), new(30, 0), new(1, 2)], [new(0, 0), new(30, 0), new(30, 25), new(0, 25)], [new(0, 0), new(10, 3), new(20, -8), new(35, 10)]];
        using var pixels = Image.CreateEmpty(4, 2, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(pixels);
        using var gradient = new Gradient { Colors = [new(.2f, .7f, .3f, .4f), new(.9f, .1f, .6f, .8f)] };
        using var curve = new Curve(); curve.AddPoint(new(0, 1), rightMode: Curve.TangentMode.Linear); curve.AddPoint(new(1, .25f), leftMode: Curve.TangentMode.Linear);
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        for (var sample = 0; sample < 3888; sample++)
        {
            var value = sample; var shape = value % 6; value /= 6; var joint = value % 3; value /= 3; var begin = value % 3; value /= 3; var end = value % 3; value /= 3;
            var mode = value % 3; value /= 3; var closed = value % 2 != 0; value /= 2; var useCurve = value % 2 != 0; value /= 2; var useGradient = value % 2 != 0; var precision = sample % 7 == 0 ? 1 : sample % 7 == 1 ? 3 : 8;
            using var line = new Line { Points = paths[shape], Width = 8, SharpLimit = sample % 3 == 0 ? 1 : 2, RoundPrecision = precision, Closed = closed, JointMode = (LineJointMode)joint, BeginCapMode = (LineCapMode)begin, EndCapMode = (LineCapMode)end, TextureMode = (LineTextureMode)mode, Texture = texture, WidthCurve = useCurve ? curve : null, Gradient = useGradient ? gradient : null, DefaultColor = new(.6f, .3f, .8f, .5f) };
            line.PrepareCanvas(); vertices.Clear(); batches.Clear(); line.AppendCanvas(vertices, batches, Transform.Identity);
            Check(vertices.All(v => v.Position.IsFinite() && v.UV.IsFinite() && v.Color.IsFinite()), "Every recorded strip attribute is finite.");
            rows.Add(new { Sample = sample, Points = paths[shape].Select(v => new[] { v.X, v.Y }).ToArray(), Joint = joint, Begin = begin, End = end, Mode = mode, Closed = closed, Curve = useCurve, Gradient = useGradient, Precision = precision, SharpLimit = line.SharpLimit, Geometry = vertices.Select(v => new[] { v.Position.X, v.Position.Y, v.UV.X, v.UV.Y, v.Color.R, v.Color.G, v.Color.B, v.Color.A }).ToArray() });
        }
        File.WriteAllText(path, JsonSerializer.Serialize(rows)); Console.WriteLine($"Dumped {rows.Count} independent line geometry fixtures.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
