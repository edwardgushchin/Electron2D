namespace Electron2D;

/// <summary>A sampled instance ID and borrowed target; rebinding a consumer never retargets an older sample.</summary>
internal readonly record struct ObjectIdentity(ulong ID, WeakReference<ElectronObject>? Reference)
{
    internal ElectronObject? Target => RawTarget is { IsDisposed: false } value ? value : null;
    internal ElectronObject? RawTarget => Reference is { } weak && weak.TryGetTarget(out var value) ? value : null;
}
