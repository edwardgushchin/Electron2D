# CompressedTextureArray

Last updated: 2026-10-16

Source: [CompressedTextureArray.cs](../../src/Scene/Resources/CompressedTextureArray.cs)

```csharp
public sealed class CompressedTextureArray : CompressedTextureLayered
```

Inherits: [CompressedTextureLayered](CompressedTextureLayered.md).

## Description and example

Concrete independent-array file resource. All loading, channel/mip validation,
metadata, copied layer access and lifetime follow the base class. ResourceLoader
recognizes `.ctexarray`; typed archives persist original pixels without depending
on that file at restore time. The inherited LayeredType is Array. This resource
has no ordinary Sprite drawing or mutable image-array authoring methods.

```csharp
using var texture = ResourceLoader.Load<CompressedTextureArray>("res://terrain.ctexarray");
using var pixels = texture.GetLayerData(0);
```

The snippet requires an existing imported array asset.

## Members

| Declaration | Contract |
| --- | --- |
| `public CompressedTextureArray()` | Creates an uninitialized resource with empty LoadPath and zero dimensions/layers. |
| `protected override Resource CreateDuplicateInstance()` | Creates an independent empty concrete target for inherited Resource duplication. |

The duplicate factory transfers no native identity. Inherited copying supplies
the immutable pixels and LoadPath; future reloads are independent. See the
[component](../components/compressed-arrays.md) for dependencies, invariants,
executed tests and explicit renderer/platform limitations.

### CompressedTextureArray()

Creates an empty concrete array. Load or assign LoadPath before sampling or reading
layers; metadata queries are available immediately. Dispose releases its logical
resource identity and any later renderer storage through inherited ownership.

### CreateDuplicateInstance()

Protected Resource duplication hook. Returns a newly constructed exact-type target;
it does not copy data itself. The graph session owns cleanup on later copy failure,
and the inherited CopyCustomStateTo publishes the source snapshot without I/O.

[ETC/EAC/ASTC decoding](../components/image-block-decoding.md) now supplies prepared pixels for these texture snapshots while retaining original block formats, bytes and all mips. Signed EAC and ASTC HDR use floating-point upload; the current Compatibility driver rejects that precision. Integer sampling remains a separate integration.
