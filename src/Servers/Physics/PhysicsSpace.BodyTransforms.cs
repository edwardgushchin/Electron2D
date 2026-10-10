namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private PhysicsBodyRuntime?[] _transformBodies = [];
    private Transform[] _transformResults = [];
    private GPUPhysicsBodyStore.BodyHandle[] _transformHandles = [];
    private System.Numerics.Vector4[] _transformPoses = [];
    private int[] _transformIndices = [];
    private bool _readingBodyTransforms;

    internal void ReadBodyTransforms(ReadOnlySpan<RID> bodies, Span<Transform> results)
    {
        EnsureQueryAccess();
        if (_readingBodyTransforms) throw new InvalidOperationException("A body transform batch is already active in this space.");
        _readingBodyTransforms = true;
        try
        {
            if (_transformBodies.Length < bodies.Length)
            {
                var capacity = Math.Max(8, bodies.Length);
                _transformBodies = new PhysicsBodyRuntime[capacity]; _transformResults = new Transform[capacity];
                _transformHandles = new GPUPhysicsBodyStore.BodyHandle[capacity]; _transformPoses = new System.Numerics.Vector4[capacity]; _transformIndices = new int[capacity];
            }
            for (var i = 0; i < bodies.Length; i++)
            {
                var runtime = PhysicsServer.Service.BodyRuntime(bodies[i]); runtime.PrepareForceAccess(false);
                if (!ReferenceEquals(runtime.Space, this)) throw new ArgumentException("Every requested body must belong to the selected space.", nameof(bodies));
                _transformBodies[i] = runtime;
            }
            var count = 0;
            for (var i = 0; i < bodies.Length; i++)
            {
                var runtime = _transformBodies[i]!; var owner = runtime.Owners;
                if (GPUStore is not null && owner.Server is { Mode: not PhysicsServer.BodyMode.Static })
                {
                    _transformHandles[count] = runtime.Backend.GPUHandle; _transformIndices[count++] = i;
                }
                else _transformResults[i] = runtime.GetTransform();
            }
            if (count > 0)
            {
                FlushGPUWakes(); GPUStore!.ReadPoses(_transformHandles.AsSpan(0, count), _transformPoses.AsSpan(0, count));
                for (var i = 0; i < count; i++)
                {
                    var at = _transformIndices[i]; var runtime = _transformBodies[at]!;
                    runtime.Backend.AcceptGPUPose(_transformPoses[i]); _transformResults[at] = runtime.GetTransform();
                }
            }
            _transformResults.AsSpan(0, bodies.Length).CopyTo(results);
        }
        finally { Array.Clear(_transformBodies, 0, Math.Min(bodies.Length, _transformBodies.Length)); _readingBodyTransforms = false; }
    }
}
