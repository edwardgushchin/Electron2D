namespace Electron2D;

public partial class CPUParticles
{
    /// <summary>Copies a GPU emitter's shared emission and built-in process settings without its native state.</summary>
    /// <param name="particles">The GPU emitter to read; source resources remain borrowed.</param>
    /// <remarks>Scalar/vector curve sources and gradient ramps retain their typed source identity. Point texels copy into arrays.
    /// GPU-only force channels and custom process shaders have no CPU projection. Reverse-lifetime ordering rejects before mutation.</remarks>
    public void ConvertFromParticles(Node particles)
    {
        Edit(); ArgumentNullException.ThrowIfNull(particles);
        if (particles is not GPUParticles source) throw new ArgumentException("Conversion requires a GPU particle emitter.", nameof(particles));
        if (source.Amount > 65536) throw new ArgumentOutOfRangeException(nameof(particles), "CPU particle capacity is at most 65536.");
        if (source.DrawOrder == ParticlesDrawOrder.ReverseLifetime) throw new NotSupportedException("Reverse-lifetime order has no CPU particle draw-order value.");
        var material = source.ProcessMaterial as ParticleProcessMaterial;
        var points = Array.Empty<Vector2>(); var normals = Array.Empty<Vector2>(); var colors = Array.Empty<Color>();
        if (material is not null)
        {
            if (material.EmissionPointTexture is { } pointTexture)
            {
                using var image = pointTexture.GetImage() ?? throw new InvalidOperationException("Emission points require readable texture pixels for CPU conversion.");
                var count = Math.Min(material.EmissionPointCount, checked(image.Width * image.Height)); points = new Vector2[count];
                using var normalImage = material.EmissionNormalTexture?.GetImage(); using var colorImage = material.EmissionColorTexture?.GetImage();
                if (normalImage is not null) normals = new Vector2[count]; if (colorImage is not null) colors = new Color[count];
                for (var i = 0; i < count; i++)
                {
                    var x = i % image.Width; var y = i / image.Width; var position = image.GetPixel(x, y);
                    points[i] = new Vector2(position.R, position.G) * material.EmissionShapeScale + material.EmissionShapeOffset;
                    if (!points[i].IsFinite()) throw new InvalidDataException("Emission point texture contains nonfinite positions.");
                    if (normalImage is not null) { var normal = normalImage.GetPixel(x, y); normals[i] = new(normal.R, normal.G); if (!normals[i].IsFinite()) throw new InvalidDataException("Emission normal texture contains nonfinite data."); }
                    if (colorImage is not null) { var color = colorImage.GetPixel(x, y); if (!float.IsFinite(color.R) || !float.IsFinite(color.G) || !float.IsFinite(color.B) || !float.IsFinite(color.A)) throw new InvalidDataException("Emission color texture contains nonfinite data."); colors[i] = color; }
                }
            }
        }
        Amount = source.Amount; Lifetime = source.Lifetime; OneShot = source.OneShot; Preprocess = source.Preprocess; Explosiveness = source.Explosiveness; Randomness = source.Randomness;
        LocalCoords = source.LocalCoords; FixedFPS = Math.Max(0, source.FixedFPS); FractDelta = source.FractDelta; SpeedScale = source.SpeedScale;
        DrawOrder = (DrawOrderMode)source.DrawOrder; Texture = source.Texture; if (source.Material is { } draw) Material = draw;
        if (material is not null)
        {
            Direction = material.Direction; Spread = material.Spread; Color = material.Color; Gravity = material.Gravity; LifetimeRandomness = material.LifetimeRandomness;
            ColorRamp = (material.ColorRamp as GradientRampTexture)?.Gradient; ColorInitialRamp = (material.ColorInitialRamp as GradientRampTexture)?.Gradient; ParticleFlagAlignY = material.ParticleFlagAlignY;
            EmissionShape = material.EmissionShape; EmissionSphereRadius = material.EmissionSphereRadius; EmissionRectExtents = material.EmissionRectExtents; EmissionRingRadius = material.EmissionRingRadius; EmissionRingInnerRadius = material.EmissionRingInnerRadius;
            EmissionPoints = points; EmissionNormals = normals; EmissionColors = colors;
            for (var channel = 0; channel < 12; channel++)
            {
                var cpu = (Parameter)channel; var gpu = (ParticleProcessMaterial.Parameter)channel;
                SetParamMin(cpu, material.GetParamMin(gpu)); SetParamMax(cpu, material.GetParamMax(gpu));
                SetParamCurve(cpu, channel == 0 ? material.CPUInitialVelocityCurve?.Curve : (material.GetParamTexture(gpu) as CurveTexture)?.Curve);
            }
            if (material.CPUScalarScaleCurve is { } scalar) ScaleAmountCurve = scalar.Curve;
            var scale = material.ScaleCurve as CurveXYZTexture; SplitScale = scale is not null; ScaleCurveX = scale?.CurveX; ScaleCurveY = scale?.CurveY;
            if (material.RetainFileResources() is { } lease) AdoptSceneFileLease(lease);
        }
        Emitting = source.Emitting;
    }
}
