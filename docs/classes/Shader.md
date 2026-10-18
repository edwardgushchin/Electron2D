# Shader

Last updated: 2026-10-18

- Declaration: `public sealed class Shader : Resource`
- Source: [Shader.cs](../../src/Scene/Resources/Shader.cs)
- Inherits: [Resource](Resource.md)
- Component: [Shader materials](../components/shader-materials.md)

## Description

A managed canvas program loaded from copied fragment SPIR-V, with an optional authored vertex module. HLSL and GLSL source compilation belongs to import/build tooling. Code and reflected layout are published as one immutable version; failed replacement preserves the previous version. GPU pipelines belong to the renderer and are created on demand. Resource duplication shares immutable program data safely. Disposal prevents further resource access and does not dispose materials that borrow the shader.

Runtime loading checks module structure, capabilities and reflected interfaces through the shared engine/SDL3-CS Shadercross path. Import tooling additionally runs instruction-level validation. Runtime loading does not guarantee that it detects every invalid instruction; successful reflection does not establish that arbitrary SPIR-V is valid. Translation and native program-creation failures propagate when the program is first used by a renderer.

Both creation and replacement reject payloads larger than 16 MiB, shorter than the header or not aligned to 32-bit words before allocating a copy. Subsequent structure/reflection checks operate on the owned copy so later caller mutations cannot change the checked program.

## Example

```csharp
using var shader = Shader.CreateFromSPIRV(System.IO.File.ReadAllBytes("material.spv"));
using var material = new ShaderMaterial { Shader = shader };
// The imported program declares a float4 uniform named tint.
material.SetShaderParameter("tint", Colors.White);
```

A paired program uses the same material API; this snippet assumes matching imported modules that declare gain and offset:

```csharp
using var paired = Shader.CreateFromSPIRV(
    System.IO.File.ReadAllBytes("material.vert.spv"),
    System.IO.File.ReadAllBytes("material.frag.spv"));
using var pairedMaterial = new ShaderMaterial { Shader = paired };
pairedMaterial.SetShaderParameter("gain", 1f);
pairedMaterial.SetShaderParameter("offset", new Vector2(16, 0));
```

## API summary

| Declaration | Contract |
| --- | --- |
| `override RID GetRID()` | Stable borrowed logical shader identity; reads use RenderingServer, mutations/free remain resource-owned. |
| `Shader()` | Precompiled texture-times-color fragment program. |
| `Mode GetMode()` | Returns `Mode.CanvasItem`. |
| `static Shader CreateFromSPIRV(ReadOnlySpan<byte> bytecode)` | Copies and validates a fragment program. |
| `void SetSPIRV(ReadOnlySpan<byte> bytecode)` | Validates then atomically replaces the program and emits `Changed`. |
| `byte[] GetSPIRV()` | Returns an independent copy of the fragment module. |
| `static Shader CreateFromSPIRV(ReadOnlySpan<byte> vertexBytecode, ReadOnlySpan<byte> fragmentBytecode)` | Creates one validated vertex/fragment program. |
| `void SetSPIRV(ReadOnlySpan<byte> vertexBytecode, ReadOnlySpan<byte> fragmentBytecode)` | Replaces both stages atomically and emits Changed once. |
| `byte[] GetSPIRV(RenderingDevice.ShaderStage stage)` | Copies the selected authored Vertex or Fragment module. |
| `IReadOnlyList<PropertyDescriptor> GetShaderUniformList()` | Immutable typed material descriptors for the current program version. |
| `void SetDefaultTextureParameter(string name, Texture? texture, int index = 0)` | Sets or clears a borrowed default texture. |
| `Texture? GetDefaultTextureParameter(string name, int index = 0)` | Gets the borrowed default texture. |
| [Mode](Shader.Mode.md) | Canvas shader domain. |

The fragment interface optionally accepts float4 color at location 0 and float2 UV at location 1, raw float4 instance custom at location 2 and secondary float2 UV at location 4, mesh-local float3 normals at location 5 and float4 tangent/orientation at location 6. `TEXTURE` at set 2, binding 0 receives the current command texture or white for untextured geometry. It is reserved and omitted from uniform descriptors/default-parameter APIs. An optional scalar float32 TIME member in any valid fragment uniform buffer receives the renderer clock. Its offset and binding are reflected, not fixed. TIME is omitted from material descriptors and parameter APIs; lowercase time remains an ordinary parameter. See the [shared shader interface](../components/shader-materials.md#current-shader-interface).

## Method descriptions

### Shader

Creates the embedded fragment program multiplying the command texture sample by drawing color. An assigned shader requires GPU rendering. A null `ShaderMaterial.Shader` uses ordinary canvas drawing on either backend.

### GetMode

Returns `Shader.Mode.CanvasItem` (1). Throws `ObjectDisposedException` after disposal.

### CreateFromSPIRV

The one-module overload copies `bytecode` before validation. The input must contain the supported canvas fragment interface, one `main` entry point and a little-endian SPIR-V header, within 16 MiB. Invalid structure/reflection throws `ArgumentException`; unsupported interfaces/resources throw `NotSupportedException`. Native-library initialization/load failures remain visible. Named sampled images use fixed linear filtering, clamp-to-edge coordinates and LOD zero independently of canvas policies. The complete current profile is in the component page.

### SetSPIRV

Uses the same copied-input validation and errors as creation. No mutation occurs until validation succeeds. Publication is synchronized; `Changed` runs afterwards and callback failures propagate without rolling the new program back. Existing materials migrate matching named values and texture overrides on next access; defaults for removed texture names are discarded. Calling on a disposed shader throws.

### GetSPIRV

The parameterless overload returns copied fragment bytes and never exposes the backing module. It is not a complete export of a paired program. The stage overload copies Vertex or Fragment; Vertex is empty for a fragment-only program. Undefined stage values throw ArgumentOutOfRangeException; other defined stages throw NotSupportedException. The snapshot may safely outlive subsequent reload or disposal. Calling on a disposed shader throws.

### GetShaderUniformList

Returns a read-only list with exact uniform names. Descriptors use `ShaderMaterial` as owner, scalar/vector/matrix element types, `T[]` for fixed arrays, or `Texture?`/`TextureLayered?` for scalar sampled bindings and their typed arrays for descriptor arrays. Float4 descriptors use `Vector4`; typed parameter calls also accept `Color` and `Rect2`. Square mat3/mat4 use copied column-major `float[]` descriptors for scalar values and concatenated fixed arrays, with identity defaults. Float2x2 uses `Transform` with identity defaults; row/column matrix storage and strides are reflected. Float3 uses `Vector3`, with `Color` as an RGB alias and canonical alpha one. Signed/unsigned integer vectors use `Vector2i`/`Vector3i`/`Vector4i`, retaining component bits. Logical bool uses bool; bool2/3/4 use int component masks with false/zero defaults, backed by validated [artifact metadata](../components/shader-materials.md#boolean-type-information). Fixed arrays use the corresponding canonical element descriptors. Getter/setter delegates use the material's current matching parameter, so an incompatible shader replacement is rejected on access. The list itself remains a snapshot after reload. Reserved TEXTURE and TIME are omitted. There are no grouping hints in the current import format.

### SetDefaultTextureParameter

Validates a named sampled texture and stores a borrowed default. Null clears it. A material override wins over this default. Index selects the reflected descriptor element; scalars accept only zero and out-of-range indices throw ArgumentOutOfRangeException. Bad names/types throw ArgumentException; a disposed shader or supplied texture throws ObjectDisposedException. Mutation precedes Changed; callback failure does not undo it. Matching named defaults survive shader reload.

### GetDefaultTextureParameter

Returns the assigned default or null, without transferring ownership. Uses the same name/index and shader-disposal checks as the setter. The returned texture remains shared.

## Protected resource hooks

`CreateDuplicateInstance()` creates a fresh Shader. `CopyCustomStateTo(Resource, bool, DeepDuplicateMode, Func<Resource?, Resource?>, Func<Resource?, Resource?>)` shares the immutable program snapshot and applies graph-copy policy to default textures; base Resource owns identity/path and graph-copy policy. `Dispose(bool)` releases the program reference and delegates base cleanup.

## Limits and checks

Logical boolean annotations are retained in copied bytecode and checked against the physical buffer layout for all input origins. Invalid annotations preserve the previous program. Runtime validation is structural and reflective; full semantic validation currently runs during import. Named sampler configuration, nonsquare matrices, nested structs, other shader modes, source preservation and editor code inspection remain unimplemented. Both language import paths, copied payloads, invalid replacement and live uniform reload have executable checks in [RenderingRuntimeTests.cs](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs); native execution has been checked on Linux X11/Wayland Vulkan. See [ADR 0028](../decisions/rendering.md#adr-0028).

## Instance data input

Fragment programs may consume raw float4 at location two, alongside tint location zero and UV location one. MultiMesh supplies its optional custom-data components independently of color multiplication; ordinary canvas geometry supplies zero. HLSL input semantics must compile to these locations (the tested signature uses active TEXCOORD0, TEXCOORD1 and TEXCOORD2 inputs). GLSL can use explicit layout locations. The built-in vertex stage forwards this channel; authored vertex programs use the paired-stage factory. MultiMeshRenderingTests verifies two independent colors, live data replacement and compatibility's explicit shader rejection.

Reserved SCREEN_TEXTURE and optional float2 SCREEN_PIXEL_SIZE now share the validated SPIR-V path with TEXTURE/TIME. They are excluded from material/default parameter lists and value migration; the renderer supplies the screen image and inverse current target dimensions. HLSL/GLSL fixtures exercise actual screen copying and group LOD. See [composition](../components/canvas-rendering.md#group-composition-and-screen-snapshots).

CreateFromSPIRV/SetSPIRV resolve packaged native reflection before renderer startup; the cold factory in CPUParticlesTests then feeds its custom-data shader to a real GPU host. No active graphics device is required to validate stored bytecode. Platform package/library availability remains required.

## Array sampler and RID operations

See [array resources](../components/texture-arrays.md) for copied storage, ownership, verification and format dependencies. Sampled shape changes clear incompatible overrides/defaults.

| Member | Kind | Summary |
| --- | --- | --- |
| [`public Electron2D.TextureLayered GetDefaultLayeredTextureParameter(System.String name, System.Int32 index = 0)`](#member-5c554c6e1090) | method | Gets the borrowed default image-array resource. |
| [`public System.Void SetDefaultLayeredTextureParameter(System.String name, Electron2D.TextureLayered texture, System.Int32 index = 0)`](#member-f5d8c3215912) | method | Sets a borrowed default resource for an image-array sampler. |

<a id="member-5c554c6e1090"></a>
### `GetDefaultLayeredTextureParameter(System.String, System.Int32)`

Kind: `method`

```csharp
public Electron2D.TextureLayered GetDefaultLayeredTextureParameter(System.String name, System.Int32 index = 0)
```

#### Summary

Gets the borrowed default image-array resource.

#### Returns

The assigned resource, or null.

#### Parameters

- `name`: The exact reflected array sampler name.
- `index`: Zero for a scalar descriptor, otherwise the zero-based fixed descriptor array element.

#### Exceptions

- `T:System.ArgumentException`: The parameter is not a layered sampler.
- `T:System.ArgumentOutOfRangeException`: The index is outside the reflected element count.
- `T:System.ObjectDisposedException`: The shader is disposed.


<a id="member-f5d8c3215912"></a>
### `SetDefaultLayeredTextureParameter(System.String, Electron2D.TextureLayered, System.Int32)`

Kind: `method`

```csharp
public System.Void SetDefaultLayeredTextureParameter(System.String name, Electron2D.TextureLayered texture, System.Int32 index = 0)
```

#### Summary

Sets a borrowed default resource for an image-array sampler.

#### Remarks

Material overrides take precedence. Ownership remains with the caller; Changed follows mutation.

#### Parameters

- `name`: The exact reflected array sampler name.
- `texture`: A live array resource, or null to clear its default.
- `index`: Zero for a scalar descriptor, otherwise the zero-based fixed descriptor array element.

#### Exceptions

- `T:System.ArgumentException`: The parameter is not a layered sampler.
- `T:System.ArgumentOutOfRangeException`: The index is outside the reflected element count.
- `T:System.ObjectDisposedException`: The shader or resource is disposed.

## Prepared reference publication

The protected `RemapResourceReferences(Func<Resource, Resource> remap)` hook maps freshly prepared references to final cached/new identities before owner publication under ADR 0013. Resource defaults to stored resource descriptors; PackedScene includes its node model; Shader includes default texture bindings. Opaque application storage overrides this hook and calls base for descriptor state. Primitive descriptors are not rewritten by the default remapper. This is a typed graph-publication extension, not a dynamic property API.

## Canvas lighting integration

[Canvas lights and occlusion](../components/canvas-lights.md) records this type's receiving/material, resource, occlusion, sampling or static-service consumer, with current GPU and compatibility behavior.

Integer device image consumers now validate float/signed/unsigned sampled scalar types and array roles, migrate reload bindings and use nearest native integer sampling. Cold snapshots expose raw-valued float32 RGBA; byte device readback preserves all integer bits. See [the integer canvas checkpoint](../components/local-compute.md#integer-canvas-sampling-checkpoint) for executable evidence and the connected scalar-aware lighting and alpha-mask behavior.

[Automatic scene lighting](../components/shader-materials.md#automatic-scene-lighting) composes the computed fragment color before blend, preserving parameters, texture roles, discard, returns, reload and hardware instancing. Normal/specular and point-mask samplers share the native sixteen-binding stage capacity; four user uniform slots remain available. Current rendered evidence is Linux/Vulkan.

The reserved Instance block has separate reflection/descriptor storage. File factories persist copied SPIRV bytes before sampled defaults; the decoder loads Shader before ShaderMaterial parameters. The protected GetPropertyDescriptors override exposes these stored schemas. See [per-item shader parameters](../components/shader-materials.md#per-item-shader-parameters).

## Secondary mesh UV channel

[MeshSurfaceData.UV2s](MeshSurfaceData.md#uv2s) supplies copied finite float pairs independent of primary UV and instance data. [The mesh component](../components/meshes.md#secondary-uv-storage-and-shader-consumer) records packing, archives, shader locations and executable verification.

## Mesh custom shader inputs

Four optional mesh-local float4 inputs occupy fragment locations 7, 8, 9 and 10, forwarded from vertex locations 10 through 13. Each MeshSurfaceData custom payload has its own packed format. Missing stored components use zero RGB/one alpha; a wholly absent channel supplies zero. Instance custom data remains independent at location 2. See [the custom mesh contract](../components/meshes.md#numerical-custom-channels).

## Paired canvas stages

`CreateFromSPIRV(vertexBytecode, fragmentBytecode)` and paired `SetSPIRV` copy each module, validate each stage and matching varyings, then publish a single version. Each module has the same sixteen-MiB bound. Failure preserves both prior stages and defaults without emitting Changed. A successful replacement commits before Changed; observer failure does not roll the replacement back. The single-module setter returns to the built-in vertex stage.

Authored vertices receive transformed framebuffer-pixel positions at location zero, command color at one, UV at two and optional raw instance custom data at three. The renderer applies hardware instance transforms and colors before invoking the authored main function, then applies canvas modulation and forwards its private glyph/lighting channels. Programs convert pixel positions to clip space using the renderer-owned Frame float2 client size at set one, binding zero. This is the native HLSL/GLSL canvas ABI. Vertex deformation does not change CPU hit testing, clipping bounds, light culling or retained command bounds. Hardware instance attributes four through six are reserved; see the complete [paired-stage interface](../components/shader-materials.md#authored-vertex-stages).

Each stage may expose typed material and per-item parameters and sampled textures. Shared names form one logical parameter when type, signedness, boolean width, array length and sampled shape match; stage bindings, offsets, strides and matrix storage order may differ. A name cannot mix material, instance and texture roles. Defaults, material overrides, typed descriptors and migration use this combined namespace. Shared values are copied into the correct stage buffer at submission, without prepared managed allocations. Renderer uniforms remain excluded from public parameter lists.

Resource duplication shares the immutable paired version. Resource archives retain both modules in a versioned envelope inside the existing SPIRV storage property; old fragment-only archives remain readable. No native GPU identity is serialized. Runtime loading retains the structural/reflection validation boundary; import tools provide instruction-level validation.

The public-only source consumer also verifies paired creation/export, resource archives and actual GPU pixels in JIT and NativeAOT on Linux X11 and Wayland. Its source build imports the existing native-consumer delivery props; compilation without native delivery alone does not establish runtime acceptance.

## Shared renderer globals

Reserved Globals numeric blocks and GLOBAL_ sampled-image names use the [typed renderer-session global service](../components/global-shader-parameters.md). They are excluded from material parameter descriptors, defaults and stored material values. Shaders retain declarations through reload/archives; renderer-session values remain independent.

For Mesh and triangle-array drawing, authored vertex `gl_VertexIndex`/`SV_VertexID` is the original zero-based surface vertex index. Indices, LODs, expanded primitives, morphs, skinning, instances and batching preserve that identity. Other canvas geometry retains native stream indices. See the [authored vertex interface](../components/shader-materials.md#authored-vertex-stages); vertex deformation retains the existing CPU visibility bounds.

For [position-free mesh surfaces](../components/meshes.md#procedural-surfaces-without-positions), the authored vertex position input is zero. Output uses local pixels converted with Frame size; canvas/command/instance/skin transforms and optional generated-pixel snapping follow afterward, preserving clip Z/W. Ordinary supplied-position drawing retains framebuffer input. Visibility remains an authored CPU rectangle.

## Indexed sampled defaults

Both ordinary and layered default families accept fixed descriptor array indices. Each material element independently overrides its indexed default. Stored array-valued descriptors preserve aliases and original shader payloads. Validation snapshots callback-sensitive input, rejects reentrant default mutation or program replacement, and publishes only after all values pass. A length, image role or sampled scalar change on reload drops the complete incompatible array. See [the descriptor array contract](../components/shader-materials.md#fixed-sampled-descriptor-arrays) and SamplerArrayTests for executable evidence and limits.
