# NetworkSockets

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [NetworkSockets.cs](../../src/Core/Networking/NetworkSockets.cs). **Component:** [Networking](../components/networking.md).

## Responsibilities and invariants

Private socket creation, host resolution, address/family normalization and port validation. Linux listener setup pins SafeHandle for one SO_REUSEADDR call without SO_REUSEPORT; other native hosts use exclusive binding. Browser raw sockets reject explicitly.

## Verification

NetworkingTests exercises this helper through native loopback transports and caller-span packet cycles. See [ADR 0094](../decisions/networking.md#adr-0094) for limits and [the component](../components/networking.md) for measured boundaries.
