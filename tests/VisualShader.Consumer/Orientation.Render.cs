using Electron2D;
using M4 = System.Numerics.Matrix4x4;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    private static float[] MatrixColumns(M4 matrix)
    { var result = new float[16]; for (var i = 0; i < 16; i++) result[i] = matrix[i % 4, i / 4]; return result; }
    private static void OrientationOverride(ShaderMaterial material, bool alternate)
    {
        if (!alternate)
        { foreach (var name in OrientationNames) material.SetShaderParameter(name, (Texture?)null); return; }
        material.SetShaderParameter("orientation_vector", AlternateAxis.Value); material.SetShaderParameter("orientation_angle", AlternateAxis.Angle); material.SetShaderParameter("orientation_axis", AlternateAxis.Axis);
        material.SetShaderParameter("orientation_expected_vector", AxisExpected(AlternateAxis)); material.SetShaderMatrixParameter("orientation_expected_matrix", AlternateAxisMatrix);
    }
    private static readonly string[] OrientationNames = ["orientation_vector", "orientation_angle", "orientation_axis", "orientation_expected_vector", "orientation_expected_matrix"];
    private static void CheckOrientationBlit(ShaderMaterial material)
    {
        const int cases = 18; using var vector = new DrawableTexture(); using var matrix = new DrawableTexture(); using var last = new DrawableTexture();
        foreach (var target in new[] { vector, matrix, last }) target.Setup(cases * 4, 1, DrawableTexture.DrawableFormat.RGBAF, Colors.Magenta);
        for (var phase = 0; phase < 3; phase++)
        {
            OrientationOverride(material, phase == 1); Draw();
            using var vectors = vector.GetImage()!; using var matrices = matrix.GetImage()!; using var tails = last.GetImage()!;
            for (var sample = 0; sample < cases; sample++) for (var column = 0; column < 4; column++)
                {
                    var pixel = sample * 4 + column; var actual = vectors.GetPixel(pixel, 0); var actualMatrix = matrices.GetPixel(pixel, 0); var tail = tails.GetPixel(pixel, 0);
                    var wantedMatrix = M4.Identity; Vector3 wanted; var fourth = 0f;
                    if (sample < AxisCases.Length)
                    { var value = sample == 0 && phase == 1 ? AlternateAxis : AxisCases[sample]; wanted = AxisExpected(value); wantedMatrix = AxisMatrix(value); }
                    else
                    { var f = sample - AxisCases.Length; var width = 2 + f / 3; var sign = f % 3 == 0 ? 1 : -1; wanted = new(.125f * sign, .25f * sign, width == 2 ? 0 : .375f * sign); fourth = width == 4 ? .5f * sign : 0; }
                    Near(actual.R, wanted.X); Near(actual.G, wanted.Y); Near(actual.B, wanted.Z); Near(actual.A, 1);
                    Near(actualMatrix.R, wantedMatrix[0, column]); Near(actualMatrix.G, wantedMatrix[1, column]); Near(actualMatrix.B, wantedMatrix[2, column]); Near(actualMatrix.A, 1);
                    Near(tail.R, fourth); Near(tail.G, wantedMatrix[3, column]); Near(tail.B, 1); Near(tail.A, 1);
                    void Near(float value, float expected) => Check(MathF.Abs(value - expected) < .0002f, $"Orientation raw phase={phase}, case={sample}, column={column}: {value} vs {expected}.");
                }
        }
        material.SetShaderParameter("orientation_axis", Vector3.Zero); Draw();
        using (var probe = vector.GetImage()!) Console.WriteLine("Native zero-axis diagnostic (no portable value asserted): " + probe.GetPixel(0, 0));
        OrientationOverride(material, false); Draw();
        using (var recovered = last.GetImage()!) Check(recovered.GetPixel(0, 0).B == 1, "A valid axis recovers after the undefined zero-axis diagnostic.");
        Console.WriteLine("Orientation RGBAF: vector components, all sixteen matrix components, three FaceForward widths/signs, live inputs and reset passed.");
        void Draw() => vector.BlitRectMulti(new(0, 0, cases * 4, 1), [], new DrawableTexture[] { matrix, last }, material: material);
    }
    private sealed partial class GraphScene
    {
        private readonly List<(ShaderMaterial Material, Vector2 Position)> _orientationCases = [];
        private void ProcessOrientation()
        {
            if (_frame is not (4 or 6) && _frame is not (> 30 and <= 94)) return;
            foreach (var item in _orientationCases) OrientationOverride(item.Material, _frame == 4 || (_frame > 30 && _frame % 2 != 0));
        }
        private void CheckOrientationFrames()
        {
            if (_orientationCases.Count == 0 || _frame is not (5 or 7)) return;
            using var image = GetWindow()!.GetTexture().GetImage()!;
            foreach (var item in _orientationCases) Check(EffectPixelsMatch(image.GetPixel((int)item.Position.X + 8, (int)item.Position.Y + 8), new(.25f, .5f, .75f), GetWindow()!.UseHDR2D), "Live orientation inputs update and reset through the retained program.");
        }
    }
    private sealed partial class DomainScene
    {
        private GPUParticles[] _orientationParticles = [];
        private void PrepareOrientationParticles(string folder, Texture texture)
        {
            var files = Directory.GetFiles(folder, "particle-orientation-*.e2dres").Order(StringComparer.Ordinal).ToArray(); _orientationParticles = new GPUParticles[files.Length];
            if (files.Length == 0) return;
            var contour = OwnDomain(new OccluderPolygon { Polygon = [new(16, 32), new(112, 32), new(112, 112), new(16, 112)] }); AddChild(new LightOccluder { Occluder = contour });
            using var image = Image.CreateEmpty(4, 2, false, Image.Format.Rgba8); image.Fill(Colors.White); var rectangle = OwnDomain(ImageTexture.CreateFromImage(image));
            for (var i = 0; i < files.Length; i++)
            {
                var transformed = IOPath.GetFileNameWithoutExtension(files[i]) == "particle-orientation-transform"; var position = transformed ? new Vector2(104, 16) : new Vector2(24 + i * 16, 40);
                var shader = OwnDomain(ResourceLoader.Load<VisualShader>(files[i], ResourceLoader.CacheMode.IgnoreDeep)); var material = OwnDomain(new ShaderMaterial { Shader = shader });
                var particle = new GPUParticles { Amount = 1, Emitting = false, Lifetime = 1000, FixedFPS = 8, Interpolate = false, LocalCoords = true, SpeedScale = 0, ProcessMaterial = material, Texture = transformed ? rectangle : texture, Position = transformed ? position : Vector2.Zero };
                _orientationParticles[i] = particle; AddChild(particle); particle.EmitParticle(new Transform(0, transformed ? Vector2.Zero : position), Vector2.Zero, Colors.White, Colors.White, (ParticlesEmitFlags)31); particle.RequestParticlesProcess(.125f);
            }
        }
        private void CheckOrientationParticles(Image image)
        {
            for (var i = 0; i < _orientationParticles.Length; i++)
            {
                var particle = _orientationParticles[i]; var bounds = particle.CaptureRect(); var position = bounds.GetCenter() + particle.Position; var actual = image.GetPixel((int)position.X, (int)position.Y); var expected = new Color(.25f, .5f, .75f); if (GetWindow()!.UseHDR2D) expected = expected.SRGBToLinear();
                Check(MathF.Abs(actual.R - expected.R) < .005f && MathF.Abs(actual.G - expected.G) < .005f && MathF.Abs(actual.B - expected.B) < .005f, $"Orientation particle {i}: {actual} at {position}.");
                if (particle.Position != Vector2.Zero)
                {
                    var tip = image.GetPixel((int)position.X, (int)position.Y - 2); var side = image.GetPixel((int)position.X - 2, (int)position.Y);
                    Check(MathF.Abs(tip.R - expected.R) < .005f && MathF.Abs(tip.G - expected.G) < .005f && MathF.Abs(tip.B - expected.B) < .005f && side.R == 0 && side.G == 0 && side.B == 0, "Rotation matrix output rotates the actual nonsquare particle quad.");
                    Check(bounds.GetCenter().DistanceTo(Vector2.Zero) < .001f, "The rotation matrix retains a zero translation in native particle bounds.");
                }
            }
            if (_orientationParticles.Length != 0) Console.WriteLine($"{_orientationParticles.Length} orientation particle programs produced actual pixels and rotated geometry.");
        }
    }
}
