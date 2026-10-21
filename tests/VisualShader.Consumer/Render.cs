using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    private const int GraphColumns = 48;
    internal static void Render(string folder, string[] args)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (args.Contains("msaa")) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        var rows = (Directory.GetFiles(folder, "*.e2dres").Length + GraphColumns - 1) / GraphColumns;
        using var window = new Window { Size = new(8 + GraphColumns * 24, Math.Max(224, rows * 24 + 24)), Title = "Imported visual shader graphs", UseHDR2D = args.Contains("hdr") };
        window.AddChild(new GraphScene(folder)); Engine.Run(window);
        Check(window.IsDisposed && !RenderingServer.IsAvailable, "Graph render host retires its resources.");
    }
    private sealed class GraphPainter(Texture texture) : Entity
    {
        internal int Draws;
        protected override void OnDraw() { Draws++; DrawTextureRect(texture, new(0, 0, 16, 16), false); }
    }
    private sealed class GraphScene(string folder) : Node
    {
        private readonly List<Resource> _owned = [];
        private readonly List<(GraphPainter Painter, string Name, Vector2 Position)> _cases = [];
        private int _frame;
        private long _ownerBytes, _allBytes;
        private int[] _drawCounts = [];
        private T Own<T>(T resource) where T : Resource { _owned.Add(resource); return resource; }
        protected override void OnReady()
        {
            OS.LowProcessorUsageMode = false; RenderingServer.SetDefaultClearColor(Colors.Black);
            using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(Colors.White);
            var texture = Own(ImageTexture.CreateFromImage(image));
            foreach (var file in Directory.GetFiles(folder, "*.e2dres").Order(StringComparer.Ordinal))
            {
                var name = IOPath.GetFileNameWithoutExtension(file);
                var shader = Own(ResourceLoader.Load<VisualShader>(file, ResourceLoader.CacheMode.IgnoreDeep));
                if (name.StartsWith("integer-", StringComparison.Ordinal)) VerifyIntegerArchive(name, shader);
                if (name.StartsWith("condition-", StringComparison.Ordinal)) VerifyConditionArchive(name, shader);
                if (name.StartsWith("matrix-", StringComparison.Ordinal)) VerifyMatrixArchive(name, shader);
                if (name.StartsWith("range-", StringComparison.Ordinal)) VerifyRangeArchive(name, shader);
                var material = Own(new ShaderMaterial { Shader = shader });
                var position = new Vector2(8 + _cases.Count % GraphColumns * 24, 8 + _cases.Count / GraphColumns * 24);
                var painter = new GraphPainter(texture) { Position = position, Material = material };
                AddChild(painter); _cases.Add((painter, name, position));
            }
            Check(_cases.Count > 0, "Compiled graph fixtures exist.");
            RenderingServer.FramePostDraw += AfterDraw;
        }
        protected override void OnProcess(double delta)
        {
            _frame++;
            if (_frame is > 30 and <= 94)
                foreach (var item in _cases) item.Painter.Position = item.Position + new Vector2(_frame % 2, 0);
        }
        private void AfterDraw()
        {
            if (_frame == 3)
            {
                using var pixels = GetWindow()!.GetTexture().GetImage()!;
                foreach (var item in _cases)
                {
                    var expected = Expected(item.Name);
                    if (!GetWindow()!.UseHDR2D)
                        expected = new(Math.Clamp(expected.R, 0, 1), Math.Clamp(expected.G, 0, 1), Math.Clamp(expected.B, 0, 1), expected.A);
                    var actual = pixels.GetPixel((int)item.Position.X + 8, (int)item.Position.Y + 8);
                    var integerBytes = item.Name.StartsWith("integer-", StringComparison.Ordinal) || item.Name.StartsWith("condition-s-1-", StringComparison.Ordinal) || item.Name.StartsWith("condition-s-2-", StringComparison.Ordinal);
                    var tolerance = integerBytes ? .001f : (item.Name.StartsWith("condition-", StringComparison.Ordinal) || item.Name.StartsWith("matrix-", StringComparison.Ordinal)) ? .0021f : .016f;
                    Check(MathF.Abs(actual.R - expected.R) < tolerance && MathF.Abs(actual.G - expected.G) < tolerance && MathF.Abs(actual.B - expected.B) < tolerance,
                        $"Graph {item.Name}: actual {actual}, expected {expected}.");
                    if (item.Name == "vertex-shift")
                    {
                        var left = pixels.GetPixel((int)item.Position.X + 1, (int)item.Position.Y + 8);
                        var right = pixels.GetPixel((int)item.Position.X + 18, (int)item.Position.Y + 8);
                        Check(left.R < .01f && right.B > .7f, "Authored vertex arithmetic moves the submitted geometry.");
                    }
                    if (item.Name == "matrix-procedural")
                    {
                        var left = pixels.GetPixel((int)item.Position.X + 2, (int)item.Position.Y + 8);
                        var right = pixels.GetPixel((int)item.Position.X + 13, (int)item.Position.Y + 8);
                        var leftExpected = MatrixObserved(MatrixData(MatrixB), 5);
                        var rightExpected = MatrixObserved(MatrixData(MatrixA), 5);
                        Check(MathF.Abs(left.R - leftExpected.R) < .0021f && MathF.Abs(left.G - leftExpected.G) < .0021f && MathF.Abs(left.B - leftExpected.B) < .0021f &&
                            MathF.Abs(right.R - rightExpected.R) < .0021f && MathF.Abs(right.G - rightExpected.G) < .0021f && MathF.Abs(right.B - rightExpected.B) < .0021f,
                            "UV control selects different numeric matrices across the same submitted quad.");
                    }
                    if (item.Name == "condition-procedural")
                    {
                        var left = pixels.GetPixel((int)item.Position.X + 2, (int)item.Position.Y + 8);
                        var right = pixels.GetPixel((int)item.Position.X + 13, (int)item.Position.Y + 8);
                        Check(left.R < .0021f && left.G < .0021f && left.B < .0021f && MathF.Abs(right.R - .25f) < .0021f && MathF.Abs(right.G - .5f) < .0021f && MathF.Abs(right.B - .75f) < .0021f,
                            "UV-driven comparison selects different branch colors across the same submitted quad.");
                    }
                }
                Console.WriteLine($"{_cases.Count} imported graph programs produced their expected framebuffer pixels.");
                _drawCounts = _cases.Select(c => c.Painter.Draws).ToArray();
            }
            if (_frame is 30 or 110) { _ownerBytes = GC.GetAllocatedBytesForCurrentThread(); _allBytes = GC.GetTotalAllocatedBytes(true); }
            if (_frame is 94 or 174)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _ownerBytes;
                var all = GC.GetTotalAllocatedBytes(true) - _allBytes;
                Console.WriteLine($"Graphs {(_frame == 94 ? "active" : "idle")}: 64 frames, owner={owner}, all={all} bytes.");
                Check(owner == 0 && all == 0, "Compiled visual graph replay has zero prepared managed allocations.");
                for (var i = 0; i < _cases.Count; i++) Check(_cases[i].Painter.Draws == _drawCounts[i], "Graph replay retains draw commands.");
                if (_frame == 174) Tree!.Quit();
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RenderingServer.FramePostDraw -= AfterDraw;
                for (var i = _owned.Count - 1; i >= 0; i--) _owned[i].Dispose();
            }
            base.Dispose(disposing);
        }
    }
    private static Color Expected(string name) => name.StartsWith("matrix-", StringComparison.Ordinal) ? MatrixExpected(name) : name.StartsWith("condition-", StringComparison.Ordinal) ? ConditionExpected(name) : name.StartsWith("integer-", StringComparison.Ordinal) ? IntegerExpected(name) : name.StartsWith("range-", StringComparison.Ordinal) ? RangeExpected(name) : name.StartsWith("flow-", StringComparison.Ordinal) ? DataflowExpected(name) : name.StartsWith("unary-", StringComparison.Ordinal) ? UnaryExpected(name) : name switch
    {
        "color" or "vertex-shift" => new(.25f, .5f, .75f, 1),
        "cleared" => Colors.Black,
        "uv" => new(8.5f / 16, 8.5f / 16, 0, 1),
        "vertex-uv" => new(.25f, .75f, 0, 1),
        _ => NumericExpected(name)
    };
}
