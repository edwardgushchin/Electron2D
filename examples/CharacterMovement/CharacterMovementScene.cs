namespace Electron2D.Examples;

/// <summary>Builds a small scene using only the public runtime API.</summary>
internal static class CharacterMovementScene
{
    private const int GridSpacing = 32;
    private static readonly Rect2 PlayArea = new(32, 96, 736, 432);

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
            SnapTransformsToPixel = true
        };

        // Rendering services become available when the window enters the running scene.
        window.Ready += _ => RenderingServer.SetDefaultClearColor(Color.FromHTML("#241B2C"));

        var grid = new Entity { Name = "Grid" };
        grid.Draw += DrawGrid;
        window.AddChild(grid);

        window.AddChild(CreateLabel("Title", "Character movement", font, new(32, 24), 28, "#F9F3EE"));
        window.AddChild(CreateLabel("Instructions", "Arrow keys to move  /  Escape to exit", font, new(32, 552), 16, "#F2A6CC"));
        window.AddChild(new Player(character, PlayArea));
        return window;
    }

    private static void DrawGrid(CanvasItem canvas)
    {
        var lineColor = Color.FromHTML("#3D2749");
        canvas.DrawRect(PlayArea, Color.FromHTML("#2E2238"));

        // Draw callbacks record geometry once; moving the character does not redraw the grid.
        for (var x = PlayArea.Position.X; x <= PlayArea.End.X; x += GridSpacing)
            canvas.DrawLine(new(x, PlayArea.Position.Y), new(x, PlayArea.End.Y), lineColor);

        for (var y = PlayArea.Position.Y; y <= PlayArea.End.Y; y += GridSpacing)
            canvas.DrawLine(new(PlayArea.Position.X, y), new(PlayArea.End.X, y), lineColor);

        canvas.DrawRect(PlayArea, Color.FromHTML("#A63B75"), filled: false);
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
