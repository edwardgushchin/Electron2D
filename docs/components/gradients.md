# Gradients component

Last updated: 2026-09-23

## Scope and owned types

[Gradient](../classes/Gradient.md), [InterpolationMode](../classes/InterpolationMode.md), [Gradient.ColorSpace](../classes/Gradient.ColorSpace.md), [GradientRampTexture](../classes/GradientRampTexture.md), [GradientTexture](../classes/GradientTexture.md), [FillEnum](../classes/FillEnum.md), [Repeat](../classes/Repeat.md), and internal [GradientTextureData](../classes/GradientTextureData.md). Sources live in `src/Scene/Resources/`, within Resources and the single Electron2D.dll.

## Runtime flow and dependencies

1. Gradient owns copied color points and lazily sorts them for indexed operations or sampling. Bulk arrays and RemovePoint use current storage order.
2. Sampling applies Linear, Constant or Cubic interpolation in SRGB, LinearSRGB or OKLAB; alpha remains independent. Existing Color/Mathf/internal OKLAB math supplies the conversions and interpolation.
3. Textures borrow the gradient and invalidate on its synchronous Changed event without forwarding the event. Texture-setting notifications follow their documented equality policy.
4. Image access or renderer use coalesces pending work into one coherent copied RGBA8/RGBAF snapshot. Ramp samples include both endpoints; planar fills implement Linear/Radial/Square/Conic and None/Repeat/Mirror. Null source keeps prior pixels. Baking emits no texture event.
5. Existing Texture drawing and material bindings upload snapshots lazily. Resource hooks provide exact-state copies, graph policy and PackedScene local ownership. No editor/importer/backend is needed for CPU use.

## Invariants and implemented behavior

Explicit offsets, colors and fill coordinates are finite; texture dimensions are 1..16384. HDR retains signed and overbright float values. Sampled byte colors round; solid planar fills use Image.Fill truncation. Warm Gradient.Sample allocates nothing. Baking/copying/array exports allocate and can fail for excessive buffers. Per-resource locking serializes edits with a complete bake; no cross-resource transaction or throughput guarantee is provided.

Events run on the editing thread after mutation and outside state locks. Callback failures retain committed state and follow the inherited C# event short-circuit behavior. Borrowed sources must stay alive while used; a failed lazy bake keeps the old payload and pending work. Texture disposal detaches sources, never disposes them. Null-source duplicates may share immutable CPU payloads safely; generated source copies obey Resource deep/shallow policy. Scene roots own only local duplicates.

## Correspondence and boundaries

Gradient retains its applicable reference API. GradientTexture1D maps to GradientRampTexture and GradientTexture2D maps to GradientTexture: distinct resource roles without dimensional suffixes. The selected mode enums are namespace-level under ADR 0051; resource properties retain their names. Acronym spelling for methods and properties follows ADR 0045. The reference private update_now helper is not a public method and is not exported. Width-one ramp sampling explicitly uses offset zero; widened arithmetic avoids finite coordinate overflow, under [ADR 0013](../decisions/resources.md#adr-0013) and ADR 0034.

Shared Resource/Texture integration gaps remain Partial on their own rows. Native placeholder IDs, further backend formats, import/disk serialization and editor authoring are absent or deferred to those first concrete slices; no inert APIs are added. The first editor inspector must hide interpolation color space when Constant is selected. Cross-resource automatic serialization remains deferred to the typed asset-format slice.

## Verification and limits

[GradientTests](../../tests/Electron2D.Tests/GradientTests.cs) verifies numeric interpolation and all spaces/modes, lazy storage ordering, inclusive pixels, all fills/repeats, extreme/degenerate coordinates, byte quantization, copies/local ownership, events/failure/disposal, checked-size recovery, coherent concurrent images and warm allocation behavior.

[RenderingGradientTests](../../tests/Electron2D.Tests/RenderingGradientTests.cs) verifies LDR canvas patterns and worker changes without geometry rebuilding on Linux Wayland GPU/compatibility and dummy/software. GPU preserves HDR in canvas and six signed/HDR/live-source/removal stages for each HLSL/GLSL material. Tested compatibility drivers explicitly reject unavailable HDR precision and release resources. Uninitialized binding failure and borrowed ownership are checked. This does not establish other platforms, owner visual acceptance, fresh AOT/self-contained publication or throughput targets.
