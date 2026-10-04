# HTTPClient

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.HTTPClient`. **Inherits:** [ElectronObject](ElectronObject.md). **Source:** [HTTPClient.cs](../../src/Core/Networking/HTTPClient.cs).

## Description

Executes polled HTTP/1.1 requests over owned TCP/TLS or a borrowed StreamPeer.

Calls require the constructing thread. Request/header snapshots allocate outside body-span polling. Caller-span body reads reuse prepared storage, remove transfer framing and preserve response byte order. Header budgets reject oversized input. GetResponseHeaders consumes the header snapshot; status queries do not poll.

[HTTP and compression](../components/http.md) and [ADR 0094](../decisions/networking.md#adr-0094) define wire, ownership, worker, allocation and platform boundaries.

## Example

Public API excerpt. Caller context and indicated polling/tree prerequisites are required; HTTPTests exercises full workflows.

```csharp
using var client = new HTTPClient();
client.ConnectToHost("https://service.example");
// Poll until Connected, then start the request.
client.Request(HTTPMethod.Get, "/data", []);
// Poll headers, then repeatedly read body chunks until Body completes.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public HTTPClient()` | Creates a disconnected HTTP client with prepared header and chunk-line storage. |

## Constructor Descriptions

<a id="member-2e813f5f00a7"></a>
### .ctor

`public HTTPClient()`

Creates a disconnected HTTP client with prepared header and chunk-line storage.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean BlockingModeEnabled { get; set; }` | Gets or sets whether send/header/body operations may wait for progress. |
| `public Electron2D.StreamPeer Connection { get; set; }` | Gets the attached transport or borrows a live replacement. |
| `public System.Int32 MaxResponseHeaderBytes { get; set; }` | Gets or sets the prepared response-header byte budget. |
| `public System.Int32 ReadChunkSize { get; set; }` | Gets or sets the maximum decoded body bytes returned per read. |

## Property Descriptions

<a id="member-d704e490c475"></a>
### BlockingModeEnabled

`public System.Boolean BlockingModeEnabled { get; set; }`

Gets or sets whether send/header/body operations may wait for progress.

Value: False initially; connection/DNS polling remains nonblocking.

<a id="member-bbe00bc3a312"></a>
### Connection

`public Electron2D.StreamPeer Connection { get; set; }`

Gets the attached transport or borrows a live replacement.

Value: Null while closed. Replacing the transport closes owned state and discards the previous response.

System.ArgumentNullException: A null replacement is supplied.

<a id="member-50bb0422e9f9"></a>
### MaxResponseHeaderBytes

`public System.Int32 MaxResponseHeaderBytes { get; set; }`

Gets or sets the prepared response-header byte budget.

Value: 65536 initially; 256 through 16 MiB, configurable while disconnected or idle.

<a id="member-0cede4b9f200"></a>
### ReadChunkSize

`public System.Int32 ReadChunkSize { get; set; }`

Gets or sets the maximum decoded body bytes returned per read.

Value: 65536 initially; 256 through 16 MiB.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void Close()` | Closes owned network state and forgets a borrowed connection without disposing it. |
| `public System.Void ConnectToHost(System.String host, System.Int32 port = -1, Electron2D.TLSOptions tlsOptions = null)` | Starts connection to a host, optionally through a configured proxy and TLS. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |
| `public System.Int64 GetResponseBodyLength()` | Gets the declared wire-body length. |
| `public Electron2D.HTTPResponseCode GetResponseCode()` | Gets the response status code. |
| `public System.String[] GetResponseHeaders()` | Consumes the response header snapshot. |
| `public System.Collections.Generic.Dictionary<System.String, System.String> GetResponseHeadersAsDictionary()` | Consumes response headers into a typed dictionary. |
| `public Electron2D.HTTPStatus GetStatus()` | Returns the cached HTTP phase. |
| `public System.Boolean HasResponse()` | Reports whether an unread response header snapshot exists, including headerless responses. |
| `public System.Boolean IsResponseChunked()` | Reports whether response transfer framing is chunked. |
| `public System.Void Poll()` | Advances DNS/connect/TLS/proxy and request/header progress. |
| `public static System.String QueryStringFromDict(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, System.String>> values)` | Builds an escaped typed query string, preserving input order and valueless null entries. |
| `public static System.String QueryStringFromDict(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, System.String[]>> values)` | Builds an escaped query with repeated values for each key. |
| `public System.Byte[] ReadResponseBodyChunk()` | Reads a copied decoded body prefix. |
| `public System.Int32 ReadResponseBodyChunk(System.Span<System.Byte> destination)` | Reads a decoded body prefix into reused caller storage. |
| `public System.Void Request(Electron2D.HTTPMethod method, System.String url, System.Collections.Generic.IEnumerable<System.String> headers, System.String body = "")` | Starts one request with UTF-8 body encoding. |
| `public System.Void RequestRaw(Electron2D.HTTPMethod method, System.String url, System.Collections.Generic.IEnumerable<System.String> headers, System.ReadOnlySpan<System.Byte> body)` | Starts one request with copied raw body bytes. |
| `public System.Void SetHTTPProxy(System.String host, System.Int32 port)` | Sets a plaintext forward proxy, or clears it. |
| `public System.Void SetHTTPSProxy(System.String host, System.Int32 port)` | Sets a CONNECT tunnel proxy for HTTPS, or clears it. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-9a389ab810e3"></a>
### Close

`public System.Void Close()`

Closes owned network state and forgets a borrowed connection without disposing it.

<a id="member-c7871e6241c2"></a>
### ConnectToHost

`public System.Void ConnectToHost(System.String host, System.Int32 port = -1, Electron2D.TLSOptions tlsOptions = null)`

Starts connection to a host, optionally through a configured proxy and TLS.

host: DNS/IP host, optionally prefixed by http:// or https://.

port: Remote port; negative selects 80 or 443.

tlsOptions: Client TLS configuration; null selects plaintext unless https:// is supplied.

<a id="member-69e140e66509"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-2cefe5954ca0"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Overrides append or replace descriptors; they must not yield null entries.

<a id="member-168937ec6f71"></a>
### GetResponseBodyLength

`public System.Int64 GetResponseBodyLength()`

Gets the declared wire-body length.

Returns: Minus one for chunked/close-delimited bodies; zero for bodyless responses.

<a id="member-0a3bc9e28802"></a>
### GetResponseCode

`public Electron2D.HTTPResponseCode GetResponseCode()`

Gets the response status code.

Returns: Zero before response; unknown valid numeric codes are preserved.

<a id="member-45cfab8347ff"></a>
### GetResponseHeaders

`public System.String[] GetResponseHeaders()`

Consumes the response header snapshot.

Returns: A caller-owned array preserving field order and duplicates.

<a id="member-3d7c77460133"></a>
### GetResponseHeadersAsDictionary

`public System.Collections.Generic.Dictionary<System.String, System.String> GetResponseHeadersAsDictionary()`

Consumes response headers into a typed dictionary.

Returns: Ordinal field-name keys; the last identically spelled duplicate wins.

<a id="member-6fabfa2a4a77"></a>
### GetStatus

`public Electron2D.HTTPStatus GetStatus()`

Returns the cached HTTP phase.

Returns: The last polled phase.

<a id="member-ce2be18131ff"></a>
### HasResponse

`public System.Boolean HasResponse()`

Reports whether an unread response header snapshot exists, including headerless responses.

Returns: True until a header accessor consumes it.

<a id="member-22a0fdb95f3f"></a>
### IsResponseChunked

`public System.Boolean IsResponseChunked()`

Reports whether response transfer framing is chunked.

Returns: False before a response.

<a id="member-ad2510c94d0c"></a>
### Poll

`public System.Void Poll()`

Advances DNS/connect/TLS/proxy and request/header progress.

System.IO.IOException: Transport or HTTP framing fails; the phase records the failure.

<a id="member-1646d8237aa6"></a>
### QueryStringFromDict

`public static System.String QueryStringFromDict(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, System.String>> values)`

Builds an escaped typed query string, preserving input order and valueless null entries.

values: Typed scalar query entries.

Returns: Query text without a leading question mark.

<a id="member-d065edd2817a"></a>
### QueryStringFromDict

`public static System.String QueryStringFromDict(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, System.String[]>> values)`

Builds an escaped query with repeated values for each key.

values: Typed array-valued query entries.

Returns: Query text without a leading question mark.

<a id="member-825abe1960b2"></a>
### ReadResponseBodyChunk

`public System.Byte[] ReadResponseBodyChunk()`

Reads a copied decoded body prefix.

Returns: A caller-owned array, empty while waiting or after completion.

<a id="member-8b3d9ef14231"></a>
### ReadResponseBodyChunk

`public System.Int32 ReadResponseBodyChunk(System.Span<System.Byte> destination)`

Reads a decoded body prefix into reused caller storage.

destination: Borrowed output storage.

Returns: Decoded bytes, or zero while waiting/after completion; inspect GetStatus.

<a id="member-e8fc5ffd8406"></a>
### Request

`public System.Void Request(Electron2D.HTTPMethod method, System.String url, System.Collections.Generic.IEnumerable<System.String> headers, System.String body = "")`

Starts one request with UTF-8 body encoding.

method: Request method.

url: Origin/absolute request target, CONNECT authority or OPTIONS asterisk.

headers: Field lines copied for this request.

body: UTF-8 request data.

<a id="member-26e44e257b3c"></a>
### RequestRaw

`public System.Void RequestRaw(Electron2D.HTTPMethod method, System.String url, System.Collections.Generic.IEnumerable<System.String> headers, System.ReadOnlySpan<System.Byte> body)`

Starts one request with copied raw body bytes.

method: Request method.

url: HTTP request target.

headers: Copied field lines.

body: Borrowed for this call, then copied.

<a id="member-0feda60100fd"></a>
### SetHTTPProxy

`public System.Void SetHTTPProxy(System.String host, System.Int32 port)`

Sets a plaintext forward proxy, or clears it.

host: Empty clears.

port: Minus one clears; otherwise a valid remote port.

<a id="member-c4b2450a004e"></a>
### SetHTTPSProxy

`public System.Void SetHTTPSProxy(System.String host, System.Int32 port)`

Sets a CONNECT tunnel proxy for HTTPS, or clears it.

host: Empty clears.

port: Minus one clears; otherwise a valid remote port.

<a id="member-54d459e9ca15"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Verification and limits

[HTTPTests](../../tests/Electron2D.Tests/HTTPTests.cs) verifies the exercised native Linux IPv4/DNS/TLS/proxy and public scene/worker paths, fragmented binary/framing/codec/security edges and prepared managed-allocation intervals. Native codec internals, real routed throughput, other-platform/browser delivery and owner acceptance remain separate gates. See [coverage](../coverage/classes/HTTPClient.md).

The WebSocket client consumer reuses this HTTP upgrade path and checks the parsed protocol version through an internal property; the public HTTP status/header/body contract stays as above. See [WebSocket](../components/websocket.md).
