namespace Electron2D;

/// <summary>Selects the default canvas environment of the viewport's world.</summary>
/// <remarks>The first node with a live environment in scene order controls each world, including viewports sharing
/// that world. A world without an environment has no glow. This node contributes no canvas geometry.</remarks>
public sealed class WorldEnvironment : Node
{
    private CanvasEnvironment? _environment;

    /// <summary>Creates a detached world environment selector.</summary>
    public WorldEnvironment() { }

    /// <summary>Gets or sets the borrowed canvas environment selected by this node.</summary>
    /// <value>Null initially. Null clears the selection.</value>
    /// <exception cref="ObjectDisposedException">This node or the supplied resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">Attached mutation is off-owner or during renderer submission.</exception>
    public CanvasEnvironment? Environment
    {
        get { ThrowIfDisposed(); Tree?.EnsureProcessingReadAccess(); return _environment; }
        set
        {
            EnsureMutable();
            ObjectDisposedException.ThrowIf(value is { IsDisposed: true }, value!);
            if (ReferenceEquals(_environment, value)) return;
            if (Tree is not null) RenderingServer.Service?.EnsureViewportMutation();
            _environment = value;
            UpdateConfigurationWarnings();
        }
    }

    internal CanvasEnvironment? LiveEnvironment => _environment is { IsDisposed: false } value ? value : null;

    /// <inheritdoc />
    protected override void OnEnterTree() => Tree!.RegisterWorldEnvironment(this);

    /// <inheritdoc />
    protected override void OnExitTree() => Tree!.UnregisterWorldEnvironment(this);

    /// <inheritdoc />
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings();
        if (LiveEnvironment is null) return [.. warnings, "Assign a canvas environment to configure this world."];
        if (Tree is { } tree && GetViewport()?.FindWorld() is { } world && tree.SelectWorldEnvironment(world.Runtime) is { } selected && !ReferenceEquals(selected, this))
            return [.. warnings, "Only the first canvas environment in scene order controls this world."];
        return warnings;
    }

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreateWorldEnvironment;

    private static Node CreateWorldEnvironment() => new WorldEnvironment();

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        try { base.Dispose(disposing); }
        finally { if (disposing) _environment = null; }
    }

    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<WorldEnvironment, CanvasEnvironment?>(nameof(Environment), n => n.Environment, (n, v) => n.Environment = v, _ => null, stored: true)
    ];
}

public sealed partial class SceneTree
{
    private readonly List<WorldEnvironment> _worldEnvironments = [];
    internal bool HasWorldEnvironment => _worldEnvironments.Count != 0;

    internal void RegisterWorldEnvironment(WorldEnvironment node)
    {
        _worldEnvironments.Add(node);
        RenderingServer.NotifyRenderMutation();
    }

    internal void UnregisterWorldEnvironment(WorldEnvironment node)
    {
        _worldEnvironments.Remove(node);
        RenderingServer.NotifyRenderMutation();
    }

    internal WorldEnvironment? SelectWorldEnvironment(WorldRuntime world)
    {
        WorldEnvironment? selected = null;
        foreach (var node in _worldEnvironments)
        {
            if (node.IsDepartingTree || node.LiveEnvironment is null ||
                !ReferenceEquals(node.GetViewport()?.FindWorld()?.Runtime, world)) continue;
            if (selected is null || selected.IsGreaterThan(node)) selected = node;
        }
        return selected;
    }
}
