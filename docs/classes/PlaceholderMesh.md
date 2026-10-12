# PlaceholderMesh

Last updated: 2026-10-12

**Namespace:** `Electron2D` · **Source:** [PlaceholderMesh.cs](../../src/Scene/Resources/PlaceholderMesh.cs).

## Description

A surface-free mesh with a stable logical RID and copied finite nonnegative Rect2 AABB. GetFaces returns no fabricated geometry and surface queries reject nonexistent indices. AABB edits publish Changed and affect bounds without creating surfaces.

Mesh.CreatePlaceholder returns a new caller-owned placeholder with source bounds. Duplicate/archives retain its AABB. When a version-2 file lacks a custom Mesh factory, original factory identity and fields are retained privately; a later process with that compiled factory can restore actual geometry. Other exact Mesh subclasses remain incompatible until their factory is present. Resource and mesh lifetime follow the inherited contract.

## Member summary

| Declaration | Contract |
| --- | --- |
| `public sealed class Electron2D.PlaceholderMesh` | Retains two-dimensional bounds for an absent mesh without inventing drawable surfaces. |
| `public PlaceholderMesh()` | Creates an empty surface-free mesh with zero bounds. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `protected override Electron2D.Rect2 OnGetAABB()` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `protected override System.Int32 OnGetSurfaceCount()` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `protected override System.Int32 OnSurfaceGetArrayIndexLen(System.Int32 surfaceIndex)` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `protected override System.Int32 OnSurfaceGetArrayLen(System.Int32 surfaceIndex)` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `protected override Electron2D.MeshSurfaceData OnSurfaceGetArrays(System.Int32 surfaceIndex)` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `protected override Electron2D.Mesh.ArrayFormat OnSurfaceGetFormat(System.Int32 surfaceIndex)` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `protected override Electron2D.Material OnSurfaceGetMaterial(System.Int32 surfaceIndex)` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `protected override Electron2D.Mesh.PrimitiveType OnSurfaceGetPrimitiveType(System.Int32 surfaceIndex)` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `protected override System.Void OnSurfaceSetMaterial(System.Int32 surfaceIndex, Electron2D.Material material)` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `protected override System.Void RemapResourceReferences(System.Func<Electron2D.Resource, Electron2D.Resource> remap)` | Implements the inherited typed resource/mesh lifecycle or surface callback contract. |
| `public Electron2D.Rect2 AABB { get; set; }` | Gets or sets the mesh-local visibility rectangle. |

## Member descriptions

### PlaceholderMesh

`public sealed class Electron2D.PlaceholderMesh`

Retains two-dimensional bounds for an absent mesh without inventing drawable surfaces.

### .ctor

`public PlaceholderMesh()`

Creates an empty surface-free mesh with zero bounds.

### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### OnGetAABB

`protected override Electron2D.Rect2 OnGetAABB()`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### OnGetSurfaceCount

`protected override System.Int32 OnGetSurfaceCount()`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### OnSurfaceGetArrayIndexLen

`protected override System.Int32 OnSurfaceGetArrayIndexLen(System.Int32 surfaceIndex)`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### OnSurfaceGetArrayLen

`protected override System.Int32 OnSurfaceGetArrayLen(System.Int32 surfaceIndex)`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### OnSurfaceGetArrays

`protected override Electron2D.MeshSurfaceData OnSurfaceGetArrays(System.Int32 surfaceIndex)`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### OnSurfaceGetFormat

`protected override Electron2D.Mesh.ArrayFormat OnSurfaceGetFormat(System.Int32 surfaceIndex)`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### OnSurfaceGetMaterial

`protected override Electron2D.Material OnSurfaceGetMaterial(System.Int32 surfaceIndex)`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### OnSurfaceGetPrimitiveType

`protected override Electron2D.Mesh.PrimitiveType OnSurfaceGetPrimitiveType(System.Int32 surfaceIndex)`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### OnSurfaceSetMaterial

`protected override System.Void OnSurfaceSetMaterial(System.Int32 surfaceIndex, Electron2D.Material material)`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### RemapResourceReferences

`protected override System.Void RemapResourceReferences(System.Func<Electron2D.Resource, Electron2D.Resource> remap)`

Implements the inherited typed resource/mesh lifecycle or surface callback contract.

### AABB

`public Electron2D.Rect2 AABB { get; set; }`

Gets or sets the mesh-local visibility rectangle.

Value : Finite coordinates and nonnegative size. Zero initially.

## Verification

[MissingResourceTests](../../tests/Electron2D.Tests/MissingResourceTests.cs) exercises typed authoring, identity/copy/alias bounds, malformed input, Replace, external dependencies, threaded policy, version-1 known files and separate-process preservation/restoration. [MissingResourceNativeTests](../../tests/Electron2D.Tests/MissingResourceNativeTests.cs) checks fresh unknown/restored custom mesh pixels on GPU and compatibility, with 64 active and 64 idle complete frames at zero measured managed bytes. Native allocation totals, foreign targets and human acceptance remain separate.
