namespace Electron2D;

public partial class Node
{
    // Mandatory membership cleanup also runs after public mutation has closed for disposal.
    internal void SetLifecycleGroup(string group, bool member)
    {
        if (member) _groups.TryAdd(group, false); else _groups.Remove(group);
    }
    internal bool IsGreaterThanCore(Node node)
    {
        if (ReferenceEquals(this, node)) return false;

        var left = this; var right = node;
        var leftDepth = 0; var rightDepth = 0;
        for (var at = Parent; at is not null; at = at.Parent) leftDepth++;
        for (var at = node.Parent; at is not null; at = at.Parent) rightDepth++;
        var descendant = leftDepth > rightDepth;
        while (leftDepth > rightDepth) { left = left.Parent!; leftDepth--; }
        while (rightDepth > leftDepth) { right = right.Parent!; rightDepth--; }
        if (ReferenceEquals(left, right)) return descendant;
        while (!ReferenceEquals(left.Parent, right.Parent)) { left = left.Parent!; right = right.Parent!; }
        return left.Parent!._children.IndexOf(left) > right.Parent!._children.IndexOf(right);
    }
}
