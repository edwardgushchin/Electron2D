using Electron2D;

internal static partial class GPUParticlesNodeTests
{
    private sealed class NodeCallbackTexture : Texture
    {
        internal Action? SizeCallback, ImageCallback;
        public override int GetWidth() { SizeCallback?.Invoke(); return 4; }
        public override int GetHeight() => 4;
        public override Image GetImage() { ImageCallback?.Invoke(); var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); image.Fill(Colors.White); return image; }
    }
    internal static void Callbacks()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; var parent = new Entity { Name = "Owner" }; root.AddChild(parent);
        using var texture = new NodeCallbackTexture(); using var ramp = new NodeCallbackTexture();
        using var material = new ParticleProcessMaterial { Gravity = Vector2.Zero, Color = Colors.Green, ColorRamp = ramp };
        var node = new GPUParticles { Name = "Particles", Amount = 1, Emitting = false, SpeedScale = 0, LocalCoords = true, Interpolate = false, ProcessMaterial = material, Texture = texture, Position = new(20, 20) }; parent.AddChild(node);
        var failed = 0; root.Ready += _ =>
        {
            foreach (Action mutation in new Action[] { () => node.Amount = 2, () => parent.Position = Vector2.One, node.Dispose, parent.Dispose })
            {
                texture.SizeCallback = mutation; Reject<InvalidOperationException>(() => node.RequestParticlesProcess(.125f)); failed++;
                Check(!node.IsDisposed && !parent.IsDisposed && ReferenceEquals(node.Parent, parent) && node.Amount == 1 && parent.Position == Vector2.Zero && node.CaptureRect() == default, "Texture-size callback rejection precedes scene ownership or property changes.");
            }
            texture.SizeCallback = null; node.EmitParticle(Transform.Identity, Vector2.Zero, Colors.White, Colors.White, ParticlesEmitFlags.Position); node.RequestParticlesProcess(.125f);
            ramp.ImageCallback = () => parent.Dispose(); Reject<InvalidOperationException>(() => RenderingServer.ForceDraw(false)); failed++;
            Check(!node.IsDisposed && !parent.IsDisposed && node.CaptureRect() == default, "Process texture callbacks cannot dispose the emitter's parent before publication.");
            ramp.ImageCallback = null; RenderingServer.ForceDraw(false);
            using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(20, 20);
            Check(pixel.G > .98 && pixel.R < .02 && node.CaptureRect().HasArea(), "Retry executes the preserved birth after the texture callback is repaired.");
            root.Tree!.Quit();
        };
        Engine.Run(root); Check(failed == 5 && !texture.IsDisposed && !ramp.IsDisposed && !material.IsDisposed, "Five scene/native preparation guards completed with borrowed resources preserved.");
        Console.WriteLine("GPU emitter texture callbacks reject configuration, ancestor transform and disposal before mutation; native retry preserves the queued birth.");
    }
}
