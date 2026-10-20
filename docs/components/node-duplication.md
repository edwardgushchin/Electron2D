# Runtime node hierarchy duplication

Last updated: 2026-10-20

Node.Duplicate returns a caller-owned detached hierarchy. It is cold authoring/spawning work, using the same static scene factories, typed property snapshots, intrinsic property schemas, linked templates and graph ownership as PackedScene. It does not serialize a temporary scene or apply Pack's Owner selection. [ADR 0023](../decisions/packed-scenes.md#adr-0023) owns the contract; [ADR 0091](../decisions/scripting.md#adr-0091) preserves compiled C# types and the separate live-script-binding obligation.

## Flags and construction

The nested Node.DuplicateFlags bitmask retains Signals1, Groups2, Scripts4, UseInstantiation8, InternalState16 and Default15. Zero is valid; unknown bits reject before construction.

- All ordinary descendants are copied regardless of their Owner. Internal children originate in fresh constructors or templates, without copying old internal identities. Names and scene-origin/editable/placeholder metadata follow the source. Raw nodes have no authoring owner; reused linked contents keep their template ownership.
- Scripts constructs the exact compiled source roles through static CreateSceneInstanceFactory hooks. Without it, fresh nodes use the nearest engine base through its registered concrete factory; properties absent from that role are omitted. Types already supplied by a linked template remain. With Scripts, a differing template role is replaced through the existing detached replacement mechanism. This does not mutate CLR identity or implement GetScript/SetScript.
- UseInstantiation starts from the actual linked PackedScene or saved source path. Stable source identities, with a source-path fallback for replaced authored nodes, select inherited targets. New ordinary additions are reconstructed at their source parent/index, including additions below inherited nodes. Template-supplied groups, compiled roles and subscriptions remain when the corresponding copy flag is disabled.
- Groups copies both runtime and persistent memberships. Signals copies active registered authored SceneConnectionBinding subscriptions after properties/references. Internal receivers remap, outside receivers remain borrowed, and ordinary CLR event handlers remain with their original objects. Existing constructor/template subscriptions are matched by schema, receiver and typed encoded binding payload, consuming each baseline once and retaining authored multiplicity. A compiled callback cannot survive removal of its endpoint role when Scripts is omitted.

The result has fresh node identities and does not receive Ready until actual activation. Reusing a PackedScene keeps its normal scene-instantiated notification before copied overrides. Factory hooks retain their context construction barrier; source capture rejects mutation/reentry. Invalid, attached, borrowed/reused or mismatched factory results fail explicitly. Derived classes need the established exact static factory hook; this is the typed C# construction boundary.

## State and lifetime

Writable stored descriptors are captured by default. InternalState additionally selects writable nonstored typed descriptors under the existing supported value profile. Known theme overrides, indexed fields, shader parameters, preloader slots and foldable title-control roles use their existing typed reconstruction bindings. Inherited translation domains remain inherited.

Supported array containers are independent. Node references inside the duplicated hierarchy remap after construction; references outside it remain borrowed. Ordinary resource edges retain identity, including scene-local resources. Each always-duplicate resource property receives an independent shallow Resource.Duplicate result. No new dynamic value, reflection setter or property-name invocation boundary is introduced.

Before callbacks, duplication retains file and owned-resource leases across the source subtree, its ancestors and authoring-owner chain. Copies therefore survive deletion of their original scene, including a copied child whose resource belongs to the scene root. The existing internal ResourceFileOwnership lease owns scene resources from adoption, accepts appended resources, transfers through node replacement and releases on the last holder. Shared resources keep their original weak local-scene association; disposing that scene clears the association without rebinding the resource to the copy. Caller-owned outside resources remain borrowed and explicit caller disposal remains visible.

Isolated save preparation transfers only exclusive direct resource ownership. Shared/composite ownership cannot silently transfer. Source capture, owner affinity, native physics authoring checks and disposed-resource validation remain in effect. Returned nodes stay protected from activation/disposal during reconstruction. Property/factory/subscription/topology failure attempts cleanup of every acquired node, shallow resource copy and lease, preserving borrowed outside objects and aggregating cleanup errors.

## Executable verification

NodeDuplicationTests exercises stored/nonstored flags, compiled type omission, both group modes, ordinary/internal children, array independence, bidirectional node references, inside/outside authored subscribers, CLR handler omission, linked overlays/additions, resource leases, later scene activation and constructor/setter/getter/owner failures. Its catalog profile covers the current registered production node factories.

The file producer copies an authored typed actor hierarchy with a scene-local texture, removes its source, assigns storage owners and saves the copy. A separate process registers the same compiled factory, loads it, duplicates again, removes the loaded source and executes real headless lifecycle/frame callbacks. Native profiles load a separately produced file, draw two differently modulated copies using the same actual texture, delete the original and check that the surviving copy still draws its expected pixels. Prepared redraw intervals measure managed owner allocation separately from cold duplication.

Evidence belongs under bin/node-duplication/2026-10-20. Source-built Linux x64, software native GPU/Compatibility, independent package/NativeAOT, foreign-platform, native-allocation/performance and human acceptance are separate gates; only executed results may be claimed.

The broader EditorSceneTests native-resize smoke reaches its unchanged 120-frame failure on the retained main runtime without Node.Duplicate and on the candidate (Wayland and X11 controls). This remains an open editor/window verification gate, recorded in editor-controls.json; it is not a successful editor acceptance result.
