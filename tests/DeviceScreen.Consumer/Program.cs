using Electron2D;

try
{
    using (var local = RenderingServer.CreateLocalRenderingDevice())
        Check(local.ScreenGetWidth() == RenderingDevice.InvalidID && local.ScreenGetHeight() == RenderingDevice.InvalidID && local.ScreenGetFramebufferFormat() == RenderingDevice.InvalidID && local.DrawListBeginForScreen() == RenderingDevice.InvalidID, "Local device screen contract.");
    ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
    var hdr = args.Contains("hdr");
    using var root = new Window { Size = new(96, 80), Borderless = true, UseHDR2D = hdr };
    var child = new Window { Size = new(80, 64), Borderless = true };
    root.AddChild(new ColorRect { Size = new(256, 256), Color = Colors.Green }); root.AddChild(child);
    RenderingDevice device = null!; RID shader = default, pipeline = default; var frames = 0; var childID = 0;
    root.Ready += _ =>
    {
        device = RenderingServer.GetRenderingDevice()!; childID = child.GetWindowID();
        Check(childID > 0 && device.ScreenGetWidth() > 0 && device.ScreenGetHeight(childID) > 0, "Native window screen identities.");
        using var module = new RDShaderSPIRV
        {
            BytecodeVertex = Read("vert"),
            BytecodeFragment = Read("frag"),
        };
        shader = device.ShaderCreateFromSPIRV(module);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment();
        using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var format = device.ScreenGetFramebufferFormat();
        Check(format == device.ScreenGetFramebufferFormat(childID), "Reusable native SDR screen layout.");
        pipeline = device.RenderPipelineCreate(shader, format, -1, RenderingDevice.RenderPrimitive.Triangles, raster, blend);
        RenderingServer.FramePreDraw += () =>
        {
            for (var i = 0; i < 2; i++)
            {
                var screen = i == 0 ? 0 : childID;
                var list = device.DrawListBeginForScreen(screen, Colors.Blue);
                device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
            }
        };
        RenderingServer.FramePostDraw += () =>
        {
            using var canvas = root.GetTexture().GetImage()!;
            var value = canvas.GetPixel(24, 24);
            Check(MathF.Abs(value.G - (hdr ? Colors.Green.SRGBToLinear().G : Colors.Green.G)) < .01f && value.R < .01f && value.B < .01f, "Screen rendering preserves public canvas texture output.");
            if (++frames == 3) { device.FreeRID(pipeline); device.FreeRID(shader); root.Tree!.Quit(); }
        };
    };
    Engine.Run(root);
    Check(frames == 3 && root.IsDisposed && child.IsDisposed && !RenderingServer.IsAvailable, "Public native screen host completes and retires.");
    Console.WriteLine("Public native screen consumer passed.");
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }

static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

static byte[] Read(string stage)
{
    using var input = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Electron2D.Device.DeviceProcedural." + stage + ".spv")!;
    using var bytes = new MemoryStream(); input.CopyTo(bytes); return bytes.ToArray();
}
