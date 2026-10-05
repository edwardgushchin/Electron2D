# NetworkSockets

Last updated: 2026-10-06

**Visibility:** internal. **Source:** [NetworkSockets.cs](../../src/Core/Networking/NetworkSockets.cs). **Component:** [Networking](../components/networking.md).

## Responsibilities and invariants

Private socket creation, host resolution, address/family normalization and port validation. Linux listener setup pins SafeHandle for one SO_REUSEADDR call without SO_REUSEPORT; other native hosts use exclusive binding. Browser raw sockets reject explicitly.

Datagram construction raises native send/receive buffer sizes to at least 65536 bytes, retaining larger OS defaults. Both standalone UDP peers and shared UDP listeners use this path, so one maximum IPv4 datagram fits even when the OS's original send buffer is smaller. Buffer-configuration errors close the candidate socket and propagate.

On Windows, datagram creation disables SIO_UDP_CONNRESET using Socket.IOControl. ICMP port-unreachable replies cannot reset shared UDP/ENet listeners when one remote endpoint departs. TCP reset handling and other native socket failures are unchanged; option failures close the candidate socket and propagate.

## Verification

ReceiveDatagram restores the reusable SocketAddress.Size to the socket family's maximum before ReceiveFrom overwrites it with the native result. UDPServer, unconnected PacketPeerUDP and plain ENet receives share this path. NetworkingTests deliberately shrinks IPv4/IPv6 address sizes before two real receives and verifies payload/sender metadata; connected UDP reads use their retained remote endpoint instead.

NetworkingTests exercises this helper through native loopback transports and caller-span packet cycles. See [ADR 0094](../decisions/networking.md#adr-0094) for limits and [the component](../components/networking.md) for measured boundaries.

Its IPv4/IPv6 peer-departure regression sends to a closed UDP port and verifies a healthy sender's payload/metadata. A Windows-only reset-enabled negative control requires a real ICMP-triggered ConnectionReset; that platform branch requires Windows execution.
