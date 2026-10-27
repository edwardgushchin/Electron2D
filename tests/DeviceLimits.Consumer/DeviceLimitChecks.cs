using Electron2D;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using static Electron2D.RenderingDevice;

internal static partial class DeviceLimitChecks
{
    internal static void Run()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        var selectors = Enum.GetValues<Limit>();
        foreach (var selector in selectors) { var value = device.LimitGet(selector); Check(value > 0, selector.ToString()); Console.WriteLine($"{selector}={value}"); }
        Reject<ArgumentOutOfRangeException>(() => device.LimitGet((Limit)(-1)));
        Reject<ArgumentOutOfRangeException>(() => device.LimitGet((Limit)11));
        Reject<InvalidOperationException>(() => Task.Run(() => device.LimitGet(Limit.MaxTextureSize2D)).GetAwaiter().GetResult());
        using (var other = RenderingServer.CreateLocalRenderingDevice())
            foreach (var selector in selectors) Check(other.LimitGet(selector) == device.LimitGet(selector), "Shared native device capacities agree.");
        for (var i = 0; i < 64; i++) foreach (var selector in selectors) _ = device.LimitGet(selector);
        var before = GC.GetAllocatedBytesForCurrentThread(); long sum = 0;
        for (var i = 0; i < 1024; i++) foreach (var selector in selectors) sum += device.LimitGet(selector);
        Check(sum > 0 && GC.GetAllocatedBytesForCurrentThread() == before, "Prepared native limit queries allocate zero managed bytes.");
        var uniform = device.UniformBufferCreate((uint)device.LimitGet(Limit.MaxUniformBufferSize)); device.FreeRID(uniform);
        Reject<ArgumentOutOfRangeException>(() => device.UniformBufferCreate((uint)device.LimitGet(Limit.MaxUniformBufferSize) + 4));
        Compute(device); Console.WriteLine("Native compute capacities passed.");
        Textures(device); Console.WriteLine("Native texture extents passed.");
        Graphics(device); Console.WriteLine("Native graphics capacities passed.");
        device.Dispose(); Reject<ObjectDisposedException>(() => device.LimitGet(Limit.MaxComputeWorkgroupInvocations));
        Console.WriteLine("Effective limits: queries, native compute/graphics boundaries, resource extents, ownership and prepared zero-allocation reads passed.");
    }
    private static void Compute(RenderingDevice device)
    {
        var code = Read("LimitWork.comp"); var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan());
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
            if ((words[at] & 65535) == 71 && words[at] >> 16 == 4 && words[at + 2] == 34) words[at + 3] = 1000;
        using var module = new RDShaderSPIRV { BytecodeCompute = code };
        var shader = device.ShaderCreateFromSPIRV(module); var output = device.StorageBufferCreate(16);
        using var binding = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; binding.AddID(output);
        var set = device.UniformSetCreate([binding], shader, 1000); var bytes = new byte[16];
        using var x = new RDPipelineSpecializationConstant<uint> { ConstantID = 0, Value = 1 };
        using var y = new RDPipelineSpecializationConstant<uint> { ConstantID = 1, Value = 1 };
        using var z = new RDPipelineSpecializationConstant<uint> { ConstantID = 2, Value = 1 };
        using var shared = new RDPipelineSpecializationConstant<uint> { ConstantID = 3, Value = 1 };
        RID Pipeline() => device.ComputePipelineCreate(shader, [x, y, z, shared]);
        void Execute(RID pipeline, uint groupsX, uint groupsY, uint groupsZ)
        {
            device.BufferClear(output, 0, 16);
            var list = device.ComputeListBegin();
            try { device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 1000); device.ComputeListDispatch(list, groupsX, groupsY, groupsZ); }
            finally { device.ComputeListEnd(); }
            device.BufferGetData(output, bytes.AsSpan());
            var groups = groupsX * groupsY * groupsZ; var count = shared.Value!.Value;
            Check(BinaryPrimitives.ReadUInt32LittleEndian(bytes) == groups && BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)) == (uint)((ulong)groups * count * (count + 1) / 2) && BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8)) == x.Value * y.Value * z.Value, "Queried workgroup/shared capacities execute real native arithmetic.");
        }
        foreach (var selector in new[] { Limit.MaxComputeWorkgroupSizeX, Limit.MaxComputeWorkgroupSizeY, Limit.MaxComputeWorkgroupSizeZ })
        {
            x.Value = y.Value = z.Value = 1; var size = (uint)Math.Min(device.LimitGet(selector), device.LimitGet(Limit.MaxComputeWorkgroupInvocations));
            if (selector == Limit.MaxComputeWorkgroupSizeX) x.Value = size; else if (selector == Limit.MaxComputeWorkgroupSizeY) y.Value = size; else z.Value = size;
            var pipeline = Pipeline(); Execute(pipeline, 2, 1, 1); device.FreeRID(pipeline);
        }
        x.Value = y.Value = z.Value = 1; shared.Value = (uint)device.LimitGet(Limit.MaxComputeSharedMemorySize) / 4;
        var sharedPipeline = Pipeline(); Execute(sharedPipeline, 1, 1, 1); device.FreeRID(sharedPipeline); shared.Value = 1;
        var ordinary = Pipeline();
        Execute(ordinary, (uint)device.LimitGet(Limit.MaxComputeWorkgroupCountX), 1, 1);
        Execute(ordinary, 1, (uint)device.LimitGet(Limit.MaxComputeWorkgroupCountY), 1);
        Execute(ordinary, 1, 1, (uint)device.LimitGet(Limit.MaxComputeWorkgroupCountZ));
        var bad = device.ComputeListBegin(); device.ComputeListBindComputePipeline(bad, ordinary); device.ComputeListBindUniformSet(bad, set, 1000);
        Reject<ArgumentOutOfRangeException>(() => device.ComputeListDispatch(bad, 0, 1, 1));
        Reject<ArgumentOutOfRangeException>(() => device.ComputeListDispatch(bad, 1, 1, (uint)device.LimitGet(Limit.MaxComputeWorkgroupCountZ) + 1)); device.ComputeListEnd();
        z.Value = (uint)device.LimitGet(Limit.MaxComputeWorkgroupSizeZ) + 1;
        Reject<ArgumentException>(() => Pipeline());
        device.FreeRID(ordinary); device.FreeRID(output); device.FreeRID(shader);
    }
    private static void Textures(RenderingDevice device)
    {
        using var view = new RDTextureView();
        using var format = new RDTextureFormat { Width = (uint)device.LimitGet(Limit.MaxTextureSize2D), Format = DataFormat.R8UNORM, UsageBits = TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling };
        var wide = device.TextureCreate(format, view); Check(device.TextureGetData(wide, 0).Length == format.Width, "Maximum queried image extent allocates and reads native storage."); device.FreeRID(wide);
        format.Width++; Reject<ArgumentOutOfRangeException>(() => device.TextureCreate(format, view));
        format.Width = 1; format.TextureType = TextureType.TextureArray; format.ArrayLayers = (uint)device.LimitGet(Limit.MaxTextureArrayLayers);
        var array = device.TextureCreate(format, view); var one = new byte[1]; device.TextureGetData(array, format.ArrayLayers - 1, one.AsSpan()); Check(one[0] == 0, "Last layer of the maximum queried array remains addressable."); device.FreeRID(array);
        format.ArrayLayers++; Reject<ArgumentOutOfRangeException>(() => device.TextureCreate(format, view));
    }
    private static byte[] Read(string name) { using var stream = typeof(DeviceLimitChecks).Assembly.GetManifestResourceStream("Electron2D.Device." + name + ".spv")!; using var output = new MemoryStream(); stream.CopyTo(output); return output.ToArray(); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
