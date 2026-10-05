namespace Electron2D.Examples;

/// <summary>Builds a small scene using only the public runtime API.</summary>
internal static class CharacterMovementScene
{
    private const int GridSpacing = 32;
    private const int Margin = 32;
    private const int HeaderHeight = 96;
    private const int FooterHeight = 72;

    /// <summary>Creates a grid, live labels and an arrow-key-controlled character.</summary>
    /// <param name="character">The borrowed texture, kept alive by the entry point.</param>
    /// <param name="font">The borrowed font, kept alive by the entry point.</param>
    /// <returns>A detached root window for Engine.Run.</returns>
    internal static Window CreateWindow(Texture character, Font font)
    {
        var window = new Window
        {
            Title = "Electron2D: character movement",
            Size = new(800, 600),
            MinSize = new(400, 300),
            SnapTransformsToPixel = true
        };

        // Rendering services become available when the window enters the running scene.
        window.Ready += _ => RenderingServer.SetDefaultClearColor(Color.FromHTML("#241B2C"));

        var playArea = GetPlayArea(window.Size);
        var grid = new Entity { Name = "Grid" };
        grid.Draw += canvas => DrawGrid(canvas, playArea);
        window.AddChild(grid);

        window.AddChild(CreateLabel("Title", "Character movement", font, new(Margin, 24), 28, "#F9F3EE"));
        var instructions = CreateLabel("Instructions", "Arrow keys to move  /  Escape to exit", font, new(Margin, playArea.End.Y + 24), 16, "#F2A6CC");
        var player = new Player(character, playArea);
        window.AddChild(instructions);
        window.AddChild(player);

        // Reflow only when the actual window size changes, keeping text and the sprite at their pixel sizes.
        window.SizeChanged += () =>
        {
            playArea = GetPlayArea(window.Size);
            instructions.Position = new(Margin, playArea.End.Y + 24);
            player.SetPlayArea(playArea);
            grid.QueueRedraw();
        };
        return window;
    }

    private static Rect2 GetPlayArea(Vector2i windowSize) =>
        new(Margin, HeaderHeight, windowSize.X - 2 * Margin, windowSize.Y - HeaderHeight - FooterHeight);

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
