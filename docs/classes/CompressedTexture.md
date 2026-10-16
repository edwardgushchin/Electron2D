# CompressedTexture

Last updated: 2026-10-16

Source: [CompressedTexture.cs](../../src/Scene/Resources/CompressedTexture.cs)

```csharp
public sealed class CompressedTexture : Texture
```

Inherits: [Texture](Texture.md). Component: [Imported image textures](../components/compressed-textures.md).

## Description

Loads one ordinary texture from a bounded imported `.ctex` asset. The logical
drawing dimensions can differ from the stored image dimensions. The resource
retains immutable copied source pixels, including their format and complete mip
chain, and a prepared sampling snapshot. GetImage returns an independent copy.
The renderer owns native storage; consumers borrow this resource. Load retains
the resource RID while replacing storage and expiring old borrowed device views.

The default resource has zero logical dimensions, no image, empty LoadPath, L8
PixelFormat and no mipmaps. HasAlpha always reports false for this imported type;
IsPixelOpaque still tests actual decoded alpha. This flag does not discard alpha.
Metadata, images, duplication and archives work without an active renderer.
Per-call state access synchronizes. Callers coordinate compound edits/disposal;
notifications execute synchronously on the loading thread outside the state lock.

## Example

Requires an existing imported texture file. Nodes borrow the texture; keep it
alive while they draw it.

```csharp
using var texture = ResourceLoader.Load<CompressedTexture>("res://character.ctex");
using var sprite = new Sprite { Texture = texture };
using var image = texture.GetImage(); // Original physical pixels and all mips.
Vector2 drawingSize = texture.GetSize();
```

## Constructors, properties and methods

| Declaration | Contract |
| --- | --- |
| `public CompressedTexture()` | Creates an empty resource. |
| `public string LoadPath { get; set; }` | Last successful input path; assignment invokes Load. Stored descriptor; initially empty. |
| `public override bool HasAlpha { get; }` | False; per-pixel opacity still uses actual alpha. |
| `public void Load(string path)` | Prepares and atomically publishes the complete imported asset. |
| `public override int GetWidth()` | Logical width, initially zero. |
| `public override int GetHeight()` | Logical height, initially zero. |
| `public override Vector2 GetSize()` | One synchronized read of the logical drawing size. |
| `public override Image? GetImage()` | Independent original image, or null while empty. |
| `public override bool IsPixelOpaque(int x, int y)` | Clamped logical-coordinate alpha test. |

PixelFormat, HasMIPMaps, MIPMapCount, GetRID, Draw, DrawRect and DrawRectRegion are
inherited from Texture. ResourceLocalToScene is the ordinary inherited Resource
property with default false; this class does not shadow it. No manual SetImage,
Update or logical-size mutators are exposed by this file-backed resource.

## Constructor and property descriptions

### CompressedTexture()

Creates no pixels or native storage. Its lazily allocated inherited RID is valid
even while empty and remains stable until resource disposal.

### LoadPath

Initially empty. The getter returns the exact last successful Load argument;
the setter has all Load side effects and exceptions. It differs from ResourcePath,
which identifies the resource in a cache/archive. Duplicating or restoring state
copies LoadPath without invoking its setter or reopening the source file.

### HasAlpha

Returns false for empty and populated resources, including RGBA and alpha-bearing
compressed sources. Original alpha remains in GetImage, sampling and IsPixelOpaque.
Disposed access throws ObjectDisposedException.

## Method descriptions

### Load(string path)

Accepts operating-system, res:// and user:// paths. A GST2 version zero or one
contains positive logical dimensions and one raw, PNG/WebP mip or Basis LDR/HDR
image. Stored image dimensions are independent. Base-only or full mip chains
execute; streaming and size hints do not remove mips because Load has no requested
size argument. See the component page for channel roles and storage limits.

Parsing, decoding and snapshot construction precede publication. Failure preserves
the previous image, logical size, LoadPath and RID without notifying observers.
Success publishes all state, then invokes PropertyListChanged before Changed.
Subscriber exceptions propagate after publication; state remains committed.
ResourcePath assignment, including clearing it, updates the renderer label.
Direct loading uses the input path as a temporary label when ResourcePath is empty.

Null/empty paths throw ArgumentException; invalid/truncated/oversized assets throw
InvalidDataException. Read and codec working-memory failures use IOException.
Unsupported sampling formats throw NotSupportedException. Block codec working
budgets use InvalidOperationException. Disposed access throws ObjectDisposedException.
Native driver limits are checked before subsequent drawing.

### GetWidth(), GetHeight(), GetSize()

Return logical drawing dimensions from the outer file header. GetSize captures
both axes together. They remain zero until successful loading. These reads do
not decode, copy image data or allocate managed objects after preparation.

### GetImage()

Returns null while empty, otherwise a caller-owned Image with physical dimensions,
original format, exact retained bytes and all stored mips. Changing or disposing
it cannot affect the texture. PNG/WebP restore their declared source layout;
Basis uses its channel role. Compressed blocks remain compressed in this copy.

### IsPixelOpaque(int x, int y)

Maps logical coordinates to physical pixels in wide arithmetic, clamps outside
coordinates to the image edges and returns whether alpha exceeds 0.1. Empty
resources return true. Prepared block decoding supplies alpha without repeated
decompression or allocation. It operates independently from HasAlpha.

## Protected overrides

This sealed type retains the inherited extension contracts in its compiled API.

| Declaration | Behavior |
| --- | --- |
| `protected override void OnResourcePathChanged(string path)` | Updates the renderer label after visible path publication. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Adds stored typed LoadPath to inherited metadata. |
| `protected override Resource CreateDuplicateInstance()` | Constructs an empty instance of this exact resource type. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies immutable pixels, size and source path without source I/O; later publication is independent. |
| `protected override void Dispose(bool disposing)` | Drops managed snapshots/path state and releases the inherited resource identity. |

## Lifecycle and verification

Shallow/deep copies have distinct resource identities and independent later
replacement. Scene-local copies preserve shared references within one instance
and create distinct resources for independent scene instances. Typed archives
embed pixels, logical size and LoadPath; restored resources do not require the
original imported file. ResourceLoader supports typed discovery, weak reuse,
atomic Replace and worker loading through its ordinary format pipeline.

See [managed tests](../../tests/Electron2D.Tests/CompressedTextureTests.cs),
[rendering tests](../../tests/Electron2D.Tests/CompressedTextureTests.Rendering.cs)
and the component's verification limits. Integer sampling and foreign-platform
execution retain their documented dependencies. ADRs 0004/0013/0014/0028/0039 apply.
