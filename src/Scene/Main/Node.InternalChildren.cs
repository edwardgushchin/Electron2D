using System.Collections;

namespace Electron2D;

public partial class Node
{
    /// <summary>Places an implementation child before or after the ordinary children.</summary>
    public enum InternalMode
    {
        /// <summary>Includes the child in ordinary child queries and scene capture.</summary>
        Disabled = 0,
        /// <summary>Places a hidden implementation child before ordinary children.</summary>
        Front = 1,
        /// <summary>Places a hidden implementation child after ordinary children.</summary>
        Back = 2
    }

    private InternalMode _internalMode;
    private int _internalFrontCount, _internalBackCount;
    private readonly IReadOnlyList<Node> _publicChildrenView;
    internal bool IsInternalChild => _internalMode != InternalMode.Disabled;
    internal IReadOnlyList<Node> AllChildren => _childrenView;
    private int ExternalChildCount => _children.Count - _internalFrontCount - _internalBackCount;

    /// <summary>Gets the number of direct children in the selected view.</summary>
    /// <param name="includeInternal">Whether implementation children are counted; false by default.</param>
    /// <returns>The ordinary child count or complete child count.</returns>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public int GetChildCount(bool includeInternal = false)
    {
        ThrowIfDisposed(); return includeInternal ? _children.Count : ExternalChildCount;
    }

    /// <summary>Returns a caller-owned snapshot of direct children in scene order.</summary>
    /// <param name="includeInternal">Whether front/back implementation children are included; false by default.</param>
    /// <returns>A new array containing borrowed node identities.</returns>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Node[] GetChildren(bool includeInternal = false)
    {
        ThrowIfDisposed();
        if (includeInternal) return _children.ToArray();
        var children = new Node[ExternalChildCount];
        _children.CopyTo(_internalFrontCount, children, 0, children.Length);
        return children;
    }

    private int ChildInsertionIndex(InternalMode mode) => mode switch
    {
        InternalMode.Front => _internalFrontCount,
        InternalMode.Back => _children.Count,
        _ => _children.Count - _internalBackCount
    };
    private void ChangeInternalChildCount(InternalMode mode, int delta)
    {
        if (mode == InternalMode.Front) _internalFrontCount += delta;
        else if (mode == InternalMode.Back) _internalBackCount += delta;
    }

    private sealed class OrdinaryChildrenView(Node owner) : IReadOnlyList<Node>
    {
        public int Count => owner.ExternalChildCount;
        public Node this[int index] => (uint)index < (uint)Count ? owner._children[owner._internalFrontCount + index] : throw new ArgumentOutOfRangeException(nameof(index));
        public IEnumerator<Node> GetEnumerator()
        {
            for (var index = 0; index < Count; index++) yield return this[index];
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
