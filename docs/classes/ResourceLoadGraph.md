# ResourceLoadGraph

Last updated: 2026-10-21

Internal staged graph implementation in [ResourceLoadGraph.cs](../../src/Core/IO/ResourceLoadGraph.cs), compiled into Electron2D.dll. No public constructor/backend surface is exported. [Threaded resource loading](../components/threaded-resource-loading.md) owns its actual verification and limits.

Entry records retain path/cache policy, preparation gate, dependency records, prepared/final identities, archive ownership, UID and transfer state. Thread-local scope carries only the active graph/file/depth; dependency group callbacks explicitly restore it. Shared records coordinate compatible per-path/policy requests; independent ignore modes preserve fresh ownership. Dependency edges use canonical file paths and reject cycles before blocking on an active entry.

Preparation returns independent data and captured contexts. Planning chooses current cache identities; publication rewrites stored/opaque references before compatible copying or fresh registration. Archive contexts transfer local/new external ownership, copied graphs retain existing lease semantics, and cleanup attempts all unused contexts. The synchronous loader gate protects owner publication; worker preparation bypasses that global cache gate and does not mutate live cache resources.

Records, parser buffers, closures and preparation may allocate outside the hot interval. Terminal status polling reuses retained request state and is separately checked. Foreign/native allocation and target acceptance are not inferred from this managed implementation.

Version-2 role-aware missing-resource storage, snapshot policy and reference relocation are described in [missing resource recovery](../components/resource-files.md#missing-resource-recovery). Known version-1 schemas remain readable; unknown legacy identities do not become safe placeholders.

## Localized graph preparation

A graph captures the immutable resource map and global culture name. Entries retain logical paths/cache keys and record whether they are mapped; decoding uses the selected physical file. Internal paths and source UID remain logical, while relative external dependencies resolve beside the selected archive. Stale mapped preparation is rejected before cache/UID publication; unrelated settings versions preserve the map identity when records are unchanged. Final/borrowed mapped results enter ResourceLoader's weak association table. Reentrant locale changes during publication trigger a current refresh. Independent IgnoreDeep preparation supplies in-place live refresh without borrowing the target graph. ResourceLocales.Consumer exercises archived bundled resources, original/variant UID separation, physical/original format hooks, culture/configuration changes while pending and deterministic cleanup.
