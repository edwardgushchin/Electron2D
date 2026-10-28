# Node contract audit

Last updated: 2026-10-28

## Declaration boundary

The pinned `4.7.2-stable` Node at
`ed1daf0bf001b61586d9930840f2f1394092c079` declares 213 members:
49 constants, seven enums, 26 enum values, 106 methods, fourteen properties
and eleven signals. `NOTIFICATION_MOVED_IN_PARENT` is deprecated and excluded
under ADR 0004; the applicable replacement is `NotificationChildOrderChanged`.
The other 212 members already have implemented mappings in the
[Node table](../coverage/classes/Node.md). This audit closes the remaining
class declaration; it adds no runtime capability or public API.

The compiled `Node : ElectronObject` is concrete and directly constructible.
Its 193 own compiled declarations, together with its nested enum declarations,
participate in the bidirectional coverage audit. Typed properties, delegates,
generic calls and explicit host/lifetime operations retain their documented
C# adaptations. `CanvasItem` and `Entity` retain drawing and spatial state;
neutral nodes, timers, viewports and windows keep their separate roles under
[ADR 0008](../decisions/scene.md#adr-0008).

As in the [MainLoop audit](main-loop.md#whole-type-contract-audit), a class row
records that type's own contract. Inherited declarations remain requirements
on their declaring pages. In particular, this audit does not close Object's
fixed CLR script replacement or complete `ScriptChanged`, or the remaining
Object/Resource class audits. It neither changes ADR 0091 nor accepts the
pending bounded scripting adaptation.

## Connected behavior

| Contract | Implementation and executable checks |
| --- | --- |
| Neutral hierarchy, internal child order, weak owner metadata, names, paths, replacement and departure | [Scene hierarchy](scene-hierarchy.md), `SceneHierarchyTests`, `NodeTreeDiagnosticsTests`, `NodeUniqueNameTests`, `NodeReplacementTests` |
| Entry/readiness/exit ordering, exception continuation, pause, callback switches, priorities and frame deltas | [Scene tree](scene-tree.md), `NodeInternalProcessingTests`, `BackendChecks.NodeProcessing`, `BackendChecks.ProcessModes`, root lifecycle checks |
| Group membership, persistence, real processing groups and typed immediate/deferred thread messages | [Node groups](node-groups.md), [thread messages](scene-thread-messages.md), `BackendChecks.NodeGroups`, `BackendChecks.ProcessGroups`, `BackendChecks.GroupContext` |
| Duplication, scene storage, resource endpoints and guarded subtree calls | [Node duplication](node-duplication.md), [resource endpoints](scene-hierarchy.md#resource-endpoints-and-subtree-operations), `NodeDuplicationTests`, `SubtreeResourceAuthoringTests` |
| Translation, input stages and typed multiplayer authority/RPC | [Localization](localization.md), [scene multiplayer](scene-multiplayer.md), `NodeLocalizationTests`, `TranslationDomainTests`, root Input checks, `SceneMultiplayerTests` |
| Editor metadata, interpolation and native lifecycle consumers | [Node authoring](scene-hierarchy.md#node-authoring-observations), [interpolation](canvas-rendering.md#physics-interpolation), `BackendChecks.EditorState`, native internal-processing and duplication checks; individual display/accessibility rows retain their source-specific evidence |

Processing groups grant the receiver permissions documented by ADRs 0006/0011;
the thread-safe wrappers do not bypass hierarchy, renderer or numerical-owner
guards. Pure group work can publish to a later main group after its barrier.
This audit grants no additional worker mutation permissions.

## Verification and limits

Current-source qualification is retained under
`bin/physics-node-contract/2026-10-28/`. The root and public consumer assemblies
are built together from `main`; runtime, test, consumer, PDB and native-bridge
hashes are checked before and after the profile run. CPU profiles deny Vulkan
device discovery and use the dummy display. GPU profiles select the NVIDIA
Vulkan driver. Both ordinary and separate numerical owners execute through
the public consumer; native profiles use the Linux Wayland window hosts.
The bridge is rebuilt from current sources using the existing pinned SDL
3.4.18 headers, without downloading another source tree.

All 36 profiles pass, followed by two passing Input/lifecycle and scene
multiplayer profiles on the same assemblies. Prepared public processing and
group cases retain their existing 128-frame strict zero owner/all-thread
managed-byte assertions. Native cases verify actual window output; no additional
mass benchmark is run for this documentation-only audit.

The coverage regression requires all 214 Node rows: the class and 212 applicable
members Implemented, one precisely deprecated member Excluded. It also retains
the inherited Object/Resource gaps. The regression fails on the preceding
generic class status before the explicit class audit mapping is applied.

These checks qualify the existing functional Node contract on Linux CLR.
Earlier per-member NativeAOT/platform receipts remain with their owning
components; they are not rerun or relabelled here. Full ordinary-suite,
native-heap, foreign-platform, human, network-impairment and GPU mass-performance
acceptance remain separate. Physics declaration coverage is **1364/1369
(99.63%)**, five Partial and 92 Excluded; this is one newly completed audit
declaration, not a new physics operation or overall goal completion.
