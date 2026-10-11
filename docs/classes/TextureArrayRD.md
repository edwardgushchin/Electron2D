# TextureArrayRD

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.TextureArrayRD`. **Source:** [TextureArrayRD.cs](../../src/Scene/Resources/TextureArrayRD.cs). **Component:** [Local compute](../components/local-compute.md#device-images-in-canvas).

**Inherits:** [TextureLayeredRD](TextureLayeredRD.md).

## Description

Concrete independent device image-array consumer. TextureRDRID and all metadata/readback behavior are inherited from TextureLayeredRD; the constructor creates an unbound array. Integer canvas sampling needs the matching material descriptor interface.

The binding borrows device pixels; disposing the resource or freeing a server wrapper never frees the source image. Freeing the device image empties live metadata; renderer shutdown expires the device. Canvas drawing samples native storage directly with no CPU frame bridge. Snapshots require CPURead or CanCopyFrom, synchronize device work and copy every mip. Representable formats retain encoded data; other sampled color formats convert on GPU to floating RGBA. Resource archives reconstruct an unbound object; duplication borrows the same image. Mutating bindings and disposing live bindings obey device owner/submission guards.

## Example

The snippet runs inside an active GPU session with an already-created sampled global-device image; snapshot access additionally requires a readable role.

```csharp
using var layers = new TextureArrayRD { TextureRDRID = sampledArrayImage };
shader.SetDefaultLayeredTextureParameter("layers", layers);
```

## Members

| Member | Kind | Summary |
| --- | --- | --- |
| [`public TextureArrayRD()`](#member-96f2c183edb1) | constructor | Creates an unbound device image array. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-df94a6acf48b) | method | Overrides the inherited resource metadata/readback or managed lifetime contract. |

## Member reference

<a id="member-96f2c183edb1"></a>

### .ctor

```csharp
public TextureArrayRD()
```

Creates an unbound device image array.


<a id="member-df94a6acf48b"></a>

### CreateDuplicateInstance

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

Uses the inherited [TextureLayeredRD](TextureLayeredRD.md) contract with the device image behavior described above.

