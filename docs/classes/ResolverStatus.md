# ResolverStatus

Last updated: 2026-10-14

**Namespace:** `Electron2D`. **Declaration:** `public enum ResolverStatus`. **Source:** [IPType.cs](../../src/Core/Networking/IPType.cs). **Component:** [Address resolution](../components/address-resolution.md).

Reports the current retained queue slot phase. It is a top-level semantic value domain distinct from socket/TLS connection state. Example: `var value = ResolverStatus.None;`.

| Declaration | Value | Contract |
| --- | --- | --- |
| `None = 0` | 0 | Unused or erased in-range slot. |
| `Waiting = 1` | 1 | Native resolution is pending. |
| `Done = 2` | 2 | Distinct matching addresses are available; slot remains occupied. |
| `Error = 3` | 3 | Lookup failure or no matching addresses; slot remains occupied. |

## Enumeration descriptions

### None

`None = 0`. Unused or erased in-range slot.

### Waiting

`Waiting = 1`. Native resolution is pending.

### Done

`Done = 2`. Distinct matching addresses are available; slot remains occupied.

### Error

`Error = 3`. Lookup failure or no matching addresses; slot remains occupied.

IPTests verifies family filtering and queue transitions, including invalid family/ID validation. ADR 0051 defines enum identity; ADR 0094 defines the resolver.
