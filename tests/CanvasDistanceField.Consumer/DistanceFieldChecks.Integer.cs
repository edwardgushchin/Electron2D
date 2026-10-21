using Electron2D;

internal static partial class DistanceFieldChecks
{
    private static void Integer()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var texture = new TextureRD(); using var root = new Window { Size = new(80, 32) }; var painter = new Painter(texture); root.AddChild(painter);
        RenderingDevice? device = null;
        root.Ready += _ =>
        {
            device = RenderingServer.GetRenderingDevice()!;
            using var format = new RDTextureFormat { Width = 4, Height = 1, Format = RenderingDevice.DataFormat.R8G8B8A8UINT, UsageBits = RenderingDevice.TextureUsageBits.Sampling };
            using var view = new RDTextureView(); texture.TextureRDRID = device.TextureCreate(format, view, [Samples]);
            RenderingServer.CanvasItemSetDistanceFieldMode(painter.GetCanvasItem(), true);
        };
        try { Engine.Run(root); throw new InvalidOperationException("Integer images unexpectedly executed MSDF decoding."); }
        catch (NotSupportedException error) when (error.Message.Contains("Glyph coverage", StringComparison.Ordinal)) { }
        Check(root.IsDisposed && device is { IsDisposed: true } && !RenderingServer.IsAvailable, "Integer rejection releases the owning renderer/device.");
        Console.WriteLine("Integer image MSDF decoding rejects before submission and releases the renderer/device.");
    }
}
