using Electron2D;

try { SamplingChecks.Run(args); }
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }

internal static class SamplingChecks
{
    internal static void Run(string[] args)
    {
        Reject<InvalidOperationException>(() => RenderingServer.ViewportSetAnisotropicFilteringLevel(default, AnisotropicFiltering.Disabled));
        if (args.Length == 2 && args[0] == "load")
        {
            using var packed = ResourceLoader.Load<PackedScene>(args[1], ResourceLoader.CacheMode.IgnoreDeep);
            using var restored = (SubViewport)packed.Instantiate();
            Check(restored.AnisotropicFilteringLevel == AnisotropicFiltering.Anisotropy8X, "Fresh-file shared enum schema.");
            Console.WriteLine("Fresh viewport sampling archive passed."); return;
        }
        using (var viewport = new SubViewport())
        {
            Check(viewport.AnisotropicFilteringLevel == AnisotropicFiltering.Anisotropy4X, "Project default.");
            foreach (var invalid in new[] { -1, 5, 99 }) Reject<ArgumentOutOfRangeException>(() => viewport.AnisotropicFilteringLevel = (AnisotropicFiltering)invalid);
            foreach (var level in Enum.GetValues<AnisotropicFiltering>().Where(v => v != AnisotropicFiltering.Max))
            {
                viewport.AnisotropicFilteringLevel = level;
                using var packed = new PackedScene(); packed.Pack(viewport);
                using var restored = (SubViewport)packed.Instantiate();
                Check(restored.AnisotropicFilteringLevel == level, "Typed packed shared enum.");
            }
            viewport.AnisotropicFilteringLevel = AnisotropicFiltering.Anisotropy8X;
            if (args.Length == 2 && args[0] == "save")
            {
                using var packed = new PackedScene(); packed.Pack(viewport);
                ResourceSaver.Save(packed, args[1]);
                Console.WriteLine("Viewport sampling archive saved."); return;
            }
        }
        if (args.Length == 0) { Console.WriteLine("Viewport sampling managed checks passed."); return; }
        ProjectSettings.Set(ProjectSettings.RenderingMethod, args[0]);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Native(args[0] == "compatibility", args.Contains("hdr"), reset: false);
        if (args[0] != "compatibility") Native(false, args.Contains("hdr"), reset: true);
        Check(!RenderingServer.IsAvailable, "Session retired.");
        Console.WriteLine("Viewport sampling native checks passed.");
    }

    private sealed class Stripe(Texture texture) : Entity
    {
        internal Action? DuringDraw;
        internal int Draws;
        protected override void OnDraw()
        { Draws++; DuringDraw?.Invoke(); DrawTextureRect(texture, new(4, 20, 64, 4), false); }
    }

    private static void Native(bool compatibility, bool hdr, bool reset)
    {
        using var image = Image.CreateEmpty(64, 64, false, Image.Format.Rgba8);
        for (var y = 0; y < 64; y++)
            for (var x = 0; x < 64; x++) image.SetPixel(x, y, x % 4 < 2 ? Colors.Red : Colors.Blue);
        image.GenerateMIPMaps();
        using var texture = ImageTexture.CreateFromImage(image);
        using var channel = new CanvasTexture { DiffuseTexture = texture, TextureFilter = TextureFilter.LinearWithMipmapsAnisotropic };
        using var root = new Window { Size = new(96, 64), AnisotropicFilteringLevel = AnisotropicFiltering.Disabled, UseHDR2D = hdr };
        var sub = new SubViewport { Size = new(96, 64), RenderTargetUpdateMode = ViewportUpdateMode.Always, AnisotropicFilteringLevel = AnisotropicFiltering.Disabled, UseHDR2D = hdr };
        root.AddChild(sub);
        var a = new Stripe(texture) { TextureFilter = TextureFilter.LinearWithMipmapsAnisotropic };
        var b = new Stripe(texture) { TextureFilter = TextureFilter.LinearWithMipmapsAnisotropic };
        root.AddChild(a); sub.AddChild(b);
        RID owned = default, canvas = default, item = default, channelItem = default, plain = default;
        RID[] views = new RID[3], textures = new RID[3];
        var contrast = new float[3, 5]; var normal = new float[3];
        var frames = 0; var warm = 0; var draws = 0; long before = 0, active = 0, idle = 0;
        var limit = compatibility || reset ? 1 : 7;
        root.Ready += _ =>
        {
            OS.LowProcessorUsageMode = false;
            RenderingServer.SetDefaultClearColor(Colors.Black);
            views[0] = root.GetViewportRID(); views[1] = sub.GetViewportRID();
            views[2] = owned = RenderingServer.ViewportCreate(); RenderingServer.ViewportSetSize(owned, 96, 64);
            RenderingServer.ViewportSetUpdateMode(owned, ViewportUpdateMode.Always); RenderingServer.ViewportSetActive(owned, true);
            RenderingServer.ViewportSetUseHDR2D(owned, hdr);
            canvas = RenderingServer.CanvasCreate(); item = RenderingServer.CanvasItemCreate(); channelItem = RenderingServer.CanvasItemCreate(); plain = RenderingServer.CanvasItemCreate();
            foreach (var view in views) RenderingServer.ViewportAttachCanvas(view, canvas);
            foreach (var entry in new[] { item, channelItem, plain }) RenderingServer.CanvasItemSetParent(entry, canvas);
            RenderingServer.CanvasItemSetDefaultTextureFilter(item, TextureFilter.LinearWithMipmapsAnisotropic);
            RenderingServer.CanvasItemSetDefaultTextureFilter(channelItem, TextureFilter.Nearest);
            RenderingServer.CanvasItemSetDefaultTextureFilter(plain, TextureFilter.LinearWithMipmaps);
            RenderingServer.CanvasItemAddTextureRect(item, new(4, 4, 64, 4), texture.GetRID());
            RenderingServer.CanvasItemAddTextureRect(item, new(4, 36, 64, 16), texture.GetRID());
            RenderingServer.CanvasItemAddTextureRect(channelItem, new(4, 12, 64, 4), channel.GetRID());
            RenderingServer.CanvasItemAddTextureRect(plain, new(4, 28, 64, 4), texture.GetRID());
            for (var i = 0; i < 3; i++) textures[i] = RenderingServer.ViewportGetTexture(views[i]);
            foreach (var invalid in new[] { -1, 5, 99 }) Reject<ArgumentOutOfRangeException>(() => RenderingServer.ViewportSetAnisotropicFilteringLevel(views[0], (AnisotropicFiltering)invalid));
            Reject<ArgumentException>(() => RenderingServer.ViewportSetAnisotropicFilteringLevel(default, AnisotropicFiltering.Disabled));
            Reject<ArgumentException>(() => RenderingServer.ViewportSetAnisotropicFilteringLevel(canvas, AnisotropicFiltering.Disabled));
            using (var detached = new SubViewport()) Reject<InvalidOperationException>(() => RenderingServer.ViewportSetAnisotropicFilteringLevel(detached.GetViewportRID(), AnisotropicFiltering.Disabled));
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.ViewportSetAnisotropicFilteringLevel(views[0], AnisotropicFiltering.Disabled)).GetAwaiter().GetResult());
            a.DuringDraw = () => Reject<InvalidOperationException>(() => RenderingServer.ViewportSetAnisotropicFilteringLevel(views[0], AnisotropicFiltering.Disabled));
            if (!reset) SetLevels(0);
            RenderingServer.FramePreDraw += Before;
            RenderingServer.FramePostDraw += After;
        };
        void SetLevels(int frame)
        {
            RenderingServer.ViewportSetAnisotropicFilteringLevel(views[0], (AnisotropicFiltering)frame);
            RenderingServer.ViewportSetAnisotropicFilteringLevel(views[1], (AnisotropicFiltering)(4 - frame));
            RenderingServer.ViewportSetAnisotropicFilteringLevel(views[2], (AnisotropicFiltering)((frame + 2) % 5));
        }
        void Before()
        {
            if (frames < limit) return;
            before = GC.GetAllocatedBytesForCurrentThread();
            if (warm < 96) SetLevels(warm % 5);
        }
        void After()
        {
            if (frames >= limit)
            {
                if (warm is >= 32 and < 96) active += GC.GetAllocatedBytesForCurrentThread() - before;
                else if (warm >= 96) idle += GC.GetAllocatedBytesForCurrentThread() - before;
                if (++warm == 32) draws = a.Draws;
                if (warm != 160) return;
                Check(active == 0 && idle == 0, $"64 active/64 idle frames: {active}/{idle} owner managed bytes.");
                Check(a.Draws == draws, "Sampler changes reuse retained scene commands.");
                RenderingServer.FreeRID(owned);
                Reject<ArgumentException>(() => RenderingServer.ViewportSetAnisotropicFilteringLevel(owned, AnisotropicFiltering.Disabled));
                RenderingServer.FreeRID(item); RenderingServer.FreeRID(channelItem); RenderingServer.FreeRID(plain); RenderingServer.FreeRID(canvas);
                root.Tree!.Quit(); return;
            }
            var frame = frames++;
            for (var i = 0; i < 3; i++)
            {
                using var pixels = RenderingServer.Texture2DGet(textures[i])!;
                var value = Contrast(pixels, 5); var wrapped = Contrast(pixels, 13);
                Check(MathF.Abs(value - wrapped) < .2f, $"CanvasTexture sampler uses viewport limit {i}/{frame}: {value}/{wrapped}.");
                if (i < 2) Check(MathF.Abs(value - Contrast(pixels, 21)) < .2f, "Scene and server items share viewport sampler.");
                if (reset)
                {
                    var moderate = Contrast(pixels, 41);
                    Check(i == 2 ? moderate > 5 : moderate < 2, $"Fresh session limits: viewport={i}, contrast={value}, root={root.AnisotropicFilteringLevel}, sub={sub.AnisotropicFilteringLevel}, project={ProjectSettings.GetWithOverride(ProjectSettings.AnisotropicFilteringLevel)}.");
                    continue;
                }
                if (frame < 5)
                {
                    var level = i == 0 ? frame : i == 1 ? 4 - frame : (frame + 2) % 5;
                    contrast[i, level] = value;
                    if (frame == 0) normal[i] = Contrast(pixels, 29);
                    else Check(MathF.Abs(Contrast(pixels, 29) - normal[i]) < .2f, "Ordinary mip filtering ignores anisotropy limit.");
                }
                if (frame == 5 && i == 0) Check(MathF.Abs(value - contrast[0, 0]) < .2f, "Equal scene assignment republishes Disabled.");
                if (frame == 6 && i == 0) Check(value > contrast[0, 0] + 5, "Changed authored level republishes sixteen samples.");
            }
            Check(root.AnisotropicFilteringLevel == (frame == 6 ? AnisotropicFiltering.Anisotropy16X : AnisotropicFiltering.Disabled) && sub.AnisotropicFilteringLevel == AnisotropicFiltering.Disabled, "Overrides preserve authored getters.");
            if (reset) { draws = a.Draws; return; }
            if (frame < 4) { SetLevels(frame + 1); return; }
            if (frame == 4)
            {
                for (var i = 0; i < 3; i++)
                {
                    Console.WriteLine($"Viewport {i} stripe contrast: {contrast[i, 0]} -> {contrast[i, 4]}.");
                    Check(contrast[i, 4] > contrast[i, 0] + 5, $"Real anisotropic stripe contrast viewport {i}: {contrast[i, 0]} -> {contrast[i, 4]}.");
                    for (var j = 1; j < 5; j++) Check(contrast[i, j] + .2f >= contrast[i, j - 1], "Increasing limits preserve contrast.");
                }
                using var packed = new PackedScene(); packed.Pack(sub);
                using var restored = (SubViewport)packed.Instantiate();
                Check(restored.AnisotropicFilteringLevel == AnisotropicFiltering.Disabled, "Packed values exclude renderer override.");
                root.AnisotropicFilteringLevel = AnisotropicFiltering.Disabled;
            }
            if (frame == 5) root.AnisotropicFilteringLevel = AnisotropicFiltering.Anisotropy16X;
            draws = a.Draws;
        }
        if (compatibility) Reject<NotSupportedException>(() => Engine.Run(root)); else Engine.Run(root);
        Check(root.IsDisposed && !RenderingServer.IsAvailable, "Host cleanup.");
        Check(compatibility ? frames == 0 : frames == limit && warm == 160, "Expected complete frames or explicit compatibility failure.");
        Console.WriteLine($"Viewport sampling: reset={reset}, HDR={hdr}, compatibility={compatibility}, frames={frames}, measured={active}/{idle}.");
    }
    private static float Contrast(Image pixels, int y)
    { var contrast = 0f; for (var x = 8; x < 60; x++) contrast += MathF.Abs(pixels.GetPixel(x, y).R - pixels.GetPixel(x, y).B); return contrast; }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
