# IPType

Last updated: 2026-10-14

**Namespace:** `Electron2D`. **Declaration:** `public enum IPType`. **Source:** [IPType.cs](../../src/Core/Networking/IPType.cs). **Component:** [Address resolution](../components/address-resolution.md).

Selects an address-family policy for hostname resolution. It is a top-level semantic value domain distinct from socket/TLS connection state. Example: `var value = IPType.None;`.

| Declaration | Value | Contract |
| --- | --- | --- |
| `None = 0` | 0 | Host unspecified-family policy; the native Unix profile behaves like Any. |
| `IPv4 = 1` | 1 | IPv4 addresses only. |
| `IPv6 = 2` | 2 | IPv6 addresses only. |
| `Any = 3` | 3 | Any supported family; default. |

## Enumeration descriptions

### None

`None = 0`. Host unspecified-family policy; the native Unix profile behaves like Any.

### IPv4

`IPv4 = 1`. IPv4 addresses only.

### IPv6

`IPv6 = 2`. IPv6 addresses only.

### Any

`Any = 3`. Any supported family; default.

IPTests verifies family filtering and queue transitions, including invalid family/ID validation. ADR 0051 defines enum identity; ADR 0094 defines the resolver.
