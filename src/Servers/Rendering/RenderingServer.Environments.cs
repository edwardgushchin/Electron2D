using System.Runtime.CompilerServices;

namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly Dictionary<RID, CanvasEnvironment> _ownedEnvironments = [];
    private ConditionalWeakTable<WorldRuntime, WeakReference<CanvasEnvironment>>? _worldEnvironmentOverrides;

    /// <summary>Creates a caller-owned canvas environment in the active rendering session.</summary>
    /// <returns>A live identity that can be bound to a world and released with <see cref="FreeRID"/>.</returns>
    /// <exception cref="InvalidOperationException">The rendering service is unavailable, access is off-owner or a submission is in progress.</exception>
    public static RID EnvironmentCreate()
    {
        var server = RequireService();
        server.EnsureServerViewportChange();
        var environment = new CanvasEnvironment();
        var rid = environment.GetRID();
        try { server._ownedEnvironments.Add(rid, environment); }
        catch { environment.Dispose(); throw; }
        return rid;
    }

    /// <summary>Selects an overriding canvas environment for a live world.</summary>
    /// <param name="worldCanvas">The borrowed <see cref="World.Canvas"/> identity of the destination world.</param>
    /// <param name="environment">A live environment identity or an empty RID to restore scene selection.</param>
    /// <remarks>All viewports using the world see the override when their environment mode is enabled.
    /// The binding lasts for the rendering session and borrows the environment. Freeing or disposing it
    /// restores scene selection. The override is runtime state and is not saved to a scene.</remarks>
    /// <exception cref="ArgumentException">An identity is invalid or the destination is not a world canvas.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable, the world belongs to another
    /// scene owner, access is off-owner or a submission is in progress.</exception>
    public static void WorldSetEnvironment(RID worldCanvas, RID environment)
    {
        var server = RequireService();
        server.EnsureServerViewportChange();
        var canvas = RenderingCanvasRegistry.Resolve(worldCanvas);
        if (canvas.World is null || !canvas.World.TryGetTarget(out var world) || !world.Alive)
            throw new ArgumentException("The canvas does not identify a live world.", nameof(worldCanvas));
        if (world.SceneOwner is { } owner && !ReferenceEquals(owner, server.CanvasSceneTree))
            throw new InvalidOperationException("The world belongs to another scene owner.");
        if (environment.IsValid())
        {
            var resource = RenderingProgramRegistry<CanvasEnvironment>.Resolve(environment);
            (server._worldEnvironmentOverrides ??= new()).AddOrUpdate(world, new(resource));
        }
        else server._worldEnvironmentOverrides?.Remove(world);
    }

    private bool ReleaseEnvironmentRID(RID rid)
    {
        if (!_ownedEnvironments.TryGetValue(rid, out var environment))
        {
            if (RenderingProgramRegistry<CanvasEnvironment>.Contains(rid))
                throw new InvalidOperationException("Borrowed environments cannot be freed by the renderer.");
            return false;
        }

        EnsureServerViewportChange();
        _ownedEnvironments.Remove(rid);
        environment.Dispose();
        return true;
    }

    private void ReleaseOwnedEnvironments()
    {
        _worldEnvironmentOverrides = null;
        foreach (var environment in _ownedEnvironments.Values) environment.Dispose();
        _ownedEnvironments.Clear();
    }
}
