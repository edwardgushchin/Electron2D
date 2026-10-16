using Electron2D;
using Path = System.IO.Path;

internal static partial class ImageBlockTests
{
    private sealed class Painter(IReadOnlyList<ImageTexture> textures) : Entity
    {
        protected override void OnDraw()
        {
            for (var i = 0; i < textures.Count; ++i)
                DrawTextureRect(textures[i], new(4 + i % 2 * 64, 4 + i / 2 * 40, 56, 32), false);
        }
    }
    private sealed class ArrayPatch : Entity
    {
        protected override void OnDraw() => DrawRect(new(0, 0, 56, 32), Colors.White);
    }

    internal static void RunNative(string profile)
    {
        var parts = profile.Split(':'); var backend = parts[0];
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var oracle = Oracle();
        if (parts.Length == 2)
        {
            var fixture = oracle.RootElement.GetProperty("fixtures").EnumerateArray().Single(entry => entry.GetProperty("name").GetString() == parts[1]);
            using var image = Source(fixture); using var texture = ImageTexture.CreateFromImage(image);
            var host = new Window(); host.AddChild(new Sprite { Texture = texture });
            Reject<NotSupportedException>(() => Engine.Run(host)); Check(host.IsDisposed && RenderingServer.Service is null, "Unavailable precision releases its host.");
            Console.WriteLine($"Block textures {backend}/{parts[1]}: explicit floating-point driver rejection and cleanup passed."); return;
        }
        var textures = new List<ImageTexture>(); var resources = new List<Resource>(); var materials = new List<ShaderMaterial>();
        var directory = Path.Combine(Path.GetTempPath(), "electron2d-block-render-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var entries = oracle.RootElement.GetProperty("fixtures").EnumerateArray().Where(entry => backend != "compatibility" || entry.GetProperty("format").GetString() == "Rgba8").ToArray();
            var host = new Window { Size = new(backend == "gpu" ? 256 : 128, entries.Length * 40) };
            for (var row = 0; row < entries.Length; ++row)
            {
                using var image = Source(entries[row]); using var expected = Expected(entries[row]);
                textures.Add(ImageTexture.CreateFromImage(image)); textures.Add(ImageTexture.CreateFromImage(expected));
                if (backend != "gpu") continue;
                var path = Path.Combine(directory, row + ".ctexarray"); File.WriteAllBytes(path, CompressedArrayTests.Container([(image, 0, null), (image, 0, null)]));
                var array = new CompressedTextureArray(); resources.Add(array); array.Load(path);
                var plain = new TextureArray(); resources.Add(plain); plain.CreateFromImages([expected, expected]);
                var shader = TextureArrayTests.Load("ImportedArray"); resources.Add(shader);
                foreach (var value in new TextureLayered[] { array, plain })
                {
                    var material = new ShaderMaterial { Shader = shader }; resources.Add(material); materials.Add(material);
                    material.SetShaderLayeredParameter("layers", value); material.SetShaderParameter("exposure", .1f);
                    host.AddChild(new ArrayPatch { Position = new(132 + (value == plain ? 64 : 0), 4 + row * 40), Material = material });
                }
            }
            var painter = new Painter(textures) { TextureFilter = TextureFilter.Nearest }; host.AddChild(painter); using var target = host.GetTexture();
            var frames = 0; long before = 0, allocated = 0;
            host.Ready += _ =>
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == backend && (backend != "gpu" || RenderingServer.GetCurrentRenderingDriverName() != "software"), "Actual requested backend is exercised.");
                RenderingServer.SetDefaultClearColor(Colors.Black);
                if (backend == "gpu")
                {
                    var device = RenderingServer.GetRenderingDevice()!;
                    for (var i = 0; i < textures.Count; i += 2)
                    {
                        var prepared = textures[i].CapturePixels()!; var storage = RenderingServer.TextureGetRDTexture(textures[i].GetRID());
                        using var format = device.TextureGetFormat(storage);
                        Check(format.Format == (prepared.Upload.Format == Image.Format.Rgba8 ? RenderingDevice.DataFormat.R8G8B8A8UNORM : RenderingDevice.DataFormat.R32G32B32A32SFLOAT) && format.MIPMaps == 3, "Actual decoded GPU storage and full mip count.");
                        Check(device.TextureGetData(storage, 0).AsSpan().SequenceEqual(prepared.Upload.Data), "Ordinary upload reads back every decoded mip.");
                    }
                    foreach (var material in materials)
                    {
                        var array = material.GetShaderLayeredParameter("layers")!; var storage = RenderingServer.TextureGetRDTexture(array.GetRID());
                        Check(device.TextureGetData(storage, 1).AsSpan().SequenceEqual(array.CaptureLayers()!.First.Upload.Data), "Array upload reads back every decoded mip.");
                    }
                }
                RenderingServer.FramePreDraw += () =>
                {
                    before = GC.GetAllocatedBytesForCurrentThread();
                    foreach (var material in materials) { material.SetShaderParameter("selectedLayer", (float)(frames % 2)); material.SetShaderParameter("selectedMip", (float)(frames % 3)); }
                    painter.QueueRedraw();
                };
                RenderingServer.FramePostDraw += () =>
                {
                    if (frames >= 24) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                    if (++frames < 24 || frames > 26 && frames < 88) return;
                    using var pixels = target.GetImage()!; var visible = 0;
                    for (var pair = 0; pair < (backend == "gpu" ? 2 : 1); ++pair)
                        for (var y = 0; y < host.Size.Y; ++y) for (var x = 0; x < 64; ++x)
                            {
                                var left = pixels.GetPixel(pair * 128 + x, y); var right = pixels.GetPixel(pair * 128 + x + 64, y);
                                Check(MathF.Abs(left.R - right.R) < .012f && MathF.Abs(left.G - right.G) < .012f && MathF.Abs(left.B - right.B) < .012f, "Native block sampling matches independent image/array pixels.");
                                if (left.R > .05f || left.G > .05f || left.B > .05f) ++visible;
                            }
                    Check(visible > 1000, "Decoded blocks draw visible content.");
                    if (frames != 88) return;
                    Check(allocated == 0, $"Prepared block frames allocated {allocated} owner-thread bytes.");
                    host.Tree!.Quit();
                };
            };
            Check(Engine.Run(host) == 0 && frames == 88 && host.IsDisposed && RenderingServer.Service is null, "Block rendering host completes and releases resources.");
            Console.WriteLine($"Block textures {backend}: {entries.Length} independent source pixel pairs; GPU also samples array layers/mips and reads actual storage; 24 warmup + 64 active frames, {allocated} owner-thread bytes.");
        }
        finally { foreach (var resource in resources.AsEnumerable().Reverse()) resource.Dispose(); foreach (var texture in textures) texture.Dispose(); Directory.Delete(directory, true); }
    }
}
