namespace Electron2D;

/// <summary>Retains nested contact-pair staging and validates a complete result before caller output changes.</summary>
internal sealed class PhysicsShapePairBuffers
{
    private readonly List<Vector2[]> _frames = [];
    private int _depth;
    internal static void ValidateCapacity(int length)
    {
        if ((length & 1) != 0) throw new ArgumentException("Contact output must contain complete point pairs.", nameof(length));
    }
    internal Span<Vector2> Begin(int length)
    {
        ValidateCapacity(length);
        if (_frames.Count == _depth) _frames.Add([]);
        var scratch = _frames[_depth];
        if (scratch.Length < length) _frames[_depth] = scratch = new Vector2[length];
        _depth++; return scratch.AsSpan(0, length);
    }
    internal void End() => _depth--;
    internal void Clear() => _frames.Clear();
    internal static void Commit(bool hit, int count, ReadOnlySpan<Vector2> scratch, Span<Vector2> contacts)
    {
        if ((uint)count > (uint)(scratch.Length / 2) || !hit && count != 0)
            throw new InvalidOperationException("A shape-pair hook returned an invalid contact count.");
        var used = scratch[..(count * 2)];
        foreach (var point in used)
            if (!point.IsFinite()) throw new InvalidOperationException("A shape-pair hook returned a nonfinite contact point.");
        used.CopyTo(contacts);
    }
}
