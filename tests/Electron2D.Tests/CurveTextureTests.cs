using Electron2D;

internal static class CurveTextureTests
{
    internal static void Run()
    {
        Scalar(); ChannelsAndCopies(); FailuresAndConcurrency();
        Console.WriteLine("Curve texture resources, copies and concurrency checks passed.");
    }

    private static void Scalar()
    {
        using var t = new CurveTexture(); var changes = 0; t.Changed += _ => changes++;
        Check(t.Width == 256 && t.GetWidth() == 256 && t.GetHeight() == 1 && t.GetSize() == new Vector2(256, 1) &&
            t.Curve is null && t.TextureMode == CurveTexture.TextureModeEnum.RGB && t.GetImage() is null && !t.ResourceLocalToScene, "Scalar defaults.");
        t.Width = 256; t.Curve = null; t.TextureMode = CurveTexture.TextureModeEnum.RGB;
        Check(changes == 0 && t.GetImage() is null, "Equal assignments do not initialize or notify.");
        t.Width = 32; Check(changes == 1, "Width initializes null samples.");
        using (var black = t.GetImage()!) Check(black.PixelFormat == Image.Format.Rgbf && black.GetPixel(31, 0) == new Color(0, 0, 0, 1), "Null RGB channels and format.");
        using var c = new Curve { MinValue = -2, MaxValue = 3 };
        c.AddPoint(new(0, -2), rightMode: Curve.TangentMode.Linear); c.AddPoint(new(1, 3), leftMode: Curve.TangentMode.Linear);
        t.Curve = c;
        using (var image = t.GetImage()!)
        {
            Check(image.Width == 32 && image.Height == 1 && !image.HasMipmaps && image.GetData().Length == 32 * 12, "RGB float layout.");
            for (var i = 0; i < 32; i++) Near(image.GetPixel(i, 0), new Color(-2 + 5 * i / 32f, -2 + 5 * i / 32f, -2 + 5 * i / 32f, 1));
            image.Fill(Colors.White);
        }
        var allocation = t.CapturePixels()!.Allocation;
        c.SetPointValue(1, 4);
        Check(ReferenceEquals(allocation, t.CapturePixels()!.Allocation), "Curve updates retain the allocation.");
        using (var image = t.GetImage()!) Near(image.GetPixel(31, 0).R, c.SampleBaked(31 / 32f));
        t.TextureMode = CurveTexture.TextureModeEnum.Red;
        Check(!ReferenceEquals(allocation, t.CapturePixels()!.Allocation) && !t.HasAlpha && !t.HasMipmaps && t.MipmapCount == 0 && t.IsPixelOpaque(-1, int.MaxValue), "Mode replaces allocation and retains opaque unmipped metadata.");
        using (var image = t.GetImage()!)
        {
            Check(image.PixelFormat == Image.Format.Rf && image.GetData().Length == 32 * 4, "Red-only source storage.");
            Near(image.GetPixel(0, 0), new Color(-2, 0, 0, 1));
        }
        var before = changes; c.BakeResolution = 1; Check(changes == before, "Bake resolution alone does not notify textures.");
        c.EmitChanged(); using (var image = t.GetImage()!) Near(image.GetPixel(0, 0).R, 4);
        t.Width = 4096; using (var image = t.GetImage()!) Check(image.Width == 4096, "Upper width boundary.");
        before = changes;
        foreach (var width in new[] { int.MinValue, 0, 1, 31, 4097, int.MaxValue }) Reject<ArgumentOutOfRangeException>(() => t.Width = width);
        Reject<ArgumentOutOfRangeException>(() => t.TextureMode = (CurveTexture.TextureModeEnum)2);
        Check(changes == before && t.Width == 4096 && t.TextureMode == CurveTexture.TextureModeEnum.Red, "Invalid settings preserve state and event count.");
        var descriptor = t.GetPropertyList().OfType<PropertyDescriptor<CurveTexture, int>>().Single(p => p.Name == "Width");
        descriptor.SetValue(t, 32); Check(t.GetWidth() == 32 && t.GetPropertyList().Count(p => p.Name == "Width") == 1, "One writable width descriptor and polymorphic query.");
        using var domain = new Curve { MinDomain = -1, MaxDomain = 2, MinValue = -1, MaxValue = 2 };
        domain.AddPoint(new(-1, -1), rightMode: Curve.TangentMode.Linear); domain.AddPoint(new(2, 2), leftMode: Curve.TangentMode.Linear);
        t.Curve = domain; using (var image = t.GetImage()!) { Near(image.GetPixel(0, 0).R, 0); Near(image.GetPixel(31, 0).R, 31 / 32f); }
        t.Curve = null; before = changes; c.EmitChanged(); domain.EmitChanged(); Check(changes == before, "Replaced source subscriptions detach.");
        using var copy = (CurveTexture)t.Duplicate(true); using (var copied = copy.GetImage()) Check(copy.Width == 32 && copy.TextureMode == CurveTexture.TextureModeEnum.Red && copied is not null, "Scalar copies retain width, mode and initialized-null state.");
        using var empty = new CurveTexture(); copy.CopyFromResource(empty); Check(copy.Width == 256 && copy.GetImage() is null, "Copying defaults restores uninitialized state.");
    }

    private static void ChannelsAndCopies()
    {
        using var c = Constant(2); using var other = Constant(-1);
        using var t = new CurveXYZTexture(); var changes = 0; t.Changed += _ => changes++;
        Check(t.Width == 256 && t.GetHeight() == 1 && t.CurveX is null && t.CurveY is null && t.CurveZ is null && t.GetImage() is null && !t.ResourceLocalToScene, "XYZ defaults.");
        t.CurveX = c; t.CurveY = other; t.CurveZ = c;
        using (var image = t.GetImage()!) Near(image.GetPixel(100, 0), new Color(2, -1, 2, 1));
        var before = changes; c.SetPointValue(0, 3); Check(changes == before + 1, "Shared channels subscribe once.");
        t.CurveX = null; before = changes; c.SetPointValue(0, 4); Check(changes == before + 1, "Subscription remains until the last channel detaches.");
        t.CurveZ = null; before = changes; c.EmitChanged(); Check(changes == before, "Last channel disconnects.");
        t.CurveX = c; t.CurveZ = c; t.Width = 32;
        var descriptor = t.GetPropertyList().OfType<PropertyDescriptor<CurveXYZTexture, Curve?>>().Single(p => p.Name == "CurveY");
        descriptor.SetValue(t, c); Check(ReferenceEquals(t.CurveY, c), "Typed curve descriptor.");
        using var shallow = (CurveXYZTexture)t.Duplicate(); using var deep = (CurveXYZTexture)t.Duplicate(true);
        Check(shallow.CurveX == c && deep.CurveX != c && deep.CurveX == deep.CurveY && deep.CurveX == deep.CurveZ, "Deep graph aliases and shallow borrowing.");
        using var deepCurve = deep.CurveX!;
        c.SetPointValue(0, 2); using (var image = shallow.GetImage()!) Near(image.GetPixel(0, 0).R, 2);
        using (var image = deep.GetImage()!) Near(image.GetPixel(0, 0).R, 4);
        var copyChanges = 0; deep.Changed += _ => copyChanges++;
        deep.CopyFromResource(t); Check(deep.CurveX == c && deep.Width == 32 && copyChanges == 1, "CopyFromResource shares source curves and width, and emits once.");
        c.ResourcePath = "external-curve"; using var external = (CurveXYZTexture)t.Duplicate(true);
        Check(external.CurveX == c, "Internal copy mode shares external resources."); c.ResourcePath = "";
        using var sprite = new Sprite { Texture = t }; using var scene = new PackedScene();
        scene.Pack(sprite); using var shared = (Sprite)scene.Instantiate(); Check(shared.Texture == t, "Nonlocal texture is borrowed by instances.");
        c.ResourceLocalToScene = t.ResourceLocalToScene = true; scene.Pack(sprite); var local = (Sprite)scene.Instantiate();
        var localTexture = (CurveXYZTexture)local.Texture!; var localCurve = localTexture.CurveX!;
        Check(localTexture != t && localCurve != c && localTexture.CurveY == localCurve && localTexture.CurveZ == localCurve, "Local graph copies preserve aliased channels.");
        local.Dispose(); Check(localTexture.IsDisposed && localCurve.IsDisposed && !t.IsDisposed && !c.IsDisposed, "Scene owns only its local copies.");
        t.Dispose(); shallow.Dispose(); deep.Dispose(); external.Dispose(); before = changes; c.EmitChanged();
        Check(!c.IsDisposed && changes == before, "Texture disposal detaches without owning curves.");
    }

    private static void FailuresAndConcurrency()
    {
        using var c = Constant(1); using var t = new CurveTexture { Curve = c };
        using var dead = new Curve(); dead.Dispose(); Reject<ObjectDisposedException>(() => t.Curve = dead); Check(t.Curve == c, "Dead assignment preserves original source.");
        Action<Resource> fail = _ => throw new ApplicationException("texture observer"); t.Changed += fail;
        Reject<ApplicationException>(() => c.SetPointValue(0, 2)); t.Changed -= fail;
        using (var image = t.GetImage()!) Near(image.GetPixel(0, 0).R, 2);
        using var xyz = new CurveXYZTexture { CurveX = c, CurveY = c, CurveZ = c };
        Task.WaitAll(Task.Run(() => { for (var i = 0; i < 100; i++) c.SetPointValue(0, i % 3 - 1); }),
            Task.Run(() => { for (var i = 0; i < 100; i++) { using var image = xyz.GetImage()!; var first = image.GetPixel(0, 0); for (var x = 0; x < image.Width; x++) Near(image.GetPixel(x, 0), first); } }),
            Task.Run(() => { for (var i = 0; i < 100; i++) { xyz.Width = i % 2 == 0 ? 32 : 64; xyz.CurveZ = c; } }));
        using var reentrant = new CurveTexture { Curve = c }; var notified = false;
        reentrant.Changed += _ => { if (notified) return; notified = true; reentrant.Curve = null; };
        c.SetPointValue(0, .5f); using (var image = reentrant.GetImage()!) Near(image.GetPixel(0, 0).R, 0);
        using var dying = new CurveTexture { Curve = c }; dying.Changed += r => r.Dispose(); c.SetPointValue(0, 1);
        Check(dying.IsDisposed && !c.IsDisposed, "Disposal inside a notification is safe.");
        using var stale = CurveTextureTests.Constant(3); using var recover = new CurveTexture { Curve = stale };
        stale.Dispose(); Reject<ObjectDisposedException>(() => recover.Width = 32);
        Check(recover.Width == 256 && recover.Curve == stale, "Failed rebake preserves prior configuration.");
        using (var last = recover.GetImage()!) Near(last.GetPixel(0, 0).R, 3);
        recover.Curve = null; using (var zero = recover.GetImage()!) Near(zero.GetPixel(0, 0).R, 0);
        t.Dispose(); Reject<ObjectDisposedException>(() => t.Width = 32); Reject<ObjectDisposedException>(() => t.GetHeight()); Reject<ObjectDisposedException>(() => t.GetImage());
    }

    internal static Curve Constant(float value) { var c = new Curve(); c.AddPoint(Vector2.Zero); c.SetPointValue(0, value); return c; }
    private static void Near(Color a, Color b) { Near(a.R, b.R); Near(a.G, b.G); Near(a.B, b.B); Near(a.A, b.A); }
    private static void Near(float a, float b) => Check(Math.Abs(a - b) < .0001f, $"Expected {b}, got {a}.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
