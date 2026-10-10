# ResourceLoadGraph

Last updated: 2026-10-10

Internal staged graph implementation in [ResourceLoadGraph.cs](../../src/Core/IO/ResourceLoadGraph.cs), compiled into Electron2D.dll. No public constructor/backend surface is exported. [Threaded resource loading](../components/threaded-resource-loading.md) owns its actual verification and limits.

Entry records retain path/cache policy, preparation gate, dependency records, prepared/final identities, archive ownership, UID and transfer state. Thread-local scope carries only the active graph/file/depth; dependency group callbacks explicitly restore it. Shared records coordinate compatible per-path/policy requests; independent ignore modes preserve fresh ownership. Dependency edges use canonical file paths and reject cycles before blocking on an active entry.

Preparation returns independent data and captured contexts. Planning chooses current cache identities; publication rewrites stored/opaque references before compatible copying or fresh registration. Archive contexts transfer local/new external ownership, copied graphs retain existing lease semantics, and cleanup attempts all unused contexts. The synchronous loader gate protects owner publication; worker preparation bypasses that global cache gate and does not mutate live cache resources.

Records, parser buffers, closures and preparation may allocate outside the hot interval. Terminal status polling reuses retained request state and is separately checked. Foreign/native allocation and target acceptance are not inferred from this managed implementation.
