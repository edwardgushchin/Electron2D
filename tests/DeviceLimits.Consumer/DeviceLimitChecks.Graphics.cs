using Electron2D;
using System.Buffers.Binary;
using static Electron2D.RenderingDevice;

internal static partial class DeviceLimitChecks
{
    private static void Graphics(RenderingDevice device)
    {
        var count = (int)device.LimitGet(Limit.MaxVertexInputAttributes);
        Check(count == 16 && device.LimitGet(Limit.MaxVertexInputBindings) == 16, "The native portable vertex profile supplies this 16-binding fixture.");
        var stride = (uint)device.LimitGet(Limit.MaxVertexInputBindingStride); var offset = Math.Min((uint)device.LimitGet(Limit.MaxVertexInputAttributeOffset), stride - 4) & ~3u;
        var attributes = Enumerable.Range(0, count).Select(i => new RDVertexAttribute { Binding = (uint)i, Location = (uint)i, Stride = stride, Offset = offset, Format = DataFormat.R32SFLOAT }).ToArray();
        try
        {
            var format = device.VertexFormatCreate(attributes); var data = new byte[2 * stride + offset + 4];
            for (var vertex = 0u; vertex < 3; vertex++) BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan((int)(vertex * stride + offset)), 1f / count);
            var buffers = Enumerable.Range(0, count).Select(_ => device.VertexBufferCreate((uint)data.Length, data)).ToArray(); var vertices = device.VertexArrayCreate(3, format, buffers);
            attributes[0].Stride = stride + 1; Reject<ArgumentException>(() => device.VertexFormatCreate(attributes)); attributes[0].Stride = stride;
            attributes[^1].Binding = (uint)device.LimitGet(Limit.MaxVertexInputBindings); Reject<ArgumentException>(() => device.VertexFormatCreate(attributes)); attributes[^1].Binding = (uint)count - 1;
            attributes[0].Offset = (uint)device.LimitGet(Limit.MaxVertexInputAttributeOffset) + 1; Reject<ArgumentException>(() => device.VertexFormatCreate(attributes)); attributes[0].Offset = offset;
            using var imageFormat = new RDTextureFormat { Width = 1, Height = 1, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom };
            using var view = new RDTextureView(); var colors = (int)device.LimitGet(Limit.MaxFramebufferColorAttachments);
            Check(colors == 8, "The current native eight-output fixture requires eight color targets.");
            var images = Enumerable.Range(0, colors).Select(_ => device.TextureCreate(imageFormat, view)).ToArray(); var framebuffer = device.FramebufferCreate(images);
            using var module = new RDShaderSPIRV { BytecodeVertex = Read("LimitDraw.vert"), BytecodeFragment = Read("LimitDraw.frag") };
            var shader = device.ShaderCreateFromSPIRV(module);
            using var raster = new RDPipelineRasterizationState(); var attachments = Enumerable.Range(0, colors).Select(_ => new RDPipelineColorBlendStateAttachment()).ToArray();
            try
            {
                using var blend = new RDPipelineColorBlendState { Attachments = attachments };
                var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), format, RenderPrimitive.Triangles, raster, blend);
                var list = device.DrawListBegin(framebuffer);
                try { device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindVertexArray(list, vertices); device.DrawListDraw(list, false, 1); }
                finally { device.DrawListEnd(); }
                for (var i = 0; i < images.Length; i++)
                {
                    var pixel = device.TextureGetData(images[i], 0); Check(Math.Abs(pixel[0] - 255f * (i + 1) / 8) <= 1 && pixel[1] == 255 && pixel[2] == 0 && pixel[3] == 255, "Maximum vertex binding/stride/offset and framebuffer target capacities execute native pixels.");
                }
                device.FreeRID(pipeline);
            }
            finally { foreach (var attachment in attachments) attachment.Dispose(); }
            device.FreeRID(shader); device.FreeRID(framebuffer); foreach (var image in images) device.FreeRID(image);
            device.FreeRID(vertices); foreach (var buffer in buffers) device.FreeRID(buffer);
        }
        finally { foreach (var attribute in attributes) attribute.Dispose(); }
    }
}
