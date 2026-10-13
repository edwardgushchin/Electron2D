# Performance.MonitorType

Last updated: 2026-10-13

**Namespace:** Electron2D · **Declaration:** `public enum Performance.MonitorType` · **Source:** [Performance.CustomMonitors.cs](../../src/Core/Config/Performance.CustomMonitors.cs).

## Description

Presentation unit for a custom runtime monitor. The service retains and reports this metadata without converting raw typed results. Undefined values reject at registration.

## Values

| Value | Number | Contract |
| --- | --- | --- |
| Quantity | 0 | Integer-style display of a unitless count. |
| Memory | 1 | Submitted numeric value describes bytes. |
| Time | 2 | Submitted numeric value describes seconds, for millisecond display. |
| Percentage | 3 | Submitted numeric value is a fractional ratio, such as 0.5 for 50 percent. |

<a id="quantity"></a>
### Quantity

Default unit. It does not round the returned value.

<a id="memory"></a>
### Memory

Byte unit. It does not measure application memory or own native allocations.

<a id="time"></a>
### Time

Second unit. It does not advance or measure an engine frame by itself.

<a id="percentage"></a>
### Percentage

Fractional ratio. Range validation and formatting belong to the numeric display client.

## Example

```csharp
var memory = Performance.AddCustomMonitor("Game/Cache", () => cacheBytes,
    Performance.MonitorType.Memory);
```

Requires an application cacheBytes value. See [runtime diagnostics](../components/runtime-diagnostics.md) for the complete callback lifecycle and verification.
