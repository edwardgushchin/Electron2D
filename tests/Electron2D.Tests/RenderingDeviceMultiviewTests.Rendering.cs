using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceMultiviewTests
{
    private sealed class Patch : Entity { protected override void OnDraw() => DrawRect(new(0, 0, 16, 16), Colors.White); }
    internal static void Canvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var layers = new TextureArrayRD(); using var glsl = Load("ArrayGLSL"); using var hlsl = Load("ArrayHLSL");
        using var white = Electron2D.Image.CreateEmpty(1, 1, false, Electron2D.Image.Format.Rgba8); white.Fill(Colors.White); using var ordinary = ImageTexture.CreateFromImage(white);
        glsl.SetDefaultTextureParameter("detailMap", ordinary); hlsl.SetDefaultTextureParameter("detailMap", ordinary);
        var materials = new List<ShaderMaterial>(); var root = new Window { Size = new(64, 64) }; var frames = 0; long before = 0;
        RenderingDevice device = null!; RID framebuffer = default, pipeline = default; float[] tint = [1, 1, 1, 1];
        try
        {
            root.Ready += _ =>
            {
                device = RenderingServer.GetRenderingDevice()!; var image = Image(device, layers: 3, sampled: true); framebuffer = device.FramebufferCreate([image], viewCount: 3);
                using var module = RenderingDeviceGraphicsTests.Load("DeviceMultiviewGLSL"); var shader = device.ShaderCreateFromSPIRV(module);
                using var raster = new RDPipelineRasterizationState(); using var a = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [a] };
                pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend); layers.TextureRDRID = image;
                foreach (var canvasShader in new[] { glsl, hlsl }) for (var layer = 0; layer < 3; layer++)
                    {
                        var material = new ShaderMaterial { Shader = canvasShader }; materials.Add(material); material.SetShaderLayeredParameter("layers", layers); material.SetShaderParameter("selectedLayer", (float)layer);
                        root.AddChild(new Patch { Position = new(layer * 16, ReferenceEquals(canvasShader, glsl) ? 0 : 16), Material = material });
                    }
                RenderingServer.FramePreDraw += Generate; RenderingServer.FramePostDraw += Captured;
            };
            Engine.Run(root);
            Check(root.IsDisposed && device.IsDisposed && RenderingServer.GetRenderingDevice() is null, "Retained multiview shutdown releases device and borrowed array bindings.");
        }
        finally { foreach (var material in materials) material.Dispose(); }
        Console.WriteLine("Multiview retained canvas: GLSL/HLSL sampled every generated view, 32 changing frames used zero managed owner bytes, and shutdown passed.");
        void Generate()
        {
            if (frames >= 8) before = GC.GetAllocatedBytesForCurrentThread();
            tint.AsSpan().Fill(frames % 2 == 0 ? 1 : .5f); tint[3] = 1;
            var list = device.DrawListBegin(framebuffer, DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline);
            device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(tint.AsSpan()), 16); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        }
        void Captured()
        {
            if (frames >= 8) Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared retained multiview generation/canvas submission allocates zero managed owner bytes.");
            if (frames is 0 or 39)
            {
                using var pixels = root.GetTexture().GetImage()!;
                for (var language = 0; language < 2; language++) for (var layer = 0; layer < 3; layer++)
                    {
                        var pixel = pixels.GetPixel(layer * 16 + 8, language * 16 + 8); var expected = (layer == 0 ? Colors.Red : layer == 1 ? new Color(0, 1, 0, 1) : Colors.Blue) * tint[0];
                        Check(Math.Abs(pixel.R - expected.R) < .02f && Math.Abs(pixel.G - expected.G) < .02f && Math.Abs(pixel.B - expected.B) < .02f, "Generated layered output reaches both native canvas shader languages without a CPU bridge.");
                    }
            }
            if (++frames != 40) return;
            RenderingServer.FramePreDraw -= Generate; RenderingServer.FramePostDraw -= Captured; root.Tree!.Quit();
        }
    }
}
