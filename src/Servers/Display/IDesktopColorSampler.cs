namespace Electron2D;

internal interface IDesktopColorSampler : IDisposable
{
    bool TryReadSelection(out Vector2i point, out bool accepted);
}
