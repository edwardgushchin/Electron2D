using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class NarrowInteger
{
    private static void Profiles(RenderingDevice device, string mode)
    {
        var mask = mode.StartsWith("unavailable-", StringComparison.Ordinal) ? mode[12..] : "";
        foreach (var name in new[] { "arithmetic", "storage", "uniform", "push", "floating", "legacy" })
        {
            var available = name == "legacy" || (mask switch { "all" or "api" => false, "integer" => name is "storage" or "uniform" or "floating", "arithmetic" => name != "floating", "buffer" => name is "arithmetic" or "push" or "floating", "uniform" => name != "uniform", "push" => name != "push", _ => true });
            using var code = Code(name + (name == "legacy" ? ".comp.hlsl" : ".comp.glsl"));
            if (!available) { Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(code)); Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(code)); continue; }
            var shader = device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(code)); var pipeline = device.ComputePipelineCreate(shader); var output = device.StorageBufferCreate(8);
            ushort[] bits = [0x8000, 1, 0x7e55, 0x7bff]; var parameters = new short[8]; parameters[0] = -123;
            var input = name == "uniform" ? device.UniformBufferCreate(16, MemoryMarshal.AsBytes(parameters.AsSpan())) : device.StorageBufferCreate(8, MemoryMarshal.AsBytes(bits.AsSpan()));
            using var from = new RDUniform { Binding = 0, UniformType = name == "uniform" ? UniformType.UniformBuffer : UniformType.StorageBuffer }; from.AddID(input);
            using var to = new RDUniform { Binding = name is "storage" or "uniform" ? 1 : 0, UniformType = UniformType.StorageBuffer }; to.AddID(output);
            var set = device.UniformSetCreate(name is "storage" or "uniform" ? [from, to] : [to], shader, 0);
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0);
            if (name == "push") device.ComputeListSetPushConstant(list, MemoryMarshal.AsBytes(parameters.AsSpan()), 2);
            device.ComputeListDispatch(list, name == "storage" ? 4u : 1u, 1, 1); device.ComputeListEnd(); var result = device.BufferGetData(output);
            Check(name == "storage" ? MemoryMarshal.Cast<byte, ushort>(result).SequenceEqual(bits) : name is "legacy" or "floating" ? BitConverter.ToSingle(result) == (name == "legacy" ? 1.0001220703125f : .625f) : BitConverter.ToInt32(result) == (name == "arithmetic" ? -369 : -123), "Independent integer arithmetic/storage/push profile executes exact output.");
        }
        if (mask == "io")
        {
            byte[] Read(string stage) => File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "io." + stage + ".glsl.spv"));
            using var code = new RDShaderSPIRV { BytecodeVertex = Read("vert"), BytecodeFragment = Read("frag") }; Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(code)); Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(code));
        }
        Console.WriteLine("PASS independent shader16 arithmetic, buffer/uniform/push profiles and refusal before shader/binary publication: " + mode);
    }
}
