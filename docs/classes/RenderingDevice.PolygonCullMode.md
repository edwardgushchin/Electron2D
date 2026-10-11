# RenderingDevice.PolygonCullMode

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.PolygonCullMode`. **Source:** [RenderingDevice.GraphicsEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.GraphicsEnums.cs). **Component:** [Local device](../components/local-compute.md).

## Description

Identifies PolygonCullMode values for executable graphics operations. Numeric identities are retained. Unknown or extent selectors reject when consumed. Only executable roles are accepted by the current device; missing backend/attachment families remain coverage prerequisites.

## Example

Use this selector through the owning [RenderingDevice](RenderingDevice.md) operation. The [complete public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceGraphicsTests.cs) exercises current numeric roles.

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.PolygonCullMode Back = 2` | Selects Back. |
| `public const Electron2D.RenderingDevice.PolygonCullMode Disabled = 0` | Selects Disabled. |
| `public const Electron2D.RenderingDevice.PolygonCullMode Front = 1` | Selects Front. |

## Member descriptions

<a id="member-fdc78d5428b5"></a>
### Back

```csharp
public const Electron2D.RenderingDevice.PolygonCullMode Back = 2
```

Selects Back.


<a id="member-9516c6f11bfc"></a>
### Disabled

```csharp
public const Electron2D.RenderingDevice.PolygonCullMode Disabled = 0
```

Selects Disabled.


<a id="member-a60bc04535f4"></a>
### Front

```csharp
public const Electron2D.RenderingDevice.PolygonCullMode Front = 1
```

Selects Front.

## Lifetime and verification

Device-owned resources remain on their creating thread. Descriptions are caller-owned and copied at creation; inherited disposal guards apply. Invalid configurations, foreign/freed RIDs and list-state errors reject explicitly. Native creation failures propagate. RenderingDeviceGraphicsTests checks Linux/Vulkan pixels, resource storage, lifetime and prepared owner-thread allocation boundaries. Other platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/RenderingDevice.md) for absent roles.
