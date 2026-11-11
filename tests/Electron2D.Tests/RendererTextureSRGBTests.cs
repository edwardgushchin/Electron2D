using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static class RendererTextureSRGBTests
{
    private sealed class Patch(Texture? texture = null) : Entity
    {
        protected override void OnDraw()
        {
            if (texture is not null) DrawTextureRect(texture, new(0, 0, 12, 12), false);
            else DrawRect(new(0, 0, 12, 12), Colors.White);
        }
    }

    internal static void Run(string profile)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var authored = Pixels(64, 128, 192);
        using var layer = Pixels(192, 128, 64);
        using var gpuLayer = Pixels(64, 192, 128);
        using var source = ImageTexture.CreateFromImage(authored);
        using var array = new TextureArray();
        array.CreateFromImages([authored, layer]);
        using var color = new TextureRD();
        using var arrayColor = new TextureArrayRD();
        byte[] block = [0x10, 0x84, 0, 0, 0, 0, 0, 0];
        using var compressedImage = Image.CreateFromData(4, 4, false, Image.Format.Dxt1, block);
        using var compressed = ImageTexture.CreateFromImage(compressedImage);
        using var compressedColor = new TextureRD();
        using var arrayShader = Load("ArrayGLSL");
        using var arrayMaterial = new ShaderMaterial { Shader = arrayShader };
        using var white = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        white.Fill(Colors.White);
        using var detail = ImageTexture.CreateFromImage(white);
        arrayShader.SetDefaultTextureParameter("detailMap", detail);
        var root = new Window { Size = new(64, 20) };
        root.AddChild(new Patch(source) { Position = new(2, 2) });
        var frame = 0;
        var finished = false;
        root.Ready += _ =>
        {
            var device = RenderingServer.GetRenderingDevice()!;
            Check(RenderingServer.GetCurrentRenderingDriverName() == (profile == "preceding" ? "vulkan" : profile), "The requested GPU driver is active.");
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RID ordinary = default, arrayLinear = default, ordinarySRGB = default, layeredSRGB = default;
            RenderingServer.FramePostDraw += AfterFrame;

            void AfterFrame()
            {
                using var image = root.GetTexture().GetImage()!;
                if (frame++ == 0)
                {
                    // Exercise preparation of a texture the canvas has already drawn.
                    Pixel(image, 8, 64, 128, 192);
                    ordinary = RenderingServer.TextureGetRDTexture(source.GetRID());
                    if (profile != "vulkan")
                    {
                        Reject<NotSupportedException>(() => RenderingServer.TextureGetRDTexture(source.GetRID(), true));
                        Reject<NotSupportedException>(() => RenderingServer.TextureGetRDTexture(array.GetRID(), true));
                        Check(device.TextureGetData(ordinary, 0).AsSpan().SequenceEqual(authored.GetData()), "An unsupported view leaves the linear upload readable.");
                        Finish();
                        return;
                    }

                    Check((device.TextureGetFormat(ordinary).UsageBits & TextureUsageBits.Storage) != 0, "The linear export keeps its storage role.");
                    WriteStorage(device, ordinary);
                    ordinarySRGB = RenderingServer.TextureGetRDTexture(source.GetRID(), true);
                    Check(device.TextureIsShared(ordinarySRGB) && !device.TextureIsShared(ordinary) &&
                        (device.TextureGetFormat(ordinarySRGB).UsageBits & TextureUsageBits.Storage) == 0,
                        "The sRGB view shares bytes and exposes only its own native roles.");
                    var native = device.TextureGetData(ordinarySRGB, 0);
                    Check(native.AsSpan(0, 4).SequenceEqual(new byte[] { 128, 64, 192, 255 }) &&
                        native.AsSpan(64).SequenceEqual(authored.GetData().AsSpan(64)), "A pre-export GPU storage write and unmodified mips are visible through the sRGB view.");
                    using (var snapshot = source.GetImage()!)
                        Check(snapshot.GetData().AsSpan().SequenceEqual(authored.GetData()), "Storage writes leave the authored image unchanged.");

                    layeredSRGB = RenderingServer.TextureGetRDTexture(array.GetRID(), true);
                    arrayLinear = RenderingServer.TextureGetRDTexture(array.GetRID());
                    device.TextureUpdate(layeredSRGB, 1, gpuLayer.GetData());
                    Check(device.TextureGetData(arrayLinear, 1).AsSpan().SequenceEqual(gpuLayer.GetData()) &&
                        device.TextureGetData(layeredSRGB, 0).AsSpan().SequenceEqual(authored.GetData()), "The array view shares every mip and isolates the selected layer.");
                    color.TextureRDRID = ordinarySRGB;
                    arrayColor.TextureRDRID = layeredSRGB;
                    arrayMaterial.SetShaderLayeredParameter("layers", arrayColor);
                    arrayMaterial.SetShaderParameter("selectedLayer", 1f);
                    root.AddChild(new Patch(color) { Position = new(18, 2) });
                    root.AddChild(new Patch { Position = new(34, 2), Material = arrayMaterial });
                    if (device.TextureIsFormatSupportedForUsage(DataFormat.BC1RGBASRGBBLOCK, TextureUsageBits.Sampling))
                    {
                        var compressedView = RenderingServer.TextureGetRDTexture(compressed.GetRID(), true);
                        using var encoded = device.TextureGetFormat(compressedView);
                        Check(encoded.Format == DataFormat.BC1RGBAUNORMBLOCK && device.TextureGetData(compressedView, 0).AsSpan().SequenceEqual(block),
                            "BC1 upload retains the encoded block through its sRGB view.");
                        compressedColor.TextureRDRID = compressedView;
                        root.AddChild(new Patch(compressedColor) { Position = new(50, 2) });
                    }
                    return;
                }

                if (frame == 2)
                {
                    Pixel(image, 8, 128, 64, 192);
                    Pixel(image, 24, 55, 13, 134);
                    Pixel(image, 40, 13, 134, 55);
                    if (compressedColor.TextureRDRID.IsValid())
                    {
                        Pixel(image, 56, 59, 57, 59);
                        Console.WriteLine("Renderer BC1 sRGB pixels and encoded bytes verified.");
                    }
                    source.Update(layer);
                    array.UpdateLayer(authored, 1);
                    Check(ordinary == RenderingServer.TextureGetRDTexture(source.GetRID()) &&
                        ordinarySRGB == RenderingServer.TextureGetRDTexture(source.GetRID(), true) &&
                        layeredSRGB == RenderingServer.TextureGetRDTexture(array.GetRID(), true), "Compatible authored updates preserve both exported identities.");
                    return;
                }

                Pixel(image, 8, 192, 128, 64);
                Pixel(image, 24, 134, 55, 13);
                Pixel(image, 40, 13, 55, 134);
                var queued = new byte[layer.DataSize];
                var completed = false;
                device.TextureGetDataAsync(ordinarySRGB, 0, queued.AsMemory(), () => completed = true);
                source.Dispose();
                Check(!device.TextureIsValid(ordinary) && !device.TextureIsShared(ordinarySRGB), "Source disposal immediately invalidates both views.");
                device.FreeRID(ordinary);
                Check(completed && queued.AsSpan().SequenceEqual(layer.GetData()), "A previously recorded view read survives source disposal.");
                device.FreeRID(layeredSRGB);
                var recreated = RenderingServer.TextureGetRDTexture(array.GetRID(), true);
                Check(device.TextureIsValid(arrayLinear) && recreated != layeredSRGB && device.TextureIsShared(recreated),
                    "Freeing an sRGB view preserves the renderer allocation and allows recreation.");
                using var smaller = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
                array.CreateFromImages([smaller, smaller]);
                Check(!device.TextureIsValid(arrayLinear) && !device.TextureIsShared(recreated), "Array allocation replacement expires both previous exports.");
                device.FreeRID(arrayLinear);
                var resized = RenderingServer.TextureGetRDTexture(array.GetRID(), true);
                using var resizedFormat = device.TextureGetFormat(resized);
                Check(resized != recreated && device.TextureIsShared(resized) && resizedFormat.Width == 2, "The replacement gets its own sRGB view.");
                Finish();
            }

            void Finish()
            {
                RenderingServer.FramePostDraw -= AfterFrame;
                finished = true;
                root.Tree!.Quit();
            }
        };
        Check(Engine.Run(root) == 0 && finished, "The renderer sRGB fixture completed.");
        Console.WriteLine($"Renderer upload sRGB views verified ({profile}).");
    }

    private static void WriteStorage(RenderingDevice device, RID target)
    {
        using var module = new RDShaderSPIRV();
        using var stream = typeof(RendererTextureSRGBTests).Assembly.GetManifestResourceStream("Electron2D.Compute.LocalRendererSRGB.comp.spv")!;
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        module.BytecodeCompute = bytes.ToArray();
        var shader = device.ShaderCreateFromSPIRV(module);
        var pipeline = device.ComputePipelineCreate(shader);
        using var image = new RDUniform { Binding = 0, UniformType = UniformType.Image };
        image.AddID(target);
        var set = device.UniformSetCreate([image], shader, 0);
        var list = device.ComputeListBegin();
        device.ComputeListBindComputePipeline(list, pipeline);
        device.ComputeListBindUniformSet(list, set, 0);
        device.ComputeListDispatch(list, 1, 1, 1);
        device.ComputeListEnd();
        device.FreeRID(set);
        device.FreeRID(pipeline);
        device.FreeRID(shader);
    }

    private static Image Pixels(byte red, byte green, byte blue)
    {
        var bytes = new byte[(16 + 4 + 1) * 4];
        for (var i = 0; i < bytes.Length; i += 4)
        {
            bytes[i] = red;
            bytes[i + 1] = green;
            bytes[i + 2] = blue;
            bytes[i + 3] = 255;
        }
        return Image.CreateFromData(4, 4, true, Image.Format.Rgba8, bytes);
    }

    private static void Pixel(Image image, int x, int red, int green, int blue)
    {
        var color = image.GetPixel(x, 8);
        Check(Math.Abs(color.R * 255 - red) < 2 && Math.Abs(color.G * 255 - green) < 2 &&
            Math.Abs(color.B * 255 - blue) < 2, $"Native sample at {x} has RGB {color}. Expected ({red}, {green}, {blue}).");
    }
}
