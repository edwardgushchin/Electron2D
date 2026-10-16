# ShaderGlobalsOverride

Last updated: 2026-10-16

Source: [ShaderGlobalsOverride.cs](../../src/Scene/Main/ShaderGlobalsOverride.cs). Declaration: `public class ShaderGlobalsOverride : Node`.
Inherits [Node](Node.md). No spatial or visibility role is added.

Temporarily supplies scene-authored values to the shared [global shader registry](../components/global-shader-parameters.md).

## Description

The first attached node in depth-first tree order is active, including nodes with no authored entries. Other nodes wait with their values intact. Reordering an ancestor, reparenting, removal and disposal immediately reconcile selection. `shader_overrides_group` contains all attached instances; `shader_overrides_group_active` identifies the selected instance. Synthetic entry/exit notifications and arbitrary nodes in those groups do not affect selection. Each SceneTree selects independently; only the tree of the renderer's root Window publishes values to that rendering session.

The node stores copied numeric values and borrowed ordinary/array images without requiring a renderer. Logical roles use [RenderingServer.GlobalShaderParameterType](RenderingServer.GlobalShaderParameterType.md). Numeric Mat3/Mat4 use nine/sixteen column-major floats. Mat2 and Transform2D use Transform, with distinct retained roles. Floating-point values must be finite. Changing an authored role requires clearing the entry first. Null scalar/image values and empty matrix spans disable an override rather than storing an inert enabled flag.

An undeclared name remains authored and takes effect when the renderer registers it. Conflicting roles reject before live assignment or registration; an incompatible offline-authored scene fails activation/render preflight and can be repaired by clearing its entry. No incorrect carrier is uploaded. A disposed borrowed image rejects before new scene values are applied.

Activation publishes all authored entries into the renderer's temporary overrides. Subsequent node edits publish only their named entry. Base writes remain independent. A manual RenderingServer temporary override commits immediately; a later node edit of that name or node activation republishes its authored value. Leaving clears the selected node's authored temporary names, then publishes the next node. With no replacement, the current base values apply; a previous manual temporary value is not restored. Unowned temporary names remain intact. Renderer disposal releases references and borrows no resources beyond the session.

Reads require the attached tree owner. Writes also reject packed-scene capture, conflicting authoring access and native submission. Declared but unset globals appear as nonstored nullable descriptors when a renderer is active; stored entries use `params/<numeric-role>/<name>` to preserve shared-carrier distinctions without an untyped value. Ordinary typed property setters/getters and revert-to-null apply. PackedScene archives restore these roles and resource aliases in a fresh process; unset global defaults and session values are not embedded.

## Example

Partial snippet: `scene` is the parent Node; renderer startup registers the matching Float global.

```csharp
var weather = new ShaderGlobalsOverride { Name = "Weather" };
weather.SetShaderParameter<float>("rain", RenderingServer.GlobalShaderParameterType.Float, 0.8f);
scene.AddChild(weather);
weather.Owner = scene; // Include it when packing the containing scene.
// Register the matching "rain" global during renderer startup.
// Removing weather restores the current base rain value or promotes another override node.
```

## Public API

All instance methods retain the access/lifetime/error rules above.

| Signature | Result or effect |
| --- | --- |
| `public ShaderGlobalsOverride()` | Detached empty node. |
| `public void SetShaderParameter<T>(string name, RenderingServer.GlobalShaderParameterType type, T? value) where T : unmanaged` | Copies or clears a scalar/vector/rectangle/color/Transform role. |
| `public void SetShaderParameter(string name, RenderingServer.GlobalShaderParameterType type, ReadOnlySpan<float> value)` | Copies a Mat3/Mat4, or clears with an empty span. |
| `public void SetShaderParameter(string name, Texture? value)` | Borrows or clears an ordinary image. |
| `public void SetShaderParameter(string name, TextureLayered? value)` | Borrows or clears an image array. |
| `public T? GetShaderParameter<T>(string name) where T : unmanaged` | Reads a copied authored value or null; carrier mismatches reject. |
| `public float[]? GetShaderParameterMatrix(string name)` | Copies authored matrix elements or returns null. |
| `public Texture? GetShaderParameterTexture(string name)` | Returns the borrowed authored ordinary image or null. |
| `public TextureLayered? GetShaderParameterLayeredTexture(string name)` | Returns the borrowed authored image array or null. |
| `public string[] GetShaderParameterList()` | Independent ordinal-sorted authored name list. |
| `public RenderingServer.GlobalShaderParameterType GetShaderParameterType(string name)` | Authored logical role; Max for an absent name. |
| `public override string[] GetConfigurationWarnings()` | Inherited warnings plus the waiting-node warning. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Inherited, authored stored and live unset declaration descriptors. |
| `protected override void Dispose(bool disposing)` | Completes inherited cleanup and clears retained authored values without disposing borrowed images. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Exact built-in factory; derived types retain the inherited factory contract. |

## Method descriptions

### SetShaderParameter

Names are case-sensitive and nonblank; reserved canvas names reject. A supplied role must match its carrier. Invalid names, shapes, nonfinite values or incompatible roles throw ArgumentException before changing that entry. A disposed image throws ObjectDisposedException. After commitment, publication and property-list notification are both attempted and their failures aggregate. If another incompatible offline entry prevents publication, the edited authored value remains committed and its descriptor change is still notified; clear the incompatible entry before rendering. A user notification failure also occurs after commitment. Numeric matrix setters copy input before returning. Clearing removes the corresponding stored descriptor.

### GetShaderParameter, GetShaderParameterMatrix, GetShaderParameterTexture, GetShaderParameterLayeredTexture

Return authored values independently of selection and renderer base/temporary state. Missing names return null. An existing incompatible carrier/role throws ArgumentException. Matrices are independent arrays; images retain borrowed identity.

### GetShaderParameterList and GetShaderParameterType

Return authored names and logical roles only. Mutating the returned list changes no node state. The Max sentinel indicates absence; it is not an authorable role.

### GetConfigurationWarnings

An attached waiting node reports that another node precedes it. Detached and selected nodes add no selection warning. Selection, renderer publication and edited-scene warning refreshes are mandatory membership work, independent of overridable notifications. Warning refreshes follow actual registration even during owner-thread pre-delete reordering; the temporary disposed flag does not suppress them. Callback failures are aggregated after attempting remaining cleanup/promotion and warning delivery.

### GetPropertyDescriptors and CreateSceneInstanceFactory

Expose the complete inherited schema plus typed scene values and preserve exact built-in reconstruction. Derived types must follow Node's factory requirements. Dynamic storage reconstruction accepts canonical numeric role identities and exact value schemas; malformed or unsupported descriptors are rejected.

## Verification and limits

[ShaderGlobalsOverrideTests](../../tests/Electron2D.Tests/ShaderGlobalsOverrideTests.cs) owns all-role authoring/archive, fresh-process load, real membership/ordering, tree transfer, guards and prepared owner-allocation checks, plus GLSL/HLSL pixel phases and active/idle rendering checks. Actual results are recorded with the completed slice in the [component](../components/global-shader-parameters.md#scene-owned-overrides).

The node adds no source compiler or backend. Shader drawing retains the GPU requirement. Project-default registry loading, external samplers, foreign platforms, native allocator measurements and human visual acceptance remain separate obligations. See ADRs [0004](../decisions/product.md#adr-0004), [0023](../decisions/scene.md#adr-0023), [0028](../decisions/rendering.md#adr-0028) and [0090](../decisions/agent-native.md#adr-0090).
