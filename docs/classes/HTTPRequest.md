# HTTPRequest

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.HTTPRequest`. **Inherits:** [Node](Node.md). **Source:** [HTTPRequest.cs](../../src/Core/Networking/HTTPRequest.cs).

## Description

Runs HTTP/HTTPS transfers from a scene node and delivers completion on its tree owner thread.

Requests require tree membership. UseThreads moves an operation and its transports to a dedicated worker; scene polling alone delivers results. Cancellation/tree exit suppress stale completion. File downloads use a sibling temporary file and replace the destination only after successful transfer and decompression.

[HTTP and compression](../components/http.md) and [ADR 0094](../decisions/networking.md#adr-0094) define wire, ownership, worker, allocation and platform boundaries.

## Example

Public API excerpt. Caller context and indicated polling/tree prerequisites are required; HTTPTests exercises full workflows.

```csharp
var request = new HTTPRequest { BodySizeLimit = 16 * 1024 * 1024 };
root.AddChild(request); // root belongs to a SceneTree.
request.RequestCompleted += (result, code, headers, body) =>
{
    // Inspect transport result and response code independently.
};
request.Request("https://service.example/data");
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public HTTPRequest()` | Creates an idle request node with gzip enabled and eight allowed redirects. |

## Constructor Descriptions

<a id="member-db68ea0efd74"></a>
### .ctor

`public HTTPRequest()`

Creates an idle request node with gzip enabled and eight allowed redirects.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean AcceptGZIP { get; set; }` | Gets or sets gzip/deflate negotiation and decoding. |
| `public System.Int64 BodySizeLimit { get; set; }` | Gets or sets the maximum wire/decoded body bytes. |
| `public System.Int32 DownloadChunkSize { get; set; }` | Gets or sets prepared download/decode chunk capacity. |
| `public System.String DownloadFile { get; set; }` | Gets or sets the destination engine filesystem path. |
| `public System.Int32 MaxRedirects { get; set; }` | Gets or sets the automatic redirect limit. |
| `public System.Double Timeout { get; set; }` | Gets or sets the request timeout in scene process seconds. |
| `public System.Boolean UseThreads { get; set; }` | Gets or sets dedicated-worker execution. |

## Property Descriptions

<a id="member-5ee43c9f527e"></a>
### AcceptGZIP

`public System.Boolean AcceptGZIP { get; set; }`

Gets or sets gzip/deflate negotiation and decoding.

Value: True initially. A current response samples this when its headers arrive.

<a id="member-b1612c5a9797"></a>
### BodySizeLimit

`public System.Int64 BodySizeLimit { get; set; }`

Gets or sets the maximum wire/decoded body bytes.

Value: Minus one initially; any negative value disables the limit. Configurable while idle.

<a id="member-e02188881d3f"></a>
### DownloadChunkSize

`public System.Int32 DownloadChunkSize { get; set; }`

Gets or sets prepared download/decode chunk capacity.

Value: 65536 initially; 256 through 16 MiB. Configurable while idle.

<a id="member-914af01d7925"></a>
### DownloadFile

`public System.String DownloadFile { get; set; }`

Gets or sets the destination engine filesystem path.

Value: Empty returns bytes in memory. Existing destinations survive cancellation and failed transfers.

<a id="member-ab2db4914309"></a>
### MaxRedirects

`public System.Int32 MaxRedirects { get; set; }`

Gets or sets the automatic redirect limit.

Value: Eight initially; negative is unlimited. Changes apply to active response processing.

<a id="member-3034316977c8"></a>
### Timeout

`public System.Double Timeout { get; set; }`

Gets or sets the request timeout in scene process seconds.

Value: Zero disables timeout. Finite nonnegative values are sampled when a request starts.

<a id="member-8ec5c1562bcb"></a>
### UseThreads

`public System.Boolean UseThreads { get; set; }`

Gets or sets dedicated-worker execution.

Value: False initially; configurable while idle.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void CancelRequest()` | Cancels the active operation without emitting completion. |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Creates a reusable factory for packed-scene instances of this exact runtime node type. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public System.Int64 GetBodySize()` | Gets declared response wire-body size. |
| `public System.Int64 GetDownloadedBytes()` | Gets consumed response wire-body bytes before content decoding. |
| `public Electron2D.HTTPStatus GetHTTPClientStatus()` | Gets the cached HTTP transport phase. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |
| `protected override System.Void OnNotification(System.Int32 what)` | Handles an engine notification delivered to this object. |
| `public System.Void Request(System.String url, System.Collections.Generic.IEnumerable<System.String> customHeaders = null, Electron2D.HTTPMethod method = Get, System.String requestData = "")` | Starts a request to an absolute HTTP/HTTPS URL with a UTF-8 body. |
| `public System.Void RequestRaw(System.String url, System.Collections.Generic.IEnumerable<System.String> customHeaders = null, Electron2D.HTTPMethod method = Get, System.ReadOnlySpan<System.Byte> requestData = default)` | Starts a request with copied raw body data. |
| `public System.Void SetHTTPProxy(System.String host, System.Int32 port)` | Sets or clears the HTTP forward proxy. |
| `public System.Void SetHTTPSProxy(System.String host, System.Int32 port)` | Sets or clears the HTTPS CONNECT proxy. |
| `public System.Void SetTLSOptions(Electron2D.TLSOptions options)` | Sets borrowed client TLS options for HTTPS. |

## Method Descriptions

<a id="member-519a23307604"></a>
### CancelRequest

`public System.Void CancelRequest()`

Cancels the active operation without emitting completion.

<a id="member-117f92aa6d32"></a>
### CreateSceneInstanceFactory

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

Creates a reusable factory for packed-scene instances of this exact runtime node type.

Returns: A non-null factory that creates a fresh node of the exact same runtime type.

Remarks: The base implementation supports only an exact Electron2D.Node. Derived node types that can be packed must return a static, non-capturing factory that remains valid after the source node is disposed and creates a live, detached, parentless, childless, unowned, and non-queued instance. Stored writable property descriptors restore the instance state.

System.NotSupportedException: A derived node has not explicitly supplied an instancing factory.

<a id="member-3634f7a90b1e"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

Remarks: Cancels queued deletion, detaches this node, recursively disposes every owned child, clears groups and event subscribers, and then calls the base implementation. Every teardown stage is attempted before failures are reported together.

<a id="member-88d04cc31a3c"></a>
### GetBodySize

`public System.Int64 GetBodySize()`

Gets declared response wire-body size.

Returns: Minus one before headers or for an unknown length.

<a id="member-d41546b9524e"></a>
### GetDownloadedBytes

`public System.Int64 GetDownloadedBytes()`

Gets consumed response wire-body bytes before content decoding.

Returns: Zero initially; the most recent operation's count after completion.

<a id="member-10d446f5321c"></a>
### GetHTTPClientStatus

`public Electron2D.HTTPStatus GetHTTPClientStatus()`

Gets the cached HTTP transport phase.

Returns: Disconnected while idle.

<a id="member-7ba4d64c2559"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Appends this class's typed hierarchy, ownership, processing, and automatic translation descriptors to the inherited descriptors.

<a id="member-005265e11d2a"></a>
### OnNotification

`protected override System.Void OnNotification(System.Int32 what)`

Handles an engine notification delivered to this object.

what: The notification identifier.

Remarks: Calls the base implementation, then maps enter, exit, ready, process, and physics-process notification IDs to the corresponding typed virtual callbacks. Pause and application-suspend notifications reset eligible physics presentation history. Manual Electron2D.ElectronObject.Notify(System.Int32) calls invoke callbacks but do not mutate tree membership, ready state, or delta values.

<a id="member-e8d352e974f2"></a>
### Request

`public System.Void Request(System.String url, System.Collections.Generic.IEnumerable<System.String> customHeaders = null, Electron2D.HTTPMethod method = Get, System.String requestData = "")`

Starts a request to an absolute HTTP/HTTPS URL with a UTF-8 body.

url: URL; fragments are not sent.

customHeaders: Optional copied field lines.

method: GET initially.

requestData: UTF-8 text body.

<a id="member-8de83e11b158"></a>
### RequestRaw

`public System.Void RequestRaw(System.String url, System.Collections.Generic.IEnumerable<System.String> customHeaders = null, Electron2D.HTTPMethod method = Get, System.ReadOnlySpan<System.Byte> requestData = default)`

Starts a request with copied raw body data.

url: Absolute HTTP/HTTPS URL.

customHeaders: Optional copied header lines.

method: GET initially.

requestData: Raw body, borrowed for this call.

<a id="member-caff58b8fb35"></a>
### SetHTTPProxy

`public System.Void SetHTTPProxy(System.String host, System.Int32 port)`

Sets or clears the HTTP forward proxy.

host: Empty clears.

port: Minus one clears; otherwise a remote port.

<a id="member-de4a1cd69b7b"></a>
### SetHTTPSProxy

`public System.Void SetHTTPSProxy(System.String host, System.Int32 port)`

Sets or clears the HTTPS CONNECT proxy.

host: Empty clears.

port: Minus one clears; otherwise a remote port.

<a id="member-822613b73d3a"></a>
### SetTLSOptions

`public System.Void SetTLSOptions(Electron2D.TLSOptions options)`

Sets borrowed client TLS options for HTTPS.

options: A live client configuration.

## Event summary

| Complete C# signature | Contract |
| --- | --- |
| `public event System.Action<Electron2D.HTTPRequestResult, Electron2D.HTTPResponseCode, System.String[], System.Byte[]> RequestCompleted` | Occurs after state/resources are released; handlers may start another request. |

## Event Descriptions

<a id="member-dadb6e7c9d0a"></a>
### RequestCompleted

`public event System.Action<Electron2D.HTTPRequestResult, Electron2D.HTTPResponseCode, System.String[], System.Byte[]> RequestCompleted`

Occurs after state/resources are released; handlers may start another request.

## Verification and limits

[HTTPTests](../../tests/Electron2D.Tests/HTTPTests.cs) verifies the exercised native Linux IPv4/DNS/TLS/proxy and public scene/worker paths, fragmented binary/framing/codec/security edges and prepared managed-allocation intervals. Native codec internals, real routed throughput, other-platform/browser delivery and owner acceptance remain separate gates. See [coverage](../coverage/classes/HTTPRequest.md).
