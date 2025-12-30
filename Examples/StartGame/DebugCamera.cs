using System.Numerics;
using Electron2D;

namespace StartGame;

public class DebugCamera() : Camera("DebugCamera")
{
    private const float _speed = 6f;
    private const float _rotateSpeed = 3f;
    private const float _zoomSpeed = 2f;
    private const float minOrtho = 0.25f;
    private const float maxOrtho = 50f;
    
    protected override void Process(float delta)
    {
        var move = Vector2.Zero;

        if (Input.IsKeyDown(KeyCode.Kp4)) move.X -= 1f;
        if (Input.IsKeyDown(KeyCode.Kp6)) move.X += 1f;
        if (Input.IsKeyDown(KeyCode.Kp8)) move.Y += 1f;
        if (Input.IsKeyDown(KeyCode.Kp5)) move.Y -= 1f; // если хочешь, логичнее Kp2

        // диагональная нормализация без sqrt
        if (move.X != 0f && move.Y != 0f)
            move *= 0.70710678f;

        if (move != Vector2.Zero)
            Transform.TranslateSelf(move * (_speed * delta));

        var rot = 0f;
        if (Input.IsKeyDown(KeyCode.Kp9)) rot -= _rotateSpeed * delta;
        if (Input.IsKeyDown(KeyCode.Kp7)) rot += _rotateSpeed * delta;
        if (rot != 0f) Transform.Rotate(rot);

        var factor = 1f;
        if (Input.IsKeyDown(KeyCode.KpPlus))  factor *= MathF.Exp(-_zoomSpeed * delta); // zoom in
        if (Input.IsKeyDown(KeyCode.KpMinus)) factor *= MathF.Exp(+_zoomSpeed * delta); // zoom out

        if (factor != 1f)
            OrthoSize = Math.Clamp(OrthoSize * factor, minOrtho, maxOrtho);
    }

    
    protected override void HandleUnhandledKeyInput(InputEvent inputEvent)
    {
        if (inputEvent is { Type: InputEventType.KeyDown, Code: KeyCode.Kp0 })
        {
            Transform.WorldPosition = Vector2.Zero;
            Transform.WorldRotation = 0f;
            OrthoSize = 5f; // дефолт камеры
        }
    }
}