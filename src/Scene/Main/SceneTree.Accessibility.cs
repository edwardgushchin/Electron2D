using System.Diagnostics;
namespace Electron2D;

public sealed partial class SceneTree
{
    private SceneAccessibility? _accessibility;
    private bool _accessibilityConfigured;
    private readonly long _accessibilityUpdateInterval = Math.Max(1, (long)Math.Ceiling(Stopwatch.Frequency / (double)ProjectSettings.GetWithOverride(ProjectSettings.AccessibilityUpdatesPerSecond)));
    private long _lastAccessibilityUpdate;
    private bool _accessibilitySynchronized;
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
