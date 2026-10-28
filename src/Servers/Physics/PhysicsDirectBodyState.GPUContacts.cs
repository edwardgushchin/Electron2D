namespace Electron2D;

public partial class PhysicsDirectBodyState
{
    private int[] _gpuContactSources = [];
    private long _gpuContactVersion;
    private int _gpuContactOffset, _gpuContactRows;
    private bool _gpuContactsPending;

    internal void BeginGPUContacts(long version, int offset, int rows)
    { _gpuContactVersion = version; _gpuContactOffset = offset; _gpuContactRows = rows; _gpuContactsPending = true; }
    internal void SetGPUContactSource(int slot, int source) => _gpuContactSources[slot] = source;
    private void MaterializeGPUContacts()
    {
        if (!_gpuContactsPending) return;
        _runtime.GPUContactDetailsRequested = true;
        ApplyGPUContactDetails(_gpuContactVersion, _space.ReadGPUContactRow(_gpuContactVersion, _gpuContactOffset, _gpuContactRows), _gpuContactOffset);
    }
    internal void ApplyGPUContactDetails(long version, ReadOnlySpan<GPUPhysicsBodyStore.ContactReport> reports, int offset = 0)
    {
        if (!_gpuContactsPending || version != _gpuContactVersion) return;
        for (var i = 0; i < _contactCount; i++)
        {
            ref readonly var report = ref reports[_gpuContactOffset - offset + _gpuContactSources[i]];
            // Ownership and logical slots were sampled with the keys, before subsequent authoring edits.
            _contacts[i] = _contacts[i] with
            {
                LocalPoint = report.Position,
                ColliderPoint = report.ColliderPosition,
                Normal = report.Normal,
                LocalVelocity = report.Velocity,
                ColliderVelocity = report.ColliderVelocity,
                Impulse = report.Impulse,
                Depth = report.Depth
            };
        }
        _gpuContactsPending = false;
    }
}
