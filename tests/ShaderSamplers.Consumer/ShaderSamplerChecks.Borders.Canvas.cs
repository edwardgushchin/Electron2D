using Electron2D;

internal static partial class ShaderSamplerChecks
{
    private static void BordersCanvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(32, 32), Borderless = true };
        root.Ready += _ =>
        {
            RenderingServer.RenderLoopEnabled = false;
            using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(Colors.Red);
            using var texture = ImageTexture.CreateFromImage(image);
            using var shader = Load("BlitGLSL", true);
            using var material = new ShaderMaterial { Shader = shader };
            using var target = new DrawableTexture(); target.Setup(4, 4, DrawableTexture.DrawableFormat.RGBA8, Colors.Transparent);
            foreach (var color in new[] { RenderingDevice.SamplerBorderColor.FloatTransparentBlack, RenderingDevice.SamplerBorderColor.FloatOpaqueBlack, RenderingDevice.SamplerBorderColor.FloatOpaqueWhite })
            {
                using var state = new RDSamplerState { RepeatU = RenderingDevice.SamplerRepeatMode.ClampToBorder, BorderColor = color };
                shader.SetTextureSamplerState("SOURCE_TEXTURE0", state);
                material.SetShaderParameter("Coordinates", new Vector4(-1, .5f, 0, 0));
                target.Setup(4, 4, DrawableTexture.DrawableFormat.RGBA8, Colors.Transparent); target.BlitRect(new(0, 0, 4, 4), texture, material: material);
                using var result = target.GetImage()!;
                var expected = color == RenderingDevice.SamplerBorderColor.FloatOpaqueWhite ? Colors.White : color == RenderingDevice.SamplerBorderColor.FloatOpaqueBlack ? Colors.Black : Colors.Transparent;
                Match(result.GetPixel(2, 2), expected, "copied high-level border " + color);
            }
            using var linear = new RDSamplerState { RepeatU = RenderingDevice.SamplerRepeatMode.ClampToBorder, BorderColor = RenderingDevice.SamplerBorderColor.FloatOpaqueWhite, MinFilter = RenderingDevice.SamplerFilter.Linear, MagFilter = RenderingDevice.SamplerFilter.Linear };
            shader.SetTextureSamplerState("SOURCE_TEXTURE0", linear);
            material.SetShaderParameter("Coordinates", new Vector4(0, .5f, 0, 0));
            target.Setup(4, 4, DrawableTexture.DrawableFormat.RGBA8, Colors.Transparent); target.BlitRect(new(0, 0, 4, 4), texture, material: material);
            using var blended = target.GetImage()!; Match(blended.GetPixel(2, 2), new Color(1, .5f, .5f, 1), "linear high-level border blend");
            root.Tree!.Quit();
        };
        Engine.Run(root);
        Console.WriteLine("Copied shader sampler cache: all floating-point border colors and native linear edge blending passed through GPU drawable readback.");
    }
}
