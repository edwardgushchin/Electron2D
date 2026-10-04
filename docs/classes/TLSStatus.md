# TLSStatus

Last updated: 2026-10-04

**Source:** [StreamPeerTLS.cs](../../src/Core/Networking/StreamPeerTLS.cs). **Component:** [TLS](../components/tls.md).

| Name | Value | Contract |
| --- | ---: | --- |
| `Connected` | 2 | Authenticated TLS application bytes can be transferred. |
| `Disconnected` | 0 | No TLS session is attached. |
| `Error` | 3 | The session failed; the borrowed transport remains caller-owned. |
| `ErrorHostnameMismatch` | 4 | Certificate validation failed because the expected DNS name or IP did not match. |
| `Handshaking` | 1 | The polled TLS handshake is in progress. |
