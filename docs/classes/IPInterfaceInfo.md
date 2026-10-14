# IPInterfaceInfo

Last updated: 2026-10-14

**Namespace:** `Electron2D`. **Declaration:** `public readonly record struct IPInterfaceInfo(string Name, string Friendly, string Index, string[] Addresses)`. **Source:** [IPInterfaceInfo.cs](../../src/Core/Networking/IPInterfaceInfo.cs). **Component:** [Address resolution](../components/address-resolution.md).

## Description and example

Typed data returned by IP.GetLocalInterfaces. Each returned record contains a fresh caller-owned address array. Snapshot mutation cannot alter resolver or host state; IPv6 scope is retained in strings when provided by the host. It has no native handle or disposal ownership. Example: `foreach (var adapter in IP.GetLocalInterfaces()) Console.WriteLine(adapter.Name);`.

Record copies/with expressions copy the Addresses reference; equality/hash compare that reference rather than recursively comparing string contents. Clone the array when an independent deep copy is required. A default record has default/null reference fields; host-created snapshots provide nonnull strings/arrays. The constructor stores caller-supplied values directly.

## API summary

| Complete signature | Contract |
| --- | --- |
| `public IPInterfaceInfo(string Name, string Friendly, string Index, string[] Addresses)` | Constructs the data projection without cloning the supplied array. |
| `public string Name { get; init; }` | Host adapter identifier. |
| `public string Friendly { get; init; }` | Human-readable name; may be empty. |
| `public string Index { get; init; }` | Decimal native interface index. |
| `public string[] Addresses { get; init; }` | Caller-owned unicast IPv4/IPv6 strings. |
| `public void Deconstruct(out string Name, out string Friendly, out string Index, out string[] Addresses)` | Compiler-generated positional projection. |
| `public bool Equals(IPInterfaceInfo other)` | Compiler-generated record comparison. |
| `public override bool Equals(object? obj)` | Type-aware record comparison. |
| `public override int GetHashCode()` | Record hash; array identity participates. |
| `public override string ToString()` | Compiler-generated record diagnostic text. |
| `public static bool operator ==(IPInterfaceInfo left, IPInterfaceInfo right)` | Record equality. |
| `public static bool operator !=(IPInterfaceInfo left, IPInterfaceInfo right)` | Negated record equality. |

## Property descriptions

### Name

The native adapter identifier, not a route preference or open socket. Enumeration can change as adapters are added/removed.

### Friendly

Display name retained independently of Name. No locale- or platform-independent spelling is guaranteed.

### Index

A decimal string matching the typed adapter field role. Native IPv4 index is selected where supported, otherwise IPv6 index, with zero for neither family. IPTests checks actual host numeric strings.

### Addresses

A newly enumerated array of native unicast address strings. Link-local IPv6 can contain a scope suffix. Changing this array affects only its snapshot and record copies sharing that reference.

## Method and operator descriptions

The positional constructor and Deconstruct retain field order Name/Friendly/Index/Addresses. Equals, object Equals, GetHashCode, == and != use normal record value-field comparison with array reference equality. ToString is diagnostic text, not a file or network format. They neither enumerate interfaces nor mutate native state; no generated method implies deep-copy or persistent hash stability.

## Verification and limits

IPTests reads real Linux adapters and addresses and validates numeric indexes and address syntax. Snapshot arrays and constructor values are data, not native ownership tokens. Foreign adapter names/indexes and hardware configurations remain host-specific execution gates. ADR 0001 supplies the typed dictionary projection; ADR 0094 owns native behavior.
