using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static class GPUPhysicsPairHashTests
{
    internal static void Run()
    {
        const int capacity = 65536, count = 4096;
        var expected = new uint[capacity]; var keys = new List<uint>(count * 2);
        void Add(uint low, uint high)
        {
            if ((low | high) == 0 || keys.Count == count * 2) return;
            var key = ((ulong)high << 32) | low;
            key ^= key >> 33; key = unchecked(key * 0xff51afd7ed558ccdUL);
            key ^= key >> 33; key = unchecked(key * 0xc4ceb9fe1a85ec53UL);
            var slot = (uint)(key ^ (key >> 33)) & (capacity - 1);
            // Distinct home slots make parallel insertion order irrelevant to the oracle.
            if (expected[slot] != 0) return;
            expected[slot] = (uint)(keys.Count / 2 + 1); keys.Add(low); keys.Add(high);
        }
        uint[] edges = [0, 1, 0xffff, 0x10000, 0x7fffffff, 0x80000000, 0xfffeffff, uint.MaxValue];
        foreach (var low in edges) foreach (var high in edges) Add(low, high);
        var random = new Random(127);
        while (keys.Count < count * 2) Add((uint)random.NextInt64(1L << 32), (uint)random.NextInt64(1L << 32));

        using var device = RenderingServer.CreateLocalRenderingDevice();
        using var input = typeof(PhysicsServer).Assembly.GetManifestResourceStream("Electron2D.PhysicsShaders.PhysicsPairTable.comp.spv")!;
        using var bytes = new MemoryStream(); input.CopyTo(bytes);
        using var module = new RDShaderSPIRV { BytecodeCompute = bytes.ToArray() };
        var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        var updates = device.StorageBufferCreate(16, new byte[16]);
        var keyBuffer = device.StorageBufferCreate(count * 8, MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(keys)));
        var slots = device.StorageBufferCreate(capacity * 4, new byte[capacity * 4]);
        var status = device.StorageBufferCreate(4, new byte[4]);
        uint[] settings = [count, count, capacity - 1, 3, 0, 0, 0, 0];
        var parameters = device.UniformBufferCreate(32, MemoryMarshal.AsBytes(settings.AsSpan()));
        using var updateBinding = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; updateBinding.AddID(updates);
        using var keyBinding = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; keyBinding.AddID(keyBuffer);
        using var slotBinding = new RDUniform { Binding = 1, UniformType = UniformType.StorageBuffer }; slotBinding.AddID(slots);
        using var statusBinding = new RDUniform { Binding = 2, UniformType = UniformType.StorageBuffer }; statusBinding.AddID(status);
        using var parameterBinding = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; parameterBinding.AddID(parameters);
        var updateSet = device.UniformSetCreate([updateBinding], shader, 0);
        var tableSet = device.UniformSetCreate([keyBinding, slotBinding, statusBinding], shader, 1);
        var parameterSet = device.UniformSetCreate([parameterBinding], shader, 2);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline);
        device.ComputeListBindUniformSet(list, updateSet, 0); device.ComputeListBindUniformSet(list, tableSet, 1); device.ComputeListBindUniformSet(list, parameterSet, 2);
        device.ComputeListDispatch(list, count / 64, 1, 1); device.ComputeListEnd();
        var actual = new uint[capacity]; device.BufferGetData(slots, MemoryMarshal.AsBytes(actual.AsSpan()));
        Span<uint> error = stackalloc uint[1]; device.BufferGetData(status, MemoryMarshal.AsBytes(error));
        if (error[0] != 0 || !actual.SequenceEqual(expected)) throw new InvalidOperationException("GPU pair hashing must preserve the canonical 64-bit hash, including half-word carries and high-bit keys.");
        Console.WriteLine($"GPU pair hash: {count} boundary/random keys occupy their canonical slots on {device.GetDeviceName()}.");
    }
}
