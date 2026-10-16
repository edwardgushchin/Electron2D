using System.Text.Json;
using Electron2D;
using Path = System.IO.Path;

internal static partial class CompressedTextureTests
{
    private sealed class Painter(Texture texture) : Entity
    {
        protected override void OnDraw()
        {
            texture.Draw(this, new(56, 4));
            texture.Draw(this, new(78, 4), transpose: true);
            texture.DrawRectRegion(this, new(98, 4, 24, 20), new(0, 0, 7, 10));
        }
    }
    private sealed class ShaderPatch : Entity
    {
        protected override void OnDraw() => DrawRect(new(0, 0, 24, 20), Colors.White);
    }

    internal static void RunNative(string profile)
    {
        var parts = profile.Split(':'); var backend = parts[0];
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var stream = typeof(CompressedTextureTests).Assembly.GetManifestResourceStream("TestBlocks.oracle.json")!; using var oracle = JsonDocument.Parse(stream);
        var directory = Path.Combine(Path.GetTempPath(), "electron2d-ctex-render-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        var resources = new List<Resource>(); var imported = new List<CompressedTexture>(); var expected = new List<ImageTexture>(); var painters = new List<Painter>();
        try
        {
            if (parts.Length == 2)
            {
                var entry = oracle.RootElement.GetProperty("fixtures").EnumerateArray().Single(value => value.GetProperty("name").GetString() == parts[1]);
                using var source = Image.CreateFromData(7, 5, true, Enum.Parse<Image.Format>(entry.GetProperty("source_format").GetString()!), entry.GetProperty("encoded").GetBytesFromBase64());
                var path = Path.Combine(directory, "precision.ctex"); File.WriteAllBytes(path, Container(source));
                using var texture = new CompressedTexture(); texture.Load(path);
                var host = new Window(); host.AddChild(new Sprite { Texture = texture });
                Reject<NotSupportedException>(() => Engine.Run(host)); Check(host.IsDisposed && RenderingServer.Service is null, "Unavailable precision releases its host.");
                Console.WriteLine("Compressed texture " + profile + ": explicit precision rejection and cleanup passed."); return;
            }
            using var ordinary = CompressedArrayTests.Source(0);
            for (uint mode = 0; mode < 3; mode++) Add(ordinary, ordinary, mode);
            using (var bc = (Image)ordinary.Duplicate()) { bc.Compress(Image.CompressMode.S3tc); using var decoded = (Image)bc.Duplicate(); decoded.Decompress(); Add(bc, decoded); }
            foreach (var entry in oracle.RootElement.GetProperty("fixtures").EnumerateArray())
            {
                if (backend == "compatibility" && entry.GetProperty("format").GetString() != "Rgba8") continue;
                using var source = Image.CreateFromData(7, 5, true, Enum.Parse<Image.Format>(entry.GetProperty("source_format").GetString()!), entry.GetProperty("encoded").GetBytesFromBase64());
                using var decoded = Image.CreateFromData(7, 5, true, Enum.Parse<Image.Format>(entry.GetProperty("format").GetString()!), entry.GetProperty("pixels").GetBytesFromBase64());
                Add(source, decoded);
            }
            var root = new Window { Size = new(256, imported.Count * 32 + (backend == "gpu" ? 32 : 0)) };
            for (var row = 0; row < imported.Count; row++)
                for (var side = 0; side < 2; side++)
                {
                    Texture texture = side == 0 ? imported[row] : expected[row]; var x = side * 128;
                    root.AddChild(new Sprite { Texture = texture, Centered = false, Position = new(x + 4, row * 32 + 4), TextureFilter = TextureFilter.Nearest });
                    root.AddChild(new TextureRect { Texture = texture, Position = new(x + 24, row * 32 + 4), Size = new(28, 20), TextureFilter = TextureFilter.Nearest });
                    var painter = new Painter(texture) { Position = new(x, row * 32), TextureFilter = TextureFilter.Nearest }; painters.Add(painter); root.AddChild(painter);
                }
            if (backend == "gpu")
            {
                var shader = TextureArrayTests.Load("ArrayOrdinary"); resources.Add(shader);
                using var white = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); white.Fill(Colors.White);
                var detail = ImageTexture.CreateFromImage(white); resources.Add(detail); shader.SetDefaultTextureParameter("detailMap", detail);
                shader.SetDefaultTextureParameter("layers", imported[0]);
                for (var side = 0; side < 2; side++)
                {
                    var material = new ShaderMaterial { Shader = shader }; resources.Add(material);
                    if (side != 0) material.SetShaderParameter("layers", expected[0]);
                    root.AddChild(new ShaderPatch { Position = new(side * 128 + 4, imported.Count * 32 + 4), Material = material });
                }
            }
            using var target = root.GetTexture(); var frames = 0; long before = 0, allocated = 0;
            root.Ready += _ =>
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == backend && (backend != "gpu" || RenderingServer.GetCurrentRenderingDriverName() != "software"), "Actual requested backend selected.");
                RenderingServer.SetDefaultClearColor(Colors.Black);
                var texture = imported[0]; var identity = texture.GetRID();
                Check(RenderingServer.TextureGetPath(identity) == texture.LoadPath, "Direct load supplies renderer label.");
                texture.SetPathCache("res://imported"); Check(RenderingServer.TextureGetPath(identity) == "res://imported", "ResourcePath supersedes load label.");
                texture.SetPathCache(""); Check(RenderingServer.TextureGetPath(identity) == "", "Clearing ResourcePath clears renderer label.");
                if (backend == "gpu")
                {
                    var device = RenderingServer.GetRenderingDevice()!;
                    foreach (var value in imported)
                    {
                        var storage = RenderingServer.TextureGetRDTexture(value.GetRID()); using var format = device.TextureGetFormat(storage);
                        var data = device.TextureGetData(storage, 0); var snapshot = value.CapturePixels()!;
                        Check(format.Width == 7 && format.Height == 5 && format.MIPMaps == 3 && (data.AsSpan().SequenceEqual(snapshot.Upload.Data) || data.AsSpan().SequenceEqual(snapshot.Source.Data)), $"Physical GPU storage retains {value.PixelFormat} and every mip: {format.Width}x{format.Height}, {format.MIPMaps} levels, {data.Length} bytes.");
                    }
                }
                RenderingServer.FramePreDraw += () => { before = GC.GetAllocatedBytesForCurrentThread(); foreach (var painter in painters) painter.QueueRedraw(); };
                RenderingServer.FramePostDraw += () =>
                {
                    if (frames >= 24) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                    ++frames;
                    if (frames == 1)
                    {
                        Compare();
                        var old = backend == "gpu" ? RenderingServer.TextureGetRDTexture(identity) : default;
                        using var replacement = CompressedArrayTests.Source(1); File.WriteAllBytes(texture.LoadPath, Container(replacement, 2, width: 20)); texture.Load(texture.LoadPath);
                        expected[0].SetImage(replacement); expected[0].SetSizeOverride(new(20, 10));
                        if (backend == "gpu")
                        {
                            var current = RenderingServer.TextureGetRDTexture(identity);
                            Check(texture.GetRID() == identity && old != current && !RenderingServer.GetRenderingDevice()!.TextureIsValid(old), "Reload retains resource RID and retires borrowed device storage.");
                        }
                    }
                    if (frames == 2) { Compare(); File.WriteAllBytes(texture.LoadPath, [1, 2, 3]); Reject<InvalidDataException>(() => texture.Load(texture.LoadPath)); }
                    if (frames == 3) Compare();
                    if (frames == 16) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
                    if (frames != 88) return;
                    Compare(); Check(allocated == 0, $"Prepared imported texture frames allocated {allocated} owner-thread bytes."); root.Tree!.Quit();
                };
            };
            Check(Engine.Run(root) == 0 && frames == 88 && root.IsDisposed && RenderingServer.Service is null, "Imported texture host releases resources.");
            Console.WriteLine($"Compressed textures {backend}: {imported.Count} source/oracle pairs in Sprite, TextureRect, direct/transposed/region draws, reload and failed reload; GPU also verifies default/override named samplers, full physical mip uploads and RD expiration; 24 warmup + 64 active frames, {allocated} owner-thread bytes.");

            void Add(Image source, Image decoded, uint mode = 0)
            {
                var path = Path.Combine(directory, imported.Count + ".ctex"); File.WriteAllBytes(path, Container(source, mode));
                var value = new CompressedTexture(); resources.Add(value); value.Load(path); imported.Add(value);
                var plain = ImageTexture.CreateFromImage(decoded); resources.Add(plain); plain.SetSizeOverride(new(14, 10)); expected.Add(plain);
            }
            void Compare()
            {
                using var pixels = target.GetImage()!; var visible = 0;
                for (var y = 0; y < root.Size.Y; y++) for (var x = 0; x < 128; x++)
                    {
                        var left = pixels.GetPixel(x, y); var right = pixels.GetPixel(x + 128, y);
                        Check(MathF.Abs(left.R - right.R) < .015f && MathF.Abs(left.G - right.G) < .015f && MathF.Abs(left.B - right.B) < .015f, $"Imported/reference rendering differs at {x},{y}: {left} vs {right}.");
                        if (left.R > .05f || left.G > .05f || left.B > .05f) visible++;
                    }
                Check(visible > 1000, "Imported textures produce visible content.");
                if (frames == 88 && Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } capture) { Directory.CreateDirectory(capture); pixels.SavePNG(Path.Combine(capture, "compressed-texture-" + backend + ".png")); }
            }
        }
        finally { foreach (var value in resources.AsEnumerable().Reverse()) value.Dispose(); Directory.Delete(directory, true); }
    }
}
