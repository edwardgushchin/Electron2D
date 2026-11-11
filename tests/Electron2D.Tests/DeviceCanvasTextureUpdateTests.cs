using Electron2D;
using static TextureArrayTests;

internal static class DeviceCanvasTextureUpdateTests
{
    private const RenderingDevice.TextureUsageBits UploadRoles = RenderingDevice.TextureUsageBits.Sampling |
        RenderingDevice.TextureUsageBits.CanUpdate | RenderingDevice.TextureUsageBits.CanCopyFrom;

    private sealed class Patch(RID texture = default) : Entity
    {
        internal int Draws { get; private set; }

        protected override void OnDraw()
        {
            Draws++;
            if (texture.IsValid()) RenderingServer.CanvasItemAddTextureRect(GetCanvasItem(), new(0, 0, 16, 16), texture);
            else DrawRect(new(0, 0, 16, 16), Colors.White);
        }
    }

    internal static void Run(string profile)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var white = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        white.Fill(Colors.White);
        using var detail = ImageTexture.CreateFromImage(white);
        using var shader = Load("ArrayGLSL");
        var root = new Window { Size = new(48, 28) };
        var phase = 0;
        root.Ready += _ =>
        {
            var device = RenderingServer.GetRenderingDevice()!;
            Check(RenderingServer.GetCurrentRenderingDriverName() == profile, "The requested native GPU driver is active.");
            RenderingServer.SetDefaultClearColor(Colors.Black);
            VerifyEncodings(device);
            if (profile == "vulkan") VerifyBoundedView(device);

            using var format = new RDTextureFormat
            {
                Width = 4,
                Height = 4,
                MIPMaps = 3,
                Format = RenderingDevice.DataFormat.R8G8B8A8UNORM,
                UsageBits = UploadRoles,
            };
            using var view = new RDTextureView();
            var ordinary = device.TextureCreate(format, view);
            format.TextureType = RenderingDevice.TextureType.TextureArray;
            format.ArrayLayers = 2;
            var array = device.TextureCreate(format, view);
            var ordinaryCanvas = RenderingServer.TextureRDCreate(ordinary);
            var arrayCanvas = RenderingServer.TextureRDCreate(array);
            var alias = RenderingServer.TextureProxyCreate(ordinaryCanvas);
            var left = new Patch(alias) { Position = new(3, 3) };
            var right = new Patch { Position = new(26, 3) };
            root.AddChild(left);
            root.AddChild(right);
            var ownedShader = RenderingServer.ShaderCreate();
            RenderingServer.ShaderSetSPIRV(ownedShader, shader.GetSPIRV());
            RenderingServer.ShaderSetDefaultTextureParameter(ownedShader, "detailMap", detail.GetRID());
            var material = RenderingServer.MaterialCreate();
            RenderingServer.MaterialSetShader(material, ownedShader);
            RenderingServer.MaterialSetParam(material, "layers", arrayCanvas);
            RenderingServer.MaterialSetParam(material, "selectedLayer", 1f);
            RenderingServer.CanvasItemSetMaterial(right.GetCanvasItem(), material);

            using (var initial = ColorChain(Colors.Blue, Colors.Green))
            {
                RenderingServer.Texture2DUpdate(ordinaryCanvas, initial);
                RenderingServer.Texture2DUpdate(arrayCanvas, initial, 1);
            }

            byte[] updated = [];
            RenderingServer.FramePostDraw += AfterFrame;
            void AfterFrame()
            {
                using var frame = root.GetTexture().GetImage()!;
                if (phase == 0)
                {
                    CheckPixel(frame, 8, Colors.Blue);
                    CheckPixel(frame, 31, Colors.Blue);

                    using var next = ColorChain(Colors.Red, Colors.Yellow);
                    updated = next.GetData();
                    Reject<ArgumentOutOfRangeException>(() => RenderingServer.Texture2DUpdate(ordinaryCanvas, next, 1));
                    Reject<ArgumentOutOfRangeException>(() => RenderingServer.Texture2DUpdate(arrayCanvas, next, -1));
                    Reject<ArgumentOutOfRangeException>(() => RenderingServer.Texture2DUpdate(arrayCanvas, next, 2));
                    Reject<InvalidOperationException>(() => RenderingServer.Texture2DUpdate(alias, next));
                    using var wrongSize = Image.CreateEmpty(2, 4, true, Image.Format.Rgba8);
                    using var wrongFormat = Image.CreateEmpty(4, 4, true, Image.Format.Rgb8);
                    using var noMIPs = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
                    Reject<ArgumentException>(() => RenderingServer.Texture2DUpdate(ordinaryCanvas, wrongSize));
                    Reject<ArgumentException>(() => RenderingServer.Texture2DUpdate(ordinaryCanvas, wrongFormat));
                    Reject<ArgumentException>(() => RenderingServer.Texture2DUpdate(ordinaryCanvas, noMIPs));
                    Reject<ArgumentNullException>(() => RenderingServer.Texture2DUpdate(ordinaryCanvas, null!));
                    device.ComputeListBegin();
                    try { Reject<InvalidOperationException>(() => RenderingServer.Texture2DUpdate(ordinaryCanvas, next)); }
                    finally { device.ComputeListEnd(); }
                    Check(device.TextureGetData(ordinary, 0).SequenceEqual(device.TextureGetData(array, 1)), "Rejected updates leave previous mip data intact.");
                    RenderingServer.Texture2DUpdate(ordinaryCanvas, next);
                    RenderingServer.Texture2DUpdate(arrayCanvas, next, 1);
                    next.Fill(Colors.White);
                    Check(RenderingServer.TextureGetRDTexture(ordinaryCanvas) == ordinary && RenderingServer.TextureGetRDTexture(arrayCanvas) == array,
                        "Canvas and device identities survive uploads.");
                    phase++;
                    return;
                }

                CheckPixel(frame, 8, Colors.Red);
                CheckPixel(frame, 31, Colors.Red);
                Check(device.TextureGetData(ordinary, 0).SequenceEqual(updated), "Ordinary mip bytes are copied before the source changes.");
                Check(device.TextureGetData(array, 1).SequenceEqual(updated) && device.TextureGetData(array, 0).All(value => value == 0),
                    "Only the selected array layer receives the copied mip chain.");
                Check(left.Draws == 1 && right.Draws == 1, "Retained draw and material bindings survive native uploads.");
                device.FreeRID(ordinary);
                using var stale = ColorChain(Colors.White, Colors.White);
                Reject<ArgumentException>(() => RenderingServer.Texture2DUpdate(ordinaryCanvas, stale));
                RenderingServer.FramePostDraw -= AfterFrame;
                phase++;
                root.Tree!.Quit();
            }
        };

        Check(Engine.Run(root) == 0 && phase == 2, "The device upload fixture completed.");
        Console.WriteLine($"Device canvas uploads preserved retained pixels, mip chains, layers and raw encodings ({profile}).");
    }

    private static void VerifyEncodings(RenderingDevice device)
    {
        using var view = new RDTextureView();
        using var format = new RDTextureFormat { Width = 4, Height = 4, UsageBits = UploadRoles };
        Verify(RenderingDevice.DataFormat.R16G16B16A16UINT, Image.Format.Rgba16I);
        Verify(RenderingDevice.DataFormat.BC1RGBAUNORMBLOCK, Image.Format.Dxt1);
        Verify(RenderingDevice.DataFormat.R8G8B8A8SRGB, Image.Format.Rgba8);
        Verify(RenderingDevice.DataFormat.B8G8R8A8UNORM, Image.Format.Rgba8, bytes =>
        {
            for (var i = 0; i < bytes.Length; i += 4)
                (bytes[i], bytes[i + 2]) = (bytes[i + 2], bytes[i]);
        });
        Verify(RenderingDevice.DataFormat.B5G6R5UNORMPACK16, Image.Format.Rgb565);
        Verify(RenderingDevice.DataFormat.B4G4R4A4UNORMPACK16, Image.Format.Rgba4444, bytes =>
        {
            for (var i = 0; i < bytes.Length; i += 2)
            {
                var lo = bytes[i];
                var hi = bytes[i + 1];
                bytes[i] = (byte)((hi << 4) | (lo >> 4));
                bytes[i + 1] = (byte)((lo << 4) | (hi >> 4));
            }
        });
        format.Format = RenderingDevice.DataFormat.B10G11R11UFLOATPACK32;
        var rawOnly = device.TextureCreate(format, view);
        var rawCanvas = RenderingServer.TextureRDCreate(rawOnly);
        using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
        Reject<NotSupportedException>(() => RenderingServer.Texture2DUpdate(rawCanvas, image));
        RenderingServer.FreeRID(rawCanvas);
        device.FreeRID(rawOnly);
        format.Format = RenderingDevice.DataFormat.R8G8B8A8UNORM;
        format.UsageBits &= ~RenderingDevice.TextureUsageBits.CanUpdate;
        var unwriteable = device.TextureCreate(format, view);
        var canvas = RenderingServer.TextureRDCreate(unwriteable);
        Reject<InvalidOperationException>(() => RenderingServer.Texture2DUpdate(canvas, image));
        RenderingServer.FreeRID(canvas);
        device.FreeRID(unwriteable);

        void Verify(RenderingDevice.DataFormat nativeFormat, Image.Format imageFormat, Action<byte[]>? encode = null)
        {
            if (!device.TextureIsFormatSupportedForUsage(nativeFormat, UploadRoles))
            {
                Console.WriteLine($"The selected GPU does not support {nativeFormat} uploads.");
                return;
            }
            format.Format = nativeFormat;
            var rd = device.TextureCreate(format, view);
            var rid = RenderingServer.TextureRDCreate(rd);
            var bytes = Enumerable.Range(0, Image.GetRequiredDataSize(4, 4, imageFormat, false)).Select(i => (byte)(i * 7 + 11)).ToArray();
            using var input = Image.CreateFromData(4, 4, false, imageFormat, bytes);
            RenderingServer.Texture2DUpdate(rid, input);
            encode?.Invoke(bytes);
            Check(device.TextureGetData(rd, 0).SequenceEqual(bytes), $"Encoded upload preserves {nativeFormat} data.");
            RenderingServer.FreeRID(rid);
            device.FreeRID(rd);
        }
    }

    private static void VerifyBoundedView(RenderingDevice device)
    {
        using var format = new RDTextureFormat
        {
            Width = 8,
            Height = 8,
            ArrayLayers = 2,
            MIPMaps = 4,
            TextureType = RenderingDevice.TextureType.TextureArray,
            Format = RenderingDevice.DataFormat.R8G8B8A8UNORM,
            UsageBits = UploadRoles,
        };
        using var view = new RDTextureView();
        var image = device.TextureCreate(format, view);
        var slice = device.TextureCreateSharedFromSlice(view, image, 1, 1, 2);
        var canvas = RenderingServer.TextureRDCreate(slice);
        using var input = ColorChain(Colors.Cyan, Colors.Magenta);
        RenderingServer.Texture2DUpdate(canvas, input);
        var data = device.TextureGetData(image, 1);
        const int baseBytes = 8 * 8 * 4, selectedBytes = 4 * 4 * 4 + 2 * 2 * 4;
        Check(data.AsSpan(baseBytes, selectedBytes).SequenceEqual(input.GetData().AsSpan(0, selectedBytes)),
            "A bounded view consumes the matching prefix of a complete image chain.");
        Check(data.AsSpan(0, baseBytes).IndexOfAnyExcept((byte)0) == -1 &&
              data.AsSpan(baseBytes + selectedBytes).IndexOfAnyExcept((byte)0) == -1 &&
              device.TextureGetData(image, 0).All(value => value == 0), "Unselected mips and layers stay unchanged.");
        RenderingServer.FreeRID(canvas);
        device.FreeRID(image);
    }

    private static Image ColorChain(Color baseColor, Color mipColor)
    {
        var bytes = new byte[(16 + 4 + 1) * 4];
        for (var i = 0; i < bytes.Length; i += 4)
        {
            var color = i < 64 ? baseColor : mipColor;
            bytes[i] = (byte)(color.R * 255);
            bytes[i + 1] = (byte)(color.G * 255);
            bytes[i + 2] = (byte)(color.B * 255);
            bytes[i + 3] = 255;
        }
        return Image.CreateFromData(4, 4, true, Image.Format.Rgba8, bytes);
    }

    private static void CheckPixel(Image image, int x, Color color)
    {
        var actual = image.GetPixel(x, 8);
        Check(Math.Abs(actual.R - color.R) < .005f && Math.Abs(actual.G - color.G) < .005f &&
              Math.Abs(actual.B - color.B) < .005f, $"Expected {color} at x={x}, received {actual}.");
    }
}
