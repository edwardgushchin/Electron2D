using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static unsafe class DeviceArraySliceTests
{
    internal static void Run(string profile)
    {
        var debug = Environment.GetEnvironmentVariable("ELECTRON2D_D3D12_IMPORT_DEBUG") == "1";
        var fixture = debug ? NativeLibrary.Load(Environment.GetEnvironmentVariable("ELECTRON2D_D3D12_IMPORT_FIXTURE") ??
            throw new InvalidOperationException("Set ELECTRON2D_D3D12_IMPORT_FIXTURE.")) : 0;
        try
        {
            if (debug) Marshal.ThrowExceptionForHR(((delegate* unmanaged[Cdecl]<int>)NativeLibrary.GetExport(fixture, "EnableDebug"))());
            Run(profile, fixture);
        }
        finally { if (fixture != 0) NativeLibrary.Free(fixture); }
    }

    private static void Run(string profile, nint fixture)
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        var format = new RDTextureFormat
        {
            Width = 4,
            Height = 4,
            MIPMaps = 3,
            ArrayLayers = 2,
            TextureType = TextureType.TextureArray,
            Format = DataFormat.R32UINT,
            UsageBits = TextureUsageBits.Storage | TextureUsageBits.StorageAtomic |
                TextureUsageBits.CanUpdate | TextureUsageBits.CanCopyFrom,
        };
        using (format)
        using (var view = new RDTextureView())
        {
            var root = device.TextureCreate(format, view, [Pixels(11, 12, 13), Pixels(21, 22, 23)]);
            format.Format = DataFormat.R8G8B8A8UNORM;
            format.UsageBits = TextureUsageBits.Sampling | TextureUsageBits.CanCopyFrom;
            var sampled = device.TextureCreate(format, view, [Pixels(11, 12, 13), Pixels(21, 22, 23)]);
            if (profile == "unavailable")
            {
                Reject<NotSupportedException>(() => device.TextureCreateSharedFromSlice(view, root, 1, 1, 2));
                Check(device.TextureGetData(root, 1).SequenceEqual(Pixels(21, 22, 23)), "Refused slice preserves the owner bytes.");
                return;
            }
            Check(profile is "direct3d12" or "vulkan", "Unknown slice test profile.");
            var read = device.TextureCreateSharedFromSlice(view, sampled, 0, 1, 2);
            var write = device.TextureCreateSharedFromSlice(view, root, 1, 1, 2);
            var ordinary = device.TextureCreateSharedFromSlice(view, root, 1, 0);
            using var selected = device.TextureGetFormat(read);
            Check(device.TextureIsShared(read) && selected.Width == 2, "The slice has its own logical extent.");
            Check(device.GetDriverResource(DriverResource.Texture, write, 0) == device.GetDriverResource(DriverResource.Texture, root, 0),
                "The slice binds the original allocation.");
            Reject<ArgumentOutOfRangeException>(() => device.TextureCreateSharedFromSlice(view, root, 2, 0));
            Reject<ArgumentOutOfRangeException>(() => device.TextureCreateSharedFromSlice(view, root, 0, 2, 2));
            Dispatch(device, read, write, ordinary);
            var words = MemoryMarshal.Cast<byte, uint>(device.TextureGetData(root, 1));
            Check(words[0] == 21 && words[16] == 33 && words[17] == 22 && words[20] == 23,
                "Compute atomics changed one texel of the selected layer and mip.");
            Check(device.TextureGetData(root, 0).SequenceEqual(Pixels(11, 12, 13)), "The other layer keeps its bytes.");
            if (profile == "direct3d12") ReadStorageInGraphics(device, write);
            if (fixture != 0)
            {
                var native = (nint)device.GetDriverResource(DriverResource.Texture, root, 0);
                Marshal.ThrowExceptionForHR(((delegate* unmanaged[Cdecl]<nint, int>)NativeLibrary.GetExport(fixture, "CheckDebug"))(native));
                Console.WriteLine("Direct3D 12 debug layer reported no errors or corruption.");
            }
            device.FreeRID(sampled);
            device.FreeRID(root);
            Check(!device.TextureIsValid(read) && !device.TextureIsValid(write), "Retirement invalidates each slice.");
        }
        Console.WriteLine($"Sampled and storage slices passed on {profile} / {device.GetDeviceName()}.");
    }

    private static void Dispatch(RenderingDevice device, RID read, RID write, RID otherMip)
    {
        using var input = typeof(DeviceArraySliceTests).Assembly.GetManifestResourceStream("Electron2D.Compute.Direct3DLayerSlice.comp.spv")!;
        using var bytes = new MemoryStream();
        input.CopyTo(bytes);
        using var code = new RDShaderSPIRV { BytecodeCompute = bytes.ToArray() };
        var shader = device.ShaderCreateFromSPIRV(code);
        var pipeline = device.ComputePipelineCreate(shader);
        using var samplerState = new RDSamplerState();
        var sampler = device.SamplerCreate(samplerState);
        var results = device.StorageBufferCreate(20);
        using var sample = Uniform(0, UniformType.SamplerWithTexture, sampler, read);
        using var storage = Uniform(1, UniformType.Image, write);
        using var output = Uniform(2, UniformType.StorageBuffer, results);
        var set = device.UniformSetCreate([sample, storage, output], shader, 0);
        var list = device.ComputeListBegin();
        device.ComputeListBindComputePipeline(list, pipeline);
        device.ComputeListBindUniformSet(list, set, 0);
        device.ComputeListDispatch(list, 1, 1, 1);
        device.ComputeListEnd();
        Check(MemoryMarshal.Cast<byte, uint>(device.BufferGetData(results).AsSpan()).SequenceEqual<uint>([2, 2, 13, 2, 22]),
            "Compute sees the selected mip range, physical texels and prior atomic value.");
        Check(MemoryMarshal.Cast<byte, uint>(device.TextureGetData(otherMip, 0))[0] == 21, "A different mip of the same layer remains unchanged.");
    }

    private static void ReadStorageInGraphics(RenderingDevice device, RID slice)
    {
        using var module = RenderingDeviceGraphicsTests.Load("DeviceSliceStorage");
        var shader = device.ShaderCreateFromSPIRV(module);
        using var input = Uniform(0, UniformType.Image, slice);
        var set = device.UniformSetCreate([input], shader, 0);
        using var format = new RDTextureFormat
        {
            Width = 2,
            Height = 2,
            Format = DataFormat.R32UINT,
            UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom,
        };
        using var view = new RDTextureView();
        var texture = device.TextureCreate(format, view);
        var framebuffer = device.FramebufferCreate([texture]);
        using var raster = new RDPipelineRasterizationState();
        using var attachment = new RDPipelineColorBlendStateAttachment();
        using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend);
        var list = device.DrawListBegin(framebuffer);
        device.DrawListBindRenderPipeline(list, pipeline);
        device.DrawListBindUniformSet(list, set, 0);
        device.DrawListDraw(list, false, 1, 3);
        device.DrawListEnd();
        Check(MemoryMarshal.Cast<byte, uint>(device.TextureGetData(texture, 0).AsSpan()).SequenceEqual<uint>([37, 37, 37, 37]),
            "Vertex and fragment image loads address the same physical slice.");
    }

    private static RDUniform Uniform(int binding, UniformType type, params RID[] ids)
    {
        var uniform = new RDUniform { Binding = binding, UniformType = type };
        foreach (var id in ids) uniform.AddID(id);
        return uniform;
    }

    private static byte[] Pixels(uint first, uint second, uint third)
    {
        var words = new uint[21];
        words.AsSpan(0, 16).Fill(first);
        words.AsSpan(16, 4).Fill(second);
        words[20] = third;
        return MemoryMarshal.AsBytes(words.AsSpan()).ToArray();
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action operation) where T : Exception
    {
        try { operation(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
