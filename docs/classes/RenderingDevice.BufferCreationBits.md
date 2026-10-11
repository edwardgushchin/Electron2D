# RenderingDevice.BufferCreationBits

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.BufferCreationBits`. **Source:** [RenderingDevice.GraphicsEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.GraphicsEnums.cs). **Component:** [Local device](../components/local-compute.md).

## Description

Identifies BufferCreationBits values for executable graphics operations. Numeric identities are retained. Unknown or extent selectors reject when consumed. Only executable roles are accepted by the current device; missing backend/attachment families remain coverage prerequisites.

## Example

Use this selector through the owning [RenderingDevice](RenderingDevice.md) operation. The [complete public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceGraphicsTests.cs) exercises current numeric roles.

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.BufferCreationBits AsStorage = 2` | Selects AsStorage. |

## Member descriptions

<a id="member-59c04a8566e2"></a>
### AsStorage

```csharp
public const Electron2D.RenderingDevice.BufferCreationBits AsStorage = 2
```

Selects AsStorage.

## Lifetime and verification

Device-owned resources remain on their creating thread. Descriptions are caller-owned and copied at creation; inherited disposal guards apply. Invalid configurations, foreign/freed RIDs and list-state errors reject explicitly. Native creation failures propagate. RenderingDeviceGraphicsTests checks Linux/Vulkan pixels, resource storage, lifetime and prepared owner-thread allocation boundaries. Other platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/RenderingDevice.md) for absent roles.
