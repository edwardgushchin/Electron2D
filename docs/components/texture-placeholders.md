# Texture placeholders

Last updated: 2026-10-17

[PlaceholderTexture](../classes/PlaceholderTexture.md) preserves ordinary texture dimensions for headless layout and explicitly stripped assets. [Texture.CreatePlaceholder](../classes/Texture.md) consumes virtual logical size, returns independent metadata and drops the source image and source resource identity. The same producer works for custom textures, image textures, atlas views and viewport textures.

The resource reuses typed property descriptors, resource duplication, built-in file registration, scene-local ownership and the existing diagnostic rendering fallback. No new renderer allocation or global service is introduced. `GetImage()` stays null while RenderingServer RID readback returns a caller-owned diagnostic image. GPU and Compatibility draw that fallback through ordinary object and RID canvas commands.

The pinned scalar size getter reads integer width/height, so fractional writes truncate toward zero. Signed and zero metadata remain representable. Nonfinite or nonrepresentable dimensions reject before mutation rather than invoking undefined float-to-integer conversion. Equal assignments emit `Changed`; consumers invalidate their usual layout and draw caches.

## Slice selection

| Candidate | API value and dependent work | Current prerequisite |
| --- | --- | --- |
| Node duplication: eight declarations | Reusable runtime gameplay/UI subtrees and clone commands; flags must control groups, scripts, connections and state. | ADR 0091 live script binding/identity and ADR 0023 linked scene instantiation, plus complete runtime subtree/reference copying. Owner-selected Pack cannot replace these semantics. |
| Deferred scene placeholders: six declarations including Node flags | Load large subscenes on demand; enables deferred scene authoring. | Loader exists; linked subscene override records, typed pending properties and failure-safe replacement/ownership are still absent. |
| Ordinary texture placeholders: four declarations | Explicit image stripping, persisted headless layout and diagnostic runtime consumers; reuses current resource/renderer backends. | Selected: typed archive registration, metadata copying and native diagnostic paths can execute now. Completed by the version-4 recovery follow-on below. |

## Verification

The focused runner uses `ELECTRON2D_TEST_TEXTURE_PLACEHOLDER=managed`, `gpu` or `compatibility`. It covers defaults, truncation and integer boundaries, rejection without mutation, equal-write notifications, layout invalidation, virtual factory size, source disposal, copies and stable/fresh RID ownership. Compressed big-endian resource and bundled scene archives load in a fresh process; scene-local aliases, independent instances and disposal are checked.

Native profiles inspect actual checkerboard pixels, object/RID drawing, zero-size redraw and borrowed lifetime. Each measures 64 prepared active size-update/render frames. The exercised native profiles pass on Linux both offscreen and under Wayland with software Vulkan/Compatibility; physical GPUs, foreign platforms, engine-owned native allocation totals, owner acceptance remain unverified.

## Typed missing-texture recovery

Version 4 extends the resource archive to retain ordinary texture dimensions and missing-payload provenance. Tolerant loading preserves unknown custom texture fields and references privately in PlaceholderTexture. Duplicate/local/file sessions relocate references normally, including cycles. Re-registering the compiled factory restores the actual class in a fresh process; ReplaceDeep can publish the new cache identity while a borrowed placeholder remains alive.

An unavailable external file provides metadata rather than a recoverable payload. Bundled saves retain this distinction: tolerant loading keeps the placeholder and strict loading rejects, including for known built-in factories. Unbundled saves preserve the dependency identity and allow a returning file to restore actual pixels. Missing external resources retain name, local-scene policy and scene ID. External factory identity is checked even when both resources have the same placeholder wrapper type.

The follow-on compared this capability with linked scene instances and Node duplication. It unlocks ordinary texture loading, headless layout and diagnostic scenes on the current backends without the unresolved linked-scene or live-script contracts. Layered textures and incompatible concrete property types retain explicit separate prerequisites.

The focused runner uses ELECTRON2D_TEST_MISSING_TEXTURE=managed/gpu/compatibility. Managed checks cover fresh producer/preserve/restore processes, opaque aliases/cycles, duplication, local scenes, threads, available-factory cache replacement, missing known-image dependencies, bundled rejection and external-file revival, empty original identities, external identity mismatches, legacy versions 1–3 and checksum-repaired malformed metadata. Native profiles consume missing and restored scenes through actual renderers and measure prepared active frames. See the retained verification manifest for completed profiles; physical/foreign/native-allocation and owner acceptance remain separate gates.
