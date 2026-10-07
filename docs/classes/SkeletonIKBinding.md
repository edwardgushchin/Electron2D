# SkeletonIKBinding

Last updated: 2026-10-07

- Declaration: `internal sealed class SkeletonIKBinding`
- Source: [SkeletonIKBinding.cs](../../src/Scene/Resources/SkeletonIKBinding.cs)
- Component: [Skeletal animation](../components/skeletal-animation.md)

Shared concrete weak selection and numeric helpers for TwoBoneIK/CCDIK/FABRIK. Resolve keys a weak node by weak skeleton owner, path/index, setup generation and scene PathRevision, caching missing paths until a revision changes. Bone additionally requires current SkeletonOwner membership. SelectionPath validates a nonnegative current index and authors the attached relative path, or retains detached deferred selection. Path validates length/NUL boundaries. BoundSkeleton on the existing base modifier supplies owner validation without reverse stack-gate lookup during configuration.

Distance/Direction use widened arithmetic for finite coordinate differences and safe coincident direction fallback. Point checks finite output before publication. Length retains positive GetLength times minimum absolute global scale; nonpositive lengths return zero and overflow fails. Axis transforms the actual endpoint direction; Handed respects reflection. Aim writes ordinary global rotation with endpoint angle/handedness, preserving Entity local scale/skew/position. Request submits a transient local override at concrete stack strength. The helpers introduce no public selector, solver interface or native pointer.

Cold cache preparation allocates weak references; hits reuse them and arrays in the owners. It never owns the scene nodes. SkeletonIKTests exercises rename/reconnect, mirrored axes, setters, malformed files and warmed allocation; target-platform/native allocator acceptance remains separate.

Jiggle now reuses the same weak Bone/Entity path/index selection and finite/reflected endpoint helpers. This adds a fourth concrete runtime consumer without a generic public solver layer. Jiggle history keys bone identity/setup and never holds live scene nodes.
