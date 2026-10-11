# RenderingDevice.IndexBufferFormat

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.IndexBufferFormat`. **Source:** [RenderingDevice.GraphicsEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.GraphicsEnums.cs). **Component:** [Local device](../components/local-compute.md).

## Description

Identifies IndexBufferFormat values for executable graphics operations. Numeric identities are retained. Unknown or extent selectors reject when consumed. Only executable roles are accepted by the current device; missing backend/attachment families remain coverage prerequisites.

## Example

Use this selector through the owning [RenderingDevice](RenderingDevice.md) operation. The [complete public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceGraphicsTests.cs) exercises current numeric roles.

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.IndexBufferFormat UInt16 = 0` | Selects UInt16. |
| `public const Electron2D.RenderingDevice.IndexBufferFormat UInt32 = 1` | Selects UInt32. |

## Member descriptions

<a id="member-c6d8f1342adb"></a>
### UInt16

```csharp
public const Electron2D.RenderingDevice.IndexBufferFormat UInt16 = 0
```

Selects UInt16.


<a id="member-738958c8daa6"></a>
### UInt32

```csharp
public const Electron2D.RenderingDevice.IndexBufferFormat UInt32 = 1
```

Selects UInt32.

## Lifetime and verification

Device-owned resources remain on their creating thread. Descriptions are caller-owned and copied at creation; inherited disposal guards apply. Invalid configurations, foreign/freed RIDs and list-state errors reject explicitly. Native creation failures propagate. RenderingDeviceGraphicsTests checks Linux/Vulkan pixels, resource storage, lifetime and prepared owner-thread allocation boundaries. Other platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/RenderingDevice.md) for absent roles.
