# DatagramAddress

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [DatagramQueue.cs](../../src/Core/Networking/DatagramQueue.cs). **Component:** [Networking](../components/networking.md).

## Responsibilities and invariants

Immutable sender endpoint key containing IPv6 address bits, scope and port. Captures reused SocketAddress bytes without allocating an EndPoint per packet; IP presentation/endpoint construction is cold.

## Verification

NetworkingTests exercises this helper through native loopback transports and caller-span packet cycles. See [ADR 0094](../decisions/networking.md#adr-0094) for limits and [the component](../components/networking.md) for measured boundaries.
