using System.Diagnostics;
using Electron2D;

internal static class TextServerFontRenderingTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 120;
        var s = TextServerManager.GetPrimaryInterface(); var root = s.CreateFont(); var link = s.CreateFontLinkedVariation(root);
        var normal = s.CreateShapedText(); var linked = s.CreateShapedText(); var vertical = s.CreateShapedText(orientation: TextOrientation.Vertical);
        s.FontSetAllowSystemFallback(root, false); s.FontSetData(root, FontTestFixtures.OpenSans);
        s.ShapedTextAddString(normal, "source", [s.FontGetResource(root)], 24); s.ShapedTextAddString(linked, "source", [s.FontGetResource(link)], 24); s.ShapedTextAddString(vertical, "A", [s.FontGetResource(link)], 24);
        s.FontSetSpacing(link, TextSpacingType.Glyph, 3); s.FontSetBaselineOffset(link, .3f);
        var glyph = s.FontGetGlyphIndex(root, 24, 'A', 0); var outlineGlyph = s.FontGetGlyphIndex(root, 48, 'I', 0);
        Vector2[] polygon = [];
        void ReadContour()
        {
            var contour = s.FontGetGlyphContours(root, 48, outlineGlyph);
            Check(contour.Contours.Length == 1 && contour.Points.Length >= 4 && contour.Points.All(point => point.Tag == TextServer.ContourPointTag.On), "Native I outline supplies a simple real polygon.");
            polygon = contour.Points.Select(point => point.Position + new Vector2(465, 110)).ToArray();
        }
        ReadContour();
        using var window = new Window { Size = new(560, 270) }; var canvas = new Entity(); window.AddChild(canvas); using var target = window.GetTexture();
        var frames = 0; var draws = 0; var changed = false; long before = 0, active = 0, idle = 0; var initialVertical = 0;
        canvas.Draw += item =>
        {
            draws++; s.ShapedTextDraw(normal, item, new(20, 70), color: Colors.White); s.ShapedTextDraw(linked, item, new(20, 130), color: Colors.Green);
            s.FontDrawGlyph(root, item, 24, new(300, 70), glyph, Colors.Cyan); s.FontDrawGlyph(link, item, 24, new(360, 70), glyph, Colors.Yellow);
            s.FontDrawGlyphOutline(link, item, 24, 2, new(300, 150), glyph, Colors.Magenta);
            s.ShapedTextDraw(vertical, item, new(420, 190), color: Colors.Red); item.DrawColoredPolygon(polygon, Colors.Blue);
        };
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            window.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frames == 25)
                {
                    s.FontSetData(link, FontTestFixtures.OpenSans); s.FontSetEmbolden(root, .75f); s.FontSetBaselineOffset(link, -.25f); changed = true; ReadContour();
                    Check(s.FontGetResource(root).GetRID() == root && s.FontGetResource(link).GetRID() == link, "Source replacement preserves root and linked identities.");
                }
                if (frames < 135) canvas.QueueRedraw();
                s.FontGetSpacing(link, TextSpacingType.Glyph); s.FontGetBaselineOffset(link); s.FontGetAntialiasing(root); s.ShapedTextGetWidth(normal);
            };
            RenderingServer.FramePostDraw += Capture;
        };
        void Capture()
        {
            var delta = GC.GetAllocatedBytesForCurrentThread() - before;
            if (frames is >= 71 and < 135) active += delta;
            if (frames is >= 135 and < 199) idle += delta;
            frames++;
            if (frames is 2 or 50)
            {
                using var image = target.GetImage() ?? throw new InvalidOperationException("Font service readback.");
                var white = Bounds(image, new(10, 20, 240, 80), 0); var green = Bounds(image, new(10, 95, 240, 85), 1);
                var height = s.FontGetAscent(root, 24) + s.FontGetDescent(root, 24); var baseline = s.FontGetBaselineOffset(link);
                Check(white.HasArea() && green.HasArea() && Math.Abs(green.Position.Y - white.Position.Y - 60 - baseline * height) <= 2, "Linked baseline moves actual shaped pixels by its source-height fraction.");
                Check(green.Size.X > white.Size.X + 8, "Independent linked spacing reaches actual glyph drawing.");
                var cyan = Bounds(image, new(285, 20, 55, 75), 2); var yellow = Bounds(image, new(345, 20, 55, 75), 3);
                Check(cyan.HasArea() && yellow.HasArea() && cyan.Position.Y == yellow.Position.Y && cyan.Size == yellow.Size, "Direct linked glyph drawing shares raw cache geometry independently of shaping baseline.");
                Check(Bounds(image, new(280, 105, 65, 65), 4).HasArea(), "Native glyph outline cache is rendered.");
                Check(Bounds(image, new(450, 50, 75, 75), 5).HasArea(), "Copied native contour points drive real canvas polygon drawing.");
                var red = Bounds(image, new(380, 175, 75, 80), 6); Check(red.HasArea(), "Vertical linked glyph renders.");
                if (!changed) initialVertical = red.Position.X; else Check(red.Position.X < initialVertical - 4, "Vertical linked baseline changes the cross axis.");
                var rootTexture = s.FontGetGlyphTextureRID(root, new(24, 0), glyph); Check(rootTexture != default && rootTexture == s.FontGetGlyphTextureRID(link, new(24, 0), glyph), "Links expose the same actual shared renderer atlas.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } directory)
                { Directory.CreateDirectory(directory); image.SavePNG(System.IO.Path.Combine(directory, "text-font-owner-" + backend + "-" + frames + ".png")); }
            }
            if (frames < 199) return;
            Check(draws >= 135 && active == 0 && idle == 0, "64 active + 64 idle complete frame owner bytes=" + active + "/" + idle);
            RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
        }
        try { Check(Engine.Run(window) == 0 && frames == 199, "Font service drawing host completes."); }
        finally
        {
            if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture;
            s.FreeRID(normal); s.FreeRID(linked); s.FreeRID(vertical); s.FreeRID(link); s.FreeRID(root);
        }
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_FONT_OWNER_CHILD") == "1") Console.WriteLine("{\"fontOwner\":true,\"freshProcess\":true,\"rendered\":true}");
        else Fresh(backend);
        Console.WriteLine("TextServer font " + backend + ": owned source/cache, linked baseline/spacing, direct/outline/vertical pixels, real contour polygon and source replacement; 64 active + 64 idle frames=0/0 owner bytes passed.");
    }
    private static void Fresh(string backend)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
        if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(TextServerFontRenderingTests).Assembly.Location);
        start.Environment["ELECTRON2D_TEST_TEXT_SERVER_FONT_RENDER"] = backend; start.Environment["ELECTRON2D_TEST_FONT_OWNER_CHILD"] = "1";
        using var child = Process.Start(start)!; var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh font service drawing host."); }
        var output = stdout.GetAwaiter().GetResult(); var error = stderr.GetAwaiter().GetResult();
        Check(child.ExitCode == 0 && output.Contains("\"fontOwner\":true"), "Fresh source/cache authoring and drawing: " + output + error); Console.WriteLine(output.Trim());
    }
    private static Rect2i Bounds(Image image, Rect2i area, int kind)
    {
        var minX = area.End.X; var minY = area.End.Y; var maxX = -1; var maxY = -1;
        for (var y = area.Position.Y; y < area.End.Y; y++) for (var x = area.Position.X; x < area.End.X; x++)
            {
                var c = image.GetPixel(x, y);
                if (!(kind switch
                {
                    0 => c.R > .5f && c.G > .5f && c.B > .5f,
                    1 => c.G > .5f && c.R < .2f && c.B < .2f,
                    2 => c.G > .5f && c.B > .5f && c.R < .2f,
                    3 => c.R > .5f && c.G > .5f && c.B < .2f,
                    4 => c.R > .5f && c.B > .5f && c.G < .2f,
                    5 => c.B > .5f && c.R < .2f && c.G < .2f,
                    _ => c.R > .5f && c.G < .2f && c.B < .2f
                })) continue;
                minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
            }
        return maxX < 0 ? default : new(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("TextServer font rendering: " + message); }
}
