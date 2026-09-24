using Electron2D;
using SDL3;

internal static class InputPointerNativeTests
{
    internal static void Run()
    {
        using (var display = DisplayServer.Open("Input pointer contract", new Vector2i(320, 240)))
        {
            DisplayServerPointerNativeTests.Run(display);
            foreach (var shape in Enum.GetValues<Input.CursorShape>())
            {
                Input.Instance.SetDefaultCursorShape(shape);
                Check(Input.Instance.GetCurrentCursorShape() == shape && SDL.GetCursor() != 0,
                    "Every Input cursor shape reaches the native cursor.");
            }
            display.CursorSetShape(DisplayServer.CursorShape.IBeam);
            Check(Input.Instance.GetCurrentCursorShape() == Input.CursorShape.IBeam,
                "Input reads a cursor shape selected directly through the display server.");
            Input.Instance.SetDefaultCursorShape();
            var system = SDL.GetCursor();
            using (var image = Electron2D.Image.CreateFromData(2, 2, false, Electron2D.Image.Format.Rgba8,
                       [255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 255, 255]))
            {
                Input.Instance.SetCustomMouseCursor(image, hotspot: new Vector2(1, 1));
                Check(SDL.GetCursor() != 0 && SDL.GetCursor() != system &&
                    Input.Instance.GetCurrentCursorShape() == Input.CursorShape.Arrow,
                    "Custom Input cursor uses a native copy of the caller-owned image.");
            }
            Input.Instance.SetCustomMouseCursor(null);
            Check(SDL.GetCursor() != 0, "Clearing a custom Input cursor restores the system shape.");
            Reject<ArgumentOutOfRangeException>(() => Input.Instance.MouseMode = Input.MouseModeEnum.Max);
            Reject<ArgumentOutOfRangeException>(() => Input.Instance.SetDefaultCursorShape((Input.CursorShape)17));
            Reject<ArgumentException>(() => Input.Instance.WarpMouse(new Vector2(float.PositiveInfinity, 0)));
        }
        Reject<InvalidOperationException>(() => _ = Input.Instance.MouseMode);
        Reject<InvalidOperationException>(() => Input.Instance.GetCurrentCursorShape());
        Reject<InvalidOperationException>(() => Input.Instance.SetCustomMouseCursor(null));
        Reject<InvalidOperationException>(() => Input.Instance.WarpMouse(new Vector2(1, 1)));
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
