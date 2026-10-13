# Performance.CustomMonitor<TValue>

Last updated: 2026-10-13

**Namespace:** Electron2D · **Declaration:** `public sealed class Performance.CustomMonitor<TValue>` · **Source:** [Performance.CustomMonitors.cs](../../src/Core/Config/Performance.CustomMonitors.cs).

## Description

Immutable typed identity returned by Performance.AddCustomMonitor. TValue is the exact provider result type. There is no public constructor; the retained service issues the handle. Removing its ID invalidates it and clears the provider, so a caller may retain stale metadata without retaining the callback's captured objects. Reregistering the ID creates a new identity. Mutable returned objects remain application-owned.

## Example

```csharp
var key = Performance.AddCustomMonitor("Game/Actors", () => actorCount);
int actors = Performance.GetCustomMonitor(key);
Performance.RemoveCustomMonitor(key.ID);
```

The snippet requires an application integer variable actorCount whose access obeys its own threading policy.

## Property summary

| Signature | Contract |
| --- | --- |
| `public string ID { get; }` | Exact ordinal registration ID. |
| `public Performance.MonitorType Type { get; }` | Display unit assigned at registration. |

## Property descriptions

<a id="id"></a>
### ID

Gets the original ID, including empty or slash-delimited IDs. This metadata remains readable after removal; HasCustomMonitor(ID) describes current name membership, while GetCustomMonitor(key) also validates this exact generation.

<a id="type"></a>
### Type

Gets immutable Quantity, Memory, Time or Percentage presentation metadata. It neither converts nor validates the provider result. See [runtime diagnostics](../components/runtime-diagnostics.md) for units, lifetime, concurrency, tests and limits.
