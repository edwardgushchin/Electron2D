namespace Electron2D;

internal readonly record struct CanvasInstance(Vector4 Basis, Vector4 Translation, Color Color, Color Custom)
{
    internal CanvasInstance(Transform pose, Color color, Color custom)
        : this(new(pose.X.X, pose.X.Y, pose.Y.X, pose.Y.Y), new(pose.Origin.X, pose.Origin.Y, 0, 0), color, custom) { }
}

public sealed partial class RenderingServer
{
    private List<CanvasInstance> _instances = [];
    private List<CanvasModelPose> _modelPoses = [];
    private Rect2? _appendedInstanceBounds;

    internal bool CanInstance(CanvasItem? owner) => _rendering && _backend is GPUCanvasBackend &&
        owner is not null && owner is not VisibleOnScreenNotifier && !IsCompositor(owner) && ParentGroup(owner) is null or CanvasGroup;

    internal bool TryAppendInstances(MultiMesh resource, Transform canvas, Color modulation, Rect2 bounds, float fraction, out int first, bool linear = false, bool recordBounds = true, bool models = false)
    {
        first = _instances.Count;
        Rect2? combined = null;
        _instances.EnsureCapacity(checked(first + resource.DrawCount));
        for (var i = 0; i < resource.DrawCount; i++)
        {
            resource.Presentation(i, fraction, out var pose, out var color, out var custom);
            if (models)
            {
                while (_modelPoses.Count <= first + i) _modelPoses.Add(default);
                _modelPoses[first + i] = new(pose);
            }
            pose = canvas * pose; color = (linear ? CanvasGeometry.LinearColor(color) : color) * modulation;
            var transformedBounds = pose * bounds;
            if (!pose.IsFinite() || !color.IsFinite() || !transformedBounds.IsFinite() || !transformedBounds.End.IsFinite())
            {
                _instances.RemoveRange(first, _instances.Count - first);
                return false;
            }
            _instances.Add(new(pose, color, custom));
            combined = combined is { } prior ? prior.Merge(transformedBounds) : transformedBounds;
        }
        if (recordBounds && combined is { } actual) _appendedInstanceBounds = _appendedInstanceBounds is { } prior ? prior.Merge(actual) : actual;
        return true;
    }
}
