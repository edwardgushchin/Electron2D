using System.Runtime.InteropServices;
using System.Text;
namespace Electron2D;

/// <summary>Negotiates owner-polled WebRTC data channels over a retained native peer or typed extension.</summary>
public class WebRTCPeerConnection : ElectronObject
{
    /// <summary>Describes transport establishment.</summary>
    public enum ConnectionState
    {
        /// <summary>New state.</summary>
        New = 0,
        /// <summary>Connecting state.</summary>
        Connecting = 1,
        /// <summary>Connected state.</summary>
        Connected = 2,
        /// <summary>Disconnected state.</summary>
        Disconnected = 3,
        /// <summary>Failed state.</summary>
        Failed = 4,
        /// <summary>Closed state.</summary>
        Closed = 5
    }
    /// <summary>Describes local ICE discovery.</summary>
    public enum GatheringState
    {
        /// <summary>New state.</summary>
        New = 0,
        /// <summary>Gathering state.</summary>
        Gathering = 1,
        /// <summary>Complete state.</summary>
        Complete = 2
    }
    /// <summary>Describes SDP offer/answer commitment.</summary>
    public enum SignalingState
    {
        /// <summary>Stable state.</summary>
        Stable = 0,
        /// <summary>HaveLocalOffer state.</summary>
        HaveLocalOffer = 1,
        /// <summary>HaveRemoteOffer state.</summary>
        HaveRemoteOffer = 2,
        /// <summary>HaveLocalPranswer state.</summary>
        HaveLocalPranswer = 3,
        /// <summary>HaveRemotePranswer state.</summary>
        HaveRemotePranswer = 4,
        /// <summary>Closed state.</summary>
        Closed = 5
    }
    private static Func<WebRTCPeerConnectionExtension>? _factory;
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private readonly List<WebRTCDataChannel> _channels = [];
    private WebRTCPeerHandle? _peer;
    private bool _preparing, _polling, _closed;
    private static readonly UTF8Encoding UTF8 = new(false, true);
    /// <summary>Creates the common owner identity for native and managed implementations.</summary>
    protected WebRTCPeerConnection() { }
    /// <summary>Constructs the selected extension, or the native implementation when none is selected.</summary>
    /// <returns>A caller-owned peer without an open connection.</returns>
    public static WebRTCPeerConnection Create()
    { var factory = Volatile.Read(ref _factory); var peer = factory is null ? new WebRTCPeerConnection() : factory() ?? throw new InvalidOperationException("The peer factory returned null."); ObjectDisposedException.ThrowIf(peer.IsDisposed, peer); return peer; }
    /// <summary>Selects a typed constructor for subsequent Create calls.</summary>
    /// <param name="factory">An extension factory, or null to restore the native implementation.</param>
    public static void SetDefaultExtension(Func<WebRTCPeerConnectionExtension>? factory) => Volatile.Write(ref _factory, factory);
    /// <summary>Checks lifetime, owner thread and configuration callback reentry.</summary>
    protected void CheckPeerAccess() { ThrowIfDisposed(); if (Environment.CurrentManagedThreadId != _owner) throw new InvalidOperationException("WebRTC peer access requires its constructing thread."); if (_preparing) throw new InvalidOperationException("WebRTC configuration callbacks cannot change their peer."); }
    /// <summary>Occurs on Poll when an in-band remote channel is ready to be delivered.</summary>
    public event Action<WebRTCDataChannel>? DataChannelReceived;
    /// <summary>Occurs on Poll with a media identity, index and local candidate.</summary>
    public event Action<string, int, string>? ICECandidateCreated;
    /// <summary>Occurs on Poll with an offer/answer to commit and send to the remote peer.</summary>
    public event Action<string, string>? SessionDescriptionCreated;
    internal static void Text(string value, string name, int max = 1048576)
    { ArgumentNullException.ThrowIfNull(value, name); if (value.Contains('\0') || UTF8.GetByteCount(value) > max) throw new ArgumentException("Invalid or oversized UTF-8 text.", name); }
    private WebRTCPeerHandle Native() { if (_closed) throw new InvalidOperationException("The peer connection is closed."); if (_peer is null) Initialize(); return _peer!; }
    /// <summary>Reinitializes the connection with a copied set of ICE endpoints.</summary>
    /// <param name="configuration">Null uses host candidates without STUN/TURN servers.</param>
    /// <remarks>Invalid input/setup preserves the old cohort. Successful replacement closes its channels without disposing their caller-owned wrappers.</remarks>
    public virtual unsafe void Initialize(WebRTCConfiguration? configuration = null)
    {
        CheckPeerAccess(); _preparing = true; var allocations = new List<nint>();
        try
        {
            if (OperatingSystem.IsBrowser()) throw new PlatformNotSupportedException("The browser WebRTC adapter is not installed.");
            var entries = new List<(string URL, string? User, string? Password)>();
            foreach (var server in configuration?.ICEServers ?? Array.Empty<WebRTCICEServer>())
            {
                ArgumentNullException.ThrowIfNull(server); ArgumentNullException.ThrowIfNull(server.URLs); if (server.Username is { } user) Text(user, nameof(configuration), 65535); if (server.Credential is { } password) Text(password, nameof(configuration), 65535);
                foreach (var url in server.URLs)
                { Text(url, nameof(configuration), 65535); if (!url.StartsWith("stun:", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("stuns:", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("turn:", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("turns:", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Expected a STUN/TURN URL.", nameof(configuration)); entries.Add((url, server.Username, server.Credential)); if (entries.Count > 1024) throw new ArgumentException("Too many ICE URLs.", nameof(configuration)); }
            }
            nint* urls = stackalloc nint[entries.Count]; nint* users = stackalloc nint[entries.Count]; nint* passwords = stackalloc nint[entries.Count];
            nint Allocate(string? value) { if (value is null) return 0; var p = Marshal.StringToCoTaskMemUTF8(value); allocations.Add(p); return p; }
            for (var i = 0; i < entries.Count; i++) { urls[i] = Allocate(entries[i].URL); users[i] = Allocate(entries[i].User); passwords[i] = Allocate(entries[i].Password); }
            var next = new WebRTCPeerHandle(WebRTCNative.Require(WebRTCNative.Create(urls, users, passwords, entries.Count))); var old = _peer; _peer = next; _closed = false; _channels.Clear(); old?.Dispose();
        }
        finally { foreach (var p in allocations) Marshal.FreeCoTaskMem(p); _preparing = false; }
    }
    /// <summary>Reports the current connection state without delivering events.</summary><returns>The native or initial New state.</returns>
    public virtual ConnectionState GetConnectionState() { CheckPeerAccess(); return _closed ? ConnectionState.Closed : _peer is null ? ConnectionState.New : (ConnectionState)State(0); }
    /// <summary>Reports the current ICE gathering state.</summary><returns>The native or initial New state.</returns>
    public virtual GatheringState GetGatheringState() { CheckPeerAccess(); return _peer is null ? GatheringState.New : (GatheringState)State(1); }
    /// <summary>Reports the current SDP signaling state.</summary><returns>The native or initial Stable state.</returns>
    public virtual SignalingState GetSignalingState() { CheckPeerAccess(); return _closed ? SignalingState.Closed : _peer is null ? SignalingState.Stable : (SignalingState)State(2); }
    private int State(int kind) { var value = WebRTCNative.State(_peer!, kind); WebRTCNative.Check(value); return value; }
    /// <summary>Creates an in-band or explicitly negotiated caller-owned data channel.</summary>
    /// <param name="label">A UTF-8 channel label of at most 65535 bytes.</param><param name="options">Null uses reliable ordered in-band delivery.</param>
    /// <returns>A channel belonging to this native cohort.</returns>
    public virtual WebRTCDataChannel CreateDataChannel(string label, WebRTCDataChannelOptions? options = null)
    {
        CheckPeerAccess(); Text(label, nameof(label), 65535); options ??= new(); Text(options.Protocol, nameof(options), 65535);
        if (options.ID is < 0 or > 65534 || options.Negotiated && options.ID is null || options.MaxPacketLifeTime is < 0 or > 65535 || options.MaxRetransmits is < 0 or > 65535 || options.MaxPacketLifeTime is not null && options.MaxRetransmits is not null) throw new ArgumentException("Invalid channel identity or reliability options.", nameof(options));
        if (GetConnectionState() != ConnectionState.New) throw new InvalidOperationException("Data channels require a new peer connection.");
        var handle = WebRTCNative.Require(WebRTCNative.CreateChannel(Native(), label, options.Protocol, options.Ordered ? 1 : 0, options.Negotiated ? 1 : 0, options.ID ?? -1, options.MaxPacketLifeTime ?? -1, options.MaxRetransmits ?? -1));
        var channel = new NativeWebRTCDataChannel(handle, options.Negotiated); _channels.Add(channel); return channel;
    }
    /// <summary>Generates an SDP offer without committing it or starting ICE publication.</summary>
    public virtual void CreateOffer() { CheckPeerAccess(); if (GetSignalingState() != SignalingState.Stable) throw new InvalidOperationException("Offer creation requires stable signaling."); if (!_channels.Any(c => !c.IsDisposed && c.GetReadyState() != WebRTCDataChannel.ChannelState.Closed)) throw new InvalidOperationException("At least one live data channel is required."); WebRTCNative.Check(WebRTCNative.Offer(Native())); }
    /// <summary>Commits the current emitted local offer or answer and starts native negotiation.</summary>
    /// <param name="type">The emitted proposal type.</param><param name="sdp">The current emitted proposal text; stale or foreign transport identity rejects.</param>
    public virtual void SetLocalDescription(string type, string sdp) { CheckPeerAccess(); Text(type, nameof(type), 32); Text(sdp, nameof(sdp)); WebRTCNative.Check(WebRTCNative.Local(Native(), type, sdp)); }
    /// <summary>Applies remote SDP and prepares an answer for a received offer.</summary>
    /// <param name="type">Remote description type.</param><param name="sdp">Remote SDP text.</param>
    public virtual void SetRemoteDescription(string type, string sdp) { CheckPeerAccess(); Text(type, nameof(type), 32); Text(sdp, nameof(sdp)); WebRTCNative.Check(WebRTCNative.Remote(Native(), type, sdp)); }
    /// <summary>Adds one remote ICE candidate.</summary>
    /// <param name="media">Application media identity.</param><param name="index">The zero-based application media index.</param><param name="name">Candidate text.</param>
    public virtual void AddICECandidate(string media, int index, string name) { CheckPeerAccess(); Text(media, nameof(media), 65535); Text(name, nameof(name), 65535); if (index < 0) throw new ArgumentOutOfRangeException(nameof(index)); WebRTCNative.Check(WebRTCNative.Candidate(Native(), media, name)); }
    /// <summary>Delivers queued native observations once on the constructing thread.</summary>
    /// <remarks>Events commit before callbacks. Reinitialization/disposal ends delivery for the old cohort. Callback errors aggregate after other selected subscribers.</remarks>
    public virtual void Poll()
    {
        CheckPeerAccess(); if (_polling) throw new InvalidOperationException("Peer polling cannot reenter itself."); var peer = _peer; if (peer is null) return; _polling = true; List<Exception>? errors = null;
        try
        {
            while (!IsDisposed && ReferenceEquals(peer, _peer))
            {
                var item = WebRTCNative.Event(peer); if (item == 0)
                {
                    try { WebRTCNative.CheckError(); } catch (Exception error) { Node.CollectException(ref errors, error); try { Close(); } catch (Exception cleanup) { Node.CollectException(ref errors, cleanup); } }
                    break;
                }
                try
                {
                    var kind = WebRTCNative.EventKind(item);
                    if (kind == 3) { var channel = new NativeWebRTCDataChannel(WebRTCNative.Require(WebRTCNative.EventChannel(item)), false); _channels.Add(channel); EmitDataChannelReceived(channel); }
                    else { var first = Marshal.PtrToStringUTF8(WebRTCNative.EventString(item, 0))!; var second = Marshal.PtrToStringUTF8(WebRTCNative.EventString(item, 1))!; if (kind == 1) EmitSessionDescriptionCreated(first, second); else EmitICECandidateCreated(first, 0, second); }
                }
                catch (Exception error) { Node.CollectException(ref errors, error); }
                finally { WebRTCNative.ReleaseEvent(item); }
            }
        }
        finally { _polling = false; }
        Node.ThrowCollected("WebRTC event callbacks failed.", errors);
    }
    /// <summary>Publishes a typed incoming-channel event on the owner thread.</summary><param name="channel">Caller-owned channel.</param>
    protected void EmitDataChannelReceived(WebRTCDataChannel channel) { CheckPeerAccess(); ArgumentNullException.ThrowIfNull(channel); var handlers = BeginEventEmission(DataChannelReceived); List<Exception>? errors = null; if (handlers is not null) foreach (Action<WebRTCDataChannel> handler in handlers.GetInvocationList()) try { handler(channel); } catch (Exception e) { Node.CollectException(ref errors, e); } Node.ThrowCollected("Channel callbacks failed.", errors); }
    /// <summary>Publishes a typed candidate event on the owner thread.</summary><param name="media">Media identity.</param><param name="index">Media index.</param><param name="name">Candidate text.</param>
    protected void EmitICECandidateCreated(string media, int index, string name) { CheckPeerAccess(); var handlers = BeginEventEmission(ICECandidateCreated); List<Exception>? errors = null; if (handlers is not null) foreach (Action<string, int, string> handler in handlers.GetInvocationList()) try { handler(media, index, name); } catch (Exception e) { Node.CollectException(ref errors, e); } Node.ThrowCollected("Candidate callbacks failed.", errors); }
    /// <summary>Publishes a typed offer/answer event on the owner thread.</summary><param name="type">Proposal type.</param><param name="sdp">Proposal text.</param>
    protected void EmitSessionDescriptionCreated(string type, string sdp) { CheckPeerAccess(); var handlers = BeginEventEmission(SessionDescriptionCreated); List<Exception>? errors = null; if (handlers is not null) foreach (Action<string, string> handler in handlers.GetInvocationList()) try { handler(type, sdp); } catch (Exception e) { Node.CollectException(ref errors, e); } Node.ThrowCollected("Description callbacks failed.", errors); }
    /// <summary>Closes native transport and pending observations without disposing channel wrappers.</summary>
    public virtual void Close() { CheckPeerAccess(); if (_peer is not null) WebRTCNative.Check(WebRTCNative.Close(_peer)); _closed = true; _channels.Clear(); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); CheckPeerAccess(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _peer?.Dispose(); _peer = null; _channels.Clear(); DataChannelReceived = null; ICECandidateCreated = null; SessionDescriptionCreated = null; } base.Dispose(disposing); }
}
