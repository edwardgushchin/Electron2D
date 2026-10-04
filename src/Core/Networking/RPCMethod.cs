namespace Electron2D;

/// <summary>Selects which remote peers may invoke a configured RPC.</summary>
public enum RPCMode
{
    /// <summary>Rejects remote invocations.</summary>
    Disabled = 0,
    /// <summary>Accepts invocations from every admitted peer.</summary>
    AnyPeer = 1,
    /// <summary>Accepts only the receiving node's multiplayer authority.</summary>
    Authority = 2
}
/// <summary>Encodes one concrete RPC argument model into borrowed output storage.</summary>
/// <typeparam name="T">The application argument type, including a typed tuple or record.</typeparam>
/// <param name="value">Value to encode.</param><param name="destination">Exactly the size returned by the method's size function.</param><returns>Bytes written, which must equal destination length.</returns>
public delegate int RPCEncoder<T>(T value, Span<byte> destination);
/// <summary>Decodes and validates a complete borrowed RPC argument payload.</summary>
/// <typeparam name="T">The concrete application argument type.</typeparam><param name="source">Complete encoded argument bytes.</param><returns>The decoded value; reject malformed/trailing bytes with an exception.</returns>
public delegate T RPCDecoder<T>(ReadOnlySpan<byte> source);
/// <summary>Receives borrowed custom/authentication bytes on the multiplayer owner thread.</summary>
/// <param name="id">Original remote peer identity.</param><param name="data">Bytes valid only during this callback; copy when retaining them.</param>
public delegate void MultiplayerPacketHandler(int id, ReadOnlySpan<byte> data);
/// <summary>Immutable RPC policy; an unconfigured method is disabled.</summary>
public readonly record struct RPCOptions
{
    /// <summary>Creates an authority-only remote-only reliable policy. A zero-initialized default value remains disabled.</summary>
    public RPCOptions() : this(RPCMode.Authority, false, TransferMode.Reliable, 0) { }
    /// <summary>Creates an explicit invocation policy.</summary>
    /// <param name="mode">Admission policy.</param><param name="callLocal">Also invokes locally when the target includes this peer.</param><param name="transferMode">Requested transport mode.</param><param name="channel">Nonnegative outgoing channel.</param>
    public RPCOptions(RPCMode mode = RPCMode.Authority, bool callLocal = false, TransferMode transferMode = TransferMode.Reliable, int channel = 0)
    { if ((uint)mode > 2 || (uint)transferMode > 2 || channel < 0) throw new ArgumentOutOfRangeException(nameof(mode)); Mode = mode; CallLocal = callLocal; TransferMode = transferMode; Channel = channel; }
    /// <summary>Gets remote invocation admission policy.</summary>
    public RPCMode Mode { get; }
    /// <summary>Gets whether targets including the local peer invoke locally after sending.</summary>
    public bool CallLocal { get; }
    /// <summary>Gets requested outgoing delivery mode.</summary>
    public TransferMode TransferMode { get; }
    /// <summary>Gets requested nonnegative outgoing channel.</summary>
    public int Channel { get; }
}
/// <summary>Identifies a concrete typed RPC method without reflection or string method dispatch.</summary>
/// <remarks>Wire IDs must be unique per node and agreed by all participants. The application explicitly supplies its typed codec.
/// Tokens are immutable shared configuration; they own no node/transport lifetime.</remarks>
public abstract class RPCMethod
{
    /// <summary>Creates a method identity.</summary><param name="id">Nonzero stable application wire identity.</param>
    private protected RPCMethod(uint id) { if (id == 0) throw new ArgumentOutOfRangeException(nameof(id)); ID = id; }
    /// <summary>Gets the stable application wire identity.</summary>
    public uint ID { get; }
    /// <summary>Invokes this token from a complete encoded argument payload.</summary><param name="node">Compatible live Node receiver.</param><param name="arguments">Borrowed codec input.</param><remarks>Custom MultiplayerAPI implementations enforce authority/admission and sender scope before calling this hook.</remarks>
    public void InvokeEncoded(Node node, ReadOnlySpan<byte> arguments) { ArgumentNullException.ThrowIfNull(node); ObjectDisposedException.ThrowIf(node.IsDisposed, node); node.Tree?.EnsureOwnerThread(); if (!Accepts(node)) throw new ArgumentException("RPC receiver type does not match.", nameof(node)); Invoke(node, arguments); }
    internal abstract bool Accepts(Node node);
    internal abstract void Invoke(Node node, ReadOnlySpan<byte> arguments);
}
/// <summary>Pairs a strongly typed Node receiver and argument model with an explicit wire codec.</summary>
/// <typeparam name="TNode">The receiver Node subtype.</typeparam><typeparam name="T">A concrete argument type; tuples/records model multiple arguments.</typeparam>
public sealed class RPCMethod<TNode, T> : RPCMethod where TNode : Node
{
    private readonly Action<TNode, T> _method;
    private readonly Func<T, int> _size;
    private readonly RPCEncoder<T> _encode;
    private readonly RPCDecoder<T> _decode;
    /// <summary>Creates an immutable callable and codec contract.</summary><param name="id">Nonzero unique per-node wire identity.</param><param name="method">Direct receiver callback.</param><param name="size">Nonnegative complete encoded size.</param><param name="encode">Writes exactly that size.</param><param name="decode">Validates a complete payload and returns concrete arguments.</param>
    public RPCMethod(uint id, Action<TNode, T> method, Func<T, int> size, RPCEncoder<T> encode, RPCDecoder<T> decode) : base(id)
    { ArgumentNullException.ThrowIfNull(method); ArgumentNullException.ThrowIfNull(size); ArgumentNullException.ThrowIfNull(encode); ArgumentNullException.ThrowIfNull(decode); _method = method; _size = size; _encode = encode; _decode = decode; }
    internal override bool Accepts(Node node) => node is TNode;
    internal override void Invoke(Node node, ReadOnlySpan<byte> arguments) => _method((TNode)node, _decode(arguments));
    /// <summary>Invokes the direct typed callback without encoding.</summary><param name="node">Compatible live receiver.</param><param name="arguments">Concrete argument model.</param><remarks>The provider owns remote admission and sender scope.</remarks>
    public void Invoke(TNode node, T arguments) { ArgumentNullException.ThrowIfNull(node); ObjectDisposedException.ThrowIf(node.IsDisposed, node); node.Tree?.EnsureOwnerThread(); _method(node, arguments); }
    /// <summary>Decodes complete borrowed arguments for a custom provider.</summary><param name="source">Complete codec payload.</param><returns>The concrete decoded value.</returns>
    public T Decode(ReadOnlySpan<byte> source) => _decode(source);
    /// <summary>Measures complete codec output.</summary><param name="value">Concrete arguments.</param><returns>A nonnegative encoded size.</returns>
    public int GetEncodedSize(T value) { var size = _size(value); if (size < 0) throw new InvalidOperationException("RPC codec returned a negative size."); return size; }
    /// <summary>Writes a complete codec payload into exactly sized borrowed storage.</summary><param name="value">Concrete arguments.</param><param name="destination">Exactly GetEncodedSize bytes.</param>
    public void Encode(T value, Span<byte> destination) { if (destination.Length != GetEncodedSize(value)) throw new ArgumentException("RPC encoding storage must equal the declared size.", nameof(destination)); if (_encode(value, destination) != destination.Length) throw new InvalidDataException("RPC codec did not write its declared payload size."); }
}
/// <summary>One immutable configured method and its policy, as returned by Node.GetNodeRPCConfig.</summary>
public readonly record struct RPCRegistration
{
    /// <summary>Creates a typed configuration snapshot.</summary><param name="method">Shared immutable method token.</param><param name="options">Invocation policy.</param>
    public RPCRegistration(RPCMethod method, RPCOptions options) { ArgumentNullException.ThrowIfNull(method); Method = method; Options = options; }
    /// <summary>Gets the shared immutable method token.</summary>
    public RPCMethod Method { get; }
    /// <summary>Gets the copied invocation policy.</summary>
    public RPCOptions Options { get; }
}
