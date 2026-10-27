using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static void Texels(string mode)
    {
        if (mode == "texel-canvas") { TexelCanvas("glsl"); TexelCanvas("hlsl"); return; }
        using var device = RenderingServer.CreateLocalRenderingDevice(); Console.WriteLine("Graphics texel device: " + device.GetDeviceName());
        foreach (var language in new[] { "glsl", "hlsl" })
        {
            if (mode.StartsWith("texel-unavailable-", StringComparison.Ordinal)) TexelUnavailable(device, language, mode["texel-unavailable-".Length..]);
            else if (mode is "texel-formats" or "texel-core-formats") TexelFormats(device, language);
            else if (mode is "texel-nonuniform" or "texel-core-nonuniform") TexelNonUniform(device, language);
            else if (mode is "texel-atomics" or "texel-core-atomics") TexelAtomics(device, language);
            else TexelChain(device, language, mode == "texel-source");
        }
    }
    private static RID TexelSet(RenderingDevice device, RID shader, uint setIndex, int binding, UniformType type, params RID[] resources)
    {
        using var description = new RDUniform { Binding = binding, UniformType = type }; foreach (var resource in resources) description.AddID(resource); return device.UniformSetCreate([description], shader, setIndex);
    }
    private static RID TexelChain(RenderingDevice device, string language, bool authoring = false)
    {
        var writer = Shader(device, "texel-write", language, authoring); var reader = Shader(device, "texel-read", language, authoring); var compute = Shader(device, "texel-chain", language, authoring);
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var factor = new RDPipelineSpecializationConstant<uint> { ConstantID = 0, Value = 1 };
        var write = device.RenderPipelineCreate(writer, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, specializationConstants: [factor]); var read = device.RenderPipelineCreate(reader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend); var kernel = device.ComputePipelineCreate(compute);
        var texels = device.TextureBufferCreate(20, DataFormat.R8G8B8A8UNORM); var counters = device.TextureBufferCreate(3, DataFormat.R32UINT); using var samplerState = new RDSamplerState(); var sampler = device.SamplerCreate(samplerState);
        var writeSet = TexelSet(device, writer, 3, 7, UniformType.ImageBuffer, texels); var counterSet = TexelSet(device, writer, 4, 2, UniformType.ImageBuffer, counters); var computeSet = TexelSet(device, compute, 3, 7, UniformType.ImageBuffer, texels); var computeCounters = TexelSet(device, compute, 4, 2, UniformType.ImageBuffer, counters);
        var readSet = language == "glsl" ? TexelSet(device, reader, 3, 7, UniformType.SamplerWithTextureBuffer, sampler, texels) : TexelSet(device, reader, 3, 7, UniformType.TextureBuffer, texels);
        var wrongFormat = device.TextureBufferCreate(20, DataFormat.R32G32B32A32SFLOAT); Reject<ArgumentException>(() => TexelSet(device, writer, 3, 7, UniformType.ImageBuffer, wrongFormat)); device.FreeRID(wrongFormat);
        var data = new byte[80]; var pixels = new byte[64]; uint[] counterData = new uint[3]; uint[] push = [1];
        void Draw()
        {
            device.BufferClear(texels, 0, 80); device.BufferClear(counters, 0, 12); var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, write); device.DrawListBindUniformSet(list, writeSet, 3); device.DrawListBindUniformSet(list, counterSet, 4);
            push[0] = 1; device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 4); device.DrawListDraw(list, false, 1, 3); push[0] = 2; device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 4); device.DrawListDraw(list, false, 1, 3);
            device.DrawListBindRenderPipeline(list, read); device.DrawListBindUniformSet(list, readSet, 3); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        }
        Draw(); device.BufferGetData(texels, data.AsSpan()); Check(data.AsSpan(12, 4).SequenceEqual(new byte[4]), "Neighbor gap survives both writing stages and negative/past-end stores.");
        void Chain()
        {
            Draw(); var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, kernel); device.ComputeListBindUniformSet(list, computeSet, 3); device.ComputeListBindUniformSet(list, computeCounters, 4); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(texels, data.AsSpan()); device.BufferGetData(counters, MemoryMarshal.AsBytes(counterData.AsSpan())); device.TextureGetData(image, 0, pixels.AsSpan());
        }
        Chain(); Check(counterData[0] == 6 && counterData[1] == 48 && counterData[2] == 54, "Vertex/fragment texel atomics reach following compute and CPU.");
        for (var i = 0; i < 20; i++) { var at = i * 4; Check(i == 3 ? data[at] == 255 && data[at + 1] == 0 && data[at + 2] == 0 && data[at + 3] == 255 : data[at] == 0 && data[at + 1] == 255 && data[at + 2] is 127 or 128 && data[at + 3] == 255, "Original encoded bytes survive graphics/compute transitions and metadata is excluded."); }
        for (var i = 0; i < 16; i++) Check(pixels[i * 4] == 0 && pixels[i * 4 + 1] == 255 && pixels[i * 4 + 2] is 127 or 128 && pixels[i * 4 + 3] == 255, "Sampled texel draw sees prior graphics writes in every pixel.");
        for (var i = 0; i < 16; i++) Chain(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Chain(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared graphics texel write/read/compute/readback cycles allocate zero owner bytes.");
        device.FreeRID(texels); Check(!device.UniformSetIsValid(writeSet) && !device.UniformSetIsValid(readSet), "Formatted writers and readers borrow the same allocation lifetime.");
        foreach (var resource in new[] { writeSet, counterSet, readSet, computeSet, computeCounters, write, read, kernel, writer, reader, compute, counters, sampler, target }) device.FreeRID(resource);
        Console.WriteLine("PASS " + language + ": writable formatted vertex/fragment buffers, atomic counts, exact bytes, bounds, sampled/compute consumers, borrowed lifetime and 32 zero-owner-byte cycles."); return image;
    }
    private static void TexelUnavailable(RenderingDevice device, string language, string mask)
    {
        using var both = Code("texel-write", language); using var read = Code("read", language); using var vertex = new RDShaderSPIRV { BytecodeVertex = both.BytecodeVertex, BytecodeFragment = read.BytecodeFragment }; using var fragment = new RDShaderSPIRV { BytecodeVertex = read.BytecodeVertex, BytecodeFragment = both.BytecodeFragment };
        foreach (var item in new[] { (vertex, mask != "fragment"), (fragment, mask != "vertex"), (both, true) })
        {
            if (item.Item2) { Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(item.Item1)); Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(item.Item1)); }
            else device.FreeRID(device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(item.Item1)));
        }
        Console.WriteLine("PASS " + language + ": graphics ImageBuffer feature refusal before identity/binary publication: " + mask);
    }
    private static void TexelCanvas(string language)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var root = new Window { Size = new(16, 16) }; using var texture = new TextureRD(); var frames = 0;
        root.Ready += _ => { texture.TextureRDRID = TexelChain(RenderingServer.GetRenderingDevice()!, language); root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured; };
        void Captured() { if (++frames < 3) return; RenderingServer.FramePostDraw -= Captured; using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(8, 8); Check(pixel.R < .01f && pixel.G > .99f && Math.Abs(pixel.B - .5f) < .01f, "Formatted graphics stores reach retained canvas pixels."); root.Tree!.Quit(); }
        Engine.Run(root); Console.WriteLine("PASS " + language + ": formatted graphics stores reach retained canvas.");
    }
}
