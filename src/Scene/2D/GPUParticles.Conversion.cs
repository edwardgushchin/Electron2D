namespace Electron2D;

public partial class GPUParticles
{
    /// <summary>Copies a CPU emitter's emission settings and representable process resources without its live particles.</summary>
    /// <param name="particles">The CPU particle emitter to read; it remains caller-owned.</param>
    /// <remarks>Generated process and texture resources belong to this node. Source curves, gradients and draw resources remain borrowed.
    /// CPU velocity-lifetime and combined scale curves use private stored compute inputs without adding public parameter roles.</remarks>
    public void ConvertFromParticles(Node particles)
    {
        Edit(); ArgumentNullException.ThrowIfNull(particles);
        if (particles is not CPUParticles source) throw new ArgumentException("Conversion requires a CPU particle emitter.", nameof(particles));
        Number(source.Lifetime, true); Number(source.Preprocess); Number(source.SpeedScale);
        if (NativeLive) _ = TextureSize(source.Texture);
        var material = new ParticleProcessMaterial(); var generated = new List<Resource>();
        try
        {
            material.Direction = source.Direction; material.Spread = source.Spread; material.Color = source.Color; material.Gravity = source.Gravity; material.LifetimeRandomness = source.LifetimeRandomness;
            if (source.ColorRamp is { } ramp) { var texture = new GradientRampTexture { Gradient = ramp, UseHDR = true }; generated.Add(texture); material.ColorRamp = texture; }
            if (source.ColorInitialRamp is { } initial) { var texture = new GradientRampTexture { Gradient = initial, UseHDR = true }; generated.Add(texture); material.ColorInitialRamp = texture; }
            if (source.GetParamCurve(CPUParticles.Parameter.InitialLinearVelocity) is { } velocity)
            { var texture = new CurveTexture { Curve = velocity }; generated.Add(texture); material.CPUInitialVelocityCurve = texture; }
            material.ParticleFlagAlignY = source.ParticleFlagAlignY;
            material.EmissionShape = source.EmissionShape; material.EmissionSphereRadius = source.EmissionSphereRadius; material.EmissionRectExtents = source.EmissionRectExtents;
            material.EmissionRingRadius = source.EmissionRingRadius; material.EmissionRingInnerRadius = source.EmissionRingInnerRadius;
            for (var channel = 0; channel < 12; channel++)
            {
                var cpu = (CPUParticles.Parameter)channel; var gpu = (ParticleProcessMaterial.Parameter)channel;
                material.SetParam(gpu, new(source.GetParamMin(cpu), source.GetParamMax(cpu)));
                if (channel != 0 && source.GetParamCurve(cpu) is { } curve)
                { var texture = new CurveTexture { Curve = curve }; generated.Add(texture); material.SetParamTexture(gpu, texture); }
            }
            if (source.SplitScale)
            {
                var texture = new CurveXYZTexture { CurveX = source.ScaleCurveX, CurveY = source.ScaleCurveY }; generated.Add(texture); material.ScaleCurve = texture;
                if (source.ScaleAmountCurve is { } scalar) { var combined = new CurveTexture { Curve = scalar }; generated.Add(combined); material.CPUScalarScaleCurve = combined; }
                if (source.ScaleCurveX is null && texture.CurveX is { } x) generated.Add(x);
                if (source.ScaleCurveY is null && texture.CurveY is { } y) generated.Add(y);
                if (texture.CurveZ is { } z) generated.Add(z);
            }
            var points = source.EmissionPoints;
            if (points.Length != 0)
            {
                var normals = source.EmissionNormals; var colors = source.EmissionColors;
                var positions = PointTexture(points, []); generated.Add(positions); material.EmissionPointTexture = positions; material.EmissionPointCount = points.Length;
                if (normals.Length == points.Length) { var texture = PointTexture(normals, []); generated.Add(texture); material.EmissionNormalTexture = texture; }
                if (colors.Length == points.Length) { var texture = PointTexture([], colors); generated.Add(texture); material.EmissionColorTexture = texture; }
            }
            _ = material.GetCanvasState();
            material.AdoptFileResources(generated.ToArray()); generated.Clear();
            Amount = source.Amount; Lifetime = source.Lifetime; OneShot = source.OneShot; Preprocess = source.Preprocess; Explosiveness = source.Explosiveness; Randomness = source.Randomness;
            LocalCoords = source.LocalCoords; FixedFPS = source.FixedFPS; FractDelta = source.FractDelta; SpeedScale = source.SpeedScale;
            DrawOrder = (ParticlesDrawOrder)source.DrawOrder; Texture = source.Texture; if (source.Material is { } draw) Material = draw;
            ProcessMaterial = material; AdoptSceneFileLease(new ResourceFileOwnership([material]).TransferToLease()); material = null!;
            if (_emitting != source.Emitting) Emitting = source.Emitting;
            if (_oneShot && _emitting && IsInsideTree) BeginCycle();
        }
        finally { material?.Dispose(); foreach (var resource in generated) resource.Dispose(); }
    }
    private static ImageTexture PointTexture(ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors)
    {
        var count = Math.Max(points.Length, colors.Length); var width = Math.Min(count, 4096); var height = (count + width - 1) / width;
        var bytes = new byte[checked(width * height * 16)];
        for (var i = 0; i < count; i++)
        {
            var value = colors.IsEmpty ? new Color(points[i].X, points[i].Y, 0, 1) : colors[i];
            var pixel = bytes.AsSpan(i * 16, 16);
            System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(pixel, value.R); System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(pixel[4..], value.G);
            System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(pixel[8..], value.B); System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(pixel[12..], value.A);
        }
        using var image = Image.CreateFromData(width, height, false, Image.Format.Rgbaf, bytes); return ImageTexture.CreateFromImage(image);
    }
}
