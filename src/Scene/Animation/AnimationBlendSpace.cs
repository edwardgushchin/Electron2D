using System.Globalization;

namespace Electron2D;

/// <summary>Selects interpolation or nearest-point playback in either animation blend space.</summary>
public enum AnimationBlendMode
{
    /// <summary>Interpolate adjacent points.</summary>
    Interpolated = 0,
    /// <summary>Play only the nearest point, retaining its independent clock.</summary>
    Discrete = 1,
    /// <summary>Play the nearest point and carry the previous selected timeline on changes.</summary>
    DiscreteCarry = 2,
}
/// <summary>Selects inactive-clock and cycle-length synchronization in either animation blend space.</summary>
public enum AnimationSyncMode
{
    /// <summary>Freeze inactive points.</summary>
    None = 0,
    /// <summary>Advance all points at their independent rate.</summary>
    Independent = 1,
    /// <summary>Scale each clip delta to the weighted active timeline length.</summary>
    CyclicMutable = 2,
    /// <summary>Scale each clip delta to a configured common cycle length.</summary>
    CyclicConstant = 3,
}
internal sealed class AnimationBlendPoint<T>(AnimationRootNode node, T position, string name)
{ internal AnimationRootNode Node = node; internal T Position = position; internal string Name = name; }
internal sealed class AnimationBlendPoints<T> : IDisposable
{
    internal readonly List<AnimationBlendPoint<T>> Items = [];
    private readonly AnimationNode _owner;
    private readonly Action<Resource> _changed;
    private readonly Action<ulong, string> _removed;
    private readonly Action<ulong, string, string> _renamed;
    internal AnimationBlendPoints(AnimationNode owner) { _owner = owner; _changed = _ => { if (!owner.IsDisposed) owner.EmitGraphChanged(); }; _removed = owner.Removed; _renamed = owner.Renamed; }
    internal int Add(AnimationRootNode node, T position, int index, string name)
    {
        ArgumentNullException.ThrowIfNull(node); ObjectDisposedException.ThrowIf(node.IsDisposed, node);
        if (Items.Count == 64) throw new InvalidOperationException("A blend space has at most 64 points.");
        if (index < -1 || index > Items.Count) throw new ArgumentOutOfRangeException(nameof(index));
        if (AnimationNodeBlendTree.Contains(node, _owner, new HashSet<AnimationNode>(ReferenceEqualityComparer.Instance))) throw new ArgumentException("Blend point would contain a resource cycle.", nameof(node));
        index = index == -1 ? Items.Count : index;
        name = UniqueName(name, index); Items.Insert(index, new(node, position, name)); Attach(node); return index;
    }
    internal string UniqueName(string name, int index, AnimationBlendPoint<T>? except = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name.Length == 0) { do { name = (index++).ToString(CultureInfo.InvariantCulture); } while (Find(name) >= 0); return name; }
        if (name.Contains('/') || name.Contains('.')) throw new ArgumentException("Blend point names cannot contain slash or dot.", nameof(name));
        var stem = name; var suffix = 2; while (Items.Any(p => p != except && p.Name == name)) name = stem + " " + (suffix++).ToString(CultureInfo.InvariantCulture); return name;
    }
    internal int Find(string name) { ArgumentNullException.ThrowIfNull(name); for (var i = 0; i < Items.Count; i++) if (Items[i].Name == name) return i; return -1; }
    internal void SetNode(int index, AnimationRootNode node)
    {
        ArgumentNullException.ThrowIfNull(node); ObjectDisposedException.ThrowIf(node.IsDisposed, node); var point = Items[index];
        if (AnimationNodeBlendTree.Contains(node, _owner, new HashSet<AnimationNode>(ReferenceEqualityComparer.Instance))) throw new ArgumentException("Blend point would contain a resource cycle.", nameof(node));
        Detach(point.Node); point.Node = node; Attach(node);
    }
    internal AnimationBlendPoint<T> Remove(int index) { var point = Items[index]; Items.RemoveAt(index); Detach(point.Node); return point; }
    private void Attach(AnimationRootNode node) { node.Changed += _changed; node.AnimationNodeRemoved += _removed; node.AnimationNodeRenamed += _renamed; }
    private void Detach(AnimationRootNode node) { node.Changed -= _changed; node.AnimationNodeRemoved -= _removed; node.AnimationNodeRenamed -= _renamed; }
    internal IEnumerable<KeyValuePair<string, AnimationNode>> Children() { foreach (var point in Items) yield return new(point.Name, point.Node); }
    internal void CopyTo(AnimationBlendPoints<T> target, bool deep, Func<Resource?, Resource?> duplicate) { foreach (var point in Items) target.Add(deep ? (AnimationRootNode)duplicate(point.Node)! : point.Node, point.Position, -1, point.Name); }
    public void Dispose() { foreach (var point in Items) Detach(point.Node); Items.Clear(); }
}
internal static class AnimationBlendSpacePlayback<T>
{
    internal static readonly AnimationParameter<AnimationBlendPoint<T>?> Closest = new("closest", null, true);
    internal static void Validate(List<AnimationBlendPoint<T>> points, AnimationSyncMode sync) { if (sync >= AnimationSyncMode.CyclicMutable) foreach (var point in points) if (point.Node is not AnimationNodeAnimation) throw new InvalidOperationException("Cyclic synchronization requires clip-leaf points."); }
    internal static double Process(AnimationNode owner, List<AnimationBlendPoint<T>> points, Span<double> weights, int closest, AnimationBlendMode mode, AnimationSyncMode sync, double cycleLength, bool singlePass = false)
    {
        var c = owner.Current(); var test = c.TestOnly; var selected = default(AnimationGraphTime);
        Validate(points, sync);
        if (singlePass) { selected = c.Tree.BlendChild(c, c.Instance.Children[points[0].Name], c.Time, c.Seek, c.External, 1, AnimationNode.FilterAction.Ignore, true, test); c.Result = selected; c.HasTime = true; return selected.Remaining; }
        Span<double> deltas = stackalloc double[64];
        if (sync >= AnimationSyncMode.CyclicMutable)
        {
            double target = 0, total = 0;
            for (var i = 0; i < points.Count; i++) { var length = ((AnimationNodeAnimation)points[i].Node).GraphLength(c.Tree); deltas[i] = length; if (weights[i] > 0 && length > 1e-5) { target += weights[i] * length; total += weights[i]; } }
            if (sync == AnimationSyncMode.CyclicMutable) { if (total > 1e-5) target /= total; } else target = cycleLength;
            for (var i = 0; i < points.Count; i++) deltas[i] = target > 1e-5 ? c.Delta * deltas[i] / target : 0;
        }
        else for (var i = 0; i < points.Count; i++) deltas[i] = sync == AnimationSyncMode.None && weights[i] < 1e-5 ? double.NaN : c.Delta;
        var previous = owner.GetParameter(Closest); var priorIndex = previous is null ? -1 : points.IndexOf(previous);
        if (mode == AnimationBlendMode.DiscreteCarry && sync < AnimationSyncMode.CyclicMutable && priorIndex >= 0 && priorIndex != closest)
        {
            var from = c.Instance.Children[points[priorIndex].Name]; var to = c.Instance.Children[points[closest].Name];
            if (from.Definition is AnimationNodeAnimation && to.Definition is AnimationNodeAnimation) AnimationNodeAnimation.CopyBackward(from, to, test);
            var advanced = c.Tree.BlendChild(c, from, c.Delta, false, c.External, 0, AnimationNode.FilterAction.Ignore, true, true);
            selected = c.Tree.BlendChild(c, to, advanced.Position, true, c.External, 1, AnimationNode.FilterAction.Ignore, true, test, c.Delta); deltas[closest] = double.NaN;
        }
        for (var i = 0; i < points.Count; i++)
        {
            if (double.IsNaN(deltas[i])) continue;
            Animation.Finite(deltas[i]); var info = c.Tree.BlendChild(c, c.Instance.Children[points[i].Name], c.Seek ? c.Time : deltas[i], c.Seek, c.External, weights[i], AnimationNode.FilterAction.Ignore, true, test, deltas[i]);
            if (i == closest) selected = info;
        }
        owner.SetParameter(Closest, points[closest]); c.Result = selected; c.HasTime = true; return selected.Remaining;
    }
}
