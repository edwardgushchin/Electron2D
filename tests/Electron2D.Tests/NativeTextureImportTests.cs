using System.Runtime.InteropServices;
using Electron2D;

internal static unsafe partial class NativeTextureImportTests
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Description
    {
        internal uint Width, Height, Mips, Layers, Format, Samples, Quality, Usage, Bind, CPUAccess, Misc;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct InitialData
    {
        internal byte* Data;
        internal uint Pitch, SlicePitch;
    }

    private const int Width = 7, Height = 3;

    internal static void Run(string profile)
    {
        Reject<InvalidOperationException>(() => Import(1));
        var gpu = profile is "gpu-direct3d12" or "gpu-direct3d12-local" or "gpu-direct3d12-preceding" or "gpu-direct3d12-sampled-abi";
        var d3d12 = gpu || profile == "direct3d12";
        var supported = d3d12 || profile == "direct3d11";
        var fixturePath = Environment.GetEnvironmentVariable("ELECTRON2D_D3D12_IMPORT_FIXTURE");
        var debug = Environment.GetEnvironmentVariable("ELECTRON2D_D3D12_IMPORT_DEBUG") == "1";
        var fixture = d3d12 ? NativeLibrary.Load(fixturePath ?? throw new InvalidOperationException("Set ELECTRON2D_D3D12_IMPORT_FIXTURE.")) : 0;
        try
        {
            if (gpu) RunGPUImport(profile, fixture, debug);
            else RunScene(profile, supported, fixture, debug);
        }
        finally { if (fixture != 0) NativeLibrary.Free(fixture); }
        Reject<InvalidOperationException>(() => Import(1));
        Console.WriteLine($"Native texture import {profile} passed.");
    }

    private static void RunScene(string profile, bool supported, nint fixture, bool debug)
    {
        if (fixture != 0 && debug)
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Cdecl]<int>)NativeLibrary.GetExport(fixture, "EnableDebug"))());
        ProjectSettings.Set(ProjectSettings.RenderingMethod, profile == "vulkan" ? "gpu" : "compatibility");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(48, 24), Title = "Native texture import" };
        using var output = root.GetTexture();
        using var seed = Image.CreateEmpty(Width, Height, false, Image.Format.Rgba8);
        seed.Fill(Colors.Yellow);
        var complete = false;
        root.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingDriverName() == profile, "Expected the selected renderer.");
            if (supported)
            {
                var source = RenderingServer.Texture2DCreate(seed);
                var native = (nint)RenderingServer.TextureGetNativeHandle(source);
                try
                {
                    Reject<ArgumentOutOfRangeException>(() => Import(0));
                    Reject<ArgumentOutOfRangeException>(() => Import((ulong)native, depth: 2));
                    Reject<ArgumentOutOfRangeException>(() => Import((ulong)native, layers: 2));
                    Reject<ArgumentOutOfRangeException>(() => Import((ulong)native, width: -1));
                    Reject<ArgumentOutOfRangeException>(() => Import((ulong)native, type: (RenderingServer.TextureType)2));
                    Reject<NotSupportedException>(() => Import((ulong)native, format: Image.Format.Rgb8));
                    Reject<InvalidOperationException>(() => Task.Run(() => Import((ulong)native)).GetAwaiter().GetResult());
                    if (fixture != 0) VerifyD3D12(root, output, native, seed, fixture, debug);
                    else VerifyD3D11(root, output, native, seed);
                }
                finally { RenderingServer.FreeRID(source); }
            }
            else
            {
                if (profile == "vulkan")
                    Reject<NotSupportedException>(() => RenderingServer.TextureCreateFromNativeHandle(
                        RenderingServer.TextureType.Texture, Image.Format.Rgb8, 1, Width, Height, 1));
                else
                    Reject<NotSupportedException>(() => Import(1));
                RenderingServer.ForceDraw();
                using var image = output.GetImage();
                Check(image is not null, "Explicit refusal preserves ordinary rendering.");
            }
            complete = true;
            root.Tree!.Quit();
        };
        Check(Engine.Run(root) == 0 && complete, "The import scene completed.");
    }

    private static void VerifyD3D11(Window root, Texture output, nint native, Image seed)
    {
        nint device;
        ((delegate* unmanaged[Stdcall]<nint, nint*, void>)Methods(native)[3])(native, &device);
        var currentDevice = device;
        try
        {
            Reject<ArgumentException>(() => Import((ulong)currentDevice));
            Reject<ArgumentException>(() => Import((ulong)native, width: Width + 1));
            VerifyForeignDevice();
            VerifyPixels(root, output, seed, (format, layers) => Create(currentDevice, format, layers),
                resource => UpdateExternally(currentDevice, resource));
        }
        finally { Marshal.Release(device); }
    }

    private static void VerifyD3D12(Window root, Texture output, nint native, Image seed, nint fixture, bool debug)
    {
        var create = NativeLibrary.GetExport(fixture, "CreateSample");
        nint CreateNative(uint format, uint layers, int foreign = 0)
        {
            nint resource;
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Cdecl]<nint, uint, uint, int, nint*, int>)create)
                (native, format, layers, foreign, &resource));
            return resource;
        }
        Reject<ArgumentException>(() => Import((ulong)native));
        var foreign = CreateNative(28, 1, 1);
        try { Reject<ArgumentException>(() => Import((ulong)foreign)); }
        finally { Marshal.Release(foreign); }
        var SRGB = CreateNative(29, 1);
        try { Reject<NotSupportedException>(() => Import((ulong)SRGB)); }
        finally { Marshal.Release(SRGB); }
        VerifyPixels(root, output, seed, (format, layers) => CreateNative(format, layers), null);
        var target = RenderingServer.TextureGetNativeHandle(output.GetRID());
        Check(target != 0, "A completed viewport exposes its allocation.");
        Reject<ArgumentException>(() => RenderingServer.TextureCreateFromNativeHandle(RenderingServer.TextureType.Texture,
            Image.Format.Rgba8, target, 48, 24, 1));
        if (debug)
        {
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Cdecl]<nint, int>)NativeLibrary.GetExport(fixture, "CheckDebug"))(native));
            Console.WriteLine("Direct3D 12 debug layer reported no errors or corruption.");
        }
    }

    private static void VerifyPixels(Window root, Texture output, Image seed, Func<uint, uint, nint> create, Action<nint>? externalUpdate)
    {
        var rgba = create(28, 1);
        var bgra = create(87, 1);
        var layered = create(28, 2);
        try { Reject<NotSupportedException>(() => Import((ulong)layered)); }
        finally { Marshal.Release(layered); }
        var canvas = RenderingServer.CanvasCreate();
        var item = RenderingServer.CanvasItemCreate();
        RenderingServer.ViewportAttachCanvas(root.GetViewportRID(), canvas);
        RenderingServer.CanvasItemSetParent(item, canvas);
        RenderingServer.CanvasItemSetDefaultTextureFilter(item, TextureFilter.Nearest);
        Reject<ArgumentException>(() => Import((ulong)rgba, width: Width + 1));
        var first = Import((ulong)rgba);
        var second = Import((ulong)bgra);
        var proxy = RenderingServer.TextureProxyCreate(second);
        var channels = RenderingServer.CanvasTextureCreate();
        try
        {
            if (externalUpdate is null) Reject<ArgumentException>(() => Import((ulong)rgba));
            Check(RenderingServer.TextureGetNativeHandle(first) == (ulong)rgba &&
                  RenderingServer.TextureGetNativeHandle(proxy, SRGB: true) == (ulong)bgra, "Both formats and the proxy retain the original allocations.");
            Check(RenderingServer.TextureGetFormat(proxy) == Image.Format.Rgba8, "BGRA storage projects portable RGBA8.");
            Check(!RenderingServer.TextureGetRDTexture(first).IsValid(), "Compatibility imports do not claim device backing.");
            using (var image = RenderingServer.Texture2DGet(first))
                Check(image?.GetPixel(4, 2) == Colors.Red, "An independent native producer supplies the initial RGBA pixels.");
            RenderingServer.TextureSetPath(second, "native-source");
            Check(RenderingServer.TextureGetPath(second) == "native-source", "The owned wrapper carries normal texture metadata.");
            RenderingServer.CanvasItemAddTextureRect(item, new(0, 0, 14, 6), first);
            RenderingServer.TextureSetSizeOverride(second, 14, 6);
            RenderingServer.CanvasItemAddTextureRectRegion(item, new(16, 0, 14, 6), proxy, new(0, 0, 14, 6));
            RenderingServer.CanvasTextureSetChannel(channels, CanvasTextureChannel.Diffuse, proxy);
            RenderingServer.CanvasItemAddTextureRectRegion(item, new(32, 0, 14, 6), channels, new(0, 0, 14, 6));
            RenderingServer.ForceDraw();
            CheckOutput(output, Colors.Red, Colors.Blue);
            using (var image = RenderingServer.Texture2DGet(channels))
            {
                Check(image is { Width: Width, Height: Height, PixelFormat: Image.Format.Rgba8 } &&
                      image.GetPixel(0, 0) == Colors.Blue && image.GetPixel(6, 2) == new Color(0, 1, 0, 128 / 255f),
                    "Native readback preserves format, pitch, row order and alpha.");
                if (externalUpdate is null)
                    Check(image!.GetPixel(5, 2) == new Color(45 / 255f, 17 / 255f, 238 / 255f, 0),
                        "Unblended readback preserves RGB in a fully transparent texel.");
                image!.SetPixel(0, 0, Colors.White);
            }
            if (externalUpdate is not null) externalUpdate(rgba);
            else
            {
                using var green = Image.CreateEmpty(Width, Height, false, Image.Format.Rgba8);
                green.Fill(Colors.Lime);
                RenderingServer.Texture2DUpdate(first, green);
            }
            RenderingServer.ForceDraw();
            CheckOutput(output, Colors.Lime, Colors.Blue);
            Marshal.Release(rgba);
            rgba = 0;
            Marshal.Release(bgra);
            bgra = 0;
            RenderingServer.Texture2DUpdate(second, seed);
            RenderingServer.ForceDraw();
            CheckOutput(output, Colors.Lime, Colors.Yellow);
            using (var image = RenderingServer.Texture2DGet(proxy))
                Check(image?.GetPixel(1, 1) == Colors.Yellow, "Canvas update writes into the imported allocation.");
            using var wrongSize = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
            Reject<ArgumentException>(() => RenderingServer.Texture2DUpdate(second, wrongSize));
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.Texture2DUpdate(second, seed, 1));

            for (var i = 0; i < 64; i++) RenderingServer.TextureGetNativeHandle(proxy);
            var bytes = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) RenderingServer.TextureGetNativeHandle(proxy);
            Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Prepared imported handle export has no managed owner allocation.");

            for (var i = 0; i < 24; i++) RenderingServer.ForceDraw(swapBuffers: false);
            bytes = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) RenderingServer.ForceDraw(swapBuffers: false);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - bytes;
            Check(allocated == 0, $"64 prepared active frames allocated {allocated} managed owner bytes.");

            var replacement = RenderingServer.Texture2DCreate(seed);
            RenderingServer.TextureReplace(replacement, first);
            Check(RenderingServer.TextureGetNativeHandle(replacement) != 0, "Replacement transfers native storage to an existing RID.");
            Reject<ArgumentException>(() => RenderingServer.TextureGetNativeHandle(first));
            first = default;
            RenderingServer.TextureReplace(second, replacement);
            Reject<ArgumentException>(() => RenderingServer.TextureGetNativeHandle(replacement));
            RenderingServer.ForceDraw();
            using (var image = output.GetImage())
                Check(image?.GetPixel(20, 2) == Colors.Lime, "A retained proxy sees replaced native storage.");
            replacement = RenderingServer.Texture2DCreate(seed);
            RenderingServer.TextureReplace(second, replacement);
            RenderingServer.ForceDraw();
            using (var image = output.GetImage())
                Check(image?.GetPixel(20, 2) == Colors.Yellow, "The same proxy follows ordinary uploads after native replacement.");
        }
        finally
        {
            RenderingServer.FreeRID(item);
            RenderingServer.FreeRID(canvas);
            RenderingServer.FreeRID(channels);
            RenderingServer.FreeRID(proxy);
            RenderingServer.FreeRID(second);
            if (first.IsValid()) RenderingServer.FreeRID(first);
            if (bgra != 0) Marshal.Release(bgra);
            if (rgba != 0) Marshal.Release(rgba);
        }
    }

    private static void CheckOutput(Texture output, Color first, Color second)
    {
        using var image = output.GetImage();
        Check(image is not null, "The canvas has a completed image.");
        var direct = image!.GetPixel(4, 2);
        var proxy = image.GetPixel(20, 2);
        var diffuse = image.GetPixel(36, 2);
        Check(direct == first && proxy == second && diffuse == second,
            $"Canvas sampled {direct}, {proxy}, {diffuse}; expected {first}, {second}, {second}.");
    }

    private static void VerifyForeignDevice()
    {
        Marshal.ThrowExceptionForHR(CreateDevice(0, 5, 0, 0, 0, 0, 7, out var device, 0, 0));
        try
        {
            var texture = Create(device, 28);
            try { Reject<ArgumentException>(() => Import((ulong)texture)); }
            finally { Marshal.Release(texture); }
        }
        finally { Marshal.Release(device); }
    }

    private static nint Create(nint device, uint format, uint layers = 1)
    {
        var description = new Description
        {
            Width = Width,
            Height = Height,
            Format = format,
            Mips = 1,
            Layers = layers,
            Samples = 1,
            Bind = 8,
        };
        var pixels = stackalloc uint[Width * Height];
        new Span<uint>(pixels, Width * Height).Fill(0xff0000ff);
        pixels[Width * Height - 1] = 0x8000ff00;
        var data = new InitialData { Data = (byte*)pixels, Pitch = Width * 4 };
        nint texture;
        Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, Description*, InitialData*, nint*, int>)Methods(device)[5])
            (device, &description, layers == 1 ? &data : null, &texture));
        return texture;
    }

    private static void UpdateExternally(nint device, nint texture)
    {
        nint context;
        ((delegate* unmanaged[Stdcall]<nint, nint*, void>)Methods(device)[40])(device, &context);
        try
        {
            var pixels = stackalloc uint[Width * Height];
            new Span<uint>(pixels, Width * Height).Fill(0xff00ff00);
            ((delegate* unmanaged[Stdcall]<nint, nint, uint, nint, uint*, uint, uint, void>)Methods(context)[48])
                (context, texture, 0, 0, pixels, Width * 4, 0);
        }
        finally { Marshal.Release(context); }
    }

    private static nint* Methods(nint value) => *(nint**)value;

    private static RID Import(ulong handle, int width = Width, int depth = 1, int layers = 1,
        RenderingServer.TextureType type = RenderingServer.TextureType.Texture, Image.Format format = Image.Format.Rgba8) =>
        RenderingServer.TextureCreateFromNativeHandle(type, format, handle, width, Height, depth, layers);

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    [LibraryImport("d3d11.dll", EntryPoint = "D3D11CreateDevice")]
    private static partial int CreateDevice(nint adapter, int driver, nint software, uint flags, nint levels,
        uint levelCount, uint version, out nint device, nint selectedLevel, nint context);
}
