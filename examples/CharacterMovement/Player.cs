namespace Electron2D.Examples;

/// <summary>A visible character controlled by the arrow keys.</summary>
internal sealed class Player : Sprite
{
    private const float MovementSpeed = 160f;
    private const float DisplaySize = 96f;
    private readonly Rect2 _movementBounds;

    /// <summary>Centers the character in the field and scales its borrowed texture to 96 pixels.</summary>
    /// <param name="texture">The character texture, kept alive by the entry point.</param>
    /// <param name="playArea">The fixed field that contains the whole character.</param>
    internal Player(Texture texture, Rect2 playArea)
    {
        Name = "Player";
        Texture = texture;
        TextureFilter = TextureFilter.Nearest;
        Position = playArea.GetCenter();
        Scale = new(DisplaySize / texture.GetWidth(), DisplaySize / texture.GetHeight());

        // The sprite is centered on Position, so leave half its size clear at each field edge.
        _movementBounds = playArea.Grow(-DisplaySize / 2);
    }

    /// <summary>Enables input and frame updates after scene activation.</summary>
    protected override void OnReady()
    {
        InputEnabled = true;
        ProcessEnabled = true;
    }

    /// <summary>Exits the application on the first Escape press.</summary>
    protected override void OnInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
            Tree!.Quit();
    }

    /// <summary>Moves at a fixed speed and keeps the character inside the field.</summary>
    protected override void OnProcess(double delta)
    {
        var direction = new Vector2(
            (Input.IsKeyPressed(Key.Right) ? 1 : 0) - (Input.IsKeyPressed(Key.Left) ? 1 : 0),
            (Input.IsKeyPressed(Key.Down) ? 1 : 0) - (Input.IsKeyPressed(Key.Up) ? 1 : 0));

        // Normalize to keep diagonal movement at the same speed; delta is elapsed time in seconds.
        var displacement = direction.Normalized() * MovementSpeed * (float)delta;
        Position = (Position + displacement).Clamp(_movementBounds.Position, _movementBounds.End);
    }
}
