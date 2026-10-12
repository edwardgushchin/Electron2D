using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;
using static Electron2D.RenderingDevice;

internal static class RenderingDevicePushTests
{
    private static RDShaderSPIRV Compute(string language)
    {
        using var input = typeof(RenderingDevicePushTests).Assembly.GetManifestResourceStream("Electron2D.Compute.LocalPush" + language + ".comp.spv")!;
        using var stream = new MemoryStream(); input.CopyTo(stream); return new() { BytecodeCompute = stream.ToArray() };
    }
    internal static void Run()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        foreach (var language in new[] { "GLSL", "HLSL" }) { Compute(device, language); Graphics(device, language); }
        Capacity(device); Canvas();
        Console.WriteLine("Native push constants: compute, vertex/fragment, original std430 arrays/matrices/structs, copied snapshots and zero allocation passed on " + device.GetDeviceName());
    }
    private static void Export(byte[] source, ShaderStage stage, string name)
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_PUSH_OUTPUT"); if (directory is null) return; Directory.CreateDirectory(directory);
        var remap = typeof(RenderingDevice).GetMethod("Remap", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var result = remap.Invoke(null, [source, stage])!; var bytes = (byte[])result.GetType().GetField("Item1")!.GetValue(result)!;
        File.WriteAllBytes(System.IO.Path.Combine(directory, name + ".spv"), bytes);
    }
    private static void Compute(RenderingDevice device, string language)
    {
        using var module = Compute(language); Export(module.BytecodeCompute, ShaderStage.Compute, language + "-compute"); var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader); var replacement = device.ComputePipelineCreate(shader);
        var output = device.StorageBufferCreate(12); var uniform = device.UniformBufferCreate(16);
        using var data = new RDUniform { Binding = 7, UniformType = UniformType.StorageBuffer }; data.AddID(output); var dataSet = device.UniformSetCreate([data], shader, 3);
        using var user = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; user.AddID(uniform); var userSet = device.UniformSetCreate([user], shader, 0);
        var push = new float[21]; var payload = MemoryMarshal.AsBytes(push.AsSpan()); var indices = MemoryMarshal.Cast<float, uint>(push.AsSpan()); float[] multiplier = [2, 0, 0, 0]; var values = new float[3];
        push[0] = 2; push[1] = 3; push[2] = 5; push[4] = 7; push[5] = 11; push[6] = 13; push[7] = language == "GLSL" ? 101 : 103; push[8] = language == "GLSL" ? 103 : 101; push[9] = 17; push[12] = 31; push[18] = 19; push[19] = 23; push[20] = 29; indices[16] = 2;
        Reject<ArgumentException>(() => device.ComputeListSetPushConstant(100, pushBytes(), 84));
        var list = device.ComputeListBegin(); Reject<InvalidOperationException>(() => device.ComputeListSetPushConstant(list, pushBytes(), 84)); device.ComputeListBindComputePipeline(list, pipeline);
        device.ComputeListBindUniformSet(list, dataSet, 3); device.ComputeListBindUniformSet(list, userSet, 0);
        Reject<InvalidOperationException>(() => device.ComputeListDispatch(list, 1, 1, 1)); Reject<ArgumentException>(() => device.ComputeListSetPushConstant(list, pushBytes(), 80)); Reject<ArgumentException>(() => device.ComputeListSetPushConstant(list, new byte[80], 84));
        device.BufferUpdate(uniform, 0, 16, MemoryMarshal.AsBytes(multiplier.AsSpan())); device.ComputeListSetPushConstant(list, payload, 84); push[12] = 999; indices[16] = 0; indices[17] = 2;
        device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListAddBarrier(list);
        push[12] = 37; indices[16] = 1; indices[17] = 1; multiplier[0] = 3; device.BufferUpdate(uniform, 0, 16, MemoryMarshal.AsBytes(multiplier.AsSpan())); device.ComputeListSetPushConstant(list, payload, 84); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
        device.BufferGetData(output, MemoryMarshal.AsBytes(values.AsSpan())); Check(values[0] == 1301 && values[1] == 1936 && values[2] == 0, language + " native push preserves dynamic array/compact matrix/struct ABI and request-time copies: " + string.Join(",", values));
        list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); Reject<InvalidOperationException>(() => device.ComputeListDispatch(list, 1, 1, 1)); device.ComputeListSetPushConstant(list, payload, 84); device.ComputeListBindComputePipeline(list, replacement); Reject<InvalidOperationException>(() => device.ComputeListDispatch(list, 1, 1, 1)); device.ComputeListEnd();
        Reject<InvalidOperationException>(() => Task.Run(() => { var id = device.ComputeListBegin(); device.ComputeListEnd(); }).GetAwaiter().GetResult());
        void Warm()
        {
            var id = device.ComputeListBegin(); device.ComputeListBindComputePipeline(id, pipeline); device.ComputeListBindUniformSet(id, dataSet, 3); device.ComputeListBindUniformSet(id, userSet, 0); device.ComputeListSetPushConstant(id, MemoryMarshal.AsBytes(push.AsSpan()), 84); device.ComputeListDispatch(id, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(output, MemoryMarshal.AsBytes(values.AsSpan()));
        }
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); for (var i = 0; i < 16; i++) Warm(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Warm(); var allocated = GC.GetAllocatedBytesForCurrentThread() - before; Check(allocated == 0, "Prepared compute push/upload/dispatch/readback allocates zero bytes: " + allocated); Console.WriteLine(language + " compute push: 32 cycles, " + allocated + " managed bytes.");
        byte[] pushBytes() => MemoryMarshal.AsBytes(push.AsSpan()).ToArray();
    }
    private static void Capacity(RenderingDevice device)
    {
        using var module = Compute("Capacity"); var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader); var output = device.StorageBufferCreate(4);
        using var storage = new RDUniform { Binding = 7, UniformType = UniformType.StorageBuffer }; storage.AddID(output); var dataSet = device.UniformSetCreate([storage], shader, 3);
        var descriptors = new RDUniform[3];
        for (var i = 0; i < 3; i++) { float[] value = [i + 1, 0, 0, 0]; descriptors[i] = new() { Binding = i, UniformType = UniformType.UniformBuffer }; descriptors[i].AddID(device.UniformBufferCreate(16, MemoryMarshal.AsBytes(value.AsSpan()))); }
        var userSet = device.UniformSetCreate(descriptors, shader, 0); foreach (var descriptor in descriptors) descriptor.Dispose(); float[] payload = new float[32]; payload[0] = 4; payload[31] = 5;
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, dataSet, 3); device.ComputeListBindUniformSet(list, userSet, 0); device.ComputeListSetPushConstant(list, MemoryMarshal.AsBytes(payload.AsSpan()), 128); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); float[] result = new float[1]; device.BufferGetData(output, MemoryMarshal.AsBytes(result.AsSpan())); Check(result[0] == 15, "Full 128-byte push block coexists with three native uniform buffers.");
        using var over = Compute("OverCapacity"); Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(over));
        var original = module.BytecodeCompute; var words = MemoryMarshal.Cast<byte, uint>(original.AsSpan()); var changed = false;
        for (var i = 5; i < words.Length; i += (int)(words[i] >> 16)) if ((words[i] & 65535) == 43 && (words[i] >> 16) == 4 && words[i + 3] == 32) { words[i + 3] = 33; changed = true; break; }
        Check(changed, "Boundary module contains the pinned array length."); using var oversized = new RDShaderSPIRV { BytecodeCompute = original }; Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(oversized));
    }
    private static RID Graphics(RenderingDevice device, string language)
    {
        using var module = RenderingDeviceGraphicsTests.Load("DevicePush" + language); Export(module.BytecodeVertex, ShaderStage.Vertex, language + "-vertex"); Export(module.BytecodeFragment, ShaderStage.Fragment, language + "-fragment"); var shader = device.ShaderCreateFromSPIRV(module);
        using var format = new RDTextureFormat { Width = 32, Height = 16, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
        float[] ones = [1, 1, 1, 1]; var buffer = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(ones.AsSpan())); using var u = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; u.AddID(buffer); var set = device.UniformSetCreate([u], shader, 0);
        float[] payload = [1, 0, 0, 1, -.5f, 0, 0, 0]; var pixels = new byte[32 * 16 * 4];
        var list = device.DrawListBegin(target, DrawFlags.ClearColorAll, [Colors.Black]); Reject<InvalidOperationException>(() => device.DrawListSetPushConstant(list, new byte[32], 32)); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); Reject<InvalidOperationException>(() => device.DrawListDraw(list, false, 1, 3));
        device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(payload.AsSpan()), 32); payload[0] = 0; payload[1] = 1; payload[4] = .5f; device.DrawListDraw(list, false, 1, 3);
        payload[1] = 0; payload[2] = 1; device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(payload.AsSpan()), 32); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.TextureGetData(image, 0, pixels.AsSpan());
        var left = (8 * 32 + 8) * 4; var right = (8 * 32 + 24) * 4; Check(pixels[left] == 255 && pixels[left + 1] == 0 && pixels[right] == 0 && pixels[right + 2] == 255, language + " graphics push data controls both geometry and copied fragment color.");
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_PUSH_OUTPUT"); if (directory is not null) { using var proof = Electron2D.Image.CreateFromData(32, 16, false, Electron2D.Image.Format.Rgba8, pixels); proof.SavePNG(System.IO.Path.Combine(directory, language + "-push.png")); }
        void Warm()
        { var id = device.DrawListBegin(target); device.DrawListBindRenderPipeline(id, pipeline); device.DrawListBindUniformSet(id, set, 0); device.DrawListSetPushConstant(id, MemoryMarshal.AsBytes(payload.AsSpan()), 32); device.DrawListDraw(id, false, 1, 3); device.DrawListEnd(); device.TextureGetData(image, 0, pixels.AsSpan()); }
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); for (var i = 0; i < 16; i++) Warm(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Warm(); var allocated = GC.GetAllocatedBytesForCurrentThread() - before; Check(allocated == 0, "Prepared graphics push/draw/readback allocates zero bytes: " + allocated); Console.WriteLine(language + " graphics push: 32 cycles, " + allocated + " managed bytes."); return image;
    }
    private sealed class Patch(Texture texture) : Entity { protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 32, 16), false); }
    private static void Canvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var texture = new TextureRD(); using var root = new Window { Size = new(40, 24) }; var frames = 0;
        root.Ready += _ => { var device = RenderingServer.GetRenderingDevice()!; RenderingServer.SetDefaultClearColor(Colors.Black); texture.TextureRDRID = Graphics(device, "GLSL"); root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured; };
        void Captured()
        {
            if (++frames < 3) return; RenderingServer.FramePostDraw -= Captured; using var image = root.GetTexture().GetImage()!; Check(image.GetPixel(8, 8).R > .99f && image.GetPixel(24, 8).B > .99f, "Push-controlled geometry/color reach actual retained canvas pixels without CPU image copies.");
            var directory = Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_PUSH_OUTPUT"); if (directory is not null) image.SavePNG(System.IO.Path.Combine(directory, "canvas-push.png")); root.Tree!.Quit();
        }
        Engine.Run(root);
    }

}
