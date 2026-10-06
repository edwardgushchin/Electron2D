namespace Electron2D.Examples;

/// <summary>A fixed-size character controlled by directional input and touch dragging.</summary>
internal sealed class Player : Sprite
{
    private const float MovementSpeed = 160f;
    private const float DisplaySize = 96f;
    private Rect2 _movementBounds;
    private int? _dragContact;
    private Vector2 _dragOffset;

    /// <summary>Centers the character in the field and scales its borrowed texture to 96 pixels.</summary>
    /// <param name="texture">The character texture, kept alive by the entry point.</param>
    /// <param name="playArea">The field that contains the whole character.</param>
    internal Player(Texture texture, Rect2 playArea)
    {
        Name = "Player";
        Texture = texture;
        TextureFilter = TextureFilter.Nearest;
        Position = playArea.GetCenter();
        Scale = new(DisplaySize / texture.GetWidth(), DisplaySize / texture.GetHeight());

        SetPlayArea(playArea);
    }

    /// <summary>Keeps the character within the available logical field.</summary>
    /// <param name="playArea">The current field rectangle before the uniform canvas scale.</param>
    internal void SetPlayArea(Rect2 playArea)
    {
        // The sprite is centered on Position, so leave half its size clear at each field edge.
        _movementBounds = playArea.Grow(-DisplaySize / 2);
        Position = Position.Clamp(_movementBounds.Position, _movementBounds.End);
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
        if (@event is InputEventScreenTouch touch)
        {
            if (touch.Pressed && _dragContact is null &&
                new Rect2(Position - new Vector2(DisplaySize / 2, DisplaySize / 2), new(DisplaySize, DisplaySize)).HasPoint(touch.Position))
            {
                _dragContact = touch.Index;
                _dragOffset = Position - touch.Position;
                GetViewport()!.SetInputAsHandled();
            }
            else if (!touch.Pressed && _dragContact == touch.Index)
                CancelDrag();
        }
        else if (@event is InputEventScreenDrag drag && _dragContact == drag.Index)
        {
            Position = (drag.Position + _dragOffset).Clamp(_movementBounds.Position, _movementBounds.End);
            GetViewport()!.SetInputAsHandled();
        }
    }

    internal void CancelDrag() => _dragContact = null;

    /// <summary>Moves at a fixed speed and keeps the character inside the field.</summary>
    protected override void OnProcess(double delta)
    {
        if (_dragContact is not null)
            return;
        var direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        // Normalize to keep diagonal movement at the same speed; delta is elapsed time in seconds.
        var displacement = direction.Normalized() * MovementSpeed * (float)delta;
        Position = (Position + displacement).Clamp(_movementBounds.Position, _movementBounds.End);
    }
}
