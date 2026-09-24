using Electron2D;

internal static class ControlClipTests
{
    internal static void Run()
    {
        var root = new TestViewport();
        var parent = new Control { Position = new(10, 10), Size = new(20, 20), MouseFilter = MouseFilter.Pass };
        var child = new Control { Position = new(15, 0), Size = new(20, 20) };
        root.AddChild(parent);
        parent.AddChild(child);
        using var tree = new SceneTree(root);
        var childInputs = 0;
        var childExits = 0;
        var parentInputs = 0;
        child.GUIInput += _ => childInputs++;
        child.MouseExited += () => childExits++;
        parent.GUIInput += _ => parentInputs++;

        Move(root, new(35, 15));
        Check(childInputs == 1 && childExits == 0, "An unclipped child receives pointer input outside its parent's rectangle.");
        parent.ClipContents = true;
        Check(childExits == 1, "Enabling clipping immediately releases hover outside the parent.");
        Press(root, new(35, 15));
        Check(childInputs == 1 && parentInputs == 0, "A clipped descendant receives no pointer input outside the parent.");
        Move(root, new(25, 15));
        Press(root, new(25, 15));
        Check(childInputs == 3, "A descendant still receives pointer input inside the clip.");
        Press(root, new(12, 15));
        Check(parentInputs == 1, "Clipping does not suppress the parent's own input.");

        child.TopLevel = true;
        child.Position = new(30, 10);
        Press(root, new(35, 15));
        Check(childInputs == 4, "A top-level child starts a new canvas branch outside the parent clip.");
        child.TopLevel = false;
        child.Position = new(15, 0);
        parent.ClipContents = false;
        Press(root, new(35, 15));
        Check(childInputs == 5, "Disabling clipping restores child targeting.");

        parent.Position = new(50, 50);
        parent.Size = new(20, 10);
        parent.RotationDegrees = 90;
        parent.ClipContents = true;
        child.Position = Vector2.Zero;
        child.Size = new(20, 20);
        Press(root, new(35, 55));
        Check(childInputs == 5, "A rotated ancestor excludes points outside its local clip rectangle.");
        Press(root, new(45, 55));
        Check(childInputs == 6, "A rotated ancestor still accepts points inside its local clip rectangle.");

        var neutral = new Node();
        var independent = new Control { Position = new(75, 75), Size = new(10, 10) };
        parent.AddChild(neutral); neutral.AddChild(independent);
        var independentInputs = 0;
        independent.GUIInput += _ => independentInputs++;
        Press(root, new(80, 80));
        Check(independentInputs == 1, "A non-canvas parent ends clipping inheritance.");

        using var template = new Control { ClipContents = true };
        using var packed = new PackedScene();
        packed.Pack(template);
        using var copy = (Control)packed.Instantiate();
        Check(copy.ClipContents, "Packed controls retain clipping policy.");
        Console.WriteLine("Control descendant clipping input and packed state checks passed.");
    }

    private static void Move(Viewport root, Vector2 point)
    {
        using var input = new InputEventMouseMotion { Position = point };
        root.PushInput(input, inLocalCoordinates: true);
    }

    private static void Press(Viewport root, Vector2 point)
    {
        using var input = new InputEventMouseButton { Position = point, Pressed = true, ButtonIndex = MouseButton.Left };
        root.PushInput(input, inLocalCoordinates: true);
    }

    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 100, 100); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
