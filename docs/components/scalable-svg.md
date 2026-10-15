# Scalable SVG textures

Last updated: 2026-10-15

DPITexture integrates SVG source, typed paint remapping, image processing, resource storage and retained canvas rendering. The existing SDL SVG decoder supplies raster pixels. No new codec or modified vendored source is introduced. Paint replacement operates on source attributes before rasterization, preserving antialiased edges and gradient interpolation rather than recoloring completed pixels.

## Viewport and ownership

Viewport.Oversampling defaults true and OversamplingOverride defaults zero. Disabled oversampling returns one. A positive override wins; otherwise GetOversampling uses the maximum active stretch axis, including the physical density of embedded windows. Fonts consume the same factor. Root content presentation and offscreen logical stretch retain their existing separate coordinate roles.

DPI raster keys truncate the effective factor, clamped to 0.1 through 100, into 1/64 steps. The base resource identity is stable. Other variants have independent borrowed RIDs, logical sizes and native raster dimensions. Active viewport counts and weak texture registrations retain only used scale levels. Changing or disposing the last viewport at a level retires its variants outside registry locks. Resource edits/disposal also retire variants. A generation token distinguishes a new level from an old level with the same numeric scale.

Retained canvas commands borrow the original DPITexture. Replay resolves the actual viewport variant, including live edits and foreign canvas targets; it does not retain obsolete scale objects in recorded drawing. Typed source/map/settings persist through an explicit resource factory and deterministic bounded Color-to-Color archive codec. Scene-local copies acquire independent resource identities and raster caches.

Built-in SVG theme icons retain DPITexture sources, including the universal error icon. Raster decoding is lazy. Nonunit default-theme construction and the remaining theme asset audit retain their own coverage obligations; viewport adaptation alone does not complete ThemeDB.

## Processing and limits

SVG source is validated as bounded UTF-8/XML without DTD resolution. The image decoder keeps its 64 MiB source/pixel budgets and axis limits. Double-quoted fill, stroke and stop-color attributes map exact Color keys to replacement RGB; opacity is independent, and none/url paints stay intact. Saturation, alpha-edge repair and optional premultiplication follow rasterization in that order. Backend SVG syntax and source-layout/color limitations remain the Image codec obligations.

Scale/settings changes and first raster generation allocate. Stable replay must meet the measured warmed-frame budget. Host-owned shader samplers retain ordinary base-RID behavior unless the caller explicitly selects a scaled RID. Premultiplied pixels require a corresponding material.

## Verification

DPITextureTests exercises native paint/gradient/stroke/opacity output, base and logical dimensions, defensive copies, equal-write notifications, validation failures, independent duplicates, typed archives and viewport policy. DPITextureFlowTests exercises actual GPU/compatibility pixels, simultaneous two/three/unit scale identities, last-view retirement, live paint/source clear-and-restore changes without custom rerecording, quantized identity reuse, actual font raster changes and 64 consecutive prepared full frames at zero managed bytes on the owner thread on Linux. The measurement restarts only on observed external window/input transitions, never on an allocation count; a bounded frame deadline fails if the host never settles. An allocation-positive control verifies that ordinary per-frame allocations still fail the strict zero-byte gate. Fresh-process JIT and independent NuGet/NativeAOT consumers restore SVG/maps and independent scene-local resources, render saved scenes on GPU/compatibility and pass the same pixel/lifetime/strict warmed-frame profiles. Strict IL warnings-as-errors publishing passes. A separate consumer without a native package saves and reloads authoring data and viewport policy without decoding pixels. The allocation-positive control is rejected at 9728 bytes across 64 samples; ordinary profiles retain the zero-byte threshold. Ignored evidence is retained under bin/dpi-texture-validation/2026-10-15. Foreign hosts and native allocation counts remain unverified.

[ADR 0039](../decisions/resources.md#adr-0039) owns the contract; [ADR 0083](../decisions/rendering.md#adr-0083) owns theme consumers.
