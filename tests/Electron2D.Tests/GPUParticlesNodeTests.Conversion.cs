using Electron2D;

internal static partial class GPUParticlesNodeTests
{
    private static void Conversions()
    {
        using var curve = new Curve { MinValue = 0, MaxValue = 4 }; curve.AddPoint(new(0, 2)); curve.AddPoint(new(1, 2));
        using var gradient = new Gradient { Colors = [new(2, -1, 3, 1), new(4, -2, 6, 1)] };
        using var source = new CPUParticles { Amount = 4, Lifetime = 2, Emitting = false, LocalCoords = true, FixedFPS = 8, Gravity = new(3, 4), Direction = new(0, 1), AngularVelocityMin = 30, AngularVelocityMax = 60, AngularVelocityCurve = curve, ColorRamp = gradient, ColorInitialRamp = gradient, SplitScale = true, ScaleCurveX = curve, ScaleCurveY = curve, EmissionShape = ParticlesEmissionShape.DirectedPoints, EmissionPoints = [new(4, 5), new(6, 7)], EmissionNormals = [new(1, 0), new(0, 1)], EmissionColors = [Colors.Red, Colors.Blue] };
        using var gpu = new GPUParticles(); gpu.ConvertFromParticles(source);
        var process = (ParticleProcessMaterial)gpu.ProcessMaterial!;
        Check(gpu.Amount == 4 && gpu.Lifetime == 2 && !gpu.Emitting && gpu.LocalCoords && gpu.FixedFPS == 8 && process.Gravity == new Vector2(3, 4) && process.Direction == new Vector2(0, 1) && process.AngularVelocityMin == 30 && process.AngularVelocityMax == 60, "CPU conversion copies emission, force and range configuration.");
        Check(process.AngularVelocityCurve is CurveTexture { Curve: { } scalar } && ReferenceEquals(scalar, curve) && process.ScaleCurve is CurveXYZTexture { CurveX: { } x, CurveY: { } y } && ReferenceEquals(x, curve) && ReferenceEquals(y, curve), "Generated curve wrappers borrow the actual source curves.");
        using (var pixels = process.ColorRamp!.GetImage()!) Check(pixels.PixelFormat == Image.Format.Rgbaf && pixels.GetPixel(0, 0) == new Color(2, -1, 3, 1), "Converted gradient textures preserve numeric HDR source colors.");
        using var cpu = new CPUParticles(); cpu.ConvertFromParticles(gpu);
        Check(cpu.Amount == source.Amount && cpu.Direction == source.Direction && cpu.Gravity == source.Gravity && cpu.EmissionShape == source.EmissionShape && cpu.EmissionPoints.SequenceEqual(source.EmissionPoints) && cpu.EmissionNormals.SequenceEqual(source.EmissionNormals) && cpu.EmissionColors.SequenceEqual(source.EmissionColors), "Point, normal and color texels round-trip as copied CPU arrays.");
        Check(ReferenceEquals(cpu.AngularVelocityCurve, curve) && ReferenceEquals(cpu.ColorRamp, gradient) && cpu.SplitScale && ReferenceEquals(cpu.ScaleCurveX, curve), "CPU conversion preserves borrowed scalar, split-scale and gradient identities.");
        process.EmissionShapeOffset = new(10, 20); process.EmissionShapeScale = new(2, 3); cpu.ConvertFromParticles(gpu); Check(cpu.EmissionPoints[0] == new Vector2(18, 35), "Point offset and scale bake into copied positions.");
        gpu.DrawOrder = ParticlesDrawOrder.ReverseLifetime; var previous = cpu.Amount; Reject<NotSupportedException>(() => cpu.ConvertFromParticles(gpu)); Check(cpu.Amount == previous, "An unsupported draw-order domain rejects before destination mutation."); gpu.DrawOrder = ParticlesDrawOrder.Lifetime;
        source.SetParamCurve(CPUParticles.Parameter.InitialLinearVelocity, curve); source.ScaleAmountCurve = curve; gpu.ConvertFromParticles(source); cpu.ConvertFromParticles(gpu); Check(ReferenceEquals(cpu.GetParamCurve(CPUParticles.Parameter.InitialLinearVelocity), curve) && ReferenceEquals(cpu.ScaleAmountCurve, curve) && cpu.SplitScale, "CPU-only lifetime velocity and combined scale curves round-trip through private stored compute inputs.");
        using var wrong = new Entity(); Reject<ArgumentException>(() => gpu.ConvertFromParticles(wrong)); Reject<ArgumentException>(() => cpu.ConvertFromParticles(wrong));
        gpu.Dispose(); Check(!curve.IsDisposed && !gradient.IsDisposed, "Converted resource retirement preserves borrowed source ownership.");
    }
}
