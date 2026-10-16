using System.Buffers.Binary;
using Electron2D;
using Path = System.IO.Path;
using static TextureArrayTests;

internal static partial class IntegerSourceTextureTests
{
    private sealed class Producer(Image source) : Texture
    {
        private readonly Image _image = (Image)source.Duplicate();
        internal int Reads;
        public override int GetWidth() => 7;
        public override int GetHeight() => 5;
        public override Image GetImage() { Reads++; return (Image)_image.Duplicate(); }
        protected override void Dispose(bool disposing) { if (disposing) _image.Dispose(); base.Dispose(disposing); }
    }
    private sealed class LayerProducer(Image first, Image second) : TextureLayered
    {
        private readonly Image[] _images = [(Image)first.Duplicate(), (Image)second.Duplicate()];
        internal int Reads;
        public override LayeredType GetLayeredType() => LayeredType.Array;
        public override Image.Format GetFormat() => _images[0].PixelFormat;
        public override int GetWidth() => 7;
        public override int GetHeight() => 5;
        public override int GetLayers() => 2;
        public override bool HasMIPMaps() => true;
        public override Image GetLayerData(int layerIndex) { Reads++; return (Image)_images[layerIndex].Duplicate(); }
        protected override void Dispose(bool disposing) { if (disposing) foreach (var image in _images) image.Dispose(); base.Dispose(disposing); }
    }
    private readonly record struct DrawEntry(Texture? Texture, RID RID);
    private sealed class SourcePainter(DrawEntry[] entries, Texture expected) : Entity
    {
        protected override void OnDraw()
        {
            for (var i = 0; i < entries.Length; ++i)
            {
                var rectangle = new Rect2(i * 56 + 4, 4, 20, 20);
                if (entries[i].RID.IsValid()) RenderingServer.CanvasItemAddTextureRect(GetCanvasItem(), rectangle, entries[i].RID);
                else DrawTextureRect(entries[i].Texture!, rectangle, false);
                DrawTextureRect(expected, new(i * 56 + 32, 4, 20, 20), false);
            }
        }
    }
    private sealed class MaterialPatch : Entity
    {
        protected override void OnDraw() => DrawRect(new(0, 0, 20, 20), Colors.White);
    }
    private sealed record Group(Image.Format Format, ImageTexture Primary, ImageTexture Expected, TextureArray Array, CompressedTexture Imported, CompressedTextureArray ImportedArray, Producer Producer, LayerProducer LayerProducer, ShaderMaterial GLSL, ShaderMaterial HLSL)
    {
        internal RID Storage, Server, ServerArray, ServerMaterial;
        internal int Seed;
    }

    private static Image FloatOracle(Image image)
    {
        var source = image.GetData(); var channels = (int)image.PixelFormat - (int)Image.Format.R16I + 1; var bytes = new byte[source.Length / channels / 2 * 16];
        for (var pixel = 0; pixel < bytes.Length / 16; ++pixel)
            for (var channel = 0; channel < 4; ++channel)
            {
                var value = channel < channels ? BinaryPrimitives.ReadUInt16LittleEndian(source.AsSpan((pixel * channels + channel) * 2)) : channel == 3 ? 1 : 0;
                BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(pixel * 16 + channel * 4), value);
            }
        return Image.CreateFromData(7, 5, true, Image.Format.Rgbaf, bytes);
    }

    internal static void RunNative(string profile)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, profile == "compatibility" ? "compatibility" : "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (profile != "gpu") { RejectedNative(profile); return; }
        var directory = Path.Combine(Path.GetTempPath(), "e2d-integer-render-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        var resources = new List<Resource>(); var groups = new List<Group>(); var painters = new List<SourcePainter>();
        try
        {
            var root = new Window { Size = new(384, 256), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
            foreach (var format in Formats)
            {
                using var first = Source(format); using var second = Source(format, 1); using var oracle = FloatOracle(first);
                var primary = Own(ImageTexture.CreateFromImage(first)); primary.SetSizeOverride(new(14, 10));
                var expected = Own(ImageTexture.CreateFromImage(oracle)); expected.SetSizeOverride(new(14, 10));
                var array = Own(new TextureArray()); array.CreateFromImages([first, second]);
                var imagePath = Path.Combine(directory, format + ".ctex"); File.WriteAllBytes(imagePath, CompressedTextureTests.Container(first));
                var arrayPath = Path.Combine(directory, format + ".ctexarray"); File.WriteAllBytes(arrayPath, CompressedArrayTests.Container([(first, 0, null), (second, 0, null)]));
                var imported = Own(ResourceLoader.Load<CompressedTexture>(imagePath, ResourceLoader.CacheMode.Ignore));
                var importedArray = Own(ResourceLoader.Load<CompressedTextureArray>(arrayPath, ResourceLoader.CacheMode.Ignore));
                var producer = Own(new Producer(first)); var layeredProducer = Own(new LayerProducer(first, second));
                var glsl = Own(Load("IntegerSourcesGLSL")); var hlsl = Own(Load("IntegerSourcesHLSL"));
                glsl.SetDefaultTextureParameter("values", primary); glsl.SetDefaultLayeredTextureParameter("layers", importedArray);
                hlsl.SetDefaultTextureParameter("values", producer); hlsl.SetDefaultLayeredTextureParameter("layers", layeredProducer);
                var left = Own(new ShaderMaterial { Shader = glsl }); var right = Own(new ShaderMaterial { Shader = hlsl });
                left.SetShaderParameter("exposure", 1f / 131070); right.SetShaderParameter("exposure", 1f / 131070);
                groups.Add(new(format, primary, expected, array, imported, importedArray, producer, layeredProducer, left, right));
                root.AddChild(new MaterialPatch { Position = new(4, (groups.Count - 1) * 64 + 36), Material = left });
                root.AddChild(new MaterialPatch { Position = new(60, (groups.Count - 1) * 64 + 36), Material = right });
            }
            using var target = root.GetTexture(); var frames = 0; long before = 0, allocated = 0;
            root.Ready += _ =>
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == "gpu" && RenderingServer.GetCurrentRenderingDriverName() != "software", "Actual GPU backend selected.");
                RenderingServer.SetDefaultClearColor(Colors.Black); var device = RenderingServer.GetRenderingDevice()!;
                for (var row = 0; row < groups.Count; ++row)
                {
                    var group = groups[row]; using var first = Source(group.Format); using var second = Source(group.Format, 1);
                    group.Server = RenderingServer.Texture2DCreate(first); RenderingServer.TextureSetSizeOverride(group.Server, 14, 10);
                    group.ServerArray = RenderingServer.Texture2DLayeredCreate([first, second], TextureLayered.LayeredType.Array);
                    group.ServerMaterial = RenderingServer.MaterialCreate();
                    RenderingServer.MaterialSetShader(group.ServerMaterial, group.GLSL.Shader!.GetRID());
                    RenderingServer.MaterialSetParam(group.ServerMaterial, "values", group.Server);
                    RenderingServer.MaterialSetParam(group.ServerMaterial, "layers", group.ServerArray);
                    RenderingServer.MaterialSetParam(group.ServerMaterial, "exposure", 1f / 131070);
                    var serverPatch = new MaterialPatch { Position = new(116, row * 64 + 36) }; root.AddChild(serverPatch);
                    RenderingServer.CanvasItemSetMaterial(serverPatch.GetCanvasItem(), group.ServerMaterial);
                    var proxy = RenderingServer.TextureProxyCreate(group.Server);
                    var atlas = Own(new AtlasTexture { Atlas = group.Primary, Region = new(0, 0, 14, 10) });
                    var painter = new SourcePainter([new(group.Primary, default), new(group.Imported, default), new(null, group.Server), new(atlas, default), new(null, proxy), new(group.Producer, default)], group.Expected) { Position = new(0, row * 64), TextureFilter = TextureFilter.Nearest };
                    painters.Add(painter); root.AddChild(painter);
                    foreach (var value in new Resource[] { group.Primary, group.Imported, group.Producer, group.Array, group.ImportedArray, group.LayerProducer })
                    {
                        var storage = RenderingServer.TextureGetRDTexture(value.GetRID()); using var native = device.TextureGetFormat(storage);
                        Check(native.Format == RenderingDevice.DataFormat.R16G16B16A16UINT && native.MIPMaps == 3 && native.Width == 7 && native.Height == 5, "Actual uploaded integer storage and full mip count.");
                        Check(device.TextureGetData(storage, 0).AsSpan().SequenceEqual(group.Primary.CapturePixels()!.Upload.Data), "Uploaded original integer bits include every mip.");
                        if (value is TextureLayered) Check(device.TextureGetData(storage, 1).AsSpan().SequenceEqual(group.Array.CaptureLayers()!.Layers[1].Upload.Data), "Actual second layer integer bits.");
                    }
                    group.Storage = RenderingServer.TextureGetRDTexture(group.Primary.GetRID());
                    RenderingServer.TextureGetRDTexture(group.Server); RenderingServer.TextureGetRDTexture(group.ServerArray);
                }
                root.Tree!.ProcessFrameStarted += _ => before = GC.GetAllocatedBytesForCurrentThread();
                RenderingServer.FramePreDraw += () =>
                {
                    foreach (var group in groups)
                    {
                        group.GLSL.SetShaderParameter("selectedLayer", (float)(frames % 2)); group.HLSL.SetShaderParameter("selectedLayer", (float)(frames % 2));
                        group.GLSL.SetShaderParameter("selectedMip", (float)(frames % 3)); group.HLSL.SetShaderParameter("selectedMip", (float)(frames % 3));
                        RenderingServer.MaterialSetParam(group.ServerMaterial, "selectedLayer", (float)(frames % 2));
                        RenderingServer.MaterialSetParam(group.ServerMaterial, "selectedMip", (float)(frames % 3));
                        switch (frames % 4)
                        {
                            case 0: group.GLSL.SetShaderLayeredParameter("layers", group.Array); break;
                            case 1: group.GLSL.SetShaderLayeredParameter("layers", group.ImportedArray); break;
                            case 2: group.GLSL.SetShaderLayeredParameter("layers", group.LayerProducer); break;
                            default: group.GLSL.SetShaderLayeredParameter("layers", group.Array); break;
                        }
                    }
                    foreach (var painter in painters) painter.QueueRedraw();
                };
                RenderingServer.FramePostDraw += () =>
                {
                    if (frames >= 24) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                    ++frames;
                    if (frames <= 3 || frames == 88) Compare();
                    if (frames == 1)
                        foreach (var group in groups)
                        {
                            using var changed = Source(group.Format, 2); var packed = TexturePixels.FromImage(changed).Upload.Data;
                            device.TextureUpdate(group.Storage, 0, packed); group.Seed = 2;
                            using var authored = group.Primary.GetImage()!; using var original = Source(group.Format);
                            Check(authored.GetData().AsSpan().SequenceEqual(original.GetData()) && device.TextureGetData(group.Storage, 0).AsSpan().SequenceEqual(packed), "Device writes preserve authored CPU source while changing actual integer storage.");
                        }
                    if (frames == 2)
                        foreach (var group in groups)
                        {
                            using var changed = Source(group.Format, 1); group.Primary.Update(changed); group.Seed = 1;
                            Check(RenderingServer.TextureGetRDTexture(group.Primary.GetRID()) == group.Storage && device.TextureIsValid(group.Storage), "Compatible updates retain borrowed native integer storage.");
                        }
                    if (frames == 3)
                        foreach (var group in groups)
                        {
                            using var changed = Source(group.Format, 3); group.Primary.SetImage(changed); group.Primary.SetSizeOverride(new(14, 10)); group.Seed = 3;
                            var replacement = RenderingServer.TextureGetRDTexture(group.Primary.GetRID());
                            Check(replacement != group.Storage && !device.TextureIsValid(group.Storage), "Whole replacement retires borrowed integer storage."); group.Storage = replacement;
                        }
                    if (frames == 16) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
                    if (frames != 88) return;
                    Check(groups.All(group => group.Producer.Reads == 1 && group.LayerProducer.Reads == 2), "Custom source snapshots stay prepared.");
                    Check(allocated == 0, $"Integer source prepared frames allocated {allocated} owner-thread bytes."); root.Tree!.Quit();
                };
            };
            Check(Engine.Run(root) == 0 && frames == 88 && root.IsDisposed && RenderingServer.Service is null, "Integer source renderer lifetime.");
            Console.WriteLine($"Integer sources GPU: four formats; ordinary/imported/server/atlas/proxy/custom output, GLSL/HLSL and four layered source families, exact mip/layer UInt16 readback, device writes and update/replacement lifetime; 24 warmup + 64 active frames, {allocated} owner-thread bytes.");

            T Own<T>(T value) where T : Resource { resources.Add(value); return value; }
            void Compare()
            {
                using var pixels = target.GetImage()!;
                for (var row = 0; row < groups.Count; ++row)
                {
                    for (var role = 0; role < 6; ++role) for (var y = 0; y < 24; ++y) for (var x = 0; x < 24; ++x)
                                Near(pixels.GetPixel(role * 56 + x, row * 64 + y), pixels.GetPixel(role * 56 + x + 28, row * 64 + y));
                    var group = groups[row]; var mip = (frames - 1) % 3; var layer = (frames - 1) % 2;
                    using var first = Source(group.Format, group.Seed); using var second = Source(group.Format, layer); using var hlslFirst = Source(group.Format);
                    using var firstLevel = MIP(first, mip); using var secondLevel = MIP(second, mip); using var hlslLevel = MIP(hlslFirst, mip);
                    var a = firstLevel.GetPixel(0, 0); var b = secondLevel.GetPixel(0, 0); var c = hlslLevel.GetPixel(0, 0);
                    Near(pixels.GetPixel(12, row * 64 + 44), new((a.R + b.R) / 131070, (a.G + b.G) / 131070, (a.B + b.B) / 131070, 1));
                    Near(pixels.GetPixel(68, row * 64 + 44), new((c.R + b.R) / 131070, (c.G + b.G) / 131070, (c.B + b.B) / 131070, 1));
                    Near(pixels.GetPixel(124, row * 64 + 44), new((c.R + b.R) / 131070, (c.G + b.G) / 131070, (c.B + b.B) / 131070, 1));
                }
                if (frames == 88 && Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } capture) { Directory.CreateDirectory(capture); pixels.SavePNG(Path.Combine(capture, "integer-sources-gpu.png")); }
            }
        }
        finally { foreach (var value in resources.AsEnumerable().Reverse()) value.Dispose(); Directory.Delete(directory, true); }
    }

    private static Image MIP(Image image, int mip)
    {
        var bytes = image.GetData(); var start = image.GetMIPMapOffset(mip); var end = mip == image.MIPMapCount ? bytes.Length : image.GetMIPMapOffset(mip + 1);
        return Image.CreateFromData(Math.Max(1, 7 >> mip), Math.Max(1, 5 >> mip), false, image.PixelFormat, bytes.AsSpan(start, end - start).ToArray());
    }
    private static void Near(Color actual, Color expected) => Check(MathF.Abs(actual.R - expected.R) < .018f && MathF.Abs(actual.G - expected.G) < .018f && MathF.Abs(actual.B - expected.B) < .018f, $"Integer source pixel {actual} differs from {expected}.");

    private static void RejectedNative(string profile)
    {
        using var source = Source(Image.Format.Rgba16I); using var texture = ImageTexture.CreateFromImage(source);
        using var shader = Load("IntegerSourcesGLSL"); using var array = new TextureArray(); array.CreateFromImages([source, source]);
        using var material = new ShaderMaterial { Shader = shader }; shader.SetDefaultTextureParameter("values", texture); shader.SetDefaultLayeredTextureParameter("layers", array);
        var root = new Window { Size = new(256, 128) };
        var sprite = new Sprite { Texture = texture, Centered = false, TextureFilter = profile == "linear" ? TextureFilter.Linear : TextureFilter.Nearest };
        root.AddChild(sprite);
        if (profile == "scalar")
        {
            sprite.Material = material;
            root.Ready += _ => RenderingServer.FramePostDraw += () => { using var floating = Image.CreateEmpty(7, 5, true, Image.Format.Rgba8); texture.SetImage(floating); };
            Reject<ArgumentException>(() => Engine.Run(root));
        }
        else Reject<NotSupportedException>(() => Engine.Run(root));
        Check(root.IsDisposed && RenderingServer.Service is null, "Integer capability/scalar failure releases native host.");
        Console.WriteLine("Integer sources " + profile + ": explicit rejection before invalid sampling and cleanup passed.");
    }
}
