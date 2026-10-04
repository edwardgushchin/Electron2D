namespace Electron2D;

/// <summary>Creates a detached node from a copied explicit spawn argument protocol.</summary>
/// <remarks>Immutable factory/codec configuration is borrowed by spawners. IDs/codecs must match on participants.
/// A factory must return a fresh live unparented node; ownership transfers to the spawn parent after insertion.</remarks>
public class SpawnFactory
{
    private readonly Func<Node>? _factory;
    /// <summary>Creates a parameterless custom spawn factory.</summary><param name="id">Nonzero protocol identity.</param><param name="factory">Fresh detached node constructor.</param>
    public SpawnFactory(uint id, Func<Node> factory) : this(id) { ArgumentNullException.ThrowIfNull(factory); _factory = factory; }
    private protected SpawnFactory(uint id) { if (id == 0) throw new ArgumentOutOfRangeException(nameof(id)); ID = id; }
    /// <summary>Gets the immutable protocol identity.</summary>
    public uint ID { get; }
    /// <summary>Creates and validates a new node from complete encoded arguments.</summary><param name="arguments">Copied/borrowed complete payload.</param><returns>A fresh unparented live node.</returns>
    public Node Instantiate(ReadOnlySpan<byte> arguments) { var node = Create(arguments) ?? throw new InvalidOperationException("Spawn factory returned null."); if (node.IsDisposed || node.Parent is not null || node.Tree is not null || node.IsQueuedForDeletion) throw new InvalidOperationException("Spawn factory must return a live detached unparented node."); return node; }
    private protected virtual Node Create(ReadOnlySpan<byte> arguments) { if (!arguments.IsEmpty) throw new InvalidDataException("Parameterless spawn factory cannot decode arguments."); return _factory!(); }
    internal virtual byte[] DefaultArguments() => [];
}
/// <summary>Creates custom nodes using an explicit concrete typed argument model and wire codec.</summary><typeparam name="T">Concrete arguments, including tuples/records.</typeparam>
public sealed class SpawnFactory<T> : SpawnFactory
{
    private readonly Func<T, Node> _factory;
    private readonly Func<T, int> _size;
    private readonly RPCEncoder<T> _encode;
    private readonly RPCDecoder<T> _decode;
    /// <summary>Creates immutable factory/codec configuration.</summary><param name="id">Nonzero protocol identity.</param><param name="factory">Fresh detached node factory.</param><param name="size">Complete encoded size.</param><param name="encode">Complete writer.</param><param name="decode">Validates complete payload and returns arguments.</param>
    public SpawnFactory(uint id, Func<T, Node> factory, Func<T, int> size, RPCEncoder<T> encode, RPCDecoder<T> decode) : base(id)
    { ArgumentNullException.ThrowIfNull(factory); ArgumentNullException.ThrowIfNull(size); ArgumentNullException.ThrowIfNull(encode); ArgumentNullException.ThrowIfNull(decode); _factory = factory; _size = size; _encode = encode; _decode = decode; }
    /// <summary>Copies arguments into their complete wire representation.</summary><param name="data">Concrete arguments.</param><returns>Copied payload up to 64 MiB, subject to the consuming transport budget.</returns>
    public byte[] Encode(T data) { var size = _size(data); if (size is < 0 or > 67108864) throw new ArgumentOutOfRangeException(nameof(data)); var bytes = new byte[size]; if (_encode(data, bytes) != size) throw new InvalidDataException("Spawn codec returned an unexpected size."); return bytes; }
    private protected override Node Create(ReadOnlySpan<byte> arguments) => _factory(_decode(arguments));
    internal override byte[] DefaultArguments() => Encode(default!);
}
