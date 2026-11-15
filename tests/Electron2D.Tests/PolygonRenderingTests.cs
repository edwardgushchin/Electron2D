using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyPolygonNode(string backend, string? fixture = null)
    {
        using var image = Image.CreateFromData(2, 1, false, Image.Format.Rgba8,
            [255, 0, 0, 255, 0, 255, 0, 255]);
        using var texture = ImageTexture.CreateFromImage(image);
        using var atlas = new AtlasTexture { Atlas = texture, Region = new Rect2(1, 0, 1, 1) };
        using var shader = fixture is null ? null : LoadShader(fixture);
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        var window = new Window { Size = new(100, 60), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var filled = new Polygon
        {
            Name = "Filled",
            Vertices = [new(0, 0), new(20, 0), new(20, 20), new(0, 20)],
            Position = new(4, 4),
            Color = Colors.Red,
            Material = material,
            Polygons = [[0, 1, 2], [0, 2, 3]],
        };
        var textured = new Polygon
        {
            Name = "Textured",
            Vertices = [new(0, 0), new(20, 0), new(20, 20), new(0, 20)],
            Position = new(32, 4),
            Texture = texture,
            UV = [new(0, 0), new(2, 0), new(2, 1), new(0, 1)],
        };
        var inverted = new Polygon
        {
            Name = "Inverted",
            Vertices = [new(0, 0), new(12, 0), new(12, 12), new(0, 12)],
            Position = new(64, 20),
            InvertEnabled = true,
            InvertBorder = 8,
            Color = Colors.Yellow,
        };
        window.AddChild(filled); window.AddChild(textured); window.AddChild(inverted);
        var rig = new Skeleton { Name = "Rig" };
        var bone = new Bone { Name = "Bone", Rest = Transform.Identity };
        bone.SetAutocalculateLengthAndAngle(false);
        rig.AddChild(bone);
        window.AddChild(rig);
        var skin = new Polygon
        {
            Position = new(4, 36),
            Skeleton = "../Rig",
            Color = Colors.Blue,
            Antialiased = true,
            Vertices = [Vector2.Zero, new(16, 0), new(16, 16), new(0, 16), new(8, 8)],
            InternalVertexCount = 1,
        };
        skin.AddBone("Bone", [1, 1, 1, 1]);
        window.AddChild(skin);
        var driver = "";
        var adapter = "";
        var ran = false;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Polygon pixels use the requested backend.");
            driver = RenderingServer.GetCurrentRenderingDriverName();
            adapter = RenderingServer.GetVideoAdapterName();
            RenderingServer.RenderLoopEnabled = false;
            RenderingServer.SetDefaultClearColor(Colors.Black);
            bone.Position = new(10, 0);

            void VerifyFrame(int step)
            {
                RenderingServer.ForceDraw();
                using var frame = window.GetTexture().GetImage()!;
                Pixel(frame, 8, 8, step == 0 ? Colors.Red : Colors.Blue);
                Pixel(frame, 22, 22, step == 0 ? Colors.Red : Colors.Blue);
                Pixel(frame, 28, 8, Colors.Black);
                Pixel(frame, 35, 12, step switch { 0 => Colors.Red, 1 => Colors.Green, 2 or 3 => Colors.Magenta, 4 => Colors.Cyan, _ => Colors.Blue });
                Pixel(frame, 49, 22, step switch { 0 => Colors.Green, 1 => Colors.Red, 2 or 3 or 4 => Colors.Cyan, _ => Colors.Blue });
                Pixel(frame, 59, 30, Colors.Yellow); Pixel(frame, 81, 30, Colors.Yellow);
                Pixel(frame, 70, 26, Colors.Black); Pixel(frame, 90, 30, Colors.Black);
                Pixel(frame, 54, 30, step == 0 ? Colors.Black : Colors.Yellow);
                Pixel(frame, 8, 43, step == 1 ? Colors.Blue : Colors.Black);
                Pixel(frame, 22, 43, step == 1 ? Colors.Black : Colors.Blue);
                if (Environment.GetEnvironmentVariable("ELECTRON2D_POLYGON_OUTPUT") is { } output)
                    frame.SavePNG(System.IO.Path.Combine(output, $"polygon-{backend}-{fixture ?? "default"}-{step}.png"));
            }

            VerifyFrame(0);
            filled.Color = Colors.Blue;
            inverted.InvertBorder = 12;
            textured.TextureRotation = MathF.PI / 2;
            textured.TextureOffset = new(1.25f, 0);
            textured.TextureScale = new(2, 1);
            skin.Polygons = [[0, 1, 2, 3]];
            VerifyFrame(1);
            skin.Polygons = [];
            using var edited = Image.CreateFromData(2, 1, false, Image.Format.Rgba8, [0, 255, 255, 255, 255, 0, 255, 255]);
            texture.Update(edited);
            VerifyFrame(2);
            using var resized = Image.CreateFromData(4, 1, false, Image.Format.Rgba8,
                [0, 255, 255, 255, 255, 0, 255, 255, 255, 255, 0, 255, 255, 255, 255, 255]);
            texture.SetImage(resized);
            VerifyFrame(3);
            textured.Texture = atlas;
            textured.UV = [Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero];
            textured.TextureOffset = Vector2.Zero;
            VerifyFrame(4);
            textured.Texture = null;
            textured.Color = Colors.Blue;
            VerifyFrame(5);

            void Redraw(int i)
            {
                inverted.InvertBorder = 10 + (i & 1);
                skin.QueueRedraw();
                bone.Position = new(10 + (i & 1), 0);
                RenderingServer.ForceDraw();
            }

            for (var i = 0; i < 64; i++) Redraw(i);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 128; i++) Redraw(i);
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared inversion redraw and skin presentation allocate zero owner bytes.");
            ran = true;
            window.Tree!.Quit();
        };
        Check(Engine.Run(window) == 0 && window.IsDisposed && !RenderingServer.IsAvailable,
            "The window and its renderer shut down after the scenario.");
        Check(ran && !texture.IsDisposed, "All frames completed and node disposal preserved the borrowed texture.");
        Console.WriteLine($"Polygon pixels, texture update and retirement, skin source changes and 128 redraws at 0 owner bytes passed: {backend}/{driver}, {adapter}, {fixture ?? "default"}.");
    }
}
