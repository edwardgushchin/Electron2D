using System;
using System.Numerics;
using Electron2D;

namespace StartGame;

public class Player() : Node("Player")
{
    private SpriteRenderer _spriteRenderer = null!;
    private Texture _playerTexture;
    private Sprite _playerSprite = null!;

    public bool IsAI { get; set; } = true;

    // --- AI params ---
    private const float WanderRadius = 10.0f;          // предел от точки спавна
    private const float ReturnHysteresis = 0.50f;      // чтобы не дрожало на границе

    private const float MinMoveSpeed = 1.0f;           // units/sec
    private const float MaxMoveSpeed = 4.0f;

    private const float MinAngularSpeed = 0.5f;        // rad/sec
    private const float MaxAngularSpeed = 3.0f;

    private const float MinBehaviorTime = 0.6f;        // seconds
    private const float MaxBehaviorTime = 2.0f;

    private Vector2 _home;
    private bool _aiInitialized;

    private float _moveSpeed;
    private float _angularSpeed; // rad/sec (может быть отрицательная)
    private float _behaviorTimer;
    private bool _flipX;

    private bool _returning; // режим возврата внутрь радиуса

    private static readonly Random Rng = new();

    protected override void EnterTree()
    {
        _playerTexture = Resources.GetTexture("player_idle.png");
        _playerSprite = new Sprite(_playerTexture)
        {
            PixelsPerUnit = 236
        };

        _spriteRenderer = AddComponent<SpriteRenderer>();
        _spriteRenderer.SetSprite(_playerSprite);

        // ВАЖНО: home НЕ фиксируем тут (позицию обычно ставят после AddChild)
        // _home = Transform.WorldPosition;
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

        _playerSprite.FlipMode = _flipX ? FlipMode.Horizontal : FlipMode.None;
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

        // Проверка выхода за радиус
        var toHome = _home - pos;
        var distFromHome = toHome.Length();

        if (_returning)
        {
            // Возврат внутрь радиуса
            if (distFromHome <= (WanderRadius - ReturnHysteresis))
            {
                _returning = false;
                // сразу обновим поведение, чтобы не было "монотонного" движения
                RandomizeBehavior();
                _behaviorTimer = NextFloat(MinBehaviorTime, MaxBehaviorTime);
            }
            else
            {
                // Едем к дому по "топору":
                // выставляем rotation так, чтобы +X смотрел в сторону дома,
                // flip выключаем (иначе поедем от дома)
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

        // Периодически меняем скорость/вращение/flip
        _behaviorTimer -= delta;
        if (_behaviorTimer <= 0f)
        {
            RandomizeBehavior();
            _behaviorTimer = NextFloat(MinBehaviorTime, MaxBehaviorTime);
        }

        // Случайное вращение
        var da = _angularSpeed * delta;
        if (da > 0f) Transform.RotateRight(da);
        else if (da < 0f) Transform.RotateLeft(-da);

        // Движение строго по направлению топора (rotation + flip)
        MoveForwardByAxe(delta);
    }

    private void MoveForwardByAxe(float delta)
    {
        var rot = Transform.WorldRotation;

        // forward = направление локального +X в world
        var forward = new Vector2(MathF.Cos(rot), MathF.Sin(rot));

        // flip = едем "в обратную сторону топора"
        if (_flipX) forward = -forward;

        var step = forward * (_moveSpeed * delta);

        // Если у тебя TranslateX/Y — это мировые оси, то так корректно.
        // Если TranslateX/Y двигает по локальным осям — скажи, и я дам вариант без них.
        Transform.TranslateX(step.X);
        Transform.TranslateY(step.Y);
    }

    private void RandomizeBehavior()
    {
        _moveSpeed = NextFloat(MinMoveSpeed, MaxMoveSpeed);

        // угловая скорость со знаком
        var abs = NextFloat(MinAngularSpeed, MaxAngularSpeed);
        _angularSpeed = (Rng.Next(0, 2) == 0) ? abs : -abs;

        // flip (куда "смотрит топор")
        _flipX = (Rng.Next(0, 2) == 0);
    }

    private static float NextFloat(float min, float max)
        => min + (float)Rng.NextDouble() * (max - min);

    private void ManualControl(float delta)
    {
        const float speed = 5f;

        if (Input.IsKeyDown(KeyCode.W)) Transform.TranslateY(speed * delta);
        if (Input.IsKeyDown(KeyCode.A)) { _flipX = true;  Transform.TranslateX(-speed * delta); }
        if (Input.IsKeyDown(KeyCode.S)) Transform.TranslateY(-speed * delta);
        if (Input.IsKeyDown(KeyCode.D)) { _flipX = false; Transform.TranslateX(speed * delta); }

        if (Input.IsKeyDown(KeyCode.Q)) Transform.RotateLeft(speed * delta);
        if (Input.IsKeyDown(KeyCode.E)) Transform.RotateRight(speed * delta);

        _playerSprite.FlipMode = _flipX ? FlipMode.Horizontal : FlipMode.None;
    }
}
