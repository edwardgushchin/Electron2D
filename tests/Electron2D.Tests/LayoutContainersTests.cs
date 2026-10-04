using Electron2D;

internal static class LayoutContainersTests
{
    internal static void Run()
    {
        VerifyMargins();
        VerifyCenter();
        VerifyAspect();
        VerifyPackedState();
        VerifyFailureContinuation();
        VerifyAllocations();
        Console.WriteLine("Margin, center and aspect containers: theme bounds, fitting, RTL, policies and scene state passed.");
    }

    private static void VerifyMargins()
    {
        var theme = ThemeDB.GetDefaultTheme();
        foreach (var side in new[] { "left", "top", "right", "bottom" })
            Check(theme.HasConstant("margin_" + side, "MarginContainer") && theme.GetConstant("margin_" + side, "MarginContainer") == 0,
                "All four default margin constants exist with zero values.");
        var viewport = new TestViewport();
        var margin = new MarginContainer { Name = "Margin", Size = new(100, 80) };
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_top", 5);
        margin.AddThemeConstantOverride("margin_right", 15);
        margin.AddThemeConstantOverride("margin_bottom", 7);
        var child = new Control { Name = "Child", CustomMinimumSize = new(20, 12) };
        margin.AddChild(child); viewport.AddChild(margin);
        using var tree = new SceneTree(viewport);
        tree.FlushDeferred();
        Check(margin.GetMinimumSize() == new Vector2(45, 24) && child.Position == new Vector2(10, 5) && child.Size == new Vector2(75, 68),
            "Margins add to the largest child minimum and reduce the real allocation.");
        Check(margin.GetMarginSize(Side.Right) == 15 && margin.GetMarginSize(Side.Bottom) == 7,
            "The side accessor resolves inherited typed theme constants.");
        margin.AddThemeConstantOverride("margin_left", -4);
        tree.FlushDeferred();
        Check(margin.GetMinimumSize() == new Vector2(31, 24) && child.Position == new Vector2(-4, 5) && child.Size.X == 89,
            "Signed theme changes refresh child placement and minimum size.");
        child.Visible = false;
        tree.FlushDeferred();
        Check(margin.GetMinimumSize() == new Vector2(11, 12), "Locally hidden children stop contributing to minimum size.");
        child.Visible = true;
        child.TopLevel = true;
        tree.FlushDeferred();
        Check(margin.GetMinimumSize() == new Vector2(11, 12), "Top-level controls are not container layout children.");
        child.TopLevel = false;
        margin.CustomMaximumSize = new(60, 50);
        tree.FlushDeferred(); tree.FlushDeferred(); tree.FlushDeferred();
        Check(child.GetCombinedMaximumSize() == new Vector2(49, 38),
            "Propagated maximum bounds subtract signed content margins on both axes.");
        Reject<ArgumentOutOfRangeException>(() => margin.GetMarginSize((Side)99));
    }

    private static void VerifyCenter()
    {
        var viewport = new TestViewport();
        var center = new CenterContainer { Size = new(101, 61) };
        var child = new Control { CustomMinimumSize = new(20, 10) };
        center.AddChild(child); viewport.AddChild(center);
        using var tree = new SceneTree(viewport);
        tree.FlushDeferred();
        Check(!center.UseTopLeft && center.GetMinimumSize() == new Vector2(20, 10) &&
              child.Position == new Vector2(40, 25) && child.Size == new Vector2(20, 10),
            "Ordinary centering floors the offset and retains the bound child minimum.");
        center.UseTopLeft = true;
        tree.FlushDeferred();
        Check(center.GetMinimumSize() == Vector2.Zero && child.Position == new Vector2(-10, -5),
            "Top-left mode centers the child's minimum rectangle on the origin and has no intrinsic minimum.");
        child.CustomMinimumSize = new(21, 11);
        tree.FlushDeferred();
        tree.FlushDeferred();
        Check(child.Position == new Vector2(-11, -6), $"A changed child minimum reflows top-left centering; actual={child.Position}, size={child.Size}.");
    }

    private static void VerifyAspect()
    {
        var viewport = new TestViewport();
        var aspect = new AspectRatioContainer { Size = new(100, 60), Ratio = 2 };
        var child = new Control { CustomMinimumSize = new(10, 10) };
        aspect.AddChild(child); viewport.AddChild(aspect);
        using var tree = new SceneTree(viewport);
        tree.FlushDeferred();
        Check(aspect.Stretch == AspectRatioContainer.StretchMode.Fit &&
              aspect.AlignmentHorizontal == AlignmentMode.Center &&
              aspect.AlignmentVertical == AlignmentMode.Center &&
              child.Position == new Vector2(0, 5) && child.Size == new Vector2(100, 50),
            "Fit preserves the target aspect and centers the complete child inside both axes.");
        child.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        child.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        tree.FlushDeferred();
        Check(child.Position == new Vector2(45, 25) && child.Size == new Vector2(10, 10),
            "Inherited non-fill flags shrink the child inside its aspect allocation.");
        child.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        child.SizeFlagsVertical = Control.SizeFlags.Fill;
        tree.FlushDeferred();
        aspect.Stretch = AspectRatioContainer.StretchMode.Cover;
        tree.FlushDeferred();
        Check(child.Position == new Vector2(-10, 0) && child.Size == new Vector2(120, 60),
            "Cover spans both axes and can overflow the container.");
        aspect.Stretch = AspectRatioContainer.StretchMode.HeightControlsWidth;
        tree.FlushDeferred();
        Check(child.Size == new Vector2(120, 60), "Height-controls-width uses the full height.");
        aspect.Stretch = AspectRatioContainer.StretchMode.WidthControlsHeight;
        aspect.AlignmentVertical = AlignmentMode.End;
        tree.FlushDeferred();
        Check(child.Position == new Vector2(0, 10) && child.Size == new Vector2(100, 50),
            "Width-controls-height and trailing alignment use the remaining vertical space.");
        aspect.Stretch = AspectRatioContainer.StretchMode.Fit;
        aspect.Ratio = 1;
        aspect.AlignmentHorizontal = AlignmentMode.Begin;
        aspect.LayoutDirection = LayoutDirection.RTL;
        tree.FlushDeferred();
        Check(child.Position == new Vector2(40, 0) && child.Size == new Vector2(60, 60),
            "Horizontal Begin is mirrored under RTL while the vertical axis is unchanged.");
        aspect.AlignmentHorizontal = AlignmentMode.End;
        tree.FlushDeferred();
        Check(child.Position == Vector2.Zero, "RTL End selects the physical left edge.");
        child.CustomMinimumSize = new(80, 80);
        tree.FlushDeferred();
        tree.FlushDeferred();
        Check(aspect.Size == new Vector2(100, 80) && child.Size == new Vector2(80, 80) && child.Position == Vector2.Zero,
            $"A growing child minimum expands the container before the aspect refit; container={aspect.Size}, child={child.Position}/{child.Size}.");
        var retainedRatio = aspect.Ratio;
        Reject<ArgumentOutOfRangeException>(() => aspect.Ratio = 0);
        Reject<ArgumentOutOfRangeException>(() => aspect.Ratio = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => aspect.Ratio = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => aspect.Stretch = (AspectRatioContainer.StretchMode)99);
        Reject<ArgumentOutOfRangeException>(() => aspect.AlignmentVertical = (AlignmentMode)99);
        Check(aspect.Ratio == retainedRatio, "Invalid finite-geometry input leaves the prior ratio intact.");
    }

    private static void VerifyPackedState()
    {
        using var center = new CenterContainer { UseTopLeft = true };
        using var margin = new MarginContainer(); margin.AddThemeConstantOverride("margin_left", 13);
        using var aspect = new AspectRatioContainer
        {
            Ratio = 2,
            Stretch = AspectRatioContainer.StretchMode.Cover,
            AlignmentHorizontal = AlignmentMode.End,
            AlignmentVertical = AlignmentMode.Begin
        };
        using var packedCenter = new PackedScene(); packedCenter.Pack(center);
        using var packedMargin = new PackedScene(); packedMargin.Pack(margin);
        using var packedAspect = new PackedScene(); packedAspect.Pack(aspect);
        using var centerCopy = (CenterContainer)packedCenter.Instantiate();
        using var marginCopy = (MarginContainer)packedMargin.Instantiate();
        using var aspectCopy = (AspectRatioContainer)packedAspect.Instantiate();
        Check(centerCopy.UseTopLeft && marginCopy.GetMarginSize(Side.Left) == 13 && aspectCopy.Ratio == 2 &&
              aspectCopy.Stretch == AspectRatioContainer.StretchMode.Cover &&
              aspectCopy.AlignmentHorizontal == AlignmentMode.End &&
              aspectCopy.AlignmentVertical == AlignmentMode.Begin,
            "Exact scene factories retain each container type and its typed policy/theme state.");
    }

    private static void VerifyAllocations()
    {
        var viewport = new TestViewport();
        var margin = new MarginContainer { Name = "Margin", Size = new(100, 60) };
        var center = new CenterContainer { Name = "Center", Size = new(100, 60) };
        var aspect = new AspectRatioContainer { Name = "Aspect", Size = new(100, 60), Ratio = 2 };
        margin.AddChild(new Control { CustomMinimumSize = new(10, 10) });
        center.AddChild(new Control { CustomMinimumSize = new(10, 10) });
        aspect.AddChild(new Control { CustomMinimumSize = new(10, 10) });
        viewport.AddChild(margin); viewport.AddChild(center); viewport.AddChild(aspect);
        using var tree = new SceneTree(viewport);
        for (var pass = 0; pass < 64; pass++)
        {
            var size = pass % 2 == 0 ? new Vector2(100, 60) : new Vector2(102, 62);
            margin.Size = size; center.Size = size; aspect.Size = size; tree.ProcessFrame(0);
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 64; pass++)
        {
            var size = pass % 2 == 0 ? new Vector2(100, 60) : new Vector2(102, 62);
            margin.Size = size; center.Size = size; aspect.Size = size; tree.ProcessFrame(0);
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, $"Prepared active three-container resize and layout allocated {allocated} managed bytes.");
    }

    private static void VerifyFailureContinuation()
    {
        foreach (var container in new Container[]
        {
            new MarginContainer { Size = new(40, 30) },
            new CenterContainer { Size = new(40, 30) },
            new AspectRatioContainer { Size = new(40, 30) }
        })
        {
            var first = new Control { Name = "First", CustomMinimumSize = new(10, 10) };
            var second = new Control { Name = "Second", CustomMinimumSize = new(10, 10) };
            container.AddChild(first); container.AddChild(second);
            using var tree = new SceneTree(container);
            tree.FlushDeferred();
            Action<CanvasItem> fail = _ => throw new ApplicationException("expected layout observer failure");
            first.ItemRectChanged += fail;
            container.Size = new(50, 40);
            Reject<AggregateException>(() => tree.ProcessFrame(0));
            first.ItemRectChanged -= fail;
            var expected = container switch
            {
                MarginContainer => new Rect2(0, 0, 50, 40),
                CenterContainer => new Rect2(20, 15, 10, 10),
                _ => new Rect2(5, 0, 40, 40)
            };
            Check(second.Position == expected.Position && second.Size == expected.Size,
                $"{container.GetType().Name} continues fitting a later child after a callback throws; actual={second.Position}/{second.Size}.");
            tree.FlushDeferred();
        }
    }

    private sealed class TestViewport : Viewport
    {
        public override Rect2 GetVisibleRect() => new(0, 0, 320, 240);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
