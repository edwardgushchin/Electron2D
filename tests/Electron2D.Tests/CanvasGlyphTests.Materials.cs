using Electron2D;

internal static partial class CanvasGlyphTests
{
    private static void RunMaterial(string profile)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var lit = profile.EndsWith("-lit", StringComparison.Ordinal); var name = profile[9..]; if (lit) name = name[..^4];
        using var shader = TextureArrayTests.Load(name); using var material = new ShaderMaterial { Shader = shader };
        var tint = new Color(.6f, .8f, .7f, .5f); material.SetShaderParameter("tint", tint); material.SetShaderParameter("threshold", 0f); if (name == "GlyphScalar") material.SetShaderParameter("component", 3);
        foreach (var parameter in new[] { "scaleB", "scaleC", "scaleD" }) material.SetShaderParameter(parameter, 1f / 3);
        using var lcdImage = Image.CreateFromData(2, 1, false, Image.Format.Rgba8, [64, 128, 191, 255, 128, 128, 128, 128]);
        using var msdfImage = Image.CreateFromData(4, 1, false, Image.Format.Rgba8, [64, 64, 191, 230, 191, 64, 191, 230, 128, 128, 128, 230, 64, 191, 64, 255]);
        using var lcd = ImageTexture.CreateFromImage(lcdImage); using var msdf = ImageTexture.CreateFromImage(msdfImage);
        var parentColor = new Color(.5f, .8f, .5f, .8f); var clear = new Color(.8f, .1f, .9f, 1);
        var window = new Window { Size = new(240, 128) }; var parent = new Entity { Modulate = parentColor }; window.AddChild(parent);
        parent.AddChild(new Painter(lcd, msdf) { Material = material, TextureFilter = TextureFilter.Nearest });
        if (lit) window.AddChild(new DirectionalLight { Color = Colors.White, Energy = .25f });
        using var target = window.GetTexture(); var frame = 0; var canvasTexture = shader.GetProgram().Textures.Any(texture => texture.IsCanvasTexture);
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(clear);
            if (lit) RenderingServer.CanvasSetModulate(window.FindWorld()!.Canvas, new(.1f, .1f, .1f));
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = target.GetImage()!; var factor = lit ? .35f : 1f;
                if (frame == 1) { Pixel(pixels, 20, 12, clear); Pixel(pixels, 52, 44, clear); }
                else
                {
                    var command = new Color(.2f, .6f, .8f, .5f) * parentColor;
                    var coverage = new Color(64 / 255f, 128 / 255f, 191 / 255f, 1);
                    var source = coverage * tint; if (canvasTexture) source *= coverage;
                    source = new(source.R * factor * command.A * tint.A, source.G * factor * command.A * tint.A, source.B * factor * command.A * tint.A, command.A * tint.A);
                    var expected = new Color(source.R * command.R + clear.R * (1 - source.R), source.G * command.G + clear.G * (1 - source.G), source.B * command.B + clear.B * (1 - source.B), 1);
                    Pixel(pixels, 20, 12, expected); Pixel(pixels, 60, 12, clear);
                    var distance = new Color(.8f, .3f, .1f, .5f) * parentColor * tint;
                    if (canvasTexture) distance *= new Color(191 / 255f, 64 / 255f, 191 / 255f, 230 / 255f);
                    distance = new(distance.R * factor, distance.G * factor, distance.B * factor, distance.A);
                    Pixel(pixels, 52, 44, Blend(distance, clear));
                    Pixel(pixels, 52, 52, Blend(new(distance.R * .5f, distance.G * .5f, distance.B * .5f, distance.A), clear));
                }
                if (frame == 0) material.SetShaderParameter("threshold", 1f);
                if (frame == 1) { material.SetShaderParameter("threshold", 0f); shader.SetSPIRV(shader.GetSPIRV()); }
                if (++frame == 4) window.Tree!.Quit();
            };
        };
        Check(Engine.Run(window) == 0 && frame == 4, "Glyph material host completes.");
        Console.WriteLine($"{name}, light={lit}: LCD/MSDF pixels, four uniform slots, discard, early return and shader reload passed.");
    }
    private sealed class Solid : Entity
    {
        protected override void OnDraw() => DrawRect(new(0, 0, 32, 16), Colors.Lime);
    }
    private static void RunGroups()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var image = Image.CreateFromData(2, 1, false, Image.Format.Rgba8, [191, 191, 191, 0, 64, 64, 64, 255]);
        using var texture = ImageTexture.CreateFromImage(image);
        var window = new Window { Size = new(160, 96) }; var clear = new Color(.2f, .2f, .2f, 1);
        var mask = new Probe { Name = "Mask", Position = new(8, 8), ClipChildren = ClipChildrenMode.Only, TextureFilter = TextureFilter.Nearest };
        mask.Drawing = () => mask.DrawMSDFTextureRectRegion(texture, new(0, 0, 32, 16), new(0, 0, 2, 1), new(1, 1, 1, .5f));
        mask.AddChild(new Solid()); window.AddChild(mask);
        var group = new Probe { Name = "Group", Position = new(64, 8), TextureFilter = TextureFilter.Nearest };
        group.Drawing = () => group.DrawLCDTextureRectRegion(texture, new(0, 0, 32, 16), new(0, 0, 2, 1), new(.5f, .8f, .6f, .5f));
        group.AddChild(new Solid()); window.AddChild(group);
        var outline = new Probe { Name = "Outline", Position = new(8, 40), TextureFilter = TextureFilter.Nearest };
        outline.Drawing = () =>
        {
            outline.DrawMSDFTextureRectRegion(texture, new(0, 0, 32, 16), new(0, 0, 2, 1), new(1, 0, 0, .5f), outline: 4, pixelRange: 4);
            outline.DrawMSDFTextureRectRegion(texture, new(40, 0, -32, 16), new(0, 0, 2, 1), new(1, 0, 0, .5f), outline: 4, pixelRange: 4, scale: 4);
        }; window.AddChild(outline); using var target = window.GetTexture(); var frame = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(clear);
            RenderingServer.CanvasItemSetCanvasGroupMode(group.GetCanvasItem(), RenderingServer.CanvasGroupMode.Transparent);
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = target.GetImage()!;
                Pixel(pixels, 12, 12, Blend(new(0, 1, 0, .5f), clear)); Pixel(pixels, 32, 12, clear);
                Pixel(pixels, 68, 12, Blend(new(0, .8f, 0, .5f), clear));
                Pixel(pixels, 12, 44, clear); Pixel(pixels, 32, 44, Blend(new(1, 0, 0, .5f * Math.Clamp((64 / 255f - .5f + .25f) * 64, 0, 1)), clear));
                Pixel(pixels, 52, 44, clear); Pixel(pixels, 72, 44, clear);
                if (++frame == 3) window.Tree!.Quit();
            };
        };
        Check(Engine.Run(window) == 0 && frame == 3, "Glyph mask/group host completes.");
        Console.WriteLine("MSDF scalar mask, LCD group override, outline alpha-min and scaled signed destinations passed.");
    }
}
