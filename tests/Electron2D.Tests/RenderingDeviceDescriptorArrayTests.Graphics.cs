using System.Reflection;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceDescriptorArrayTests
{
    private static void Graphics(RenderingDevice device, ShaderLanguage language, string role, bool multisample = false)
    {
        var input = role == "input"; var storage = role == "storage";
        using var source = new RDShaderSource
        {
            Language = language,
            SourceVertex = language == ShaderLanguage.GLSL ? "#version 450\nvoid main(){vec2 p=gl_VertexIndex==1?vec2(3,-1):gl_VertexIndex==2?vec2(-1,3):vec2(-1,-1);gl_Position=vec4(p,0,1);}" : "float4 main(uint i:SV_VertexID):SV_Position{return float4(i==1?float2(3,-1):i==2?float2(-1,3):float2(-1,-1),0,1);}",
            SourceFragment = language == ShaderLanguage.GLSL ? "#version 450\n" +
                (input ? "layout(input_attachment_index=0,set=3,binding=7)uniform " + (multisample ? "subpassInputMS" : "subpassInput") + " images[2];" : storage ? "layout(set=3,binding=7,rgba8)uniform readonly image2D images[2];" : "layout(set=3,binding=7)uniform sampler2D images[2];") +
                "layout(location=0)out vec4 color;void main(){uint i=uint(gl_FragCoord.x)/2; color=" + (input ? "subpassLoad(images[i]" + (multisample ? ",0)" : ")") : storage ? "imageLoad(images[i],ivec2(0))" : "texture(images[i],vec2(.5))") + ";}" :
                (input ? "[[vk::input_attachment_index(0),vk::binding(7,3)]] SubpassInput<float4> images[2];" : "[[vk::binding(7,3)]] Texture2D<float4> images[2];[[vk::binding(2,4)]] SamplerState filters[2];") +
                "float4 main(float4 p:SV_Position):SV_Target0{uint i=(uint)p.x/2;return " + (input ? "images[i].SubpassLoad()" : "images[i].Sample(filters[1-i],float2(.5,.5))") + ";}"
        };
        using var module = device.ShaderCompileSPIRVFromSource(source, false);
        if (input && language == ShaderLanguage.HLSL)
        {
            Check(module.CompileErrorFragment.Contains("attribute only applies to SubpassInput", StringComparison.Ordinal), "Packaged HLSL frontend reports its array attribute prerequisite.");
            Console.WriteLine("HLSL input-array source blocked by packaged frontend attribute support; GLSL/SPIR-V executes."); return;
        }
        Check(module.CompileErrorVertex == "" && module.CompileErrorFragment == "", module.CompileErrorVertex + module.CompileErrorFragment);
        if (Environment.GetEnvironmentVariable("ELECTRON2D_DESCRIPTOR_OUTPUT") is { } path)
        {
            var result = typeof(RenderingDevice).GetMethod("Remap", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [module.BytecodeFragment, ShaderStage.Fragment])!;
            File.WriteAllBytes(System.IO.Path.Combine(path, "Graphics" + role + (multisample ? "MS" : "") + language + ".spv"), (byte[])result.GetType().GetField("Item1")!.GetValue(result)!);
        }
        var shader = device.ShaderCreateFromSPIRV(module);
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling };
        using var view = new RDTextureView(); var output = device.TextureCreate(format, view);
        if (input) { format.UsageBits |= TextureUsageBits.InputAttachment; if (multisample) { format.Samples = TextureSamples.Samples4; format.UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.InputAttachment; } }
        var a = input ? device.TextureCreate(format, view) : Image(device, 255, storage ? TextureUsageBits.Sampling | TextureUsageBits.Storage : TextureUsageBits.Sampling);
        var b = input ? device.TextureCreate(format, view) : Image(device, 128, storage ? TextureUsageBits.Sampling | TextureUsageBits.Storage : TextureUsageBits.Sampling);
        using var first = new RDFramebufferPass { ColorAttachments = [0, 1] }; using var last = new RDFramebufferPass { ColorAttachments = [2], InputAttachments = [0, 1] };
        var framebuffer = input ? device.FramebufferCreateMultipass([a, b, output], [first, last]) : device.FramebufferCreate([output]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend, forRenderPass: input ? 1u : 0u);
        using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state);
        var type = input ? UniformType.InputAttachment : storage ? UniformType.Image : language == ShaderLanguage.GLSL ? UniformType.SamplerWithTexture : UniformType.Texture;
        var images = Set(device, shader, 3, 7, type, type == UniformType.SamplerWithTexture ? [sampler, a, sampler, b] : [a, b]);
        var filters = !input && !storage && language == ShaderLanguage.HLSL ? Set(device, shader, 4, 2, UniformType.Sampler, sampler, sampler) : default;
        if (!storage)
        {
            var wrong = Set(device, shader, 3, 7, type, input ? [a, a] : type == UniformType.SamplerWithTexture ? [sampler, a, sampler, output] : [a, output]);
            var failed = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, input ? [Colors.Red, Colors.Red, Colors.Black] : [Colors.Black]); if (input) device.DrawListSwitchToNextPass();
            device.DrawListBindRenderPipeline(failed, pipeline); device.DrawListBindUniformSet(failed, wrong, 3); if (filters.IsValid()) device.DrawListBindUniformSet(failed, filters, 4);
            Reject<InvalidOperationException>(() => device.DrawListDraw(failed, false, 1, 3));
            device.DrawListEnd();
        }
        var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, input ? [Colors.Red, new(128f / 255, 0, 0, 1), Colors.Black] : [Colors.Black]);
        if (input) device.DrawListSwitchToNextPass();
        device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, images, 3); if (filters.IsValid()) device.DrawListBindUniformSet(list, filters, 4);
        device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); var bytes = device.TextureGetData(output, 0);
        for (var y = 0; y < 4; y++) for (var x = 0; x < 4; x++) Check(bytes[(y * 4 + x) * 4] == (x < 2 ? 255 : 128) && bytes[(y * 4 + x) * 4 + 3] == 255, "Divergent fragment " + role + " array elements reach the correct pixels.");
        Console.WriteLine(language + " graphics " + role + " arrays passed, multisample=" + multisample);
    }
}
