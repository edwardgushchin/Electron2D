# Property usage storage and access audit

Last updated: 2026-11-16

Source: [4.7.2-stable @GlobalScope.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/@GlobalScope.xml#L2993-L3094), commit `ed1daf0bf001b61586d9930840f2f1394092c079`. None of these values is deprecated. Owning decisions: [ADR 0005](../decisions/core-object-runtime.md#adr-0005) for discovery/access and future tooling, [ADR 0023](../decisions/packed-scenes.md#adr-0023) for storage. No numeric usage transport is added.

| Role | State | Semantics, projection and remaining prerequisite |
| --- | --- | --- |
| STORAGE | Implemented | Bit 2 selects scene serialization. IsStored=true on writable typed descriptors drives capture and ordinary setter restoration. PropertyUsage.Consumer checks memory/file round trips and transient exclusion; storage does not imply editor visibility. |
| NONE | Blocked | Value 0 combines no storage with no Inspector display. stored=false retains discovery/read/write and skips storage. Trigger: independent typed Inspector visibility metadata and consuming inspector under ADR 0005. |
| NO_EDITOR | Blocked | Value 2 aliases STORAGE without the EDITOR bit. IsStored=true covers storage only and does not hide discovery or Inspector display. Trigger: independent typed Inspector visibility metadata and consuming inspector under ADR 0005. |
| READ_ONLY | Blocked | Bit 268435456 restricts Inspector editing, not programmatic setters or restoration. IsReadOnly means no programmatic setter and is not equivalent. Trigger: independent editor-only editability metadata and consuming inspector preserving typed writes under ADR 0005. |

Blocked remains in the coverage denominator. Other PropertyUsageFlags rows retain their states. An absent editor consumer does not exclude applicable behavior.

## Runtime consumer and boundaries

`tests/PropertyUsage.Consumer` uses only public API. It exercises heterogeneous discovery, typed indexed read/write, writable stored/transient and getter-only transient descriptors; validation and invalid stored-without-setter/revert-without-setter metadata; memory and file PackedScene restoration and transient exclusion. Getter failure/retry, reentrant reads and disposed access are explicit cases. `ObjectPropertyTests` covers teardown inspection, delegate errors and physical owner/capture guards; the resource-local-scene root profile covers adjacent packed graph/archive behavior. These checks do not qualify an Inspector, rendering, foreign hosts or the full ADR 0021 platform matrix.

Capture uses `Node.ShouldStoreSceneProperty` and `PropertyDescriptor.ShouldStore`; restoration uses `PackedScene.ApplyProperties`. Conditional storage and resource duplication retain their existing policies. No runtime discrepancy requiring a storage algorithm change was found. XML distinguishes runtime capabilities from editor presentation. A writable stored descriptor is not evidence that an Inspector respects READ_ONLY, and discoverability is not editor visibility.

## Pinned source consumers

The [Inspector](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/editor/inspector/editor_inspector.cpp#L4353) computes property_read_only from the usage bit and inspector state. This is a presentation consumer, independent of descriptor setter existence. The same Inspector filters PROPERTY_USAGE_EDITOR before constructing property controls; NONE and NO_EDITOR omit that bit. These consumers explain the separate Blocked prerequisites.

The [packed-scene capture loop](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/resources/packed_scene.cpp#L799-L802) selects STORAGE independently of Inspector flags. The [visibility filter](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/editor/inspector/editor_inspector.cpp#L4067-L4070) requires EDITOR.
