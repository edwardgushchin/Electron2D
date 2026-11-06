using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class StencilTexturesTests
{
    private static int _checks;
    private static string? _input, _output;
    private const TextureUsageBits Transfers = TextureUsageBits.CanUpdate | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo;
    private const TextureUsageBits Roles = TextureUsageBits.DepthStencilAttachment | TextureUsageBits.Sampling;
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); _checks++; }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static RID Shader(RenderingDevice device, string name, ShaderLanguage language, string fragment)
    {
        name += "-" + language;
        if (_input is not null) return device.ShaderCreateFromBytecode(File.ReadAllBytes(System.IO.Path.Combine(_input, name + ".bin")));
        using var source = new RDShaderSource
        {
            Language = language,
            SourceVertex = language == ShaderLanguage.GLSL ? "#version 450\nvoid main(){vec2 p[3]=vec2[3](vec2(-1,-1),vec2(3,-1),vec2(-1,3));gl_Position=vec4(p[gl_VertexIndex],.5,1);}" : "float4 main(uint id:SV_VertexID):SV_Position{float2 p=float2((id<<1)&2,id&2);return float4(p*2-1,.5,1);}",
            SourceFragment = fragment
        };
        using var module = device.ShaderCompileSPIRVFromSource(source, false);
        Check(module.CompileErrorVertex + module.CompileErrorFragment == "", name + ": " + module.CompileErrorVertex + module.CompileErrorFragment);
        if (_output is not null)
        {
            Directory.CreateDirectory(_output); File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".bin"), device.ShaderCompileBinaryFromSPIRV(module));
            File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".vert.spv"), module.BytecodeVertex); File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".frag.spv"), module.BytecodeFragment);
        }
        return device.ShaderCreateFromSPIRV(module);
    }
    private static RID Image(RenderingDevice device, DataFormat encoding, TextureUsageBits usage, TextureSamples samples = TextureSamples.Samples1, uint layers = 1, uint mips = 1, ReadOnlySpan<byte[]> data = default, uint width = 4, uint height = 4)
    {
        using var format = new RDTextureFormat { Width = width, Height = height, Format = encoding, UsageBits = usage, Samples = samples, TextureType = layers == 1 ? TextureType.Texture : TextureType.TextureArray, ArrayLayers = layers, MIPMaps = mips }; using var view = new RDTextureView();
        return device.TextureCreate(format, view, data);
    }
    internal static void Run(string? directory = null)
    {
        _input = directory; _output = Environment.GetEnvironmentVariable("ELECTRON2D_STENCIL_OUTPUT");
        using var device = RenderingServer.CreateLocalRenderingDevice();
        if (Environment.GetEnvironmentVariable("ELECTRON2D_STENCIL_REFUSAL") is { } refusal) { Failures(device, refusal); return; }
        Check(device.TextureIsFormatSupportedForUsage(DataFormat.S8UINT, Roles | Transfers), "Native S8 stencil/sampling/transfer roles are available.");
        TransfersAndViews(device);
        foreach (var language in new[] { ShaderLanguage.GLSL, ShaderLanguage.HLSL }) { Faces(device, language); Operations(device, language); Multisample(device, language); foreach (var samples in new[] { TextureSamples.Samples1, TextureSamples.Samples4 }) foreach (var sampling in new[] { false, true }) Inputs(device, language, samples, sampling); }
        Canvas();
        Console.WriteLine($"Stencil textures PASS {_checks}; {(_input is null ? "source/SPIR-V" : "binary")} on {device.GetDeviceName()}");
    }
    private static void Faces(RenderingDevice device, ShaderLanguage language)
    {
        var shader = Shader(device, "faces", language, language == ShaderLanguage.GLSL ? "#version 450\nlayout(location=0)out uint facing;void main(){facing=gl_FrontFacing?1u:0u;}" : "uint main(bool face:SV_IsFrontFace):SV_Target0{return face?1u:0u;}");
        var stencil = Image(device, DataFormat.S8UINT, Roles | Transfers); var color = Image(device, DataFormat.R32UINT, TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom); var target = device.FramebufferCreate([color, stencil]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        using var state = new RDPipelineDepthStencilState { EnableStencil = true, FrontOpCompare = CompareOperator.Always, BackOpCompare = CompareOperator.Always, FrontOpPass = StencilOperation.Replace, BackOpPass = StencilOperation.Replace, FrontOpWriteMask = 240, BackOpWriteMask = 15, FrontOpReference = 49, BackOpReference = 199 };
        foreach (var face in new[] { PolygonFrontFace.Clockwise, PolygonFrontFace.CounterClockwise })
        {
            raster.FrontFace = face; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: state);
            var list = device.DrawListBegin(target, DrawFlags.ClearAll, [new Color()], clearStencilValue: 170); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
            var facingBytes = device.TextureGetData(color, 0); var facing = MemoryMarshal.Cast<byte, uint>(facingBytes); var bytes = device.TextureGetData(stencil, 0);
            for (var i = 0; i < bytes.Length; i++) Check(bytes[i] == (facing[i] == 1 ? 58 : 167), "Independent stencil-only face masks/references agree with actual fragment facing.");
            Sample(device, language, stencil, bytes); device.FreeRID(pipeline);
        }
        state.EnableDepthTest = true; Reject<ArgumentException>(() => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: state)); state.EnableDepthTest = false;
        foreach (var rid in new[] { target, stencil, color, shader }) device.FreeRID(rid);
    }
    private static void Sample(RenderingDevice device, ShaderLanguage language, RID image, byte[] expected, int sampleIndex = 0)
    {
        using var imageInfo = device.TextureGetFormat(image); var ms = imageInfo.Samples != TextureSamples.Samples1; var count = 1 << (int)imageInfo.Samples;
        var fragment = language == ShaderLanguage.GLSL
            ? "#version 450\n#extension GL_EXT_samplerless_texture_functions:require\nlayout(set=0,binding=0)uniform utexture2D" + (ms ? "MS" : "") + $" image;layout(location=0)out uint value;void main(){{value=texelFetch(image,ivec2(gl_FragCoord.xy),{sampleIndex}).r;}}"
            : $"[[vk::binding(0,0)]]Texture2D{(ms ? "MS" : "")}<uint> image;uint main(float4 p:SV_Position):SV_Target0{{return image.Load({(ms ? $"int2(p.xy),{sampleIndex}" : "int3(p.xy,0)")});}}";
        var shader = Shader(device, "sample-" + count + "-" + sampleIndex, language, fragment); using var uniform = new RDUniform { Binding = 0, UniformType = UniformType.Texture }; uniform.AddID(image); var set = device.UniformSetCreate([uniform], shader, 0);
        var output = Image(device, DataFormat.R32UINT, TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom, width: imageInfo.Width, height: imageInfo.Height); var target = device.FramebufferCreate([output]); using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
        var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); var data = device.TextureGetData(output, 0);
        var pixels = MemoryMarshal.Cast<byte, uint>(data); for (var i = 0; i < pixels.Length; i++) Check(pixels[i] == expected[i], "Stencil-only images sample exact unsigned values.");
        foreach (var rid in new[] { pipeline, target, output, set, shader }) device.FreeRID(rid);
    }
}
