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

        Expect<ArgumentNullException>(() => DisplayServer.SetIcon(null!));
        Expect<ArgumentNullException>(() => DisplayServer.WindowSetIcon(null!));
        Expect<ArgumentException>(() => DisplayServer.SetIcon(empty));
        Expect<ArgumentException>(() => DisplayServer.WindowSetIcon(empty));
        Expect<ArgumentOutOfRangeException>(() => DisplayServer.WindowSetIcon(square, 1));

        if (driver == "wayland")
        {
            Expect<ArgumentException>(() => DisplayServer.SetIcon(rectangle));
            Expect<ArgumentException>(() => DisplayServer.WindowSetIcon(rectangle));
        }

        if (driver == "dummy")
        {
            Expect<InvalidOperationException>(() => DisplayServer.WindowSetIcon(square));
            Expect<InvalidOperationException>(() => DisplayServer.SetIcon(square));
            Check(!DisplayServer.HasFeature(DisplayServer.Feature.Icon),
                "A rejected native icon request is not advertised as a capability.");
            return;
        }

        try
        {
            DisplayServer.SetIcon(square);
        }
        catch (InvalidOperationException)
        {
            // A compositor without the icon protocol rejects both native icon requests.
            Expect<InvalidOperationException>(() => DisplayServer.WindowSetIcon(square));
            Expect<InvalidOperationException>(() => DisplayServer.SetIcon(square));
            Check(!DisplayServer.HasFeature(DisplayServer.Feature.Icon),
                "A compositor without icon support is not advertised as capable.");
            return;
        }

        Check(DisplayServer.HasFeature(DisplayServer.Feature.Icon),
            "A successful native request establishes Wayland icon support.");
        DisplayServer.WindowSetIcon(square);
        DisplayServer.SetIcon(square);
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
