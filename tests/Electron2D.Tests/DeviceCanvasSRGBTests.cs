using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static class DeviceCanvasSRGBTests
{
    private sealed class Patch(Texture? texture = null) : Entity
    {
        protected override void OnDraw()
        {
            if (texture is not null) DrawTextureRect(texture, new(0, 0, 12, 12), false);
            else DrawRect(new(0, 0, 12, 12), Colors.White);
        }
    }

    internal static unsafe void Run(string profile)
    {
        var debug = profile == "direct3d12" && Environment.GetEnvironmentVariable("ELECTRON2D_D3D12_IMPORT_DEBUG") == "1";
        var fixture = debug ? NativeLibrary.Load(Environment.GetEnvironmentVariable("ELECTRON2D_D3D12_IMPORT_FIXTURE") ??
            throw new InvalidOperationException("Set ELECTRON2D_D3D12_IMPORT_FIXTURE.")) : 0;
        try
        {
            if (debug) Marshal.ThrowExceptionForHR(((delegate* unmanaged[Cdecl]<int>)NativeLibrary.GetExport(fixture, "EnableDebug"))());
            VerifyCanvas(profile, fixture);
        }
        finally { if (fixture != 0) NativeLibrary.Free(fixture); }
    }

    private static unsafe void VerifyCanvas(string profile, nint debugFixture)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var linear = new TextureRD();
        using var decoded = new TextureRD();
        using var sliceTexture = new TextureRD();
        using var sampledSlice = new TextureRD();
        using var sampledArray = new TextureArrayRD();
        using var arrayShader = Load("ArrayGLSL");
        using var arrayMaterial = new ShaderMaterial { Shader = arrayShader };
        using var white = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        white.Fill(Colors.White);
        using var detail = ImageTexture.CreateFromImage(white);
        var root = new Window { Size = new(64, 20) };
        var finished = false;
        root.Ready += _ =>
        {
            var device = RenderingServer.GetRenderingDevice()!;
            Check(RenderingServer.GetCurrentRenderingDriverName() == (profile == "direct3d12-preceding" ? "direct3d12" : profile), "The selected driver is active.");
            using var format = new RDTextureFormat
            {
                Width = 2,
                Height = 2,
                Format = DataFormat.R8G8B8A8UNORM,
                UsageBits = TextureUsageBits.Sampling | TextureUsageBits.CanUpdate | TextureUsageBits.CanCopyFrom,
            };
            using var view = new RDTextureView();
            var plain = device.TextureCreate(format, view);
            linear.TextureRDRID = plain;
            Reject<NotSupportedException>(() => RenderingServer.TextureGetRDTexture(linear.GetRID(), true));
            Check(!device.TextureIsShared(plain) && !device.TextureIsShared(default) &&
                !device.TextureIsShared(detail.GetRID()), "Owner, empty and foreign identities are not shared views.");
            device.FreeRID(plain);
            format.Format = DataFormat.R8G8B8A8SRGB;
            var encoded = device.TextureCreate(format, view);
            linear.TextureRDRID = encoded;
            Check(RenderingServer.TextureGetRDTexture(linear.GetRID(), true) == encoded, "Native sRGB is returned directly.");
            device.FreeRID(encoded);
            Check(!RenderingServer.TextureGetRDTexture(linear.GetRID(), true).IsValid(), "A dead source has no export.");
            format.Format = DataFormat.R8G8B8A8UNORM;
            format.AddShareableFormat(DataFormat.R8G8B8A8UNORM);
            format.AddShareableFormat(DataFormat.R8G8B8A8SRGB);
            if (profile == "direct3d12-preceding")
            {
                Reject<NotSupportedException>(() => device.TextureCreate(format, view));
                finished = true;
                root.Tree!.Quit();
                return;
            }

            var direct3D = profile == "direct3d12";
            if (direct3D) VerifyDirect3DFormats(device, format, view);

            var source = device.TextureCreate(format, view);
            linear.TextureRDRID = source;
            var proxy = RenderingServer.TextureProxyCreate(linear.GetRID());
            device.ComputeListBegin();
            try { Reject<InvalidOperationException>(() => RenderingServer.TextureGetRDTexture(proxy, true)); }
            finally { device.ComputeListEnd(); }
            var alias = RenderingServer.TextureGetRDTexture(proxy, true);
            Check(alias != source && device.TextureIsShared(alias) &&
                alias == RenderingServer.TextureGetRDTexture(linear.GetRID(), true), "A proxy reuses the source's distinct sRGB view.");
            device.FreeRID(alias);
            Check(!device.TextureIsShared(alias) && device.TextureIsValid(source), "Freeing the view preserves its source.");
            var recreated = RenderingServer.TextureGetRDTexture(linear.GetRID(), true);
            Check(recreated != alias && device.TextureIsShared(recreated), "A freed cached view is recreated.");
            decoded.TextureRDRID = recreated;
            if (direct3D)
            {
                var handle = RenderingServer.TextureGetNativeHandle(proxy);
                Check(handle != 0 && RenderingServer.TextureGetNativeHandle(proxy, true) == handle &&
                    handle == device.GetDriverResource(DriverResource.Texture, recreated, 0),
                    "The native canvas exports report the shared allocation.");
            }

            format.TextureType = TextureType.TextureArray;
            format.ArrayLayers = 2;
            format.MIPMaps = 2;
            var array = device.TextureCreate(format, view);
            var arrayCanvas = RenderingServer.TextureRDCreate(array);
            var arrayAlias = RenderingServer.TextureGetRDTexture(arrayCanvas, true);
            sampledArray.TextureRDRID = arrayAlias;
            Check(device.TextureIsShared(arrayAlias) && sampledArray.GetLayers() == 2 &&
                sampledArray.HasMIPMaps(), "An sRGB array retains its layer and mip counts.");
            view.SwizzleR = TextureSwizzle.B;
            view.SwizzleB = TextureSwizzle.R;
            var sliceOwner = array;
            var sliceLayer = 1u;
            if (direct3D)
            {
                Reject<NotSupportedException>(() => device.TextureCreateSharedFromSlice(view, array, 1, 1));
                format.TextureType = TextureType.Texture;
                format.ArrayLayers = 1;
                sliceOwner = device.TextureCreate(format, view);
                sliceLayer = 0;
            }
            var slice = device.TextureCreateSharedFromSlice(view, sliceOwner, sliceLayer, 1);
            sliceTexture.TextureRDRID = slice;
            var sliceAlias = RenderingServer.TextureGetRDTexture(sliceTexture.GetRID(), true);
            sampledSlice.TextureRDRID = sliceAlias;
            Check(device.TextureIsShared(slice) && device.TextureIsShared(sliceAlias) &&
                sampledSlice.GetWidth() == 1 && sampledSlice.GetHeight() == 1 && !sampledSlice.HasMIPMaps,
                "The sRGB export keeps the selected mip extent.");

            byte[] texel = [128, 64, 192, 255];
            byte[] ordinaryPixels = [128, 64, 192, 255, 128, 64, 192, 255, 128, 64, 192, 255, 128, 64, 192, 255];
            byte[] arrayPixels = [64, 128, 192, 255, 64, 128, 192, 255, 64, 128, 192, 255, 64, 128, 192, 255, 0, 0, 0, 0];
            device.TextureUpdate(source, 0, ordinaryPixels);
            device.TextureUpdate(array, 0, new byte[20]);
            device.TextureUpdate(array, 1, arrayPixels);
            device.TextureUpdate(sliceAlias, 0, texel);
            Check(device.TextureGetData(recreated, 0).SequenceEqual(ordinaryPixels) &&
                device.TextureGetData(slice, 0).SequenceEqual(texel) &&
                device.TextureGetData(sliceOwner, sliceLayer).AsSpan(16).SequenceEqual(texel) &&
                (!direct3D || device.TextureGetData(arrayAlias, 1).AsSpan(16).SequenceEqual(arrayPixels.AsSpan(16))) &&
                device.TextureGetData(arrayAlias, 1).AsSpan(0, 16).SequenceEqual(arrayPixels.AsSpan(0, 16)) &&
                device.TextureGetData(arrayAlias, 0).All(value => value == 0),
                "Views share encoded bytes and writes touch the selected physical subresources.");

            arrayShader.SetDefaultTextureParameter("detailMap", detail);
            arrayMaterial.SetShaderLayeredParameter("layers", sampledArray);
            arrayMaterial.SetShaderParameter("selectedLayer", 1f);
            root.AddChild(new Patch(linear) { Position = new(2, 2) });
            root.AddChild(new Patch(decoded) { Position = new(18, 2) });
            root.AddChild(new Patch(sampledSlice) { Position = new(34, 2) });
            root.AddChild(new Patch { Position = new(50, 2), Material = arrayMaterial });
            RenderingServer.FramePostDraw += AfterFrame;

            void AfterFrame()
            {
                using var frame = root.GetTexture().GetImage()!;
                Pixel(frame, 8, 128, 64, 192);
                Pixel(frame, 24, 55, 13, 134);
                Pixel(frame, 40, 134, 13, 55);
                Pixel(frame, 56, 13, 55, 134);
                device.FreeRID(slice);
                Check(!device.TextureIsValid(sliceAlias) && device.TextureIsShared(arrayAlias),
                    "Freeing the source slice expires its export and preserves the array view.");
                if (direct3D) device.FreeRID(sliceOwner);
                device.FreeRID(array);
                if (debugFixture != 0)
                {
                    var native = (nint)device.GetDriverResource(DriverResource.Texture, source, 0);
                    Marshal.ThrowExceptionForHR(((delegate* unmanaged[Cdecl]<nint, int>)NativeLibrary.GetExport(debugFixture, "CheckDebug"))(native));
                    Console.WriteLine("Direct3D 12 debug layer reported no errors or corruption.");
                }
                device.FreeRID(source);
                Check(!device.TextureIsShared(arrayAlias) && !device.TextureIsShared(recreated), "Owners retire their exports.");
                RenderingServer.FramePostDraw -= AfterFrame;
                finished = true;
                root.Tree!.Quit();
            }
        };
        Check(Engine.Run(root) == 0 && finished, "The device canvas sRGB fixture completed.");
        Console.WriteLine($"Device canvas sRGB export verified ({profile}).");
    }

    private static void VerifyDirect3DFormats(RenderingDevice device, RDTextureFormat format, RDTextureView view)
    {
        var usage = format.UsageBits;
        format.UsageBits |= TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyTo;
        view.FormatOverride = DataFormat.R8G8B8A8SRGB;
        var encoded = device.TextureCreate(format, view);
        view.FormatOverride = DataFormat.Max;
        var linear = device.TextureCreateShared(view, encoded);
        var resource = device.GetDriverResource(DriverResource.Texture, encoded, 0);
        Check(resource != 0 && resource == device.GetDriverResource(DriverResource.Texture, linear, 0) &&
            device.GetDriverResource(DriverResource.TextureDataFormat, linear, 0) == 28 &&
            device.GetDriverResource(DriverResource.TextureDataFormat, encoded, 0) == 29 &&
            device.GetDriverResource(DriverResource.TextureView, linear, 0) != device.GetDriverResource(DriverResource.TextureView, encoded, 0),
            "The initial sRGB view and the linear view have distinct typed SRVs on one D3D12 resource.");
        var framebuffer = device.FramebufferCreate([encoded]);
        device.DrawListBegin(framebuffer, DrawFlags.ClearColor0, [new Color(.5f, .25f, 0, 1)]);
        device.DrawListEnd();
        var bytes = device.TextureGetData(linear, 0);
        Check(Math.Abs(bytes[0] - 188) <= 1 && Math.Abs(bytes[1] - 137) <= 1 && bytes[2] == 0 && bytes[3] == 255,
            "Clearing the sRGB attachment encodes into shared linear storage.");
        var copy = device.TextureCreate(format, view);
        device.TextureCopy(encoded, copy, default, default, new(2, 2, 1), 0, 0, 0, 0);
        Check(device.TextureGetData(copy, 0).SequenceEqual(bytes), "Copies between linear and sRGB views preserve encoded bytes.");
        Reject<ArgumentException>(() => device.TextureCopy(encoded, linear, default, default, new(2, 2, 1), 0, 0, 0, 0));
        device.FreeRID(copy);
        device.FreeRID(framebuffer);
        device.FreeRID(encoded);
        Check(!device.TextureIsValid(linear), "Freeing an initial alternate view retires the underlying allocation and aliases.");
        format.UsageBits = usage | TextureUsageBits.Storage;
        Reject<NotSupportedException>(() => device.TextureCreate(format, view));
        format.UsageBits = usage;
        format.AddShareableFormat(DataFormat.B8G8R8A8SRGB);
        Reject<NotSupportedException>(() => device.TextureCreate(format, view));
        format.RemoveShareableFormat(DataFormat.B8G8R8A8SRGB);
    }

    private static void Pixel(Image frame, int x, int red, int green, int blue)
    {
        var color = frame.GetPixel(x, 8);
        Check(Math.Abs(color.R * 255 - red) < 2 && Math.Abs(color.G * 255 - green) < 2 &&
            Math.Abs(color.B * 255 - blue) < 2, $"Native sample at {x} has RGB {color}; expected ({red}, {green}, {blue}).");
    }
}
