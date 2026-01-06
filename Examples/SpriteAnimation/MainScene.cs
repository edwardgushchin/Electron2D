using System.Numerics;
using Electron2D;

namespace SpriteAnimation;

public class MainScene() : Node("MainScene")
{
    private Background _background = null!;
    private Camera _camera = null!;
    private Player _player = null!;
    private const float _groundY = -0.1f;

    protected override void EnterTree()
    {
        _background = new Background("Background");

        _camera = new PixelPerfectCamera("Main Camera")
        {
            Current = true,
            PixelsPerUnit = 100,
            SnapPosition = true,
            EnforceNoRotation = true
        };

        AddChild(_background);
        AddChild(_player = new Player("Player"));
        AddChild(_camera);

        // Дефолтная позиция персонажа: нижняя часть экрана.
        // При Pivot = (0.5, 1.0) координата Y воспринимается как "пол".
        _player.Transform.WorldPosition = new Vector2(-0.7f, _groundY);
    }

    protected override void Process(float delta)
    {
        // Минимальный follow (без сглаживания) — камера центрируется по игроку.
        // PixelPerfectCamera сама снапает позицию на сетку 1/PPU.
        var p = _player.Transform.WorldPosition;
        _camera.Transform.WorldPosition = p with { Y = 0f };
        
        //_camera.Transform.WorldPosition = Vector2.Lerp(_camera.Transform.WorldPosition, _player.Transform.WorldPosition, delta);
    }
}