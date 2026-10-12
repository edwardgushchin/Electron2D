using System.Runtime.InteropServices;
using Electron2D;

internal static class RenderingDeviceTextureTransferTests
{
    internal static void Run()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        Regions(device); Clears(device); IntegerAndFormats(device); Compressed(device); ComputeChain(device);
        Console.WriteLine("Native image regions, mip/layer clears, encoded formats, snapshots, compute chains and prepared allocation passed.");
    }
    private const RenderingDevice.TextureUsageBits TransferRoles = RenderingDevice.TextureUsageBits.CPURead | RenderingDevice.TextureUsageBits.CanCopyFrom | RenderingDevice.TextureUsageBits.CanCopyTo;
    private static void Regions(RenderingDevice device)
    {
        using var format = new RDTextureFormat { Width = 8, Height = 8, MIPMaps = 4, ArrayLayers = 2, TextureType = RenderingDevice.TextureType.TextureArray, Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageBits = TransferRoles };
        using var view = new RDTextureView();
        byte[] a = Enumerable.Range(0, 340).Select(i => (byte)i).ToArray(), b = Enumerable.Range(0, 340).Select(i => (byte)(255 - i)).ToArray();
        var source = device.TextureCreate(format, view, [a, b]); var destination = device.TextureCreate(format, view);
        byte[] expected = new byte[340];
        device.TextureCopy(source, destination, new(1, 2, 0), new(0, 1, 0), new(3, 2, 1), 0, 1, 1, 0);
        for (var y = 0; y < 2; y++) b.AsSpan(((y + 2) * 8 + 1) * 4, 12).CopyTo(expected.AsSpan(256 + ((y + 1) * 4) * 4));
        Check(device.TextureGetData(destination, 0).SequenceEqual(expected), "Region copies across array layers and mip levels preserve untouched bytes.");
        Check(device.TextureGetData(source, 1).SequenceEqual(b), "A region copy does not mutate its source.");
        device.TextureCopy(source, source, new(0, 0, 0), new(4, 4, 0), new(2, 2, 1), 0, 0, 0, 0);
        var selfExpected = (byte[])a.Clone(); for (var y = 0; y < 2; y++) a.AsSpan(y * 8 * 4, 8).CopyTo(selfExpected.AsSpan(((y + 4) * 8 + 4) * 4));
        Check(device.TextureGetData(source, 0).SequenceEqual(selfExpected), "Disjoint self copies use retained intermediate storage.");
        device.TextureCopy(source, source, new(0, 0, 0), new(0, 0, 0), new(2, 2, 1), 0, 1, 0, 0);
        for (var y = 0; y < 2; y++) selfExpected.AsSpan(y * 8 * 4, 8).CopyTo(selfExpected.AsSpan(256 + y * 4 * 4));
        Check(device.TextureGetData(source, 0).SequenceEqual(selfExpected), "Different subresources of one texture copy in command order.");
        Reject<ArgumentException>(() => device.TextureCopy(source, source, new(0, 0, 0), new(1, 1, 0), new(2, 2, 1), 0, 0, 0, 0));
        Reject<ArgumentOutOfRangeException>(() => device.TextureCopy(source, destination, new(8, 0, 0), new(0, 0, 0), new(1, 1, 1), 0, 0, 0, 0));
        Reject<ArgumentOutOfRangeException>(() => device.TextureCopy(source, destination, new(float.NaN, 0, 0), new(0, 0, 0), new(1, 1, 1), 0, 0, 0, 0));
        Reject<ArgumentOutOfRangeException>(() => device.TextureCopy(source, destination, new(0, 0, 1), new(0, 0, 0), new(1, 1, 1), 0, 0, 0, 0));
        Reject<ArgumentOutOfRangeException>(() => device.TextureCopy(source, destination, new(0, 0, 0), new(0, 0, 0), new(0, 1, 1), 0, 0, 0, 0));
        Reject<ArgumentOutOfRangeException>(() => device.TextureCopy(source, destination, new(0, 0, 0), new(0, 0, 0), new(1, 1, 1), 32, 0, 0, 0));
        Reject<ArgumentOutOfRangeException>(() => device.TextureCopy(source, destination, new(0, 0, 0), new(0, 0, 0), new(1, 1, 1), 0, 0, uint.MaxValue, 0));
        using (var other = RenderingServer.CreateLocalRenderingDevice()) Reject<ArgumentException>(() => other.TextureCopy(source, destination, new(0, 0, 0), new(0, 0, 0), new(1, 1, 1), 0, 0, 0, 0));
        device.TextureCopy(source, destination, new(.25f, .25f, 0), new(.5f, .5f, 0), new(1.5f, 1.5f, 1), 0, 0, 0, 1);
        Check(device.TextureGetData(destination, 1).AsSpan(0, 4).SequenceEqual(selfExpected.AsSpan(0, 4)), "Fractional coordinates retain the integral texel adapter after bounds checks.");
        var list = device.ComputeListBegin(); Reject<InvalidOperationException>(() => device.TextureCopy(source, destination, new(0, 0, 0), new(0, 0, 0), new(1, 1, 1), 0, 0, 0, 0)); device.ComputeListEnd();
        device.FreeRID(source); Reject<ArgumentException>(() => device.TextureCopy(source, destination, new(0, 0, 0), new(0, 0, 0), new(1, 1, 1), 0, 0, 0, 0)); device.FreeRID(destination);
    }
    private static void Clears(RenderingDevice device)
    {
        using var format = new RDTextureFormat { Width = 8, Height = 8, MIPMaps = 4, ArrayLayers = 2, TextureType = RenderingDevice.TextureType.TextureArray, Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageBits = TransferRoles }; using var view = new RDTextureView();
        byte[] initial = Enumerable.Repeat((byte)31, 340).ToArray(); var texture = device.TextureCreate(format, view, [initial, initial]);
        device.TextureClear(texture, new(.25f, .5f, .75f, 1), 1, 2, 1, 1); var expected = (byte[])initial.Clone();
        var layer0 = device.TextureGetData(texture, 0); var layer1 = device.TextureGetData(texture, 1); var clearPixel = layer1.AsSpan(256, 4).ToArray();
        Check(clearPixel[0] == 64 && clearPixel[1] is 127 or 128 && clearPixel[2] == 191 && clearPixel[3] == 255, "Normalized clear conversion stays within half a stored channel unit.");
        for (var i = 256; i < 336; i += 4) clearPixel.CopyTo(expected, i);
        Check(layer0.SequenceEqual(initial) && layer1.SequenceEqual(expected), "A clear selects only requested mip/layer ranges and preserves every neighboring byte.");
        byte[]? old = null; byte[]? next = null; device.TextureGetDataAsync(texture, 1, bytes => old = bytes); device.TextureClear(texture, new(0, 1, 0, 1), 0, 4, 1, 1); device.TextureGetDataAsync(texture, 1, bytes => next = bytes);
        device.Submit(); Check(old is null && next is null, "Clear and snapshot recording do not invoke callbacks."); device.Sync(); Check(old!.SequenceEqual(expected), "A queued snapshot predates subsequent clears.");
        Check(Enumerable.Range(0, 85).All(i => next![i * 4] == 0 && next[i * 4 + 1] == 255 && next[i * 4 + 2] == 0 && next[i * 4 + 3] == 255), "A later queued snapshot captures the clear.");
        Reject<ArgumentOutOfRangeException>(() => device.TextureClear(texture, default, 0, 0, 0, 1)); Reject<ArgumentOutOfRangeException>(() => device.TextureClear(texture, default, 0, 1, 0, 0));
        Reject<ArgumentOutOfRangeException>(() => device.TextureClear(texture, default, uint.MaxValue, 1, 0, 1)); Reject<ArgumentOutOfRangeException>(() => device.TextureClear(texture, default, 0, uint.MaxValue, 0, 1));
        Reject<ArgumentOutOfRangeException>(() => device.TextureClear(texture, default, 0, 1, 1, uint.MaxValue));
        format.UsageBits = RenderingDevice.TextureUsageBits.CPURead; var restricted = device.TextureCreate(format, view); Reject<InvalidOperationException>(() => device.TextureClear(restricted, default, 0, 1, 0, 1));
        Reject<InvalidOperationException>(() => device.TextureCopy(restricted, texture, new(0, 0, 0), new(0, 0, 0), new(1, 1, 1), 0, 0, 0, 0));
        Reject<InvalidOperationException>(() => device.TextureCopy(texture, restricted, new(0, 0, 0), new(0, 0, 0), new(1, 1, 1), 0, 0, 0, 0));
        device.ComputeListBegin(); Reject<InvalidOperationException>(() => device.TextureClear(texture, default, 0, 1, 0, 1)); device.ComputeListEnd(); device.FreeRID(restricted); device.FreeRID(texture);
    }
    private static void IntegerAndFormats(RenderingDevice device)
    {
        using var format = new RDTextureFormat { Format = RenderingDevice.DataFormat.R32UINT, UsageBits = TransferRoles }; using var view = new RDTextureView(); var source = device.TextureCreate(format, view);
        device.TextureClear(source, new(BitConverter.Int32BitsToSingle(7), 0, 0, 0), 0, 1, 0, 1); Check(BitConverter.ToUInt32(device.TextureGetData(source, 0)) == 7, "Integer clear lanes preserve encoded bits.");
        format.Format = RenderingDevice.DataFormat.R32SINT; var signed = device.TextureCreate(format, view); device.TextureClear(signed, new(BitConverter.Int32BitsToSingle(-7), 0, 0, 0), 0, 1, 0, 1);
        Check(BitConverter.ToInt32(device.TextureGetData(signed, 0)) == -7, "Signed clear bit lanes preserve negative values without float conversion.");
        format.Format = RenderingDevice.DataFormat.R32SFLOAT; var destination = device.TextureCreate(format, view); device.TextureClear(source, new(1, 0, 0, 0), 0, 1, 0, 1);
        device.TextureCopy(source, destination, new(0, 0, 0), new(0, 0, 0), new(1, 1, 1), 0, 0, 0, 0); Check(BitConverter.ToSingle(device.TextureGetData(destination, 0)) == 1, "Compatible formats copy encoded bits without color conversion.");
        device.TextureClear(destination, new(-3.5f, 0, 0, 0), 0, 1, 0, 1); Check(BitConverter.ToSingle(device.TextureGetData(destination, 0)) == -3.5f, "Float clears preserve HDR and signed values.");
        format.Format = RenderingDevice.DataFormat.R8UNORM; var mismatch = device.TextureCreate(format, view); Reject<ArgumentException>(() => device.TextureCopy(source, mismatch, new(0, 0, 0), new(0, 0, 0), new(1, 1, 1), 0, 0, 0, 0));
        format.Format = RenderingDevice.DataFormat.R8SINT; var narrow = device.TextureCreate(format, view); device.TextureClear(narrow, new(BitConverter.Int32BitsToSingle(-7), 0, 0, 0), 0, 1, 0, 1); Check(device.TextureGetData(narrow, 0)[0] == 249, "Narrow signed clear lanes preserve representable negative values.");
        Reject<ArgumentOutOfRangeException>(() => device.TextureClear(narrow, new(1, 0, 0, 0), 0, 1, 0, 1));
        format.Format = RenderingDevice.DataFormat.R8G8B8A8SRGB; var srgb = device.TextureCreate(format, view); device.TextureClear(srgb, new(.5f, 0, 1, 1), 0, 1, 0, 1); var encoded = device.TextureGetData(srgb, 0); Check(encoded[0] is 187 or 188 && encoded[1] == 0 && encoded[2] == 255 && encoded[3] == 255, "sRGB clear converts linear color before raw copies.");
        device.FreeRID(narrow); device.FreeRID(srgb); device.FreeRID(source); device.FreeRID(signed); device.FreeRID(destination); device.FreeRID(mismatch);
    }
    private static void Compressed(RenderingDevice device)
    {
        using var format = new RDTextureFormat { Width = 5, Height = 3, MIPMaps = 3, Format = RenderingDevice.DataFormat.BC3UNORMBLOCK, UsageBits = TransferRoles }; using var view = new RDTextureView();
        if (!device.TextureIsFormatSupportedForUsage(format.Format, format.UsageBits)) return;
        byte[] bytes = Enumerable.Range(0, 64).Select(x => (byte)x).ToArray(); var source = device.TextureCreate(format, view, [bytes]); var destination = device.TextureCreate(format, view);
        device.TextureCopy(source, destination, new(0, 0, 0), new(0, 0, 0), new(5, 3, 1), 0, 0, 0, 0); device.TextureCopy(source, destination, new(0, 0, 0), new(0, 0, 0), new(2, 1, 1), 1, 1, 0, 0); device.TextureCopy(source, destination, new(0, 0, 0), new(0, 0, 0), new(1, 1, 1), 2, 2, 0, 0);
        Check(device.TextureGetData(destination, 0).SequenceEqual(bytes), "Compressed full edges and small mips copy complete encoded blocks.");
        Reject<ArgumentException>(() => device.TextureCopy(source, destination, new(1, 0, 0), new(0, 0, 0), new(4, 3, 1), 0, 0, 0, 0)); Reject<ArgumentException>(() => device.TextureCopy(source, destination, new(0, 0, 0), new(0, 0, 0), new(3, 3, 1), 0, 0, 0, 0));
        device.TextureCopy(source, destination, new(4, 0, 0), new(4, 0, 0), new(1, 3, 1), 0, 0, 0, 0); Check(device.TextureGetData(destination, 0).SequenceEqual(bytes), "Partial edge block copies preserve encoded content.");
        Reject<NotSupportedException>(() => device.TextureClear(destination, default, 0, 1, 0, 1)); device.FreeRID(source); device.FreeRID(destination);
    }
    private static void ComputeChain(RenderingDevice device)
    {
        using var format = new RDTextureFormat { Width = 9, Height = 7, Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageBits = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CanCopyTo | RenderingDevice.TextureUsageBits.CPURead }; using var view = new RDTextureView(); var source = device.TextureCreate(format, view);
        format.Format = RenderingDevice.DataFormat.R32G32B32A32SFLOAT; format.UsageBits = RenderingDevice.TextureUsageBits.Storage | RenderingDevice.TextureUsageBits.CanCopyFrom; var output = device.TextureCreate(format, view);
        format.UsageBits = TransferRoles; var destination = device.TextureCreate(format, view); using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state); var count = device.StorageBufferCreate(4);
        using var module = new RDShaderSPIRV(); using var resource = typeof(RenderingDeviceTextureTransferTests).Assembly.GetManifestResourceStream("Electron2D.Compute.LocalTexture.comp.spv")!; using var memory = new MemoryStream(); resource.CopyTo(memory); module.BytecodeCompute = memory.ToArray(); var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        using var sampled = new RDUniform { Binding = 7, UniformType = RenderingDevice.UniformType.SamplerWithTexture }; sampled.AddID(sampler); sampled.AddID(source); var sourceSet = device.UniformSetCreate([sampled], shader, 3);
        using var target = new RDUniform { Binding = 2, UniformType = RenderingDevice.UniformType.Image }; target.AddID(output); var targetSet = device.UniformSetCreate([target], shader, 5);
        using var counter = new RDUniform { Binding = 4, UniformType = RenderingDevice.UniformType.StorageBuffer }; counter.AddID(count); var counterSet = device.UniformSetCreate([counter], shader, 9);
        byte[] result = new byte[9 * 7 * 16]; Action callback = () => { };
        void Prepared()
        {
            device.TextureClear(source, new(.5f, .25f, .125f, 1), 0, 1, 0, 1); device.BufferClear(count, 0, 4);
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, sourceSet, 3); device.ComputeListBindUniformSet(list, targetSet, 5); device.ComputeListBindUniformSet(list, counterSet, 9); device.ComputeListDispatch(list, 2, 1, 1); device.ComputeListEnd();
            device.TextureClear(destination, default, 0, 1, 0, 1); device.TextureCopy(output, destination, new(1, 2, 0), new(2, 1, 0), new(4, 3, 1), 0, 0, 0, 0); device.TextureGetDataAsync(destination, 0, result.AsMemory(), callback); device.Submit(); device.Sync();
        }
        Prepared(); var floats = MemoryMarshal.Cast<byte, float>(result.AsSpan()); var encoded = device.TextureGetData(source, 0)[0]; Check(encoded is 127 or 128, "Compute input uses the native normalized clear result.");
        for (var y = 0; y < 7; y++) for (var x = 0; x < 9; x++) { var selected = x >= 2 && x < 6 && y >= 1 && y < 4; Check(Math.Abs(floats[(y * 9 + x) * 4] - (selected ? encoded / 255f + .25f : 0)) < .00001f, "Clear, compute, region copy and queued snapshot preserve command ordering."); }
        for (var i = 0; i < 16; i++) Prepared(); var allocation = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Prepared(); Check(GC.GetAllocatedBytesForCurrentThread() == allocation, "Prepared clear/compute/copy/Memory snapshot chains allocate zero owner-thread managed bytes.");
        foreach (var rid in new[] { source, output, destination, sampler, count, sourceSet, targetSet, counterSet, pipeline, shader }) device.FreeRID(rid);
    }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
