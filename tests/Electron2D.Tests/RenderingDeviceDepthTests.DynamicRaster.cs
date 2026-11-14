using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceDepthTests
{
    internal static void RunDynamicRaster(bool unsupported)
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        using var raster = new RDPipelineRasterizationState();
        using var attachment = new RDPipelineColorBlendStateAttachment();
        using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        using var lineModule = RenderingDeviceGraphicsTests.Load("DeviceDynamicLine");
        var lineShader = device.ShaderCreateFromSPIRV(lineModule);
        var color = Image(device, DataFormat.R8G8B8A8UNORM, TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom, 2);
        var lineFrame = device.FramebufferCreate([color], viewCount: 2);
        RID LinePipeline(PipelineDynamicStateFlags flags) => device.RenderPipelineCreate(lineShader, device.FramebufferGetFormat(lineFrame),
            -1, RenderPrimitive.Lines, raster, blend, flags);
        if (unsupported)
        {
            Reject<NotSupportedException>(() => LinePipeline(PipelineDynamicStateFlags.LineWidth));
            Console.WriteLine("Dynamic raster rejected without a matching Vulkan command adapter.");
            return;
        }

        var line = LinePipeline(PipelineDynamicStateFlags.LineWidth);
        var staticLine = LinePipeline(0);
        raster.LineWidth = 8;
        var black = new[] { Colors.Black };
        var pixels = new byte[16 * 16 * 4];
        var list = device.DrawListBegin(lineFrame, DrawFlags.ClearColorAll, black);
        device.DrawListBindRenderPipeline(list, line);
        Reject<ArgumentOutOfRangeException>(() => device.DrawListSetLineWidth(list, float.NaN));
        Reject<ArgumentOutOfRangeException>(() => device.DrawListSetLineWidth(list, 0));
        device.DrawListSetLineWidth(list, 5);
        device.DrawListEnableScissor(list, new(0, 0, 8, 16));
        device.DrawListDraw(list, false, 1, 2);
        device.DrawListBindRenderPipeline(list, staticLine);
        Reject<InvalidOperationException>(() => device.DrawListSetLineWidth(list, 5));
        device.DrawListBindRenderPipeline(list, line);
        device.DrawListEnableScissor(list, new(8, 0, 8, 16));
        device.DrawListDraw(list, false, 1, 2);
        device.DrawListEnd();
        for (uint layer = 0; layer < 2; layer++)
        {
            device.TextureGetData(color, layer, pixels.AsSpan());
            int Count(int x)
            {
                var count = 0;
                for (var y = 0; y < 16; y++) if (pixels[(y * 16 + x) * 4] == 255) count++;
                return count;
            }
            Check(Count(4) == 5 && Count(11) == 1, "Line width changes and copied defaults replay for every view.");
        }

        using var module = RenderingDeviceGraphicsTests.Load("DeviceDepthGLSL");
        var shader = device.ShaderCreateFromSPIRV(module);
        var depth = Image(device, DataFormat.D32SFLOAT, TextureUsageBits.DepthStencilAttachment, 2);
        using var pass = new RDFramebufferPass { ColorAttachments = [0], DepthAttachment = 1 };
        var frame = device.FramebufferCreateMultipass([color, depth], [pass, pass], viewCount: 2);
        using var state = new RDPipelineDepthStencilState { EnableDepthTest = true, DepthCompareOperator = CompareOperator.Less };
        const PipelineDynamicStateFlags flags = PipelineDynamicStateFlags.DepthBias | PipelineDynamicStateFlags.DepthBounds;
        RID DepthPipeline(uint passIndex) => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(frame), -1,
            RenderPrimitive.Triangles, raster, blend, flags, state, forRenderPass: passIndex);
        raster.LineWidth = 1;
        Reject<ArgumentException>(() => DepthPipeline(0));
        raster.DepthBiasEnabled = true;
        Reject<ArgumentException>(() => DepthPipeline(0));
        state.EnableDepthRange = true;
        state.DepthRangeMin = .4f;
        state.DepthRangeMax = .6f;
        var first = DepthPipeline(0);
        var second = DepthPipeline(1);
        raster.DepthBiasConstantFactor = 1e8f;
        state.DepthRangeMax = .45f;
        RID Uniforms(Color tint, float z, float slope = 0)
        {
            float[] values = [tint.R, tint.G, tint.B, tint.A, z, 0, slope, 0];
            var buffer = device.UniformBufferCreate(32, MemoryMarshal.AsBytes(values.AsSpan()));
            using var uniform = new RDUniform { Binding = 7, UniformType = UniformType.UniformBuffer };
            uniform.AddID(buffer);
            return device.UniformSetCreate([uniform], shader, 5);
        }
        var blue = Uniforms(Colors.Blue, .25f);
        var green = Uniforms(Colors.Green, .75f);
        var red = Uniforms(Colors.Red, .5f, .2f);
        uint[] args = [3, 1, 0, 0];
        var indirect = device.StorageBufferCreate(16, MemoryMarshal.AsBytes(args.AsSpan()), StorageBufferUsage.DispatchIndirect);
        void Draw(long id, RID set, Rect2 region, bool useIndirect = false)
        {
            device.DrawListEnableScissor(id, region);
            device.DrawListBindUniformSet(id, set, 5);
            if (useIndirect) device.DrawListDrawIndirect(id, false, indirect);
            else device.DrawListDraw(id, false, 1, 3);
        }
        void Record()
        {
            var id = device.DrawListBegin(frame, DrawFlags.ClearAll, black, clearDepthValue: .5f);
            device.DrawListBindRenderPipeline(id, first);
            device.DrawListSetDepthBounds(id, .5f, .5f);
            device.DrawListSetDepthBias(id, 1e8f, 0, 0);
            Draw(id, blue, new(0, 0, 8, 8));
            device.DrawListSetDepthBias(id, 1e8f, .001f, 0);
            Draw(id, blue, new(8, 0, 8, 8));
            device.DrawListSetDepthBounds(id, .6f, 1);
            Draw(id, blue, new(0, 8, 8, 8));
            device.DrawListSwitchToNextPass();
            device.DrawListBindRenderPipeline(id, second);
            device.DrawListSetDepthBias(id, -1e8f, -.001f, 0);
            Draw(id, green, new(0, 0, 8, 8));
            device.DrawListSetDepthBias(id, -1e8f, 0, 0);
            Draw(id, green, new(0, 10, 4, 6), useIndirect: true);
            device.DrawListSetDepthBias(id, 0, 0, -10);
            Draw(id, red, new(4, 10, 4, 6));
            device.DrawListBindRenderPipeline(id, second);
            Draw(id, blue, new(8, 8, 8, 8));
            device.DrawListEnd();
            for (uint layer = 0; layer < 2; layer++)
            {
                device.TextureGetData(color, layer, pixels.AsSpan());
                Pixel(4, 4, 0, 0, 0);
                Pixel(12, 4, 0, 0, 255);
                Pixel(2, 9, 0, 0, 0);
                Pixel(2, 12, 0, 255, 0);
                Pixel(6, 12, 255, 0, 0);
                Pixel(12, 12, 0, 0, 255);
            }
        }
        void Pixel(int x, int y, byte r, byte g, byte b)
        {
            var at = (y * 16 + x) * 4;
            if (pixels[at] != r || pixels[at + 1] != g || pixels[at + 2] != b)
                throw new InvalidOperationException($"Dynamic depth pixel ({x}, {y}) was {pixels[at]}, {pixels[at + 1]}, {pixels[at + 2]}.");
        }
        list = device.DrawListBegin(frame);
        device.DrawListBindRenderPipeline(list, first);
        Reject<ArgumentOutOfRangeException>(() => device.DrawListSetDepthBounds(list, .8f, .1f));
        Reject<ArgumentOutOfRangeException>(() => device.DrawListSetDepthBounds(list, 0, float.PositiveInfinity));
        Reject<ArgumentOutOfRangeException>(() => device.DrawListSetDepthBias(list, 0, 0, float.NaN));
        device.DrawListEnd();
        Reject<ArgumentException>(() => device.DrawListSetDepthBounds(list, 0, 1));
        for (var i = 0; i < 32; i++) Record();
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 32; i++) Record();
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Dynamic depth records and reads back with zero warmed owner-thread allocations.");
        Console.WriteLine("Dynamic line width, depth bias and bounds, two views and passes, and 32 warmed depth cycles passed on " + device.GetDeviceName());
    }
}
