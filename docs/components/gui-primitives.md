# GUI backgrounds and separators

Last updated: 2026-10-15

Owns [ColorRect](../classes/ColorRect.md), abstract [Separator](../classes/Separator.md), [HSeparator](../classes/HSeparator.md) and [VSeparator](../classes/VSeparator.md). These primitives add common panel/list decoration through existing Control, typed themes, StyleBox and canvas drawing. They add no renderer, dependency or separate assembly.

ColorRect stores a finite fill color and records the inherited local rectangle. It contributes zero intrinsic minimum and preserves ordinary pointer input policy; Ignore makes a background decorative. Separator contributes the axis-specific theme separation, centers a borrowed style by exact truncated integer dimensions and preserves that style's own drawing/orientation. Built-in horizontal/vertical gray line resources have spacing four and matching content margins. Theme changes reuse inherited owner invalidation, including deferred changes from workers.

PackedScene and ResourceFileTypes reconstruct all three concrete types without reflection. Inherited type variations, local theme overrides, color/layout/visibility/input settings retain their existing storage rules. The base Separator remains abstract. Resources remain borrowed; file-root/scene graph leases keep decoded authored resources alive independently of templates.

## Verification

GUIPrimitivesTests covers defaults and abstractness, finite/signed/HDR color and atomic failure, exact fractional/oversized style rectangles, signed separation versus combined minimum, type variations, live worker theme/style changes, borrowed lifetime, input stop/ignore, all concrete packed/file factories, capture/owner/disposal guards and warm active/idle recording.

GUIPrimitivesRenderingTests loads an authored compressed big-endian Window scene and verifies actual fill, both default axes, live color/style/thickness and visibility on GPU/compatibility. Warm frame allocation is measured from ProcessFrameStarted through FramePostDraw, separately from readback and input setup. Captures remain in the validation proof. Native allocation, broader GUI performance, accessibility host integration, other platforms and final owner acceptance are separate gates.

## Dependencies and remaining families

[ReferenceRect](../classes/ReferenceRect.md) now supplies editor/runtime border drawing through [editor canvas guides](editor-canvas-guides.md). These consumers use the existing Control/canvas pipeline and ADRs 0008, 0023, 0028 and 0083. [DPITexture](scalable-svg.md) now supplies paint remapping and retained viewport raster variants. TextLine/TextParagraph need their full typed builder, inline objects/metadata and shaped handle contract; these controls do not stand in for that API.
