using Electron2D;

internal static partial class DistanceFieldChecks
{
    private sealed class Encoded(Texture texture, bool explicitDraw) : Entity
    {
        protected override void OnDraw()
        {
            if (explicitDraw) DrawMSDFTextureRectRegion(texture, new(0, 0, 64, 16), new(0, 0, 4, 1), Tint);
            else DrawTextureRect(texture, new(0, 0, 64, 16), false, Tint);
        }
    }
    private static void Integration(string profile, string? output)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (profile.Contains("msaa", StringComparison.Ordinal)) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        using var image = Image.CreateFromData(4, 1, false, Image.Format.Rgba8, Samples); using var texture = ImageTexture.CreateFromImage(image);
        using var root = new Window { Size = new(320, 144), UseHDR2D = profile.Contains("hdr", StringComparison.Ordinal), TransparentBG = profile.Contains("transparent", StringComparison.Ordinal) };
        var subjects = new List<Encoded>(); var references = new List<Encoded>();
        for (var row = 0; row < 4; row++)
        {
            var subject = new Encoded(texture, false) { Position = new(8, 8 + row * 32), TextureFilter = TextureFilter.Nearest };
            var reference = new Encoded(texture, true) { Position = new(168, 8 + row * 32), TextureFilter = TextureFilter.Nearest };
            if (row == 1) { subject.Scale = reference.Scale = new(1.5f, .75f); subject.Rotation = reference.Rotation = .1f; }
            if (row == 2) { subject.ClipChildren = reference.ClipChildren = ClipChildrenMode.Only; subject.AddChild(new Solid()); reference.AddChild(new Solid()); }
            if (row == 3) { subject.AddChild(new Solid()); reference.AddChild(new Solid()); }
            root.AddChild(subject); root.AddChild(reference); subjects.Add(subject); references.Add(reference);
        }
        if (profile.Contains("lit", StringComparison.Ordinal)) root.AddChild(new DirectionalLight { Color = Colors.White, Energy = .25f });
        var frames = 0; var compared = 0;
        root.Ready += _ =>
        {
            OS.LowProcessorUsageMode = false; RenderingServer.SetDefaultClearColor(Background);
            foreach (var subject in subjects) RenderingServer.CanvasItemSetDistanceFieldMode(subject.GetCanvasItem(), true);
            RenderingServer.CanvasItemSetCanvasGroupMode(subjects[3].GetCanvasItem(), RenderingServer.CanvasGroupMode.Transparent);
            RenderingServer.CanvasItemSetCanvasGroupMode(references[3].GetCanvasItem(), RenderingServer.CanvasGroupMode.Transparent);
            RenderingServer.FramePostDraw += () =>
            {
                if (++frames < 3) return;
                using var pixels = root.GetTexture().GetImage()!;
                for (var row = 0; row < 4; row++)
                    for (var y = 0; y < 24; y++)
                        for (var x = 0; x < 104; x++) { Match(pixels.GetPixel(4 + x, row * 32 + y + 4), pixels.GetPixel(164 + x, row * 32 + y + 4), profile + "/equivalent"); compared++; }
                var background = pixels.GetPixel(0, 0);
                foreach (var subject in subjects)
                {
                    var point = subject.GetGlobalTransform() * new Vector2(24, 4); var value = pixels.GetPixel((int)point.X, (int)point.Y);
                    Check(MathF.Abs(value.R - background.R) + MathF.Abs(value.G - background.G) + MathF.Abs(value.B - background.B) + MathF.Abs(value.A - background.A) > .03f, profile + "/nonempty");
                }
                if (output is not null) pixels.SavePNG(output);
                Console.WriteLine($"Retained MSDF integration {profile}: {compared} pixels match explicit MSDF across direct, transformed, mask and group output."); root.Tree!.Quit();
            };
        };
        Engine.Run(root);
        Check(root.IsDisposed && !RenderingServer.IsAvailable && frames == 3, "Integration host completes and tears down.");
    }
}
