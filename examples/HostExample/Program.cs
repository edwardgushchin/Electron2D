using Electron2D;

var window = new Window { Title = "Electron2D: window and input", Size = new Vector2i(640, 360) };
window.AddChild(new ExampleRoot());
Engine.Instance.MaxFPS = 60;
return Engine.Instance.Run(window);

sealed class ExampleRoot : Entity
{
    private double _reportTime;

    protected override void OnReady()
    {
        Name = "ExampleRoot";
        InputEnabled = true;
        ProcessEnabled = true;
        Console.WriteLine("Hold arrow keys to move the scene node. Press Escape or close the window to quit.");
    }

    protected override void OnInput(InputEvent @event)
    {
        if (@event is InputEventKey { Echo: false } arrow &&
            arrow.Keycode is Key.Left or Key.Right or Key.Up or Key.Down)
            Console.WriteLine($"{arrow.Keycode}: {(arrow.Pressed ? "down" : "up")}");

        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
            Tree!.Quit();
    }

    protected override void OnProcess(double delta)
    {
        var input = Input.Instance;
        var direction = new Vector2(
            (input.IsKeyPressed(Key.Right) ? 1 : 0) - (input.IsKeyPressed(Key.Left) ? 1 : 0),
            (input.IsKeyPressed(Key.Down) ? 1 : 0) - (input.IsKeyPressed(Key.Up) ? 1 : 0));
        Position += direction * (float)(100d * delta);
        _reportTime += delta;
        if (_reportTime < 1d)
            return;
        _reportTime -= 1d;
        if (direction != Vector2.Zero)
            Console.WriteLine($"Position: {Position}");
    }
}
