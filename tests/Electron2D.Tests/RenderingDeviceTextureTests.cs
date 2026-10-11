using System.Runtime.InteropServices;
using Electron2D;

internal static class RenderingDeviceTextureTests
{
    internal static void Run()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        foreach (var name in new[] { "LocalTexture.comp", "LocalTextureHLSL.comp" })
        {
            using var format = new RDTextureFormat { Width = 13, Height = 9, Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageBits = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CanUpdate | RenderingDevice.TextureUsageBits.CPURead };
            using var outputFormat = new RDTextureFormat { Width = 13, Height = 9, Format = RenderingDevice.DataFormat.R32G32B32A32SFLOAT, UsageBits = RenderingDevice.TextureUsageBits.Storage | RenderingDevice.TextureUsageBits.CPURead };
            using var view = new RDTextureView(); using var samplerState = new RDSamplerState();
            byte[] pixels = new byte[13 * 9 * 4]; for (var i = 0; i < pixels.Length; i += 4) { pixels[i] = 128; pixels[i + 3] = 255; }
            var texture = device.TextureCreate(format, view, [pixels]); var output = device.TextureCreate(outputFormat, view); var sampler = device.SamplerCreate(samplerState); var count = device.StorageBufferCreate(4);
            using var module = new RDShaderSPIRV(); using var stream = typeof(RenderingDeviceTextureTests).Assembly.GetManifestResourceStream("Electron2D.Compute." + name + ".spv")!;
            using var memory = new MemoryStream(); stream.CopyTo(memory); module.BytecodeCompute = memory.ToArray(); var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
            var invalidSize = module.BytecodeCompute; var words = MemoryMarshal.Cast<byte, uint>(invalidSize.AsSpan());
            for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
                if ((words[at] & 65535) == 16 && (words[at] >> 16) == 6 && words[at + 2] == 17) { words[at + 3] = 1u << 31; words[at + 4] = 1u << 31; words[at + 5] = 4; break; }
            module.BytecodeCompute = invalidSize; Reject<ArgumentException>(() => device.ShaderCreateFromSPIRV(module));

            using var sourceBinding = new RDUniform { Binding = 7, UniformType = RenderingDevice.UniformType.SamplerWithTexture }; sourceBinding.AddID(sampler); sourceBinding.AddID(texture);
            using var targetBinding = new RDUniform { Binding = 2, UniformType = RenderingDevice.UniformType.Image }; targetBinding.AddID(output);
            using var countBinding = new RDUniform { Binding = 4, UniformType = RenderingDevice.UniformType.StorageBuffer }; countBinding.AddID(count);
            using var mismatchFormat = new RDTextureFormat { Format = RenderingDevice.DataFormat.R32SFLOAT, UsageBits = RenderingDevice.TextureUsageBits.Storage }; var mismatch = device.TextureCreate(mismatchFormat, view);
            using var bad = new RDUniform { Binding = 2, UniformType = RenderingDevice.UniformType.Image }; bad.AddID(mismatch);
            if (name == "LocalTexture.comp") Reject<ArgumentException>(() => device.UniformSetCreate([bad], shader, 5)); device.FreeRID(mismatch);
            var sources = device.UniformSetCreate([sourceBinding], shader, 3); var targets = device.UniformSetCreate([targetBinding], shader, 5); var counts = device.UniformSetCreate([countBinding], shader, 9);
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, sources, 3); device.ComputeListBindUniformSet(list, targets, 5); device.ComputeListBindUniformSet(list, counts, 9); device.ComputeListDispatch(list, 2, 2, 1); device.ComputeListEnd();
            var result = device.TextureGetData(output, 0); var floats = MemoryMarshal.Cast<byte, float>(result.AsSpan());
            for (var i = 0; i < 13 * 9; i++) Check(Math.Abs(floats[i * 4] - (128f / 255 + .25f)) < .00001f && floats[i * 4 + 3] == 1, "Sampled image, sampler and writable image compute preserve pixels.");
            Check(BitConverter.ToUInt32(device.BufferGetData(count)) == 117, "Image and buffer bindings share one compute dispatch.");
            Check(device.TextureGetData(texture, 0).SequenceEqual(pixels), "Initial uploads/readback preserve exact bytes.");
            byte[] scratch = new byte[result.Length];
            void Prepared()
            {
                device.TextureUpdate(texture, 0, pixels);
                var work = device.ComputeListBegin(); device.ComputeListBindComputePipeline(work, pipeline);
                device.ComputeListBindUniformSet(work, sources, 3); device.ComputeListBindUniformSet(work, targets, 5); device.ComputeListBindUniformSet(work, counts, 9);
                device.ComputeListDispatch(work, 2, 2, 1); device.ComputeListEnd(); device.TextureGetData(output, 0, scratch.AsSpan());
            }
            for (var i = 0; i < 16; i++) Prepared(); var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Prepared();
            Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Prepared texture update/compute/span-readback allocates no owner-thread managed bytes.");
            format.Width = 1; using var reported = device.TextureGetFormat(texture); Check(reported.Width == 13, "Allocation descriptions are copied independently of later config changes.");
            reported.Width = 2; using var again = device.TextureGetFormat(texture); Check(again.Width == 13, "Format getters return independent descriptions.");
            device.FreeRID(texture); Check(!device.UniformSetIsValid(sources) && !device.TextureIsValid(texture), "Freeing a texture invalidates borrowing sets.");
            device.FreeRID(output); device.FreeRID(sampler); device.FreeRID(count); device.FreeRID(sources); device.FreeRID(targets); device.FreeRID(counts); device.FreeRID(shader); device.FreeRID(pipeline);

            Console.WriteLine(name + " sampled/storage texture compute passed.");
        }
        LayersAndMips(device);
        SamplingModes(device);
        IntegerImages(device);
        CompressedTransfer(device);
        Cancellation();
    }
    private static void LayersAndMips(RenderingDevice device)
    {
        using var format = new RDTextureFormat { Width = 3, Height = 3, Mipmaps = 2, ArrayLayers = 2, TextureType = RenderingDevice.TextureType.TextureArray, Format = RenderingDevice.DataFormat.R8UNORM, UsageBits = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CanUpdate | RenderingDevice.TextureUsageBits.CPURead };
        using var view = new RDTextureView(); byte[] a = Enumerable.Range(1, 10).Select(x => (byte)x).ToArray(); byte[] b = Enumerable.Range(11, 10).Select(x => (byte)x).ToArray();
        var texture = device.TextureCreate(format, view, [a, b]);
        Check(device.TextureGetData(texture, 0).SequenceEqual(a) && device.TextureGetData(texture, 1).SequenceEqual(b), "Odd-sized mip chains keep tight public layout and aligned native offsets across layers.");
        Array.Fill(a, (byte)37); device.TextureUpdate(texture, 0, a);
        Check(device.TextureGetData(texture, 0).SequenceEqual(a) && device.TextureGetData(texture, 1).SequenceEqual(b), "Layer updates preserve other layers and every mip.");
        Reject<ArgumentOutOfRangeException>(() => device.TextureGetData(texture, 2));
        Reject<ArgumentException>(() => device.TextureUpdate(texture, 0, new byte[9]));
        Reject<ArgumentException>(() => device.TextureCreate(format, view, [a]));
        view.SwizzleR = RenderingDevice.TextureSwizzle.B; Reject<NotSupportedException>(() => device.TextureCreate(format, view)); view.SwizzleR = RenderingDevice.TextureSwizzle.R;
        using var sampler = new RDSamplerState { RepeatU = RenderingDevice.SamplerRepeatMode.ClampToBorder }; Reject<NotSupportedException>(() => device.SamplerCreate(sampler));

        device.ComputeListBegin(); Reject<InvalidOperationException>(() => device.TextureUpdate(texture, 0, a)); device.ComputeListEnd();
        using (var other = RenderingServer.CreateLocalRenderingDevice()) Reject<ArgumentException>(() => other.TextureGetData(texture, 0));
        byte[]? captured = null; device.TextureGetDataAsync(texture, 0, bytes => captured = bytes); Array.Fill(a, (byte)49); device.TextureUpdate(texture, 0, a);
        byte[] memory = new byte[10]; bool completed = false; device.TextureGetDataAsync(texture, 0, memory.AsMemory(), () => completed = true); device.Submit(); Check(captured is null && !completed, "Texture callbacks wait for explicit completion."); device.Sync();
        Check(captured!.All(x => x == 37) && memory.All(x => x == 49) && completed, "Queued texture reads snapshot all mips at request time.");
        Action callback = () => { }; void PreparedRead() { device.TextureGetDataAsync(texture, 1, memory.AsMemory(), callback); device.Submit(); device.Sync(); }
        for (var i = 0; i < 16; i++) PreparedRead(); var allocation = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) PreparedRead(); Check(GC.GetAllocatedBytesForCurrentThread() == allocation, "Prepared mip-chain async reads allocate zero managed bytes.");
        Reject<ArgumentException>(() => device.TextureGetDataAsync(texture, 0, new byte[9].AsMemory(), callback));
        Reject<ArgumentNullException>(() => device.TextureGetDataAsync(texture, 0, (Action<byte[]>)null!));
        using var huge = new RDTextureFormat { Width = int.MaxValue, Height = int.MaxValue, Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageBits = RenderingDevice.TextureUsageBits.Sampling }; Reject<ArgumentOutOfRangeException>(() => device.TextureCreate(huge, view));
        huge.Format = RenderingDevice.DataFormat.R32G32B32A32SFLOAT; Reject<ArgumentOutOfRangeException>(() => device.TextureCreate(huge, view));
        Check(!device.TextureIsFormatSupportedForUsage(RenderingDevice.DataFormat.D16UNORM, RenderingDevice.TextureUsageBits.Sampling), "Depth storage needs attachment/transfer integration.");
        device.FreeRID(texture); Check(!device.TextureIsValid(texture), "Freed texture identity is invalid.");
        Reject<ArgumentException>(() => device.TextureGetData(texture, 0));
    }
    private static void SamplingModes(RenderingDevice device)
    {
        using var format = new RDTextureFormat { Width = 2, Height = 2, ArrayLayers = 2, Mipmaps = 2, TextureType = RenderingDevice.TextureType.TextureArray, Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageBits = RenderingDevice.TextureUsageBits.Sampling };
        using var view = new RDTextureView();
        byte[] first = [0, 0, 0, 255, 255, 0, 0, 255, 0, 0, 0, 255, 255, 0, 0, 255, 0, 255, 0, 255];
        byte[] second = [0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255, 255];
        var texture = device.TextureCreate(format, view, [first, second]); var output = device.StorageBufferCreate(16); var parameters = device.UniformBufferCreate(16);
        using var module = new RDShaderSPIRV(); using var stream = typeof(RenderingDeviceTextureTests).Assembly.GetManifestResourceStream("Electron2D.Compute.LocalTextureModes.comp.spv")!;
        using var memory = new MemoryStream(); stream.CopyTo(memory); module.BytecodeCompute = memory.ToArray(); var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        using var target = new RDUniform { Binding = 4, UniformType = RenderingDevice.UniformType.StorageBuffer }; target.AddID(output); var targetSet = device.UniformSetCreate([target], shader, 9);
        using var parameter = new RDUniform { Binding = 1, UniformType = RenderingDevice.UniformType.UniformBuffer }; parameter.AddID(parameters); var parameterSet = device.UniformSetCreate([parameter], shader, 2);
        float[] Sample(RDSamplerState state, float u, float v, float layer, float lod)
        {
            var sampler = device.SamplerCreate(state); using var source = new RDUniform { Binding = 7, UniformType = RenderingDevice.UniformType.SamplerWithTexture }; source.AddID(sampler); source.AddID(texture);
            var sourceSet = device.UniformSetCreate([source], shader, 3); float[] coords = [u, v, layer, lod]; device.BufferUpdate(parameters, 0, 16, MemoryMarshal.AsBytes(coords.AsSpan()));
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, sourceSet, 3); device.ComputeListBindUniformSet(list, parameterSet, 2); device.ComputeListBindUniformSet(list, targetSet, 9); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
            var value = MemoryMarshal.Cast<byte, float>(device.BufferGetData(output).AsSpan()).ToArray(); device.FreeRID(sampler); Check(!device.UniformSetIsValid(sourceSet), "Sampler free invalidates borrowing sets."); device.FreeRID(sourceSet); return value;
        }
        using var state = new RDSamplerState();
        Check(Sample(state, .75f, .25f, 0, 0)[0] == 1, "Nearest selects an individual pixel.");
        state.MinFilter = state.MagFilter = RenderingDevice.SamplerFilter.Linear; Check(Math.Abs(Sample(state, .5f, .5f, 0, 0)[0] - .5f) < .0001f, "Linear filtering interpolates neighboring pixels.");
        state.MinFilter = state.MagFilter = RenderingDevice.SamplerFilter.Nearest; state.RepeatU = RenderingDevice.SamplerRepeatMode.Repeat; Check(Sample(state, 1.25f, .25f, 0, 0)[0] == 0, "Repeat wraps out-of-range coordinates.");
        state.RepeatU = RenderingDevice.SamplerRepeatMode.MirroredRepeat; Check(Sample(state, 1.25f, .25f, 0, 0)[0] == 1, "Mirror repeat reverses alternating intervals.");
        state.RepeatU = RenderingDevice.SamplerRepeatMode.ClampToEdge; Check(Sample(state, 1.25f, .25f, 0, 0)[0] == 1, "Clamp selects the edge pixel.");
        Check(Sample(state, .25f, .25f, 1, 0)[2] == 1, "Array sampling selects the requested layer.");
        state.MipFilter = RenderingDevice.SamplerFilter.Linear; var mixed = Sample(state, .75f, .25f, 0, .5f); Check(Math.Abs(mixed[0] - .5f) < .0001f && Math.Abs(mixed[1] - .5f) < .0001f, "Mip filtering interpolates two mip levels.");
        state.MinLOD = state.MaxLOD = 1; Check(Sample(state, .75f, .25f, 0, 0)[1] == 1, "The minimum LOD clamps to the smaller mip.");
        state.MinLOD = state.MaxLOD = 0; Check(Sample(state, .75f, .25f, 0, 1)[0] == 1, "The maximum LOD clamps to the larger mip.");
        using var wrong = new RDTextureFormat { Format = RenderingDevice.DataFormat.R8G8B8A8UINT, UsageBits = RenderingDevice.TextureUsageBits.Sampling }; var integer = device.TextureCreate(wrong, view); var samplerRID = device.SamplerCreate(state);
        using var binding = new RDUniform { Binding = 7, UniformType = RenderingDevice.UniformType.SamplerWithTexture }; binding.AddID(samplerRID); binding.AddID(integer); Reject<ArgumentException>(() => device.UniformSetCreate([binding], shader, 3));
        device.FreeRID(integer); device.FreeRID(samplerRID); device.FreeRID(texture); device.FreeRID(output); device.FreeRID(parameters); device.FreeRID(targetSet); device.FreeRID(parameterSet); device.FreeRID(pipeline); device.FreeRID(shader);
        Console.WriteLine("Texture layers, nearest/linear, repeat/mirror/clamp and mip/LOD sampling passed.");
    }
    private static void IntegerImages(RenderingDevice device)
    {
        using var view = new RDTextureView(); using var format = new RDTextureFormat { Format = RenderingDevice.DataFormat.R8G8B8A8UINT, UsageBits = RenderingDevice.TextureUsageBits.Sampling };
        var unsigned = device.TextureCreate(format, view, [new byte[] { 201, 0, 0, 1 }]); format.Format = RenderingDevice.DataFormat.R8G8B8A8SINT; var signed = device.TextureCreate(format, view, [new byte[] { 254, 0, 0, 1 }]);
        format.Format = RenderingDevice.DataFormat.R32UINT; format.UsageBits = RenderingDevice.TextureUsageBits.Storage | RenderingDevice.TextureUsageBits.CPURead; var unsignedOutput = device.TextureCreate(format, view); format.Format = RenderingDevice.DataFormat.R32SINT; var signedOutput = device.TextureCreate(format, view);
        using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state); state.MinFilter = RenderingDevice.SamplerFilter.Linear; var linear = device.SamplerCreate(state);
        using var module = new RDShaderSPIRV(); using var stream = typeof(RenderingDeviceTextureTests).Assembly.GetManifestResourceStream("Electron2D.Compute.LocalTextureInteger.comp.spv")!; using var memory = new MemoryStream(); stream.CopyTo(memory); module.BytecodeCompute = memory.ToArray(); var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        using var u = new RDUniform { Binding = 7, UniformType = RenderingDevice.UniformType.SamplerWithTexture }; u.AddID(linear); u.AddID(unsigned);
        using var i = new RDUniform { Binding = 8, UniformType = RenderingDevice.UniformType.SamplerWithTexture }; i.AddID(sampler); i.AddID(signed); Reject<ArgumentException>(() => device.UniformSetCreate([u, i], shader, 3)); u.ClearIDs(); u.AddID(sampler); u.AddID(unsigned); var sourceSet = device.UniformSetCreate([u, i], shader, 3);
        using var outU = new RDUniform { Binding = 2, UniformType = RenderingDevice.UniformType.Image }; outU.AddID(unsignedOutput); using var outI = new RDUniform { Binding = 3, UniformType = RenderingDevice.UniformType.Image }; outI.AddID(signedOutput); var targetSet = device.UniformSetCreate([outU, outI], shader, 5);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, sourceSet, 3); device.ComputeListBindUniformSet(list, targetSet, 5); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
        Check(BitConverter.ToUInt32(device.TextureGetData(unsignedOutput, 0)) == 202 && BitConverter.ToInt32(device.TextureGetData(signedOutput, 0)) == -5, "Signed and unsigned sampled/storage images preserve their scalar contracts.");
        foreach (var rid in new[] { unsigned, signed, unsignedOutput, signedOutput, sampler, linear, sourceSet, targetSet, pipeline, shader }) device.FreeRID(rid);
        Console.WriteLine("Signed/unsigned image computation and integer filter validation passed.");
    }
    private static void CompressedTransfer(RenderingDevice device)
    {
        if (!device.TextureIsFormatSupportedForUsage(RenderingDevice.DataFormat.BC3UNORMBLOCK, RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CPURead)) return;
        using var format = new RDTextureFormat { Width = 5, Height = 3, Mipmaps = 3, Format = RenderingDevice.DataFormat.BC3UNORMBLOCK, UsageBits = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CPURead | RenderingDevice.TextureUsageBits.CanUpdate }; using var view = new RDTextureView();
        byte[] data = Enumerable.Range(0, 64).Select(x => (byte)x).ToArray(); var texture = device.TextureCreate(format, view, [data]); Check(device.TextureGetData(texture, 0).SequenceEqual(data), "Compressed partial blocks and small mips preserve exact bytes.");
        Array.Fill(data, (byte)11); device.TextureUpdate(texture, 0, data); byte[] result = new byte[64]; device.TextureGetDataAsync(texture, 0, result.AsMemory(), () => { }); device.Submit(); device.Sync(); Check(result.SequenceEqual(data), "Compressed asynchronous mip readback uses aligned physical storage."); device.FreeRID(texture);
    }
    private static void Cancellation()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice(); using var format = new RDTextureFormat { UsageBits = RenderingDevice.TextureUsageBits.CPURead }; using var view = new RDTextureView(); var texture = device.TextureCreate(format, view); bool called = false;
        device.TextureGetDataAsync(texture, 0, _ => called = true); device.Submit(); device.Dispose(); Check(!called, "Disposal cancels submitted image callbacks.");
    }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
