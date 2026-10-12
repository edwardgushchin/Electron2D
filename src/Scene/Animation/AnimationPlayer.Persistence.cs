namespace Electron2D;

public partial class AnimationPlayer
{
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory()
    {
        if (GetType() != typeof(AnimationPlayer)) return base.CreateSceneInstanceFactory();
        return CreateFilePlayer;
    }
    private static Node CreateFilePlayer() => new AnimationPlayer();
    internal override void ValidateSceneInstance()
    {
        // Schema-only archive preparation has no libraries; validate references after complete scene restoration.
        base.ValidateSceneInstance(); GetFileNext(); ValidateFilePairs(GetFileBlendPairs(), false, true);
        foreach (var duration in _blendTimes.Values)
            if (!double.IsFinite(duration) || duration <= 0) throw new InvalidDataException("Incomplete or invalid stored blend duration.");
    }
    private IEnumerable<PropertyDescriptor> GetFileTransitionProperties()
    {
        yield return new PropertyDescriptor<AnimationPlayer, string[]>("NextAnimations", n => n.GetFileNext(), (n, v) => n.SetFileNext(v), stored: true);
        yield return new PropertyDescriptor<AnimationPlayer, string[]>("BlendPairs", n => n.GetFileBlendPairs(), (n, v) => n.SetFileBlendPairs(v), stored: true);
        var pairs = GetFileBlendPairs();
        for (var i = 0; i < pairs.Length; i += 2)
        {
            var from = pairs[i]; var to = pairs[i + 1];
            yield return new PropertyDescriptor<AnimationPlayer, double>($"Blend[{i / 2}]", n => n.GetFileBlendTime(from, to), (n, v) => n.SetFileBlendTime(from, to, v), stored: true);
        }
    }
    private string[] GetFileNext()
    {
        var pairs = _next.OrderBy(p => p.Key, StringComparer.Ordinal).SelectMany(p => new[] { p.Key, p.Value }).ToArray();
        ValidateFilePairs(pairs, true, true); return pairs;
    }
    private string[] GetFileBlendPairs()
    {
        var pairs = _blendTimes.Keys.OrderBy(p => p.From, StringComparer.Ordinal).ThenBy(p => p.To, StringComparer.Ordinal).SelectMany(p => new[] { p.From, p.To }).ToArray();
        ValidateFilePairs(pairs, false); return pairs;
    }
    private double GetFileBlendTime(string from, string to)
    {
        RequireAnimation(from); RequireAnimation(to); return GetBlendTime(from, to);
    }
    private void SetFileBlendTime(string from, string to, double seconds)
    {
        if (!double.IsFinite(seconds) || seconds <= 0) throw new InvalidDataException("Stored blend duration must be positive and finite.");
        EnsureAnimationMutable(); _blendTimes[(from, to)] = seconds;
    }
    private void ValidateFilePairs(string[] pairs, bool uniqueSources, bool requireAnimations = false)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        if (pairs.Length > 16384 || pairs.Length % 2 != 0) throw new InvalidDataException("Invalid animation transition pairs.");
        var seen = new HashSet<(string, string)>(); var sources = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < pairs.Length; i += 2)
        {
            if (pairs[i] is null || pairs[i + 1] is null || pairs[i].Length == 0 || pairs[i + 1].Length == 0 ||
                (requireAnimations && (!HasAnimation(pairs[i]) || !HasAnimation(pairs[i + 1]))) ||
                !seen.Add((pairs[i], pairs[i + 1])) || (uniqueSources && !sources.Add(pairs[i])))
                throw new InvalidDataException("Unknown or repeated animation transition.");
        }
    }
    private void SetFileNext(string[] pairs)
    {
        EnsureAnimationMutable(); ValidateFilePairs(pairs, true);
        _next.Clear(); for (var i = 0; i < pairs.Length; i += 2) _next.Add(pairs[i], pairs[i + 1]);
    }
    private void SetFileBlendPairs(string[] pairs)
    {
        EnsureAnimationMutable(); ValidateFilePairs(pairs, false);
        var sorted = pairs.Chunk(2).OrderBy(p => p[0], StringComparer.Ordinal).ThenBy(p => p[1], StringComparer.Ordinal).SelectMany(p => p).ToArray();
        if (!pairs.SequenceEqual(sorted, StringComparer.Ordinal)) throw new InvalidDataException("Blend pairs must use ordinal order.");
        _blendTimes.Clear(); for (var i = 0; i < pairs.Length; i += 2) _blendTimes.Add((pairs[i], pairs[i + 1]), 0);
        NotifyPropertyListChanged();
    }
}
