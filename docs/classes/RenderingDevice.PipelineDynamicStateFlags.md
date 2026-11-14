# RenderingDevice.PipelineDynamicStateFlags

Last updated: 2026-11-14

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.PipelineDynamicStateFlags`. **Source:** [RenderingDevice.GraphicsEnums.cs](../../src/Servers/Rendering/Device/RenderingDevice.GraphicsEnums.cs). **Component:** [Local device](../components/local-compute.md).

## Description

Selects pipeline states that may change during a draw list. Numeric identities are retained. Unknown or extent selectors reject when consumed. Only executable roles are accepted by the current device; missing backend/attachment families remain coverage prerequisites.

## Example

Use this selector through the owning [RenderingDevice](RenderingDevice.md) operation. The [complete public-API consumer](../../tests/Electron2D.Tests/RenderingDeviceGraphicsTests.cs) exercises current numeric roles.

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.PipelineDynamicStateFlags LineWidth = 1` | Enables draw-list line width in pixels. |
| `public const Electron2D.RenderingDevice.PipelineDynamicStateFlags DepthBias = 2` | Enables draw-list depth bias factors and clamp. |
| `public const Electron2D.RenderingDevice.PipelineDynamicStateFlags BlendConstants = 4` | Allows DrawListSetBlendConstants to replace the copied pipeline constant. |
| `public const Electron2D.RenderingDevice.PipelineDynamicStateFlags DepthBounds = 8` | Enables draw-list stored-depth bounds. |
| `public const Electron2D.RenderingDevice.PipelineDynamicStateFlags StencilCompareMask = 16` | Enables separate draw-list comparison masks for both faces. |
| `public const Electron2D.RenderingDevice.PipelineDynamicStateFlags StencilWriteMask = 32` | Enables separate draw-list write masks for both faces. |
| `public const Electron2D.RenderingDevice.PipelineDynamicStateFlags StencilReference = 64` | Enables separate draw-list references for both faces. |

## Member descriptions

<a id="member-e7827370dbb9"></a>
### LineWidth

Allows [DrawListSetLineWidth](RenderingDevice.md#drawlistsetlinewidth) to replace the copied raster width.

<a id="member-36519af95e6c"></a>
### DepthBias

Allows [DrawListSetDepthBias](RenderingDevice.md#drawlistsetdepthbias) to replace the copied bias factors and clamp when depth bias is enabled.

<a id="member-f1b85bb3145f"></a>
### DepthBounds

Allows [DrawListSetDepthBounds](RenderingDevice.md#drawlistsetdepthbounds) to replace the copied range when depth bounds testing is enabled.

<a id="member-908dc4ea305d"></a>
### BlendConstants

```csharp
public const Electron2D.RenderingDevice.PipelineDynamicStateFlags BlendConstants = 4
```

Allows DrawListSetBlendConstants to replace the copied pipeline constant.

<a id="member-c925e244308a"></a>
### StencilCompareMask

Allows [DrawListSetStencilCompareMask](RenderingDevice.md#drawlistsetstencilcomparemask) to replace the copied comparison masks.

<a id="member-59d0862d10a7"></a>
### StencilWriteMask

Allows [DrawListSetStencilWriteMask](RenderingDevice.md#drawlistsetstencilwritemask) to replace the copied write masks.

<a id="member-b31349d138d4"></a>
### StencilReference

Allows [DrawListSetStencilReference](RenderingDevice.md#drawlistsetstencilreference) to replace the copied references.

## Lifetime and verification

Device-owned resources remain on their creating thread. Descriptions are caller-owned and copied at creation; inherited disposal guards apply. Invalid configurations, foreign/freed RIDs and list-state errors reject explicitly. Native creation failures propagate. RenderingDeviceGraphicsTests checks Linux/Vulkan pixels, resource storage, lifetime and prepared owner-thread allocation boundaries. Other platforms and native/driver allocations remain unverified. See [ADR 0028](../decisions/rendering.md#adr-0028) and [coverage](../coverage/classes/RenderingDevice.md) for absent roles.
