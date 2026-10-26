# Skeletal modification editor gizmos

Last updated: 2026-10-27

SkeletonModification.EditorDrawGizmo selects a stored authoring capability.
Protected OnDrawEditorGizmo runs through the bound Skeleton's ordinary canvas
recording scope. The callback obtains that node through
GetModificationStack().GetSkeleton() and uses its public drawing functions.
It requires both Engine.IsEditorHint and membership in SceneTree.EditedSceneRoot.
Setting the flag in a game does not create an editor owner or invoke the callback.
The owning decision is [ADR 0092](../decisions/mesh.md#skeletal-modification-editor-drawing).

The base default is false. LookAt, CCDIK and TwoBoneIK enable their gizmos by
default. Stack numerical enablement and Idle/Physics phase selection do not
control authoring drawing; built-in disabled modifications omit their own guides.
Custom callbacks can choose their diagnostic policy. The modification and stack
remain borrowed, and the resource cannot draw independently of its skeleton.

## Geometry and refresh

| Modification | Visible guide |
| --- | --- |
| LookAt | Selected bone length and configured angle interval or its complement, including bone angle and local/global constraint coordinates |
| CCDIK | The same angle guide for each selected joint with Get/SetCCDIKJointEditorDrawGizmo enabled; each new joint defaults true |
| TwoBoneIK | Bend-direction marker and optional target-distance endpoints/interval, selected by EditorDrawMinMax, initially false |
| StackHolder | Child stack drawing when the holder's EditorDrawGizmo is enabled |
| Custom compiled modification | Its OnDrawEditorGizmo commands in the same guarded canvas scope |

FABRIK, Jiggle and PhysicalBones retain the base no-guide default. No numerical
result is fabricated for their drawing. Missing selections and singular skeletal
transforms produce no undefined built-in guide geometry. Configured bone paths
and actual poses are read on the owner; no query, solve or pose override is implied.

An active visible editor rig requests recording each rendered frame. Bone/target
movement, changes to configured constraints and visibility, held stacks and
membership changes therefore refresh the real retained output. Leaving the
editor context or disabling its last guide requests one clearing redraw. Ordinary
runtime drawing avoids the editor stack traversal. Hidden canvases keep their
ordinary deferred-recording behavior.

The existing canvas notification precedes Draw and OnDraw. After the stack,
drawing transforms reset to identity before ordinary canvas callbacks. The stack
guards reentry, structural list edits and modification disposal; the skeleton
rejects stack replacement and its own disposal during the borrow. A callback
exception discards partial commands, releases these guards and retains retry.
No metadata lock spans caller-defined drawing code.

## Copies and stored data

The base visibility flag copies and stores with its resource. TwoBoneIK's optional
distance-guide visibility and CCDIK's per-joint flags use their concrete copied
schemas. CCDIK writes version two of its private joint record and reads both
versions: version one supplies the original true per-joint visibility default.
An invalid version or truncated record preserves the preceding configuration.
Weak scene bindings, running state and canvas commands are not stored.

## Shared editor preview

The actual ProjectWorkspace preview now accepts exact built-in Skeleton/Bone
nodes in addition to Node, Entity, Polygon, Line and Sprite. Before duplication it
validates a bounded, alias-aware graph containing only the seven built-in
modification resources. Unknown modification callbacks retain an explicit
unsupported-preview result. This preserves the current project-class boundary;
it does not load arbitrary game assemblies into the editor.

Preview rigs use disabled automatic processing while preserving their configured
stacks and authoring data. ProjectWorkspace registers the existing preview as
EditedSceneRoot on entry/reentry and after each accepted shared revision. The
same shared session supplies unsaved poses, selection and history; the preview
does not create a second editable scene. Source scene bytes remain untouched by
an unsaved edit. Snapshot jobs retain the existing gameplay path and are qualified
separately. See [project workflow](project-jobs.md).

## Verification scope

The public-only --editor-gizmos consumer checks real window pixels for custom and
built-in callbacks, editor ownership, enable/clear/reentry, selected physics and
renderer identities, guarded failure/retry, storage/copy/migration and strict
128-warmup/128-measured active custom-callback redraw allocations. The CLR-hosted --skeletal-gizmos
workspace fixture with an actual operating-system window checks initial owner registration, trusted passive projection,
custom rejection, shared unsaved movement, reentry and unchanged source bytes.
It captures both its authored viewport and the actual editor workspace window.
These checks qualify editor drawing; full numerical/network/native-heap,
foreign-platform, large-rig performance and owner visual acceptance remain separate.


## Qualified Linux x64 cohort

Evidence: [validation manifest](../../bin/physics-editor-gizmos/2026-10-27/validation.json)
and its per-process check/log, payload hashes, mapped driver identities, kernel
samples and captured pixels. Each core profile selects CPU or resident GPU
physics, compatibility or GPU rendering, and ordinary or separate numerical
execution, with fallback disabled.

| Cohort | Passed profiles | Result |
| --- | ---: | --- |
| Public-only CLR consumer | 8 | All editor checks; 128 active custom redraws at exactly 0 owner/all-thread managed bytes |
| Strict full-trim NativeAOT public-only consumer | 8 | Same checks and strict zero-allocation intervals; no CoreCLR mapping |
| Actual shared ProjectWorkspace in CLR | 2 | Compatibility/OpenGL and GPU/Vulkan native windows; unsaved shared pose, reentry, real canvas/window pixels and unchanged source bytes |
| Existing Skeleton, IK and Jiggle regressions | 3 | Existing checks/assertions retained |

Portable PDB/PE identity and every authored-source hash match the frozen CLR
runtime (2677 files), consumer (292), root checks (1004), Session SDK (31) and
workspace (11). Native compiler inputs retain matching runtime/consumer PDBs
and the ILC response file alongside the qualified executable.

Four initial GPU-renderer attempts were rejected before drawing because NVIDIA's
offscreen path lacked VK_EXT_headless_surface. Their replacements passed through
the actual Wayland host; they are separate terminal attempts in the evidence.
The full workspace's strict NativeAOT build failed with 33 existing analyzer
errors in Session SDK JSON reflection and Assembly.Location use. No warnings were
suppressed. NativeAOT acceptance here covers the runtime/public consumer; the
whole editor NativeAOT host remains unqualified. Software compatibility draws
the core gizmos; the full workspace requires the existing OpenGL compatibility
capability for linearly filtered viewport textures.

The declared SkeletonModification family now has all 12 rows Implemented.
Three editor methods and its class row raise physics declarations from
1354/1369 (98.90%) to **1358/1369 (99.20%)**, leaving 11 open and 92 Excluded.
These are declaration counts; they do not certify the broader goal.
