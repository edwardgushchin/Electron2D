using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static void ArrayStorageGraphics(RenderingDevice device, string language, bool source)
    {
        const TextureUsageBits usage = TextureUsageBits.Storage | TextureUsageBits.Sampling | TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo;
        using var format = new RDTextureFormat { Width = 4, Height = 4, ArrayLayers = 3, MIPMaps = 2, TextureType = TextureType.TextureArray, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = usage }; using var plain = new RDTextureView();
        float[] initial = new float[80]; initial.AsSpan().Fill(-7); var bytes = MemoryMarshal.AsBytes(initial.AsSpan()).ToArray(); RID[] destinations = [device.TextureCreate(format, plain, [bytes, bytes, bytes]), device.TextureCreate(format, plain, [bytes, bytes, bytes])];
        format.Format = DataFormat.R32SINT; RID[] signed = [device.TextureCreate(format, plain), device.TextureCreate(format, plain)];
        format.Format = DataFormat.R32SFLOAT; format.AddShareableFormat(DataFormat.R32SFLOAT); format.AddShareableFormat(DataFormat.R32UINT); float[] sampled = new float[20]; sampled.AsSpan(16).Fill(.375f); bytes = MemoryMarshal.AsBytes(sampled.AsSpan()).ToArray();
        RID[] originals = [device.TextureCreate(format, plain, [bytes, bytes, bytes]), device.TextureCreate(format, plain, [bytes, bytes, bytes])]; using var integer = new RDTextureView { FormatOverride = DataFormat.R32UINT }; RID[] unsigned = [device.TextureCreateShared(integer, originals[0]), device.TextureCreateShared(integer, originals[1])];
        using var colorFormat = new RDTextureFormat { Width = 4, Height = 3, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom }; var color = device.TextureCreate(colorFormat, plain); var target = device.FramebufferCreate([color]);
        var shader = Shader(device, "array-storage", language, source); using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state);
        using var writes = ViewUniform(UniformType.Image, 0, destinations); using var adds = ViewUniform(UniformType.Image, 1, unsigned); using var subtracts = ViewUniform(UniformType.Image, 2, signed); using var sample = ViewUniform(UniformType.SamplerWithTexture, 3, sampler, originals[0]); var set = device.UniformSetCreate([writes, adds, subtracts, sample], shader, 0);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var factor = new RDPipelineSpecializationConstant<uint> { ConstantID = 0, Value = 1 }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, specializationConstants: [factor]);
        uint[] push = [0]; float[] pixels = new float[80]; int[] counts = new int[20];
        void Draw()
        {
            foreach (var image in unsigned) device.TextureClear(image, default, 0, 1, 0, 3); foreach (var image in signed) device.TextureClear(image, default, 0, 1, 0, 3);
            var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0);
            for (uint selected = 0; selected < 4; selected++) { push[0] = selected == 3 ? uint.MaxValue : selected; device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 4); device.DrawListDraw(list, false, 1, 3); }
            device.DrawListEnd();
            for (var element = 0; element < 2; element++) for (var layer = 0; layer < 3; layer++)
            {
                device.TextureGetData(destinations[element], (uint)layer, MemoryMarshal.AsBytes(pixels.AsSpan())); device.TextureGetData(unsigned[element], (uint)layer, MemoryMarshal.AsBytes(counts.AsSpan()));
                for (var i = 0; i < 16; i++)
                {
                    var active = i != 3; var increment = i < 3 ? 1 : 2;
                    Check(counts[i] == (active ? increment : 0), "Layered descriptor arrays suppress invalid atomic selectors without touching neighbors.");
                    Check(pixels[i * 4] == (active ? .375f : -7) && pixels[i * 4 + 1] == (active ? increment : -7) && pixels[i * 4 + 2] == (active ? -2 * increment : -7) && pixels[i * 4 + 3] == (active ? 3 : -7), "Both graphics stages preserve array coordinates, loads, dimensions and unsigned/signed atomic prior values.");
                }
                Check(pixels.AsSpan(64).IndexOfAnyExcept(-7f) < 0, "Graphics storage arrays preserve every higher mip.");
                device.TextureGetData(signed[element], (uint)layer, MemoryMarshal.AsBytes(counts.AsSpan())); for (var i = 0; i < 16; i++) Check(counts[i] == (i == 3 ? 0 : i < 3 ? -2 : -4), "Signed layered atomic final bytes remain exact.");
            }
        }
        Draw(); for (var i = 0; i < 16; i++) Draw(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared layered graphics descriptor/load/store/atomic cycles allocate zero owner bytes.");
        device.FreeRID(originals[1]); Check(!device.UniformSetIsValid(set), "Retiring a nonzero descriptor-array owner invalidates its shared array-image set.");
        foreach (var rid in new[] { pipeline, shader, set, sampler, target, color, originals[0] }) device.FreeRID(rid); foreach (var image in destinations) device.FreeRID(image); foreach (var image in signed) device.FreeRID(image);
        Console.WriteLine("PASS " + language + " array storage graphics: both stages, signed/unsigned descriptor-array atomics and prior values, load/store/dimensions, sampled same-image mip, invalid selectors, specialization and zero owner bytes.");
    }
}
