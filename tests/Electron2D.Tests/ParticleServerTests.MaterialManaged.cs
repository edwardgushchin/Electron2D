using Electron2D;
using Path = System.IO.Path;

internal static partial class ParticleServerTests
{
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
    private static void MaterialManaged()
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_PARTICLE_MATERIAL_FILE") is { Length: > 0 } file)
        {
            using var loaded = ResourceLoader.Load<ParticleProcessMaterial>(file, ResourceLoader.CacheMode.Ignore);
            using var pixels = loaded.ColorRamp!.GetImage()!;
            Check(loaded.EmissionShape == ParticlesEmissionShape.Ring && loaded.EmissionRingInnerRadius == 2 && loaded.ParticleFlagDampingAsFriction && ReferenceEquals(loaded.AngleCurve, loaded.AngularVelocityCurve) && ((CurveTexture)loaded.AngleCurve!).Curve!.Sample(.5f) == 1 && loaded.ScaleCurve is CurveXYZTexture { Width: 32, CurveX.PointCount: 2, CurveY.PointCount: 2, CurveZ.PointCount: 2 } && loaded.ColorRamp is GradientRampTexture { Width: 17, Gradient: null } && pixels.Width == 9 && pixels.GetPixel(0, 0) == new Color(2, -1, 3, 1), "Fresh-process material graph retains settings, aliases, curves and detached HDR pixels.");
            Console.WriteLine("Fresh particle material file passed."); return;
        }
        using var material = new ParticleProcessMaterial();
        Check(material.Direction == new Vector2(1, 0) && material.Gravity == new Vector2(0, -9.8f) && material.ScaleMin == 1 && material.ScaleMax == 1 && material.DirectionalVelocityMin == 1 && material.TurbulenceInfluenceMin == .1f && material.SubEmitterFrequency == 4, "Material authoring defaults require no renderer.");
        material.SetParam(ParticleProcessMaterial.Parameter.InitialLinearVelocity, new(4, 2));
        Check(material.InitialVelocityMin == 2 && material.InitialVelocityMax == 2, "An inverted pair follows ordered min/max assignment.");
        material.AngleMin = 10; Check(material.AngleMax == 10, "Minimum raises maximum."); material.AngleMax = -5; Check(material.AngleMin == -5, "Maximum lowers minimum.");
        Reject<ArgumentOutOfRangeException>(() => material.SetParamTexture(ParticleProcessMaterial.Parameter.InitialLinearVelocity, null));
        Reject<ArgumentOutOfRangeException>(() => material.GetParamMin(ParticleProcessMaterial.Parameter.TurbInfluenceOverLife));
        Reject<ArgumentOutOfRangeException>(() => material.SetParticleFlag((ParticleProcessMaterial.ParticleFlags)1, true));
        Reject<ArgumentOutOfRangeException>(() => material.CollisionMode = ParticleProcessMaterial.CollisionModeEnum.Max);
        Reject<ArgumentOutOfRangeException>(() => material.SubEmitterFrequency = 0);
        Reject<ArgumentOutOfRangeException>(() => material.SetParamMax(ParticleProcessMaterial.Parameter.Damping, float.NaN));
        using var texture = new CurveTexture(); material.AngularVelocityCurve = texture;
        Check(texture.Curve is { PointCount: 2, MinValue: -360, MaxValue: 360 } && texture.Curve.Sample(.5f) == 1, "Assigning an empty scalar curve texture creates its unit curve and owning range.");
        var authored = texture.Curve; material.AngleCurve = texture; Check(ReferenceEquals(authored, texture.Curve), "Authored scalar curves are preserved.");
        material.EmissionShape = ParticlesEmissionShape.Ring; material.EmissionRingRadius = 4; material.EmissionRingInnerRadius = 2; material.ParticleFlagDampingAsFriction = true;
        using var shallow = (ParticleProcessMaterial)material.Duplicate(); using var deep = (ParticleProcessMaterial)material.Duplicate(true);
        Check(shallow.EmissionShape == ParticlesEmissionShape.Ring && shallow.ParticleFlagDampingAsFriction && ReferenceEquals(shallow.AngleCurve, texture), "Shallow copying preserves settings and borrowed textures.");
        Check(!ReferenceEquals(deep.AngleCurve, texture) && ReferenceEquals(deep.AngleCurve, deep.AngularVelocityCurve) && ((CurveTexture)deep.AngleCurve!).Curve!.Sample(.5f) == 1, "Deep copying preserves shared texture graph identity and curve values.");
        using var xyz = new CurveXYZTexture { Width = 32 }; material.ScaleCurve = xyz;
        using var gradient = new Gradient { Colors = [new Color(2, -1, 3, 1), new Color(4, -2, 6, 1)] };
        using var ramp = new GradientRampTexture { Width = 9, UseHDR = true, Gradient = gradient };
        using var oldPixels = ramp.GetImage()!; ramp.Gradient = null; ramp.Width = 17;
        using var surface = new GradientTexture { Width = 5, Height = 3, UseHDR = true, Gradient = gradient, Fill = FillEnum.Radial };
        material.ColorRamp = ramp; material.AlphaCurve = ramp; material.ColorInitialRamp = surface;
        var path = Path.Combine(Path.GetTempPath(), "electron2d-particle-material-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(material, path); using var loaded = ResourceLoader.Load<ParticleProcessMaterial>(path, ResourceLoader.CacheMode.Ignore);
            Check(loaded.EmissionShape == ParticlesEmissionShape.Ring && loaded.EmissionRingInnerRadius == 2 && loaded.ParticleFlagDampingAsFriction && ReferenceEquals(loaded.AngleCurve, loaded.AngularVelocityCurve) && ((CurveTexture)loaded.AngleCurve!).Curve!.PointCount == 2 && ((CurveTexture)loaded.AngleCurve!).Curve!.Sample(.5f) == 1, "File storage preserves typed settings, shared enum and borrowed texture graph.");
            using var frozen = loaded.ColorRamp!.GetImage()!;
            Check(loaded.ColorRamp is GradientRampTexture { Width: 17, UseHDR: true, Gradient: null } && ReferenceEquals(loaded.ColorRamp, loaded.AlphaCurve) && frozen.Width == 9 && frozen.GetData().SequenceEqual(oldPixels.GetData()), "Gradient ramps preserve detached pixels and authored dimensions without rebaking.");
            Check(loaded.ScaleCurve is CurveXYZTexture { Width: 32, CurveX.PointCount: 2, CurveY.PointCount: 2, CurveZ.PointCount: 2 } && loaded.ColorInitialRamp is GradientTexture { Width: 5, Height: 3, Fill: FillEnum.Radial, UseHDR: true }, "Vector curves and gradient surfaces retain their procedural settings and source graph.");
            var start = new System.Diagnostics.ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(typeof(ParticleServerTests).Assembly.Location); start.Environment["ELECTRON2D_TEST_PARTICLE_SERVER"] = "material-managed"; start.Environment["ELECTRON2D_PARTICLE_MATERIAL_FILE"] = path;
            using var child = System.Diagnostics.Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Particle material file child."); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh particle material file passed.", StringComparison.Ordinal), error.GetAwaiter().GetResult());
        }
        finally { File.Delete(path); }
        using var disposed = new CurveTexture(); disposed.Dispose(); Reject<ObjectDisposedException>(() => material.ScaleCurve = disposed);
        Check(ReferenceEquals(material.ScaleCurve, xyz), "Disposed texture assignment leaves the prior channel unchanged.");
        var frozenDescriptor = (PropertyDescriptor<GradientRampTexture, byte[]>)ramp.GetPropertyList().Single(p => p.Name == "_frozen_gradient_pixels");
        var original = frozenDescriptor.GetValue(ramp); var malformed = (byte[])original.Clone();
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(malformed.AsSpan(4), int.MaxValue);
        Reject<InvalidDataException>(() => frozenDescriptor.SetValue(ramp, malformed));
        Check(frozenDescriptor.GetValue(ramp).SequenceEqual(original), "Malformed frozen dimensions reject before replacing the cached pixels.");
        Console.WriteLine("Particle material authoring, range roles, defaults, curve initialization, duplication and file graphs passed without a rendering session.");
    }
}
