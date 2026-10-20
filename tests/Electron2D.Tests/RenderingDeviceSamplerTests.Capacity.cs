using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceSamplerTests
{
    private static void Capacity(RenderingDevice device)
    {
        string Source(int count) => "#version 450\nlayout(local_size_x=1) in;\nlayout(set=3,binding=7) uniform texture2D image;\nlayout(set=9,binding=4,std430) buffer Result { vec4 value; };\nlayout(constant_id=2) const float gain=1;\n" + string.Join('\n', Enumerable.Range(0, count).Select(i => $"layout(set=2,binding={i}) uniform sampler state_{i};")) + "\nvoid main() {value=vec4(0);" + string.Join('\n', Enumerable.Range(0, count).Select(i => $"value+=textureLod(sampler2D(image,state_{i}),vec2(.5),0)*gain/{count}.0;")) + "}";
        using var description = new RDShaderSource { SourceCompute = Source(16) }; using var module = device.ShaderCompileSPIRVFromSource(description, false); Check(module.CompileErrorCompute == "", module.CompileErrorCompute); Export(module, "Capacity16"); var shader = device.ShaderCreateFromSPIRV(module);
        using var gain = new RDPipelineSpecializationConstant<float> { ConstantID = 2, Value = .5f }; var pipeline = device.ComputePipelineCreate(shader, [gain]); gain.Value = 9;
        var image = Image(device, 1, 1, [255, 255, 255, 255]); using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state); var output = device.StorageBufferCreate(16);
        var uniforms = Enumerable.Range(0, 16).Select(i => new RDUniform { Binding = i, UniformType = UniformType.Sampler }).ToArray(); foreach (var uniform in uniforms) uniform.AddID(sampler); var samplerSet = device.UniformSetCreate(uniforms, shader, 2); foreach (var uniform in uniforms) uniform.Dispose();
        var imageSet = Set(device, shader, 3, 7, UniformType.Texture, image); var outputSet = Set(device, shader, 9, 4, UniformType.StorageBuffer, output); var data = new byte[16];
        void Execute()
        {
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, samplerSet, 2); device.ComputeListBindUniformSet(list, imageSet, 3); device.ComputeListBindUniformSet(list, outputSet, 9); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(output, data.AsSpan());
            var values = MemoryMarshal.Cast<byte, float>(data.AsSpan()); Check(values[0] == .5f && values[3] == .5f, "All sixteen pairs execute their copied specialization value.");
        }
        Execute(); for (var i = 0; i < 8; i++) Execute(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Execute(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Sixteen-pair specialized recording/readback allocates zero managed owner bytes.");
        description.SourceCompute = Source(17); using var rejected = device.ShaderCompileSPIRVFromSource(description, false); Check(rejected.BytecodeCompute.Length == 0 && rejected.CompileErrorCompute.Contains("sixteen", StringComparison.OrdinalIgnoreCase), "The seventeenth actual pair rejects before native creation with its capacity diagnostic.");
        foreach (var rid in new[] { samplerSet, imageSet, outputSet, pipeline, shader, image, sampler, output }) device.FreeRID(rid);
        Console.WriteLine("Native sampler capacity: sixteen actual pairs/specialization/zero owner bytes passed; seventeen rejects explicitly.");
    }
}
