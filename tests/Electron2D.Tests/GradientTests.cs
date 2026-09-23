using Electron2D;

internal static class GradientTests
{
    internal static void Run()
    {
        PointsAndSampling(); Textures(); CopiesAndFailures();
        Console.WriteLine("Gradient interpolation, texture fills, copies and concurrent baking checks passed.");
    }

    private static void PointsAndSampling()
    {
        using var g = new Gradient(); var events = new List<string>();
        g.Changed += _ => events.Add("change"); g.PropertyListChanged += _ => events.Add("list");
        Check(g.GetPointCount() == 2 && g.Offsets.SequenceEqual(new float[] { 0, 1 }) && g.Colors.SequenceEqual(new[] { Colors.Black, Colors.White }), "Default points.");
        Near(g.Sample(.5f), new(.5f, .5f, .5f, 1));
        g.InterpolationColorSpace = Gradient.ColorSpace.LinearSRGB; Near(g.Sample(.5f), new(.73535698f, .73535698f, .73535698f, 1));
        g.InterpolationColorSpace = Gradient.ColorSpace.OKLAB; Near(g.Sample(.5f), new(.38857286f, .38857286f, .38857286f, 1));
        g.Colors = [new(1, 0, 0, .2f), new(0, 0, 1, .8f)]; Near(g.Sample(.5f), new(.550441f, .325621f, .636501f, .5f));
        Check(g.Sample(0) == g.Colors[0] && g.Sample(1) == g.Colors[1], "Exact endpoints avoid conversion drift.");
        events.Clear(); g.InterpolationMode = Gradient.InterpolationModeEnum.Constant;
        Check(events.SequenceEqual(new[] { "change", "list" }), "Mode event order.");
        events.Clear(); g.InterpolationMode = g.InterpolationMode; g.InterpolationColorSpace = g.InterpolationColorSpace;
        Check(events.Count == 0 && g.Sample(.999f) == g.Colors[0] && g.Sample(1) == g.Colors[1], "Constant boundary and silent enum equality.");
        g.InterpolationColorSpace = Gradient.ColorSpace.SRGB; g.InterpolationMode = Gradient.InterpolationModeEnum.Cubic;
        g.Offsets = [0, .5f, 1]; g.Colors = [Colors.Black, Colors.White, Colors.Black]; Near(g.Sample(.25f), new(.5625f, .5625f, .5625f, 1));
        g.InterpolationMode = Gradient.InterpolationModeEnum.Linear;
        g.Offsets = [1, -1]; g.Colors = [Colors.Red, Colors.Blue];
        Check(g.Offsets.SequenceEqual(new float[] { 1, -1 }), "Bulk queries keep insertion order.");
        g.RemovePoint(0); Check(g.GetPointCount() == 1 && g.Sample(2) == Colors.Blue, "Removal uses unsorted storage.");
        Reject<InvalidOperationException>(() => g.RemovePoint(0));
        g.Colors = []; Check(g.GetPointCount() == 0 && g.Sample(0) == Colors.Black, "Bulk clear and empty sample.");
        g.AddPoint(2, Colors.Red); g.AddPoint(-2, Colors.Blue);
        Near(g.Sample(1), new(.75f, 0, .25f, 1)); Check(g.GetOffset(0) == -2, "Offsets outside the unit domain are not clamped.");
        g.SetOffset(0, 3); Check(g.Offsets[0] == 3 && g.GetColor(0) == Colors.Red, "Indexed operations sort lazily.");
        g.SetColor(1, Colors.Green); g.Reverse(); Check(g.Offsets.SequenceEqual(new float[] { -2, -1 }) && g.GetColor(0) == Colors.Green, "Reverse mirrors around 0.5.");
        var input = new float[] { 0, 1, 2 }; g.Offsets = input; input[0] = 99; var output = g.Offsets; output[0] = 88;
        Check(g.GetOffset(0) == 0 && g.GetColor(2) == Colors.Black, "Offsets copy and growth defaults.");
        g.Colors = [Colors.Red, Colors.Blue, Colors.White, Colors.Green]; Check(g.Offsets[3] == 0, "Colors growth initializes zero offset.");
        var before = events.Count; g.SetColor(0, g.GetColor(0)); g.SetOffset(0, g.GetOffset(0)); g.Offsets = g.Offsets; g.Colors = g.Colors;
        Check(events.Count == before + 4, "Equal point and array assignments notify.");
        g.Offsets = [-float.MaxValue, float.MaxValue]; g.Colors = [Colors.Black, Colors.White]; Near(g.Sample(0), new(.5f, .5f, .5f, 1));
        g.Offsets = [0, 0]; Check(g.Sample(0) == g.Colors[0] || g.Sample(0) == g.Colors[1], "Coincident points return a stored color.");
        g.Offsets = [0, 1];
        for (var i = 0; i < 1000; i++) g.Sample(.2f);
        var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1000; i++) g.Sample(.2f);
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Warm samples allocate nothing.");
        before = events.Count;
        Reject<ArgumentException>(() => g.Sample(float.NaN)); Reject<ArgumentException>(() => g.Offsets = [float.PositiveInfinity]);
        Reject<ArgumentException>(() => g.Colors = [new(float.NaN, 0, 0)]); Reject<ArgumentException>(() => g.AddPoint(float.NaN, Colors.White));
        Reject<ArgumentException>(() => g.SetOffset(0, float.NaN)); Reject<ArgumentException>(() => g.SetColor(0, new(float.NaN, 0, 0)));
        Reject<ArgumentOutOfRangeException>(() => g.GetColor(-1)); Reject<ArgumentOutOfRangeException>(() => g.GetOffset(2));
        Reject<ArgumentOutOfRangeException>(() => g.InterpolationMode = (Gradient.InterpolationModeEnum)9);
        Reject<ArgumentOutOfRangeException>(() => g.InterpolationColorSpace = (Gradient.ColorSpace)9);
        Reject<ArgumentNullException>(() => g.Colors = null!); Reject<ArgumentNullException>(() => g.Offsets = null!);
        Check(events.Count == before && g.GetPointCount() == 2, "Rejected edits preserve state and events.");
    }

    private static void Textures()
    {
        using var g = new Gradient(); using var ramp = new GradientRampTexture(); using var t = new GradientTexture();
        Check(ramp.Width == 256 && ramp.GetHeight() == 1 && ramp.Gradient is null && !ramp.UseHDR && ramp.GetImage() is null && ramp.HasAlpha, "Ramp defaults.");
        Check(t.GetSize() == new Vector2(64, 64) && t.Gradient is null && !t.UseHDR && t.Fill == GradientTexture.FillEnum.Linear && t.Repeat == GradientTexture.RepeatEnum.None && t.FillFrom == Vector2.Zero && t.FillTo == new Vector2(1, 0) && t.GetImage() is null && t.HasAlpha, "Fill defaults.");
        var changes = 0; t.Changed += _ => changes++;
        t.Gradient = null; t.UseHDR = false; Check(changes == 0, "Source and HDR equal assignments are silent.");
        t.Width = t.Width; t.Height = t.Height; t.Fill = t.Fill; t.FillFrom = t.FillFrom; t.FillTo = t.FillTo; t.Repeat = t.Repeat;
        Check(changes == 6 && t.GetImage() is null, "All other texture settings notify even equal values.");
        ramp.Gradient = g; ramp.Width = 3;
        using (var image = ramp.GetImage()!) { Check(image.PixelFormat == Image.Format.Rgba8 && !image.HasMipmaps, "Byte storage."); Near(image.GetPixel(0, 0), Colors.Black); Near(image.GetPixel(1, 0), new(128 / 255f, 128 / 255f, 128 / 255f, 1)); Near(image.GetPixel(2, 0), Colors.White); image.Fill(Colors.Red); }
        ramp.Width = 1; using (var image = ramp.GetImage()!) Near(image.GetPixel(0, 0), Colors.Black);
        ramp.Width = 16384; using (var image = ramp.GetImage()!) Near(image.GetPixel(16383, 0), Colors.White);
        t.Gradient = g; t.Width = t.Height = 3; t.UseHDR = true;
        foreach (var fill in Enum.GetValues<GradientTexture.FillEnum>())
        {
            t.Fill = fill; using var image = t.GetImage()!;
            var expected = fill switch { GradientTexture.FillEnum.Linear => .5f, GradientTexture.FillEnum.Radial => System.MathF.Sqrt(.5f), GradientTexture.FillEnum.Square => .5f, _ => .125f };
            Near(image.GetPixel(1, 1), new(expected, expected, expected, 1));
        }
        t.Fill = GradientTexture.FillEnum.Conic; t.FillFrom = new(.5f, .5f); t.FillTo = new(1, .5f);
        using (var image = t.GetImage()!) { Near(image.GetPixel(2, 1).R, 0); Near(image.GetPixel(1, 2).R, .25f); Near(image.GetPixel(0, 1).R, .5f); Near(image.GetPixel(1, 0).R, .75f); }
        t.Fill = GradientTexture.FillEnum.Linear; t.FillFrom = new(.25f, 0); t.FillTo = new(.75f, 0);
        foreach (var repeat in Enum.GetValues<GradientTexture.RepeatEnum>())
        {
            t.Repeat = repeat; using var image = t.GetImage()!;
            Near(image.GetPixel(0, 1).R, repeat == GradientTexture.RepeatEnum.None ? 0 : .5f);
            Near(image.GetPixel(2, 1).R, repeat == GradientTexture.RepeatEnum.None ? 1 : .5f);
        }
        t.FillFrom = Vector2.Zero; t.FillTo = new(1e-11f, 0); using (var image = t.GetImage()!) Near(image.GetPixel(1, 1), Colors.Black);
        t.FillTo = t.FillFrom; using (var image = t.GetImage()!) Near(image.GetPixel(1, 1), Colors.Black);
        t.Repeat = GradientTexture.RepeatEnum.None; t.FillFrom = new(-float.MaxValue, 0); t.FillTo = new(float.MaxValue, 0);
        using (var image = t.GetImage()!) Near(image.GetPixel(1, 1).R, .5f);
        t.Width = t.Height = 1; t.FillFrom = Vector2.Zero; t.FillTo = new(1, 0); using (var image = t.GetImage()!) Near(image.GetPixel(0, 0), Colors.Black);
        var snapshot = t.CapturePixels(); var before = changes;
        g.Colors = [new(2, -1, .5f, .25f)]; g.SetColor(0, new(3, -1, .5f, .25f));
        Check(changes == before, "Source changes invalidate without forwarding Changed.");
        using (var image = t.GetImage()!) { Near(image.GetPixel(0, 0), new(3, -1, .5f, .25f)); Check(image.PixelFormat == Image.Format.Rgbaf, "HDR retains signed channels."); }
        Check(t.CapturePixels() != snapshot && ReferenceEquals(t.CapturePixels(), t.CapturePixels()), "Coalesced baking replaces immutable snapshot once.");
        t.UseHDR = false; g.Colors = [new(.5f, .5f, .5f, 1)];
        using (var image = t.GetImage()!) Near(image.GetPixel(0, 0).R, 127 / 255f);
        using (var image = ramp.GetImage()!) Near(image.GetPixel(0, 0).R, 128 / 255f);
        var old = t.CapturePixels(); t.Gradient = null; t.Width = 7; t.UseHDR = true;
        Check(ReferenceEquals(old, t.CapturePixels()) && t.GetWidth() == 7 && t.PixelFormat == Image.Format.Rgba8, "Null source freezes prior pixels independently of logical settings.");
        using (var image = t.GetImage()!) Check(image.Width == 1, "Old physical size survives source removal.");
        g.Colors = []; t.Gradient = g; using (var image = t.GetImage()!) Near(image.GetPixel(0, 0), Colors.Black);
        foreach (var size in new[] { 0, -1, 16385, int.MaxValue }) { Reject<ArgumentOutOfRangeException>(() => t.Width = size); Reject<ArgumentOutOfRangeException>(() => t.Height = size); Reject<ArgumentOutOfRangeException>(() => ramp.Width = size); }
        Reject<ArgumentOutOfRangeException>(() => t.Fill = (GradientTexture.FillEnum)99); Reject<ArgumentOutOfRangeException>(() => t.Repeat = (GradientTexture.RepeatEnum)99);
        Reject<ArgumentException>(() => t.FillFrom = new(float.NaN, 0)); Reject<ArgumentException>(() => t.FillTo = new(0, float.PositiveInfinity));
        var descriptor = t.GetPropertyList().OfType<PropertyDescriptor<GradientTexture, int>>().Single(p => p.Name == "Width");
        descriptor.SetValue(t, 3); Check(t.GetSize() == new Vector2(3, 1), "Typed descriptors and logical size.");
    }

    private static void CopiesAndFailures()
    {
        using var g = new Gradient { Offsets = [1, 0], Colors = [Colors.Red, Colors.Blue], InterpolationColorSpace = Gradient.ColorSpace.OKLAB };
        using var gc = (Gradient)g.Duplicate(true); Check(gc.Offsets.SequenceEqual(new float[] { 1, 0 }) && gc.InterpolationColorSpace == g.InterpolationColorSpace, "Copies preserve raw storage order and space.");
        using var t = new GradientTexture { Gradient = g, Width = 8, Height = 4, Fill = GradientTexture.FillEnum.Square, Repeat = GradientTexture.RepeatEnum.Mirror, UseHDR = true, FillFrom = new(.1f, .2f), FillTo = new(.8f, .7f) };
        using var shallow = (GradientTexture)t.Duplicate(); using var deep = (GradientTexture)t.Duplicate(true); using var copiedGradient = deep.Gradient!;
        Check(shallow.Gradient == g && deep.Gradient != g && deep.Width == 8 && deep.Height == 4 && deep.Fill == t.Fill && deep.Repeat == t.Repeat && deep.FillFrom == t.FillFrom && deep.FillTo == t.FillTo && deep.UseHDR, "Texture graph policies and full settings.");
        var changes = 0; deep.Changed += _ => changes++; deep.CopyFromResource(t); Check(changes == 1 && deep.Gradient == g, "CopyFromResource replaces subscriptions and emits once.");
        g.Colors = [Colors.Red]; using (var image = deep.GetImage()!) Near(image.GetPixel(0, 0), Colors.Red);
        using var ramp = new GradientRampTexture { Width = 3, Gradient = g, UseHDR = true }; var frozen = ramp.CapturePixels(); ramp.Gradient = null;
        using var frozenCopy = (GradientRampTexture)ramp.Duplicate(true); Check(ReferenceEquals(frozen, frozenCopy.CapturePixels()) && frozenCopy.Gradient is null && frozenCopy.Width == 3, "Null-source duplicate preserves last immutable payload.");
        using var empty = new GradientRampTexture(); frozenCopy.CopyFromResource(empty); Check(frozenCopy.GetImage() is null && frozenCopy.Width == 256 && !frozenCopy.UseHDR, "Copying defaults resets state.");
        using var sprite = new Sprite { Texture = t }; using var scene = new PackedScene(); scene.Pack(sprite);
        using var shared = (Sprite)scene.Instantiate(); Check(shared.Texture == t, "Scene borrows nonlocal texture.");
        g.ResourceLocalToScene = t.ResourceLocalToScene = true; scene.Pack(sprite); var local = (Sprite)scene.Instantiate();
        var lt = (GradientTexture)local.Texture!; var lg = lt.Gradient!; Check(lt != t && lg != g, "Local graph duplicated.");
        local.Dispose(); Check(lt.IsDisposed && lg.IsDisposed && !t.IsDisposed && !g.IsDisposed, "Local ownership.");
        Task.WaitAll(Task.Run(() => { for (var i = 0; i < 100; i++) g.Colors = i % 2 == 0 ? [Colors.Red, Colors.Red] : [Colors.Blue, Colors.Blue]; }),
            Task.Run(() => { for (var i = 0; i < 100; i++) { using var image = t.GetImage()!; var first = image.GetPixel(0, 0); for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++) Near(image.GetPixel(x, y), first); } }));
        using var dead = new Gradient(); dead.Dispose(); Reject<ObjectDisposedException>(() => t.Gradient = dead); Check(t.Gradient == g, "Dead source assignment does not mutate.");
        using var fail = new GradientTexture { Gradient = g }; var pixels = fail.CapturePixels(); g.Dispose(); fail.Width = 2;
        Reject<ObjectDisposedException>(() => fail.GetImage()); fail.Gradient = null; Check(ReferenceEquals(pixels, fail.CapturePixels()), "Failed deferred bake preserves old payload; detaching recovers.");
        using var hugeSource = new Gradient(); using var huge = new GradientTexture { Gradient = hugeSource, UseHDR = true, Width = 1, Height = 1 };
        var previous = huge.CapturePixels(); huge.Width = huge.Height = 16384; Reject<OverflowException>(() => huge.GetImage());
        huge.Gradient = null; Check(ReferenceEquals(previous, huge.CapturePixels()), "Oversized HDR buffer fails checked sizing and preserves pixels.");
        using var observer = new Gradient(); observer.Changed += _ => throw new ApplicationException("observer");
        Reject<ApplicationException>(() => observer.SetColor(0, Colors.Red)); Check(observer.GetColor(0) == Colors.Red, "Callback failure follows commitment.");
        using var dying = new Gradient(); dying.Changed += r => r.Dispose(); dying.InterpolationMode = Gradient.InterpolationModeEnum.Cubic;
        Check(dying.IsDisposed, "Disposal during mode notification.");
        using var dyingTexture = new GradientTexture(); dyingTexture.Changed += r => r.Dispose(); dyingTexture.Width = 2; Check(dyingTexture.IsDisposed, "Disposal during texture notification.");
        t.Dispose(); ramp.Dispose(); Reject<ObjectDisposedException>(() => t.GetImage()); Reject<ObjectDisposedException>(() => ramp.GetHeight()); Reject<ObjectDisposedException>(() => t.Width = 2);
        Reject<ObjectDisposedException>(() => g.Sample(0)); Reject<ObjectDisposedException>(() => g.GetPointCount());
    }

    private static void Near(Color a, Color b) { Near(a.R, b.R); Near(a.G, b.G); Near(a.B, b.B); Near(a.A, b.A); }
    private static void Near(float a, float b) => Check(Math.Abs(a - b) < .0001f, $"Expected {b}, got {a}.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
