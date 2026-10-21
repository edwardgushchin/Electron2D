# ShaderMaterial

Last updated: 2026-10-21

- Declaration: `public sealed partial class ShaderMaterial : Material`
- Source: [Material.cs](../../src/Scene/Resources/Material.cs), [Material.Textures.cs](../../src/Scene/Resources/Material.Textures.cs), [Material.Matrices.cs](../../src/Scene/Resources/Material.Matrices.cs)
- Inherits: [Material](Material.md), [Resource](Resource.md)
- Component: [Shader materials](../components/shader-materials.md)

## Description

Pairs a borrowed Shader with independently owned parameter values and borrowed texture overrides. Multiple materials may share one Shader and hold different values; nodes sharing one material see the same updates. Null Shader selects ordinary canvas color drawing. Any assigned Shader requires GPU rendering. Material disposal clears its own state and never disposes the borrowed Shader or textures.

Reserved TIME is not a user parameter: getters, setters, property discovery, duplication and value migration exclude it. The renderer fills its reflected float32 location immediately before each buffer upload without emitting Changed.

State operations are serialized per material. Native uniform upload uses the same gate. Notifications run after mutation outside the gate. Successful scalar/vector/matrix or span updates reuse initialized storage. Matrices initially contain identity; other stored components contain zero. Float3 Color readers reconstruct alpha one; float2x2 Transform readers reconstruct zero Origin. No source defaults are extracted. Shader replacement migrates matching names, shader element types (including integer signedness and logical boolean width) and array lengths to new offsets/bindings on the next parameter access or draw. Incompatible or newly introduced members start at their typed defaults; assigning null discards values.

## Example

```csharp
using var shader = Shader.CreateFromSPIRV(System.IO.File.ReadAllBytes("material.spv"));
using var material = new ShaderMaterial { Shader = shader };
// The imported program declares these names and types.
material.SetShaderParameter("tint", Colors.Red);
material.SetShaderParameter("gain", 0.5f);
material.SetShaderParameter<float>("weights", new float[] { 0.25f, 0.75f }.AsSpan());
Color tint = material.GetShaderParameter<Color>("tint");
```

## API summary

| Declaration | Contract |
| --- | --- |
| `ShaderMaterial()` | Creates a material with no Shader. |
| `Shader? Shader { get; set; }` | Borrowed program, null by default. |
| `void SetShaderParameter<T>(string name, T value) where T : unmanaged` | Sets a matching scalar/vector/matrix. |
| `void SetShaderParameter<T>(string name, ReadOnlySpan<T> values) where T : unmanaged` | Copies a complete fixed-size array. |
| `void SetShaderParameter(string name, Texture? texture)` | Sets a borrowed texture override; null uses the Shader default. |
| `Texture? GetShaderParameter(string name)` | Returns the explicit texture override or null. |
| `T GetShaderParameter<T>(string name) where T : unmanaged` | Reads a matching scalar/vector/matrix. |
| `T[] GetShaderParameterArray<T>(string name) where T : unmanaged` | Returns a copy of a complete fixed-size array. |
| `void CopyShaderParameterArray<T>(string name, Span<T> destination) where T : unmanaged` | Copies the complete reflected array into exact-length caller storage; prepared copies allocate no managed memory. |

Numeric mat3/mat4 values and fixed arrays use `SetShaderMatrixParameter(string, ReadOnlySpan<float>)`, `GetShaderMatrixParameter(string)` and `CopyShaderMatrixParameter(string, Span<float>)`. Each matrix contributes nine/sixteen column-major components. Values are copied, finite, identity by default and retained across compatible layout reload. Typed float-array descriptors use the same storage and revert contract. See [numeric material matrices](../components/shader-materials.md#numeric-material-matrices).

## Property descriptions

### Shader

Assignment borrows a live program. Assigning the same instance is a no-op; a changed reference emits `Changed`. A disposed material or supplied Shader causes `ObjectDisposedException`. Shader code may later be replaced through `Shader.SetSPIRV`; code/layout publication and the next material migration use whole versions. A disposed borrowed Shader makes parameter access or rendering fail explicitly.

## Method descriptions

### SetShaderParameter

Names are nonblank and case-sensitive. Supported types currently include bool, float, int, uint, Vector2, Vector3, Vector4, Color, Rect2, Transform, Vector2i, Vector3i and Vector4i, subject to the reflected element type. Vector3 supplies arbitrary float3 values; Color supplies RGB to float3 or RGBA to float4 without implicit gamma conversion. Float3 ignores alpha when storing; reads always reconstruct alpha one. Rect2 aliases float4 as position.x, position.y, size.x, size.y, including negative sizes. Vector2i, Vector3i and Vector4i carry either signed or unsigned shader vectors with unchanged 32-bit component patterns: -1 supplies 0xffffffffu. Transform supplies X/Y basis columns to float2x2, omits Origin, and reads it back as zero. Row/column storage order and matrix/array strides are reflected automatically. Every floating component must be finite, including unused Color alpha and Transform Origin. Array calls require exactly the reflected array length and copy into internal padded storage.

Logical bool uses C# bool; bool2/3/4 use an int component mask (low bits X/Y/Z/W). Higher bits are ignored and reads return the represented bits. Arrays use one bool or int mask per logical shader element. These types require validated [artifact metadata](../components/shader-materials.md#boolean-type-information); unannotated unsigned storage remains numeric. Defaults and reverts are false/zero.

Texture overloads require a reflected sampled image name and a live Texture, or null to use the Shader default. Missing defaults or unreadable/disposed textures fail before rendering. Texture updates and changes to bindings affect later frames without redrawing geometry. By default, named float samplers use linear filtering and integer samplers use nearest, with clamp-to-edge coordinates and LOD zero independently of CanvasItem/Viewport properties. Shader.SetTextureSamplerState supplies explicit shared sampler state; see [the verified defaults](../components/shader-materials.md#named-sampler-defaults).

The full request is validated before writing, including all array elements. A failed update preserves previous values. A successful update emits `Changed` and takes effect on later draws without `QueueRedraw`. Callback failure propagates after committed mutation. No Shader causes `InvalidOperationException`; bad names/types/shapes/lengths/nonfinite values cause `ArgumentException`; disposal causes `ObjectDisposedException`. The generic unmanaged constraint does not imply arbitrary unmanaged types are accepted.

### GetShaderParameter

The generic overload returns the matching scalar/vector/matrix value, initially identity for float2x2 and zero for other stored components (alpha one for float3 Color). Transform Origin is always zero. The nongeneric overload returns the explicit borrowed texture override, or null when the Shader default applies. The type and scalar shape must match. Uses the same name, missing-Shader and disposal checks as setters. No mutable backing storage is returned.

### GetShaderParameterArray

Returns an independent `T[]` containing all elements without buffer padding. Caller writes cannot mutate the material. Type/array shape, missing-Shader and disposal checks match setters. This convenience reader allocates an array; scalar/vector/matrix updates and renderer capture do not.

## Tooling and protected hooks

Inherited property discovery includes Shader and typed parameter descriptors named `shader_parameter/<uniform>`. Their typed revert values (identity for float2x2, zero components otherwise, alpha one for float3 Color) and setters share the parameter API. `Shader.GetShaderUniformList()` offers descriptors with unprefixed uniform names for explicit shader inspection.

`CreateDuplicateInstance()` creates a fresh ShaderMaterial. `CopyCustomStateTo(Resource, bool, DeepDuplicateMode, Func<Resource?, Resource?>, Func<Resource?, Resource?>)` independently copies all parameter buffers and applies the resource graph policy to Shader and texture overrides, preserving aliases shared with Shader defaults. `GetPropertyDescriptors()` supplies the stored Shader and parameter descriptors. `Dispose(bool)` clears borrowed/program references and value storage before base cleanup. Resource path, identity and local-scene rules remain inherited.

## Verification and remaining work

[RenderingRuntimeTests.cs](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs) checks named typed access, failure atomicity, independent copies and returned arrays, property discovery, cross-layout reload, disposal and zero allocations over warmed repeated updates. Four GPU frames per imported language check float/int/uint/vector/array values, distinct materials sharing a shader, and image changes without drawing-command regeneration. [ShaderVectorRenderingTests](../../tests/Electron2D.Tests/ShaderVectorRenderingTests.cs) covers RGB/Rect2 and unsigned vectors, scalar and arrays, defaults/descriptors, adjacent-field integrity, failures, copies, signedness resets, two-language pixels, shared materials, layout reload and warmed allocation checks. [ShaderMatrixRenderingTests](../../tests/Electron2D.Tests/ShaderMatrixRenderingTests.cs) verifies matrix storage order/stride, identity defaults versus explicit zero, Origin omission, arrays, copying, reload, invalid layouts and GPU pixels for both source languages. [ShaderBooleanRenderingTests](../../tests/Electron2D.Tests/ShaderBooleanRenderingTests.cs) verifies logical types, masks/arrays, defaults, copying, metadata rejection, reload and two-language pixels with zero warm allocation. Native coverage is Linux Wayland/Vulkan only. Nonsquare matrices, nested structures and further numeric mappings remain incomplete; see [the component limits](../components/shader-materials.md).

Renderer-owned SCREEN_TEXTURE and SCREEN_PIXEL_SIZE do not appear in stored material descriptors or typed user parameter/default access. They resolve per target draw; ordinary uniforms still use the existing lock/capture/migration rules. See [composition](../components/canvas-rendering.md#group-composition-and-screen-snapshots).

`CopyShaderParameterArray` validates name, element type, array shape and exact destination length before touching caller storage. It uses the same locked state migration as the allocating array getter. [RenderingProgramTests](../../tests/Electron2D.Tests/RenderingProgramTests.cs) checks independent RID identities, lifetime and 2000 prepared copies.

## Array sampler and RID operations

See [array resources](../components/texture-arrays.md) for copied storage, ownership, verification and format dependencies. Sampled shape changes clear incompatible overrides/defaults.

| Member | Kind | Summary |
| --- | --- | --- |
| [`public Electron2D.TextureLayered GetShaderLayeredParameter(System.String name)`](#member-3192b6a8e6c8) | method | Gets the explicitly assigned image-array override. |
| [`public System.Void SetShaderLayeredParameter(System.String name, Electron2D.TextureLayered texture)`](#member-8506ee080f15) | method | Sets a borrowed image-array override for a reflected array sampler. |

<a id="member-3192b6a8e6c8"></a>
### `GetShaderLayeredParameter(System.String)`

Kind: `method`

```csharp
public Electron2D.TextureLayered GetShaderLayeredParameter(System.String name)
```

#### Summary

Gets the explicitly assigned image-array override.

#### Returns

The borrowed override, or null when the shader default is used.

#### Parameters

- `name`: The exact sampler parameter name.

#### Exceptions

- `T:System.ArgumentException`: The parameter is not a layered sampler.
- `T:System.InvalidOperationException`: No shader is assigned.
- `T:System.ObjectDisposedException`: The material or shader is disposed.


<a id="member-8506ee080f15"></a>
### `SetShaderLayeredParameter(System.String, Electron2D.TextureLayered)`

Kind: `method`

```csharp
public System.Void SetShaderLayeredParameter(System.String name, Electron2D.TextureLayered texture)
```

#### Summary

Sets a borrowed image-array override for a reflected array sampler.

#### Remarks

Sampling uses linear filtering, clamp addressing and base-level LOD. Changes need no geometry rerecording. An unbound parameter fails before drawing. Ownership remains with the caller.

#### Parameters

- `name`: The exact sampler parameter name.
- `texture`: A live array resource, or null to use the shader default.

#### Exceptions

- `T:System.ArgumentException`: The parameter is not a layered sampler.
- `T:System.InvalidOperationException`: No shader is assigned.
- `T:System.ObjectDisposedException`: The material, shader or array is disposed.

## Canvas lighting integration

[Canvas lights and occlusion](../components/canvas-lights.md) records this type's receiving/material, resource, occlusion, sampling or static-service consumer, with current GPU and compatibility behavior.

Integer device image consumers now validate float/signed/unsigned sampled scalar types and array roles, migrate reload bindings and use nearest native integer sampling. Cold snapshots expose raw-valued float32 RGBA; byte device readback preserves all integer bits. See [the integer canvas checkpoint](../components/local-compute.md#integer-canvas-sampling-checkpoint) for executable evidence and the connected scalar-aware lighting and alpha-mask behavior.

[Automatic scene lighting](../components/shader-materials.md#automatic-scene-lighting) composes the computed fragment color before blend, preserving parameters, texture roles, discard, returns, reload and hardware instancing. Normal/specular and point-mask samplers share the native sixteen-binding stage capacity; four user uniform slots remain available. Current rendered evidence is Linux/Vulkan.

Instance fields are excluded from shared material setters/descriptors. Its portable factory restores the Shader graph first, then shared parameters. [Per-item shader parameters](../components/shader-materials.md#per-item-shader-parameters) provides independent item values over the same borrowed material.

## Paired stages

An authored vertex stage uses the same Shader property and typed material/default/instance namespace as fragments. Names merge only with matching logical types and roles; physical bindings, offsets and matrix layouts may differ. Reload migrates values by logical name/type. Three vertex material blocks follow renderer-owned Frame; all four fragment slots remain available. Native textures are bound per stage from one logical resource list. See [the paired-stage contract](../components/shader-materials.md#authored-vertex-stages).

## Shared renderer globals

Reserved Globals numeric blocks and GLOBAL_ sampled-image names use the [typed renderer-session global service](../components/global-shader-parameters.md). They are excluded from material parameter descriptors, defaults and stored material values. Shaders retain declarations through reload/archives; renderer-session values remain independent.

## Fixed sampled descriptor arrays

| Declaration | Contract |
| --- | --- |
| `void SetShaderTextureParameterArray(string name, ReadOnlySpan<Texture?> textures)` | Atomically replaces all ordinary-image overrides; exact reflected length. |
| `void SetShaderLayeredParameterArray(string name, ReadOnlySpan<TextureLayered?> textures)` | Same whole-array update for layered-image resources. |
| `Texture?[] GetShaderTextureParameterArray(string name)` | Returns a copied array of explicit ordinary-image overrides. |
| `TextureLayered?[] GetShaderLayeredParameterArray(string name)` | Returns copied explicit layered-image overrides. |
| `void CopyShaderTextureParameterArray(string name, Span<Texture?> destination)` | Copies all ordinary-image overrides to exact-length caller storage. |
| `void CopyShaderLayeredParameterArray(string name, Span<TextureLayered?> destination)` | Copies all layered-image overrides to exact-length caller storage. |

All six methods require a fixed descriptor array, including arrays of length one. Null entries select the respective indexed shader default. Scalar accessors, including Set/GetShaderLayeredParameter with one resource, require a scalar descriptor. Returned references stay borrowed. Bad names, roles, sampled scalar types or lengths raise ArgumentException; disposed resources raise ObjectDisposedException; missing programs or reentrant/replaced-program validation raise InvalidOperationException. Setter input is snapshotted before producer callbacks and all elements validate before one commit/Changed. Callback failures after Changed retain committed values. Prepared setters and copies allocate no managed memory. Getters allocate independent arrays.

Typed descriptors, graph duplication, resource archives and name/role/scalar/length-compatible reload share the same storage. [SamplerArrayTests](../../tests/Electron2D.Tests/SamplerArrayTests.cs) exercises resource and server consumers, native pixels and warm frames. See [the full descriptor array contract](../components/shader-materials.md#fixed-sampled-descriptor-arrays) for source declarations, limits and remaining platform evidence.

### SetShaderTextureParameterArray(string, ReadOnlySpan<Texture?>)

Copies an exact-length array of borrowed ordinary images, validates sampled scalar roles and commits once. Null selects the matching indexed shader default. Failure preserves the old array and emits no Changed; a Changed callback failure occurs after commit. Wrong roles or scalar descriptor names reject.

### SetShaderLayeredParameterArray(string, ReadOnlySpan<TextureLayered?>)

Uses the same atomic update and error/lifetime contract for layered images. Layer count is an image property independent of this descriptor array's length. Ordinary images and scalar descriptors reject.

### GetShaderTextureParameterArray(string)

Returns a new Texture?[] containing explicit borrowed overrides for the named ordinary-image descriptor array. Null elements retain their default-selection meaning. Does not resolve defaults or transfer resource ownership.

### GetShaderLayeredParameterArray(string)

Returns the corresponding independent TextureLayered?[] for a layered-image descriptor array. The reflected name, descriptor array shape and image role must match; missing/disposed program checks apply.

### CopyShaderTextureParameterArray(string, Span<Texture?>)

Validates the name, ordinary-image descriptor shape and exact destination length before writing all explicit overrides. Prepared calls allocate no managed memory; destination elements remain borrowed references.

### CopyShaderLayeredParameterArray(string, Span<TextureLayered?>)

Copies the complete layered-image override array under the material gate with the same validation, ownership and prepared allocation behavior.

The distinct array setter names preserve existing untyped-null calls to scalar texture setters. SamplerArrayTests compiles and checks both scalar null calls, which reject a descriptor array name without ambiguous overload resolution.

[NumericMatrixTests](../../tests/Electron2D.Tests/NumericMatrixTests.cs) adds mat3/mat4 scalar/fixed-array identities, complete finite validation, copied descriptors/graphs/archives, shape resets and layout-aware reload. GLSL/HLSL resource and RID pixels, including paired stages, execute on Linux Wayland software Vulkan; 64 active/64 idle prepared frames allocate zero managed bytes. Hardware/native-allocation and foreign-platform acceptance remain open.

A Shader with Mode.TextureBlit executes through DrawableTexture blit methods and RenderingServer.TextureDrawableBlitRect. Its uniforms, default images and globals use the same typed material state; canvas use rejects this domain. See [writable textures](../components/drawable-textures.md).

## Particle compute profile

The [particle shader contract](../components/particle-shaders.md) adds compiled HLSL/GLSL compute modules, typed material parameters, portable state/frame/subemission includes and explicit RenderingDevice execution. Process programs preserve their domain through duplication and file graphs and reject canvas/blit submission. Retained emission, particle nodes and canvas output remain separate dependencies.
