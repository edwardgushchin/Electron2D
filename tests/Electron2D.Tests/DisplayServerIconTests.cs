using Electron2D;
using SDL3;
using Image = Electron2D.Image;

internal static class DisplayServerIconTests
{
    public static void Run(DisplayServer display)
    {
        var driver = SDL.GetCurrentVideoDriver();
        if (driver is not ("dummy" or "wayland"))
            return;

        using var square = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        using var rectangle = Image.CreateEmpty(2, 3, false, Image.Format.Rgba8);
        using var empty = new Image();

        Expect<ArgumentNullException>(() => display.SetIcon(null!));
        Expect<ArgumentNullException>(() => display.WindowSetIcon(null!));
        Expect<ArgumentException>(() => display.SetIcon(empty));
        Expect<ArgumentException>(() => display.WindowSetIcon(empty));
        Expect<ArgumentOutOfRangeException>(() => display.WindowSetIcon(square, 1));

        if (driver == "wayland")
        {
            Expect<ArgumentException>(() => display.SetIcon(rectangle));
            Expect<ArgumentException>(() => display.WindowSetIcon(rectangle));
        }

        if (driver == "dummy")
        {
            Expect<InvalidOperationException>(() => display.WindowSetIcon(square));
            Expect<InvalidOperationException>(() => display.SetIcon(square));
            Check(!display.HasFeature(DisplayServer.Feature.Icon),
                "A rejected native icon request is not advertised as a capability.");
            return;
        }

        try
        {
            display.SetIcon(square);
        }
        catch (InvalidOperationException)
        {
            // A compositor without the icon protocol rejects both native icon requests.
            Expect<InvalidOperationException>(() => display.WindowSetIcon(square));
            Expect<InvalidOperationException>(() => display.SetIcon(square));
            Check(!display.HasFeature(DisplayServer.Feature.Icon),
                "A compositor without icon support is not advertised as capable.");
            return;
        }

        Check(display.HasFeature(DisplayServer.Feature.Icon),
            "A successful native request establishes Wayland icon support.");
        display.WindowSetIcon(square);
        display.SetIcon(square);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Expect<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name} from an icon operation.");
    }
}
