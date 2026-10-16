# Global canvas shader parameters

Last updated: 2026-10-16

## Responsibility and ownership

The retained RenderingServer owns one typed registry per renderer session. Public operations use static access; games never obtain a second global service or backend handle. One update affects every consuming ShaderMaterial without replacing materials or recording geometry again. Entries retain borrowed Texture/TextureLayered references; removal and renderer shutdown release references without disposing user resources. A restarted renderer starts with an empty registry. Shader import, validation, duplication and archive restoration require no active registry.

Every operation requires an active renderer and its owner thread. Mutations additionally reject native submission and shutdown through the ordinary server guard. Use Window.Ready or the owner's frame callbacks to author session values. Missing numeric declarations, mismatched logical roles and absent/disposed/mismatched sampled resources reject a consuming submission; there is no silent default or texture substitution. Entries may be removed before their shaders are used again.

## Public API

Names are exact, case-sensitive, nonblank strings. Canvas built-in names and the GLOBAL_ prefix are reserved. Duplicate additions reject; removing an absent name is harmless. GetType returns Max for an absent name. Other reads and updates reject an absent name. GetList returns a copied ordinal-sorted array. Numeric reads require the exact registered C# carrier. Nonfinite floating-point components reject before mutation, including unused Transform origin or Color channels. Invalid matrix shapes preserve both stored values and caller destinations.

| Static RenderingServer declaration | Contract |
| --- | --- |
| `void GlobalShaderParameterAdd<T>(string name, GlobalShaderParameterType type, T value) where T : unmanaged` | Registers a typed global scalar, vector, rectangle, color or planar basis. |
| `void GlobalShaderParameterAdd(string name, GlobalShaderParameterType type, ReadOnlySpan<float> value)` | Registers a copied numeric global matrix. |
| `void GlobalShaderParameterAdd(string name, Texture? value)` | Registers a borrowed global ordinary texture. |
| `void GlobalShaderParameterAdd(string name, TextureLayered? value)` | Registers a borrowed global image array. |
| `T GlobalShaderParameterGet<T>(string name) where T : unmanaged` | Returns a copied typed base value, independently of its override. |
| `float[] GlobalShaderParameterGetMatrix(string name)` | Returns copied column-major base matrix elements. |
| `void GlobalShaderParameterGetMatrix(string name, Span<float> destination)` | Copies base matrix elements into caller storage. |
| `Texture? GlobalShaderParameterGetTexture(string name)` | Returns the borrowed ordinary base texture, independently of its override. |
| `TextureLayered? GlobalShaderParameterGetLayeredTexture(string name)` | Returns the borrowed base image array, independently of its override. |
| `string[] GlobalShaderParameterGetList()` | Lists the registered global names in ordinal order. |
| `GlobalShaderParameterType GlobalShaderParameterGetType(string name)` | Returns the declared logical role of a global parameter. |
| `void GlobalShaderParameterRemove(string name)` | Removes a global declaration and its override. |
| `void GlobalShaderParameterSet<T>(string name, T value) where T : unmanaged` | Updates a typed global base value while retaining any override. |
| `void GlobalShaderParameterSet(string name, ReadOnlySpan<float> value)` | Replaces copied base matrix elements while retaining any override. |
| `void GlobalShaderParameterSet(string name, Texture? value)` | Updates a borrowed ordinary base texture while retaining any override. |
| `void GlobalShaderParameterSet(string name, TextureLayered? value)` | Updates a borrowed base image array while retaining any override. |
| `void GlobalShaderParameterSetOverride<T>(string name, T? value) where T : unmanaged` | Sets or clears a temporary typed global override. |
| `void GlobalShaderParameterSetMatrixOverride(string name, ReadOnlySpan<float> value)` | Sets or clears a copied numeric matrix override. |
| `void GlobalShaderParameterSetOverride(string name, Texture? value)` | Sets or clears a borrowed ordinary texture override. |
| `void GlobalShaderParameterSetOverride(string name, TextureLayered? value)` | Sets or clears a borrowed image-array override. |

## Value and override behavior

Get operations return base values. Set operations change the base while retaining any override. SetOverride selects an effective value for rendering; nullable scalar/vector/Transform null, texture null or an empty matrix-override span clears it. Clearing immediately reveals the latest base. Texture null is an unassigned base, and a consumer rejects until supplied; it is not a separately stored null override. Copied numeric matrices have exactly nine or sixteen finite column-major elements. Their base and override storage is prepared at addition, so later updates/override changes require no managed allocation. All image getters borrow the resource rather than copying its pixels or native identity.

Bool uses bool; boolean vectors use low-bit int masks. Signed/unsigned vectors use the existing Vector2i/Vector3i/Vector4i bit carriers. Integer and floating rectangles preserve position then size. Color is literal RGBA, independently of HDR canvas policy; apply any intended source-color transfer explicitly in shader code. Mat2 uses the Transform X/Y basis; Transform2D uses its complete planar basis/origin as a homogeneous mat3. Mat3 and Mat4 are numeric float sequences, with no scene-space or three-dimensional object role. The [logical type enum](../classes/RenderingServer.GlobalShaderParameterType.md) lists exact numeric identities and carriers.

## Shader ABI and execution

Declare numeric globals in a std140 Globals block (type.Globals is the equivalent HLSL reflection name). It occupies one of the ordinary contiguous stage buffer bindings: fragment set three, bindings zero through three; vertex set one, bindings one through three after the required Frame buffer at zero. Each block retains the sixteen-KiB limit. Globals do not contain arrays, nested structures or renderer built-ins. Larger square float matrices are accepted only in Globals; the ordinary material profile remains unchanged. Imported boolean metadata covers material, Instance and Globals blocks in both languages.

Name a sampled ordinary/array image GLOBAL_name, with registry name name. Use fragment set two or vertex set zero and that stage's ordinary contiguous sampler bindings. Globals consume real sampler slots; stage capacity stays sixteen. All image signedness/filtering and ownership checks remain those of the existing image/material path. Global fields and sampled globals are excluded from material values, defaults, property descriptors and per-item parameter access. Shader code and node archives store declarations, never renderer-session registry contents.

Matrix values retain the [existing SPIR-V logical-column contract](shader-materials.md#matrix-parameters): apply a planar global with `affine * point` in GLSL and `mul(point, affine)` in HLSL. DXC represents HLSL matrices by their transposes; storage-order selection does not change the value contract.

Both stages share one logical namespace. Matching global names require identical scalar signedness, boolean width and matrix size. Globals cannot alias material, per-item or sampled-resource roles. Physical offsets, matrix storage order and strides may differ; the renderer writes the canonical value and propagates logical components to each layout before its native uniform submission. Sampled globals bind to each stage's existing independent image map. This uses the existing GPU canvas and lighting/instance composition rather than a second shader system.

## Verification and limits

ShaderGlobalParametersTests exercises copied matrices, invalid finite/shape/type edits, base-versus-override reads, independent layout storage, borrowed resources and 1024 prepared updates at zero owner managed bytes. Native GLSL/HLSL profiles draw two separate materials with all 23 numeric roles and both image roles. They verify both stages, true planar translation, integer/boolean values, base/override transitions and 64 active/64 idle owner frames. HDR/MSAA use the same path. Tests run on Linux software Vulkan with hardware mappings absent; these results do not establish physical-device performance, native allocation totals or foreign-platform execution.

The fallback retains its explicit custom-shader rejection. ShaderGlobalsOverride supplies the scene-node priority/lifetime/portable authoring consumer below; a project-settings registry loader retains its exact dependency. External samplers require the external-image ownership layer. Three-dimensional transform, volume and cube roles stay outside the product boundary under ADR 0004. [ADR 0028](../decisions/rendering.md#adr-0028) owns this contract.

## Scene-owned overrides

[ShaderGlobalsOverride](../classes/ShaderGlobalsOverride.md) is the scene authoring/lifetime consumer for the same 25 executable logical roles. It stores values before renderer startup, round-trips exact roles through PackedScene and projects selected entries into the existing temporary-value service. First tree order wins, including ancestor reordering; waiting nodes retain their data. Each SceneTree owns selection and observable groups, while the renderer uses only its root tree. Mandatory membership cleanup removes a disposed selection and promotes its successor even if user lifecycle callbacks fail.

Declared unset globals appear as nonstored nullable descriptors. Authored paths `params/<numeric-role>/<name>` encode the exact role alongside the typed schema, preserving Mat2/Transform2D and signed/unsigned/boolean-mask distinctions without a Variant container. Numeric matrices copy; sampled resources remain borrowed. Dynamic scene/archive schema restoration reconstructs the descriptor without a renderer or a custom type registry. Nullable Rect2i/Transform codecs complete the portable scalar carrier family.

Base reads/writes retain their session semantics. Activation publishes authored temporary entries; editing one entry republishes just that name. A manual temporary write is immediate and remains until an authored edit/activation or leaving the owning node. Leaving clears owned temporary names and restores current bases, then applies the next node. Unowned temporary names remain intact. Unknown authored names activate when registered; conflicting roles reject.

ShaderGlobalsOverrideTests passed 25-role authoring with a shared image alias, fresh-process storage, live unset-descriptor restoration, real membership and ancestor-order changes, independent tree transfer, capture/thread guards, failed exit/activation repair, disposed-image preflight and 1024 prepared updates at 0 owner managed bytes. GLSL/HLSL each passed 160 software-Vulkan frames on Linux Wayland and X11, with twelve pixel phases and 64 active/64 idle intervals at 0 owner managed bytes. A separate public-only GLSL JIT and NativeAOT application passed fresh-file numeric/image schema loading, native pixels and base restoration on both display protocols; NativeAOT maps contained no CoreCLR and no hardware graphics drivers were loaded. Project registry settings, external image ownership and broader platform/native allocation acceptance remain separate.
