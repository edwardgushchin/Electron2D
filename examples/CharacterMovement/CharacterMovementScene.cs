namespace Electron2D.Examples;

/// <summary>Builds a small scene using only the public runtime API.</summary>
internal static class CharacterMovementScene
{
    private const int GridSpacing = 32;
    private const int Margin = 32;
    private const int HeaderHeight = 96;
    private const int FooterHeight = 72;
    private static readonly Vector2i DesktopSize = new(800, 600);

    /// <summary>Creates a grid, live labels and an arrow-key-controlled character.</summary>
    /// <param name="character">The borrowed texture, kept alive by the entry point.</param>
    /// <param name="font">The borrowed font, kept alive by the entry point.</param>
    /// <returns>A detached root window for Engine.Run.</returns>
    internal static Window CreateWindow(Texture character, Font font, bool fullscreen = false)
    {
        var window = new Window
        {
            Title = "CharacterMovement",
            Size = DesktopSize,
            Unresizable = !fullscreen,
            Mode = fullscreen && !OperatingSystem.IsBrowser() ? WindowMode.Fullscreen : WindowMode.Windowed,
            MinSize = fullscreen ? Vector2i.Zero : DesktopSize,
            MaxSize = fullscreen ? Vector2i.Zero : DesktopSize,
            SnapTransformsToPixel = true
        };

        // Rendering services become available when the window enters the running scene.
        window.Ready += _ => RenderingServer.SetDefaultClearColor(Color.FromHTML("#241B2C"));

        var playArea = GetPlayArea(window.Size);
        var grid = new Entity { Name = "Grid" };
        grid.Draw += canvas => DrawGrid(canvas, playArea);
        window.AddChild(grid);

        window.AddChild(CreateLabel("Title", "CharacterMovement", font, new(Margin, 24), 28, "#F9F3EE"));
        var instructions = CreateLabel("Instructions", "Arrow keys  /  D-pad  /  Touch", font, new(Margin, playArea.End.Y + 24), 26, "#F2A6CC");
        var player = new Player(character, playArea);
        window.AddChild(instructions);
        window.AddChild(player);

        var firstLayout = true;
        void UseNativeSurfaceSize()
        {
            if (window.Size.X <= 0 || window.Size.Y <= 0)
                return;
            var (scale, canvasSize) = GetSurfaceLayout(window.Size);
            window.GlobalCanvasTransform = new Transform(0, Vector2.One * scale, 0, Vector2.Zero);
            playArea = GetPlayArea(canvasSize);
            instructions.Position = new(Margin, playArea.End.Y + 24);
            player.SetPlayArea(playArea);
            if (firstLayout)
                player.Position = playArea.GetCenter();
            firstLayout = false;
            player.CancelDrag();
            grid.QueueRedraw();
        }
        if (fullscreen)
        {
            window.Ready += _ => UseNativeSurfaceSize();
            window.SizeChanged += UseNativeSurfaceSize;
        }
        window.FocusExited += player.CancelDrag;
        return window;
    }

    internal static (float Scale, Vector2 Size) GetSurfaceLayout(Vector2i surfaceSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(surfaceSize.X);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(surfaceSize.Y);
        var scale = Math.Min(surfaceSize.X, surfaceSize.Y) / 600f;
        return (scale, new Vector2(surfaceSize.X / scale, surfaceSize.Y / scale));
    }

    private static Rect2 GetPlayArea(Vector2 windowSize) =>
        new(Margin, HeaderHeight, Math.Max(96, windowSize.X - 2 * Margin), Math.Max(96, windowSize.Y - HeaderHeight - FooterHeight));

    private static void DrawGrid(CanvasItem canvas, Rect2 playArea)
    {
        var lineColor = Color.FromHTML("#3D2749");
        canvas.DrawRect(playArea, Color.FromHTML("#2E2238"));

        // Geometry is retained between resizes; moving the character does not redraw the grid.
        for (var x = playArea.Position.X; x <= playArea.End.X; x += GridSpacing)
            canvas.DrawLine(new(x, playArea.Position.Y), new(x, playArea.End.Y), lineColor);

        for (var y = playArea.Position.Y; y <= playArea.End.Y; y += GridSpacing)
            canvas.DrawLine(new(playArea.Position.X, y), new(playArea.End.X, y), lineColor);

        canvas.DrawRect(playArea, Color.FromHTML("#A63B75"), filled: false);
    }

    private static Label CreateLabel(string name, string text, Font font, Vector2 position, int fontSize, string color)
    {
        var label = new Label(text)
        {
            Name = name,
            Position = position,
            MouseFilter = MouseFilter.Ignore
        };
        label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", Color.FromHTML(color));
        return label;
    }
}
