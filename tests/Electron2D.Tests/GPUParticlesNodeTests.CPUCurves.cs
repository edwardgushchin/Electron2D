using Electron2D;

internal static partial class GPUParticlesNodeTests
{
    internal static void CPUCurves(string? savedPath = null)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
        using var curve = new Curve { MinValue = 0, MaxValue = 4 }; curve.AddPoint(new(0, 2)); curve.AddPoint(new(1, 2));
        using var source = new CPUParticles { Amount = 1, Emitting = false, Lifetime = 10, LocalCoords = true, SpeedScale = 0, FixedFPS = 8, Texture = texture, Color = Colors.Green, Gravity = Vector2.Zero, Direction = new(1, 0), Spread = 0, InitialVelocityMin = 8, InitialVelocityMax = 8, SplitScale = true, ScaleCurveX = curve, ScaleCurveY = curve, ScaleAmountCurve = curve, EmissionShape = ParticlesEmissionShape.Points, EmissionPoints = [new(20, 20)] };
        source.SetParamCurve(CPUParticles.Parameter.InitialLinearVelocity, curve);
        var node = new GPUParticles { Interpolate = false }; node.ConvertFromParticles(source);
        if (savedPath is not null) node.ProcessMaterial = ResourceLoader.Load<ParticleProcessMaterial>(savedPath, ResourceLoader.CacheMode.Ignore);
        else
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-cpu-curves-" + Guid.NewGuid().ToString("N") + ".e2dres");
            try
            {
                ResourceSaver.Save(node.ProcessMaterial!, path);
                var start = new System.Diagnostics.ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
                start.ArgumentList.Add(typeof(GPUParticlesNodeTests).Assembly.Location); start.Environment["ELECTRON2D_TEST_PARTICLE_SERVER"] = "node-cpu-file"; start.Environment["ELECTRON2D_GPU_NODE_FILE"] = path;
                using var child = System.Diagnostics.Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
                if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Converted CPU curves file child."); }
                Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("CPU-to-GPU lifetime velocity", StringComparison.Ordinal), error.GetAwaiter().GetResult());
            }
            finally { System.IO.File.Delete(path); }
        }
        root.AddChild(node); var frames = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); node.EmitParticle(Transform.Identity, Vector2.Zero, Colors.White, Colors.White, 0); node.RequestParticlesProcess(.125f);
            RenderingServer.FramePostDraw += () =>
            {
                using var frame = root.GetTexture().GetImage()!; var pixel = frame.GetPixel(26, 20);
                Check(pixel.G > .98 && pixel.R < .02 && node.CaptureRect().GetCenter().DistanceTo(new(20.25f, 20)) < .001f, "Converted lifetime velocity and scalar-times-split scale curves execute on native GPU storage and pixels.");
                frames++; root.Tree!.Quit();
            };
        };
        Engine.Run(root); Check(frames == 1 && !curve.IsDisposed && !texture.IsDisposed, "Converted native curve frame completed with borrowed sources preserved.");
        Console.WriteLine("CPU-to-GPU lifetime velocity and composed scale curves execute as private stored compute inputs.");
    }
}
