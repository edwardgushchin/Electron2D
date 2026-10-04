using System.Buffers.Binary;
using CryptoRandom = System.Security.Cryptography.RandomNumberGenerator;
namespace Electron2D;

/// <summary>Describes the multiplayer transport's local connection lifecycle.</summary>
public enum MultiplayerConnectionStatus
{
    /// <summary>No multiplayer connection is active.</summary>
    Disconnected = 0,
    /// <summary>Transport establishment or peer identity assignment is pending.</summary>
    Connecting = 1,
    /// <summary>The multiplayer endpoint is ready to exchange application packets.</summary>
    Connected = 2
}
/// <summary>Selects the transport delivery and ordering contract.</summary>
public enum TransferMode
{
    /// <summary>Packets may be lost or arrive out of order.</summary>
    Unreliable = 0,
    /// <summary>Packets may be lost, while delivered packets preserve ordering.</summary>
    UnreliableOrdered = 1,
    /// <summary>Packets are retransmitted and delivered in order.</summary>
    Reliable = 2
}

/// <summary>Defines typed peer identity, packet metadata, routing and connection events for multiplayer transports.</summary>
/// <remarks>Calls and disposal require the constructing thread. Implementations inherit the span packet hooks from
/// PacketPeer and override lifecycle, metadata and configurable delivery properties. Transport-specific capabilities
/// determine whether transfer channel/mode settings affect the wire. Scene replication is a separate consumer.</remarks>
public abstract class MultiplayerPeer : PacketPeer
{
    private int _channel;
    private TransferMode _mode = TransferMode.Reliable;
    private bool _refuse;
    /// <summary>Selects all connected remote peers.</summary>
    public const int TargetPeerBroadcast = 0;
    /// <summary>Identifies the server endpoint.</summary>
    public const int TargetPeerServer = 1;
    /// <summary>Creates a multiplayer transport owned by the current thread.</summary>
    protected MultiplayerPeer() { }
    /// <summary>Occurs after a remote peer and its identity have been committed.</summary>
    /// <remarks>All subscribers run on the owner thread; failures aggregate after remaining subscribers.</remarks>
    public event Action<int>? PeerConnected;
    /// <summary>Occurs after a remote peer has been detached; Close and forced local disconnect suppress it.</summary>
    public event Action<int>? PeerDisconnected;
    /// <summary>Gets or sets the requested outgoing transport channel.</summary><value>Zero initially; nonnegative. A transport may provide only channel zero.</value>
    /// <exception cref="ArgumentOutOfRangeException">The requested channel is negative.</exception>
    public virtual int TransferChannel { get { CheckPacketPeer(); return _channel; } set { CheckPacketPeer(); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _channel = value; } }
    /// <summary>Gets or sets the requested delivery mode.</summary><value>Reliable initially; a reliable-only transport can ignore the selected mode on the wire.</value>
    /// <exception cref="ArgumentOutOfRangeException">The selector is invalid.</exception>
    public virtual TransferMode TransferMode { get { CheckPacketPeer(); return _mode; } set { CheckPacketPeer(); if ((uint)value > 2) throw new ArgumentOutOfRangeException(nameof(value)); _mode = value; } }
    /// <summary>Gets or sets whether a server admits new remote peers.</summary><value>False initially; existing connections remain usable.</value>
    public virtual bool RefuseNewConnections { get { CheckPacketPeer(); return _refuse; } set { CheckPacketPeer(); _refuse = value; } }
    /// <summary>Chooses broadcast, a positive peer, or all peers except a negative ID.</summary><param name="id">Zero broadcasts; one is server; int.MinValue cannot denote an exclusion.</param>
    public abstract void SetTargetPeer(int id);
    /// <summary>Gets the source identity of the next queued packet.</summary><returns>A peer ID; empty-queue behavior is transport-specific.</returns>
    public abstract int GetPacketPeer();
    /// <summary>Gets the next packet's transport channel.</summary><returns>The receiving channel; zero for single-channel transports.</returns>
    public abstract int GetPacketChannel();
    /// <summary>Gets the next packet's actual delivery mode.</summary><returns>The wire mode, independent of requested outgoing configuration.</returns>
    public abstract TransferMode GetPacketMode();
    /// <summary>Gets the local multiplayer identity.</summary><returns>One for servers, greater than one for assigned clients, or zero while disconnected/connecting.</returns>
    public abstract int GetUniqueID();
    /// <summary>Gets the cached local connection status.</summary><returns>The latest polled status.</returns>
    public abstract MultiplayerConnectionStatus GetConnectionStatus();
    /// <summary>Reports whether this endpoint acts as server.</summary><returns>The current server role.</returns>
    public abstract bool IsServer();
    /// <summary>Reports whether a higher multiplayer layer can implement server relay through this transport.</summary><returns>False by default; this does not automatically relay application bytes.</returns>
    public virtual bool IsServerRelaySupported() { CheckPacketPeer(); return false; }
    /// <summary>Generates a positive random peer identity distinct from broadcast and server.</summary><returns>Two through int.MaxValue; a host must still reject identity collisions.</returns>
    public int GenerateUniqueID() { CheckPacketPeer(); Span<byte> bytes = stackalloc byte[4]; int value; do { CryptoRandom.Fill(bytes); value = BinaryPrimitives.ReadInt32LittleEndian(bytes) & int.MaxValue; } while (value < 2); return value; }
    /// <summary>Advances connection and packet progress using the concrete transport's polling policy.</summary>
    public abstract void Poll();
    /// <summary>Immediately releases all owned transport state without disconnection events.</summary>
    public abstract void Close();
    /// <summary>Disconnects one remote peer.</summary><param name="peer">Remote identity.</param><param name="force">True suppresses the local disconnection event and tears down immediately.</param>
    public abstract void DisconnectPeer(int peer, bool force = false);
    /// <summary>Delivers a committed connection event on the owner thread.</summary><param name="id">Remote peer identity.</param>
    /// <exception cref="AggregateException">Subscribers failed after every subscriber was attempted.</exception>
    protected void EmitPeerConnected(int id) { CheckPacketPeer(); Emit(PeerConnected, id); }
    /// <summary>Delivers a committed disconnection event on the owner thread.</summary><param name="id">Detached peer identity.</param>
    /// <exception cref="AggregateException">Subscribers failed after every subscriber was attempted.</exception>
    protected void EmitPeerDisconnected(int id) { CheckPacketPeer(); Emit(PeerDisconnected, id); }
    private static void Emit(Action<int>? subscribers, int id)
    {
        List<Exception>? errors = null; foreach (var subscriber in Delegate.EnumerateInvocationList(subscribers)) { try { subscriber(id); } catch (Exception error) { (errors ??= []).Add(error); } }
        if (errors is not null) throw new AggregateException(errors);
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<MultiplayerPeer, int>(nameof(TransferChannel), p => p.TransferChannel, (p, v) => p.TransferChannel = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<MultiplayerPeer, TransferMode>(nameof(TransferMode), p => p.TransferMode, (p, v) => p.TransferMode = v, _ => Electron2D.TransferMode.Reliable, stored: true);
        yield return new PropertyDescriptor<MultiplayerPeer, bool>(nameof(RefuseNewConnections), p => p.RefuseNewConnections, (p, v) => p.RefuseNewConnections = v, _ => false, stored: true);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { PeerConnected = null; PeerDisconnected = null; } base.Dispose(disposing); }
}

/// <summary>Models a permanently connected local server without remote endpoints.</summary>
/// <remarks>The local ID is one. Writes discard bytes, reads return an empty packet, available count and maximum size
/// are zero. Poll, Close, routing and disconnection validate ownership but preserve the offline server identity.
/// This is the local authority transport; automatic SceneTree multiplayer assignment remains a separate integration.</remarks>
public class OfflineMultiplayerPeer : MultiplayerPeer
{
    /// <summary>Creates an offline local authority endpoint.</summary>
    public OfflineMultiplayerPeer() { }
    /// <inheritdoc />
    public override int GetAvailablePacketCount() { CheckPacketPeer(); return 0; }
    /// <inheritdoc />
    public override int GetMaxPacketSize() { CheckPacketPeer(); return 0; }
    /// <inheritdoc />
    protected override int NextPacketSize() => 0;
    /// <inheritdoc />
    protected override void ReadPacketCore(Span<byte> destination) { }
    /// <inheritdoc />
    public override void PutPacket(ReadOnlySpan<byte> data) { CheckPacketPeer(); }
    /// <inheritdoc />
    public override void SetTargetPeer(int id) { CheckPacketPeer(); }
    /// <inheritdoc />
    public override int GetPacketPeer() { CheckPacketPeer(); return 0; }
    /// <inheritdoc />
    public override int GetPacketChannel() { CheckPacketPeer(); return 0; }
    /// <inheritdoc />
    public override TransferMode GetPacketMode() { CheckPacketPeer(); return Electron2D.TransferMode.Reliable; }
    /// <inheritdoc />
    public override int GetUniqueID() { CheckPacketPeer(); return TargetPeerServer; }
    /// <inheritdoc />
    public override MultiplayerConnectionStatus GetConnectionStatus() { CheckPacketPeer(); return MultiplayerConnectionStatus.Connected; }
    /// <inheritdoc />
    public override bool IsServer() { CheckPacketPeer(); return true; }
    /// <inheritdoc />
    public override void Poll() { CheckPacketPeer(); }
    /// <inheritdoc />
    public override void Close() { CheckPacketPeer(); }
    /// <inheritdoc />
    public override void DisconnectPeer(int peer, bool force = false) { CheckPacketPeer(); }
}
