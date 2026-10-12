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
        EnsureImplementationWriteAccess();
        if (limit < 0 || limit > (int.MaxValue - 8) / 8) throw new ArgumentOutOfRangeException(nameof(limit));
        if (_debugContactLimit == limit) return;
        var next = _debugContacts.Length < limit ? new Vector2[limit] : _debugContacts;
        if (limit > 0) EnsureQueryAccess();
        _backend.SetDebugContacts(limit);
        _debugContacts = next; _debugContactLimit = limit; ResetDebugContacts();
    }

    private void ResetDebugContacts() { _debugContactCount = 0; DebugContactRevision++; }
    internal void CaptureDebugContacts()
    {
        if (_debugContactLimit == 0) return;
        _debugContactCount = _backend.ReadDebugContacts(_debugContacts.AsSpan(0, _debugContactLimit));
    }
    internal void AddDebugContact(Vector2 value)
    {
        if (_debugContactCount == _debugContactLimit) return;
        if (!value.IsFinite()) throw new InvalidOperationException("Physics contact diagnostics contain a nonfinite point.");
        _debugContacts[_debugContactCount++] = value;
    }
}
