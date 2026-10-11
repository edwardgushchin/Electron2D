# RenderingDevice.BlendOperation

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.BlendOperation`. **Source:** [RenderingDevice.GraphicsEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.GraphicsEnums.cs). **Component:** [Local device](../components/local-compute.md).

## Description

Identifies BlendOperation values for executable graphics operations. Numeric identities are retained. Unknown or extent selectors reject when consumed. Only executable roles are accepted by the current device; missing backend/attachment families remain coverage prerequisites.

## Example

Use this selector through the owning [RenderingDevice](RenderingDevice.md) operation. The [complete public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceGraphicsTests.cs) exercises current numeric roles.

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.BlendOperation Add = 0` | Selects Add. |
| `public const Electron2D.RenderingDevice.BlendOperation Max = 5` | Marks the exclusive numeric extent; it is not an executable selection. |
| `public const Electron2D.RenderingDevice.BlendOperation Maximum = 4` | Selects Maximum. |
| `public const Electron2D.RenderingDevice.BlendOperation Minimum = 3` | Selects Minimum. |
| `public const Electron2D.RenderingDevice.BlendOperation ReverseSubtract = 2` | Selects ReverseSubtract. |
| `public const Electron2D.RenderingDevice.BlendOperation Subtract = 1` | Selects Subtract. |

## Member descriptions

<a id="member-08bfd20c1a40"></a>
### Add

```csharp
public const Electron2D.RenderingDevice.BlendOperation Add = 0
```

Selects Add.


<a id="member-1b43ee6286da"></a>
### Max

```csharp
public const Electron2D.RenderingDevice.BlendOperation Max = 5
```

Marks the exclusive numeric extent; it is not an executable selection.


<a id="member-b4e01bd87905"></a>
### Maximum

```csharp
public const Electron2D.RenderingDevice.BlendOperation Maximum = 4
```

Selects Maximum.


<a id="member-b9f1155556fd"></a>
### Minimum

```csharp
public const Electron2D.RenderingDevice.BlendOperation Minimum = 3
```

Selects Minimum.


<a id="member-38b598ebd007"></a>
### ReverseSubtract

```csharp
public const Electron2D.RenderingDevice.BlendOperation ReverseSubtract = 2
```

Selects ReverseSubtract.


<a id="member-3cc5f5d9a140"></a>
### Subtract

```csharp
public const Electron2D.RenderingDevice.BlendOperation Subtract = 1
```

Selects Subtract.

## Lifetime and verification

Device-owned resources remain on their creating thread. Descriptions are caller-owned and copied at creation; inherited disposal guards apply. Invalid configurations, foreign/freed RIDs and list-state errors reject explicitly. Native creation failures propagate. RenderingDeviceGraphicsTests checks Linux/Vulkan pixels, resource storage, lifetime and prepared owner-thread allocation boundaries. Other platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/RenderingDevice.md) for absent roles.
