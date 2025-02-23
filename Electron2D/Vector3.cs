namespace Electron2D;

public struct Vector3(float x, float y, float z)
{
    public float X = x;
    public float Y = y;
    public float Z = z;
    
    public static Vector3 Left => new(-1, 0, 0);
    
    public static Vector3 Right => new(1, 0, 0);
    
    public static Vector3 Zero => new(0, 0, 0);
    
    public static Vector3 Up => new(0, 1, 0);
    
    public static Vector3 Down => new(0, -1, 0);
    
    public static Vector3 Back => new(0, 0, -1);

    public static Vector3 Forward => new(0, 0, 1);
    

    // Получение длины вектора
    public float Length => MathF.Sqrt(X * X + Y * Y + Z * Z);

    // Получение квадратной длины (без вычислений с корнем, если длина не нужна)
    public float LengthSquared => X * X + Y * Y + Z * Z;

    // Нормализация вектора
    public Vector3 Normalized
    {
        get
        {
            var length = Length;
            return length > 0.0001f ? new Vector3(X / length, Y / length, Z / Length) : this;
        }
    }

    // Сложение двух векторов
    public static Vector3 operator +(Vector3 v1, Vector3 v2) => new (v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);

    // Вычитание двух векторов
    public static Vector3 operator -(Vector3 v1, Vector3 v2) => new (v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);

    // Умножение вектора на скаляр
    public static Vector3 operator *(Vector3 v, float scalar) => new (v.X * scalar, v.Y * scalar, v.Z * scalar);

    // Деление вектора на скаляр
    public static Vector3 operator /(Vector3 v, float scalar)
    {
        if (scalar == 0)
            throw new DivideByZeroException("Cannot divide by zero.");
        return new Vector3(v.X / scalar, v.Y / scalar, v.Z / scalar);
    }

    // Операция скалярного произведения
    public static float Dot(Vector3 v1, Vector3 v2) => v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;

    // Операция пересечения векторов (перпендикуляр)
    public static float Cross(Vector3 v1, Vector3 v2) => v1.X * v2.Y - v1.Y * v2.X + v1.Z * v2.Z;

    // Угол между векторами
    public static float AngleBetween(Vector3 v1, Vector3 v2)
    {
        var dot = Dot(v1, v2);
        var lengths = v1.Length * v2.Length;
        if (lengths == 0) return 0;
        return MathF.Acos(Math.Clamp(dot / lengths, -1f, 1f));
    }

    // Проверка на равенство
    // Метод для сравнения с погрешностью
    public bool Equals(Vector3 other)
    {
        return MathF.Abs(X - other.X) < float.Epsilon && MathF.Abs(Y - other.Y) < float.Epsilon && MathF.Abs(Z - other.Z) < float.Epsilon;
    }

    // Переопределение Equals
    public override bool Equals(object? obj)
    {
        return obj is Vector3 vector3 && Equals(vector3);
    }

    // Переопределение хеш-кода
    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    // Строковое представление вектора
    public override string ToString() => $"({X}x{Y}x{Z})";
    
    // Вспомогательные методы

    // Изменение длины вектора на заданную
    public void SetLength(float length)
    {
        var currentLength = Length;
        if (!(currentLength > 0.0001f)) return;
        X *= length / currentLength;
        Y *= length / currentLength;
        Z *= length / currentLength;
    }

    // Векторное расстояние
    public static float Distance(Vector3 v1, Vector3 v2) => (v1 - v2).Length;

    // Векторное расстояние в квадрате
    public static float DistanceSquared(Vector3 v1, Vector3 v2) => (v1 - v2).LengthSquared;
}