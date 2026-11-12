using System.Runtime.InteropServices;
using Electron2D;
using Usage = Electron2D.RenderingDevice.TextureUsageBits;
using Format = Electron2D.RenderingDevice.DataFormat;

internal static unsafe partial class NativeTextureImportTests
{
    private const Usage Transfer = GPUTransfer & ~Usage.Sampling;
    private const int MIP1 = 16, MIP2 = 20;
    private const uint Red = 0xff0000ff, Green = 0xff00ff00, Blue = 0xffff0000, White = 0xffffffff;

    private static void VerifyGPULayerTargets(nint fixture, bool debug, bool preceding)
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        using var view = new RDTextureView();
        using var format = new RDTextureFormat
        {
            Format = Format.R8G8B8A8UNORM,
            Width = 4,
            Height = 4,
            ArrayLayers = 2,
            MIPMaps = 3,
            TextureType = RenderingDevice.TextureType.TextureArray,
            UsageBits = Transfer | Usage.ColorAttachment
        };
        var a = MakePixels(Red);
        var b = MakePixels(Green);
        var root = device.TextureCreate(format, view, [a, b]);
        Check(device.GetDriverResource(RenderingDevice.DriverResource.TextureDataFormat, root, 0) == 28,
            "Select Direct3D 12 for this profile.");
        if (preceding)
        {
            Reject<NotSupportedException>(() => device.TextureCreateSharedFromSlice(view, root, 1, 1));
            Check(device.TextureGetData(root, 1).AsSpan().SequenceEqual(b), "Missing adapter preserves the original allocation.");
            Console.WriteLine("Direct3D 12 preceding adapter rejects layer targets and retains transfers.");
            return;
        }

        var whole = device.TextureCreateShared(view, root);
        var first = device.TextureCreateSharedFromSlice(view, whole, 0, 1);
        var second = device.TextureCreateSharedFromSlice(view, first, 1, 1);
        var twin = device.TextureCreateSharedFromSlice(view, root, 1, 1);
        var interval = device.TextureCreateSharedFromSlice(view, root, 1, 1, 2);
        var tail = device.TextureCreateSharedFromSlice(view, root, 1, 2);
        Reject<ArgumentOutOfRangeException>(() => device.TextureCreateSharedFromSlice(view, root, 2, 1));
        Reject<ArgumentOutOfRangeException>(() => device.TextureCreateSharedFromSlice(view, root, 1, 2, 2));
        Check(device.TextureIsShared(second) && device.GetDriverResource(RenderingDevice.DriverResource.Texture, second, 0) ==
            device.GetDriverResource(RenderingDevice.DriverResource.Texture, root, 0), "A selected layer shares the original native resource.");
        using (var selected = device.TextureGetFormat(interval))
            Check(selected.Width == 2 && selected.Height == 2 && selected.ArrayLayers == 1 && selected.MIPMaps == 2 &&
                selected.TextureType == RenderingDevice.TextureType.Texture && selected.UsageBits == format.UsageBits,
                "An interval retains ordinary shape and bounded extent.");
        Check(device.TextureGetData(interval, 0).AsSpan().SequenceEqual(b.AsSpan(MIP1 * 4)), "The interval reads the selected physical layer and mips.");
        Reject<ArgumentException>(() => device.FramebufferCreate([second, twin]));
        Reject<ArgumentException>(() => device.TextureCopy(second, twin, default, default, new(1, 1, 1), 0, 0, 0, 0));

        device.FreeRID(whole);
        var framebuffer = device.FramebufferCreate([first, second]);
        device.DrawListBegin(framebuffer, RenderingDevice.DrawFlags.ClearColor0 | RenderingDevice.DrawFlags.ClearColor1, [Colors.Blue, Colors.White]);
        device.DrawListEnd();
        device.FreeRID(framebuffer);
        device.TextureCopy(first, tail, default, default, new(1, 1, 1), 0, 0, 0, 0);
        MemoryMarshal.Cast<byte, uint>(a.AsSpan()).Slice(MIP1, 4).Fill(Blue);
        var texels = MemoryMarshal.Cast<byte, uint>(b.AsSpan());
        texels.Slice(MIP1, 4).Fill(White);
        texels[MIP2] = Blue;
        Check(device.TextureGetData(root, 0).AsSpan().SequenceEqual(a) && device.TextureGetData(root, 1).AsSpan().SequenceEqual(b),
            "The framebuffer and copy change exactly the addressed layers and mips.");

        device.FreeRID(root);
        Check(!device.TextureIsValid(first) && !device.TextureIsValid(second) && !device.TextureIsValid(twin) &&
            !device.TextureIsValid(interval) && !device.TextureIsValid(tail), "Freeing the owner expires every dependent view.");
        format.UsageBits = Transfer;
        var copyOnly = device.TextureCreate(format, view, [a, b]);
        var copyLayer = device.TextureCreateSharedFromSlice(view, copyOnly, 1, 1, 2);
        device.TextureUpdate(copyLayer, 0, a.AsSpan(MIP1 * 4));
        a.AsSpan(MIP1 * 4).CopyTo(b.AsSpan(MIP1 * 4));
        Check(device.TextureGetData(copyOnly, 1).AsSpan().SequenceEqual(b), "Transfer-only slices write back to the selected interval.");
        device.FreeRID(copyOnly);

        format.UsageBits = Transfer | Usage.Sampling;
        var sampled = device.TextureCreate(format, view, [a, b]);
        Reject<NotSupportedException>(() => device.TextureCreateSharedFromSlice(view, sampled, 1, 1));
        Check(device.TextureGetData(sampled, 1).AsSpan().SequenceEqual(b), "Refusing an unsupported sampled slice preserves its owner.");
        device.FreeRID(sampled);

        format.UsageBits = Transfer | Usage.ColorAttachment;
        format.AddShareableFormat(Format.R8G8B8A8UNORM);
        format.AddShareableFormat(Format.R8G8B8A8SRGB);
        var mutable = device.TextureCreate(format, view, [a, b]);
        view.FormatOverride = Format.R8G8B8A8SRGB;
        var encoded = device.TextureCreateSharedFromSlice(view, mutable, 1, 2);
        Check(device.GetDriverResource(RenderingDevice.DriverResource.TextureDataFormat, encoded, 0) == 29,
            "The layer view uses the declared sRGB encoding.");
        framebuffer = device.FramebufferCreate([encoded]);
        device.DrawListBegin(framebuffer, RenderingDevice.DrawFlags.ClearColor0, [Colors.Red]);
        device.DrawListEnd();
        device.FreeRID(framebuffer);
        texels[MIP2] = Red;
        Check(device.TextureGetData(mutable, 0).AsSpan().SequenceEqual(a) && device.TextureGetData(mutable, 1).AsSpan().SequenceEqual(b),
            "The typed sRGB attachment changes only the selected subresource.");
        CheckGPUDebug(fixture, (nint)device.GetDriverResource(RenderingDevice.DriverResource.Texture, mutable, 0), debug);
        device.FreeRID(mutable);
        Console.WriteLine("Direct3D 12 layer targets: shared identity, simultaneous attachment clears, mip/layer transfer, alias guards and retirement passed.");
    }

    private static byte[] MakePixels(uint color)
    {
        var bytes = new byte[(MIP2 + 1) * 4];
        MemoryMarshal.Cast<byte, uint>(bytes.AsSpan()).Fill(color);
        return bytes;
    }
}
