using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceTexelTests
{
    private static (RID Target, Action<bool> Draw) GraphicsProducer(RenderingDevice device, ShaderLanguage language, bool readback, bool mixed = false, bool storage = false)
    {
        using var source = new RDShaderSource
        {
            Language = language,
            SourceVertex = language == ShaderLanguage.GLSL ? """
            #version 450
            layout(set=3,binding=7) uniform samplerBuffer inputValues;
            void main(){ vec2 p=gl_VertexIndex==1 ? vec2(3,-1) : gl_VertexIndex==2 ? vec2(-1,3) : vec2(-1,-1); gl_Position=vec4(p+texelFetch(inputValues,0).xx,0,1); }
            """ : """
            [[vk::binding(7,3)]] Buffer<float4> inputValues;
            float4 main(uint id:SV_VertexID):SV_Position { float2 p=id==1 ? float2(3,-1) : id==2 ? float2(-1,3) : float2(-1,-1); return float4(p+inputValues.Load(0).xx,0,1); }
            """,
            SourceFragment = language == ShaderLanguage.GLSL ? """
            #version 450
            layout(set=3,binding=7) uniform samplerBuffer inputValues;
            layout(location=0) out vec4 color;
            void main(){ color=texelFetch(inputValues,1); }
            """ : """
            [[vk::binding(7,3)]] Buffer<float4> inputValues;
            float4 main():SV_Target0 { return inputValues.Load(1); }
            """
        };
        if (storage)
        {
            source.SourceVertex = source.SourceVertex.Replace("uniform samplerBuffer inputValues;", "uniform readonly imageBuffer inputValues;", StringComparison.Ordinal).Replace("binding=7)", "binding=7,rgba8)", StringComparison.Ordinal).Replace("texelFetch(inputValues,0)", "imageLoad(inputValues,0)", StringComparison.Ordinal);
            source.SourceFragment = source.SourceFragment.Replace("uniform samplerBuffer inputValues;", "uniform readonly imageBuffer inputValues;", StringComparison.Ordinal).Replace("binding=7)", "binding=7,rgba8)", StringComparison.Ordinal).Replace("texelFetch(inputValues,1)", "imageLoad(inputValues,1)", StringComparison.Ordinal);
        }
        using var module = device.ShaderCompileSPIRVFromSource(source, false); Check(module.CompileErrorVertex == "" && module.CompileErrorFragment == "", module.CompileErrorVertex + module.CompileErrorFragment);
        if (mixed)
        {
            using var fragmentSource = new RDShaderSource { Language = ShaderLanguage.HLSL, SourceFragment = "[[vk::binding(7,3)]] Buffer<float4> inputValues; float4 main():SV_Target0{return inputValues.Load(1);}" };
            using var fragment = device.ShaderCompileSPIRVFromSource(fragmentSource, false); Check(fragment.CompileErrorFragment == "", fragment.CompileErrorFragment); module.BytecodeFragment = fragment.BytecodeFragment;
        }
        Export(module, "GraphicsVertex" + language + (mixed ? "Mixed" : storage ? "Storage" : ""), ShaderStage.Vertex); Export(module, "GraphicsFragment" + language + (mixed ? "Mixed" : storage ? "Storage" : ""), ShaderStage.Fragment);
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEXEL_EXPORT_ONLY") == "1") return default;
        var shader = device.ShaderCreateFromSPIRV(module); var input = device.TextureBufferCreate(2, DataFormat.R8G8B8A8UNORM, [0, 0, 0, 255, 0, 255, 0, 255]);
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling }; using var view = new RDTextureView();
        var image = device.TextureCreate(format, view); var framebuffer = device.FramebufferCreate([image]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend);
        using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state);
        var set = storage ? Set(device, shader, 3, 7, UniformType.ImageBuffer, input) : language == ShaderLanguage.GLSL ? Set(device, shader, 3, 7, UniformType.SamplerWithTextureBuffer, sampler, input) : Set(device, shader, 3, 7, UniformType.TextureBuffer, input);
        var pixels = new byte[64]; byte[] tint = [0, 255, 0, 255]; Color[] clear = [Colors.Black];
        void Draw(bool green)
        {
            tint[0] = green ? (byte)0 : (byte)255; tint[1] = green ? (byte)255 : (byte)0; device.BufferUpdate(input, 4, 4, tint);
            var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, clear); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 3); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
            if (!readback) return;
            device.TextureGetData(image, 0, pixels.AsSpan()); var index = (2 * 4 + 2) * 4;
            Check(pixels[index] == tint[0] && pixels[index + 1] == tint[1] && pixels[index + 2] == 0 && pixels[index + 3] == 255, "Both graphics stages consume current typed-buffer bytes.");
        }
        return (image, Draw);
    }
    private static void Graphics(RenderingDevice device, ShaderLanguage language, bool mixed = false, bool storage = false)
    {
        var producer = GraphicsProducer(device, language, true, mixed, storage);
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEXEL_EXPORT_ONLY") == "1") return;
        for (var i = 0; i < 8; i++) producer.Draw(i % 2 == 0);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) producer.Draw(i % 2 == 0);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared changing texel graphics and readback allocate zero managed owner bytes.");
        Console.WriteLine(language + ": actual vertex/fragment texel output and 32 changing zero-allocation cycles passed.");
    }
}
