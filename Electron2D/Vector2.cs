namespace Electron2D;

public struct Vector2(float x, float y)
{
    public float X = x;
    public float Y = y;

    // Получение длины вектора
    public float Length => MathF.Sqrt(X * X + Y * Y);

    // Получение квадратной длины (без вычислений с корнем, если длина не нужна)
    public float LengthSquared => X * X + Y * Y;

    // Нормализация вектора
    public Vector2 Normalized
    {
        get
        {
            var length = Length;
            return length > 0.0001f ? new Vector2(X / length, Y / length) : this;
        }
    }

    // Сложение двух векторов
    public static Vector2 operator +(Vector2 v1, Vector2 v2) => new Vector2(v1.X + v2.X, v1.Y + v2.Y);

    // Вычитание двух векторов
    public static Vector2 operator -(Vector2 v1, Vector2 v2) => new Vector2(v1.X - v2.X, v1.Y - v2.Y);

    // Умножение вектора на скаляр
    public static Vector2 operator *(Vector2 v, float scalar) => new Vector2(v.X * scalar, v.Y * scalar);

    // Деление вектора на скаляр
    public static Vector2 operator /(Vector2 v, float scalar)
    {
        if (scalar == 0)
            throw new DivideByZeroException("Cannot divide by zero.");
        return new Vector2(v.X / scalar, v.Y / scalar);
    }

    // Операция скалярного произведения
    public static float Dot(Vector2 v1, Vector2 v2) => v1.X * v2.X + v1.Y * v2.Y;

    // Операция пересечения векторов (перпендикуляр)
    public static float Cross(Vector2 v1, Vector2 v2) => v1.X * v2.Y - v1.Y * v2.X;

    // Угол между векторами
    /*public static float AngleBetween(Vector2 v1, Vector2 v2)
    {
        var dot = Dot(v1, v2);
        var lengths = v1.Length * v2.Length;
        if (lengths == 0) return 0;
        return MathF.Acos(MathF.Clamp(dot / lengths, -1f, 1f));
    }*/

    // Проверка на равенство
    // Метод для сравнения с погрешностью
    public bool Equals(Vector2 other)
    {
        return MathF.Abs(X - other.X) < float.Epsilon && MathF.Abs(Y - other.Y) < float.Epsilon;
    }

    // Переопределение Equals
    public override bool Equals(object? obj)
    {
        return obj is Vector2 vector2 && Equals(vector2);
    }

    // Переопределение хеш-кода
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    // Строковое представление вектора
    public override string ToString() => $"({X}, {Y})";
    
    // Вспомогательные методы

    // Изменение длины вектора на заданную
    public void SetLength(float length)
    {
        var currentLength = Length;
        if (!(currentLength > 0.0001f)) return;
        X *= length / currentLength;
        Y *= length / currentLength;
    }

    // Векторное расстояние
    public static float Distance(Vector2 v1, Vector2 v2) => (v1 - v2).Length;

    // Векторное расстояние в квадрате
    public static float DistanceSquared(Vector2 v1, Vector2 v2) => (v1 - v2).LengthSquared;
}