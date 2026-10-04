# HTTPMethod

Last updated: 2026-10-04

**Source:** [HTTPMethod.cs](../../src/Core/Networking/HTTPMethod.cs). **Component:** [HTTP](../components/http.md).

| Name | Value | Contract |
| --- | ---: | --- |
| `Connect` | 7 | Opens an authority-form tunnel; successful responses preserve following transport bytes. |
| `Delete` | 4 | Requests removal of the target resource. |
| `Get` | 0 | Retrieves the target representation. |
| `Head` | 1 | Retrieves response metadata without consuming a response body. |
| `Max` | 9 | Bounds the method domain; this sentinel cannot dispatch a request. |
| `Options` | 5 | Queries communication options, including the asterisk target. |
| `Patch` | 8 | Applies a partial change with caller-supplied body bytes. |
| `Post` | 2 | Submits caller-supplied content to the target. |
| `Put` | 3 | Replaces the target representation with caller-supplied content. |
| `Trace` | 6 | Requests a diagnostic loopback exchange. |

HTTPMethod.Max is a sentinel and rejects dispatch. HTTPResponseCode preserves an unknown valid response number through its underlying integer; zero means no response. Status and completion-result domains are distinct and follow ADR 0051.
