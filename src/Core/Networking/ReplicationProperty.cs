namespace Electron2D;

/// <summary>Identifies one explicitly typed, encoded scene property without reflection.</summary>
/// <remarks>IDs are nonzero and unique within a configuration. Participants agree on codecs/IDs. NodePath locates
/// a node relative to the synchronized root; direct getter/setter delegates identify its concrete property.</remarks>
public abstract class ReplicationProperty
{
    private protected ReplicationProperty(uint id, string nodePath, int maxEncodedBytes)
    { if (id == 0) throw new ArgumentOutOfRangeException(nameof(id)); ArgumentException.ThrowIfNullOrEmpty(nodePath); if (nodePath.StartsWith('/') || nodePath.Contains('\0')) throw new ArgumentException("Property node paths must be relative.", nameof(nodePath)); if (maxEncodedBytes is < 0 or > 67108864) throw new ArgumentOutOfRangeException(nameof(maxEncodedBytes)); ID = id; NodePath = nodePath; MaxEncodedBytes = maxEncodedBytes; }
    /// <summary>Gets the stable per-configuration wire identity.</summary>
    public uint ID { get; }
    /// <summary>Gets the relative node path, initially supplied by the application.</summary>
    public string NodePath { get; }
    /// <summary>Gets the finite maximum encoded value size prepared by a synchronizer.</summary>
    public int MaxEncodedBytes { get; }
    internal abstract BoundReplicationProperty Bind(Node root);
}
/// <summary>Pairs a concrete Node property with direct typed accessors and an explicit wire codec.</summary>
/// <typeparam name="TNode">Target Node subtype.</typeparam><typeparam name="T">Concrete property value; process-local engine objects/RIDs cannot be synchronized.</typeparam>
public sealed class ReplicationProperty<TNode, T> : ReplicationProperty where TNode : Node
{
    private readonly Func<TNode, T> _get;
    private readonly Action<TNode, T> _set;
    private readonly Func<T, int> _size;
    private readonly RPCEncoder<T> _encode;
    private readonly RPCDecoder<T> _decode;
    /// <summary>Creates an immutable property/codec descriptor.</summary><param name="id">Stable nonzero wire identity.</param><param name="get">Direct getter.</param><param name="set">Direct setter.</param><param name="size">Exact encoded size.</param><param name="encode">Writes exactly the measured bytes.</param><param name="decode">Validates complete payload, returning a concrete value.</param><param name="maxEncodedBytes">Preparation budget for this value.</param><param name="nodePath">Relative node path; dot selects the synchronized root.</param>
    public ReplicationProperty(uint id, Func<TNode, T> get, Action<TNode, T> set, Func<T, int> size, RPCEncoder<T> encode, RPCDecoder<T> decode, int maxEncodedBytes = 65535, string nodePath = ".") : base(id, nodePath, maxEncodedBytes)
    { if (typeof(ElectronObject).IsAssignableFrom(typeof(T)) || typeof(T) == typeof(RID)) throw new NotSupportedException("Process-local objects and RIDs cannot be replicated."); ArgumentNullException.ThrowIfNull(get); ArgumentNullException.ThrowIfNull(set); ArgumentNullException.ThrowIfNull(size); ArgumentNullException.ThrowIfNull(encode); ArgumentNullException.ThrowIfNull(decode); _get = get; _set = set; _size = size; _encode = encode; _decode = decode; }
    internal override BoundReplicationProperty Bind(Node root)
    { var node = root.GetNode(NodePath); if (node is not TNode target || !ReferenceEquals(node, root) && !root.IsAncestorOf(node)) throw new ArgumentException("Replication property target must be a compatible node in the root subtree."); return new BoundReplicationProperty<TNode, T>(this, target, _get, _set, _size, _encode, _decode); }
}
internal abstract class BoundReplicationProperty(ReplicationProperty property)
{
    internal readonly ReplicationProperty Property = property;
    internal readonly byte[] Current = new byte[property.MaxEncodedBytes], Previous = new byte[property.MaxEncodedBytes];
    internal int Length, PreviousLength = -1, Index;
    internal bool Spawn = true;
    internal ReplicationMode Mode = ReplicationMode.Always;
    internal uint Revision;
    internal bool Staged;
    internal abstract void Capture();
    internal abstract void Decode(ReadOnlySpan<byte> input);
    internal abstract void Apply();
    internal void Watch() { Capture(); if (PreviousLength == Length && Current.AsSpan(0, Length).SequenceEqual(Previous.AsSpan(0, Length))) return; Current.AsSpan(0, Length).CopyTo(Previous); PreviousLength = Length; Revision++; }
}
internal sealed class BoundReplicationProperty<TNode, T>(ReplicationProperty property, TNode node, Func<TNode, T> get, Action<TNode, T> set, Func<T, int> size, RPCEncoder<T> encode, RPCDecoder<T> decode) : BoundReplicationProperty(property) where TNode : Node
{
    private T _decoded = default!;
    internal override void Capture() { ObjectDisposedException.ThrowIf(node.IsDisposed, node); var value = get(node); var count = size(value); if ((uint)count > (uint)Current.Length) throw new ArgumentException("Replication value exceeds its declared encoded budget."); if (encode(value, Current.AsSpan(0, count)) != count) throw new InvalidDataException("Property codec did not write its declared size."); Length = count; }
    internal override void Decode(ReadOnlySpan<byte> input) { if (input.Length > Current.Length) throw new InvalidDataException("Property payload exceeds its declared budget."); _decoded = decode(input); Staged = true; }
    internal override void Apply() { if (!Staged) return; Staged = false; var value = _decoded; _decoded = default!; ObjectDisposedException.ThrowIf(node.IsDisposed, node); set(node, value); }
}
