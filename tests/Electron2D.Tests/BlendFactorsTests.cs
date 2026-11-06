using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class BlendFactorsTests
{
    private static int _checks;
    private static string? _input, _output;
    private static byte[] _vertex = [], _fragment = [];
    private static readonly float[] Source = [.8f, .6f, .4f, .5f], Second = [.2f, .3f, .7f, .25f], Destination = [.1f, .2f, .3f, .4f], Constant = [.3f, .5f, .7f, .625f];
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); _checks++; }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static Color Color(float[] values) => new(values[0], values[1], values[2], values[3]);
    private static RID Shader(RenderingDevice device, ShaderLanguage language, bool dual, int location = 0, bool sampled = false, bool secondaryRGB = false)
    {
        var name = $"blend-{language}-{dual}-{location}-{sampled}" + (secondaryRGB ? "-rgb" : "");
        if (_input is not null)
        {
            _vertex = File.ReadAllBytes(System.IO.Path.Combine(_input, name + ".vert.spv")); _fragment = File.ReadAllBytes(System.IO.Path.Combine(_input, name + ".frag.spv"));
            return device.ShaderCreateFromBytecode(File.ReadAllBytes(System.IO.Path.Combine(_input, name + ".bin")));
        }
        var vertex = language == ShaderLanguage.HLSL ? "float4 main(uint id:SV_VertexID):SV_Position{float2 p=float2((id<<1)&2,id&2);return float4(p*2-1,.5,1);}" : "#version 450\nvoid main(){vec2 p[3]=vec2[3](vec2(-1,-1),vec2(3,-1),vec2(-1,3));gl_Position=vec4(p[gl_VertexIndex],.5,1);}";
        var declarations = sampled ? language == ShaderLanguage.HLSL ? "[[vk::binding(0,0)]]Texture2D<float4> image;[[vk::binding(1,0)]]SamplerState state;" : "layout(set=0,binding=0)uniform sampler2D image;" : "";
        var value = sampled ? language == ShaderLanguage.HLSL ? "image.SampleLevel(state,float2(.5,.5),0)" : "textureLod(image,vec2(.5),0)" : language == ShaderLanguage.HLSL ? "float4(.8,.6,.4,.5)" : "vec4(.8,.6,.4,.5)";
        var fragment = language == ShaderLanguage.HLSL
            ? declarations + $"struct Out{{[[vk::location(0)]][[vk::index(0)]]float4 primary:SV_Target0;" + (dual ? $"[[vk::location(0)]][[vk::index(1)]]float{(secondaryRGB ? 3 : 4)} secondary:SV_Target1;" : "") + $"}};Out main(){{Out o;o.primary={value};" + (dual ? secondaryRGB ? "o.secondary=float3(.2,.3,.7);" : "o.secondary=float4(.2,.3,.7,.25);" : "") + "return o;}"
            : "#version 450\n" + declarations + $"layout(location={location},index=0)out vec4 primary;" + (dual ? $"layout(location={location},index=1)out vec{(secondaryRGB ? 3 : 4)} secondary;" : "") + $"void main(){{primary={value};" + (dual ? secondaryRGB ? "secondary=vec3(.2,.3,.7);" : "secondary=vec4(.2,.3,.7,.25);" : "") + "}";
        using var source = new RDShaderSource { Language = language, SourceVertex = vertex, SourceFragment = fragment };
        using var module = device.ShaderCompileSPIRVFromSource(source, false);
        Check(module.CompileErrorVertex + module.CompileErrorFragment == "", name + ": " + module.CompileErrorVertex + module.CompileErrorFragment);
        if (language == ShaderLanguage.HLSL && location != 0)
        {
            // The pinned source compiler accepts dual outputs only at zero; external SPIR-V retains logical locations.
            var words = MemoryMarshal.Cast<byte, uint>(module.BytecodeFragment).ToArray(); var outputs = new HashSet<uint>();
            for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
                if ((words[at] & 65535) == 59 && words[at] >> 16 >= 4 && words[at + 3] == 3) outputs.Add(words[at + 2]);
            for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
                if ((words[at] & 65535) == 71 && words[at] >> 16 == 4 && words[at + 2] == 30 && outputs.Contains(words[at + 1])) words[at + 3] = (uint)location;
            module.BytecodeFragment = MemoryMarshal.AsBytes(words.AsSpan()).ToArray();
        }
        _vertex = module.BytecodeVertex; _fragment = module.BytecodeFragment;
        if (_output is not null)
        {
            Directory.CreateDirectory(_output); File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".bin"), device.ShaderCompileBinaryFromSPIRV(module));
            File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".vert.spv"), module.BytecodeVertex); File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".frag.spv"), module.BytecodeFragment);
        }
        return device.ShaderCreateFromSPIRV(module);
    }
    internal static void Run(string? binaryDirectory = null)
    {
        _input = binaryDirectory; _output = Environment.GetEnvironmentVariable("ELECTRON2D_BLEND_OUTPUT");
        using var device = RenderingServer.CreateLocalRenderingDevice();
        var refusal = Environment.GetEnvironmentVariable("ELECTRON2D_BLEND_REFUSAL");
        if (refusal is not null) { Refusal(device, refusal); return; }
        Check(Enum.GetValues<BlendFactor>().Length == 20, "The complete blend factor family is present, including the exclusive sentinel.");
        foreach (var language in new[] { ShaderLanguage.GLSL, ShaderLanguage.HLSL })
        {
            Pixels(device, language); Interfaces(device); Edges(device, language, TextureSamples.Samples1); Edges(device, language, TextureSamples.Samples4);
        }
        MultipleTargets(device);
        Console.WriteLine($"Blend factors PASS {_checks}; {(_input is null ? "source/SPIR-V" : "binary")} on {device.GetDeviceName()}");
    }
    private static float Factor(BlendFactor factor, int channel) => factor switch
    {
        BlendFactor.Zero => 0,
        BlendFactor.One => 1,
        BlendFactor.SrcColor => Source[channel],
        BlendFactor.OneMinusSrcColor => 1 - Source[channel],
        BlendFactor.DstColor => Destination[channel],
        BlendFactor.OneMinusDstColor => 1 - Destination[channel],
        BlendFactor.SrcAlpha => Source[3],
        BlendFactor.OneMinusSrcAlpha => 1 - Source[3],
        BlendFactor.DstAlpha => Destination[3],
        BlendFactor.OneMinusDstAlpha => 1 - Destination[3],
        BlendFactor.ConstantColor => Constant[channel],
        BlendFactor.OneMinusConstantColor => 1 - Constant[channel],
        BlendFactor.ConstantAlpha => Constant[3],
        BlendFactor.OneMinusConstantAlpha => 1 - Constant[3],
        BlendFactor.SrcAlphaSaturate => channel == 3 ? 1 : Math.Min(Source[3], 1 - Destination[3]),
        BlendFactor.Src1Color => Second[channel],
        BlendFactor.OneMinusSrc1Color => 1 - Second[channel],
        BlendFactor.Src1Alpha => Second[3],
        BlendFactor.OneMinusSrc1Alpha => 1 - Second[3],
        _ => throw new ArgumentOutOfRangeException(nameof(factor))
    };
    private static void Pixels(RenderingDevice device, ShaderLanguage language)
    {
        var ordinary = Shader(device, language, false); var dual = Shader(device, language, true);
        using var format = new RDTextureFormat { Width = 2, Height = 2, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView();
        var image = device.TextureCreate(format, view); var framebuffer = device.FramebufferCreate([image]); var layout = device.FramebufferGetFormat(framebuffer);
        using var attachment = new RDPipelineColorBlendStateAttachment { EnableBlend = true }; using var blend = new RDPipelineColorBlendState { BlendConstant = Color(Constant), Attachments = [attachment] }; using var raster = new RDPipelineRasterizationState();
        var bytes = new byte[64]; Color[] clear = [Color(Destination)];
        void Draw(RID pipeline, bool dynamic = false)
        {
            var list = device.DrawListBegin(framebuffer, DrawFlags.ClearColor0, clear); device.DrawListBindRenderPipeline(list, pipeline);
            if (dynamic) device.DrawListSetBlendConstants(list, Color(Constant));
            device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.TextureGetData(image, 0, bytes);
        }
        foreach (var factor in Enum.GetValues<BlendFactor>().Where(value => value != BlendFactor.Max))
            for (var field = 0; field < 4; field++)
            {
                attachment.SrcColorBlendFactor = field == 0 ? factor : BlendFactor.Zero; attachment.DstColorBlendFactor = field == 1 ? factor : BlendFactor.Zero;
                attachment.SrcAlphaBlendFactor = field == 2 ? factor : BlendFactor.Zero; attachment.DstAlphaBlendFactor = field == 3 ? factor : BlendFactor.Zero;
                var pipeline = device.RenderPipelineCreate(factor >= BlendFactor.Src1Color ? dual : ordinary, layout, -1, RenderPrimitive.Triangles, raster, blend);
                attachment.SrcColorBlendFactor = BlendFactor.One; attachment.DstColorBlendFactor = BlendFactor.One; attachment.SrcAlphaBlendFactor = BlendFactor.One; attachment.DstAlphaBlendFactor = BlendFactor.One;
                Draw(pipeline); var pixels = MemoryMarshal.Cast<byte, float>(bytes);
                for (var i = 0; i < pixels.Length; i++)
                {
                    var channel = i % 4; var affected = field < 2 ? channel < 3 : channel == 3;
                    var expected = affected ? (field is 0 or 2 ? Source[channel] : Destination[channel]) * Factor(factor, channel) : 0;
                    Check(Math.Abs(pixels[i] - expected) < .00001, $"{language} {factor} field {field}, channel {channel}: {pixels[i]} != {expected}");
                }
                device.FreeRID(pipeline);
            }
        attachment.SrcColorBlendFactor = BlendFactor.ConstantAlpha; attachment.DstColorBlendFactor = BlendFactor.Zero; attachment.SrcAlphaBlendFactor = BlendFactor.OneMinusConstantAlpha; attachment.DstAlphaBlendFactor = BlendFactor.Zero;
        blend.BlendConstant = new(0, 0, 0, 0);
        var dynamicPipeline = device.RenderPipelineCreate(ordinary, layout, -1, RenderPrimitive.Triangles, raster, blend, PipelineDynamicStateFlags.BlendConstants);
        for (var i = 0; i < 8; i++) Draw(dynamicPipeline, true); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(dynamicPipeline, true); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared native blend/draw/span-readback cycles allocate zero owner managed bytes.");
        var dynamicPixels = MemoryMarshal.Cast<byte, float>(bytes); for (var i = 0; i < dynamicPixels.Length; i++) Check(Math.Abs(dynamicPixels[i] - Source[i % 4] * (i % 4 == 3 ? 1 - Constant[3] : Constant[3])) < .00001, "Dynamic constant alpha is independent of constant RGB.");
        attachment.SrcColorBlendFactor = BlendFactor.Src1Color; Reject<ArgumentException>(() => device.RenderPipelineCreate(ordinary, layout, -1, RenderPrimitive.Triangles, raster, blend));
        blend.EnableLogicOp = true; blend.LogicOp = LogicOperation.Xor;
        var logical = device.RenderPipelineCreate(dual, layout, -1, RenderPrimitive.Triangles, raster, blend); Draw(logical);
        var logicalPixels = MemoryMarshal.Cast<byte, float>(bytes); for (var i = 0; i < logicalPixels.Length; i++) Check(logicalPixels[i] == Source[i % 4], "The complete native factor adapter preserves logical operations, which copy floating source components."); device.FreeRID(logical); blend.EnableLogicOp = false;
        foreach (var invalid in new[] { (BlendFactor)(-1), BlendFactor.Max, (BlendFactor)20 }) { attachment.SrcColorBlendFactor = invalid; Reject<ArgumentOutOfRangeException>(() => device.RenderPipelineCreate(ordinary, layout, -1, RenderPrimitive.Triangles, raster, blend)); }
        var narrow = Shader(device, language, true, secondaryRGB: true);
        attachment.SrcColorBlendFactor = BlendFactor.Src1Color; attachment.DstColorBlendFactor = BlendFactor.Zero; attachment.SrcAlphaBlendFactor = BlendFactor.Zero; attachment.DstAlphaBlendFactor = BlendFactor.Zero; attachment.WriteA = false;
        var narrowPipeline = device.RenderPipelineCreate(narrow, layout, -1, RenderPrimitive.Triangles, raster, blend); Draw(narrowPipeline);
        var narrowPixels = MemoryMarshal.Cast<byte, float>(bytes); for (var i = 0; i < narrowPixels.Length; i++) Check(Math.Abs(narrowPixels[i] - (i % 4 == 3 ? Destination[3] : Source[i % 4] * Second[i % 4])) < .00001, "Secondary RGB width may differ from primary RGBA when the equation consumes only defined RGB components.");
        device.FreeRID(narrowPipeline); device.FreeRID(narrow);
        device.FreeRID(dynamicPipeline); device.FreeRID(framebuffer); device.FreeRID(image); device.FreeRID(ordinary); device.FreeRID(dual);
    }
}
