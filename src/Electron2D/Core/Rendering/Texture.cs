using System.Runtime.CompilerServices;

namespace Electron2D;

public sealed class Texture : IEquatable<Texture>
{
    internal Texture(nint handle, int width, int height)
    {
        Handle = handle;
        Width = width;
        Height = height;
    }

    internal nint Handle { get; private set; }

    public bool IsValid => Handle != 0;
    public int Width { get; private set; }
    public int Height { get; private set; }

    internal void Reset(nint handle, int width, int height)
    {
        Handle = handle;
        Width = width;
        Height = height;
    }

    internal void Invalidate() => Reset(0, 0, 0);
    
    public FilterMode FilterMode { get; set; } = FilterMode.Inherit;

    public bool Equals(Texture? other) => ReferenceEquals(this, other);
    public override bool Equals(object? obj) => ReferenceEquals(this, obj);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}