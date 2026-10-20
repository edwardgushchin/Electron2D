using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;
using IOPath = System.IO.Path;

internal static partial class RenderingDeviceBinaryTests
{
    private static RID Graphics(RenderingDevice device, byte[] binary, Action<Action<bool>>? retain = null)
    {
        var placeholder = device.ShaderCreatePlaceholder(); var shader = device.ShaderCreateFromBytecode(binary, placeholder); Check(shader == placeholder && device.ShaderGetVertexInputAttributeMask(shader) == 0, "Paired binary stages populate the same RID and retain their procedural vertex interface.");
        using var format = new RDTextureFormat { Width = 32, Height = 16, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
        float[] ones = [1, 1, 1, 1], payload = [1, 0, 0, 1, -.5f, 0, 0, 0]; var uniform = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(ones.AsSpan())); var set = Set(device, shader, 0, 0, UniformType.UniformBuffer, uniform); var pixels = new byte[2048];
        void Work(bool swap)
        {
            var list = device.DrawListBegin(target, DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0);
            payload[0] = swap ? 0 : 1; payload[2] = swap ? 1 : 0; payload[4] = -.5f; device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(payload.AsSpan()), 32); device.DrawListDraw(list, false, 1, 3);
            payload[0] = swap ? 1 : 0; payload[2] = swap ? 0 : 1; payload[4] = .5f; device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(payload.AsSpan()), 32); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        }
        Work(false); device.TextureGetData(image, 0, pixels.AsSpan()); var left = (8 * 32 + 8) * 4; var right = (8 * 32 + 24) * 4;
        Check(pixels[left] == 255 && pixels[left + 2] == 0 && pixels[right] == 0 && pixels[right + 2] == 255, "Loaded paired graphics preserve push snapshots, stage descriptors and actual red/blue pixels.");
        void Cycle() { Work(true); device.TextureGetData(image, 0, pixels.AsSpan()); }
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); for (var i = 0; i < 16; i++) Cycle(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Cycle(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared binary graphics/readback has zero owner managed bytes.");
        if (retain is not null) { retain(Work); return image; }
        device.FreeRID(shader); Check(!device.RenderPipelineIsValid(pipeline) && !device.UniformSetIsValid(set), "Paired binary lifetime invalidates graphics borrowers."); device.FreeRID(pipeline); device.FreeRID(set); device.FreeRID(uniform); device.FreeRID(target); device.FreeRID(image); return default;
    }
    internal static void Canvas(string language)
    {
        Check(language is "GLSL" or "HLSL", "Canvas binary test selects a supported language.");
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var texture = new TextureRD(); using var root = new Window { Size = new(256, 128) }; var frames = 0; long before = 0; Action<bool> work = null!; RenderingDevice device = null!;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); device = RenderingServer.GetRenderingDevice()!; using var module = RenderingDeviceGraphicsTests.Load("DevicePush" + language); var binary = device.ShaderCompileBinaryFromSPIRV(module, "Retained binary " + language);
            texture.TextureRDRID = Graphics(device, binary, callback => work = callback); Array.Fill(binary, (byte)0); module.Dispose(); root.AddChild(new Sprite { Texture = texture, Centered = false, Position = new(8, 8), Scale = new(4, 4) }); RenderingServer.FramePreDraw += Generate; RenderingServer.FramePostDraw += Captured;
        };
        void Generate() { if (frames >= 8) before = GC.GetAllocatedBytesForCurrentThread(); work(frames % 2 != 0); }
        void Captured()
        {
            if (frames >= 8) Check(GC.GetAllocatedBytesForCurrentThread() == before, "Binary-program draw and retained canvas frames have zero owner managed bytes.");
            if (frames is 0 or 39)
            {
                using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(40, 40); Check(Math.Abs(pixel.R - (frames % 2 == 0 ? 1 : 0)) < .01f && Math.Abs(pixel.B - (frames % 2 == 0 ? 0 : 1)) < .01f, "Retained canvas observes binary shader output on both frame states.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_BINARY_OUTPUT") is { } output) { Directory.CreateDirectory(output); image.SavePNG(IOPath.Combine(output, language + "-canvas-" + frames + ".png")); }
            }
            if (++frames != 40) return; RenderingServer.FramePreDraw -= Generate; RenderingServer.FramePostDraw -= Captured; root.Tree!.Quit();
        }
        Engine.Run(root); Check(root.IsDisposed && device.IsDisposed, "Retained binary shader programs retire with the renderer session."); Console.WriteLine(language + " binary graphics reaches canvas; 32 prepared full frames allocate zero owner managed bytes.");
    }
}
