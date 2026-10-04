# TLSStatus

Last updated: 2026-10-04

**Source:** [StreamPeerTLS.cs](../../src/Core/Networking/StreamPeerTLS.cs). **Component:** [TLS](../components/tls.md).

| Name | Value | Contract |
| --- | ---: | --- |
| `Connected` | 2 | Authenticated TLS application bytes or DTLS packets can be transferred. |
| `Disconnected` | 0 | No TLS or DTLS session is attached. |
| `Error` | 3 | The session failed; the borrowed transport remains caller-owned. |
| `ErrorHostnameMismatch` | 4 | Certificate validation failed because the expected DNS name or IP did not match. |
| `Handshaking` | 1 | The polled TLS or DTLS handshake is in progress. |

This same status domain also describes [PacketPeerDTLS](PacketPeerDTLS.md), preserving all five identities under ADR 0051. Connected means authenticated application bytes or complete packets; Disconnected/Handshaking/Error/name mismatch have the same session meaning across the two transports. [DTLSTests](../../tests/Electron2D.Tests/DTLSTests.cs) exercises the DTLS transitions and trust failures.
