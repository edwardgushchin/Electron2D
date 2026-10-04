# StreamSocketStatus

Last updated: 2026-10-04

**Source:** [StreamPeerSocket.cs](../../src/Core/Networking/StreamPeerSocket.cs). **Component:** [Networking](../components/networking.md).

| Name | Value | Contract |
| --- | ---: | --- |
| `Connected` | 2 | The socket can transfer stream bytes. |
| `Connecting` | 1 | A nonblocking connection attempt is in progress. |
| `Error` | 3 | The last polled connection failed. |
| `None` | 0 | No active connection, including graceful remote closure. |
