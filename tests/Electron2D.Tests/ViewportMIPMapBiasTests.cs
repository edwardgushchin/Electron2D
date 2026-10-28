using Electron2D;

internal static class ViewportMIPMapBiasTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void RunManaged()
    {
        var original = ProjectSettings.Get(ProjectSettings.TextureMIPMapBias);
        using var view = new SubViewport(); Check(view.TextureMIPMapBias == 0, "Default bias.");
        view.TextureMIPMapBias = -.75f;
        foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Reject<ArgumentOutOfRangeException>(() => view.TextureMIPMapBias = invalid);
            Reject<ArgumentException>(() => ProjectSettings.Set(ProjectSettings.TextureMIPMapBias, invalid));
        }
        Check(view.TextureMIPMapBias == -.75f, "Rejected values preserve authored state.");
        using var packed = new PackedScene(); packed.Pack(view); using var copy = (SubViewport)packed.Instantiate();
        Check(copy.TextureMIPMapBias == -.75f, "Packed typed policy.");
        var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron-mip-bias-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try
        {
            ResourceSaver.Save(packed, file); using var loaded = ResourceLoader.Load<PackedScene>(file, cacheMode: ResourceLoader.CacheMode.Ignore);
            using var restored = (SubViewport)loaded!.Instantiate(); Check(restored.TextureMIPMapBias == -.75f, "Real scene file retains bias.");
        }
        finally { File.Delete(file); }
        using var window = new Window();
        ProjectSettings.Set(ProjectSettings.TextureMIPMapBias, 1.25f); window.ApplyProjectMIPMapBias();
        Check(window.TextureMIPMapBias == 1.25f && view.TextureMIPMapBias == -.75f, "Root startup policy does not mutate other viewports.");
        window.TextureMIPMapBias = .5f; ProjectSettings.Set(ProjectSettings.TextureMIPMapBias, -.5f); window.ApplyProjectMIPMapBias();
        Check(window.TextureMIPMapBias == .5f, "Explicit root assignment wins."); ProjectSettings.Set(ProjectSettings.TextureMIPMapBias, original);
        using (var tree = new SceneTree(view)) Reject<InvalidOperationException>(() => Task.Run(() => view.TextureMIPMapBias = 1).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => RenderingServer.ViewportSetTextureMIPMapBias(default, 0));
        view.Dispose(); Reject<ObjectDisposedException>(() => view.TextureMIPMapBias = 0);
        Console.WriteLine("Viewport mipmap bias: typed state, validation, owner and scene files passed.");
    }

    private sealed class Patch(Texture texture) : Entity
    {
        protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 16, 16), false);
    }
    private sealed class MutationProbe(Viewport viewport) : Entity
    {
        protected override void OnDraw()
        {
            Reject<InvalidOperationException>(() => viewport.TextureMIPMapBias = 2);
            Reject<InvalidOperationException>(() => RenderingServer.ViewportSetTextureMIPMapBias(viewport.GetViewportRID(), 2));
        }
    }
    private static ImageTexture Mips()
    {
        var data = new List<byte>(); var colors = new[] { Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow, Colors.Magenta, Colors.White };
        for (int size = 32, mip = 0; size > 0; size /= 2, mip++)
            for (var i = 0; i < size * size; i++) { var c = colors[mip]; data.Add((byte)(c.R * 255)); data.Add((byte)(c.G * 255)); data.Add((byte)(c.B * 255)); data.Add(255); }
        using var image = Image.CreateFromData(32, 32, true, Image.Format.Rgba8, data.ToArray()); return ImageTexture.CreateFromImage(image);
    }
    internal static void RunNative(string profile)
    {
        var compatibility = profile == "compatibility";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, compatibility ? "compatibility" : "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.TextureMIPMapBias, 1);
        using var texture = Mips(); using var root = new Window { Size = new(320, 96) };
        if (compatibility)
        {
            root.AddChild(new Patch(texture)); Reject<NotSupportedException>(() => Engine.Run(root));
            Check(root.IsDisposed && RenderingServer.Service is null, "Unsupported compatibility bias refuses and retires renderer."); return;
        }
        if (profile == "limit")
        {
            root.TextureMIPMapBias = float.MaxValue; root.CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.NearestWithMipmaps;
            root.AddChild(new Patch(texture)); Reject<InvalidOperationException>(() => Engine.Run(root));
            Check(root.IsDisposed && RenderingServer.Service is null, "Native sampler limit failure cleans up."); return;
        }
        var language = profile.Contains("hlsl", StringComparison.Ordinal) ? "HLSL" : "GLSL";
        using var stream = typeof(ViewportMIPMapBiasTests).Assembly.GetManifestResourceStream($"TestShaders.MIPMapBias{language}.frag.spv")!;
        using var bytes = new MemoryStream(); stream.CopyTo(bytes); using var shader = Shader.CreateFromSPIRV(bytes.ToArray());
        shader.SetDefaultTextureParameter("colorMaps", texture, 0); shader.SetDefaultTextureParameter("colorMaps", texture, 1);
        using var ordinary = new RDSamplerState { MaxLOD = 1000 }; using var additive = new RDSamplerState { MaxLOD = 1000, LODBias = 1 };
        shader.SetTextureSamplerState("colorMaps", ordinary, 0); shader.SetTextureSamplerState("colorMaps", additive, 1);
        using var material = new ShaderMaterial { Shader = shader }; material.SetShaderParameter("selected", 0);
        using var second = new ShaderMaterial { Shader = shader }; second.SetShaderParameter("selected", 1);
        root.CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.NearestWithMipmaps;
        var fixedView = new SubViewport { Size = new(96, 32), TextureMIPMapBias = -1, RenderTargetUpdateMode = ViewportUpdateMode.Always };
        root.AddChild(fixedView);
        foreach (var view in new Viewport[] { root, fixedView })
        {
            view.CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.NearestWithMipmaps;
            view.AddChild(new Patch(texture) { Position = new(4, 4) });
            view.AddChild(new Patch(texture) { Position = new(24, 4), Material = material });
            view.AddChild(new Patch(texture) { Position = new(44, 4), Material = second });
            view.AddChild(new Patch(texture) { Position = new(64, 4), TextureFilter = TextureFilter.Nearest });
        }
        var rootTexture = root.GetTexture(); var fixedTexture = fixedView.GetTexture(); var identity = rootTexture.GetRID();
        root.AddChild(new MutationProbe(root));
        RID owned = default, canvas = default, item = default, ownedTexture = default;
        var phase = 0; long before = 0, active = 0, idle = 0; var warm = 0; var previousSize = Vector2.Zero;
        void Pixels(ViewportTexture target, Color first, Color extra)
        {
            using var image = target.GetImage()!; var scale = ReferenceEquals(target, rootTexture) ? root.RenderDensity : 1f;
            foreach (var x in new[] { 12, 32 }) Check(image.GetPixel((int)(x * scale), (int)(12 * scale)).IsEqualApprox(first), $"{language} phase {phase}, x={x}: expected {first}, got {image.GetPixel((int)(x * scale), (int)(12 * scale))}.");
            Check(image.GetPixel((int)(52 * scale), (int)(12 * scale)).IsEqualApprox(extra), "Additive explicit sampler bias.");
            Check(image.GetPixel((int)(72 * scale), (int)(12 * scale)).IsEqualApprox(Colors.Red), "Base-level filtering ignores bias.");
        }
        root.Ready += _ =>
        {
            owned = RenderingServer.ViewportCreate(); RenderingServer.ViewportSetSize(owned, 24, 24);
            RenderingServer.ViewportSetActive(owned, true); RenderingServer.ViewportSetUpdateMode(owned, ViewportUpdateMode.Always);
            RenderingServer.ViewportSetDefaultCanvasItemTextureFilter(owned, TextureFilter.NearestWithMipmaps);
            RenderingServer.ViewportSetTextureMIPMapBias(owned, 1); ownedTexture = RenderingServer.ViewportGetTexture(owned);
            canvas = RenderingServer.CanvasCreate(); item = RenderingServer.CanvasItemCreate();
            RenderingServer.CanvasItemSetParent(item, canvas); RenderingServer.ViewportAttachCanvas(owned, canvas);
            RenderingServer.CanvasItemAddTextureRect(item, new(4, 4, 16, 16), texture.GetRID());
            Reject<ArgumentException>(() => RenderingServer.ViewportSetTextureMIPMapBias(texture.GetRID(), 0));
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.ViewportSetTextureMIPMapBias(root.GetViewportRID(), float.NaN));
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.ViewportSetTextureMIPMapBias(root.GetViewportRID(), 0)).GetAwaiter().GetResult());
            RenderingServer.FramePreDraw += () =>
            {
                if (phase < 5) return;
                before = GC.GetAllocatedBytesForCurrentThread();
                if (warm < 96) RenderingServer.ViewportSetTextureMIPMapBias(root.GetViewportRID(), warm % 2 == 0 ? 0 : 1);
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (phase >= 5)
                {
                    var size = rootTexture.GetSize();
                    if (size != previousSize) { previousSize = size; warm = 0; active = idle = 0; }
                    else if (warm >= 32 && warm < 96) active += GC.GetAllocatedBytesForCurrentThread() - before;
                    else if (warm >= 96) idle += GC.GetAllocatedBytesForCurrentThread() - before;
                    if (++warm == 160)
                    {
                        Check(active == 0 && idle == 0, $"64 active/64 idle prepared bias frames: {active}/{idle} managed bytes.");
                        Console.WriteLine($"{language}: native mips, additive arrays, isolation and 64 active/64 idle frames: {active}/{idle} managed bytes.");
                        RenderingServer.FreeRID(owned); Reject<ArgumentException>(() => RenderingServer.ViewportSetTextureMIPMapBias(owned, 0));
                        RenderingServer.FreeRID(item); RenderingServer.FreeRID(canvas); root.Tree!.Quit();
                    }
                    if (++phase > 2048) throw new InvalidOperationException("Stable bias workload not reached."); return;
                }
                using (var image = RenderingServer.Texture2DGet(ownedTexture)!) Check(image.GetPixel(12, 12).IsEqualApprox(Colors.Blue), "Caller-owned viewport consumes its independent bias.");
                Pixels(fixedTexture, Colors.Red, Colors.Green); Check(rootTexture.GetRID() == identity, "Stable target identity.");
                switch (++phase)
                {
                    case 1:
                        Pixels(rootTexture, Colors.Blue, Colors.Yellow); Check(root.TextureMIPMapBias == 1, "Project startup reaches rendering.");
                        RenderingServer.ViewportSetTextureMIPMapBias(root.GetViewportRID(), -1); break;
                    case 2:
                        Pixels(rootTexture, Colors.Red, Colors.Green); Check(root.TextureMIPMapBias == 1, "Session override preserves authored getter.");
                        root.TextureMIPMapBias = 1; break;
                    case 3:
                        Pixels(rootTexture, Colors.Blue, Colors.Yellow); root.TextureMIPMapBias = 0; break;
                    case 4:
                        Pixels(rootTexture, Colors.Green, Colors.Blue); root.TextureMIPMapBias = .5f; break;
                    case 5:
                        using (var image = rootTexture.GetImage()!) Check(image.GetPixel(12, 12).G > .1f && image.GetPixel(12, 12).B > .1f, "Fractional trilinear bias interpolates levels.");
                        root.TextureMIPMapBias = 0; break;
                }
            };
        };
        Engine.Run(root);
    }
}
