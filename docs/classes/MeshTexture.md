# MeshTexture

Last updated: 2026-10-18

- Declaration: `public sealed class MeshTexture : Texture`
- Source: [MeshTexture.cs](../../src/Scene/Resources/MeshTexture.cs)
- Inherits: [Texture](Texture.md), [Resource](Resource.md)
- Component: [Meshes](../components/meshes.md#mesh-textures)

## Description

Draws borrowed two-dimensional mesh geometry through Texture consumers such as Sprite, TextureRect and the virtual canvas drawing methods. `Mesh` supplies vertices, UVs and per-surface materials; `BaseTexture` supplies sampled pixels. Both default null, and either missing input draws nothing. The view never owns these resources. Actual replacements and source content changes emit Changed after commitment; source disposal clears its edge. Equal property assignments are silent. Texture dependency cycles reject before replacement.

`ImageSize` defaults zero and retains finite signed/fractional components within the signed 32-bit pixel range. GetWidth/GetHeight truncate toward zero; GetSize returns those integer dimensions. ImageSize does not rescale point drawing. Rectangle drawing divides the destination size by raw ImageSize, and skips a zero logical axis before undefined division. A negative destination axis shifts the drawing origin by ImageSize on that axis. Transpose exchanges the diagonal scale components, leaving mesh UVs unchanged. Point transpose keeps the identity basis. Tile, source-region and UV-clip requests draw the whole mesh once, with rectangle scaling; they do not crop or repeat geometry.

This is a drawing-only view: GetImage returns null, alpha/mipmap metadata stays absent, and pixel-opacity queries are opaque. The inherited stable logical RID preserves virtual drawing through RenderingServer texture-rectangle commands; it does not provide a raster image for arbitrary samplers. Base atlas textures use their full backing image under the ordinary DrawMesh contract.

## API

| Member | Contract |
| --- | --- |
| `MeshTexture()` | Creates an empty view. |
| `Mesh? Mesh` | Borrowed geometry; null by default. |
| `Texture? BaseTexture` | Borrowed sampled image; null by default. |
| `Vector2 ImageSize` | Logical raw scaling dimensions; zero by default. |
| `GetWidth()`, `GetHeight()` | Truncated logical dimensions. |
| `Draw`, `DrawRect`, `DrawRectRegion` | Record actual mesh commands under the inherited drawing-scope, owner, finite-value and disposal guards. |

## Storage and lifetime

Typed stored descriptors retain the three properties and inherited ResourceLocalToScene=false. Exact resource factories support fresh-process resource archives. Shallow copies borrow sources; deep policies duplicate the existing graph while preserving aliases. Scene-local instances own their copied graph and release it with the scene. View disposal detaches subscriptions and releases its logical identity while preserving borrowed inputs.

Commands retain the selected mesh and base texture. Surface/image edits remain visible through the existing renderer. Replacing view properties requires the drawing consumer to record again; Changed provides that signal. Sprite and TextureRect already consume it. Static server commands remain copied until cleared/replaced, and a scene node redraw replaces its command edits.

## Verification limits

[MeshTextureTests](../../tests/Electron2D.Tests/MeshTextureTests.cs) covers fixed reference geometry, metadata, mutation guards, notifications, graph cycles, copying, scene-local ownership and fresh-process archives. [Native checks](../../tests/Electron2D.Tests/MeshTextureTests.Native.cs) exercise real canvas consumers and copied server identities. Backend evidence is recorded with the component; hardware/native-allocation and other-platform acceptance remain separate.
