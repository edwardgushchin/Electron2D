using Electron2D;
using SDL3;

internal static class InputPointerNativeTests
{
    internal static void Run()
    {
        using (var display = DisplayServer.Open("Input pointer contract", new Vector2i(320, 240)))
        {
            DisplayServerPointerNativeTests.Run(display);
            foreach (var shape in Enum.GetValues<CursorShape>())
            {
                if (shape == CursorShape.Max) continue;
                Input.SetDefaultCursorShape(shape);
                Check(Input.GetCurrentCursorShape() == shape && SDL.GetCursor() != 0,
                    "Every Input cursor shape reaches the native cursor.");
            }
            DisplayServer.CursorSetShape(CursorShape.IBeam);
            Check(Input.GetCurrentCursorShape() == CursorShape.IBeam,
                "Input reads a cursor shape selected directly through the display server.");
            Input.SetDefaultCursorShape();
            var system = SDL.GetCursor();
            using (var image = Electron2D.Image.CreateFromData(2, 2, false, Electron2D.Image.Format.Rgba8,
                       [255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 255, 255]))
            {
                Input.SetCustomMouseCursor(image, hotspot: new Vector2(1, 1));
                Check(SDL.GetCursor() != 0 && SDL.GetCursor() != system &&
                    Input.GetCurrentCursorShape() == CursorShape.Arrow,
                    "Custom Input cursor uses a native copy of the caller-owned image.");
            }
            Input.SetCustomMouseCursor(null);
            Check(SDL.GetCursor() != 0, "Clearing a custom Input cursor restores the system shape.");
            Reject<ArgumentOutOfRangeException>(() => Input.MouseMode = MouseMode.Max);
            Reject<ArgumentOutOfRangeException>(() => Input.SetDefaultCursorShape((CursorShape)17));
            Reject<ArgumentException>(() => Input.WarpMouse(new Vector2(float.PositiveInfinity, 0)));
        }
        Reject<InvalidOperationException>(() => _ = Input.MouseMode);
        Reject<InvalidOperationException>(() => Input.GetCurrentCursorShape());
        Reject<InvalidOperationException>(() => Input.SetCustomMouseCursor(null));
        Reject<InvalidOperationException>(() => Input.WarpMouse(new Vector2(1, 1)));
        Console.WriteLine("Input native pointer modes, cursor shapes, image and cleanup passed.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
