# Resource contract audit

Last updated: 2026-10-28

## Declaration boundary

The pinned `4.7.2-stable` Resource at
`ed1daf0bf001b61586d9930840f2f1394092c079` declares 28 members:
one enum, three enum values, eighteen methods, four properties and two signals.
The public setup method and setup-requested signal are deprecated and remain
Excluded under ADRs 0004/0013. The other 26 members already have Implemented
mappings in the [Resource table](../coverage/classes/Resource.md).

This audit qualifies the concrete `Resource : ElectronObject` role and closes
its class declaration. It adds no runtime capability. As in the
[Node audit](node-contract-audit.md#declaration-boundary), a class row records
that type's own contract; inherited declarations remain requirements on their
declaring pages. Object's fixed CLR SetScript/ScriptChanged and Object class
acceptance remain open. No scripting adaptation is accepted by this audit.

## Connected behavior

| Contract | Implementation and executable checks |
| --- | --- |
| Direct construction, name, reset, synchronous Changed, disposal and weak ordinal registered/raw/taken-over paths | [Resource component](resources.md), `VerifyResources`, `ResourceLifecycle`, `ResourceFactoryOwnership` |
| Exact-type shallow copying, all deep modes, per-edge alias/cycle policy, typed stored state and failure cleanup | [Copy contract](resources.md#copy-and-duplicate-contract), `ResourceStoredState`, `ResourceCopyFailures`, `ResourceEdgePolicy`, `ScriptGraphs`, `FactoryConstructionChecks` |
| Scene-local graph selection, setup after stored restoration, weak owning root and rollback | [Scene-local resources](resources.md), `RunLocalScenes`, packed-scene/subtree and resource-binding checks |
| Scene unique IDs, atomic persistence, per-referrer path IDs and fresh-process reconstruction | [Path IDs](resources.md#per-referrer-path-ids), `RunResourceIdentity`, `RunResourcePathIDs`, `ResourceArchiveTests` |
| Borrowed backend identity, physical resource families, collection and retirement | `RunResourceRIDs`, including nine shape kinds, 27 raw binding/GC cases and other inherited resource roles |
| Installed base-resource RID/reset/path/setup callbacks, native stored schemas and copy/cache ownership | [Script Resource bindings](scripting.md#resource-bindings), `RoleHookTests`, `ResourceBindingTests`, `ScriptPropertyTests` |

Derived resources own their concrete storage and backend specialization. Their
per-member acceptance is retained with their owning components; this base audit
does not certify every possible application subclass. The accepted typed copy
hooks reject unsupported derived factories explicitly and preserve borrowed
objects during failure cleanup.

## Verification and limits

Current-source receipts are retained under
`bin/physics-resource-contract/2026-10-28/current-main/`. The 85-file immutable
cohort is rebuilt from main at `949e3b2a7`, after the viewport-mipmap merge, with
the render bridge rebuilt from the same current sources and existing pinned SDL
headers. Runtime SHA-256 is
`1392c4d95cf879f6e526705dde2b43733e8eb20ee9616aa0cb333bee843305cd`;
render bridge SHA-256 is
`10776db22db32a8735179afb7a005369dbde6e1e8fd269e871be5ffbf7db52e0`.
Each CPU/registered process denies Vulkan device discovery; GPU processes use
the NVIDIA Vulkan driver. Owner and separate numerical lanes exercise actual
public physical resources, file reconstruction and existing warmed strict 0/0
owner/all-thread managed allocation assertions.

All 42 Resource profiles pass: six root resource/archive/loading profiles,
four public resource/identity/local/path-ID families on CPU, registered and GPU
backends in both numerical lanes, and twelve live-binding/property/role-hook
profiles on CPU/GPU in both lanes. Source and payload manifests bind the result
to this cohort. Earlier partial profiles used the preceding runtime and remain
separate receipts; they are not substituted for these current runs.

The owner and separate contact-key rechecks also pass on this same cohort
(29.103/29.275 seconds). All 44 profiles preserve the payload hashes and the
captured runtime/test/native source files. These two integration controls do
not add declaration coverage or a GPU speed claim.

The earlier unfiltered root attempt uses a different frozen cohort and reaches
its 2400-second deadline without a completion marker. That attempt is
[incomplete full-suite evidence](gpu-resident-reports.md#ordinary-root-attempt-2026-10-28),
not a passing full-runtime or current-main qualification.

The coverage regression requires all 29 Resource rows: 27 Implemented,
including the class, and the two precisely deprecated members Excluded.
It leaves Object script/class requirements and the complete
Performance.GetMonitor producer open.
Physics declaration coverage becomes **1365/1369 (99.71%)**, four Partial and
92 Excluded. This is one completed class audit, not a new physics operation.

This source qualifies Linux CLR functional behavior. Earlier per-member strict
NativeAOT and other platform receipts remain at their owning components; they
are not rerun here. Native heap accounting, foreign platforms, controlled whole
GPU speedup, real-window FPS and complete-goal acceptance remain separate.
