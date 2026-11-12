using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static unsafe partial class NativeTextureImportTests
{
    private static void VerifyGPUImportedStorageArray(RenderingDevice device, nint fixture, bool debug, uint layers)
    {
        const TextureUsageBits usage = GPUTransfer | TextureUsageBits.Storage;
        var native = CreateGPUResource(device, fixture, layers, Levels, options: 8);
        RID image = default;
        try
        {
            if (layers > 1)
            {
                var missingUAV = CreateGPUResource(device, fixture, layers, Levels);
                try { Reject<ArgumentException>(() => Import(missingUAV)); }
                finally { Marshal.Release(missingUAV); }
            }

            image = Import(native);
            using var view = new RDTextureView();
            var alias = device.TextureCreateShared(view, image);
            Check(device.GetDriverResource(DriverResource.Texture, alias, 0) == (ulong)native,
                "The storage view retains the producer allocation.");
            VulkanTextureImportTests.WriteImportedImage(device, alias, layers, array: true);
            for (uint layer = 0; layer < layers; layer++)
            {
                var expected = GPUMipColors((int)layer * 2);
                var texels = MemoryMarshal.Cast<byte, uint>(expected.AsSpan());
                texels[0] = layer == 0 ? 0xffff0000u : 0xff0000ffu;
                Check(device.TextureGetData(image, layer).AsSpan().SequenceEqual(expected),
                    "The array UAV writes the selected texel in each layer without altering the other mips.");
            }

            device.FreeRID(image);
            image = default;
            Check(!device.TextureIsValid(alias), "The storage view retires with the original import.");
            RewriteGPUResource(fixture, native, 3);
            image = Import(native);
            Check(device.TextureGetData(image, layers - 1).AsSpan().SequenceEqual(GPUMipColors(3 + ((int)layers - 1) * 2)),
                "Retirement returns every layer and mip to the producer's sampled state.");
            CheckGPUDebug(fixture, native, debug);
        }
        finally
        {
            if (image.IsValid()) device.FreeRID(image);
            Marshal.Release(native);
        }

        RID Import(nint handle) => device.TextureCreateFromExtension(TextureType.TextureArray, DataFormat.R8G8B8A8UNORM,
            TextureSamples.Samples1, usage, (ulong)handle, Width, Height, 1, layers, Levels);
    }

    private static void VerifyGPUStorageArrayAtomics(RenderingDevice device)
    {
        const uint layers = 66;
        using var format = new RDTextureFormat
        {
            TextureType = TextureType.TextureArray,
            Format = DataFormat.R32UINT,
            ArrayLayers = layers,
            Width = 2,
            Height = 2,
            MIPMaps = 2,
            UsageBits = TextureUsageBits.Storage | TextureUsageBits.StorageAtomic |
                TextureUsageBits.CanCopyFrom | TextureUsageBits.CPURead
        };
        using var view = new RDTextureView();
        var image = device.TextureCreate(format, view);
        try
        {
            using var stream = typeof(NativeTextureImportTests).Assembly.GetManifestResourceStream("Electron2D.Compute.Direct3DStorageArrayAtomic.comp.spv")!;
            using var bytes = new MemoryStream();
            stream.CopyTo(bytes);
            using var module = new RDShaderSPIRV { BytecodeCompute = bytes.ToArray() };
            using var binding = new RDUniform { Binding = 0, UniformType = UniformType.Image };
            binding.AddID(image);
            var shader = device.ShaderCreateFromSPIRV(module);
            try
            {
                var pipeline = device.ComputePipelineCreate(shader);
                var set = device.UniformSetCreate([binding], shader, 0);
                var list = device.ComputeListBegin();
                try
                {
                    device.ComputeListBindComputePipeline(list, pipeline);
                    device.ComputeListBindUniformSet(list, set, 0);
                    for (var i = 0; i < 8; i++) device.ComputeListDispatch(list, 1, 1, layers);
                    var before = GC.GetAllocatedBytesForCurrentThread();
                    for (var i = 0; i < 64; i++) device.ComputeListDispatch(list, 1, 1, layers);
                    var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                    Check(allocated == 0, $"64 warmed array dispatches allocated {allocated} managed owner bytes.");
                }
                finally { device.ComputeListEnd(); }
            }
            finally { device.FreeRID(shader); }

            for (uint layer = 0; layer < layers; layer++)
            {
                var data = device.TextureGetData(image, layer).AsSpan();
                Check(BinaryPrimitives.ReadUInt32LittleEndian(data) == 72 * 3 &&
                    data[4..].IndexOfAnyExcept((byte)0) == -1,
                    "Every storage-only array layer receives all atomic operations and preserves the untouched mip.");
            }
        }
        finally { device.FreeRID(image); }
    }
}
