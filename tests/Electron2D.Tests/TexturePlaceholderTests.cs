using Electron2D;
using System.Diagnostics;
using static TextureArrayTests;

internal static class TexturePlaceholderTests
{
    private sealed class LogicalTexture : Texture
    {
        public override int GetWidth() => 19;
        public override int GetHeight() => 11;
        public override Vector2 GetSize() => new(19.75f, 11.5f);
    }

    private sealed class RIDPatch(RID texture) : Entity
    {
        protected override void OnDraw() => DrawTexture(texture, Vector2.Zero);
    }

    internal static void RunManaged()
    {
        using var metadata = new PlaceholderTexture();
        Check(metadata.Size == Vector2.One && metadata.GetSize() == Vector2.One && !metadata.ResourceLocalToScene, "Placeholder defaults.");
        Check(metadata.GetImage() is null && !metadata.HasAlpha && !metadata.HasMIPMaps && metadata.MIPMapCount == 0 && metadata.PixelFormat == Image.Format.Max && metadata.IsPixelOpaque(-1, int.MaxValue), "No readable image, alpha or mipmaps; opaque fallback.");
        var changes = 0; metadata.Changed += _ => changes++;
        metadata.Size = new(19.75f, 11.5f); metadata.Size = new(19, 11);
        Check(changes == 2 && metadata.Size == new Vector2(19, 11) && metadata.GetWidth() == 19 && metadata.GetHeight() == 11, "Dimensions truncate and equal writes still notify.");
        foreach (var size in new Vector2[] { new(float.NaN, 1), new(1, float.PositiveInfinity), new(float.MaxValue, 1), new(1, -2147483904f) })
            Reject<ArgumentOutOfRangeException>(() => metadata.Size = size);
        Check(changes == 2 && metadata.Size == new Vector2(19, 11), "Invalid dimensions cannot mutate or notify.");
        using (var evented = new PlaceholderTexture())
        {
            Action<Resource> fail = _ => throw new InvalidOperationException("Changed failure"); evented.Changed += fail;
            Reject<InvalidOperationException>(() => evented.Size = new(7, 9));
            Check(evented.Size == new Vector2(7, 9), "Notification failure preserves committed metadata."); evented.Changed -= fail;
            evented.Changed += _ => { if (evented.Size == new Vector2(8, 10)) evented.Size = new(9, 11); };
            evented.Size = new(8, 10); Check(evented.Size == new Vector2(9, 11), "Reentrant change publishes outside the size gate.");
        }
        metadata.Size = new(-2.75f, 0); Check(metadata.Size == new Vector2(-2, 0), "Signed and zero metadata are preserved.");
        metadata.Size = new(int.MinValue, 2147483520f); Check(metadata.GetWidth() == int.MinValue && metadata.GetHeight() == 2147483520, "Finite integer conversion boundaries.");
        metadata.Size = new(19, 11);
        using var control = new TextureRect { Texture = metadata };
        Check(control.GetMinimumSize() == metadata.Size, "Texture layout uses placeholder dimensions without a renderer.");
        var rid = metadata.GetRID(); metadata.Size = new(23, 13);
        Check(metadata.GetRID() == rid && control.GetMinimumSize() == new Vector2(23, 13), "Size changes preserve RID and invalidate layout.");
        metadata.ResourceName = "Authored"; metadata.ResourceLocalToScene = true; metadata.SetPathCache("res://placeholder-source.e2dres");
        using var copy = (PlaceholderTexture)metadata.DuplicateDeep();
        Check(copy.Size == metadata.Size && copy.ResourceName == metadata.ResourceName && copy.ResourceLocalToScene && copy.ResourcePath.Length == 0 && copy.GetRID() != rid, "Duplication copies authored state and creates fresh identity.");
        copy.Size = new(5, 7); Check(metadata.Size == new Vector2(23, 13), "Copies have independent metadata.");
        using var target = new PlaceholderTexture(); target.CopyFromResource(metadata);
        Check(target.Size == metadata.Size && target.GetImage() is null, "Resource replacement copies dimensions without pixels.");
        using var derived = metadata.CreatePlaceholder();
        Check(derived.Size == metadata.Size && !derived.ResourceLocalToScene && derived.ResourceName.Length == 0 && derived.ResourcePath.Length == 0, "Placeholder creation copies only logical dimensions.");
        using var custom = new LogicalTexture(); using var customPlaceholder = custom.CreatePlaceholder();
        Check(customPlaceholder.Size == new Vector2(19, 11), "Factory consumes virtual logical size.");
        using var image = Image.CreateEmpty(3, 2, false, Image.Format.Rgba8); image.Fill(Colors.Red);
        var texture = ImageTexture.CreateFromImage(image); using var stripped = texture.CreatePlaceholder(); texture.Dispose();
        Check(stripped.Size == new Vector2(3, 2) && stripped.GetImage() is null, "Factory output survives source disposal without image data.");
        using (var logicalImage = ImageTexture.CreateFromImage(image))
        {
            logicalImage.SetSizeOverride(new(13, 7)); using var logical = logicalImage.CreatePlaceholder();
            Check(logical.Size == new Vector2(13, 7), "Factory retains logical size rather than physical image dimensions.");
            using var atlas = new AtlasTexture { Atlas = logicalImage, Region = new(0, 0, 2, 1), Margin = new(0, 0, 1, 1) }; using var atlasMetadata = atlas.CreatePlaceholder();
            Check(atlasMetadata.Size == new Vector2(3, 2), "Atlas view contributes region and margin dimensions.");
        }
        using (var viewport = new SubViewport { Size = new(33, 27) })
        using (var view = viewport.GetTexture())
        using (var viewMetadata = view.CreatePlaceholder()) Check(viewMetadata.Size == new Vector2(33, 27), "Detached viewport dimensions need no image readback.");
        Reject<ObjectDisposedException>(() => texture.CreatePlaceholder());
        var dead = new PlaceholderTexture(); dead.GetRID(); dead.Dispose();
        Reject<ObjectDisposedException>(() => dead.GetWidth()); Reject<ObjectDisposedException>(() => dead.GetHeight()); Reject<ObjectDisposedException>(() => dead.GetSize());
        Reject<ObjectDisposedException>(() => dead.GetImage()); Reject<ObjectDisposedException>(() => dead.GetRID()); Reject<ObjectDisposedException>(() => dead.Size = Vector2.One);
        VerifyArchives();
        Console.WriteLine("Texture placeholders: dimensions, conversion/guards, notifications, layout, fresh identities, source independence and fresh-process scene/resource archives passed.");
    }

    private static void VerifyArchives()
    {
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-placeholder-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using (var placeholder = new PlaceholderTexture { Size = new(24, 12), ResourceName = "Stripped", ResourceLocalToScene = true })
            using (var root = new Node())
            using (var scene = new PackedScene())
            {
                root.AddChild(new Sprite { Name = "First", Texture = placeholder }); root.AddChild(new Sprite { Name = "Second", Texture = placeholder });
                root.GetNode("First").Owner = root; root.GetNode("Second").Owner = root;
                scene.Pack(root);
                ResourceSaver.Save(placeholder, System.IO.Path.Combine(folder, "texture.e2dres"), SaverFlags.SaveBigEndian | SaverFlags.Compress);
                ResourceSaver.Save(scene, System.IO.Path.Combine(folder, "scene.e2dscene"), SaverFlags.BundleResources);
            }
            ReadArchive(folder);
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            start.ArgumentList.Add(typeof(TexturePlaceholderTests).Assembly.Location); start.ArgumentList.Add("--texture-placeholder-archive"); start.ArgumentList.Add(folder); start.Environment.Remove("ELECTRON2D_TEST_TEXTURE_PLACEHOLDER");
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh texture-placeholder process deadline."); }
            Check(child.ExitCode == 0, "Fresh archive process: " + output.GetAwaiter().GetResult() + errors.GetAwaiter().GetResult());
        }
        finally { Directory.Delete(folder, true); }
    }

    internal static void ReadArchive(string folder)
    {
        using var texture = ResourceLoader.Load<PlaceholderTexture>(System.IO.Path.Combine(folder, "texture.e2dres"), ResourceLoader.CacheMode.Ignore);
        Check(texture.Size == new Vector2(24, 12) && texture.ResourceName == "Stripped" && texture.ResourceLocalToScene && texture.GetImage() is null, "Explicit placeholder archive restores metadata without image data.");
        using var scene = ResourceLoader.Load<PackedScene>(System.IO.Path.Combine(folder, "scene.e2dscene"), ResourceLoader.CacheMode.Ignore);
        using var root = scene.Instantiate(); using var second = scene.Instantiate();
        var firstTexture = (PlaceholderTexture)((Sprite)root.GetNode("First")).Texture!;
        Check(ReferenceEquals(firstTexture, ((Sprite)root.GetNode("Second")).Texture) && !ReferenceEquals(firstTexture, ((Sprite)second.GetNode("First")).Texture), "Scene-local placeholders preserve aliases within each independent instance.");
        Check(firstTexture.Size == new Vector2(24, 12) && firstTexture.GetLocalScene() == root, "Scene-local metadata and association restored.");
        scene.Dispose(); Check(!firstTexture.IsDisposed, "Instance retains loaded graph after template disposal."); root.Dispose();
        Check(firstTexture.IsDisposed, "Root releases its own local placeholder.");
    }

    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var metadata = new PlaceholderTexture { Size = new(16, 16) };
        var rid = metadata.GetRID(); var window = new Window { Size = new(64, 32), Title = "Texture placeholder validation" };
        window.AddChild(new Sprite { Position = new(4, 4), Texture = metadata, Centered = false, TextureFilter = TextureFilter.Nearest });
        window.AddChild(new RIDPatch(rid) { Position = new(28, 4), TextureFilter = TextureFilter.Nearest });
        using var target = window.GetTexture(); var frames = 0; long start = 0, bytes = 0;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested renderer selected without fallback.");
            RenderingServer.SetDefaultClearColor(Colors.Green);
            using var pixels = RenderingServer.Texture2DGet(rid)!;
            Check(pixels.Width == 4 && pixels.Height == 4 && pixels.GetPixel(0, 0) == Colors.Magenta && pixels.GetPixel(1, 0) == Colors.Black, "Borrowed RID exposes independent diagnostic readback.");
            Reject<InvalidOperationException>(() => RenderingServer.FreeRID(rid));
            window.Tree!.ProcessFrameStarted += _ =>
            {
                if (frames < 16) return;
                start = GC.GetAllocatedBytesForCurrentThread(); metadata.Size = new((frames & 1) == 0 ? 16 : 8, 16);
            };
            RenderingServer.FramePostDraw += Capture;
        };
        void Capture()
        {
            if (frames >= 16) bytes += GC.GetAllocatedBytesForCurrentThread() - start;
            frames++;
            if (frames == 4)
            {
                using var image = target.GetImage()!;
                Pixel(image, 5, 5, Colors.Magenta); Pixel(image, 9, 5, Colors.Black); Pixel(image, 29, 5, Colors.Magenta);
                metadata.Size = Vector2.Zero;
            }
            if (frames == 5)
            {
                using var image = target.GetImage()!; Pixel(image, 5, 5, Colors.Green); metadata.Size = new(16, 16);
            }
            if (frames == 12) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers(); }
            if (frames != 80) return;
            Check(bytes == 0, $"Prepared placeholder size updates and rendering allocated {bytes} owner-thread managed bytes.");
            using var final = target.GetImage()!; Pixel(final, 5, 5, Colors.Magenta); Pixel(final, 13, 5, Colors.Green);
            if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } output) { Directory.CreateDirectory(output); final.SavePNG(System.IO.Path.Combine(output, "texture-placeholder-" + backend + ".png")); }
            RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
        }
        Engine.Run(window);
        Check(window.IsDisposed && !metadata.IsDisposed && metadata.GetRID() == rid, "Renderer shutdown preserves borrowed metadata and its RID.");
        Console.WriteLine($"Texture placeholder {backend}: diagnostic pixels, object/RID draws, zero-size redraw, borrowed ownership and 64 active frames={bytes} owner-thread managed bytes passed.");
    }

    private static void Pixel(Image image, int x, int y, Color expected)
    {
        var actual = image.GetPixel(x, y); Check(MathF.Abs(actual.R - expected.R) < .012f && MathF.Abs(actual.G - expected.G) < .012f && MathF.Abs(actual.B - expected.B) < .012f, $"Pixel {x},{y}: expected {expected}, got {actual}.");
    }
}
