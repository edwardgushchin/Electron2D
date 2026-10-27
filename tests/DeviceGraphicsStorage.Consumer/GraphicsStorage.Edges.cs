using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static void Edges(RenderingDevice device, string language)
    {
        var writer = Shader(device, "write", language, false); var reader = Shader(device, "read", language, false);
        using var format = new RDTextureFormat { Width = 4, Height = 4, ArrayLayers = 2, TextureType = TextureType.TextureArray, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view);
        using var depthFormat = new RDTextureFormat { Width = 4, Height = 4, ArrayLayers = 2, TextureType = TextureType.TextureArray, Format = DataFormat.D32SFLOAT, UsageBits = TextureUsageBits.DepthStencilAttachment }; var depth = device.TextureCreate(depthFormat, view);
        using var first = new RDFramebufferPass { ColorAttachments = [0], DepthAttachment = 1 }; using var second = new RDFramebufferPass { ColorAttachments = [0], DepthAttachment = 1 }; var target = device.FramebufferCreateMultipass([image, depth], [first, second], viewCount: 2);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var depthState = new RDPipelineDepthStencilState { EnableDepthTest = true, EnableDepthWrite = true, DepthCompareOperator = CompareOperator.LessOrEqual }; using var factor = new RDPipelineSpecializationConstant<uint> { ConstantID = 0, Value = 2 };
        var write0 = device.RenderPipelineCreate(writer, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: depthState, specializationConstants: [factor]); var write1 = device.RenderPipelineCreate(writer, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: depthState, specializationConstants: [factor], forRenderPass: 1); var read1 = device.RenderPipelineCreate(reader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: depthState, forRenderPass: 1);
        var buffer = device.StorageBufferCreate(48, usage: StorageBufferUsage.DispatchIndirect); var writeSet = Set(device, writer, buffer); var readSet = Set(device, reader, buffer); uint[] push = [1]; uint[] result = new uint[12]; var pixels = new float[64];
        void Draw()
        {
            device.BufferClear(buffer, 0, 48); var list = device.DrawListBegin(target, DrawFlags.ClearAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, write0); device.DrawListBindUniformSet(list, writeSet, 0); push[0] = 1; device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 4); device.DrawListDraw(list, false, 1, 3);
            device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(list, write1); device.DrawListBindUniformSet(list, writeSet, 0); push[0] = 2; device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 4);
            device.DrawListDrawIndirect(list, false, buffer, offset: 16, drawCount: 0); device.DrawListDrawIndirect(list, false, buffer, offset: 16, drawCount: 2); device.DrawListBindRenderPipeline(list, read1); device.DrawListBindUniformSet(list, readSet, 0); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
            device.BufferGetData(buffer, MemoryMarshal.AsBytes(result.AsSpan()));
            for (uint layer = 0; layer < 2; layer++) { device.TextureGetData(image, layer, MemoryMarshal.AsBytes(pixels.AsSpan())); for (var at = 0; at < pixels.Length; at += 4) Check(Math.Abs(pixels[at] - 3.6f) < .0001 && Math.Abs(pixels[at + 1] - 1.6f) < .0001 && pixels[at + 2] == 0, "Every multiview layer sees all prior writes across direct, indirect, restart and subpass boundaries."); }
        }
        Draw(); Check(result[0] == 36 && result[1] == 160 && result[4] == 3 && result[8] == 3, "GPU-produced indirect commands and stage atomics execute in two views and two passes; zero draw count has no writes.");
        for (var i = 0; i < 16; i++) Draw(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared indirect multiview multipass writes allocate zero owner bytes.");
        device.FreeRID(buffer); Check(!device.UniformSetIsValid(writeSet) && !device.UniformSetIsValid(readSet), "Storage sets borrow buffer lifetime."); var invalid = device.DrawListBegin(target); device.DrawListBindRenderPipeline(invalid, write0); Reject<ArgumentException>(() => device.DrawListBindUniformSet(invalid, writeSet, 0)); device.DrawListEnd();
        device.FreeRID(writeSet); device.FreeRID(readSet); device.FreeRID(write0); device.FreeRID(write1); device.FreeRID(read1); device.FreeRID(writer); device.FreeRID(reader); device.FreeRID(target); device.FreeRID(image); device.FreeRID(depth);
        Console.WriteLine("PASS " + language + ": GPU-produced indirect draws, zero count, same-pass readonly consumer, copied specialization, multiview, multipass, depth, borrowed lifetime and 32 zero-owner-byte cycles.");
    }
}
