# RenderingDevice.VertexFrequency

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.VertexFrequency`. **Source:** [RenderingDevice.GraphicsEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.GraphicsEnums.cs). **Component:** [Local device](../components/local-compute.md).

## Description

Identifies VertexFrequency values for executable graphics operations. Numeric identities are retained. Unknown or extent selectors reject when consumed. Only executable roles are accepted by the current device; missing backend/attachment families remain coverage prerequisites.

## Example

Use this selector through the owning [RenderingDevice](RenderingDevice.md) operation. The [complete public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceGraphicsTests.cs) exercises current numeric roles.

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.VertexFrequency Instance = 1` | Selects Instance. |
| `public const Electron2D.RenderingDevice.VertexFrequency Vertex = 0` | Selects Vertex. |

## Member descriptions

<a id="member-e1c7f6a92124"></a>
### Instance

```csharp
public const Electron2D.RenderingDevice.VertexFrequency Instance = 1
```

Selects Instance.


<a id="member-af63c7b52995"></a>
### Vertex

```csharp
public const Electron2D.RenderingDevice.VertexFrequency Vertex = 0
```

Selects Vertex.

## Lifetime and verification

Device-owned resources remain on their creating thread. Descriptions are caller-owned and copied at creation; inherited disposal guards apply. Invalid configurations, foreign/freed RIDs and list-state errors reject explicitly. Native creation failures propagate. RenderingDeviceGraphicsTests checks Linux/Vulkan pixels, resource storage, lifetime and prepared owner-thread allocation boundaries. Other platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/RenderingDevice.md) for absent roles.
