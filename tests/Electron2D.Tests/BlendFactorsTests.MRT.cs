using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class BlendFactorsTests
{
    private static void MultipleTargets(RenderingDevice device)
    {
        const string name = "blend-mrt"; RID shader;
        if (_input is not null) shader = device.ShaderCreateFromBytecode(File.ReadAllBytes(System.IO.Path.Combine(_input, name + ".bin")));
        else
        {
            using var source = new RDShaderSource
            {
                SourceVertex = "#version 450\nvoid main(){vec2 p[3]=vec2[3](vec2(-1,-1),vec2(3,-1),vec2(-1,3));gl_Position=vec4(p[gl_VertexIndex],.5,1);}",
                SourceFragment = "#version 450\nlayout(location=0)out vec4 first;layout(location=2)out vec4 second;void main(){first=vec4(.8,.6,.4,.5);second=vec4(.8,.6,.4,.5);}"
            };
            using var module = device.ShaderCompileSPIRVFromSource(source, false); Check(module.CompileErrorVertex + module.CompileErrorFragment == "", "MRT source compiles.");
            if (_output is not null)
            {
                File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".bin"), device.ShaderCompileBinaryFromSPIRV(module));
                File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".vert.spv"), module.BytecodeVertex); File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".frag.spv"), module.BytecodeFragment);
            }
            shader = device.ShaderCreateFromSPIRV(module);
        }
        using var format = new RDTextureFormat { Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView();
        var first = device.TextureCreate(format, view); var second = device.TextureCreate(format, view); using var pass = new RDFramebufferPass { ColorAttachments = [0, -1, 1] }; var framebuffer = device.FramebufferCreateMultipass([first, second], [pass]);
        using var a = new RDPipelineColorBlendStateAttachment { EnableBlend = true, SrcColorBlendFactor = BlendFactor.ConstantAlpha, SrcAlphaBlendFactor = BlendFactor.OneMinusConstantAlpha };
        using var b = new RDPipelineColorBlendStateAttachment { EnableBlend = true, DstColorBlendFactor = BlendFactor.OneMinusConstantAlpha, DstAlphaBlendFactor = BlendFactor.ConstantAlpha };
        using var unused = new RDPipelineColorBlendStateAttachment { SrcColorBlendFactor = (BlendFactor)999, DstAlphaBlendFactor = BlendFactor.Max };
        using var blend = new RDPipelineColorBlendState { Attachments = [a, unused, b, unused], BlendConstant = Color(Constant) }; using var raster = new RDPipelineRasterizationState();
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend);
        var list = device.DrawListBegin(framebuffer, DrawFlags.ClearColorAll, [Color(Destination), Color(Destination)]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        foreach (var image in new[] { first, second })
        {
            var pixels = MemoryMarshal.Cast<byte, float>(device.TextureGetData(image, 0));
            for (var channel = 0; channel < 4; channel++) Check(Math.Abs(pixels[channel] - (image == first ? Source[channel] * (channel == 3 ? 1 - Constant[3] : Constant[3]) : Destination[channel] * (channel == 3 ? Constant[3] : 1 - Constant[3]))) < .00001, "Sparse MRT attachments retain independent native source/destination RGB/alpha factors and ignore unused/trailing descriptions.");
        }
        foreach (var rid in new[] { pipeline, framebuffer, first, second, shader }) device.FreeRID(rid);
    }
}
