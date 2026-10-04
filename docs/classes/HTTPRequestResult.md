# HTTPRequestResult

Last updated: 2026-10-04

**Source:** [HTTPRequestResult.cs](../../src/Core/Networking/HTTPRequestResult.cs). **Component:** [HTTP](../components/http.md).

| Name | Value | Contract |
| --- | ---: | --- |
| `BodyDecompressFailed` | 8 | Compressed content is malformed, truncated or fails checksum verification. |
| `BodySizeLimitExceeded` | 7 | Wire or decoded body size exceeds the configured limit. |
| `CantConnect` | 2 | The remote or proxy transport could not connect. |
| `CantResolve` | 3 | The remote or proxy hostname could not resolve. |
| `ChunkedBodySizeMismatch` | 1 | Response body framing is invalid or ends prematurely. |
| `ConnectionError` | 4 | The active connection failed during transfer. |
| `DownloadFileCantOpen` | 10 | The sibling download staging file could not be opened. |
| `DownloadFileWriteError` | 11 | Download writing or final destination replacement failed. |
| `NoResponse` | 6 | The peer ended the exchange before a response was available. |
| `RedirectLimitReached` | 12 | Following another redirect would exceed the configured limit. |
| `RequestFailed` | 9 | Request preparation or dispatch failed. |
| `Success` | 0 | A complete response was received; its HTTP status may still describe a server/client error. |
| `TLSHandshakeError` | 5 | TLS establishment or certificate validation failed. |
| `Timeout` | 13 | The sampled scene-process timeout elapsed. |

HTTPMethod.Max is a sentinel and rejects dispatch. HTTPResponseCode preserves an unknown valid response number through its underlying integer; zero means no response. Status and completion-result domains are distinct and follow ADR 0051.
