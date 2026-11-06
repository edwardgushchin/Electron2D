using System.Buffers.Binary;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class DepthInputsTests
{
    private static int _checks;
    private static string? _input, _output;
    private const TextureUsageBits Copies = TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo;
    private static string Vertex(ShaderLanguage language) => language == ShaderLanguage.HLSL ? "float4 main(uint id:SV_VertexID):SV_Position{float2 p=float2((id<<1)&2,id&2);return float4(p*2-1,.5,1);}" : "#version 450\nvoid main(){vec2 p[3]=vec2[3](vec2(-1,-1),vec2(3,-1),vec2(-1,3));gl_Position=vec4(p[gl_VertexIndex],.5,1);}";
    private static RID Reader(RenderingDevice device, ShaderLanguage language, TextureSamples samples)
    {
        var count = 1 << (int)samples; var selected = count - 1;
        var load = count == 1 ? "source.SubpassLoad()" : $"source.SubpassLoad({selected})";
        var fragment = language == ShaderLanguage.HLSL ? $"[[vk::input_attachment_index(1)]][[vk::binding(3,4)]] SubpassInput{(count == 1 ? "" : "MS")}<float> source;float main():SV_Target0{{return {load};}}" : $"#version 450\nlayout(input_attachment_index=1,set=4,binding=3)uniform subpassInput{(count == 1 ? "" : "MS")} source;layout(location=0)out float value;void main(){{value=subpassLoad(source{(count == 1 ? "" : "," + selected)}).r;}}";
        return Shader(device, $"reader-{language}-{count}", Vertex(language), fragment, language);
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); _checks++; }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static RID Shader(RenderingDevice device, string name, string vertex, string fragment, ShaderLanguage language)
    {
        if (_input is not null) return device.ShaderCreateFromBytecode(File.ReadAllBytes(System.IO.Path.Combine(_input, name + ".bin")));
        using var source = new RDShaderSource { Language = language, SourceVertex = vertex, SourceFragment = fragment };
        using var module = device.ShaderCompileSPIRVFromSource(source, false);
        Check(module.CompileErrorVertex + module.CompileErrorFragment == "", name + ": " + module.CompileErrorVertex + module.CompileErrorFragment);
        if (_output is not null)
        {
            Directory.CreateDirectory(_output); File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".bin"), device.ShaderCompileBinaryFromSPIRV(module));
            File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".vert.spv"), module.BytecodeVertex); File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".frag.spv"), module.BytecodeFragment);
        }
        return device.ShaderCreateFromSPIRV(module);
    }
    internal static void Run(string? binaryDirectory = null)
    {
        _input = binaryDirectory; _output = Environment.GetEnvironmentVariable("ELECTRON2D_DEPTH_INPUTS_OUTPUT");
        using var device = RenderingServer.CreateLocalRenderingDevice();
        if (Environment.GetEnvironmentVariable("ELECTRON2D_DEPTH_INPUTS_REFUSAL") == "1") { Refusal(device); return; }
        foreach (var encoding in new[] { DataFormat.D16UNORM, DataFormat.X8D24UNORMPACK32, DataFormat.D32SFLOAT, DataFormat.D24UNORMS8UINT, DataFormat.D32SFLOATS8UINT })
            Check(device.TextureIsFormatSupportedForUsage(encoding, TextureUsageBits.DepthStencilAttachment | TextureUsageBits.InputAttachment), encoding + " native depth inputs are available.");
        if (Environment.GetEnvironmentVariable("ELECTRON2D_DEPTH_INPUTS_FOCUS") == "feedback") { Layered(device, ShaderLanguage.GLSL, TextureSamples.Samples1); return; }
        foreach (var language in new[] { ShaderLanguage.GLSL, ShaderLanguage.HLSL })
            foreach (var samples in new[] { TextureSamples.Samples1, TextureSamples.Samples2, TextureSamples.Samples4, TextureSamples.Samples8 })
                foreach (var encoding in new[] { DataFormat.D16UNORM, DataFormat.X8D24UNORMPACK32, DataFormat.D32SFLOAT, DataFormat.D24UNORMS8UINT, DataFormat.D32SFLOATS8UINT })
                    foreach (var sampling in new[] { false, true }) Exercise(device, language, encoding, samples, sampling);
        foreach (var language in new[] { ShaderLanguage.GLSL, ShaderLanguage.HLSL })
            foreach (var samples in new[] { TextureSamples.Samples1, TextureSamples.Samples4 }) Layered(device, language, samples);
        InputOnlyAndArrays(device); ColorInputs(device);
        Console.WriteLine($"Depth inputs PASS {_checks}; {(_input is null ? "source/SPIR-V" : "binary")} on {device.GetDeviceName()}");
    }
    private static void Refusal(RenderingDevice device)
    {
        using var state = new RDTextureFormat { Width = 4, Height = 4, UsageBits = TextureUsageBits.InputAttachment }; using var view = new RDTextureView();
        foreach (var encoding in new[] { DataFormat.D16UNORM, DataFormat.X8D24UNORMPACK32, DataFormat.D32SFLOAT, DataFormat.D24UNORMS8UINT, DataFormat.D32SFLOATS8UINT })
        {
            state.Format = encoding; Check(!device.TextureIsFormatSupportedForUsage(encoding, state.UsageBits), "Depth inputs require ABI2."); Reject<NotSupportedException>(() => device.TextureCreate(state, view));
        }
        state.Format = DataFormat.R32SFLOAT; state.UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.InputAttachment | TextureUsageBits.CanCopyFrom;
        var color = device.TextureCreate(state, view); var framebuffer = device.FramebufferCreate([color]); device.DrawListBegin(framebuffer, DrawFlags.ClearColor0, [new Color(.375f, 0, 0, 1)]); device.DrawListEnd();
        Check(BinaryPrimitives.ReadSingleLittleEndian(device.TextureGetData(color, 0)) == .375f, "Ordinary color input allocation/rendering survives ABI2 refusal.");
        state.Format = DataFormat.D32SFLOAT; state.UsageBits = Copies | TextureUsageBits.DepthStencilAttachment; var data = new byte[64]; for (var i = 0; i < 16; i++) BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(i * 4), .25f);
        var depth = device.TextureCreate(state, view, [data]); Check(device.TextureGetData(depth, 0).SequenceEqual(data), "ABI1 depth transfers survive ABI2 refusal.");
        Console.WriteLine($"Depth inputs ABI1 control PASS {_checks}; ordinary native rendering/transfers preserved");
    }
    private static void Exercise(RenderingDevice device, ShaderLanguage language, DataFormat encoding, TextureSamples samples, bool sampling)
    {
        var hlsl = language == ShaderLanguage.HLSL; var count = 1 << (int)samples; var selected = count - 1;
        var vertex = Vertex(language);
        var producerFragment = hlsl ? "float main(float4 p:SV_Position,uint s:SV_SampleIndex):SV_Depth{return .125+(floor(p.x)+floor(p.y)*4)/64+float(s)/16;}" : "#version 450\nvoid main(){gl_FragDepth=.125+(floor(gl_FragCoord.x)+floor(gl_FragCoord.y)*4)/64+float(gl_SampleID)/16;}";
        var producerShader = Shader(device, "producer-" + language, vertex, producerFragment, language);
        var readerShader = Reader(device, language, samples);
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = encoding, Samples = samples, UsageBits = Copies | TextureUsageBits.DepthStencilAttachment | TextureUsageBits.InputAttachment | (sampling ? TextureUsageBits.Sampling : 0) };
        using var view = new RDTextureView(); var source = device.TextureCreate(format, view);
        format.Format = DataFormat.R32SFLOAT; format.Samples = TextureSamples.Samples1; format.UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom; var output = device.TextureCreate(format, view);
        using var first = new RDFramebufferPass { DepthAttachment = 0 }; using var second = new RDFramebufferPass { ColorAttachments = [1], InputAttachments = [RDFramebufferPass.AttachmentUnused, 0] };
        var framebuffer = device.FramebufferCreateMultipass([source, output], [first, second]); var layout = device.FramebufferGetFormat(framebuffer);
        using var raster = new RDPipelineRasterizationState(); using var noColors = new RDPipelineColorBlendState(); using var color = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [color] };
        using var depth = new RDPipelineDepthStencilState { EnableDepthTest = true, EnableDepthWrite = true, DepthCompareOperator = CompareOperator.Always };
        using var multi = new RDPipelineMultisampleState { SampleCount = samples };
        var producer = device.RenderPipelineCreate(producerShader, layout, -1, RenderPrimitive.Triangles, raster, noColors, depthStencilState: depth, multisampleState: multi);
        var reader = device.RenderPipelineCreate(readerShader, layout, -1, RenderPrimitive.Triangles, raster, blend, forRenderPass: 1);
        using var input = new RDUniform { Binding = 3, UniformType = UniformType.InputAttachment }; input.AddID(source); var set = device.UniformSetCreate([input], readerShader, 4);
        var bytes = new byte[64];
        void Draw()
        {
            var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, producer); device.DrawListDraw(list, false, 1, 3);
            device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(list, reader); device.DrawListBindUniformSet(list, set, 4); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.TextureGetData(output, 0, bytes.AsSpan());
        }
        Draw();
        for (var pixel = 0; pixel < 16; pixel++) Check(Math.Abs(BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(pixel * 4)) - (.125f + pixel / 64f + selected / 16f)) < .00002f, $"{language}/{encoding}/{samples}/{sampling}: independent depth input at pixel {pixel}.");
        for (var i = 0; i < 16; i++) Draw(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw();
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0, "Prepared input-pass/readback cycles allocate zero owner managed bytes.");
        device.FreeRID(source); device.FreeRID(output); device.FreeRID(framebuffer); device.FreeRID(producer); device.FreeRID(reader); device.FreeRID(set); device.FreeRID(producerShader); device.FreeRID(readerShader);
    }
}
