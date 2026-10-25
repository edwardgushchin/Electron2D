using System.Diagnostics;
using Electron2D;
using Path = System.IO.Path;
using static TextureArrayTests;

internal static class DrawableTextureTests
{
    private static Shader Program(string name)
    {
        using var input = typeof(DrawableTextureTests).Assembly.GetManifestResourceStream("TestShaders." + name + ".spv")!;
        using var bytes = new MemoryStream(); input.CopyTo(bytes); return Shader.CreateFromSPIRV(bytes.ToArray(), Shader.Mode.TextureBlit);
    }
    private static ImageTexture Solid(Color color)
    { using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(color); return ImageTexture.CreateFromImage(image); }
    private static void Pixel(Image image, int x, int y, Color expected, float tolerance = .012f)
    {
        var value = image.GetPixel(x, y);
        Check(MathF.Abs(value.R - expected.R) < tolerance && MathF.Abs(value.G - expected.G) < tolerance && MathF.Abs(value.B - expected.B) < tolerance && MathF.Abs(value.A - expected.A) < tolerance,
            $"Drawable pixel {x},{y}: {value}, expected {expected}.");
    }
    private static void Pixel(DrawableTexture texture, Color expected)
    { using var image = texture.GetImage()!; Pixel(image, 0, 0, expected); }
    internal static void Run(string profile)
    {
        if (profile == "archive")
        {
            var directory = Environment.GetEnvironmentVariable("ELECTRON2D_DRAWABLE_ARCHIVE")!;
            using var target = ResourceLoader.Load<DrawableTexture>(Path.Combine(directory, "target.e2dres"), ResourceLoader.CacheMode.Ignore)!;
            using var material = ResourceLoader.Load<ShaderMaterial>(Path.Combine(directory, "material.e2dres"), ResourceLoader.CacheMode.Ignore)!;
            using var shader = material.Shader!; using var detail = material.GetShaderParameter("detail")!;
            Check(target.GetSize() == new Vector2(7, 5) && target.PixelFormat == Image.Format.Rgbaf && target.GetUseMIPMaps() && shader.GetMode() == Shader.Mode.TextureBlit && material.GetShaderParameter<Vector4>("gain") == Vector4.One, "Fresh-process setup and compiled-domain material factories.");
            return;
        }
        if (profile == "managed") Managed(); else Native(profile == "compatibility");
        Console.WriteLine("Drawable texture " + profile + " checks passed.");
    }
    private static void Managed()
    {
        using var texture = new DrawableTexture(); var rid = texture.GetRID();
        Check(texture.GetSize() == Vector2.Zero && texture.GetImage() is null && !texture.HasAlpha && !texture.GetUseMIPMaps() && !texture.ResourceLocalToScene, "Empty writable texture defaults.");
        texture.Setup(1, 1, 0, useMIPMaps: true); Check(texture.GetUseMIPMaps() && !texture.HasMIPMaps && texture.MIPMapCount == 0, "A one-pixel image has no lower mip levels despite requesting the full chain.");
        texture.Setup(7, 5, DrawableTexture.DrawableFormat.RGBAH, new(.1f, .2f, .3f, .4f), true);
        Check(texture.GetSize() == new Vector2(7, 5) && texture.PixelFormat == Image.Format.Rgbah && texture.MIPMapCount == 2 && texture.GetRID() == rid, "Authored metadata and stable identity before native startup.");
        Reject<ArgumentOutOfRangeException>(() => texture.Setup(0, 5, 0));
        Reject<ArgumentOutOfRangeException>(() => texture.Setup(1, 16385, 0));
        Reject<ArgumentOutOfRangeException>(() => texture.SetFormat((DrawableTexture.DrawableFormat)9));
        Reject<ArgumentException>(() => texture.Setup(1, 1, 0, new(float.NaN, 0, 0)));
        Check(texture.GetSize() == new Vector2(7, 5), "Invalid setup preserves previous configuration.");
        texture.SetUseMIPMaps(false); texture.SetFormat(DrawableTexture.DrawableFormat.RGBAF);
        using var shallow = (DrawableTexture)texture.Duplicate(); using var deep = (DrawableTexture)texture.Duplicate(true);
        Check(shallow.GetSize() == texture.GetSize() && deep.PixelFormat == Image.Format.Rgbaf && shallow.GetRID() != rid && deep.GetRID() != rid, "Duplicates preserve setup with independent storage identity.");
        using var blend = new BlitMaterial { BlendMode = BlitBlendMode.Sub }; using var copy = (BlitMaterial)blend.Duplicate();
        Check(copy.BlendMode == BlitBlendMode.Sub, "Built-in material duplication."); Reject<ArgumentOutOfRangeException>(() => blend.BlendMode = (BlitBlendMode)9);
        var directory = Path.Combine(Path.GetTempPath(), "electron2d-drawable-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "target.e2dres"); ResourceSaver.Save(texture, path);
            using var restored = ResourceLoader.Load<DrawableTexture>(path, ResourceLoader.CacheMode.Ignore)!;
            Check(restored.GetSize() == new Vector2(7, 5) && restored.PixelFormat == Image.Format.Rgbaf && !restored.GetUseMIPMaps(), "Authored setup archive.");
            texture.SetUseMIPMaps(true); ResourceSaver.Save(texture, path);
            texture.ResourceLocalToScene = true;
            using (var root = new Entity()) using (var packed = new PackedScene())
            {
                var first = new Sprite { Name = "first", Texture = texture }; var second = new Sprite { Name = "second", Texture = texture };
                root.AddChild(first); root.AddChild(second); first.Owner = second.Owner = root; packed.Pack(root);
                DrawableTexture localized;
                using (var a = packed.Instantiate()) using (var b = packed.Instantiate())
                {
                    localized = (DrawableTexture)a.GetNode<Sprite>("first").Texture!;
                    Check(ReferenceEquals(localized, a.GetNode<Sprite>("second").Texture) && !ReferenceEquals(localized, b.GetNode<Sprite>("first").Texture) && ReferenceEquals(localized.GetLocalScene(), a), "Scene-local writable setup preserves per-instance aliases.");
                }
                Check(localized.IsDisposed && !texture.IsDisposed, "Scene disposal owns only localized resources.");
            }
            texture.ResourceLocalToScene = false;
            foreach (var name in new[] { "DrawableGLSL", "DrawableHLSL" })
            {
                using var shader = Program(name); using var material = new ShaderMaterial { Shader = shader }; using var detail = Solid(Colors.White);
                Check(shader.GetMode() == Shader.Mode.TextureBlit && shader.GetShaderUniformList().Count == 2, "Blit source samplers are renderer-owned; gain and detail are material parameters.");
                material.SetShaderParameter("gain", new Vector4(1, 1, 1, 1)); material.SetShaderParameter("detail", detail);
                Reject<ArgumentException>(() => material.SetShaderParameter("SOURCE_TEXTURE0", detail));
                Reject<NotSupportedException>(() => Shader.CreateFromSPIRV(shader.GetSPIRV()));
                var materialPath = Path.Combine(directory, "material.e2dres"); ResourceSaver.Save(material, materialPath); using var restoredMaterial = ResourceLoader.Load<ShaderMaterial>(materialPath, ResourceLoader.CacheMode.Ignore)!;
                using var restoredShader = restoredMaterial.Shader!; using var restoredDetail = restoredMaterial.GetShaderParameter("detail")!;
                Check(restoredShader.GetMode() == Shader.Mode.TextureBlit && restoredMaterial.GetShaderParameter<Vector4>("gain") == Vector4.One, "Blit program domain and typed values survive archive.");
                using var duplicate = (Shader)shader.Duplicate(); Check(duplicate.GetMode() == Shader.Mode.TextureBlit, "Shader duplication preserves domain.");
                Reject<NotSupportedException>(() => material.RequireCanvasState());
            }
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(DrawableTextureTests).Assembly.Location); start.Environment["ELECTRON2D_TEST_DRAWABLE"] = "archive"; start.Environment["ELECTRON2D_DRAWABLE_ARCHIVE"] = directory;
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEnd(); var error = child.StandardError.ReadToEnd();
            Check(child.WaitForExit(30000) && child.ExitCode == 0, "Fresh archive process: " + output + error);
        }
        finally { Directory.Delete(directory, true); }
        texture.Dispose(); Reject<ObjectDisposedException>(() => texture.GetWidth()); Reject<ObjectDisposedException>(() => texture.GetUseMIPMaps());
    }
    private sealed class Probe(Texture texture) : Entity
    {
        internal int Draws;
        protected override void OnDraw() { Draws++; DrawTextureRect(texture, new(8, 8, 32, 32), false); }
    }
    private sealed class ReentrantTexture(Texture source, Action callback) : Texture
    {
        public override int GetWidth() => 2;
        public override int GetHeight() => 2;
        public override RID GetRID() { callback(); return source.GetRID(); }
    }
    private static void Native(bool compatibility)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, compatibility ? "compatibility" : "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var texture = new DrawableTexture(); texture.Setup(8, 8, 0, Colors.Red);
        using var lazy = new DrawableTexture(); lazy.Setup(8, 8, 0, Colors.Green);
        using var root = new Window { Size = new(128, 64), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var probe = new Probe(texture); root.AddChild(probe); root.AddChild(new Probe(lazy) { Position = new(88, 0) });
        if (compatibility) { Reject<NotSupportedException>(() => Engine.Run(root)); Check(root.IsDisposed && !texture.IsDisposed, "Unsupported sampling tears down only the host."); return; }
        using var red = Solid(Colors.Red); using var blue = Solid(Colors.Blue); using var green = Solid(Colors.Green); using var white = Solid(Colors.White);
        using var replace = new BlitMaterial { BlendMode = BlitBlendMode.Disabled };
        RID owned = default, canvas = default; var frame = 0; long before = 0, active = 0, idle = 0; var retained = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); Check(RenderingServer.GetRenderingDevice() is not null, "Actual GPU renderer.");
            using (var initial = texture.GetImage()) Check(initial is not null, "Offline setup initializes in the active session.");
            foreach (var format in Enum.GetValues<DrawableTexture.DrawableFormat>())
            {
                using var target = new DrawableTexture(); target.Setup(4, 4, format, new(.25f, .5f, .75f, 1), true);
                if (format is DrawableTexture.DrawableFormat.RGBAH or DrawableTexture.DrawableFormat.RGBAF)
                {
                    target.BlitRect(new(0, 0, 4, 4), null, new(2, -.5f, .25f, 1), material: replace); Pixel(target, new(2, -.5f, .25f, 1));
                }
                if (format == DrawableTexture.DrawableFormat.RGBA8SRGB)
                {
                    target.BlitRect(new(0, 0, 4, 4), null, new(.25f, .5f, .75f, 1), material: replace); Pixel(target, new(.537099f, .735357f, .880825f, 1));
                }
                target.BlitRect(new(0, 0, 4, 4), red, material: replace); Pixel(target, Colors.Red);
                target.BlitRect(new(0, 0, 2, 2), blue, mipmap: 1, material: replace);
                using var image = target.GetImage()!; Check(image.MIPMapCount == 2, "Readback retains mip chain.");
                var bytes = image.GetData(); var offset = image.GetMIPMapOffset(1); var length = image.GetMIPMapOffset(2) - offset;
                using var mip = Image.CreateFromData(2, 2, false, image.PixelFormat, bytes.AsSpan(offset, length).ToArray()); Pixel(mip, 0, 0, Colors.Blue);
                target.GenerateMIPMaps(); using var generated = target.GetImage()!; var last = generated.GetData()[generated.GetMIPMapOffset(2)..];
                using var lastMip = Image.CreateFromData(1, 1, false, generated.PixelFormat, last); Pixel(lastMip, 0, 0, Colors.Red);
                target.SetUseMIPMaps(false); Check(!target.HasMIPMaps, "Mip setter reallocates actual storage.");
            }
            texture.BlitRect(new(-2, -2, 4, 4), blue, material: replace);
            using (var clipped = texture.GetImage()!) { Pixel(clipped, 0, 0, Colors.Blue); Pixel(clipped, 3, 3, Colors.Red); }
            using var b = new DrawableTexture(); using var c = new DrawableTexture(); using var d = new DrawableTexture();
            foreach (var target in new[] { b, c, d }) target.Setup(8, 8, 0, Colors.Black);
            texture.BlitRectMulti(new(0, 0, 8, 8), [red, green], [b], material: replace); Pixel(b, Colors.Green);
            texture.BlitRectMulti(new(0, 0, 8, 8), [red, green, blue], [b, c], material: replace); Pixel(c, Colors.Blue);
            texture.BlitRectMulti(new(0, 0, 8, 8), [red, green, blue], [b, c, d], material: replace);
            Pixel(texture, Colors.Red); Pixel(b, Colors.Green); Pixel(c, Colors.Blue); Pixel(d, Colors.White);
            Reject<ArgumentException>(() => texture.BlitRect(new(0, 0, 8, 8), texture));
            var exported = RenderingServer.TextureGetRDTexture(texture.GetRID());
            RenderingServer.GetRenderingDevice()!.FreeRID(exported); Pixel(texture, Colors.Red);
            using var proxyTexture = new TextureRD { TextureRDRID = RenderingServer.TextureGetRDTexture(texture.GetRID()) };
            Reject<ArgumentException>(() => texture.BlitRect(new(0, 0, 8, 8), proxyTexture));
            using (var atlas = new AtlasTexture { Atlas = red }) { texture.BlitRect(new(0, 0, 8, 8), atlas, material: replace); Pixel(texture, Colors.White); }
            texture.BlitRect(new(0, 0, 8, 8), red, material: replace);
            var proxy = RenderingServer.TextureProxyCreate(texture.GetRID());
            Reject<ArgumentException>(() => RenderingServer.TextureDrawableBlitRect([texture.GetRID()], new(0, 0, 8, 8), default, Colors.White, [proxy])); RenderingServer.FreeRID(proxy);
            Reject<ArgumentException>(() => texture.BlitRectMulti(new(0, 0, 8, 8), [red], [texture]));
            Reject<ArgumentOutOfRangeException>(() => texture.BlitRect(new(0, 0, 8, 8), red, mipmap: 1));
            using var reentrant = new ReentrantTexture(red, () => texture.Setup(2, 2, 0));
            Reject<InvalidOperationException>(() => texture.BlitRect(new(0, 0, 8, 8), reentrant)); Pixel(texture, Colors.Red);
            foreach (var name in new[] { "DrawableGLSL", "DrawableHLSL" })
            {
                using var shader = Program(name); using var material = new ShaderMaterial { Shader = shader };
                material.SetShaderParameter("gain", Vector4.One); shader.SetDefaultTextureParameter("detail", white);
                texture.BlitRectMulti(new(0, 0, 8, 8), [blue, green], [b], material: material); Pixel(texture, Colors.Blue); Pixel(b, Colors.Green);
                Reject<ArgumentException>(() => texture.BlitRect(new(0, 0, 8, 8), red, material: material));
                using (var atlas = new AtlasTexture { Atlas = blue })
                {
                    material.SetShaderParameter("detail", atlas); texture.BlitRectMulti(new(0, 0, 8, 8), [red, white], [b], material: material); Pixel(b, Colors.Blue);
                    atlas.Atlas = null;
                    Reject<InvalidOperationException>(() => texture.BlitRectMulti(new(0, 0, 8, 8), [red, white], [b], material: material)); Pixel(b, Colors.Blue);
                    using var channels = new CanvasTexture { DiffuseTexture = atlas };
                    material.SetShaderParameter("detail", channels);
                    Reject<InvalidOperationException>(() => texture.BlitRectMulti(new(0, 0, 8, 8), [red, white], [b], material: material)); Pixel(b, Colors.Blue);
                }
                material.SetShaderParameter("detail", white);
                using (var channels = new CanvasTexture { DiffuseTexture = texture })
                    Reject<ArgumentException>(() => texture.BlitRectMulti(new(0, 0, 8, 8), [channels, white], [b], material: material));
                material.SetShaderParameter("detail", texture); Reject<ArgumentException>(() => texture.BlitRectMulti(new(0, 0, 8, 8), [red, green], [b], material: material));
            }
            using (var description = new RDTextureFormat { Width = 2, Height = 2, Format = RenderingDevice.DataFormat.R8G8B8A8UINT, UsageBits = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CanCopyFrom | RenderingDevice.TextureUsageBits.CanCopyTo })
            using (var view = new RDTextureView())
            {
                var device = RenderingServer.GetRenderingDevice()!; var integerImage = device.TextureCreate(description, view);
                using var source = new TextureRD { TextureRDRID = integerImage };
                Reject<ArgumentException>(() => texture.BlitRect(new(0, 0, 8, 8), source)); device.FreeRID(integerImage);
            }
            b.CopyFromResource(texture); Pixel(b, Colors.Red); b.BlitRect(new(0, 0, 8, 8), green, material: replace); b.ResetState(); Pixel(b, Colors.Red);
            using (var pattern = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8))
            {
                pattern.SetPixel(0, 0, Colors.Red); pattern.SetPixel(1, 0, Colors.Green); pattern.SetPixel(0, 1, Colors.Blue); pattern.SetPixel(1, 1, Colors.White);
                using var source = ImageTexture.CreateFromImage(pattern); texture.BlitRect(new(8, 8, -8, -8), source, material: replace);
                using var flipped = texture.GetImage()!; Pixel(flipped, 0, 0, Colors.White); Pixel(flipped, 7, 7, Colors.Red);
            }
            foreach (var mode in Enum.GetValues<BlitBlendMode>())
            {
                using var target = new DrawableTexture(); target.Setup(2, 2, DrawableTexture.DrawableFormat.RGBAF, new(.4f, .4f, .4f, .5f));
                using var material = new BlitMaterial { BlendMode = mode };
                target.BlitRect(new(0, 0, 2, 2), null, new(.2f, .2f, .2f, .5f), material: material);
                var expected = mode switch { BlitBlendMode.Mix => new Color(.3f, .3f, .3f, .75f), BlitBlendMode.Add => new(.5f, .5f, .5f, .75f), BlitBlendMode.Sub => new(.3f, .3f, .3f, .25f), BlitBlendMode.Mul => new(.08f, .08f, .08f, .25f), _ => new(.2f, .2f, .2f, .5f) };
                Pixel(target, expected);
            }
            using (var callbackTarget = new DrawableTexture())
            {
                callbackTarget.Setup(2, 2, 0, Colors.Blue); var prior = RenderingServer.TextureGetRDTexture(callbackTarget.GetRID()); var called = false;
                var device = RenderingServer.GetRenderingDevice()!;
                device.TextureGetDataAsync(prior, 0, _ => { called = true; Reject<InvalidOperationException>(() => callbackTarget.Setup(1, 1, 0)); });
                callbackTarget.Setup(4, 4, 0, Colors.Red); Check(called, "Storage replacement delivers completed callbacks with mutation guarded.");
                Pixel(callbackTarget, Colors.Red); Check(device.CanvasInfo(prior) is null, "Replacement expires exported allocation identity."); device.FreeRID(prior);
            }
            var failedTarget = RenderingServer.TextureDrawableCreate(2, 2, 0, Colors.Blue); var intactSource = RenderingServer.TextureDrawableCreate(2, 2, 0, Colors.Green);
            var failedExport = RenderingServer.TextureGetRDTexture(failedTarget);
            RenderingServer.GetRenderingDevice()!.TextureGetDataAsync(failedExport, 0, _ => throw new ApplicationException("readback fixture"));
            Reject<AggregateException>(() => RenderingServer.TextureReplace(failedTarget, intactSource));
            using (var image = RenderingServer.Texture2DGet(intactSource)!) Pixel(image, 0, 0, Colors.Green);
            using (var image = RenderingServer.Texture2DGet(failedTarget)!) Pixel(image, 0, 0, Colors.Blue);
            RenderingServer.GetRenderingDevice()!.FreeRID(failedExport); RenderingServer.FreeRID(failedTarget); RenderingServer.FreeRID(intactSource);
            owned = RenderingServer.TextureDrawableCreate(8, 8, 0, Colors.Blue); var replacement = RenderingServer.TextureDrawableCreate(8, 8, 0, Colors.Green);
            var ownedResource = (DrawableTexture)RenderingTextureRegistry.Resolve(owned);
            using (var reentrantFree = new ReentrantTexture(red, () => RenderingServer.FreeRID(owned)))
                Reject<InvalidOperationException>(() => ownedResource.BlitRect(new(0, 0, 8, 8), reentrantFree));
            using (var unchanged = RenderingServer.Texture2DGet(owned)!) Pixel(unchanged, 0, 0, Colors.Blue);
            RenderingServer.TextureReplace(owned, replacement); using (var image = RenderingServer.Texture2DGet(owned)!) Pixel(image, 0, 0, Colors.Green);
            var defaultMaterial = RenderingServer.TextureDrawableGetDefaultMaterial();
            Check(defaultMaterial == RenderingServer.TextureDrawableGetDefaultMaterial(), "Retained default material identity.");
            RenderingServer.FreeRID(defaultMaterial); Check(defaultMaterial != RenderingServer.TextureDrawableGetDefaultMaterial(), "Freed default material is recreated.");
            RenderingServer.TextureSetSizeOverride(owned, 16, 16); RenderingServer.TextureSetPath(owned, "drawable/owned");
            Check(RenderingServer.TextureGetPath(owned) == "drawable/owned" && RenderingServer.TextureGetFormat(owned) == Image.Format.Rgba8, "Owned writable path/format metadata.");
            RenderingServer.TextureDrawableBlitRect([owned], new(0, 0, 8, 8), replace.GetRID(), Colors.White, [blue.GetRID()]);
            canvas = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(canvas, probe.GetCanvas()); RenderingServer.CanvasItemAddTextureRect(canvas, new(56, 8, 32, 32), owned);
            texture.BlitRect(new(0, 0, 8, 8), blue, material: replace);
            root.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frame is >= 24 and < 88) texture.BlitRect(new(0, 0, 8, 8), frame % 2 == 0 ? red : blue, material: replace);
            };
            RenderingServer.FramePostDraw += () =>
            {
                var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                if (frame is >= 24 and < 88) active += allocated; if (frame is >= 88 and < 152) idle += allocated;
                if (frame == 88) retained = probe.Draws;
                if (frame is 0 or 151)
                {
                    using var capture = RenderingServer.Service!.Readback(); Pixel(capture, 12, 12, Colors.Blue); Pixel(capture, 60, 12, Colors.Blue); Pixel(capture, 100, 12, Colors.Green);
                    if (Environment.GetEnvironmentVariable("ELECTRON2D_DRAWABLE_CAPTURE") is { } path) capture.SavePNG(path + "." + frame + ".png");
                }
                if (++frame == 152)
                {
                    Check(active == 0 && idle == 0 && probe.Draws == retained, $"Prepared writable frames allocate {active}/{idle} bytes and retain geometry.");
                    RenderingServer.FreeRID(canvas); RenderingServer.FreeRID(owned); root.Tree!.Quit();
                }
            };
        };
        Engine.Run(root); Check(root.IsDisposed && !texture.IsDisposed, "Shutdown retires owned native storage and preserves the borrowed authoring resource.");
        using var second = new Window { Size = new(64, 64) }; second.AddChild(new Probe(texture));
        second.Ready += _ => RenderingServer.FramePostDraw += () => { Pixel(texture, Colors.Red); second.Tree!.Quit(); };
        Engine.Run(second); Check(second.IsDisposed && texture.GetImage() is null, "A later session recreates authored clear storage; no native readback survives shutdown.");
    }
}
