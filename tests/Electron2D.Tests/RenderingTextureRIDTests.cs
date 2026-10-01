using System.Reflection;
using System.Runtime.CompilerServices;
using Electron2D;

internal static partial class RenderingRuntimeTests
{
    internal static void VerifyTextureRIDResources()
    {
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(Colors.Red);
        using var texture = ImageTexture.CreateFromImage(image);
        var rid = texture.GetRID();
        Check(rid.IsValid() && rid == texture.GetRID() && ReferenceEquals(RenderingTextureRegistry.Resolve(rid), texture), "Resource texture RID is stable before renderer startup.");
        using var duplicate = (ImageTexture)texture.Duplicate();
        Check(duplicate.GetRID() != rid, "Texture duplicates receive independent resource identities.");
        using var atlas = new AtlasTexture { Atlas = texture, Region = new(0, 0, 1, 1) };
        using var nested = new AtlasTexture { Atlas = atlas };
        using var emptyAtlas = new AtlasTexture();
        Check(atlas.GetRID() == rid && nested.GetRID() == rid && !emptyAtlas.GetRID().IsValid(), "Atlas views share their underlying source identity; empty views return empty.");
        atlas.Atlas = duplicate; Check(atlas.GetRID() == duplicate.GetRID(), "Changing an atlas source changes its borrowed RID.");
        atlas.Dispose(); Check(ReferenceEquals(RenderingTextureRegistry.Resolve(duplicate.GetRID()), duplicate), "Disposing a view preserves the underlying RID.");
        using var empty = new ImageTexture();
        Check(empty.GetRID().IsValid() && empty.GetImage() is null, "Uninitialized resource identities preserve the resource's empty state.");
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 256; i++) Check(texture.GetRID() == rid && ReferenceEquals(RenderingTextureRegistry.Resolve(rid), texture), "Warmed RID lookup stays live.");
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed resource RID lookup allocates no managed memory.");
        Check(Task.Run(texture.GetRID).GetAwaiter().GetResult() == rid, "Resource identity reads are independent of the renderer owner thread.");
        Reject<ArgumentException>(() => RenderingTextureRegistry.Resolve(default));
        using var shape = new CircleShape();
        Reject<ArgumentException>(() => RenderingTextureRegistry.Resolve(shape.GetRID()));
        texture.Dispose();
        Reject<ObjectDisposedException>(() => texture.GetRID());
        Reject<ArgumentException>(() => RenderingTextureRegistry.Resolve(rid));
        var registry = (System.Collections.IDictionary)typeof(RenderingTextureRegistry).GetField("Entries", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var count = registry.Count;
        var abandoned = AbandonTextureRID();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Check(!abandoned.Texture.TryGetTarget(out _), "A borrowed RID does not retain its resource.");
        for (var i = 0; i < 256; i++) { using var temporary = new ImageTexture(); temporary.GetRID(); }
        Check(registry.Count <= count, "Cold registration sweeps abandoned resource identities.");
        Reject<ArgumentException>(() => RenderingTextureRegistry.Resolve(abandoned.RID));
        Console.WriteLine("Texture RID resource identity, duplication, disposal, collection and allocation checks passed.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (RID RID, WeakReference<Texture> Texture) AbandonTextureRID()
    {
        var texture = new ImageTexture();
        return (texture.GetRID(), new(texture));
    }

    private static void VerifyRenderingTextureRIDs(string backend)
    {
        using var red = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); red.Fill(Colors.Red);
        using var blue = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); blue.Fill(Colors.Blue);
        using var green = Image.CreateEmpty(3, 2, false, Image.Format.Rgba8); green.Fill(Colors.Green);
        using var mipmaps = Image.CreateEmpty(2, 2, true, Image.Format.Rgba8);
        using var otherFormat = Image.CreateEmpty(2, 2, false, Image.Format.Rgb8);
        using var emptyImage = new Image();
        using var resource = ImageTexture.CreateFromImage(red);
        using var patterned = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); patterned.Fill(Colors.Red); patterned.SetPixel(1, 0, Colors.Blue);
        resource.Update(patterned);
        using var emptyResource = new ImageTexture();
        using var atlas = new AtlasTexture { Atlas = resource, Region = new(0, 0, 1, 1) };
        var submissionChecked = false;
        using var submissionTexture = new RIDSubmissionTexture(red, () =>
        {
            Reject<InvalidOperationException>(() => RenderingServer.Instance!.Texture2DCreate(red));
            submissionChecked = true;
        });
        var resourceRID = resource.GetRID(); var atlasRID = atlas.GetRID();
        var window = new Window { Size = new(96, 64), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        RID owned = default, shutdown = default, placeholder = default;
        var phase = 0; var frames = 0; var drawn = 0; var warmedFrames = 0;
        long before = 0, activeBytes = 0, idleBytes = 0;
        RenderingServer? captured = null;
        var node = new CanvasNode
        {
            DrawAction = n =>
            {
                drawn++;
                if (phase < 4)
                {
                    n.DrawTextureRect(owned, new(4, 4, 16, 16), false);
                    n.DrawTextureRectRegion(owned, new(24, 4, 16, 16), new(0, 0, 1, 1));
                }
                n.DrawTexture(resourceRID, new(44, 4));
                n.DrawTextureRect(atlasRID, new(48, 4, 8, 8), false);
                n.DrawTextureRect(placeholder, new(60, 4, 16, 16), false);
                n.DrawTexture(submissionTexture, new(84, 4));
            },
            ReadyAction = n =>
            {
                var tree = n.Tree!; var server = captured = RenderingServer.Instance!;
                server.SetDefaultClearColor(Colors.Black);
                server.FramePreDraw += () =>
                {
                    before = GC.GetAllocatedBytesForCurrentThread();
                    if (phase >= 4)
                    {
                        Check(server.TextureGetFormat(resourceRID) == Image.Format.Rgba8, "Owner-thread texture reads work during frame preparation.");
                        if (warmedFrames < 84) n.QueueRedraw();
                        return;
                    }
                    if (phase == 0)
                    {
                        owned = server.Texture2DCreate(red); shutdown = server.Texture2DCreate(red); placeholder = server.Texture2DPlaceholderCreate();
                        Reject<ArgumentNullException>(() => server.Texture2DCreate(null!));
                        Reject<ArgumentException>(() => server.Texture2DCreate(emptyImage));
                        using var integerImage = Image.CreateEmpty(2, 2, false, Image.Format.R16I);
                        using var compressedImage = Image.CreateFromData(4, 4, false, Image.Format.Dxt1, new byte[8]);
                        Reject<NotSupportedException>(() => server.Texture2DCreate(integerImage));
                        Reject<NotSupportedException>(() => server.Texture2DCreate(compressedImage));
                        using var disposedImage = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); disposedImage.Dispose();
                        Reject<ObjectDisposedException>(() => server.Texture2DCreate(disposedImage));
                        Reject<ArgumentException>(() => server.Texture2DGet(default));
                        using var shape = new CircleShape();
                        Reject<ArgumentException>(() => server.TextureGetFormat(shape.GetRID()));
                        server.TextureSetPath(owned, "diagnostic://pixels");
                        Check(server.TextureGetPath(owned) == "diagnostic://pixels", "Diagnostic path round-trips without loading data.");
                        Reject<ArgumentNullException>(() => server.TextureSetPath(owned, null!));
                        Reject<ArgumentOutOfRangeException>(() => server.TextureSetSizeOverride(owned, 0, 2));
                        Reject<ArgumentOutOfRangeException>(() => server.TextureSetSizeOverride(owned, 2, 16385));
                        server.TextureSetSizeOverride(owned, 16384, 1); server.TextureSetSizeOverride(owned, 2, 2);
                        using var copy = server.Texture2DGet(owned)!; copy.Fill(Colors.Green);
                        using var unchanged = server.Texture2DGet(owned)!;
                        Check(unchanged.GetPixel(0, 0).IsEqualApprox(Colors.Red), "Texture output is an independent copy.");
                        using var atlasPixels = server.Texture2DGet(atlasRID)!;
                        Check(atlasPixels.Size == new Vector2i(2, 2) && server.TextureGetFormat(atlasRID) == Image.Format.Rgba8, "Server queries use atlas backing pixels and format.");
                        using var emptyPixels = server.Texture2DGet(emptyResource.GetRID())!;
                        Check(emptyPixels.Size == new Vector2i(4, 4) && emptyPixels.GetPixel(0, 0) == Colors.Magenta && server.TextureGetFormat(emptyResource.GetRID()) == Image.Format.Rgba8 && emptyResource.GetImage() is null, "An empty resource RID exposes rendering placeholder data without initializing the resource.");
                        Reject<ArgumentException>(() => server.Texture2DUpdate(owned, green));
                        Reject<ArgumentException>(() => server.Texture2DUpdate(owned, mipmaps));
                        Reject<ArgumentException>(() => server.Texture2DUpdate(owned, otherFormat));
                        Reject<ArgumentOutOfRangeException>(() => server.Texture2DUpdate(owned, blue, 1));
                        Reject<InvalidOperationException>(() => server.Texture2DUpdate(resourceRID, blue));
                        Reject<InvalidOperationException>(() => server.FreeRID(resourceRID));
                        Reject<InvalidOperationException>(() => server.TextureReplace(owned, resourceRID));
                        Reject<InvalidOperationException>(() => Task.Run(() => server.TextureGetFormat(owned)).GetAwaiter().GetResult());
                        server.TextureReplace(owned, owned);
                        var failureTarget = server.Texture2DCreate(red); var failureSource = server.Texture2DCreate(green);
                        RenderingTextureRegistry.Resolve(failureSource).Disposed += _ => throw new InvalidOperationException("Injected free failure.");
                        Reject<InvalidOperationException>(() => server.TextureReplace(failureTarget, failureSource));
                        Reject<ArgumentException>(() => server.Texture2DGet(failureSource));
                        using var committed = server.Texture2DGet(failureTarget)!;
                        Check(committed.GetPixel(0, 0).IsEqualApprox(Colors.Green), "Replacement commits before disposal callbacks and leaves no consumed RID after failure.");
                        server.FreeRID(failureTarget);
                    }
                    else if (phase == 1)
                    {
                        server.Texture2DUpdate(owned, blue); server.TextureSetSizeOverride(owned, 8, 4);
                        Check(RenderingTextureRegistry.Resolve(owned).GetSize() == new Vector2(8, 4), "Logical size override executes independently of source allocation.");
                        using var pixels = server.Texture2DGet(owned)!; Check(pixels.Size == new Vector2i(2, 2), "Size override does not resample pixels.");
                    }
                    else if (phase == 2)
                    {
                        var replacement = server.Texture2DCreate(green); server.TextureSetPath(replacement, "replacement");
                        server.TextureReplace(owned, replacement);
                        Reject<ArgumentException>(() => server.Texture2DGet(replacement));
                        Check(server.TextureGetPath(owned) == "replacement" && RenderingTextureRegistry.Resolve(owned).GetSize() == new Vector2(3, 2), "Replacement transfers dimensions and metadata, retaining the destination identity.");
                    }
                    else
                    {
                        server.FreeRID(owned);
                        Reject<ArgumentException>(() => server.FreeRID(owned));
                        n.DrawAction = _ => { }; // Keep previously recorded commands to exercise stale-texture replay.
                    }
                };
                server.FramePostDraw += () =>
                {
                    frames++;
                    if (phase >= 4)
                    {
                        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                        if (++warmedFrames is > 20 and <= 84) activeBytes += bytes;
                        if (warmedFrames > 84) idleBytes += bytes;
                        if (warmedFrames == 148) tree.Quit();
                        return;
                    }
                    using var frame = server.Readback();
                    var color = phase switch { 0 => Colors.Red, 1 => Colors.Blue, 2 => Colors.Green, _ => Colors.Black };
                    Pixel(frame, 8, 8, color); Pixel(frame, 28, 8, color); Pixel(frame, 44, 4, Colors.Red); Pixel(frame, 45, 4, Colors.Blue);
                    Pixel(frame, 50, 6, Colors.Red); Pixel(frame, 54, 6, Colors.Blue);
                    Pixel(frame, 61, 5, Colors.Magenta); Pixel(frame, 65, 5, Colors.Black);
                    using var query = server.Texture2DGet(resourceRID); Check(query is not null, "Post-draw reads remain available.");
                    var profile = Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "dummy" ? $"{backend}-dummy" : backend;
                    frame.SavePNG($"/tmp/e2d-texture-rid-{profile}-{phase}.png");
                    if (phase < 3) Check(drawn == 1, "Update and replacement affect retained commands without redraw.");
                    if (++phase == 4)
                    {
                        n.DrawAction = item => { item.DrawTexture(resourceRID, new(44, 4)); item.DrawTextureRect(placeholder, new(60, 4, 16, 16), false); };
                    }
                };
            }
        };
        window.AddChild(node);
        Engine.Instance.Run(window);
        Check(frames == 152 && activeBytes == 0 && idleBytes == 0, $"Texture RID active/idle rendering allocated {activeBytes}/{idleBytes} bytes over 64/64 warmed frames ({backend}).");
        Check(submissionChecked, "Texture mutation during geometry submission is rejected before state changes.");
        Reject<ArgumentException>(() => RenderingTextureRegistry.Resolve(shutdown));
        Reject<ObjectDisposedException>(() => captured!.TextureGetFormat(resourceRID));
        Check(resource.GetRID() == resourceRID, "Borrowed texture RID survives renderer shutdown.");
        resource.Dispose(); Reject<ArgumentException>(() => RenderingTextureRegistry.Resolve(resourceRID));
        VerifyTextureRIDCleanup(backend);
        Console.WriteLine($"Rendering texture RID create/update/replace/placeholder/draw/free/lifetime checks passed ({backend}); active/idle managed bytes: {activeBytes}/{idleBytes}.");
    }

    private static void VerifyTextureRIDCleanup(string backend)
    {
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        RID first = default, failing = default; RenderingServer? server = null;
        var window = new Window { Size = new(32, 32) };
        window.AddChild(new CanvasNode
        {
            ReadyAction = n =>
            {
                server = RenderingServer.Instance!;
                first = server.Texture2DCreate(image); failing = server.Texture2DCreate(image);
                RenderingTextureRegistry.Resolve(failing).Disposed += _ =>
                {
                    Reject<InvalidOperationException>(() => server.Texture2DCreate(image));
                    throw new InvalidOperationException("Injected texture disposal failure.");
                };
                server.FramePostDraw += () => n.Tree!.Quit();
            }
        });
        Reject<AggregateException>(() => Engine.Instance.Run(window));
        Check(RenderingServer.Instance is null && DisplayServer.Instance is null, $"Texture disposal failure still detaches both native services ({backend}).");
        Reject<ArgumentException>(() => RenderingTextureRegistry.Resolve(first));
        Reject<ArgumentException>(() => RenderingTextureRegistry.Resolve(failing));
    }

    private sealed class RIDSubmissionTexture(Image image, Action capture) : Texture
    {
        public override int GetWidth() => image.Width;
        public override int GetHeight() => image.Height;
        public override Image GetImage() { capture(); return TexturePixels.FromImage(image).CopyImage(); }
    }
}
