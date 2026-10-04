using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyCanvasTexture(string backend, string? fixture = null)
    {
        using var source = Image.CreateFromData(2, 2, false, Image.Format.Rgba8,
            new byte[] { 255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 0, 255 });
        using var texture = ImageTexture.CreateFromImage(source);
        using var empty = new ImageTexture();
        using var shader = fixture is null ? null : LoadShader(fixture);
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        using var custom = new DrawOverrideTexture();
        using var caller = new Entity();
        Reject<InvalidOperationException>(() => caller.DrawTexture(texture, Vector2.Zero));
        Reject<InvalidOperationException>(() => texture.Draw(caller, Vector2.Zero));
        Reject<ArgumentNullException>(() => texture.Draw(null!, Vector2.Zero));
        if (shader is not null)
        {
            Check(shader.GetShaderUniformList().Count == 0, "The canvas TEXTURE is not a material parameter.");
            Reject<ArgumentException>(() => shader.SetDefaultTextureParameter("TEXTURE", texture));
            Reject<ArgumentException>(() => material!.SetShaderParameter("TEXTURE", texture));
        }
        var window = new Window { CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest, Size = new Vector2i(128, 96) };
        var frames = 0;
        var node = new CanvasNode
        {
            Material = material,
            DrawAction = n =>
        {
            Reject<ArgumentNullException>(() => n.DrawTexture(null!, Vector2.Zero));
            Reject<ArgumentException>(() => n.DrawTexture(texture, new Vector2(float.NaN, 0)));
            Reject<ArgumentException>(() => n.DrawTextureRect(texture, new Rect2(0, 0, 2, float.PositiveInfinity), false));
            Reject<ArgumentException>(() => n.DrawTextureRectRegion(texture, new Rect2(0, 0, 2, 2), new Rect2(float.NaN, 0, 1, 1)));
            Reject<ArgumentException>(() => n.DrawTexture(texture, Vector2.Zero, new Color(float.NaN, 1, 1, 1)));
            n.DrawTexture(empty, Vector2.Zero);
            n.DrawTextureRect(texture, new Rect2(0, 0, 0, 5), false);
            n.DrawTexture(texture, new Vector2(4, 4));
            n.DrawTextureRect(texture, new Rect2(8, 8, 16, 16), false);
            texture.DrawRect(n, new Rect2(28, 8, -16, 16), false);
            n.DrawTextureRect(texture, new Rect2(48, 8, 16, -16), false);
            n.DrawTextureRect(texture, new Rect2(68, 8, 16, 8), false, transpose: true);
            n.DrawTextureRectRegion(texture, new Rect2(8, 32, 16, 16), new Rect2(1, 0, 1, 1));
            texture.DrawRectRegion(n, new Rect2(28, 32, -16, 16), new Rect2(0, 0, -2, 2));
            n.DrawTextureRect(texture, new Rect2(48, 32, 16, 16), true);
            n.DrawTextureRectRegion(texture, new Rect2(68, 32, 16, 16), new Rect2(-2, -2, 1, 1), clipUV: false);
            texture.Draw(n, new Vector2(8, 64), transpose: true);
            n.DrawSetTransform(new Vector2(28, 64), scale: new Vector2(4, 4));
            n.DrawTexture(texture, Vector2.Zero, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            n.DrawSetTransform(Vector2.Zero);
            n.DrawTexture(custom, new Vector2(96, 8));
            n.DrawTextureRect(custom, new Rect2(96, 20, 4, 4), false);
            n.DrawTextureRectRegion(custom, new Rect2(96, 32, 4, 4), new Rect2(0, 0, 1, 1));
            n.DrawRect(new Rect2(96, 48, 4, 4), Colors.White);
        }
        };
        node.ReadyAction = n =>
        {
            var server = RenderingServer.Service!;
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "The canvas texture backend is active.");
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                frames++;
                using var frame = server.Readback();
                try
                {
                    if (frames == 1)
                    {
                        Pixel(frame, 4, 4, Colors.Red); Pixel(frame, 5, 5, Colors.Yellow);
                        Pixel(frame, 10, 10, Colors.Red); Pixel(frame, 22, 22, Colors.Yellow);
                        Pixel(frame, 30, 10, Colors.Green); Pixel(frame, 42, 22, Colors.Blue);
                        Pixel(frame, 50, 10, Colors.Blue); Pixel(frame, 62, 22, Colors.Green);
                        Pixel(frame, 69, 10, Colors.Red); Pixel(frame, 74, 10, Colors.Blue);
                        Pixel(frame, 69, 22, Colors.Green); Pixel(frame, 80, 10, Colors.Black);
                        Pixel(frame, 10, 34, Colors.Green); Pixel(frame, 22, 46, Colors.Green);
                        Pixel(frame, 30, 34, Colors.Red); Pixel(frame, 42, 46, Colors.Yellow);
                        Pixel(frame, 48, 32, Colors.Red); Pixel(frame, 49, 32, Colors.Green);
                        Pixel(frame, 50, 32, Colors.Red); Pixel(frame, 49, 33, Colors.Yellow);
                        Pixel(frame, 70, 34, Colors.Red);
                        Pixel(frame, 8, 65, Colors.Green);
                        Pixel(frame, 29, 65, new Color(0.25f, 0, 0, 1));
                        source.Fill(Colors.Cyan); texture.Update(source);
                    }
                    else
                    {
                        var expected = frames == 2 ? Colors.Cyan : Colors.Magenta;
                        Pixel(frame, 10, 10, expected); Pixel(frame, 30, 10, expected);
                        Pixel(frame, 49, 33, expected);
                        Pixel(frame, 29, 65, new Color(expected.R * 0.25f, expected.G * 0.25f, expected.B * 0.25f, 1));
                        if (frames == 2)
                        {
                            using var replacement = Image.CreateEmpty(3, 2, false, Image.Format.Rgb8);
                            replacement.Fill(Colors.Magenta);
                            texture.SetImage(replacement);
                        }
                        else n.Tree!.Quit();
                    }
                    Pixel(frame, 97, 9, Colors.Magenta); Pixel(frame, 97, 21, Colors.Magenta);
                    Pixel(frame, 97, 33, Colors.Magenta); Pixel(frame, 97, 49, Colors.White);
                    Check(node.Draws == 1 && custom.Draws == 3, "Texture updates reuse retained commands and custom virtual drawing.");
                }
                catch (Exception e) { throw new InvalidOperationException($"{backend}/{fixture ?? "default"} canvas texture frame {frames}: {e.Message}", e); }
            };
        };
        window.AddChild(node);
        Engine.Run(window);
        Released(window);
        Check(frames == 3 && !texture.IsDisposed, "Canvas texture ownership survives renderer shutdown.");
        Console.WriteLine($"Canvas texture checks passed: {backend}/{fixture ?? "default"}.");
    }

    private static void VerifyCanvasUV()
    {
        using var source = Image.CreateEmpty(4, 1, false, Image.Format.Rgba8);
        source.Fill(Colors.White);
        using var texture = ImageTexture.CreateFromImage(source);
        texture.SetSizeOverride(new Vector2i(8, 2));
        using var shader = LoadShader("CanvasUV");
        using var material = new ShaderMaterial { Shader = shader };
        var window = new Window { CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest, Size = new Vector2i(96, 80) };
        window.AddChild(new CanvasNode
        {
            Material = material,
            DrawAction = n =>
            {
                n.DrawTextureRectRegion(texture, new Rect2(0, 0, 32, 8), new Rect2(0, 0, 8, 2));
                n.DrawTextureRectRegion(texture, new Rect2(0, 16, 32, 8), new Rect2(0, 0, 8, 2), clipUV: false);
            },
            ReadyAction = n => RenderingServer.FramePostDraw += () =>
            {
                using var frame = RenderingServer.Service!.Readback();
                Pixel(frame, 0, 1, new Color(0.125f, 0.5f, 0, 1));
                Pixel(frame, 8, 1, new Color(8.5f / 32, 0.5f, 0, 1));
                Pixel(frame, 31, 1, new Color(0.875f, 0.5f, 0, 1));
                Pixel(frame, 0, 17, new Color(0.5f / 32, 1.5f / 8, 0, 1));
                n.Tree!.Quit();
            }
        });
        Engine.Run(window); Released(window);
    }

    private static void VerifyCanvasHDR(string backend)
    {
        using var source = Image.CreateEmpty(1, 1, false, Image.Format.Rgbaf);
        source.Fill(new Color(2, 0, 0, 1));
        using var texture = ImageTexture.CreateFromImage(source);
        var window = new Window { CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest, Size = new Vector2i(96, 80) };
        window.AddChild(new CanvasNode
        {
            DrawAction = n => n.DrawTextureRect(texture, new Rect2(0, 0, 16, 16), false, new Color(0.25f, 1, 1, 1)),
            ReadyAction = n => RenderingServer.FramePostDraw += () =>
            {
                using var frame = RenderingServer.Service!.Readback();
                Pixel(frame, 8, 8, new Color(0.5f, 0, 0, 1));
                n.Tree!.Quit();
            }
        });
        try { Engine.Run(window); Console.WriteLine($"Canvas HDR precision passed: {backend}."); }
        catch (NotSupportedException e) when (backend == "compatibility" && e.Message.Contains("HDR texture precision"))
        { Console.WriteLine("Canvas HDR precision explicitly rejected by this compatibility driver."); }
        Released(window);
    }

    private static void VerifyCanvasGeometryAllocations()
    {
        using var source = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
        using var texture = ImageTexture.CreateFromImage(source);
        using var node = new CanvasNode { DrawAction = n => n.DrawTextureRectRegion(texture, new Rect2(0, 0, 64, 64), new Rect2(0, 0, 4, 4)) };
        var vertices = new List<CanvasVertex>();
        var batches = new List<CanvasBatch>();
        node.PrepareCanvas();
        for (var i = 0; i < 1000; i++) { vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, node.GetGlobalTransform()); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) { vertices.Clear(); batches.Clear(); node.AppendCanvas(vertices, batches, node.GetGlobalTransform()); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before && node.Draws == 1, "Retained texture geometry allocates no managed memory after warmup.");
    }

    private static void VerifyCanvasTextureFailures()
    {
        using var source = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        using var texture = ImageTexture.CreateFromImage(source);
        var window = new Window();
        window.AddChild(new CanvasNode { DrawAction = n => { n.DrawTexture(texture, Vector2.Zero); texture.Dispose(); } });
        Reject<ObjectDisposedException>(() => Engine.Run(window));
        Released(window);
        using var unreadable = new UnreadableTexture();
        window = new Window();
        window.AddChild(new CanvasNode { DrawAction = n => n.DrawTexture(unreadable, Vector2.Zero) });
        Reject<InvalidOperationException>(() => Engine.Run(window));
        Released(window);
    }

    private sealed class UnreadableTexture : Texture
    {
        public override int GetWidth() => 2;
        public override int GetHeight() => 2;
    }

    private sealed class DrawOverrideTexture : Texture
    {
        internal int Draws;
        public override int GetWidth() => 4;
        public override int GetHeight() => 4;
        public override void Draw(CanvasItem canvasItem, Vector2 position, Color? modulate = null, bool transpose = false)
        { Draws++; canvasItem.DrawRect(new Rect2(position, GetSize()), Colors.Magenta); }
        public override void DrawRect(CanvasItem canvasItem, Rect2 rect, bool tile, Color? modulate = null, bool transpose = false)
        { Draws++; canvasItem.DrawRect(rect, Colors.Magenta); }
        public override void DrawRectRegion(CanvasItem canvasItem, Rect2 rect, Rect2 sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)
        { Draws++; canvasItem.DrawRect(rect, Colors.Magenta); }
    }
}
