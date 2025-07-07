using Electron2D;
using Electron2D.Graphics;
using Electron2D.Inputs;
using Electron2D.Resources;

namespace FlappyBird;

public class Bird : Node
{
    private readonly Sprite _bird;

    // Параметры поворота
    private const float MaxUpAngle = -30f; // отрицательный — наклон вверх
    private const float MaxDownAngle = +30f; // положительный — наклон вниз
    private const float RotationSpeed = 6000; // скорость поворота (градусов в секунду)

    // Текущий угол в градусах
    private float _currentAngle;

    public Bird(string name, Texture texture) : base(name)
    {
        _bird = new Sprite("bird", texture)
        {
            Layer = 10,
        };
    }

    protected override void Awake()
    {
        AddChild(_bird);
    }

    protected override void Update(float deltaTime)
    {
        // Обновляем положение
        _bird.Transform.LocalPosition = Transform.LocalPosition;

        // Прыжок
        if (Input.IsKeyPressed(Scancode.Space) || Input.GetMouseButtonDown(MouseButtonFlags.Left))
        {
            Velocity = Velocity with { Y = JumpVelocity };
        }

        // Гравитация
        Velocity = Velocity with { Y = Velocity.Y + Gravity * deltaTime };

        // Перемещение
        Transform.LocalPosition = Transform.LocalPosition with { Y = Transform.LocalPosition.Y + Velocity.Y * deltaTime };

        // Нормализованная скорость
        var normalizedVelocity = Math.Clamp(Velocity.Y / JumpVelocity, -1f, 1f);

        // Целевой угол
        float targetAngle;
        if (normalizedVelocity > 0)
        {
            targetAngle = normalizedVelocity * MaxUpAngle;
        }
        else
        {
            targetAngle = -normalizedVelocity * MaxDownAngle;
        }

        // Плавный переход текущего угла к целевому
        _currentAngle = MoveTowards(_currentAngle, targetAngle, RotationSpeed * deltaTime);

        // Перевод в радианы
        _bird.Transform.LocalRotation = _currentAngle * (MathF.PI / 180f);
    }

    /// <summary>
    /// Плавное движение от current к target с ограничением приращения
    /// </summary>
    private float MoveTowards(float current, float target, float maxDelta)
    {
        if (Math.Abs(target - current) <= maxDelta)
            return target;

        return current + MathF.Sign(target - current) * maxDelta;
    }

    private float Gravity { get; } = -4f;
    private float JumpVelocity { get; } = 2f;
    public Vector2 Velocity { get; set; } = new(0, 0);
}
