using System;
using System.Numerics;
using Electron2D;

namespace SpriteAnimation;

/// <summary>
/// Минимальный controllable персонаж для демо:
/// - движение влево/вправо (A/D или стрелки)
/// - прыжок (Space)
/// - атака (J)
/// - смерть (K), респавн (R)
/// </summary>
public sealed class Player(string name) : Node(name)
{
    private enum PlayerState
    {
        Idle,
        Run,
        Jump,
        Fall,
        Attack,
        Death
    }

    private enum PlayerDirection
    {
        Left = -1,
        Right = 1
    }

    // Параметры движения (world-units). При PPU=100:
    // 1.0 unit = 100px, видимая высота = 1.8 units.
    public float MoveSpeed { get; set; } = 1.55f;
    public float JumpVelocity { get; set; } = 3.25f;
    public float Gravity { get; set; } = 9.5f;
    public float GroundY { get; set; } = -0.78f;

    private SpriteRenderer _renderer = null!;
    private SpriteAnimator _animator = null!;
    private CharacterAnimations.AnimationSet _clips = null!;

    private PlayerState _state = PlayerState.Idle;
    private PlayerDirection _dir = PlayerDirection.Right;

    private Vector2 _velocity;
    private bool _grounded = true;
    private bool _attackRequested;

    private Signal.Subscription _onAnimFinishedSub;

    protected override void EnterTree()
    {
        _clips = CharacterAnimations.GetOrCreate();

        _renderer = AddComponent<SpriteRenderer>();
        _renderer.Layer = 10;   // чтобы точно поверх всех 3 слоёв фона
        _renderer.Order = 0;

        // ВАЖНО: выставляем стартовый спрайт явно
        _renderer.SetSprite(_clips.Idle.Frames[0]);

        _animator = AddComponent<SpriteAnimator>();
        _animator.Play(_clips.Idle, true);

        _onAnimFinishedSub = _animator.OnFinished.Connect(OnAnimationFinished);
    }


    protected override void ExitTree()
    {
        _animator?.OnFinished.Disconnect(_onAnimFinishedSub);
        base.ExitTree();
    }

    protected override void Process(float delta)
    {
        // Управление отключаем, если умер.
        if (_state != PlayerState.Death)
            ReadInput();

        // Мягкая логика физики (без коллизий, только "пол").
        Simulate(delta);

        // FSM + анимации.
        UpdateStateAndAnimation();

        base.Process(delta);
    }

    private void ReadInput()
    {
        var left = Input.IsKeyDown(KeyCode.A) || Input.IsKeyDown(KeyCode.Left);
        var right = Input.IsKeyDown(KeyCode.D) || Input.IsKeyDown(KeyCode.Right);

        float x = 0f;
        if (left) x -= 1f;
        if (right) x += 1f;

        if (x < 0f) _dir = PlayerDirection.Left;
        else if (x > 0f) _dir = PlayerDirection.Right;

        // Атака (one-shot)
        if (Input.IsKeyPressed(KeyCode.J))
            _attackRequested = true;

        // Смерть/респавн для теста.
        if (Input.IsKeyPressed(KeyCode.K))
            Die();

        if (Input.IsKeyPressed(KeyCode.R))
            Respawn();

        // Прыжок.
        if (_grounded && Input.IsKeyPressed(KeyCode.Space))
        {
            _velocity.Y = JumpVelocity;
            _grounded = false;
        }

        // Горизонтальная скорость.
        if (_state != PlayerState.Attack) // во время атаки "замораживаем" бег
            _velocity.X = x * MoveSpeed;
        else
            _velocity.X = 0f;
    }

    private void Simulate(float dt)
    {
        // Гравитация.
        if (!_grounded)
            _velocity.Y -= Gravity * dt;

        // Интеграция.
        Transform.Translate(_velocity * dt);

        // "Пол".
        var p = Transform.WorldPosition;
        if (p.Y <= GroundY)
        {
            p.Y = GroundY;
            Transform.WorldPosition = p;

            _velocity.Y = 0f;
            _grounded = true;
        }
        else
        {
            _grounded = false;
        }

        // Флип по направлению.
        var sx = (int)_dir;
        Transform.LocalScale = new Vector2(sx, 1f);
    }

    private void UpdateStateAndAnimation()
    {
        // Death всегда приоритетен.
        if (_state == PlayerState.Death)
        {
            EnsureClip(_clips.Death);
            return;
        }

        // Attack: запускаем по запросу, но не прерываем до окончания.
        if (_state == PlayerState.Attack)
        {
            EnsureClip(_clips.Attack);
            return;
        }

        if (_attackRequested)
        {
            _attackRequested = false;
            _state = PlayerState.Attack;
            EnsureClip(_clips.Attack, restart: true);
            return;
        }

        // В воздухе.
        if (!_grounded)
        {
            if (_velocity.Y > 0.01f)
            {
                _state = PlayerState.Jump;
                EnsureClip(_clips.Jump);
            }
            else
            {
                _state = PlayerState.Fall;
                EnsureClip(_clips.Fall);
            }

            return;
        }

        // На земле.
        if (MathF.Abs(_velocity.X) > 0.001f)
        {
            _state = PlayerState.Run;
            EnsureClip(_clips.Run);
        }
        else
        {
            _state = PlayerState.Idle;
            EnsureClip(_clips.Idle);
        }
    }

    private void EnsureClip(SpriteAnimationClip clip, bool restart = false)
        => _animator.Play(clip, restart);

    private void OnAnimationFinished()
    {
        // Если закончилась атака — возвращаемся в нормальную логику (Idle/Run/Jump/Fall).
        if (_state == PlayerState.Attack)
            _state = PlayerState.Idle;
    }

    private void Die()
    {
        if (_state == PlayerState.Death)
            return;

        _state = PlayerState.Death;
        _velocity = Vector2.Zero;
        _attackRequested = false;
        EnsureClip(_clips.Death, restart: true);
    }

    private void Respawn()
    {
        _state = PlayerState.Idle;
        _velocity = Vector2.Zero;
        _grounded = true;
        _attackRequested = false;

        Transform.WorldPosition = new Vector2(0f, GroundY);
        EnsureClip(_clips.Idle, restart: true);
    }
}
