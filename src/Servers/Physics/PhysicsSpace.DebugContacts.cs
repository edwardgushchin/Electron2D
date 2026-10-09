using Box2D.NET;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private Vector2[] _debugContacts = [];
    private int _debugContactLimit, _debugContactCount;
    internal ulong DebugContactRevision { get; private set; }
    internal int DebugContactLimit => _debugContactLimit;
    internal ReadOnlySpan<Vector2> DebugContacts { get { EnsureQueryAccess(); return _debugContacts.AsSpan(0, _debugContactCount); } }

    internal void SetDebugContacts(int limit)
    {
        EnsureReleaseAccess();
        if (limit < 0 || limit > (int.MaxValue - 8) / 8) throw new ArgumentOutOfRangeException(nameof(limit));
        if (_debugContactLimit == limit) return;
        var next = _debugContacts.Length < limit ? new Vector2[limit] : _debugContacts;
        if (limit > 0) EnsureQueryAccess();
        GPUStore?.PrepareDebugContacts(limit);
        _debugContacts = next; _debugContactLimit = limit; ResetDebugContacts();
    }

    private void ResetDebugContacts() { _debugContactCount = 0; DebugContactRevision++; }
    private void CaptureDebugContacts()
    {
        if (_debugContactLimit == 0) return;
        if (GPUStore is not null) { _debugContactCount = GPUStore.ReadDebugContacts(_debugContacts.AsSpan(0, _debugContactLimit)); return; }
        var world = b2GetWorldFromId(_worldID);
        foreach (var color in world.constraintGraph.colors)
            foreach (var contact in color.contactSims.data.AsSpan(0, color.contactSims.count))
            {
                for (var i = 0; i < contact.manifold.pointCount; i++)
                {
                    ref readonly var point = ref contact.manifold.points[i];
                    if (point.separation >= 0) continue;
                    var half = point.separation * .5f * contact.manifold.normal;
                    Add(point.point - half);
                    if (_debugContactCount == _debugContactLimit) return;
                    Add(point.point + half);
                    if (_debugContactCount == _debugContactLimit) return;
                }
            }
        void Add(B2Vec2 point)
        {
            var value = ToScene(point);
            if (!value.IsFinite()) throw new InvalidOperationException("Physics contact diagnostics contain a nonfinite point.");
            _debugContacts[_debugContactCount++] = value;
        }
    }
}
