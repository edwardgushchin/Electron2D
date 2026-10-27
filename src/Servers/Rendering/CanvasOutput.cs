namespace Electron2D;

internal readonly record struct CanvasOutput(bool Available, bool Supported, bool Enabled, float Reference, float Maximum)
{
    internal float MaxLinear => Enabled ? Maximum / Reference : 1;
}
