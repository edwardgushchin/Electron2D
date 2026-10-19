using Electron2D;

internal static partial class ParticleServerTests
{
    private sealed class ParticleCallbackTexture : Texture
    {
        internal Action? Callback;
        internal int Calls;
        public override int GetWidth() => 1;
        public override int GetHeight() => 1;
        public override Image GetImage() { Calls++; Callback?.Invoke(); var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White); return image; }
    }
    private static void MaterialCallbacks()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; var owner = new Entity(); root.AddChild(owner);
        using var mesh = Quad(); using var texture = new ParticleCallbackTexture(); using var storageTexture = new ParticleCallbackTexture();
        using var material = new ParticleProcessMaterial { Gravity = Vector2.Zero, ColorRamp = texture };
        var completed = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); var particles = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particles, 1);
            RenderingServer.ParticlesSetUseLocalCoordinates(particles, true); RenderingServer.ParticlesSetFixedFPS(particles, 0); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetLifetime(particles, 10); RenderingServer.ParticlesSetProcessMaterial(particles, material.GetRID());
            var item = Attach(owner, particles, mesh); RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(20, 20)), Vector2.Zero, Colors.White, Colors.White, 1); RenderingServer.ParticlesRequestProcessTime(particles, .125f);
            RenderingServer.FramePostDraw += () => completed++;
            void Failed()
            {
                var failed = false; try { RenderingServer.ForceDraw(false); } catch (InvalidOperationException error) { Console.WriteLine(error.Message); failed = true; }
                Check(failed && completed == 0 && State(particles).ManualCount == 1 && State(particles).Phase == 0, $"Callback failure preserves unpublished timing and pending manual emission: failed={failed}, completed={completed}, pending={State(particles).ManualCount}, phase={State(particles).Phase}, amount={State(particles).Amount}, textureCalls={texture.Calls}/{storageTexture.Calls}.");
            }
            texture.Callback = () => material.Color = Colors.Green; Failed();
            storageTexture.Callback = () => RenderingServer.ParticlesSetAmount(particles, 2); material.ColorRamp = storageTexture; Failed();
            Check(State(particles).Amount == 1, "Texture callbacks cannot change retained particle capacity during preparation.");
            storageTexture.Callback = null; RenderingServer.ForceDraw(false);
            using var image = root.GetTexture().GetImage()!; Pixel(image, 20, 20, Colors.Green);
            Check(completed == 1 && State(particles).ManualCount == 0 && Math.Abs(State(particles).Phase - .0125) < .00001, "Retry commits the original queued birth and seek once.");
            RenderingServer.FreeRID(item); RenderingServer.FreeRID(particles); root.Tree!.Quit();
        };
        Engine.Run(root); Console.WriteLine("Built-in process texture callbacks reject material/storage mutation before publication and retry the preserved birth once.");
    }
}
