using Electron2D;

try
{
    var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-line-" + Guid.NewGuid().ToString("N") + ".e2dscene");
    using var curve = new Curve(); curve.AddPoint(new(0, 1)); curve.AddPoint(new(1, .25f));
    using var gradient = new Gradient { Colors = [Colors.Red, Colors.Blue] };
    using var pixels = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); pixels.Fill(Colors.Green);
    using var texture = ImageTexture.CreateFromImage(pixels);
    using var line = new Line { Points = [new(0, 0), new(30, 0), new(30, 0), new(30, 20), new(0, 0)], Width = 8, WidthCurve = curve, Gradient = gradient, Texture = texture, DefaultColor = Colors.Yellow, Closed = true, TextureMode = LineTextureMode.Stretch, JointMode = LineJointMode.Round, BeginCapMode = LineCapMode.Box, EndCapMode = LineCapMode.Round, SharpLimit = 3, RoundPrecision = 12, Antialiased = true };
    using var packed = new PackedScene(); packed.Pack(line);
    try
    {
        ResourceSaver.Save(packed, path, SaverFlags.BundleResources);
        using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep);
        using var restored = (Line)loaded.Instantiate();
        Check(restored.Points.SequenceEqual(line.Points) && restored.Closed && restored.Width == 8 && restored.DefaultColor == Colors.Yellow, "Stored points, closure and colors survive file restoration.");
        Check(restored.WidthCurve!.SampleBaked(1) == .25f && restored.Gradient!.Sample(1) == Colors.Blue && restored.Texture!.GetWidth() == 1, "Borrowed resources restore through the bundled typed graph.");
        Check(restored.TextureMode == LineTextureMode.Stretch && restored.JointMode == LineJointMode.Round && restored.BeginCapMode == LineCapMode.Box && restored.EndCapMode == LineCapMode.Round && restored.SharpLimit == 3 && restored.RoundPrecision == 12 && restored.Antialiased, "All stored geometry settings restore.");
        var copy = restored.Points; copy[0] = new(999, 999); Check(restored.GetPointPosition(0) == Vector2.Zero, "Restored point reads remain copied.");
    }
    finally { File.Delete(path); }
    Console.WriteLine("Public line consumer: all fourteen property roles and bundled fresh-file state passed.");
    if (args.Length > 0) LineGeometryRenderingTests.Run(args[0]);
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }

static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
