# NetworkSockets

Last updated: 2026-10-05

**Visibility:** internal. **Source:** [NetworkSockets.cs](../../src/Core/Networking/NetworkSockets.cs). **Component:** [Networking](../components/networking.md).

## Responsibilities and invariants

Private socket creation, host resolution, address/family normalization and port validation. Linux listener setup pins SafeHandle for one SO_REUSEADDR call without SO_REUSEPORT; other native hosts use exclusive binding. Browser raw sockets reject explicitly.

Datagram construction raises native send/receive buffer sizes to at least 65536 bytes, retaining larger OS defaults. Both standalone UDP peers and shared UDP listeners use this path, so one maximum IPv4 datagram fits even when the OS's original send buffer is smaller. Buffer-configuration errors close the candidate socket and propagate.

## Verification

NetworkingTests exercises this helper through native loopback transports and caller-span packet cycles. See [ADR 0094](../decisions/networking.md#adr-0094) for limits and [the component](../components/networking.md) for measured boundaries.
