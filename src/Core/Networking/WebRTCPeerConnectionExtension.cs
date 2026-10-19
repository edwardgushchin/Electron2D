namespace Electron2D;

/// <summary>Defines the typed virtual peer contract for an application-supplied transport.</summary>
/// <remarks>Overrides implement the same owner/lifetime checks through CheckPeerAccess and publish typed events through the inherited emission helpers.</remarks>
public abstract class WebRTCPeerConnectionExtension : WebRTCPeerConnection
{
    /// <summary>Creates an extension identity on the current owner thread.</summary>
    protected WebRTCPeerConnectionExtension() { }
    /// <inheritdoc />
    public abstract override void Initialize(WebRTCConfiguration? configuration = null);
    /// <inheritdoc />
    public abstract override void AddICECandidate(string media, int index, string name);
    /// <inheritdoc />
    public abstract override void Close();
    /// <inheritdoc />
    public abstract override WebRTCDataChannel CreateDataChannel(string label, WebRTCDataChannelOptions? options = null);
    /// <inheritdoc />
    public abstract override void CreateOffer();
    /// <inheritdoc />
    public abstract override ConnectionState GetConnectionState();
    /// <inheritdoc />
    public abstract override GatheringState GetGatheringState();
    /// <inheritdoc />
    public abstract override SignalingState GetSignalingState();
    /// <inheritdoc />
    public abstract override void Poll();
    /// <inheritdoc />
    public abstract override void SetLocalDescription(string type, string sdp);
    /// <inheritdoc />
    public abstract override void SetRemoteDescription(string type, string sdp);
}
