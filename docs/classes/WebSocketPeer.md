# WebSocketPeer

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.WebSocketPeer`. **Inherits:** [PacketPeer](PacketPeer.md). **Source:** [WebSocketPeer.cs](../../src/Core/Networking/WebSocketPeer.cs).

## Description

Exchanges complete WebSocket messages over polled WS/WSS or a borrowed ordered stream.

Calls and disposal require the constructing thread. ConnectToURL owns its HTTP/TCP/TLS state; AcceptStream borrows its stream and never disposes it. Poll advances handshakes, messages and close/control frames. Prepared span sends/reads and polling use bounded storage; snapshots, handshakes and close diagnostics are cold work.

[WebSocket messages](../components/websocket.md) and [ADR 0094](../decisions/networking.md#adr-0094) define handshake, queue, ownership and protocol/error boundaries. GetPacket/GetPacketError/LastReadException inherit the complete-message ownership contract from PacketPeer; reads do not implicitly poll. Public snapshots and encoding/diagnostic failure paths remain explicit allocation boundaries. Buffer/queue configuration requires Closed and is prepared at connection; runtime operations retain bounded storage.

## Example

Public C# excerpt for a caller-provided WS endpoint. Poll from the same constructing thread, for example inside a Node.OnProcess; WebSocketTests runs that scene path and verifies native WS/WSS in both roles. This standalone snippet compiles, but remote service availability and payload interpretation are caller prerequisites.

```csharp
using var peer = new WebSocketPeer();
peer.ConnectToURL("ws://localhost:8080/play");
while (peer.GetReadyState() != WebSocketState.Closed)
{
    peer.Poll();
    while (peer.GetAvailablePacketCount() > 0)
    {
        byte[] message = peer.GetPacket();
        bool text = peer.WasStringPacket();
    }
    Thread.Sleep(1);
}
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public WebSocketPeer()` | Creates a closed WebSocket peer with default configuration. |

## Constructor Descriptions

<a id="member-d26b9e8e54a0"></a>
### .ctor

`public WebSocketPeer()`

Creates a closed WebSocket peer with default configuration.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.String[] HandshakeHeaders { get; set; }` | Gets or sets copied extra field lines for the next client or server handshake. |
| `public System.Double HeartbeatInterval { get; set; }` | Gets or sets the ping interval in monotonic seconds. |
| `public System.Int32 InboundBufferSize { get; set; }` | Gets or sets the maximum incoming message/queued payload bytes. |
| `public System.Int32 MaxQueuedPackets { get; set; }` | Gets or sets the number of queued application messages in either direction. |
| `public System.Int32 OutboundBufferSize { get; set; }` | Gets or sets the outgoing message/queued payload budget. |
| `public System.String[] SupportedProtocols { get; set; }` | Gets or sets copied, trimmed, unique subprotocol tokens for the next handshake. |

## Property Descriptions

<a id="member-3eac97ede838"></a>
### HandshakeHeaders

`public System.String[] HandshakeHeaders { get; set; }`

Gets or sets copied extra field lines for the next client or server handshake.

Value: Empty initially. Required framing/upgrade fields cannot be overridden.

System.ArgumentException: A header is invalid or overrides a reserved handshake field.

<a id="member-464e875acc14"></a>
### HeartbeatInterval

`public System.Double HeartbeatInterval { get; set; }`

Gets or sets the ping interval in monotonic seconds.

Value: Zero disables heartbeat. Finite nonnegative values reset the timer; an unanswered ping at the next interval aborts.

System.ArgumentOutOfRangeException: The interval is negative, nonfinite or exceeds the monotonic clock range.

<a id="member-3b9e46ac1167"></a>
### InboundBufferSize

`public System.Int32 InboundBufferSize { get; set; }`

Gets or sets the maximum incoming message/queued payload bytes.

Value: 65535 initially; zero accepts only empty data messages. Configurable while closed, up to 64 MiB.

System.ArgumentOutOfRangeException: The byte budget is outside zero through 64 MiB.

System.InvalidOperationException: The connection is active.

<a id="member-42f25bfabf11"></a>
### MaxQueuedPackets

`public System.Int32 MaxQueuedPackets { get; set; }`

Gets or sets the number of queued application messages in either direction.

Value: 4096 initially; zero through 65536 while closed. Control traffic uses four additional reserved records.

System.ArgumentOutOfRangeException: The count is outside zero through 65536.

System.InvalidOperationException: The connection is active.

<a id="member-90ec9ec7fe31"></a>
### OutboundBufferSize

`public System.Int32 OutboundBufferSize { get; set; }`

Gets or sets the outgoing message/queued payload budget.

Value: 65535 initially; zero selects the explicit 64 MiB prepared ceiling. Configurable while closed.

System.ArgumentOutOfRangeException: The byte budget is outside zero through 64 MiB.

System.InvalidOperationException: The connection is active.

<a id="member-4fc090db2eca"></a>
### SupportedProtocols

`public System.String[] SupportedProtocols { get; set; }`

Gets or sets copied, trimmed, unique subprotocol tokens for the next handshake.

Value: Empty initially. A configured protocol list requires a negotiated member.

System.ArgumentException: A protocol is null, duplicated or not an HTTP token.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void AcceptStream(Electron2D.StreamPeer stream)` | Starts a server upgrade over a borrowed live stream, including an established/pending TLS stream. |
| `public System.Void Close(System.Int32 code = 1000, System.String reason = "")` | Starts a close handshake, or immediately aborts for a negative code. |
| `public System.Void ConnectToURL(System.String url, Electron2D.TLSOptions tlsClientOptions = null)` | Starts nonblocking WS/WSS connection and HTTP upgrade. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public override System.Int32 GetAvailablePacketCount()` | Returns the number of complete immediately available packets. |
| `public System.Int32 GetCloseCode()` | Gets the received close status or a locally detected protocol-error status. |
| `public System.String GetCloseReason()` | Gets the close reason after closure. |
| `public System.String GetConnectedHost()` | Gets the underlying native remote address. |
| `public System.Int32 GetConnectedPort()` | Gets the underlying native remote port. |
| `public System.Int32 GetCurrentOutboundBufferedAmount()` | Gets application payload bytes retained in the outbound queue. |
| `public override System.Int32 GetMaxPacketSize()` | Reports the maximum outgoing raw payload supported by this transport. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |
| `public Electron2D.WebSocketState GetReadyState()` | Gets the latest explicit WebSocket state. |
| `public System.String GetRequestedURL()` | Gets the requested client URL or server URL assembled from Host and target. |
| `public System.String GetSelectedProtocol()` | Gets the negotiated subprotocol. |
| `protected override System.Int32 NextPacketSize()` | Reports the next packet size without consuming it. |
| `public System.Void Poll()` | Advances transport/upgrade, complete-message queues, heartbeat and close handshake without waiting. |
| `public override System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)` | Sends exactly one packet. |
| `protected override System.Void ReadPacketCore(System.Span<System.Byte> destination)` | Consumes the next complete packet into a validated destination. |
| `public System.Void Send(System.ReadOnlySpan<System.Byte> message, Electron2D.WebSocketWriteMode writeMode = Binary)` | Copies and queues one complete message, then attempts immediate nonblocking transmission. |
| `public System.Void SendText(System.String message)` | Encodes and sends one UTF-8 text message through prepared storage. |
| `public System.Void SetNoDelay(System.Boolean enabled)` | Sets TCP_NODELAY on the live native transport. |
| `public System.Boolean WasStringPacket()` | Reports the text marker of the most recently consumed application message. |

## Method Descriptions

<a id="member-0bf40aebd9de"></a>
### AcceptStream

`public System.Void AcceptStream(Electron2D.StreamPeer stream)`

Starts a server upgrade over a borrowed live stream, including an established/pending TLS stream.

stream: Ordered stream; custom transports support framing but have no native endpoint or TCP option.

System.ArgumentNullException: The stream is null.

System.ObjectDisposedException: The borrowed stream is disposed.

System.InvalidOperationException: A connection is already active.

<a id="member-8fcdccf38a9f"></a>
### Close

`public System.Void Close(System.Int32 code = 1000, System.String reason = "")`

Starts a close handshake, or immediately aborts for a negative code.

code: 1000 initially; valid protocol/application status. Negative aborts silently.

reason: Valid UTF-8 text, at most 123 encoded bytes.

System.ArgumentOutOfRangeException: The nonnegative close code is reserved or invalid.

System.ArgumentException: The encoded reason exceeds 123 bytes.

System.Text.EncoderFallbackException: The reason contains invalid UTF-16.

<a id="member-244ec4cd655f"></a>
### ConnectToURL

`public System.Void ConnectToURL(System.String url, Electron2D.TLSOptions tlsClientOptions = null)`

Starts nonblocking WS/WSS connection and HTTP upgrade.

url: URL, optionally without ws://; credentials are forbidden and fragments are not sent.

tlsClientOptions: Borrowed client options for WSS; null uses system trust/name validation.

System.ArgumentException: The URL or TLS role is invalid.

System.InvalidOperationException: A connection is already active.

<a id="member-a0175b917380"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-8b278c16f672"></a>
### GetAvailablePacketCount

`public override System.Int32 GetAvailablePacketCount()`

Returns the number of complete immediately available packets.

Returns: Nonnegative packet count.

<a id="member-4edc758d750d"></a>
### GetCloseCode

`public System.Int32 GetCloseCode()`

Gets the received close status or a locally detected protocol-error status.

Returns: Minus one for transport/forced closure; 1005 for a received empty close. Query while closed.

System.InvalidOperationException: The WebSocket has not closed.

<a id="member-7afcbfc4ef2d"></a>
### GetCloseReason

`public System.String GetCloseReason()`

Gets the close reason after closure.

Returns: Received UTF-8 reason or local protocol diagnostic; empty after an unclean close.

System.InvalidOperationException: The WebSocket has not closed.

<a id="member-66bcb572bc61"></a>
### GetConnectedHost

`public System.String GetConnectedHost()`

Gets the underlying native remote address.

Returns: IP literal, or empty for closed/custom streams.

<a id="member-f7468fdc9499"></a>
### GetConnectedPort

`public System.Int32 GetConnectedPort()`

Gets the underlying native remote port.

Returns: Port, or zero for closed/custom streams.

<a id="member-12659022a6ce"></a>
### GetCurrentOutboundBufferedAmount

`public System.Int32 GetCurrentOutboundBufferedAmount()`

Gets application payload bytes retained in the outbound queue.

Returns: Includes a partially sent message until its frame drains; excludes control/frame overhead.

<a id="member-fc5a1f8d892e"></a>
### GetMaxPacketSize

`public override System.Int32 GetMaxPacketSize()`

Reports the maximum outgoing raw payload supported by this transport.

Returns: A nonnegative byte count; operating-system limits may be narrower.

<a id="member-077735edc116"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Overrides append or replace descriptors; they must not yield null entries.

<a id="member-18ef8fd337c7"></a>
### GetReadyState

`public Electron2D.WebSocketState GetReadyState()`

Gets the latest explicit WebSocket state.

Returns: The cached state; this does not poll.

<a id="member-e857e932f01e"></a>
### GetRequestedURL

`public System.String GetRequestedURL()`

Gets the requested client URL or server URL assembled from Host and target.

Returns: Empty before a handshake; retained after closure until the next connection.

<a id="member-5a9862e4cc41"></a>
### GetSelectedProtocol

`public System.String GetSelectedProtocol()`

Gets the negotiated subprotocol.

Returns: Empty if no subprotocol was negotiated.

<a id="member-93eac8b93dd3"></a>
### NextPacketSize

`protected override System.Int32 NextPacketSize()`

Reports the next packet size without consuming it.

Returns: Length, or minus one when absent.

<a id="member-ce4b9f259664"></a>
### Poll

`public System.Void Poll()`

Advances transport/upgrade, complete-message queues, heartbeat and close handshake without waiting.

System.IO.IOException: Transport or handshake validation fails; owned state is released.

<a id="member-d790da25239f"></a>
### PutPacket

`public override System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)`

Sends exactly one packet.

data: Packet bytes, borrowed only for this call.

<a id="member-b9edb9d0ff72"></a>
### ReadPacketCore

`protected override System.Void ReadPacketCore(System.Span<System.Byte> destination)`

Consumes the next complete packet into a validated destination.

destination: An exact-sized destination.

<a id="member-e984a58ec953"></a>
### Send

`public System.Void Send(System.ReadOnlySpan<System.Byte> message, Electron2D.WebSocketWriteMode writeMode = Binary)`

Copies and queues one complete message, then attempts immediate nonblocking transmission.

message: Borrowed payload.

writeMode: Binary initially; text requires strict UTF-8.

System.InvalidOperationException: Not open or prepared queue capacity is exhausted; the message is not queued.

System.ArgumentException: The payload exceeds its budget or text is invalid UTF-8.

System.ArgumentOutOfRangeException: The write mode is not text or binary.

<a id="member-a28424b76239"></a>
### SendText

`public System.Void SendText(System.String message)`

Encodes and sends one UTF-8 text message through prepared storage.

message: Text with valid UTF-16; no temporary encoded array is allocated.

System.Text.EncoderFallbackException: The string contains invalid UTF-16.

System.ArgumentException: Encoded text exceeds the output budget.

System.InvalidOperationException: Not open or the prepared queue capacity is exhausted.

<a id="member-4aee2eda99f4"></a>
### SetNoDelay

`public System.Void SetNoDelay(System.Boolean enabled)`

Sets TCP_NODELAY on the live native transport.

enabled: Whether to bypass Nagle aggregation; enabled automatically at connection.

System.NotSupportedException: A custom stream has no TCP transport.

<a id="member-75e86661ed04"></a>
### WasStringPacket

`public System.Boolean WasStringPacket()`

Reports the text marker of the most recently consumed application message.

Returns: False initially; too-small/failed reads do not change it.

## State, errors and verification

Closed → Connecting → Open → Closing → Closed. Send requires Open; Close(-1) aborts immediately, and close metadata requires Closed. Received empty close exposes 1005; local protocol errors expose their close code, and unclean transport/forced closure minus one. Poll is required for normal close completion. A silent remote close has no implicit timeout; the caller can abort. Disposal and all operations require the constructing thread. Borrowed accepted streams survive WebSocket close/disposal; an owned client transport releases deterministically. TLS buffered application bytes drain before native TCP EOF closes the protocol.

Prepared input/output budgets, complete fragmented text validation, capacity failures, control reserve and heartbeat units are detailed in the component. Configured subprotocols require negotiation. Browser host/backend integration, multiplayer and scene replication are separate dependencies.

[WebSocketTests](../../tests/Electron2D.Tests/WebSocketTests.cs) verifies the documented wire and native/scene paths, both-role independent .NET interoperability, queue/security/owner edges and 64 warmed server active/idle plus WS/WSS client mask/text/poll/span intervals with zero managed bytes. Foreign targets/browser, native RNG/TLS/OS allocation, routed throughput and human acceptance remain separate. See [coverage](../coverage/classes/WebSocketPeer.md).
