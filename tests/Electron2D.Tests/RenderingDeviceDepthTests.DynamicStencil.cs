using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceDepthTests
{
    internal static void RunDynamicStencil(bool unsupported)
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        const TextureUsageBits depthUsage = TextureUsageBits.DepthStencilAttachment;
        var format = device.TextureIsFormatSupportedForUsage(DataFormat.D24UNORMS8UINT, depthUsage) ? DataFormat.D24UNORMS8UINT : DataFormat.D32SFLOATS8UINT;
        var depth = Image(device, format, depthUsage, 2);
        var color = Image(device, DataFormat.R8G8B8A8UNORM, TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom, 2);
        using var first = new RDFramebufferPass { ColorAttachments = [0], DepthAttachment = 1 };
        using var second = new RDFramebufferPass { ColorAttachments = [0], DepthAttachment = 1 };
        var frame = device.FramebufferCreateMultipass([color, depth], [first, second], viewCount: 2);
        using var module = RenderingDeviceGraphicsTests.Load("DeviceDepthGLSL");
        var shader = device.ShaderCreateFromSPIRV(module);
        using var raster = new RDPipelineRasterizationState();
        using var colorState = new RDPipelineColorBlendStateAttachment();
        using var blend = new RDPipelineColorBlendState { Attachments = [colorState] };
        using var stencil = new RDPipelineDepthStencilState
        {
            EnableStencil = true,
            FrontOpReference = 0x30,
            BackOpReference = 0x60,
            FrontOpCompare = CompareOperator.Equal,
            BackOpCompare = CompareOperator.Equal,
            FrontOpCompareMask = 0xf0,
            BackOpCompareMask = 0xf0,
            FrontOpWriteMask = 0x0f,
            BackOpWriteMask = 0xf0,
            FrontOpFail = StencilOperation.Keep,
            BackOpFail = StencilOperation.Keep,
            FrontOpPass = StencilOperation.Replace,
            BackOpPass = StencilOperation.Replace
        };
        const PipelineDynamicStateFlags all = PipelineDynamicStateFlags.StencilCompareMask | PipelineDynamicStateFlags.StencilWriteMask | PipelineDynamicStateFlags.StencilReference;
        RID Pipeline(PipelineDynamicStateFlags flags, uint pass) => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(frame), -1,
            RenderPrimitive.Triangles, raster, blend, flags, stencil, forRenderPass: pass);
        if (unsupported)
        {
            Reject<NotSupportedException>(() => Pipeline(all, 0));
            Console.WriteLine("Dynamic stencil rejected without a matching Vulkan command adapter.");
            return;
        }

        Check((int)all == 112, "Dynamic stencil bits retain their public identities.");
        Reject<ArgumentOutOfRangeException>(() => Pipeline((PipelineDynamicStateFlags)128, 0));
        stencil.EnableStencil = false;
        Reject<ArgumentException>(() => Pipeline(all, 0));
        stencil.EnableStencil = true;
        var writer = Pipeline(all, 0);
        stencil.FrontOpPass = stencil.BackOpPass = StencilOperation.Keep;
        stencil.FrontOpCompareMask = 0x0f;
        stencil.BackOpCompareMask = 0xf0;
        stencil.FrontOpReference = 0x0d;
        stencil.BackOpReference = 0xb0;
        var reader = Pipeline(PipelineDynamicStateFlags.StencilReference, 1);
        var fixedReader = Pipeline(0, 1);
        stencil.FrontOpReference = stencil.BackOpReference = 0;
        RID Uniforms(Color tint, bool front)
        {
            var values = new float[] { tint.R, tint.G, tint.B, tint.A, .5f, front ? 1 : 0, 0, 0 };
            var buffer = device.UniformBufferCreate(32, MemoryMarshal.AsBytes(values.AsSpan()));
            using var descriptor = new RDUniform { Binding = 7, UniformType = UniformType.UniformBuffer };
            descriptor.AddID(buffer);
            return device.UniformSetCreate([descriptor], shader, 5);
        }
        var front = Uniforms(Colors.Blue, true);
        var back = Uniforms(Colors.Green, false);
        var writeFront = Uniforms(Colors.Red, true);
        var writeBack = Uniforms(Colors.Red, false);
        var whiteFront = Uniforms(Colors.White, true);
        var whiteBack = Uniforms(Colors.White, false);
        uint[] command = [3, 1, 0, 0];
        var indirect = device.StorageBufferCreate(16, MemoryMarshal.AsBytes(command.AsSpan()), StorageBufferUsage.DispatchIndirect);
        var clear = new[] { Colors.Black };
        var left = new Rect2(0, 0, 8, 16);
        var right = new Rect2(8, 0, 8, 16);
        var lowerLeft = new Rect2(0, 8, 8, 8);
        var lowerRight = new Rect2(8, 8, 8, 8);
        var bytes = new byte[16 * 16 * 4];
        void Draw(long list, RID uniformSet, Rect2 region, bool useIndirect = false)
        {
            device.DrawListEnableScissor(list, region);
            device.DrawListBindUniformSet(list, uniformSet, 5);
            if (useIndirect) device.DrawListDrawIndirect(list, false, indirect);
            else device.DrawListDraw(list, false, 1, 3);
        }
        void Record(bool validate)
        {
            var list = device.DrawListBegin(frame, DrawFlags.ClearAll, clear, clearStencilValue: 0x32);
            if (validate)
            {
                var handle = list;
                Reject<InvalidOperationException>(() => device.DrawListSetStencilReference(handle, 1, 2));
            }
            device.DrawListBindRenderPipeline(list, writer);
            device.DrawListSetStencilCompareMask(list, 0x1f0, 0x10f);
            device.DrawListSetStencilWriteMask(list, 0x10f, -16);
            device.DrawListSetStencilReference(list, 0x3d, 0xb2);
            if (validate)
            {
                var handle = list;
                Reject<ArgumentOutOfRangeException>(() => device.DrawListSetStencilReference(handle, 256, 0));
            }
            Draw(list, writeFront, left);
            Draw(list, writeBack, right);
            device.DrawListSetStencilReference(list, 0x3e, 0xc2);
            Draw(list, writeFront, lowerLeft);
            Draw(list, writeBack, lowerRight, useIndirect: true);
            Check(device.DrawListSwitchToNextPass() == list, "The next pass retains the draw-list identity.");
            if (validate)
            {
                var handle = list;
                Reject<InvalidOperationException>(() => device.DrawListSetStencilReference(handle, 1, 2));
            }
            device.DrawListBindRenderPipeline(list, reader);
            if (validate)
            {
                var handle = list;
                Reject<InvalidOperationException>(() => device.DrawListSetStencilWriteMask(handle, 0, 0));
                Reject<InvalidOperationException>(() => device.DrawListSetStencilCompareMask(handle, 0, 0));
            }
            device.DrawListSetStencilReference(list, 0x0e, 0xc0);
            Draw(list, whiteFront, left);
            Draw(list, whiteBack, right);
            device.DrawListBindRenderPipeline(list, fixedReader);
            if (validate)
            {
                var handle = list;
                Reject<InvalidOperationException>(() => device.DrawListSetStencilReference(handle, 1, 2));
            }
            Draw(list, front, new Rect2(0, 4, 8, 4));
            Draw(list, back, new Rect2(8, 4, 8, 4));
            device.DrawListBindRenderPipeline(list, reader);
            Draw(list, whiteFront, new Rect2(0, 0, 8, 4));
            Draw(list, whiteBack, new Rect2(8, 0, 8, 4));
            device.DrawListEnd();
            if (validate)
            {
                var handle = list;
                Reject<ArgumentException>(() => device.DrawListSetStencilReference(handle, 1, 2));
            }
            for (uint layer = 0; layer < 2; layer++)
            {
                device.TextureGetData(color, layer, bytes.AsSpan());
                CheckPixel(4, 2, 255, 255, 255, layer, "The front reference resets when the dynamic pipeline is rebound.");
                CheckPixel(12, 2, 255, 255, 255, layer, "The back reference resets when the dynamic pipeline is rebound.");
                CheckPixel(4, 6, 0, 0, 255, layer, "The static pipeline retains the copied front reference.");
                CheckPixel(12, 6, 0, 255, 0, layer, "The static pipeline retains the copied back reference.");
                CheckPixel(4, 12, 255, 255, 255, layer, "Front compare and write masks produce the expected stencil.");
                CheckPixel(12, 12, 255, 255, 255, layer, "The indirect draw uses the dynamic back compare and write masks.");
            }
        }
        void CheckPixel(int x, int y, byte r, byte g, byte b, uint layer, string message)
        {
            var at = (y * 16 + x) * 4;
            if (bytes[at] != r || bytes[at + 1] != g || bytes[at + 2] != b)
                throw new InvalidOperationException($"{message} Layer {layer}, pixel ({x}, {y}): {bytes[at]}, {bytes[at + 1]}, {bytes[at + 2]}.");
        }

        Record(true);
        for (var i = 0; i < 32; i++) Record(false);
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 32; i++) Record(false);
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Warmed dynamic stencil recording and readback allocate zero managed owner bytes.");
        Console.WriteLine("Dynamic stencil: distinct per-face masks and references, two views/passes, binding resets, failure guards and 32 warm cycles passed on " + device.GetDeviceName());
    }
}
