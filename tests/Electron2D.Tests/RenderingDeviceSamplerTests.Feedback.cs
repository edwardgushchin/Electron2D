using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceSamplerTests
{
    private static void Feedback(RenderingDevice device)
    {
        const string vertex = "Texture2D<float4> image:register(t7,space3);SamplerState state:register(s5,space2);struct Vertex{float4 position:SV_Position;float4 tint:TEXCOORD0;float2 uv:TEXCOORD1;};Vertex main(uint id:SV_VertexID){Vertex output;float2 p=float2((id<<1)&2,id&2);output.position=float4(p*2-1,0,1);output.uv=p;output.tint=image.SampleLevel(state,float2(.75,.25),0);return output;}";
        using var source = new RDShaderSource { Language = ShaderLanguage.HLSL, SourceVertex = vertex }; using var compiled = device.ShaderCompileSPIRVFromSource(source, false); Check(compiled.CompileErrorVertex == "", compiled.CompileErrorVertex); using var module = RenderingDeviceGraphicsTests.Load("DeviceSamplersHLSL"); module.BytecodeVertex = compiled.BytecodeVertex; Export(module, "IndependentVertex"); var shader = device.ShaderCreateFromSPIRV(module);
        using var format = new RDTextureFormat { Width = 2, Height = 2, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.Sampling | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView(); var color = device.TextureCreate(format, view); var framebuffer = device.FramebufferCreate([color]);
        using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state); var first = Set(device, shader, 3, 7, UniformType.Texture, color); var a = Set(device, shader, 2, 5, UniformType.Sampler, sampler); var b = Set(device, shader, 5, 13, UniformType.Sampler, sampler);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend);
        var blue = Image(device, 1, 1, [0, 0, 255, 255]); var recovered = Set(device, shader, 3, 7, UniformType.Texture, blue);
        var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, first, 3); device.DrawListBindUniformSet(list, a, 2); device.DrawListBindUniformSet(list, b, 5); Reject<InvalidOperationException>(() => device.DrawListDraw(list, false, 1, 3));
        device.DrawListBindUniformSet(list, recovered, 3); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); var data = device.TextureGetData(color, 0); Check(data[0] == 0 && data[1] == 0 && data[2] == 255 && data[3] == 255, "A rejected independent feedback draw recovers using current replacement bindings.");
        source.SourceVertex = "Texture2D<float> image:register(t7,space3);SamplerState state:register(s5,space2);float4 main(uint id:SV_VertexID):SV_Position{float x=image.SampleLevel(state,float2(.5,.5),0);return float4(x,id,0,1);}";
        source.SourceFragment = "Texture2D<float> image:register(t7,space3);SamplerComparisonState state:register(s5,space2);float4 main():SV_Target0{return image.SampleCmp(state,float2(.5,.5),.25);}";
        using var incompatible = device.ShaderCompileSPIRVFromSource(source, false); Check(incompatible.CompileErrorVertex == "" && incompatible.CompileErrorFragment == "", incompatible.CompileErrorVertex + incompatible.CompileErrorFragment); Reject<ArgumentException>(() => device.ShaderCreateFromSPIRV(incompatible));
        foreach (var rid in new[] { first, a, b, recovered, pipeline, shader, framebuffer, color, blue, sampler }) device.FreeRID(rid);
        Console.WriteLine("Independent vertex/fragment feedback rejection, same-list recovery and incompatible comparison modes passed.");
    }
}
