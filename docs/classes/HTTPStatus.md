# HTTPStatus

Last updated: 2026-10-04

**Source:** [HTTPStatus.cs](../../src/Core/Networking/HTTPStatus.cs). **Component:** [HTTP](../components/http.md).

| Name | Value | Contract |
| --- | ---: | --- |
| `Body` | 7 | Response headers are parsed; response body progress is caller-driven. |
| `CantConnect` | 4 | All attempted transport connections failed. |
| `CantResolve` | 2 | DNS lookup produced no usable address or failed. |
| `Connected` | 5 | The transport is ready for a new request or a completed bodyless response. |
| `Connecting` | 3 | TCP, proxy negotiation or TLS establishment is progressing. |
| `ConnectionError` | 8 | Request transmission or response framing/transport failed. |
| `Disconnected` | 0 | No active transport connection is attached. |
| `Requesting` | 6 | Request transmission or response-header parsing is progressing. |
| `Resolving` | 1 | An asynchronous hostname lookup is pending. |
| `TLSHandshakeError` | 9 | TLS establishment or certificate validation failed. |

HTTPMethod.Max is a sentinel and rejects dispatch. HTTPResponseCode preserves an unknown valid response number through its underlying integer; zero means no response. Status and completion-result domains are distinct and follow ADR 0051.
