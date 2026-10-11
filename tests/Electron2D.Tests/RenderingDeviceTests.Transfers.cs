using System.Runtime.InteropServices;
using Electron2D;

internal static partial class RenderingDeviceTests
{
    private static void Transfers(RenderingDevice device, RID storage, RID shader)
    {
        byte[] seed = Enumerable.Range(0, 32).Select(i => (byte)(i + 1)).ToArray();
        var source = device.StorageBufferCreate(32, seed);
        var target = device.StorageBufferCreate(32, seed);
        var uniform = device.UniformBufferCreate(32, seed);
        device.BufferClear(target, 8, 16);
        device.BufferCopy(source, target, 4, 12, 8);
        var expected = seed.ToArray(); Array.Clear(expected, 8, 16); seed.AsSpan(4, 8).CopyTo(expected.AsSpan(12));
        Check(device.BufferGetData(target).SequenceEqual(expected), "GPU clear/copy preserves untouched bytes and operation ordering.");
        device.BufferCopy(source, source, 0, 16, 8);
        seed.AsSpan(0, 8).CopyTo(seed.AsSpan(16));
        Check(device.BufferGetData(source).SequenceEqual(seed), "Disjoint same-buffer copy works.");
        device.BufferCopy(source, uniform, 0, 0, 32);
        Check(device.BufferGetData(uniform).SequenceEqual(seed), "Storage-to-uniform copies synchronize the GPU source.");
        device.BufferCopy(uniform, target, 3, 5, 7);
        device.BufferClear(uniform, 0, 32);
        seed.AsSpan(3, 7).CopyTo(expected.AsSpan(5));
        Check(device.BufferGetData(target).SequenceEqual(expected), "Uniform upload snapshots its source before later host changes.");
        device.BufferCopy(target, source, 5, 3, 7);
        Check(device.BufferGetData(source, 3, 7).SequenceEqual(expected.AsSpan(5, 7).ToArray()), "GPU byte copies accept unaligned offsets and lengths.");
        device.BufferClear(source, 1, 4);
        Check(device.BufferGetData(source, 1, 4).All(b => b == 0), "Storage clear accepts a byte offset and a four-byte size.");
        device.BufferCopy(target, uniform, 0, 0, 32);
        device.BufferCopy(uniform, uniform, 0, 16, 8);
        expected.AsSpan(0, 8).CopyTo(expected.AsSpan(16));
        Check(device.BufferGetData(uniform).SequenceEqual(expected), "Uniform-to-uniform copy preserves byte data.");
        device.BufferClear(uniform, 1, 4); Array.Clear(expected, 1, 4);
        Check(device.BufferGetData(uniform).SequenceEqual(expected), "Uniform clear changes only its selected range.");
        device.BufferCopy(source, target, 32, 32, 0); device.BufferClear(source, 32, 0);
        Reject<ArgumentOutOfRangeException>(() => device.BufferCopy(source, target, uint.MaxValue, 0, 4));
        Reject<ArgumentOutOfRangeException>(() => device.BufferCopy(source, target, 0, 31, 4));
        Reject<ArgumentOutOfRangeException>(() => device.BufferClear(source, 0, 3));
        Reject<ArgumentOutOfRangeException>(() => device.BufferClear(source, 30, 4));
        Reject<ArgumentException>(() => device.BufferCopy(source, source, 0, 4, 8));
        Reject<ArgumentException>(() => device.BufferCopy(shader, target, 0, 0, 0));
        Reject<ArgumentException>(() => device.BufferClear(shader, 0, 0));
        using (var other = RenderingServer.CreateLocalRenderingDevice())
            Reject<ArgumentException>(() => other.BufferCopy(source, target, 0, 0, 4));
        device.ComputeListBegin();
        Reject<InvalidOperationException>(() => device.BufferCopy(source, target, 0, 0, 0));
        Reject<InvalidOperationException>(() => device.BufferClear(uniform, 0, 0));
        device.ComputeListEnd();
        device.FreeRID(source); device.FreeRID(target); device.FreeRID(uniform);
        Reject<ArgumentException>(() => device.BufferCopy(source, storage, 0, 0, 0));
    }

    private static void Indirect(RenderingDevice device, RID pipeline, RID dataSet, RID parameters, RID valuesBuffer, RID uniformBuffer)
    {
        using var module = new RDShaderSPIRV();
        using var input = typeof(RenderingDeviceTests).Assembly.GetManifestResourceStream("Electron2D.Compute.IndirectArguments.comp.spv")!;
        using var stream = new MemoryStream(); input.CopyTo(stream); module.BytecodeCompute = stream.ToArray();
        var shader = device.ShaderCreateFromSPIRV(module); var producer = device.ComputePipelineCreate(shader);
        var arguments = device.StorageBufferCreate(32, usage: RenderingDevice.StorageBufferUsage.DispatchIndirect);
        var copied = device.StorageBufferCreate(12, usage: RenderingDevice.StorageBufferUsage.DispatchIndirect);
        using var valuesBinding = new RDUniform { Binding = 0, UniformType = RenderingDevice.UniformType.StorageBuffer }; valuesBinding.AddID(valuesBuffer);
        using var argumentsBinding = new RDUniform { Binding = 1, UniformType = RenderingDevice.UniformType.StorageBuffer }; argumentsBinding.AddID(arguments);
        var producerSet = device.UniformSetCreate([valuesBinding, argumentsBinding], shader, 0);
        float[] values = new float[1024]; values[0] = 128;
        float[] settings = [1, 1024, 1, 0];
        device.BufferUpdate(uniformBuffer, 0, 16, MemoryMarshal.AsBytes(settings.AsSpan()));
        long Produce()
        {
            device.BufferUpdate(valuesBuffer, 0, (uint)(values.Length * 4), MemoryMarshal.AsBytes(values.AsSpan()));
            device.BufferClear(arguments, 0, 32);
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, producer);
            device.ComputeListBindUniformSet(list, producerSet, 0); device.ComputeListDispatch(list, 1, 1, 1);
            return list;
        }
        void Consume(long list, RID buffer, uint offset)
        {
            device.ComputeListBindComputePipeline(list, pipeline);
            device.ComputeListBindUniformSet(list, dataSet, 3); device.ComputeListBindUniformSet(list, parameters, 2);
            device.ComputeListDispatchIndirect(list, buffer, offset); device.ComputeListEnd();
        }
        Produce();
        device.ComputeListEnd();
        device.BufferCopy(arguments, copied, 16, 0, 12);
        var commands = device.ComputeListBegin(); Consume(commands, copied, 0);
        var output = new float[1024]; device.BufferGetData(valuesBuffer, MemoryMarshal.AsBytes(output.AsSpan()));
        Check(output[0] == 129 && output.Skip(1).Take(127).All(v => v == 1) && output.Skip(128).All(v => v == 0), "GPU-produced and copied group counts drive exactly two workgroups.");
        void Chain()
        {
            var list = Produce(); Consume(list, arguments, 16);
            device.BufferCopy(valuesBuffer, copied, 0, 0, 12);
            device.BufferGetData(valuesBuffer, MemoryMarshal.AsBytes(output.AsSpan()));
        }
        for (var i = 0; i < 16; i++) Chain();
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 32; i++) Chain();
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Prepared clear/copy and GPU-produced indirect chain allocate no owner-thread managed memory.");
        Check(output[127] == 1 && output[128] == 0, "In-list indirect dispatch observes the immediately preceding GPU write.");
        values[0] = 0; Chain(); Check(output.All(v => v == 0), "A GPU-produced zero group count skips the consumer.");
        Reject<ArgumentOutOfRangeException>(() => device.StorageBufferCreate(16, usage: (RenderingDevice.StorageBufferUsage)2));
        var invalid = device.StorageBufferCreate(12);
        commands = device.ComputeListBegin();
        Reject<ArgumentException>(() => device.ComputeListDispatchIndirect(commands, invalid, 0));
        Reject<ArgumentOutOfRangeException>(() => device.ComputeListDispatchIndirect(commands, arguments, 1));
        Reject<ArgumentOutOfRangeException>(() => device.ComputeListDispatchIndirect(commands, copied, 4));
        Reject<ArgumentOutOfRangeException>(() => device.ComputeListDispatchIndirect(commands, arguments, uint.MaxValue));
        Reject<InvalidOperationException>(() => device.ComputeListDispatchIndirect(commands, arguments, 0));
        device.ComputeListBindComputePipeline(commands, pipeline);
        Reject<InvalidOperationException>(() => device.ComputeListDispatchIndirect(commands, arguments, 0));
        device.ComputeListEnd();
        Reject<ArgumentException>(() => device.ComputeListDispatchIndirect(commands, arguments, 0));
        device.FreeRID(arguments);
        commands = device.ComputeListBegin(); Reject<ArgumentException>(() => device.ComputeListDispatchIndirect(commands, arguments, 0)); device.ComputeListEnd();
        device.FreeRID(copied); device.FreeRID(invalid); device.FreeRID(shader); device.FreeRID(producer); device.FreeRID(producerSet);
    }
}
