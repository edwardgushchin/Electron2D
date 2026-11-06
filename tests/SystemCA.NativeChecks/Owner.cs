namespace Electron2D;

// The isolated build links the exact producer source without the engine module's native resolver.
public sealed partial class OS
{
    private static OS Service { get; } = new();
    private void ThrowIfDisposed() { }
}
