using System;
using System.Numerics;
using Electron2D;

namespace StartGame;

public class Box(Texture _texture) : Node("Box")
{
    private SpriteRenderer _spriteRenderer = null!;
    private Sprite _sprite = null!;

    public bool IsAI { get; set; } = true;

    // --- AI params ---
    private const float WanderRadius = 10.0f;
    private const float ReturnHysteresis = 0.50f;

    private const float MinMoveSpeed = 1.0f;
    private const float MaxMoveSpeed = 4.0f;

    private const float MinAngularSpeed = 0.5f;
    private const float MaxAngularSpeed = 3.0f;

    private const float MinBehaviorTime = 0.6f;
    private const float MaxBehaviorTime = 2.0f;

    private Vector2 _home;
    private bool _aiInitialized;

    private float _moveSpeed;
    private float _angularSpeed;
    private float _behaviorTimer;
    private bool _flipX;

    private bool _returning;

    private static readonly Random Rng = new();

    protected override void EnterTree()
    {
        _sprite = new Sprite(_texture)
        {
            PixelsPerUnit = 512
        };

        _spriteRenderer = AddComponent<SpriteRenderer>();
        _spriteRenderer.SetSprite(_sprite);
    }

    protected override void Process(float delta)
    {
        if (!IsAI)
        {
            ManualControl(delta);
            return;
        }

        EnsureAIInitialized();
        TickAI(delta);

        _sprite.FlipMode = _flipX ? FlipMode.Horizontal : FlipMode.None;
    }

    private void EnsureAIInitialized()
    {
        if (_aiInitialized) return;

        _home = Transform.WorldPosition;
        RandomizeBehavior();
        _behaviorTimer = NextFloat(MinBehaviorTime, MaxBehaviorTime);

        _aiInitialized = true;
    }

    private void TickAI(float delta)
    {
        var pos = Transform.WorldPosition;

        var toHome = _home - pos;
        var distFromHome = toHome.Length();

        if (_returning)
        {
            if (distFromHome <= (WanderRadius - ReturnHysteresis))
            {
                _returning = false;
                RandomizeBehavior();
                _behaviorTimer = NextFloat(MinBehaviorTime, MaxBehaviorTime);
            }
            else
            {
                if (distFromHome > 0.0001f)
                {
                    var desired = MathF.Atan2(toHome.Y, toHome.X);
                    Transform.WorldRotation = desired;
                    _flipX = false;
                }

                MoveForwardByAxe(delta);
                return;
            }
        }
        else
        {
            if (distFromHome > WanderRadius)
            {
                _returning = true;
                return;
            }
        }

        _behaviorTimer -= delta;
        if (_behaviorTimer <= 0f)
        {
            RandomizeBehavior();
            _behaviorTimer = NextFloat(MinBehaviorTime, MaxBehaviorTime);
        }

        var da = _angularSpeed * delta;
        if (da > 0f) Transform.RotateRight(da);
        else if (da < 0f) Transform.RotateLeft(-da);

        MoveForwardByAxe(delta);
    }

    private void MoveForwardByAxe(float delta)
    {
        var rot = Transform.WorldRotation;

        var forward = new Vector2(MathF.Cos(rot), MathF.Sin(rot));
        if (_flipX) forward = -forward;

        var step = forward * (_moveSpeed * delta);

        Transform.TranslateX(step.X);
        Transform.TranslateY(step.Y);
    }

    private void RandomizeBehavior()
    {
        _moveSpeed = NextFloat(MinMoveSpeed, MaxMoveSpeed);

        var abs = NextFloat(MinAngularSpeed, MaxAngularSpeed);
        _angularSpeed = (Rng.Next(0, 2) == 0) ? abs : -abs;

        _flipX = (Rng.Next(0, 2) == 0);
    }

    private static float NextFloat(float min, float max)
        => min + (float)Rng.NextDouble() * (max - min);

    private void ManualControl(float delta)
    {
        const float speed = 5f;

        if (Input.IsKeyDown(KeyCode.W)) Transform.TranslateY(speed * delta);
        if (Input.IsKeyDown(KeyCode.A)) { _flipX = true; Transform.TranslateX(-speed * delta); }
        if (Input.IsKeyDown(KeyCode.S)) Transform.TranslateY(-speed * delta);
        if (Input.IsKeyDown(KeyCode.D)) { _flipX = false; Transform.TranslateX(speed * delta); }

        if (Input.IsKeyDown(KeyCode.Q)) Transform.RotateLeft(speed * delta);
        if (Input.IsKeyDown(KeyCode.E)) Transform.RotateRight(speed * delta);

        _sprite.FlipMode = _flipX ? FlipMode.Horizontal : FlipMode.None;
    }
}
