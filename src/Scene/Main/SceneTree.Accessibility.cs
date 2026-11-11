using System.Diagnostics;
namespace Electron2D;

public sealed partial class SceneTree
{
    private SceneAccessibility? _accessibility;
    private bool _accessibilityConfigured;
    private readonly long _accessibilityUpdateInterval = Math.Max(1, (long)Math.Ceiling(Stopwatch.Frequency / (double)ProjectSettings.GetWithOverride(ProjectSettings.AccessibilityUpdatesPerSecond)));
    private long _lastAccessibilityUpdate;
    private bool _accessibilitySynchronized;
    /// <summary>Reports whether this tree automatically publishes scene accessibility updates.</summary>
    /// <returns>True when this tree owns the active native display, its captured policy enables automatic
    /// scene output and the native accessibility bridge is connected.</returns>
    /// <remarks>Under Auto, the display samples the host accessibility status when it opens. Manual
    /// accessibility elements do not turn on automatic scene publication. Pausing the scene leaves it active.</remarks>
    /// <exception cref="InvalidOperationException">The caller is off the scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The scene tree is disposed.</exception>
    public bool IsAccessibilityEnabled()
    {
        ThrowIfDisposed();
        EnsureOwnerThread();
        return OwnsAccessibilityDisplay() && AccessibilityServer.AutomaticOutput;
    }

    /// <summary>Reports whether this tree's active display supports native accessibility.</summary>
    /// <returns>True when the selected, permitted bridge is connected. A headless tree, a disabled
    /// policy or driver, and an unavailable native bridge return false.</returns>
    /// <remarks>The first support query can initialize the display's bridge. A connected bridge permits
    /// explicit accessibility elements even when automatic scene publication is inactive.</remarks>
    /// <exception cref="InvalidOperationException">The caller is off the scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The scene tree is disposed.</exception>
    public bool IsAccessibilitySupported()
    {
        ThrowIfDisposed();
        EnsureOwnerThread();
        return OwnsAccessibilityDisplay() && AccessibilityServer.IsSupported();
    }

    private bool OwnsAccessibilityDisplay() =>
        Root is Window { NativeDisplay: { } display } && ReferenceEquals(display, DisplayServer.Service);

    private SceneAccessibility? Accessibility
    {
        get
        {
            if (!_accessibilityConfigured) { _accessibilityConfigured = true; _accessibility = SceneAccessibility.Create(this); }
            return _accessibility;
        }
    }
    internal RID GetAccessibilityElement(Node node) => Accessibility?.GetElement(node) ?? default;
    internal void SynchronizeAccessibilityForRequest()
    { EnsureOwnerThread(); EnsureAcceptingWork(); SynchronizeAccessibility(force: true); }
    private void SynchronizeAccessibility(bool force = false)
    {
        if (Accessibility is not { } scene) return;
        var now = Stopwatch.GetTimestamp();
        if (!force && _accessibilitySynchronized && now - _lastAccessibilityUpdate < _accessibilityUpdateInterval)
        { AccessibilityServer.FlushAnnouncements(); return; }
        _lastAccessibilityUpdate = now; _accessibilitySynchronized = true;
        scene.Sync();
    }
}
