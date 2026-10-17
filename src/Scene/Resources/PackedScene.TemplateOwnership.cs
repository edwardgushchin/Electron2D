using System.Runtime.ExceptionServices;

namespace Electron2D;

public sealed partial class PackedScene
{
    private ResourceFileOwnership? _capturedTemplateOwnership;
    private ResourceFileOwnership? RetainCapturedTemplates(PackedSceneData data)
    {
        var owners = new List<ResourceFileOwnership>();
        try
        {
            foreach (var scene in data.Nodes.Select(node => node.Instance).OfType<PackedScene>().Distinct<PackedScene>(ReferenceEqualityComparer.Instance))
            {
                if (ReferenceEquals(scene, this)) continue;
                if (ResourceLoader.RetainSceneTemplateOwner(scene) is { } owner) owners.Add(owner);
            }
            return owners.Count == 0 ? null : new ResourceFileOwnership([], owners.ToArray());
        }
        catch { foreach (var owner in owners) owner.ReleaseOwner(); throw; }
    }
    internal override ResourceFileOwnership? RetainFileOwnership()
    {
        lock (_gate)
        {
            var file = base.RetainFileOwnership();
            if (_capturedTemplateOwnership is null) return file;
            var captured = _capturedTemplateOwnership.RetainOwner();
            return file is null ? captured : new ResourceFileOwnership([], [file, captured]);
        }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        Exception? release = null;
        if (disposing)
        {
            ResourceFileOwnership? captured;
            lock (_gate) { captured = _capturedTemplateOwnership; _capturedTemplateOwnership = null; }
            try { captured?.ReleaseOwner(); } catch (Exception error) { release = error; }
        }
        try { base.Dispose(disposing); }
        catch (Exception error) { if (release is not null) throw new AggregateException(release, error); throw; }
        if (release is not null) ExceptionDispatchInfo.Capture(release).Throw();
    }
}
