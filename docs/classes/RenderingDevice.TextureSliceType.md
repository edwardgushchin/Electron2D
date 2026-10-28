# RenderingDevice.TextureSliceType

Last updated: 2026-10-28

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.RenderingDevice.TextureSliceType`. **Source:** [RenderingDevice.TextureViews.cs](../../src/Servers/Rendering/Device/RenderingDevice.TextureViews.cs). **Component:** [Shared texture views](../components/shared-texture-views.md).

## Description

Selects the ordinary image role for TextureCreateSharedFromSlice. Layer and mip indices refer to the original owner even when the source RID is already a view. The factory validates the selector and complete positive interval before publishing a view.

## Example

Given a live `device`, `owner` and caller-owned `view`:

```csharp
var slice = device.TextureCreateSharedFromSlice(view, owner, 0, 0,
    sliceType: RenderingDevice.TextureSliceType.Texture);
device.FreeRID(slice);
```

## API

| Declaration | Contract |
| --- | --- |
| `public const Electron2D.RenderingDevice.TextureSliceType Texture = 0` | One ordinary layer and the requested mip interval. |

## Enumeration descriptions

<a id="texture"></a>
### Texture

Selects one original image layer. Width and height shrink to the selected first mip; the result has one layer. Other numeric selector values throw ArgumentOutOfRangeException.

## Lifetime and verification

This value owns no resource. The factory returns a device RID automatically freed with its original owner. See the component for native checks, platform limits and dependent usage roles.
