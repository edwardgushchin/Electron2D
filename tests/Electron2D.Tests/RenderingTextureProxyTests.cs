using System.Collections;
using System.Reflection;
using Electron2D;

internal static partial class RenderingRuntimeTests
{
    internal static void VerifyTextureProxyResources()
    {
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(Colors.Red);
        using var resource = ImageTexture.CreateFromImage(image);
        var source = resource.GetRID(); var aliases = new List<(RID RID, ServerTexture Texture)>();
        try
        {
            var target = source;
            for (var i = 0; i < 256; i++)
            {
                var proxy = new ServerTexture(target, RenderingTextureRegistry.Resolve(target));
                var rid = RenderingTextureRegistry.Register(proxy); proxy.Bind(rid); aliases.Add((rid, proxy)); target = rid;
            }
            Check(ReferenceEquals(RenderingTextureRegistry.ResolveProxySource(target), resource), "Deep proxy chains resolve iteratively to their source.");
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 256; i++) RenderingTextureRegistry.ResolveProxySource(target);
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed chained proxy lookup allocates no managed memory.");
            image.Fill(Colors.Blue); resource.Update(image);
            using var copy = aliases[^1].Texture.GetImage()!;
            Check(copy.GetPixel(0, 0) == Colors.Blue, "Proxy snapshots observe live source pixel updates.");
            using var broken = new BrokenProxyTexture();
            Reject<InvalidOperationException>(() => aliases[0].Texture.SetProxyTarget(broken.GetRID(), broken));
            Check(aliases[0].Texture.ProxyTarget == source, "Source callback failure leaves the prior proxy target committed.");
            using var releasing = new ReleasingProxyTexture(() => { aliases[0].Texture.Released = true; aliases[0].Texture.Dispose(); });
            Reject<ObjectDisposedException>(() => aliases[0].Texture.SetProxyTarget(releasing.GetRID(), releasing));
            Check(!aliases[0].Texture.ProxyTarget.IsValid(), "Metadata callbacks cannot resurrect a released destination.");
            resource.Dispose();
            Check(RenderingTextureRegistry.ResolveProxySource(target) is null && aliases[^1].Texture.GetImage() is null, "Source disposal invalidates data without destroying aliases.");
        }
        finally
        {
            foreach (var alias in aliases) { RenderingTextureRegistry.Remove(alias.RID); alias.Texture.Released = true; alias.Texture.Dispose(); }
        }
        Console.WriteLine("Texture proxy deep-chain, source update/disposal, failure rollback and zero-allocation lookup passed.");
    }

    private sealed class BrokenProxyTexture : Texture
    {
        public override int GetWidth() => 2;
        public override int GetHeight() => 2;
        public override Image? GetImage() => throw new InvalidOperationException("Injected proxy source failure.");
    }

    private sealed class ReleasingProxyTexture(Action callback) : Texture
    {
        public override int GetWidth() => 2;
        public override int GetHeight() => 2;
        public override Image GetImage() { callback(); return Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); }
    }

    private static void VerifyTextureProxies(string backend)
    {
        using var red = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); red.Fill(Colors.Red);
        using var blue = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); blue.Fill(Colors.Blue);
        using var green = Image.CreateEmpty(3, 2, false, Image.Format.Rgba8); green.Fill(Colors.Green);
        using var borrowed = ImageTexture.CreateFromImage(red);
        using var empty = new ImageTexture();
        var window = new Window { Size = new(96, 64), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        RID first = default, second = default, proxy = default, nested = default, consumed = default, redirected = default, emptyProxy = default, borrowedProxy = default;
        var phase = 0; var draws = 0; var frames = 0; var warm = 0;
        long before = 0, allocated = 0, idleAllocated = 0;
        IDictionary? nativeCache = null; nint firstAllocation = 0, secondAllocation = 0;
        Texture? firstTexture = null, secondTexture = null;
        RenderingServer? renderer = null;
        var node = new CanvasNode
        {
            DrawAction = n =>
            {
                draws++;
                n.DrawTextureRect(proxy, new(4, 4, 16, 16), false);
                n.DrawTextureRectRegion(nested, new(24, 4, 16, 16), new(0, 0, 1, 1));
                n.DrawTextureRect(redirected, new(44, 4, 16, 16), false);
                n.DrawTextureRect(emptyProxy, new(64, 4, 16, 16), false);
                n.DrawTexture(borrowedProxy, new(84, 4));
                if (phase == 0) { n.DrawTexture(first, new(4, 32)); n.DrawTexture(second, new(8, 32)); }
            },
            ReadyAction = n =>
            {
                var tree = n.Tree!; var server = renderer = RenderingServer.Service!;
                RenderingServer.SetDefaultClearColor(Colors.Black);
                RenderingServer.FramePreDraw += () =>
                {
                    before = GC.GetAllocatedBytesForCurrentThread();
                    if (phase >= 5)
                    {
                        if (warm < 84) RenderingServer.TextureProxyUpdate(proxy, warm % 2 == 0 ? first : second);
                        return;
                    }
                    if (phase == 0)
                    {
                        first = RenderingServer.Texture2DCreate(red); second = RenderingServer.Texture2DCreate(blue);
                        consumed = RenderingServer.Texture2DCreate(green);
                        proxy = RenderingServer.TextureProxyCreate(first); nested = RenderingServer.TextureProxyCreate(proxy);
                        RenderingServer.TextureSetSizeOverride(proxy, 7, 5);
                        Check(RenderingTextureRegistry.Resolve(proxy).GetSize() == new Vector2(7, 5), "Proxy logical-size overrides execute independently of source size.");
                        redirected = RenderingServer.TextureProxyCreate(consumed);
                        emptyProxy = RenderingServer.TextureProxyCreate(empty.GetRID()); borrowedProxy = RenderingServer.TextureProxyCreate(borrowed.GetRID());
                        firstTexture = RenderingTextureRegistry.Resolve(first); secondTexture = RenderingTextureRegistry.Resolve(second);
                        Reject<ArgumentException>(() => RenderingServer.TextureProxyCreate(default));
                        using var shape = new CircleShape(); Reject<ArgumentException>(() => RenderingServer.TextureProxyCreate(shape.GetRID()));
                        Reject<ArgumentException>(() => RenderingServer.TextureProxyUpdate(proxy, default));
                        Reject<InvalidOperationException>(() => RenderingServer.TextureProxyUpdate(first, second));
                        Reject<InvalidOperationException>(() => RenderingServer.TextureProxyUpdate(borrowed.GetRID(), second));
                        Reject<InvalidOperationException>(() => RenderingServer.TextureProxyUpdate(proxy, proxy));
                        Reject<InvalidOperationException>(() => RenderingServer.TextureProxyUpdate(proxy, nested));
                        Reject<InvalidOperationException>(() => RenderingServer.TextureReplace(proxy, first));
                        Reject<InvalidOperationException>(() => RenderingServer.TextureReplace(first, proxy));
                        Reject<InvalidOperationException>(() => RenderingServer.Texture2DUpdate(proxy, blue));
                        Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.TextureProxyUpdate(proxy, second)).GetAwaiter().GetResult());
                        using var pixels = RenderingServer.Texture2DGet(proxy)!; pixels.Fill(Colors.Blue);
                        using var unchanged = RenderingServer.Texture2DGet(first)!;
                        Check(unchanged.GetPixel(0, 0) == Colors.Red, "Proxy image outputs are independent source copies.");
                        var freed = RenderingServer.TextureProxyCreate(first); RenderingServer.FreeRID(freed);
                        Reject<ArgumentException>(() => RenderingServer.Texture2DGet(freed));
                        using var remaining = RenderingServer.Texture2DGet(first);
                        Check(remaining is not null, "Freeing an alias preserves its source.");
                    }
                    else if (phase == 1)
                    {
                        RenderingServer.TextureSetPath(second, "blue"); RenderingServer.TextureSetSizeOverride(second, 8, 4);
                        RenderingServer.TextureProxyUpdate(proxy, second);
                        Check(proxy.GetID() != second.GetID() && RenderingServer.TextureGetPath(proxy) == "blue" && RenderingTextureRegistry.Resolve(proxy).GetSize() == new Vector2(8, 4), "Retarget preserves proxy identity and updates dimensions/path.");
                        RenderingServer.TextureReplace(first, consumed);
                        Reject<ArgumentException>(() => RenderingServer.Texture2DGet(consumed));
                        Check(RenderingServer.Texture2DGet(redirected) is not null, "Replacing a source redirects its aliases before consuming it.");
                    }
                    else if (phase == 2)
                    {
                        borrowed.Dispose();
                        RenderingServer.FreeRID(second);
                        Check(RenderingServer.Texture2DGet(proxy) is null && RenderingServer.Texture2DGet(nested) is null && RenderingServer.Texture2DGet(borrowedProxy) is null, "Freed or disposed sources leave live but empty aliases.");
                    }
                    else if (phase == 3)
                    {
                        RenderingServer.TextureProxyUpdate(proxy, first);
                        RenderingServer.FreeRID(nested);
                        Reject<ArgumentException>(() => RenderingServer.TextureProxyUpdate(nested, first));
                    }
                    else
                    {
                        second = RenderingServer.Texture2DCreate(blue); secondTexture = RenderingTextureRegistry.Resolve(second);
                        n.DrawAction = item => { item.DrawTextureRect(proxy, new(4, 4, 16, 16), false); item.DrawTexture(second, new(8, 32)); };
                        n.QueueRedraw();
                    }
                };
                RenderingServer.FramePostDraw += () =>
                {
                    frames++;
                    if (phase >= 5)
                    {
                        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                        if (++warm is > 20 and <= 84) allocated += bytes;
                        if (warm > 84) idleAllocated += bytes;
                        if (warm == 148)
                        {
                            Check(ProxyNativeHandle(nativeCache!, firstTexture!) == firstAllocation, "GPU source allocation survives active retargets.");
                            Check(ProxyNativeHandle(nativeCache!, secondTexture!) == secondAllocation, "GPU alternate source allocation survives active retargets.");
                            tree.Quit();
                        }
                        return;
                    }
                    using var frame = server.Readback();
                    if (phase == 0)
                    {
                        Pixel(frame, 8, 8, Colors.Red); Pixel(frame, 28, 8, Colors.Red); Pixel(frame, 48, 8, Colors.Green);
                        Pixel(frame, 65, 5, Colors.Magenta); Pixel(frame, 84, 4, Colors.Red);
                    }
                    else if (phase == 1)
                    {
                        Pixel(frame, 8, 8, Colors.Blue); Pixel(frame, 28, 8, Colors.Blue); Pixel(frame, 48, 8, Colors.Green);
                        Check(draws == 1, "Retarget and source replacement update retained proxy commands without rerecording.");
                    }
                    else if (phase == 2)
                    {
                        Pixel(frame, 8, 8, Colors.Black); Pixel(frame, 28, 8, Colors.Black); Pixel(frame, 48, 8, Colors.Green); Pixel(frame, 84, 4, Colors.Black);
                    }
                    else if (phase == 3)
                    {
                        Pixel(frame, 8, 8, Colors.Green); Pixel(frame, 28, 8, Colors.Black); Pixel(frame, 48, 8, Colors.Green);
                    }
                    if (phase < 4)
                    {
                        var profile = Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "dummy" ? $"{backend}-dummy" : backend;
                        frame.SavePNG(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"e2d-texture-proxy-{profile}-{phase}.png"));
                    }
                    if (++phase == 5)
                    {
                        var nativeBackend = typeof(RenderingServer).GetField("_backend", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(server)!;
                        nativeCache = (IDictionary)nativeBackend.GetType().GetField("_textures", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(nativeBackend)!;
                        firstAllocation = ProxyNativeHandle(nativeCache, firstTexture!); secondAllocation = ProxyNativeHandle(nativeCache, secondTexture!);
                        Check(nativeCache.Count == 3, "Proxy/nested aliases share source storage; only two owned sources and one placeholder have allocations.");
                    }
                };
            }
        };
        window.AddChild(node); Engine.Run(window);
        Check(frames == 153 && allocated == 0 && idleAllocated == 0, $"Warmed active proxy retarget/replay/submission allocated {allocated} managed bytes ({backend}).");
        Reject<ArgumentException>(() => RenderingTextureRegistry.Resolve(proxy));
        Reject<InvalidOperationException>(() => RenderingServer.TextureProxyCreate(empty.GetRID()));
        Reject<ObjectDisposedException>(() => renderer!.TextureProxyCreateCore(empty.GetRID()));
        Check(empty.GetRID().IsValid(), "Renderer shutdown preserves borrowed empty resources.");
        Console.WriteLine($"Texture proxy create/retarget/nesting/replacement/free/shared-storage passed ({backend}); {allocated}/{idleAllocated} managed bytes over 64 active/64 retained warmed frames.");
    }

    private static nint ProxyNativeHandle(IDictionary cache, Texture texture)
    {
        var allocation = cache[texture]!;
        return allocation is GPUTexture gpu ? gpu.Handle :
            ((RenderHandle)allocation.GetType().GetField("Item1")!.GetValue(allocation)!).DangerousGetHandle();
    }
}
