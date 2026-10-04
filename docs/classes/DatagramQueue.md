# DatagramQueue

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [DatagramQueue.cs](../../src/Core/Networking/DatagramQueue.cs). **Component:** [Networking](../components/networking.md).

## Responsibilities and invariants

Prepared circular payload storage and value-record array. Charges payload plus 24 bytes against the rounded receive budget, retains IPv6 scope and whole-packet FIFO order, drops overflow without partially queueing a datagram, and reuses capacities on clear.

## Verification

NetworkingTests exercises this helper through native loopback transports and caller-span packet cycles. See [ADR 0094](../decisions/networking.md#adr-0094) for limits and [the component](../components/networking.md) for measured boundaries.
