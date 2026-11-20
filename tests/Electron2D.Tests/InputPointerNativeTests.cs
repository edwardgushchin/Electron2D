using System.Reflection;
using System.Runtime.InteropServices;
using Electron2D;
using SDL3;
using Image = Electron2D.Image;

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
            using (var image = Image.CreateFromData(2, 2, false, Image.Format.Rgba8,
                       [255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 255, 255]))
            {
                Input.SetCustomMouseCursor(image, hotspot: new Vector2(1, 1));
                Check(SDL.GetCursor() != 0 && SDL.GetCursor() != system &&
                    Input.GetCurrentCursorShape() == CursorShape.Arrow,
                    "Custom Input cursor uses a native copy of the caller-owned image.");
            }
            Input.SetCustomMouseCursor(null);
            Check(SDL.GetCursor() == system, "Clearing a custom Input cursor restores the system shape.");
            CheckAnimatedCursor(system);
            Reject<ArgumentOutOfRangeException>(() => Input.MouseMode = MouseMode.Max);
            Reject<ArgumentOutOfRangeException>(() => Input.SetDefaultCursorShape((CursorShape)17));
            Reject<ArgumentException>(() => Input.WarpMouse(new Vector2(float.PositiveInfinity, 0)));
        }
        Reject<InvalidOperationException>(() => _ = Input.MouseMode);
        Reject<InvalidOperationException>(() => Input.GetCurrentCursorShape());
        Reject<InvalidOperationException>(() => Input.SetCustomMouseCursor(null));
        Reject<InvalidOperationException>(() => Input.WarpMouse(new Vector2(1, 1)));
        Console.WriteLine("Input native pointer modes, cursor shapes, Image, Texture, and AnimatedTexture passed.");
    }

    private static void CheckAnimatedCursor(nint system)
    {
        byte[] firstPixels =
        [
            255, 0, 0, 255, 0, 0, 255, 128, 0, 0, 0, 0,
            255, 255, 255, 255, 255, 255, 0, 255, 255, 0, 255, 255,
        ];
        using var firstImage = Image.CreateFromData(3, 2, false, Image.Format.Rgba8, firstPixels);
        using var secondImage = Image.CreateEmpty(2, 3, false, Image.Format.Rgba8);
        secondImage.Fill(Colors.Green);
        using var first = ImageTexture.CreateFromImage(firstImage);
        using var second = ImageTexture.CreateFromImage(secondImage);
        using var animation = new AnimatedTexture { Frames = 2, Pause = true };
        animation.SetFrameTexture(0, first);
        animation.SetFrameTexture(1, second);
        animation.CurrentFrame = 1;

        var field = typeof(SDL).GetField("CreateColorCursorNativeFunction", BindingFlags.NonPublic | BindingFlags.Static)!;
        var original = (Delegate)field.GetValue(null)!;
        var capture = (Width: 0, Height: 0, HotX: 0, HotY: 0, Pixels: Array.Empty<byte>(), Handle: (nint)0);
        var creations = 0;
        // Inspect the live SDL surface and still call the real native cursor factory.
        Func<nint, int, int, nint> observe = (surface, x, y) =>
        {
            var dimensions = Marshal.PtrToStructure<SDL.Surface>(surface);
            var pixels = ReadCursorPixels(surface, dimensions.Width, dimensions.Height);
            var handle = (nint)original.DynamicInvoke(surface, x, y)!;
            capture = (dimensions.Width, dimensions.Height, x, y, pixels, handle);
            creations++;
            return handle;
        };
        field.SetValue(null, Delegate.CreateDelegate(field.FieldType, observe.Target, observe.Method));
        try
        {
            Input.SetCustomMouseCursor(second, hotspot: new Vector2(1, 2));
            Check(capture is { Width: 2, Height: 3, HotX: 1, HotY: 2, Handle: not 0 } &&
                capture.Pixels.SequenceEqual(secondImage.GetData()) && SDL.GetCursor() == capture.Handle,
                "A regular Texture still sends its pixels, size and hotspot to SDL.");

            Input.SetCustomMouseCursor(animation, hotspot: new Vector2(2, 1));
            var installed = capture.Handle;
            Check(capture is { Width: 3, Height: 2, HotX: 2, HotY: 1, Handle: not 0 } &&
                capture.Pixels.SequenceEqual(firstPixels) && SDL.GetCursor() == installed && installed != system,
                "SDL receives the complete first frame and its hotspot while frame two is active.");
            Check(animation.CurrentFrame == 1, "Selecting cursor pixels does not change animation playback.");
            Check(Reject<ArgumentOutOfRangeException>(() =>
                Input.SetCustomMouseCursor(animation, hotspot: new Vector2(1, 2))).ParamName == "hotspot",
                "The first frame's height rejects a hotspot that fits within only the active frame.");
            Check(SDL.GetCursor() == installed, "An invalid hotspot preserves the installed cursor.");

            animation.SetFrameTexture(0, null);
            CheckUnavailableFrame(animation, installed);
            using var unreadable = new PlaceholderTexture();
            animation.SetFrameTexture(0, unreadable);
            CheckUnavailableFrame(animation, installed);

            animation.SetFrameTexture(0, first);
            animation.CurrentFrame = 0;
            Check(SDL.GetCursor() == installed, "Changing frames retains the installed cursor.");
            animation.CurrentFrame = 1;
            first.Dispose();
            CheckUnavailableFrame(animation, installed);
            animation.Dispose();
            second.Dispose();
            Input.SetDefaultCursorShape(CursorShape.IBeam);
            Input.SetDefaultCursorShape();
            Check(SDL.GetCursor() == installed && creations == 2,
                "The original SDL cursor is reused after frame changes, errors and source disposal.");

            Input.SetCustomMouseCursor(null);
            Check(SDL.GetCursor() == system && creations == 2,
                "Reset restores the cached system cursor after the animation is disposed.");
        }
        finally
        {
            field.SetValue(null, original);
        }
    }

    private static byte[] ReadCursorPixels(nint surface, int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var offset = (y * width + x) * 4;
                Check(SDL.ReadSurfacePixel(surface, x, y, out pixels[offset], out pixels[offset + 1],
                    out pixels[offset + 2], out pixels[offset + 3]), "Could not read the SDL cursor surface.");
            }
        }
        return pixels;
    }

    private static void CheckUnavailableFrame(AnimatedTexture animation, nint installed)
    {
        Check(Reject<ArgumentException>(() => Input.SetCustomMouseCursor(animation)).ParamName == "image",
            "An unavailable first frame is reported as an invalid cursor resource.");
        Check(SDL.GetCursor() == installed, "An unavailable first frame preserves the installed cursor.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static T Reject<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T error)
        {
            return error;
        }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
