using Electron2D;

internal static partial class RenderingRuntimeTests
{
    internal static void VerifyAtlasResources()
    {
        using var image = Image.CreateEmpty(4, 2, false, Image.Format.Rgba8);
        image.Fill(Colors.Red); image.SetPixel(1, 0, Colors.Transparent);
        using var texture = ImageTexture.CreateFromImage(image);
        using var atlas = new AtlasTexture();
        Check(atlas.Size == Vector2.One && !atlas.HasAlpha && !atlas.HasMipmaps && atlas.GetImage() is null &&
            atlas.IsPixelOpaque(int.MaxValue, int.MinValue) && atlas.Region == default && atlas.Margin == default &&
            !atlas.FilterClip && !atlas.ResourceLocalToScene && atlas.PixelFormat == Image.Format.Max && atlas.MipmapCount == 0, "Empty atlas defaults.");
        using var mipImage = MipImage(Colors.Red, Colors.Green, Colors.Blue);
        using var mipSource = ImageTexture.CreateFromImage(mipImage);
        using var mipAtlas = new AtlasTexture { Atlas = mipSource, Region = new Rect(0, 0, 1, 1) };
        Check(!mipAtlas.HasMipmaps && mipAtlas.MipmapCount == 0 && mipAtlas.PixelFormat == Image.Format.Max &&
            mipAtlas.CapturePixels()!.Levels == 3, "View metadata remains unspecified/no mips while rendering retains full source mip storage.");
        var changes = 0; atlas.Changed += _ => changes++;
        atlas.Atlas = texture; atlas.Atlas = texture; atlas.Region = default; atlas.Margin = default;
        Check(changes == 1 && atlas.Size == new Vector2(4, 2), "Identity and equal rectangle setters do not emit.");
        atlas.FilterClip = false;
        Check(changes == 2, "Filter clipping emits even when unchanged.");
        atlas.Region = new Rect(1.25f, 0, 2.9f, 1.9f); atlas.Margin = new Rect(1, 1, 3.9f, 2.9f);
        Check(atlas.Size == new Vector2(5, 3) && atlas.Region.Size == new Vector2(2.9f, 1.9f), "Only region size is floored; dimension sums truncate.");
        using (var cropped = atlas.GetImage()!)
        {
            Check(cropped.Size == new Vector2I(2, 1) && !cropped.HasMipmaps, "Image crop excludes margins and mipmaps.");
            Check(cropped.GetPixel(0, 0).A == 0 && cropped.GetPixel(1, 0) == Colors.Red, "Fractional position truncates for image extraction.");
            cropped.Fill(Colors.Blue);
        }
        Check(!atlas.IsPixelOpaque(1, 1) && atlas.IsPixelOpaque(0, 1) && !atlas.IsPixelOpaque(int.MaxValue, 0), "Opacity translates into full atlas bounds, including neighboring margin pixels.");
        Check(ReferenceEquals(atlas.RenderingTexture, texture) && ReferenceEquals(atlas.CapturePixels(), texture.CapturePixels()), "Rendering shares full source storage.");
        var before = changes; image.Fill(Colors.Green); texture.Update(image);
        Check(changes == before && atlas.IsPixelOpaque(1, 1), "Ordinary source changes do not forward but pixels remain live.");
        atlas.Region = new Rect(0, 0, 0, 1.9f);
        Check(atlas.Size == new Vector2(4, 3), "Zero region axis ignores its margin size.");
        Reject<ArgumentException>(() => atlas.Region = new Rect(float.NaN, 0, 1, 1));
        Reject<ArgumentException>(() => atlas.Margin = new Rect(0, 0, float.PositiveInfinity, 1));
        using var nested = new AtlasTexture { Atlas = atlas, Region = new Rect(0, 0, 1, 1) };
        var nestedChanges = 0; nested.Changed += _ => nestedChanges++;
        atlas.FilterClip = true;
        Check(nestedChanges == 1 && ReferenceEquals(nested.RenderingTexture, texture), "Nested atlas changes forward and storage resolves recursively.");
        Reject<ArgumentException>(() => atlas.Atlas = nested);
        Reject<ArgumentException>(() => atlas.Atlas = atlas);
        Check(ReferenceEquals(atlas.Atlas, texture), "Cycle rejection preserves the original edge.");
        using var dead = new ImageTexture(); dead.Dispose();
        Reject<ObjectDisposedException>(() => atlas.Atlas = dead);
        using var shallow = (AtlasTexture)nested.Duplicate();
        Check(ReferenceEquals(shallow.Atlas, atlas) && shallow.Region == nested.Region, "Shallow duplication borrows source.");
        using var deep = (AtlasTexture)nested.Duplicate(true);
        using var deepInner = (AtlasTexture)deep.Atlas!;
        using var deepTexture = (ImageTexture)deepInner.Atlas!;
        Check(!ReferenceEquals(deepInner, atlas) && !ReferenceEquals(deepTexture, texture) && deepInner.Margin == atlas.Margin && deepInner.FilterClip,
            "Deep duplication copies built-in atlas graphs and view state.");
        using var copy = new AtlasTexture { Atlas = nested };
        var copyChanges = 0; copy.Changed += _ => copyChanges++;
        copy.CopyFromResource(atlas);
        Check(copyChanges == 1 && ReferenceEquals(copy.Atlas, texture) && copy.Region == atlas.Region, "Copy coalesces changes and replaces source subscriptions.");
        atlas.TakeOverPath("res://atlas-test");
        using var external = (AtlasTexture)nested.Duplicate(true);
        Check(ReferenceEquals(external.Atlas, atlas), "Internal duplication preserves external resource references.");
        using var all = (AtlasTexture)nested.DuplicateDeep(DeepDuplicateMode.All);
        using var allInner = (AtlasTexture)all.Atlas!;
        using var allTexture = (ImageTexture)allInner.Atlas!;
        Check(!ReferenceEquals(allInner, atlas), "All mode duplicates external atlas resources.");
        atlas.ResourcePath = "";
        atlas.ResourceLocalToScene = nested.ResourceLocalToScene = texture.ResourceLocalToScene = true;
        using var sprite = new Sprite { Texture = nested };
        using var scene = new PackedScene(); scene.Pack(sprite);
        AtlasTexture local, localInner; Texture localTexture;
        using (var instance = (Sprite)scene.Instantiate())
        {
            local = (AtlasTexture)instance.Texture!; localInner = (AtlasTexture)local.Atlas!; localTexture = localInner.Atlas!;
            Check(!ReferenceEquals(local, nested) && !ReferenceEquals(localInner, atlas) && !ReferenceEquals(localTexture, texture) &&
                ReferenceEquals(local.GetLocalScene(), instance) && ReferenceEquals(localTexture.GetLocalScene(), instance), "Packed scenes localize the complete atlas graph.");
        }
        Check(local.IsDisposed && localInner.IsDisposed && localTexture.IsDisposed && !texture.IsDisposed, "Scene root owns local duplicates, not the template sources.");
        nested.Atlas = texture; nestedChanges = 0; atlas.FilterClip = false;
        Check(nestedChanges == 0, "Replacing a nested atlas disconnects its old source.");
        Action<Resource> failure = _ => throw new ApplicationException("atlas change");
        atlas.Changed += failure;
        Reject<ApplicationException>(() => atlas.Margin = default);
        Check(atlas.Margin == default, "Observer failure follows committed state.");
        atlas.Changed -= failure;
        using var first = new AtlasTexture(); using var second = new AtlasTexture();
        Parallel.Invoke(() => { try { first.Atlas = second; } catch (ArgumentException) { } },
            () => { try { second.Atlas = first; } catch (ArgumentException) { } });
        Check(!(ReferenceEquals(first.Atlas, second) && ReferenceEquals(second.Atlas, first)), "Concurrent setters cannot create a cycle.");
        atlas.Dispose();
        Reject<ObjectDisposedException>(() => _ = atlas.Width);
        Reject<ObjectDisposedException>(() => atlas.GetImage());
        Reject<ObjectDisposedException>(() => atlas.CapturePixels());
        Check(!texture.IsDisposed, "Disposing a view retains the borrowed source.");
        VerifyAtlasMapping();
        Console.WriteLine("Atlas resource, duplication, scene and drawing mapping checks passed.");
    }

    private static void VerifyAtlasMapping()
    {
        using var source = new AtlasDrawProbe();
        using var atlas = new AtlasTexture { Atlas = source, Region = new Rect(2, 3, 4.9f, 2.9f), Margin = new Rect(1, 2, 2, 4) };
        using var empty = new AtlasTexture();
        using var node = new CanvasNode();
        Reject<InvalidOperationException>(() => empty.Draw(node, Vector2.Zero));
        Reject<ArgumentNullException>(() => empty.Draw(null!, Vector2.Zero));
        node.DrawAction = n =>
        {
            Reject<ArgumentException>(() => empty.Draw(n, new Vector2(float.NaN, 0)));
            Reject<ArgumentException>(() => empty.DrawRectRegion(n, default, new Rect(0, 0, float.NaN, 0)));
            empty.Draw(n, Vector2.Zero);
            atlas.Draw(n, new Vector2(10, 20), Colors.Red, true);
            Check(source.Destination == new Rect(11, 22, 4, 2) && source.Source == new Rect(2, 3, 4, 2) &&
                source.Transpose && source.Color == Colors.Red && !source.Clip, "Position drawing offsets margins without stretching.");
            atlas.DrawRect(n, new Rect(10, 20, 60, 60), true);
            Check(source.Destination == new Rect(20, 40, 40, 20) && source.Source == new Rect(2, 3, 4, 2), "Tiling request stretches once with proportional margins.");
            atlas.DrawRect(n, new Rect(10, 20, -60, -60), false);
            Check(source.Destination == new Rect(20, 40, -40, -20), "Negative destinations preserve origin and mirrored margins.");
            atlas.DrawRectRegion(n, new Rect(0, 0, 8, 8), new Rect(1, 2, 2, 2), clipUV: true);
            Check(source.Destination == new Rect(0, 0, 8, 8) && source.Source == new Rect(2, 3, 2, 2) && !source.Clip, "Source translation and resource clipping override.");
            atlas.FilterClip = true;
            atlas.DrawRectRegion(n, new Rect(0, 0, 8, 8), new Rect(1, 2, 2, 2), clipUV: false);
            Check(source.Clip, "FilterClip overrides false caller flag as well.");
            var draws = source.Draws;
            atlas.DrawRectRegion(n, new Rect(0, 0, 8, 8), new Rect(100, 100, 1, 1));
            atlas.DrawRectRegion(n, new Rect(0, 0, 8, 8), new Rect(0, 0, 0, 1));
            Check(source.Draws == draws, "Outside and degenerate regions produce no commands.");
            atlas.Margin = default;
            atlas.DrawRectRegion(n, new Rect(0, 0, 8, 4), default);
            Check(source.Source == new Rect(2, 3, 4, 2), "Both zero source axes use the region.");
            using var outer = new AtlasTexture { Atlas = atlas, Region = new Rect(1, 0, 2, 2) };
            outer.DrawRect(n, new Rect(0, 0, 8, 8), false);
            Check(source.Source == new Rect(3, 3, 2, 2) && source.Clip, "Nested views remap through each layer; innermost FilterClip wins.");
            atlas.Region = default;
            source.ReadWidthAction = () => atlas.Atlas = null;
            atlas.Draw(n, Vector2.Zero);
            Check(atlas.Atlas is null && source.Source == new Rect(0, 0, 16, 16), "Reentrant source queries do not replace the in-flight view snapshot.");
        };
        node.PrepareCanvas();
    }

    private sealed class AtlasDrawProbe : Texture
    {
        internal Action? ReadWidthAction;
        public override int Width { get { ReadWidthAction?.Invoke(); return 16; } }
        public override int Height => 16;
        internal Rect Destination, Source;
        internal Color? Color;
        internal bool Transpose, Clip;
        internal int Draws;
        public override void DrawRectRegion(CanvasItem canvasItem, Rect rect, Rect sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)
        { Destination = rect; Source = sourceRect; Color = modulate; Transpose = transpose; Clip = clipUV; Draws++; }
    }

    private static void VerifyAtlasFrame(string backend, string? fixture = null)
    {
        using var image = Image.CreateEmpty(4, 2, false, Image.Format.Rgba8);
        for (var y = 0; y < 2; y++)
        {
            image.SetPixel(0, y, Colors.Red); image.SetPixel(1, y, Colors.Green);
            image.SetPixel(2, y, Colors.Blue); image.SetPixel(3, y, Colors.Yellow);
        }
        using var texture = ImageTexture.CreateFromImage(image);
        using var atlas = new AtlasTexture { Atlas = texture, Region = new Rect(1, 0, 2, 2), Margin = new Rect(1, 1, 2, 2), FilterClip = true };
        using var nested = new AtlasTexture { Atlas = atlas, Region = new Rect(2, 1, 1, 2) };
        using var shader = fixture is null ? null : LoadShader(fixture);
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        var window = new Window { Size = new Vector2I(128, 96), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var sprite = new Sprite { Texture = atlas, Centered = false, Position = new Vector2(8, 64), Scale = new Vector2(4, 4), Material = material };
        var child = new Sprite { Name = "Nested", Texture = nested, Centered = false, Position = new Vector2(40, 64), Scale = new Vector2(8, 8), Material = material };
        window.AddChild(sprite); window.AddChild(child);
        var frames = 0;
        var node = new CanvasNode
        {
            Material = material,
            DrawAction = n =>
            {
                atlas.Draw(n, new Vector2(4, 4));
                atlas.DrawRect(n, new Rect(8, 16, 32, 32), true);
                atlas.DrawRect(n, new Rect(48, 16, -32, 32), false);
                atlas.DrawRectRegion(n, new Rect(88, 16, 16, 8), new Rect(1, 1, 1, 2), transpose: true);
            },
            ReadyAction = n =>
            {
                var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
                server.FramePostDraw += () =>
                {
                    frames++;
                    using var frame = server.Readback();
                    try
                    {
                        if (frames < 3)
                        {
                            Pixel(frame, 4, 4, Colors.Black); Pixel(frame, 5, 5, Colors.Green); Pixel(frame, 6, 5, Colors.Blue);
                            Pixel(frame, 10, 18, Colors.Black); Pixel(frame, 18, 26, Colors.Green); Pixel(frame, 30, 38, Colors.Blue);
                            Pixel(frame, 58, 26, Colors.Blue); Pixel(frame, 70, 26, Colors.Green); Pixel(frame, 90, 20, Colors.Green);
                            Pixel(frame, 13, 70, frames == 1 ? Colors.Green : Colors.Red);
                            // Software input truncates half-texel UVs before interpolation; hardware retains their fractions.
                            var software = server.GetCurrentRenderingDriverName() == "software";
                            Pixel(frame, 17, 70, software ? (frames == 1 ? Colors.Green : Colors.Red) : (frames == 1 ? Colors.Blue : Colors.Green));
                            Pixel(frame, 19, 70, frames == 1 ? Colors.Blue : Colors.Green);
                            Pixel(frame, 42, 70, frames == 1 ? Colors.Blue : Colors.Green);
                            if (frames == 1) atlas.Region = new Rect(0, 0, 2, 2);
                            else { image.Fill(Colors.Cyan); texture.Update(image); }
                        }
                        else
                        {
                            Pixel(frame, 18, 26, Colors.Cyan); Pixel(frame, 13, 70, Colors.Cyan); Pixel(frame, 42, 70, Colors.Cyan);
                            n.Tree!.Quit();
                        }
                        Check(((CanvasNode)n).Draws == 1, "Atlas region changes redraw Sprite but custom commands retain their recorded region.");
                    }
                    catch (Exception e) { throw new InvalidOperationException($"Atlas {backend}/{fixture ?? "default"} frame {frames}: {e.Message}", e); }
                };
            }
        };
        window.AddChild(node); Engine.Instance.Run(window); Released(window);
        Check(frames == 3 && !atlas.IsDisposed && !texture.IsDisposed, "Renderer shutdown retains borrowed atlas resources.");
        Console.WriteLine($"Atlas pixel checks passed: {backend}/{fixture ?? "default"}.");
    }
    private static void VerifyAtlasMaterial(string fixture)
    {
        using var image = Image.CreateEmpty(2, 1, false, Image.Format.Rgba8);
        image.SetPixel(0, 0, Colors.Red); image.SetPixel(1, 0, Colors.Green);
        using var texture = ImageTexture.CreateFromImage(image);
        using var atlas = new AtlasTexture { Atlas = texture, Region = new Rect(1, 0, 1, 1) };
        using var nested = new AtlasTexture { Atlas = atlas };
        using var shader = LoadShader(fixture);
        using var material = new ShaderMaterial { Shader = shader };
        shader.SetDefaultTextureParameter("colorMap", nested);
        material.SetShaderParameter("detailMap", texture);
        material.SetShaderParameter("tint", Colors.White);
        var window = new Window { Size = new Vector2I(96, 80), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var frames = 0;
        window.AddChild(new CanvasNode
        {
            Material = material,
            DrawAction = n => n.DrawRect(new Rect(0, 0, 16, 16), Colors.White),
            ReadyAction = _ => RenderingServer.Instance!.FramePostDraw += () =>
            {
                using var frame = RenderingServer.Instance.Readback();
                Pixel(frame, 8, 8, frames == 0 ? Colors.Red : Colors.Cyan);
                if (++frames == 1) { image.Fill(Colors.Cyan); texture.Update(image); }
                else atlas.Atlas = null;
            }
        });
        try { Engine.Instance.Run(window); throw new Exception("An empty material atlas must fail."); }
        catch (InvalidOperationException error) when (error.Message.Contains("sampled atlas has no source")) { }
        Released(window);
        Check(frames == 2 && !atlas.IsDisposed && !texture.IsDisposed, "Material atlases sample full live storage and reject an empty source on the following frame.");
        Console.WriteLine($"Atlas material binding checks passed: {fixture}.");
    }

}
