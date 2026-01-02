namespace Electron2D;

/// <summary>
/// Лёгкий фасад текстуры. Не владеет ресурсом.
/// Владение и уничтожение SDL_Texture выполняет <c>ResourceSystem</c>.
/// </summary>
public readonly struct Texture : IEquatable<Texture>
{
    internal Texture(nint handle, int width, int height)
    {
        Handle = handle;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Нативный handle текстуры (SDL_Texture*).
    /// </summary>
    internal nint Handle { get; }

    /// <summary>
    /// Возвращает <see langword="true"/>, если текстура указывает на валидный нативный ресурс.
    /// </summary>
    public bool IsValid => Handle != 0;

    public int Width { get; }
    public int Height { get; }

    public bool Equals(Texture other) => Handle == other.Handle;

    public override bool Equals(object? obj) => obj is Texture other && Equals(other);

    public override int GetHashCode() => Handle.GetHashCode();

    public static bool operator ==(Texture left, Texture right) => left.Equals(right);

    public static bool operator !=(Texture left, Texture right) => !left.Equals(right);
}