using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static void Empty(string mode)
    {
        if (mode == "empty-canvas") { EmptyCanvas(); EmptyCanvas(); return; }
        using var device = RenderingServer.CreateLocalRenderingDevice(); Console.WriteLine("Attachmentless device: " + device.GetDeviceName());
        if (mode is "empty-unavailable" or "empty-failure")
        {
            if (mode == "empty-unavailable")
            {
                Reject<NotSupportedException>(() => device.FramebufferFormatCreateEmpty());
                Reject<NotSupportedException>(() => device.FramebufferCreateEmpty(new(7, 5)));
            }
            else
            {
                device.FramebufferFormatCreateEmpty();
                for (var i = 0; i < 3; i++) Reject<InvalidOperationException>(() => device.FramebufferCreateEmpty(new(7, 5)));
            }
            var image = Execute(device, "glsl"); device.FreeRID(image);
            Console.WriteLine("PASS masked attachmentless adapter refuses unavailable/failed creation; ordinary graphics remains executable."); return;
        }
        foreach (var language in new[] { "glsl", "hlsl" })
        {
            var image = ExecuteEmpty(device, language, mode == "empty-source"); device.FreeRID(image);
        }
    }
    private static RID ExecuteEmpty(RenderingDevice device, string language, bool authoring = false)
    {
        var format = device.FramebufferFormatCreateEmpty(); Check(format == device.FramebufferFormatCreateEmpty() && device.FramebufferFormatGetTextureSamples(format) == TextureSamples.Samples1, "Empty format identity and exact sample query.");
        var target = device.FramebufferCreateEmpty(new(7, 5), validateWithFormat: format);
        Check(device.FramebufferIsValid(target) && device.FramebufferGetFormat(target) == format, "Empty target has retained format identity.");
        Reject<ArgumentOutOfRangeException>(() => device.FramebufferCreateEmpty(new(0, 5)));
        Reject<ArgumentOutOfRangeException>(() => device.FramebufferCreateEmpty(new(7, -1)));
        Reject<ArgumentOutOfRangeException>(() => device.FramebufferCreateEmpty(new(int.MaxValue, 5)));
        Reject<ArgumentOutOfRangeException>(() => device.FramebufferFormatCreateEmpty((TextureSamples)(-1)));
        Reject<ArgumentOutOfRangeException>(() => device.FramebufferFormatCreateEmpty(TextureSamples.Max));
        Reject<NotSupportedException>(() => device.FramebufferFormatCreateEmpty(TextureSamples.Samples64));
        Reject<ArgumentException>(() => device.FramebufferCreateEmpty(new(1, 1), validateWithFormat: int.MaxValue));
        var shader = Shader(device, "empty", language, authoring); var chain = Shader(device, "chain", language, authoring);
        using var raster = new RDPipelineRasterizationState(); using var blend = new RDPipelineColorBlendState();
        var pipeline = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend); var kernel = device.ComputePipelineCreate(chain);
        var buffer = device.StorageBufferCreate(172); var copied = device.StorageBufferCreate(172); uint[] arguments = [3, 1, 0, 0]; var indirect = device.StorageBufferCreate(16, MemoryMarshal.AsBytes(arguments.AsSpan()), StorageBufferUsage.DispatchIndirect); var set = Set(device, shader, buffer); var computeSet = Set(device, chain, buffer); var result = new uint[43];
        void Draw(bool cropped)
        {
            device.BufferClear(buffer, 0, 172);
            var list = device.DrawListBegin(target, region: cropped ? new(1, 1, 5, 3) : default);
            device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0);
            device.DrawListEnableScissor(list, cropped ? new(1, 1, 2, 3) : new(0, 0, 3, 5)); device.DrawListDraw(list, false, 1, 3);
            device.DrawListEnableScissor(list, cropped ? new(3, 1, 3, 3) : new(3, 0, 4, 5)); device.DrawListDrawIndirect(list, false, indirect, drawCount: 0); device.DrawListDrawIndirect(list, false, indirect);
            Reject<InvalidOperationException>(() => device.FreeRID(target));
            device.DrawListEnd();
        }
        void Read() { device.BufferCopy(buffer, copied, 0, 0, 172); device.BufferGetData(copied, MemoryMarshal.AsBytes(result.AsSpan())); }
        void Verify(bool cropped, uint samples = 1)
        {
            Check(result[0] == 6 && result[1] >= (cropped ? 15u : 35u) && result[1] <= (cropped ? 15u : 35u) * samples, "Vertex and fragment side effects execute without images.");
            uint count = 0;
            for (var y = 0; y < 5; y++) for (var x = 0; x < 7; x++)
                {
                    var active = !cropped || x >= 1 && x < 6 && y >= 1 && y < 4; var value = result[8 + x + y * 7];
                    Check(active ? value >= 1 && value <= samples : value == 0, "Framebuffer extent, region and preserved scissor cover the exact cells."); count += value;
                }
            Check(count == result[1], "Every fragment writes its own framebuffer cell.");
        }
        Draw(true); Read(); Verify(true); Draw(false); Read(); Verify(false);
        void Cycle()
        {
            Draw(false); var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, kernel); device.ComputeListBindUniformSet(list, computeSet, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); Read();
        }
        // Keep failure-case delegates outside the measured draw cycle.
        void Prepared()
        {
            device.BufferClear(buffer, 0, 172); var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListEnableScissor(list, new(0, 0, 3, 5)); device.DrawListDraw(list, false, 1, 3); device.DrawListEnableScissor(list, new(3, 0, 4, 5)); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
            list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, kernel); device.ComputeListBindUniformSet(list, computeSet, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); Read();
        }
        Cycle(); Verify(false); Check(result[2] == 41, "Attachmentless writes reach compute and readback.");
        for (var i = 0; i < 16; i++) Prepared(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Prepared(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared attachmentless cycles allocate zero owner bytes.");
        foreach (var samples in new[] { TextureSamples.Samples2, TextureSamples.Samples4, TextureSamples.Samples8 })
        {
            int sampled;
            try { sampled = device.FramebufferFormatCreateEmpty(samples); } catch (NotSupportedException) { Console.WriteLine("Unavailable exact empty sample count: " + samples); continue; }
            Check(sampled != format && device.FramebufferFormatGetTextureSamples(sampled) == samples, "Sample counts have distinct format identities.");
            Reject<ArgumentException>(() => device.FramebufferCreateEmpty(new(7, 5), samples, format));
            using var state = new RDPipelineMultisampleState { SampleCount = samples }; var oldTarget = target; var oldPipeline = pipeline;
            target = device.FramebufferCreateEmpty(new(7, 5), samples, sampled); pipeline = device.RenderPipelineCreate(shader, sampled, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state);
            Reject<ArgumentException>(() => device.RenderPipelineCreate(shader, sampled, -1, RenderPrimitive.Triangles, raster, blend));
            Draw(false); device.FreeRID(target); Read(); Verify(false, 1u << (int)samples); device.FreeRID(pipeline); target = oldTarget; pipeline = oldPipeline;
        }
        using var colorFormat = new RDTextureFormat { Width = 1, Height = 1, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling }; using var view = new RDTextureView(); var image = device.TextureCreate(colorFormat, view); var colorTarget = device.FramebufferCreate([image]);
        Reject<ArgumentException>(() => device.FramebufferCreateEmpty(new(1, 1), validateWithFormat: device.FramebufferGetFormat(colorTarget)));
        var reader = Shader(device, "read", language, authoring); var readerSet = Set(device, reader, buffer); using var attachment = new RDPipelineColorBlendStateAttachment(); using var colorBlend = new RDPipelineColorBlendState { Attachments = [attachment] }; var readPipeline = device.RenderPipelineCreate(reader, device.FramebufferGetFormat(colorTarget), -1, RenderPrimitive.Triangles, raster, colorBlend);
        Prepared(); var draw = device.DrawListBegin(colorTarget); device.DrawListBindRenderPipeline(draw, readPipeline); device.DrawListBindUniformSet(draw, readerSet, 0); device.DrawListDraw(draw, false, 1, 3); device.DrawListEnd();
        var pixel = new float[4]; device.TextureGetData(image, 0, MemoryMarshal.AsBytes(pixel.AsSpan())); Check(Math.Abs(pixel[0] - .6f) < .001f && Math.Abs(pixel[1] - .35f) < .001f && Math.Abs(pixel[2] - .41f) < .001f, "Attachmentless output reaches an ordinary color pass.");
        Draw(false); device.FreeRID(target); Read(); Verify(false); Check(!device.FramebufferIsValid(target), "Retirement completes recorded work and invalidates the target."); Reject<ArgumentException>(() => device.DrawListBegin(target));
        foreach (var rid in new[] { pipeline, readPipeline, kernel, set, computeSet, readerSet, buffer, copied, indirect, shader, chain, reader, colorTarget }) device.FreeRID(rid);
        Console.WriteLine("PASS " + language + ": attachmentless extent, region/scissor, exact samples, pass restart, compute/copy/readback, retirement and 32 zero-owner-byte cycles."); return image;
    }
    private static void EmptyCanvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var root = new Window { Size = new(16, 16) }; using var texture = new TextureRD(); var frames = 0;
        root.Ready += _ => { texture.TextureRDRID = ExecuteEmpty(RenderingServer.GetRenderingDevice()!, "glsl"); root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured; };
        void Captured()
        {
            if (++frames < 3) return; RenderingServer.FramePostDraw -= Captured; using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(8, 8); Check(Math.Abs(pixel.R - .6f) < .01f && Math.Abs(pixel.G - .35f) < .01f && Math.Abs(pixel.B - .41f) < .01f, "Attachmentless output reaches retained canvas pixels."); root.Tree!.Quit();
        }
        Engine.Run(root); Console.WriteLine("PASS attachmentless retained renderer canvas pixels.");
    }
}
