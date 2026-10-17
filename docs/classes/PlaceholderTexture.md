# PlaceholderTexture

Last updated: 2026-10-17

- Declaration: `public sealed class PlaceholderTexture : Texture`
- Source: [PlaceholderTexture.cs](../../src/Scene/Resources/PlaceholderTexture.cs)
- Inherits: [Texture](Texture.md)
- Component: [Texture placeholders](../components/texture-placeholders.md)

Retains logical dimensions without retaining source image data. Create one directly or call `Texture.CreatePlaceholder()`. The factory returns a caller-owned resource containing only the current logical size, with default resource metadata. It does not retain the source texture.

```csharp
using var metadata = texture.CreatePlaceholder();
ResourceSaver.Save(metadata, "user://texture-layout.e2dres");
```

| Declaration | Contract |
| --- | --- |
| `PlaceholderTexture()` | Starts at one by one, with `ResourceLocalToScene` false. |
| `Vector2 Size { get; set; }` | Components truncate toward zero. Equal writes still emit `Changed`. Zero and negative metadata are retained. Nonfinite or out-of-range pixel dimensions throw before mutation. |
| `int GetWidth()`, `int GetHeight()`, `Vector2 GetSize()` | Return logical dimensions. |
| `Image.Format PixelFormat` | `Image.Format.Max`: no image format is present. |

Inherited `GetImage()` returns null, `HasAlpha` and `HasMIPMaps` are false, `MIPMapCount` is zero and `IsPixelOpaque` returns true. These queries do not read diagnostic rendering pixels.

The stable borrowed RID and ordinary canvas drawing use the existing diagnostic checkerboard on GPU and Compatibility. Zero dimensions record no geometry; negative metadata is rejected by ordinary drawing. The placeholder does not guarantee ordinary texture-coordinate behavior in materials or shaders. Resizing preserves RID identity and notifies layout and sprite consumers. Renderer shutdown does not dispose the resource.

Duplication, `CopyFromResource`, explicit resource archives and scene-local instances preserve dimensions through the existing typed schema. Scene-local aliases share one duplicate per instance; the root owns that duplicate. No pixel payload is serialized. Version-4 tolerant archives also substitute this type for unknown ordinary texture factories and missing external archive textures. Original encoded fields and references survive duplication, scene-local copies and resaving; a later process with the factory restores the source class. Size edits update placeholder layout metadata without rewriting opaque original properties. A restored class consumes its original property schema.

A missing external payload remains marked missing when bundled: the available original factory cannot manufacture a successful empty texture. Tolerant loading retains the placeholder and strict loading rejects. Saving unbundled retains the external identity/path so a returning file can supply real pixels. Name, local-scene policy and scene ID are retained for missing external resources. Layered and other exact resource roles keep their separate substitution boundaries.
