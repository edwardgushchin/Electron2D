namespace Electron2D;

public abstract partial class Viewport
{
    private World? _world;
    private bool _ownsWorld = true;
    private World LocalWorld
    {
        get
        {
            if (_world is null) _world = new(WorldRuntime.CreateSceneDefault());
            else if (!_world.Runtime.Alive && _ownsWorld) { var next = new World(WorldRuntime.CreateSceneDefault()); _world.Dispose(); _world = next; }
            else if (_world.IsDisposed) { _world = new(_world.Runtime); _ownsWorld = true; }
            return _world;
        }
    }
    /// <summary>Gets or selects the borrowed world supplying this viewport's canvas and physics space.</summary>
    /// <value>An independent world capturing the project physics selection and startup fallback. Null assignment captures fresh defaults.</value>
    /// <remarks>Assign another viewport's world to share scene content and simulation. The current scene tree steps each
    /// selected runtime world once. Assignment preserves node RIDs and shape owners while moving bodies, areas and joints.
    /// Project setting changes affect later default worlds only. Explicit supplied worlds retain their own selection.
    /// This live association is not serialized. Disposing a wrapper while bound permits a replacement wrapper for the same identities.</remarks>
    /// <exception cref="InvalidOperationException">Access/mutation is off-owner, during capture/physics/native submission, or the world belongs to another scene tree.</exception>
    /// <exception cref="ArgumentException">The project physics implementation name is unknown when creating a default world.</exception>
    /// <exception cref="ObjectDisposedException">The viewport or supplied world is disposed.</exception>
    public World? World
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return LocalWorld; }
        set
        {
            EnsureMutable(); RenderingOwner?.EnsureViewportMutation(); Tree?.EnsureWorldBindingChange();
            if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value));
            if (ReferenceEquals(_world, value) && value is not null) return;
            var next = value ?? new World(WorldRuntime.CreateSceneDefault());
            if (Tree is { } tree) { next.Runtime.ValidateOwner(tree); tree.BindWorld(next.Runtime); }
            var old = _world; var owned = _ownsWorld; _world = next; _ownsWorld = value is null;
            if (_viewportRID.IsValid() && !ReferenceEquals(old?.Runtime, next.Runtime)) { old?.Runtime.Canvas.Attachments.Remove(_viewportRID); next.Runtime.Canvas.Attachments.Remove(_viewportRID); }
            try { if (Tree is { } active && !ReferenceEquals(old?.Runtime, next.Runtime)) active.RebindViewportWorld(this); }
            finally { if (owned) old?.Dispose(); }
        }
    }
    /// <summary>Returns the valid world supplying this viewport's canvas and physics space.</summary>
    /// <returns>The live local world; ordinary viewports always have an independent default or explicit shared world.</returns>
    /// <exception cref="InvalidOperationException">An attached viewport is accessed off-owner or shares a world with another scene tree.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public World? FindWorld() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); var world = LocalWorld; Tree?.BindWorld(world.Runtime); return world; }
    private void PublishWorldViewTransform() { if (_world is not null && _world.Runtime.Canvas.Attachments.TryGetValue(_viewportRID, out var view)) view.Transform = null; }
    private void ReleaseViewportWorld() { var world = _world; _world = null; if (_ownsWorld) world?.Dispose(); }
    private static readonly PropertyDescriptor[] WorldProperties =
        [new PropertyDescriptor<Viewport, World?>(nameof(World), n => n.World, (n, v) => n.World = v, _ => null, stored: false)];
}
