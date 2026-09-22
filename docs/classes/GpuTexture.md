# GpuTexture

Last updated: 2026-09-22

- Declaration: `internal sealed unsafe class GpuTexture : IDisposable`
- Source: [GpuTexture.cs](../../src/Servers/Rendering/GpuTexture.cs)
- Component: [shader-materials](../components/shader-materials.md)
- Visibility: internal; unavailable to engine consumers.

## Description

Owns a sampled native 2D texture and upload transfer buffer under a retained GPU device. It holds managed TexturePixels snapshots but never owns the public Texture resource. GpuCanvasBackend replaces this allocation when the snapshot allocation identity changes; a compatible ImageTexture.Update replaces Pixels instead.

## Internal usage

This fragment belongs inside the runtime and requires the surrounding owner state.

```csharp
gpuTexture.Upload(commandBuffer);
// After successful submission:
gpuTexture.CommitUpload();
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal GpuTexture(RenderHandle device, TexturePixels pixels)` | [Construction](#construction) |
| `internal TexturePixels Pixels` | [Pending pixels](#pending-pixels) |
| `internal nint Handle { get; }` | [Native handle](#native-handle) |
| `internal void Upload(nint command)` | [Upload](#upload) |
| `internal void CommitUpload()` | [Commit upload](#commit-upload) |
| `public void Dispose()` | [Disposal](#disposal) |

## Member descriptions

### Construction

`internal GpuTexture(RenderHandle device, TexturePixels pixels)`

Checks native sampling format support, creates an RGBA8 or RGBA32Float texture with every stored mip level, and allocates matching upload storage. Transfer creation failure releases the texture. Unsupported sampling raises NotSupportedException.

### Pending pixels

`internal TexturePixels Pixels`

Current immutable snapshot to upload. Replacing it requires an unchanged allocation identity and pixel layout, enforced by the owning backend.

### Native handle

`internal nint Handle { get; }`

Borrows the native texture pointer internally. Invalid after disposal; callers retain the backend/device lifetime.

### Upload

`internal void Upload(nint command)`

Skips the already committed snapshot. Otherwise maps/copies upload bytes, encodes all mip levels, cycles the native texture only at the first level, and closes the copy pass. Does not mark success until the containing command has been submitted. Native mapping/pass failures throw.

### Commit upload

`internal void CommitUpload()`

Marks the current snapshot uploaded after successful command submission. Never call for cancelled/failed frames.

### Disposal

`public void Dispose()`

Disposes texture and transfer SafeHandles; device retention is released with them. Repeated disposal is safe.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.
