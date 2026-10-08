using System.Runtime.InteropServices;
using Electron2D;

internal static class RenderingDeviceTests
{
    internal static void Run()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        Span<float> settings = stackalloc float[4];
        foreach (var moduleName in new[] { "LocalCompute.comp", "LocalComputeHLSL.comp" })
        {
            using var module = new RDShaderSPIRV();
            using var input = typeof(RenderingDeviceTests).Assembly.GetManifestResourceStream("Electron2D.Compute." + moduleName + ".spv")!;
            using var stream = new MemoryStream(); input.CopyTo(stream); module.BytecodeCompute = stream.ToArray();
            using var duplicate = (RDShaderSPIRV)module.Duplicate();
            Check(duplicate.BytecodeCompute.SequenceEqual(module.BytecodeCompute), "Compiled shader duplication copies bytecode.");
            duplicate.BytecodeCompute = [];
            Check(module.BytecodeCompute.Length > 0, "Shader copies have independent storage.");
            var archive = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-compute-" + Guid.NewGuid().ToString("N") + ".e2dres");
            try
            {
                ResourceSaver.Save(module, archive);
                using var restored = ResourceLoader.Load<RDShaderSPIRV>(archive, ResourceLoader.CacheMode.Ignore);
                Check(restored.BytecodeCompute.SequenceEqual(module.BytecodeCompute), "Compiled compute resources round-trip through public resource storage.");
            }
            finally { if (File.Exists(archive)) File.Delete(archive); }
            var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
            float[] values = Enumerable.Range(0, 1024).Select(i => (float)i).ToArray();
            var buffer = device.StorageBufferCreate((uint)(values.Length * 4), MemoryMarshal.AsBytes(values.AsSpan()));
            var uniformBuffer = device.UniformBufferCreate(16);
            using var dataBinding = new RDUniform { Binding = 7, UniformType = RenderingDevice.UniformType.StorageBuffer }; dataBinding.AddID(buffer);
            using var parameterBinding = new RDUniform { Binding = 4, UniformType = RenderingDevice.UniformType.UniformBuffer }; parameterBinding.AddID(uniformBuffer);
            var dataSet = device.UniformSetCreate([dataBinding], shader, 3); var parameters = device.UniformSetCreate([parameterBinding], shader, 2);
            settings[0] = 2; settings[1] = values.Length; settings[2] = 3; settings[3] = 0;
            device.BufferUpdate(uniformBuffer, 0, 16, MemoryMarshal.AsBytes(settings));
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline);
            device.ComputeListBindUniformSet(list, dataSet, 3); device.ComputeListBindUniformSet(list, parameters, 2);
            device.ComputeListDispatch(list, 16, 1, 1); device.ComputeListAddBarrier(list);
            settings[0] = 3; settings[2] = -1;
            device.BufferUpdate(uniformBuffer, 0, 16, MemoryMarshal.AsBytes(settings));
            device.ComputeListDispatch(list, 16, 1, 1); device.ComputeListEnd(); device.Submit(); device.Sync();
            device.BufferGetData(buffer, MemoryMarshal.AsBytes(values.AsSpan()));
            for (var i = 0; i < values.Length; i++) Check(values[i] == i * 6 + 8, "GPU compute dispatch and barriers preserve uploaded data.");
            settings[0] = 1; settings[2] = 0;
            device.BufferUpdate(uniformBuffer, 0, 16, MemoryMarshal.AsBytes(settings));
            void IdentityPass()
            {
                var commands = device.ComputeListBegin(); device.ComputeListBindComputePipeline(commands, pipeline);
                device.ComputeListBindUniformSet(commands, dataSet, 3); device.ComputeListBindUniformSet(commands, parameters, 2);
                device.ComputeListDispatch(commands, 16, 1, 1); device.ComputeListEnd();
                device.BufferGetData(buffer, MemoryMarshal.AsBytes(values.AsSpan()));
            }
            for (var i = 0; i < 16; i++) IdentityPass();
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 32; i++) IdentityPass();
            Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Prepared device dispatch and span readback allocate no owner-thread managed memory.");
            Reject<ArgumentOutOfRangeException>(() => device.BufferGetData(buffer, uint.MaxValue));
            Reject<ArgumentException>(() => device.UniformSetCreate([dataBinding, dataBinding], shader, 3));
            Reject<InvalidOperationException>(() => Task.Run(() => device.GetDeviceName()).GetAwaiter().GetResult());
            device.FreeRID(buffer); Check(!device.UniformSetIsValid(dataSet), "Freed buffer invalidates its set.");
            device.FreeRID(shader); Check(!device.ComputePipelineIsValid(pipeline), "Freed shader invalidates its pipeline.");
        }
        Console.WriteLine($"GLSL/HLSL local compute, readback, barriers, descriptor remapping and lifetime passed on {device.GetDeviceName()}.");
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
}
