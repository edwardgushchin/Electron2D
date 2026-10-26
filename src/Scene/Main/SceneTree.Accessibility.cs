namespace Electron2D;

public sealed partial class SceneTree
{
    private SceneAccessibility? _accessibility;
    private bool _accessibilityConfigured;
    private SceneAccessibility? Accessibility
    {
        get
        {
            if (!_accessibilityConfigured) { _accessibilityConfigured = true; _accessibility = SceneAccessibility.Create(this); }
            return _accessibility;
        }
    }
    internal RID GetAccessibilityElement(Node node) => Accessibility?.GetElement(node) ?? default;
    private void SynchronizeAccessibility() => Accessibility?.Sync();
}
