namespace Electron2D;

public sealed partial class SceneTree
{
    private readonly List<ShaderGlobalsOverride> _shaderGlobals = new();
    internal ShaderGlobalsOverride? SelectedShaderGlobals { get; private set; }
    internal void RegisterShaderGlobals(ShaderGlobalsOverride node)
    { if (!_shaderGlobals.Contains(node)) _shaderGlobals.Add(node); ReconcileShaderGlobals(); }
    internal void UnregisterShaderGlobals(ShaderGlobalsOverride node)
    { _shaderGlobals.Remove(node); ReconcileShaderGlobals(); }
    internal void ReconcileShaderGlobals()
    {
        ShaderGlobalsOverride? next = null;
        foreach (var node in _shaderGlobals)
            if (ReferenceEquals(node.RegisteredTree, this) && (next is null || next.IsGreaterThanCore(node))) next = node;
        if (ReferenceEquals(next, SelectedShaderGlobals)) return;
        var previous = SelectedShaderGlobals; SelectedShaderGlobals = next;
        previous?.SetLifecycleGroup("shader_overrides_group_active", false);
        next?.SetLifecycleGroup("shader_overrides_group_active", true);
        List<Exception>? errors = null;
        try { RenderingServer.SyncSceneGlobals(this); } catch (Exception error) { CollectException(ref errors, error); }
        foreach (var node in _shaderGlobals.ToArray())
            if (!node.IsDisposed && ReferenceEquals(node.RegisteredTree, this))
                try { node.UpdateConfigurationWarnings(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Scene shader selection callbacks failed.", errors);
    }
}
