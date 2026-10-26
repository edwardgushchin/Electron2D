# Material placeholders and recovery

Last updated: 2026-10-26

## Public contract

[Material.CreatePlaceholder](../classes/Material.md) returns a fresh caller-owned [PlaceholderMaterial](../classes/PlaceholderMaterial.md) with default resource metadata. It copies neither shader/parameters nor resource names, paths or scene-local policy. Every live CanvasItemMaterial, ShaderMaterial, ParticleProcessMaterial, BlitMaterial and PlaceholderMaterial supports the factory; disposed sources reject it. The result has a stable borrowed material RID and ordinary canvas color drawing. It cannot serve as a particle compute program.

The built-in PlaceholderMaterial factory permits strict save/load of explicitly authored placeholders. This explicit operation differs from automatic recovery: it has no lost source payload to restore.

## Typed missing assets

The version-8 archive adds Material role 3, without bounds. Unknown factories and missing external archive dependencies can produce PlaceholderMaterial only when ResourceLoader.SetAbortOnMissingResources(false) was captured at graph entry or threaded request creation. Strict mode is the default. Known version-1–7 files retain their original layouts and material role rules; an unknown legacy generic resource cannot safely be reclassified as Material.

Original factory IDs, raw scalar bytes and byte order, nullable resource arrays, aliases/cycles and missing-payload provenance survive deep copies, bundled/compressed/big-endian files and scene-local instantiation. Local aliases share one copy inside an instance and differ between instances. Saving a missing subresource as a root preserves its original identity. Shallow CopyFromResource preserves the target RID and shallow source reference edges.

An unavailable external payload remains unavailable after bundling, even if its factory is known: tolerant reload retains the placeholder and strict reload rejects. An unbundled external asset can revive when the actual file returns. Fresh processes with the original factory restore retained fields and scene aliases. Reuse keeps the existing cached identity; successful ReplaceDeep can publish a returned concrete material without disposing borrowed old wrappers. Failed decode leaves prior cache entries intact.

Role/provenance corruption, incompatible concrete material fields, schema mismatch and factory failures reject without a fallback or partially published cache. Generic MissingResource cannot impersonate Material. Recovery uses registered compiled factories and bounded typed fields, with no CLR member dispatch. See [resource files](resource-files.md), [ADR 0013](../decisions/resources.md#adr-0013) and [ADR 0014](../decisions/resources.md#adr-0014).

## Canvas integration and verification

The loaded missing scene renders its original blue polygon color over red. Rebinding the restored CanvasItemMaterial RID with additive blending produces magenta, and rebinding the placeholder restores blue. Both high-level scene ownership and direct RenderingServer bindings use the existing material lifetime. FreeRID rejects borrowed resource identities.

The managed profile passes 62 checks. MaterialPlaceholderTests covers the entire factory family, metadata/lifetime, worker policy capture, copies/cycles/arrays, scene-local aliases, fresh-process restoration, external revival, caches, concrete subtype boundaries, known legacy versions 1–7 and corrupt version/role/provenance. MaterialPlaceholderTests.Rendering exercises 80 actual native frames per GPU/Compatibility profile and 64 prepared alternating material switches with zero measured owner-thread managed bytes. PNG readbacks are retained under the local verification artifact directory.

Evidence uses Linux Wayland, Vulkan lavapipe and Mesa llvmpipe. This establishes software-native output for the current host; physical GPUs, native allocation totals, foreign platforms and owner acceptance remain separate.
