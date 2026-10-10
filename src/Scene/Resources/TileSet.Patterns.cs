namespace Electron2D;

public sealed partial class TileSet
{
    private readonly List<TileMapPattern> _patterns = [];
    /// <summary>Inserts a borrowed nonempty pattern; any negative index appends.</summary>
    /// <param name="pattern">Live pattern; the same instance may belong to other sets.</param>
    /// <param name="index">Insertion position, or a negative value to append.</param><returns>The inserted index.</returns>
    /// <exception cref="ArgumentException">The pattern is empty or already belongs to this list.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index exceeds the list size.</exception>
    public int AddPattern(TileMapPattern pattern, int index = -1)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(pattern); ObjectDisposedException.ThrowIf(pattern.IsDisposed, pattern);
        if (pattern.IsEmpty() || _patterns.Contains(pattern)) throw new ArgumentException("A nonempty distinct pattern is required.", nameof(pattern));
        if (index < 0) index = _patterns.Count; if (index > _patterns.Count) throw new ArgumentOutOfRangeException(nameof(index));
        _patterns.Insert(index, pattern); pattern.Disposed += PatternDisposed; PatternsChanged(); return index;
    }
    /// <summary>Returns the borrowed pattern at an explicit zero-based index.</summary>
    /// <param name="index">Pattern index; the default minus one is invalid.</param><returns>The borrowed pattern.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the list.</exception>
    public TileMapPattern GetPattern(int index = -1)
    { ThrowIfDisposed(); if ((uint)index >= (uint)_patterns.Count) throw new ArgumentOutOfRangeException(nameof(index)); return _patterns[index]; }
    /// <summary>Removes a pattern reference without disposing the resource.</summary>
    /// <param name="index">Zero-based pattern index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the list.</exception>
    public void RemovePattern(int index) { var pattern = GetPattern(index); _patterns.RemoveAt(index); pattern.Disposed -= PatternDisposed; PatternsChanged(); }
    /// <summary>Returns the number of stored pattern references.</summary><returns>The list size.</returns>
    public int GetPatternsCount() { ThrowIfDisposed(); return _patterns.Count; }
    private void PatternDisposed(ElectronObject pattern)
    { var index = _patterns.FindIndex(p => ReferenceEquals(p, pattern)); if (index >= 0) RemovePattern(index); }
    private void ClearPatterns() { foreach (var pattern in _patterns) pattern.Disposed -= PatternDisposed; _patterns.Clear(); }
    private void RestorePatterns(TileMapPattern[] patterns)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(patterns);
        var snapshot = (TileMapPattern[])patterns.Clone(); var seen = new HashSet<TileMapPattern>();
        foreach (var pattern in snapshot) { ArgumentNullException.ThrowIfNull(pattern); ObjectDisposedException.ThrowIf(pattern.IsDisposed, pattern); if (!seen.Add(pattern)) throw new ArgumentException("Duplicate pattern reference.", nameof(patterns)); }
        ClearPatterns(); foreach (var pattern in snapshot) { _patterns.Add(pattern); pattern.Disposed += PatternDisposed; }
        PatternsChanged();
    }
    private void PatternsChanged() { try { NotifyPropertyListChanged(); } finally { EmitChanged(); } }
    private IEnumerable<PropertyDescriptor> PatternProperties()
    { yield return new PropertyDescriptor<TileSet, TileMapPattern[]>("Patterns", n => n._patterns.ToArray(), (n, v) => n.RestorePatterns(v), _ => [], stored: true); }
    internal Vector2i PatternOffset(Vector2i origin, Vector2i coords, bool inverse = false)
    {
        ThrowIfDisposed(); long x = inverse ? (long)coords.X - origin.X : coords.X, y = inverse ? (long)coords.Y - origin.Y : coords.Y;
        if (_tileShape != TileShape.Square && _tileLayout is TileLayout.Stacked or TileLayout.StackedOffset)
        {
            var delta = (_tileLayout == TileLayout.Stacked ? 1 : -1) * (inverse ? -1 : 1);
            if (_tileOffsetAxis == TileOffsetAxis.Horizontal && (origin.Y & 1) != 0 && (y & 1) != 0) x += delta;
            else if (_tileOffsetAxis == TileOffsetAxis.Vertical && (origin.X & 1) != 0 && (x & 1) != 0) y += delta;
        }
        if (!inverse) { x += origin.X; y += origin.Y; }
        return new(checked((int)x), checked((int)y));
    }
}
