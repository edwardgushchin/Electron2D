using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceSamplerTests
{
    private static void Depth(RenderingDevice device)
    {
        using var format = new RDTextureFormat { Format = DataFormat.D32SFLOAT, UsageBits = TextureUsageBits.Sampling | TextureUsageBits.DepthStencilAttachment }; using var view = new RDTextureView(); var depth = device.TextureCreate(format, view); var framebuffer = device.FramebufferCreate([depth]); device.DrawListBegin(framebuffer, DrawFlags.ClearDepth, clearDepthValue: .5f); device.DrawListEnd();
        using var state = new RDSamplerState { EnableCompare = true, CompareOp = CompareOperator.Less }; var sampler = device.SamplerCreate(state); var output = device.StorageBufferCreate(16);
        foreach (var language in new[] { ShaderLanguage.GLSL, ShaderLanguage.HLSL })
        {
            using var source = new RDShaderSource { Language = language, SourceCompute = language == ShaderLanguage.GLSL ? "#version 450\n#extension GL_EXT_samplerless_texture_functions : require\nlayout(local_size_x=1) in;layout(set=3,binding=7) uniform texture2D image;layout(set=2,binding=5) uniform samplerShadow state;layout(set=9,binding=4,std430) buffer Result {vec4 value;};void main(){value=vec4(textureLod(sampler2DShadow(image,state),vec3(.5,.5,.25),0),textureLod(sampler2DShadow(image,state),vec3(.5,.5,.75),0),textureSize(image,0).x,texelFetch(image,ivec2(0),0).r);}" : "Texture2D<float> image:register(t7,space3);SamplerComparisonState state:register(s5,space2);RWStructuredBuffer<float4> value:register(u4,space9);[numthreads(1,1,1)]void main(){uint w,h;image.GetDimensions(w,h);value[0]=float4(image.SampleCmpLevelZero(state,float2(.5,.5),.25),image.SampleCmpLevelZero(state,float2(.5,.5),.75),w,image.Load(int3(0,0,0)));}" };
            using var module = device.ShaderCompileSPIRVFromSource(source, false); Check(module.CompileErrorCompute == "", module.CompileErrorCompute); Export(module, "Depth" + language); var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader); var imageSet = Set(device, shader, 3, 7, UniformType.Texture, depth); var samplerSet = Set(device, shader, 2, 5, UniformType.Sampler, sampler); var outputSet = Set(device, shader, 9, 4, UniformType.StorageBuffer, output); var data = new byte[16];
            void Execute()
            {
                var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, imageSet, 3); device.ComputeListBindUniformSet(list, samplerSet, 2); device.ComputeListBindUniformSet(list, outputSet, 9); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(output, data.AsSpan()); var values = MemoryMarshal.Cast<byte, float>(data.AsSpan()); Check(values[0] == 1 && values[1] == 0 && values[2] == 1 && values[3] == .5f, "Separate depth comparison, dimensions and fetch share their real depth image.");
            }
            for (var i = 0; i < 8; i++) Execute(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Execute(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared independent comparison sampling allocates zero managed owner bytes.");
            foreach (var rid in new[] { imageSet, samplerSet, outputSet, pipeline, shader }) device.FreeRID(rid);
            DepthGraphics(device, depth, sampler, language);
            Console.WriteLine(language + ": independent depth comparison/fetch/query and 32 zero-allocation cycles passed.");
        }
        foreach (var rid in new[] { framebuffer, depth, sampler, output }) device.FreeRID(rid);
    }
    private static void DepthGraphics(RenderingDevice device, RID depth, RID sampler, ShaderLanguage language)
    {
        using var source = new RDShaderSource
        {
            Language = language,
            SourceVertex = language == ShaderLanguage.GLSL ? "#version 450\nvoid main(){vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2);gl_Position=vec4(p*2-1,0,1);}" : "float4 main(uint id:SV_VertexID):SV_Position{float2 p=float2((id<<1)&2,id&2);return float4(p*2-1,0,1);}",
            SourceFragment = language == ShaderLanguage.GLSL ? "#version 450\nlayout(set=3,binding=7)uniform texture2D image;layout(set=2,binding=5)uniform samplerShadow state;layout(location=0)out vec4 color;void main(){color=vec4(texture(sampler2DShadow(image,state),vec3(.5,.5,.25)),0,0,1);}" : "Texture2D<float> image:register(t7,space3);SamplerComparisonState state:register(s5,space2);float4 main():SV_Target0{return float4(image.SampleCmp(state,float2(.5,.5),.25),0,0,1);}"
        };
        using var module = device.ShaderCompileSPIRVFromSource(source, false); Check(module.CompileErrorVertex == "" && module.CompileErrorFragment == "", module.CompileErrorVertex + module.CompileErrorFragment); Export(module, "DepthGraphics" + language); var shader = device.ShaderCreateFromSPIRV(module);
        using var format = new RDTextureFormat { Width = 2, Height = 2, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView(); var color = device.TextureCreate(format, view); var framebuffer = device.FramebufferCreate([color]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend); var imageSet = Set(device, shader, 3, 7, UniformType.Texture, depth); var samplerSet = Set(device, shader, 2, 5, UniformType.Sampler, sampler); var data = new byte[16];
        void Execute() { var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, imageSet, 3); device.DrawListBindUniformSet(list, samplerSet, 2); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.TextureGetData(color, 0, data.AsSpan()); Check(data[0] == 255 && data[1] == 0 && data[2] == 0 && data[3] == 255, "Implicit comparison sampling reaches actual graphics pixels."); }
        for (var i = 0; i < 8; i++) Execute(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Execute(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared implicit comparison graphics uses zero managed owner bytes.");
        foreach (var rid in new[] { imageSet, samplerSet, pipeline, framebuffer, color, shader }) device.FreeRID(rid);
    }

}
