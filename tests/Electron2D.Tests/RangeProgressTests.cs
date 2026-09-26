using Electron2D;

internal static class RangeProgressTests
{
    internal static void Run()
    {
        var root = new Node(); var a = new TextureProgressBar { Name = "A" }; var b = new TextureProgressBar { Name = "B" }; var c = new TextureProgressBar { Name = "C" };
        root.AddChild(a); root.AddChild(b); root.AddChild(c); using var tree = new SceneTree(root);
        Check(a.MinValue == 0 && a.MaxValue == 100 && a.Step == 1 && a.Value == 0 && a.Page == 0 && a.Ratio == 0 && a.MouseFilter == MouseFilter.Pass, "Progress range defaults.");
        a.Step = .2; a.MinValue = .1; a.Value = .39; Check(Math.Abs(a.Value - .3) < 1e-12, "Decimal snapping uses min offset.");
        a.Step = 0; a.Value = -2; Check(a.Value == .1, "Lower clamp."); a.Page = 10; a.Value = 101; Check(a.Value == 90, "Page reduces upper cap.");
        a.AllowGreater = true; a.Value = 200; Check(a.Value == 200 && a.Ratio == 1, "AllowGreater changes storage but ratio stays clamped.");
        a.AllowLesser = true; a.Value = -2; Check(a.Value == -2 && a.Ratio == 0, "AllowLesser projection.");
        a.Rounded = true; a.Value = -1.5; Check(a.Value == -2, "Integer ties round away from zero.");
        var changed = 0; var values = 0; a.Changed += () => changed++; a.ValueChanged += _ => values++;
        a.Step = 2; Check(changed == 1 && values == 0, "Step change does not resnap current value.");
        a.Share(b); b.Share(c); Check(b.Value == a.Value && c.Value == a.Value, "Shared configuration.");
        a.Unshare(); a.Value = 8; Check(b.Value != a.Value && b.Value == c.Value, "Unshare isolates only one owner.");
        a.Share(b); b.Value = 16; Check(a.Value == b.Value && c.Value != b.Value, "Share moves the target only; old peers remain in their prior group.");
        b.Rounded = false; a.Rounded = true; a.Step = .2; a.MinValue = 0; b.Value = 3.4; Check(a.Value == 3.4, "Rounded policy is local to the writer.");
        values = 0; a.SetValueNoSignal(4.1); Check(values == 0 && a.Value == 4 && b.Value == 4, "Silent shared value uses caller rounding.");
        a.Value = double.NaN; var previous = values; a.Value = double.NaN; Check(double.IsNaN(b.Value) && values == previous, "NaN is retained and repeated signal suppressed.");
        a.AllowGreater = a.AllowLesser = false; a.Page = 0; a.Rounded = false; a.Step = 0; a.MinValue = 1; a.MaxValue = 16; a.ExpEdit = true; a.Ratio = .5;
        Check(a.Value == 4 && Math.Abs(a.Ratio - .5) < 1e-12, "Exponential ratio conversion.");
        a.MinValue = 16; Check(a.MaxValue == 16 && a.Ratio == 1, "Equal bounds ratio one.");
        Reject<ArgumentOutOfRangeException>(() => a.Step = double.NaN); Reject<ArgumentOutOfRangeException>(() => a.FillMode = (TextureProgressFillMode)9);
        a.RadialInitialAngle = -90; a.RadialFillDegrees = 999; Check(a.RadialInitialAngle == 270 && a.RadialFillDegrees == 360, "Radial degree wrap/clamp.");
        Check(Task.Run(() => Capture(() => b.Value = 2)).Result is InvalidOperationException, "Shared writers require owner thread.");
        using var image = Image.CreateEmpty(8, 6, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image);
        a.TextureProgress = texture; a.TextureUnder = texture; Check(a.GetMinimumSize() == new Vector2(8, 6), "Unstretched minimum uses largest texture.");
        a.TextureUnder = null; image.Fill(Colors.White); texture.Update(image); a.NinePatchStretch = true; a.StretchMarginLeft = 2; a.StretchMarginRight = 3;
        Check(a.GetMinimumSize() == new Vector2(5, 0), "Nine-patch minimum is margin sums.");
        texture.Dispose(); Check(a.TextureProgress is null, "Borrowed disposal clears all remaining slots.");
        a.MinValue = 0; a.MaxValue = 10; a.Step = .1; a.Value = 3.3;
        foreach (var child in root.Children) child.Owner = root; using var packed = new PackedScene(); packed.Pack(root); using var copy = packed.Instantiate();
        Check(copy.GetNodeOrNull("A") is TextureProgressBar clone && clone.RadialInitialAngle == 270 && clone.NinePatchStretch && Math.Abs(clone.Value - 3.3) < 1e-12 && clone.Step == .1, "Exact progress class/descriptors pack.");
        VerifyFailureAndWarmSharedChanges();
        Console.WriteLine("Shared range values, policy timing, progress state, resources and allocations passed.");
    }
    private static void VerifyFailureAndWarmSharedChanges()
    {
        var root = new Node(); var a = new TextureProgressBar { Name = "A", Step = .1 }; var b = new TextureProgressBar { Name = "B" }; root.AddChild(a); root.AddChild(b);
        using var tree = new SceneTree(root); a.Share(b); var delivered = 0; b.ValueChanged += _ => delivered++;
        Action<double> failure = _ => throw new ApplicationException("expected"); a.ValueChanged += failure;
        Reject<AggregateException>(() => a.Value = 1); Check(delivered == 1 && a.Value == b.Value, "Failure continues later shared owners with committed value."); a.ValueChanged -= failure;
        Action<double> unlink = _ => b.Unshare(); a.ValueChanged += unlink; a.Value = 1.5; a.ValueChanged -= unlink;
        Check(a.Value == 1.5 && b.Value == 1.5, "Reentrant unshare preserves committed state and suppresses stale target delivery.");
        a.Share(b);
        for (var pass = 0; pass < 64; pass++) a.Value = pass % 2 == 0 ? 2.1 : 3.2;
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var pass = 0; pass < 64; pass++) a.Value = pass % 2 == 0 ? 2.1 : 3.2;
        Check(GC.GetAllocatedBytesForCurrentThread() == before && delivered >= 128, "Warmed decimal snap/shared signal paths allocate zero managed bytes.");
    }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
