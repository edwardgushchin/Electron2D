using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static RID TexelArraySet(RenderingDevice device, RID shader, ReadOnlySpan<RID> unsigned, ReadOnlySpan<RID> signed)
    {
        using var u = new RDUniform { Binding = 1, UniformType = UniformType.ImageBuffer }; using var s = new RDUniform { Binding = 2, UniformType = UniformType.ImageBuffer }; foreach (var id in unsigned) u.AddID(id); foreach (var id in signed) s.AddID(id); return device.UniformSetCreate([u, s], shader, 3);
    }
    private static void TexelAtomics(RenderingDevice device, string language)
    {
        using var module = Code("texel-atomic", language); if (language == "hlsl") { module.BytecodeVertex = SpecializeTexelArray(module.BytecodeVertex); module.BytecodeFragment = SpecializeTexelArray(module.BytecodeFragment); }
        ExportTexel(module, "atomic-" + language);
        var shader = device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(module)); using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]); using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var count = new RDPipelineSpecializationConstant<int> { ConstantID = 0, Value = 1 }; using var unsignedCount = new RDPipelineSpecializationConstant<uint> { ConstantID = 0, Value = 1 }; RDPipelineSpecializationConstant selectedCount = language == "glsl" ? count : unsignedCount;
        var normal = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend); var single = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, specializationConstants: [selectedCount]);
        RID[] unsigned = [device.TextureBufferCreate(2, DataFormat.R32UINT), device.TextureBufferCreate(2, DataFormat.R32UINT)]; RID[] signed = [device.TextureBufferCreate(2, DataFormat.R32SINT), device.TextureBufferCreate(2, DataFormat.R32SINT)]; var reports = device.StorageBufferCreate(128); var set = TexelArraySet(device, shader, unsigned, signed); var one = TexelArraySet(device, shader, unsigned.AsSpan(0, 1), signed.AsSpan(0, 1)); var output = TexelSet(device, shader, 4, 0, UniformType.StorageBuffer, reports); var data = new uint[32]; uint[] push = [0]; var pixels = new byte[64];
        void Reset() { foreach (var id in unsigned) device.BufferClear(id, 0, 8); foreach (var id in signed) device.BufferClear(id, 0, 8); device.BufferClear(reports, 0, 128); }
        void Record(RID pipeline, RID arrays, uint slot)
        {
            var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, arrays, 3); device.DrawListBindUniformSet(list, output, 4); push[0] = slot; device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 4); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.BufferGetData(reports, MemoryMarshal.AsBytes(data.AsSpan()));
        }
        uint[] expected = [0, 100, 107, 50, 75, 11, 139, 136, 99, 0, unchecked((uint)-100), unchecked((uint)-200), unchecked((uint)-150), unchecked((uint)-130)];
        for (uint slot = 0; slot < 2; slot++) { Reset(); Record(normal, set, slot); for (var lane = 0; lane < 2; lane++) Check(data.AsSpan(lane * 16, 14).SequenceEqual(expected), "Each graphics stage preserves atomic prior values, signedness and array selection."); var u = MemoryMarshal.Cast<byte, uint>(device.BufferGetData(unsigned[slot]).AsSpan()); var s = MemoryMarshal.Cast<byte, int>(device.BufferGetData(signed[slot]).AsSpan()); Check(u[0] == 11 && u[1] == 11 && s[0] == -2 && s[1] == -2, "Selected original formatted bytes retain atomic results."); }
        Reset(); Record(normal, set, 2); Check(data.All(v => v == 0) && device.BufferGetData(unsigned[0]).All(v => v == 0) && device.BufferGetData(signed[1]).All(v => v == 0), "Out-of-range descriptor choices return zero and suppress atomic writes.");
        var invalid = device.DrawListBegin(target); device.DrawListBindRenderPipeline(invalid, single); device.DrawListBindUniformSet(invalid, set, 3); device.DrawListBindUniformSet(invalid, output, 4); device.DrawListSetPushConstant(invalid, MemoryMarshal.AsBytes(push.AsSpan()), 4); Reject<ArgumentException>(() => device.DrawListDraw(invalid, false, 1, 3)); device.DrawListEnd();
        Reset(); Record(single, one, 0); Check(data.AsSpan(0, 14).SequenceEqual(expected), "Specialized descriptor count executes its one-resource layout after failed recording."); Reset(); Record(single, one, 1); Check(data.All(v => v == 0), "Specialized descriptor count bounds selectors at the executed size.");
        void Cycle() { Reset(); Record(normal, set, 1); device.TextureGetData(image, 0, pixels.AsSpan()); }
        for (var i = 0; i < 16; i++) Cycle(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Cycle(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared graphics atomic arrays allocate zero owner bytes.");
        device.FreeRID(unsigned[0]); Check(!device.UniformSetIsValid(set) && !device.UniformSetIsValid(one), "Retiring any borrowed array element invalidates dependent sets.");
        foreach (var resource in new[] { unsigned[1], signed[0], signed[1], reports, set, one, output, normal, single, shader, target, image }) device.FreeRID(resource);
        Console.WriteLine("PASS " + language + ": signed/unsigned graphics atomic prior values, dynamic array choices, specialized counts, invalid choices, recovery, borrowed lifetime and zero owner bytes.");
    }
}
