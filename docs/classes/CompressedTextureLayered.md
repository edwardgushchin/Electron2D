# CompressedTextureLayered

Last updated: 2026-10-16

Source: [CompressedTextureLayered.cs](../../src/Scene/Resources/CompressedTextureLayered.cs)

```csharp
public abstract class CompressedTextureLayered : TextureLayered
```

Inherits: [TextureLayered](TextureLayered.md). Inherited by:
[CompressedTextureArray](CompressedTextureArray.md).

## Description

Owns immutable copied layers loaded from a bounded compressed array asset.
The resource's RID stays stable across reloads; native storage can retire.
No caller Image or native pointer is retained. Per-call state access synchronizes;
coordinate multi-call edits and disposal. GetLayerData returns an independent
caller-owned image, including source format and all mips. Empty resources have
zero dimensions/layers, L8 format and no mipmaps. Cube and volume roles are absent.
See [file-backed arrays](../components/compressed-arrays.md) for the container,
channel roles, limits, cache/archive workflow, renderer and verification contract.

## Example

Requires an existing imported file.

```csharp
using var texture = new CompressedTextureArray();
texture.Load("res://terrain.ctexarray");
TextureLayered layers = texture;
using var image = layers.GetLayerData(0);
```

## Constructors, properties and methods

| Declaration | Contract |
| --- | --- |
| `protected CompressedTextureLayered()` | Initializes an empty file-backed array. |
| `public string LoadPath { get; set; }` | Last successfully loaded input path; assigning calls Load. Initially empty. Stored descriptor. |
| `public void Load(string path)` | Prepares and atomically publishes an entire array. |
| `public override Image.Format GetFormat()` | Original source format, L8 while empty. |
| `public override int GetWidth()` | Pixel width per layer, zero while empty. |
| `public override int GetHeight()` | Pixel height per layer, zero while empty. |
| `public override int GetLayers()` | Number of independent images, zero while empty. |
| `public override bool HasMIPMaps()` | True for a stored complete mip chain. |
| `public override Image? GetLayerData(int layerIndex)` | Independent image for a valid zero-based layer. |

## Constructor and property descriptions

### CompressedTextureLayered()

Protected constructor for a concrete array resource. It creates no pixel or native
allocation. The inherited Resource/TextureLayered lifetime and invalidation hooks
apply immediately.

### LoadPath

Stored string, initially empty. The getter returns the last successful Load input;
the setter invokes Load and has its same validation, publication and exceptions.
It differs from ResourcePath, which identifies the resource in its cache/archive.
Copying and restoring stored state do not invoke this setter or reopen the source.

## Method descriptions

### Load(string path)

Accept operating-system, res:// and user:// paths. Every layer must share size,
source format and mip state. Raw, PNG/WebP mip payloads and Basis LDR/HDR execute.
Input/decoding failure preserves previous pixels, path and RID. Notifications run
after publication, PropertyListChanged before Changed; subscriber failure retains
the new state. ResourcePath is independent from LoadPath. Setting/clearing
ResourcePath updates the renderer label. Duplication and archive restoration copy
the validated snapshot and LoadPath without I/O on the original file.

Empty/null paths throw ArgumentException, malformed/truncated/oversized data
throws InvalidDataException, I/O and decoder working-memory failure use IOException,
unavailable role/format uses NotSupportedException, and disposed access throws
ObjectDisposedException. Subscriber exceptions propagate after publication.

### GetFormat()

Returns Image.Format for the retained source pixels; empty state returns L8.
PNG/WebP restore their declared source layout, while Basis uses its channel role.

### GetWidth()

Returns integer pixel width of one layer, or zero before successful loading.

### GetHeight()

Returns integer pixel height of one layer, or zero before successful loading.

### GetLayers()

Returns the nonnegative number of stored image layers, initially zero.

### HasMIPMaps()

Returns true when every layer has the same complete mip chain; false for base-only
images and uninitialized resources. The base image itself is not counted as a mip.

### GetLayerData(int layerIndex)

Metadata queries do not decode or allocate pixel copies. GetLayerData validates
the index and throws ArgumentOutOfRangeException outside the stored range,
including any index on an empty resource. The returned Image can be mutated or
disposed independently. Disposed resources reject these calls.

## Protected extension points

| Declaration | Contract |
| --- | --- |
| `protected override void OnResourcePathChanged(string path)` | Publishes the renderer label after the Resource path change. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Extends inherited descriptors with stored LoadPath. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies the immutable snapshot and source label to the exact-type target, without reopening the file; inherited graph policy applies. |
| `protected override void Dispose(bool disposing)` | Releases retained snapshot/labels before inherited resource cleanup. |

Derived concrete resources must implement the inherited exact-type duplicate
factory. Inherited GetRID, ResourcePath, notifications and deterministic disposal
retain their Resource/TextureLayered contracts. No native backend type is exposed.
