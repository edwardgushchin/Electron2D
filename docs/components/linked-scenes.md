# Linked packed scenes

Last updated: 2026-10-17

## Scope and selection

PackedScene now retains actual runtime subscene templates, reconstructs their complete branches and overlays the selected typed state. This extends executable reuse and scene files on the existing node, resource and rendering backends. Deferred materialization now executes through [deferred scenes](deferred-scenes.md). Root inheritance and non-Disabled PackedSceneEditState modes retain their separate contracts.

| Candidate | Public value and dependent slices | Current feasibility |
| --- | --- | --- |
| Linked runtime scenes | Reusable actors/levels, actual SceneState instance/index/type queries; supplies the template boundary needed by six deferred-placeholder declarations and the instantiation part of Node duplication. | Selected. Existing compiled factories, typed properties, file graph and both canvas renderers can execute the result. |
| Node duplication: eight declarations | Complete runtime subtree copying, groups, signals, script and internal-state policies. | ADR 0091 must first settle live script ownership/identity; the Scripts bit cannot be replaced by stored-property capture. Runtime duplication remains separate from this scene model. |
| Layered missing-resource recovery | Restorable custom array producers and shader consumers. | Existing array placeholders and GPU sampling are usable, but another asset role would need independent metadata/provenance and typed substitutions. It unlocks fewer dependent scene capabilities. |

## Capture and inspection

A real instantiated root retains its borrowed PackedScene identity and original node paths. Packing it as an owned nonroot stores that template reference. An editable inherited child stores its original relative identity, allowing a renamed child to be reused rather than constructed again. New local nodes retain ordinary exact factories. Additions inside an instance store their sibling position.

Owner selection and complete stored-property capture remain the existing contract. Selected nodes save their full current state; unselected subscene contents come from the current template. This is runtime snapshot capture, not a sparse editor-diff or inherited-root authoring model. Removing inherited nodes/groups does not create deletion records. A metadata-only legacy origin path remains a concrete capture until an actual template identity exists.

GetNodeInstance returns the borrowed linked template only for a recorded subscene root. GetNodeIndex returns recorded placement or -1 for an ordinary local node. GetNodeType returns an empty string for a linked root/reused override and the diagnostic unqualified type name for a fresh local node. GetNodeCount counts stored records, including overrides; it does not expand unselected template contents.

Template and stored-resource references retain ordinary Resource lifetime. Disposing a borrowed template makes capture/instantiation fail. Consumers that return instantiated hierarchies must keep their templates and borrowed source resources alive through later captures; portable snapshots of path-only fixture templates use BundleResources. File-backed instances retain their decoded graphs through the existing leases. Deep copying remaps links through the same resource identity session; repeated references retain aliases.

## Reconstruction and failure

Before factories run, a template snapshot graph checks disposed/empty templates, cycles, at most 128 template levels on every reference path (including reused shared templates) and at most 65,536 expanded nodes for linked graphs. A child finishes typed restoration, local setup and its root notification before outer overrides. The construction barrier stays active through the enclosing operation, so callbacks cannot activate or dispose an unfinished nested graph.

Reused nodes retain their template identity, children and ordinary owner. Added nodes follow stored parent/owner/sibling records. Typed node references resolve after names and topology are complete. Original paths select reused nodes even after an authored name swap; a path that vanished from a changed template rejects explicitly. Stable-ID recovery across independently renamed asset files is not implemented.

Local overrides reuse a compatible already-local resource at most once per source and scene root. Copies preserve nested resource aliases/cycles, including recorded missing-resource fields. Local setup and extra created resources belong to that same nested root. Non-local borrowing and forced/isolated copy policies retain ADR 0013. Existing inherited subscriptions are omitted when recapturing a parent; newly added subscriptions remain independent.

Topology validation includes every returned nested node, including unselected template children. Setter, setup, notification and cleanup failures release acquired nodes/resources while retaining borrowed templates/assets. RESET save preparation transfers created resources from every nested owner before disposing the temporary hierarchy.

## File layout

Archive version 5 retains version-4 resource roles/provenance and adds three fields after each scene record's sibling index: a zero-based resource reference (-1 for none), a prior linked-root record index (-1 for a fresh node), and its original relative node path (empty for a fresh node). The type ID remains a compiled schema guard. Linked references must identify PackedScene resources; reused records must lie under their selected linked root. Versions 1–4 keep their original concrete scene layouts.

Relative external references follow saved template files and normal deep-cache policy. BundleResources embeds independent snapshots. Unknown/malformed schemas and unsupported missing exact scene substitutions reject before publication. Class/dependency metadata queries understand the same versioned record format.

## Verification

ELECTRON2D_TEST_LINKED_SCENES=managed/gpu/compatibility selects the focused runner. Checks cover template identity/updates, implicit children, three levels, full typed overlays, placement/name swaps, local pixel and recorded-resource alias/cycle remapping, independent instances, inherited/additional bindings, deep graph copying, failure cleanup, external/relative/bundled fresh processes, versions 1–4 and corrupt-link cache rejection.

Wayland GPU and Compatibility profiles load an archived scene, inspect actual blue/red pixels and a retained local RID, and exercise redraw plus inherited event delivery for 64 prepared active frames. Instantiation/capture are allocating cold operations; the frame check concerns owner-thread managed allocation. Physical GPUs, foreign platforms, native allocation totals, editor UI and owner acceptance remain separate gates. Current results are retained in the slice verification manifest.
