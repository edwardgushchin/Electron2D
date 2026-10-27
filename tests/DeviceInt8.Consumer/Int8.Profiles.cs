using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class ByteInteger
{
    private static void Profiles(RenderingDevice device, string mode)
    {
        var mask = mode.StartsWith("unavailable-", StringComparison.Ordinal) ? mode[12..] : "";
        foreach (var name in new[] { "arithmetic", "storage", "uniform", "push", "integer16", "floating", "legacy" })
        {
            var available = name == "legacy" || (mask switch
            {
                "api" => false,
                "all" => name is "integer16" or "floating",
                "arithmetic" => name is "storage" or "uniform" or "integer16" or "floating",
                "buffer" => name is "arithmetic" or "push" or "integer16" or "floating",
                "uniform" => name != "uniform",
                "push" => name != "push",
                "int16" => name != "integer16",
                "float16" => name != "floating",
                _ => true
            });
            using var code = Code(name + (name == "legacy" ? ".comp.hlsl" : ".comp.glsl"));
            if (!available) { Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(code)); Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(code)); continue; }
            var shader = device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(code)); var pipeline = device.ComputePipelineCreate(shader); var output = device.StorageBufferCreate(4);
            byte[] bits = [0x80, 1, 0xff, 0x7f]; byte[] parameters = new byte[16]; parameters[0] = 0x85;
            var input = name == "uniform" ? device.UniformBufferCreate(16, parameters) : device.StorageBufferCreate(4, bits);
            using var from = new RDUniform { Binding = 0, UniformType = name == "uniform" ? UniformType.UniformBuffer : UniformType.StorageBuffer }; from.AddID(input);
            using var to = new RDUniform { Binding = name is "storage" or "uniform" ? 1 : 0, UniformType = UniformType.StorageBuffer }; to.AddID(output);
            var set = device.UniformSetCreate(name is "storage" or "uniform" ? [from, to] : [to], shader, 0);
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0);
            if (name == "push") device.ComputeListSetPushConstant(list, parameters, 1);
            device.ComputeListDispatch(list, name == "storage" ? 4u : 1u, 1, 1); device.ComputeListEnd(); var result = device.BufferGetData(output);
            Check(name == "storage" ? result.SequenceEqual(bits) : name is "legacy" or "floating" ? BitConverter.ToSingle(result) == (name == "legacy" ? 1.0001220703125f : .625f) : BitConverter.ToInt32(result) == (name == "arithmetic" ? -21 : name == "integer16" ? -369 : -123), "Independent byte arithmetic/storage and 16-bit siblings execute exact output.");
        }
        if (mask.Length > 0)
        {
            using var mixed = Code("data.comp.glsl"); Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(mixed)); Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(mixed));
        }
        Console.WriteLine("PASS independent Int8 arithmetic, buffer/uniform/push, Int16/Float16 siblings and mixed-module refusal: " + mode);
    }
}
