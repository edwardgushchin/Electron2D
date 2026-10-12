using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static class RenderingDeviceGraphicsTests
{
    internal static RDShaderSPIRV Load(string name)
    {
        byte[] Read(string stage) { using var input = typeof(RenderingDeviceGraphicsTests).Assembly.GetManifestResourceStream("Electron2D.Device." + name + "." + stage + ".spv")!; using var bytes = new MemoryStream(); input.CopyTo(bytes); return bytes.ToArray(); }
        return new() { BytecodeVertex = Read("vert"), BytecodeFragment = Read("frag") };
    }
    private static RID Image(RenderingDevice device, uint size = 16)
    {
        using var format = new RDTextureFormat { Width = size, Height = size, Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageBits = RenderingDevice.TextureUsageBits.ColorAttachment | RenderingDevice.TextureUsageBits.CanCopyFrom | RenderingDevice.TextureUsageBits.CanCopyTo | RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.Storage };
        using var view = new RDTextureView(); return device.TextureCreate(format, view);
    }
    internal static void Run()
    {
        using (var device = RenderingServer.CreateLocalRenderingDevice())
        {
            foreach (var language in new[] { "DeviceGLSL", "DeviceHLSL" }) Draw(device, language);
            Procedural(device); MultipleTargets(device);
            Console.WriteLine("Device graphics: GLSL/HLSL native vertex/index/instance drawing, copied pipeline state, blending, scissor, lifecycle and readback passed on " + device.GetDeviceName());
        }
        Canvas();
    }
    internal static void Export(RDShaderSPIRV module, string language)
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_GRAPHICS_OUTPUT") ?? Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_DEPTH_OUTPUT") ?? Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_MSAA_OUTPUT"); if (directory is null) return; Directory.CreateDirectory(directory);
        var remap = typeof(RenderingDevice).GetMethod("Remap", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        foreach (var stage in new[] { RenderingDevice.ShaderStage.Vertex, RenderingDevice.ShaderStage.Fragment })
        {
            var result = remap.Invoke(null, [module.GetStageBytecode(stage), stage])!; var bytes = (byte[])result.GetType().GetField("Item1")!.GetValue(result)!;
            File.WriteAllBytes(System.IO.Path.Combine(directory, language + "." + stage + ".spv"), bytes);
        }
    }
    private static void Draw(RenderingDevice device, string language)
    {
        using var module = Load(language); using var copy = (RDShaderSPIRV)module.Duplicate();
        copy.BytecodeVertex = []; Check(module.BytecodeVertex.Length > 0 && copy.BytecodeFragment.SequenceEqual(module.BytecodeFragment), "Graphics module duplication owns its bytecode.");
        Export(module, language);
        var archive = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-device-graphics-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try { ResourceSaver.Save(module, archive); using var restored = ResourceLoader.Load<RDShaderSPIRV>(archive, ResourceLoader.CacheMode.Ignore); Check(restored.BytecodeVertex.SequenceEqual(module.BytecodeVertex) && restored.BytecodeFragment.SequenceEqual(module.BytecodeFragment), "Graphics stages round-trip through the resource archive."); }
        finally { File.Delete(archive); }
        var shader = device.ShaderCreateFromSPIRV(module); Check(device.ShaderGetVertexInputAttributeMask(shader) == 3, "Vertex reflection exposes declared input locations.");
        using var position = new RDVertexAttribute { Binding = 0, Location = 0, Stride = 8, Format = RenderingDevice.DataFormat.R32G32SFLOAT };
        using var offset = new RDVertexAttribute { Binding = 1, Location = 1, Stride = 8, Format = RenderingDevice.DataFormat.R32G32SFLOAT, Frequency = RenderingDevice.VertexFrequency.Instance };
        var format = device.VertexFormatCreate([position, offset]);
        using (var other = RenderingServer.CreateLocalRenderingDevice())
        {
            var foreignFormat = other.VertexFormatCreate([position, offset]); Reject<ArgumentException>(() => device.VertexArrayCreate(1, foreignFormat, []));
            using var description = new RDAttachmentFormat { Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageFlags = RenderingDevice.TextureUsageBits.ColorAttachment }; var foreignTarget = other.FramebufferFormatCreate([description]); Reject<ArgumentException>(() => device.FramebufferFormatGetTextureSamples(foreignTarget));
        }
        float[] vertices = [-.9f, -.9f, -.1f, -.9f, -.1f, .9f, -.9f, .9f]; float[] offsets = [0, 0, 1, 0]; ushort[] indices = [0, 1, 2, 0, 2, 3];
        var v = device.VertexBufferCreate(32, MemoryMarshal.AsBytes(vertices.AsSpan())); var o = device.VertexBufferCreate(16, MemoryMarshal.AsBytes(offsets.AsSpan()));
        var i = device.IndexBufferCreate(6, RenderingDevice.IndexBufferFormat.UInt16, MemoryMarshal.AsBytes(indices.AsSpan()));
        var array = device.VertexArrayCreate(4, format, [v, o]); var indexArray = device.IndexArrayCreate(i, 0, 6);
        var output = Image(device); var framebuffer = device.FramebufferCreate([output]);
        Reject<ArgumentException>(() => device.FramebufferCreate([output, output]));
        Reject<ArgumentException>(() => device.FramebufferCreate([output, Image(device, 8)]));
        Reject<ArgumentOutOfRangeException>(() => device.DrawListBegin(framebuffer, region: new(0, 0, float.NaN, 1)));
        Reject<InvalidOperationException>(() => Task.Run(() => device.FramebufferGetFormat(framebuffer)).GetAwaiter().GetResult());
        Reject<ArgumentException>(() => device.ComputePipelineCreate(shader));
        using var attachmentFormat = new RDAttachmentFormat { Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageFlags = RenderingDevice.TextureUsageBits.ColorAttachment };
        var explicitFormat = device.FramebufferFormatCreate([attachmentFormat]); Check(device.FramebufferFormatGetTextureSamples(explicitFormat) == RenderingDevice.TextureSamples.Samples1, "Copied attachment format has one sample.");
        var validated = device.FramebufferCreate([output], explicitFormat); Check(device.FramebufferGetFormat(validated) == explicitFormat, "Framebuffer validates against an explicit matching layout.");
        Reject<ArgumentOutOfRangeException>(() => device.FramebufferFormatGetTextureSamples(explicitFormat, 1));
        var texture = Image(device, 1); device.TextureClear(texture, Colors.White, 0, 1, 0, 1);
        using var samplerDescription = new RDSamplerState(); var sampler = device.SamplerCreate(samplerDescription);
        float[] ones = [1, 1, 1, 1]; var storage = device.StorageBufferCreate(16, MemoryMarshal.AsBytes(ones.AsSpan())); var uniform = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(ones.AsSpan()));
        using var u = new RDUniform { Binding = 7, UniformType = RenderingDevice.UniformType.UniformBuffer }; u.AddID(uniform);
        using var s = new RDUniform { Binding = 8, UniformType = RenderingDevice.UniformType.StorageBuffer }; s.AddID(storage);
        using var t = new RDUniform { Binding = 13, UniformType = RenderingDevice.UniformType.SamplerWithTexture }; t.AddID(sampler); t.AddID(texture);
        using var shortUniform = new RDUniform { Binding = 7, UniformType = RenderingDevice.UniformType.UniformBuffer }; shortUniform.AddID(device.UniformBufferCreate(4));
        Reject<ArgumentException>(() => device.UniformSetCreate([shortUniform], shader, 5));
        using var storageImage = new RDUniform { Binding = 9, UniformType = RenderingDevice.UniformType.Image }; storageImage.AddID(texture);
        var us = device.UniformSetCreate([u], shader, 5); var ss = device.UniformSetCreate(language == "DeviceGLSL" ? [s, storageImage] : [s], shader, 6); var ts = device.UniformSetCreate([t], shader, 2);
        using var raster = new RDPipelineRasterizationState(); using var blendAttachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [blendAttachment] };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), format, RenderingDevice.RenderPrimitive.Triangles, raster, blend);
        position.Stride = 100; blendAttachment.WriteR = false; raster.CullMode = RenderingDevice.PolygonCullMode.Front;
        uint[] arguments = [6, 2, 0, 0, 0]; var indirect = device.StorageBufferCreate(20, MemoryMarshal.AsBytes(arguments.AsSpan()), RenderingDevice.StorageBufferUsage.DispatchIndirect);
        var bytes = new byte[16 * 16 * 4];
        void Render()
        {
            var list = device.DrawListBegin(framebuffer, RenderingDevice.DrawFlags.ClearColorAll, [Colors.Black]);
            device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindVertexArray(list, array); device.DrawListBindIndexArray(list, indexArray);
            device.DrawListBindUniformSet(list, us, 5); device.DrawListBindUniformSet(list, ss, 6); device.DrawListBindUniformSet(list, ts, 2);
            device.DrawListDraw(list, true, 2); device.DrawListDrawIndirect(list, true, indirect); device.DrawListEnd(); device.TextureGetData(output, 0, bytes.AsSpan());
        }
        using var feedback = new RDUniform { Binding = 13, UniformType = RenderingDevice.UniformType.SamplerWithTexture }; feedback.AddID(sampler); feedback.AddID(output); var feedbackSet = device.UniformSetCreate([feedback], shader, 2);
        var invalid = device.DrawListBegin(framebuffer);
        Reject<InvalidOperationException>(() => device.ComputeListBegin()); Reject<ArgumentException>(() => device.ComputeListBindUniformSet(invalid, us, 5));
        Reject<ArgumentException>(() => device.DrawListDraw(invalid + 1, true, 1)); Reject<ArgumentException>(device.ComputeListEnd);
        device.DrawListBindRenderPipeline(invalid, pipeline); device.DrawListBindVertexArray(invalid, array); device.DrawListBindIndexArray(invalid, indexArray);
        Reject<ArgumentException>(() => device.DrawListDraw(invalid, true, 3));
        device.DrawListBindUniformSet(invalid, us, 5); device.DrawListBindUniformSet(invalid, ss, 6); device.DrawListBindUniformSet(invalid, feedbackSet, 2);
        Reject<InvalidOperationException>(() => device.DrawListDraw(invalid, true, 1));
        Reject<InvalidOperationException>(() => device.FreeRID(output)); Reject<InvalidOperationException>(device.Dispose);
        device.DrawListEnd();
        Render();
        Check(bytes[(8 * 16 + 4) * 4] == 255 && bytes[(8 * 16 + 12) * 4] == 255 && bytes[0] == 0, "Indexed instanced quads execute and copied state remains unchanged.");
        for (var n = 0; n < 16; n++) Render();
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var n = 0; n < 32; n++) Render();
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared draw and span readback allocate zero owner-thread managed bytes.");
        var direct = device.DrawListBegin(framebuffer); device.DrawListBindRenderPipeline(direct, pipeline);
        device.DrawListBindVertexBuffersFormat(direct, format, 4, [v, o]); device.DrawListBindIndexArray(direct, indexArray);
        device.DrawListBindUniformSet(direct, us, 5); device.DrawListBindUniformSet(direct, ss, 6); device.DrawListBindUniformSet(direct, ts, 2); device.DrawListDraw(direct, true, 2); device.DrawListEnd();
        var read = new byte[32]; device.BufferGetData(v, read.AsSpan()); Check(read.AsSpan().SequenceEqual(MemoryMarshal.AsBytes(vertices.AsSpan())), "Vertex buffer readback retains uploaded data.");
        var snapshot = false; device.BufferGetDataAsync(v, _ => snapshot = true); device.Submit(); device.Sync(); Check(snapshot, "Vertex buffers share queued readback completion.");
        device.BufferCopy(v, o, 0, 0, 16); device.BufferClear(o, 0, 16); Check(device.BufferGetData(o).All(b => b == 0), "Graphics buffers share GPU transfers and clear.");
        device.FreeRID(v); Check(device.FramebufferIsValid(framebuffer), "Unrelated buffer free retains framebuffer.");
        device.FreeRID(output); Check(!device.FramebufferIsValid(framebuffer), "Freed attachment invalidates framebuffer.");
        device.FreeRID(shader); Check(!device.RenderPipelineIsValid(pipeline) && !device.UniformSetIsValid(us), "Shader free invalidates pipeline and sets.");
    }
    private static void Procedural(RenderingDevice device)
    {
        using var module = Load("DeviceProcedural"); Export(module, "DeviceProcedural"); var shader = device.ShaderCreateFromSPIRV(module); var output = Image(device); var target = device.FramebufferCreate([output]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); attachment.SetAsMix();
        using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderingDevice.RenderPrimitive.Triangles, raster, blend);
        using var compute = new RDShaderSPIRV(); using (var input = typeof(RenderingDeviceGraphicsTests).Assembly.GetManifestResourceStream("Electron2D.Compute.DeviceArguments.comp.spv")!) { using var bytes = new MemoryStream(); input.CopyTo(bytes); compute.BytecodeCompute = bytes.ToArray(); }
        var commandBuffer = device.StorageBufferCreate(48, usage: RenderingDevice.StorageBufferUsage.DispatchIndirect); var writer = device.ShaderCreateFromSPIRV(compute); var computePipeline = device.ComputePipelineCreate(writer);
        using var destination = new RDUniform { Binding = 0, UniformType = RenderingDevice.UniformType.StorageBuffer }; destination.AddID(commandBuffer); var commandSet = device.UniformSetCreate([destination], writer, 0);
        var commands = device.ComputeListBegin(); device.ComputeListBindComputePipeline(commands, computePipeline); device.ComputeListBindUniformSet(commands, commandSet, 0); device.ComputeListDispatch(commands, 1, 1, 1); device.ComputeListEnd();
        var list = device.DrawListBegin(target, RenderingDevice.DrawFlags.ClearColorAll, [Colors.Blue]); device.DrawListBindRenderPipeline(list, pipeline);
        device.DrawListEnableScissor(list, new(-20, -20, 28, 36)); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        var pixels = device.TextureGetData(output, 0); var inside = (8 * 16 + 4) * 4; var outside = (8 * 16 + 12) * 4;
        Check(pixels[inside] is >= 127 and <= 128 && pixels[inside + 2] is >= 127 and <= 128 && pixels[outside] == 0 && pixels[outside + 2] == 255, "Procedural triangle, source alpha mixing and scissor produce exact target pixels.");
        list = device.DrawListBegin(target, RenderingDevice.DrawFlags.ClearColorAll, [Colors.Blue]); device.DrawListBindRenderPipeline(list, pipeline);
        Reject<ArgumentOutOfRangeException>(() => device.DrawListDrawIndirect(list, false, commandBuffer, 1));
        Reject<ArgumentOutOfRangeException>(() => device.DrawListDrawIndirect(list, false, commandBuffer, drawCount: 3, stride: 32));
        device.DrawListDrawIndirect(list, false, commandBuffer, drawCount: 2, stride: 32); device.DrawListEnd();
        pixels = device.TextureGetData(output, 0); Check(pixels[inside] is >= 190 and <= 192 && pixels[inside + 2] is >= 63 and <= 65, "GPU-generated strided indirect commands execute two ordered blend draws without readback.");
        attachment.SrcColorBlendFactor = RenderingDevice.BlendFactor.ConstantColor; attachment.DstColorBlendFactor = RenderingDevice.BlendFactor.Zero;
        var dynamicPipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderingDevice.RenderPrimitive.Triangles, raster, blend, RenderingDevice.PipelineDynamicStateFlags.BlendConstants);
        list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); Reject<InvalidOperationException>(() => device.DrawListSetBlendConstants(list, Colors.White));
        device.DrawListBindRenderPipeline(list, dynamicPipeline); device.DrawListSetBlendConstants(list, new(.25f, .5f, .75f, 1)); device.DrawListEnableScissor(list); device.DrawListDisableScissor(list); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        pixels = device.TextureGetData(output, 0); Check(pixels[inside] is >= 63 and <= 65, $"Dynamic blend constants and restored full scissor execute in the native pass: {pixels[inside]}, {pixels[inside + 1]}, {pixels[inside + 2]}, {pixels[inside + 3]}.");
    }
    private static void MultipleTargets(RenderingDevice device)
    {
        var color = Image(device); using var view = new RDTextureView(); using var format = new RDTextureFormat { Width = 16, Height = 16, Format = RenderingDevice.DataFormat.R32G32B32A32UINT, UsageBits = RenderingDevice.TextureUsageBits.ColorAttachment | RenderingDevice.TextureUsageBits.CanCopyFrom };
        var numbers = device.TextureCreate(format, view); var target = device.FramebufferCreate([color, numbers]);
        var bits = new Color(BitConverter.Int32BitsToSingle(123), BitConverter.Int32BitsToSingle(45), BitConverter.Int32BitsToSingle(67), BitConverter.Int32BitsToSingle(-1));
        var list = device.DrawListBegin(target, RenderingDevice.DrawFlags.ClearColorAll, [Colors.Green, bits]); device.DrawListEnd();
        var cleared = MemoryMarshal.Cast<byte, uint>(device.TextureGetData(numbers, 0)); Check(cleared[0] == 123 && cleared[3] == uint.MaxValue, "Integer attachment clears preserve all raw lane bits, including NaN-shaped float representations.");
        using var module = Load("DeviceMRT"); Export(module, "DeviceMRT"); var shader = device.ShaderCreateFromSPIRV(module); using var raster = new RDPipelineRasterizationState(); using var a = new RDPipelineColorBlendStateAttachment(); using var b = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [a, b] };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderingDevice.RenderPrimitive.Triangles, raster, blend);
        list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        var output = device.TextureGetData(color, 0); var integers = MemoryMarshal.Cast<byte, uint>(device.TextureGetData(numbers, 0)); Check(output[0] == 0 && output[2] == 255 && integers[0] == 42 && integers[3] == 45, "Multiple float and unsigned color attachments receive correctly typed fragment outputs.");
        b.EnableBlend = true; Reject<ArgumentException>(() => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderingDevice.RenderPrimitive.Triangles, raster, blend));
    }
    private sealed class Patch(Texture texture) : Entity { protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 16, 16), false); }
    private static void Canvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var texture = new TextureRD(); var root = new Window { Size = new(32, 32) }; var frames = 0;
        root.Ready += _ =>
        {
            var device = RenderingServer.GetRenderingDevice()!; RenderingServer.SetDefaultClearColor(Colors.Black); var image = Image(device); var target = device.FramebufferCreate([image]);
            using var module = Load("DeviceProcedural"); Export(module, "DeviceProcedural"); var shader = device.ShaderCreateFromSPIRV(module);
            using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
            var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderingDevice.RenderPrimitive.Triangles, raster, blend);
            var list = device.DrawListBegin(target, RenderingDevice.DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
            texture.TextureRDRID = image; root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured;
        };
        void Captured()
        {
            if (++frames < 3) return;
            using var screenshot = root.GetTexture().GetImage()!; var directory = Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_GRAPHICS_OUTPUT"); if (directory is not null) screenshot.SavePNG(System.IO.Path.Combine(directory, "canvas.png")); Check(screenshot.GetPixel(8, 8).R is > .49f and < .51f && screenshot.GetPixel(8, 8).G < .01f && screenshot.GetPixel(8, 8).B < .01f, "Device graphics output reaches canvas through the retained GPU texture without a CPU copy."); root.Tree!.Quit();
        }
        Engine.Run(root);
        Console.WriteLine("Device graphics: retained texture reached rendered canvas pixels.");
        var interrupted = new Window { Size = new(32, 32) };
        interrupted.Ready += _ => { var device = RenderingServer.GetRenderingDevice()!; var image = Image(device); var target = device.FramebufferCreate([image]); device.DrawListBegin(target); throw new ArgumentException("Interrupted graphics authoring."); };
        Reject<AggregateException>(() => Engine.Run(interrupted)); Check(interrupted.IsDisposed && RenderingServer.GetRenderingDevice() is null, "Renderer shutdown closes an active render pass before cancelling its command buffer.");
    }
}
